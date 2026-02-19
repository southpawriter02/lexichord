// =============================================================================
// File: RelationshipValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Validation result for relationships in an extraction.
// =============================================================================
// LOGIC: Contains the validation outcome for relationship validation
//   including overall pass/fail and list of invalid relationships with errors.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: ValidationError
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Validation result for relationships in an extraction.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IExtractionValidator.ValidateRelationshipsAsync"/>. Contains:
/// </para>
/// <list type="bullet">
///   <item><b>AllValid:</b> Whether all relationships passed validation.</item>
///   <item><b>InvalidRelationships:</b> Details of relationships that failed.</item>
/// </list>
/// <para>
/// <b>Validation Checks:</b>
/// - FromEntityId must reference a valid entity
/// - ToEntityId must reference a valid entity
/// - Relationship type must be compatible with entity types per schema
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await validator.ValidateRelationshipsAsync(relationships, entities, ct);
/// if (!result.AllValid)
/// {
///     foreach (var (relationshipId, error) in result.InvalidRelationships)
///     {
///         Console.WriteLine($"Relationship {relationshipId}: {error.Message}");
///     }
/// }
/// </code>
/// </example>
public record RelationshipValidationResult
{
    /// <summary>
    /// Whether all relationships passed validation.
    /// </summary>
    /// <value>True if all relationships are valid, false if any failed.</value>
    /// <remarks>
    /// LOGIC: Quick check for overall relationship validity. When false,
    /// check InvalidRelationships for details on which relationships failed.
    /// </remarks>
    public required bool AllValid { get; init; }

    /// <summary>
    /// Relationships that failed validation with their errors.
    /// </summary>
    /// <value>
    /// A read-only list of tuples containing the relationship ID and
    /// its validation error. Empty if all relationships are valid.
    /// </value>
    /// <remarks>
    /// LOGIC: Each entry identifies a specific relationship that failed
    /// and the reason for failure. Common errors:
    /// - "FromEntityId references non-existent entity"
    /// - "ToEntityId references non-existent entity"
    /// - "Relationship type 'X' not allowed between entity types 'A' and 'B'"
    /// </remarks>
    public IReadOnlyList<(Guid RelationshipId, ValidationError Error)> InvalidRelationships { get; init; } = [];

    /// <summary>
    /// Creates a successful relationship validation result.
    /// </summary>
    /// <returns>A passing validation result indicating all relationships are valid.</returns>
    public static RelationshipValidationResult Success()
        => new() { AllValid = true };

    /// <summary>
    /// Creates a failed relationship validation result.
    /// </summary>
    /// <param name="invalidRelationships">The invalid relationships with their errors.</param>
    /// <returns>A failing validation result with details of invalid relationships.</returns>
    public static RelationshipValidationResult Failed(
        IReadOnlyList<(Guid RelationshipId, ValidationError Error)> invalidRelationships)
        => new() { AllValid = false, InvalidRelationships = invalidRelationships };
}
