// =============================================================================
// File: LicenseTooltipConverter.cs
// Project: Lexichord.Modules.RAG
// Description: Value converter for license-aware tooltip text on expand buttons.
// =============================================================================
// LOGIC: Returns appropriate tooltip text based on license state.
//   - When licensed: Returns "Expand to show context"
//   - When not licensed: Returns "Upgrade to Writer Pro to expand context"
//   Expects a boolean value indicating whether the user is licensed.
// =============================================================================
// VERSION: v0.5.3d (Context Preview UI)
// =============================================================================

using System.Globalization;
using Avalonia.Data.Converters;

namespace Lexichord.Modules.RAG.Converters;

/// <summary>
/// Converts a license state boolean to appropriate tooltip text for expand buttons.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="LicenseTooltipConverter"/> provides context-appropriate tooltip text
/// based on whether the user has access to the Context Expansion feature (Writer Pro+).
/// </para>
/// <para>
/// <b>Usage in XAML:</b>
/// <code>
/// ToolTip.Tip="{Binding IsLicensed,
///     Converter={StaticResource LicenseTooltipConverter}}"
/// </code>
/// </para>
/// <para>
/// <b>Return Values:</b>
/// <list type="bullet">
///   <item><description>
///     <c>true</c>: Returns "Expand to show context"
///   </description></item>
///   <item><description>
///     <c>false</c>: Returns "Upgrade to Writer Pro to expand context"
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3d for context preview expand button tooltips.
/// </para>
/// </remarks>
public class LicenseTooltipConverter : IValueConverter
{
    /// <summary>
    /// Tooltip text shown when the user has the required license.
    /// </summary>
    public const string LicensedTooltip = "Expand to show context";

    /// <summary>
    /// Tooltip text shown when the user does not have the required license.
    /// </summary>
    public const string UnlicensedTooltip = "Upgrade to Writer Pro to expand context";

    /// <summary>
    /// Converts a boolean license state to appropriate tooltip text.
    /// </summary>
    /// <param name="value">
    /// The boolean indicating license state.
    /// <c>true</c> if licensed, <c>false</c> if not licensed.
    /// </param>
    /// <param name="targetType">The target type (should be string).</param>
    /// <param name="parameter">Not used.</param>
    /// <param name="culture">Culture info (not used).</param>
    /// <returns>
    /// <see cref="LicensedTooltip"/> if <paramref name="value"/> is <c>true</c>;
    /// <see cref="UnlicensedTooltip"/> if <paramref name="value"/> is <c>false</c> or null.
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isLicensed && isLicensed)
        {
            return LicensedTooltip;
        }

        return UnlicensedTooltip;
    }

    /// <summary>
    /// Not implemented. Reverse conversion is not supported.
    /// </summary>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
