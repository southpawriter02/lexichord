// =============================================================================
// File: ValidationOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for a validation pass.
// =============================================================================
// LOGIC: Encapsulates the configuration that controls how validation runs:
//   which mode to use, timeout limits, maximum findings cap, and the
//   license tier for gating. Immutable record with a Default() factory.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Configuration options for a document validation pass.
/// </summary>
/// <remarks>
/// <para>
/// Controls the behavior of the <see cref="IValidationEngine"/> during a
/// validation pass. The <see cref="Mode"/> determines which validators run,
/// <see cref="Timeout"/> limits total execution time, <see cref="MaxFindings"/>
/// caps the number of findings returned, and <see cref="LicenseTier"/>
/// restricts validators to those available at the current tier.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
/// <param name="Mode">
/// The validation mode determining which validators are eligible.
/// Defaults to <see cref="ValidationMode.OnDemand"/>.
/// </param>
/// <param name="Timeout">
/// Maximum time allowed for the entire validation pass.
/// Individual validators that exceed this are cancelled and recorded as timed out.
/// Defaults to 30 seconds.
/// </param>
/// <param name="MaxFindings">
/// Optional cap on the total number of findings returned.
/// When set, validation may stop early once the cap is reached.
/// Null means no limit.
/// </param>
/// <param name="LicenseTier">
/// The current user's license tier. Validators requiring a higher tier
/// than this value are skipped. Defaults to <see cref="Contracts.LicenseTier.Core"/>.
/// </param>
public record ValidationOptions(
    ValidationMode Mode = ValidationMode.OnDemand,
    TimeSpan? Timeout = null,
    int? MaxFindings = null,
    LicenseTier LicenseTier = LicenseTier.Core
)
{
    /// <summary>
    /// Gets the effective timeout, defaulting to 30 seconds if not specified.
    /// </summary>
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates default validation options suitable for on-demand validation.
    /// </summary>
    /// <returns>A new <see cref="ValidationOptions"/> with sensible defaults.</returns>
    public static ValidationOptions Default() => new();

    /// <summary>
    /// Creates options for real-time validation with a short timeout.
    /// </summary>
    /// <param name="licenseTier">The current user's license tier.</param>
    /// <returns>A new <see cref="ValidationOptions"/> configured for real-time use.</returns>
    public static ValidationOptions ForRealTime(LicenseTier licenseTier = LicenseTier.Core) =>
        new(
            Mode: ValidationMode.RealTime,
            Timeout: TimeSpan.FromMilliseconds(50),
            LicenseTier: licenseTier
        );

    /// <summary>
    /// Creates options for pre-publish validation with comprehensive checking.
    /// </summary>
    /// <param name="licenseTier">The current user's license tier.</param>
    /// <returns>A new <see cref="ValidationOptions"/> configured for pre-publish use.</returns>
    public static ValidationOptions ForPrePublish(LicenseTier licenseTier = LicenseTier.Core) =>
        new(
            Mode: ValidationMode.PrePublish,
            Timeout: TimeSpan.FromMinutes(2),
            LicenseTier: licenseTier
        );
}
