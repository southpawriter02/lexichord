// <copyright file="ChartThemeConfiguration.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using SkiaSharp;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Configures LiveCharts themes to match Lexichord application themes.
/// </summary>
/// <remarks>
/// <para>LOGIC: Provides consistent color palettes for light and dark modes.</para>
/// <para>Colors are chosen to match Lexichord's design system.</para>
/// <para>Introduced in v0.3.5a.</para>
/// </remarks>
public static class ChartThemeConfiguration
{
    /// <summary>
    /// Light theme colors.
    /// </summary>
    public static class Light
    {
        /// <summary>Chart background color.</summary>
        public static readonly SKColor Background = SKColors.White;

        /// <summary>Grid line color (gray-200).</summary>
        public static readonly SKColor GridLines = new(229, 231, 235);

        /// <summary>Axis label color (gray-700).</summary>
        public static readonly SKColor AxisLabels = new(55, 65, 81);

        /// <summary>Current values fill color (blue 40% opacity).</summary>
        public static readonly SKColor CurrentFill = new(74, 158, 255, 100);

        /// <summary>Current values stroke color (blue).</summary>
        public static readonly SKColor CurrentStroke = new(74, 158, 255);

        /// <summary>Target values fill color (green 20% opacity).</summary>
        public static readonly SKColor TargetFill = new(34, 197, 94, 50);

        /// <summary>Target values stroke color (green).</summary>
        public static readonly SKColor TargetStroke = new(34, 197, 94);
    }

    /// <summary>
    /// Dark theme colors.
    /// </summary>
    public static class Dark
    {
        /// <summary>Chart background color (gray-800).</summary>
        public static readonly SKColor Background = new(31, 41, 55);

        /// <summary>Grid line color (gray-700).</summary>
        public static readonly SKColor GridLines = new(55, 65, 81);

        /// <summary>Axis label color (gray-300).</summary>
        public static readonly SKColor AxisLabels = new(209, 213, 219);

        /// <summary>Current values fill color (blue 60% opacity).</summary>
        public static readonly SKColor CurrentFill = new(74, 158, 255, 150);

        /// <summary>Current values stroke color (blue).</summary>
        public static readonly SKColor CurrentStroke = new(74, 158, 255);

        /// <summary>Target values fill color (green 30% opacity).</summary>
        public static readonly SKColor TargetFill = new(34, 197, 94, 75);

        /// <summary>Target values stroke color (green).</summary>
        public static readonly SKColor TargetStroke = new(34, 197, 94);
    }

    /// <summary>
    /// Gets theme colors based on current application theme.
    /// </summary>
    /// <param name="isDarkTheme">True if dark theme is active.</param>
    /// <returns>Color palette for the current theme.</returns>
    public static ChartColors GetColors(bool isDarkTheme) =>
        isDarkTheme
            ? new ChartColors(Dark.Background, Dark.GridLines, Dark.AxisLabels,
                Dark.CurrentFill, Dark.CurrentStroke, Dark.TargetFill, Dark.TargetStroke)
            : new ChartColors(Light.Background, Light.GridLines, Light.AxisLabels,
                Light.CurrentFill, Light.CurrentStroke, Light.TargetFill, Light.TargetStroke);
}

/// <summary>
/// Encapsulates a complete color palette for chart rendering.
/// </summary>
/// <param name="Background">Chart background color.</param>
/// <param name="GridLines">Grid line color.</param>
/// <param name="AxisLabels">Axis label text color.</param>
/// <param name="CurrentFill">Fill color for current values polygon.</param>
/// <param name="CurrentStroke">Stroke color for current values polygon.</param>
/// <param name="TargetFill">Fill color for target values polygon.</param>
/// <param name="TargetStroke">Stroke color for target values polygon.</param>
public record ChartColors(
    SKColor Background,
    SKColor GridLines,
    SKColor AxisLabels,
    SKColor CurrentFill,
    SKColor CurrentStroke,
    SKColor TargetFill,
    SKColor TargetStroke);
