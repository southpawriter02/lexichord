using System.Text.Json;
using Avalonia.Media;
using AvaloniaEdit;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// Service for managing editor configuration settings with persistence.
/// </summary>
/// <remarks>
/// LOGIC: Implements IEditorConfigurationService with:
/// - File-based JSON persistence (following WindowStateService pattern)
/// - Thread-safe settings access via lock
/// - Font fallback chain using Avalonia's FontManager
/// - Debounced persistence for zoom operations (500ms)
/// - Settings validation on load
///
/// v0.1.3d: Full implementation replacing the v0.1.3a stub.
/// </remarks>
public sealed class EditorConfigurationService : IEditorConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<EditorConfigurationService> _logger;
    private readonly string _settingsFilePath;
    private readonly object _lock = new();
    private readonly HashSet<string> _installedFonts;
    private readonly List<string> _installedMonospaceFonts;

    private EditorSettings _settings = new();
    private string? _resolvedFontFamily;
    private CancellationTokenSource? _persistDebounceToken;
    private const int PersistDebounceMs = 500;
    private const double DefaultFontSize = 14.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public EditorConfigurationService(ILogger<EditorConfigurationService> logger)
    {
        _logger = logger;

        // LOGIC: Use platform-appropriate AppData location
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var lexichordDir = Path.Combine(appData, "Lexichord");
        Directory.CreateDirectory(lexichordDir);
        _settingsFilePath = Path.Combine(lexichordDir, "editor-settings.json");

        // LOGIC: Cache installed fonts at startup for efficient lookup
        _installedFonts = GetSystemFonts();
        _installedMonospaceFonts = BuildMonospaceFontList();

        _logger.LogDebug(
            "EditorConfigurationService initialized. Settings file: {FilePath}, {FontCount} system fonts detected",
            _settingsFilePath, _installedFonts.Count);
    }

    /// <inheritdoc/>
    public event EventHandler<EditorSettingsChangedEventArgs>? SettingsChanged;

    #region IEditorConfigurationService Implementation

    /// <inheritdoc/>
    public EditorSettings GetSettings()
    {
        lock (_lock)
        {
            return _settings;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateSettingsAsync(EditorSettings settings)
    {
        EditorSettings oldSettings;
        EditorSettings validated;

        lock (_lock)
        {
            oldSettings = _settings;
            validated = settings.Validated();
            _settings = validated;
            _resolvedFontFamily = null; // Reset cached font resolution
        }

        _logger.LogDebug("Editor settings updated");

        // LOGIC: Raise change event
        OnSettingsChanged(oldSettings, validated);

        // LOGIC: Persist to disk
        await SaveSettingsAsync();
    }

    /// <inheritdoc/>
    public async Task LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogDebug("No saved editor settings found, using defaults");
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var loaded = JsonSerializer.Deserialize<EditorSettings>(json, JsonOptions);

            if (loaded is not null)
            {
                lock (_lock)
                {
                    _settings = loaded.Validated();
                    _resolvedFontFamily = null;
                }

                _logger.LogInformation(
                    "Loaded editor settings: FontFamily={FontFamily}, FontSize={FontSize}",
                    _settings.FontFamily, _settings.FontSize);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Corrupted editor settings file, using defaults");
            TryDeleteCorruptedFile();
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read editor settings, using defaults");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error loading editor settings, using defaults");
        }
    }

    /// <inheritdoc/>
    public async Task SaveSettingsAsync()
    {
        try
        {
            EditorSettings settingsToSave;
            lock (_lock)
            {
                settingsToSave = _settings;
            }

            var json = JsonSerializer.Serialize(settingsToSave, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            _logger.LogDebug("Editor settings saved to {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save editor settings");
        }
    }

    #endregion

    #region Zoom Operations

    /// <inheritdoc/>
    public void ZoomIn()
    {
        lock (_lock)
        {
            var newSize = Math.Min(_settings.FontSize + _settings.ZoomIncrement, _settings.MaxFontSize);
            if (Math.Abs(newSize - _settings.FontSize) > 0.01)
            {
                var oldSettings = _settings;
                _settings = _settings with { FontSize = newSize };
                _logger.LogDebug("Zoom in: {OldSize} -> {NewSize}", oldSettings.FontSize, newSize);
                OnSettingsChanged(oldSettings, _settings, nameof(EditorSettings.FontSize));
                DebouncePersist();
            }
        }
    }

    /// <inheritdoc/>
    public void ZoomOut()
    {
        lock (_lock)
        {
            var newSize = Math.Max(_settings.FontSize - _settings.ZoomIncrement, _settings.MinFontSize);
            if (Math.Abs(newSize - _settings.FontSize) > 0.01)
            {
                var oldSettings = _settings;
                _settings = _settings with { FontSize = newSize };
                _logger.LogDebug("Zoom out: {OldSize} -> {NewSize}", oldSettings.FontSize, newSize);
                OnSettingsChanged(oldSettings, _settings, nameof(EditorSettings.FontSize));
                DebouncePersist();
            }
        }
    }

    /// <inheritdoc/>
    public void ResetZoom()
    {
        lock (_lock)
        {
            if (Math.Abs(_settings.FontSize - DefaultFontSize) > 0.01)
            {
                var oldSettings = _settings;
                _settings = _settings with { FontSize = DefaultFontSize };
                _logger.LogDebug("Zoom reset to {DefaultSize}", DefaultFontSize);
                OnSettingsChanged(oldSettings, _settings, nameof(EditorSettings.FontSize));
                DebouncePersist();
            }
        }
    }

    #endregion

    #region Font Resolution

    /// <inheritdoc/>
    public string GetResolvedFontFamily()
    {
        lock (_lock)
        {
            if (_resolvedFontFamily is not null)
            {
                return _resolvedFontFamily;
            }

            // LOGIC: Check if configured font is installed
            if (IsFontInstalled(_settings.FontFamily))
            {
                _resolvedFontFamily = _settings.FontFamily;
                return _resolvedFontFamily;
            }

            // LOGIC: Fall back to first available font
            foreach (var fallback in EditorSettings.FallbackFonts)
            {
                if (IsFontInstalled(fallback))
                {
                    _logger.LogWarning(
                        "Configured font '{ConfiguredFont}' not found, using fallback '{FallbackFont}'",
                        _settings.FontFamily, fallback);
                    _resolvedFontFamily = fallback;
                    return _resolvedFontFamily;
                }
            }

            // LOGIC: Use generic monospace as last resort
            _logger.LogWarning("No suitable font found, using system default monospace");
            _resolvedFontFamily = "monospace";
            return _resolvedFontFamily;
        }
    }

    /// <inheritdoc/>
    public bool IsFontInstalled(string fontFamily)
    {
        return _installedFonts.Contains(fontFamily);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetInstalledMonospaceFonts()
    {
        return _installedMonospaceFonts;
    }

    #endregion

    #region ApplySettings (Called directly by View)

    /// <summary>
    /// Applies current settings to a TextEditor control.
    /// </summary>
    /// <param name="editor">The editor to configure.</param>
    /// <remarks>
    /// Note: This method is not part of the interface to avoid coupling
    /// the Abstractions project to AvaloniaEdit. Call directly on the
    /// concrete service instance from the View.
    /// </remarks>
    public void ApplySettings(TextEditor editor)
    {
        if (editor is null) return;

        var settings = GetSettings();

        // LOGIC: Apply font settings
        editor.FontFamily = new FontFamily(GetResolvedFontFamily());
        editor.FontSize = settings.FontSize;

        // LOGIC: Apply display settings
        editor.ShowLineNumbers = settings.ShowLineNumbers;
        editor.WordWrap = settings.WordWrap;

        // LOGIC: Apply TextEditor options
        var options = editor.Options;
        options.ShowSpaces = settings.ShowWhitespace;
        options.ShowTabs = settings.ShowWhitespace;
        options.ShowEndOfLine = settings.ShowEndOfLine;
        options.ConvertTabsToSpaces = settings.UseSpacesForTabs;
        options.IndentationSize = settings.IndentSize;
        options.EnableHyperlinks = true;
        options.HighlightCurrentLine = settings.HighlightCurrentLine;

        _logger.LogDebug(
            "Applied settings to editor: Font={Font}@{Size}pt, LineNumbers={LineNumbers}, WordWrap={WordWrap}",
            GetResolvedFontFamily(), settings.FontSize, settings.ShowLineNumbers, settings.WordWrap);
    }

    #endregion

    #region Private Methods

    private void OnSettingsChanged(EditorSettings oldSettings, EditorSettings newSettings, string? propertyName = null)
    {
        SettingsChanged?.Invoke(this, new EditorSettingsChangedEventArgs(oldSettings, newSettings, propertyName));
    }

    private void DebouncePersist()
    {
        // LOGIC: Cancel any pending persistence operation
        _persistDebounceToken?.Cancel();
        _persistDebounceToken = new CancellationTokenSource();

        var token = _persistDebounceToken.Token;

        // LOGIC: Schedule persistence after debounce period
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(PersistDebounceMs, token);
                if (!token.IsCancellationRequested)
                {
                    await SaveSettingsAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when debounce is cancelled
            }
        }, token);
    }

    private HashSet<string> GetSystemFonts()
    {
        var fonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var family in FontManager.Current.SystemFonts)
            {
                fonts.Add(family.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate system fonts");
        }

        return fonts;
    }

    private List<string> BuildMonospaceFontList()
    {
        // LOGIC: Start with known monospace fonts from fallback list
        var monoFonts = new List<string>();

        foreach (var font in EditorSettings.FallbackFonts)
        {
            if (font != "monospace" && IsFontInstalled(font))
            {
                monoFonts.Add(font);
            }
        }

        return monoFonts;
    }

    private void TryDeleteCorruptedFile()
    {
        try
        {
            File.Delete(_settingsFilePath);
            _logger.LogDebug("Deleted corrupted settings file at {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete corrupted settings file");
        }
    }

    #endregion
}
