namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Provides underline colors for style violations based on severity.
/// </summary>
/// <remarks>
/// LOGIC: IViolationColorProvider abstracts the color mapping from the
/// renderer, enabling theme-aware colors in the future.
///
/// Default Colors (matching VS Code/Roslyn conventions):
/// - Error: Red (#E51400) - Critical issues that must be fixed
/// - Warning: Yellow/Orange (#FFC000) - Advisory issues
/// - Info: Blue (#1E90FF) - Informational messages
/// - Hint: Gray (#A0A0A0) - Subtle suggestions
///
/// Future: Could read colors from IThemeManager for dark/light mode support.
///
/// Version: v0.2.4a
/// </remarks>
public interface IViolationColorProvider
{
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
}
