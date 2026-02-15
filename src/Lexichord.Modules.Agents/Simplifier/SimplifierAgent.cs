// -----------------------------------------------------------------------
// <copyright file="SimplifierAgent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implements the Simplifier Agent — a specialized IAgent for text
//   simplification to target Flesch-Kincaid grade levels (v0.7.4b).
//   Orchestrates context assembly, prompt rendering, LLM invocation, and
//   response parsing for the Simplification Pipeline.
//
//   Invocation flow (§5.1):
//     1. Validate request (empty text, max length, target)
//     2. Analyze original text readability via IReadabilityService
//     3. GatherContext — delegates to IContextOrchestrator with 4000-token budget
//     4. Build prompt variables (target, metrics, strategy)
//     5. RenderMessages — renders Mustache template to ChatMessage array
//     6. GetOptionsForStrategy — adjusts temperature per strategy
//     7. Invoke LLM — batch (CompleteAsync) with timeout CTS
//     8. Parse response — extracts simplified text, changes, glossary
//     9. Analyze simplified text readability
//    10. Build SimplificationResult — calculates metrics and usage
//
//   License gating (§3.2):
//     - Agent access requires WriterPro (enforced by [RequiresLicense])
//     - Feature code: FeatureCodes.SimplifierAgent ("Feature.SimplifierAgent")
//
//   Error handling:
//     - OperationCanceledException from user CancellationToken → "Simplification cancelled"
//     - OperationCanceledException from timeout CTS → "Simplification timed out"
//     - Generic exceptions → logged and wrapped in failed SimplificationResult
//
//   Thread safety (§9.1):
//     - All injected services are stateless or thread-safe
//     - No shared mutable state — all variables are per-invocation
//
//   Introduced in: v0.7.4b
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// The Simplifier Agent — specializes in text simplification to target readability levels.
/// </summary>
/// <remarks>
/// <para>
/// Implements both <see cref="IAgent"/> and <see cref="ISimplificationPipeline"/> to function
/// as a registered agent (discoverable via <see cref="IAgentRegistry"/>) while exposing
/// simplification-specific operations consumed by command handlers.
/// </para>
/// <para>
/// The agent supports three simplification strategies, each with distinct temperature settings:
/// </para>
/// <list type="bullet">
///   <item><description>Conservative (0.3): Minimal changes with high fidelity</description></item>
///   <item><description>Balanced (0.4): Moderate changes (default)</description></item>
///   <item><description>Aggressive (0.5): Maximum simplification</description></item>
/// </list>
/// <para>
/// Context is gathered via <see cref="IContextOrchestrator"/> to include surrounding text,
/// style rules, and terminology from the active document. The orchestrator enforces a
/// 4000-token budget with "style" and "terminology" as required strategies.
/// </para>
/// <para><b>Introduced in:</b> v0.7.4b</para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro)]
[AgentDefinition("simplifier", Priority = 101)]
public sealed class SimplifierAgent : IAgent, ISimplificationPipeline
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IChatCompletionService _chatService;
    private readonly IPromptRenderer _promptRenderer;
    private readonly IPromptTemplateRepository _templateRepository;
    private readonly IContextOrchestrator _contextOrchestrator;
    private readonly IReadabilityTargetService _targetService;
    private readonly IReadabilityService _readabilityService;
    private readonly ISimplificationResponseParser _responseParser;
    private readonly ILogger<SimplifierAgent> _logger;

    // ── Constants ────────────────────────────────────────────────────────

    /// <summary>
    /// Template ID for the simplifier prompt template.
    /// </summary>
    private const string TemplateId = "specialist-simplifier";

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
    /// context window. The remaining window is used for the system prompt,
    /// original text, and response generation.
    /// </remarks>
    private const int ContextTokenBudget = 4000;

    /// <summary>
    /// Maximum tokens to generate in the response.
    /// </summary>
    /// <remarks>
    /// LOGIC: 4096 tokens is sufficient for most simplification operations,
    /// including structured output with changes and optional glossary.
    /// </remarks>
    private const int DefaultMaxTokens = 4096;

    /// <summary>
    /// Warning threshold for long text that may impact performance.
    /// </summary>
    private const int LongTextWarningThreshold = 20_000;

    /// <summary>
    /// Initializes a new instance of <see cref="SimplifierAgent"/>.
    /// </summary>
    /// <param name="chatService">LLM chat completion service for batch and streaming requests.</param>
    /// <param name="promptRenderer">Template renderer for prompt assembly.</param>
    /// <param name="templateRepository">Repository for loading prompt templates.</param>
    /// <param name="contextOrchestrator">Context assembly from document, style, and terminology sources.</param>
    /// <param name="targetService">Readability target resolution service.</param>
    /// <param name="readabilityService">Text readability analysis service.</param>
    /// <param name="responseParser">Parser for structured LLM responses.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public SimplifierAgent(
        IChatCompletionService chatService,
        IPromptRenderer promptRenderer,
        IPromptTemplateRepository templateRepository,
        IContextOrchestrator contextOrchestrator,
        IReadabilityTargetService targetService,
        IReadabilityService readabilityService,
        ISimplificationResponseParser responseParser,
        ILogger<SimplifierAgent> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _contextOrchestrator = contextOrchestrator ?? throw new ArgumentNullException(nameof(contextOrchestrator));
        _targetService = targetService ?? throw new ArgumentNullException(nameof(targetService));
        _readabilityService = readabilityService ?? throw new ArgumentNullException(nameof(readabilityService));
        _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── IAgent Properties ───────────────────────────────────────────────

    /// <inheritdoc />
    public string AgentId => "simplifier";

    /// <inheritdoc />
    public string Name => "The Simplifier";

    /// <inheritdoc />
    public string Description =>
        "Simplifies text to target readability levels while maintaining meaning.";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns the simplifier template. The template is used for IAgent.InvokeAsync
    /// and the direct SimplifyAsync pathways.
    /// </remarks>
    public IPromptTemplate Template => _templateRepository.GetTemplate(TemplateId)!;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: The Simplifier Agent supports chat, document context, and streaming.
    /// RAGContext and StyleEnforcement are included for context gathering.
    /// </remarks>
    public AgentCapabilities Capabilities =>
        AgentCapabilities.Chat |
        AgentCapabilities.DocumentContext |
        AgentCapabilities.StyleEnforcement |
        AgentCapabilities.Streaming;

    // ── IAgent.InvokeAsync ──────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Delegates to <see cref="SimplifyAsync"/> with a Balanced strategy.
    /// The selection from the <see cref="AgentRequest"/> becomes the text to simplify.
    /// If no selection is provided, the user message is used as the text to simplify.
    /// </remarks>
    public async Task<AgentResponse> InvokeAsync(
        AgentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        _logger.LogDebug(
            "SimplifierAgent.InvokeAsync: delegating to SimplifyAsync with Balanced strategy");

        // LOGIC: Get default target (Grade 8, General Public)
        var target = await _targetService.GetTargetAsync(presetId: "general-public", cancellationToken: ct);

        // LOGIC: Map AgentRequest to SimplificationRequest
        var simplificationRequest = new SimplificationRequest
        {
            OriginalText = request.Selection ?? request.UserMessage,
            Target = target,
            DocumentPath = request.DocumentPath,
            Strategy = SimplificationStrategy.Balanced,
            GenerateGlossary = false,
            PreserveFormatting = true,
            AdditionalInstructions = request.HasSelection ? request.UserMessage : null
        };

        var result = await SimplifyAsync(simplificationRequest, ct);

        return new AgentResponse(
            result.Success ? result.SimplifiedText : $"Simplification failed: {result.ErrorMessage}",
            Citations: null,
            result.TokenUsage);
    }

    // ── ISimplificationPipeline Implementation ─────────────────────────

    /// <inheritdoc />
    public async Task<SimplificationResult> SimplifyAsync(
        SimplificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting {Strategy} simplification for {CharCount} characters, target grade {TargetGrade}",
                request.Strategy,
                request.OriginalText.Length,
                request.Target.TargetGradeLevel);

            // 1. Analyze original text readability
            var originalMetrics = await _readabilityService.AnalyzeAsync(request.OriginalText, cancellationToken);

            _logger.LogDebug(
                "Original text metrics: Grade {Grade:F1}, Avg sentence {AvgSentence:F1} words, Complex ratio {ComplexRatio:P1}",
                originalMetrics.FleschKincaidGradeLevel,
                originalMetrics.AverageWordsPerSentence,
                originalMetrics.ComplexWordRatio);

            // 2. Gather context via IContextOrchestrator
            var context = await GatherContextAsync(request, cancellationToken);

            _logger.LogDebug(
                "Context gathered: {FragmentCount} fragments, {TotalTokens} tokens",
                context.Fragments.Count,
                context.TotalTokens);

            // 3. Get template
            var template = _templateRepository.GetTemplate(TemplateId);

            if (template is null)
            {
                _logger.LogError("Prompt template not found: {TemplateId}", TemplateId);

                stopwatch.Stop();
                return SimplificationResult.Failed(
                    request.OriginalText,
                    request.Strategy,
                    request.Target,
                    $"Prompt template '{TemplateId}' not found.",
                    stopwatch.Elapsed);
            }

            // 4. Build prompt variables from request, context, and metrics
            var variables = BuildPromptVariables(request, context, originalMetrics);

            // 5. Render prompt messages
            var messages = _promptRenderer.RenderMessages(template, variables);

            // 6. Get chat options adjusted for strategy
            var options = GetOptionsForStrategy(request.Strategy);

            // 7. Invoke LLM with timeout
            using var timeoutCts = new CancellationTokenSource(request.Timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var chatRequest = new ChatRequest(messages.ToImmutableArray(), options);
            var response = await _chatService.CompleteAsync(chatRequest, linkedCts.Token);

            // 8. Parse response
            var parseResult = _responseParser.Parse(response.Content);

            // 9. Analyze simplified text readability
            var simplifiedMetrics = await _readabilityService.AnalyzeAsync(parseResult.SimplifiedText, cancellationToken);

            stopwatch.Stop();

            // 10. Calculate usage metrics
            var usage = UsageMetrics.Calculate(
                response.PromptTokens,
                response.CompletionTokens,
                DefaultPromptCostPer1K,
                DefaultCompletionCostPer1K);

            _logger.LogInformation(
                "Completed {Strategy} simplification: Grade {OriginalGrade:F1} → {SimplifiedGrade:F1} ({Reduction:F1} reduction) in {DurationMs}ms",
                request.Strategy,
                originalMetrics.FleschKincaidGradeLevel,
                simplifiedMetrics.FleschKincaidGradeLevel,
                originalMetrics.FleschKincaidGradeLevel - simplifiedMetrics.FleschKincaidGradeLevel,
                stopwatch.ElapsedMilliseconds);

            return new SimplificationResult
            {
                SimplifiedText = parseResult.SimplifiedText,
                OriginalMetrics = originalMetrics,
                SimplifiedMetrics = simplifiedMetrics,
                Changes = parseResult.Changes,
                Glossary = parseResult.Glossary,
                TokenUsage = usage,
                ProcessingTime = stopwatch.Elapsed,
                StrategyUsed = request.Strategy,
                TargetUsed = request.Target,
                Success = true,
                ErrorMessage = null
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // LOGIC: User-initiated cancellation — distinguish from timeout.
            stopwatch.Stop();
            _logger.LogWarning("Simplification cancelled by user");

            return SimplificationResult.Failed(
                request.OriginalText,
                request.Strategy,
                request.Target,
                "Simplification cancelled by user.",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Timeout — the timeoutCts fired before the user ct.
            stopwatch.Stop();
            _logger.LogWarning(
                "Simplification timed out after {Timeout}",
                request.Timeout);

            return SimplificationResult.Failed(
                request.OriginalText,
                request.Strategy,
                request.Target,
                $"Simplification timed out after {request.Timeout.TotalSeconds:F0} seconds.",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            // LOGIC: Generic failure — log and return failed result.
            stopwatch.Stop();
            _logger.LogError(ex, "Simplification failed for {Strategy}", request.Strategy);

            return SimplificationResult.Failed(
                request.OriginalText,
                request.Strategy,
                request.Target,
                ex.Message,
                stopwatch.Elapsed);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SimplificationChunk> SimplifyStreamingAsync(
        SimplificationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        // LOGIC: Yield initializing state immediately so the UI shows feedback.
        yield return SimplificationChunk.Text(string.Empty);

        // 1. Analyze original text readability
        ReadabilityMetrics? originalMetrics = null;
        string? analysisError = null;

        try
        {
            originalMetrics = await _readabilityService.AnalyzeAsync(request.OriginalText, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Original text analysis failed during streaming simplification");
            analysisError = ex.Message;
        }

        if (analysisError is not null)
        {
            yield return SimplificationChunk.Complete($"Analysis failed: {analysisError}");
            yield break;
        }

        // 2. Gather context
        AssembledContext? context = null;
        string? contextError = null;

        try
        {
            context = await GatherContextAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Context gathering failed during streaming simplification");
            contextError = ex.Message;
        }

        if (contextError is not null)
        {
            yield return SimplificationChunk.Complete($"Context gathering failed: {contextError}");
            yield break;
        }

        // 3. Build prompt
        var template = _templateRepository.GetTemplate(TemplateId);

        if (template is null)
        {
            yield return SimplificationChunk.Complete($"Prompt template '{TemplateId}' not found.");
            yield break;
        }

        var variables = BuildPromptVariables(request, context!, originalMetrics!);
        var messages = _promptRenderer.RenderMessages(template, variables);
        var options = GetOptionsForStrategy(request.Strategy);

        // 4. Stream LLM response
        var partialText = new StringBuilder();
        var chatRequest = new ChatRequest(messages.ToImmutableArray(), options);

        await foreach (var token in _chatService.StreamAsync(chatRequest, cancellationToken))
        {
            if (token.HasContent)
            {
                partialText.Append(token.Token);

                yield return SimplificationChunk.Text(token.Token);
            }
        }

        // 5. Yield completion
        yield return SimplificationChunk.Complete(string.Empty);
    }

    /// <inheritdoc />
    public SimplificationValidation ValidateRequest(SimplificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();
        var warnings = new List<string>();

        // LOGIC: Validate OriginalText
        if (string.IsNullOrWhiteSpace(request.OriginalText))
        {
            errors.Add("Original text is required and cannot be empty or whitespace.");
        }
        else if (request.OriginalText.Length > SimplificationRequest.MaxTextLength)
        {
            errors.Add($"Original text exceeds maximum length of {SimplificationRequest.MaxTextLength:N0} characters. " +
                       $"Current length: {request.OriginalText.Length:N0} characters.");
        }
        else if (request.OriginalText.Length > LongTextWarningThreshold)
        {
            warnings.Add($"Text is very long ({request.OriginalText.Length:N0} characters). " +
                        "Simplification may take longer than usual.");
        }

        // LOGIC: Validate Target
        if (request.Target is null)
        {
            errors.Add("Readability target is required.");
        }
        else
        {
            // LOGIC: Check if target is achievable
            if (request.Target.TargetGradeLevel < 1 || request.Target.TargetGradeLevel > 20)
            {
                errors.Add($"Target grade level ({request.Target.TargetGradeLevel}) must be between 1 and 20.");
            }

            if (request.Target.MaxSentenceLength < 5 || request.Target.MaxSentenceLength > 50)
            {
                errors.Add($"Maximum sentence length ({request.Target.MaxSentenceLength}) must be between 5 and 50 words.");
            }
        }

        // LOGIC: Validate Strategy
        if (!Enum.IsDefined(typeof(SimplificationStrategy), request.Strategy))
        {
            errors.Add($"Invalid simplification strategy: {request.Strategy}.");
        }

        // LOGIC: Validate Timeout
        if (request.Timeout <= TimeSpan.Zero)
        {
            errors.Add("Timeout must be positive.");
        }
        else if (request.Timeout > SimplificationRequest.MaxTimeout)
        {
            errors.Add($"Timeout exceeds maximum of {SimplificationRequest.MaxTimeout.TotalMinutes:N0} minutes.");
        }

        // LOGIC: Build result
        if (errors.Count > 0)
        {
            return warnings.Count > 0
                ? SimplificationValidation.InvalidWithWarnings(errors, warnings)
                : SimplificationValidation.Invalid(errors);
        }

        return warnings.Count > 0
            ? SimplificationValidation.ValidWithWarnings(warnings)
            : SimplificationValidation.Valid();
    }

    // ── Private Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Gathers context for a simplification operation via <see cref="IContextOrchestrator"/>.
    /// </summary>
    /// <param name="request">The simplification request containing document path and text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assembled context with fragments sorted by priority.</returns>
    /// <remarks>
    /// LOGIC: Creates a context gathering request with:
    /// <list type="bullet">
    ///   <item><description>Document path from the simplification request</description></item>
    ///   <item><description>Original text for surrounding context strategies</description></item>
    ///   <item><description>Agent ID "simplifier" for strategy filtering</description></item>
    ///   <item><description>4000-token budget with "style" and "terminology" required</description></item>
    /// </list>
    /// If context assembly fails, logs a warning and returns
    /// <see cref="AssembledContext.Empty"/> for graceful degradation.
    /// </remarks>
    private async Task<AssembledContext> GatherContextAsync(
        SimplificationRequest request,
        CancellationToken ct)
    {
        // LOGIC: Build hints dictionary from additional instructions
        IReadOnlyDictionary<string, object>? hints = null;
        if (!string.IsNullOrEmpty(request.AdditionalInstructions))
        {
            hints = new Dictionary<string, object>
            {
                ["additional_instructions"] = request.AdditionalInstructions
            };
        }

        var gatheringRequest = new ContextGatheringRequest(
            DocumentPath: request.DocumentPath,
            CursorPosition: 0,
            SelectedText: request.OriginalText,
            AgentId: AgentId,
            Hints: hints);

        var budget = new ContextBudget(
            MaxTokens: ContextTokenBudget,
            RequiredStrategies: new[] { "style", "terminology" },
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
            // LOGIC: Context assembly failure should not block the simplification.
            // The agent can still function with the original text alone.
            _logger.LogWarning(ex, "Context assembly failed, proceeding with limited context");
            return AssembledContext.Empty;
        }
    }

    /// <summary>
    /// Builds prompt template variables from the simplification request and context.
    /// </summary>
    /// <param name="request">The simplification request with original text and target.</param>
    /// <param name="context">The assembled context with fragments and variables.</param>
    /// <param name="metrics">The readability metrics of the original text.</param>
    /// <returns>Dictionary of variable names to values for Mustache rendering.</returns>
    /// <remarks>
    /// LOGIC: Maps request, context, and metrics to template variables:
    /// <list type="bullet">
    ///   <item><description>original_text: The text to simplify</description></item>
    ///   <item><description>target_grade_level, max_sentence_words, simplify_jargon: From target</description></item>
    ///   <item><description>strategy, strategy_*: From request</description></item>
    ///   <item><description>current_*: From original metrics</description></item>
    ///   <item><description>style_rules, terminology, surrounding_context: From context</description></item>
    /// </list>
    /// </remarks>
    private Dictionary<string, object> BuildPromptVariables(
        SimplificationRequest request,
        AssembledContext context,
        ReadabilityMetrics metrics)
    {
        var variables = new Dictionary<string, object>
        {
            // LOGIC: Original text to simplify
            ["original_text"] = request.OriginalText,

            // LOGIC: Target parameters
            ["target_grade_level"] = request.Target.TargetGradeLevel.ToString("F1"),
            ["max_sentence_words"] = request.Target.MaxSentenceLength,
            ["simplify_jargon"] = request.Target.AvoidJargon,

            // LOGIC: Strategy
            ["strategy"] = request.Strategy.ToString(),
            ["strategy_conservative"] = request.Strategy == SimplificationStrategy.Conservative,
            ["strategy_balanced"] = request.Strategy == SimplificationStrategy.Balanced,
            ["strategy_aggressive"] = request.Strategy == SimplificationStrategy.Aggressive,

            // LOGIC: Current metrics
            ["current_flesch_kincaid"] = metrics.FleschKincaidGradeLevel.ToString("F1"),
            ["current_avg_sentence"] = metrics.AverageWordsPerSentence.ToString("F1"),
            ["current_complex_ratio"] = (metrics.ComplexWordRatio * 100).ToString("F1"),

            // LOGIC: Optional flags
            ["generate_glossary"] = request.GenerateGlossary
        };

        // LOGIC: Add additional instructions if provided
        if (!string.IsNullOrEmpty(request.AdditionalInstructions))
        {
            variables["additional_instructions"] = request.AdditionalInstructions;
        }

        // LOGIC: Map context fragments to template variables by source ID.
        foreach (var fragment in context.Fragments)
        {
            switch (fragment.SourceId)
            {
                case "surrounding-text":
                    variables["surrounding_context"] = fragment.Content;
                    break;
                case "style":
                    variables["style_rules"] = fragment.Content;
                    break;
                case "terminology":
                    variables["terminology"] = fragment.Content;
                    break;
            }
        }

        // LOGIC: Merge orchestrator-extracted variables
        foreach (var (key, value) in context.Variables)
        {
            // Don't overwrite explicitly-set variables.
            variables.TryAdd(key, value);
        }

        return variables;
    }

    /// <summary>
    /// Gets chat options with temperature adjusted for the simplification strategy.
    /// </summary>
    /// <param name="strategy">The simplification strategy.</param>
    /// <returns>Chat options configured for the specified strategy.</returns>
    /// <remarks>
    /// LOGIC: Temperature varies by strategy:
    /// <list type="bullet">
    ///   <item><description>Conservative (0.3): Low temperature for consistent, faithful output</description></item>
    ///   <item><description>Balanced (0.4): Moderate temperature (default)</description></item>
    ///   <item><description>Aggressive (0.5): Higher temperature for creative restructuring</description></item>
    /// </list>
    /// MaxTokens is set to <see cref="DefaultMaxTokens"/> (4096) for all strategies.
    /// </remarks>
    private static ChatOptions GetOptionsForStrategy(SimplificationStrategy strategy)
    {
        var temperature = strategy switch
        {
            SimplificationStrategy.Conservative => 0.3,
            SimplificationStrategy.Balanced => 0.4,
            SimplificationStrategy.Aggressive => 0.5,
            _ => 0.4
        };

        return ChatOptions.Default
            .WithTemperature(temperature)
            .WithMaxTokens(DefaultMaxTokens);
    }
}
