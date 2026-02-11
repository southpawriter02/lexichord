// -----------------------------------------------------------------------
// <copyright file="PerformanceBaseline.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Performance;

/// <summary>
/// Captures a single performance baseline measurement.
/// </summary>
/// <param name="MetricName">The name of the metric being measured (e.g., "ContextAssembly", "TemplateRendering").</param>
/// <param name="TargetMs">The target latency in milliseconds.</param>
/// <param name="ActualMs">The measured actual latency in milliseconds.</param>
/// <param name="P95Ms">The 95th percentile latency in milliseconds.</param>
/// <param name="MeasuredAt">The timestamp when the measurement was taken.</param>
/// <remarks>
/// <para>
/// Performance baselines are used to track whether the Agents module meets its
/// defined performance targets over time. Each baseline records the metric name,
/// the target value, the actual measured value, and the P95 latency.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8c as part of Performance Optimization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var baseline = new PerformanceBaseline(
///     MetricName: "ContextAssembly",
///     TargetMs: 200.0,
///     ActualMs: 145.3,
///     P95Ms: 198.7,
///     MeasuredAt: DateTimeOffset.UtcNow);
///
/// Console.WriteLine($"{baseline.MetricName}: {baseline.ActualMs:F1}ms (target: {baseline.TargetMs:F1}ms)");
/// </code>
/// </example>
public sealed record PerformanceBaseline(
    string MetricName,
    double TargetMs,
    double ActualMs,
    double P95Ms,
    DateTimeOffset MeasuredAt);
