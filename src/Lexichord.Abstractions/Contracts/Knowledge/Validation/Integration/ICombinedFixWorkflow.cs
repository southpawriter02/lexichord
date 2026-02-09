// =============================================================================
// File: ICombinedFixWorkflow.cs
// Project: Lexichord.Abstractions
// Description: Manages combined fix workflow for validation and linter fixes.
// =============================================================================
// LOGIC: Provides conflict detection and ordering for safe application
//   of fixes from mixed sources.
//
// SPEC ADAPTATION:
//   - ApplyOrderedFixesAsync removed (application is a simple text
//     replacement, handled by LinterIntegration directly)
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Manages the combined fix workflow for validation and linter fixes.
/// </summary>
/// <remarks>
/// <para>
/// Responsible for detecting conflicts between fixes and ordering them
/// for safe sequential application. Fixes targeting the same finding
/// are flagged as conflicts; remaining fixes are ordered by finding ID
/// for deterministic application.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be stateless and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public interface ICombinedFixWorkflow
{
    /// <summary>
    /// Validates that fixes don't conflict with each other.
    /// </summary>
    /// <param name="fixes">The fixes to check for conflicts.</param>
    /// <returns>
    /// A <see cref="FixConflictResult"/> listing any detected conflicts.
    /// </returns>
    /// <remarks>
    /// LOGIC: Two fixes conflict if they target the same FindingId.
    /// This is a lightweight check; actual text overlap detection
    /// is deferred to application time.
    /// </remarks>
    FixConflictResult CheckForConflicts(
        IReadOnlyList<UnifiedFix> fixes);

    /// <summary>
    /// Orders fixes for safe sequential application.
    /// </summary>
    /// <param name="fixes">The fixes to order.</param>
    /// <returns>Fixes ordered for safe application (by FindingId).</returns>
    /// <remarks>
    /// LOGIC: Orders by FindingId descending to allow reverse-order
    /// application that avoids offset shifting problems.
    /// </remarks>
    IReadOnlyList<UnifiedFix> OrderFixesForApplication(
        IReadOnlyList<UnifiedFix> fixes);
}
