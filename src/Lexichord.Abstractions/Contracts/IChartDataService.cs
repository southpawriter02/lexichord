// <copyright file="IChartDataService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for aggregating and normalizing chart data from various analysis sources.
/// </summary>
/// <remarks>
/// <para>LOGIC: Central service for the Resonance Dashboard charting infrastructure.</para>
/// <para>Data is cached until explicitly invalidated or source metrics change.</para>
/// <para>All values are normalized to 0-100 scale for consistent chart rendering.</para>
/// <para>Introduced in v0.3.5a.</para>
/// </remarks>
public interface IChartDataService
{
    /// <summary>
    /// Gets the current chart data with all axis values normalized.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete chart data ready for rendering.</returns>
    /// <remarks>
    /// LOGIC: Returns cached data if available. Cache is invalidated when
    /// source metrics change or <see cref="InvalidateCache"/> is called.
    /// </remarks>
    Task<ResonanceChartData> GetChartDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cached chart data, forcing recomputation on next request.
    /// Call this when source metrics have changed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Thread-safe cache invalidation. Does not block on recomputation.
    /// </remarks>
    void InvalidateCache();

    /// <summary>
    /// Event raised when chart data has been updated.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after successful computation. Includes timing information
    /// for performance monitoring.
    /// </remarks>
    event EventHandler<ChartDataUpdatedEventArgs>? DataUpdated;
}

/// <summary>
/// Event arguments for chart data updates.
/// </summary>
/// <remarks>
/// LOGIC: Provides both the computed data and timing metrics for
/// observability and debugging.
/// </remarks>
public class ChartDataUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or initializes the updated chart data.
    /// </summary>
    public required ResonanceChartData ChartData { get; init; }

    /// <summary>
    /// Gets or initializes the time taken to compute the data.
    /// </summary>
    public TimeSpan ComputationTime { get; init; }
}
