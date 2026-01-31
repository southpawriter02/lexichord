// <copyright file="ChartDataService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Aggregates metrics from various sources and normalizes for chart rendering.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5a - Central service for the Resonance Dashboard charting infrastructure.</para>
/// <para>Aggregates data from:</para>
/// <list type="bullet">
///   <item>IReadabilityService: Flesch Reading Ease, FK Grade Level</item>
///   <item>IPassiveVoiceDetector: Passive voice percentage</item>
///   <item>IWeakWordScanner: Weak word density</item>
///   <item>IVoiceProfileService: Active profile for target comparison</item>
/// </list>
/// <para>Thread-safe caching with double-checked locking pattern.</para>
/// </remarks>
public sealed partial class ChartDataService : IChartDataService
{
    private readonly IReadabilityService _readabilityService;
    private readonly IPassiveVoiceDetector _passiveVoiceDetector;
    private readonly IWeakWordScanner _weakWordScanner;
    private readonly IVoiceProfileService _profileService;
    private readonly IResonanceAxisProvider _axisProvider;
    private readonly ILogger<ChartDataService> _logger;

    private ResonanceChartData? _cachedData;
    private readonly SemaphoreSlim _computeLock = new(1, 1);

    /// <inheritdoc/>
    public event EventHandler<ChartDataUpdatedEventArgs>? DataUpdated;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartDataService"/> class.
    /// </summary>
    public ChartDataService(
        IReadabilityService readabilityService,
        IPassiveVoiceDetector passiveVoiceDetector,
        IWeakWordScanner weakWordScanner,
        IVoiceProfileService profileService,
        IResonanceAxisProvider axisProvider,
        ILogger<ChartDataService> logger)
    {
        _readabilityService = readabilityService;
        _passiveVoiceDetector = passiveVoiceDetector;
        _weakWordScanner = weakWordScanner;
        _profileService = profileService;
        _axisProvider = axisProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ResonanceChartData> GetChartDataAsync(CancellationToken ct = default)
    {
        // LOGIC: Fast path - return cached data if available
        if (_cachedData is not null)
        {
            LogReturningCachedData();
            return _cachedData;
        }

        await _computeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // LOGIC: Double-check after acquiring lock
            if (_cachedData is not null)
            {
                LogReturningCachedData();
                return _cachedData;
            }

            var stopwatch = Stopwatch.StartNew();
            var axes = _axisProvider.GetAxes();

            LogComputingChartData(axes.Count);

            // LOGIC: Get current document text from active editor
            // For now, use empty text - will be wired up in integration
            var currentText = string.Empty;

            // LOGIC: Gather metrics
            var readability = _readabilityService.Analyze(currentText);
            
            _passiveVoiceDetector.GetPassiveVoicePercentage(
                currentText, 
                out var passiveCount, 
                out var totalSentences);
            var passivePercentage = totalSentences > 0 
                ? (double)passiveCount / totalSentences * 100.0 
                : 0.0;

            var profile = await _profileService.GetActiveProfileAsync(ct).ConfigureAwait(false);
            var weakWordStats = _weakWordScanner.GetStatistics(currentText, profile);

            // LOGIC: Build data points
            var dataPoints = new List<ResonanceDataPoint>();

            foreach (var axis in axes)
            {
                var rawValue = GetRawValue(
                    axis, 
                    readability, 
                    passivePercentage, 
                    weakWordStats);
                var normalizedValue = axis.Normalize(rawValue);

                LogAxisComputation(axis.Name, rawValue, normalizedValue);

                dataPoints.Add(new ResonanceDataPoint(
                    AxisName: axis.Name,
                    NormalizedValue: normalizedValue,
                    RawValue: rawValue,
                    Unit: axis.Unit,
                    Description: axis.Description));
            }

            _cachedData = new ResonanceChartData(
                DataPoints: dataPoints.AsReadOnly(),
                ComputedAt: DateTimeOffset.UtcNow);

            stopwatch.Stop();

            LogChartDataComputed(stopwatch.ElapsedMilliseconds);

            DataUpdated?.Invoke(this, new ChartDataUpdatedEventArgs
            {
                ChartData = _cachedData,
                ComputationTime = stopwatch.Elapsed
            });

            return _cachedData;
        }
        finally
        {
            _computeLock.Release();
        }
    }

    /// <inheritdoc/>
    public void InvalidateCache()
    {
        _cachedData = null;
        LogCacheInvalidated();
    }

    /// <summary>
    /// Gets the raw metric value for a given axis.
    /// </summary>
    private static double GetRawValue(
        ResonanceAxisDefinition axis,
        ReadabilityMetrics readability,
        double passivePercentage,
        WeakWordStats weakWordStats)
    {
        return axis.MetricKey switch
        {
            "FleschReadingEase" => readability.FleschReadingEase,
            "PassiveVoicePercentage" => passivePercentage,
            "WeakWordDensity" => weakWordStats.WeakWordPercentage,
            "FleschKincaidGrade" => readability.FleschKincaidGradeLevel,
            "AverageWordsPerSentence" => readability.AverageWordsPerSentence,
            "SentenceLengthVariance" => CalculateSentenceLengthVariance(readability),
            _ => 0
        };
    }

    /// <summary>
    /// Calculates a proxy for sentence length variance from available metrics.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses syllable variance as a proxy since we don't have direct
    /// sentence length variance. Will be refined in v0.3.5b.
    /// </remarks>
    private static double CalculateSentenceLengthVariance(ReadabilityMetrics readability)
    {
        // LOGIC: Placeholder - map average words per sentence to a variance proxy
        // Short sentences (5-10) have low variance, medium (15-20) moderate, long (25+) high
        var avg = readability.AverageWordsPerSentence;
        return avg switch
        {
            <= 10 => 25,
            <= 15 => 50,
            <= 20 => 75,
            _ => 90
        };
    }

    // LOGIC: Source-generated logging for performance
    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning cached chart data")]
    private partial void LogReturningCachedData();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Computing chart data for {AxisCount} axes")]
    private partial void LogComputingChartData(int axisCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Axis {AxisName}: raw={RawValue:F2}, normalized={NormalizedValue:F2}")]
    private partial void LogAxisComputation(string axisName, double rawValue, double normalizedValue);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Chart data computed in {ElapsedMs}ms")]
    private partial void LogChartDataComputed(long elapsedMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Chart data cache invalidated")]
    private partial void LogCacheInvalidated();
}
