// =============================================================================
// File: ConditionOperator.cs
// Project: Lexichord.Abstractions
// Description: Defines operators for conditional axiom rule evaluation.
// =============================================================================
// LOGIC: Axiom rules can be conditional, only applying when certain conditions
//   are met. This enum defines the comparison operators used in AxiomCondition
//   to evaluate whether a rule should be applied.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Comparison operators for evaluating <see cref="AxiomCondition"/> clauses.
/// </summary>
/// <remarks>
/// Used to determine whether a conditional axiom rule should be applied
/// based on the value of a property.
/// </remarks>
public enum ConditionOperator
{
    /// <summary>
    /// Property value equals the condition value.
    /// </summary>
    Equals = 0,

    /// <summary>
    /// Property value does not equal the condition value.
    /// </summary>
    NotEquals = 1,

    /// <summary>
    /// Property value (string) contains the condition value.
    /// </summary>
    Contains = 2,

    /// <summary>
    /// Property value (string) starts with the condition value.
    /// </summary>
    StartsWith = 3,

    /// <summary>
    /// Property value (string) ends with the condition value.
    /// </summary>
    EndsWith = 4,

    /// <summary>
    /// Property value is greater than the condition value.
    /// </summary>
    GreaterThan = 5,

    /// <summary>
    /// Property value is less than the condition value.
    /// </summary>
    LessThan = 6,

    /// <summary>
    /// Property value is null or missing.
    /// </summary>
    IsNull = 7,

    /// <summary>
    /// Property value is not null.
    /// </summary>
    IsNotNull = 8
}
