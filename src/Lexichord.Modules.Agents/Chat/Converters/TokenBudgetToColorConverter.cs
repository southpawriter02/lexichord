// -----------------------------------------------------------------------
// <copyright file="TokenBudgetToColorConverter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Lexichord.Modules.Agents.Chat.Converters;

/// <summary>
/// Converts token budget percentage to a color for visual feedback.
/// </summary>
/// <remarks>
/// <para>
/// This converter provides a traffic-light style color scheme based on the
/// percentage of token budget consumed:
/// </para>
/// <list type="bullet">
///   <item><description>0-70%: Green (healthy budget)</description></item>
///   <item><description>71-90%: Yellow/Orange (approaching limit)</description></item>
///   <item><description>91-100%: Red (at or over limit)</description></item>
/// </list>
/// <para>
/// The converter returns an <see cref="ISolidColorBrush"/> for use with
/// Avalonia UI elements.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;ProgressBar
///     Value="{Binding TokenBudgetPercentage}"
///     Foreground="{Binding TokenBudgetPercentage,
///         Converter={StaticResource TokenBudgetToColorConverter}}" /&gt;
/// </code>
/// </example>
/// <seealso cref="ContextPanelViewModel"/>
public sealed class TokenBudgetToColorConverter : IValueConverter
{
    #region Constants

    /// <summary>
    /// Threshold for warning state (orange/yellow).
    /// </summary>
    private const int WarningThreshold = 70;

    /// <summary>
    /// Threshold for danger state (red).
    /// </summary>
    private const int DangerThreshold = 90;

    #endregion

    #region Static Properties

    /// <summary>
    /// Gets a shared singleton instance of the converter.
    /// </summary>
    public static TokenBudgetToColorConverter Instance { get; } = new();

    #endregion

    #region Color Properties

    /// <summary>
    /// Gets or sets the color for healthy budget usage (0-70%).
    /// </summary>
    /// <value>Defaults to <c>#10B981</c> (Emerald green).</value>
    public ISolidColorBrush HealthyColor { get; set; } = new SolidColorBrush(Color.Parse("#10B981"));

    /// <summary>
    /// Gets or sets the color for warning budget usage (71-90%).
    /// </summary>
    /// <value>Defaults to <c>#F59E0B</c> (Amber orange).</value>
    public ISolidColorBrush WarningColor { get; set; } = new SolidColorBrush(Color.Parse("#F59E0B"));

    /// <summary>
    /// Gets or sets the color for danger budget usage (91-100%+).
    /// </summary>
    /// <value>Defaults to <c>#EF4444</c> (Red).</value>
    public ISolidColorBrush DangerColor { get; set; } = new SolidColorBrush(Color.Parse("#EF4444"));

    /// <summary>
    /// Gets or sets the fallback color when conversion fails.
    /// </summary>
    /// <value>Defaults to <c>#6B7280</c> (Gray).</value>
    public ISolidColorBrush FallbackColor { get; set; } = new SolidColorBrush(Color.Parse("#6B7280"));

    #endregion

    #region IValueConverter

    /// <summary>
    /// Converts a token budget percentage to a color brush.
    /// </summary>
    /// <param name="value">The percentage value (0-100 or higher).</param>
    /// <param name="targetType">The target type (expected: IBrush).</param>
    /// <param name="parameter">Optional parameter (unused).</param>
    /// <param name="culture">The culture info (unused).</param>
    /// <returns>
    /// An <see cref="ISolidColorBrush"/> representing the budget state:
    /// green for healthy, orange for warning, red for danger.
    /// </returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Validate input type
        if (value is not int percentage)
        {
            // Try to parse from other numeric types
            if (value is double d)
            {
                percentage = (int)d;
            }
            else if (value is float f)
            {
                percentage = (int)f;
            }
            else
            {
                return FallbackColor;
            }
        }

        // Apply traffic-light color scheme
        return percentage switch
        {
            <= WarningThreshold => HealthyColor,
            <= DangerThreshold => WarningColor,
            _ => DangerColor
        };
    }

    /// <summary>
    /// Not implemented - this is a one-way converter.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("TokenBudgetToColorConverter does not support ConvertBack.");
    }

    #endregion
}
