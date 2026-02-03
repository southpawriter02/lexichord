// =============================================================================
// File: ICitationService.cs
// Project: Lexichord.Abstractions
// Description: Interface defining the contract for creating, formatting, and
//              validating source citations from search results.
// =============================================================================
// LOGIC: Defines the service contract for the Citation Engine (v0.5.2a).
//   - CreateCitation: Builds a Citation record from a SearchHit, calculating
//     line numbers from character offsets by reading the source file.
//   - CreateCitations: Batch creation for multiple search hits.
//   - FormatCitation: Formats a Citation in one of three styles (Inline,
//     Footnote, Markdown). License gating occurs at the formatting layer.
//   - ValidateCitationAsync: Checks whether the source file has changed since
//     indexing by comparing file modification timestamps.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for creating, formatting, and validating source citations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ICitationService"/> is the primary entry point for the Citation Engine.
/// It transforms <see cref="SearchHit"/> instances into <see cref="Citation"/> records
/// with complete provenance information (document path, heading, line number).
/// </para>
/// <para>
/// <b>License Gating:</b> Citation creation is always performed regardless of license tier.
/// The license check occurs at the formatting layer:
/// <list type="bullet">
///   <item><description>Core users: <see cref="FormatCitation"/> returns the document path only.</description></item>
///   <item><description>Writer Pro+: Full formatted citation in the requested style.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent access.
/// The service is registered as a singleton in the DI container.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2a as part of the Citation Engine.
/// </para>
/// </remarks>
public interface ICitationService
{
    /// <summary>
    /// Creates a citation from a search result hit.
    /// </summary>
    /// <param name="hit">
    /// The search hit containing chunk and document data.
    /// Must not be null.
    /// </param>
    /// <returns>
    /// A <see cref="Citation"/> with complete provenance information.
    /// The <see cref="Citation.LineNumber"/> may be null if the source file
    /// is not accessible or the offset exceeds the file length.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="hit"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Extracts document and chunk data from the SearchHit, calculates the
    /// line number from the chunk's character offset by reading the source file, and
    /// publishes a <c>CitationCreatedEvent</c> via MediatR.
    /// <para>
    /// The line number calculation reads the source file content and counts
    /// newline characters up to <see cref="TextChunk.StartOffset"/>. If the file
    /// is inaccessible or the offset is out of bounds, <see cref="Citation.LineNumber"/>
    /// is set to null (graceful degradation, no exception thrown).
    /// </para>
    /// </remarks>
    Citation CreateCitation(SearchHit hit);

    /// <summary>
    /// Creates citations for multiple search hits.
    /// </summary>
    /// <param name="hits">
    /// The search hits to create citations for. Must not be null.
    /// Individual null entries are skipped with a warning log.
    /// </param>
    /// <returns>
    /// Citations in the same order as the input hits, excluding any
    /// null entries that were skipped.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="hits"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Iterates over the input hits and delegates to <see cref="CreateCitation"/>
    /// for each non-null entry. A <c>CitationCreatedEvent</c> is published for each
    /// successfully created citation.
    /// </remarks>
    IReadOnlyList<Citation> CreateCitations(IEnumerable<SearchHit> hits);

    /// <summary>
    /// Formats a citation according to the specified style.
    /// </summary>
    /// <param name="citation">
    /// The citation to format. Must not be null.
    /// </param>
    /// <param name="style">
    /// The desired citation style (Inline, Footnote, or Markdown).
    /// </param>
    /// <returns>
    /// The formatted citation string appropriate for the requested style.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="citation"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="style"/> is not a defined <see cref="CitationStyle"/> value.
    /// </exception>
    /// <remarks>
    /// LOGIC: Delegates to the appropriate format method based on the style:
    /// <list type="bullet">
    ///   <item><description><see cref="CitationStyle.Inline"/>: <c>[filename.md, §Heading]</c></description></item>
    ///   <item><description><see cref="CitationStyle.Footnote"/>: <c>[^XXXXXXXX]: /path:line</c></description></item>
    ///   <item><description><see cref="CitationStyle.Markdown"/>: <c>[Title](file:///path#Lline)</c></description></item>
    /// </list>
    /// <para>
    /// License gating is applied in the implementation: Core users receive the
    /// document path only, regardless of the requested style.
    /// </para>
    /// </remarks>
    string FormatCitation(Citation citation, CitationStyle style);

    /// <summary>
    /// Validates that the citation's source has not changed since indexing.
    /// </summary>
    /// <param name="citation">
    /// The citation to validate. Must not be null.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// <c>true</c> if the source file exists and has not been modified since
    /// <see cref="Citation.IndexedAt"/>; <c>false</c> if the file is missing
    /// or has been modified (stale).
    /// </returns>
    /// <remarks>
    /// LOGIC: Performs a two-step validation:
    /// <list type="number">
    ///   <item><description>Checks if the file exists at <see cref="Citation.DocumentPath"/>.</description></item>
    ///   <item><description>Compares the file's <see cref="FileInfo.LastWriteTimeUtc"/> against <see cref="Citation.IndexedAt"/>.</description></item>
    /// </list>
    /// <para>
    /// This is a basic validation suitable for v0.5.2a. The full validation
    /// with hash comparison and batch support is implemented by <c>ICitationValidator</c>
    /// in v0.5.2c.
    /// </para>
    /// </remarks>
    Task<bool> ValidateCitationAsync(Citation citation, CancellationToken ct = default);
}
