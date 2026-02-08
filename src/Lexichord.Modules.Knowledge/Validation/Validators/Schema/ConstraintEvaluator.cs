// =============================================================================
// File: ConstraintEvaluator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Evaluates property constraints (numeric, string, pattern).
// =============================================================================
// LOGIC: Checks property values against constraints defined in PropertySchema:
//   - MinValue / MaxValue → numeric range checks
//   - MaxLength → string length limit
//   - Pattern → regex match for strings
//   - EnumValues → handled upstream by SchemaValidatorService (not here)
//
// Null values are silently skipped (required-ness is a separate check).
// Regex compilation failures are caught and logged rather than thrown.
//
// v0.6.5f: Schema Validator (CKVS Phase 3a)
// Dependencies: PropertySchema (v0.4.5f), ValidationFinding (v0.6.5e),
//               SchemaFindingCodes (v0.6.5f), KnowledgeEntity (v0.4.5e)
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Schema;

/// <summary>
/// Evaluates property constraints declared in a <see cref="PropertySchema"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ConstraintEvaluator"/> checks property values against numeric,
/// string, and pattern constraints from the property schema. Each violation
/// produces a <see cref="ValidationFinding"/> with a specific
/// <see cref="SchemaFindingCodes"/> code.
/// </para>
/// <para>
/// <b>Error Handling:</b> Invalid regex patterns are caught and logged as
/// warnings rather than propagated. This ensures a malformed pattern in one
/// schema does not block validation of other properties.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is stateless beyond the logger and safe
/// for concurrent use.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public sealed class ConstraintEvaluator : IConstraintEvaluator
{
    /// <summary>
    /// The validator ID used in findings produced by this evaluator.
    /// </summary>
    private const string ValidatorId = "schema-validator";

    private readonly ILogger<ConstraintEvaluator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstraintEvaluator"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ConstraintEvaluator(ILogger<ConstraintEvaluator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<ValidationFinding> Evaluate(
        KnowledgeEntity entity,
        string propertyName,
        object? value,
        PropertySchema propertySchema)
    {
        var findings = new List<ValidationFinding>();

        // LOGIC: Null values have no constraint to violate.
        if (value == null) return findings;

        // LOGIC: Numeric range constraints (MinValue / MaxValue).
        if (value is IConvertible convertible &&
            (propertySchema.MinValue.HasValue || propertySchema.MaxValue.HasValue))
        {
            EvaluateNumericConstraints(
                entity, propertyName, convertible,
                propertySchema, findings);
        }

        // LOGIC: String length and pattern constraints.
        if (value is string strValue)
        {
            EvaluateStringConstraints(
                entity, propertyName, strValue,
                propertySchema, findings);
        }

        return findings;
    }

    /// <summary>
    /// Evaluates numeric range constraints (MinValue / MaxValue).
    /// </summary>
    /// <param name="entity">The entity being validated.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="convertible">The numeric value to check.</param>
    /// <param name="schema">The property schema with range constraints.</param>
    /// <param name="findings">The findings list to append to.</param>
    private void EvaluateNumericConstraints(
        KnowledgeEntity entity,
        string propertyName,
        IConvertible convertible,
        PropertySchema schema,
        List<ValidationFinding> findings)
    {
        try
        {
            var numValue = Convert.ToDouble(convertible);

            if (schema.MinValue.HasValue && numValue < schema.MinValue.Value)
            {
                _logger.LogDebug(
                    "ConstraintEvaluator: Property '{PropertyName}' value {Value} < min {Min} on entity '{EntityName}'",
                    propertyName, numValue, schema.MinValue.Value, entity.Name);

                findings.Add(ValidationFinding.Error(
                    ValidatorId,
                    SchemaFindingCodes.ValueTooSmall,
                    $"Property '{propertyName}' on '{entity.Name}': value {numValue} is less than minimum {schema.MinValue.Value}",
                    propertyName,
                    $"Set value to at least {schema.MinValue.Value}"));
            }

            if (schema.MaxValue.HasValue && numValue > schema.MaxValue.Value)
            {
                _logger.LogDebug(
                    "ConstraintEvaluator: Property '{PropertyName}' value {Value} > max {Max} on entity '{EntityName}'",
                    propertyName, numValue, schema.MaxValue.Value, entity.Name);

                findings.Add(ValidationFinding.Error(
                    ValidatorId,
                    SchemaFindingCodes.ValueTooLarge,
                    $"Property '{propertyName}' on '{entity.Name}': value {numValue} is greater than maximum {schema.MaxValue.Value}",
                    propertyName,
                    $"Set value to at most {schema.MaxValue.Value}"));
            }
        }
        catch (FormatException)
        {
            // LOGIC: If the value cannot be converted to double, skip range validation.
            // Type validation would have already caught the mismatch.
            _logger.LogTrace(
                "ConstraintEvaluator: Skipping numeric constraint for '{PropertyName}' — not convertible to double",
                propertyName);
        }
        catch (OverflowException)
        {
            // LOGIC: If the value overflows double, skip range validation.
            _logger.LogTrace(
                "ConstraintEvaluator: Skipping numeric constraint for '{PropertyName}' — double overflow",
                propertyName);
        }
    }

    /// <summary>
    /// Evaluates string constraints (MaxLength, Pattern).
    /// </summary>
    /// <param name="entity">The entity being validated.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="strValue">The string value to check.</param>
    /// <param name="schema">The property schema with string constraints.</param>
    /// <param name="findings">The findings list to append to.</param>
    private void EvaluateStringConstraints(
        KnowledgeEntity entity,
        string propertyName,
        string strValue,
        PropertySchema schema,
        List<ValidationFinding> findings)
    {
        // LOGIC: MaxLength constraint.
        if (schema.MaxLength.HasValue && strValue.Length > schema.MaxLength.Value)
        {
            _logger.LogDebug(
                "ConstraintEvaluator: Property '{PropertyName}' length {Length} > max {Max} on entity '{EntityName}'",
                propertyName, strValue.Length, schema.MaxLength.Value, entity.Name);

            findings.Add(ValidationFinding.Error(
                ValidatorId,
                SchemaFindingCodes.StringTooLong,
                $"Property '{propertyName}' on '{entity.Name}': string length {strValue.Length} exceeds maximum {schema.MaxLength.Value}",
                propertyName,
                $"Shorten to at most {schema.MaxLength.Value} characters"));
        }

        // LOGIC: Pattern constraint.
        if (!string.IsNullOrEmpty(schema.Pattern))
        {
            try
            {
                if (!Regex.IsMatch(strValue, schema.Pattern))
                {
                    _logger.LogDebug(
                        "ConstraintEvaluator: Property '{PropertyName}' value does not match pattern '{Pattern}' on entity '{EntityName}'",
                        propertyName, schema.Pattern, entity.Name);

                    findings.Add(ValidationFinding.Error(
                        ValidatorId,
                        SchemaFindingCodes.PatternMismatch,
                        $"Property '{propertyName}' on '{entity.Name}': value '{strValue}' does not match pattern '{schema.Pattern}'",
                        propertyName));
                }
            }
            catch (ArgumentException ex)
            {
                // LOGIC: Invalid regex pattern in schema — log warning, skip.
                _logger.LogWarning(
                    ex,
                    "ConstraintEvaluator: Invalid regex pattern '{Pattern}' for property '{PropertyName}' on type '{EntityType}'",
                    schema.Pattern, propertyName, entity.Type);
            }
        }
    }
}
