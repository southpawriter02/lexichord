// =============================================================================
// File: ClaimValidationMessage.cs
// Project: Lexichord.Abstractions
// Description: Validation messages for claims with severity and context.
// =============================================================================
// LOGIC: Provides detailed validation feedback for claims, including error
//   messages, warning codes, and references to related axioms or conflicting
//   claims. Used by the validation engine to report issues.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: None (pure records and enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// A validation message for a claim.
/// </summary>
/// <remarks>
/// <para>
/// When a claim is validated against axioms and the knowledge graph, any
/// issues are reported as <see cref="ClaimValidationMessage"/> instances.
/// These messages provide actionable feedback for human reviewers.
/// </para>
/// <para>
/// <b>Message Codes:</b> Follow the pattern "CLM-{CATEGORY}-{NUMBER}":
/// <list type="bullet">
///   <item>CLM-AXM-*: Axiom violations</item>
///   <item>CLM-CNF-*: Conflict with other claims</item>
///   <item>CLM-ENT-*: Entity resolution issues</item>
///   <item>CLM-SCH-*: Schema violations</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public record ClaimValidationMessage
{
    /// <summary>
    /// Severity level of the validation message.
    /// </summary>
    /// <value>The importance level: Info, Warning, or Error.</value>
    public ClaimMessageSeverity Severity { get; init; }

    /// <summary>
    /// Machine-readable message code.
    /// </summary>
    /// <value>
    /// A unique code identifying the validation issue.
    /// Example: "CLM-AXM-001" for axiom violation.
    /// </value>
    /// <remarks>
    /// LOGIC: Codes are stable across versions for programmatic handling.
    /// See project documentation for the full code catalog.
    /// </remarks>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable message describing the issue.
    /// </summary>
    /// <value>A descriptive message suitable for display to users.</value>
    public required string Message { get; init; }

    /// <summary>
    /// Related axiom ID if this is an axiom violation.
    /// </summary>
    /// <value>
    /// The ID of the axiom that was violated, or null if not applicable.
    /// </value>
    /// <remarks>
    /// LOGIC: References an <see cref="Axiom.Id"/> from the Axiom Store
    /// (v0.4.6e). Used to navigate to the axiom definition.
    /// </remarks>
    public string? AxiomId { get; init; }

    /// <summary>
    /// Related claim ID if this is a conflict.
    /// </summary>
    /// <value>
    /// The ID of the conflicting claim, or null if not applicable.
    /// </value>
    /// <remarks>
    /// LOGIC: References another <see cref="Claim.Id"/> that conflicts
    /// with this claim. Used to display conflict pairs in the UI.
    /// </remarks>
    public Guid? ConflictingClaimId { get; init; }
}

/// <summary>
/// Severity level for claim validation messages.
/// </summary>
/// <remarks>
/// <para>
/// Indicates the importance of a validation message. Errors indicate
/// invalid claims, warnings indicate potential issues, and info messages
/// provide additional context.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public enum ClaimMessageSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    /// <remarks>
    /// LOGIC: Provides context but does not indicate a problem.
    /// The claim is still considered valid.
    /// </remarks>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    /// <remarks>
    /// LOGIC: Indicates a potential issue that should be reviewed.
    /// The claim may be valid but warrants human attention.
    /// </remarks>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    /// <remarks>
    /// LOGIC: Indicates a validation failure. The claim's
    /// <see cref="ClaimValidationStatus"/> should be <see cref="ClaimValidationStatus.Invalid"/>
    /// or <see cref="ClaimValidationStatus.Conflict"/>.
    /// </remarks>
    Error
}
