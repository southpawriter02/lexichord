// =============================================================================
// File: CitationCopiedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a citation is copied.
// =============================================================================
// LOGIC: Published by ICitationClipboardService after a copy operation succeeds.
//   Enables downstream consumers to:
//   - Track citation copy patterns for analytics (v0.5.4d)
//   - Show toast notifications confirming the copy (v0.5.2d)
//   - Log copy operations for Enterprise audit trails
// =============================================================================

using Lexichord.Abstractions.Contracts;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when citation data is copied to the clipboard.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="ICitationClipboardService"/> operations
/// after successfully copying data to the clipboard. It carries information about
/// what was copied, in what format, and when.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Toast notification handler to confirm copy (v0.5.2d)</description></item>
///   <item><description>Citation analytics tracking (v0.5.4d, future)</description></item>
///   <item><description>Enterprise audit logging (future)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2d as part of the Citation Engine.
/// </para>
/// </remarks>
/// <param name="Citation">
/// The citation that was copied. Contains the source document information
/// and chunk identifiers for traceability.
/// </param>
/// <param name="Format">
/// The format of the copied content (FormattedCitation, ChunkText, DocumentPath, FileUri).
/// </param>
/// <param name="Style">
/// The citation style used for formatting, if applicable. Null when
/// <paramref name="Format"/> is not <see cref="CitationCopyFormat.FormattedCitation"/>.
/// </param>
/// <param name="Timestamp">
/// UTC timestamp of when the copy operation completed.
/// </param>
public record CitationCopiedEvent(
    Citation Citation,
    CitationCopyFormat Format,
    CitationStyle? Style,
    DateTime Timestamp) : INotification;
