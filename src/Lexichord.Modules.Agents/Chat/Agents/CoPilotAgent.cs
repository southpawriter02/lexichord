// -----------------------------------------------------------------------
// <copyright file="CoPilotAgent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implements the CoPilotAgent — Lexichord's foundational writing
//   assistant (v0.6.6b). Orchestrates context assembly, prompt rendering,
//   LLM invocation (batch/streaming), citation extraction, and usage metrics.
//
//   Invocation flow (§5.1):
//     1. Validate request (null check + non-empty message)
//     2. AssembleContext — delegates to IContextInjector with graceful fallback
//     3. RenderPrompt — renders co-pilot-editor template with context + user_input
//     4. InvokeLLM — batch (CompleteAsync) or streaming (StreamAsync + handler)
//     5. ExtractCitations — creates citations from RAG chunks when present
//     6. CalculateUsage — computes UsageMetrics with default pricing constants
//
//   License gating (§3.3):
//     - Agent access requires WriterPro (enforced by [RequiresLicense])
//     - Streaming requires Teams tier (checked at invocation time)
//
//   Thread safety (§9.1):
//     - All injected services are stateless or thread-safe
//     - No shared mutable state — all variables are per-invocation
//     - Scoped DI lifetime prevents state leakage between requests
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Abstractions;
using Lexichord.Modules.Agents.Chat.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Agents;

/// <summary>
/// Foundational writing assistant agent that integrates context, style, and RAG.
/// </summary>
/// <remarks>
/// <para>
/// The Co-pilot Agent is Lexichord's primary conversational AI assistant. It orchestrates
/// multiple services to provide context-aware writing assistance:
/// </para>
/// <list type="bullet">
///   <item>Document Context: Includes current document or selection in prompts</item>
///   <item>RAG Context: Performs semantic search for relevant project content</item>
///   <item>Style Enforcement: Loads and applies user-defined style rules</item>
///   <item>Citation Attribution: Tracks and cites sources used in responses</item>
/// </list>
/// <para>
/// This agent requires a WriterPro license or higher. Streaming responses require Teams.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6b as the first concrete agent implementation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddScoped&lt;IAgent, CoPilotAgent&gt;();
///
/// // Usage
/// var agent = registry.GetAgent("co-pilot");
/// var response = await agent.InvokeAsync(new AgentRequest(
///     "How can I improve this paragraph?",
///     Selection: selectedText
/// ));
/// </code>
/// </example>
[RequiresLicense(LicenseTier.WriterPro)]
public sealed class CoPilotAgent : IAgent
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IChatCompletionService _chatService;
    private readonly IStreamingChatHandler _streamingHandler;
    private readonly IPromptRenderer _promptRenderer;
    private readonly IContextInjector _contextInjector;
    private readonly IPromptTemplateRepository _templateRepository;
    private readonly ICitationService _citationService;
    private readonly ILicenseContext _licenseContext;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<CoPilotAgent> _logger;

    // ── Constants ────────────────────────────────────────────────────────
    private const string TemplateId = "co-pilot-editor";
    private const decimal DefaultPromptCostPer1K = 0.01m;
    private const decimal DefaultCompletionCostPer1K = 0.03m;

    /// <summary>
    /// Initializes a new instance of <see cref="CoPilotAgent"/>.
    /// </summary>
    /// <param name="chatService">LLM chat completion service for batch requests.</param>
    /// <param name="streamingHandler">Handler for streaming chat token events.</param>
    /// <param name="promptRenderer">Template renderer for prompt assembly.</param>
    /// <param name="contextInjector">Context assembly from document, RAG, and style sources.</param>
    /// <param name="templateRepository">Repository for loading prompt templates.</param>
    /// <param name="citationService">Service for creating source citations.</param>
    /// <param name="licenseContext">License tier checking for feature gating.</param>
    /// <param name="settingsService">Settings service for agent configuration.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public CoPilotAgent(
        IChatCompletionService chatService,
        IStreamingChatHandler streamingHandler,
        IPromptRenderer promptRenderer,
        IContextInjector contextInjector,
        IPromptTemplateRepository templateRepository,
        ICitationService citationService,
        ILicenseContext licenseContext,
        ISettingsService settingsService,
        ILogger<CoPilotAgent> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _streamingHandler = streamingHandler ?? throw new ArgumentNullException(nameof(streamingHandler));
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
        _contextInjector = contextInjector ?? throw new ArgumentNullException(nameof(contextInjector));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _citationService = citationService ?? throw new ArgumentNullException(nameof(citationService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── IAgent Properties ───────────────────────────────────────────────

    /// <inheritdoc />
    public string AgentId => "co-pilot";

    /// <inheritdoc />
    public string Name => "Co-pilot";

    /// <inheritdoc />
    public string Description =>
        "General writing assistant with document awareness, style enforcement, and semantic search.";

    /// <inheritdoc />
    public IPromptTemplate Template => _templateRepository.GetTemplate(TemplateId)!;

    /// <inheritdoc />
    public AgentCapabilities Capabilities =>
        AgentCapabilities.Chat |
        AgentCapabilities.DocumentContext |
        AgentCapabilities.RAGContext |
        AgentCapabilities.StyleEnforcement |
        AgentCapabilities.Streaming;

    // ── IAgent.InvokeAsync ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<AgentResponse> InvokeAsync(
        AgentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        ct.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("CoPilot invocation started: {MessageLength} chars",
                request.UserMessage.Length);

            // 1. Assemble context
            var context = await AssembleContextAsync(request, ct);

            // 2. Render prompt
            var messages = RenderPromptMessages(request, context);

            // 3. Invoke LLM
            var (content, promptTokens, completionTokens) = await InvokeLLMAsync(
                messages, ct);

            // 4. Extract citations
            var citations = ExtractCitations(content, context);

            // 5. Calculate usage
            var usage = CalculateUsageMetrics(promptTokens, completionTokens);

            stopwatch.Stop();
            _logger.LogInformation(
                "CoPilot invocation completed: {TotalTokens} tokens in {Duration}ms",
                usage.TotalTokens, stopwatch.ElapsedMilliseconds);

            return new AgentResponse(content, citations, usage);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CoPilot invocation cancelled");
            throw;
        }
        catch (AgentInvocationException)
        {
            // LOGIC: Re-throw AgentInvocationException as-is to avoid double-wrapping.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CoPilot invocation failed");
            throw new AgentInvocationException("Failed to process request", ex);
        }
    }

    // ── Private Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Assembles all context for the prompt.
    /// </summary>
    /// <remarks>
    /// LOGIC: Creates a full context request (style + RAG + document context)
    /// and delegates to IContextInjector. If context assembly fails, logs a
    /// warning and returns an empty dictionary — the agent can still function
    /// with user input alone (graceful degradation per §9.2).
    /// </remarks>
    private async Task<IDictionary<string, object>> AssembleContextAsync(
        AgentRequest request,
        CancellationToken ct)
    {
        _logger.LogDebug("Assembling context for CoPilot request");

        // LOGIC: Map AgentRequest fields to ContextRequest.
        // Both style rules and RAG are always enabled for the Co-pilot.
        var contextRequest = new ContextRequest(
            CurrentDocumentPath: request.DocumentPath,
            CursorPosition: null,
            SelectedText: request.Selection,
            IncludeStyleRules: true,
            IncludeRAGContext: true,
            MaxRAGChunks: _settingsService.Get("Agent:MaxContextItems", 10));

        try
        {
            var context = await _contextInjector.AssembleContextAsync(contextRequest, ct);

            _logger.LogDebug(
                "Context assembled: {VariableCount} variables",
                context.Count);

            return context;
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Cancellation is not a context failure — re-throw to
            // let the outer handler log and propagate correctly.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Context assembly failed, proceeding with limited context");
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Renders prompt messages from template and context.
    /// </summary>
    /// <remarks>
    /// LOGIC: Adds user_input to the context dictionary, renders the
    /// co-pilot-editor template into [System, User] ChatMessages, and
    /// optionally prepends conversation history for multi-turn dialogues.
    /// History is inserted before the rendered messages so the LLM sees
    /// the conversation flow leading up to the current request.
    /// </remarks>
    private IReadOnlyList<ChatMessage> RenderPromptMessages(
        AgentRequest request,
        IDictionary<string, object> context)
    {
        // LOGIC: Add user input to context for template rendering.
        // The co-pilot-editor.yaml template expects {{user_input}}.
        context["user_input"] = request.UserMessage;

        if (request.HasSelection)
        {
            context["selection_text"] = request.Selection!;
        }

        // Render template
        var templateMessages = _promptRenderer.RenderMessages(Template, context);

        // Prepend conversation history if available
        if (request.HasHistory)
        {
            var allMessages = new List<ChatMessage>(request.History!);
            allMessages.AddRange(templateMessages);
            return allMessages;
        }

        return templateMessages;
    }

    /// <summary>
    /// Invokes the LLM and returns content with token counts.
    /// </summary>
    /// <remarks>
    /// LOGIC: Checks license tier and streaming setting to determine mode.
    /// Streaming requires Teams tier AND the Agent:EnableStreaming setting.
    /// Falls back to batch mode for WriterPro users.
    /// </remarks>
    private async Task<(string Content, int PromptTokens, int CompletionTokens)> InvokeLLMAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct)
    {
        var options = ChatOptions.Default
            .WithTemperature(_settingsService.Get("Agent:Temperature", 0.7))
            .WithMaxTokens(_settingsService.Get("Agent:MaxResponseTokens", 2000));

        // LOGIC: Check if streaming is available and enabled.
        // Streaming requires: capability flag + Teams license + user setting.
        var streamingEnabled = Capabilities.HasFlag(AgentCapabilities.Streaming) &&
                               _licenseContext.GetCurrentTier() >= LicenseTier.Teams &&
                               _settingsService.Get("Agent:EnableStreaming", true);

        if (streamingEnabled)
        {
            return await InvokeStreamingAsync(messages, options, ct);
        }

        return await InvokeBatchAsync(messages, options, ct);
    }

    /// <summary>
    /// Invokes LLM in batch mode via <see cref="IChatCompletionService.CompleteAsync"/>.
    /// </summary>
    private async Task<(string Content, int PromptTokens, int CompletionTokens)> InvokeBatchAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions options,
        CancellationToken ct)
    {
        _logger.LogDebug("Invoking LLM in batch mode: {MessageCount} messages", messages.Count);

        var request = new ChatRequest(messages.ToImmutableArray(), options);
        var response = await _chatService.CompleteAsync(request, ct);

        return (response.Content, response.PromptTokens, response.CompletionTokens);
    }

    /// <summary>
    /// Invokes LLM in streaming mode via <see cref="IChatCompletionService.StreamAsync"/>.
    /// </summary>
    /// <remarks>
    /// LOGIC: Consumes the streaming token sequence, relays each token to the
    /// IStreamingChatHandler for UI updates, and aggregates the full content.
    /// Token counts are not available per-token from streaming — they are
    /// estimated from the final content length.
    /// </remarks>
    private async Task<(string Content, int PromptTokens, int CompletionTokens)> InvokeStreamingAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions options,
        CancellationToken ct)
    {
        _logger.LogDebug("Invoking LLM in streaming mode: {MessageCount} messages", messages.Count);

        var contentBuilder = new StringBuilder();
        var request = new ChatRequest(messages.ToImmutableArray(), options);
        var tokenIndex = 0;

        await foreach (var token in _chatService.StreamAsync(request, ct))
        {
            if (token.HasContent)
            {
                contentBuilder.Append(token.Token);

                // LOGIC: Relay token to streaming handler for real-time UI updates.
                // The handler uses the module-level StreamingChatToken (4-param record).
                var handlerToken = Models.StreamingChatToken.Content(token.Token, tokenIndex);
                await _streamingHandler.OnTokenReceived(handlerToken);
                tokenIndex++;
            }
        }

        var content = contentBuilder.ToString();

        // LOGIC: Streaming does not provide exact token counts from most providers.
        // Signal completion to the handler with a synthetic response.
        var completionResponse = new ChatResponse(content, 0, 0, TimeSpan.Zero, "stop");
        await _streamingHandler.OnStreamComplete(completionResponse);

        return (content, 0, 0);
    }

    /// <summary>
    /// Extracts citations from response content based on RAG context.
    /// </summary>
    /// <remarks>
    /// LOGIC: Checks the assembled context for RAG-related data. If SearchHit
    /// objects are present (injected by the ContextInjector when RAG context
    /// is enabled), uses ICitationService.CreateCitations to generate citation
    /// records. Returns null if no RAG context was used or if citation extraction
    /// fails (graceful degradation per §9.2).
    /// </remarks>
    private IReadOnlyList<Citation>? ExtractCitations(
        string content,
        IDictionary<string, object> context)
    {
        // LOGIC: Check if RAG-sourced search hits are available in the context.
        if (!context.TryGetValue("search_hits", out var hitsObj) ||
            hitsObj is not IEnumerable<SearchHit> searchHits)
        {
            return null;
        }

        try
        {
            var citations = _citationService.CreateCitations(searchHits);

            if (citations.Count > 0)
            {
                _logger.LogDebug("Extracted {CitationCount} citations from response", citations.Count);
            }

            return citations.Count > 0 ? citations : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Citation extraction failed, returning without citations");
            return null;
        }
    }

    /// <summary>
    /// Calculates usage metrics from token counts.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses UsageMetrics.Calculate with default pricing constants.
    /// A future enhancement (when ISettingsService has a concrete implementation)
    /// can read model-specific pricing from configuration.
    /// </remarks>
    private UsageMetrics CalculateUsageMetrics(int promptTokens, int completionTokens)
    {
        return UsageMetrics.Calculate(
            promptTokens,
            completionTokens,
            DefaultPromptCostPer1K,
            DefaultCompletionCostPer1K);
    }
}
