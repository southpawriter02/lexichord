// -----------------------------------------------------------------------
// <copyright file="IQuickActionsService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Models;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Provides quick action execution and management for the floating toolbar.
/// </summary>
/// <remarks>
/// <para>
/// Quick actions are pre-defined or custom AI operations that can be
/// invoked with a single click or keyboard shortcut. Each action is
/// associated with a prompt template and target agent.
/// </para>
/// <para>
/// Actions are context-aware and may be filtered based on the current
/// document content type (e.g., code-specific actions only appear when
/// the cursor is in a code block).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get available actions for current context
/// var actions = await _quickActions.GetAvailableActionsAsync();
///
/// // Execute an action on selection
/// var result = await _quickActions.ExecuteAsync("improve", selection);
/// if (result.Success)
/// {
///     await _insertionService.ShowPreviewAsync(result.Text, location);
/// }
/// </code>
/// </example>
public interface IQuickActionsService
{
    /// <summary>
    /// Gets all registered quick actions, ordered by <see cref="QuickAction.Order"/>.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns both built-in and custom-registered actions,
    /// regardless of current context or license tier. Use
    /// <see cref="GetAvailableActionsAsync"/> for context-filtered actions.
    /// </remarks>
    IReadOnlyList<QuickAction> AllActions { get; }

    /// <summary>
    /// Gets actions available for the current editor context.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A filtered list of available actions ordered by <see cref="QuickAction.Order"/>.</returns>
    /// <remarks>
    /// LOGIC: Actions are filtered based on:
    /// <list type="bullet">
    ///   <item><description>Current content type (code, prose, table, etc.) via <see cref="IDocumentContextAnalyzer"/></description></item>
    ///   <item><description>Selection state (some actions require selection)</description></item>
    ///   <item><description>License tier (some actions are Teams-only)</description></item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<QuickAction>> GetAvailableActionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Executes a quick action on the specified text.
    /// </summary>
    /// <param name="actionId">The unique identifier of the action to execute.</param>
    /// <param name="inputText">The text to process (typically the editor selection).</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The result of the action execution.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="actionId"/> is not a registered action.
    /// </exception>
    /// <remarks>
    /// LOGIC: Execution flow:
    /// <list type="number">
    ///   <item><description>Look up the action by ID</description></item>
    ///   <item><description>Resolve the prompt template (repository first, then built-in fallback)</description></item>
    ///   <item><description>Render the prompt with the input text</description></item>
    ///   <item><description>Invoke the agent (specific or default)</description></item>
    ///   <item><description>Publish <see cref="QuickActionExecutedEvent"/> via MediatR</description></item>
    ///   <item><description>Raise <see cref="ActionExecuted"/> event</description></item>
    /// </list>
    /// </remarks>
    Task<QuickActionResult> ExecuteAsync(
        string actionId,
        string inputText,
        CancellationToken ct = default);

    /// <summary>
    /// Registers a custom quick action.
    /// </summary>
    /// <param name="action">The action to register.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an action with the same <see cref="QuickAction.ActionId"/> already exists.
    /// </exception>
    /// <remarks>
    /// LOGIC: Custom actions are persisted in memory for the session lifetime.
    /// Custom action registration requires Teams tier or higher.
    /// </remarks>
    void RegisterAction(QuickAction action);

    /// <summary>
    /// Unregisters a custom quick action.
    /// </summary>
    /// <param name="actionId">The action ID to unregister.</param>
    /// <returns><c>true</c> if the action was removed; <c>false</c> if not found.</returns>
    bool UnregisterAction(string actionId);

    /// <summary>
    /// Gets whether the quick actions panel should be visible.
    /// </summary>
    /// <remarks>
    /// LOGIC: The panel is visible when there is a selection or when
    /// explicitly triggered via keyboard shortcut (<c>Ctrl+.</c>).
    /// </remarks>
    bool ShouldShowPanel { get; }

    /// <summary>
    /// Raised when an action is executed (successfully or not).
    /// </summary>
    event EventHandler<QuickActionExecutedEventArgs>? ActionExecuted;
}

/// <summary>
/// Result of a quick action execution.
/// </summary>
/// <remarks>
/// <para>
/// Contains the outcome of an <see cref="IQuickActionsService.ExecuteAsync"/> call,
/// including the generated text on success or an error message on failure.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
/// <param name="Success">Whether the action completed successfully.</param>
/// <param name="Text">The generated text on success; empty string on failure.</param>
/// <param name="ErrorMessage">An error message on failure; null on success.</param>
/// <param name="Duration">The total execution duration including agent invocation.</param>
public record QuickActionResult(
    bool Success,
    string Text,
    string? ErrorMessage = null,
    TimeSpan Duration = default);
