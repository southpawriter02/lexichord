// =============================================================================
// File: PropertyTypeChecker.cs
// Project: Lexichord.Modules.Knowledge
// Description: Checks property values against expected PropertyType.
// =============================================================================
// LOGIC: Maps each PropertyType enum value to the set of CLR types that are
//   considered valid. Null values are always valid (required-ness is checked
//   separately). Returns a TypeCheckResult with diagnostic info on failure.
//
// Supported mappings:
//   String/Text → string
//   Number      → int, long, float, double, decimal
//   Boolean     → bool
//   Enum        → string, Enum
//   DateTime    → DateTime, DateTimeOffset, parseable string
//   Reference   → Guid, string
//   Array       → IEnumerable (any enumerable)
//
// v0.6.5f: Schema Validator (CKVS Phase 3a)
// Dependencies: PropertyType (v0.4.5f), TypeCheckResult (v0.6.5f)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;
using System.Collections;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Schema;

/// <summary>
/// Checks property values against expected <see cref="PropertyType"/> declarations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PropertyTypeChecker"/> implements <see cref="IPropertyTypeChecker"/>
/// to determine whether a runtime property value is compatible with the type
/// declared in a <see cref="PropertySchema"/>. Each <see cref="PropertyType"/> maps
/// to one or more acceptable CLR types.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is stateless and safe for concurrent use.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public sealed class PropertyTypeChecker : IPropertyTypeChecker
{
    private readonly ILogger<PropertyTypeChecker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyTypeChecker"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public PropertyTypeChecker(ILogger<PropertyTypeChecker> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public TypeCheckResult CheckType(object? value, PropertyType expectedType)
    {
        // LOGIC: Null values are always valid — required-ness is a separate check.
        if (value == null)
        {
            _logger.LogTrace(
                "PropertyTypeChecker: Null value is valid for type {ExpectedType}",
                expectedType);

            return new TypeCheckResult { IsValid = true, ActualType = "null" };
        }

        var actualType = value.GetType();
        var isValid = expectedType switch
        {
            // LOGIC: String and Text both accept CLR string.
            PropertyType.String => value is string,
            PropertyType.Text => value is string,

            // LOGIC: Number accepts any numeric CLR type.
            PropertyType.Number => value is int or long or float or double or decimal or short or byte,

            // LOGIC: Boolean requires bool.
            PropertyType.Boolean => value is bool,

            // LOGIC: DateTime accepts DateTime, DateTimeOffset, DateOnly, or parseable strings.
            PropertyType.DateTime => value is DateTime or DateTimeOffset or DateOnly
                || (value is string s && DateTime.TryParse(s, out _)),

            // LOGIC: Enum accepts string or CLR Enum (value validated separately).
            PropertyType.Enum => value is string or Enum,

            // LOGIC: Reference accepts Guid or string (entity ID).
            PropertyType.Reference => value is Guid or string,

            // LOGIC: Array accepts any IEnumerable (including typed arrays and lists).
            PropertyType.Array => value is IEnumerable and not string,

            _ => false
        };

        if (!isValid)
        {
            _logger.LogDebug(
                "PropertyTypeChecker: Type mismatch — expected {ExpectedType}, got {ActualType} ({ValueType})",
                expectedType, actualType.Name, value.GetType().FullName);
        }

        return new TypeCheckResult
        {
            IsValid = isValid,
            ActualType = actualType.Name,
            Message = isValid ? null : $"Expected {expectedType} but got {actualType.Name}"
        };
    }
}
