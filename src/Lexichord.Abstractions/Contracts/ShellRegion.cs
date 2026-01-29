namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the available shell regions in the Host.
/// </summary>
/// <remarks>
/// LOGIC: Shell regions map to predefined areas in MainWindow:
/// - Top: Below title bar, full width (toolbars, ribbons)
/// - Left: Left sidebar (navigation, file tree)
/// - Center: Main content area (editor, documents)
/// - Right: Right sidebar (properties, inspector)
/// - Bottom: Status bar area (status, output)
///
/// The Host's MainWindow defines containers for each region.
/// Modules contribute views that are placed in these containers.
/// </remarks>
public enum ShellRegion
{
    /// <summary>
    /// Top toolbar/ribbon area, full width.
    /// </summary>
    Top = 0,

    /// <summary>
    /// Left sidebar/navigation area.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Main content/editor area.
    /// </summary>
    Center = 2,

    /// <summary>
    /// Right sidebar/properties area.
    /// </summary>
    Right = 3,

    /// <summary>
    /// Bottom status bar/output area.
    /// </summary>
    Bottom = 4
}
