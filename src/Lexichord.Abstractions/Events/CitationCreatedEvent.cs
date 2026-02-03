// =============================================================================
// File: CitationCreatedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a new citation is created.
// =============================================================================
// LOGIC: Published by CitationService.CreateCitation() after a Citation is
//   successfully built from a SearchHit. Enables downstream consumers to:
//   - Track citation creation for analytics (v0.5.4d)
//   - Update UI with citation metadata (v0.5.2d context menu)
//   - Log citation creation for audit trails (Enterprise tier)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a new citation is created from a search result.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="ICitationService.CreateCitation"/>
/// after successfully constructing a <see cref="Citation"/> from a
/// <see cref="SearchHit"/>. It carries the created citation and a UTC
/// timestamp for when the creation occurred.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Citation analytics tracking (v0.5.4d, future)</description></item>
///   <item><description>UI notification for citation clipboard actions (v0.5.2d)</description></item>
///   <item><description>Enterprise audit logging (future)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2a as part of the Citation Engine.
/// </para>
/// </remarks>
/// <param name="Citation">
/// The citation that was created. Contains complete provenance information
/// including document path, heading, line number, and indexing timestamp.
/// </param>
/// <param name="Timestamp">
/// UTC timestamp of when the citation was created. This reflects the moment
/// <see cref="ICitationService.CreateCitation"/> completed, not when the
/// event was processed by handlers.
/// </param>
public record CitationCreatedEvent(
    Citation Citation,
    DateTime Timestamp) : INotification;
