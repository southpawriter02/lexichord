// =============================================================================
// File: AxiomRule.cs
// Project: Lexichord.Abstractions
// Description: Defines a single constraint rule within an axiom.
// =============================================================================
// LOGIC: An axiom can have multiple rules, each defining a specific constraint.
//   Rules can target a single property or multiple properties, and can be
//   conditional (only apply when certain conditions are met).
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// A single rule within an <see cref="Axiom"/> that defines a constraint.
/// </summary>
/// <remarks>
/// <para>
/// Rules are the building blocks of axioms. Each rule specifies a constraint
/// that must be satisfied for the axiom to pass validation. Different constraint
/// types use different properties of this record.
/// </para>
/// <para>
/// Key properties by constraint type:
/// </para>
/// <list type="bullet">
///   <item><description>Single-property constraints: Use <see cref="Property"/>.</description></item>
///   <item><description>Multi-property constraints: Use <see cref="Properties"/>.</description></item>
///   <item><description>Value constraints: Use <see cref="Values"/>.</description></item>
///   <item><description>Range constraints: Use <see cref="Min"/> and <see cref="Max"/>.</description></item>
///   <item><description>Cardinality constraints: Use <see cref="MinCount"/> and <see cref="MaxCount"/>.</description></item>
///   <item><description>Pattern constraints: Use <see cref="Pattern"/>.</description></item>
///   <item><description>Reference constraints: Use <see cref="ReferenceType"/>.</description></item>
/// </list>
/// </remarks>
public record AxiomRule
{
    /// <summary>
    /// Property name this rule applies to (for single-property constraints).
    /// </summary>
    /// <remarks>
    /// Used by constraints like <see cref="AxiomConstraintType.Required"/>,
    /// <see cref="AxiomConstraintType.OneOf"/>, <see cref="AxiomConstraintType.Pattern"/>, etc.
    /// </remarks>
    public string? Property { get; init; }

    /// <summary>
    /// Property names this rule applies to (for multi-property constraints).
    /// </summary>
    /// <remarks>
    /// Used by constraints like <see cref="AxiomConstraintType.NotBoth"/> and
    /// <see cref="AxiomConstraintType.RequiresTogether"/>.
    /// </remarks>
    public IReadOnlyList<string>? Properties { get; init; }

    /// <summary>
    /// The type of constraint this rule enforces.
    /// </summary>
    public required AxiomConstraintType Constraint { get; init; }

    /// <summary>
    /// Expected value(s) for equality/one_of/not_one_of constraints.
    /// </summary>
    /// <remarks>
    /// For <see cref="AxiomConstraintType.Equals"/> and <see cref="AxiomConstraintType.NotEquals"/>,
    /// only the first element is used.
    /// </remarks>
    public IReadOnlyList<object>? Values { get; init; }

    /// <summary>
    /// Minimum value for range constraints (inclusive).
    /// </summary>
    /// <remarks>
    /// Used by <see cref="AxiomConstraintType.Range"/>. Can be numeric,
    /// date, or any comparable type.
    /// </remarks>
    public object? Min { get; init; }

    /// <summary>
    /// Maximum value for range constraints (inclusive).
    /// </summary>
    /// <remarks>
    /// Used by <see cref="AxiomConstraintType.Range"/>. Can be numeric,
    /// date, or any comparable type.
    /// </remarks>
    public object? Max { get; init; }

    /// <summary>
    /// Regular expression pattern for pattern matching constraints.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="AxiomConstraintType.Pattern"/>. Should be a valid
    /// .NET regular expression.
    /// </remarks>
    public string? Pattern { get; init; }

    /// <summary>
    /// Minimum count for cardinality constraints (inclusive).
    /// </summary>
    /// <remarks>
    /// Used by <see cref="AxiomConstraintType.Cardinality"/> to specify
    /// the minimum number of items in a collection property.
    /// </remarks>
    public int? MinCount { get; init; }

    /// <summary>
    /// Maximum count for cardinality constraints (inclusive).
    /// </summary>
    /// <remarks>
    /// Used by <see cref="AxiomConstraintType.Cardinality"/> to specify
    /// the maximum number of items in a collection property.
    /// </remarks>
    public int? MaxCount { get; init; }

    /// <summary>
    /// Conditional clause specifying when this rule should be applied.
    /// </summary>
    /// <remarks>
    /// If specified, the rule is only evaluated when the condition is true.
    /// This enables contextual rules like "required parameter cannot have default."
    /// </remarks>
    public AxiomCondition? When { get; init; }

    /// <summary>
    /// Custom error message to display when this rule is violated.
    /// </summary>
    /// <remarks>
    /// If not specified, a default message is generated based on the constraint type.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Entity or relationship type for reference constraints.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="AxiomConstraintType.ReferenceExists"/> to specify
    /// what type the referenced entity must be.
    /// </remarks>
    public string? ReferenceType { get; init; }
}
