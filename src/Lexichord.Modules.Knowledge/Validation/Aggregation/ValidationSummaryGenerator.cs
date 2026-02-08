// =============================================================================
// File: ValidationSummaryGenerator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Generates summary statistics from a validation result.
// =============================================================================
// LOGIC: Static utility that computes ValidationSummary from a ValidationResult.
//   Counts findings by severity, validator, and code. Counts fixable findings
//   (those with non-null SuggestedFix).
//
// Spec Adaptations:
//   - AutoFixableFindings omitted (SuggestedFix is string?, no CanAutoApply)
//   - ValidatorName → ValidatorId
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

namespace Lexichord.Modules.Knowledge.Validation.Aggregation;

/// <summary>
/// Generates <see cref="ValidationSummary"/> statistics from a
/// <see cref="ValidationResult"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a stateless utility class. All computation is done from the
/// findings list on the provided <see cref="ValidationResult"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public static class ValidationSummaryGenerator
{
    /// <summary>
    /// Generates summary statistics from a <see cref="ValidationResult"/>.
    /// </summary>
    /// <param name="result">The validation result to summarize.</param>
    /// <returns>A <see cref="ValidationSummary"/> with computed statistics.</returns>
    /// <remarks>
    /// <para>
    /// Computes:
    /// <list type="bullet">
    ///   <item>Total finding count.</item>
    ///   <item>Finding count grouped by <see cref="ValidationSeverity"/>.</item>
    ///   <item>Finding count grouped by <see cref="ValidationFinding.ValidatorId"/>.</item>
    ///   <item>Finding count grouped by <see cref="ValidationFinding.Code"/>.</item>
    ///   <item>Count of findings with non-null <see cref="ValidationFinding.SuggestedFix"/>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static ValidationSummary Generate(ValidationResult result)
    {
        var findings = result.Findings;

        // LOGIC: Count findings by severity level.
        var bySeverity = findings
            .GroupBy(f => f.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        // LOGIC: Count findings by validator ID.
        var byValidator = findings
            .GroupBy(f => f.ValidatorId)
            .ToDictionary(g => g.Key, g => g.Count());

        // LOGIC: Count findings by machine-readable code.
        var byCode = findings
            .GroupBy(f => f.Code)
            .ToDictionary(g => g.Key, g => g.Count());

        // LOGIC: Count findings that have a suggested fix.
        var fixableCount = findings.Count(f => f.SuggestedFix != null);

        return new ValidationSummary
        {
            TotalFindings = findings.Count,
            BySeverity = bySeverity,
            ByValidator = byValidator,
            ByCode = byCode,
            FixableFindings = fixableCount
        };
    }
}
