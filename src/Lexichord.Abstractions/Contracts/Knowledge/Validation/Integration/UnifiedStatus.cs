// =============================================================================
// File: UnifiedStatus.cs
// Project: Lexichord.Abstractions
// Description: Overall status of a unified finding result.
// =============================================================================
// LOGIC: Computed from the highest severity in the combined findings:
//   - No findings or no errors/warnings → Pass
//   - Warnings but no errors → PassWithWarnings
//   - Any errors → Fail
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Overall status of a <see cref="UnifiedFindingResult"/>.
/// </summary>
/// <remarks>
/// <para>
/// Computed from the severity distribution of the combined findings.
/// Only <see cref="UnifiedSeverity.Error"/>-level findings cause
/// <see cref="Fail"/> status.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public enum UnifiedStatus
{
    /// <summary>All checks passed with no errors or warnings.</summary>
    Pass,

    /// <summary>Passed with warnings but no errors.</summary>
    PassWithWarnings,

    /// <summary>Failed — at least one error-level finding.</summary>
    Fail
}
