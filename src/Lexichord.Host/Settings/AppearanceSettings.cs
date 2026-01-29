namespace Lexichord.Host.Settings;

/// <summary>
/// Settings record for appearance preferences.
/// </summary>
/// <remarks>
/// LOGIC: Persisted via ISettingsService to maintain theme preferences across sessions.
///
/// Version: v0.1.6b
/// </remarks>
public record AppearanceSettings
{
    /// <summary>
    /// Gets or sets the theme preference (Light, Dark, or System).
    /// </summary>
    public string Theme { get; init; } = "System";

    /// <summary>
    /// Gets or sets whether to use the system accent color.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reserved for future implementation. When true, UI accent colors
    /// will match the OS accent color preference.
    /// </remarks>
    public bool UseSystemAccentColor { get; init; } = true;

    /// <summary>
    /// Gets or sets the UI scale factor.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reserved for future implementation. Values typically range from 0.75 to 2.0.
    /// Default of 1.0 means 100% scale.
    /// </remarks>
    public double UiScale { get; init; } = 1.0;
}
