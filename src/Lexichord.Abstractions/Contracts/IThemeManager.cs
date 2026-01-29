namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Manages application theme switching and persistence.
/// </summary>
/// <remarks>
/// LOGIC: The ThemeManager is responsible for:
/// 1. Setting the application's RequestedThemeVariant
/// 2. Detecting and following system theme preferences
/// 3. Raising events when the theme changes
/// 4. Publishing MediatR notifications for cross-module awareness
///
/// Modules can inject IThemeManager to respond to theme changes.
///
/// Version: v0.1.6b - Updated interface with async methods and ThemeVariant support.
/// </remarks>
public interface IThemeManager
{
    /// <summary>
    /// Gets the currently selected theme mode (user preference).
    /// </summary>
    /// <remarks>
    /// This returns the user's preference (System/Dark/Light), not the
    /// effective theme. Use <see cref="EffectiveTheme"/> to get the
    /// actual Dark or Light theme being displayed.
    /// </remarks>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Gets the effective theme variant being displayed.
    /// </summary>
    /// <remarks>
    /// LOGIC: When CurrentTheme is System, this reflects the actual OS theme.
    /// This never returns a System variant - always Light or Dark.
    /// </remarks>
    ThemeVariant EffectiveTheme { get; }

    /// <summary>
    /// Raised when the theme changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: This event fires when:
    /// - SetThemeAsync() is called with a different value
    /// - The OS theme changes (when CurrentTheme is System)
    ///
    /// The event args contain both the theme mode and effective variant.
    /// </remarks>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Sets the application theme asynchronously.
    /// </summary>
    /// <param name="theme">The theme mode to apply.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: This immediately updates Application.RequestedThemeVariant
    /// and raises the ThemeChanged event. All controls using DynamicResource
    /// bindings will automatically update. Publishes ThemeChangedEvent via MediatR.
    /// </remarks>
    Task SetThemeAsync(ThemeMode theme);

    /// <summary>
    /// Gets the current system theme preference.
    /// </summary>
    /// <returns>The current OS theme as <see cref="ThemeVariant"/>.</returns>
    /// <remarks>
    /// LOGIC: Queries the operating system's current theme preference.
    /// Used to resolve ThemeMode.System to an actual Light or Dark variant.
    /// </remarks>
    ThemeVariant GetSystemTheme();
}
