# LCS-DES-v0.10.1-KG-e: Design Specification — Branch/Merge

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-101-e` | Knowledge Graph Versioning sub-part e |
| **Feature Name** | `Branch/Merge` | Support graph branching and merging |
| **Target Version** | `v0.10.1e` | Fifth sub-part of v0.10.1-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Knowledge Graph Versioning module |
| **Swimlane** | `Graph Versioning` | Versioning vertical |
| **License Tier** | `Enterprise` | Available in Enterprise tier only |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVersioning` | Graph versioning feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.1-KG](./LCS-SBD-v0.10.1-KG.md) | Knowledge Graph Versioning scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.1-KG S2.1](./LCS-SBD-v0.10.1-KG.md#21-sub-parts) | e = Branch/Merge |
| **Estimated Hours** | 10h | Development and testing effort |

---

## 2. Executive Summary

### 2.1 The Requirement

Branch/Merge enables experimental graph changes in isolation from production. It must:

1. Create new branches from current or historical versions
2. Switch between branches (context switching)
3. Track branch ancestry and divergence
4. Detect merge conflicts when branches diverge
5. Perform three-way merge with conflict resolution
6. Support branch deletion and cleanup
7. Provide branch comparison and status information

### 2.2 The Proposed Solution

A comprehensive branching service with:

1. **Branch Creation:** New branch at current state with ancestry tracking
2. **Branch Switching:** Atomically switch active branch
3. **Branch Management:** List, describe, delete branches
4. **Merge Operations:** Three-way merge with conflict detection
5. **Conflict Resolution:** Interactive conflict resolution UI
6. **Branch Comparison:** Show ahead/behind counts and divergence

**Key Design Principles:**
- Git-like branching semantics
- Immutable version history per branch
- Three-way merge for smart conflict detection
- Fast-forward detection
- Automatic cleanup of stale branches

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IVersionStore` | v0.10.1a | Version creation and retrieval |
| `IChangeTracker` | v0.10.1b | Change recording |
| `IGraphRepository` | v0.4.5e | Graph state operations |
| `IMediator` | v0.0.7a | Event publishing |
| PostgreSQL | v0.4.6-KG | Branch metadata storage |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Text.Json` | latest | Conflict serialization |

### 3.2 Licensing Behavior

- **Teams Tier:** Not available (branching is Enterprise-only)
- **Enterprise Tier:** Full branch/merge functionality with unlimited branches

### 3.3 Branch Strategy

```
Version History:
  main:      V1 → V2 → V3 → V4 → V5 (HEAD)
                 ↓
  feature:   V2.1 → V2.2 → V2.3 (HEAD)
                          ↓
  experiment: V2.2.1 → V2.2.2 (stale, can cleanup)

Branch Metadata:
  - Name
  - HeadVersionId
  - BaseBranchVersionId
  - CreatedAt, CreatedBy
  - Description
  - IsDefault
```

---

## 4. Data Contract (The API)

### 4.1 IGraphBranchService Interface

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Manages graph branches for experimental changes and feature development.
/// Provides Git-like branching semantics with merge conflict detection.
/// </summary>
public interface IGraphBranchService
{
    /// <summary>
    /// Creates a new branch from current state.
    /// </summary>
    Task<GraphBranch> CreateBranchAsync(
        string branchName,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new branch from a specific version.
    /// </summary>
    Task<GraphBranch> CreateBranchFromVersionAsync(
        string branchName,
        Guid fromVersionId,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Switches the active branch.
    /// </summary>
    Task SwitchBranchAsync(
        string branchName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active branch.
    /// </summary>
    Task<GraphBranch> GetCurrentBranchAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific branch by name.
    /// </summary>
    Task<GraphBranch?> GetBranchAsync(
        string branchName,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all branches with their metadata.
    /// </summary>
    Task<IReadOnlyList<GraphBranch>> GetBranchesAsync(CancellationToken ct = default);

    /// <summary>
    /// Merges a source branch into the current branch.
    /// Detects conflicts and returns merge result.
    /// </summary>
    Task<MergeResult> MergeBranchAsync(
        string sourceBranch,
        MergeOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets merge status/preview without committing.
    /// </summary>
    Task<MergeResult> PreviewMergeAsync(
        string sourceBranch,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a branch.
    /// </summary>
    Task DeleteBranchAsync(
        string branchName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets branch comparison (ahead/behind counts).
    /// </summary>
    Task<BranchComparison> CompareBranchesAsync(
        string branchA,
        string branchB,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all changes between two branches.
    /// </summary>
    Task<IReadOnlyList<GraphChange>> GetBranchDiffAsync(
        string fromBranch,
        string toBranch,
        CancellationToken ct = default);
}
```

### 4.2 MergeOptions Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Options for merge operations.
/// </summary>
public record MergeOptions
{
    /// <summary>
    /// User ID performing the merge.
    /// </summary>
    public string? MergedBy { get; init; }

    /// <summary>
    /// Merge commit message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// If true, automatically resolve conflicts using source branch values.
    /// If false, return conflicts for manual resolution.
    /// </summary>
    public bool AutoResolveConflicts { get; init; } = false;

    /// <summary>
    /// Strategy for auto-resolving: "ours" (keep target) or "theirs" (take source).
    /// </summary>
    public ConflictResolutionStrategy ResolutionStrategy { get; init; } = ConflictResolutionStrategy.Ours;

    /// <summary>
    /// If true, don't actually commit the merge (preview only).
    /// </summary>
    public bool DryRun { get; init; } = false;

    /// <summary>
    /// Conflict resolutions provided by user.
    /// </summary>
    public IReadOnlyDictionary<string, ConflictResolution>? ResolutionMap { get; init; }
}

public enum ConflictResolutionStrategy
{
    Ours = 1,      // Keep target branch values
    Theirs = 2,    // Take source branch values
    Manual = 3     // Require explicit resolution
}

public record ConflictResolution
{
    public string ElementId { get; init; } = "";
    public object? ResolvedValue { get; init; }
    public ConflictResolutionChoice Choice { get; init; }
}

public enum ConflictResolutionChoice
{
    UseSource = 1,  // Use source branch value
    UseTarget = 2,  // Use target branch value
    UseCustom = 3   // Use provided custom value
}
```

### 4.3 MergeResult Record

```csharp
namespace Lexichord.Abstractions.Contracts.Versioning;

/// <summary>
/// Result of a merge operation.
/// </summary>
public record MergeResult
{
    /// <summary>
    /// Overall merge status.
    /// </summary>
    public MergeStatus Status { get; init; }

    /// <summary>
    /// Version ID created by merge (if successful).
    /// </summary>
    public Guid? MergeVersionId { get; init; }

    /// <summary>
    /// List of detected conflicts.
    /// </summary>
    public IReadOnlyList<MergeConflict> Conflicts { get; init; } = [];

    /// <summary>
    /// Statistics about merged changes.
    /// </summary>
    public GraphChangeStats MergedStats { get; init; } = new();

    /// <summary>
    /// Message describing merge result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Timestamp of merge operation.
    /// </summary>
    public DateTimeOffset? MergedAt { get; init; }

    /// <summary>
    /// User who performed merge.
    /// </summary>
    public string? MergedBy { get; init; }
}

public enum MergeStatus
{
    Success = 1,         // Merge completed without conflicts
    Conflict = 2,        // Merge has conflicts requiring resolution
    NothingToMerge = 3,  // Source and target are identical
    FastForward = 4      // Merge is fast-forward (no merge commit needed)
}
```

### 4.4 MergeConflict Record

```csharp
namespace Lexichord.Abstractions.Contracts.Versioning;

/// <summary>
/// Represents a conflict during merge.
/// </summary>
public record MergeConflict
{
    /// <summary>
    /// Type of graph element in conflict.
    /// </summary>
    public GraphElementType ElementType { get; init; }

    /// <summary>
    /// ID of the conflicting element.
    /// </summary>
    public Guid ElementId { get; init; }

    /// <summary>
    /// Human-readable label of element.
    /// </summary>
    public string? ElementLabel { get; init; }

    /// <summary>
    /// Property name that conflicts.
    /// </summary>
    public string PropertyName { get; init; } = "";

    /// <summary>
    /// Value in source branch.
    /// </summary>
    public object? SourceValue { get; init; }

    /// <summary>
    /// Value in target branch.
    /// </summary>
    public object? TargetValue { get; init; }

    /// <summary>
    /// Common ancestor value (base).
    /// </summary>
    public object? BaseValue { get; init; }

    /// <summary>
    /// Type of conflict.
    /// </summary>
    public ConflictType ConflictType { get; init; }
}

public enum ConflictType
{
    UpdateUpdate = 1,  // Both branches modified same property
    DeleteUpdate = 2,  // One deleted, other updated
    AddAdd = 3,        // Both added different values
    TypeChange = 4     // Value type differs between branches
}
```

### 4.5 GraphBranch Record

```csharp
namespace Lexichord.Abstractions.Contracts.Versioning;

/// <summary>
/// Represents a graph branch.
/// </summary>
public record GraphBranch
{
    /// <summary>
    /// Branch name (unique).
    /// </summary>
    public string Name { get; init; } = "main";

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version ID at branch head.
    /// </summary>
    public Guid HeadVersionId { get; init; }

    /// <summary>
    /// Base branch version (where this branch originated).
    /// </summary>
    public Guid? BaseBranchVersionId { get; init; }

    /// <summary>
    /// When branch was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// User who created branch.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Whether this is the default branch.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Whether branch is archived/deleted.
    /// </summary>
    public bool IsArchived { get; init; } = false;

    /// <summary>
    /// Last activity timestamp.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; init; }
}
```

### 4.6 BranchComparison Record

```csharp
namespace Lexichord.Modules.CKVS.Abstractions;

/// <summary>
/// Comparison between two branches.
/// </summary>
public record BranchComparison
{
    /// <summary>
    /// Source branch name.
    /// </summary>
    public string FromBranch { get; init; } = "";

    /// <summary>
    /// Target branch name.
    /// </summary>
    public string ToBranch { get; init; } = "";

    /// <summary>
    /// Number of commits in FromBranch not in ToBranch.
    /// </summary>
    public int AheadCount { get; init; }

    /// <summary>
    /// Number of commits in ToBranch not in FromBranch.
    /// </summary>
    public int BehindCount { get; init; }

    /// <summary>
    /// Total diverged commits.
    /// </summary>
    public int DivergedCount => AheadCount + BehindCount;

    /// <summary>
    /// Whether branches can fast-forward.
    /// </summary>
    public bool CanFastForward { get; init; }

    /// <summary>
    /// Common ancestor version ID.
    /// </summary>
    public Guid? CommonAncestor { get; init; }

    /// <summary>
    /// Total changes from → to branch.
    /// </summary>
    public GraphChangeStats TotalChanges { get; init; } = new();
}
```

---

## 5. Three-Way Merge Algorithm

### 5.1 Merge Process

```
Input: base_version, source_branch_head, target_branch_head

1. Load versions:
   - base_state = graph state at base_version
   - source_state = graph state at source_branch_head
   - target_state = current graph state (target_branch_head)

2. For each entity/relationship:
   a. If unchanged in source: no conflict
   b. If unchanged in target: take source (auto-apply)
   c. If changed in both (different values): CONFLICT
   d. If deleted in source, modified in target: CONFLICT
   e. If deleted in target, modified in source: CONFLICT

3. Collect conflicts and changes:
   - Changes that don't conflict → apply
   - Conflicts → report for resolution

4. If no conflicts:
   - Create merge version
   - Apply all non-conflicting changes
   - Return MergeResult.Success

5. If conflicts:
   - Return MergeResult.Conflict with conflict list
   - Wait for user resolution

6. On resolution:
   - Apply resolved conflicts
   - Create final merge version
   - Return MergeResult.Success
```

### 5.2 Conflict Detection Example

```csharp
// Scenario: Same property modified differently

base:    Entity { name: "Old Name", version: "1.0" }
source:  Entity { name: "New Name", version: "1.0" }  // Changed name
target:  Entity { name: "Old Name", version: "2.0" }  // Changed version

Result: CONFLICT on 'name' property
  - sourceValue: "New Name"
  - targetValue: "Old Name"
  - baseValue: "Old Name"
```

---

## 6. Implementation Details

### 6.1 Branch Storage Schema (PostgreSQL)

```sql
CREATE TABLE graph_branches (
    BranchId UUID PRIMARY KEY,
    BranchName VARCHAR(255) NOT NULL UNIQUE,
    Description VARCHAR(1024),
    HeadVersionId UUID NOT NULL REFERENCES graph_versions(VersionId),
    BaseBranchVersionId UUID REFERENCES graph_versions(VersionId),
    CreatedAt TIMESTAMPTZ NOT NULL,
    CreatedBy VARCHAR(255),
    IsDefault BOOLEAN DEFAULT false,
    IsArchived BOOLEAN DEFAULT false,
    LastModifiedAt TIMESTAMPTZ
);

CREATE TABLE merge_history (
    MergeId UUID PRIMARY KEY,
    SourceBranchName VARCHAR(255) NOT NULL,
    TargetBranchName VARCHAR(255) NOT NULL,
    MergeVersionId UUID NOT NULL REFERENCES graph_versions(VersionId),
    Status INT NOT NULL, -- 0=Success, 1=Conflict, 2=NothingToMerge
    ConflictsCount INT DEFAULT 0,
    MergedAt TIMESTAMPTZ NOT NULL,
    MergedBy VARCHAR(255),
    Message TEXT
);

CREATE TABLE merge_conflicts (
    ConflictId UUID PRIMARY KEY,
    MergeId UUID NOT NULL REFERENCES merge_history(MergeId),
    ElementId UUID NOT NULL,
    ElementType INT NOT NULL, -- 0=Entity, 1=Relationship, 2=Claim
    PropertyName VARCHAR(255),
    SourceValue TEXT, -- JSON
    TargetValue TEXT, -- JSON
    BaseValue TEXT,   -- JSON
    ConflictType INT NOT NULL,
    Resolution INT DEFAULT 0 -- 0=Unresolved, 1=UseSource, 2=UseTarget
);

CREATE INDEX idx_branches_head ON graph_branches(HeadVersionId);
CREATE INDEX idx_branches_active ON graph_branches(IsArchived, IsDefault);
CREATE INDEX idx_merge_history_branches ON merge_history(SourceBranchName, TargetBranchName);
```

### 6.2 Branch Manager Service Pattern

```csharp
namespace Lexichord.Modules.CKVS.Services;

public class GraphBranchService : IGraphBranchService
{
    private readonly IVersionStore _versionStore;
    private readonly IChangeTracker _changeTracker;
    private readonly IGraphRepository _graphRepo;
    private readonly IMediator _mediator;
    private readonly ILogger<GraphBranchService> _logger;
    private readonly IBranchStore _branchStore;

    public async Task<GraphBranch> CreateBranchAsync(
        string branchName,
        string? description = null,
        CancellationToken ct = default)
    {
        // Validate branch name (no special characters, etc.)
        ValidateBranchName(branchName);

        // Get current head version
        var currentVersion = await _versionStore.GetCurrentVersionAsync(ct);

        // Create branch record
        var branch = new GraphBranch
        {
            Name = branchName,
            Description = description,
            HeadVersionId = currentVersion.VersionId,
            BaseBranchVersionId = currentVersion.VersionId,
            CreatedAt = DateTimeOffset.UtcNow,
            IsDefault = false
        };

        // Store branch
        await _branchStore.CreateBranchAsync(branch, ct);

        // Publish event
        await _mediator.PublishAsync(new BranchCreatedEvent(branch), ct);

        _logger.LogInformation("Branch '{BranchName}' created from version {VersionId}",
            branchName, currentVersion.VersionId);

        return branch;
    }

    public async Task<MergeResult> MergeBranchAsync(
        string sourceBranch,
        MergeOptions options,
        CancellationToken ct = default)
    {
        // Get branches
        var sourceBranchInfo = await _branchStore.GetBranchAsync(sourceBranch, ct)
            ?? throw new InvalidOperationException($"Source branch '{sourceBranch}' not found");

        var targetBranchInfo = await _branchStore.GetBranchAsync(
            await _branchStore.GetCurrentBranchNameAsync(ct), ct)
            ?? throw new InvalidOperationException("Current branch not found");

        // Get versions
        var sourceHead = await _versionStore.GetVersionByIdAsync(
            sourceBranchInfo.HeadVersionId, ct)
            ?? throw new InvalidOperationException("Source head version not found");

        var targetHead = await _versionStore.GetVersionByIdAsync(
            targetBranchInfo.HeadVersionId, ct)
            ?? throw new InvalidOperationException("Target head version not found");

        // Find common ancestor
        var commonAncestor = await FindCommonAncestorAsync(sourceHead.VersionId, targetHead.VersionId, ct);

        // Perform three-way merge
        var mergeResult = await PerformThreeWayMergeAsync(
            commonAncestor, sourceHead.VersionId, targetHead.VersionId, options, ct);

        return mergeResult;
    }

    private async Task<MergeResult> PerformThreeWayMergeAsync(
        Guid baseVersionId,
        Guid sourceVersionId,
        Guid targetVersionId,
        MergeOptions options,
        CancellationToken ct)
    {
        // Load base, source, target states
        var baseState = await _graphRepo.GetGraphStateAsync(baseVersionId, ct);
        var sourceState = await _graphRepo.GetGraphStateAsync(sourceVersionId, ct);
        var targetState = await _graphRepo.GetGraphStateAsync(targetVersionId, ct);

        var conflicts = new List<MergeConflict>();
        var changes = new List<GraphChange>();

        // Detect conflicts in entities
        foreach (var sourceEntity in sourceState.Entities)
        {
            var baseEntity = baseState.Entities.FirstOrDefault(e => e.Id == sourceEntity.Id);
            var targetEntity = targetState.Entities.FirstOrDefault(e => e.Id == sourceEntity.Id);

            if (DetectEntityConflict(baseEntity, sourceEntity, targetEntity, out var conflict))
            {
                conflicts.Add(conflict);
            }
            else if (sourceEntity != baseEntity)
            {
                changes.Add(GraphChange.FromEntity(sourceEntity, sourceVersionId));
            }
        }

        // If conflicts and not auto-resolving, return with conflicts
        if (conflicts.Count > 0 && !options.AutoResolveConflicts)
        {
            return new MergeResult
            {
                Status = MergeStatus.Conflict,
                Conflicts = conflicts,
                Message = $"Merge has {conflicts.Count} conflicts"
            };
        }

        // Auto-resolve conflicts if requested
        if (conflicts.Count > 0 && options.AutoResolveConflicts)
        {
            ResolveConflictsAutomatically(conflicts, options, changes);
        }

        // Create merge version
        var mergeVersion = await _versionStore.CreateVersionAsync(
            new GraphVersionCreateRequest
            {
                ParentVersionId = targetVersionId,
                BranchName = await _branchStore.GetCurrentBranchNameAsync(ct),
                Message = options.Message ?? $"Merge {sourceBranchInfo.Name} into current branch",
                CreatedBy = options.MergedBy,
                Stats = ComputeStats(changes)
            }, ct);

        // Apply changes
        foreach (var change in changes)
        {
            await _changeTracker.RecordChangeAsync(mergeVersion.VersionId, change, ct);
        }

        // Publish merge event
        await _mediator.PublishAsync(
            new BranchMergedEvent(sourceBranchInfo.Name, mergeVersion), ct);

        return new MergeResult
        {
            Status = MergeStatus.Success,
            MergeVersionId = mergeVersion.VersionId,
            MergedStats = ComputeStats(changes),
            Message = "Merge completed successfully",
            MergedAt = mergeVersion.CreatedAt,
            MergedBy = options.MergedBy
        };
    }

    private bool DetectEntityConflict(
        Entity? baseEntity,
        Entity sourceEntity,
        Entity? targetEntity,
        out MergeConflict conflict)
    {
        conflict = default!;

        // Both modified to different values
        if (baseEntity != null && sourceEntity != baseEntity && targetEntity != null && sourceEntity != targetEntity)
        {
            conflict = new MergeConflict
            {
                ElementType = GraphElementType.Entity,
                ElementId = sourceEntity.Id,
                ElementLabel = sourceEntity.Label,
                SourceValue = sourceEntity,
                TargetValue = targetEntity,
                BaseValue = baseEntity,
                ConflictType = ConflictType.UpdateUpdate
            };
            return true;
        }

        return false;
    }
}
```

---

## 7. Error Handling

### 7.1 Invalid Branch Name

**Scenario:** Attempting to create branch with invalid characters.

**Handling:**
- Validate branch name format (alphanumeric, hyphens, underscores, slashes)
- Throw `InvalidOperationException`
- Log validation failure

**Code:**
```csharp
private void ValidateBranchName(string branchName)
{
    if (string.IsNullOrWhiteSpace(branchName))
        throw new InvalidOperationException("Branch name cannot be empty");

    if (branchName.Length > 255)
        throw new InvalidOperationException("Branch name too long (max 255 chars)");

    if (!Regex.IsMatch(branchName, @"^[a-zA-Z0-9/_-]+$"))
        throw new InvalidOperationException("Branch name contains invalid characters");

    if (branchName == "main" || branchName == "master")
        throw new InvalidOperationException("Cannot create branch with reserved name");
}
```

### 7.2 Merge Conflict Resolution Failure

**Scenario:** User provides invalid conflict resolutions.

**Handling:**
- Validate each resolution maps to actual conflict
- Check resolved values are compatible with element type
- Return validation error details

**Code:**
```csharp
private void ValidateConflictResolutions(
    IReadOnlyList<MergeConflict> conflicts,
    IReadOnlyDictionary<string, ConflictResolution> resolutions)
{
    foreach (var conflict in conflicts)
    {
        var key = $"{conflict.ElementId}:{conflict.PropertyName}";
        if (!resolutions.TryGetValue(key, out var resolution))
            throw new InvalidOperationException($"Missing resolution for conflict: {key}");

        // Validate resolution value
        if (resolution.Choice == ConflictResolutionChoice.UseCustom && resolution.ResolvedValue == null)
            throw new InvalidOperationException($"Custom resolution requires a value: {key}");
    }
}
```

### 7.3 Branch Not Found

**Scenario:** Attempt to switch to non-existent branch.

**Handling:**
- Query branch store
- Return null or throw appropriate exception
- List available branches in error message

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class GraphBranchServiceTests
{
    private GraphBranchService _service;
    private Mock<IVersionStore> _versionStore;
    private Mock<IBranchStore> _branchStore;
    private Mock<IGraphRepository> _graphRepo;

    [TestInitialize]
    public void Setup()
    {
        _versionStore = new Mock<IVersionStore>();
        _branchStore = new Mock<IBranchStore>();
        _graphRepo = new Mock<IGraphRepository>();

        _service = new GraphBranchService(
            _versionStore.Object,
            _branchStore.Object,
            _graphRepo.Object,
            Mock.Of<IMediator>(),
            Mock.Of<ILogger<GraphBranchService>>());
    }

    [TestMethod]
    public async Task CreateBranch_WithValidName_CreatesBranch()
    {
        // Arrange
        var branchName = "feature/api-v2";
        var currentVersion = new GraphVersion { VersionId = Guid.NewGuid() };
        _versionStore.Setup(x => x.GetCurrentVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentVersion);

        // Act
        var result = await _service.CreateBranchAsync(branchName);

        // Assert
        Assert.AreEqual(branchName, result.Name);
        Assert.AreEqual(currentVersion.VersionId, result.HeadVersionId);
        Assert.IsFalse(result.IsDefault);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task CreateBranch_WithInvalidName_Throws()
    {
        await _service.CreateBranchAsync("invalid@branch!");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task CreateBranch_WithReservedName_Throws()
    {
        await _service.CreateBranchAsync("main");
    }

    [TestMethod]
    public async Task MergeBranch_NoConflicts_SuccessfulMerge()
    {
        // Arrange
        var sourceVersion = new GraphVersion { VersionId = Guid.NewGuid() };
        var targetVersion = new GraphVersion { VersionId = Guid.NewGuid() };
        var baseVersion = new GraphVersion { VersionId = Guid.NewGuid() };

        // Setup mocks...

        // Act
        var result = await _service.MergeBranchAsync("feature/branch", new MergeOptions());

        // Assert
        Assert.AreEqual(MergeStatus.Success, result.Status);
        Assert.AreEqual(0, result.Conflicts.Count);
    }

    [TestMethod]
    public async Task MergeBranch_WithConflicts_ReturnsConflictStatus()
    {
        // Arrange
        var sourceVersion = new GraphVersion { VersionId = Guid.NewGuid() };
        var targetVersion = new GraphVersion { VersionId = Guid.NewGuid() };
        // Setup versions with conflicting changes...

        // Act
        var result = await _service.MergeBranchAsync("feature/branch",
            new MergeOptions { AutoResolveConflicts = false });

        // Assert
        Assert.AreEqual(MergeStatus.Conflict, result.Status);
        Assert.IsTrue(result.Conflicts.Count > 0);
    }

    [TestMethod]
    public async Task CompareBranches_ReturnsAheadBehindCounts()
    {
        // Arrange
        var comparison = new BranchComparison
        {
            FromBranch = "feature/api-v2",
            ToBranch = "main",
            AheadCount = 5,
            BehindCount = 3
        };

        // Act
        var result = await _service.CompareBranchesAsync("feature/api-v2", "main");

        // Assert
        Assert.AreEqual(5, result.AheadCount);
        Assert.AreEqual(3, result.BehindCount);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class BranchMergeIntegrationTests
{
    [TestMethod]
    public async Task FullBranchWorkflow_CreateMergeBranch_Success()
    {
        // Setup test graph
        var graph = await TestGraphBuilder.CreateTestGraphAsync();

        // 1. Create branch
        var branch = await graph.BranchService.CreateBranchAsync("feature/test");
        Assert.IsNotNull(branch);

        // 2. Make changes on branch
        await graph.SwitchBranchAsync("feature/test");
        var entity = new Entity { Label = "New Entity" };
        await graph.CreateEntityAsync(entity);

        // 3. Switch back to main
        await graph.SwitchBranchAsync("main");

        // 4. Merge branch
        var result = await graph.BranchService.MergeBranchAsync(
            "feature/test",
            new MergeOptions { Message = "Merge feature" });

        // Assert
        Assert.AreEqual(MergeStatus.Success, result.Status);
        Assert.IsNotNull(result.MergeVersionId);
    }

    [TestMethod]
    public async Task BranchMerge_WithConflictResolution_Success()
    {
        // Create conflicting branches
        var graph = await TestGraphBuilder.CreateTestGraphAsync();

        // Create feature branch and modify entity
        await graph.BranchService.CreateBranchAsync("feature/conflict");
        await graph.SwitchBranchAsync("feature/conflict");
        var entity = graph.Entities.First();
        entity.Description = "Feature version";
        await graph.UpdateEntityAsync(entity);

        // Back to main and modify same entity differently
        await graph.SwitchBranchAsync("main");
        entity = graph.Entities.First();
        entity.Description = "Main version";
        await graph.UpdateEntityAsync(entity);

        // Attempt merge
        var previewResult = await graph.BranchService.MergeBranchAsync(
            "feature/conflict",
            new MergeOptions { DryRun = true, AutoResolveConflicts = false });

        Assert.AreEqual(MergeStatus.Conflict, previewResult.Status);
        Assert.IsTrue(previewResult.Conflicts.Count > 0);

        // Resolve conflict
        var resolution = new MergeOptions
        {
            AutoResolveConflicts = true,
            ResolutionStrategy = ConflictResolutionStrategy.Theirs,
            Message = "Merged with conflict resolution"
        };

        var mergeResult = await graph.BranchService.MergeBranchAsync("feature/conflict", resolution);
        Assert.AreEqual(MergeStatus.Success, mergeResult.Status);
    }
}
```

---

## 9. Performance Targets

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Branch creation | <1s P95 | Direct version reference, minimal IO |
| Branch switching | <100ms P95 | Context switch in memory |
| Merge preview | <5s P95 | Delta computation with caching |
| Three-way merge | <10s P95 (100 changes) | Optimized delta comparison |
| Branch comparison | <500ms P95 | Version chain traversal |
| Conflict detection | <2s P95 | Parallel entity comparison |

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Accidental main branch deletion | Critical | Prevent deletion of default branches |
| Invalid merge resolution | High | Validate resolutions before apply |
| Race conditions in merge | High | Pessimistic locking during merge |
| Unauthorized branch access | Medium | License tier check, branch ownership |
| Large branch divergence | Medium | Merge preview before commit |

---

## 11. License Gating

| Tier | Branching Support |
| :--- | :--- |
| Core | ❌ Not available |
| WriterPro | ❌ Not available |
| Teams | ❌ Not available (merge only) |
| Enterprise | ✅ Full branch/merge support |

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Graph with main branch | Create new branch | Branch created with correct head version |
| 2 | Multiple branches exist | Switch branch | Active branch changes, graph state updates |
| 3 | Two diverged branches | Merge without conflicts | Merge succeeds, new version created |
| 4 | Two diverged branches | Merge with conflicts | Conflicts detected and reported |
| 5 | Conflicts reported | Provide resolutions | Conflicts resolved, merge completes |
| 6 | Two branches | Compare | Ahead/behind counts accurate |
| 7 | Feature branch | Delete | Branch removed (if not default) |
| 8 | Default branch | Attempt delete | Delete fails, error returned |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - Branch creation, switching, three-way merge, conflict resolution |

---
