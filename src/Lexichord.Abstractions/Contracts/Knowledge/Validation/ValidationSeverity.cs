// =============================================================================
// File: ValidationSeverity.cs
// Project: Lexichord.Abstractions
// Description: Defines severity levels for validation findings.
// =============================================================================
// LOGIC: Severity levels control how findings are presented and whether they
//   block document operations. Only Error-level findings cause IsValid to
//   return false on ValidationResult.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Defines severity levels for validation findings.
/// </summary>
/// <remarks>
/// <para>
/// Severity determines whether a finding blocks document operations and how
/// prominently it is displayed in the UI. Only <see cref="Error"/>-level
/// findings cause <see cref="ValidationResult.IsValid"/> to return <c>false</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational finding — does not indicate a problem.
    /// </summary>
    /// <remarks>
    /// Used for suggestions, best practices, or style recommendations.
    /// Does not affect <see cref="ValidationResult.IsValid"/>.
    /// </remarks>
    Info = 0,

    /// <summary>
    /// Warning-level finding — potential issue that may need attention.
    /// </summary>
    /// <remarks>
    /// Used for non-blocking issues such as missing optional properties,
    /// deprecated usage, or style guide deviations.
    /// Does not affect <see cref="ValidationResult.IsValid"/>.
    /// </remarks>
    Warning = 1,

    /// <summary>
    /// Error-level finding — a problem that must be resolved.
    /// </summary>
    /// <remarks>
    /// Used for blocking issues such as schema violations, missing required
    /// fields, or constraint failures. Causes <see cref="ValidationResult.IsValid"/>
    /// to return <c>false</c>.
    /// </remarks>
    Error = 2
}
