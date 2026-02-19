// =============================================================================
// File: ValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Result of extraction validation against graph schema.
// =============================================================================
// LOGIC: Contains validation outcome including pass/fail status, errors,
//   warnings, and counts of validated items. Used by IExtractionValidator.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: ValidationError, ValidationWarning
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Result of validating extraction data against the knowledge graph schema.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IExtractionValidator.ValidateAsync"/>. Contains:
/// </para>
/// <list type="bullet">
///   <item><b>IsValid:</b> Overall pass/fail determination.</item>
///   <item><b>Errors:</b> Blocking issues that prevent sync.</item>
///   <item><b>Warnings:</b> Non-blocking issues for review.</item>
///   <item><b>Counts:</b> Statistics on validated items.</item>
/// </list>
/// <para>
/// <b>Validation Logic:</b>
/// - IsValid is true only when there are no errors (warnings allowed).
/// - Critical errors always fail validation.
/// - Regular errors fail unless auto-correction is enabled and succeeds.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await validator.ValidateAsync(extraction, context, ct);
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"[{error.Severity}] {error.Code}: {error.Message}");
///     }
/// }
/// </code>
/// </example>
public record ValidationResult
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    /// <value>True if no errors (warnings allowed), false if any errors exist.</value>
    /// <remarks>
    /// LOGIC: Determines if sync can proceed. True when Errors collection
    /// is empty. Warnings do not affect this value.
    /// </remarks>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors that block sync.
    /// </summary>
    /// <value>
    /// A read-only list of errors. Empty if validation passed.
    /// </value>
    /// <remarks>
    /// LOGIC: Each error represents an issue that must be resolved before
    /// sync can proceed. Includes entity-specific and relationship-specific
    /// errors with their severity and details.
    /// </remarks>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings (non-blocking issues).
    /// </summary>
    /// <value>
    /// A read-only list of warnings. May be non-empty even when IsValid is true.
    /// </value>
    /// <remarks>
    /// LOGIC: Warnings indicate potential issues that don't block sync but
    /// should be reviewed. Examples: deprecated entity type, unusual property values.
    /// </remarks>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Number of entities that were validated.
    /// </summary>
    /// <value>Count of entities processed during validation.</value>
    /// <remarks>
    /// LOGIC: Useful for understanding validation coverage and performance.
    /// Includes both valid and invalid entities.
    /// </remarks>
    public int EntitiesValidated { get; init; }

    /// <summary>
    /// Number of relationships that were validated.
    /// </summary>
    /// <value>Count of relationships processed during validation.</value>
    /// <remarks>
    /// LOGIC: Relationships are validated for valid entity references and
    /// type compatibility. Zero if no relationships in extraction.
    /// </remarks>
    public int RelationshipsValidated { get; init; }

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <param name="entitiesValidated">Number of entities validated.</param>
    /// <param name="relationshipsValidated">Number of relationships validated.</param>
    /// <returns>A passing validation result.</returns>
    /// <remarks>
    /// LOGIC: Factory method for the common success case.
    /// </remarks>
    public static ValidationResult Success(int entitiesValidated = 0, int relationshipsValidated = 0)
        => new()
        {
            IsValid = true,
            EntitiesValidated = entitiesValidated,
            RelationshipsValidated = relationshipsValidated
        };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="entitiesValidated">Number of entities validated.</param>
    /// <param name="relationshipsValidated">Number of relationships validated.</param>
    /// <returns>A failing validation result.</returns>
    /// <remarks>
    /// LOGIC: Factory method for creating failure results.
    /// </remarks>
    public static ValidationResult Failed(
        IReadOnlyList<ValidationError> errors,
        int entitiesValidated = 0,
        int relationshipsValidated = 0)
        => new()
        {
            IsValid = false,
            Errors = errors,
            EntitiesValidated = entitiesValidated,
            RelationshipsValidated = relationshipsValidated
        };
}
