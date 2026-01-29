namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents the effective theme being displayed by the application.
/// </summary>
/// <remarks>
/// LOGIC: Unlike <see cref="ThemeMode"/> which includes System as a user preference,
/// ThemeVariant represents the actual resolved theme (Light or Dark) that is applied.
/// When ThemeMode.System is selected, the ThemeVariant is resolved based on the OS theme.
///
/// Version: v0.1.6b
/// </remarks>
public enum ThemeVariant
{
    /// <summary>
    /// Light theme is applied.
    /// </summary>
    Light = 0,

    /// <summary>
    /// Dark theme is applied.
    /// </summary>
    Dark = 1
}
