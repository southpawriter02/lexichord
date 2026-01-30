using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Provides theme-aware underline colors for style violations based on severity.
/// </summary>
/// <remarks>
/// LOGIC: ViolationColorProvider maps ViolationSeverity to colors,
/// with different palettes for light and dark themes.
///
/// Light Theme Palette (high contrast on white):
/// - Error: Red (#E51400) - High urgency
/// - Warning: Orange (#F0A30A) - Medium urgency
/// - Info: Blue (#0078D4) - Low urgency/suggestion
/// - Hint: Gray (#808080) - Subtle suggestions
///
/// Dark Theme Palette (softer on dark backgrounds):
/// - Error: Salmon Red (#FF6B6B) - Visible but not harsh
/// - Warning: Light Orange (#FFB347) - Warm and visible
/// - Info: Light Blue (#4FC3F7) - Cool and visible
/// - Hint: Light Gray (#B0B0B0) - Subtle suggestions
///
/// Colors are chosen for:
/// 1. Visibility against background
/// 2. Consistency with industry conventions (VS Code, Visual Studio)
/// 3. Accessibility (sufficient contrast ratios)
///
/// Version: v0.2.4b - Theme-aware implementation
/// </remarks>
public sealed class ViolationColorProvider : IViolationColorProvider
{
    private ThemeVariant _currentTheme = ThemeVariant.Light;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViolationColorProvider"/> class.
    /// </summary>
    public ViolationColorProvider()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViolationColorProvider"/> class
    /// with an initial theme from the theme manager.
    /// </summary>
    /// <param name="themeManager">The theme manager for initial theme detection.</param>
    public ViolationColorProvider(IThemeManager themeManager)
    {
        ArgumentNullException.ThrowIfNull(themeManager);
        _currentTheme = themeManager.EffectiveTheme;
    }

    /// <inheritdoc />
    public void SetTheme(ThemeVariant theme)
    {
        _currentTheme = theme;
    }

    /// <inheritdoc />
    public UnderlineColor GetUnderlineColor(ViolationSeverity severity)
    {
        return _currentTheme == ThemeVariant.Dark
            ? GetDarkUnderlineColor(severity)
            : GetLightUnderlineColor(severity);
    }

    /// <inheritdoc />
    public UnderlineColor? GetBackgroundColor(ViolationSeverity severity)
    {
        return _currentTheme == ThemeVariant.Dark
            ? GetDarkBackgroundColor(severity)
            : GetLightBackgroundColor(severity);
    }

    /// <inheritdoc />
    public UnderlineColor GetTooltipBorderColor(ViolationSeverity severity)
    {
        // LOGIC: Tooltip border uses the same color as the underline
        return GetUnderlineColor(severity);
    }

    /// <inheritdoc />
    public string GetSeverityIcon(ViolationSeverity severity)
    {
        // LOGIC: Return Material Design icon paths
        return severity switch
        {
            ViolationSeverity.Error =>
                "M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z",
            ViolationSeverity.Warning =>
                "M12,2L1,21H23M12,6L19.53,19H4.47M11,10V14H13V10M11,16V18H13V16",
            ViolationSeverity.Info =>
                "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M11,16.5L6.5,12L7.91,10.59L11,13.67L16.59,8.09L18,9.5L11,16.5Z",
            ViolationSeverity.Hint =>
                "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M11,17H13V11H11V17M11,9H13V7H11V9Z",
            _ =>
                "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z"
        };
    }

    /// <summary>
    /// Gets the light theme underline color for a severity.
    /// </summary>
    private static UnderlineColor GetLightUnderlineColor(ViolationSeverity severity)
    {
        return severity switch
        {
            ViolationSeverity.Error => UnderlineColor.LightError,
            ViolationSeverity.Warning => UnderlineColor.LightWarning,
            ViolationSeverity.Info => UnderlineColor.LightInfo,
            ViolationSeverity.Hint => UnderlineColor.LightHint,
            _ => UnderlineColor.LightHint
        };
    }

    /// <summary>
    /// Gets the dark theme underline color for a severity.
    /// </summary>
    private static UnderlineColor GetDarkUnderlineColor(ViolationSeverity severity)
    {
        return severity switch
        {
            ViolationSeverity.Error => UnderlineColor.DarkError,
            ViolationSeverity.Warning => UnderlineColor.DarkWarning,
            ViolationSeverity.Info => UnderlineColor.DarkInfo,
            ViolationSeverity.Hint => UnderlineColor.DarkHint,
            _ => UnderlineColor.DarkHint
        };
    }

    /// <summary>
    /// Gets the light theme background color for a severity.
    /// </summary>
    private static UnderlineColor GetLightBackgroundColor(ViolationSeverity severity)
    {
        return severity switch
        {
            ViolationSeverity.Error => UnderlineColor.LightErrorBackground,
            ViolationSeverity.Warning => UnderlineColor.LightWarningBackground,
            ViolationSeverity.Info => UnderlineColor.LightInfoBackground,
            _ => UnderlineColor.LightInfoBackground
        };
    }

    /// <summary>
    /// Gets the dark theme background color for a severity.
    /// </summary>
    private static UnderlineColor GetDarkBackgroundColor(ViolationSeverity severity)
    {
        return severity switch
        {
            ViolationSeverity.Error => UnderlineColor.DarkErrorBackground,
            ViolationSeverity.Warning => UnderlineColor.DarkWarningBackground,
            ViolationSeverity.Info => UnderlineColor.DarkInfoBackground,
            _ => UnderlineColor.DarkInfoBackground
        };
    }
}
