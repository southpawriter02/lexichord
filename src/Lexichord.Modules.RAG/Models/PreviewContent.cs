// =============================================================================
// File: PreviewContent.cs
// Project: Lexichord.Modules.RAG
// Description: Content to display in the preview pane with surrounding context.
// =============================================================================
// LOGIC: Immutable record containing preview display data.
//   - DocumentPath/Title: Source document identification.
//   - Breadcrumb: Heading hierarchy (e.g., "API › Auth › Tokens").
//   - PrecedingContext: Text before the matched content.
//   - MatchedContent: The actual search hit content.
//   - FollowingContext: Text after the matched content.
//   - LineNumber: Source line number for navigation.
//   - HighlightSpans: Character ranges to highlight within MatchedContent.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.6a: HighlightSpan (highlight data).
//   - v0.5.7c: Introduced as part of Preview Pane feature.
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Content to display in the preview pane, including surrounding context.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PreviewContent"/> is the output of <see cref="IPreviewContentBuilder.BuildAsync"/>,
/// providing all data needed to render the preview pane UI. Content is divided
/// into three sections for distinct visual treatment.
/// </para>
/// <para>
/// <b>Content Sections:</b>
/// <list type="number">
///   <item><description><see cref="PrecedingContext"/>: Lines before the match (dimmed)</description></item>
///   <item><description><see cref="MatchedContent"/>: The search hit content (highlighted)</description></item>
///   <item><description><see cref="FollowingContext"/>: Lines after the match (dimmed)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7c as part of the Preview Pane feature.
/// </para>
/// </remarks>
/// <param name="DocumentPath">Full path to the source document.</param>
/// <param name="DocumentTitle">Display title for the preview header.</param>
/// <param name="Breadcrumb">Heading hierarchy path (e.g., "API › Auth › Tokens"). Null if unavailable.</param>
/// <param name="PrecedingContext">Lines before the matched content.</param>
/// <param name="MatchedContent">The highlighted matched content.</param>
/// <param name="FollowingContext">Lines after the matched content.</param>
/// <param name="LineNumber">Line number in source document.</param>
/// <param name="HighlightSpans">Character ranges to highlight within MatchedContent.</param>
public record PreviewContent(
    string DocumentPath,
    string DocumentTitle,
    string? Breadcrumb,
    string PrecedingContext,
    string MatchedContent,
    string FollowingContext,
    int LineNumber,
    IReadOnlyList<HighlightSpan> HighlightSpans)
{
    /// <summary>
    /// Creates an empty preview for placeholder display.
    /// </summary>
    /// <remarks>
    /// LOGIC: Factory for early-return scenarios (no selection, loading states).
    /// </remarks>
    public static PreviewContent Empty => new(
        string.Empty, string.Empty, null,
        string.Empty, string.Empty, string.Empty,
        0, Array.Empty<HighlightSpan>());

    /// <summary>
    /// Gets whether this preview has actual content.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="MatchedContent"/> is non-empty; otherwise, <c>false</c>.
    /// </value>
    public bool HasContent => !string.IsNullOrWhiteSpace(MatchedContent);

    /// <summary>
    /// Gets whether preceding context is available.
    /// </summary>
    public bool HasPrecedingContext => !string.IsNullOrWhiteSpace(PrecedingContext);

    /// <summary>
    /// Gets whether following context is available.
    /// </summary>
    public bool HasFollowingContext => !string.IsNullOrWhiteSpace(FollowingContext);

    /// <summary>
    /// Gets whether a breadcrumb is available.
    /// </summary>
    public bool HasBreadcrumb => !string.IsNullOrWhiteSpace(Breadcrumb);
}
