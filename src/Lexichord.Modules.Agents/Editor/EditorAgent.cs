// -----------------------------------------------------------------------
// <copyright file="EditorAgent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implements the Editor Agent — a specialized IAgent for text rewriting
//   operations (v0.7.3b). Orchestrates context assembly, prompt rendering,
//   and LLM invocation for four rewrite intents: Formal, Simplified,
//   Expanded, and Custom.
//
//   Invocation flow (§5.1):
//     1. Validate request (empty text, max length, custom instruction)
//     2. GatherRewriteContext — delegates to IContextOrchestrator with 4000-token budget
//     3. Select prompt template — maps RewriteIntent to template ID
//     4. BuildPromptVariables — extracts context fragments into template variables
//     5. RenderMessages — renders Mustache template to ChatMessage array
//     6. GetOptionsForIntent — adjusts temperature per intent
//     7. Invoke LLM — batch (CompleteAsync) with timeout CTS
//     8. Build RewriteResult — calculates UsageMetrics with default pricing
//
//   License gating (§3.2):
//     - Agent access requires WriterPro (enforced by [RequiresLicense])
//     - Feature code: FeatureCodes.EditorAgent ("Feature.EditorAgent")
//
//   Error handling:
//     - OperationCanceledException from user CancellationToken → "Rewrite cancelled"
//     - OperationCanceledException from timeout CTS → "Rewrite timed out"
//     - Generic exceptions → logged and wrapped in failed RewriteResult
//
//   Thread safety (§9.1):
//     - All injected services are stateless or thread-safe
//     - No shared mutable state — all variables are per-invocation
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// The Editor Agent — specializes in text rewriting with context awareness.
/// </summary>
/// <remarks>
/// <para>
/// Implements both <see cref="IAgent"/> and <see cref="IEditorAgent"/> to function
/// as a registered agent (discoverable via <see cref="IAgentRegistry"/>) while exposing
/// rewrite-specific operations consumed by <see cref="RewriteCommandHandler"/>.
/// </para>
/// <para>
/// The agent supports four rewrite intents, each backed by a distinct prompt template
/// with Mustache-based variable substitution:
/// </para>
/// <list type="bullet">
///   <item><description>Formal: <c>editor-rewrite-formal</c> (Temperature 0.3)</description></item>
///   <item><description>Simplified: <c>editor-rewrite-simplify</c> (Temperature 0.4)</description></item>
///   <item><description>Expanded: <c>editor-rewrite-expand</c> (Temperature 0.5)</description></item>
///   <item><description>Custom: <c>editor-rewrite-custom</c> (Temperature 0.5)</description></item>
/// </list>
/// <para>
/// Context is gathered via <see cref="IContextOrchestrator"/> to include surrounding text,
/// style rules, and terminology from the active document. The orchestrator enforces a
/// 4000-token budget with "style" and "terminology" as required strategies.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro)]
[AgentDefinition("editor", Priority = 101)]
public sealed class EditorAgent : IAgent, IEditorAgent
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IChatCompletionService _chatService;
    private readonly IPromptRenderer _promptRenderer;
    private readonly IPromptTemplateRepository _templateRepository;
    private readonly IContextOrchestrator _contextOrchestrator;
    private readonly ILogger<EditorAgent> _logger;

    // ── Constants ────────────────────────────────────────────────────────

    /// <summary>
    /// Default cost per 1,000 prompt tokens (USD).
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches the CoPilotAgent default pricing. A future enhancement
    /// can read model-specific pricing from configuration.
    /// </remarks>
    private const decimal DefaultPromptCostPer1K = 0.01m;

    /// <summary>
    /// Default cost per 1,000 completion tokens (USD).
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches the CoPilotAgent default pricing.
    /// </remarks>
    private const decimal DefaultCompletionCostPer1K = 0.03m;

    /// <summary>
    /// Maximum token budget for context assembly.
    /// </summary>
    /// <remarks>
    /// LOGIC: 4000 tokens provides sufficient context for surrounding text,
    /// style rules, and terminology without consuming too much of the model's
    /// context window. The remaining window is used for the system prompt,
    /// selected text, and response generation.
    /// </remarks>
    private const int ContextTokenBudget = 4000;

    /// <summary>
    /// Maximum tokens to generate in the response.
    /// </summary>
    /// <remarks>
    /// LOGIC: 2048 tokens is sufficient for most rewrite operations.
    /// Longer selections may require more, but the response is typically
    /// similar in length to the input.
    /// </remarks>
    private const int DefaultMaxTokens = 2048;

    /// <summary>
    /// Initializes a new instance of <see cref="EditorAgent"/>.
    /// </summary>
    /// <param name="chatService">LLM chat completion service for batch and streaming requests.</param>
    /// <param name="promptRenderer">Template renderer for prompt assembly.</param>
    /// <param name="templateRepository">Repository for loading prompt templates.</param>
    /// <param name="contextOrchestrator">Context assembly from document, style, and terminology sources.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public EditorAgent(
        IChatCompletionService chatService,
        IPromptRenderer promptRenderer,
        IPromptTemplateRepository templateRepository,
        IContextOrchestrator contextOrchestrator,
        ILogger<EditorAgent> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _contextOrchestrator = contextOrchestrator ?? throw new ArgumentNullException(nameof(contextOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── IAgent Properties ───────────────────────────────────────────────

    /// <inheritdoc />
    public string AgentId => "editor";

    /// <inheritdoc />
    public string Name => "The Editor";

    /// <inheritdoc />
    public string Description =>
        "Transforms text while respecting context and style rules.";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns the default template used for the IAgent.InvokeAsync pathway.
    /// The rewrite-specific pathways (RewriteAsync) select templates dynamically
    /// based on the <see cref="RewriteIntent"/>.
    /// </remarks>
    public IPromptTemplate Template => _templateRepository.GetTemplate("editor-rewrite-formal")!;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: The Editor Agent supports chat, document context, style enforcement,
    /// and streaming. RAGContext is not included because the editor operates on
    /// user-selected text rather than retrieved knowledge base content.
    /// </remarks>
    public AgentCapabilities Capabilities =>
        AgentCapabilities.Chat |
        AgentCapabilities.DocumentContext |
        AgentCapabilities.StyleEnforcement |
        AgentCapabilities.Streaming;

    // ── IAgent.InvokeAsync ──────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Delegates to <see cref="RewriteAsync"/> with a Custom intent.
    /// The user message from the <see cref="AgentRequest"/> is treated as a
    /// custom rewriting instruction. If the request has a selection, it becomes
    /// the text to rewrite; otherwise, the user message is used.
    /// </remarks>
    public async Task<AgentResponse> InvokeAsync(
        AgentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        _logger.LogDebug(
            "EditorAgent.InvokeAsync: delegating to RewriteAsync with Custom intent");

        // LOGIC: Map AgentRequest to RewriteRequest for the Custom intent pathway.
        // Selection becomes the text to rewrite; user message becomes the instruction.
        var rewriteRequest = new RewriteRequest
        {
            SelectedText = request.Selection ?? request.UserMessage,
            SelectionSpan = new Abstractions.Contracts.Editor.TextSpan(0, (request.Selection ?? request.UserMessage).Length),
            Intent = RewriteIntent.Custom,
            CustomInstruction = request.HasSelection ? request.UserMessage : "Improve this text.",
            DocumentPath = request.DocumentPath
        };

        var result = await RewriteAsync(rewriteRequest, ct);

        return new AgentResponse(
            result.RewrittenText,
            Citations: null,
            result.Usage);
    }

    // ── IEditorAgent Implementation ─────────────────────────────────────

    /// <inheritdoc />
    public async Task<RewriteResult> RewriteAsync(
        RewriteRequest request,
        CancellationToken ct = default)
    {
        request.Validate();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting {Intent} rewrite for {CharCount} characters",
                request.Intent,
                request.SelectedText.Length);

            // 1. Gather context via IContextOrchestrator
            var context = await GatherRewriteContextAsync(request, ct);

            _logger.LogDebug(
                "Context gathered: {FragmentCount} fragments, {TotalTokens} tokens",
                context.Fragments.Count,
                context.TotalTokens);

            // 2. Get template for the requested intent
            var templateId = GetTemplateId(request.Intent);
            var template = _templateRepository.GetTemplate(templateId);

            if (template is null)
            {
                _logger.LogError(
                    "Prompt template not found: {TemplateId}",
                    templateId);

                stopwatch.Stop();
                return RewriteResult.Failed(
                    request.SelectedText,
                    request.Intent,
                    $"Prompt template '{templateId}' not found.",
                    stopwatch.Elapsed);
            }

            // 3. Build prompt variables from request and context
            var variables = BuildPromptVariables(request, context);

            // 4. Render prompt messages
            var messages = _promptRenderer.RenderMessages(template, variables);

            // 5. Get chat options adjusted for intent
            var options = GetOptionsForIntent(request.Intent);

            // 6. Invoke LLM with timeout
            using var timeoutCts = new CancellationTokenSource(request.Timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var chatRequest = new ChatRequest(messages.ToImmutableArray(), options);
            var response = await _chatService.CompleteAsync(chatRequest, linkedCts.Token);

            stopwatch.Stop();

            // 7. Calculate usage metrics
            var usage = UsageMetrics.Calculate(
                response.PromptTokens,
                response.CompletionTokens,
                DefaultPromptCostPer1K,
                DefaultCompletionCostPer1K);

            _logger.LogInformation(
                "Completed {Intent} rewrite: {OriginalLength} -> {RewrittenLength} chars in {DurationMs}ms",
                request.Intent,
                request.SelectedText.Length,
                response.Content.Trim().Length,
                stopwatch.ElapsedMilliseconds);

            return new RewriteResult
            {
                OriginalText = request.SelectedText,
                RewrittenText = response.Content.Trim(),
                Intent = request.Intent,
                Success = true,
                ErrorMessage = null,
                Usage = usage,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // LOGIC: User-initiated cancellation — distinguish from timeout.
            stopwatch.Stop();
            _logger.LogWarning("Rewrite cancelled by user");

            return RewriteResult.Failed(
                request.SelectedText,
                request.Intent,
                "Rewrite cancelled by user.",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Timeout — the timeoutCts fired before the user ct.
            stopwatch.Stop();
            _logger.LogWarning(
                "Rewrite timed out after {Timeout}",
                request.Timeout);

            return RewriteResult.Failed(
                request.SelectedText,
                request.Intent,
                $"Rewrite timed out after {request.Timeout.TotalSeconds:F0} seconds.",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            // LOGIC: Generic failure — log and return failed result.
            stopwatch.Stop();
            _logger.LogError(ex, "Rewrite failed for {Intent}", request.Intent);

            return RewriteResult.Failed(
                request.SelectedText,
                request.Intent,
                ex.Message,
                stopwatch.Elapsed);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<RewriteProgressUpdate> RewriteStreamingAsync(
        RewriteRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        request.Validate();

        // LOGIC: Yield initializing state immediately so the UI shows feedback.
        yield return new RewriteProgressUpdate
        {
            PartialText = string.Empty,
            ProgressPercentage = 0,
            State = RewriteProgressState.Initializing,
            StatusMessage = "Preparing rewrite..."
        };

        // 1. Gather context
        yield return new RewriteProgressUpdate
        {
            PartialText = string.Empty,
            ProgressPercentage = 10,
            State = RewriteProgressState.GatheringContext,
            StatusMessage = "Gathering document context..."
        };

        // LOGIC: C# does not allow yield return inside catch blocks.
        // Use a flag pattern to handle context gathering failures.
        AssembledContext? context = null;
        string? contextError = null;

        try
        {
            context = await GatherRewriteContextAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Context gathering failed during streaming rewrite");
            contextError = ex.Message;
        }

        if (contextError is not null)
        {
            yield return new RewriteProgressUpdate
            {
                PartialText = string.Empty,
                ProgressPercentage = 0,
                State = RewriteProgressState.Failed,
                StatusMessage = $"Context gathering failed: {contextError}"
            };
            yield break;
        }

        // 2. Build prompt
        var templateId = GetTemplateId(request.Intent);
        var template = _templateRepository.GetTemplate(templateId);

        if (template is null)
        {
            yield return new RewriteProgressUpdate
            {
                PartialText = string.Empty,
                ProgressPercentage = 0,
                State = RewriteProgressState.Failed,
                StatusMessage = $"Prompt template '{templateId}' not found."
            };
            yield break;
        }

        var variables = BuildPromptVariables(request, context!);
        var messages = _promptRenderer.RenderMessages(template, variables);
        var options = GetOptionsForIntent(request.Intent);

        yield return new RewriteProgressUpdate
        {
            PartialText = string.Empty,
            ProgressPercentage = 25,
            State = RewriteProgressState.GeneratingRewrite,
            StatusMessage = "Generating rewrite..."
        };

        // 3. Stream LLM response
        var partialText = new StringBuilder();
        var chatRequest = new ChatRequest(messages.ToImmutableArray(), options);

        await foreach (var token in _chatService.StreamAsync(chatRequest, ct))
        {
            if (token.HasContent)
            {
                partialText.Append(token.Token);

                // LOGIC: Estimate progress based on expected output length.
                // Output is typically similar in length to input, so we scale
                // between 25% (start of generation) and 95% (near completion).
                var estimatedCompletion = Math.Min(
                    25 + (partialText.Length / (double)request.SelectedText.Length) * 70,
                    95);

                yield return new RewriteProgressUpdate
                {
                    PartialText = partialText.ToString(),
                    ProgressPercentage = estimatedCompletion,
                    State = RewriteProgressState.GeneratingRewrite,
                    StatusMessage = "Generating rewrite..."
                };
            }
        }

        // 4. Yield completion
        yield return new RewriteProgressUpdate
        {
            PartialText = partialText.ToString().Trim(),
            ProgressPercentage = 100,
            State = RewriteProgressState.Completed,
            StatusMessage = "Rewrite complete."
        };
    }

    /// <inheritdoc />
    public string GetTemplateId(RewriteIntent intent) => intent switch
    {
        RewriteIntent.Formal => "editor-rewrite-formal",
        RewriteIntent.Simplified => "editor-rewrite-simplify",
        RewriteIntent.Expanded => "editor-rewrite-expand",
        RewriteIntent.Custom => "editor-rewrite-custom",
        _ => throw new ArgumentOutOfRangeException(
            nameof(intent),
            intent,
            $"Unsupported rewrite intent: {intent}")
    };

    // ── Private Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Gathers context for a rewrite operation via <see cref="IContextOrchestrator"/>.
    /// </summary>
    /// <param name="request">The rewrite request containing document path and selection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assembled context with fragments sorted by priority.</returns>
    /// <remarks>
    /// LOGIC: Creates a context gathering request with:
    /// <list type="bullet">
    ///   <item><description>Document path from the rewrite request</description></item>
    ///   <item><description>Cursor position at the selection start</description></item>
    ///   <item><description>Selected text for surrounding context strategies</description></item>
    ///   <item><description>Agent ID "editor" for strategy filtering</description></item>
    ///   <item><description>4000-token budget with "style" and "terminology" required</description></item>
    /// </list>
    /// If context assembly fails, logs a warning and returns
    /// <see cref="AssembledContext.Empty"/> for graceful degradation.
    /// </remarks>
    private async Task<AssembledContext> GatherRewriteContextAsync(
        RewriteRequest request,
        CancellationToken ct)
    {
        var gatheringRequest = new ContextGatheringRequest(
            DocumentPath: request.DocumentPath,
            CursorPosition: request.SelectionSpan.Start,
            SelectedText: request.SelectedText,
            AgentId: AgentId,
            Hints: request.AdditionalContext);

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
            // LOGIC: Context assembly failure should not block the rewrite.
            // The agent can still function with the selected text alone.
            _logger.LogWarning(ex, "Context assembly failed, proceeding with limited context");
            return AssembledContext.Empty;
        }
    }

    /// <summary>
    /// Builds prompt template variables from the rewrite request and assembled context.
    /// </summary>
    /// <param name="request">The rewrite request with selected text and custom instruction.</param>
    /// <param name="context">The assembled context with fragments and variables.</param>
    /// <returns>Dictionary of variable names to values for Mustache rendering.</returns>
    /// <remarks>
    /// LOGIC: Maps context fragments to template variables by source ID:
    /// <list type="bullet">
    ///   <item><description>"surrounding-text" → <c>surrounding_context</c></description></item>
    ///   <item><description>"style" → <c>style_rules</c></description></item>
    ///   <item><description>"terminology" → <c>terminology</c></description></item>
    /// </list>
    /// The <c>selection</c> variable always comes from the request.
    /// The <c>custom_instruction</c> variable is included when present.
    /// Additional context variables from the orchestrator are merged last.
    /// </remarks>
    private Dictionary<string, object> BuildPromptVariables(
        RewriteRequest request,
        AssembledContext context)
    {
        var variables = new Dictionary<string, object>
        {
            ["selection"] = request.SelectedText
        };

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

        // LOGIC: Include custom instruction for Custom intent.
        if (!string.IsNullOrEmpty(request.CustomInstruction))
        {
            variables["custom_instruction"] = request.CustomInstruction;
        }

        // LOGIC: Merge orchestrator-extracted variables (document name, cursor position, etc.)
        foreach (var (key, value) in context.Variables)
        {
            // Don't overwrite explicitly-set variables from request/fragments.
            variables.TryAdd(key, value);
        }

        return variables;
    }

    /// <summary>
    /// Gets chat options with temperature adjusted for the rewrite intent.
    /// </summary>
    /// <param name="intent">The rewrite intent.</param>
    /// <returns>Chat options configured for the specified intent.</returns>
    /// <remarks>
    /// LOGIC: Temperature varies by intent:
    /// <list type="bullet">
    ///   <item><description>Formal (0.3): Low temperature for consistent, professional output</description></item>
    ///   <item><description>Simplified (0.4): Slightly higher for readability variation</description></item>
    ///   <item><description>Expanded (0.5): More creative for elaboration</description></item>
    ///   <item><description>Custom (0.5): Balanced for user-directed transformations</description></item>
    /// </list>
    /// MaxTokens is set to <see cref="DefaultMaxTokens"/> (2048) for all intents.
    /// </remarks>
    private static ChatOptions GetOptionsForIntent(RewriteIntent intent)
    {
        var temperature = intent switch
        {
            RewriteIntent.Formal => 0.3,
            RewriteIntent.Simplified => 0.4,
            RewriteIntent.Expanded => 0.5,
            RewriteIntent.Custom => 0.5,
            _ => 0.3
        };

        return ChatOptions.Default
            .WithTemperature(temperature)
            .WithMaxTokens(DefaultMaxTokens);
    }
}
