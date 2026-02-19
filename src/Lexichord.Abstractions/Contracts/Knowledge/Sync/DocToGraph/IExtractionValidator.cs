// =============================================================================
// File: IExtractionValidator.cs
// Project: Lexichord.Abstractions
// Description: Interface for validating extraction results against graph schema.
// =============================================================================
// LOGIC: Validates extracted entities and relationships before graph upsert
//   to ensure schema compliance, valid references, and data integrity.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: ExtractionResult, ValidationResult, DocToGraphValidationContext,
//               KnowledgeEntity, KnowledgeRelationship, EntityValidationResult,
//               RelationshipValidationResult
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Validates extraction results against the knowledge graph schema.
/// </summary>
/// <remarks>
/// <para>
/// Performs validation before graph upsert to ensure data quality:
/// </para>
/// <list type="bullet">
///   <item><b>Entity Validation:</b> Type registration, required properties, property types.</item>
///   <item><b>Relationship Validation:</b> Valid entity references, type compatibility.</item>
///   <item><b>Schema Compliance:</b> Adherence to registered schema definitions.</item>
/// </list>
/// <para>
/// <b>Validation Modes:</b>
/// - Strict: Requires exact schema compliance.
/// - Lenient: Allows minor deviations with warnings.
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>ExtractionValidator</c> in
/// Lexichord.Modules.Knowledge.Sync.DocToGraph.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new DocToGraphValidationContext
/// {
///     DocumentId = document.Id,
///     StrictMode = true
/// };
///
/// var result = await validator.ValidateAsync(extraction, context, ct);
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"{error.Code}: {error.Message}");
///     }
/// }
/// </code>
/// </example>
public interface IExtractionValidator
{
    /// <summary>
    /// Validates an extraction result against the graph schema.
    /// </summary>
    /// <param name="extraction">The extraction result to validate.</param>
    /// <param name="context">Validation configuration and context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> containing the validation outcome,
    /// including any errors and warnings.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Validation process:
    /// </para>
    /// <list type="number">
    ///   <item>Validate each entity type against schema registry.</item>
    ///   <item>Check required properties are present.</item>
    ///   <item>Validate property types match schema.</item>
    ///   <item>Check relationship entity references are valid.</item>
    ///   <item>Validate relationship types are compatible with entity types.</item>
    /// </list>
    /// </remarks>
    Task<ValidationResult> ValidateAsync(
        ExtractionResult extraction,
        DocToGraphValidationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a single knowledge entity.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An <see cref="EntityValidationResult"/> for the entity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Entity validation checks:
    /// </para>
    /// <list type="bullet">
    ///   <item>Type is registered in schema.</item>
    ///   <item>Name is non-empty.</item>
    ///   <item>Required properties (per schema) are present.</item>
    ///   <item>Property values match expected types.</item>
    /// </list>
    /// </remarks>
    Task<EntityValidationResult> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default);

    /// <summary>
    /// Validates relationships reference valid entities.
    /// </summary>
    /// <param name="relationships">The relationships to validate.</param>
    /// <param name="entities">The entities that relationships may reference.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="RelationshipValidationResult"/> indicating validity of relationships.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Relationship validation checks:
    /// </para>
    /// <list type="bullet">
    ///   <item>FromEntityId exists in entities list or graph.</item>
    ///   <item>ToEntityId exists in entities list or graph.</item>
    ///   <item>Relationship type is valid per schema.</item>
    ///   <item>Entity types are compatible with relationship type per schema rules.</item>
    /// </list>
    /// </remarks>
    Task<RelationshipValidationResult> ValidateRelationshipsAsync(
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);
}
