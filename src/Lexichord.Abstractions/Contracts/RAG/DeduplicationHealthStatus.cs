// =============================================================================
// File: DeduplicationHealthStatus.cs
// Project: Lexichord.Abstractions
// Description: Record for deduplication system health status.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Provides comprehensive health status for the deduplication system,
//   including performance metrics and actionable warnings.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Health status of the deduplication system.
/// </summary>
/// <param name="Level">The overall health level (Healthy, Degraded, Unhealthy).</param>
/// <param name="Message">Human-readable status message.</param>
/// <param name="Warnings">List of active warnings, if any.</param>
/// <param name="SimilarityQueryP99Ms">99th percentile latency for similarity queries in milliseconds.</param>
/// <param name="ClassificationP99Ms">99th percentile latency for classification operations in milliseconds.</param>
/// <param name="ProcessingP99Ms">99th percentile latency for full chunk processing in milliseconds.</param>
/// <param name="IsWithinPerformanceTargets">Whether all operations are within defined performance targets.</param>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// <b>Performance Targets:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Similarity query P99: &lt; 50ms</description></item>
///   <item><description>Classification P99: &lt; 100ms (rule-based) / &lt; 2s (LLM)</description></item>
///   <item><description>Full processing P99: &lt; 150ms (excluding LLM)</description></item>
/// </list>
/// <para>
/// <b>Health Determination:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="HealthLevel.Healthy"/>: All targets met, no warnings.</description></item>
///   <item><description><see cref="HealthLevel.Degraded"/>: Some targets exceeded or warnings present.</description></item>
///   <item><description><see cref="HealthLevel.Unhealthy"/>: Critical targets exceeded or errors occurring.</description></item>
/// </list>
/// </remarks>
public record DeduplicationHealthStatus(
    HealthLevel Level,
    string Message,
    IReadOnlyList<string> Warnings,
    double SimilarityQueryP99Ms,
    double ClassificationP99Ms,
    double ProcessingP99Ms,
    bool IsWithinPerformanceTargets)
{
    /// <summary>
    /// Performance target for similarity query P99 latency in milliseconds.
    /// </summary>
    public const double SimilarityQueryTargetMs = 50.0;

    /// <summary>
    /// Performance target for classification P99 latency in milliseconds (rule-based).
    /// </summary>
    public const double ClassificationTargetMs = 100.0;

    /// <summary>
    /// Performance target for full processing P99 latency in milliseconds.
    /// </summary>
    public const double ProcessingTargetMs = 150.0;

    /// <summary>
    /// Gets a healthy status with no warnings.
    /// </summary>
    public static DeduplicationHealthStatus Healthy { get; } = new(
        HealthLevel.Healthy,
        "All systems operating normally",
        Array.Empty<string>(),
        0.0,
        0.0,
        0.0,
        true);
}
