// =============================================================================
// File: EntityValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Validation result for a single knowledge entity.
// =============================================================================
// LOGIC: Contains the validation outcome for an individual entity including
//   the entity ID, pass/fail status, and any specific errors.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: ValidationError
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Validation result for a single knowledge entity.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IExtractionValidator.ValidateEntityAsync"/>. Provides
/// entity-specific validation details:
/// </para>
/// <list type="bullet">
///   <item><b>EntityId:</b> Identifies which entity was validated.</item>
///   <item><b>IsValid:</b> Whether the entity passed validation.</item>
///   <item><b>Errors:</b> Specific errors for this entity.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await validator.ValidateEntityAsync(entity, ct);
/// if (!result.IsValid)
/// {
///     Console.WriteLine($"Entity {result.EntityId} failed validation:");
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"  - {error.Message}");
///     }
/// }
/// </code>
/// </example>
public record EntityValidationResult
{
    /// <summary>
    /// ID of the validated entity.
    /// </summary>
    /// <value>The unique identifier of the entity that was validated.</value>
    /// <remarks>
    /// LOGIC: Links the validation result back to the source entity.
    /// Used for correlating results in batch validation scenarios.
    /// </remarks>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Whether the entity passed validation.
    /// </summary>
    /// <value>True if the entity is valid, false if validation failed.</value>
    /// <remarks>
    /// LOGIC: Determined by checking the entity against the schema:
    /// - Type must be registered
    /// - Required properties must be present
    /// - Property types must match schema definitions
    /// </remarks>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors for this entity.
    /// </summary>
    /// <value>
    /// A read-only list of errors specific to this entity.
    /// Empty if validation passed.
    /// </value>
    /// <remarks>
    /// LOGIC: Contains only errors related to this entity. Each error
    /// will have EntityId set to the same value as this result's EntityId.
    /// </remarks>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful entity validation result.
    /// </summary>
    /// <param name="entityId">The ID of the validated entity.</param>
    /// <returns>A passing validation result for the entity.</returns>
    public static EntityValidationResult Success(Guid entityId)
        => new() { EntityId = entityId, IsValid = true };

    /// <summary>
    /// Creates a failed entity validation result.
    /// </summary>
    /// <param name="entityId">The ID of the validated entity.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failing validation result for the entity.</returns>
    public static EntityValidationResult Failed(Guid entityId, IReadOnlyList<ValidationError> errors)
        => new() { EntityId = entityId, IsValid = false, Errors = errors };
}
