// =============================================================================
// File: ContradictionSeverity.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of contradiction severity levels for metrics tracking.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Defines severity levels for categorizing detected contradictions,
//   enabling prioritized review workflows and alert thresholds.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines severity levels for detected contradictions.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// Severity is determined by factors including:
/// </para>
/// <list type="bullet">
///   <item><description>Similarity score between conflicting chunks.</description></item>
///   <item><description>Confidence of the contradiction classification.</description></item>
///   <item><description>Recency of the conflicting documents.</description></item>
///   <item><description>Usage frequency of the affected content.</description></item>
/// </list>
/// <para>
/// Higher severity contradictions should be reviewed first and may trigger
/// alerts when thresholds are exceeded.
/// </para>
/// </remarks>
public enum ContradictionSeverity
{
    /// <summary>
    /// Low severity contradiction.
    /// </summary>
    /// <remarks>
    /// Minor inconsistency that is unlikely to cause confusion. Examples include
    /// slight wording differences or outdated but non-critical information.
    /// These can typically be addressed during routine maintenance.
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Medium severity contradiction.
    /// </summary>
    /// <remarks>
    /// Moderate inconsistency that could cause confusion if not addressed.
    /// Examples include differing dates, names, or statistics that may be
    /// noticed by users. Should be reviewed within a reasonable timeframe.
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// High severity contradiction.
    /// </summary>
    /// <remarks>
    /// Significant inconsistency that is likely to cause confusion or errors.
    /// Examples include conflicting instructions, procedures, or policies.
    /// Should be prioritized for review and resolution.
    /// </remarks>
    High = 2,

    /// <summary>
    /// Critical severity contradiction.
    /// </summary>
    /// <remarks>
    /// Severe inconsistency that could lead to serious errors or compliance issues.
    /// Examples include conflicting safety procedures, legal terms, or regulatory
    /// requirements. Requires immediate attention and may trigger alerts.
    /// </remarks>
    Critical = 3
}
