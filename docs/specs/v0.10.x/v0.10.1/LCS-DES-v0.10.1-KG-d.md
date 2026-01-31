# LCS-DES-v0.10.1-KG-d: Design Specification — Snapshot Manager

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-101-d` | Knowledge Graph Versioning sub-part d |
| **Feature Name** | `Snapshot Manager` | Create and restore full snapshots |
| **Target Version** | `v0.10.1d` | Fourth sub-part of v0.10.1-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Knowledge Graph Versioning module |
| **Swimlane** | `Graph Versioning` | Versioning vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVersioning` | Graph versioning feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.1-KG](./LCS-SBD-v0.10.1-KG.md) | Knowledge Graph Versioning scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.1-KG S2.1](./LCS-SBD-v0.10.1-KG.md#21-sub-parts) | d = Snapshot Manager |
| **Estimated Hours** | 6h | Development and testing effort |

---

## 2. Executive Summary

### 2.1 The Requirement

The Snapshot Manager creates and restores named snapshots of the entire knowledge graph state. It must:

1. Create complete snapshots of current graph state
2. Tag snapshots with names and descriptions for easy reference
3. Restore the graph to a previous snapshot state
4. Manage snapshot metadata and lifecycle
5. Support snapshot versioning and comparison
6. Handle snapshot storage and retrieval efficiently

### 2.2 The Proposed Solution

A comprehensive snapshot service:

1. **Snapshot Creation:** Export complete graph state and store as snapshot
2. **Metadata Management:** Store snapshot names, descriptions, tags, timestamps
3. **Snapshot Restoration:** Load snapshot and apply to current graph
4. **Lifecycle Management:** Track snapshot age, size, and retention
5. **Comparison:** Compare snapshots to show evolution

**Key Design Principles:**
- Complete state capture
- Restorable snapshots (idempotent)
- Named references for easy selection
- Efficient storage (compression, deduplication)
- Immutable snapshot records

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IVersionStore` | v0.10.1a | Store snapshot metadata |
| `IGraphRepository` | v0.4.5e | Export/import full graph |
| `IMediator` | v0.0.7a | Event publishing |
| `IBlobStorage` | v0.8.0 | Snapshot data storage |
| `ILogger` | v0.9.0 | Diagnostic logging |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.IO.Compression` | latest | Snapshot compression |
| `System.Text.Json` | latest | Snapshot serialization |

### 3.2 Licensing Behavior

- **Teams Tier:** Create and restore snapshots (1-year retention)
- **Enterprise Tier:** Unlimited snapshot retention and advanced features

---

## 4. Data Contract (The API)

### 4.1 IGraphSnapshotService

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Manages creation, storage, and restoration of graph snapshots.
/// Snapshots represent complete graph state at specific points in time.
/// </summary>
public interface IGraphSnapshotService
{
    /// <summary>
    /// Creates a new named snapshot of the current graph state.
    /// </summary>
    Task<GraphSnapshot> CreateSnapshotAsync(
        GraphSnapshotCreateRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a snapshot by ID.
    /// </summary>
    Task<GraphSnapshot?> GetSnapshotByIdAsync(
        Guid snapshotId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a snapshot by name.
    /// </summary>
    Task<GraphSnapshot?> GetSnapshotByNameAsync(
        string name,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a snapshot by tag.
    /// </summary>
    Task<GraphSnapshot?> GetSnapshotByTagAsync(
        string tag,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all snapshots for a specific version.
    /// </summary>
    Task<IReadOnlyList<GraphSnapshot>> GetSnapshotsForVersionAsync(
        Guid versionId,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all snapshots with optional filtering and pagination.
    /// </summary>
    Task<SnapshotListResult> ListSnapshotsAsync(
        SnapshotListQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Restores the graph to a previous snapshot state.
    /// Creates a new version recording the restoration.
    /// </summary>
    Task<GraphVersion> RestoreSnapshotAsync(
        Guid snapshotId,
        string? message = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a snapshot (soft delete).
    /// </summary>
    Task DeleteSnapshotAsync(
        Guid snapshotId,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads snapshot data as a file.
    /// </summary>
    Task<Stream> DownloadSnapshotAsync(
        Guid snapshotId,
        CancellationToken ct = default);

    /// <summary>
    /// Compares two snapshots to show differences.
    /// </summary>
    Task<SnapshotComparison> CompareSnapshotsAsync(
        Guid snapshotId1,
        Guid snapshotId2,
        CancellationToken ct = default);

    /// <summary>
    /// Cleans up old snapshots according to retention policy.
    /// </summary>
    Task DeleteOldSnapshotsAsync(
        TimeSpan retentionPeriod,
        CancellationToken ct = default);
}
```

### 4.2 GraphSnapshot Record

```csharp
namespace Lexichord.Abstractions.Contracts.Versioning;

/// <summary>
/// Metadata for a named snapshot of graph state.
/// </summary>
public record GraphSnapshot
{
    /// <summary>
    /// Unique identifier for this snapshot.
    /// </summary>
    public Guid SnapshotId { get; init; }

    /// <summary>
    /// Version ID this snapshot represents.
    /// </summary>
    public Guid VersionId { get; init; }

    /// <summary>
    /// Human-readable name for this snapshot.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Optional description of the snapshot's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional tag for easy reference (e.g., "v2.0-release", "pre-migration").
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// When the snapshot was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// User who created the snapshot.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Number of entities in the snapshot.
    /// </summary>
    public int EntityCount { get; init; }

    /// <summary>
    /// Number of relationships in the snapshot.
    /// </summary>
    public int RelationshipCount { get; init; }

    /// <summary>
    /// Total claims in the snapshot.
    /// </summary>
    public int ClaimCount { get; init; }

    /// <summary>
    /// Size of snapshot data in bytes (compressed).
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Checksum of snapshot data for integrity verification.
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Whether this snapshot has been deleted (soft delete).
    /// </summary>
    public bool IsDeleted { get; init; } = false;

    /// <summary>
    /// When the snapshot was deleted (if soft deleted).
    /// </summary>
    public DateTimeOffset? DeletedAt { get; init; }
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
    /// Human-readable name for the snapshot.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description explaining the snapshot's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version ID to snapshot (or current if null).
    /// </summary>
    public Guid? VersionId { get; init; }

    /// <summary>
    /// Optional tag for reference (e.g., "v2.0-release").
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// User creating the snapshot.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Whether to compress the snapshot data.
    /// </summary>
    public bool Compress { get; init; } = true;
}
```

### 4.4 SnapshotListQuery Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Query parameters for listing snapshots.
/// </summary>
public record SnapshotListQuery
{
    /// <summary>
    /// Filter by snapshot name (substring match).
    /// </summary>
    public string? NameFilter { get; init; }

    /// <summary>
    /// Filter by tag.
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Include deleted snapshots in results.
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// Sort order (Name, CreatedAt, Size).
    /// </summary>
    public SnapshotSortBy SortBy { get; init; } = SnapshotSortBy.CreatedAt;

    /// <summary>
    /// Descending order.
    /// </summary>
    public bool Descending { get; init; } = true;

    /// <summary>
    /// Number of results to skip.
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Number of results to return.
    /// </summary>
    public int Take { get; init; } = 50;
}

public enum SnapshotSortBy { Name, CreatedAt, Size }
```

### 4.5 SnapshotListResult Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Result of listing snapshots.
/// </summary>
public record SnapshotListResult
{
    /// <summary>
    /// Snapshots matching the query.
    /// </summary>
    public IReadOnlyList<GraphSnapshot> Snapshots { get; init; } = [];

    /// <summary>
    /// Total count of snapshots (ignoring pagination).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Whether more results are available.
    /// </summary>
    public bool HasMore { get; init; }
}
```

### 4.6 SnapshotComparison Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Comparison between two snapshots.
/// </summary>
public record SnapshotComparison
{
    /// <summary>
    /// First snapshot.
    /// </summary>
    public GraphSnapshot Snapshot1 { get; init; } = null!;

    /// <summary>
    /// Second snapshot.
    /// </summary>
    public GraphSnapshot Snapshot2 { get; init; } = null!;

    /// <summary>
    /// Entities created between snapshots.
    /// </summary>
    public int EntitiesAdded { get; init; }

    /// <summary>
    /// Entities deleted between snapshots.
    /// </summary>
    public int EntitiesRemoved { get; init; }

    /// <summary>
    /// Entities modified between snapshots.
    /// </summary>
    public int EntitiesModified { get; init; }

    /// <summary>
    /// Relationships created between snapshots.
    /// </summary>
    public int RelationshipsAdded { get; init; }

    /// <summary>
    /// Relationships deleted between snapshots.
    /// </summary>
    public int RelationshipsRemoved { get; init; }

    /// <summary>
    /// Time elapsed between snapshots.
    /// </summary>
    public TimeSpan TimeDifference { get; init; }
}
```

---

## 5. Implementation Strategy

### 5.1 Snapshot Creation Flow

```
CreateSnapshotAsync(request)
     ↓
[Validate request]
     ↓
[Export current graph state]
     ├─ All entities
     ├─ All relationships
     └─ All claims
     ↓
[Serialize to JSON/binary]
     ↓
[Compute checksum]
     ↓
[Compress if requested]
     ↓
[Store to blob storage]
     ↓
[Record metadata in version store]
     ↓
[Publish SnapshotCreatedEvent]
     ↓
Return GraphSnapshot
```

### 5.2 Snapshot Restoration Flow

```
RestoreSnapshotAsync(snapshotId)
     ↓
[Get snapshot metadata]
     ↓
[Validate snapshot exists and not deleted]
     ↓
[Load snapshot data from blob storage]
     ↓
[Verify checksum]
     ↓
[Begin change tracking scope]
     ↓
[Delete all current entities]
     ↓
[Delete all current relationships]
     ↓
[Restore entities from snapshot]
     ↓
[Restore relationships from snapshot]
     ↓
[Restore claims from snapshot]
     ↓
[Complete tracking scope]
     ↓
[Create version recording restoration]
     ↓
[Publish SnapshotRestoredEvent]
     ↓
Return GraphVersion
```

### 5.3 GraphSnapshotService Implementation

```csharp
namespace Lexichord.Modules.CKVS.Services;

/// <summary>
/// Default implementation of snapshot management.
/// </summary>
public class GraphSnapshotService : IGraphSnapshotService
{
    private readonly IVersionStore _versionStore;
    private readonly IGraphRepository _graphRepository;
    private readonly IBlobStorage _blobStorage;
    private readonly IChangeTracker _changeTracker;
    private readonly IMediator _mediator;
    private readonly ILogger<GraphSnapshotService> _logger;

    public async Task<GraphSnapshot> CreateSnapshotAsync(
        GraphSnapshotCreateRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Snapshot name is required");

        // Determine version ID (current or specified)
        var versionId = request.VersionId ?? Guid.NewGuid(); // Will use current version

        // Export complete graph state
        var graphState = await _graphRepository.ExportFullGraphAsync(ct);

        // Serialize and compress
        var serialized = JsonSerializer.SerializeToUtf8Bytes(graphState);
        var compressed = request.Compress
            ? CompressData(serialized)
            : serialized;

        // Compute checksum
        var checksum = ComputeChecksum(compressed);

        // Store blob
        var blobPath = $"snapshots/{Guid.NewGuid()}.snapshot.gz";
        using (var stream = new MemoryStream(compressed))
        {
            await _blobStorage.UploadAsync(blobPath, stream, ct);
        }

        // Create metadata record
        var snapshot = new GraphSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            VersionId = versionId,
            Name = request.Name,
            Description = request.Description,
            Tag = request.Tag,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = request.CreatedBy,
            EntityCount = (graphState["entities"] as List<object>)?.Count ?? 0,
            RelationshipCount = (graphState["relationships"] as List<object>)?.Count ?? 0,
            ClaimCount = (graphState["claims"] as List<object>)?.Count ?? 0,
            SizeBytes = compressed.Length,
            Checksum = checksum
        };

        // Store metadata
        var snapshotRequest = new GraphSnapshotCreateRequest
        {
            Name = request.Name,
            Description = request.Description,
            VersionId = versionId,
            Tag = request.Tag,
            CreatedBy = request.CreatedBy
        };

        await _versionStore.CreateSnapshotAsync(snapshotRequest, ct);

        // Publish event
        await _mediator.Publish(new SnapshotCreatedEvent
        {
            SnapshotId = snapshot.SnapshotId,
            VersionId = versionId,
            CreatedAt = snapshot.CreatedAt
        }, ct);

        return snapshot;
    }

    public async Task<GraphVersion> RestoreSnapshotAsync(
        Guid snapshotId,
        string? message = null,
        CancellationToken ct = default)
    {
        // Get snapshot metadata
        var snapshot = await GetSnapshotByIdAsync(snapshotId, ct);
        if (snapshot == null || snapshot.IsDeleted)
            throw new SnapshotNotFoundException($"Snapshot {snapshotId} not found");

        // Load snapshot data
        var blobPath = $"snapshots/{snapshotId}.snapshot.gz";
        var snapshotData = await _blobStorage.DownloadAsync(blobPath, ct);

        // Decompress and deserialize
        var decompressed = DecompressData(snapshotData);
        var checksum = ComputeChecksum(decompressed);

        if (checksum != snapshot.Checksum)
            throw new SnapshotCorruptedException($"Snapshot {snapshotId} checksum mismatch");

        var graphState = JsonSerializer.Deserialize<Dictionary<string, object>>(decompressed);

        // Begin change tracking
        using var scope = _changeTracker.BeginScope();

        // Delete all current data
        await _graphRepository.DeleteAllEntitiesAsync(ct);
        await _graphRepository.DeleteAllRelationshipsAsync(ct);
        await _graphRepository.DeleteAllClaimsAsync(ct);

        // Restore from snapshot
        await _graphRepository.RestoreGraphStateAsync(graphState, ct);

        // Commit changes as new version
        var restorationMessage = message ?? $"Restored from snapshot: {snapshot.Name}";
        var version = await scope.CommitAsync(restorationMessage, ct);

        // Publish event
        await _mediator.Publish(new SnapshotRestoredEvent
        {
            SnapshotId = snapshotId,
            RestoredVersionId = version.VersionId
        }, ct);

        return version;
    }

    public async Task<IReadOnlyList<GraphSnapshot>> GetSnapshotsForVersionAsync(
        Guid versionId,
        CancellationToken ct = default)
    {
        return await _versionStore.GetSnapshotsForVersionAsync(versionId, ct);
    }

    public async Task<SnapshotListResult> ListSnapshotsAsync(
        SnapshotListQuery query,
        CancellationToken ct = default)
    {
        var allSnapshots = await _versionStore.ListSnapshotsAsync(
            new GraphSnapshotListQuery
            {
                IncludeDeleted = query.IncludeDeleted,
                Skip = query.Skip,
                Take = query.Take + 1 // Get one extra to determine HasMore
            },
            ct);

        var filtered = allSnapshots
            .Where(s => query.NameFilter == null || s.Name.Contains(query.NameFilter))
            .Where(s => query.Tag == null || s.Tag == query.Tag)
            .ToList();

        var sorted = query.SortBy switch
        {
            SnapshotSortBy.Name => query.Descending
                ? filtered.OrderByDescending(s => s.Name).ToList()
                : filtered.OrderBy(s => s.Name).ToList(),
            SnapshotSortBy.Size => query.Descending
                ? filtered.OrderByDescending(s => s.SizeBytes).ToList()
                : filtered.OrderBy(s => s.SizeBytes).ToList(),
            _ => query.Descending
                ? filtered.OrderByDescending(s => s.CreatedAt).ToList()
                : filtered.OrderBy(s => s.CreatedAt).ToList()
        };

        var results = sorted.Take(query.Take).ToList();
        var hasMore = sorted.Count > query.Take;

        return new SnapshotListResult
        {
            Snapshots = results,
            TotalCount = sorted.Count,
            HasMore = hasMore
        };
    }

    private byte[] CompressData(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private byte[] DecompressData(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
        {
            gzip.CopyTo(output);
        }
        return output.ToArray();
    }

    private string ComputeChecksum(byte[] data)
    {
        using var hasher = System.Security.Cryptography.SHA256.Create();
        var hash = hasher.ComputeHash(data);
        return Convert.ToHexString(hash);
    }
}
```

---

## 6. Data Flow Diagrams

### 6.1 Snapshot Creation

```
CreateSnapshotAsync("v2.0 Release")
     ↓
[ExportFullGraphAsync()]
     ├─ Entities (847)
     ├─ Relationships (1,234)
     └─ Claims (543)
     ↓
[Serialize to JSON (45 MB)]
     ↓
[GZip Compress (8.2 MB)]
     ↓
[Compute SHA256 checksum]
     ↓
[Store to blob: snapshots/uuid.snapshot.gz]
     ↓
[Record metadata in DB]
     ↓
Return: GraphSnapshot {
  SnapshotId: uuid,
  Name: "v2.0 Release",
  EntityCount: 847,
  RelationshipCount: 1234,
  SizeBytes: 8600000,
  Checksum: "abc123..."
}
```

### 6.2 Snapshot Restoration

```
RestoreSnapshotAsync(snapshotId)
     ↓
[Verify snapshot exists and valid]
     ↓
[Load compressed snapshot from blob (8.2 MB)]
     ↓
[GZip Decompress (45 MB)]
     ↓
[Verify checksum matches]
     ↓
[Parse JSON to graph state]
     ↓
[BeginChangeTrackingScope]
     ↓
[DeleteAllEntities, DeleteAllRelationships, DeleteAllClaims]
     ↓
[RestoreGraphStateAsync(state)]
     ├─ Create 847 entities
     ├─ Create 1,234 relationships
     └─ Create 543 claims
     ↓
[CommitAsync("Restored from snapshot")]
     ↓
[Create new version recording restoration]
     ↓
Return: GraphVersion (new)
```

---

## 7. Error Handling

### 7.1 Snapshot Not Found

**Scenario:** GetSnapshotByIdAsync with non-existent ID.

**Handling:**
- Query snapshot store
- Return null if not found
- Caller handles null appropriately

### 7.2 Snapshot Corrupted

**Scenario:** RestoreSnapshotAsync with bad checksum.

**Handling:**
- Compute checksum on decompression
- Compare with stored checksum
- Throw SnapshotCorruptedException
- Log error with details
- Do not restore

**Code:**
```csharp
var checksum = ComputeChecksum(decompressed);
if (checksum != snapshot.Checksum)
    throw new SnapshotCorruptedException(
        $"Snapshot {snapshotId} checksum mismatch. " +
        $"Expected {snapshot.Checksum}, got {checksum}");
```

### 7.3 Restoration Failure

**Scenario:** RestoreSnapshotAsync fails during graph restoration.

**Handling:**
- Changes are in transaction scope
- On exception, scope rollback implicit
- Transaction rolls back all changes
- Log error with snapshot ID
- Rethrow for caller to handle

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class GraphSnapshotServiceTests
{
    private GraphSnapshotService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new GraphSnapshotService(
            new MockVersionStore(),
            new MockGraphRepository(),
            new MockBlobStorage(),
            new MockChangeTracker(),
            new MockMediator(),
            new MockLogger());
    }

    [TestMethod]
    public async Task CreateSnapshotAsync_WithValidRequest_CreatesSnapshot()
    {
        var request = new GraphSnapshotCreateRequest
        {
            Name = "v1.0 Release"
        };

        var snapshot = await _service.CreateSnapshotAsync(request);

        Assert.IsNotNull(snapshot);
        Assert.AreEqual("v1.0 Release", snapshot.Name);
        Assert.IsTrue(snapshot.SizeBytes > 0);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task CreateSnapshotAsync_WithoutName_ThrowsException()
    {
        var request = new GraphSnapshotCreateRequest { Name = "" };
        await _service.CreateSnapshotAsync(request);
    }

    [TestMethod]
    public async Task GetSnapshotByIdAsync_WithValidId_ReturnsSnapshot()
    {
        var created = await _service.CreateSnapshotAsync(
            new GraphSnapshotCreateRequest { Name = "Test" });

        var retrieved = await _service.GetSnapshotByIdAsync(created.SnapshotId);

        Assert.IsNotNull(retrieved);
        Assert.AreEqual(created.SnapshotId, retrieved.SnapshotId);
    }

    [TestMethod]
    public async Task GetSnapshotByNameAsync_WithValidName_ReturnsSnapshot()
    {
        var created = await _service.CreateSnapshotAsync(
            new GraphSnapshotCreateRequest { Name = "v1.0 Release" });

        var retrieved = await _service.GetSnapshotByNameAsync("v1.0 Release");

        Assert.IsNotNull(retrieved);
        Assert.AreEqual("v1.0 Release", retrieved.Name);
    }

    [TestMethod]
    public async Task ListSnapshotsAsync_WithQuery_ReturnsFiltered()
    {
        await _service.CreateSnapshotAsync(
            new GraphSnapshotCreateRequest { Name = "v1.0", Tag = "release" });
        await _service.CreateSnapshotAsync(
            new GraphSnapshotCreateRequest { Name = "v2.0", Tag = "release" });

        var result = await _service.ListSnapshotsAsync(
            new SnapshotListQuery { Tag = "release" });

        Assert.AreEqual(2, result.Snapshots.Count);
    }

    [TestMethod]
    public async Task RestoreSnapshotAsync_WithValidSnapshot_RestoresGraph()
    {
        var created = await _service.CreateSnapshotAsync(
            new GraphSnapshotCreateRequest { Name = "Backup" });

        var restored = await _service.RestoreSnapshotAsync(created.SnapshotId);

        Assert.IsNotNull(restored);
        Assert.AreEqual("Restored from snapshot: Backup", restored.Message);
    }

    [TestMethod]
    [ExpectedException(typeof(SnapshotNotFoundException))]
    public async Task RestoreSnapshotAsync_WithDeletedSnapshot_ThrowsException()
    {
        var snapshotId = Guid.NewGuid();
        await _service.RestoreSnapshotAsync(snapshotId);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class GraphSnapshotIntegrationTests
{
    [TestMethod]
    public async Task EndToEnd_CreateRestoreSnapshot()
    {
        // Create snapshot
        var snapshot = await _service.CreateSnapshotAsync(
            new GraphSnapshotCreateRequest { Name = "Backup-1" });

        Assert.IsNotNull(snapshot);
        Assert.IsTrue(snapshot.EntityCount > 0);

        // Restore snapshot
        var restored = await _service.RestoreSnapshotAsync(snapshot.SnapshotId);

        Assert.IsNotNull(restored);
        Assert.AreEqual(snapshot.VersionId, restored.ParentVersionId);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| CreateSnapshotAsync | <5s | Async compression, streaming |
| RestoreSnapshotAsync | <10s | Streaming decompression, batch insert |
| ListSnapshotsAsync | <200ms | Indexed query on metadata |
| CompareSnapshotsAsync | <2s | Lazy delta comparison |
| GetSnapshotByName | <50ms | Name index |

**Optimization Techniques:**
- Async I/O for blob operations
- Streaming compression/decompression
- Batch entity/relationship restoration
- Metadata indexing (name, tag)
- Connection pooling

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Unauthorized restoration | High | RBAC on RestoreSnapshotAsync |
| Snapshot tampering | Critical | Checksum verification, audit logging |
| Data exposure in snapshots | Medium | PII masking, access control |
| Deletion of snapshots | Medium | Soft delete only, retention policy |

---

## 11. License Gating

```csharp
if (!_licenseContext.IsFeatureAvailable(FeatureFlags.CKVS.GraphVersioning))
{
    throw new FeatureNotAvailableException("Snapshots not available in this tier");
}
```

---

## 12. Dependencies & Integration Points

### 12.1 Consumes

- IVersionStore (metadata storage)
- IGraphRepository (export/import)
- IBlobStorage (snapshot data)
- IChangeTracker (restoration tracking)

### 12.2 Events

```csharp
public class SnapshotCreatedEvent
{
    public Guid SnapshotId { get; init; }
    public Guid VersionId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public class SnapshotRestoredEvent
{
    public Guid SnapshotId { get; init; }
    public Guid RestoredVersionId { get; init; }
}
```

---

## 13. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Current graph state | CreateSnapshotAsync called | Snapshot created with all entities/relationships |
| 2 | Snapshot metadata | GetSnapshotByIdAsync called | Snapshot returned with correct data |
| 3 | Named snapshot | GetSnapshotByNameAsync called | Snapshot found by name |
| 4 | Multiple snapshots | ListSnapshotsAsync called | All snapshots returned with pagination |
| 5 | Valid snapshot | RestoreSnapshotAsync called | Graph restored to snapshot state |
| 6 | Snapshot restored | New entities/relationships created | Version created recording restoration |
| 7 | Snapshot data | DownloadSnapshotAsync called | Compressed binary returned |
| 8 | Two snapshots | CompareSnapshotsAsync called | Differences calculated and returned |

---

## 14. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design specification for Snapshot Manager |

---
