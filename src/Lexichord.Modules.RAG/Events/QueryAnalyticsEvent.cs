// =============================================================================
// File: QueryAnalyticsEvent.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR notification for anonymized query analytics (v0.5.4d).
// =============================================================================
// LOGIC: Published when a query is executed to enable opt-in telemetry.
//   Query text is hashed for privacy. Handlers can aggregate statistics
//   without exposing actual search content.
// =============================================================================
// VERSION: v0.5.4d (Query History & Analytics)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// Published when a query is executed for opt-in telemetry.
/// </summary>
/// <param name="QueryHash">SHA256 hash of query (anonymized).</param>
/// <param name="Intent">Detected intent category.</param>
/// <param name="ResultCount">Number of results returned.</param>
/// <param name="DurationMs">Execution duration in milliseconds.</param>
/// <param name="Timestamp">When the query was executed (UTC).</param>
/// <remarks>
/// <para>
/// <see cref="QueryAnalyticsEvent"/> enables anonymous usage tracking:
/// <list type="bullet">
///   <item><description>Query volume trends</description></item>
///   <item><description>Intent distribution</description></item>
///   <item><description>Search performance metrics</description></item>
///   <item><description>Zero-result rate monitoring</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Privacy:</b> The query text is never transmitted. Only a SHA256 hash
/// is included, which cannot be reversed to recover the original query.
/// </para>
/// <para>
/// <b>Opt-in:</b> Telemetry handlers only process this event when the user
/// has explicitly enabled telemetry in settings.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4d as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Telemetry handler (only processes if telemetry enabled)
/// public class QueryTelemetryHandler : INotificationHandler&lt;QueryAnalyticsEvent&gt;
/// {
///     public Task Handle(QueryAnalyticsEvent notification, CancellationToken ct)
///     {
///         if (!_settings.TelemetryEnabled) return Task.CompletedTask;
///
///         // Aggregate statistics
///         _metrics.RecordQueryMetric(
///             notification.Intent,
///             notification.ResultCount > 0,
///             notification.DurationMs);
///
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record QueryAnalyticsEvent(
    string QueryHash,
    QueryIntent Intent,
    int ResultCount,
    long DurationMs,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Gets whether this query returned results.
    /// </summary>
    public bool HasResults => ResultCount > 0;

    /// <summary>
    /// Gets whether this was a zero-result query.
    /// </summary>
    public bool IsZeroResult => ResultCount == 0;

    /// <summary>
    /// Gets a category for performance bucketing.
    /// </summary>
    public string PerformanceCategory => DurationMs switch
    {
        < 50 => "Fast",
        < 200 => "Normal",
        < 1000 => "Slow",
        _ => "VerySloww"
    };
}
