// =============================================================================
// File: ICitationClipboardService.cs
// Project: Lexichord.Abstractions
// Description: Interface for clipboard operations on citations.
// =============================================================================
// LOGIC: Provides clipboard operations for citations from search results.
//   - CopyCitationAsync: Copies a formatted citation to the clipboard.
//   - CopyChunkTextAsync: Copies raw chunk content to the clipboard.
//   - CopyDocumentPathAsync: Copies document path (string or file:// URI).
//   - License gating occurs at the service layer; Core users get path only.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.2a: Citation, ICitationService
//   - v0.5.2b: CitationStyle, CitationFormatterRegistry
//   - v0.5.2d: CitationCopiedEvent (telemetry)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for copying citation data to the system clipboard.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ICitationClipboardService"/> provides operations for copying
/// various representations of a <see cref="Citation"/> to the clipboard. It
/// integrates with the formatting layer (<see cref="ICitationService.FormatCitation"/>)
/// and respects license gating.
/// </para>
/// <para>
/// <b>License Gating:</b> Copy operations respect the user's license tier:
/// <list type="bullet">
///   <item><description>Core users: <see cref="CopyCitationAsync"/> copies the document path only.</description></item>
///   <item><description>Writer Pro+: Full formatted citation in the requested or preferred style.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Telemetry:</b> All copy operations publish a <c>CitationCopiedEvent</c>
/// notification via MediatR for analytics and audit logging.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. Clipboard access
/// is inherently thread-sensitive in UI frameworks; implementations should
/// marshal operations to the UI thread as needed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2d as part of the Citation Engine.
/// </para>
/// </remarks>
public interface ICitationClipboardService
{
    /// <summary>
    /// Copies a formatted citation to the clipboard.
    /// </summary>
    /// <param name="citation">
    /// The citation to copy. Must not be null.
    /// </param>
    /// <param name="style">
    /// The citation style to use. If null, uses the user's preferred style
    /// from <c>CitationFormatterRegistry.GetPreferredStyleAsync</c>.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// A task that completes when the citation has been copied to the clipboard.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="citation"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Retrieves the formatted citation via <see cref="ICitationService.FormatCitation"/>,
    /// copies it to the clipboard, and publishes a <c>CitationCopiedEvent</c>.
    /// <para>
    /// When <paramref name="style"/> is null, the user's preferred style is retrieved
    /// from the <c>CitationFormatterRegistry</c>. This enables the "Copy Citation"
    /// context menu action to use the user's default style.
    /// </para>
    /// </remarks>
    Task CopyCitationAsync(Citation citation, CitationStyle? style = null, CancellationToken ct = default);

    /// <summary>
    /// Copies the raw chunk text to the clipboard.
    /// </summary>
    /// <param name="citation">
    /// The citation identifying the chunk to copy. Must not be null.
    /// </param>
    /// <param name="chunkText">
    /// The raw text content of the chunk to copy. Must not be null or empty.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// A task that completes when the text has been copied to the clipboard.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="citation"/> or <paramref name="chunkText"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Copies the raw source text from the chunk without any formatting.
    /// This is useful when users want to quote the original content rather than
    /// cite it. Publishes a <c>CitationCopiedEvent</c> with format <c>ChunkText</c>.
    /// <para>
    /// Unlike <see cref="CopyCitationAsync"/>, this operation is not license-gated.
    /// All users can copy the raw chunk text.
    /// </para>
    /// </remarks>
    Task CopyChunkTextAsync(Citation citation, string chunkText, CancellationToken ct = default);

    /// <summary>
    /// Copies the document path to the clipboard.
    /// </summary>
    /// <param name="citation">
    /// The citation containing the document path. Must not be null.
    /// </param>
    /// <param name="asFileUri">
    /// If true, copies as a <c>file://</c> URI (e.g., <c>file:///path/to/doc.md</c>).
    /// If false, copies the raw path string.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// A task that completes when the path has been copied to the clipboard.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="citation"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Copies either the raw <see cref="Citation.DocumentPath"/> or a
    /// <c>file://</c> URI representation. The URI form is useful for pasting
    /// into applications that recognize file URIs as links.
    /// <para>
    /// This operation is not license-gated. All users can copy document paths.
    /// </para>
    /// </remarks>
    Task CopyDocumentPathAsync(Citation citation, bool asFileUri = false, CancellationToken ct = default);
}
