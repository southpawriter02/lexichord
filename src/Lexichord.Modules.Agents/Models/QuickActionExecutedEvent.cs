// -----------------------------------------------------------------------
// <copyright file="QuickActionExecutedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Services;
using MediatR;

namespace Lexichord.Modules.Agents.Models;

/// <summary>
/// MediatR notification published when a quick action is executed.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="IQuickActionsService"/> after each action execution
/// (both successful and failed). Used for telemetry, analytics, and usage
/// tracking by downstream handlers.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
/// <param name="ActionId">The unique identifier of the executed action.</param>
/// <param name="ActionName">The display name of the executed action.</param>
/// <param name="InputCharacterCount">The number of characters in the input text.</param>
/// <param name="OutputCharacterCount">The number of characters in the output text.</param>
/// <param name="Duration">The total execution duration including agent invocation.</param>
/// <param name="Success">Whether the action completed successfully.</param>
/// <param name="AgentId">The ID of the agent that processed the request, or null on failure.</param>
public record QuickActionExecutedEvent(
    string ActionId,
    string ActionName,
    int InputCharacterCount,
    int OutputCharacterCount,
    TimeSpan Duration,
    bool Success,
    string? AgentId) : INotification;

/// <summary>
/// Event arguments for the <see cref="IQuickActionsService.ActionExecuted"/> event.
/// </summary>
/// <remarks>
/// <para>
/// Provides the executed action and its result to event subscribers.
/// Used by the <see cref="Lexichord.Modules.Agents.ViewModels.QuickActionsPanelViewModel"/>
/// and other UI components to react to action completion.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
public class QuickActionExecutedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the quick action that was executed.
    /// </summary>
    public required QuickAction Action { get; init; }

    /// <summary>
    /// Gets the result of the action execution.
    /// </summary>
    public required QuickActionResult Result { get; init; }
}
