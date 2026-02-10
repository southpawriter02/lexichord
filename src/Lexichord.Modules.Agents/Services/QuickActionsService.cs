// -----------------------------------------------------------------------
// <copyright file="QuickActionsService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Implementation of <see cref="IQuickActionsService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages a registry of quick actions (both built-in and custom), provides
/// context-aware filtering, and orchestrates action execution through the
/// agent pipeline. Built-in actions are loaded from <see cref="BuiltInQuickActions"/>
/// at construction time.
/// </para>
/// <para>
/// <b>Template Resolution:</b> When executing an action, the service first
/// attempts to resolve the prompt template from <see cref="IPromptTemplateRepository"/>.
/// If no match is found, it falls back to the built-in templates defined in
/// <see cref="BuiltInQuickActions.PromptTemplates"/>. This allows users to override
/// built-in templates by placing custom YAML files with matching template IDs.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
public class QuickActionsService : IQuickActionsService
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IPromptTemplateRepository _templates;
    private readonly IPromptRenderer _renderer;
    private readonly IDocumentContextAnalyzer _contextAnalyzer;
    private readonly IEditorService _editorService;
    private readonly ILicenseContext _license;
    private readonly IMediator _mediator;
    private readonly ILogger<QuickActionsService> _logger;

    /// <summary>
    /// Internal dictionary of registered actions, keyed by <see cref="QuickAction.ActionId"/>.
    /// </summary>
    private readonly Dictionary<string, QuickAction> _actions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickActionsService"/> class.
    /// </summary>
    /// <param name="agentRegistry">Agent discovery and selection service.</param>
    /// <param name="templates">Prompt template repository for template resolution.</param>
    /// <param name="renderer">Prompt renderer for Mustache template rendering.</param>
    /// <param name="contextAnalyzer">Document structure analyzer for content type detection.</param>
    /// <param name="editorService">Editor service for selection and cursor state.</param>
    /// <param name="license">License context for tier-based feature gating.</param>
    /// <param name="mediator">MediatR mediator for publishing execution events.</param>
    /// <param name="logger">Logger for structured logging.</param>
    public QuickActionsService(
        IAgentRegistry agentRegistry,
        IPromptTemplateRepository templates,
        IPromptRenderer renderer,
        IDocumentContextAnalyzer contextAnalyzer,
        IEditorService editorService,
        ILicenseContext license,
        IMediator mediator,
        ILogger<QuickActionsService> logger)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _contextAnalyzer = contextAnalyzer ?? throw new ArgumentNullException(nameof(contextAnalyzer));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _license = license ?? throw new ArgumentNullException(nameof(license));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Register built-in actions from BuiltInQuickActions.All.
        // These provide the default set of quick actions available to all
        // WriterPro+ users. Custom actions can be added via RegisterAction().
        foreach (var action in BuiltInQuickActions.All)
        {
            _actions[action.ActionId] = action;
        }

        _logger.LogDebug(
            "QuickActionsService initialized with {Count} built-in actions",
            _actions.Count);
    }

    /// <inheritdoc/>
    public IReadOnlyList<QuickAction> AllActions =>
        _actions.Values.OrderBy(a => a.Order).ToList();

    /// <inheritdoc/>
    public bool ShouldShowPanel => _editorService.HasSelection;

    /// <inheritdoc/>
#pragma warning disable CS0067 // Event may not be raised in all code paths — raised in ExecuteAsync
    public event EventHandler<QuickActionExecutedEventArgs>? ActionExecuted;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QuickAction>> GetAvailableActionsAsync(
        CancellationToken ct = default)
    {
        // ─────────────────────────────────────────────────────────────────
        // Get Current Context
        // ─────────────────────────────────────────────────────────────────
        var documentPath = _editorService.CurrentDocumentPath;
        var cursorPosition = _editorService.CaretOffset;
        var hasSelection = _editorService.HasSelection;

        var contentType = ContentBlockType.Prose;

        // LOGIC: Analyze document structure to determine content type if
        // a document is active. Default to Prose if no document is open.
        if (!string.IsNullOrEmpty(documentPath))
        {
            var context = await _contextAnalyzer.AnalyzeAtPositionAsync(
                documentPath, cursorPosition, ct);
            contentType = context.ContentType;
        }

        _logger.LogDebug(
            "Filtering actions for content type {Type}, hasSelection={HasSelection}",
            contentType, hasSelection);

        // ─────────────────────────────────────────────────────────────────
        // Filter Actions
        // ─────────────────────────────────────────────────────────────────

        // LOGIC: Filter actions by three criteria:
        // 1. Content type compatibility — action must apply to the detected content type
        // 2. Selection requirement — if action requires selection, editor must have one
        // 3. License tier — user's license must meet the action's minimum tier
        var currentTier = _license.GetCurrentTier();

        var availableActions = _actions.Values
            .Where(a => a.AppliesTo(contentType))
            .Where(a => !a.RequiresSelection || hasSelection)
            .Where(a => currentTier >= a.MinimumLicenseTier)
            .OrderBy(a => a.Order)
            .ToList();

        _logger.LogDebug(
            "Available actions: {Count} of {Total}",
            availableActions.Count, _actions.Count);

        return availableActions;
    }

    /// <inheritdoc/>
    public async Task<QuickActionResult> ExecuteAsync(
        string actionId,
        string inputText,
        CancellationToken ct = default)
    {
        // LOGIC: Validate that the action ID is registered.
        if (!_actions.TryGetValue(actionId, out var action))
        {
            throw new ArgumentException($"Unknown action: {actionId}", nameof(actionId));
        }

        _logger.LogDebug(
            "Executing quick action {ActionId}: {CharCount} chars",
            actionId, inputText.Length);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // ─────────────────────────────────────────────────────────────
            // Resolve Prompt Template
            // ─────────────────────────────────────────────────────────────

            // LOGIC: Two-tier template resolution:
            // 1. First, check the IPromptTemplateRepository (allows user overrides)
            // 2. Fall back to BuiltInQuickActions.PromptTemplates
            // 3. If neither has the template, return a failure result
            var variables = new Dictionary<string, object>
            {
                ["text"] = inputText
            };

            ChatMessage[] messages;
            var repositoryTemplate = _templates.GetTemplate(action.PromptTemplateId);

            if (repositoryTemplate is not null)
            {
                // LOGIC: Repository template found — render via IPromptRenderer.
                _logger.LogDebug(
                    "Using repository template {TemplateId} for action {ActionId}",
                    action.PromptTemplateId, actionId);

                messages = _renderer.RenderMessages(repositoryTemplate, variables);
            }
            else if (BuiltInQuickActions.PromptTemplates.TryGetValue(
                         action.PromptTemplateId, out var definition))
            {
                // LOGIC: Built-in template fallback — render system and user
                // prompts individually via IPromptRenderer.Render().
                _logger.LogDebug(
                    "Using built-in template {TemplateId} for action {ActionId}",
                    action.PromptTemplateId, actionId);

                var systemContent = _renderer.Render(definition.SystemPrompt, variables);
                var userContent = _renderer.Render(definition.UserPromptTemplate, variables);

                messages =
                [
                    ChatMessage.System(systemContent),
                    ChatMessage.User(userContent)
                ];
            }
            else
            {
                // LOGIC: No template found in either source — cannot proceed.
                _logger.LogError(
                    "Prompt template not found: {TemplateId}",
                    action.PromptTemplateId);

                return new QuickActionResult(
                    Success: false,
                    Text: string.Empty,
                    ErrorMessage: "Prompt template not found");
            }

            // ─────────────────────────────────────────────────────────────
            // Get Agent and Execute
            // ─────────────────────────────────────────────────────────────

            // LOGIC: Resolve the agent — use the action's specific agent if
            // configured, otherwise use the default agent from the registry.
            IAgent agent;

            try
            {
                agent = !string.IsNullOrEmpty(action.AgentId)
                    ? _agentRegistry.GetAgent(action.AgentId)
                    : _agentRegistry.GetDefaultAgent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No agent available for action {ActionId}", actionId);
                return new QuickActionResult(
                    Success: false,
                    Text: string.Empty,
                    ErrorMessage: "No agent available");
            }

            _logger.LogDebug(
                "Using agent {AgentId} for action {ActionId}",
                agent.AgentId, actionId);

            // LOGIC: Construct the agent request from the rendered messages.
            // The last message is the user message; preceding messages form history.
            var request = new AgentRequest(
                UserMessage: messages.Last().Content,
                History: messages.Length > 1 ? messages.SkipLast(1).ToList() : null);

            var response = await agent.InvokeAsync(request, ct);

            stopwatch.Stop();

            // ─────────────────────────────────────────────────────────────
            // Create Result
            // ─────────────────────────────────────────────────────────────
            var result = new QuickActionResult(
                Success: true,
                Text: response.Content,
                Duration: stopwatch.Elapsed);

            _logger.LogInformation(
                "Quick action {ActionId} completed: {InputChars} → {OutputChars} chars in {Duration}ms",
                actionId, inputText.Length, response.Content.Length, stopwatch.ElapsedMilliseconds);

            // ─────────────────────────────────────────────────────────────
            // Publish Event
            // ─────────────────────────────────────────────────────────────

            // LOGIC: Publish MediatR notification for telemetry and analytics.
            var executedEvent = new QuickActionExecutedEvent(
                ActionId: actionId,
                ActionName: action.Name,
                InputCharacterCount: inputText.Length,
                OutputCharacterCount: response.Content.Length,
                Duration: stopwatch.Elapsed,
                Success: true,
                AgentId: agent.AgentId);

            await _mediator.Publish(executedEvent, ct);

            // LOGIC: Raise the CLR event for local subscribers (e.g., ViewModel).
            ActionExecuted?.Invoke(this, new QuickActionExecutedEventArgs
            {
                Action = action,
                Result = result
            });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Quick action {ActionId} failed", actionId);

            return new QuickActionResult(
                Success: false,
                Text: string.Empty,
                ErrorMessage: ex.Message,
                Duration: stopwatch.Elapsed);
        }
    }

    /// <inheritdoc/>
    public void RegisterAction(QuickAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        // LOGIC: Prevent duplicate action IDs to maintain registry integrity.
        if (_actions.ContainsKey(action.ActionId))
        {
            throw new InvalidOperationException(
                $"Action with ID '{action.ActionId}' already exists.");
        }

        _actions[action.ActionId] = action;

        _logger.LogInformation(
            "Registered custom action: {ActionId} ({ActionName})",
            action.ActionId, action.Name);
    }

    /// <inheritdoc/>
    public bool UnregisterAction(string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);

        if (_actions.Remove(actionId))
        {
            _logger.LogInformation("Unregistered action: {ActionId}", actionId);
            return true;
        }

        _logger.LogDebug(
            "Action not found for unregistration: {ActionId}", actionId);
        return false;
    }
}
