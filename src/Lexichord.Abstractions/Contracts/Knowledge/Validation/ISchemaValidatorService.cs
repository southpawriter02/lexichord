// =============================================================================
// File: ISchemaValidatorService.cs
// Project: Lexichord.Abstractions
// Description: Extended validator interface for entity-level schema validation.
// =============================================================================
// LOGIC: Extends IValidator with entity-specific validation methods so callers
//   can validate individual KnowledgeEntity instances or batches directly,
//   outside the document-oriented ValidationContext pipeline.
//
// v0.6.5f: Schema Validator (CKVS Phase 3a)
// Dependencies: IValidator (v0.6.5e), KnowledgeEntity (v0.4.5e),
//               ValidationFinding (v0.6.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Extended validator interface for entity-level schema validation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ISchemaValidatorService"/> extends <see cref="IValidator"/> to
/// provide direct entity validation methods. While <see cref="IValidator.ValidateAsync"/>
/// operates on a <see cref="ValidationContext"/> (document-centric), these
/// methods allow callers to validate <see cref="KnowledgeEntity"/> instances
/// directly — useful for entity creation, import, and batch operations.
/// </para>
/// <para>
/// <b>Pipeline Integration:</b> The <see cref="IValidator.ValidateAsync"/> method
/// extracts entities from the context metadata and delegates to
/// <see cref="ValidateEntityAsync"/> for each one.
/// </para>
/// <para>
/// <b>License Requirement:</b> <see cref="LicenseTier.WriterPro"/> or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public interface ISchemaValidatorService : IValidator
{
    /// <summary>
    /// Validates a single entity against its type schema.
    /// </summary>
    /// <param name="entity">The <see cref="KnowledgeEntity"/> to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="ValidationFinding"/> instances describing
    /// schema violations. Empty if the entity is fully compliant.
    /// </returns>
    /// <remarks>
    /// LOGIC: Validation checks (in order):
    /// <list type="number">
    ///   <item>Schema exists for entity type.</item>
    ///   <item>Required properties are present and non-empty.</item>
    ///   <item>Property values match declared types.</item>
    ///   <item>Enum values are in the allowed list.</item>
    ///   <item>Constraints (length, pattern, range) are satisfied.</item>
    ///   <item>Unknown properties are flagged as info-level findings.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<ValidationFinding>> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default);

    /// <summary>
    /// Validates multiple entities in batch.
    /// </summary>
    /// <param name="entities">The entities to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All schema validation findings across all entities.</returns>
    Task<IReadOnlyList<ValidationFinding>> ValidateEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);
}
