// =============================================================================
// File: SearchEvents.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR notification events for the semantic search pipeline.
// =============================================================================
// LOGIC: Defines domain events published during semantic search operations.
//   - SemanticSearchExecutedEvent: Published on successful search completion
//     for telemetry and analytics. Includes UsedCachedEmbedding flag (v0.4.5d).
//   - SearchDeniedEvent: Published when a search is blocked due to
//     insufficient license tier for audit and upgrade prompt triggers.
//   - SearchModeChangedEvent: Published when the user changes the search mode
//     in the Reference Panel (v0.5.1d).
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Published when a semantic search query is successfully executed.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="PgVectorSearchService"/> after a search
/// completes successfully. It captures telemetry data including the query text,
/// result count, and total duration for analytics and monitoring.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Telemetry handlers for search usage analytics.</description></item>
///   <item><description>UI handlers for search history tracking (v0.4.6).</description></item>
///   <item><description>Performance monitoring for search latency alerts.</description></item>
/// </list>
/// <para>
/// <b>MediatR Pattern:</b> This is an <see cref="INotification"/> (fire-and-forget).
/// Handlers do not return values and failures do not affect the search result.
/// </para>
/// <para>
/// <b>Introduced:</b> v0.4.5b. <b>Enhanced:</b> v0.4.5d (UsedCachedEmbedding).
/// </para>
/// </remarks>
public record SemanticSearchExecutedEvent : INotification
{
    /// <summary>
    /// The original query text submitted by the user.
    /// </summary>
    /// <value>The raw query string before preprocessing.</value>
    public required string Query { get; init; }

    /// <summary>
    /// The number of search hits returned.
    /// </summary>
    /// <value>Count of <see cref="SearchHit"/> items in the result.</value>
    public required int ResultCount { get; init; }

    /// <summary>
    /// The total duration of the search operation.
    /// </summary>
    /// <value>Elapsed time from query submission to result assembly.</value>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// The UTC timestamp when the search was executed.
    /// </summary>
    /// <value>Defaults to <see cref="DateTimeOffset.UtcNow"/> at creation time.</value>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the query embedding was served from cache rather than generated via the API.
    /// </summary>
    /// <value>
    /// <c>true</c> if the embedding was retrieved from <see cref="IQueryPreprocessor"/> cache;
    /// <c>false</c> if a new embedding was generated via <see cref="IEmbeddingService"/>.
    /// Defaults to <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Tracks cache efficiency for telemetry. A high cache hit rate indicates
    /// users are repeating queries and the 5-minute sliding cache is effective.
    /// Introduced in v0.4.5d.
    /// </remarks>
    public bool UsedCachedEmbedding { get; init; }
}

/// <summary>
/// Published when a semantic search is denied due to insufficient license tier.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="SearchLicenseGuard"/> when a user attempts
/// to execute a semantic search without the required <see cref="LicenseTier.WriterPro"/>
/// license tier. It enables:
/// </para>
/// <list type="bullet">
///   <item><description>UI handlers to display upgrade prompts.</description></item>
///   <item><description>Telemetry handlers to track upgrade opportunities.</description></item>
///   <item><description>Audit logging of access control decisions.</description></item>
/// </list>
/// <para>
/// <b>MediatR Pattern:</b> This is an <see cref="INotification"/> (fire-and-forget).
/// Handlers do not return values.
/// </para>
/// <para>
/// <b>Introduced:</b> v0.4.5b.
/// </para>
/// </remarks>
public record SearchDeniedEvent : INotification
{
    /// <summary>
    /// The user's current license tier at the time of denial.
    /// </summary>
    /// <value>The <see cref="LicenseTier"/> that was insufficient.</value>
    public required LicenseTier CurrentTier { get; init; }

    /// <summary>
    /// The minimum license tier required for semantic search.
    /// </summary>
    /// <value>Always <see cref="LicenseTier.WriterPro"/> for semantic search.</value>
    public required LicenseTier RequiredTier { get; init; }

    /// <summary>
    /// The name of the feature that was denied.
    /// </summary>
    /// <value>The feature identifier (e.g., "Semantic Search").</value>
    public required string FeatureName { get; init; }

    /// <summary>
    /// The UTC timestamp when the denial occurred.
    /// </summary>
    /// <value>Defaults to <see cref="DateTimeOffset.UtcNow"/> at creation time.</value>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Published when the user changes the search mode in the Reference Panel.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="ViewModels.ReferenceViewModel"/> when the
/// user selects a different search mode (Semantic, Keyword, or Hybrid). It enables:
/// </para>
/// <list type="bullet">
///   <item><description>Telemetry handlers to track search mode usage patterns.</description></item>
///   <item><description>Analytics handlers to understand feature adoption.</description></item>
///   <item><description>Audit logging of search mode changes.</description></item>
/// </list>
/// <para>
/// <b>MediatR Pattern:</b> This is an <see cref="INotification"/> (fire-and-forget).
/// Handlers do not return values and failures do not affect the mode change.
/// </para>
/// <para>
/// <b>Introduced:</b> v0.5.1d.
/// </para>
/// </remarks>
public record SearchModeChangedEvent : INotification
{
    /// <summary>
    /// The search mode that was previously active.
    /// </summary>
    /// <value>The <see cref="SearchMode"/> before the change.</value>
    public required SearchMode PreviousMode { get; init; }

    /// <summary>
    /// The search mode that is now active.
    /// </summary>
    /// <value>The <see cref="SearchMode"/> after the change.</value>
    public required SearchMode NewMode { get; init; }

    /// <summary>
    /// The user's license tier at the time of the mode change.
    /// </summary>
    /// <value>The <see cref="LicenseTier"/> when the event was published.</value>
    public required LicenseTier LicenseTier { get; init; }

    /// <summary>
    /// The UTC timestamp when the mode change occurred.
    /// </summary>
    /// <value>Defaults to <see cref="DateTimeOffset.UtcNow"/> at creation time.</value>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
