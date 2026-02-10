// -----------------------------------------------------------------------
// <copyright file="AgentInvocationEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Events;

/// <summary>
/// MediatR notification published after each agent invocation.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by the UsageTracker after recording usage
/// metrics for an agent invocation. It enables:
/// </para>
/// <list type="bullet">
///   <item>Telemetry and analytics collection</item>
///   <item>Real-time usage dashboards</item>
///   <item>Integration with external monitoring systems</item>
/// </list>
/// <para>
/// Handlers should be lightweight and non-blocking to avoid impacting
/// response latency. Consider using background processing for expensive
/// operations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
/// <param name="AgentId">Identifier of the invoked agent.</param>
/// <param name="Model">LLM model used for the invocation.</param>
/// <param name="PromptTokens">Number of tokens in the prompt.</param>
/// <param name="CompletionTokens">Number of tokens in the completion.</param>
/// <param name="Duration">Total invocation duration.</param>
/// <param name="Streamed">Whether response was streamed.</param>
/// <example>
/// <code>
/// // Publishing an event
/// await _mediator.Publish(new AgentInvocationEvent(
///     AgentId: "co-pilot",
///     Model: "gpt-4",
///     PromptTokens: 1500,
///     CompletionTokens: 500,
///     Duration: TimeSpan.FromSeconds(2.5),
///     Streamed: true
/// ));
///
/// // Handling the event
/// public class TelemetryHandler : INotificationHandler&lt;AgentInvocationEvent&gt;
/// {
///     public Task Handle(AgentInvocationEvent notification, CancellationToken ct)
///     {
///         _telemetry.CaptureMessage($"Agent invocation: {notification.AgentId}");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record AgentInvocationEvent(
    string AgentId,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration,
    bool Streamed
) : INotification
{
    /// <summary>
    /// Total tokens consumed (prompt + completion).
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional conversation ID for correlation.
    /// </summary>
    public Guid? ConversationId { get; init; }
}
