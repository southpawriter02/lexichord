// =============================================================================
// File: EntityListView.axaml.cs
// Project: Lexichord.Modules.Knowledge
// Description: Code-behind for EntityListView.
// =============================================================================
// LOGIC: Minimal code-behind for the Entity List View. ViewModel is injected
//   via DI and bound in the constructor.
//
// v0.4.7e: Entity List View (Knowledge Graph Browser)
// Dependencies: EntityListViewModel (v0.4.7e)
// =============================================================================

using Avalonia.Controls;

namespace Lexichord.Modules.Knowledge.UI.Views;

/// <summary>
/// Code-behind for the Entity List View.
/// </summary>
/// <remarks>
/// The view binds to <see cref="ViewModels.EntityListViewModel"/> which is
/// set as the DataContext via DI injection.
/// </remarks>
public partial class EntityListView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityListView"/> class.
    /// </summary>
    public EntityListView()
    {
        InitializeComponent();
    }
}
