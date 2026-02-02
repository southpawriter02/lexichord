// =============================================================================
// File: IAxiomEvaluator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Interface for evaluating axiom rules against entities/relationships.
// =============================================================================
// LOGIC: Defines the contract for evaluating axiom constraints. The evaluator
//   is responsible for interpreting rule definitions and checking them against
//   actual property values. It supports all constraint types defined in the
//   Axiom Data Model (v0.4.6e).
//
// Constraint Types:
//   - Required: Property must be non-null and non-empty
//   - OneOf: Value must match one of allowed values
//   - Range: Numeric value within [Min, Max] bounds
//   - Pattern: String matches regex pattern
//   - Cardinality: Collection count within [MinCount, MaxCount]
//   - NotBoth: At most one of specified properties has value
//   - RequiresTogether: All specified properties present or none
//   - Equals: Property equals expected value
//   - NotEquals: Property does not equal forbidden value
//
// v0.4.6h: Axiom Query API (CKVS Phase 1e)
// Dependencies: Axiom, AxiomRule, AxiomViolation (v0.4.6e),
//               KnowledgeEntity, KnowledgeRelationship (v0.4.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Evaluates axiom rules against entities and relationships.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IAxiomEvaluator"/> is responsible for the core rule evaluation
/// logic. It interprets constraint definitions from <see cref="AxiomRule"/> and
/// checks them against actual property values, returning violations for any
/// failed constraints.
/// </para>
/// <para>
/// <b>Usage:</b> This interface is typically used internally by
/// <see cref="IAxiomStore"/>. Direct usage is rare but supported for
/// advanced scenarios like custom validation pipelines.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. The evaluator
/// is stateless and can be safely shared across threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6h as part of the Axiom Query API.
/// </para>
/// </remarks>
public interface IAxiomEvaluator
{
    /// <summary>
    /// Evaluates all rules in an axiom against an entity.
    /// </summary>
    /// <param name="axiom">The axiom containing rules to evaluate.</param>
    /// <param name="entity">The entity to validate.</param>
    /// <returns>
    /// A list of violations found. Empty list if all rules pass.
    /// </returns>
    /// <remarks>
    /// LOGIC: Iterates through all rules in the axiom. For each rule:
    /// 1. Check if the rule's condition (When clause) is satisfied
    /// 2. If condition passes (or no condition), evaluate the constraint
    /// 3. Collect violations with full context (axiom, rule, property, values)
    /// </remarks>
    /// <example>
    /// <code>
    /// var violations = evaluator.Evaluate(endpointAxiom, endpointEntity);
    /// if (violations.Any())
    /// {
    ///     foreach (var v in violations)
    ///     {
    ///         logger.LogWarning("Violation: {Message}", v.Message);
    ///     }
    /// }
    /// </code>
    /// </example>
    IReadOnlyList<AxiomViolation> Evaluate(Axiom axiom, KnowledgeEntity entity);

    /// <summary>
    /// Evaluates all rules in an axiom against a relationship.
    /// </summary>
    /// <param name="axiom">The axiom containing rules to evaluate.</param>
    /// <param name="relationship">The relationship to validate.</param>
    /// <param name="fromEntity">The source entity of the relationship.</param>
    /// <param name="toEntity">The target entity of the relationship.</param>
    /// <returns>
    /// A list of violations found. Empty list if all rules pass.
    /// </returns>
    /// <remarks>
    /// LOGIC: Builds a merged property dictionary containing:
    /// - Relationship properties
    /// - "from_*" prefixed properties from fromEntity
    /// - "to_*" prefixed properties from toEntity
    /// - Built-in properties: id, type, from_id, to_id, from_type, to_type
    /// </remarks>
    IReadOnlyList<AxiomViolation> Evaluate(
        Axiom axiom,
        KnowledgeRelationship relationship,
        KnowledgeEntity fromEntity,
        KnowledgeEntity toEntity);

    /// <summary>
    /// Evaluates a single rule against a property dictionary.
    /// </summary>
    /// <param name="rule">The rule to evaluate.</param>
    /// <param name="properties">Property values to check against.</param>
    /// <returns>
    /// A violation if the rule fails, or <c>null</c> if it passes.
    /// </returns>
    /// <remarks>
    /// LOGIC: Low-level evaluation method. Does not check conditions (When clause).
    /// Useful for testing individual constraints or custom validation scenarios.
    /// </remarks>
    AxiomViolation? EvaluateRule(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties);
}
