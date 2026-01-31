// <copyright file="SpiderChartSeriesBuilder.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Lexichord.Abstractions.Contracts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Builds LiveCharts polar series for the Resonance Dashboard spider chart.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5b - Creates styled polar line series from chart data.</para>
/// <para>Uses <see cref="ChartThemeConfiguration"/> for consistent theme colors.</para>
/// <para>Thread-safe: no mutable state.</para>
/// </remarks>
public sealed partial class SpiderChartSeriesBuilder : ISpiderChartSeriesBuilder
{
    private readonly ILogger<SpiderChartSeriesBuilder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpiderChartSeriesBuilder"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SpiderChartSeriesBuilder(ILogger<SpiderChartSeriesBuilder> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public ISeries? BuildCurrentSeries(ResonanceChartData chartData, bool isDarkTheme = false)
    {
        if (chartData.DataPoints.Count == 0)
        {
            LogEmptyChartData("current");
            return null;
        }

        var colors = ChartThemeConfiguration.GetColors(isDarkTheme);

        LogBuildingSeries("current", chartData.DataPoints.Count, isDarkTheme);

        // LOGIC: Create a closed polygon with solid fill and stroke
        var series = new PolarLineSeries<double>
        {
            Values = chartData.GetNormalizedValues(),
            Name = "Current",
            LineSmoothness = 0, // Sharp corners for spider chart
            GeometrySize = 8,   // Point marker size
            Fill = new SolidColorPaint(colors.CurrentFill),
            Stroke = new SolidColorPaint(colors.CurrentStroke, 2),
            GeometryFill = new SolidColorPaint(colors.CurrentStroke),
            GeometryStroke = new SolidColorPaint(SKColors.White, 1),
            IsClosed = true
        };

        LogSeriesBuilt("current", chartData.DataPoints.Count);

        return series;
    }

    /// <inheritdoc/>
    public ISeries? BuildTargetSeries(
        ResonanceChartData targetData,
        string profileName,
        bool isDarkTheme = false)
    {
        if (targetData.DataPoints.Count == 0)
        {
            LogEmptyChartData("target");
            return null;
        }

        var colors = ChartThemeConfiguration.GetColors(isDarkTheme);

        LogBuildingSeries("target", targetData.DataPoints.Count, isDarkTheme);

        // LOGIC: Create a dashed polygon with subtle fill, no point markers
        var series = new PolarLineSeries<double>
        {
            Values = targetData.GetNormalizedValues(),
            Name = $"Target ({profileName})",
            LineSmoothness = 0, // Sharp corners for spider chart
            GeometrySize = 0,   // No point markers for target overlay
            Fill = new SolidColorPaint(colors.TargetFill),
            Stroke = new SolidColorPaint(colors.TargetStroke, 1)
            {
                PathEffect = new DashEffect([5f, 5f]) // Dashed line pattern
            },
            IsClosed = true
        };

        LogSeriesBuilt("target", targetData.DataPoints.Count);

        return series;
    }

    // LOGIC: Source-generated logging for performance
    [LoggerMessage(Level = LogLevel.Debug, Message = "Building {SeriesType} series with {PointCount} data points, dark theme: {IsDarkTheme}")]
    private partial void LogBuildingSeries(string seriesType, int pointCount, bool isDarkTheme);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Built {SeriesType} series with {PointCount} points")]
    private partial void LogSeriesBuilt(string seriesType, int pointCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Empty chart data provided for {SeriesType} series, returning null")]
    private partial void LogEmptyChartData(string seriesType);
}
