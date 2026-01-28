using Avalonia.Controls;
using Avalonia.Interactivity;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Status bar component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: The StatusBar contains the theme toggle button which:
/// 1. Displays 🌙 (moon) for dark mode, ☀️ (sun) for light mode
/// 2. Calls ThemeManager.ToggleTheme() on click
/// 3. Updates icon when ThemeChanged event fires
/// </remarks>
public partial class StatusBar : UserControl
{
    private IThemeManager? _themeManager;
    private TextBlock? _themeIcon;

    public StatusBar()
    {
        InitializeComponent();
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
    private void OnThemeToggleClick(object? sender, RoutedEventArgs e)
    {
        _themeManager?.ToggleTheme();
    }

    /// <summary>
    /// Handles theme changed events.
    /// </summary>
    private void OnThemeChanged(object? sender, ThemeMode mode)
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

        var effective = _themeManager.GetEffectiveTheme();
        _themeIcon.Text = effective == ThemeMode.Dark ? "🌙" : "☀️";
    }
}
