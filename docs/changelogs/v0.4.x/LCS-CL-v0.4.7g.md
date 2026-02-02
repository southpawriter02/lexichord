# Changelog: v0.4.7g — Entity CRUD Operations

**Date:** 2026-02-02  
**Author:** Antigravity  
**Status:** Complete

---

## Summary

Implements the Entity CRUD service for the Knowledge Graph, enabling programmatic creation, updating, merging, and deletion of entities with validation, audit trails, and domain event publishing.

---

## Added

### Abstractions

- **`IEntityCrudService`** — Service interface with 6 methods:
    - `CreateAsync(CreateEntityCommand)` — Creates a new entity
    - `UpdateAsync(UpdateEntityCommand)` — Updates an existing entity
    - `MergeAsync(MergeEntitiesCommand)` — Merges multiple entities into one
    - `DeleteAsync(DeleteEntityCommand)` — Deletes an entity with cascade options
    - `ValidateAsync(KnowledgeEntity)` — Validates entity against schema
    - `GetHistoryAsync(Guid)` — Retrieves audit trail
- **`IGraphRepository`** — Extended with 6 CRUD methods:
    - `CreateEntityAsync()`
    - `UpdateEntityAsync()`
    - `DeleteEntityAsync()`
    - `DeleteRelationshipsForEntityAsync()`
    - `GetChangeHistoryAsync()`
    - `RecordChangeAsync()`

### Commands

- **`CreateEntityCommand`** — Type, Name, Properties, SourceDocumentId, SkipValidation
- **`UpdateEntityCommand`** — EntityId, Name, SetProperties, RemoveProperties, ChangeReason
- **`MergeEntitiesCommand`** — TargetEntityId, SourceEntityIds, MergeStrategy, PreserveAllRelationships
- **`DeleteEntityCommand`** — EntityId, CascadeMode, Force, ChangeReason

### Results

- **`EntityOperationResult`** — Success, Entity, Errors, Warnings with static factory methods
- **`MergeOperationResult`** — MergedEntity, RemovedEntityIds, Success, Errors
- **`EntityValidationResult`** — IsValid, Errors, Warnings
- **`EntityValidationError`** — PropertyName, Code, Message, Severity

### Enums

- **`PropertyMergeStrategy`** — KeepTarget, KeepSource, MergeAll, KeepLongest
- **`RelationshipCascadeMode`** — FailIfHasRelationships, DeleteRelationships, OrphanRelationships
- **`ValidationSeverity`** — Info, Warning, Error

### Domain Events

- **`EntityCreatedEvent`** — Published when an entity is created
- **`EntityUpdatedEvent`** — Published with previous and new state
- **`EntitiesMergedEvent`** — Published with removed IDs and merged entity
- **`EntityDeletedEvent`** — Published with entity ID, name, and type

### Records

- **`EntityChangeRecord`** — Audit trail record with EntityId, Operation, Timestamp, PreviousState, NewState, UserId, ChangeReason

### Services

- **`EntityCrudService`** — Full implementation with:
    - License gating (Teams tier required)
    - Schema validation against `ISchemaRegistry`
    - Audit trail recording via `IGraphRepository`
    - Event publishing via `IMediator`
    - Property merge strategies
    - Relationship cascade modes

### Repository

- **`GraphRepository`** — Extended with Neo4j Cypher queries for CRUD operations

---

## Changed

- **`KnowledgeModule.cs`** — DI registration for `EntityCrudService` (scoped)
- **`Lexichord.Modules.Knowledge.csproj`** — Added MediatR package reference

---

## File Manifest

| File                                                     | Change   |
| -------------------------------------------------------- | -------- |
| `Abstractions/Contracts/Knowledge/IEntityCrudService.cs` | NEW      |
| `Abstractions/Contracts/Knowledge/EntityCrudCommands.cs` | NEW      |
| `Abstractions/Contracts/Knowledge/EntityCrudResults.cs`  | NEW      |
| `Abstractions/Contracts/Knowledge/EntityCrudEnums.cs`    | NEW      |
| `Abstractions/Contracts/Knowledge/EntityCrudEvents.cs`   | NEW      |
| `Abstractions/Contracts/Knowledge/EntityChangeRecord.cs` | NEW      |
| `Abstractions/Contracts/IGraphRepository.cs`             | MODIFIED |
| `Modules.Knowledge/Services/EntityCrudService.cs`        | NEW      |
| `Modules.Knowledge/Graph/GraphRepository.cs`             | MODIFIED |
| `Modules.Knowledge/KnowledgeModule.cs`                   | MODIFIED |
| `Modules.Knowledge/Lexichord.Modules.Knowledge.csproj`   | MODIFIED |

---

## Tests

- `EntityCrudServiceTests.cs` — 20 tests covering:
    - CreateAsync: Valid entity, unknown type, source document linking (3 tests)
    - UpdateAsync: Valid changes, entity not found, property removal (3 tests)
    - MergeAsync: Property combination, no source entities, target in source list (3 tests)
    - DeleteAsync: Success, fail if has relationships, orphan relationships (3 tests)
    - ValidateAsync: Unknown type, missing required property, valid entity (3 tests)
    - License gating: Create, Update, Delete, Merge unlicensed throws (4 tests)
    - GetHistoryAsync: Returns history from repository (1 test)

**Total:** 20 new tests
