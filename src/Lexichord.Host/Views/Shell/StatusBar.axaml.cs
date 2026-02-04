using Avalonia.Controls;
using Avalonia.Interactivity;
using Lexichord.Abstractions.Contracts;
using System.Reflection;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Status bar component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: The StatusBar contains the theme toggle button which:
/// 1. Displays 🌙 (moon) for dark mode, ☀️ (sun) for light mode
/// 2. Cycles through Light -> Dark -> System on click
/// 3. Updates icon when ThemeChanged event fires
///
/// Version: v0.1.6b - Updated for async theme manager
/// </remarks>
public partial class StatusBar : UserControl
{
    private IThemeManager? _themeManager;
    private TextBlock? _themeIcon;
    private TextBlock? _versionText;

    public StatusBar()
    {
        InitializeComponent();

        // LOGIC: Set version display from assembly version
        _versionText = this.FindControl<TextBlock>("VersionText");
        if (_versionText is not null)
        {
            _versionText.Text = GetVersionString();
        }
    }

    /// <summary>
    /// Gets the application version string from the assembly.
    /// </summary>
    private static string GetVersionString()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version ?? new Version(0, 0, 0);
        return $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    /// <summary>
    /// Initializes the theme manager reference and wires up events.
    /// </summary>
    /// <param name="themeManager">The theme manager instance.</param>
    public void Initialize(IThemeManager themeManager)
    {
        _themeManager = themeManager;
        _themeManager.ThemeChanged += OnThemeChanged;

        // Get reference to the theme icon TextBlock
        _themeIcon = this.FindControl<TextBlock>("ThemeIcon");

        // Set initial icon state
        UpdateThemeIcon();
    }

    /// <summary>
    /// Handles the theme toggle button click.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.6b): Cycles through Light -> Dark -> System themes.
    /// </remarks>
    private async void OnThemeToggleClick(object? sender, RoutedEventArgs e)
    {
        if (_themeManager is null) return;

        // LOGIC: Cycle through themes based on current selection
        var nextTheme = _themeManager.CurrentTheme switch
        {
            ThemeMode.Light => ThemeMode.Dark,
            ThemeMode.Dark => ThemeMode.System,
            ThemeMode.System => ThemeMode.Light,
            _ => ThemeMode.System
        };

        await _themeManager.SetThemeAsync(nextTheme);
    }

    /// <summary>
    /// Handles theme changed events.
    /// </summary>
    private void OnThemeChanged(object? sender, ThemeChangedEventArgs args)
    {
        UpdateThemeIcon();
    }

    /// <summary>
    /// Updates the theme toggle icon based on current effective theme.
    /// </summary>
    private void UpdateThemeIcon()
    {
        if (_themeIcon is null || _themeManager is null)
            return;

        var effective = _themeManager.EffectiveTheme;
        _themeIcon.Text = effective == ThemeVariant.Dark ? "🌙" : "☀️";
    }
}
