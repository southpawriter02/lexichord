namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Specifies the theme mode for the Lexichord application.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Follow the operating system's theme preference.
    /// </summary>
    /// <remarks>
    /// LOGIC: When System is selected, the application subscribes to OS theme change
    /// events and automatically switches when the user changes their system preference.
    /// </remarks>
    System = 0,

    /// <summary>
    /// Force dark theme regardless of system settings.
    /// </summary>
    Dark = 1,

    /// <summary>
    /// Force light theme regardless of system settings.
    /// </summary>
    Light = 2
}
