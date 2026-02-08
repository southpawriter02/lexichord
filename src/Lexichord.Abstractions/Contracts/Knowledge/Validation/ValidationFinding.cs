// =============================================================================
// File: ValidationFinding.cs
// Project: Lexichord.Abstractions
// Description: Represents a single finding produced by a validator.
// =============================================================================
// LOGIC: Each finding captures the validator that produced it, the severity,
//   a machine-readable code, a human-readable message, an optional property
//   path for precise location, and an optional suggested fix. Immutable
//   record for thread-safe aggregation from parallel validators.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Represents a single finding produced by a validator during document validation.
/// </summary>
/// <remarks>
/// <para>
/// Findings are the atomic unit of validation output. Each validator produces
/// zero or more findings, which are aggregated into a <see cref="ValidationResult"/>
/// by the <see cref="IValidationEngine"/>.
/// </para>
/// <para>
/// <b>Immutability:</b> This type is an immutable record, safe for concurrent
/// aggregation from parallel validator execution.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
/// <param name="ValidatorId">
/// The unique identifier of the validator that produced this finding.
/// Used for filtering and attribution in the UI.
/// </param>
/// <param name="Severity">
/// The severity level of this finding. Only <see cref="ValidationSeverity.Error"/>
/// causes the overall <see cref="ValidationResult.IsValid"/> to return <c>false</c>.
/// </param>
/// <param name="Code">
/// A machine-readable error code (e.g., "SCHEMA_001", "AXIOM_VIOLATION").
/// Used for programmatic filtering and localization lookup.
/// </param>
/// <param name="Message">
/// A human-readable description of the finding. Should be clear and actionable.
/// </param>
/// <param name="PropertyPath">
/// Optional dot-separated path to the property that triggered this finding
/// (e.g., "metadata.title", "content.paragraphs[0]"). Null if the finding
/// applies to the document as a whole.
/// </param>
/// <param name="SuggestedFix">
/// Optional human-readable suggestion for resolving the finding.
/// Null if no specific fix can be suggested.
/// </param>
public record ValidationFinding(
    string ValidatorId,
    ValidationSeverity Severity,
    string Code,
    string Message,
    string? PropertyPath = null,
    string? SuggestedFix = null
)
{
    /// <summary>
    /// Creates an error-level finding.
    /// </summary>
    /// <param name="validatorId">The validator that produced this finding.</param>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable description.</param>
    /// <param name="propertyPath">Optional property path.</param>
    /// <param name="suggestedFix">Optional suggested fix.</param>
    /// <returns>A new <see cref="ValidationFinding"/> with <see cref="ValidationSeverity.Error"/>.</returns>
    public static ValidationFinding Error(
        string validatorId,
        string code,
        string message,
        string? propertyPath = null,
        string? suggestedFix = null) =>
        new(validatorId, ValidationSeverity.Error, code, message, propertyPath, suggestedFix);

    /// <summary>
    /// Creates a warning-level finding.
    /// </summary>
    /// <param name="validatorId">The validator that produced this finding.</param>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable description.</param>
    /// <param name="propertyPath">Optional property path.</param>
    /// <param name="suggestedFix">Optional suggested fix.</param>
    /// <returns>A new <see cref="ValidationFinding"/> with <see cref="ValidationSeverity.Warning"/>.</returns>
    public static ValidationFinding Warn(
        string validatorId,
        string code,
        string message,
        string? propertyPath = null,
        string? suggestedFix = null) =>
        new(validatorId, ValidationSeverity.Warning, code, message, propertyPath, suggestedFix);

    /// <summary>
    /// Creates an info-level finding.
    /// </summary>
    /// <param name="validatorId">The validator that produced this finding.</param>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable description.</param>
    /// <param name="propertyPath">Optional property path.</param>
    /// <param name="suggestedFix">Optional suggested fix.</param>
    /// <returns>A new <see cref="ValidationFinding"/> with <see cref="ValidationSeverity.Info"/>.</returns>
    public static ValidationFinding Information(
        string validatorId,
        string code,
        string message,
        string? propertyPath = null,
        string? suggestedFix = null) =>
        new(validatorId, ValidationSeverity.Info, code, message, propertyPath, suggestedFix);
}
