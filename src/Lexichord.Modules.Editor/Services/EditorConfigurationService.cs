using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// Stub implementation of editor configuration service.
/// </summary>
/// <remarks>
/// LOGIC: Provides default settings for v0.1.3a.
/// Full persistence implementation deferred to v0.1.3d.
/// </remarks>
public class EditorConfigurationService : IEditorConfigurationService
{
    private EditorSettings _settings = new();

    /// <inheritdoc/>
    public event EventHandler<EditorSettingsChangedEventArgs>? SettingsChanged;

    /// <inheritdoc/>
    public EditorSettings GetSettings() => _settings;

    /// <inheritdoc/>
    public Task SaveSettingsAsync(EditorSettings settings)
    {
        var oldSettings = _settings;
        _settings = settings;
        SettingsChanged?.Invoke(this, new EditorSettingsChangedEventArgs(oldSettings, settings));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task LoadSettingsAsync()
    {
        // LOGIC: No persistence in v0.1.3a, just use defaults
        return Task.CompletedTask;
    }
}
