// =============================================================================
// File: CitationOptions.cs
// Project: Lexichord.Abstractions
// Description: Options for controlling citation rendering behaviour.
// =============================================================================
// LOGIC: Configures the Entity Citation Renderer output:
//   Format — display layout (Compact/Detailed/TreeView/Inline),
//   MaxCitations — cap on displayed citations (default 10),
//   ShowValidationStatus — whether to show the validation status line,
//   ShowConfidence — whether to display confidence scores,
//   GroupByType — whether to group citations by entity type.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: CitationFormat (v0.6.6h)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Options for controlling citation rendering behaviour.
/// </summary>
/// <remarks>
/// <para>
/// Passed to <see cref="IEntityCitationRenderer.GenerateCitations"/> to
/// control output format, citation limits, and display preferences.
/// </para>
/// <para>
/// <b>Defaults:</b>
/// <list type="bullet">
///   <item><see cref="Format"/> = <see cref="CitationFormat.Compact"/>.</item>
///   <item><see cref="MaxCitations"/> = 10.</item>
///   <item><see cref="ShowValidationStatus"/> = <c>true</c>.</item>
///   <item><see cref="ShowConfidence"/> = <c>false</c>.</item>
///   <item><see cref="GroupByType"/> = <c>true</c>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public record CitationOptions
{
    /// <summary>
    /// Display format for citations.
    /// </summary>
    /// <value>
    /// The <see cref="CitationFormat"/> controlling the layout.
    /// Defaults to <see cref="CitationFormat.Compact"/>.
    /// </value>
    public CitationFormat Format { get; init; } = CitationFormat.Compact;

    /// <summary>
    /// Maximum number of citations to display.
    /// </summary>
    /// <value>
    /// The cap on rendered citations. Excess entities are omitted.
    /// Defaults to 10.
    /// </value>
    public int MaxCitations { get; init; } = 10;

    /// <summary>
    /// Whether to show the validation status line.
    /// </summary>
    /// <value>
    /// <c>true</c> to display the validation status text and icon;
    /// <c>false</c> to hide it. Defaults to <c>true</c>.
    /// </value>
    public bool ShowValidationStatus { get; init; } = true;

    /// <summary>
    /// Whether to show confidence scores alongside citations.
    /// </summary>
    /// <value>
    /// <c>true</c> to display the confidence score for each citation;
    /// <c>false</c> to hide it. Defaults to <c>false</c>.
    /// </value>
    public bool ShowConfidence { get; init; } = false;

    /// <summary>
    /// Whether to group citations by entity type.
    /// </summary>
    /// <value>
    /// <c>true</c> to order citations by <see cref="EntityCitation.EntityType"/>
    /// then by <see cref="EntityCitation.EntityName"/>;
    /// <c>false</c> to preserve original order. Defaults to <c>true</c>.
    /// </value>
    public bool GroupByType { get; init; } = true;
}
