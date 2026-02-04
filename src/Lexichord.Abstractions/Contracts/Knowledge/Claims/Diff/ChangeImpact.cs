// =============================================================================
// File: ChangeImpact.cs
// Project: Lexichord.Abstractions
// Description: Impact level of a claim change.
// =============================================================================
// LOGIC: Categorizes the significance of claim changes for prioritization
//   and review workflows. Higher impact changes warrant more attention.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: None (pure enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Impact level of a claim change.
/// </summary>
/// <remarks>
/// <para>
/// Impact levels help prioritize review of changes. Assessment is based on:
/// </para>
/// <list type="bullet">
///   <item><b>Predicate type:</b> Type changes are critical, deprecation is high.</item>
///   <item><b>Change type:</b> Removals are higher impact than additions.</item>
///   <item><b>Context:</b> API contract changes have higher impact.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
public enum ChangeImpact
{
    /// <summary>
    /// Low impact — minor changes, routine updates.
    /// </summary>
    /// <remarks>
    /// Examples: New optional parameters, minor description updates,
    /// small confidence adjustments.
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Medium impact — notable changes that may affect users.
    /// </summary>
    /// <remarks>
    /// Examples: Claim removals, significant confidence changes,
    /// new requirements.
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// High impact — significant changes requiring attention.
    /// </summary>
    /// <remarks>
    /// Examples: Deprecation claims, dependency changes,
    /// required parameter additions.
    /// </remarks>
    High = 2,

    /// <summary>
    /// Critical impact — potential breaking changes.
    /// </summary>
    /// <remarks>
    /// Examples: Type changes, API contract modifications,
    /// security-related claims.
    /// </remarks>
    Critical = 3
}
