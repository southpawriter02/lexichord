// -----------------------------------------------------------------------
// <copyright file="SummarizerAgent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implements the Summarizer Agent — a specialized IAgent for multi-mode
//   document summarization (v0.7.6a).
//   Orchestrates context assembly, prompt rendering, LLM invocation, and
//   response parsing for the Summarization Pipeline.
//
//   Invocation flow (§5.2):
//     1. Validate options (MaxItems, TargetWordCount, CustomPrompt)
//     2. Publish SummarizationStartedEvent
//     3. Estimate tokens — content.Length / 4 approximation
//     4. Chunk if needed — split on headings → paragraphs → sentences
//     5. For each chunk: assemble context, render prompt, invoke LLM
//     6. If chunked: combine chunk summaries with final LLM pass
//     7. Parse response — extract items for list modes
//     8. Calculate metrics — word counts, compression ratio, reading time
//     9. Publish SummarizationCompletedEvent
//    10. Return SummarizationResult
//
//   License gating (§3.2):
//     - Agent access requires WriterPro (enforced by [RequiresLicense])
//     - Feature code: FeatureCodes.SummarizerAgent ("Feature.SummarizerAgent")
//
//   Error handling:
//     - OperationCanceledException from user CancellationToken → "Summarization cancelled"
//     - OperationCanceledException from timeout CTS → "Summarization timed out"
//     - Generic exceptions → logged and wrapped in failed SummarizationResult
//
//   Thread safety (§9.1):
//     - All injected services are stateless or thread-safe
//     - No shared mutable state — all variables are per-invocation
//
//   Introduced in: v0.7.6a
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Summarizer.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Summarizer;

/// <summary>
/// The Summarizer Agent — specializes in multi-mode document summarization.
/// </summary>
/// <remarks>
/// <para>
/// Implements both <see cref="IAgent"/> and <see cref="ISummarizerAgent"/> to function
/// as a registered agent (discoverable via <see cref="IAgentRegistry"/>) while exposing
/// summarization-specific operations consumed by command handlers.
/// </para>
/// <para>
/// The agent supports six summarization modes, each with distinct output formats:
/// </para>
/// <list type="bullet">
///   <item><description>Abstract: Academic prose (150-300 words)</description></item>
///   <item><description>TLDR: Single paragraph (50-100 words)</description></item>
///   <item><description>BulletPoints: "•" list with configurable items</description></item>
///   <item><description>KeyTakeaways: Numbered insights with explanations</description></item>
///   <item><description>Executive: Business-oriented summary (100-200 words)</description></item>
///   <item><description>Custom: User-defined prompt format</description></item>
/// </list>
/// <para>
/// Long documents are automatically chunked (4000 tokens per chunk with 100-token overlap)
/// and individually summarized before combining into a final summary.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6a</para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro, FeatureCode = FeatureCodes.SummarizerAgent)]
[AgentDefinition("summarizer", Priority = 102)]
public sealed class SummarizerAgent : IAgent, ISummarizerAgent
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IChatCompletionService _chatService;
    private readonly IPromptRenderer _promptRenderer;
    private readonly IPromptTemplateRepository _templateRepository;
    private readonly IContextOrchestrator _contextOrchestrator;
    private readonly IFileService _fileService;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<SummarizerAgent> _logger;

    // ── Constants ────────────────────────────────────────────────────────

    /// <summary>
    /// Template ID for the summarizer prompt template.
    /// </summary>
    private const string TemplateId = "specialist-summarizer";

    /// <summary>
    /// Default cost per 1,000 prompt tokens (USD).
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches the CoPilotAgent and EditorAgent default pricing.
    /// A future enhancement can read model-specific pricing from configuration.
    /// </remarks>
    private const decimal DefaultPromptCostPer1K = 0.01m;

    /// <summary>
    /// Default cost per 1,000 completion tokens (USD).
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches the CoPilotAgent and EditorAgent default pricing.
    /// </remarks>
    private const decimal DefaultCompletionCostPer1K = 0.03m;

    /// <summary>
    /// Maximum token budget for context assembly.
    /// </summary>
    /// <remarks>
    /// LOGIC: 4000 tokens provides sufficient context for surrounding text,
    /// style rules, and terminology without consuming too much of the model's
    /// context window.
    /// </remarks>
    private const int ContextTokenBudget = 4000;

    /// <summary>
    /// Maximum tokens per chunk when splitting long documents.
    /// </summary>
    /// <remarks>
    /// LOGIC: 4000 tokens per chunk (§5.3). Each chunk is summarized independently
    /// before combining. The approximate token count uses content.Length / 4.
    /// </remarks>
    private const int MaxTokensPerChunk = 4000;

    /// <summary>
    /// Token overlap between adjacent chunks for context continuity.
    /// </summary>
    /// <remarks>
    /// LOGIC: 100-token overlap (§5.3) ensures context is not lost at chunk boundaries.
    /// Converted to characters: 100 * 4 = 400 characters of overlap.
    /// </remarks>
    private const int ChunkOverlapTokens = 100;

    /// <summary>
    /// Timeout for the overall summarization operation.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Regex pattern for extracting numeric values from natural language commands.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches standalone digits 1-10 in commands like "Summarize in 3 bullets".
    /// </remarks>
    private static readonly Regex NumberPattern = new(@"\b(\d{1,2})\b", RegexOptions.Compiled);

    /// <summary>
    /// Regex pattern for extracting audience from "for {audience}" phrases.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches "for X" at the end of commands or before punctuation.
    /// Captures the audience phrase after "for ".
    /// </remarks>
    private static readonly Regex AudiencePattern = new(
        @"\bfor\s+(?:a\s+)?(.+?)(?:\s*[.!?]?\s*$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Regex pattern for section headings used in chunk boundary detection.
    /// </summary>
    private static readonly Regex HeadingPattern = new(@"^#{1,6}\s+", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Initializes a new instance of <see cref="SummarizerAgent"/>.
    /// </summary>
    /// <param name="chatService">LLM chat completion service for batch requests.</param>
    /// <param name="promptRenderer">Template renderer for prompt assembly.</param>
    /// <param name="templateRepository">Repository for loading prompt templates.</param>
    /// <param name="contextOrchestrator">Context assembly from document, style, and terminology sources.</param>
    /// <param name="fileService">File system service for reading document content.</param>
    /// <param name="licenseContext">License verification context.</param>
    /// <param name="mediator">MediatR mediator for publishing events.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public SummarizerAgent(
        IChatCompletionService chatService,
        IPromptRenderer promptRenderer,
        IPromptTemplateRepository templateRepository,
        IContextOrchestrator contextOrchestrator,
        IFileService fileService,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<SummarizerAgent> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _contextOrchestrator = contextOrchestrator ?? throw new ArgumentNullException(nameof(contextOrchestrator));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── IAgent Properties ───────────────────────────────────────────────

    /// <inheritdoc />
    public string AgentId => "summarizer";

    /// <inheritdoc />
    public string Name => "The Summarizer";

    /// <inheritdoc />
    public string Description =>
        "Summarizes documents in multiple formats: abstract, TLDR, bullet points, key takeaways, and executive summary.";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns the specialist-summarizer template. The template contains
    /// mode-specific sections activated by conditional Mustache variables.
    /// </remarks>
    public IPromptTemplate Template => _templateRepository.GetTemplate(TemplateId)!;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: The Summarizer Agent supports chat, document context, and summarization.
    /// Summarization capability flag (128) indicates this agent can generate summaries.
    /// </remarks>
    public AgentCapabilities Capabilities =>
        AgentCapabilities.Chat |
        AgentCapabilities.DocumentContext |
        AgentCapabilities.Summarization;

    // ── IAgent.InvokeAsync ──────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Delegates to <see cref="SummarizeContentAsync"/> with default BulletPoints options.
    /// The user message from the <see cref="AgentRequest"/> is parsed as a command to infer
    /// the desired summarization mode. Content comes from Selection or UserMessage.
    /// </remarks>
    public async Task<AgentResponse> InvokeAsync(
        AgentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        _logger.LogDebug(
            "SummarizerAgent.InvokeAsync: parsing command from user message");

        // LOGIC: Parse the user's command to determine summarization mode
        var options = ParseCommand(request.UserMessage);

        // LOGIC: Use selection as content if available, otherwise use user message
        var content = request.Selection ?? request.UserMessage;

        var result = await SummarizeContentAsync(content, options, ct);

        return new AgentResponse(
            result.Success ? result.Summary : $"Summarization failed: {result.ErrorMessage}",
            Citations: null,
            result.Usage);
    }

    // ── ISummarizerAgent Implementation ─────────────────────────────────

    /// <inheritdoc />
    public async Task<SummarizationResult> SummarizeAsync(
        string documentPath,
        SummarizationOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogDebug(
            "SummarizeAsync: loading document from path {DocumentPath}",
            documentPath);

        // LOGIC: Read the document content via IFileService
        if (!_fileService.Exists(documentPath))
        {
            _logger.LogError("Document not found at path: {DocumentPath}", documentPath);
            throw new FileNotFoundException(
                $"Document not found at path: {documentPath}",
                documentPath);
        }

        var loadResult = await _fileService.LoadAsync(documentPath, cancellationToken: ct);

        if (!loadResult.Success || string.IsNullOrEmpty(loadResult.Content))
        {
            _logger.LogError(
                "Failed to load document at {DocumentPath}: {Error}",
                documentPath,
                loadResult.Error?.Message ?? "Unknown error");

            return SummarizationResult.Failed(
                options.Mode,
                $"Failed to load document: {loadResult.Error?.Message ?? "Unknown error"}");
        }

        _logger.LogDebug(
            "Document loaded: {CharCount} characters from {DocumentPath}",
            loadResult.Content.Length,
            documentPath);

        return await SummarizeContentAsync(loadResult.Content, options, ct);
    }

    /// <inheritdoc />
    public async Task<SummarizationResult> SummarizeContentAsync(
        string content,
        SummarizationOptions options,
        CancellationToken ct = default)
    {
        // LOGIC: Validate inputs before any LLM invocation
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));
        }

        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Resolve effective word count target based on mode defaults
        var effectiveTargetWordCount = options.TargetWordCount ?? GetDefaultWordCount(options.Mode);

        _logger.LogDebug(
            "Summarization started: Mode={Mode}, MaxItems={MaxItems}, DocumentLength={Length}",
            options.Mode,
            options.MaxItems,
            content.Length);

        // LOGIC: Publish started event for observability and UI state
        await _mediator.Publish(
            SummarizationStartedEvent.Create(options.Mode, content.Length, documentPath: null),
            ct);

        try
        {
            // LOGIC: Calculate original document metrics
            var originalWordCount = CountWords(content);
            var originalReadingMinutes = Math.Max(1, originalWordCount / 200);

            // 1. Estimate tokens — content.Length / 4 approximation (§5.3)
            var estimatedTokens = EstimateTokens(content);
            var requiresChunking = estimatedTokens > MaxTokensPerChunk;

            _logger.LogDebug(
                "Token estimate: {TokenCount}, requires chunking: {RequiresChunking}",
                estimatedTokens,
                requiresChunking);

            // 2. Get the prompt template
            var template = _templateRepository.GetTemplate(TemplateId);

            if (template is null)
            {
                _logger.LogError("Prompt template not found: {TemplateId}", TemplateId);

                stopwatch.Stop();
                var failedResult = SummarizationResult.Failed(
                    options.Mode,
                    $"Prompt template '{TemplateId}' not found.",
                    originalWordCount);

                await _mediator.Publish(
                    SummarizationFailedEvent.Create(
                        options.Mode,
                        failedResult.ErrorMessage!,
                        documentPath: null),
                    ct);

                return failedResult;
            }

            // 3. Process content — single chunk or multi-chunk
            string summaryContent;
            var totalUsage = UsageMetrics.Zero;
            var chunkCount = 1;

            if (requiresChunking)
            {
                // LOGIC: Chunk the document and summarize each chunk independently
                var chunks = ChunkContent(content);
                chunkCount = chunks.Count;

                _logger.LogDebug(
                    "Document split into {ChunkCount} chunks for processing",
                    chunkCount);

                // LOGIC: Summarize each chunk
                var chunkSummaries = new List<string>();

                for (var i = 0; i < chunks.Count; i++)
                {
                    _logger.LogDebug(
                        "Processing chunk {ChunkNumber} of {TotalChunks}",
                        i + 1,
                        chunks.Count);

                    var (chunkSummary, chunkUsage) = await SummarizeChunkAsync(
                        chunks[i],
                        options,
                        effectiveTargetWordCount,
                        template,
                        chunkNumber: i + 1,
                        totalChunks: chunks.Count,
                        ct);

                    chunkSummaries.Add(chunkSummary);
                    totalUsage = totalUsage.Add(chunkUsage);
                }

                // LOGIC: Combine chunk summaries with a final LLM pass
                var (combined, combineUsage) = await CombineChunkSummariesAsync(
                    chunkSummaries,
                    options,
                    effectiveTargetWordCount,
                    template,
                    ct);

                summaryContent = combined;
                totalUsage = totalUsage.Add(combineUsage);
            }
            else
            {
                // LOGIC: Single-chunk processing — direct summarization
                var (summary, usage) = await SummarizeSingleAsync(
                    content,
                    options,
                    effectiveTargetWordCount,
                    template,
                    ct);

                summaryContent = summary;
                totalUsage = usage;
            }

            stopwatch.Stop();

            // 4. Calculate result metrics
            var summaryWordCount = CountWords(summaryContent);
            var compressionRatio = summaryWordCount > 0
                ? (double)originalWordCount / summaryWordCount
                : 0;

            // LOGIC: Warn if summary exceeds target word count significantly
            if (effectiveTargetWordCount > 0 && summaryWordCount > effectiveTargetWordCount * 1.5)
            {
                _logger.LogWarning(
                    "Summary exceeds target word count: {ActualWords} > {TargetWords}",
                    summaryWordCount,
                    effectiveTargetWordCount);
            }

            // 5. Parse items for list-based modes
            var items = ParseItems(summaryContent, options.Mode);

            _logger.LogInformation(
                "Summarization completed: {OriginalWords} -> {SummaryWords} words ({CompressionRatio:F1}x compression)",
                originalWordCount,
                summaryWordCount,
                compressionRatio);

            // 6. Publish completed event
            await _mediator.Publish(
                SummarizationCompletedEvent.Create(
                    options.Mode,
                    originalWordCount,
                    summaryWordCount,
                    compressionRatio,
                    requiresChunking,
                    stopwatch.Elapsed),
                ct);

            return new SummarizationResult
            {
                Summary = summaryContent,
                Mode = options.Mode,
                Items = items,
                OriginalReadingMinutes = originalReadingMinutes,
                OriginalWordCount = originalWordCount,
                SummaryWordCount = summaryWordCount,
                CompressionRatio = compressionRatio,
                Usage = totalUsage,
                GeneratedAt = DateTimeOffset.UtcNow,
                Model = null,
                WasChunked = requiresChunking,
                ChunkCount = chunkCount,
                Success = true,
                ErrorMessage = null
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // LOGIC: User-initiated cancellation — distinguish from timeout.
            stopwatch.Stop();
            _logger.LogWarning("Summarization cancelled by user");

            await PublishFailedEventSafe(
                options.Mode,
                "Summarization cancelled by user.",
                documentPath: null);

            return SummarizationResult.Failed(
                options.Mode,
                "Summarization cancelled by user.",
                CountWords(content));
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Timeout — the timeoutCts fired before the user ct.
            stopwatch.Stop();
            _logger.LogWarning(
                "Summarization timed out after {Timeout}",
                DefaultTimeout);

            await PublishFailedEventSafe(
                options.Mode,
                $"Summarization timed out after {DefaultTimeout.TotalSeconds:F0} seconds.",
                documentPath: null);

            return SummarizationResult.Failed(
                options.Mode,
                $"Summarization timed out after {DefaultTimeout.TotalSeconds:F0} seconds.",
                CountWords(content));
        }
        catch (Exception ex)
        {
            // LOGIC: Generic failure — log and return failed result.
            stopwatch.Stop();
            _logger.LogError(ex, "Summarization failed: {ErrorMessage}", ex.Message);

            await PublishFailedEventSafe(
                options.Mode,
                ex.Message,
                documentPath: null);

            return SummarizationResult.Failed(
                options.Mode,
                ex.Message,
                CountWords(content));
        }
    }

    /// <inheritdoc />
    public SummarizationOptions ParseCommand(string naturalLanguageCommand)
    {
        ArgumentNullException.ThrowIfNull(naturalLanguageCommand);

        var command = naturalLanguageCommand.Trim();
        var lower = command.ToLowerInvariant();

        _logger.LogDebug(
            "Parsing command: '{Command}'",
            command);

        // LOGIC: Detect mode from keywords (§5.1 flowchart)
        // Priority order: Abstract → TLDR → Executive → KeyTakeaways → BulletPoints → Default
        var mode = DetectMode(lower);

        // LOGIC: Extract numeric value for MaxItems
        var maxItems = ExtractMaxItems(lower);

        // LOGIC: Extract target audience from "for {audience}" pattern
        var targetAudience = ExtractAudience(command);

        var options = new SummarizationOptions
        {
            Mode = mode,
            MaxItems = maxItems,
            TargetAudience = targetAudience
        };

        _logger.LogInformation(
            "Command parsed: '{Command}' -> Mode={Mode}, MaxItems={MaxItems}",
            command,
            mode,
            maxItems);

        return options;
    }

    /// <inheritdoc />
    public SummarizationOptions GetDefaultOptions(SummarizationMode mode)
    {
        return mode switch
        {
            SummarizationMode.Abstract => new SummarizationOptions
            {
                Mode = SummarizationMode.Abstract,
                TargetWordCount = 200
            },
            SummarizationMode.TLDR => new SummarizationOptions
            {
                Mode = SummarizationMode.TLDR,
                TargetWordCount = 75
            },
            SummarizationMode.BulletPoints => new SummarizationOptions
            {
                Mode = SummarizationMode.BulletPoints,
                MaxItems = 5
            },
            SummarizationMode.KeyTakeaways => new SummarizationOptions
            {
                Mode = SummarizationMode.KeyTakeaways,
                MaxItems = 5
            },
            SummarizationMode.Executive => new SummarizationOptions
            {
                Mode = SummarizationMode.Executive,
                TargetWordCount = 150
            },
            SummarizationMode.Custom => new SummarizationOptions
            {
                Mode = SummarizationMode.Custom
            },
            _ => new SummarizationOptions
            {
                Mode = SummarizationMode.BulletPoints,
                MaxItems = 5
            }
        };
    }

    // ── Private Methods: Summarization Pipeline ─────────────────────────

    /// <summary>
    /// Summarizes a single (non-chunked) content piece via LLM.
    /// </summary>
    /// <param name="content">The text content to summarize.</param>
    /// <param name="options">The summarization configuration.</param>
    /// <param name="targetWordCount">Effective target word count.</param>
    /// <param name="template">The prompt template.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of summary content and usage metrics.</returns>
    private async Task<(string Summary, UsageMetrics Usage)> SummarizeSingleAsync(
        string content,
        SummarizationOptions options,
        int targetWordCount,
        IPromptTemplate template,
        CancellationToken ct)
    {
        // LOGIC: Assemble context for the content
        var context = await GatherContextAsync(content, ct);

        // LOGIC: Build prompt variables for this mode
        var variables = BuildPromptVariables(content, options, targetWordCount, context);

        // LOGIC: Render prompt messages from template
        var messages = _promptRenderer.RenderMessages(template, variables);

        // LOGIC: Configure LLM options using the Summarization preset
        var chatOptions = ChatOptions.Summarization
            .WithMaxTokens(options.MaxResponseTokens);

        // LOGIC: Invoke LLM with timeout
        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var chatRequest = new ChatRequest(messages.ToImmutableArray(), chatOptions);
        var response = await _chatService.CompleteAsync(chatRequest, linkedCts.Token);

        // LOGIC: Calculate usage metrics
        var usage = UsageMetrics.Calculate(
            response.PromptTokens,
            response.CompletionTokens,
            DefaultPromptCostPer1K,
            DefaultCompletionCostPer1K);

        return (response.Content, usage);
    }

    /// <summary>
    /// Summarizes a single chunk as part of multi-chunk processing.
    /// </summary>
    /// <param name="chunk">The chunk content to summarize.</param>
    /// <param name="options">The summarization configuration.</param>
    /// <param name="targetWordCount">Effective target word count.</param>
    /// <param name="template">The prompt template.</param>
    /// <param name="chunkNumber">1-based chunk number.</param>
    /// <param name="totalChunks">Total number of chunks.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of chunk summary and usage metrics.</returns>
    private async Task<(string Summary, UsageMetrics Usage)> SummarizeChunkAsync(
        string chunk,
        SummarizationOptions options,
        int targetWordCount,
        IPromptTemplate template,
        int chunkNumber,
        int totalChunks,
        CancellationToken ct)
    {
        // LOGIC: Build variables with chunking context
        var variables = BuildPromptVariables(chunk, options, targetWordCount, AssembledContext.Empty);

        // LOGIC: Add chunking metadata
        variables["is_chunked"] = true;
        variables["chunk_number"] = chunkNumber;
        variables["total_chunks"] = totalChunks;

        // LOGIC: Detect current section heading for chunk context
        var chunkContext = ExtractFirstHeading(chunk);
        if (chunkContext is not null)
        {
            variables["chunk_context"] = chunkContext;
        }

        var messages = _promptRenderer.RenderMessages(template, variables);

        var chatOptions = ChatOptions.Summarization
            .WithMaxTokens(options.MaxResponseTokens);

        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var chatRequest = new ChatRequest(messages.ToImmutableArray(), chatOptions);
        var response = await _chatService.CompleteAsync(chatRequest, linkedCts.Token);

        var usage = UsageMetrics.Calculate(
            response.PromptTokens,
            response.CompletionTokens,
            DefaultPromptCostPer1K,
            DefaultCompletionCostPer1K);

        return (response.Content, usage);
    }

    /// <summary>
    /// Combines multiple chunk summaries into a single final summary.
    /// </summary>
    /// <param name="chunkSummaries">Individual summaries from each chunk.</param>
    /// <param name="options">The summarization configuration.</param>
    /// <param name="targetWordCount">Effective target word count.</param>
    /// <param name="template">The prompt template.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of combined summary and usage metrics.</returns>
    private async Task<(string Summary, UsageMetrics Usage)> CombineChunkSummariesAsync(
        List<string> chunkSummaries,
        SummarizationOptions options,
        int targetWordCount,
        IPromptTemplate template,
        CancellationToken ct)
    {
        _logger.LogDebug(
            "Combining {ChunkCount} chunk summaries into final summary",
            chunkSummaries.Count);

        // LOGIC: Build combined content from chunk summaries
        var combinedContent = new StringBuilder();
        for (var i = 0; i < chunkSummaries.Count; i++)
        {
            combinedContent.AppendLine($"--- Section {i + 1} Summary ---");
            combinedContent.AppendLine(chunkSummaries[i]);
            combinedContent.AppendLine();
        }

        // LOGIC: Summarize the combined chunk summaries as the final pass
        var variables = BuildPromptVariables(
            combinedContent.ToString(),
            options,
            targetWordCount,
            AssembledContext.Empty);

        var messages = _promptRenderer.RenderMessages(template, variables);

        var chatOptions = ChatOptions.Summarization
            .WithMaxTokens(options.MaxResponseTokens);

        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var chatRequest = new ChatRequest(messages.ToImmutableArray(), chatOptions);
        var response = await _chatService.CompleteAsync(chatRequest, linkedCts.Token);

        var usage = UsageMetrics.Calculate(
            response.PromptTokens,
            response.CompletionTokens,
            DefaultPromptCostPer1K,
            DefaultCompletionCostPer1K);

        return (response.Content, usage);
    }

    // ── Private Methods: Context Assembly ────────────────────────────────

    /// <summary>
    /// Gathers context for a summarization operation via <see cref="IContextOrchestrator"/>.
    /// </summary>
    /// <param name="content">The text content being summarized.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assembled context with fragments sorted by priority.</returns>
    /// <remarks>
    /// LOGIC: Creates a context gathering request with:
    /// <list type="bullet">
    ///   <item><description>Agent ID "summarizer" for strategy filtering</description></item>
    ///   <item><description>4000-token budget</description></item>
    ///   <item><description>No required strategies (summarization is content-first)</description></item>
    /// </list>
    /// If context assembly fails, logs a warning and returns
    /// <see cref="AssembledContext.Empty"/> for graceful degradation.
    /// </remarks>
    private async Task<AssembledContext> GatherContextAsync(
        string content,
        CancellationToken ct)
    {
        var gatheringRequest = new ContextGatheringRequest(
            DocumentPath: null,
            CursorPosition: 0,
            SelectedText: content,
            AgentId: AgentId,
            Hints: null);

        var budget = new ContextBudget(
            MaxTokens: ContextTokenBudget,
            RequiredStrategies: null,
            ExcludedStrategies: null);

        try
        {
            return await _contextOrchestrator.AssembleAsync(gatheringRequest, budget, ct);
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Cancellation is not a context failure — re-throw to
            // let the outer handler log and propagate correctly.
            throw;
        }
        catch (Exception ex)
        {
            // LOGIC: Context assembly failure should not block summarization.
            // The agent can still function with the content alone.
            _logger.LogWarning(ex, "Context assembly failed, proceeding with limited context");
            return AssembledContext.Empty;
        }
    }

    // ── Private Methods: Prompt Construction ─────────────────────────────

    /// <summary>
    /// Builds prompt template variables from the content, options, and context.
    /// </summary>
    /// <param name="content">The document content to summarize.</param>
    /// <param name="options">The summarization configuration.</param>
    /// <param name="targetWordCount">Effective target word count.</param>
    /// <param name="context">The assembled context.</param>
    /// <returns>Dictionary of variable names to values for Mustache rendering.</returns>
    /// <remarks>
    /// LOGIC: Maps options and content to template variables:
    /// <list type="bullet">
    ///   <item><description>document_content: The text to summarize</description></item>
    ///   <item><description>mode_instructions: Mode-specific LLM instructions</description></item>
    ///   <item><description>mode_is_*: Conditional flags for mode sections</description></item>
    ///   <item><description>target_word_count, max_items: Output constraints</description></item>
    ///   <item><description>target_audience, preserve_technical_terms: Style preferences</description></item>
    /// </list>
    /// </remarks>
    private Dictionary<string, object> BuildPromptVariables(
        string content,
        SummarizationOptions options,
        int targetWordCount,
        AssembledContext context)
    {
        var variables = new Dictionary<string, object>
        {
            // LOGIC: Required variables
            ["document_content"] = content,
            ["mode_instructions"] = GetModeInstructions(options, targetWordCount),

            // LOGIC: Mode conditional flags for Mustache {{#mode_is_*}} sections
            ["mode_is_abstract"] = options.Mode == SummarizationMode.Abstract,
            ["mode_is_tldr"] = options.Mode == SummarizationMode.TLDR,
            ["mode_is_bullets"] = options.Mode == SummarizationMode.BulletPoints,
            ["mode_is_takeaways"] = options.Mode == SummarizationMode.KeyTakeaways,
            ["mode_is_executive"] = options.Mode == SummarizationMode.Executive,
            ["mode_is_custom"] = options.Mode == SummarizationMode.Custom,

            // LOGIC: Output constraints
            ["target_word_count"] = targetWordCount,
            ["max_items"] = options.MaxItems,

            // LOGIC: Style preferences
            ["preserve_technical_terms"] = options.PreserveTechnicalTerms
        };

        // LOGIC: Add optional variables when present
        if (!string.IsNullOrEmpty(options.TargetAudience))
        {
            variables["target_audience"] = options.TargetAudience;
        }

        if (!string.IsNullOrEmpty(options.CustomPrompt))
        {
            variables["custom_prompt"] = options.CustomPrompt;
        }

        // LOGIC: Merge context fragments as template variables
        foreach (var fragment in context.Fragments)
        {
            switch (fragment.SourceId)
            {
                case "style":
                    variables.TryAdd("style_context", fragment.Content);
                    break;
                case "terminology":
                    variables.TryAdd("terminology_context", fragment.Content);
                    break;
            }
        }

        // LOGIC: Merge orchestrator-extracted variables
        foreach (var (key, value) in context.Variables)
        {
            variables.TryAdd(key, value);
        }

        return variables;
    }

    /// <summary>
    /// Gets mode-specific instructions to inject into the system prompt.
    /// </summary>
    /// <param name="options">The summarization configuration.</param>
    /// <param name="targetWordCount">Effective target word count.</param>
    /// <returns>Mode-specific instruction text for the system prompt.</returns>
    /// <remarks>
    /// LOGIC: Returns the appropriate instruction block from spec §6.1 mode_instructions.
    /// Each mode has distinct requirements for structure, tone, and formatting.
    /// </remarks>
    private static string GetModeInstructions(SummarizationOptions options, int targetWordCount)
    {
        return options.Mode switch
        {
            SummarizationMode.Abstract => $"""
                Generate an academic-style abstract following this structure:

                1. **Opening**: State the document's purpose or problem addressed in the first sentence
                2. **Approach**: Briefly describe the methodology, framework, or approach used
                3. **Findings**: Present the key findings, arguments, or contributions
                4. **Implications**: Conclude with significance, applications, or future directions

                Requirements:
                - Length: {targetWordCount} words (target range: 150-300 words)
                - Style: Formal, third-person, passive voice acceptable
                - Tense: Present tense for general claims, past tense for specific findings
                - No citations or references in the abstract itself
                """,

            SummarizationMode.TLDR => $"""
                Generate a TL;DR (Too Long; Didn't Read) summary with these characteristics:

                1. **First Sentence**: The single most important takeaway or conclusion
                2. **Supporting Context**: 2-3 additional sentences providing essential context
                3. **Accessibility**: Use clear, accessible language suitable for quick scanning

                Requirements:
                - Length: {targetWordCount} words (target range: 50-100 words)
                - Format: Single paragraph, no bullet points or headers
                - Tone: Direct and informative, slightly informal acceptable
                - Focus: "What do I need to know?" perspective
                """,

            SummarizationMode.BulletPoints => $"""
                Generate a bullet-point summary with exactly {options.MaxItems} items:

                Requirements for each bullet:
                1. Start with an action verb or key noun (not "This document..." or "The author...")
                2. One complete thought per bullet, maximum 25 words
                3. Cover distinct aspects - avoid redundancy between bullets
                4. Order by importance (most critical point first)

                Format:
                - Use "•" for bullet markers
                - No sub-bullets or nested lists
                - No bold or italic formatting within bullets
                - Each bullet should stand alone as a complete statement
                """,

            SummarizationMode.KeyTakeaways => $"""
                Extract exactly {options.MaxItems} actionable key takeaways:

                Format for each takeaway:
                **Takeaway N:** [One-sentence insight that can be acted upon]

                [1-2 sentence explanation of why this matters or how to apply it]

                Requirements:
                - Focus on practical, actionable insights
                - Each takeaway should be independently valuable
                - Order by actionability and impact
                - Include concrete recommendations where applicable
                """,

            SummarizationMode.Executive => $"""
                Generate an executive summary suitable for stakeholders and decision-makers:

                Structure:
                1. **Context**: One sentence on what this document addresses
                2. **Key Findings**: 2-3 sentences on the most important outcomes
                3. **Strategic Implications**: Risks, opportunities, or decisions required
                4. **Recommended Actions**: Clear next steps if applicable

                Requirements:
                - Length: {targetWordCount} words (target range: 100-200 words)
                - Tone: Professional, confident, action-oriented
                - Focus: Business impact and strategic relevance
                - Avoid: Technical jargon unless essential for understanding
                """,

            SummarizationMode.Custom => $"""
                Follow the user's custom instructions precisely:

                {options.CustomPrompt ?? "No custom instructions provided."}

                Apply general summarization best practices while adhering to the specific requirements above.
                """,

            _ => "Summarize the following document clearly and concisely."
        };
    }

    // ── Private Methods: Command Parsing ─────────────────────────────────

    /// <summary>
    /// Detects the summarization mode from a lowercased command string.
    /// </summary>
    /// <param name="lower">The lowercased command text.</param>
    /// <returns>The detected <see cref="SummarizationMode"/>.</returns>
    /// <remarks>
    /// LOGIC: Keyword priority order (§5.1 flowchart):
    /// abstract → tldr/tl;dr → executive/stakeholder/management → takeaway/insight/learning → bullet/point → default
    /// </remarks>
    private static SummarizationMode DetectMode(string lower)
    {
        // LOGIC: Check for Abstract keywords
        if (lower.Contains("abstract"))
        {
            return SummarizationMode.Abstract;
        }

        // LOGIC: Check for TLDR keywords
        if (lower.Contains("tldr") || lower.Contains("tl;dr") || lower.Contains("too long didn't read"))
        {
            return SummarizationMode.TLDR;
        }

        // LOGIC: Check for Executive keywords
        if (lower.Contains("executive") || lower.Contains("stakeholder") || lower.Contains("management"))
        {
            return SummarizationMode.Executive;
        }

        // LOGIC: Check for KeyTakeaways keywords
        if (lower.Contains("takeaway") || lower.Contains("insight") || lower.Contains("learning"))
        {
            return SummarizationMode.KeyTakeaways;
        }

        // LOGIC: Check for BulletPoints keywords
        if (lower.Contains("bullet") || lower.Contains("point"))
        {
            return SummarizationMode.BulletPoints;
        }

        // LOGIC: Default to BulletPoints for generic "summarize" commands
        return SummarizationMode.BulletPoints;
    }

    /// <summary>
    /// Extracts the MaxItems value from a command string.
    /// </summary>
    /// <param name="lower">The lowercased command text.</param>
    /// <returns>Extracted item count, or 5 as default.</returns>
    /// <remarks>
    /// LOGIC: Matches standalone 1-2 digit numbers. Only accepts values 1-10.
    /// Falls back to 5 if no valid number is found.
    /// </remarks>
    private static int ExtractMaxItems(string lower)
    {
        var match = NumberPattern.Match(lower);

        if (match.Success && int.TryParse(match.Groups[1].Value, out var number) && number >= 1 && number <= 10)
        {
            return number;
        }

        return 5;
    }

    /// <summary>
    /// Extracts the target audience from a "for {audience}" phrase.
    /// </summary>
    /// <param name="command">The original command text (preserving case).</param>
    /// <returns>Extracted audience string, or null if not found.</returns>
    /// <remarks>
    /// LOGIC: Matches "for X" patterns at the end of commands.
    /// Preserves original casing of the audience phrase.
    /// </remarks>
    private static string? ExtractAudience(string command)
    {
        var match = AudiencePattern.Match(command);

        if (match.Success)
        {
            var audience = match.Groups[1].Value.Trim();

            // LOGIC: Filter out common false positives where "for" is part of
            // normal sentence structure (e.g., "Summarize for me" or "for this")
            if (audience.Equals("me", StringComparison.OrdinalIgnoreCase) ||
                audience.Equals("this", StringComparison.OrdinalIgnoreCase) ||
                audience.Equals("it", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return audience;
        }

        return null;
    }

    // ── Private Methods: Document Chunking ───────────────────────────────

    /// <summary>
    /// Chunks document content into segments for independent summarization.
    /// </summary>
    /// <param name="content">The full document content.</param>
    /// <returns>A list of content chunks, each within the token limit.</returns>
    /// <remarks>
    /// LOGIC: Chunking algorithm (§5.3):
    /// <list type="number">
    ///   <item><description>Split on section headings (## or ###) as primary boundaries</description></item>
    ///   <item><description>Split on paragraph breaks (\n\n) as secondary boundaries</description></item>
    ///   <item><description>Split on sentence boundaries (. ) as tertiary boundaries</description></item>
    ///   <item><description>Add 100-token overlap between adjacent chunks</description></item>
    /// </list>
    /// Each chunk includes position metadata (Chunk N of M).
    /// </remarks>
    internal List<string> ChunkContent(string content)
    {
        var chunks = new List<string>();
        var overlapChars = ChunkOverlapTokens * 4; // ~4 chars per token

        // LOGIC: Try splitting on section headings first
        var segments = SplitOnHeadings(content);

        // LOGIC: If no headings found, fall back to paragraph splitting
        if (segments.Count <= 1)
        {
            segments = SplitOnParagraphs(content);
        }

        // LOGIC: If paragraphs also produce a single oversized segment,
        // fall back to sentence splitting (tertiary boundary per §5.3).
        if (segments.Count <= 1 && EstimateTokens(segments[0]) > MaxTokensPerChunk)
        {
            segments = SplitOnSentences(segments[0]);
        }

        // LOGIC: Build chunks from segments, respecting max size
        var currentChunk = new StringBuilder();

        foreach (var segment in segments)
        {
            var segmentTokens = EstimateTokens(segment);

            // LOGIC: If adding this segment would exceed the limit, finalize current chunk
            if (currentChunk.Length > 0 && EstimateTokens(currentChunk.ToString()) + segmentTokens > MaxTokensPerChunk)
            {
                chunks.Add(currentChunk.ToString().Trim());

                // LOGIC: Start new chunk with overlap from end of previous chunk
                var previousContent = currentChunk.ToString();
                currentChunk.Clear();

                if (previousContent.Length > overlapChars)
                {
                    var overlap = previousContent[^overlapChars..];
                    currentChunk.Append(overlap);
                }
            }

            currentChunk.Append(segment);
        }

        // LOGIC: Finalize last chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        // LOGIC: Ensure at least one chunk
        if (chunks.Count == 0)
        {
            chunks.Add(content);
        }

        return chunks;
    }

    /// <summary>
    /// Splits content on section headings (## or ###).
    /// </summary>
    /// <param name="content">The content to split.</param>
    /// <returns>List of segments, each starting at a heading boundary.</returns>
    private static List<string> SplitOnHeadings(string content)
    {
        var segments = new List<string>();
        var matches = HeadingPattern.Matches(content);

        if (matches.Count == 0)
        {
            segments.Add(content);
            return segments;
        }

        var lastIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                segments.Add(content[lastIndex..match.Index]);
            }

            lastIndex = match.Index;
        }

        // LOGIC: Add the remaining content after the last heading
        if (lastIndex < content.Length)
        {
            segments.Add(content[lastIndex..]);
        }

        return segments;
    }

    /// <summary>
    /// Splits content on paragraph breaks (\n\n).
    /// </summary>
    /// <param name="content">The content to split.</param>
    /// <returns>List of paragraph segments.</returns>
    private static List<string> SplitOnParagraphs(string content)
    {
        var paragraphs = content.Split(
            new[] { "\n\n", "\r\n\r\n" },
            StringSplitOptions.RemoveEmptyEntries);

        var segments = new List<string>();

        foreach (var paragraph in paragraphs)
        {
            segments.Add(paragraph + "\n\n");
        }

        return segments;
    }

    /// <summary>
    /// Splits content on sentence boundaries (". " followed by uppercase letter).
    /// </summary>
    /// <param name="content">The content to split.</param>
    /// <returns>List of sentence segments.</returns>
    /// <remarks>
    /// LOGIC: Tertiary splitting strategy (§5.3). Used when content has no headings
    /// and no paragraph breaks but exceeds the per-chunk token limit. Splits on
    /// ". " patterns to produce sentence-level segments.
    /// </remarks>
    private static List<string> SplitOnSentences(string content)
    {
        var segments = new List<string>();

        // LOGIC: Split on ". " as a simple sentence boundary heuristic.
        // Each segment retains the period for readability.
        var sentences = content.Split(". ", StringSplitOptions.RemoveEmptyEntries);

        foreach (var sentence in sentences)
        {
            // LOGIC: Re-add the ". " delimiter that was consumed by Split,
            // except for the last segment (which may not end with ". ").
            segments.Add(sentence + ". ");
        }

        // LOGIC: Fallback — if no sentence boundaries found, return the whole content.
        if (segments.Count == 0)
        {
            segments.Add(content);
        }

        return segments;
    }

    // ── Private Methods: Response Parsing ────────────────────────────────

    /// <summary>
    /// Parses individual items from the summary content for list-based modes.
    /// </summary>
    /// <param name="summaryContent">The raw summary content from the LLM.</param>
    /// <param name="mode">The summarization mode.</param>
    /// <returns>
    /// A list of extracted items for BulletPoints and KeyTakeaways modes;
    /// <c>null</c> for prose modes.
    /// </returns>
    /// <remarks>
    /// LOGIC: Item extraction varies by mode:
    /// <list type="bullet">
    ///   <item><description>BulletPoints: Lines starting with "•" or "-" are parsed</description></item>
    ///   <item><description>KeyTakeaways: Lines matching "**Takeaway N:**" are parsed</description></item>
    ///   <item><description>Prose modes: Returns null (no items to extract)</description></item>
    /// </list>
    /// </remarks>
    private static IReadOnlyList<string>? ParseItems(string summaryContent, SummarizationMode mode)
    {
        if (mode != SummarizationMode.BulletPoints && mode != SummarizationMode.KeyTakeaways)
        {
            return null;
        }

        var items = new List<string>();
        var lines = summaryContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (mode == SummarizationMode.BulletPoints)
            {
                // LOGIC: Match bullet markers "•", "-", or "*"
                if (trimmed.StartsWith("•") || trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    var itemText = trimmed.TrimStart('•', '-', '*', ' ');
                    if (!string.IsNullOrWhiteSpace(itemText))
                    {
                        items.Add(itemText);
                    }
                }
            }
            else if (mode == SummarizationMode.KeyTakeaways)
            {
                // LOGIC: Match "**Takeaway N:**" pattern
                if (trimmed.StartsWith("**Takeaway", StringComparison.OrdinalIgnoreCase))
                {
                    // LOGIC: Extract the insight text after the ":**" marker
                    var colonIndex = trimmed.IndexOf(":**", StringComparison.Ordinal);
                    if (colonIndex >= 0)
                    {
                        var itemText = trimmed[(colonIndex + 3)..].Trim().TrimEnd('*');
                        if (!string.IsNullOrWhiteSpace(itemText))
                        {
                            items.Add(itemText);
                        }
                    }
                }
            }
        }

        return items.Count > 0 ? items.AsReadOnly() : null;
    }

    // ── Private Methods: Utilities ───────────────────────────────────────

    /// <summary>
    /// Estimates the token count for a text string.
    /// </summary>
    /// <param name="text">The text to estimate tokens for.</param>
    /// <returns>Estimated token count using the content.Length / 4 approximation.</returns>
    /// <remarks>
    /// LOGIC: The approximation of 4 characters per token is widely used for
    /// English text with GPT-family tokenizers. This avoids the overhead of
    /// running the actual tokenizer for estimation purposes.
    /// </remarks>
    private static int EstimateTokens(string text) => text.Length / 4;

    /// <summary>
    /// Counts words in a text string.
    /// </summary>
    /// <param name="text">The text to count words in.</param>
    /// <returns>Word count based on whitespace splitting.</returns>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Gets the default target word count for a summarization mode.
    /// </summary>
    /// <param name="mode">The summarization mode.</param>
    /// <returns>Default word count target for the mode.</returns>
    /// <remarks>
    /// LOGIC: Mode-specific defaults from spec §5.4:
    /// Abstract=200, TLDR=75, BulletPoints=0 (item-based), KeyTakeaways=0 (item-based),
    /// Executive=150, Custom=200.
    /// </remarks>
    private static int GetDefaultWordCount(SummarizationMode mode) => mode switch
    {
        SummarizationMode.Abstract => 200,
        SummarizationMode.TLDR => 75,
        SummarizationMode.BulletPoints => 0,
        SummarizationMode.KeyTakeaways => 0,
        SummarizationMode.Executive => 150,
        SummarizationMode.Custom => 200,
        _ => 200
    };

    /// <summary>
    /// Extracts the first heading from a chunk of content.
    /// </summary>
    /// <param name="chunk">The chunk to extract a heading from.</param>
    /// <returns>The heading text, or null if no heading found.</returns>
    private static string? ExtractFirstHeading(string chunk)
    {
        var match = HeadingPattern.Match(chunk);

        if (match.Success)
        {
            // LOGIC: Extract the rest of the line after the heading markers
            var lineEnd = chunk.IndexOf('\n', match.Index);
            var heading = lineEnd >= 0
                ? chunk[match.Index..lineEnd].Trim()
                : chunk[match.Index..].Trim();

            return heading.TrimStart('#', ' ');
        }

        return null;
    }

    /// <summary>
    /// Publishes a <see cref="SummarizationFailedEvent"/> without throwing if the publish fails.
    /// </summary>
    /// <param name="mode">The summarization mode that was attempted.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="documentPath">Path to the document, if available.</param>
    /// <remarks>
    /// LOGIC: Used in catch blocks where we don't want event publishing failures to
    /// mask the original exception. Failures are logged at Warning level.
    /// </remarks>
    private async Task PublishFailedEventSafe(
        SummarizationMode mode,
        string errorMessage,
        string? documentPath)
    {
        try
        {
            await _mediator.Publish(
                SummarizationFailedEvent.Create(mode, errorMessage, documentPath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SummarizationFailedEvent");
        }
    }
}
