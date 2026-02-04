// =============================================================================
// File: HealthLevel.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of system health levels for metrics dashboard.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Defines health levels for the deduplication system status,
//   enabling traffic-light style visualization in the metrics dashboard.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines health levels for the deduplication system.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// Health level is determined by evaluating:
/// </para>
/// <list type="bullet">
///   <item><description>Processing latency compared to targets.</description></item>
///   <item><description>Error rates in deduplication operations.</description></item>
///   <item><description>Queue depth for pending reviews.</description></item>
///   <item><description>Database connectivity and query performance.</description></item>
/// </list>
/// </remarks>
public enum HealthLevel
{
    /// <summary>
    /// System is healthy and operating within normal parameters.
    /// </summary>
    /// <remarks>
    /// All performance targets are being met, error rates are low,
    /// and no queues are backing up. No action required.
    /// </remarks>
    Healthy = 0,

    /// <summary>
    /// System is operational but showing signs of degradation.
    /// </summary>
    /// <remarks>
    /// Some performance targets may be exceeded, or warning thresholds
    /// have been reached. The system remains functional but should be
    /// monitored. Consider proactive investigation.
    /// </remarks>
    Degraded = 1,

    /// <summary>
    /// System is experiencing significant issues.
    /// </summary>
    /// <remarks>
    /// Critical thresholds have been exceeded or core functionality is
    /// impaired. Immediate investigation and intervention recommended.
    /// </remarks>
    Unhealthy = 2
}
