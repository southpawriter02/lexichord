// -----------------------------------------------------------------------
// <copyright file="MetadataExtractor.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implements the Metadata Extractor Agent — a specialized IAgent for
//   extracting structured metadata from documents (v0.7.6b).
//   Orchestrates prompt rendering, LLM invocation, JSON response parsing,
//   and metadata assembly.
//
//   Invocation flow (§5.1):
//     1. Validate options (MaxKeyTerms, MaxConcepts, MaxTags ranges)
//     2. Publish MetadataExtractionStartedEvent
//     3. Calculate word count and reading time (algorithmic, no LLM)
//     4. Render extraction prompt via IPromptRenderer
//     5. Invoke LLM via IChatCompletionService.CompleteAsync
//     6. Parse JSON response
//     7. Filter key terms by MinimumTermImportance
//     8. Match suggested tags with existing tags
//     9. Calculate complexity score
//    10. Publish MetadataExtractionCompletedEvent
//    11. Return DocumentMetadata
//
//   License gating (§3.2):
//     - Agent access requires WriterPro (enforced by [RequiresLicense])
//     - Feature code: FeatureCodes.MetadataExtraction ("Feature.MetadataExtraction")
//
//   Error handling:
//     - OperationCanceledException from user CancellationToken → "Extraction cancelled"
//     - OperationCanceledException from timeout CTS → "Extraction timed out"
//     - JSON parsing errors → logged and wrapped in failed DocumentMetadata
//     - Generic exceptions → logged and wrapped in failed DocumentMetadata
//
//   Thread safety (§9.1):
//     - All injected services are stateless or thread-safe
//     - No shared mutable state — all variables are per-invocation
//
//   Introduced in: v0.7.6b
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.MetadataExtraction.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.MetadataExtraction;

/// <summary>
/// The Metadata Extractor Agent — specializes in extracting structured metadata from documents.
/// </summary>
/// <remarks>
/// <para>
/// Implements both <see cref="IAgent"/> and <see cref="IMetadataExtractor"/> to function
/// as a registered agent (discoverable via <see cref="IAgentRegistry"/>) while exposing
/// metadata extraction-specific operations consumed by command handlers.
/// </para>
/// <para>
/// The agent extracts comprehensive metadata including:
/// </para>
/// <list type="bullet">
///   <item><description>Key terms with importance scoring and frequency analysis</description></item>
///   <item><description>High-level concepts for categorization</description></item>
///   <item><description>Tag suggestions consistent with existing workspace taxonomy</description></item>
///   <item><description>Reading time calculation based on word count and complexity</description></item>
///   <item><description>Target audience inference from vocabulary and style</description></item>
///   <item><description>Document complexity scoring on a 1-10 scale</description></item>
///   <item><description>Document type classification (Tutorial, Reference, Report, etc.)</description></item>
/// </list>
/// <para><b>Introduced in:</b> v0.7.6b</para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro, FeatureCode = FeatureCodes.MetadataExtraction)]
[AgentDefinition("metadata-extractor", Priority = 103)]
public sealed class MetadataExtractor : IAgent, IMetadataExtractor
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IChatCompletionService _chatService;
    private readonly IPromptRenderer _promptRenderer;
    private readonly IPromptTemplateRepository _templateRepository;
    private readonly IFileService _fileService;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<MetadataExtractor> _logger;

    // ── Constants ────────────────────────────────────────────────────────

    /// <summary>
    /// Template ID for the metadata extractor prompt template.
    /// </summary>
    private const string TemplateId = "metadata-extractor";

    /// <summary>
    /// Template ID for the key-terms-only extraction prompt.
    /// </summary>
    private const string KeyTermsTemplateId = "metadata-extractor-keyterms";

    /// <summary>
    /// Default cost per 1,000 prompt tokens (USD).
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches the SummarizerAgent and EditorAgent default pricing.
    /// A future enhancement can read model-specific pricing from configuration.
    /// </remarks>
    private const decimal DefaultPromptCostPer1K = 0.01m;

    /// <summary>
    /// Default cost per 1,000 completion tokens (USD).
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches the SummarizerAgent and EditorAgent default pricing.
    /// </remarks>
    private const decimal DefaultCompletionCostPer1K = 0.03m;

    /// <summary>
    /// Timeout for the overall extraction operation.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Regex pattern for detecting code blocks in content.
    /// </summary>
    private static readonly Regex CodeBlockPattern = new(
        @"```[\s\S]*?```|`[^`]+`",
        RegexOptions.Compiled);

    /// <summary>
    /// Regex pattern for detecting tables in Markdown content.
    /// </summary>
    private static readonly Regex TablePattern = new(
        @"^\|.+\|$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Regex pattern for detecting image references in Markdown.
    /// </summary>
    private static readonly Regex ImagePattern = new(
        @"!\[.*?\]\(.*?\)",
        RegexOptions.Compiled);

    /// <summary>
    /// Regex pattern for detecting nested headings in Markdown.
    /// </summary>
    private static readonly Regex NestedHeadingPattern = new(
        @"^#{3,6}\s+",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Regex pattern for sentence detection.
    /// </summary>
    private static readonly Regex SentencePattern = new(
        @"[.!?]+\s+",
        RegexOptions.Compiled);

    /// <summary>
    /// Common technical terms for density calculation.
    /// </summary>
    private static readonly HashSet<string> TechnicalIndicators = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "sdk", "async", "await", "interface", "class", "function", "method",
        "parameter", "return", "exception", "database", "query", "schema", "endpoint",
        "http", "json", "xml", "html", "css", "algorithm", "protocol", "framework",
        "dependency", "injection", "repository", "service", "controller", "model",
        "view", "component", "module", "package", "namespace", "variable", "constant",
        "boolean", "integer", "string", "array", "list", "dictionary", "hash", "null",
        "undefined", "instance", "static", "abstract", "virtual", "override", "inherit"
    };

    /// <summary>
    /// JSON serializer options for parsing LLM responses.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Initializes a new instance of <see cref="MetadataExtractor"/>.
    /// </summary>
    /// <param name="chatService">LLM chat completion service for extraction requests.</param>
    /// <param name="promptRenderer">Template renderer for prompt assembly.</param>
    /// <param name="templateRepository">Repository for loading prompt templates.</param>
    /// <param name="fileService">File system service for reading document content.</param>
    /// <param name="licenseContext">License verification context.</param>
    /// <param name="mediator">MediatR mediator for publishing events.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public MetadataExtractor(
        IChatCompletionService chatService,
        IPromptRenderer promptRenderer,
        IPromptTemplateRepository templateRepository,
        IFileService fileService,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<MetadataExtractor> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── IAgent Properties ───────────────────────────────────────────────

    /// <inheritdoc />
    public string AgentId => "metadata-extractor";

    /// <inheritdoc />
    public string Name => "The Metadata Extractor";

    /// <inheritdoc />
    public string Description =>
        "Extracts structured metadata from documents including key terms, concepts, tags, reading time, complexity score, and document classification.";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns the metadata-extractor template. The template requests JSON output
    /// with configurable extraction parameters.
    /// </remarks>
    public IPromptTemplate Template => _templateRepository.GetTemplate(TemplateId)!;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: The Metadata Extractor Agent supports chat, document context, summarization
    /// (for extracting key information), and structure analysis (for document type detection).
    /// </remarks>
    public AgentCapabilities Capabilities =>
        AgentCapabilities.Chat |
        AgentCapabilities.DocumentContext |
        AgentCapabilities.Summarization |
        AgentCapabilities.StructureAnalysis;

    // ── IAgent.InvokeAsync ──────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Delegates to <see cref="ExtractFromContentAsync"/> with default options.
    /// The content comes from Selection or UserMessage.
    /// </remarks>
    public async Task<AgentResponse> InvokeAsync(
        AgentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        _logger.LogDebug(
            "MetadataExtractor.InvokeAsync: extracting metadata from request content");

        // LOGIC: Use selection as content if available, otherwise use user message
        var content = request.Selection ?? request.UserMessage;

        var metadata = await ExtractFromContentAsync(content, options: null, ct);

        if (!metadata.Success)
        {
            return new AgentResponse(
                $"Metadata extraction failed: {metadata.ErrorMessage}",
                Citations: null,
                metadata.Usage);
        }

        // LOGIC: Format metadata as human-readable response
        var response = FormatMetadataResponse(metadata);

        return new AgentResponse(
            response,
            Citations: null,
            metadata.Usage);
    }

    // ── IMetadataExtractor Implementation ─────────────────────────────────

    /// <inheritdoc />
    public async Task<DocumentMetadata> ExtractAsync(
        string documentPath,
        MetadataExtractionOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);

        _logger.LogDebug(
            "ExtractAsync: loading document from path {DocumentPath}",
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

            return DocumentMetadata.Failed(
                $"Failed to load document: {loadResult.Error?.Message ?? "Unknown error"}");
        }

        _logger.LogDebug(
            "Document loaded: {CharCount} characters from {DocumentPath}",
            loadResult.Content.Length,
            documentPath);

        return await ExtractFromContentAsync(loadResult.Content, options, ct);
    }

    /// <inheritdoc />
    public async Task<DocumentMetadata> ExtractFromContentAsync(
        string content,
        MetadataExtractionOptions? options = null,
        CancellationToken ct = default)
    {
        // LOGIC: Validate inputs before any LLM invocation
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));
        }

        options ??= MetadataExtractionOptions.Default;
        options.Validate();

        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug(
            "Metadata extraction started: MaxKeyTerms={MaxKeyTerms}, MaxConcepts={MaxConcepts}, MaxTags={MaxTags}, ContentLength={Length}",
            options.MaxKeyTerms,
            options.MaxConcepts,
            options.MaxTags,
            content.Length);

        // LOGIC: Publish started event for observability and UI state
        await _mediator.Publish(
            MetadataExtractionStartedEvent.Create(content.Length, documentPath: null),
            ct);

        try
        {
            // LOGIC: Calculate document metrics (no LLM required)
            var wordCount = CountWords(content);
            var characterCount = content.Length;
            var readingTimeMinutes = CalculateReadingTime(content, options.WordsPerMinute);
            var complexityScore = options.CalculateComplexity
                ? CalculateComplexityScore(content)
                : 5; // Neutral score when disabled

            _logger.LogDebug(
                "Document metrics: WordCount={WordCount}, ReadingTime={ReadingTime}min, Complexity={Complexity}",
                wordCount,
                readingTimeMinutes,
                complexityScore);

            // LOGIC: Get the prompt template
            var template = _templateRepository.GetTemplate(TemplateId);

            if (template is null)
            {
                _logger.LogError("Prompt template not found: {TemplateId}", TemplateId);

                stopwatch.Stop();
                var failedResult = DocumentMetadata.Failed(
                    $"Prompt template '{TemplateId}' not found.",
                    wordCount);

                await PublishFailedEventSafe(
                    failedResult.ErrorMessage!,
                    documentPath: null);

                return failedResult;
            }

            // LOGIC: Build template variables
            var variables = BuildTemplateVariables(content, options);

            // LOGIC: Render the prompt messages
            var messages = _promptRenderer.RenderMessages(template, variables);

            _logger.LogDebug(
                "Prompt rendered: {MessageCount} messages",
                messages.Length);

            // LOGIC: Create timeout cancellation token source
            using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            // LOGIC: Configure LLM options with lower temperature for consistent JSON
            var chatOptions = new ChatOptions
            {
                MaxTokens = options.MaxResponseTokens,
                Temperature = 0.3 // Lower temperature for more consistent JSON output
            };

            // LOGIC: Invoke the LLM
            var chatRequest = new ChatRequest(messages.ToImmutableArray(), chatOptions);
            var chatResponse = await _chatService.CompleteAsync(chatRequest, linkedCts.Token);

            _logger.LogDebug(
                "LLM response received: {ContentLength} chars, PromptTokens={PromptTokens}, CompletionTokens={CompletionTokens}",
                chatResponse.Content?.Length ?? 0,
                chatResponse.PromptTokens,
                chatResponse.CompletionTokens);

            // LOGIC: Parse the JSON response
            var extractedData = ParseLlmResponse(chatResponse.Content ?? string.Empty);

            if (extractedData is null)
            {
                _logger.LogWarning("Failed to parse LLM response as JSON, using fallback extraction");
                extractedData = CreateFallbackExtraction(content, options);
            }

            // LOGIC: Filter key terms by minimum importance
            var filteredKeyTerms = FilterKeyTerms(extractedData.KeyTerms, options.MinimumTermImportance);

            // LOGIC: Match suggested tags with existing tags
            var matchedTags = MatchTags(extractedData.SuggestedTags, options.ExistingTags);

            // LOGIC: Calculate usage metrics
            var usage = UsageMetrics.Calculate(
                chatResponse.PromptTokens,
                chatResponse.CompletionTokens,
                DefaultPromptCostPer1K,
                DefaultCompletionCostPer1K);

            stopwatch.Stop();

            // LOGIC: Determine document type
            var documentType = options.DetectDocumentType
                ? ParseDocumentType(extractedData.DocumentType)
                : DocumentType.Unknown;

            // LOGIC: Build the result
            var result = new DocumentMetadata
            {
                SuggestedTitle = extractedData.SuggestedTitle,
                OneLiner = extractedData.OneLiner ?? string.Empty,
                KeyTerms = filteredKeyTerms,
                Concepts = extractedData.Concepts ?? Array.Empty<string>(),
                SuggestedTags = matchedTags,
                PrimaryCategory = extractedData.PrimaryCategory,
                TargetAudience = options.InferAudience ? extractedData.TargetAudience : null,
                EstimatedReadingMinutes = readingTimeMinutes,
                ComplexityScore = complexityScore,
                DocumentType = documentType,
                NamedEntities = options.ExtractNamedEntities ? extractedData.NamedEntities : null,
                Language = extractedData.Language ?? "en",
                WordCount = wordCount,
                CharacterCount = characterCount,
                ExtractedAt = DateTimeOffset.UtcNow,
                Usage = usage,
                Model = null, // Model info not available from IChatCompletionService
                Success = true,
                ErrorMessage = null
            };

            // LOGIC: Publish completed event
            await _mediator.Publish(
                MetadataExtractionCompletedEvent.Create(
                    documentType,
                    result.KeyTerms.Count,
                    result.Concepts.Count,
                    result.SuggestedTags.Count,
                    complexityScore,
                    readingTimeMinutes,
                    wordCount,
                    stopwatch.Elapsed),
                ct);

            _logger.LogInformation(
                "Metadata extraction completed: KeyTerms={KeyTermCount}, Concepts={ConceptCount}, Tags={TagCount}, Complexity={Complexity}, Duration={Duration}ms",
                result.KeyTerms.Count,
                result.Concepts.Count,
                result.SuggestedTags.Count,
                complexityScore,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // LOGIC: User-initiated cancellation — distinguish from timeout.
            stopwatch.Stop();
            _logger.LogWarning("Metadata extraction cancelled by user");

            await PublishFailedEventSafe(
                "Metadata extraction cancelled by user.",
                documentPath: null);

            return DocumentMetadata.Failed(
                "Metadata extraction cancelled by user.",
                CountWords(content));
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Timeout — the timeoutCts fired before the user ct.
            stopwatch.Stop();
            _logger.LogWarning(
                "Metadata extraction timed out after {Timeout}",
                DefaultTimeout);

            await PublishFailedEventSafe(
                $"Metadata extraction timed out after {DefaultTimeout.TotalSeconds:F0} seconds.",
                documentPath: null);

            return DocumentMetadata.Failed(
                $"Metadata extraction timed out after {DefaultTimeout.TotalSeconds:F0} seconds.",
                CountWords(content));
        }
        catch (Exception ex)
        {
            // LOGIC: Generic failure — log and return failed result.
            stopwatch.Stop();
            _logger.LogError(ex, "Metadata extraction failed: {ErrorMessage}", ex.Message);

            await PublishFailedEventSafe(
                ex.Message,
                documentPath: null);

            return DocumentMetadata.Failed(
                ex.Message,
                CountWords(content));
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> SuggestTagsAsync(
        string content,
        IReadOnlyList<string> existingWorkspaceTags,
        int maxSuggestions = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));
        }

        ArgumentNullException.ThrowIfNull(existingWorkspaceTags);

        _logger.LogDebug(
            "SuggestTagsAsync: requesting {MaxSuggestions} tag suggestions, {ExistingCount} existing tags",
            maxSuggestions,
            existingWorkspaceTags.Count);

        // LOGIC: Use full extraction with limited options for tag suggestion
        var options = new MetadataExtractionOptions
        {
            MaxKeyTerms = 5,
            MaxConcepts = 3,
            MaxTags = maxSuggestions,
            ExistingTags = existingWorkspaceTags,
            ExtractNamedEntities = false,
            InferAudience = false,
            CalculateComplexity = false,
            DetectDocumentType = false,
            IncludeDefinitions = false,
            MaxResponseTokens = 1024
        };

        var metadata = await ExtractFromContentAsync(content, options, ct);

        return metadata.Success ? metadata.SuggestedTags : Array.Empty<string>();
    }

    /// <inheritdoc />
    public int CalculateReadingTime(string content, int wordsPerMinute = 200)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        // LOGIC: Validate words per minute range
        wordsPerMinute = Math.Clamp(wordsPerMinute, 100, 400);

        // LOGIC: Count words
        var wordCount = CountWords(content);

        // LOGIC: Base time calculation
        var baseMinutes = (double)wordCount / wordsPerMinute;

        // LOGIC: Add time for code blocks (30 sec each)
        var codeBlockCount = CodeBlockPattern.Matches(content).Count;
        baseMinutes += codeBlockCount * 0.5;

        // LOGIC: Add time for tables (30 sec each, count table rows/3 as approximation)
        var tableRowCount = TablePattern.Matches(content).Count;
        var tableCount = Math.Max(1, tableRowCount / 3);
        if (tableRowCount > 0)
        {
            baseMinutes += tableCount * 0.5;
        }

        // LOGIC: Add time for images (12 sec each)
        var imageCount = ImagePattern.Matches(content).Count;
        baseMinutes += imageCount * 0.2;

        // LOGIC: Apply complexity multipliers
        var complexityMultiplier = 1.0;

        // Complex sentences: average > 25 words per sentence
        var avgSentenceLength = CalculateAverageSentenceLength(content);
        if (avgSentenceLength > 25)
        {
            complexityMultiplier += 0.1; // +10% for complex sentences
        }

        // Technical vocabulary
        var technicalDensity = CalculateTechnicalDensity(content);
        if (technicalDensity > 0.1) // More than 10% technical terms
        {
            complexityMultiplier += 0.2; // +20% for technical content
        }

        var totalMinutes = baseMinutes * complexityMultiplier;

        // LOGIC: Minimum 1 minute for non-empty content
        return Math.Max(1, (int)Math.Ceiling(totalMinutes));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KeyTerm>> ExtractKeyTermsAsync(
        string content,
        int maxTerms = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));
        }

        _logger.LogDebug(
            "ExtractKeyTermsAsync: extracting up to {MaxTerms} key terms",
            maxTerms);

        // LOGIC: Use lightweight extraction focused on key terms only
        var options = new MetadataExtractionOptions
        {
            MaxKeyTerms = maxTerms,
            MaxConcepts = 0,
            MaxTags = 0,
            ExtractNamedEntities = false,
            InferAudience = false,
            CalculateComplexity = false,
            DetectDocumentType = false,
            IncludeDefinitions = true,
            MaxResponseTokens = 1024
        };

        var metadata = await ExtractFromContentAsync(content, options, ct);

        return metadata.Success ? metadata.KeyTerms : Array.Empty<KeyTerm>();
    }

    /// <inheritdoc />
    public MetadataExtractionOptions GetDefaultOptions() => MetadataExtractionOptions.Default;

    // ── Private Methods: Template Building ──────────────────────────────

    /// <summary>
    /// Builds the template variables dictionary for prompt rendering.
    /// </summary>
    /// <param name="content">The document content.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>Dictionary of template variables.</returns>
    private static Dictionary<string, object> BuildTemplateVariables(
        string content,
        MetadataExtractionOptions options)
    {
        var variables = new Dictionary<string, object>
        {
            ["document_content"] = content,
            ["max_key_terms"] = options.MaxKeyTerms,
            ["max_concepts"] = options.MaxConcepts,
            ["max_tags"] = options.MaxTags,
            ["extract_named_entities"] = options.ExtractNamedEntities,
            ["infer_audience"] = options.InferAudience
        };

        if (options.ExistingTags is { Count: > 0 })
        {
            variables["existing_tags"] = string.Join(", ", options.ExistingTags);
        }

        if (!string.IsNullOrWhiteSpace(options.DomainContext))
        {
            variables["domain_context"] = options.DomainContext;
        }

        return variables;
    }

    // ── Private Methods: Response Parsing ───────────────────────────────

    /// <summary>
    /// Parses the LLM response JSON into extraction data.
    /// </summary>
    /// <param name="responseContent">The raw LLM response content.</param>
    /// <returns>Parsed extraction data, or null if parsing fails.</returns>
    private ExtractionResponse? ParseLlmResponse(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

        try
        {
            // LOGIC: Clean the response — remove markdown code blocks if present
            var cleaned = responseContent.Trim();

            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[7..];
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned[3..];
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned[..^3];
            }

            cleaned = cleaned.Trim();

            var response = JsonSerializer.Deserialize<ExtractionResponse>(cleaned, JsonOptions);

            if (response is null)
            {
                _logger.LogWarning("JSON deserialization returned null");
                return null;
            }

            _logger.LogDebug(
                "Successfully parsed LLM response: KeyTerms={KeyTermCount}, Concepts={ConceptCount}, Tags={TagCount}",
                response.KeyTerms?.Count ?? 0,
                response.Concepts?.Count ?? 0,
                response.SuggestedTags?.Count ?? 0);

            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON: {Response}", responseContent[..Math.Min(200, responseContent.Length)]);
            return null;
        }
    }

    /// <summary>
    /// Creates fallback extraction data when JSON parsing fails.
    /// </summary>
    /// <param name="content">The original document content.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>Fallback extraction response with basic data.</returns>
    private ExtractionResponse CreateFallbackExtraction(string content, MetadataExtractionOptions options)
    {
        _logger.LogDebug("Creating fallback extraction response");

        // LOGIC: Extract basic information algorithmically
        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var word in words)
        {
            var cleaned = word.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']');
            if (cleaned.Length > 3 && !IsCommonWord(cleaned))
            {
                wordFrequency[cleaned] = wordFrequency.GetValueOrDefault(cleaned, 0) + 1;
            }
        }

        var topTerms = wordFrequency
            .OrderByDescending(kv => kv.Value)
            .Take(options.MaxKeyTerms)
            .Select((kv, index) => new KeyTermResponse
            {
                Term = kv.Key,
                Importance = 1.0 - (index * 0.1),
                Frequency = kv.Value,
                IsTechnical = TechnicalIndicators.Contains(kv.Key)
            })
            .ToList();

        return new ExtractionResponse
        {
            OneLiner = "Document metadata extracted with fallback method.",
            KeyTerms = topTerms,
            Concepts = Array.Empty<string>(),
            SuggestedTags = Array.Empty<string>(),
            DocumentType = "Unknown",
            Language = "en"
        };
    }

    /// <summary>
    /// Filters key terms by minimum importance threshold.
    /// </summary>
    /// <param name="terms">The extracted key terms.</param>
    /// <param name="minimumImportance">Minimum importance threshold.</param>
    /// <returns>Filtered and mapped key terms.</returns>
    private static IReadOnlyList<KeyTerm> FilterKeyTerms(
        IReadOnlyList<KeyTermResponse>? terms,
        double minimumImportance)
    {
        if (terms is null || terms.Count == 0)
        {
            return Array.Empty<KeyTerm>();
        }

        return terms
            .Where(t => t.Importance >= minimumImportance)
            .OrderByDescending(t => t.Importance)
            .Select(t => new KeyTerm(
                t.Term ?? string.Empty,
                t.Importance,
                t.Frequency,
                t.IsTechnical,
                t.Definition,
                t.Category,
                t.RelatedTerms))
            .ToList();
    }

    /// <summary>
    /// Matches suggested tags with existing workspace tags.
    /// </summary>
    /// <param name="suggestedTags">Tags suggested by the LLM.</param>
    /// <param name="existingTags">Existing workspace tags.</param>
    /// <returns>Final tag list preferring existing tags.</returns>
    private IReadOnlyList<string> MatchTags(
        IReadOnlyList<string>? suggestedTags,
        IReadOnlyList<string>? existingTags)
    {
        if (suggestedTags is null || suggestedTags.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (existingTags is null || existingTags.Count == 0)
        {
            // LOGIC: No existing tags — return suggested tags formatted correctly
            return suggestedTags
                .Select(FormatTag)
                .Distinct()
                .ToList();
        }

        var result = new List<string>();
        var existingLookup = existingTags.ToDictionary(t => t.ToLowerInvariant(), t => t);

        foreach (var suggested in suggestedTags)
        {
            var normalizedSuggested = suggested.ToLowerInvariant();

            // LOGIC: Priority 1 - Exact match
            if (existingLookup.TryGetValue(normalizedSuggested, out var exactMatch))
            {
                result.Add(exactMatch);
                continue;
            }

            // LOGIC: Priority 2 - Fuzzy match (Levenshtein similarity > 0.8)
            var fuzzyMatch = FindFuzzyMatch(normalizedSuggested, existingTags, 0.8);
            if (fuzzyMatch is not null)
            {
                result.Add(fuzzyMatch);
                continue;
            }

            // LOGIC: Priority 3 - Novel tag
            result.Add(FormatTag(suggested));
        }

        return result.Distinct().ToList();
    }

    /// <summary>
    /// Finds a fuzzy match in existing tags using Levenshtein distance.
    /// </summary>
    /// <param name="target">The target string to match.</param>
    /// <param name="candidates">Candidate strings to search.</param>
    /// <param name="minSimilarity">Minimum similarity threshold (0.0-1.0).</param>
    /// <returns>Best matching candidate, or null if none meet threshold.</returns>
    private static string? FindFuzzyMatch(string target, IReadOnlyList<string> candidates, double minSimilarity)
    {
        string? bestMatch = null;
        var bestSimilarity = 0.0;

        foreach (var candidate in candidates)
        {
            var similarity = CalculateLevenshteinSimilarity(target, candidate.ToLowerInvariant());
            if (similarity > minSimilarity && similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Calculates Levenshtein similarity between two strings.
    /// </summary>
    /// <param name="s1">First string.</param>
    /// <param name="s2">Second string.</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    private static double CalculateLevenshteinSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
        {
            return 1.0;
        }

        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
        {
            return 0.0;
        }

        var distance = CalculateLevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    /// <param name="s1">First string.</param>
    /// <param name="s2">Second string.</param>
    /// <returns>Edit distance.</returns>
    private static int CalculateLevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= m; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// Formats a tag to lowercase with hyphens.
    /// </summary>
    /// <param name="tag">The tag to format.</param>
    /// <returns>Formatted tag.</returns>
    private static string FormatTag(string tag)
    {
        return tag
            .ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('_', '-');
    }

    /// <summary>
    /// Parses the document type string to enum.
    /// </summary>
    /// <param name="documentTypeString">The document type string from LLM.</param>
    /// <returns>Parsed DocumentType enum value.</returns>
    private static DocumentType ParseDocumentType(string? documentTypeString)
    {
        if (string.IsNullOrWhiteSpace(documentTypeString))
        {
            return DocumentType.Unknown;
        }

        return Enum.TryParse<DocumentType>(documentTypeString, ignoreCase: true, out var result)
            ? result
            : DocumentType.Unknown;
    }

    // ── Private Methods: Text Analysis ──────────────────────────────────

    /// <summary>
    /// Counts words in the content.
    /// </summary>
    /// <param name="content">The text content.</param>
    /// <returns>Word count.</returns>
    private static int CountWords(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        return content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Calculates the average sentence length in words.
    /// </summary>
    /// <param name="content">The text content.</param>
    /// <returns>Average words per sentence.</returns>
    private static double CalculateAverageSentenceLength(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        var sentences = SentencePattern.Split(content)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (sentences.Length == 0)
        {
            return CountWords(content);
        }

        var totalWords = sentences.Sum(s => CountWords(s));
        return (double)totalWords / sentences.Length;
    }

    /// <summary>
    /// Calculates the technical term density in the content.
    /// </summary>
    /// <param name="content">The text content.</param>
    /// <returns>Ratio of technical words to total words (0.0-1.0).</returns>
    private static double CalculateTechnicalDensity(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return 0;
        }

        var technicalCount = words.Count(w =>
        {
            var cleaned = w.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']');
            return TechnicalIndicators.Contains(cleaned);
        });

        return (double)technicalCount / words.Length;
    }

    /// <summary>
    /// Calculates the complexity score for the document.
    /// </summary>
    /// <param name="content">The document content.</param>
    /// <returns>Complexity score from 1 to 10.</returns>
    private int CalculateComplexityScore(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 5; // Neutral
        }

        // LOGIC: Start with base score of 5 (spec §5.3)
        var score = 5.0;

        // Technical density adjustments
        var technicalDensity = CalculateTechnicalDensity(content);
        if (technicalDensity > 0.15)
        {
            score += 1; // +1 if technicalDensity > 15%
        }
        else if (technicalDensity < 0.05)
        {
            score -= 1; // -1 if technicalDensity < 5%
        }

        // Sentence length adjustments
        var avgSentenceLength = CalculateAverageSentenceLength(content);
        if (avgSentenceLength > 25)
        {
            score += 1; // +1 if avgWordsPerSentence > 25
        }
        else if (avgSentenceLength < 12)
        {
            score -= 1; // -1 if avgWordsPerSentence < 12
        }

        // Structure adjustments
        var hasNestedHeadings = NestedHeadingPattern.IsMatch(content);
        var hasTables = TablePattern.IsMatch(content);

        if (hasNestedHeadings && hasTables)
        {
            score += 0.5; // +0.5 if hasNestedHeadings && hasTables
        }

        // Simple structure (no headings, short content)
        var hasAnyHeadings = Regex.IsMatch(content, @"^#{1,6}\s+", RegexOptions.Multiline);
        var wordCount = CountWords(content);
        if (!hasAnyHeadings && wordCount < 500)
        {
            score -= 0.5; // -0.5 if simpleStructure
        }

        // Code content increases complexity
        var codeBlockCount = CodeBlockPattern.Matches(content).Count;
        if (codeBlockCount > 5)
        {
            score += 0.5;
        }

        // LOGIC: Clamp to 1-10 range
        return Math.Clamp((int)Math.Round(score), 1, 10);
    }

    /// <summary>
    /// Checks if a word is a common/stop word.
    /// </summary>
    /// <param name="word">The word to check.</param>
    /// <returns>True if the word is common.</returns>
    private static bool IsCommonWord(string word)
    {
        var common = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "up", "about", "into", "through", "during",
            "before", "after", "above", "below", "between", "under", "again",
            "further", "then", "once", "here", "there", "when", "where", "why",
            "how", "all", "each", "few", "more", "most", "other", "some", "such",
            "no", "nor", "not", "only", "own", "same", "so", "than", "too", "very",
            "can", "will", "just", "should", "now", "also", "this", "that", "these",
            "those", "which", "who", "whom", "what", "been", "being", "have", "has",
            "had", "having", "does", "did", "doing", "would", "could", "might",
            "must", "shall", "may", "are", "was", "were", "is", "be", "it", "its",
            "they", "them", "their", "we", "us", "our", "you", "your", "he", "him",
            "his", "she", "her", "i", "me", "my", "as", "if"
        };

        return common.Contains(word);
    }

    /// <summary>
    /// Formats metadata as a human-readable response.
    /// </summary>
    /// <param name="metadata">The extracted metadata.</param>
    /// <returns>Formatted response string.</returns>
    private static string FormatMetadataResponse(DocumentMetadata metadata)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("## Extracted Metadata");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(metadata.SuggestedTitle))
        {
            sb.AppendLine($"**Suggested Title:** {metadata.SuggestedTitle}");
        }

        sb.AppendLine($"**Summary:** {metadata.OneLiner}");
        sb.AppendLine($"**Document Type:** {metadata.DocumentType}");
        sb.AppendLine($"**Complexity:** {metadata.ComplexityScore}/10");
        sb.AppendLine($"**Reading Time:** ~{metadata.EstimatedReadingMinutes} min ({metadata.WordCount:N0} words)");
        sb.AppendLine();

        if (metadata.KeyTerms.Count > 0)
        {
            sb.AppendLine("### Key Terms");
            foreach (var term in metadata.KeyTerms.Take(10))
            {
                var technical = term.IsTechnical ? " (technical)" : "";
                sb.AppendLine($"- **{term.Term}** — importance: {term.Importance:F2}, frequency: {term.Frequency}{technical}");
            }
            sb.AppendLine();
        }

        if (metadata.Concepts.Count > 0)
        {
            sb.AppendLine("### Concepts");
            sb.AppendLine(string.Join(", ", metadata.Concepts));
            sb.AppendLine();
        }

        if (metadata.SuggestedTags.Count > 0)
        {
            sb.AppendLine("### Suggested Tags");
            sb.AppendLine(string.Join(", ", metadata.SuggestedTags.Select(t => $"`{t}`")));
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(metadata.TargetAudience))
        {
            sb.AppendLine($"**Target Audience:** {metadata.TargetAudience}");
        }

        if (metadata.NamedEntities is { Count: > 0 })
        {
            sb.AppendLine($"**Named Entities:** {string.Join(", ", metadata.NamedEntities)}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Publishes a <see cref="MetadataExtractionFailedEvent"/> without throwing if the publish fails.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="documentPath">Path to the document, if available.</param>
    /// <remarks>
    /// LOGIC: Used in catch blocks where we don't want event publishing failures to
    /// mask the original exception. Failures are logged at Warning level.
    /// </remarks>
    private async Task PublishFailedEventSafe(
        string errorMessage,
        string? documentPath)
    {
        try
        {
            await _mediator.Publish(
                MetadataExtractionFailedEvent.Create(errorMessage, documentPath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish MetadataExtractionFailedEvent");
        }
    }

    // ── Private Types: Response Models ──────────────────────────────────

    /// <summary>
    /// Internal model for deserializing LLM extraction response.
    /// </summary>
    private sealed class ExtractionResponse
    {
        public string? SuggestedTitle { get; set; }
        public string? OneLiner { get; set; }
        public IReadOnlyList<KeyTermResponse>? KeyTerms { get; set; }
        public IReadOnlyList<string>? Concepts { get; set; }
        public IReadOnlyList<string>? SuggestedTags { get; set; }
        public string? PrimaryCategory { get; set; }
        public string? TargetAudience { get; set; }
        public string? DocumentType { get; set; }
        public IReadOnlyList<string>? NamedEntities { get; set; }
        public string? Language { get; set; }
    }

    /// <summary>
    /// Internal model for deserializing key term from LLM response.
    /// </summary>
    private sealed class KeyTermResponse
    {
        public string? Term { get; set; }
        public double Importance { get; set; }
        public int Frequency { get; set; }
        public bool IsTechnical { get; set; }
        public string? Definition { get; set; }
        public string? Category { get; set; }
        public IReadOnlyList<string>? RelatedTerms { get; set; }
    }
}
