namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Provides underline colors for style violations based on severity.
/// </summary>
/// <remarks>
/// LOGIC: IViolationColorProvider abstracts the color mapping from the
/// renderer, enabling theme-aware colors for light/dark mode support.
///
/// Light Theme Palette (high contrast on white):
/// - Error: Red (#E51400) - Critical issues that must be fixed
/// - Warning: Orange (#F0A30A) - Advisory issues
/// - Info: Blue (#0078D4) - Informational messages
/// - Hint: Gray (#808080) - Subtle suggestions
///
/// Dark Theme Palette (softer on dark backgrounds):
/// - Error: Salmon Red (#FF6B6B) - Visible but not harsh
/// - Warning: Light Orange (#FFB347) - Warm and visible
/// - Info: Light Blue (#4FC3F7) - Cool and visible
/// - Hint: Light Gray (#B0B0B0) - Subtle suggestions
///
/// Version: v0.2.4b - Added theme-aware methods
/// </remarks>
public interface IViolationColorProvider
{
    /// <summary>
    /// Sets the current theme for color selection.
    /// </summary>
    /// <param name="theme">The theme variant to use.</param>
    /// <remarks>
    /// LOGIC: Called by the renderer when the theme changes.
    /// Colors returned by subsequent calls will match the new theme.
    /// </remarks>
    void SetTheme(ThemeVariant theme);

    /// <summary>
    /// Gets the underline color for a violation severity.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>The color to use for the wavy underline.</returns>
    /// <remarks>
    /// LOGIC: Called by StyleViolationRenderer during ColorizeLine
    /// to determine what color underline to register.
    /// </remarks>
    UnderlineColor GetUnderlineColor(ViolationSeverity severity);

    /// <summary>
    /// Gets the semi-transparent background color for highlighting violations.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>A semi-transparent color for background highlighting, or null if no background.</returns>
    /// <remarks>
    /// LOGIC: Used for optional background highlighting of violation ranges.
    /// Returns colors with ~12-19% opacity depending on theme.
    /// </remarks>
    UnderlineColor? GetBackgroundColor(ViolationSeverity severity);

    /// <summary>
    /// Gets the border color for violation tooltips.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>The color to use for tooltip borders.</returns>
    /// <remarks>
    /// LOGIC: Tooltips should have borders matching the underline color
    /// for visual consistency.
    /// </remarks>
    UnderlineColor GetTooltipBorderColor(ViolationSeverity severity);

    /// <summary>
    /// Gets the SVG path data for a severity icon.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>Material Design icon path data string.</returns>
    /// <remarks>
    /// LOGIC: Returns pre-defined Material Design icon paths for each severity:
    /// - Error: X in circle
    /// - Warning: Triangle with exclamation
    /// - Info: Circle with checkmark
    /// - Hint: Simple circle
    /// </remarks>
    string GetSeverityIcon(ViolationSeverity severity);
}

