namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Manages window state persistence between application sessions.
/// </summary>
/// <remarks>
/// LOGIC: The WindowStateService is responsible for:
/// 1. Loading saved window state from the file system
/// 2. Saving window state when the application closes
/// 3. Validating that loaded state is applicable to current screen configuration
///
/// The service uses async I/O to prevent blocking the UI thread.
/// </remarks>
public interface IWindowStateService
{
    /// <summary>
    /// Loads the saved window state from persistent storage.
    /// </summary>
    /// <returns>
    /// The saved window state, or null if no saved state exists or
    /// the saved state could not be loaded (corrupted file, etc.).
    /// </returns>
    /// <remarks>
    /// LOGIC: This method should never throw. File I/O errors or JSON
    /// deserialization errors are caught and result in null return.
    /// </remarks>
    Task<WindowStateRecord?> LoadAsync();

    /// <summary>
    /// Saves the current window state to persistent storage.
    /// </summary>
    /// <param name="state">The window state to save.</param>
    /// <returns>A task representing the async save operation.</returns>
    /// <remarks>
    /// LOGIC: This method should never throw. File I/O errors are caught
    /// and logged, but do not affect application behavior.
    /// </remarks>
    Task SaveAsync(WindowStateRecord state);

    /// <summary>
    /// Validates whether a saved position is visible on current screens.
    /// </summary>
    /// <param name="state">The state to validate.</param>
    /// <returns>True if the position is valid and visible.</returns>
    /// <remarks>
    /// LOGIC: A position is valid if at least a portion of the window
    /// (e.g., 100x100 pixels) would be visible on any connected screen.
    /// This handles cases where a monitor was disconnected.
    /// </remarks>
    bool IsPositionValid(WindowStateRecord state);
}
