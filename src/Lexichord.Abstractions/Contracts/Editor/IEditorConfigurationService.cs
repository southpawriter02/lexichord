namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Service for managing editor configuration settings.
/// </summary>
/// <remarks>
/// LOGIC: Provides access to editor configuration with:
/// - Settings retrieval for ViewModel binding
/// - Settings persistence to JSON storage
/// - Real-time zoom operations (Ctrl+Scroll)
/// - Font fallback resolution
/// - Change notification for live updates
///
/// v0.1.3d: Full implementation with persistence and zoom support.
/// 
/// Note: ApplySettings(TextEditor) is implemented on the concrete class
/// and called directly by the view to avoid coupling Abstractions to AvaloniaEdit.
/// </remarks>
public interface IEditorConfigurationService
{
    /// <summary>
    /// Gets the current editor settings.
    /// </summary>
    /// <returns>The active editor settings.</returns>
    EditorSettings GetSettings();

    /// <summary>
    /// Updates editor settings with validation.
    /// </summary>
    /// <param name="settings">The settings to apply.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateSettingsAsync(EditorSettings settings);

    /// <summary>
    /// Loads settings from persistence.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task LoadSettingsAsync();

    /// <summary>
    /// Saves current settings to persistence.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task SaveSettingsAsync();

    #region Zoom Operations

    /// <summary>
    /// Increases font size by the configured increment.
    /// </summary>
    void ZoomIn();

    /// <summary>
    /// Decreases font size by the configured increment.
    /// </summary>
    void ZoomOut();

    /// <summary>
    /// Resets font size to default (14pt).
    /// </summary>
    void ResetZoom();

    #endregion

    #region Font Resolution

    /// <summary>
    /// Gets the resolved font family (configured or first available fallback).
    /// </summary>
    /// <returns>The font family name to use.</returns>
    string GetResolvedFontFamily();

    /// <summary>
    /// Checks if a font family is installed on the system.
    /// </summary>
    /// <param name="fontFamily">The font family name to check.</param>
    /// <returns>True if the font is installed.</returns>
    bool IsFontInstalled(string fontFamily);

    /// <summary>
    /// Gets a list of installed monospace fonts suitable for code editing.
    /// </summary>
    /// <returns>List of installed monospace font family names.</returns>
    IReadOnlyList<string> GetInstalledMonospaceFonts();

    #endregion

    /// <summary>
    /// Raised when settings change.
    /// </summary>
    event EventHandler<EditorSettingsChangedEventArgs> SettingsChanged;
}

