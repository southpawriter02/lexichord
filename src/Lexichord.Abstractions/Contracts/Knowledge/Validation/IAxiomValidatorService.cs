// =============================================================================
// File: IAxiomValidatorService.cs
// Project: Lexichord.Abstractions
// Description: Extended validator interface for axiom-based entity validation.
// =============================================================================
// LOGIC: Extends IValidator with entity-specific axiom validation methods so
//   callers can validate individual KnowledgeEntity instances or batches
//   directly against domain axioms, outside the document-oriented
//   ValidationContext pipeline.
//
// v0.6.5g: Axiom Validator (CKVS Phase 3a)
// Dependencies: IValidator (v0.6.5e), KnowledgeEntity (v0.4.5e),
//               Axiom (v0.4.6e), ValidationFinding (v0.6.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Extended validator interface for axiom-based entity validation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IAxiomValidatorService"/> extends <see cref="IValidator"/> to
/// provide direct entity validation methods against domain axioms. While
/// <see cref="IValidator.ValidateAsync"/> operates on a <see cref="ValidationContext"/>
/// (document-centric), these methods allow callers to validate
/// <see cref="KnowledgeEntity"/> instances directly — useful for entity
/// creation, import, and batch operations.
/// </para>
/// <para>
/// <b>Pipeline Integration:</b> The <see cref="IValidator.ValidateAsync"/> method
/// extracts entities from the context metadata and delegates to
/// <see cref="ValidateEntityAsync"/> for each one. It reuses the existing
/// <see cref="IAxiomEvaluator"/> (v0.4.6h) for rule evaluation and maps
/// <see cref="AxiomViolation"/> instances to <see cref="ValidationFinding"/>
/// for pipeline compatibility.
/// </para>
/// <para>
/// <b>License Requirement:</b> <see cref="LicenseTier.Teams"/> or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5g as part of the Axiom Validator.
/// </para>
/// </remarks>
public interface IAxiomValidatorService : IValidator
{
    /// <summary>
    /// Validates a single entity against all applicable axioms.
    /// </summary>
    /// <param name="entity">The <see cref="KnowledgeEntity"/> to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="ValidationFinding"/> instances describing
    /// axiom violations. Empty if the entity satisfies all applicable axioms.
    /// </returns>
    /// <remarks>
    /// LOGIC: Validation steps (in order):
    /// <list type="number">
    ///   <item>Fetch all enabled axioms from <see cref="IAxiomStore"/>.</item>
    ///   <item>Match axioms by <c>TargetType</c> and <c>TargetKind</c> against the entity.</item>
    ///   <item>Evaluate each matching axiom's rules via <see cref="IAxiomEvaluator"/>.</item>
    ///   <item>Convert <see cref="AxiomViolation"/>s to <see cref="ValidationFinding"/>s.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<ValidationFinding>> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default);

    /// <summary>
    /// Validates multiple entities in batch against all applicable axioms.
    /// </summary>
    /// <param name="entities">The entities to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All axiom validation findings across all entities.</returns>
    Task<IReadOnlyList<ValidationFinding>> ValidateEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the axioms that apply to a given entity based on its type.
    /// </summary>
    /// <param name="entity">The entity to find applicable axioms for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="Axiom"/> instances whose
    /// <c>TargetType</c> matches the entity's type.
    /// </returns>
    /// <remarks>
    /// Useful for UI display of which axioms govern a particular entity type,
    /// or for debugging axiom matching logic.
    /// </remarks>
    Task<IReadOnlyList<Axiom>> GetApplicableAxiomsAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default);
}
