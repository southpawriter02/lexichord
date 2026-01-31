# LCS-DES-047-KG-g: Entity CRUD Operations

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-047-KG-g |
| **Feature ID** | KG-047g |
| **Feature Name** | Entity CRUD Operations |
| **Target Version** | v0.4.7g |
| **Module Scope** | `Lexichord.Modules.Knowledge` |
| **Swimlane** | Memory |
| **License Tier** | Teams (CRUD), Enterprise (bulk) |
| **Feature Gate Key** | `knowledge.browser.crud` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Users need to manually create, edit, merge, and delete entities in the Knowledge Graph when automatic extraction produces errors or incomplete results.

### 2.2 The Proposed Solution

Implement entity CRUD operations:

- **Create**: Manual entity creation with schema validation.
- **Edit**: Modify entity properties with undo support.
- **Merge**: Combine duplicate entities into one.
- **Delete**: Remove incorrect entities with cascade options.
- **Audit Trail**: Track all changes for accountability.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.4.5-KG: `IGraphRepository` — Graph operations
- v0.4.5f: `ISchemaRegistry` — Schema validation
- v0.4.6-KG: `IAxiomStore` — Axiom validation
- v0.0.7a: `IMediator` — Event publishing

### 3.2 Module Placement

```
Lexichord.Modules.Knowledge/
├── Services/
│   ├── IEntityCrudService.cs
│   └── EntityCrudService.cs
├── Commands/
│   ├── CreateEntityCommand.cs
│   ├── UpdateEntityCommand.cs
│   ├── MergeEntitiesCommand.cs
│   └── DeleteEntityCommand.cs
├── Events/
│   ├── EntityCreatedEvent.cs
│   ├── EntityUpdatedEvent.cs
│   ├── EntitiesMergedEvent.cs
│   └── EntityDeletedEvent.cs
```

---

## 4. Data Contract (The API)

### 4.1 Service Interface

```csharp
namespace Lexichord.Modules.Knowledge.Services;

/// <summary>
/// Service for CRUD operations on Knowledge Graph entities.
/// </summary>
public interface IEntityCrudService
{
    /// <summary>
    /// Creates a new entity in the Knowledge Graph.
    /// </summary>
    /// <param name="command">Create command with entity data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created entity result.</returns>
    Task<EntityOperationResult> CreateAsync(
        CreateEntityCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="command">Update command with changes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Update result.</returns>
    Task<EntityOperationResult> UpdateAsync(
        UpdateEntityCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Merges multiple entities into one.
    /// </summary>
    /// <param name="command">Merge command with source entities.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Merge result with new entity.</returns>
    Task<MergeOperationResult> MergeAsync(
        MergeEntitiesCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an entity from the Knowledge Graph.
    /// </summary>
    /// <param name="command">Delete command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Delete result.</returns>
    Task<EntityOperationResult> DeleteAsync(
        DeleteEntityCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Validates an entity before save.
    /// </summary>
    /// <param name="entity">Entity to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<EntityValidationResult> ValidateAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the change history for an entity.
    /// </summary>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Change history.</returns>
    Task<IReadOnlyList<EntityChangeRecord>> GetHistoryAsync(
        Guid entityId,
        CancellationToken ct = default);
}
```

### 4.2 Commands

```csharp
namespace Lexichord.Modules.Knowledge.Commands;

/// <summary>
/// Command to create a new entity.
/// </summary>
public record CreateEntityCommand
{
    /// <summary>Entity type (must exist in schema).</summary>
    public required string Type { get; init; }

    /// <summary>Entity name.</summary>
    public required string Name { get; init; }

    /// <summary>Entity properties.</summary>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>Source document ID (optional).</summary>
    public Guid? SourceDocumentId { get; init; }

    /// <summary>Skip validation (for imports).</summary>
    public bool SkipValidation { get; init; }
}

/// <summary>
/// Command to update an entity.
/// </summary>
public record UpdateEntityCommand
{
    /// <summary>Entity ID to update.</summary>
    public required Guid EntityId { get; init; }

    /// <summary>New name (null = no change).</summary>
    public string? Name { get; init; }

    /// <summary>Properties to set (key-value).</summary>
    public Dictionary<string, object?>? SetProperties { get; init; }

    /// <summary>Properties to remove.</summary>
    public IReadOnlyList<string>? RemoveProperties { get; init; }

    /// <summary>Change reason for audit.</summary>
    public string? ChangeReason { get; init; }
}

/// <summary>
/// Command to merge entities.
/// </summary>
public record MergeEntitiesCommand
{
    /// <summary>Entity IDs to merge.</summary>
    public required IReadOnlyList<Guid> SourceEntityIds { get; init; }

    /// <summary>ID of entity to keep (others merged into this).</summary>
    public required Guid TargetEntityId { get; init; }

    /// <summary>Property merge strategy.</summary>
    public PropertyMergeStrategy MergeStrategy { get; init; } = PropertyMergeStrategy.KeepTarget;

    /// <summary>Whether to preserve relationships from all sources.</summary>
    public bool PreserveAllRelationships { get; init; } = true;

    /// <summary>Change reason for audit.</summary>
    public string? ChangeReason { get; init; }
}

public enum PropertyMergeStrategy
{
    /// <summary>Keep target entity properties.</summary>
    KeepTarget,

    /// <summary>Keep source entity properties (newest wins).</summary>
    KeepSource,

    /// <summary>Merge all properties (source overwrites target).</summary>
    MergeAll,

    /// <summary>Keep properties with longest string values.</summary>
    KeepLongest
}

/// <summary>
/// Command to delete an entity.
/// </summary>
public record DeleteEntityCommand
{
    /// <summary>Entity ID to delete.</summary>
    public required Guid EntityId { get; init; }

    /// <summary>How to handle relationships.</summary>
    public RelationshipCascadeMode CascadeMode { get; init; } = RelationshipCascadeMode.DeleteRelationships;

    /// <summary>Change reason for audit.</summary>
    public string? ChangeReason { get; init; }

    /// <summary>Force delete even with warnings.</summary>
    public bool Force { get; init; }
}

public enum RelationshipCascadeMode
{
    /// <summary>Delete relationships to/from this entity.</summary>
    DeleteRelationships,

    /// <summary>Fail if entity has relationships.</summary>
    FailIfHasRelationships,

    /// <summary>Orphan relationships (leave dangling).</summary>
    OrphanRelationships
}
```

### 4.3 Results

```csharp
namespace Lexichord.Modules.Knowledge.Commands;

/// <summary>
/// Result of an entity operation.
/// </summary>
public record EntityOperationResult
{
    /// <summary>Whether operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Affected entity.</summary>
    public KnowledgeEntity? Entity { get; init; }

    /// <summary>Validation errors.</summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>Warnings (operation succeeded but has issues).</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static EntityOperationResult Succeeded(KnowledgeEntity entity) =>
        new() { Success = true, Entity = entity };

    public static EntityOperationResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };

    public static EntityOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new() { Success = false, ValidationErrors = errors.ToList() };
}

/// <summary>
/// Result of a merge operation.
/// </summary>
public record MergeOperationResult : EntityOperationResult
{
    /// <summary>IDs of entities that were merged (deleted).</summary>
    public IReadOnlyList<Guid> MergedEntityIds { get; init; } = Array.Empty<Guid>();

    /// <summary>Number of relationships updated.</summary>
    public int RelationshipsUpdated { get; init; }

    /// <summary>Number of document references updated.</summary>
    public int DocumentReferencesUpdated { get; init; }
}

/// <summary>
/// Entity validation result.
/// </summary>
public record EntityValidationResult
{
    /// <summary>Whether entity is valid.</summary>
    public bool IsValid => !Errors.Any();

    /// <summary>Validation errors.</summary>
    public required IReadOnlyList<EntityValidationError> Errors { get; init; }

    /// <summary>Validation warnings.</summary>
    public IReadOnlyList<EntityValidationError> Warnings { get; init; } = Array.Empty<EntityValidationError>();
}

public record EntityValidationError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? PropertyName { get; init; }
}

/// <summary>
/// Record of an entity change for audit.
/// </summary>
public record EntityChangeRecord
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string ChangeType { get; init; }
    public required string UserId { get; init; }
    public string? UserName { get; init; }
    public required string ChangeDescription { get; init; }
    public string? PreviousStateJson { get; init; }
    public string? NewStateJson { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
```

---

## 5. Implementation Logic

### 5.1 EntityCrudService

```csharp
namespace Lexichord.Modules.Knowledge.Services;

public sealed class EntityCrudService : IEntityCrudService
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly IAxiomStore _axiomStore;
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly IUserContext _userContext;
    private readonly ILogger<EntityCrudService> _logger;

    public EntityCrudService(
        IGraphRepository graphRepository,
        ISchemaRegistry schemaRegistry,
        IAxiomStore axiomStore,
        IMediator mediator,
        ILicenseContext licenseContext,
        IUserContext userContext,
        ILogger<EntityCrudService> logger)
    {
        _graphRepository = graphRepository;
        _schemaRegistry = schemaRegistry;
        _axiomStore = axiomStore;
        _mediator = mediator;
        _licenseContext = licenseContext;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<EntityOperationResult> CreateAsync(
        CreateEntityCommand command,
        CancellationToken ct = default)
    {
        EnsureLicensed(LicenseTier.Teams);

        // Validate type exists
        if (!_schemaRegistry.EntityTypes.ContainsKey(command.Type))
        {
            return EntityOperationResult.Failed($"Unknown entity type: {command.Type}");
        }

        var entity = new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Type = command.Type,
            Name = command.Name,
            Properties = new Dictionary<string, object>(command.Properties),
            SourceDocuments = command.SourceDocumentId.HasValue
                ? new List<Guid> { command.SourceDocumentId.Value }
                : new List<Guid>(),
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Validate
        if (!command.SkipValidation)
        {
            var validation = await ValidateAsync(entity, ct);
            if (!validation.IsValid)
            {
                return EntityOperationResult.ValidationFailed(
                    validation.Errors.Select(e => e.Message));
            }
        }

        // Save
        await _graphRepository.CreateEntityAsync(entity, ct);

        // Audit
        await RecordChangeAsync(entity.Id, "Created", null, entity, command.ToString(), ct);

        // Publish event
        await _mediator.Publish(new EntityCreatedEvent(entity), ct);

        _logger.LogInformation("Created entity {EntityId} ({Type})", entity.Id, entity.Type);

        return EntityOperationResult.Succeeded(entity);
    }

    public async Task<EntityOperationResult> UpdateAsync(
        UpdateEntityCommand command,
        CancellationToken ct = default)
    {
        EnsureLicensed(LicenseTier.Teams);

        var entity = await _graphRepository.GetEntityByIdAsync(command.EntityId, ct);
        if (entity == null)
        {
            return EntityOperationResult.Failed($"Entity not found: {command.EntityId}");
        }

        var previousState = entity with { }; // Clone for audit

        // Apply changes
        if (command.Name != null)
        {
            entity = entity with { Name = command.Name };
        }

        var properties = new Dictionary<string, object>(entity.Properties);

        if (command.SetProperties != null)
        {
            foreach (var (key, value) in command.SetProperties)
            {
                if (value == null)
                    properties.Remove(key);
                else
                    properties[key] = value;
            }
        }

        if (command.RemoveProperties != null)
        {
            foreach (var key in command.RemoveProperties)
            {
                properties.Remove(key);
            }
        }

        entity = entity with
        {
            Properties = properties,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Validate
        var validation = await ValidateAsync(entity, ct);
        if (!validation.IsValid)
        {
            return EntityOperationResult.ValidationFailed(
                validation.Errors.Select(e => e.Message));
        }

        // Save
        await _graphRepository.UpdateEntityAsync(entity, ct);

        // Audit
        await RecordChangeAsync(entity.Id, "Updated", previousState, entity, command.ChangeReason, ct);

        // Publish event
        await _mediator.Publish(new EntityUpdatedEvent(previousState, entity), ct);

        _logger.LogInformation("Updated entity {EntityId}", entity.Id);

        return EntityOperationResult.Succeeded(entity);
    }

    public async Task<MergeOperationResult> MergeAsync(
        MergeEntitiesCommand command,
        CancellationToken ct = default)
    {
        EnsureLicensed(LicenseTier.Teams);

        if (command.SourceEntityIds.Count < 2)
        {
            return new MergeOperationResult
            {
                Success = false,
                ErrorMessage = "At least 2 entities required for merge"
            };
        }

        if (!command.SourceEntityIds.Contains(command.TargetEntityId))
        {
            return new MergeOperationResult
            {
                Success = false,
                ErrorMessage = "Target entity must be in source list"
            };
        }

        var target = await _graphRepository.GetEntityByIdAsync(command.TargetEntityId, ct);
        if (target == null)
        {
            return new MergeOperationResult
            {
                Success = false,
                ErrorMessage = $"Target entity not found: {command.TargetEntityId}"
            };
        }

        var entitiesToMerge = new List<KnowledgeEntity>();
        foreach (var id in command.SourceEntityIds.Where(id => id != command.TargetEntityId))
        {
            var entity = await _graphRepository.GetEntityByIdAsync(id, ct);
            if (entity == null)
            {
                return new MergeOperationResult
                {
                    Success = false,
                    ErrorMessage = $"Source entity not found: {id}"
                };
            }
            if (entity.Type != target.Type)
            {
                return new MergeOperationResult
                {
                    Success = false,
                    ErrorMessage = "Cannot merge entities of different types"
                };
            }
            entitiesToMerge.Add(entity);
        }

        // Merge properties
        var mergedProperties = MergeProperties(target, entitiesToMerge, command.MergeStrategy);
        var mergedDocuments = target.SourceDocuments
            .Concat(entitiesToMerge.SelectMany(e => e.SourceDocuments))
            .Distinct()
            .ToList();

        var mergedEntity = target with
        {
            Properties = mergedProperties,
            SourceDocuments = mergedDocuments,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Update relationships
        var relationshipsUpdated = 0;
        if (command.PreserveAllRelationships)
        {
            foreach (var source in entitiesToMerge)
            {
                var relationships = await _graphRepository.GetRelationshipsForEntityAsync(source.Id, ct);
                foreach (var rel in relationships)
                {
                    var updatedRel = rel.FromEntityId == source.Id
                        ? rel with { FromEntityId = target.Id }
                        : rel with { ToEntityId = target.Id };

                    await _graphRepository.UpdateRelationshipAsync(updatedRel, ct);
                    relationshipsUpdated++;
                }
            }
        }

        // Save merged entity
        await _graphRepository.UpdateEntityAsync(mergedEntity, ct);

        // Delete source entities
        foreach (var source in entitiesToMerge)
        {
            await _graphRepository.DeleteEntityAsync(source.Id, ct);
        }

        // Audit
        await RecordChangeAsync(target.Id, "Merged",
            new { Target = target, Sources = entitiesToMerge },
            mergedEntity,
            command.ChangeReason, ct);

        // Publish event
        await _mediator.Publish(new EntitiesMergedEvent(
            entitiesToMerge.Select(e => e.Id).ToList(),
            mergedEntity), ct);

        _logger.LogInformation("Merged {Count} entities into {TargetId}",
            entitiesToMerge.Count + 1, target.Id);

        return new MergeOperationResult
        {
            Success = true,
            Entity = mergedEntity,
            MergedEntityIds = entitiesToMerge.Select(e => e.Id).ToList(),
            RelationshipsUpdated = relationshipsUpdated,
            DocumentReferencesUpdated = mergedDocuments.Count - target.SourceDocuments.Count
        };
    }

    public async Task<EntityOperationResult> DeleteAsync(
        DeleteEntityCommand command,
        CancellationToken ct = default)
    {
        EnsureLicensed(LicenseTier.Teams);

        var entity = await _graphRepository.GetEntityByIdAsync(command.EntityId, ct);
        if (entity == null)
        {
            return EntityOperationResult.Failed($"Entity not found: {command.EntityId}");
        }

        // Check relationships
        var relationships = await _graphRepository.GetRelationshipsForEntityAsync(command.EntityId, ct);
        if (relationships.Any())
        {
            switch (command.CascadeMode)
            {
                case RelationshipCascadeMode.FailIfHasRelationships when !command.Force:
                    return EntityOperationResult.Failed(
                        $"Entity has {relationships.Count} relationships. Use Force to delete anyway.");

                case RelationshipCascadeMode.DeleteRelationships:
                    foreach (var rel in relationships)
                    {
                        await _graphRepository.DeleteRelationshipAsync(rel.Id, ct);
                    }
                    break;
            }
        }

        // Delete
        await _graphRepository.DeleteEntityAsync(command.EntityId, ct);

        // Audit
        await RecordChangeAsync(command.EntityId, "Deleted", entity, null, command.ChangeReason, ct);

        // Publish event
        await _mediator.Publish(new EntityDeletedEvent(entity), ct);

        _logger.LogInformation("Deleted entity {EntityId}", command.EntityId);

        return new EntityOperationResult
        {
            Success = true,
            Entity = entity,
            Warnings = relationships.Any()
                ? new[] { $"Deleted {relationships.Count} relationships" }
                : Array.Empty<string>()
        };
    }

    public async Task<EntityValidationResult> ValidateAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        var errors = new List<EntityValidationError>();
        var warnings = new List<EntityValidationError>();

        // Schema validation
        var schemaResult = _schemaRegistry.ValidateEntity(entity);
        errors.AddRange(schemaResult.Errors.Select(e => new EntityValidationError
        {
            Code = "SCHEMA_ERROR",
            Message = e,
            PropertyName = null
        }));

        // Axiom validation
        var axiomResult = _axiomStore.ValidateEntity(entity);
        foreach (var violation in axiomResult.Violations)
        {
            var error = new EntityValidationError
            {
                Code = violation.Axiom.Id,
                Message = violation.Message,
                PropertyName = violation.PropertyName
            };

            if (violation.Severity == AxiomSeverity.Error)
                errors.Add(error);
            else
                warnings.Add(error);
        }

        return new EntityValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }

    public async Task<IReadOnlyList<EntityChangeRecord>> GetHistoryAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        return await _graphRepository.GetEntityHistoryAsync(entityId, ct);
    }

    private Dictionary<string, object> MergeProperties(
        KnowledgeEntity target,
        IReadOnlyList<KnowledgeEntity> sources,
        PropertyMergeStrategy strategy)
    {
        var result = new Dictionary<string, object>(target.Properties);

        foreach (var source in sources)
        {
            foreach (var (key, value) in source.Properties)
            {
                var shouldUpdate = strategy switch
                {
                    PropertyMergeStrategy.KeepTarget => !result.ContainsKey(key),
                    PropertyMergeStrategy.KeepSource => true,
                    PropertyMergeStrategy.MergeAll => true,
                    PropertyMergeStrategy.KeepLongest =>
                        !result.TryGetValue(key, out var existing) ||
                        (value?.ToString()?.Length ?? 0) > (existing?.ToString()?.Length ?? 0),
                    _ => false
                };

                if (shouldUpdate)
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    private async Task RecordChangeAsync(
        Guid entityId,
        string changeType,
        object? previousState,
        object? newState,
        string? reason,
        CancellationToken ct)
    {
        var record = new EntityChangeRecord
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            ChangeType = changeType,
            UserId = _userContext.UserId,
            UserName = _userContext.UserName,
            ChangeDescription = reason ?? $"{changeType} by {_userContext.UserName}",
            PreviousStateJson = previousState != null ? JsonSerializer.Serialize(previousState) : null,
            NewStateJson = newState != null ? JsonSerializer.Serialize(newState) : null,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _graphRepository.SaveChangeRecordAsync(record, ct);
    }

    private void EnsureLicensed(LicenseTier required)
    {
        if (_licenseContext.Tier < required)
        {
            throw new FeatureNotLicensedException("Entity CRUD", required);
        }
    }
}
```

---

## 6. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7g")]
public class EntityCrudServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidEntity_Succeeds()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateEntityCommand
        {
            Type = "Endpoint",
            Name = "GET /users",
            Properties = new() { ["method"] = "GET", ["path"] = "/users" }
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Entity.Should().NotBeNull();
        result.Entity!.Name.Should().Be("GET /users");
    }

    [Fact]
    public async Task CreateAsync_UnknownType_Fails()
    {
        // Arrange
        var service = CreateService();
        var command = new CreateEntityCommand
        {
            Type = "UnknownType",
            Name = "Test"
        };

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown entity type");
    }

    [Fact]
    public async Task MergeAsync_TwoEntities_CombinesProperties()
    {
        // Arrange
        var service = CreateService();
        var entity1 = await CreateTestEntity("Endpoint", "Entity 1");
        var entity2 = await CreateTestEntity("Endpoint", "Entity 2");

        var command = new MergeEntitiesCommand
        {
            SourceEntityIds = new[] { entity1.Id, entity2.Id },
            TargetEntityId = entity1.Id,
            MergeStrategy = PropertyMergeStrategy.MergeAll
        };

        // Act
        var result = await service.MergeAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.MergedEntityIds.Should().Contain(entity2.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithRelationships_DeletesRelationships()
    {
        // Arrange
        var service = CreateService();
        var entity = await CreateTestEntityWithRelationships();

        var command = new DeleteEntityCommand
        {
            EntityId = entity.Id,
            CascadeMode = RelationshipCascadeMode.DeleteRelationships
        };

        // Act
        var result = await service.DeleteAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().ContainSingle(w => w.Contains("relationships"));
    }
}
```

---

## 7. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Create validates against schema and axioms. |
| 2 | Update preserves unchanged properties. |
| 3 | Merge combines properties per strategy. |
| 4 | Merge updates relationships to point to target. |
| 5 | Delete cascades or fails per cascade mode. |
| 6 | All operations record audit trail. |
| 7 | All operations publish events. |
| 8 | Teams license required for CRUD. |

---

## 8. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `IEntityCrudService` interface | [ ] |
| 2 | `EntityCrudService` implementation | [ ] |
| 3 | Command records (Create, Update, Merge, Delete) | [ ] |
| 4 | Result records | [ ] |
| 5 | Event classes | [ ] |
| 6 | Unit tests | [ ] |

---

## 9. Changelog Entry

```markdown
### Added (v0.4.7g)

- `IEntityCrudService` for entity CRUD operations
- Create, Update, Merge, Delete commands
- Property merge strategies for entity merging
- Cascade modes for relationship handling
- Audit trail for all entity changes
```

---
