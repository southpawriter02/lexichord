# LCS-DES-v0.10.1-KG-b: Design Specification — Change Tracking

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-101-b` | Knowledge Graph Versioning sub-part b |
| **Feature Name** | `Change Tracking` | Capture mutations as versioned changes |
| **Target Version** | `v0.10.1b` | Second sub-part of v0.10.1-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Knowledge Graph Versioning module |
| **Swimlane** | `Graph Versioning` | Versioning vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVersioning` | Graph versioning feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.1-KG](./LCS-SBD-v0.10.1-KG.md) | Knowledge Graph Versioning scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.1-KG S2.1](./LCS-SBD-v0.10.1-KG.md#21-sub-parts) | b = Change Tracking |
| **Estimated Hours** | 6h | Development and testing effort |

---

## 2. Executive Summary

### 2.1 The Requirement

Change Tracking must intercept all graph mutations and capture them as versioned change records. It must:

1. Intercept all mutations to entities, relationships, claims, and axioms
2. Capture before/after values for each change
3. Record metadata (timestamp, user, source document)
4. Calculate change statistics (entities created/modified/deleted, etc.)
5. Generate versioned change records
6. Maintain performance (<100ms overhead per mutation)
7. Provide audit trail for compliance

### 2.2 The Proposed Solution

An interceptor-based approach:

1. **Mutation Interceptor:** Hooks into IGraphRepository mutation methods
2. **Change Capture:** Records before/after state for each mutation
3. **Change Accumulator:** Batches changes into a transaction
4. **Version Creation:** On transaction commit, creates version with accumulated changes
5. **Statistics Calculator:** Computes change counts (created, modified, deleted)

**Key Design Principles:**
- Non-invasive interception
- Automatic capture with no code changes required
- Complete audit trail
- Low performance overhead
- Transaction-aligned batching

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Graph mutations to intercept |
| `ISyncService` | v0.7.6-KG | Sync service triggers (external mutations) |
| `IMediator` | v0.0.7a | Event publishing |
| `IVersionStore` | v0.10.1a | Store captured changes |
| `IUserContext` | v0.9.1 | Current user identification |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `Castle.Core` | latest | Dynamic proxy for interception |
| `System.Text.Json` | latest | JSON serialization of before/after values |

### 3.2 Licensing Behavior

- **Teams Tier:** Full change tracking with default retention (1 year)
- **Enterprise Tier:** Full + unlimited retention

---

## 4. Data Contract (The API)

### 4.1 IChangeTracker Service

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Tracks and records all mutations to the knowledge graph.
/// This service intercepts mutations and captures changes for versioning.
/// </summary>
public interface IChangeTracker
{
    /// <summary>
    /// Begins tracking mutations within a transaction scope.
    /// All mutations within this scope are accumulated.
    /// </summary>
    IChangeTrackingScope BeginScope();

    /// <summary>
    /// Records a mutation manually (used by interceptors).
    /// </summary>
    void RecordMutation(GraphChange change);

    /// <summary>
    /// Gets changes accumulated in the current scope.
    /// </summary>
    IReadOnlyList<GraphChange> GetCurrentChanges();

    /// <summary>
    /// Gets statistics about current accumulated changes.
    /// </summary>
    GraphChangeStats GetChangeStatistics();

    /// <summary>
    /// Completes the current tracking scope and creates a version.
    /// </summary>
    Task<GraphVersion> CompleteTrackingAsync(
        string? message = null,
        CancellationToken ct = default);

    /// <summary>
    /// Aborts the current scope without recording changes.
    /// </summary>
    void AbortScope();

    /// <summary>
    /// Gets change history for a specific entity.
    /// </summary>
    Task<IReadOnlyList<GraphChange>> GetEntityHistoryAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets change history for a specific relationship.
    /// </summary>
    Task<IReadOnlyList<GraphChange>> GetRelationshipHistoryAsync(
        Guid relationshipId,
        CancellationToken ct = default);
}
```

### 4.2 IChangeTrackingScope Interface

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Represents a scope of change tracking.
/// Changes are accumulated and can be committed or rolled back.
/// </summary>
public interface IChangeTrackingScope : IAsyncDisposable
{
    /// <summary>
    /// Current changes accumulated in this scope.
    /// </summary>
    IReadOnlyList<GraphChange> Changes { get; }

    /// <summary>
    /// Statistics about changes in this scope.
    /// </summary>
    GraphChangeStats Statistics { get; }

    /// <summary>
    /// Completes the scope and creates a version.
    /// </summary>
    Task<GraphVersion> CommitAsync(string? message = null, CancellationToken ct = default);

    /// <summary>
    /// Rolls back changes without persisting.
    /// </summary>
    Task RollbackAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a change to the accumulated set.
    /// </summary>
    void AddChange(GraphChange change);
}
```

### 4.3 GraphChange Record (Reference)

```csharp
namespace Lexichord.Abstractions.Contracts.Versioning;

/// <summary>
/// A recorded change to the graph.
/// </summary>
public record GraphChange
{
    /// <summary>
    /// Unique ID for this change record.
    /// </summary>
    public Guid ChangeId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Version this change belongs to.
    /// </summary>
    public Guid VersionId { get; init; }

    /// <summary>
    /// Type of change (Create, Update, Delete).
    /// </summary>
    public GraphChangeType ChangeType { get; init; }

    /// <summary>
    /// Type of element changed (Entity, Relationship, Claim, Axiom).
    /// </summary>
    public GraphElementType ElementType { get; init; }

    /// <summary>
    /// ID of the element that changed.
    /// </summary>
    public Guid ElementId { get; init; }

    /// <summary>
    /// Label/name of the element (for display).
    /// </summary>
    public string? ElementLabel { get; init; }

    /// <summary>
    /// Previous value before this change (null for Create).
    /// </summary>
    public JsonDocument? OldValue { get; init; }

    /// <summary>
    /// New value after this change (null for Delete).
    /// </summary>
    public JsonDocument? NewValue { get; init; }

    /// <summary>
    /// When the change occurred.
    /// </summary>
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who made the change.
    /// </summary>
    public string? ChangedBy { get; init; }

    /// <summary>
    /// Source of the change (API, Sync Service, UI, etc.).
    /// </summary>
    public string? SourceDocument { get; init; }
}

public enum GraphChangeType { Create, Update, Delete }
public enum GraphElementType { Entity, Relationship, Claim, Axiom }
```

### 4.4 GraphChangeStats Record

```csharp
namespace Lexichord.Abstractions.Contracts.Versioning;

/// <summary>
/// Statistics about changes in a version.
/// </summary>
public record GraphChangeStats
{
    /// <summary>
    /// Count of entities created.
    /// </summary>
    public int EntitiesCreated { get; init; }

    /// <summary>
    /// Count of entities modified.
    /// </summary>
    public int EntitiesModified { get; init; }

    /// <summary>
    /// Count of entities deleted.
    /// </summary>
    public int EntitiesDeleted { get; init; }

    /// <summary>
    /// Count of relationships created.
    /// </summary>
    public int RelationshipsCreated { get; init; }

    /// <summary>
    /// Count of relationships deleted.
    /// </summary>
    public int RelationshipsDeleted { get; init; }

    /// <summary>
    /// Count of claims affected (created, modified, or validated).
    /// </summary>
    public int ClaimsAffected { get; init; }
}
```

### 4.5 ChangeTrackingConfiguration Record

```csharp
namespace Lexichord.Modules.CKVS.Configuration;

/// <summary>
/// Configuration for the change tracking system.
/// </summary>
public record ChangeTrackingConfiguration
{
    /// <summary>
    /// Whether change tracking is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Whether to capture before/after values for all changes.
    /// </summary>
    public bool CaptureValues { get; init; } = true;

    /// <summary>
    /// Maximum size of change batches before auto-commit.
    /// </summary>
    public int MaxChangesPerBatch { get; init; } = 1000;

    /// <summary>
    /// Timeout for auto-committing pending changes.
    /// </summary>
    public TimeSpan AutoCommitTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to track axiom changes.
    /// </summary>
    public bool TrackAxiomChanges { get; init; } = true;

    /// <summary>
    /// Whether to track claim changes.
    /// </summary>
    public bool TrackClaimChanges { get; init; } = true;

    /// <summary>
    /// Whether to compute detailed statistics.
    /// </summary>
    public bool ComputeStatistics { get; init; } = true;
}
```

---

## 5. Implementation Strategy

### 5.1 Interception Architecture

```
Application Code Calls IGraphRepository Methods
     ↓
[DynamicProxy Interceptor]
     ↓
[BeforeCall: Capture initial state]
     ↓
[Actual Method Execution]
     ↓
[AfterCall: Capture final state, calculate delta]
     ↓
[RecordMutation → Change Tracker]
     ↓
[Accumulate in Current Scope]
```

### 5.2 GraphRepositoryInterceptor Implementation

```csharp
namespace Lexichord.Modules.CKVS.Interception;

/// <summary>
/// Intercepts IGraphRepository calls to track mutations.
/// </summary>
public class GraphRepositoryInterceptor : IInterceptor
{
    private readonly IChangeTracker _changeTracker;
    private readonly IUserContext _userContext;
    private readonly ILogger<GraphRepositoryInterceptor> _logger;

    public GraphRepositoryInterceptor(
        IChangeTracker changeTracker,
        IUserContext userContext,
        ILogger<GraphRepositoryInterceptor> logger)
    {
        _changeTracker = changeTracker;
        _userContext = userContext;
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var methodName = invocation.Method.Name;

        // Only intercept mutation methods
        if (!IsMutationMethod(methodName))
        {
            invocation.Proceed();
            return;
        }

        try
        {
            var beforeState = CaptureState(invocation);
            invocation.Proceed();
            var afterState = CaptureState(invocation);

            var change = CreateChangeRecord(methodName, beforeState, afterState);
            _changeTracker.RecordMutation(change);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking mutation for method {Method}", methodName);
            throw;
        }
    }

    private bool IsMutationMethod(string methodName) =>
        methodName.Contains("Create") ||
        methodName.Contains("Update") ||
        methodName.Contains("Delete") ||
        methodName.Contains("Upsert");

    private object? CaptureState(IInvocation invocation)
    {
        // Capture state before/after method execution
        // Implementation depends on method signature
        return null;
    }

    private GraphChange CreateChangeRecord(
        string methodName,
        object? beforeState,
        object? afterState)
    {
        var changeType = DetermineChangeType(methodName);
        var elementType = DetermineElementType(methodName);

        return new GraphChange
        {
            ChangeId = Guid.NewGuid(),
            ChangeType = changeType,
            ElementType = elementType,
            ElementId = ExtractElementId(beforeState, afterState),
            ElementLabel = ExtractLabel(beforeState, afterState),
            OldValue = SerializeToJson(beforeState),
            NewValue = SerializeToJson(afterState),
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = _userContext.UserId?.ToString(),
            SourceDocument = "IGraphRepository"
        };
    }
}
```

### 5.3 ChangeTracker Service Implementation

```csharp
namespace Lexichord.Modules.CKVS.Services;

/// <summary>
/// Default implementation of IChangeTracker.
/// </summary>
public class ChangeTrackerService : IChangeTracker
{
    private readonly IVersionStore _versionStore;
    private readonly IUserContext _userContext;
    private readonly IMediator _mediator;
    private readonly ILogger<ChangeTrackerService> _logger;

    [ThreadStatic]
    private static ChangeTrackingScope? _currentScope;

    public IChangeTrackingScope BeginScope()
    {
        _currentScope = new ChangeTrackingScope();
        return _currentScope;
    }

    public void RecordMutation(GraphChange change)
    {
        if (_currentScope == null)
            return;

        _currentScope.AddChange(change);
    }

    public IReadOnlyList<GraphChange> GetCurrentChanges()
    {
        return _currentScope?.Changes ?? [];
    }

    public GraphChangeStats GetChangeStatistics()
    {
        return _currentScope?.Statistics ?? new GraphChangeStats();
    }

    public async Task<GraphVersion> CompleteTrackingAsync(
        string? message = null,
        CancellationToken ct = default)
    {
        if (_currentScope == null)
            throw new InvalidOperationException("No tracking scope active");

        try
        {
            var version = await _currentScope.CommitAsync(message, ct);
            await _mediator.Publish(new VersionCreatedEvent { VersionId = version.VersionId }, ct);
            return version;
        }
        finally
        {
            _currentScope = null;
        }
    }

    public void AbortScope()
    {
        _currentScope = null;
    }

    public async Task<IReadOnlyList<GraphChange>> GetEntityHistoryAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        // Query from version store
        var changes = await _versionStore.GetDeltasAsync(default, default, ct);
        return changes.Where(c => c.ElementId == entityId && c.ElementType == GraphElementType.Entity).ToList();
    }

    public async Task<IReadOnlyList<GraphChange>> GetRelationshipHistoryAsync(
        Guid relationshipId,
        CancellationToken ct = default)
    {
        var changes = await _versionStore.GetDeltasAsync(default, default, ct);
        return changes.Where(c => c.ElementId == relationshipId && c.ElementType == GraphElementType.Relationship).ToList();
    }
}
```

### 5.4 ChangeTrackingScope Implementation

```csharp
namespace Lexichord.Modules.CKVS.Services;

public class ChangeTrackingScope : IChangeTrackingScope
{
    private readonly List<GraphChange> _changes = [];
    private readonly IVersionStore _versionStore;
    private readonly IUserContext _userContext;

    public IReadOnlyList<GraphChange> Changes => _changes.AsReadOnly();
    public GraphChangeStats Statistics => CalculateStatistics();

    public void AddChange(GraphChange change)
    {
        _changes.Add(change);
    }

    public async Task<GraphVersion> CommitAsync(string? message = null, CancellationToken ct = default)
    {
        var versionRequest = new GraphVersionCreateRequest
        {
            BranchName = "main",
            Message = message,
            CreatedBy = _userContext.UserId?.ToString(),
            Stats = CalculateStatistics()
        };

        var version = await _versionStore.CreateVersionAsync(versionRequest, ct);
        await _versionStore.StoreDeltaAsync(version.VersionId, _changes, ct);

        return version;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        _changes.Clear();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _changes.Clear();
        return ValueTask.CompletedTask;
    }

    private GraphChangeStats CalculateStatistics()
    {
        return new GraphChangeStats
        {
            EntitiesCreated = _changes.Count(c => c.ElementType == GraphElementType.Entity && c.ChangeType == GraphChangeType.Create),
            EntitiesModified = _changes.Count(c => c.ElementType == GraphElementType.Entity && c.ChangeType == GraphChangeType.Update),
            EntitiesDeleted = _changes.Count(c => c.ElementType == GraphElementType.Entity && c.ChangeType == GraphChangeType.Delete),
            RelationshipsCreated = _changes.Count(c => c.ElementType == GraphElementType.Relationship && c.ChangeType == GraphChangeType.Create),
            RelationshipsDeleted = _changes.Count(c => c.ElementType == GraphElementType.Relationship && c.ChangeType == GraphChangeType.Delete),
            ClaimsAffected = _changes.Count(c => c.ElementType == GraphElementType.Claim)
        };
    }
}
```

---

## 6. Data Flow Diagrams

### 6.1 Mutation Capture Flow

```
Application Calls CreateEntityAsync()
     ↓
[Interceptor BeforeMutation Hook]
     ↓
[Capture initial state]
     ↓
[Call actual CreateEntityAsync()]
     ↓
[Interceptor AfterMutation Hook]
     ↓
[Capture final state, compare]
     ↓
[Create GraphChange record with before/after]
     ↓
[RecordMutation(change)]
     ↓
[Add to current tracking scope]
```

### 6.2 Transaction Commit Flow

```
Application Code
     ↓
using var scope = changeTracker.BeginScope()
     ↓
[Multiple mutations via IGraphRepository]
     ↓
[Each mutation intercepted → recorded]
     ↓
[Mutations accumulated in scope]
     ↓
await scope.CommitAsync("Update API documentation")
     ↓
[Create version with all accumulated changes]
     ↓
[Store deltas in version store]
     ↓
[Return GraphVersion]
```

### 6.3 Statistics Calculation

```
GraphChange Records (50 changes)
     ├─ 5 Entity Creates
     ├─ 10 Entity Updates
     ├─ 2 Entity Deletes
     ├─ 8 Relationship Creates
     ├─ 3 Relationship Deletes
     └─ 22 Claim Changes
     ↓
CalculateStatistics()
     ↓
GraphChangeStats {
  EntitiesCreated: 5,
  EntitiesModified: 10,
  EntitiesDeleted: 2,
  RelationshipsCreated: 8,
  RelationshipsDeleted: 3,
  ClaimsAffected: 22
}
```

---

## 7. Error Handling

### 7.1 No Active Scope

**Scenario:** RecordMutation called without active tracking scope.

**Handling:**
- Silently return (changes not tracked)
- Log warning for debugging
- Allow normal operation to continue

**Code:**
```csharp
public void RecordMutation(GraphChange change)
{
    if (_currentScope == null)
    {
        _logger.LogWarning("RecordMutation called without active scope");
        return;
    }
    _currentScope.AddChange(change);
}
```

### 7.2 Scope Commit Failure

**Scenario:** CommitAsync fails due to version store error.

**Handling:**
- Catch exception
- Log detailed error
- Rollback scope
- Rethrow exception
- Call code can retry

**Code:**
```csharp
try
{
    var version = await _versionStore.CreateVersionAsync(request, ct);
    await _versionStore.StoreDeltaAsync(version.VersionId, _changes, ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to commit tracking scope");
    await RollbackAsync(ct);
    throw;
}
```

### 7.3 Interception Error

**Scenario:** Exception during mutation method execution.

**Handling:**
- Log error with method name
- Rethrow to caller
- Don't record change (rollback implicit)

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class ChangeTrackerTests
{
    private ChangeTrackerService _tracker = null!;

    [TestInitialize]
    public void Setup()
    {
        _tracker = new ChangeTrackerService(null, null, null);
    }

    [TestMethod]
    public void BeginScope_CreatesActiveScope()
    {
        var scope = _tracker.BeginScope();
        Assert.IsNotNull(scope);
        Assert.AreEqual(0, scope.Changes.Count);
    }

    [TestMethod]
    public void RecordMutation_WithActiveScope_AddsChange()
    {
        _tracker.BeginScope();
        var change = new GraphChange
        {
            ChangeId = Guid.NewGuid(),
            ChangeType = GraphChangeType.Create,
            ElementType = GraphElementType.Entity,
            ElementId = Guid.NewGuid()
        };

        _tracker.RecordMutation(change);

        var changes = _tracker.GetCurrentChanges();
        Assert.AreEqual(1, changes.Count);
    }

    [TestMethod]
    public void GetChangeStatistics_CalculatesCorrectly()
    {
        _tracker.BeginScope();

        // Add 2 entity creates, 3 entity updates, 1 entity delete
        for (int i = 0; i < 2; i++)
            _tracker.RecordMutation(new GraphChange
            {
                ChangeType = GraphChangeType.Create,
                ElementType = GraphElementType.Entity,
                ElementId = Guid.NewGuid()
            });

        for (int i = 0; i < 3; i++)
            _tracker.RecordMutation(new GraphChange
            {
                ChangeType = GraphChangeType.Update,
                ElementType = GraphElementType.Entity,
                ElementId = Guid.NewGuid()
            });

        _tracker.RecordMutation(new GraphChange
        {
            ChangeType = GraphChangeType.Delete,
            ElementType = GraphElementType.Entity,
            ElementId = Guid.NewGuid()
        });

        var stats = _tracker.GetChangeStatistics();
        Assert.AreEqual(2, stats.EntitiesCreated);
        Assert.AreEqual(3, stats.EntitiesModified);
        Assert.AreEqual(1, stats.EntitiesDeleted);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task CompleteTracking_WithoutActiveScope_ThrowsException()
    {
        await _tracker.CompleteTrackingAsync();
    }

    [TestMethod]
    public void AbortScope_ClearsChanges()
    {
        _tracker.BeginScope();
        _tracker.RecordMutation(new GraphChange
        {
            ChangeType = GraphChangeType.Create,
            ElementType = GraphElementType.Entity,
            ElementId = Guid.NewGuid()
        });

        _tracker.AbortScope();

        var changes = _tracker.GetCurrentChanges();
        Assert.AreEqual(0, changes.Count);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class ChangeTrackingIntegrationTests
{
    private IGraphRepository _graphRepository = null!;
    private IChangeTracker _changeTracker = null!;

    [TestInitialize]
    public void Setup()
    {
        // Set up with real or mocked dependencies
        _changeTracker = new ChangeTrackerService(null, null, null);
    }

    [TestMethod]
    public async Task Mutation_IsInterceptedAndTracked()
    {
        using var scope = _changeTracker.BeginScope();

        // Simulate mutation
        _changeTracker.RecordMutation(new GraphChange
        {
            ChangeType = GraphChangeType.Create,
            ElementType = GraphElementType.Entity,
            ElementId = Guid.NewGuid(),
            ElementLabel = "User"
        });

        var changes = _changeTracker.GetCurrentChanges();
        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("User", changes[0].ElementLabel);
    }

    [TestMethod]
    public async Task CompleteTracking_CreatesVersionWithChanges()
    {
        using var scope = _changeTracker.BeginScope();

        _changeTracker.RecordMutation(new GraphChange
        {
            ChangeType = GraphChangeType.Create,
            ElementType = GraphElementType.Entity,
            ElementId = Guid.NewGuid()
        });

        var version = await _changeTracker.CompleteTrackingAsync("Test commit");

        Assert.IsNotNull(version);
        Assert.AreEqual("Test commit", version.Message);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| RecordMutation | <1ms | In-memory append |
| GetChangeStatistics | <5ms | Cached calculation |
| CommitAsync | <100ms | Batch insert |
| Interception overhead | <1ms | No-op for queries |
| Scope creation | <1ms | Simple object creation |

**Optimization Techniques:**
- Lazy statistics calculation
- Batch delta inserts
- Memory pooling for change objects
- Async/await for I/O

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Sensitive data in values | Medium | Mask PII in captured values, role-based filtering |
| Unintended tracking | Low | Feature gate, configuration |
| Audit tampering | Critical | Immutable records, signature verification |
| Performance DoS | Medium | Batch limits, auto-commit timeouts |

---

## 11. License Gating

```csharp
if (!_licenseContext.IsFeatureAvailable(FeatureFlags.CKVS.GraphVersioning))
{
    throw new FeatureNotAvailableException("Graph versioning not available");
}
```

| Tier | Support |
| :--- | :--- |
| Core | ❌ Not available |
| WriterPro | ❌ Not available |
| Teams | ✅ Full tracking |
| Enterprise | ✅ Full tracking + unlimited retention |

---

## 12. Dependencies & Integration Points

### 12.1 Produces

- Version records (consumed by Version Store)
- Delta records (consumed by Version Store)
- Change events (published via IMediator)

### 12.2 Consumes

- IGraphRepository mutations
- IUserContext for user identification
- IVersionStore for persistence

---

## 13. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Active tracking scope | Entity created | Change recorded with Create type |
| 2 | Active tracking scope | Entity updated | Change recorded with Update type + before/after values |
| 3 | Multiple mutations | Statistics calculated | Counts accurate (created, modified, deleted) |
| 4 | Accumulated changes | CommitAsync called | Version created with all changes |
| 5 | Active scope | AbortScope called | Changes discarded |
| 6 | No active scope | RecordMutation called | Gracefully ignored |
| 7 | Entity mutations | GetEntityHistory called | All changes for entity returned |
| 8 | Large batch | 1000+ changes | Stats computed correctly |

---

## 14. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design specification for Change Tracking |

---
