// =============================================================================
// File: CitationMarkup.cs
// Project: Lexichord.Abstractions
// Description: Citation markup result for rendering in the Co-pilot UI.
// =============================================================================
// LOGIC: Aggregates all citation data for a single Co-pilot response:
//   the list of cited entities, the overall validation status text,
//   the validation icon indicator, and optional pre-formatted markup
//   for direct display.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: EntityCitation (v0.6.6h), ValidationIcon (v0.6.6h)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Citation markup for rendering in the Co-pilot response UI.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="IEntityCitationRenderer.GenerateCitations"/>.
/// Contains the list of individual entity citations, the overall validation
/// status text, and optionally pre-formatted markup for direct display.
/// </para>
/// <para>
/// <b>Usage:</b> Pass to <see cref="CitationViewModel.Update"/> to bind
/// citation data to the <see cref="CitationPanel"/> UI component.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var markup = renderer.GenerateCitations(result, options);
/// citationViewModel.Update(markup);
/// </code>
/// </example>
public record CitationMarkup
{
    /// <summary>
    /// List of cited entities.
    /// </summary>
    /// <value>
    /// Individual entity citations, optionally ordered by type when
    /// <see cref="CitationOptions.GroupByType"/> is <c>true</c>.
    /// Limited by <see cref="CitationOptions.MaxCitations"/>.
    /// </value>
    public required IReadOnlyList<EntityCitation> Citations { get; init; }

    /// <summary>
    /// Validation status text for display.
    /// </summary>
    /// <value>
    /// A human-readable validation status such as "Validation passed",
    /// "2 warning(s)", or "1 error(s)".
    /// </value>
    public required string ValidationStatus { get; init; }

    /// <summary>
    /// Validation icon indicator.
    /// </summary>
    /// <value>
    /// The <see cref="ValidationIcon"/> corresponding to the overall
    /// validation status. Used for visual display in the citation panel.
    /// </value>
    public ValidationIcon Icon { get; init; }

    /// <summary>
    /// Pre-formatted markup for direct display.
    /// </summary>
    /// <value>
    /// Formatted text output (e.g., tree-view, compact list) ready for
    /// rendering. <c>null</c> if no pre-formatted output was generated.
    /// The format depends on <see cref="CitationOptions.Format"/>.
    /// </value>
    public string? FormattedMarkup { get; init; }
}
