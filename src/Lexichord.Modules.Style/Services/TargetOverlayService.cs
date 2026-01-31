// <copyright file="TargetOverlayService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Computes target overlay data from voice profiles for the Resonance Dashboard.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5b - Converts voice profile targets to chart-ready data.</para>
/// <para>Thread-safe caching using ConcurrentDictionary.</para>
/// <para>Uses <see cref="IResonanceAxisProvider"/> for consistent axis ordering.</para>
/// </remarks>
public sealed partial class TargetOverlayService : ITargetOverlayService
{
    private readonly IResonanceAxisProvider _axisProvider;
    private readonly ILogger<TargetOverlayService> _logger;

    /// <summary>
    /// Cached overlays keyed by profile ID string.
    /// </summary>
    private readonly ConcurrentDictionary<string, TargetOverlay> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TargetOverlayService"/> class.
    /// </summary>
    /// <param name="axisProvider">Provider for axis definitions.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public TargetOverlayService(
        IResonanceAxisProvider axisProvider,
        ILogger<TargetOverlayService> logger)
    {
        _axisProvider = axisProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<TargetOverlay?> GetOverlayAsync(VoiceProfile profile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var profileIdStr = profile.Id.ToString();

        // LOGIC: Fast path - return cached overlay
        if (_cache.TryGetValue(profileIdStr, out var cached))
        {
            LogCacheHit(profile.Name);
            return Task.FromResult<TargetOverlay?>(cached);
        }

        LogComputingOverlay(profile.Name);

        // LOGIC: Compute target data points from profile constraints
        var axes = _axisProvider.GetAxes();
        var dataPoints = new List<TargetDataPoint>();

        foreach (var axis in axes)
        {
            var targetValue = GetTargetValue(axis, profile);

            if (targetValue is null)
            {
                // LOGIC: Axis has no target - use 50 as neutral value
                dataPoints.Add(new TargetDataPoint(
                    AxisName: axis.Name,
                    NormalizedValue: 50,
                    RawValue: 0,
                    Description: $"No target for {axis.Name}")
                { Unit = axis.Unit });
                continue;
            }

            var normalized = axis.Normalize(targetValue.Value);
            dataPoints.Add(new TargetDataPoint(
                AxisName: axis.Name,
                NormalizedValue: normalized,
                RawValue: targetValue.Value,
                Description: axis.Description)
            { Unit = axis.Unit });

            LogAxisTarget(axis.Name, targetValue.Value, normalized);
        }

        var overlay = new TargetOverlay(
            ProfileId: profileIdStr,
            ProfileName: profile.Name,
            DataPoints: dataPoints.AsReadOnly(),
            ComputedAt: DateTimeOffset.UtcNow);

        // LOGIC: Cache the computed overlay
        _cache.TryAdd(profileIdStr, overlay);

        LogOverlayComputed(profile.Name, dataPoints.Count);

        return Task.FromResult<TargetOverlay?>(overlay);
    }

    /// <inheritdoc/>
    public void InvalidateCache(string profileId)
    {
        if (_cache.TryRemove(profileId, out _))
        {
            LogCacheInvalidated(profileId);
        }
    }

    /// <inheritdoc/>
    public void InvalidateAllCaches()
    {
        var count = _cache.Count;
        _cache.Clear();
        LogAllCachesInvalidated(count);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: v0.3.5d - Synchronous accessor for UI binding.
    /// Uses cached data if available, otherwise computes synchronously.
    /// </remarks>
    public TargetOverlay? GetOverlaySync(VoiceProfile profile)
    {
        var profileIdStr = profile.Id.ToString();

        // LOGIC: Fast path - return cached overlay
        if (_cache.TryGetValue(profileIdStr, out var cached))
        {
            LogCacheHit(profile.Name);
            return cached;
        }

        // LOGIC: Compute synchronously (same logic as async)
        LogComputingOverlay(profile.Name);
        var axes = _axisProvider.GetAxes();
        var dataPoints = new List<TargetDataPoint>();

        foreach (var axis in axes)
        {
            var targetValue = GetTargetValue(axis, profile);

            if (targetValue is null)
            {
                dataPoints.Add(new TargetDataPoint(
                    AxisName: axis.Name,
                    NormalizedValue: 50,
                    RawValue: 0,
                    Description: $"No target for {axis.Name}")
                { Unit = axis.Unit });
                continue;
            }

            var normalized = axis.Normalize(targetValue.Value);
            dataPoints.Add(new TargetDataPoint(
                AxisName: axis.Name,
                NormalizedValue: normalized,
                RawValue: targetValue.Value,
                Description: axis.Description)
            { Unit = axis.Unit });

            LogAxisTarget(axis.Name, targetValue.Value, normalized);
        }

        var overlay = new TargetOverlay(
            ProfileId: profileIdStr,
            ProfileName: profile.Name,
            DataPoints: dataPoints.AsReadOnly(),
            ComputedAt: DateTimeOffset.UtcNow);

        _cache.TryAdd(profileIdStr, overlay);
        LogOverlayComputed(profile.Name, dataPoints.Count);

        return overlay;
    }

    /// <summary>
    /// Gets the target value for an axis from a voice profile.
    /// </summary>
    /// <param name="axis">The axis definition.</param>
    /// <param name="profile">The voice profile.</param>
    /// <returns>Target raw value, or null if no target is defined.</returns>
    private static double? GetTargetValue(ResonanceAxisDefinition axis, VoiceProfile profile)
    {
        // LOGIC: Map axis metric keys to profile target properties
        return axis.MetricKey switch
        {
            // Readability: Target = 70 (standard readable prose)
            "FleschReadingEase" => 70.0,

            // Clarity: Use profile's max passive voice percentage (inverted in normalization)
            "PassiveVoicePercentage" => profile.MaxPassiveVoicePercentage,

            // Precision: Target = 5% weak word density (reasonable for most writing)
            "WeakWordDensity" => 5.0,

            // Accessibility: Use profile's target grade level if set
            "FleschKincaidGrade" => profile.TargetGradeLevel,

            // Density: Use profile's max sentence length as optimal
            "AverageWordsPerSentence" => profile.MaxSentenceLength * 0.7, // 70% of max is ideal

            // Flow: Target = 50 (moderate variance is ideal)
            "SentenceLengthVariance" => 50.0,

            _ => null
        };
    }

    // LOGIC: Source-generated logging for performance
    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for profile: {ProfileName}")]
    private partial void LogCacheHit(string profileName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Computing overlay for profile: {ProfileName}")]
    private partial void LogComputingOverlay(string profileName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Axis {AxisName} target: raw={RawValue:F2}, normalized={NormalizedValue:F2}")]
    private partial void LogAxisTarget(string axisName, double rawValue, double normalizedValue);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Overlay computed for profile: {ProfileName} with {DataPointCount} data points")]
    private partial void LogOverlayComputed(string profileName, int dataPointCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache invalidated for profile ID: {ProfileId}")]
    private partial void LogCacheInvalidated(string profileId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "All {Count} cached overlays invalidated")]
    private partial void LogAllCachesInvalidated(int count);
}
