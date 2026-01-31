// <copyright file="ParallelAnalysisResult.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Aggregated results from parallel scanner execution.
/// Contains results from all scanners plus timing and error information.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.7b - Result aggregation from parallel analysis.</para>
/// <para>Contains style violations from regex and fuzzy scanning combined.</para>
/// <para>Includes readability metrics and voice analysis results.</para>
/// <para>Tracks individual scanner durations for profiling.</para>
/// <para>Handles partial failures by recording errors while preserving successful results.</para>
/// </remarks>
public record ParallelAnalysisResult
{
    /// <summary>
    /// Style violations from regex and fuzzy scanning combined.
    /// </summary>
    public IReadOnlyList<StyleViolation> StyleViolations { get; init; } =
        Array.Empty<StyleViolation>();

    /// <summary>
    /// Readability metrics from ReadabilityService.
    /// Null if the scanner failed or was skipped.
    /// </summary>
    public ReadabilityMetrics? Readability { get; init; }

    /// <summary>
    /// Voice analysis results from VoiceAnalyzer.
    /// Null if the scanner failed or was skipped.
    /// </summary>
    public VoiceAnalysisResult? VoiceAnalysis { get; init; }

    /// <summary>
    /// Total wall-clock time for the parallel pipeline execution.
    /// Should be approximately equal to the longest individual scanner.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Individual scanner execution times for profiling.
    /// Keys: "Regex", "Fuzzy", "Readability", "Voice".
    /// </summary>
    public IReadOnlyDictionary<string, TimeSpan> ScannerDurations { get; init; } =
        new Dictionary<string, TimeSpan>();

    /// <summary>
    /// Indicates if any scanner was cancelled or failed.
    /// When true, some results may be missing.
    /// </summary>
    public bool IsPartialResult { get; init; }

    /// <summary>
    /// Exceptions from failed scanners (if any).
    /// Empty if all scanners completed successfully.
    /// </summary>
    public IReadOnlyList<Exception> Errors { get; init; } =
        Array.Empty<Exception>();

    /// <summary>
    /// Gets the total number of violations across all scanners.
    /// </summary>
    public int TotalViolationCount => StyleViolations.Count;

    /// <summary>
    /// Gets the calculated speedup ratio compared to sequential execution.
    /// Returns 1.0 if no scanner durations are recorded.
    /// </summary>
    public double SpeedupRatio
    {
        get
        {
            if (ScannerDurations.Count == 0 || TotalDuration.TotalMilliseconds == 0)
            {
                return 1.0;
            }

            var sequentialSum = ScannerDurations.Values.Sum(d => d.TotalMilliseconds);
            return sequentialSum / TotalDuration.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Gets an empty result for initialization or error cases.
    /// </summary>
    public static ParallelAnalysisResult Empty => new()
    {
        TotalDuration = TimeSpan.Zero,
        IsPartialResult = true
    };
}
