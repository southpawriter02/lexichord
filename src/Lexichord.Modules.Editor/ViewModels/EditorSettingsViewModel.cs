using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.ViewModels;

/// <summary>
/// ViewModel for the editor settings panel.
/// </summary>
/// <remarks>
/// LOGIC: Provides two-way binding between UI and IEditorConfigurationService.
/// - Loads initial values from service
/// - Auto-saves on property change
/// - Subscribes to service SettingsChanged for external updates (e.g., zoom)
///
/// v0.1.3d: Full implementation for settings UI.
/// </remarks>
public partial class EditorSettingsViewModel : ObservableObject
{
    private readonly IEditorConfigurationService _configService;
    private readonly ILogger<EditorSettingsViewModel> _logger;
    private bool _isUpdating;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorSettingsViewModel"/> class.
    /// </summary>
    public EditorSettingsViewModel(
        IEditorConfigurationService configService,
        ILogger<EditorSettingsViewModel> logger)
    {
        _configService = configService;
        _logger = logger;

        // LOGIC: Initialize properties from current settings
        LoadFromSettings();

        // LOGIC: Subscribe to external setting changes (e.g., zoom via Ctrl+Scroll)
        _configService.SettingsChanged += OnSettingsChanged;
    }

    #region Font Settings

    [ObservableProperty]
    private string _fontFamily = "Cascadia Code";

    [ObservableProperty]
    private double _fontSize = 14.0;

    [ObservableProperty]
    private IReadOnlyList<string> _availableFonts = Array.Empty<string>();

    partial void OnFontFamilyChanged(string value) => SaveSetting();
    partial void OnFontSizeChanged(double value) => SaveSetting();

    #endregion

    #region Indentation Settings

    [ObservableProperty]
    private bool _useSpacesForTabs = true;

    [ObservableProperty]
    private int _tabSize = 4;

    [ObservableProperty]
    private int _indentSize = 4;

    [ObservableProperty]
    private bool _autoIndent = true;

    partial void OnUseSpacesForTabsChanged(bool value) => SaveSetting();
    partial void OnTabSizeChanged(int value) => SaveSetting();
    partial void OnIndentSizeChanged(int value) => SaveSetting();
    partial void OnAutoIndentChanged(bool value) => SaveSetting();

    #endregion

    #region Display Settings

    [ObservableProperty]
    private bool _showLineNumbers = true;

    [ObservableProperty]
    private bool _wordWrap = true;

    [ObservableProperty]
    private bool _highlightCurrentLine = true;

    [ObservableProperty]
    private bool _showWhitespace = false;

    [ObservableProperty]
    private bool _showEndOfLine = false;

    [ObservableProperty]
    private bool _highlightMatchingBrackets = true;

    [ObservableProperty]
    private int _verticalRulerPosition = 80;

    [ObservableProperty]
    private bool _showVerticalRuler = false;

    [ObservableProperty]
    private bool _smoothScrolling = true;

    partial void OnShowLineNumbersChanged(bool value) => SaveSetting();
    partial void OnWordWrapChanged(bool value) => SaveSetting();
    partial void OnHighlightCurrentLineChanged(bool value) => SaveSetting();
    partial void OnShowWhitespaceChanged(bool value) => SaveSetting();
    partial void OnShowEndOfLineChanged(bool value) => SaveSetting();
    partial void OnHighlightMatchingBracketsChanged(bool value) => SaveSetting();
    partial void OnVerticalRulerPositionChanged(int value) => SaveSetting();
    partial void OnShowVerticalRulerChanged(bool value) => SaveSetting();
    partial void OnSmoothScrollingChanged(bool value) => SaveSetting();

    #endregion

    #region Cursor Settings

    [ObservableProperty]
    private bool _blinkCursor = true;

    [ObservableProperty]
    private int _cursorBlinkRate = 530;

    partial void OnBlinkCursorChanged(bool value) => SaveSetting();
    partial void OnCursorBlinkRateChanged(int value) => SaveSetting();

    #endregion

    #region Commands

    /// <summary>
    /// Command to reset all settings to defaults.
    /// </summary>
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        _logger.LogInformation("Resetting editor settings to defaults");
        await _configService.UpdateSettingsAsync(new EditorSettings());
        LoadFromSettings();
    }

    #endregion

    #region Private Methods

    private void LoadFromSettings()
    {
        _isUpdating = true;
        try
        {
            var settings = _configService.GetSettings();

            // Font
            FontFamily = settings.FontFamily;
            FontSize = settings.FontSize;
            AvailableFonts = _configService.GetInstalledMonospaceFonts();

            // Indentation
            UseSpacesForTabs = settings.UseSpacesForTabs;
            TabSize = settings.TabSize;
            IndentSize = settings.IndentSize;
            AutoIndent = settings.AutoIndent;

            // Display
            ShowLineNumbers = settings.ShowLineNumbers;
            WordWrap = settings.WordWrap;
            HighlightCurrentLine = settings.HighlightCurrentLine;
            ShowWhitespace = settings.ShowWhitespace;
            ShowEndOfLine = settings.ShowEndOfLine;
            HighlightMatchingBrackets = settings.HighlightMatchingBrackets;
            VerticalRulerPosition = settings.VerticalRulerPosition;
            ShowVerticalRuler = settings.ShowVerticalRuler;
            SmoothScrolling = settings.SmoothScrolling;

            // Cursor
            BlinkCursor = settings.BlinkCursor;
            CursorBlinkRate = settings.CursorBlinkRate;

            _logger.LogDebug("Loaded settings into ViewModel");
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SaveSetting()
    {
        if (_isUpdating) return;

        var newSettings = new EditorSettings
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            UseSpacesForTabs = UseSpacesForTabs,
            TabSize = TabSize,
            IndentSize = IndentSize,
            AutoIndent = AutoIndent,
            ShowLineNumbers = ShowLineNumbers,
            WordWrap = WordWrap,
            HighlightCurrentLine = HighlightCurrentLine,
            ShowWhitespace = ShowWhitespace,
            ShowEndOfLine = ShowEndOfLine,
            HighlightMatchingBrackets = HighlightMatchingBrackets,
            VerticalRulerPosition = VerticalRulerPosition,
            ShowVerticalRuler = ShowVerticalRuler,
            SmoothScrolling = SmoothScrolling,
            BlinkCursor = BlinkCursor,
            CursorBlinkRate = CursorBlinkRate
        };

        _logger.LogDebug("Saving editor settings");
        _ = _configService.UpdateSettingsAsync(newSettings);
    }

    private void OnSettingsChanged(object? sender, EditorSettingsChangedEventArgs e)
    {
        // LOGIC: Only update specific property if known
        if (e.PropertyName == nameof(EditorSettings.FontSize))
        {
            _isUpdating = true;
            FontSize = e.NewSettings.FontSize;
            _isUpdating = false;
        }
        else if (e.PropertyName is null)
        {
            // Full reload
            LoadFromSettings();
        }
    }

    #endregion
}
