using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Layout;

namespace Lexichord.Host.Layout;

/// <summary>
/// Lexichord-specific tool pane implementation.
/// </summary>
/// <remarks>
/// LOGIC: Extends Dock.Model.Mvvm's Tool with:
/// - Preferred region for initial placement
/// - Size constraints for minimum dimensions
/// - Close behavior control
///
/// Uses CommunityToolkit.Mvvm for observable property generation.
/// </remarks>
public partial class LexichordTool : Tool, ITool
{
    /// <summary>
    /// Gets or sets the preferred shell region for this tool.
    /// </summary>
    [ObservableProperty]
    private ShellRegion _preferredRegion = ShellRegion.Left;

    /// <summary>
    /// Gets or sets the minimum width for the tool pane.
    /// </summary>
    [ObservableProperty]
    private double _minWidth = 150;

    /// <summary>
    /// Gets or sets the minimum height for the tool pane.
    /// </summary>
    [ObservableProperty]
    private double _minHeight = 100;

    /// <summary>
    /// Gets or sets a value indicating whether the tool can be closed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Bridges to the base class CanClose property.
    /// Some tools may be essential and should not be closable.
    /// </remarks>
    public new bool CanClose
    {
        get => base.CanClose;
        set => base.CanClose = value;
    }
}
