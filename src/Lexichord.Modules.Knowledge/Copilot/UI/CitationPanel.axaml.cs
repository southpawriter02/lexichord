// =============================================================================
// File: CitationPanel.axaml.cs
// Project: Lexichord.Modules.Knowledge
// Description: Code-behind for CitationPanel.
// =============================================================================
// LOGIC: Minimal code-behind for the citation panel. All interaction logic
//   is handled by CitationViewModel. DataContext binding is set via AXAML
//   or by the parent layout.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: CitationViewModel (v0.6.6h)
// =============================================================================

using Avalonia.Controls;

namespace Lexichord.Modules.Knowledge.Copilot.UI;

/// <summary>
/// Code-behind for the Citation Panel.
/// </summary>
/// <remarks>
/// <para>
/// This panel displays entity citations for a Co-pilot response, showing
/// which Knowledge Graph entities informed the generated content along
/// with their verification status.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public partial class CitationPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CitationPanel"/> class.
    /// </summary>
    public CitationPanel()
    {
        InitializeComponent();
    }
}
