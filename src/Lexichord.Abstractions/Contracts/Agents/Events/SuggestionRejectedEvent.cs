// -----------------------------------------------------------------------
// <copyright file="SuggestionRejectedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Contracts.Agents.Events;

/// <summary>
/// Published when the user rejects a fix suggestion in the Tuning Panel.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is published by the <c>TuningPanelViewModel</c>
/// when a suggestion is rejected (dismissed without applying). Subscribers
/// can use this event for:
/// </para>
/// <list type="bullet">
///   <item><description>Analytics tracking of rejection rates per rule category</description></item>
///   <item><description>Learning loop feedback recording (v0.7.5d)</description></item>
///   <item><description>Identifying rules that produce frequently-rejected suggestions</description></item>
///   <item><description>Logging for audit trails</description></item>
/// </list>
/// <para>
/// <b>Note:</b> Skipped suggestions (via the Skip action) do <em>not</em> publish
/// this event, as skipping indicates deferral rather than rejection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5c as part of the Accept/Reject UI feature.
/// </para>
/// </remarks>
/// <param name="Deviation">The style deviation whose suggestion was rejected.</param>
/// <param name="Suggestion">The fix suggestion that was rejected.</param>
/// <param name="Timestamp">When the rejection occurred.</param>
/// <example>
/// <code>
/// // Publishing after rejecting a suggestion
/// await _mediator.Publish(SuggestionRejectedEvent.Create(deviation, suggestion));
///
/// // Handling rejection for analytics
/// public class TuningAnalyticsHandler : INotificationHandler&lt;SuggestionRejectedEvent&gt;
/// {
///     public Task Handle(SuggestionRejectedEvent notification, CancellationToken ct)
///     {
///         _analytics.TrackSuggestionRejected(
///             notification.Deviation.RuleId,
///             notification.Suggestion.Confidence);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SuggestionAcceptedEvent"/>
/// <seealso cref="StyleDeviation"/>
/// <seealso cref="FixSuggestion"/>
public record SuggestionRejectedEvent(
    StyleDeviation Deviation,
    FixSuggestion Suggestion,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new rejection event with the current timestamp.
    /// </summary>
    /// <param name="deviation">The style deviation whose suggestion was rejected.</param>
    /// <param name="suggestion">The fix suggestion that was rejected.</param>
    /// <returns>A new <see cref="SuggestionRejectedEvent"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory method that automatically sets
    /// <see cref="Timestamp"/> to <see cref="DateTime.UtcNow"/>.
    /// </remarks>
    public static SuggestionRejectedEvent Create(
        StyleDeviation deviation,
        FixSuggestion suggestion) =>
        new(deviation, suggestion, DateTime.UtcNow);
}
