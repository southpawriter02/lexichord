// =============================================================================
// File: RelationshipViewerPanel.axaml.cs
// Project: Lexichord.Modules.Knowledge
// Description: Code-behind for the Relationship Viewer panel.
// =============================================================================
// LOGIC: Minimal code-behind for AXAML initialization. The ViewModel handles
//   all business logic. This file exists only for standard Avalonia patterns.
//
// v0.4.7h: Relationship Viewer (Knowledge Graph Browser)
// Dependencies: Avalonia UI
// =============================================================================

using Avalonia.Controls;

namespace Lexichord.Modules.Knowledge.UI.Views;

/// <summary>
/// Code-behind for the Relationship Viewer panel.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RelationshipViewerPanel"/> displays entity relationships in a
/// hierarchical tree view. Business logic is handled by
/// <see cref="ViewModels.RelationshipViewerPanelViewModel"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7h as part of the Relationship Viewer.
/// </para>
/// </remarks>
public partial class RelationshipViewerPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RelationshipViewerPanel"/> class.
    /// </summary>
    public RelationshipViewerPanel()
    {
        InitializeComponent();
    }
}
