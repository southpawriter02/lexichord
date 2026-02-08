// =============================================================================
// File: IPropertyTypeChecker.cs
// Project: Lexichord.Abstractions
// Description: Interface for checking property values against expected types.
// =============================================================================
// LOGIC: Abstracts the logic that determines whether a runtime value
//   matches the PropertyType declared in a PropertySchema. Returns a
//   TypeCheckResult containing validity status, the detected actual type
//   name, and an optional diagnostic message.
//
// v0.6.5f: Schema Validator (CKVS Phase 3a)
// Dependencies: PropertyType (v0.4.5f)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Checks whether a runtime property value matches the expected <see cref="PropertyType"/>.
/// </summary>
/// <remarks>
/// <para>
/// Used by the Schema Validator to perform type-level validation on entity
/// properties. Each <see cref="PropertyType"/> maps to a set of acceptable
/// CLR types (e.g., <see cref="PropertyType.Number"/> accepts
/// <see cref="int"/>, <see cref="long"/>, <see cref="float"/>,
/// <see cref="double"/>, and <see cref="decimal"/>).
/// </para>
/// <para>
/// <b>Null Handling:</b> Null values are considered valid for all types
/// (required-ness is checked separately by the Schema Validator).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public interface IPropertyTypeChecker
{
    /// <summary>
    /// Checks if a value matches the expected property type.
    /// </summary>
    /// <param name="value">The runtime property value (may be null).</param>
    /// <param name="expectedType">The declared <see cref="PropertyType"/> from the schema.</param>
    /// <returns>
    /// A <see cref="TypeCheckResult"/> indicating validity, detected type, and any message.
    /// </returns>
    TypeCheckResult CheckType(object? value, PropertyType expectedType);
}

/// <summary>
/// Result of a property type check.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IPropertyTypeChecker.CheckType"/> to indicate whether
/// a property value matches the expected type. When <see cref="IsValid"/> is
/// <c>false</c>, the <see cref="ActualType"/> and <see cref="Message"/> fields
/// provide diagnostic information for generating <see cref="ValidationFinding"/>
/// instances.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public record TypeCheckResult
{
    /// <summary>
    /// Whether the value matches the expected type.
    /// </summary>
    /// <value><c>true</c> if the value is valid for the expected type; otherwise <c>false</c>.</value>
    public bool IsValid { get; init; }

    /// <summary>
    /// The detected CLR type name of the value (e.g., "String", "Int32", "null").
    /// </summary>
    /// <value>The <see cref="Type.Name"/> of the runtime value, or "null" for null values.</value>
    public string? ActualType { get; init; }

    /// <summary>
    /// Optional diagnostic message when the type check fails.
    /// </summary>
    /// <value>A human-readable description of the type mismatch, or <c>null</c> if valid.</value>
    public string? Message { get; init; }
}
