// =============================================================================
// File: EntityDetailView.axaml.cs
// Project: Lexichord.Modules.Knowledge
// Description: Code-behind for EntityDetailView.
// =============================================================================
// LOGIC: Minimal code-behind for EntityDetailView. DataContext binding and
//   all interaction logic is handled by EntityDetailViewModel.
//
// v0.4.7f: Entity Detail View (Knowledge Graph Browser)
// Dependencies: EntityDetailViewModel (v0.4.7f)
// =============================================================================

using Avalonia.Controls;

namespace Lexichord.Modules.Knowledge.UI.Views;

/// <summary>
/// Code-behind for the Entity Detail View.
/// </summary>
/// <remarks>
/// <para>
/// This view displays comprehensive details about a selected knowledge entity
/// including properties, relationships, and source documents.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7f as part of the Entity Detail View.
/// </para>
/// </remarks>
public partial class EntityDetailView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityDetailView"/> class.
    /// </summary>
    public EntityDetailView()
    {
        InitializeComponent();
    }
}
