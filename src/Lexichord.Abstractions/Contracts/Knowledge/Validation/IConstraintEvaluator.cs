// =============================================================================
// File: IConstraintEvaluator.cs
// Project: Lexichord.Abstractions
// Description: Interface for evaluating property value constraints.
// =============================================================================
// LOGIC: Abstracts constraint evaluation so the SchemaValidatorService can
//   delegate numeric range, string length/pattern, and array checks without
//   coupling to a specific implementation.
//
// v0.6.5f: Schema Validator (CKVS Phase 3a)
// Dependencies: PropertySchema (v0.4.5f), ValidationFinding (v0.6.5e),
//               KnowledgeEntity (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Evaluates property constraints declared in a <see cref="PropertySchema"/>.
/// </summary>
/// <remarks>
/// <para>
/// Constraints include numeric range limits (<see cref="PropertySchema.MinValue"/>,
/// <see cref="PropertySchema.MaxValue"/>), string length limits
/// (<see cref="PropertySchema.MaxLength"/>), and regex pattern matching
/// (<see cref="PropertySchema.Pattern"/>). Each violation produces a
/// <see cref="ValidationFinding"/> with the appropriate
/// <see cref="SchemaFindingCodes"/> code.
/// </para>
/// <para>
/// <b>Null Handling:</b> Null values are skipped (no constraint findings).
/// Required-ness is checked separately.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public interface IConstraintEvaluator
{
    /// <summary>
    /// Evaluates constraints on a property value.
    /// </summary>
    /// <param name="entity">The entity being validated (for finding context).</param>
    /// <param name="propertyName">The property name (for finding messages).</param>
    /// <param name="value">The runtime property value.</param>
    /// <param name="propertySchema">The property schema containing constraint definitions.</param>
    /// <returns>
    /// A read-only list of <see cref="ValidationFinding"/> instances for each
    /// constraint violation. Empty if all constraints pass.
    /// </returns>
    IReadOnlyList<ValidationFinding> Evaluate(
        KnowledgeEntity entity,
        string propertyName,
        object? value,
        PropertySchema propertySchema);
}
