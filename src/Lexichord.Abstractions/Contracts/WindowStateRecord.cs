namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents the persisted state of the main application window.
/// </summary>
/// <remarks>
/// LOGIC: This record captures all state needed to restore the window
/// to its previous position, size, and configuration. The record is
/// immutable and serialized to JSON for persistence.
///
/// Platform coordinates use screen pixels, not logical units.
/// Multi-monitor setups may have negative coordinates (left/above primary).
/// </remarks>
/// <param name="X">Window X position in screen coordinates (can be negative).</param>
/// <param name="Y">Window Y position in screen coordinates (can be negative).</param>
/// <param name="Width">Window width in pixels (minimum: 1024).</param>
/// <param name="Height">Window height in pixels (minimum: 768).</param>
/// <param name="IsMaximized">Whether the window is maximized.</param>
/// <param name="Theme">The user's preferred theme mode.</param>
public record WindowStateRecord(
    double X,
    double Y,
    double Width,
    double Height,
    bool IsMaximized,
    ThemeMode Theme
)
{
    /// <summary>
    /// Default window state for first launch.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses centered positioning (handled by caller), default size,
    /// windowed mode, and system theme detection.
    /// </remarks>
    public static WindowStateRecord Default => new(
        X: 0,
        Y: 0,
        Width: 1400,
        Height: 900,
        IsMaximized: false,
        Theme: ThemeMode.System
    );
}
