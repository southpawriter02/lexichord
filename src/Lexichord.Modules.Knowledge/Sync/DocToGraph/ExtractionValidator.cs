// =============================================================================
// File: ExtractionValidator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Validates extraction results against the knowledge graph schema.
// =============================================================================
// LOGIC: ExtractionValidator checks extracted entities and relationships for
//   schema compliance before graph upsert. It validates entity types, required
//   properties, and relationship references.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: IExtractionValidator, ExtractionResult, ValidationResult,
//               DocToGraphValidationContext, KnowledgeEntity, KnowledgeRelationship
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.DocToGraph;

/// <summary>
/// Service for validating extraction results against the graph schema.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IExtractionValidator"/> to validate entities and relationships
/// before they are upserted to the knowledge graph.
/// </para>
/// <para>
/// <b>Validation Rules:</b>
/// <list type="bullet">
///   <item>Entity type must be non-empty.</item>
///   <item>Entity name must be non-empty.</item>
///   <item>Relationship FromEntityId must reference a valid entity.</item>
///   <item>Relationship ToEntityId must reference a valid entity.</item>
///   <item>Relationship type must be non-empty.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
public sealed class ExtractionValidator : IExtractionValidator
{
    // LOGIC: Known entity types for strict validation.
    // In production, this would come from a schema registry.
    private static readonly HashSet<string> KnownEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Product", "Endpoint", "Parameter", "Component", "Concept",
        "Service", "Method", "Property", "Entity", "Relationship",
        "Document", "Section", "Term", "Definition", "Example"
    };

    private readonly ILogger<ExtractionValidator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractionValidator"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ExtractionValidator(ILogger<ExtractionValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<ValidationResult> ValidateAsync(
        ExtractionResult extraction,
        DocToGraphValidationContext context,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Validating extraction with {EntityCount} entities for document {DocumentId}",
            extraction.AggregatedEntities?.Count ?? 0, context.DocumentId);

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        int entitiesValidated = 0;

        // LOGIC: Validate each aggregated entity.
        if (extraction.AggregatedEntities is null)
        {
            return Task.FromResult(ValidationResult.Success(0, 0));
        }

        foreach (var entity in extraction.AggregatedEntities)
        {
            entitiesValidated++;

            // LOGIC: Check entity type is non-empty.
            if (string.IsNullOrWhiteSpace(entity.EntityType))
            {
                errors.Add(new ValidationError
                {
                    Code = "VAL-001",
                    Message = $"Entity with value '{entity.CanonicalValue}' has empty or null type",
                    Severity = ValidationSeverity.Error
                });
                continue;
            }

            // LOGIC: Check entity name/value is non-empty.
            if (string.IsNullOrWhiteSpace(entity.CanonicalValue))
            {
                errors.Add(new ValidationError
                {
                    Code = "VAL-002",
                    Message = $"Entity of type '{entity.EntityType}' has empty or null value",
                    Severity = ValidationSeverity.Error
                });
                continue;
            }

            // LOGIC: In strict mode, validate against known types.
            if (context.StrictMode)
            {
                if (!KnownEntityTypes.Contains(entity.EntityType))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "VAL-003",
                        Message = $"Entity type '{entity.EntityType}' is not registered in schema",
                        Severity = ValidationSeverity.Error
                    });
                }
            }
            else
            {
                // LOGIC: In lenient mode, unknown types generate warnings.
                if (!KnownEntityTypes.Contains(entity.EntityType))
                {
                    warnings.Add(new ValidationWarning
                    {
                        Code = "WARN-001",
                        Message = $"Entity type '{entity.EntityType}' is not in known types list"
                    });
                }
            }

            // LOGIC: Check allowed entity types filter if specified.
            if (context.AllowedEntityTypes is not null &&
                !context.AllowedEntityTypes.Contains(entity.EntityType))
            {
                errors.Add(new ValidationError
                {
                    Code = "VAL-004",
                    Message = $"Entity type '{entity.EntityType}' is not in allowed types list",
                    Severity = ValidationSeverity.Error
                });
            }

            // LOGIC: Check for low confidence extractions in strict mode.
            if (context.StrictMode && entity.MaxConfidence < 0.5)
            {
                errors.Add(new ValidationError
                {
                    Code = "VAL-005",
                    Message = $"Entity '{entity.CanonicalValue}' has low confidence ({entity.MaxConfidence:P0})",
                    Severity = ValidationSeverity.Error
                });
            }
            else if (entity.MaxConfidence < 0.5)
            {
                warnings.Add(new ValidationWarning
                {
                    Code = "WARN-003",
                    Message = $"Entity '{entity.CanonicalValue}' has low confidence ({entity.MaxConfidence:P0}), may need review"
                });
            }
        }

        var result = new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            EntitiesValidated = entitiesValidated,
            RelationshipsValidated = 0 // Relationships validated separately
        };

        _logger.LogDebug(
            "Validation completed for document {DocumentId}: IsValid={IsValid}, " +
            "Errors={ErrorCount}, Warnings={WarningCount}",
            context.DocumentId, result.IsValid, errors.Count, warnings.Count);

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<EntityValidationResult> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Validating entity {EntityId} of type '{Type}'",
            entity.Id, entity.Type);

        var errors = new List<ValidationError>();

        // LOGIC: Validate entity type is non-empty.
        if (string.IsNullOrWhiteSpace(entity.Type))
        {
            errors.Add(new ValidationError
            {
                Code = "VAL-001",
                Message = "Entity type cannot be empty",
                EntityId = entity.Id,
                Severity = ValidationSeverity.Error
            });
        }

        // LOGIC: Validate entity name is non-empty.
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            errors.Add(new ValidationError
            {
                Code = "VAL-002",
                Message = "Entity name cannot be empty",
                EntityId = entity.Id,
                Severity = ValidationSeverity.Error
            });
        }

        // LOGIC: Validate against known entity types.
        if (!string.IsNullOrWhiteSpace(entity.Type) &&
            !KnownEntityTypes.Contains(entity.Type))
        {
            errors.Add(new ValidationError
            {
                Code = "VAL-003",
                Message = $"Entity type '{entity.Type}' is not registered in schema",
                EntityId = entity.Id,
                Severity = ValidationSeverity.Warning
            });
        }

        var result = errors.Count == 0
            ? EntityValidationResult.Success(entity.Id)
            : EntityValidationResult.Failed(entity.Id, errors);

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<RelationshipValidationResult> ValidateRelationshipsAsync(
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Validating {RelationshipCount} relationships against {EntityCount} entities",
            relationships.Count, entities.Count);

        // LOGIC: Build a set of valid entity IDs for reference checking.
        var validEntityIds = entities.Select(e => e.Id).ToHashSet();
        var invalidRelationships = new List<(Guid RelationshipId, ValidationError Error)>();

        foreach (var relationship in relationships)
        {
            // LOGIC: Validate relationship type is non-empty.
            if (string.IsNullOrWhiteSpace(relationship.Type))
            {
                invalidRelationships.Add((
                    relationship.Id,
                    new ValidationError
                    {
                        Code = "VAL-006",
                        Message = "Relationship type cannot be empty",
                        RelationshipId = relationship.Id,
                        Severity = ValidationSeverity.Error
                    }));
                continue;
            }

            // LOGIC: Validate FromEntityId references a valid entity.
            if (!validEntityIds.Contains(relationship.FromEntityId))
            {
                invalidRelationships.Add((
                    relationship.Id,
                    new ValidationError
                    {
                        Code = "VAL-007",
                        Message = $"FromEntityId '{relationship.FromEntityId}' does not reference a valid entity",
                        RelationshipId = relationship.Id,
                        Severity = ValidationSeverity.Error
                    }));
            }

            // LOGIC: Validate ToEntityId references a valid entity.
            if (!validEntityIds.Contains(relationship.ToEntityId))
            {
                invalidRelationships.Add((
                    relationship.Id,
                    new ValidationError
                    {
                        Code = "VAL-008",
                        Message = $"ToEntityId '{relationship.ToEntityId}' does not reference a valid entity",
                        RelationshipId = relationship.Id,
                        Severity = ValidationSeverity.Error
                    }));
            }

            // LOGIC: Validate self-referential relationships.
            if (relationship.FromEntityId == relationship.ToEntityId)
            {
                invalidRelationships.Add((
                    relationship.Id,
                    new ValidationError
                    {
                        Code = "VAL-009",
                        Message = "Self-referential relationships are not allowed",
                        RelationshipId = relationship.Id,
                        Severity = ValidationSeverity.Warning
                    }));
            }
        }

        var result = invalidRelationships.Count == 0
            ? RelationshipValidationResult.Success()
            : RelationshipValidationResult.Failed(invalidRelationships);

        _logger.LogDebug(
            "Relationship validation completed: AllValid={AllValid}, Invalid={InvalidCount}",
            result.AllValid, invalidRelationships.Count);

        return Task.FromResult(result);
    }
}
