// =============================================================================
// File: ValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Aggregated result of document validation.
// =============================================================================
// LOGIC: Aggregates all findings from all validators that ran during a
//   validation pass. IsValid is computed — only Error-level findings cause
//   invalidity. Provides computed grouping (BySeverity, ByValidator) and
//   count properties. Factory methods simplify common construction patterns.
//
// Pattern: Follows AxiomValidationResult conventions for consistency.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Aggregated result of document validation from one or more validators.
/// </summary>
/// <remarks>
/// <para>
/// This record aggregates all <see cref="ValidationFinding"/> instances produced
/// by validators during a single validation pass. The <see cref="IsValid"/>
/// property is computed — only <see cref="ValidationSeverity.Error"/>-level
/// findings cause the result to be invalid.
/// </para>
/// <para>
/// <b>Pattern:</b> Follows the <c>AxiomValidationResult</c> conventions for
/// consistency across the CKVS validation subsystem.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
public record ValidationResult
{
    /// <summary>
    /// Whether validation passed (no error-level findings).
    /// </summary>
    /// <value>
    /// <c>true</c> if there are no findings with <see cref="ValidationSeverity.Error"/>;
    /// warnings and info messages do not affect validity.
    /// </value>
    public bool IsValid => !Findings.Any(f => f.Severity == ValidationSeverity.Error);

    /// <summary>
    /// All findings produced by validators during this validation pass.
    /// </summary>
    public required IReadOnlyList<ValidationFinding> Findings { get; init; }

    /// <summary>
    /// Number of validators that were executed.
    /// </summary>
    public int ValidatorsRun { get; init; }

    /// <summary>
    /// Number of validators that were skipped (due to mode or license filtering).
    /// </summary>
    public int ValidatorsSkipped { get; init; }

    /// <summary>
    /// Time taken to complete the entire validation pass.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the count of error-level findings.
    /// </summary>
    public int ErrorCount => Findings.Count(f => f.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Gets the count of warning-level findings.
    /// </summary>
    public int WarningCount => Findings.Count(f => f.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// Gets the count of info-level findings.
    /// </summary>
    public int InfoCount => Findings.Count(f => f.Severity == ValidationSeverity.Info);

    /// <summary>
    /// Gets findings grouped by severity level.
    /// </summary>
    public IReadOnlyDictionary<ValidationSeverity, IReadOnlyList<ValidationFinding>> BySeverity =>
        Findings.GroupBy(f => f.Severity)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationFinding>)g.ToList());

    /// <summary>
    /// Gets findings grouped by the validator that produced them.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ValidationFinding>> ByValidator =>
        Findings.GroupBy(f => f.ValidatorId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationFinding>)g.ToList());

    /// <summary>
    /// Creates a valid result with no findings.
    /// </summary>
    /// <param name="validatorsRun">Number of validators that were executed.</param>
    /// <param name="validatorsSkipped">Number of validators that were skipped.</param>
    /// <returns>A valid <see cref="ValidationResult"/> with empty findings.</returns>
    public static ValidationResult Valid(int validatorsRun = 0, int validatorsSkipped = 0) =>
        new()
        {
            Findings = Array.Empty<ValidationFinding>(),
            ValidatorsRun = validatorsRun,
            ValidatorsSkipped = validatorsSkipped
        };

    /// <summary>
    /// Creates a result with the specified findings.
    /// </summary>
    /// <param name="findings">The findings to include in the result.</param>
    /// <param name="validatorsRun">Number of validators that were executed.</param>
    /// <param name="validatorsSkipped">Number of validators that were skipped.</param>
    /// <param name="duration">Time taken for validation.</param>
    /// <returns>A <see cref="ValidationResult"/> containing the findings.</returns>
    public static ValidationResult WithFindings(
        IEnumerable<ValidationFinding> findings,
        int validatorsRun = 0,
        int validatorsSkipped = 0,
        TimeSpan duration = default) =>
        new()
        {
            Findings = findings.ToList(),
            ValidatorsRun = validatorsRun,
            ValidatorsSkipped = validatorsSkipped,
            Duration = duration
        };
}
