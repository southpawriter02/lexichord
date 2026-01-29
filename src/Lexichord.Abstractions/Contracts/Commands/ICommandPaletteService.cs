namespace Lexichord.Abstractions.Contracts.Commands;

/// <summary>
/// Service for controlling the Command Palette.
/// </summary>
/// <remarks>
/// LOGIC: ICommandPaletteService provides a programmatic API for:
/// - Opening the palette in different modes
/// - Closing the palette
/// - Checking if palette is visible
///
/// Used by keyboard shortcuts and menu commands.
/// </remarks>
public interface ICommandPaletteService
{
    /// <summary>
    /// Shows the Command Palette.
    /// </summary>
    /// <param name="mode">The mode to open in.</param>
    /// <param name="initialQuery">Optional initial search query.</param>
    Task ShowAsync(PaletteMode mode = PaletteMode.Commands, string? initialQuery = null);

    /// <summary>
    /// Hides the Command Palette.
    /// </summary>
    void Hide();

    /// <summary>
    /// Toggles the Command Palette visibility.
    /// </summary>
    /// <param name="mode">The mode to open in if showing.</param>
    Task ToggleAsync(PaletteMode mode = PaletteMode.Commands);

    /// <summary>
    /// Gets whether the palette is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets the current palette mode.
    /// </summary>
    PaletteMode CurrentMode { get; }

    /// <summary>
    /// Event raised when palette visibility changes.
    /// </summary>
    event EventHandler<PaletteVisibilityChangedEventArgs>? VisibilityChanged;
}

/// <summary>
/// Event args for palette visibility changes.
/// </summary>
public class PaletteVisibilityChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether the palette is now visible.
    /// </summary>
    public required bool IsVisible { get; init; }

    /// <summary>
    /// Gets the current palette mode.
    /// </summary>
    public required PaletteMode Mode { get; init; }
}
