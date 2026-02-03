// =============================================================================
// File: SearchFilterPanel.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for the SearchFilterPanel view.
// =============================================================================
// LOGIC: Minimal code-behind following MVVM pattern. All logic resides in the
//        SearchFilterPanelViewModel. This file only handles view initialization.
// =============================================================================
// VERSION: v0.5.5b (Filter UI Component)
// =============================================================================

using Avalonia.Controls;

namespace Lexichord.Modules.RAG.Views;

/// <summary>
/// Code-behind for the <see cref="SearchFilterPanel"/> view.
/// </summary>
/// <remarks>
/// <para>
/// This file contains minimal code-behind, following the MVVM pattern.
/// All business logic is handled by <see cref="ViewModels.SearchFilterPanelViewModel"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5b as part of The Filter System feature.
/// </para>
/// </remarks>
public partial class SearchFilterPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchFilterPanel"/> class.
    /// </summary>
    public SearchFilterPanel()
    {
        InitializeComponent();
    }
}
