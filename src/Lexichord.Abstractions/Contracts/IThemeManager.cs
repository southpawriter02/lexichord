namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Manages application theme switching and persistence.
/// </summary>
/// <remarks>
/// LOGIC: The ThemeManager is responsible for:
/// 1. Setting the application's RequestedThemeVariant
/// 2. Detecting and following system theme preferences
/// 3. Raising events when the theme changes
///
/// Modules can inject IThemeManager to respond to theme changes.
/// </remarks>
public interface IThemeManager
{
    /// <summary>
    /// Gets the currently selected theme mode.
    /// </summary>
    /// <remarks>
    /// This returns the user's preference (System/Dark/Light), not the
    /// effective theme. Use <see cref="GetEffectiveTheme"/> to get the
    /// actual Dark or Light theme being displayed.
    /// </remarks>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Raised when the theme changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: This event fires when:
    /// - SetTheme() is called with a different value
    /// - ToggleTheme() is called
    /// - The OS theme changes (when CurrentTheme is System)
    /// </remarks>
    event EventHandler<ThemeMode>? ThemeChanged;

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    /// <param name="mode">The theme mode to apply.</param>
    /// <remarks>
    /// LOGIC: This immediately updates Application.RequestedThemeVariant
    /// and raises the ThemeChanged event. All controls using DynamicResource
    /// bindings will automatically update.
    /// </remarks>
    void SetTheme(ThemeMode mode);

    /// <summary>
    /// Toggles between Dark and Light themes.
    /// </summary>
    /// <remarks>
    /// LOGIC: If current mode is System, this switches to the opposite of
    /// the current effective theme (e.g., if system is Dark, switches to Light).
    /// </remarks>
    void ToggleTheme();

    /// <summary>
    /// Gets the effective theme (resolves System to actual Dark or Light).
    /// </summary>
    /// <returns>Either <see cref="ThemeMode.Dark"/> or <see cref="ThemeMode.Light"/>.</returns>
    /// <remarks>
    /// LOGIC: This never returns System. It always returns the actual
    /// theme being displayed based on current settings and OS preference.
    /// </remarks>
    ThemeMode GetEffectiveTheme();
}
