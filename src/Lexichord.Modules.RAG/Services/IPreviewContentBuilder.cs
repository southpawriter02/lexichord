// =============================================================================
// File: IPreviewContentBuilder.cs
// Project: Lexichord.Modules.RAG
// Description: Interface for building preview content from search hits.
// =============================================================================
// LOGIC: Defines contract for preview content building.
//   - BuildAsync: Builds preview content for a search hit using context expansion.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchHit (search result).
//   - v0.5.7c: PreviewContent, PreviewOptions (data contracts).
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Data;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Builds preview content from a search hit using context expansion.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IPreviewContentBuilder"/> coordinates between context expansion
/// and snippet services to produce complete preview content for display
/// in the split-view preview pane.
/// </para>
/// <para>
/// <b>Responsibilities:</b>
/// <list type="bullet">
///   <item><description>Fetch expanded context via <c>IContextExpansionService</c></description></item>
///   <item><description>Extract highlight spans via <c>ISnippetService</c></description></item>
///   <item><description>Format heading breadcrumb for display</description></item>
///   <item><description>Assemble <see cref="PreviewContent"/> record</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7c as part of the Preview Pane feature.
/// </para>
/// </remarks>
public interface IPreviewContentBuilder
{
    /// <summary>
    /// Builds preview content for a search hit.
    /// </summary>
    /// <param name="hit">The search hit to build preview for. Must not be <c>null</c>.</param>
    /// <param name="options">Configuration for context expansion. Uses defaults if <c>null</c>.</param>
    /// <param name="ct">Cancellation token for async cancellation.</param>
    /// <returns>Complete preview content with context and highlights.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hit"/> is <c>null</c>.</exception>
    /// <remarks>
    /// <para>
    /// LOGIC: The build process:
    /// <list type="number">
    ///   <item><description>Convert <see cref="SearchHit.Chunk"/> to RAG <c>Chunk</c></description></item>
    ///   <item><description>Call <c>IContextExpansionService.ExpandAsync</c></description></item>
    ///   <item><description>Extract content from expanded chunks</description></item>
    ///   <item><description>Build highlight spans from snippet analysis</description></item>
    ///   <item><description>Format breadcrumb from heading hierarchy</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<PreviewContent> BuildAsync(
        SearchHit hit,
        PreviewOptions? options = null,
        CancellationToken ct = default);
}
