namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Service for managing editor configuration settings.
/// </summary>
/// <remarks>
/// LOGIC: Provides access to editor configuration with:
/// - Settings retrieval for ViewModel binding
/// - Settings persistence (v0.1.3d)
/// - Change notification for live updates
/// 
/// The service acts as the single source of truth for editor preferences.
/// </remarks>
public interface IEditorConfigurationService
{
    /// <summary>
    /// Gets the current editor settings.
    /// </summary>
    /// <returns>The active editor settings.</returns>
    EditorSettings GetSettings();

    /// <summary>
    /// Saves updated editor settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>Full implementation in v0.1.3d.</remarks>
    Task SaveSettingsAsync(EditorSettings settings);

    /// <summary>
    /// Loads settings from persistence.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>Full implementation in v0.1.3d.</remarks>
    Task LoadSettingsAsync();

    /// <summary>
    /// Raised when settings change.
    /// </summary>
    event EventHandler<EditorSettingsChangedEventArgs> SettingsChanged;
}
