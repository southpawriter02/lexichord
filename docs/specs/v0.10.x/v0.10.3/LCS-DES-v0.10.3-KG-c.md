# LCS-DES-v0.10.3-KG-c: Design Specification — Entity Merger

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-v0.10.3c` | Entity Resolution sub-part c |
| **Feature Name** | `Entity Merger` | Merge entities preserving relationships |
| **Target Version** | `v0.10.3c` | Third sub-part of v0.10.3-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Canonical Knowledge & Versioned Store module |
| **Swimlane** | `Entity Resolution` | Knowledge graph vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.EntityResolution` | Entity resolution feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.3-KG](./LCS-SBD-v0.10.3-KG.md) | Entity Resolution scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.3-KG S2.1](./LCS-SBD-v0.10.3-KG.md#21-sub-parts) | c = Entity Merger |

---

## 2. Executive Summary

### 2.1 The Requirement

When duplicate entities are confirmed, merge them into a single canonical entity while:

1. Preserving all relationships from both entities
2. Merging claims with conflict detection
3. Updating all document links to point to merged entity
4. Creating aliases for merged entity names
5. Supporting full undo via version service

### 2.2 The Proposed Solution

Implement `IEntityMerger` with:

1. **PreviewMergeAsync:** Show merge impact without executing
2. **MergeAsync:** Execute merge with conflict resolution
3. **UnmergeAsync:** Undo merge operation
4. Conflict detection and resolution strategies
5. Integration with version service for undo

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Graph modifications (relationships, claims) |
| `IEntityBrowser` | v0.4.7-KG | Entity CRUD and alias management |
| `IGraphVersionService` | v0.10.1-KG | Undo/redo support for merge operations |
| `IMediator` | v0.0.7a | Event publishing for merge completion |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Merger uses standard .NET libraries |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Not available
- **Teams Tier:** Full merge with undo support
- **Enterprise Tier:** Full merge with advanced conflict resolution

---

## 4. Data Contract (The API)

### 4.1 IEntityMerger Interface

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Merges duplicate entities into a single canonical entity.
/// Preserves all relationships, claims, and document links.
/// </summary>
public interface IEntityMerger
{
    /// <summary>
    /// Previews a merge without executing it.
    /// Shows impact of merge, conflicts, and suggested resolutions.
    /// </summary>
    /// <param name="primaryEntityId">ID of entity to merge into (primary)</param>
    /// <param name="secondaryEntityIds">IDs of entities to merge (secondaries)</param>
    /// <param name="options">Merge configuration (conflict resolution, aliases)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Preview showing merge impact and conflicts</returns>
    Task<MergePreview> PreviewMergeAsync(
        Guid primaryEntityId,
        IReadOnlyList<Guid> secondaryEntityIds,
        MergeOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a merge operation.
    /// Transfers relationships, claims, and updates document links.
    /// </summary>
    /// <param name="primaryEntityId">ID of entity to merge into (primary)</param>
    /// <param name="secondaryEntityIds">IDs of entities to merge (secondaries)</param>
    /// <param name="options">Merge configuration (conflict resolution, aliases)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result showing what was merged and undo capability</returns>
    Task<MergeResult> MergeAsync(
        Guid primaryEntityId,
        IReadOnlyList<Guid> secondaryEntityIds,
        MergeOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Undoes a merge operation.
    /// Restores secondary entities and reverts document links.
    /// </summary>
    /// <param name="mergeOperationId">ID of the merge to undo</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result showing what was unmerged</returns>
    Task<UnmergeResult> UnmergeAsync(
        Guid mergeOperationId,
        CancellationToken ct = default);
}
```

### 4.2 MergePreview Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Preview of a merge operation without executing it.
/// Shows impact on relationships, claims, and document links.
/// </summary>
public record MergePreview
{
    /// <summary>
    /// ID of the primary entity (merge target).
    /// </summary>
    public required Guid PrimaryEntityId { get; init; }

    /// <summary>
    /// Name of the primary entity.
    /// This name will be kept in merge.
    /// </summary>
    public required string PrimaryEntityName { get; init; }

    /// <summary>
    /// IDs of entities being merged into primary.
    /// </summary>
    public required IReadOnlyList<Guid> SecondaryEntityIds { get; init; }

    /// <summary>
    /// Number of relationships that will be transferred.
    /// Includes all relationships from secondary entities.
    /// </summary>
    public required int RelationshipsToTransfer { get; init; }

    /// <summary>
    /// Number of claims that will be transferred.
    /// Includes all claims from secondary entities.
    /// </summary>
    public required int ClaimsToTransfer { get; init; }

    /// <summary>
    /// Number of document links that will be updated.
    /// Document references to secondary entities → primary entity.
    /// </summary>
    public required int DocumentLinksToUpdate { get; init; }

    /// <summary>
    /// Conflicts detected during merge.
    /// Properties with different values in primary vs secondary.
    /// </summary>
    public required IReadOnlyList<MergeConflict> Conflicts { get; init; }

    /// <summary>
    /// Property merge strategy for each conflict.
    /// Shows how conflicts will be resolved.
    /// </summary>
    public required IReadOnlyList<PropertyMerge> PropertyMerges { get; init; }

    /// <summary>
    /// Estimated size of merge operation (bytes).
    /// For transaction planning.
    /// </summary>
    public long EstimatedOperationSize { get; init; }
}
```

### 4.3 MergeConflict Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// A property conflict detected during merge preview.
/// Occurs when primary and secondary entities have different values for same property.
/// </summary>
public record MergeConflict
{
    /// <summary>
    /// Name of the property with conflicting values.
    /// Example: "description", "category", "version"
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Value in the primary entity.
    /// </summary>
    public object? PrimaryValue { get; init; }

    /// <summary>
    /// Value in the secondary entity.
    /// </summary>
    public object? SecondaryValue { get; init; }

    /// <summary>
    /// Suggested resolution strategy.
    /// Based on default options and property type.
    /// </summary>
    public required MergeConflictResolution SuggestedResolution { get; init; }

    /// <summary>
    /// Reason for the suggestion.
    /// Example: "Primary entity is more recent"
    /// </summary>
    public string? SuggestionReason { get; init; }

    /// <summary>
    /// Whether this is critical (merge may fail if not resolved).
    /// </summary>
    public bool IsCritical { get; init; } = false;
}
```

### 4.4 MergeConflictResolution Enum

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Strategy for resolving a property conflict during merge.
/// </summary>
public enum MergeConflictResolution
{
    /// <summary>
    /// Keep the value from primary entity (discard secondary).
    /// Default for single-value properties.
    /// </summary>
    KeepPrimary = 1,

    /// <summary>
    /// Keep the value from secondary entity (replace primary).
    /// Useful if secondary is more up-to-date.
    /// </summary>
    KeepSecondary = 2,

    /// <summary>
    /// Combine values (for multi-value properties like tags, aliases).
    /// Union of primary and secondary values.
    /// </summary>
    KeepBoth = 3,

    /// <summary>
    /// Manual resolution required.
    /// User must explicitly choose resolution.
    /// </summary>
    Manual = 4
}
```

### 4.5 PropertyMerge Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Details of how a property will be merged.
/// Describes the merge strategy for each property.
/// </summary>
public record PropertyMerge
{
    /// <summary>
    /// Name of the property being merged.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Resolution strategy for this property.
    /// </summary>
    public required MergeConflictResolution Resolution { get; init; }

    /// <summary>
    /// Is this property affected (has conflict or transfer)?
    /// </summary>
    public bool IsAffected { get; init; }

    /// <summary>
    /// If resolution is Manual, what are the options?
    /// List of possible values to choose from.
    /// </summary>
    public IReadOnlyList<object>? ManualOptions { get; init; }
}
```

### 4.6 MergeResult Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Result of a merge operation.
/// Shows what was merged and undo capability.
/// </summary>
public record MergeResult
{
    /// <summary>
    /// Unique ID for this merge operation.
    /// Used for undo and audit tracking.
    /// </summary>
    public required Guid MergeOperationId { get; init; }

    /// <summary>
    /// ID of the primary entity (merge target).
    /// Secondary entities now merge into this one.
    /// </summary>
    public required Guid PrimaryEntityId { get; init; }

    /// <summary>
    /// IDs of entities that were merged (secondaries).
    /// These entities are now soft-deleted with aliases created.
    /// </summary>
    public required IReadOnlyList<Guid> MergedEntityIds { get; init; }

    /// <summary>
    /// Number of relationships transferred.
    /// Relationships from secondary entities now point to primary.
    /// </summary>
    public required int RelationshipsTransferred { get; init; }

    /// <summary>
    /// Number of claims transferred.
    /// Claims from secondary entities now belong to primary.
    /// </summary>
    public required int ClaimsTransferred { get; init; }

    /// <summary>
    /// Number of document links updated.
    /// References to secondary entities → primary entity.
    /// </summary>
    public required int DocumentLinksUpdated { get; init; }

    /// <summary>
    /// Whether merge can be undone.
    /// False if version service doesn't support undo.
    /// </summary>
    public required bool CanUndo { get; init; }

    /// <summary>
    /// Timestamp when merge was completed.
    /// </summary>
    public DateTimeOffset MergedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User ID who performed the merge.
    /// </summary>
    public Guid? MergedBy { get; init; }
}
```

### 4.7 MergeOptions Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Configuration options for merge operation.
/// Controls conflict resolution and post-merge behavior.
/// </summary>
public record MergeOptions
{
    /// <summary>
    /// Default strategy for resolving conflicts (default: KeepPrimary).
    /// Applied to all conflicts unless overridden.
    /// </summary>
    public MergeConflictResolution DefaultConflictResolution { get; init; } = MergeConflictResolution.KeepPrimary;

    /// <summary>
    /// Create aliases for merged entity names (default: true).
    /// Secondary entity names become aliases for primary.
    /// </summary>
    public bool CreateAliases { get; init; } = true;

    /// <summary>
    /// Update document links to merged entity (default: true).
    /// References to secondary entities → primary entity.
    /// </summary>
    public bool UpdateDocumentLinks { get; init; } = true;

    /// <summary>
    /// Preserve merge history for undo (default: true).
    /// Must be true to support UnmergeAsync.
    /// </summary>
    public bool PreserveHistory { get; init; } = true;

    /// <summary>
    /// Soft delete secondary entities (default: true).
    /// If false, secondary entities are hard deleted.
    /// </summary>
    public bool SoftDeleteSecondaries { get; init; } = true;

    /// <summary>
    /// Skip validation checks (default: false).
    /// If true, merge proceeds even if entity types don't match.
    /// </summary>
    public bool SkipValidation { get; init; } = false;

    /// <summary>
    /// List of property names to explicitly exclude from merge.
    /// These properties are not transferred.
    /// </summary>
    public IReadOnlyList<string>? ExcludeProperties { get; init; }
}
```

### 4.8 UnmergeResult Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Result of an unmerge operation.
/// Shows what was restored.
/// </summary>
public record UnmergeResult
{
    /// <summary>
    /// ID of the merge operation that was undone.
    /// </summary>
    public required Guid UndoneMergeOperationId { get; init; }

    /// <summary>
    /// ID of the primary entity.
    /// This entity's relationships are reverted to pre-merge state.
    /// </summary>
    public required Guid PrimaryEntityId { get; init; }

    /// <summary>
    /// IDs of entities that were restored.
    /// These were unmerged back to separate entities.
    /// </summary>
    public required IReadOnlyList<Guid> RestoredEntityIds { get; init; }

    /// <summary>
    /// Number of relationships reverted.
    /// Transferred relationships are undone.
    /// </summary>
    public required int RelationshipsReverted { get; init; }

    /// <summary>
    /// Number of document links reverted.
    /// Updated links are undone.
    /// </summary>
    public required int DocumentLinksReverted { get; init; }

    /// <summary>
    /// Timestamp when unmerge was completed.
    /// </summary>
    public DateTimeOffset UnmergedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User ID who performed the unmerge.
    /// </summary>
    public Guid? UnmergedBy { get; init; }
}
```

---

## 5. Implementation Details

### 5.1 PreviewMergeAsync Flow

```
Input: PrimaryEntityId + SecondaryEntityIds + MergeOptions
  ↓
1. Load primary entity and all secondary entities
  ↓
2. Validate merge is possible:
   - Primary and secondaries exist
   - No circular references
   - Types compatible (if SkipValidation=false)
  ↓
3. Identify conflicts:
   - Compare all properties across entities
   - Flag differences in single-value properties
   - Mark multi-value properties for merging
  ↓
4. Count impact:
   - Query all relationships for each secondary
   - Count claims for each secondary
   - Scan documents for references to secondaries
  ↓
5. Suggest resolutions:
   - Apply DefaultConflictResolution strategy
   - Consider entity recency (LastModified)
   - Flag critical conflicts
  ↓
6. Create PropertyMerges for all affected properties
  ↓
Output: MergePreview with conflicts and strategies
```

### 5.2 MergeAsync Flow

```
Input: PrimaryEntityId + SecondaryEntityIds + MergeOptions
  ↓
1. Call PreviewMergeAsync (same as preview)
  ↓
2. Validate user approved conflicts (if any Manual resolutions)
  ↓
3. Create MergeOperation record for undo support
  ↓
4. For each secondary entity:
   ↓
   a. Transfer all relationships:
      - Update relationship source_id to primary
      - Update any self-referencing relationships
   ↓
   b. Transfer all claims:
      - Update claim entity_id to primary
      - Merge conflicting claim values
   ↓
   c. Merge properties per conflict resolution:
      - KeepPrimary: discard secondary value
      - KeepSecondary: replace primary value
      - KeepBoth: combine values
  ↓
5. If CreateAliases=true:
   - Create alias entries for secondary entity names
   - Point aliases to primary entity
  ↓
6. If UpdateDocumentLinks=true:
   - Find all document references to secondary entities
   - Update references to point to primary entity
  ↓
7. If SoftDeleteSecondaries=true:
   - Mark secondary entities as deleted
   - Keep data for undo
  ↓
8. Store merge operation in version service (if PreserveHistory=true)
  ↓
9. Publish EntityMerged event
  ↓
Output: MergeResult with operation ID and undo capability
```

### 5.3 UnmergeAsync Flow

```
Input: MergeOperationId
  ↓
1. Load merge operation from version service
  ↓
2. Validate merge can be undone:
   - Operation exists
   - Undo history still available
   - No new relationships added to primary after merge
  ↓
3. For each secondary entity:
   ↓
   a. Restore soft-deleted entity
  ↓
   b. Revert relationship transfers:
      - Restore source_id to original secondary entity
  ↓
   c. Revert claim transfers:
      - Restore entity_id to original secondary entity
  ↓
   d. Restore deleted aliases
  ↓
4. If document links were updated:
   - Revert references from primary back to secondaries
  ↓
5. Delete merge operation from version history
  ↓
6. Publish EntityUnmerged event
  ↓
Output: UnmergeResult showing restored entities
```

---

## 6. Conflict Resolution Strategies

### 6.1 Property Type Detection

```csharp
public MergeConflictResolution SuggestResolution(string propertyName, object? primaryValue, object? secondaryValue, MergeOptions options)
{
    // Manual resolution already specified
    if (options.DefaultConflictResolution == MergeConflictResolution.Manual)
        return MergeConflictResolution.Manual;

    // Single value properties → KeepPrimary
    if (IsSingleValueProperty(propertyName))
        return MergeConflictResolution.KeepPrimary;

    // Multi-value properties → KeepBoth
    if (IsMultiValueProperty(propertyName))
        return MergeConflictResolution.KeepBoth;

    // Use default
    return options.DefaultConflictResolution;
}
```

### 6.2 Critical Conflicts

Properties that require user review:
- Entity type (if different)
- Unique identifiers
- Status/state properties
- Access control properties

---

## 7. Error Handling

### 7.1 Invalid Entity References

**Scenario:** PrimaryEntityId or SecondaryEntityIds don't exist.

**Handling:**
- Validate in PreviewMergeAsync
- Throw EntityNotFoundException with clear message
- Log which entities are missing

**Code:**
```csharp
var primary = await _entityBrowser.GetAsync(primaryEntityId, ct);
if (primary == null)
    throw new EntityNotFoundException($"Primary entity {primaryEntityId} not found");
```

### 7.2 Circular Merge

**Scenario:** Attempting to merge entity into itself.

**Handling:**
- Validate that primary is not in secondaries list
- Throw InvalidOperationException

**Code:**
```csharp
if (secondaryEntityIds.Contains(primaryEntityId))
    throw new InvalidOperationException("Cannot merge entity into itself");
```

### 7.3 Incompatible Entity Types

**Scenario:** Merging entities of different types (e.g., Endpoint into Service).

**Handling:**
- Detect in PreviewMergeAsync
- Flag as critical conflict if SkipValidation=false
- Throw MergeValidationException if merge would corrupt schema

**Code:**
```csharp
if (primary.Type != secondary.Type && !options.SkipValidation)
    throw new MergeValidationException($"Cannot merge {secondary.Type} into {primary.Type}");
```

### 7.4 Undo Not Available

**Scenario:** Attempting to undo merge but history expired.

**Handling:**
- Check if MergeOperation exists in version service
- Throw MergeHistoryExpiredException with grace period info

**Code:**
```csharp
var operation = await _versionService.GetMergeOperationAsync(mergeOperationId, ct);
if (operation == null)
    throw new MergeHistoryExpiredException($"Merge history expired after {graceperiod}");
```

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class EntityMergerTests
{
    private EntityMerger _merger;
    private Mock<IGraphRepository> _graphRepositoryMock;
    private Mock<IEntityBrowser> _entityBrowserMock;
    private Mock<IGraphVersionService> _versionServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _entityBrowserMock = new Mock<IEntityBrowser>();
        _versionServiceMock = new Mock<IGraphVersionService>();

        _merger = new EntityMerger(
            _graphRepositoryMock.Object,
            _entityBrowserMock.Object,
            _versionServiceMock.Object);
    }

    [TestMethod]
    public async Task PreviewMergeAsync_WithValidEntities_ReturnsMergePreview()
    {
        var primaryId = Guid.NewGuid();
        var secondaryId = Guid.NewGuid();

        var primary = new Entity { EntityId = primaryId, Name = "User", Type = "Entity" };
        var secondary = new Entity { EntityId = secondaryId, Name = "UserEntity", Type = "Entity" };

        _entityBrowserMock.Setup(x => x.GetAsync(primaryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(primary);
        _entityBrowserMock.Setup(x => x.GetAsync(secondaryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondary);

        _graphRepositoryMock.Setup(x => x.GetRelationships(primaryId))
            .Returns(new List<Relationship>());
        _graphRepositoryMock.Setup(x => x.GetRelationships(secondaryId))
            .Returns(new List<Relationship>());

        var options = new MergeOptions();
        var preview = await _merger.PreviewMergeAsync(primaryId, new[] { secondaryId }, options);

        Assert.AreEqual(primaryId, preview.PrimaryEntityId);
        Assert.AreEqual(1, preview.SecondaryEntityIds.Count);
    }

    [TestMethod]
    public async Task MergeAsync_WithTwoEntities_TransfersRelationships()
    {
        var primaryId = Guid.NewGuid();
        var secondaryId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var primary = new Entity { EntityId = primaryId, Name = "User", Type = "Entity" };
        var secondary = new Entity { EntityId = secondaryId, Name = "UserEntity", Type = "Entity" };

        var relationship = new Relationship
        {
            RelationshipId = Guid.NewGuid(),
            SourceId = secondaryId,
            TargetId = targetId,
            RelationType = "HasField"
        };

        _entityBrowserMock.Setup(x => x.GetAsync(primaryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(primary);
        _entityBrowserMock.Setup(x => x.GetAsync(secondaryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondary);

        _graphRepositoryMock.Setup(x => x.GetRelationships(secondaryId))
            .Returns(new List<Relationship> { relationship });

        var options = new MergeOptions();
        var result = await _merger.MergeAsync(primaryId, new[] { secondaryId }, options);

        Assert.AreEqual(1, result.RelationshipsTransferred);
        _graphRepositoryMock.Verify(
            x => x.UpdateRelationship(It.Is<Relationship>(r =>
                r.SourceId == primaryId && r.TargetId == targetId)),
            Times.Once);
    }

    [TestMethod]
    [ExpectedException(typeof(EntityNotFoundException))]
    public async Task PreviewMergeAsync_WithMissingPrimary_ThrowsException()
    {
        var primaryId = Guid.NewGuid();
        var secondaryId = Guid.NewGuid();

        _entityBrowserMock.Setup(x => x.GetAsync(primaryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entity)null);

        var options = new MergeOptions();
        await _merger.PreviewMergeAsync(primaryId, new[] { secondaryId }, options);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task MergeAsync_WithCircularMerge_ThrowsException()
    {
        var entityId = Guid.NewGuid();

        var options = new MergeOptions();
        await _merger.MergeAsync(entityId, new[] { entityId }, options);
    }

    [TestMethod]
    public async Task UnmergeAsync_WithValidOperation_RestoresEntities()
    {
        var mergeOpId = Guid.NewGuid();
        var primaryId = Guid.NewGuid();
        var secondaryId = Guid.NewGuid();

        var mergeOp = new MergeOperation
        {
            MergeOperationId = mergeOpId,
            PrimaryEntityId = primaryId,
            SecondaryEntityIds = new[] { secondaryId },
            PreserveHistory = true
        };

        _versionServiceMock.Setup(x => x.GetMergeOperationAsync(mergeOpId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergeOp);

        var result = await _merger.UnmergeAsync(mergeOpId);

        Assert.AreEqual(mergeOpId, result.UndoneMergeOperationId);
        Assert.AreEqual(1, result.RestoredEntityIds.Count);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class EntityMergerIntegrationTests
{
    [TestMethod]
    public async Task E2E_Merge_WithConflictingProperties_ResolvesPerStrategy()
    {
        // Create two entities with conflicting description
        var primary = new Entity
        {
            EntityId = Guid.NewGuid(),
            Name = "User",
            Type = "Entity",
            Description = "Represents a registered user"
        };

        var secondary = new Entity
        {
            EntityId = Guid.NewGuid(),
            Name = "UserEntity",
            Type = "Entity",
            Description = "Database entity for users"
        };

        // Preview merge
        var previewOptions = new MergeOptions
        {
            DefaultConflictResolution = MergeConflictResolution.KeepPrimary
        };

        var preview = await _merger.PreviewMergeAsync(
            primary.EntityId,
            new[] { secondary.EntityId },
            previewOptions);

        // Verify conflicts detected
        Assert.IsTrue(preview.Conflicts.Any(c => c.PropertyName == "description"));

        // Execute merge
        var mergeOptions = new MergeOptions
        {
            DefaultConflictResolution = MergeConflictResolution.KeepPrimary,
            CreateAliases = true
        };

        var result = await _merger.MergeAsync(
            primary.EntityId,
            new[] { secondary.EntityId },
            mergeOptions);

        Assert.AreEqual(1, result.MergedEntityIds.Count);
        Assert.IsTrue(result.CanUndo);
    }

    [TestMethod]
    public async Task E2E_Merge_ThenUnmerge_RestoresState()
    {
        // Create and merge entities
        var primaryId = Guid.NewGuid();
        var secondaryId = Guid.NewGuid();

        var mergeOptions = new MergeOptions();
        var mergeResult = await _merger.MergeAsync(primaryId, new[] { secondaryId }, mergeOptions);

        // Unmerge
        var unmergeResult = await _merger.UnmergeAsync(mergeResult.MergeOperationId);

        Assert.AreEqual(mergeResult.MergeOperationId, unmergeResult.UndoneMergeOperationId);
        Assert.AreEqual(1, unmergeResult.RestoredEntityIds.Count);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| PreviewMergeAsync | <500ms (P95) | Efficient graph traversal, caching |
| MergeAsync (single) | <2s (P95) | Batched updates, transaction |
| MergeAsync (bulk, 10 groups) | <20s (P95) | Parallel group processing |
| UnmergeAsync | <1s (P95) | Version service undo |

### 9.1 Optimization Strategy

- Use transactions for all merge operations
- Batch relationship updates
- Cache document references
- Parallel processing for independent secondaries
- Connection pooling for database access

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Data loss in merge | Critical | Preview before execute, full undo support, transaction safety |
| Broken relationships | High | Validate relationship integrity, test on secondary copies |
| Document link corruption | High | Link verification post-merge, audit trail |
| Unauthorized merge | Medium | Permission checks via authorization service |

---

## 11. License Gating

```csharp
public class EntityMerger : IEntityMerger
{
    private readonly ILicenseContext _licenseContext;

    public async Task<MergeResult> MergeAsync(
        Guid primaryEntityId,
        IReadOnlyList<Guid> secondaryEntityIds,
        MergeOptions options,
        CancellationToken ct = default)
    {
        if (!_licenseContext.IsAvailable(LicenseTier.Teams))
            throw new LicenseRequiredException("Entity merging requires Teams tier");

        // Implementation...
    }
}
```

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Two entities with different descriptions | PreviewMergeAsync | Conflict detected for description property |
| 2 | Entity with 5 relationships | MergeAsync | All 5 relationships transferred to primary |
| 3 | Secondary entity in 3 documents | MergeAsync with UpdateDocumentLinks=true | All 3 document references updated to primary |
| 4 | Merge executed | UnmergeAsync called | Secondary entity restored, relationships reverted |
| 5 | Entity type mismatch | PreviewMergeAsync with SkipValidation=false | Conflict marked as critical |
| 6 | KeepBoth strategy | MergeAsync with multi-value property | Values combined from both entities |
| 7 | Entity with 10 relationships + 5 claims | MergeAsync | All transferred successfully (<2s) |
| 8 | Teams license | Any merge operation | Operation succeeds |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - IEntityMerger, PreviewMergeAsync, MergeAsync, UnmergeAsync |
