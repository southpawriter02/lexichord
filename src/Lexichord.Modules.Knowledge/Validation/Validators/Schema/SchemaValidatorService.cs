// =============================================================================
// File: SchemaValidatorService.cs
// Project: Lexichord.Modules.Knowledge
// Description: IValidator implementation for schema-based entity validation.
// =============================================================================
// LOGIC: Bridges the v0.6.5e validation pipeline (IValidator) with entity-level
//   schema validation logic. When invoked via ValidateAsync, extracts entities
//   from context metadata and validates each against its registered schema.
//
// Validation steps per entity:
//   1. Look up EntityTypeSchema from ISchemaRegistry
//   2. Check required properties (RequiredProperties list)
//   3. For each entity property, find matching PropertySchema
//   4. Type-check via IPropertyTypeChecker
//   5. Enum validation (if PropertyType.Enum)
//   6. Constraint evaluation via IConstraintEvaluator
//   7. Flag unknown properties as info-level findings
//
// Includes Levenshtein distance for enum fix suggestions.
//
// v0.6.5f: Schema Validator (CKVS Phase 3a)
// Dependencies: IValidator (v0.6.5e), ISchemaRegistry (v0.4.5f),
//               IPropertyTypeChecker (v0.6.5f), IConstraintEvaluator (v0.6.5f),
//               KnowledgeEntity (v0.4.5e), ValidationFinding (v0.6.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Schema;

/// <summary>
/// Schema validator that integrates with the v0.6.5e validation pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SchemaValidatorService"/> implements
/// <see cref="ISchemaValidatorService"/> (which extends <see cref="IValidator"/>),
/// enabling it to participate in the validation orchestrator pipeline while also
/// exposing direct entity validation methods.
/// </para>
/// <para>
/// <b>Entity Extraction:</b> When invoked via <see cref="ValidateAsync"/>,
/// entities are extracted from <c>context.Metadata["entities"]</c>. If no
/// entities are present, the validator returns an empty findings list.
/// </para>
/// <para>
/// <b>Fix Suggestions:</b> For enum violations, the validator computes
/// Levenshtein edit distance to find the closest allowed value and generates
/// a suggested fix with a confidence score.
/// </para>
/// <para>
/// <b>License Requirement:</b> <see cref="LicenseTier.WriterPro"/> or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public sealed class SchemaValidatorService : ISchemaValidatorService
{
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly IPropertyTypeChecker _typeChecker;
    private readonly IConstraintEvaluator _constraintEvaluator;
    private readonly ILogger<SchemaValidatorService> _logger;

    /// <inheritdoc />
    public string Id => "schema-validator";

    /// <inheritdoc />
    public string DisplayName => "Schema Validator";

    /// <inheritdoc />
    /// <remarks>
    /// Schema validation is supported in all modes. It is fast enough for
    /// real-time use (target &lt;50ms per entity).
    /// </remarks>
    public ValidationMode SupportedModes => ValidationMode.All;

    /// <inheritdoc />
    public LicenseTier RequiredLicenseTier => LicenseTier.WriterPro;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidatorService"/> class.
    /// </summary>
    /// <param name="schemaRegistry">The schema registry to validate against.</param>
    /// <param name="typeChecker">The property type checker.</param>
    /// <param name="constraintEvaluator">The constraint evaluator.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SchemaValidatorService(
        ISchemaRegistry schemaRegistry,
        IPropertyTypeChecker typeChecker,
        IConstraintEvaluator constraintEvaluator,
        ILogger<SchemaValidatorService> logger)
    {
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _typeChecker = typeChecker ?? throw new ArgumentNullException(nameof(typeChecker));
        _constraintEvaluator = constraintEvaluator ?? throw new ArgumentNullException(nameof(constraintEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "SchemaValidatorService: Initialized — Id={Id}, Modes={Modes}, Tier={Tier}",
            Id, SupportedModes, RequiredLicenseTier);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Extracts entities from <c>context.Metadata["entities"]</c> and validates
    /// each against its type schema. If no entities key is present, returns empty.
    /// </remarks>
    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "SchemaValidatorService: ValidateAsync called for document '{DocumentId}'",
            context.DocumentId);

        // LOGIC: Extract entities from context metadata.
        if (!context.Metadata.TryGetValue("entities", out var entitiesObj) ||
            entitiesObj is not IReadOnlyList<KnowledgeEntity> entities)
        {
            _logger.LogDebug(
                "SchemaValidatorService: No entities found in context metadata for document '{DocumentId}' — skipping",
                context.DocumentId);
            return Array.Empty<ValidationFinding>();
        }

        _logger.LogDebug(
            "SchemaValidatorService: Validating {Count} entities from document '{DocumentId}'",
            entities.Count, context.DocumentId);

        return await ValidateEntitiesAsync(entities, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ValidationFinding>> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "SchemaValidatorService: Validating entity '{EntityName}' (Type={EntityType}, Id={EntityId})",
            entity.Name, entity.Type, entity.Id);

        var findings = new List<ValidationFinding>();

        // LOGIC: Step 1 — Look up schema for entity type.
        var schema = _schemaRegistry.GetEntityType(entity.Type);
        if (schema == null)
        {
            _logger.LogDebug(
                "SchemaValidatorService: No schema found for entity type '{EntityType}'",
                entity.Type);

            findings.Add(ValidationFinding.Warn(
                Id,
                SchemaFindingCodes.SchemaNotFound,
                $"No schema found for entity type '{entity.Type}'",
                suggestedFix: "Register a schema for this entity type in the schema directory"));
            return Task.FromResult<IReadOnlyList<ValidationFinding>>(findings);
        }

        // LOGIC: Step 2 — Check required properties.
        ValidateRequiredProperties(entity, schema, findings);

        // LOGIC: Steps 3-6 — Validate each entity property.
        foreach (var (propName, propValue) in entity.Properties)
        {
            ct.ThrowIfCancellationRequested();

            var propSchema = schema.Properties.FirstOrDefault(
                p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));

            if (propSchema == null)
            {
                // LOGIC: Step 7 — Unknown property (info-level).
                findings.Add(ValidationFinding.Information(
                    Id,
                    SchemaFindingCodes.UnknownProperty,
                    $"Unknown property '{propName}' on entity type '{entity.Type}'",
                    propName));
                continue;
            }

            // LOGIC: Step 4 — Type check.
            var typeResult = _typeChecker.CheckType(propValue, propSchema.Type);
            if (!typeResult.IsValid)
            {
                findings.Add(ValidationFinding.Error(
                    Id,
                    SchemaFindingCodes.TypeMismatch,
                    $"Property '{propName}' has type '{typeResult.ActualType}' but expected '{propSchema.Type}'",
                    propName,
                    $"Change '{propName}' to type {propSchema.Type}"));
            }

            // LOGIC: Step 5 — Enum validation.
            if (propSchema.Type == PropertyType.Enum)
            {
                ValidateEnumValue(entity, propName, propValue, propSchema, findings);
            }

            // LOGIC: Step 6 — Constraint evaluation.
            var constraintFindings = _constraintEvaluator.Evaluate(
                entity, propName, propValue, propSchema);
            findings.AddRange(constraintFindings);
        }

        _logger.LogDebug(
            "SchemaValidatorService: Entity '{EntityName}' validation complete — {Count} finding(s)",
            entity.Name, findings.Count);

        return Task.FromResult<IReadOnlyList<ValidationFinding>>(findings);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ValidationFinding>> ValidateEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "SchemaValidatorService: Validating batch of {Count} entities",
            entities.Count);

        var findings = new List<ValidationFinding>();
        foreach (var entity in entities)
        {
            ct.ThrowIfCancellationRequested();
            var entityFindings = await ValidateEntityAsync(entity, ct);
            findings.AddRange(entityFindings);
        }

        _logger.LogDebug(
            "SchemaValidatorService: Batch validation complete — {Count} total finding(s) across {EntityCount} entities",
            findings.Count, entities.Count);

        return findings;
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>
    /// Validates that all required properties are present and non-empty.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="schema">The entity type schema.</param>
    /// <param name="findings">The findings list to append to.</param>
    private void ValidateRequiredProperties(
        KnowledgeEntity entity,
        EntityTypeSchema schema,
        List<ValidationFinding> findings)
    {
        foreach (var requiredProp in schema.RequiredProperties)
        {
            if (!entity.Properties.ContainsKey(requiredProp) ||
                entity.Properties[requiredProp] == null ||
                (entity.Properties[requiredProp] is string s && string.IsNullOrWhiteSpace(s)))
            {
                _logger.LogDebug(
                    "SchemaValidatorService: Entity '{EntityName}' missing required property '{PropertyName}'",
                    entity.Name, requiredProp);

                // LOGIC: Find the PropertySchema to determine default value.
                var propSchema = schema.Properties.FirstOrDefault(
                    p => p.Name.Equals(requiredProp, StringComparison.OrdinalIgnoreCase));
                var defaultValue = propSchema?.DefaultValue ?? GetDefaultValueHint(propSchema?.Type);

                findings.Add(ValidationFinding.Error(
                    Id,
                    SchemaFindingCodes.RequiredPropertyMissing,
                    $"Entity '{entity.Name}' missing required property '{requiredProp}'",
                    requiredProp,
                    $"Add property '{requiredProp}' with value: {defaultValue}"));
            }
        }
    }

    /// <summary>
    /// Validates an enum property value against the allowed values list.
    /// </summary>
    /// <param name="entity">The entity being validated.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="propSchema">The property schema with EnumValues.</param>
    /// <param name="findings">The findings list to append to.</param>
    private void ValidateEnumValue(
        KnowledgeEntity entity,
        string propertyName,
        object? value,
        PropertySchema propSchema,
        List<ValidationFinding> findings)
    {
        if (propSchema.EnumValues == null || value == null)
            return;

        var strValue = value.ToString();
        if (strValue != null &&
            !propSchema.EnumValues.Contains(strValue, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "SchemaValidatorService: Invalid enum value '{Value}' for property '{PropertyName}' on entity '{EntityName}'",
                strValue, propertyName, entity.Name);

            // LOGIC: Find closest match using Levenshtein distance.
            var suggestedFix = CreateEnumFix(propertyName, propSchema.EnumValues, strValue);

            findings.Add(ValidationFinding.Error(
                Id,
                SchemaFindingCodes.InvalidEnumValue,
                $"Property '{propertyName}' has value '{strValue}' but must be one of: {string.Join(", ", propSchema.EnumValues)}",
                propertyName,
                suggestedFix));
        }
    }

    /// <summary>
    /// Creates a fix suggestion for an invalid enum value using Levenshtein distance.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="allowedValues">The allowed enum values.</param>
    /// <param name="currentValue">The current invalid value.</param>
    /// <returns>A human-readable fix suggestion.</returns>
    private static string CreateEnumFix(
        string propertyName,
        IReadOnlyList<string> allowedValues,
        string currentValue)
    {
        var closest = allowedValues
            .Select(v => (Value: v, Distance: LevenshteinDistance(currentValue, v)))
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (closest.Distance <= 3)
        {
            return $"Change '{currentValue}' to '{closest.Value}'";
        }

        return $"Use one of: {string.Join(", ", allowedValues)}";
    }

    /// <summary>
    /// Gets a default value hint for a property type (for fix suggestions).
    /// </summary>
    /// <param name="type">The property type.</param>
    /// <returns>A string representation of a sensible default.</returns>
    private static string GetDefaultValueHint(PropertyType? type) => type switch
    {
        PropertyType.String => "\"\"",
        PropertyType.Text => "\"\"",
        PropertyType.Number => "0",
        PropertyType.Boolean => "false",
        PropertyType.DateTime => "2026-01-01T00:00:00Z",
        PropertyType.Enum => "(select valid value)",
        PropertyType.Array => "[]",
        PropertyType.Reference => "(entity ID)",
        _ => "null"
    };

    /// <summary>
    /// Computes the Levenshtein (edit) distance between two strings.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns>The number of single-character edits required to transform <paramref name="a"/> into <paramref name="b"/>.</returns>
    internal static int LevenshteinDistance(string a, string b)
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
                var cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[a.Length, b.Length];
    }
}
