// -----------------------------------------------------------------------
// <copyright file="ContextAssembledEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Context;
using MediatR;

namespace Lexichord.Modules.Agents.Context;

/// <summary>
/// MediatR notification published after context assembly completes.
/// Enables the Context Preview panel and telemetry systems to react to
/// assembly results without tight coupling to the orchestrator.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="ContextOrchestrator"/> at the end of every successful
/// <see cref="IContextOrchestrator.AssembleAsync"/> invocation. Handlers may include:
/// </para>
/// <list type="bullet">
///   <item><description>Context Preview panel ViewModel (v0.7.2d) — updates the displayed fragments</description></item>
///   <item><description>Telemetry handlers — tracks assembly performance and token usage</description></item>
///   <item><description>Usage tracking — aggregates context gathering metrics</description></item>
/// </list>
/// <para>
/// <strong>Event Lifecycle:</strong>
/// This event is published <em>after</em> the assembled context is returned to the
/// calling agent. The event is fire-and-forget; handler failures do not affect the
/// assembly result.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2c as part of the Context Orchestrator.
/// </para>
/// </remarks>
/// <param name="AgentId">
/// The ID of the agent that requested context assembly.
/// Used to associate the event with the correct agent UI panel.
/// </param>
/// <param name="Fragments">
/// The final list of context fragments included in the assembly.
/// Already sorted by priority, deduplicated, and trimmed to budget.
/// </param>
/// <param name="TotalTokens">
/// Total token count across all included fragments.
/// Useful for budget utilization metrics.
/// </param>
/// <param name="Duration">
/// Wall-clock time for the entire assembly operation.
/// Includes strategy execution, deduplication, sorting, and trimming.
/// </param>
/// <example>
/// <code>
/// // Publishing (inside ContextOrchestrator)
/// await _mediator.Publish(new ContextAssembledEvent(
///     AgentId: "editor",
///     Fragments: result.Fragments,
///     TotalTokens: result.TotalTokens,
///     Duration: result.AssemblyDuration), ct);
///
/// // Handling (in ContextPreviewViewModel)
/// public class ContextPreviewHandler : INotificationHandler&lt;ContextAssembledEvent&gt;
/// {
///     public Task Handle(ContextAssembledEvent notification, CancellationToken ct)
///     {
///         UpdatePreview(notification.Fragments, notification.TotalTokens);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IContextOrchestrator"/>
/// <seealso cref="AssembledContext"/>
public sealed record ContextAssembledEvent(
    string AgentId,
    IReadOnlyList<ContextFragment> Fragments,
    int TotalTokens,
    TimeSpan Duration) : INotification;
