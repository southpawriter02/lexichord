// <copyright file="PerformanceBaseline.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Top-level data contract for performance baseline thresholds.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.8d - Defines threshold values for performance regression testing.
/// Stored in JSON format for version control and easy updates.
///
/// Version: v0.3.8d
/// </remarks>
/// <param name="Metadata">Baseline metadata including version and timestamp.</param>
/// <param name="Readability">Thresholds for readability analysis.</param>
/// <param name="FuzzyScanning">Thresholds for fuzzy terminology scanning.</param>
/// <param name="VoiceAnalysis">Thresholds for passive voice and weak word analysis.</param>
/// <param name="FullPipeline">Thresholds for full analysis pipeline execution.</param>
/// <param name="RegressionThreshold">Maximum allowed regression percentage (default: 10%).</param>
public record PerformanceBaseline(
    BaselineMetadata Metadata,
    OperationThresholds Readability,
    OperationThresholds FuzzyScanning,
    OperationThresholds VoiceAnalysis,
    OperationThresholds FullPipeline,
    double RegressionThreshold = 0.25);

/// <summary>
/// Metadata for a performance baseline configuration.
/// </summary>
/// <param name="Version">Version identifier for the baseline (e.g., "v0.3.8d").</param>
/// <param name="CreatedAt">UTC timestamp when the baseline was established.</param>
/// <param name="Machine">Machine identifier where baseline was captured.</param>
/// <param name="Notes">Optional notes about the baseline configuration.</param>
public record BaselineMetadata(
    string Version,
    DateTimeOffset CreatedAt,
    string? Machine = null,
    string? Notes = null);

/// <summary>
/// Performance thresholds for a single operation at different input sizes.
/// </summary>
/// <remarks>
/// LOGIC: Thresholds are specified in milliseconds for timing constraints
/// and megabytes for memory allocation limits.
///
/// Word count tiers:
/// - Words1K: Small documents (~1,000 words)
/// - Words10K: Medium documents (~10,000 words)
/// - Words50K: Large documents (~50,000 words)
/// </remarks>
/// <param name="Words1K">Maximum milliseconds for 1,000 word input.</param>
/// <param name="Words10K">Maximum milliseconds for 10,000 word input.</param>
/// <param name="Words50K">Maximum milliseconds for 50,000 word input.</param>
/// <param name="MaxMemoryMB">Maximum memory allocation in megabytes.</param>
public record OperationThresholds(
    int Words1K,
    int Words10K,
    int Words50K,
    double MaxMemoryMB = 100.0);

/// <summary>
/// Result of a single benchmark measurement.
/// </summary>
/// <param name="Operation">Name of the operation measured.</param>
/// <param name="WordCount">Number of words in the input.</param>
/// <param name="ElapsedMs">Elapsed time in milliseconds.</param>
/// <param name="AllocatedMB">Memory allocated in megabytes.</param>
/// <param name="WordsPerSecond">Throughput in words per second.</param>
public record BenchmarkMeasurement(
    string Operation,
    int WordCount,
    double ElapsedMs,
    double AllocatedMB,
    double WordsPerSecond);

/// <summary>
/// Provides default baseline values for performance testing.
/// </summary>
public static class DefaultBaselines
{
    /// <summary>
    /// Gets the default performance baseline based on specification targets.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default thresholds from LCS-DES-v0.3.8d specification:
    /// - Readability: 20ms/200ms/1000ms
    /// - Fuzzy: 50ms/500ms/2500ms
    /// - Voice: 30ms/300ms/1500ms
    /// - Pipeline: 100ms/1000ms/5000ms
    /// </remarks>
    public static PerformanceBaseline Default { get; } = new(
        Metadata: new BaselineMetadata(
            Version: "v0.3.8d-relaxed",
            CreatedAt: DateTimeOffset.UtcNow,
            Notes: "Relaxed baselines for parallel test execution environments"),
        Readability: new OperationThresholds(
            Words1K: 50,
            Words10K: 400,
            Words50K: 2000,
            MaxMemoryMB: 50.0),
        FuzzyScanning: new OperationThresholds(
            Words1K: 100,
            Words10K: 800,
            Words50K: 4000,
            MaxMemoryMB: 100.0),
        VoiceAnalysis: new OperationThresholds(
            Words1K: 60,
            Words10K: 500,
            Words50K: 2500,
            MaxMemoryMB: 75.0),
        FullPipeline: new OperationThresholds(
            Words1K: 200,
            Words10K: 2000,
            Words50K: 8000,
            MaxMemoryMB: 200.0),
        RegressionThreshold: 0.25);
}
