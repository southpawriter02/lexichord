// =============================================================================
// File: ValidationMode.cs
// Project: Lexichord.Abstractions
// Description: Defines the modes in which document validation can operate.
// =============================================================================
// LOGIC: Each mode determines which validators are eligible to run.
//   Real-time validation only executes fast, schema-level validators to avoid
//   blocking the UI. Pre-publish runs all validators for comprehensive checks.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Defines the modes in which document validation can operate.
/// </summary>
/// <remarks>
/// <para>
/// Each mode controls which validators are eligible to run. Validators
/// declare their supported modes via <see cref="IValidator.SupportedModes"/>,
/// and the <see cref="IValidationEngine"/> filters validators accordingly.
/// </para>
/// <para>
/// <b>Mode Hierarchy:</b> More comprehensive modes (e.g., <see cref="PrePublish"/>)
/// typically include all validators from less comprehensive modes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
[Flags]
public enum ValidationMode
{
    /// <summary>
    /// No validation mode specified. Not valid for use.
    /// </summary>
    None = 0,

    /// <summary>
    /// Real-time validation triggered during typing or editing.
    /// </summary>
    /// <remarks>
    /// Only fast, non-blocking validators (e.g., schema validation) should
    /// support this mode. Target latency: &lt;50ms.
    /// </remarks>
    RealTime = 1,

    /// <summary>
    /// Validation triggered when the document is saved.
    /// </summary>
    /// <remarks>
    /// Allows slightly more expensive validators than <see cref="RealTime"/>,
    /// as the user expects a brief processing pause. Target latency: &lt;500ms.
    /// </remarks>
    OnSave = 2,

    /// <summary>
    /// Validation triggered by explicit user action (e.g., "Validate Now" button).
    /// </summary>
    /// <remarks>
    /// Can include heavier validators such as cross-document consistency checks.
    /// UI should show a progress indicator. Target latency: &lt;5s.
    /// </remarks>
    OnDemand = 4,

    /// <summary>
    /// Comprehensive validation before publishing or exporting.
    /// </summary>
    /// <remarks>
    /// Runs all available validators including axiom evaluation, cross-reference
    /// checks, and style guide compliance. No strict latency target — user
    /// expects a full validation pass.
    /// </remarks>
    PrePublish = 8,

    /// <summary>
    /// All validation modes. Convenience flag for validators that support every mode.
    /// </summary>
    All = RealTime | OnSave | OnDemand | PrePublish
}
