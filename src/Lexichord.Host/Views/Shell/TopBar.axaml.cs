using Avalonia.Controls;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Top bar component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: This is a presentation-only component for v0.0.2b.
/// Displays application branding (logo, title, tagline).
/// Future versions will add menu items and window controls.
/// </remarks>
public partial class TopBar : UserControl
{
    public TopBar()
    {
        InitializeComponent();
    }
}
