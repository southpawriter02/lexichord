using Avalonia.Controls;

namespace Lexichord.Modules.Editor.Views;

/// <summary>
/// View for the search and replace overlay.
/// </summary>
public partial class SearchOverlayView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchOverlayView"/> class.
    /// </summary>
    public SearchOverlayView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Focuses the search text box.
    /// </summary>
    public void FocusSearchBox()
    {
        SearchTextBox?.Focus();
        SearchTextBox?.SelectAll();
    }
}
