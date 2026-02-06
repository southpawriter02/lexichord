// -----------------------------------------------------------------------
// <copyright file="RelevanceToColorConverter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Lexichord.Modules.Agents.Chat.ViewModels;

namespace Lexichord.Modules.Agents.Chat.Converters;

/// <summary>
/// Converts RAG chunk relevance tier or score to a color for visual feedback.
/// </summary>
/// <remarks>
/// <para>
/// This converter provides relevance-based coloring for RAG chunk display:
/// </para>
/// <list type="bullet">
///   <item><description>VeryHigh (0.85+): Deep green - highly relevant</description></item>
///   <item><description>High (0.70-0.84): Green - relevant</description></item>
///   <item><description>Medium (0.50-0.69): Yellow - moderately relevant</description></item>
///   <item><description>Low (0.30-0.49): Orange - marginally relevant</description></item>
///   <item><description>VeryLow (&lt;0.30): Gray - low relevance</description></item>
/// </list>
/// <para>
/// The converter accepts either a <see cref="RelevanceTier"/> enum value or
/// a float/double relevance score (0.0-1.0).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;Border Background="{Binding RelevanceTier,
///     Converter={StaticResource RelevanceToColorConverter}}"&gt;
///     &lt;TextBlock Text="{Binding Summary}" /&gt;
/// &lt;/Border&gt;
/// </code>
/// </example>
/// <seealso cref="RagChunkContextItem"/>
/// <seealso cref="RelevanceTier"/>
public sealed class RelevanceToColorConverter : IValueConverter
{
    #region Static Properties

    /// <summary>
    /// Gets a shared singleton instance of the converter.
    /// </summary>
    public static RelevanceToColorConverter Instance { get; } = new();

    #endregion

    #region Color Properties

    /// <summary>
    /// Gets or sets the color for VeryHigh relevance (0.85+).
    /// </summary>
    /// <value>Defaults to <c>#059669</c> (Deep emerald).</value>
    public ISolidColorBrush VeryHighColor { get; set; } = new SolidColorBrush(Color.Parse("#059669"));

    /// <summary>
    /// Gets or sets the color for High relevance (0.70-0.84).
    /// </summary>
    /// <value>Defaults to <c>#10B981</c> (Emerald green).</value>
    public ISolidColorBrush HighColor { get; set; } = new SolidColorBrush(Color.Parse("#10B981"));

    /// <summary>
    /// Gets or sets the color for Medium relevance (0.50-0.69).
    /// </summary>
    /// <value>Defaults to <c>#FBBF24</c> (Amber yellow).</value>
    public ISolidColorBrush MediumColor { get; set; } = new SolidColorBrush(Color.Parse("#FBBF24"));

    /// <summary>
    /// Gets or sets the color for Low relevance (0.30-0.49).
    /// </summary>
    /// <value>Defaults to <c>#F97316</c> (Orange).</value>
    public ISolidColorBrush LowColor { get; set; } = new SolidColorBrush(Color.Parse("#F97316"));

    /// <summary>
    /// Gets or sets the color for VeryLow relevance (&lt;0.30).
    /// </summary>
    /// <value>Defaults to <c>#9CA3AF</c> (Gray).</value>
    public ISolidColorBrush VeryLowColor { get; set; } = new SolidColorBrush(Color.Parse("#9CA3AF"));

    /// <summary>
    /// Gets or sets the fallback color when conversion fails.
    /// </summary>
    /// <value>Defaults to <c>#6B7280</c> (Dark gray).</value>
    public ISolidColorBrush FallbackColor { get; set; } = new SolidColorBrush(Color.Parse("#6B7280"));

    #endregion

    #region IValueConverter

    /// <summary>
    /// Converts a relevance tier or score to a color brush.
    /// </summary>
    /// <param name="value">
    /// The relevance value. Can be:
    /// <list type="bullet">
    ///   <item><description><see cref="RelevanceTier"/> enum value</description></item>
    ///   <item><description>Float/double score (0.0-1.0)</description></item>
    ///   <item><description>Integer percentage (0-100)</description></item>
    /// </list>
    /// </param>
    /// <param name="targetType">The target type (expected: IBrush).</param>
    /// <param name="parameter">Optional parameter (unused).</param>
    /// <param name="culture">The culture info (unused).</param>
    /// <returns>
    /// An <see cref="ISolidColorBrush"/> representing the relevance level.
    /// </returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Handle RelevanceTier enum
        if (value is RelevanceTier tier)
        {
            return tier switch
            {
                RelevanceTier.VeryHigh => VeryHighColor,
                RelevanceTier.High => HighColor,
                RelevanceTier.Medium => MediumColor,
                RelevanceTier.Low => LowColor,
                RelevanceTier.VeryLow => VeryLowColor,
                _ => FallbackColor
            };
        }

        // Handle numeric scores
        float score;

        if (value is float f)
        {
            score = f;
        }
        else if (value is double d)
        {
            score = (float)d;
        }
        else if (value is int i)
        {
            // Assume it's a percentage (0-100)
            score = i / 100f;
        }
        else
        {
            return FallbackColor;
        }

        // Map score to tier color
        return score switch
        {
            >= 0.85f => VeryHighColor,
            >= 0.70f => HighColor,
            >= 0.50f => MediumColor,
            >= 0.30f => LowColor,
            _ => VeryLowColor
        };
    }

    /// <summary>
    /// Not implemented - this is a one-way converter.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("RelevanceToColorConverter does not support ConvertBack.");
    }

    #endregion
}
