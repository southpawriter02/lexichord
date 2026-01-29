namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event arguments for theme change notifications.
/// </summary>
/// <remarks>
/// LOGIC: Provides both the user's preference (ThemeMode) and the resolved
/// effective theme (ThemeVariant) to allow handlers to react appropriately.
///
/// Version: v0.1.6b
/// </remarks>
public sealed class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous theme mode.
    /// </summary>
    public ThemeMode OldTheme { get; }

    /// <summary>
    /// Gets the new theme mode.
    /// </summary>
    public ThemeMode NewTheme { get; }

    /// <summary>
    /// Gets the effective theme variant that is now applied.
    /// </summary>
    /// <remarks>
    /// LOGIC: When NewTheme is System, this reflects the actual OS theme.
    /// </remarks>
    public ThemeVariant EffectiveTheme { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldTheme">The previous theme mode.</param>
    /// <param name="newTheme">The new theme mode.</param>
    /// <param name="effectiveTheme">The effective theme variant now applied.</param>
    public ThemeChangedEventArgs(
        ThemeMode oldTheme,
        ThemeMode newTheme,
        ThemeVariant effectiveTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
        EffectiveTheme = effectiveTheme;
    }
}
