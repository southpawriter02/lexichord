// =============================================================================
// File: PreviewOptions.cs
// Project: Lexichord.Modules.RAG
// Description: Configuration options for preview content building.
// =============================================================================
// LOGIC: Immutable record containing preview configuration.
//   - LinesBefore: Number of context lines before matched content (default: 5).
//   - LinesAfter: Number of context lines after matched content (default: 5).
//   - IncludeBreadcrumb: Whether to include heading hierarchy (default: true).
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.7c: Introduced as part of Preview Pane feature.
// =============================================================================

namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Configuration options for preview content building.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PreviewOptions"/> controls how <see cref="IPreviewContentBuilder"/>
/// fetches and formats preview content for a search hit. All properties have
/// sensible defaults suitable for typical use cases.
/// </para>
/// <para>
/// <b>Default Values:</b>
/// <list type="bullet">
///   <item><description><see cref="LinesBefore"/>: 5</description></item>
///   <item><description><see cref="LinesAfter"/>: 5</description></item>
///   <item><description><see cref="IncludeBreadcrumb"/>: <c>true</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7c as part of the Preview Pane feature.
/// </para>
/// </remarks>
public record PreviewOptions
{
    /// <summary>
    /// Number of lines to show before the matched content.
    /// </summary>
    /// <value>
    /// Default: 5. Set to 0 to show no preceding context.
    /// </value>
    public int LinesBefore { get; init; } = 5;

    /// <summary>
    /// Number of lines to show after the matched content.
    /// </summary>
    /// <value>
    /// Default: 5. Set to 0 to show no following context.
    /// </value>
    public int LinesAfter { get; init; } = 5;

    /// <summary>
    /// Whether to include the heading breadcrumb in the preview.
    /// </summary>
    /// <value>
    /// Default: <c>true</c>. Set to <c>false</c> to omit breadcrumb.
    /// </value>
    public bool IncludeBreadcrumb { get; init; } = true;

    /// <summary>
    /// Default options instance with standard values.
    /// </summary>
    public static PreviewOptions Default { get; } = new();
}
