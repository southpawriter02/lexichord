// =============================================================================
// File: BenchmarkConfig.cs
// Project: Lexichord.Benchmarks
// Description: Custom BenchmarkDotNet configuration for search performance tests.
// =============================================================================
// v0.5.8b: Configures benchmarks with CI mode detection, memory diagnostics,
//          and JSON/Markdown export for baseline comparison.
// =============================================================================

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;

namespace Lexichord.Benchmarks;

/// <summary>
/// Custom BenchmarkDotNet configuration for search performance benchmarks.
/// </summary>
/// <remarks>
/// <para>
/// This configuration adapts benchmark behavior based on the execution environment:
/// </para>
/// <list type="bullet">
///   <item><b>CI Mode:</b> Shorter warmup (2 iterations) and fewer measurement iterations (5)
///   for faster feedback in continuous integration pipelines.</item>
///   <item><b>Normal Mode:</b> Full warmup (5 iterations) and measurement (20 iterations)
///   for accurate baseline establishment.</item>
/// </list>
/// <para>
/// <b>Diagnostics:</b> Memory allocation profiling is enabled via <see cref="MemoryDiagnoser"/>.
/// </para>
/// <para>
/// <b>Exports:</b> JSON (for regression detection) and GitHub Markdown (for PR comments).
/// </para>
/// <para><b>Introduced:</b> v0.5.8b.</para>
/// </remarks>
public class BenchmarkConfig : ManualConfig
{
    /// <summary>
    /// Creates a new <see cref="BenchmarkConfig"/> instance with CI-aware settings.
    /// </summary>
    public BenchmarkConfig()
    {
        // LOGIC: Detect CI environment to adjust iteration counts.
        // Shorter runs in CI provide faster feedback; full runs for baseline generation.
        var ciMode = Environment.GetEnvironmentVariable("CI") == "true";

        AddJob(Job.Default
            .WithWarmupCount(ciMode ? 2 : 5)
            .WithIterationCount(ciMode ? 5 : 20)
            .WithUnrollFactor(1));

        // LOGIC: MemoryDiagnoser tracks allocations per operation for regression detection.
        AddDiagnoser(MemoryDiagnoser.Default);

        // LOGIC: Add statistical columns for latency analysis.
        AddColumn(RankColumn.Arabic);
        AddColumn(StatisticColumn.P95);
        AddColumn(StatisticColumn.Max);

        // LOGIC: JSON export enables automated regression comparison.
        // Markdown export provides human-readable output for PR reviews.
        AddExporter(JsonExporter.Full);
        AddExporter(MarkdownExporter.GitHub);

        // LOGIC: Join summary combines all benchmark results in a single table.
        WithOptions(ConfigOptions.JoinSummary);
    }
}
