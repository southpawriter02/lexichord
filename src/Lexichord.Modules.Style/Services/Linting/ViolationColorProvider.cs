using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Provides underline colors for style violations based on severity.
/// </summary>
/// <remarks>
/// LOGIC: Maps ViolationSeverity to colors following IDE conventions:
/// - Error: Red - Critical issues that must be fixed
/// - Warning: Yellow/Orange - Advisory issues
/// - Info: Blue - Informational messages
/// - Hint: Gray - Subtle suggestions
///
/// Color values are chosen to be visible on both light and dark backgrounds,
/// matching common IDE color schemes (VS Code, Visual Studio, JetBrains).
///
/// Version: v0.2.4a
/// </remarks>
public sealed class ViolationColorProvider : IViolationColorProvider
{
    /// <inheritdoc />
    public UnderlineColor GetUnderlineColor(ViolationSeverity severity)
    {
        return severity switch
        {
            ViolationSeverity.Error => UnderlineColor.ErrorRed,
            ViolationSeverity.Warning => UnderlineColor.WarningYellow,
            ViolationSeverity.Info => UnderlineColor.InfoBlue,
            ViolationSeverity.Hint => UnderlineColor.HintGray,
            _ => UnderlineColor.HintGray // Fallback for future severity levels
        };
    }
}
