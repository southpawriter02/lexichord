// =============================================================================
// File: ValidatorInfo.cs
// Project: Lexichord.Modules.Knowledge
// Description: Internal metadata wrapper for registered validators.
// =============================================================================
// LOGIC: Pairs a validator instance with its registration metadata (priority,
//   enabled state). Used by ValidatorRegistry for sorting and filtering.
//   Priority determines execution order when ordering matters — higher
//   priority validators appear first in the list.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Modules.Knowledge.Validation;

/// <summary>
/// Internal metadata wrapper for a registered validator.
/// </summary>
/// <remarks>
/// <para>
/// Associates a validator instance with its registration-time configuration.
/// The <see cref="Priority"/> controls ordering when validators are listed
/// or when execution order matters for display purposes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
/// <param name="Validator">The validator instance.</param>
/// <param name="Priority">
/// Priority for ordering. Higher values = higher priority (runs first in sorted lists).
/// Default is 0.
/// </param>
/// <param name="IsEnabled">
/// Whether this validator is currently enabled. Disabled validators are
/// skipped during validation. Default is <c>true</c>.
/// </param>
internal record ValidatorInfo(
    Abstractions.Contracts.Knowledge.Validation.IValidator Validator,
    int Priority = 0,
    bool IsEnabled = true
);
