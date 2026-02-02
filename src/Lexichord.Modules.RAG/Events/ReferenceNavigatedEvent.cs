// =============================================================================
// File: ReferenceNavigatedEvent.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR event published when a user navigates to a search result.
// =============================================================================
// LOGIC: Published by ReferenceNavigationService after successful navigation
//   from a search result to its source document. Used for telemetry and
//   analytics tracking of search-to-source navigation patterns.
// =============================================================================
// VERSION: v0.4.6c (Source Navigation)
// =============================================================================

using MediatR;

namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// Event published when user navigates from a search result to its source.
/// Used for telemetry and analytics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReferenceNavigatedEvent"/> is published by
/// <see cref="Services.ReferenceNavigationService"/> after a successful
/// navigation operation. Consumers can use this event for:
/// <list type="bullet">
///   <item><description>Tracking navigation frequency and patterns.</description></item>
///   <item><description>Measuring search result quality via click-through rates.</description></item>
///   <item><description>Building navigation history for future features.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6c as part of Source Navigation.
/// </para>
/// </remarks>
public record ReferenceNavigatedEvent : INotification
{
    /// <summary>
    /// Path to the navigated document.
    /// </summary>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// Character offset in the document.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Length of the highlighted text span.
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// Relevance score of the search hit.
    /// </summary>
    public float Score { get; init; }

    /// <summary>
    /// Timestamp of the navigation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
