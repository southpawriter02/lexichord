using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Settings;

/// <summary>
/// ViewModel for the Appearance Settings page.
/// </summary>
/// <remarks>
/// LOGIC: Provides bindable properties for theme selection.
/// When the selected theme changes, it immediately calls IThemeManager.SetThemeAsync()
/// to apply the theme in real-time.
///
/// Version: v0.1.6b
/// </remarks>
public partial class AppearanceSettingsViewModel : ObservableObject
{
    private readonly IThemeManager _themeManager;
    private readonly ILogger<AppearanceSettingsViewModel> _logger;

    [ObservableProperty]
    private bool _isLightSelected;

    [ObservableProperty]
    private bool _isDarkSelected;

    [ObservableProperty]
    private bool _isSystemSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppearanceSettingsViewModel"/> class.
    /// </summary>
    /// <param name="themeManager">The theme manager for applying themes.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public AppearanceSettingsViewModel(
        IThemeManager themeManager,
        ILogger<AppearanceSettingsViewModel> logger)
    {
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize selection based on current theme
        InitializeSelection(_themeManager.CurrentTheme);

        _logger.LogDebug("AppearanceSettingsViewModel initialized with theme: {Theme}", _themeManager.CurrentTheme);
    }

    /// <summary>
    /// Gets the currently selected theme mode.
    /// </summary>
    public ThemeMode SelectedTheme
    {
        get
        {
            if (IsDarkSelected) return ThemeMode.Dark;
            if (IsLightSelected) return ThemeMode.Light;
            return ThemeMode.System;
        }
    }

    /// <summary>
    /// Command to select the Light theme.
    /// </summary>
    [RelayCommand]
    private async Task SelectLightThemeAsync()
    {
        if (IsLightSelected) return;

        IsLightSelected = true;
        IsDarkSelected = false;
        IsSystemSelected = false;

        await ApplyThemeAsync(ThemeMode.Light);
    }

    /// <summary>
    /// Command to select the Dark theme.
    /// </summary>
    [RelayCommand]
    private async Task SelectDarkThemeAsync()
    {
        if (IsDarkSelected) return;

        IsLightSelected = false;
        IsDarkSelected = true;
        IsSystemSelected = false;

        await ApplyThemeAsync(ThemeMode.Dark);
    }

    /// <summary>
    /// Command to select the System theme.
    /// </summary>
    [RelayCommand]
    private async Task SelectSystemThemeAsync()
    {
        if (IsSystemSelected) return;

        IsLightSelected = false;
        IsDarkSelected = false;
        IsSystemSelected = true;

        await ApplyThemeAsync(ThemeMode.System);
    }

    /// <summary>
    /// Initializes the selection state based on the given theme mode.
    /// </summary>
    private void InitializeSelection(ThemeMode theme)
    {
        IsLightSelected = theme == ThemeMode.Light;
        IsDarkSelected = theme == ThemeMode.Dark;
        IsSystemSelected = theme == ThemeMode.System;
    }

    /// <summary>
    /// Applies the specified theme asynchronously.
    /// </summary>
    private async Task ApplyThemeAsync(ThemeMode theme)
    {
        try
        {
            await _themeManager.SetThemeAsync(theme);
            _logger.LogDebug("Theme selection changed to: {Theme}", theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme: {Theme}", theme);
            // Revert selection on failure
            InitializeSelection(_themeManager.CurrentTheme);
        }
    }
}
