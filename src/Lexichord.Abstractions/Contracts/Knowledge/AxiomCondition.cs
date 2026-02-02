// =============================================================================
// File: AxiomCondition.cs
// Project: Lexichord.Abstractions
// Description: Defines a conditional clause for when to apply an axiom rule.
// =============================================================================
// LOGIC: Some axiom rules should only apply under certain conditions, e.g.,
//   "required parameter cannot have default value" only applies when
//   required=true. This record captures the condition that must be met.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// A conditional clause that determines when an axiom rule should be applied.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="AxiomRule.When"/> to make rules conditional. The rule
/// is only evaluated if this condition evaluates to true.
/// </para>
/// <example>
/// Example: Apply a rule only when "required" property equals true:
/// <code>
/// var condition = new AxiomCondition
/// {
///     Property = "required",
///     Operator = ConditionOperator.Equals,
///     Value = true
/// };
/// </code>
/// </example>
/// </remarks>
public record AxiomCondition
{
    /// <summary>
    /// The property name to check for the condition.
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// The comparison operator to use when evaluating the condition.
    /// </summary>
    /// <value>Defaults to <see cref="ConditionOperator.Equals"/>.</value>
    public ConditionOperator Operator { get; init; } = ConditionOperator.Equals;

    /// <summary>
    /// The value to compare against using the specified operator.
    /// </summary>
    public required object Value { get; init; }
}
