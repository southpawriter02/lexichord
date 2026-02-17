// -----------------------------------------------------------------------
// <copyright file="FixValidationResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Result of validating a fix suggestion against the linting system.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Validation ensures that a suggested fix:
/// <list type="bullet">
///   <item><description>Resolves the original style violation</description></item>
///   <item><description>Does not introduce new violations</description></item>
///   <item><description>Preserves the semantic meaning of the original text</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Validation Process:</b>
/// <list type="number">
///   <item><description>Apply the suggested fix to the document text</description></item>
///   <item><description>Re-run linting on the fixed text</description></item>
///   <item><description>Check if the original violation is resolved</description></item>
///   <item><description>Check if any new violations were introduced</description></item>
///   <item><description>Compute semantic similarity between original and fixed text</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
public record FixValidationResult
{
    /// <summary>
    /// Whether the fix resolves the original violation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Set to <c>true</c> if re-linting the fixed text no longer
    /// detects the original violation at the same location with the same rule ID.
    /// </remarks>
    public bool ResolvesViolation { get; init; }

    /// <summary>
    /// Whether the fix introduces new violations in the fixed area.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Set to <c>true</c> if re-linting detects violations that
    /// overlap with the fix location and were not present in the original text.
    /// </remarks>
    public bool IntroducesNewViolations { get; init; }

    /// <summary>
    /// List of any new violations introduced by the fix.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains violations detected in the fix area that have
    /// different rule IDs from the original violation. Empty if no new
    /// violations were introduced.
    /// </remarks>
    public IReadOnlyList<StyleViolation>? NewViolations { get; init; }

    /// <summary>
    /// Semantic similarity score between original and fixed text (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Measures how well the fix preserves the original meaning:
    /// <list type="bullet">
    ///   <item><description>1.0 = identical meaning</description></item>
    ///   <item><description>0.9+ = excellent preservation</description></item>
    ///   <item><description>0.7-0.9 = acceptable preservation</description></item>
    ///   <item><description>&lt;0.7 = significant semantic change (warning)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Calculation:</b> Uses word overlap analysis when semantic similarity
    /// service is not available. When available, uses embedding-based similarity.
    /// </para>
    /// </remarks>
    public double SemanticSimilarity { get; init; }

    /// <summary>
    /// Overall validation status.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Summarizes the validation outcome:
    /// <list type="bullet">
    ///   <item><description><see cref="ValidationStatus.Valid"/> — Fix resolves violation, no new issues</description></item>
    ///   <item><description><see cref="ValidationStatus.ValidWithWarnings"/> — Fix resolves violation but has warnings</description></item>
    ///   <item><description><see cref="ValidationStatus.Invalid"/> — Fix doesn't resolve or introduces issues</description></item>
    ///   <item><description><see cref="ValidationStatus.ValidationFailed"/> — Validation process failed</description></item>
    /// </list>
    /// </remarks>
    public ValidationStatus Status { get; init; }

    /// <summary>
    /// Detailed validation message explaining the result.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Provides human-readable explanation of the validation
    /// outcome, suitable for display in the UI.
    /// </remarks>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Creates a valid result indicating the fix successfully resolves the violation.
    /// </summary>
    /// <param name="similarity">Semantic similarity score (default: 0.95).</param>
    /// <returns>A <see cref="FixValidationResult"/> with <see cref="ValidationStatus.Valid"/> status.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating successful validation results.
    /// </remarks>
    public static FixValidationResult Valid(double similarity = 0.95) => new()
    {
        ResolvesViolation = true,
        IntroducesNewViolations = false,
        SemanticSimilarity = similarity,
        Status = ValidationStatus.Valid,
        Message = "Fix resolves violation without introducing new issues."
    };

    /// <summary>
    /// Creates an invalid result indicating the fix should not be applied.
    /// </summary>
    /// <param name="reason">Explanation of why the fix is invalid.</param>
    /// <param name="newViolations">Optional list of new violations introduced.</param>
    /// <returns>A <see cref="FixValidationResult"/> with <see cref="ValidationStatus.Invalid"/> status.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating failed validation results.
    /// </remarks>
    public static FixValidationResult Invalid(
        string reason,
        IReadOnlyList<StyleViolation>? newViolations = null) => new()
    {
        ResolvesViolation = false,
        IntroducesNewViolations = newViolations?.Count > 0,
        NewViolations = newViolations,
        SemanticSimilarity = 0,
        Status = ValidationStatus.Invalid,
        Message = reason
    };

    /// <summary>
    /// Creates a result indicating validation failed due to an error.
    /// </summary>
    /// <param name="errorMessage">Description of the validation error.</param>
    /// <returns>A <see cref="FixValidationResult"/> with <see cref="ValidationStatus.ValidationFailed"/> status.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating validation error results.
    /// </remarks>
    public static FixValidationResult Failed(string errorMessage) => new()
    {
        ResolvesViolation = false,
        IntroducesNewViolations = false,
        SemanticSimilarity = 0,
        Status = ValidationStatus.ValidationFailed,
        Message = errorMessage
    };
}
