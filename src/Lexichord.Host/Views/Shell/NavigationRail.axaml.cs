using Avalonia.Controls;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Navigation rail component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: This is a presentation-only component for v0.0.2b.
/// Displays icon buttons for switching between application sections.
/// Future versions will add navigation command bindings.
/// </remarks>
public partial class NavigationRail : UserControl
{
    public NavigationRail()
    {
        InitializeComponent();
    }
}
