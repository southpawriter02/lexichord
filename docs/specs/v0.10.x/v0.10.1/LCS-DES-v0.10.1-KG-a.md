# LCS-DES-v0.10.1-KG-a: Design Specification — Version Store

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-101-a` | Knowledge Graph Versioning sub-part a |
| **Feature Name** | `Version Store` | Persist graph versions and deltas |
| **Target Version** | `v0.10.1a` | First sub-part of v0.10.1-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Knowledge Graph Versioning module |
| **Swimlane** | `Graph Versioning` | Versioning vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVersioning` | Graph versioning feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.1-KG](./LCS-SBD-v0.10.1-KG.md) | Knowledge Graph Versioning scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.1-KG S2.1](./LCS-SBD-v0.10.1-KG.md#21-sub-parts) | a = Version Store |
| **Estimated Hours** | 8h | Development and testing effort |

---

## 2. Executive Summary

### 2.1 The Requirement

The Version Store is the foundational persistence layer for graph versioning. It must:

1. Store graph version metadata (IDs, timestamps, branches, creators, messages)
2. Store deltas (compact change representations) for efficient storage
3. Create and manage full snapshots for performance and recovery
4. Support efficient retrieval of versions by multiple criteria (ID, timestamp, tag, branch)
5. Maintain parent-child relationships between versions (version history chain)
6. Track which versions belong to which branches

### 2.2 The Proposed Solution

A dual-store architecture:

1. **Version Metadata Store (PostgreSQL):** Relational storage for version records, metadata, and relationships
2. **Delta/Snapshot Store (Neo4j):** Graph-native storage for deltas and snapshots, with efficient querying

**Key Design Principles:**
- Immutable version records (write-once)
- Delta compression for storage efficiency
- Snapshot checkpoints for performance
- Branch-aware version tracking
- ACID properties for consistency

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Graph CRUD operations and state |
| `ISyncService` | v0.7.6-KG | Triggers version creation on mutations |
| `IMediator` | v0.0.7a | Event publishing for version created/deleted |
| PostgreSQL | v0.4.6-KG | Metadata and version history storage |
| Neo4j | v0.4.5-KG | Graph storage (versions also stored here) |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `Dapper` | latest | PostgreSQL ORM for version metadata |
| `System.Text.Json` | latest | JSON serialization for delta storage |

### 3.2 Licensing Behavior

- **Teams Tier:** Full version store functionality (unlimited history within 1 year retention)
- **Enterprise Tier:** Full + unlimited retention (configurable)

### 3.3 Storage Strategy

```
Version Record (PostgreSQL):
  VersionId (PK)
  ParentVersionId (FK)
  BranchName
  CreatedAt
  CreatedBy
  Message
  Stats (JSON)

Delta Record (PostgreSQL/Neo4j):
  DeltaId (PK)
  VersionId (FK)
  ElementType
  ElementId
  ChangeType
  OldValue (JSON)
  NewValue (JSON)

Snapshot Record (Neo4j):
  SnapshotId (PK)
  VersionId
  EntityCount
  RelationshipCount
  CreatedAt
  Name
  Description
```

---

## 4. Data Contract (The API)

### 4.1 IVersionStore Service

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Manages persistent storage of graph versions, deltas, and snapshots.
/// This is the core persistence layer for all versioning operations.
/// </summary>
public interface IVersionStore
{
    /// <summary>
    /// Creates a new version record with metadata.
    /// </summary>
    Task<GraphVersion> CreateVersionAsync(
        GraphVersionCreateRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific version by ID.
    /// </summary>
    Task<GraphVersion?> GetVersionByIdAsync(
        Guid versionId,
        CancellationToken ct = default);

    /// <summary>
    /// Finds the closest version at or before a given timestamp.
    /// Used for time-travel queries.
    /// </summary>
    Task<GraphVersion?> GetVersionAtTimestampAsync(
        DateTimeOffset timestamp,
        string? branchName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all versions for a specific branch in order.
    /// </summary>
    Task<IReadOnlyList<GraphVersion>> GetBranchHistoryAsync(
        string branchName,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all versions between two points (inclusive).
    /// </summary>
    Task<IReadOnlyList<GraphVersion>> GetVersionRangeAsync(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? branchName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Stores a delta representing changes between versions.
    /// </summary>
    Task StoreDeltaAsync(
        Guid versionId,
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all deltas between two versions.
    /// </summary>
    Task<IReadOnlyList<GraphChange>> GetDeltasAsync(
        Guid fromVersionId,
        Guid toVersionId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves deltas for a specific version.
    /// </summary>
    Task<IReadOnlyList<GraphChange>> GetVersionDeltasAsync(
        Guid versionId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a snapshot of the current graph state.
    /// </summary>
    Task<GraphSnapshot> CreateSnapshotAsync(
        GraphSnapshotCreateRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a snapshot by ID.
    /// </summary>
    Task<GraphSnapshot?> GetSnapshotByIdAsync(
        Guid snapshotId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets snapshots for a specific version.
    /// </summary>
    Task<IReadOnlyList<GraphSnapshot>> GetSnapshotsForVersionAsync(
        Guid versionId,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes old versions and deltas according to retention policy.
    /// </summary>
    Task DeleteOldVersionsAsync(
        TimeSpan retentionPeriod,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the head version for a branch.
    /// </summary>
    Task<GraphVersion?> GetBranchHeadAsync(
        string branchName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all branches and their current heads.
    /// </summary>
    Task<IReadOnlyDictionary<string, GraphVersion>> GetAllBranchHeadsAsync(
        CancellationToken ct = default);
}
```

### 4.2 GraphVersionCreateRequest Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Request to create a new version.
/// </summary>
public record GraphVersionCreateRequest
{
    /// <summary>
    /// Parent version ID (if any). Null for initial version.
    /// </summary>
    public Guid? ParentVersionId { get; init; }

    /// <summary>
    /// Branch name this version belongs to.
    /// </summary>
    public required string BranchName { get; init; } = "main";

    /// <summary>
    /// Human-readable message describing the changes.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// User ID who created this version.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Statistics about changes in this version.
    /// </summary>
    public GraphChangeStats? Stats { get; init; }
}
```

### 4.3 GraphSnapshotCreateRequest Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Request to create a named snapshot.
/// </summary>
public record GraphSnapshotCreateRequest
{
    /// <summary>
    /// Human-readable name for this snapshot.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of the snapshot.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version ID this snapshot represents.
    /// </summary>
    public required Guid VersionId { get; init; }

    /// <summary>
    /// User who created the snapshot.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Optional tag for easy reference (e.g., "v2.0-rc1").
    /// </summary>
    public string? Tag { get; init; }
}
```

### 4.4 GraphSnapshot Record

```csharp
namespace Lexichord.Abstractions.Contracts.Versioning;

/// <summary>
/// A named snapshot of the entire graph at a point in time.
/// </summary>
public record GraphSnapshot
{
    /// <summary>
    /// Unique identifier for this snapshot.
    /// </summary>
    public Guid SnapshotId { get; init; }

    /// <summary>
    /// Human-readable name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version ID this snapshot represents.
    /// </summary>
    public Guid VersionId { get; init; }

    /// <summary>
    /// When the snapshot was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// User who created the snapshot.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Optional tag (e.g., "v2.0-rc1").
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Count of entities in snapshot.
    /// </summary>
    public int EntityCount { get; init; }

    /// <summary>
    /// Count of relationships in snapshot.
    /// </summary>
    public int RelationshipCount { get; init; }

    /// <summary>
    /// Size in bytes of stored snapshot.
    /// </summary>
    public long SizeBytes { get; init; }
}
```

### 4.5 IGraphSnapshot Interface

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Represents a queryable snapshot of the graph at a point in time.
/// </summary>
public interface IGraphSnapshot
{
    /// <summary>
    /// Gets snapshot metadata.
    /// </summary>
    GraphSnapshot Metadata { get; }

    /// <summary>
    /// Gets entity count in snapshot.
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    /// Gets relationship count in snapshot.
    /// </summary>
    int RelationshipCount { get; }

    /// <summary>
    /// Gets all entities in snapshot.
    /// </summary>
    Task<IReadOnlyList<EntitySnapshot>> GetEntitiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets specific entity by ID if it exists in snapshot.
    /// </summary>
    Task<EntitySnapshot?> GetEntityAsync(Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Gets all relationships in snapshot.
    /// </summary>
    Task<IReadOnlyList<RelationshipSnapshot>> GetRelationshipsAsync(CancellationToken ct = default);
}
```

### 4.6 EntitySnapshot & RelationshipSnapshot Records

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Represents an entity as it existed in a snapshot.
/// </summary>
public record EntitySnapshot
{
    public Guid EntityId { get; init; }
    public string Label { get; init; } = "";
    public JsonDocument? Properties { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
}

/// <summary>
/// Represents a relationship as it existed in a snapshot.
/// </summary>
public record RelationshipSnapshot
{
    public Guid RelationshipId { get; init; }
    public Guid SourceEntityId { get; init; }
    public Guid TargetEntityId { get; init; }
    public string Type { get; init; } = "";
    public JsonDocument? Properties { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

---

## 5. Implementation Strategy

### 5.1 PostgreSQL Schema (Version Metadata)

```sql
-- Version records
CREATE TABLE graph_versions (
    version_id UUID PRIMARY KEY,
    parent_version_id UUID REFERENCES graph_versions(version_id),
    branch_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP NOT NULL,
    created_by VARCHAR(255),
    message TEXT,
    stats_json JSONB NOT NULL,
    created_index INT GENERATED ALWAYS AS IDENTITY,

    CONSTRAINT parent_check CHECK (parent_version_id IS NULL OR parent_version_id != version_id)
);

CREATE INDEX idx_versions_branch ON graph_versions(branch_name, created_at);
CREATE INDEX idx_versions_timestamp ON graph_versions(created_at);
CREATE INDEX idx_versions_parent ON graph_versions(parent_version_id);

-- Delta records
CREATE TABLE graph_deltas (
    delta_id UUID PRIMARY KEY,
    version_id UUID NOT NULL REFERENCES graph_versions(version_id),
    element_type VARCHAR(50) NOT NULL,
    element_id UUID NOT NULL,
    change_type VARCHAR(20) NOT NULL,
    old_value JSONB,
    new_value JSONB,
    changed_at TIMESTAMP NOT NULL,
    changed_by VARCHAR(255),
    source_document VARCHAR(255),

    CONSTRAINT check_change_type CHECK (change_type IN ('Create', 'Update', 'Delete'))
);

CREATE INDEX idx_deltas_version ON graph_deltas(version_id);
CREATE INDEX idx_deltas_element ON graph_deltas(element_id);

-- Snapshots
CREATE TABLE graph_snapshots (
    snapshot_id UUID PRIMARY KEY,
    version_id UUID NOT NULL REFERENCES graph_versions(version_id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    tag VARCHAR(100),
    created_at TIMESTAMP NOT NULL,
    created_by VARCHAR(255),
    entity_count INT NOT NULL,
    relationship_count INT NOT NULL,
    size_bytes BIGINT NOT NULL
);

CREATE INDEX idx_snapshots_version ON graph_snapshots(version_id);
CREATE INDEX idx_snapshots_tag ON graph_snapshots(tag);
```

### 5.2 Implementation Architecture

```
IVersionStore (Interface)
  ↓
VersionStoreService (Implementation)
  ├─→ VersionMetadataRepository (PostgreSQL)
  ├─→ DeltaRepository (PostgreSQL)
  ├─→ SnapshotRepository (Neo4j/PostgreSQL)
  └─→ IMediator (Event Publishing)
```

### 5.3 Key Implementation Details

**Version Creation:**
- Validate parent version exists (if specified)
- Ensure branch name is valid
- Persist version metadata to PostgreSQL
- Publish `VersionCreatedEvent`
- Return version record

**Delta Storage:**
- Accept change list from Change Tracking service
- Serialize changes to JSON
- Persist to graph_deltas table
- Maintain referential integrity with version_id

**Snapshot Creation:**
- Export current graph state from Neo4j
- Serialize to JSON/binary format
- Store metadata + blob reference
- Return snapshot record

**Version Retrieval:**
- Query by ID (direct lookup)
- Query by timestamp (find closest version <= timestamp)
- Query by branch (filter by branch_name, order by created_at)
- Use indexes for performance

---

## 6. Data Flow Diagrams

### 6.1 Version Creation Flow

```
Application Code
     ↓
CreateVersionAsync(request)
     ↓
[Validate Request]
     ↓
[Insert into graph_versions table]
     ↓
[Store related deltas]
     ↓
[Publish VersionCreatedEvent]
     ↓
Return GraphVersion Record
```

### 6.2 Time-Travel Query Flow

```
User Request (timestamp T)
     ↓
GetVersionAtTimestampAsync(T)
     ↓
[Query: SELECT * FROM graph_versions WHERE created_at <= T AND branch_name = ? ORDER BY created_at DESC LIMIT 1]
     ↓
Return Version V
     ↓
[Delta reconstruction by Time-Travel Query Service]
```

### 6.3 Snapshot Retrieval Flow

```
User Request
     ↓
GetSnapshotsForVersionAsync(V)
     ↓
[Query: SELECT * FROM graph_snapshots WHERE version_id = ?]
     ↓
Return Snapshot List
     ↓
[Reconstruct from snapshot + deltas if needed]
```

---

## 7. Error Handling

### 7.1 Parent Version Not Found

**Scenario:** CreateVersionAsync with non-existent parent ID.

**Handling:**
- Check if ParentVersionId exists before insert
- Throw `VersionNotFoundException`
- Log error for audit

**Code:**
```csharp
if (request.ParentVersionId.HasValue)
{
    var parent = await _repository.GetVersionByIdAsync(request.ParentVersionId.Value, ct);
    if (parent == null)
        throw new VersionNotFoundException($"Parent version {request.ParentVersionId} not found");
}
```

### 7.2 Invalid Branch Name

**Scenario:** CreateVersionAsync with invalid branch name.

**Handling:**
- Validate branch name format (alphanumeric, hyphens, slashes)
- Throw `InvalidBranchNameException`
- Provide clear error message

### 7.3 Retention Policy Violation

**Scenario:** DeleteOldVersionsAsync attempts to delete non-expired versions.

**Handling:**
- Only delete versions older than retention period
- Calculate cutoff date correctly
- Preserve at least one version per branch
- Log deletion summary

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class VersionStoreTests
{
    private IVersionStore _store = null!;

    [TestInitialize]
    public void Setup()
    {
        // Mock or in-memory database
        _store = new MockVersionStore();
    }

    [TestMethod]
    public async Task CreateVersion_WithValidRequest_CreatesAndReturnsVersion()
    {
        var request = new GraphVersionCreateRequest
        {
            BranchName = "main",
            Message = "Initial version"
        };

        var version = await _store.CreateVersionAsync(request);

        Assert.IsNotNull(version);
        Assert.AreEqual("main", version.BranchName);
        Assert.AreEqual("Initial version", version.Message);
    }

    [TestMethod]
    public async Task GetVersionById_WithValidId_ReturnsVersion()
    {
        var request = new GraphVersionCreateRequest { BranchName = "main" };
        var created = await _store.CreateVersionAsync(request);

        var retrieved = await _store.GetVersionByIdAsync(created.VersionId);

        Assert.IsNotNull(retrieved);
        Assert.AreEqual(created.VersionId, retrieved.VersionId);
    }

    [TestMethod]
    public async Task GetVersionAtTimestamp_WithValidTime_ReturnsClosestVersion()
    {
        var v1 = await _store.CreateVersionAsync(new GraphVersionCreateRequest { BranchName = "main" });
        await Task.Delay(100);
        var midTime = DateTimeOffset.UtcNow;
        await Task.Delay(100);
        var v2 = await _store.CreateVersionAsync(new GraphVersionCreateRequest
        {
            BranchName = "main",
            ParentVersionId = v1.VersionId
        });

        var result = await _store.GetVersionAtTimestampAsync(midTime);

        Assert.IsNotNull(result);
        Assert.AreEqual(v1.VersionId, result.VersionId);
    }

    [TestMethod]
    [ExpectedException(typeof(VersionNotFoundException))]
    public async Task CreateVersion_WithNonExistentParent_ThrowsException()
    {
        var request = new GraphVersionCreateRequest
        {
            BranchName = "main",
            ParentVersionId = Guid.NewGuid()
        };

        await _store.CreateVersionAsync(request);
    }

    [TestMethod]
    public async Task StoreDelta_WithValidChanges_PersistsDelta()
    {
        var version = await _store.CreateVersionAsync(new GraphVersionCreateRequest { BranchName = "main" });
        var changes = new List<GraphChange>
        {
            new GraphChange
            {
                ChangeId = Guid.NewGuid(),
                VersionId = version.VersionId,
                ChangeType = GraphChangeType.Create,
                ElementType = GraphElementType.Entity,
                ElementId = Guid.NewGuid(),
                ChangedAt = DateTimeOffset.UtcNow
            }
        };

        await _store.StoreDeltaAsync(version.VersionId, changes);

        var retrieved = await _store.GetVersionDeltasAsync(version.VersionId);
        Assert.AreEqual(1, retrieved.Count);
    }

    [TestMethod]
    public async Task CreateSnapshot_WithValidRequest_CreatesSnapshot()
    {
        var version = await _store.CreateVersionAsync(new GraphVersionCreateRequest { BranchName = "main" });
        var snapshotRequest = new GraphSnapshotCreateRequest
        {
            Name = "v1.0 Release",
            VersionId = version.VersionId,
            CreatedBy = "admin@company.com"
        };

        var snapshot = await _store.CreateSnapshotAsync(snapshotRequest);

        Assert.IsNotNull(snapshot);
        Assert.AreEqual("v1.0 Release", snapshot.Name);
        Assert.AreEqual(version.VersionId, snapshot.VersionId);
    }

    [TestMethod]
    public async Task GetBranchHistory_WithValidBranch_ReturnsVersionsInOrder()
    {
        var v1 = await _store.CreateVersionAsync(new GraphVersionCreateRequest { BranchName = "feature" });
        await Task.Delay(50);
        var v2 = await _store.CreateVersionAsync(new GraphVersionCreateRequest
        {
            BranchName = "feature",
            ParentVersionId = v1.VersionId
        });

        var history = await _store.GetBranchHistoryAsync("feature");

        Assert.AreEqual(2, history.Count);
        Assert.AreEqual(v1.VersionId, history[0].VersionId);
        Assert.AreEqual(v2.VersionId, history[1].VersionId);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class VersionStoreIntegrationTests
{
    private IVersionStore _store = null!;
    private PostgreSqlContainer _postgres = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _postgres = new PostgreSqlContainer();
        await _postgres.StartAsync();

        var options = new DbContextOptions { ConnectionString = _postgres.ConnectionString };
        _store = new VersionStoreService(new EFVersionRepository(options));
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _postgres.StopAsync();
    }

    [TestMethod]
    public async Task EndToEnd_VersionCreationAndRetrieval()
    {
        // Create version chain
        var v1 = await _store.CreateVersionAsync(new GraphVersionCreateRequest
        {
            BranchName = "main",
            Message = "Initial"
        });

        var v2 = await _store.CreateVersionAsync(new GraphVersionCreateRequest
        {
            BranchName = "main",
            ParentVersionId = v1.VersionId,
            Message = "Update 1"
        });

        // Store deltas
        await _store.StoreDeltaAsync(v2.VersionId, new List<GraphChange>
        {
            new GraphChange
            {
                ChangeId = Guid.NewGuid(),
                VersionId = v2.VersionId,
                ChangeType = GraphChangeType.Update,
                ElementType = GraphElementType.Entity,
                ElementId = Guid.NewGuid(),
                ElementLabel = "User",
                ChangedAt = DateTimeOffset.UtcNow
            }
        });

        // Verify retrieval
        var retrieved = await _store.GetVersionByIdAsync(v2.VersionId);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(v1.VersionId, retrieved.ParentVersionId);

        var deltas = await _store.GetVersionDeltasAsync(v2.VersionId);
        Assert.AreEqual(1, deltas.Count);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| CreateVersion | <50ms | Batch inserts, index on branch |
| GetVersionById | <5ms | Primary key lookup |
| GetVersionAtTimestamp | <20ms | Index on created_at + branch |
| GetBranchHistory | <100ms | Indexed range query |
| StoreDelta | <50ms | Batch insert deltas |
| GetDeltas | <100ms | Indexed query by version_id |
| CreateSnapshot | <2s | Background async operation |

**Optimization Techniques:**
- Indexes on branch_name, created_at, parent_version_id
- Partition large version tables by date
- Archive old versions to cold storage
- Cache recent branch heads
- Connection pooling for PostgreSQL

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Data tampering | Critical | Immutable records, audit logging |
| Unauthorized access | High | Role-based access control via IGraphVersionService |
| Data loss | Critical | Snapshots, backup strategy |
| Retention breach | Medium | Automated cleanup, retention tracking |

---

## 11. License Gating

```csharp
if (!_licenseContext.IsFeatureAvailable(FeatureFlags.CKVS.GraphVersioning))
{
    throw new FeatureNotAvailableException("Graph versioning not available in this tier");
}
```

| Tier | Support |
| :--- | :--- |
| Core | ❌ Not available |
| WriterPro | ❌ Not available |
| Teams | ✅ 1 year retention |
| Enterprise | ✅ Unlimited retention |

---

## 12. Dependencies & Integration Points

### 12.1 Consumed By

- Change Tracking (v0.10.1b) — StoreDelta
- Time-Travel Queries (v0.10.1c) — GetVersionAtTimestamp, GetDeltas
- Snapshot Manager (v0.10.1d) — CreateSnapshot, GetSnapshotsForVersionAsync
- Branch/Merge (v0.10.1e) — GetBranchHistory, GetBranchHeadAsync

### 12.2 Event Publishing

```csharp
public class VersionCreatedEvent
{
    public Guid VersionId { get; init; }
    public string BranchName { get; init; } = "";
    public Guid? ParentVersionId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}
```

---

## 13. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Empty version store | CreateVersionAsync called | Version created with valid ID |
| 2 | Existing version V1 | CreateVersionAsync called with V1 as parent | V2 created with V1 as parent |
| 3 | Version with deltas | GetVersionDeltasAsync called | All deltas returned in order |
| 4 | Multiple versions on branch | GetBranchHistoryAsync called | Versions returned in creation order |
| 5 | Timestamp between versions | GetVersionAtTimestampAsync called | Closest earlier version returned |
| 6 | Non-existent parent ID | CreateVersionAsync called | VersionNotFoundException thrown |
| 7 | Graph snapshot data | CreateSnapshotAsync called | Snapshot created with metadata |
| 8 | Snapshot with tag | CreateSnapshotAsync called | Tag stored and queryable |

---

## 14. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design specification for Version Store |

---
