using Avalonia.Controls;

namespace Lexichord.Host.Views;

/// <summary>
/// Context menu for document tabs.
/// </summary>
/// <remarks>
/// LOGIC: Provides standard IDE tab actions:
/// - Close, Close All, Close All But This, Close to the Right
/// - Pin/Unpin
/// - Copy Path, Reveal in Explorer (for file-based documents)
/// </remarks>
public partial class TabContextMenu : MenuFlyout
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabContextMenu"/> class.
    /// </summary>
    public TabContextMenu()
    {
        // MenuFlyout initializes content via XAML
    }
}
