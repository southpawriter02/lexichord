// =============================================================================
// File: CitationItemViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: View model for a single citation item in the citation panel.
// =============================================================================
// LOGIC: Simple presentation model for a single citation row in the
//   CitationPanel ItemsControl. Converts EntityCitation data into
//   display-ready properties including verified mark and colour.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: Avalonia.Media (IBrush)
// =============================================================================

using Avalonia.Media;

namespace Lexichord.Modules.Knowledge.Copilot.UI;

/// <summary>
/// View model for a single citation item in the citation panel.
/// </summary>
/// <remarks>
/// <para>
/// Provides display-ready properties for binding in the
/// <see cref="CitationPanel"/> AXAML template. Each instance represents
/// one cited entity.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public class CitationItemViewModel
{
    /// <summary>
    /// Icon for the entity type (e.g., 🔗, 📝).
    /// </summary>
    public string TypeIcon { get; init; } = "";

    /// <summary>
    /// Formatted display label for the citation.
    /// </summary>
    public string DisplayLabel { get; init; } = "";

    /// <summary>
    /// Whether the entity was verified against validation.
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>
    /// Verification mark symbol (✓ or ?).
    /// </summary>
    public string VerifiedMark { get; init; } = "";

    /// <summary>
    /// Colour for the verification mark.
    /// </summary>
    /// <value>
    /// Green for verified entities, orange for unverified.
    /// </value>
    public IBrush VerifiedColor { get; init; } = Brushes.Gray;
}
