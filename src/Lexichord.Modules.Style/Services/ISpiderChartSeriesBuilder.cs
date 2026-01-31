// <copyright file="ISpiderChartSeriesBuilder.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Lexichord.Abstractions.Contracts;
using LiveChartsCore;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Defines the contract for building LiveCharts series for the Resonance spider chart.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5b - Abstracts chart series construction for testability.</para>
/// <para>Separates data preparation from presentation layer concerns.</para>
/// </remarks>
public interface ISpiderChartSeriesBuilder
{
    /// <summary>
    /// Builds a series representing the current metric values.
    /// </summary>
    /// <param name="chartData">The aggregated chart data.</param>
    /// <param name="isDarkTheme">Whether dark theme colors should be used.</param>
    /// <returns>A series for the chart, or null if data is empty.</returns>
    ISeries? BuildCurrentSeries(ResonanceChartData chartData, bool isDarkTheme = false);

    /// <summary>
    /// Builds a series representing the target overlay from a voice profile.
    /// </summary>
    /// <param name="targetData">The target overlay data as chart data.</param>
    /// <param name="profileName">Name of the profile for the legend.</param>
    /// <param name="isDarkTheme">Whether dark theme colors should be used.</param>
    /// <returns>A series for the overlay, or null if data is empty.</returns>
    ISeries? BuildTargetSeries(ResonanceChartData targetData, string profileName, bool isDarkTheme = false);
}
