using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Configuration for a dock region's default dimensions and behavior.
/// </summary>
/// <param name="Region">The shell region this configuration applies to.</param>
/// <param name="DefaultWidth">Default width in pixels (NaN for auto-sizing).</param>
/// <param name="DefaultHeight">Default height in pixels (NaN for auto-sizing).</param>
/// <param name="MinWidth">Minimum width constraint in pixels.</param>
/// <param name="MinHeight">Minimum height constraint in pixels.</param>
/// <param name="IsCollapsible">Whether the region can be collapsed.</param>
/// <param name="DefaultCollapsed">Whether the region starts collapsed.</param>
/// <remarks>
/// LOGIC: Provides consistent region sizing across the application.
/// The factory uses these configurations when creating the default layout
/// to ensure predictable initial dimensions.
/// </remarks>
public record DockRegionConfig(
    ShellRegion Region,
    double DefaultWidth = double.NaN,
    double DefaultHeight = double.NaN,
    double MinWidth = 150,
    double MinHeight = 100,
    bool IsCollapsible = true,
    bool DefaultCollapsed = false
)
{
    /// <summary>
    /// Default configuration for the Left region (navigation, file tree).
    /// </summary>
    public static readonly DockRegionConfig Left = new(
        Region: ShellRegion.Left,
        DefaultWidth: 200,
        MinWidth: 150,
        MinHeight: 100);

    /// <summary>
    /// Default configuration for the Right region (properties, inspector).
    /// </summary>
    public static readonly DockRegionConfig Right = new(
        Region: ShellRegion.Right,
        DefaultWidth: 250,
        MinWidth: 150,
        MinHeight: 100);

    /// <summary>
    /// Default configuration for the Bottom region (output, terminal).
    /// </summary>
    public static readonly DockRegionConfig Bottom = new(
        Region: ShellRegion.Bottom,
        DefaultHeight: 200,
        MinWidth: 100,
        MinHeight: 100);

    /// <summary>
    /// Default configuration for the Center region (document area).
    /// </summary>
    /// <remarks>
    /// Center region is not collapsible and fills available space.
    /// </remarks>
    public static readonly DockRegionConfig Center = new(
        Region: ShellRegion.Center,
        IsCollapsible: false);
}
