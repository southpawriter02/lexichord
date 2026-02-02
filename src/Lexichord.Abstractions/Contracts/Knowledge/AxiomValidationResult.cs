// =============================================================================
// File: AxiomValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Aggregated result of validating against axioms.
// =============================================================================
// LOGIC: This record aggregates all violations found during validation
//   and provides computed properties for quick status checks and severity
//   grouping. Factory methods simplify creation of common result types.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Aggregated result of validating entities/relationships against axioms.
/// </summary>
/// <remarks>
/// <para>
/// Provides a summary of all violations found during validation, with computed
/// properties for quick filtering by severity. The <see cref="IsValid"/> property
/// only considers errors - warnings and info messages do not invalidate the result.
/// </para>
/// <example>
/// Checking validation result:
/// <code>
/// var result = await axiomValidator.ValidateAsync(entity);
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Violations.Where(v => v.Severity == AxiomSeverity.Error))
///     {
///         Console.WriteLine($"Error: {error.Message}");
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public record AxiomValidationResult
{
    /// <summary>
    /// Whether validation passed (no error-level violations).
    /// </summary>
    /// <value>
    /// <c>true</c> if there are no violations with <see cref="AxiomSeverity.Error"/>;
    /// warnings and info messages do not affect validity.
    /// </value>
    public bool IsValid => !Violations.Any(v => v.Severity == AxiomSeverity.Error);

    /// <summary>
    /// All violations found during validation.
    /// </summary>
    public required IReadOnlyList<AxiomViolation> Violations { get; init; }

    /// <summary>
    /// Number of axioms that were evaluated.
    /// </summary>
    public int AxiomsEvaluated { get; init; }

    /// <summary>
    /// Number of individual rules that were evaluated.
    /// </summary>
    public int RulesEvaluated { get; init; }

    /// <summary>
    /// Time taken to complete validation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets violations grouped by severity level.
    /// </summary>
    public IReadOnlyDictionary<AxiomSeverity, IReadOnlyList<AxiomViolation>> BySeverity =>
        Violations.GroupBy(v => v.Severity)
                  .ToDictionary(g => g.Key, g => (IReadOnlyList<AxiomViolation>)g.ToList());

    /// <summary>
    /// Gets the count of error-level violations.
    /// </summary>
    public int ErrorCount => Violations.Count(v => v.Severity == AxiomSeverity.Error);

    /// <summary>
    /// Gets the count of warning-level violations.
    /// </summary>
    public int WarningCount => Violations.Count(v => v.Severity == AxiomSeverity.Warning);

    /// <summary>
    /// Gets the count of info-level violations.
    /// </summary>
    public int InfoCount => Violations.Count(v => v.Severity == AxiomSeverity.Info);

    /// <summary>
    /// Creates a valid result with no violations.
    /// </summary>
    /// <param name="axiomsEvaluated">Number of axioms that were evaluated.</param>
    /// <param name="rulesEvaluated">Number of rules that were evaluated.</param>
    /// <returns>A valid <see cref="AxiomValidationResult"/> with empty violations.</returns>
    public static AxiomValidationResult Valid(int axiomsEvaluated = 0, int rulesEvaluated = 0) =>
        new()
        {
            Violations = Array.Empty<AxiomViolation>(),
            AxiomsEvaluated = axiomsEvaluated,
            RulesEvaluated = rulesEvaluated
        };

    /// <summary>
    /// Creates a result with the specified violations.
    /// </summary>
    /// <param name="violations">The violations to include in the result.</param>
    /// <param name="axiomsEvaluated">Number of axioms that were evaluated.</param>
    /// <param name="rulesEvaluated">Number of rules that were evaluated.</param>
    /// <returns>An <see cref="AxiomValidationResult"/> containing the violations.</returns>
    public static AxiomValidationResult WithViolations(
        IEnumerable<AxiomViolation> violations,
        int axiomsEvaluated = 0,
        int rulesEvaluated = 0) =>
        new()
        {
            Violations = violations.ToList(),
            AxiomsEvaluated = axiomsEvaluated,
            RulesEvaluated = rulesEvaluated
        };
}
