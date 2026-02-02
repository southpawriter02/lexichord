// =============================================================================
// File: ConfidenceToColorConverter.cs
// Project: Lexichord.Modules.Knowledge
// Description: Value converter mapping confidence scores to color strings.
// =============================================================================
// LOGIC: Converts a float confidence value (0.0-1.0) to a color string
//   for visual indication of extraction confidence in the Entity List View.
//
// Color Thresholds:
//   >= 0.9: Green (#22c55e) — High confidence
//   >= 0.7: Yellow (#eab308) — Medium-high confidence
//   >= 0.5: Orange (#f97316) — Medium confidence
//   <  0.5: Red (#ef4444) — Low confidence
//
// v0.4.7e: Entity List View (Knowledge Graph Browser)
// Dependencies: Avalonia.Data.Converters
// =============================================================================

using System.Globalization;
using Avalonia.Data.Converters;

namespace Lexichord.Modules.Knowledge.UI.Converters;

/// <summary>
/// Converts a confidence score to a color string for visual display.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ConfidenceToColorConverter"/> maps extraction confidence scores
/// to intuitive colors: green for high confidence, yellow for medium-high,
/// orange for medium, and red for low confidence.
/// </para>
/// <para>
/// <b>Usage:</b> Bind to the Confidence property of <see cref="EntityListItemViewModel"/>
/// with this converter to color the confidence badge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7e as part of the Entity List View.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;Border Background="{Binding Confidence, Converter={StaticResource ConfidenceToColor}}"&gt;
///     &lt;TextBlock Text="{Binding ConfidenceDisplay}"/&gt;
/// &lt;/Border&gt;
/// </code>
/// </example>
public sealed class ConfidenceToColorConverter : IValueConverter
{
    // LOGIC: Pre-defined color hex codes for each confidence tier.
    private const string GreenColor = "#22c55e";   // High confidence >= 0.9
    private const string YellowColor = "#eab308";  // Medium-high >= 0.7
    private const string OrangeColor = "#f97316";  // Medium >= 0.5
    private const string RedColor = "#ef4444";     // Low < 0.5
    private const string GrayColor = "#6b7280";    // Invalid/unknown

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not float confidence)
            return GrayColor;

        // LOGIC: Map confidence to color based on thresholds.
        // Thresholds align with common confidence tier conventions.
        return confidence switch
        {
            >= 0.9f => GreenColor,   // High confidence
            >= 0.7f => YellowColor,  // Medium-high confidence
            >= 0.5f => OrangeColor,  // Medium confidence
            _ => RedColor             // Low confidence
        };
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // LOGIC: One-way converter; ConvertBack is not supported.
        throw new NotSupportedException("ConfidenceToColorConverter is a one-way converter.");
    }
}
