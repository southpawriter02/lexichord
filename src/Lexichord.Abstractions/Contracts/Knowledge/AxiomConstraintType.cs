// =============================================================================
// File: AxiomConstraintType.cs
// Project: Lexichord.Abstractions
// Description: Defines the types of constraints that axiom rules can enforce.
// =============================================================================
// LOGIC: Provides a comprehensive set of constraint types that cover common
//   validation scenarios: presence checks, value constraints, pattern matching,
//   cardinality limits, cross-property rules, and referential integrity.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Types of constraints that axiom rules can enforce.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="AxiomRule.Constraint"/> to specify what kind of
/// validation the rule performs. Each constraint type may use different
/// properties of <see cref="AxiomRule"/> to configure its behavior.
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Constraint</term>
///     <description>AxiomRule Properties Used</description>
///   </listheader>
///   <item>
///     <term><see cref="Required"/></term>
///     <description><c>Property</c></description>
///   </item>
///   <item>
///     <term><see cref="OneOf"/>, <see cref="NotOneOf"/></term>
///     <description><c>Property</c>, <c>Values</c></description>
///   </item>
///   <item>
///     <term><see cref="Range"/></term>
///     <description><c>Property</c>, <c>Min</c>, <c>Max</c></description>
///   </item>
///   <item>
///     <term><see cref="Pattern"/></term>
///     <description><c>Property</c>, <c>Pattern</c></description>
///   </item>
///   <item>
///     <term><see cref="Cardinality"/></term>
///     <description><c>Property</c>, <c>MinCount</c>, <c>MaxCount</c></description>
///   </item>
///   <item>
///     <term><see cref="NotBoth"/>, <see cref="RequiresTogether"/></term>
///     <description><c>Properties</c></description>
///   </item>
///   <item>
///     <term><see cref="Equals"/>, <see cref="NotEquals"/></term>
///     <description><c>Property</c>, <c>Values</c> (first element)</description>
///   </item>
///   <item>
///     <term><see cref="ReferenceExists"/></term>
///     <description><c>Property</c>, <c>ReferenceType</c></description>
///   </item>
/// </list>
/// </remarks>
public enum AxiomConstraintType
{
    /// <summary>
    /// Property must be present and non-null.
    /// </summary>
    Required = 0,

    /// <summary>
    /// Property value must be one of the specified values.
    /// Uses <see cref="AxiomRule.Values"/>.
    /// </summary>
    OneOf = 1,

    /// <summary>
    /// Property value must not be any of the specified values.
    /// Uses <see cref="AxiomRule.Values"/>.
    /// </summary>
    NotOneOf = 2,

    /// <summary>
    /// Property value must be within the range [min, max].
    /// Uses <see cref="AxiomRule.Min"/> and <see cref="AxiomRule.Max"/>.
    /// </summary>
    Range = 3,

    /// <summary>
    /// Property value must match the specified regex pattern.
    /// Uses <see cref="AxiomRule.Pattern"/>.
    /// </summary>
    Pattern = 4,

    /// <summary>
    /// Collection property must have count within the specified bounds.
    /// Uses <see cref="AxiomRule.MinCount"/> and <see cref="AxiomRule.MaxCount"/>.
    /// </summary>
    Cardinality = 5,

    /// <summary>
    /// Properties cannot both have values (mutually exclusive).
    /// Uses <see cref="AxiomRule.Properties"/>.
    /// </summary>
    NotBoth = 6,

    /// <summary>
    /// If one property has a value, the other must too.
    /// Uses <see cref="AxiomRule.Properties"/>.
    /// </summary>
    RequiresTogether = 7,

    /// <summary>
    /// Property value must equal the specified value.
    /// Uses first element of <see cref="AxiomRule.Values"/>.
    /// </summary>
    Equals = 8,

    /// <summary>
    /// Property value must not equal the specified value.
    /// Uses first element of <see cref="AxiomRule.Values"/>.
    /// </summary>
    NotEquals = 9,

    /// <summary>
    /// Property value must be unique across all entities of this type.
    /// </summary>
    Unique = 10,

    /// <summary>
    /// Property must reference an existing entity of the specified type.
    /// Uses <see cref="AxiomRule.ReferenceType"/>.
    /// </summary>
    ReferenceExists = 11,

    /// <summary>
    /// Property value must be valid for its declared schema type.
    /// </summary>
    TypeValid = 12,

    /// <summary>
    /// Custom validation via expression or external validator.
    /// </summary>
    Custom = 13
}
