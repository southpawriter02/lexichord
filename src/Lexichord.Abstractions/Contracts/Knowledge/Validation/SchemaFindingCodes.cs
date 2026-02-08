// =============================================================================
// File: SchemaFindingCodes.cs
// Project: Lexichord.Abstractions
// Description: Machine-readable finding codes for schema validation.
// =============================================================================
// LOGIC: Defines constant strings used as the 'Code' field in
//   ValidationFinding instances produced by the SchemaValidatorService.
//   Each code corresponds to a specific class of schema violation,
//   enabling programmatic filtering, UI grouping, and localization.
//
// v0.6.5f: Schema Validator (CKVS Phase 3a)
// Dependencies: None (pure constants)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Machine-readable finding codes produced by the Schema Validator.
/// </summary>
/// <remarks>
/// <para>
/// Each constant corresponds to a specific class of schema violation detected
/// during entity validation. Codes follow the <c>SCHEMA_*</c> prefix convention
/// for namespace isolation from other validator finding codes.
/// </para>
/// <para>
/// <b>Usage:</b> These codes appear in <see cref="ValidationFinding.Code"/> and
/// can be used for:
/// <list type="bullet">
///   <item>Programmatic filtering in the validation results panel.</item>
///   <item>Localization lookup for translated error messages.</item>
///   <item>Telemetry categorization of validation failures.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public static class SchemaFindingCodes
{
    /// <summary>A required property is missing from the entity.</summary>
    public const string RequiredPropertyMissing = "SCHEMA_REQUIRED_PROPERTY";

    /// <summary>A property value does not match the expected type.</summary>
    public const string TypeMismatch = "SCHEMA_TYPE_MISMATCH";

    /// <summary>An enum property value is not in the allowed values list.</summary>
    public const string InvalidEnumValue = "SCHEMA_INVALID_ENUM";

    /// <summary>A property value violates a constraint (generic).</summary>
    public const string ConstraintViolation = "SCHEMA_CONSTRAINT";

    /// <summary>A property is not defined in the entity's type schema.</summary>
    public const string UnknownProperty = "SCHEMA_UNKNOWN_PROPERTY";

    /// <summary>A reference property points to an invalid entity.</summary>
    public const string InvalidReference = "SCHEMA_INVALID_REFERENCE";

    /// <summary>No schema is registered for the entity's type.</summary>
    public const string SchemaNotFound = "SCHEMA_NOT_FOUND";

    /// <summary>A string property does not match its regex pattern constraint.</summary>
    public const string PatternMismatch = "SCHEMA_PATTERN_MISMATCH";

    /// <summary>A numeric property is below its minimum value constraint.</summary>
    public const string ValueTooSmall = "SCHEMA_VALUE_TOO_SMALL";

    /// <summary>A numeric property exceeds its maximum value constraint.</summary>
    public const string ValueTooLarge = "SCHEMA_VALUE_TOO_LARGE";

    /// <summary>A string property is shorter than its maximum length constraint.</summary>
    public const string StringTooLong = "SCHEMA_STRING_TOO_LONG";
}
