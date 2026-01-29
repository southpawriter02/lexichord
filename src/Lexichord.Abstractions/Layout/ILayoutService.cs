namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Service for persisting and restoring dock layouts.
/// </summary>
/// <remarks>
/// LOGIC: ILayoutService provides a high-level API for layout persistence.
/// It abstracts the serialization format (JSON) and storage location (AppData).
///
/// Key Features:
/// - Named profiles for multiple workspace arrangements
/// - Schema versioning for forward compatibility
/// - Auto-save support via debouncing
/// - Migration path for schema changes
///
/// Storage Location: {AppData}/Lexichord/Layouts/{ProfileName}.json
/// </remarks>
public interface ILayoutService
{
    /// <summary>
    /// Saves the current layout to a named profile.
    /// </summary>
    /// <param name="profileName">Name of the profile (e.g., "Default", "Writing", "Editing").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if save succeeded, false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Saves serialize the entire dock hierarchy including:
    /// - Dock structure (which docks contain which children)
    /// - Proportional sizes of each dock
    /// - Collapsed/expanded state
    /// - Active/selected dockable in each dock
    ///
    /// Does NOT save:
    /// - Document content (editors save their own state)
    /// - Tool-specific state (tools save their own state)
    /// </remarks>
    Task<bool> SaveLayoutAsync(
        string profileName = "Default",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a layout from a named profile.
    /// </summary>
    /// <param name="profileName">Name of the profile to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if load succeeded, false if profile not found or load failed.</returns>
    /// <remarks>
    /// LOGIC: Load deserializes the JSON and reconstructs the dock hierarchy.
    /// Missing dockables are skipped (they'll be re-added by modules).
    /// Extra dockables in the saved layout that no longer exist are ignored.
    /// </remarks>
    Task<bool> LoadLayoutAsync(
        string profileName = "Default",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a layout profile.
    /// </summary>
    /// <param name="profileName">Name of the profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteLayoutAsync(
        string profileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available profile names.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Enumerable of profile names.</returns>
    Task<IEnumerable<string>> GetProfileNamesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a profile exists.
    /// </summary>
    /// <param name="profileName">Name of the profile to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if profile exists.</returns>
    Task<bool> ProfileExistsAsync(
        string profileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets to the default layout, discarding all customizations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reset succeeded.</returns>
    /// <remarks>
    /// LOGIC: This creates a fresh default layout from IDockFactory
    /// and saves it as the current profile, overwriting any customizations.
    /// </remarks>
    Task<bool> ResetToDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a layout to a file (for sharing/backup).
    /// </summary>
    /// <param name="profileName">Profile to export.</param>
    /// <param name="filePath">Destination file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if export succeeded.</returns>
    Task<bool> ExportLayoutAsync(
        string profileName,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a layout from a file.
    /// </summary>
    /// <param name="filePath">Source file path.</param>
    /// <param name="profileName">Name for the imported profile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if import succeeded.</returns>
    Task<bool> ImportLayoutAsync(
        string filePath,
        string profileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active profile name.
    /// </summary>
    string CurrentProfileName { get; }

    /// <summary>
    /// Gets the directory where layouts are stored.
    /// </summary>
    string LayoutDirectory { get; }

    /// <summary>
    /// Event raised when auto-save completes.
    /// </summary>
    event EventHandler<LayoutSavedEventArgs>? LayoutSaved;

    /// <summary>
    /// Event raised when layout is loaded.
    /// </summary>
    event EventHandler<LayoutLoadedEventArgs>? LayoutLoaded;

    /// <summary>
    /// Triggers a debounced auto-save.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called by IRegionManager when layout changes.
    /// Resets the debounce timer to prevent excessive saves.
    /// </remarks>
    void TriggerAutoSave();
}
