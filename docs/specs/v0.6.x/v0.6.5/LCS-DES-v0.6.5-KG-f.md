# LCS-DES-065-KG-f: Schema Validator

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-065-KG-f |
| **System Breakdown** | LCS-SBD-065-KG |
| **Version** | v0.6.5 |
| **Codename** | Schema Validator (CKVS Phase 3a) |
| **Estimated Hours** | 5 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Schema Validator** ensures that entities in documents conform to their defined type schemas. It validates that required properties are present, property values have correct types, enums contain valid values, and constraints are satisfied.

### 1.2 Key Responsibilities

- Validate entities against their type schemas
- Check required properties are present
- Verify property value types
- Validate enum values against allowed options
- Check constraints (min/max, pattern, length)
- Generate fix suggestions for schema violations

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Validation/
      Validators/
        Schema/
          ISchemaValidator.cs
          SchemaValidator.cs
          PropertyTypeChecker.cs
          ConstraintEvaluator.cs
```

---

## 2. Interface Definitions

### 2.1 Schema Validator Interface

```csharp
namespace Lexichord.KnowledgeGraph.Validation.Validators.Schema;

/// <summary>
/// Validates entities against their type schemas.
/// </summary>
public interface ISchemaValidator : IValidator
{
    /// <summary>
    /// Validates a single entity against its schema.
    /// </summary>
    /// <param name="entity">Entity to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Schema validation findings.</returns>
    Task<IReadOnlyList<ValidationFinding>> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default);

    /// <summary>
    /// Validates multiple entities in batch.
    /// </summary>
    /// <param name="entities">Entities to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All schema validation findings.</returns>
    Task<IReadOnlyList<ValidationFinding>> ValidateEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);
}
```

### 2.2 Schema Registry Interface

```csharp
/// <summary>
/// Registry of entity type schemas.
/// </summary>
public interface ISchemaRegistry
{
    /// <summary>
    /// Gets schema for an entity type.
    /// </summary>
    /// <param name="entityType">Entity type name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Schema definition or null if not found.</returns>
    Task<EntitySchema?> GetSchemaAsync(
        string entityType,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all registered schemas.
    /// </summary>
    Task<IReadOnlyList<EntitySchema>> GetAllSchemasAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Registers or updates a schema.
    /// </summary>
    Task RegisterSchemaAsync(
        EntitySchema schema,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Entity Schema

```csharp
/// <summary>
/// Schema definition for an entity type.
/// </summary>
public record EntitySchema
{
    /// <summary>
    /// Unique schema ID.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Entity type this schema defines.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Human-readable schema name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Schema description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Property definitions.
    /// </summary>
    public required IReadOnlyList<PropertySchema> Properties { get; init; }

    /// <summary>
    /// Parent schema (for inheritance).
    /// </summary>
    public string? ParentType { get; init; }

    /// <summary>
    /// Schema version.
    /// </summary>
    public int Version { get; init; } = 1;
}

/// <summary>
/// Schema for a single property.
/// </summary>
public record PropertySchema
{
    /// <summary>
    /// Property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Property data type.
    /// </summary>
    public required PropertyType Type { get; init; }

    /// <summary>
    /// Whether property is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Default value (if any).
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Property description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Constraints on the property value.
    /// </summary>
    public PropertyConstraints? Constraints { get; init; }

    /// <summary>
    /// Allowed enum values (for enum type).
    /// </summary>
    public IReadOnlyList<string>? EnumValues { get; init; }

    /// <summary>
    /// For relationship properties, the target entity type.
    /// </summary>
    public string? TargetEntityType { get; init; }
}

/// <summary>
/// Property data types.
/// </summary>
public enum PropertyType
{
    String,
    Integer,
    Float,
    Boolean,
    Date,
    DateTime,
    Enum,
    EntityReference,
    StringArray,
    IntegerArray,
    Object
}

/// <summary>
/// Constraints on property values.
/// </summary>
public record PropertyConstraints
{
    /// <summary>Minimum value (for numbers).</summary>
    public double? MinValue { get; init; }

    /// <summary>Maximum value (for numbers).</summary>
    public double? MaxValue { get; init; }

    /// <summary>Minimum length (for strings/arrays).</summary>
    public int? MinLength { get; init; }

    /// <summary>Maximum length (for strings/arrays).</summary>
    public int? MaxLength { get; init; }

    /// <summary>Regex pattern (for strings).</summary>
    public string? Pattern { get; init; }

    /// <summary>Minimum items (for arrays).</summary>
    public int? MinItems { get; init; }

    /// <summary>Maximum items (for arrays).</summary>
    public int? MaxItems { get; init; }

    /// <summary>Whether items must be unique (for arrays).</summary>
    public bool? UniqueItems { get; init; }
}
```

### 3.2 Schema Finding Codes

```csharp
/// <summary>
/// Schema validation finding codes.
/// </summary>
public static class SchemaFindingCodes
{
    public const string RequiredPropertyMissing = "SCHEMA_REQUIRED_PROPERTY";
    public const string TypeMismatch = "SCHEMA_TYPE_MISMATCH";
    public const string InvalidEnumValue = "SCHEMA_INVALID_ENUM";
    public const string ConstraintViolation = "SCHEMA_CONSTRAINT";
    public const string UnknownProperty = "SCHEMA_UNKNOWN_PROPERTY";
    public const string InvalidReference = "SCHEMA_INVALID_REFERENCE";
    public const string SchemaNotFound = "SCHEMA_NOT_FOUND";
    public const string PatternMismatch = "SCHEMA_PATTERN_MISMATCH";
    public const string ValueTooSmall = "SCHEMA_VALUE_TOO_SMALL";
    public const string ValueTooLarge = "SCHEMA_VALUE_TOO_LARGE";
    public const string StringTooShort = "SCHEMA_STRING_TOO_SHORT";
    public const string StringTooLong = "SCHEMA_STRING_TOO_LONG";
    public const string ArrayTooFew = "SCHEMA_ARRAY_TOO_FEW";
    public const string ArrayTooMany = "SCHEMA_ARRAY_TOO_MANY";
    public const string DuplicateArrayItems = "SCHEMA_DUPLICATE_ITEMS";
}
```

---

## 4. Implementation

### 4.1 Schema Validator Implementation

```csharp
/// <summary>
/// Schema validator implementation.
/// </summary>
public class SchemaValidator : ISchemaValidator
{
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly IPropertyTypeChecker _typeChecker;
    private readonly IConstraintEvaluator _constraintEvaluator;
    private readonly ILogger<SchemaValidator> _logger;

    public string Name => "SchemaValidator";
    public int Priority => 10; // Run first
    public LicenseTier RequiredTier => LicenseTier.WriterPro;
    public bool SupportsStreaming => true;

    public SchemaValidator(
        ISchemaRegistry schemaRegistry,
        IPropertyTypeChecker typeChecker,
        IConstraintEvaluator constraintEvaluator,
        ILogger<SchemaValidator> logger)
    {
        _schemaRegistry = schemaRegistry;
        _typeChecker = typeChecker;
        _constraintEvaluator = constraintEvaluator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();

        // Get entities from linked entities in context
        var entities = context.LinkedEntities
            .Select(le => le.Entity)
            .Where(e => e != null)
            .Cast<KnowledgeEntity>()
            .ToList();

        foreach (var entity in entities)
        {
            var entityFindings = await ValidateEntityAsync(entity, ct);
            findings.AddRange(entityFindings);
        }

        return findings;
    }

    public async Task<IReadOnlyList<ValidationFinding>> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();

        var schema = await _schemaRegistry.GetSchemaAsync(entity.Type, ct);
        if (schema == null)
        {
            findings.Add(CreateFinding(
                SchemaFindingCodes.SchemaNotFound,
                $"No schema found for entity type '{entity.Type}'",
                ValidationSeverity.Warning,
                entity));
            return findings;
        }

        // Check required properties
        findings.AddRange(ValidateRequiredProperties(entity, schema));

        // Check property types and constraints
        foreach (var property in entity.Properties)
        {
            var propertySchema = schema.Properties.FirstOrDefault(
                p => p.Name.Equals(property.Key, StringComparison.OrdinalIgnoreCase));

            if (propertySchema == null)
            {
                findings.Add(CreateFinding(
                    SchemaFindingCodes.UnknownProperty,
                    $"Unknown property '{property.Key}' on entity type '{entity.Type}'",
                    ValidationSeverity.Info,
                    entity));
                continue;
            }

            // Type check
            var typeResult = _typeChecker.CheckType(property.Value, propertySchema.Type);
            if (!typeResult.IsValid)
            {
                findings.Add(CreateFinding(
                    SchemaFindingCodes.TypeMismatch,
                    $"Property '{property.Key}' has type '{typeResult.ActualType}' " +
                    $"but expected '{propertySchema.Type}'",
                    ValidationSeverity.Error,
                    entity,
                    CreateTypeFix(property.Key, propertySchema)));
            }

            // Enum check
            if (propertySchema.Type == PropertyType.Enum)
            {
                findings.AddRange(ValidateEnumValue(entity, property, propertySchema));
            }

            // Constraint check
            if (propertySchema.Constraints != null)
            {
                findings.AddRange(_constraintEvaluator.Evaluate(
                    entity, property.Key, property.Value, propertySchema.Constraints));
            }
        }

        return findings;
    }

    public async Task<IReadOnlyList<ValidationFinding>> ValidateEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();
        foreach (var entity in entities)
        {
            findings.AddRange(await ValidateEntityAsync(entity, ct));
        }
        return findings;
    }

    public async IAsyncEnumerable<ValidationFinding> ValidateStreamingAsync(
        ValidationContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var linkedEntity in context.LinkedEntities)
        {
            if (linkedEntity.Entity != null)
            {
                var findings = await ValidateEntityAsync(linkedEntity.Entity, ct);
                foreach (var finding in findings)
                {
                    yield return finding;
                }
            }
        }
    }

    private IEnumerable<ValidationFinding> ValidateRequiredProperties(
        KnowledgeEntity entity,
        EntitySchema schema)
    {
        var requiredProps = schema.Properties.Where(p => p.IsRequired);

        foreach (var prop in requiredProps)
        {
            if (!entity.Properties.ContainsKey(prop.Name))
            {
                yield return CreateFinding(
                    SchemaFindingCodes.RequiredPropertyMissing,
                    $"Entity '{entity.Name}' missing required property '{prop.Name}'",
                    ValidationSeverity.Error,
                    entity,
                    CreateRequiredPropertyFix(prop));
            }
        }
    }

    private IEnumerable<ValidationFinding> ValidateEnumValue(
        KnowledgeEntity entity,
        KeyValuePair<string, object?> property,
        PropertySchema propertySchema)
    {
        if (propertySchema.EnumValues == null || property.Value == null)
            yield break;

        var value = property.Value.ToString();
        if (!propertySchema.EnumValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            yield return CreateFinding(
                SchemaFindingCodes.InvalidEnumValue,
                $"Property '{property.Key}' has value '{value}' " +
                $"but must be one of: {string.Join(", ", propertySchema.EnumValues)}",
                ValidationSeverity.Error,
                entity,
                CreateEnumFix(property.Key, propertySchema.EnumValues, value));
        }
    }

    private ValidationFinding CreateFinding(
        string code,
        string message,
        ValidationSeverity severity,
        KnowledgeEntity entity,
        ValidationFix? fix = null)
    {
        return new ValidationFinding
        {
            ValidatorName = Name,
            Code = code,
            Message = message,
            Severity = severity,
            RelatedEntity = entity,
            SuggestedFix = fix
        };
    }

    private ValidationFix CreateRequiredPropertyFix(PropertySchema prop)
    {
        var defaultValue = prop.DefaultValue?.ToString() ?? GetDefaultValueForType(prop.Type);
        return new ValidationFix
        {
            Description = $"Add required property '{prop.Name}'",
            ReplacementText = $"{prop.Name}: {defaultValue}",
            Confidence = 0.7f,
            CanAutoApply = prop.DefaultValue != null
        };
    }

    private ValidationFix CreateTypeFix(string propertyName, PropertySchema schema)
    {
        return new ValidationFix
        {
            Description = $"Change '{propertyName}' to type {schema.Type}",
            Confidence = 0.5f,
            CanAutoApply = false
        };
    }

    private ValidationFix? CreateEnumFix(
        string propertyName,
        IReadOnlyList<string> allowedValues,
        string? currentValue)
    {
        // Find closest match using edit distance
        if (currentValue == null) return null;

        var closest = allowedValues
            .Select(v => (Value: v, Distance: LevenshteinDistance(currentValue, v)))
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (closest.Distance <= 3)
        {
            return new ValidationFix
            {
                Description = $"Change '{currentValue}' to '{closest.Value}'",
                ReplacementText = closest.Value,
                Confidence = 1.0f - (closest.Distance / 10.0f),
                CanAutoApply = closest.Distance <= 1
            };
        }

        return new ValidationFix
        {
            Description = $"Use one of: {string.Join(", ", allowedValues)}",
            Confidence = 0.3f,
            CanAutoApply = false
        };
    }

    private string GetDefaultValueForType(PropertyType type) => type switch
    {
        PropertyType.String => "\"\"",
        PropertyType.Integer => "0",
        PropertyType.Float => "0.0",
        PropertyType.Boolean => "false",
        PropertyType.Date => "2026-01-01",
        PropertyType.DateTime => "2026-01-01T00:00:00Z",
        PropertyType.StringArray => "[]",
        PropertyType.IntegerArray => "[]",
        _ => "null"
    };

    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var matrix = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) matrix[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) matrix[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[a.Length, b.Length];
    }
}
```

### 4.2 Property Type Checker

```csharp
/// <summary>
/// Checks property values against expected types.
/// </summary>
public interface IPropertyTypeChecker
{
    /// <summary>
    /// Checks if a value matches the expected type.
    /// </summary>
    TypeCheckResult CheckType(object? value, PropertyType expectedType);
}

/// <summary>
/// Result of a type check.
/// </summary>
public record TypeCheckResult
{
    public bool IsValid { get; init; }
    public string? ActualType { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Property type checker implementation.
/// </summary>
public class PropertyTypeChecker : IPropertyTypeChecker
{
    public TypeCheckResult CheckType(object? value, PropertyType expectedType)
    {
        if (value == null)
        {
            return new TypeCheckResult { IsValid = true, ActualType = "null" };
        }

        var actualType = value.GetType();
        var isValid = expectedType switch
        {
            PropertyType.String => value is string,
            PropertyType.Integer => value is int or long or short or byte,
            PropertyType.Float => value is float or double or decimal,
            PropertyType.Boolean => value is bool,
            PropertyType.Date => value is DateTime or DateOnly or string s && DateTime.TryParse(s, out _),
            PropertyType.DateTime => value is DateTime or DateTimeOffset or string s && DateTime.TryParse(s, out _),
            PropertyType.Enum => value is string or Enum,
            PropertyType.EntityReference => value is Guid or string,
            PropertyType.StringArray => value is IEnumerable<string> or string[],
            PropertyType.IntegerArray => value is IEnumerable<int> or int[],
            PropertyType.Object => true,
            _ => false
        };

        return new TypeCheckResult
        {
            IsValid = isValid,
            ActualType = actualType.Name,
            Message = isValid ? null : $"Expected {expectedType} but got {actualType.Name}"
        };
    }
}
```

### 4.3 Constraint Evaluator

```csharp
/// <summary>
/// Evaluates property constraints.
/// </summary>
public interface IConstraintEvaluator
{
    /// <summary>
    /// Evaluates constraints on a property value.
    /// </summary>
    IReadOnlyList<ValidationFinding> Evaluate(
        KnowledgeEntity entity,
        string propertyName,
        object? value,
        PropertyConstraints constraints);
}

/// <summary>
/// Constraint evaluator implementation.
/// </summary>
public class ConstraintEvaluator : IConstraintEvaluator
{
    public IReadOnlyList<ValidationFinding> Evaluate(
        KnowledgeEntity entity,
        string propertyName,
        object? value,
        PropertyConstraints constraints)
    {
        var findings = new List<ValidationFinding>();

        if (value == null) return findings;

        // Numeric constraints
        if (value is IConvertible convertible)
        {
            try
            {
                var numValue = Convert.ToDouble(convertible);

                if (constraints.MinValue.HasValue && numValue < constraints.MinValue)
                {
                    findings.Add(CreateConstraintFinding(
                        entity, propertyName,
                        SchemaFindingCodes.ValueTooSmall,
                        $"Value {numValue} is less than minimum {constraints.MinValue}"));
                }

                if (constraints.MaxValue.HasValue && numValue > constraints.MaxValue)
                {
                    findings.Add(CreateConstraintFinding(
                        entity, propertyName,
                        SchemaFindingCodes.ValueTooLarge,
                        $"Value {numValue} is greater than maximum {constraints.MaxValue}"));
                }
            }
            catch { /* Not numeric */ }
        }

        // String constraints
        if (value is string strValue)
        {
            if (constraints.MinLength.HasValue && strValue.Length < constraints.MinLength)
            {
                findings.Add(CreateConstraintFinding(
                    entity, propertyName,
                    SchemaFindingCodes.StringTooShort,
                    $"String length {strValue.Length} is less than minimum {constraints.MinLength}"));
            }

            if (constraints.MaxLength.HasValue && strValue.Length > constraints.MaxLength)
            {
                findings.Add(CreateConstraintFinding(
                    entity, propertyName,
                    SchemaFindingCodes.StringTooLong,
                    $"String length {strValue.Length} is greater than maximum {constraints.MaxLength}"));
            }

            if (constraints.Pattern != null)
            {
                var regex = new Regex(constraints.Pattern);
                if (!regex.IsMatch(strValue))
                {
                    findings.Add(CreateConstraintFinding(
                        entity, propertyName,
                        SchemaFindingCodes.PatternMismatch,
                        $"Value '{strValue}' does not match pattern '{constraints.Pattern}'"));
                }
            }
        }

        // Array constraints
        if (value is ICollection collection)
        {
            if (constraints.MinItems.HasValue && collection.Count < constraints.MinItems)
            {
                findings.Add(CreateConstraintFinding(
                    entity, propertyName,
                    SchemaFindingCodes.ArrayTooFew,
                    $"Array has {collection.Count} items but minimum is {constraints.MinItems}"));
            }

            if (constraints.MaxItems.HasValue && collection.Count > constraints.MaxItems)
            {
                findings.Add(CreateConstraintFinding(
                    entity, propertyName,
                    SchemaFindingCodes.ArrayTooMany,
                    $"Array has {collection.Count} items but maximum is {constraints.MaxItems}"));
            }

            if (constraints.UniqueItems == true && value is IEnumerable<object> enumerable)
            {
                var list = enumerable.ToList();
                if (list.Count != list.Distinct().Count())
                {
                    findings.Add(CreateConstraintFinding(
                        entity, propertyName,
                        SchemaFindingCodes.DuplicateArrayItems,
                        "Array contains duplicate items but uniqueItems is true"));
                }
            }
        }

        return findings;
    }

    private ValidationFinding CreateConstraintFinding(
        KnowledgeEntity entity,
        string propertyName,
        string code,
        string message)
    {
        return new ValidationFinding
        {
            ValidatorName = "SchemaValidator",
            Code = code,
            Message = $"Property '{propertyName}' on '{entity.Name}': {message}",
            Severity = ValidationSeverity.Error,
            RelatedEntity = entity
        };
    }
}
```

---

## 5. Predefined Schemas

```csharp
/// <summary>
/// Predefined schemas for common API documentation entities.
/// </summary>
public static class PredefinedSchemas
{
    public static EntitySchema Endpoint => new()
    {
        EntityType = "Endpoint",
        Name = "API Endpoint",
        Description = "An HTTP API endpoint",
        Properties =
        [
            new PropertySchema
            {
                Name = "method",
                Type = PropertyType.Enum,
                IsRequired = true,
                EnumValues = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"]
            },
            new PropertySchema
            {
                Name = "path",
                Type = PropertyType.String,
                IsRequired = true,
                Constraints = new PropertyConstraints { Pattern = @"^/.*$" }
            },
            new PropertySchema
            {
                Name = "description",
                Type = PropertyType.String,
                IsRequired = false
            },
            new PropertySchema
            {
                Name = "deprecated",
                Type = PropertyType.Boolean,
                IsRequired = false,
                DefaultValue = false
            }
        ]
    };

    public static EntitySchema Parameter => new()
    {
        EntityType = "Parameter",
        Name = "API Parameter",
        Description = "A parameter for an API endpoint",
        Properties =
        [
            new PropertySchema
            {
                Name = "name",
                Type = PropertyType.String,
                IsRequired = true
            },
            new PropertySchema
            {
                Name = "location",
                Type = PropertyType.Enum,
                IsRequired = true,
                EnumValues = ["path", "query", "header", "cookie", "body"]
            },
            new PropertySchema
            {
                Name = "type",
                Type = PropertyType.Enum,
                IsRequired = true,
                EnumValues = ["string", "integer", "number", "boolean", "array", "object"]
            },
            new PropertySchema
            {
                Name = "required",
                Type = PropertyType.Boolean,
                IsRequired = false,
                DefaultValue = false
            },
            new PropertySchema
            {
                Name = "default",
                Type = PropertyType.String,
                IsRequired = false
            }
        ]
    };
}
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Schema not found | Warning finding, continue validation |
| Invalid property value | Type mismatch finding with severity |
| Regex compilation fails | Log warning, skip pattern check |
| Circular schema reference | Detect cycles, return error |

---

## 7. Testing Requirements

### 7.1 Unit Tests

| Test Case | Description |
| :-------- | :---------- |
| `ValidateEntity_RequiredPropertyMissing` | Detects missing required properties |
| `ValidateEntity_TypeMismatch` | Detects wrong property types |
| `ValidateEntity_InvalidEnum` | Detects invalid enum values |
| `ValidateEntity_ConstraintViolation` | Detects constraint violations |
| `ValidateEntity_UnknownProperty` | Handles unknown properties |
| `ValidateEntity_SchemaNotFound` | Handles missing schemas |
| `ValidateEntity_AllValid` | Returns empty for valid entity |

### 7.2 Integration Tests

| Test Case | Description |
| :-------- | :---------- |
| `SchemaValidator_WithSchemaRegistry` | Integration with real registry |
| `SchemaValidator_Streaming` | Streaming validation works |

---

## 8. Performance Considerations

- **Schema Caching:** Cache schemas from registry
- **Compiled Regex:** Pre-compile pattern regexes
- **Parallel Entity Validation:** Validate entities concurrently
- **Early Exit:** Stop after max findings reached

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Full schema validation |
| Teams | Full schema validation |
| Enterprise | Custom schemas + validation |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |

---
