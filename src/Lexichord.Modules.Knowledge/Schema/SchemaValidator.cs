// =============================================================================
// File: SchemaValidator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Validates entities and relationships against their type schemas.
// =============================================================================
// LOGIC: Implements validation logic for knowledge graph entities and
//   relationships. The validator checks:
//   - Entity: type exists, not abstract, name non-empty, required properties
//     present, property types match, property constraints satisfied.
//   - Relationship: type exists, from/to entity types are valid, required
//     relationship properties present.
//
// Validation produces errors (blocking) and warnings (non-blocking). An entity
// or relationship is valid only if there are zero errors.
//
// Error codes: UNKNOWN_ENTITY_TYPE, ABSTRACT_TYPE, NAME_REQUIRED,
//   REQUIRED_PROPERTY_MISSING, TYPE_MISMATCH, INVALID_ENUM_VALUE,
//   MAX_LENGTH_EXCEEDED, PATTERN_MISMATCH, BELOW_MINIMUM, ABOVE_MAXIMUM,
//   UNKNOWN_RELATIONSHIP_TYPE, INVALID_FROM_TYPE, INVALID_TO_TYPE
//
// Warning codes: UNKNOWN_PROPERTY
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// Dependencies: ISchemaRegistry (v0.4.5f), SchemaRecords (v0.4.5f),
//               KnowledgeRecords (v0.4.5e)
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Knowledge.Schema;

/// <summary>
/// Validates entities and relationships against their registered schemas.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SchemaValidator"/> performs comprehensive validation of
/// <see cref="KnowledgeEntity"/> and <see cref="KnowledgeRelationship"/>
/// instances against the schemas in the <see cref="ISchemaRegistry"/>.
/// </para>
/// <para>
/// <b>Entity Validation Order:</b>
/// <list type="number">
///   <item>Entity type exists in schema → UNKNOWN_ENTITY_TYPE</item>
///   <item>Entity type is not abstract → ABSTRACT_TYPE</item>
///   <item>Entity name is non-empty → NAME_REQUIRED</item>
///   <item>Required properties present → REQUIRED_PROPERTY_MISSING</item>
///   <item>Property types match schema → TYPE_MISMATCH, INVALID_ENUM_VALUE</item>
///   <item>Property constraints satisfied → MAX_LENGTH_EXCEEDED, PATTERN_MISMATCH, BELOW_MINIMUM, ABOVE_MAXIMUM</item>
///   <item>Unknown properties flagged → UNKNOWN_PROPERTY (warning only)</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
internal sealed class SchemaValidator
{
    private readonly ISchemaRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidator"/> class.
    /// </summary>
    /// <param name="registry">The schema registry to validate against.</param>
    public SchemaValidator(ISchemaRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Validates an entity against its type schema.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <returns>A <see cref="SchemaValidationResult"/> with errors and warnings.</returns>
    public SchemaValidationResult ValidateEntity(KnowledgeEntity entity)
    {
        var errors = new List<SchemaValidationError>();
        var warnings = new List<SchemaValidationWarning>();

        // LOGIC: Step 1 — Check entity type exists in schema.
        var schema = _registry.GetEntityType(entity.Type);
        if (schema == null)
        {
            return SchemaValidationResult.Invalid(new SchemaValidationError
            {
                Code = "UNKNOWN_ENTITY_TYPE",
                Message = $"Entity type '{entity.Type}' is not defined in schema"
            });
        }

        // LOGIC: Step 2 — Check entity type is not abstract.
        if (schema.IsAbstract)
        {
            errors.Add(new SchemaValidationError
            {
                Code = "ABSTRACT_TYPE",
                Message = $"Cannot create instance of abstract type '{entity.Type}'"
            });
        }

        // LOGIC: Step 3 — Check entity name is non-empty.
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            errors.Add(new SchemaValidationError
            {
                Code = "NAME_REQUIRED",
                Message = "Entity 'name' is required",
                PropertyName = "name"
            });
        }

        // LOGIC: Step 4 — Validate required properties are present and non-empty.
        foreach (var requiredProp in schema.RequiredProperties)
        {
            if (!entity.Properties.ContainsKey(requiredProp) ||
                entity.Properties[requiredProp] == null ||
                (entity.Properties[requiredProp] is string s && string.IsNullOrWhiteSpace(s)))
            {
                errors.Add(new SchemaValidationError
                {
                    Code = "REQUIRED_PROPERTY_MISSING",
                    Message = $"Required property '{requiredProp}' is missing or empty",
                    PropertyName = requiredProp
                });
            }
        }

        // LOGIC: Steps 5-7 — Validate each provided property against schema.
        foreach (var (propName, propValue) in entity.Properties)
        {
            var propSchema = schema.Properties.FirstOrDefault(p =>
                p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));

            if (propSchema == null)
            {
                // LOGIC: Unknown property — add warning (non-blocking).
                warnings.Add(new SchemaValidationWarning
                {
                    Code = "UNKNOWN_PROPERTY",
                    Message = $"Property '{propName}' is not defined in schema for type '{entity.Type}'",
                    PropertyName = propName
                });
                continue;
            }

            // LOGIC: Step 5 — Validate property type.
            var typeErrors = ValidatePropertyType(propSchema, propValue);
            errors.AddRange(typeErrors);

            // LOGIC: Step 6 — Validate property constraints.
            var constraintErrors = ValidatePropertyConstraints(propSchema, propValue);
            errors.AddRange(constraintErrors);
        }

        return new SchemaValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Validates a relationship against its type schema.
    /// </summary>
    /// <param name="relationship">The relationship to validate.</param>
    /// <param name="fromEntity">The source entity.</param>
    /// <param name="toEntity">The target entity.</param>
    /// <returns>A <see cref="SchemaValidationResult"/> with errors and warnings.</returns>
    public SchemaValidationResult ValidateRelationship(
        KnowledgeRelationship relationship,
        KnowledgeEntity fromEntity,
        KnowledgeEntity toEntity)
    {
        var errors = new List<SchemaValidationError>();

        // LOGIC: Step 1 — Check relationship type exists in schema.
        var schema = _registry.GetRelationshipType(relationship.Type);
        if (schema == null)
        {
            return SchemaValidationResult.Invalid(new SchemaValidationError
            {
                Code = "UNKNOWN_RELATIONSHIP_TYPE",
                Message = $"Relationship type '{relationship.Type}' is not defined in schema"
            });
        }

        // LOGIC: Step 2 — Check from entity type is valid for this relationship.
        if (!schema.FromEntityTypes.Contains(fromEntity.Type, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(new SchemaValidationError
            {
                Code = "INVALID_FROM_TYPE",
                Message = $"Relationship '{relationship.Type}' cannot originate from entity type '{fromEntity.Type}'. " +
                          $"Valid types: {string.Join(", ", schema.FromEntityTypes)}"
            });
        }

        // LOGIC: Step 3 — Check to entity type is valid for this relationship.
        if (!schema.ToEntityTypes.Contains(toEntity.Type, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(new SchemaValidationError
            {
                Code = "INVALID_TO_TYPE",
                Message = $"Relationship '{relationship.Type}' cannot target entity type '{toEntity.Type}'. " +
                          $"Valid types: {string.Join(", ", schema.ToEntityTypes)}"
            });
        }

        // LOGIC: Step 4 — Validate required relationship properties.
        if (schema.Properties != null)
        {
            foreach (var propSchema in schema.Properties.Where(p => p.Required))
            {
                if (!relationship.Properties.ContainsKey(propSchema.Name))
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "REQUIRED_PROPERTY_MISSING",
                        Message = $"Required relationship property '{propSchema.Name}' is missing",
                        PropertyName = propSchema.Name
                    });
                }
            }
        }

        return new SchemaValidationResult { Errors = errors };
    }

    /// <summary>
    /// Validates that a property value matches its declared type.
    /// </summary>
    /// <param name="schema">The property schema to validate against.</param>
    /// <param name="value">The actual property value.</param>
    /// <returns>A list of type mismatch errors (empty if valid).</returns>
    private static IEnumerable<SchemaValidationError> ValidatePropertyType(PropertySchema schema, object value)
    {
        var errors = new List<SchemaValidationError>();

        switch (schema.Type)
        {
            case PropertyType.String:
            case PropertyType.Text:
                // LOGIC: String and Text types require the value to be a string.
                if (value is not string)
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "TYPE_MISMATCH",
                        Message = $"Property '{schema.Name}' must be a string",
                        PropertyName = schema.Name,
                        ActualValue = value
                    });
                }
                break;

            case PropertyType.Number:
                // LOGIC: Number type accepts int, long, float, double, or decimal.
                if (value is not (int or long or float or double or decimal))
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "TYPE_MISMATCH",
                        Message = $"Property '{schema.Name}' must be a number",
                        PropertyName = schema.Name,
                        ActualValue = value
                    });
                }
                break;

            case PropertyType.Boolean:
                // LOGIC: Boolean type requires the value to be a bool.
                if (value is not bool)
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "TYPE_MISMATCH",
                        Message = $"Property '{schema.Name}' must be a boolean",
                        PropertyName = schema.Name,
                        ActualValue = value
                    });
                }
                break;

            case PropertyType.Enum:
                // LOGIC: Enum type requires the value's string representation
                // to match one of the declared EnumValues (case-insensitive).
                if (schema.EnumValues != null &&
                    !schema.EnumValues.Contains(value?.ToString(), StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "INVALID_ENUM_VALUE",
                        Message = $"Property '{schema.Name}' must be one of: {string.Join(", ", schema.EnumValues)}",
                        PropertyName = schema.Name,
                        ActualValue = value
                    });
                }
                break;
        }

        return errors;
    }

    /// <summary>
    /// Validates property value constraints (length, pattern, numeric range).
    /// </summary>
    /// <param name="schema">The property schema with constraint definitions.</param>
    /// <param name="value">The actual property value.</param>
    /// <returns>A list of constraint violation errors (empty if valid).</returns>
    private static IEnumerable<SchemaValidationError> ValidatePropertyConstraints(PropertySchema schema, object value)
    {
        var errors = new List<SchemaValidationError>();

        // LOGIC: MaxLength constraint applies to string values.
        if (schema.MaxLength.HasValue && value is string str && str.Length > schema.MaxLength.Value)
        {
            errors.Add(new SchemaValidationError
            {
                Code = "MAX_LENGTH_EXCEEDED",
                Message = $"Property '{schema.Name}' exceeds maximum length of {schema.MaxLength}",
                PropertyName = schema.Name,
                ActualValue = str.Length
            });
        }

        // LOGIC: Pattern constraint applies to string values.
        if (!string.IsNullOrEmpty(schema.Pattern) && value is string s)
        {
            if (!Regex.IsMatch(s, schema.Pattern))
            {
                errors.Add(new SchemaValidationError
                {
                    Code = "PATTERN_MISMATCH",
                    Message = $"Property '{schema.Name}' does not match required pattern",
                    PropertyName = schema.Name,
                    ActualValue = s
                });
            }
        }

        // LOGIC: Numeric range constraints apply to numeric values.
        if ((schema.MinValue.HasValue || schema.MaxValue.HasValue) && value is IConvertible numeric)
        {
            try
            {
                var numValue = Convert.ToDouble(numeric);

                if (schema.MinValue.HasValue && numValue < schema.MinValue.Value)
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "BELOW_MINIMUM",
                        Message = $"Property '{schema.Name}' is below minimum value of {schema.MinValue}",
                        PropertyName = schema.Name,
                        ActualValue = numValue
                    });
                }

                if (schema.MaxValue.HasValue && numValue > schema.MaxValue.Value)
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "ABOVE_MAXIMUM",
                        Message = $"Property '{schema.Name}' exceeds maximum value of {schema.MaxValue}",
                        PropertyName = schema.Name,
                        ActualValue = numValue
                    });
                }
            }
            catch (FormatException)
            {
                // LOGIC: If the value cannot be converted to double, skip range validation.
                // Type validation would have already caught the mismatch.
            }
            catch (OverflowException)
            {
                // LOGIC: If the value overflows double, skip range validation.
            }
        }

        return errors;
    }
}
