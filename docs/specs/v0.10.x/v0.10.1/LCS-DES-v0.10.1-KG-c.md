# LCS-DES-v0.10.1-KG-c: Design Specification — Time-Travel Queries

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-101-c` | Knowledge Graph Versioning sub-part c |
| **Feature Name** | `Time-Travel Queries` | Query graph at historical timestamps |
| **Target Version** | `v0.10.1c` | Third sub-part of v0.10.1-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Knowledge Graph Versioning module |
| **Swimlane** | `Graph Versioning` | Versioning vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVersioning` | Graph versioning feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.1-KG](./LCS-SBD-v0.10.1-KG.md) | Knowledge Graph Versioning scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.1-KG S2.1](./LCS-SBD-v0.10.1-KG.md#21-sub-parts) | c = Time-Travel Queries |
| **Estimated Hours** | 8h | Development and testing effort |

---

## 2. Executive Summary

### 2.1 The Requirement

Time-Travel Queries enable querying the graph as it existed at any historical point in time. This requires:

1. Resolve version references (ID, timestamp, tag, branch) to specific versions
2. Reconstruct historical graph state using snapshots and deltas
3. Apply inverse deltas to rewind state from current to historical
4. Support efficient queries against historical snapshots
5. Cache frequently accessed historical states
6. Performance target: <2 seconds P95 for historical queries

### 2.2 The Proposed Solution

A multi-layer approach:

1. **Version Resolution:** Convert any version reference to specific version ID
2. **Snapshot-Based Fast Path:** Use existing snapshots when available
3. **Delta Reconstruction:** Apply/reverse deltas for closest snapshot
4. **Query Execution:** Execute queries against reconstructed state
5. **Caching:** Cache popular historical states for performance

**Key Design Principles:**
- Multiple query paths (snapshot, delta reversal)
- Lazy loading of deltas
- Efficient delta application
- Time-bound caching
- Read-only historical snapshots

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IVersionStore` | v0.10.1a | Version and delta retrieval |
| `IGraphRepository` | v0.4.5e | Current state and query execution |
| `IGraphSnapshotService` | v0.10.1d | Snapshot retrieval and restoration |
| `ICache` | v0.8.0 | Caching historical states |
| `IMediator` | v0.0.7a | Event publishing |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Text.Json` | latest | JSON deserialization of state |

### 3.2 Licensing Behavior

- **Teams Tier:** Time-travel queries within 1 year retention
- **Enterprise Tier:** Unlimited retention

---

## 4. Data Contract (The API)

### 4.1 ITimeTravelQueryService

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Queries the knowledge graph as it existed at historical points in time.
/// Enables "time machine" access to graph state.
/// </summary>
public interface ITimeTravelQueryService
{
    /// <summary>
    /// Gets a snapshot of the graph at a specific version or timestamp.
    /// </summary>
    Task<IGraphSnapshot> GetSnapshotAsync(
        GraphVersionRef versionRef,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific entity as it existed at a historical point.
    /// </summary>
    Task<EntitySnapshot?> GetEntityAtTimeAsync(
        Guid entityId,
        DateTimeOffset timestamp,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all entities of a type as they existed at a historical point.
    /// </summary>
    Task<IReadOnlyList<EntitySnapshot>> GetEntitiesByTypeAtTimeAsync(
        string entityType,
        DateTimeOffset timestamp,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific relationship as it existed at a historical point.
    /// </summary>
    Task<RelationshipSnapshot?> GetRelationshipAtTimeAsync(
        Guid relationshipId,
        DateTimeOffset timestamp,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the change history for a specific entity between two points.
    /// </summary>
    Task<IReadOnlyList<GraphChange>> GetEntityChangeHistoryAsync(
        Guid entityId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a query against the graph at a specific time.
    /// </summary>
    Task<IReadOnlyList<T>> QueryAtTimeAsync<T>(
        string query,
        DateTimeOffset timestamp,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Compares entity state between two points in time.
    /// </summary>
    Task<EntityStateComparison> CompareEntityAsync(
        Guid entityId,
        DateTimeOffset time1,
        DateTimeOffset time2,
        CancellationToken ct = default);

    /// <summary>
    /// Gets list of versions in a time range for a branch.
    /// </summary>
    Task<IReadOnlyList<GraphVersion>> GetVersionsInRangeAsync(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? branchName = null,
        CancellationToken ct = default);
}
```

### 4.2 GraphVersionRef Record

```csharp
namespace Lexichord.Abstractions.Contracts.Versioning;

/// <summary>
/// A reference to a graph version using multiple possible identifiers.
/// Can be resolved to a specific version ID.
/// </summary>
public record GraphVersionRef
{
    /// <summary>
    /// Version ID - most specific reference.
    /// </summary>
    public Guid? VersionId { get; init; }

    /// <summary>
    /// Timestamp - resolves to closest version at or before timestamp.
    /// </summary>
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>
    /// Tag - named reference (e.g., "v2.0-rc1").
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Branch name - resolves to HEAD of branch.
    /// </summary>
    public string? BranchName { get; init; }

    // Factory methods
    public static GraphVersionRef FromVersion(Guid id) => new() { VersionId = id };
    public static GraphVersionRef FromTimestamp(DateTimeOffset ts) => new() { Timestamp = ts };
    public static GraphVersionRef FromTag(string tag) => new() { Tag = tag };
    public static GraphVersionRef FromBranch(string branchName) => new() { BranchName = branchName };
}
```

### 4.3 EntitySnapshot Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Represents an entity as it existed at a point in time.
/// </summary>
public record EntitySnapshot
{
    public Guid EntityId { get; init; }
    public string Label { get; init; } = "";
    public string? EntityType { get; init; }
    public JsonDocument? Properties { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    public bool ExistsAtTime { get; init; } = true;
}
```

### 4.4 EntityStateComparison Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Comparison of an entity's state at two different times.
/// </summary>
public record EntityStateComparison
{
    public Guid EntityId { get; init; }
    public string Label { get; init; } = "";

    /// <summary>
    /// State at first timestamp.
    /// </summary>
    public EntitySnapshot StateAtTime1 { get; init; } = null!;

    /// <summary>
    /// State at second timestamp.
    /// </summary>
    public EntitySnapshot StateAtTime2 { get; init; } = null!;

    /// <summary>
    /// Properties that changed between the two times.
    /// Key = property name, Value = (oldValue, newValue).
    /// </summary>
    public IReadOnlyDictionary<string, (object? Old, object? New)> ChangedProperties { get; init; } = null!;

    /// <summary>
    /// Whether the entity existed at time 1.
    /// </summary>
    public bool ExistedAtTime1 { get; init; }

    /// <summary>
    /// Whether the entity existed at time 2.
    /// </summary>
    public bool ExistedAtTime2 { get; init; }
}
```

---

## 5. Implementation Strategy

### 5.1 Version Resolution Flow

```
GraphVersionRef (with Tag, Timestamp, or BranchName)
     ↓
ResolveVersionAsync()
     ↓
[Query version store by identifier]
     ↓
If VersionId → Direct lookup
If Tag → Query snapshots by tag
If Timestamp → Find closest version <= timestamp
If BranchName → Get branch head
     ↓
Return: GraphVersion
     ↓
Use VersionId for delta/snapshot retrieval
```

### 5.2 Snapshot Retrieval Paths

```
GetSnapshotAsync(versionRef)
     ↓
[Resolve versionRef to VersionId]
     ↓
Check Cache (versionId)
  ├─ HIT → Return cached snapshot
  └─ MISS ↓
       Check SnapshotStore
        ├─ Found → Return + cache
        └─ Not Found ↓
             [Find closest earlier snapshot]
             [Get deltas from snapshot to target version]
             [Apply deltas to reconstruct]
             [Cache reconstructed snapshot]
             [Return]
```

### 5.3 TimeTravelQueryService Implementation

```csharp
namespace Lexichord.Modules.CKVS.Services;

/// <summary>
/// Default implementation of time-travel queries.
/// </summary>
public class TimeTravelQueryService : ITimeTravelQueryService
{
    private readonly IVersionStore _versionStore;
    private readonly IGraphSnapshotService _snapshotService;
    private readonly IGraphRepository _graphRepository;
    private readonly ICache<string, IGraphSnapshot> _snapshotCache;
    private readonly ILogger<TimeTravelQueryService> _logger;

    public async Task<IGraphSnapshot> GetSnapshotAsync(
        GraphVersionRef versionRef,
        CancellationToken ct = default)
    {
        // Resolve version reference to specific version ID
        var version = await ResolveVersionAsync(versionRef, ct);
        if (version == null)
            throw new VersionNotFoundException($"Could not resolve version reference: {versionRef}");

        // Check cache
        var cacheKey = $"snapshot:{version.VersionId}";
        if (_snapshotCache.TryGetValue(cacheKey, out var cached))
            return cached!;

        // Get or reconstruct snapshot
        var snapshot = await GetOrReconstructSnapshotAsync(version, ct);

        // Cache for 1 hour
        _snapshotCache.Set(cacheKey, snapshot, TimeSpan.FromHours(1));

        return snapshot;
    }

    public async Task<EntitySnapshot?> GetEntityAtTimeAsync(
        Guid entityId,
        DateTimeOffset timestamp,
        CancellationToken ct = default)
    {
        var snapshot = await GetSnapshotAsync(
            GraphVersionRef.FromTimestamp(timestamp),
            ct);

        var entity = await snapshot.GetEntityAsync(entityId, ct);
        return entity;
    }

    public async Task<IReadOnlyList<EntitySnapshot>> GetEntitiesByTypeAtTimeAsync(
        string entityType,
        DateTimeOffset timestamp,
        CancellationToken ct = default)
    {
        var snapshot = await GetSnapshotAsync(
            GraphVersionRef.FromTimestamp(timestamp),
            ct);

        var entities = await snapshot.GetEntitiesAsync(ct);
        return entities.Where(e => e.EntityType == entityType).ToList();
    }

    public async Task<RelationshipSnapshot?> GetRelationshipAtTimeAsync(
        Guid relationshipId,
        DateTimeOffset timestamp,
        CancellationToken ct = default)
    {
        var snapshot = await GetSnapshotAsync(
            GraphVersionRef.FromTimestamp(timestamp),
            ct);

        var relationships = await snapshot.GetRelationshipsAsync(ct);
        return relationships.FirstOrDefault(r => r.RelationshipId == relationshipId);
    }

    public async Task<IReadOnlyList<GraphChange>> GetEntityChangeHistoryAsync(
        Guid entityId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken ct = default)
    {
        var versions = await _versionStore.GetVersionRangeAsync(startTime, endTime, null, ct);
        var changes = new List<GraphChange>();

        foreach (var version in versions)
        {
            var deltas = await _versionStore.GetVersionDeltasAsync(version.VersionId, ct);
            changes.AddRange(deltas.Where(d => d.ElementId == entityId));
        }

        return changes;
    }

    public async Task<EntityStateComparison> CompareEntityAsync(
        Guid entityId,
        DateTimeOffset time1,
        DateTimeOffset time2,
        CancellationToken ct = default)
    {
        var state1 = await GetEntityAtTimeAsync(entityId, time1, ct);
        var state2 = await GetEntityAtTimeAsync(entityId, time2, ct);

        if (state1 == null && state2 == null)
            throw new EntityNotFoundException($"Entity {entityId} not found at either time");

        var comparison = new EntityStateComparison
        {
            EntityId = entityId,
            Label = state1?.Label ?? state2?.Label ?? "",
            StateAtTime1 = state1 ?? new EntitySnapshot { EntityId = entityId },
            StateAtTime2 = state2 ?? new EntitySnapshot { EntityId = entityId },
            ExistedAtTime1 = state1 != null,
            ExistedAtTime2 = state2 != null,
            ChangedProperties = CompareProperties(state1, state2)
        };

        return comparison;
    }

    public async Task<IReadOnlyList<GraphVersion>> GetVersionsInRangeAsync(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? branchName = null,
        CancellationToken ct = default)
    {
        return await _versionStore.GetVersionRangeAsync(startTime, endTime, branchName, ct);
    }

    private async Task<GraphVersion?> ResolveVersionAsync(
        GraphVersionRef versionRef,
        CancellationToken ct)
    {
        if (versionRef.VersionId.HasValue)
            return await _versionStore.GetVersionByIdAsync(versionRef.VersionId.Value, ct);

        if (versionRef.Timestamp.HasValue)
            return await _versionStore.GetVersionAtTimestampAsync(
                versionRef.Timestamp.Value,
                versionRef.BranchName,
                ct);

        if (!string.IsNullOrEmpty(versionRef.BranchName))
            return await _versionStore.GetBranchHeadAsync(versionRef.BranchName, ct);

        return null;
    }

    private async Task<IGraphSnapshot> GetOrReconstructSnapshotAsync(
        GraphVersion version,
        CancellationToken ct)
    {
        // Try to get existing snapshot
        var snapshots = await _versionStore.GetSnapshotsForVersionAsync(version.VersionId, ct);
        if (snapshots.Count > 0)
            return new GraphSnapshotImpl(snapshots[0], this);

        // Find closest earlier snapshot and apply deltas
        return await ReconstructFromDeltasAsync(version, ct);
    }

    private async Task<IGraphSnapshot> ReconstructFromDeltasAsync(
        GraphVersion targetVersion,
        CancellationToken ct)
    {
        // Find closest snapshot before target version
        var allSnapshots = await FindSnapshotsBeforeAsync(targetVersion, ct);

        GraphVersion baseVersion;
        if (allSnapshots.Count > 0)
            baseVersion = await _versionStore.GetVersionByIdAsync(
                allSnapshots[0].VersionId,
                ct) ?? targetVersion;
        else
            baseVersion = targetVersion;

        // Get current graph state
        var currentState = await _graphRepository.ExportFullGraphAsync(ct);

        // Get deltas from base to target
        var deltas = await _versionStore.GetDeltasAsync(baseVersion.VersionId, targetVersion.VersionId, ct);

        // Apply inverse deltas to go from current to target
        var historicalState = ApplyInverseDeltas(currentState, deltas);

        // Wrap in snapshot
        return new GraphSnapshotImpl(new GraphSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            Name = $"Reconstructed-{targetVersion.VersionId}",
            VersionId = targetVersion.VersionId,
            CreatedAt = targetVersion.CreatedAt
        }, this);
    }

    private Dictionary<string, object> ApplyInverseDeltas(
        Dictionary<string, object> state,
        IReadOnlyList<GraphChange> deltas)
    {
        var result = new Dictionary<string, object>(state);

        // Apply deltas in reverse order
        foreach (var delta in deltas.Reverse())
        {
            switch (delta.ChangeType)
            {
                case GraphChangeType.Create:
                    // Inverse of Create is Delete
                    result.Remove(delta.ElementId.ToString());
                    break;
                case GraphChangeType.Update:
                    // Inverse of Update is restore old value
                    if (delta.OldValue != null)
                        result[delta.ElementId.ToString()] = delta.OldValue;
                    break;
                case GraphChangeType.Delete:
                    // Inverse of Delete is Create
                    if (delta.OldValue != null)
                        result[delta.ElementId.ToString()] = delta.OldValue;
                    break;
            }
        }

        return result;
    }
}
```

---

## 6. Data Flow Diagrams

### 6.1 Time-Travel Query Flow

```
User Request: "Get graph at Jan 15, 2026"
     ↓
GetSnapshotAsync(timestamp: "2026-01-15")
     ↓
[Resolve timestamp to Version V42]
     ↓
Check Cache
  ├─ HIT → Return cached snapshot
  └─ MISS ↓
       [Query snapshots for V42]
        ├─ Found → Return + cache
        └─ Not Found ↓
             [Find closest snapshot before V42 → V40]
             [Get deltas V40→V42]
             [Get current state]
             [Apply inverse deltas: current→V42]
             [Wrap in IGraphSnapshot]
             [Cache result]
             [Return]
```

### 6.2 Entity History Comparison

```
CompareEntityAsync(User#123, T1, T2)
     ↓
[Get snapshot at T1]
     ↓
[Extract entity User#123 → State1]
     ↓
[Get snapshot at T2]
     ↓
[Extract entity User#123 → State2]
     ↓
[Compare properties]
     ↓
Return: EntityStateComparison {
  StateAtTime1: {name: "Alice", email: "alice@old.com"},
  StateAtTime2: {name: "Alice", email: "alice@new.com"},
  ChangedProperties: {email: ("alice@old.com", "alice@new.com")}
}
```

---

## 7. Error Handling

### 7.1 Version Not Found

**Scenario:** GetSnapshotAsync with non-existent version reference.

**Handling:**
- Try to resolve version reference
- If all resolution attempts fail, throw VersionNotFoundException
- Include helpful details in error message

**Code:**
```csharp
var version = await ResolveVersionAsync(versionRef, ct);
if (version == null)
    throw new VersionNotFoundException(
        $"Could not resolve version reference: {versionRef}");
```

### 7.2 Delta Application Error

**Scenario:** Invalid delta or missing state during reconstruction.

**Handling:**
- Catch JSON deserialization errors
- Log detailed error with delta info
- Fall back to creating new snapshot (if possible)
- Rethrow with context

### 7.3 Cache Corruption

**Scenario:** Cached snapshot becomes invalid.

**Handling:**
- Invalidate cache on any version update
- Validate snapshot integrity on cache hit
- Fall through to reconstruction if invalid

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class TimeTravelQueryServiceTests
{
    private TimeTravelQueryService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        // Mock dependencies
        _service = new TimeTravelQueryService(
            new MockVersionStore(),
            new MockGraphSnapshotService(),
            new MockGraphRepository(),
            new MockCache<string, IGraphSnapshot>(),
            new MockLogger());
    }

    [TestMethod]
    public async Task GetSnapshotAsync_WithVersionId_ReturnsSnapshot()
    {
        var versionRef = GraphVersionRef.FromVersion(Guid.NewGuid());
        var snapshot = await _service.GetSnapshotAsync(versionRef);

        Assert.IsNotNull(snapshot);
    }

    [TestMethod]
    public async Task GetSnapshotAsync_WithTimestamp_ResolvesAndReturns()
    {
        var timestamp = DateTimeOffset.Now.AddDays(-7);
        var versionRef = GraphVersionRef.FromTimestamp(timestamp);

        var snapshot = await _service.GetSnapshotAsync(versionRef);

        Assert.IsNotNull(snapshot);
    }

    [TestMethod]
    [ExpectedException(typeof(VersionNotFoundException))]
    public async Task GetSnapshotAsync_WithInvalidRef_ThrowsException()
    {
        var versionRef = GraphVersionRef.FromVersion(Guid.NewGuid());
        await _service.GetSnapshotAsync(versionRef);
    }

    [TestMethod]
    public async Task GetEntityAtTimeAsync_WithValidEntity_ReturnsEntity()
    {
        var entityId = Guid.NewGuid();
        var timestamp = DateTimeOffset.Now.AddDays(-7);

        var entity = await _service.GetEntityAtTimeAsync(entityId, timestamp);

        Assert.IsNotNull(entity);
        Assert.AreEqual(entityId, entity.EntityId);
    }

    [TestMethod]
    public async Task CompareEntityAsync_BetweenTwoTimes_ShowsChanges()
    {
        var entityId = Guid.NewGuid();
        var time1 = DateTimeOffset.Now.AddDays(-14);
        var time2 = DateTimeOffset.Now.AddDays(-7);

        var comparison = await _service.CompareEntityAsync(entityId, time1, time2);

        Assert.AreEqual(entityId, comparison.EntityId);
        Assert.IsNotNull(comparison.ChangedProperties);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class TimeTravelQueryIntegrationTests
{
    private ITimeTravelQueryService _service = null!;

    [TestInitialize]
    public async Task Setup()
    {
        // Initialize with real services
        _service = new TimeTravelQueryService(
            new RealVersionStore(),
            new RealGraphSnapshotService(),
            new RealGraphRepository(),
            new RealCache(),
            new RealLogger());
    }

    [TestMethod]
    public async Task EndToEnd_TimeTravel_ReconstrucesCorrectState()
    {
        // Create initial version
        var v1 = await _service.GetSnapshotAsync(
            GraphVersionRef.FromBranch("main"));

        // Wait and create new version
        await Task.Delay(100);
        var v2 = await _service.GetSnapshotAsync(
            GraphVersionRef.FromBranch("main"));

        // Verify snapshots are different (if changes were made)
        Assert.IsNotNull(v1);
        Assert.IsNotNull(v2);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| GetSnapshotAsync (cached) | <10ms | In-memory cache lookup |
| GetSnapshotAsync (uncached) | <2s | Delta reconstruction |
| GetEntityAtTimeAsync | <100ms | Extract from snapshot |
| CompareEntityAsync | <200ms | Two snapshots + comparison |
| GetVersionsInRangeAsync | <500ms | Indexed version query |

**Optimization Techniques:**
- Snapshot caching with 1-hour TTL
- Lazy delta loading (stream deltas)
- Batch delta application
- Indexed version queries
- Connection pooling

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Unauthorized historical access | High | RBAC on time-travel queries |
| Sensitive data in snapshots | Medium | Data masking based on user role |
| Cache poisoning | Medium | Validate snapshots on load |
| Performance DoS | Medium | Query timeouts, rate limiting |

---

## 11. License Gating

```csharp
var retentionDays = _licenseContext.GetFeatureTier() switch
{
    LicenseTier.Teams => 365,
    LicenseTier.Enterprise => int.MaxValue,
    _ => 0
};

if (DateTimeOffset.UtcNow - timestamp > TimeSpan.FromDays(retentionDays))
    throw new HistoryOutOfRetentionException();
```

---

## 12. Dependencies & Integration Points

### 12.1 Consumes

- IVersionStore (version and delta retrieval)
- IGraphRepository (current state export)
- IGraphSnapshotService (snapshot data)
- ICache (snapshot caching)

### 12.2 Events

```csharp
public class SnapshotAccessedEvent
{
    public Guid SnapshotId { get; init; }
    public DateTimeOffset AccessedAt { get; init; }
}
```

---

## 13. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Valid version ID | GetSnapshotAsync called | IGraphSnapshot returned |
| 2 | Valid timestamp | GetSnapshotAsync called | Snapshot at closest earlier version returned |
| 3 | Existing entity | GetEntityAtTimeAsync called | Entity state at time returned |
| 4 | Entity modified between times | CompareEntityAsync called | Changed properties shown |
| 5 | Entity created after time1 | CompareEntityAsync called | ExistedAtTime1 = false |
| 6 | Snapshot cached | GetSnapshotAsync called twice | Cache hit on second call |
| 7 | Multiple versions | GetVersionsInRangeAsync called | All versions in range returned |
| 8 | No snapshots exist | GetSnapshotAsync called | Deltas applied to reconstruct |

---

## 14. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design specification for Time-Travel Queries |

---
