// =============================================================================
// File: CitationFormat.cs
// Project: Lexichord.Abstractions
// Description: Display format enum for entity citations.
// =============================================================================
// LOGIC: Controls how citations are rendered in the Co-pilot response UI.
//   Compact = icon + name inline, Detailed = full entity details grouped,
//   TreeView = hierarchical tree, Inline = embedded within text.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Display format for entity citations.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="CitationOptions.Format"/> to control how the
/// <see cref="IEntityCitationRenderer"/> renders citation markup.
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item>WriterPro — <see cref="Compact"/> only.</item>
///   <item>Teams — <see cref="Compact"/>, <see cref="Detailed"/>, <see cref="TreeView"/>.</item>
///   <item>Enterprise — all formats including custom.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public enum CitationFormat
{
    /// <summary>
    /// Compact inline format showing icon and entity name.
    /// </summary>
    Compact,

    /// <summary>
    /// Detailed format with full entity information grouped by type.
    /// </summary>
    Detailed,

    /// <summary>
    /// Hierarchical tree view grouped by entity type.
    /// </summary>
    TreeView,

    /// <summary>
    /// Citations embedded inline within the generated text.
    /// </summary>
    Inline
}
