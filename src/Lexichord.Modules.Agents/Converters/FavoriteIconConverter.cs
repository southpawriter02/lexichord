// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using System.Globalization;
using Avalonia.Data.Converters;

namespace Lexichord.Modules.Agents.Converters;

/// <summary>
/// Converts a boolean favorite status to the appropriate star icon path data.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Provides visual feedback for favorite/non-favorite status
/// by converting boolean values to SVG path data for star icons.
/// </para>
/// <para>
/// <strong>Conversion Logic:</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see langword="true"/> — Filled star icon (favorite).</description></item>
///   <item><description><see langword="false"/> — Outline star icon (not favorite).</description></item>
/// </list>
/// <para>
/// <strong>Usage:</strong> Bind to boolean properties like <c>IsFavorite</c> in
/// <see cref="ViewModels.AgentItemViewModel"/> to display the correct star icon.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// &lt;PathIcon Data="{Binding IsFavorite, Converter={StaticResource FavoriteIconConverter}}"
///           Width="16" Height="16" /&gt;
/// </code>
/// <para>
/// <strong>Introduced in:</strong> v0.7.1d as part of the Agent Selector UI feature.
/// </para>
/// </remarks>
public sealed class FavoriteIconConverter : IValueConverter
{
    /// <summary>
    /// Gets a shared singleton instance of the converter.
    /// </summary>
    /// <remarks>
    /// <strong>LOGIC:</strong> Singleton pattern to avoid creating multiple instances
    /// of the converter, as it has no state and can be safely reused.
    /// </remarks>
    public static FavoriteIconConverter Instance { get; } = new();

    /// <summary>
    /// SVG path data for a filled star icon (favorite).
    /// </summary>
    /// <remarks>
    /// <strong>LOGIC:</strong> This is the Material Design star icon in filled state,
    /// suitable for 24x24 viewBox. Scaled appropriately by Avalonia's PathIcon.
    /// </remarks>
    private const string FilledStarPath = "M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z";

    /// <summary>
    /// SVG path data for an outline star icon (not favorite).
    /// </summary>
    /// <remarks>
    /// <strong>LOGIC:</strong> This is the Material Design star outline icon,
    /// showing only the border of the star to indicate non-favorite status.
    /// </remarks>
    private const string OutlineStarPath = "M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z M12 5.5l-1.55 3.14-3.46.5 2.5 2.44-.59 3.45L12 13.39l3.09 1.63-.59-3.45 2.5-2.44-3.46-.5L12 5.5z";

    /// <summary>
    /// Converts a boolean favorite status to star icon path data.
    /// </summary>
    /// <param name="value">
    /// The boolean value to convert. Expected type: <see cref="bool"/>.
    /// </param>
    /// <param name="targetType">The target type (expected: <see cref="string"/>).</param>
    /// <param name="parameter">Optional parameter (unused).</param>
    /// <param name="culture">The culture info (unused).</param>
    /// <returns>
    /// A string containing SVG path data:
    /// <list type="bullet">
    ///   <item><description><see langword="true"/> — <see cref="FilledStarPath"/>.</description></item>
    ///   <item><description><see langword="false"/> — <see cref="OutlineStarPath"/>.</description></item>
    ///   <item><description><see langword="null"/> or non-boolean — <see cref="OutlineStarPath"/> (fallback).</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <strong>LOGIC:</strong> Defaults to outline star if the input is not a valid boolean,
    /// providing a safe fallback for design-time or invalid bindings.
    /// </remarks>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // LOGIC: Convert boolean to appropriate star icon path data.
        if (value is bool isFavorite)
        {
            return isFavorite ? FilledStarPath : OutlineStarPath;
        }

        // LOGIC: Fallback to outline star for null or non-boolean values.
        return OutlineStarPath;
    }

    /// <summary>
    /// Not implemented - this is a one-way converter.
    /// </summary>
    /// <param name="value">The value to convert back.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Optional parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>Not applicable.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    /// <remarks>
    /// <strong>LOGIC:</strong> This converter is designed for one-way binding only
    /// (reading boolean to display icon). Reverse conversion (icon to boolean) is not meaningful.
    /// </remarks>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("FavoriteIconConverter does not support ConvertBack.");
    }
}
