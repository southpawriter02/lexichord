using Avalonia.Controls;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Content host panel component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: This is the primary content area for v0.0.2b.
/// The ModuleViewHost ContentControl serves as the injection point
/// for module views loaded via the ModuleLoader (v0.0.4+).
/// </remarks>
public partial class ContentHostPanel : UserControl
{
    public ContentHostPanel()
    {
        InitializeComponent();
    }
}
