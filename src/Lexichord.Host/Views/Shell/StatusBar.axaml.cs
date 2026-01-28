using Avalonia.Controls;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Status bar component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: This is a presentation-only component for v0.0.2b.
/// Displays system status, version info, and theme toggle.
/// Theme toggle functionality will be added in v0.0.2c.
/// </remarks>
public partial class StatusBar : UserControl
{
    public StatusBar()
    {
        InitializeComponent();
    }
}
