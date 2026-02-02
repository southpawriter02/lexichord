// =============================================================================
// File: IReferenceNavigationService.cs
// Project: Lexichord.Abstractions
// Description: Contract for navigating from search results to source documents.
// =============================================================================
// LOGIC: Defines the navigation contract for the RAG module's search-to-source
//   functionality. Implementations coordinate with IEditorService and
//   IEditorNavigationService to open, scroll, and highlight documents.
// =============================================================================
// VERSION: v0.4.6c (Source Navigation)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for navigating from search results to source documents.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IReferenceNavigationService"/> bridges the RAG module's search
/// results with the editor's document navigation capabilities. It handles
/// opening closed documents, scrolling to match locations, and highlighting
/// matched text spans.
/// </para>
/// <para>
/// <b>Navigation Flow:</b>
/// <list type="number">
///   <item><description>Resolve document by file path via <c>IEditorService</c>.</description></item>
///   <item><description>Open document if not already open.</description></item>
///   <item><description>Delegate to <c>IEditorNavigationService</c> for scroll and highlight.</description></item>
///   <item><description>Publish <c>ReferenceNavigatedEvent</c> for telemetry.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6c as part of Source Navigation.
/// </para>
/// </remarks>
public interface IReferenceNavigationService
{
    /// <summary>
    /// Navigates to the source location of a search hit.
    /// Opens the document if not already open, scrolls to the match location,
    /// and highlights the matched text span.
    /// </summary>
    /// <param name="hit">The search hit containing location information.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if navigation succeeded, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Extracts <c>Document.FilePath</c>, <c>Chunk.StartOffset</c>, and
    /// <c>Chunk.EndOffset</c> from the <paramref name="hit"/> and delegates to
    /// <see cref="NavigateToOffsetAsync"/>.
    /// </para>
    /// <para>
    /// On success, publishes a <c>ReferenceNavigatedEvent</c> via <c>IMediator</c>
    /// for telemetry tracking.
    /// </para>
    /// </remarks>
    Task<bool> NavigateToHitAsync(SearchHit hit, CancellationToken ct = default);

    /// <summary>
    /// Navigates to a specific offset in a document.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="offset">Character offset from start of document.</param>
    /// <param name="length">Length of text to highlight (0 for cursor only).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if navigation succeeded, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Opens the document if not already open, activates the tab,
    /// scrolls to the specified offset, and applies a temporary highlight
    /// if <paramref name="length"/> is greater than 0.
    /// </para>
    /// </remarks>
    Task<bool> NavigateToOffsetAsync(
        string documentPath,
        int offset,
        int length = 0,
        CancellationToken ct = default);
}
