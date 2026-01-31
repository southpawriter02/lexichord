# LCS-DES-v0.10.4-KG-b: Design Specification — Path Finder

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-104b` | Graph Visualization sub-part b |
| **Feature Name** | `Path Finder` | Find paths between entities |
| **Target Version** | `v0.10.4b` | Second sub-part of v0.10.4-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | CKVS knowledge graph module |
| **Swimlane** | `Graph Visualization` | Visualization vertical |
| **License Tier** | `WriterPro` | Available in WriterPro tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVisualization` | Graph visualization feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.4-KG](./LCS-SBD-v0.10.4-KG.md) | Graph Visualization & Search scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.4-KG S2.1](./LCS-SBD-v0.10.4-KG.md#21-sub-parts) | b = Path Finder |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.10.4-KG requires efficient path-finding algorithms to discover connections between entities. The Path Finder must:

1. Find the shortest path between two entities
2. Find all paths up to a maximum length
3. Check connectivity between entities
4. Support relationship type filtering
5. Support directional constraints (incoming/outgoing/both)
6. Execute path queries in <1 second for depth 5

### 2.2 The Proposed Solution

Implement a comprehensive Path Finder service with:

1. **IPathFinder interface:** Main contract for path-finding operations
2. **PathResult record:** Complete path with nodes and edges
3. **PathNode/PathEdge records:** Individual path components
4. **Graph traversal algorithms:** BFS for shortest path, DFS for all paths
5. **Constraint support:** Filter by relationship type, direction, max depth

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Graph data access (entities, relationships) |
| `ILicenseContext` | v0.9.2 | License tier validation for WriterPro+ |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Collections.Generic` | Built-in | Queue, Stack for traversal |

### 3.2 Licensing Behavior

- **Core Tier:** Feature not available
- **WriterPro Tier:** Basic path finding (shortest path, connectivity check)
- **Teams Tier:** All path finding enabled (all paths, complex constraints)
- **Enterprise Tier:** Teams + semantic path scoring (future enhancement)

---

## 4. Data Contract (The API)

### 4.1 IPathFinder Interface

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Finds paths between entities in the knowledge graph.
/// Supports shortest path, all paths, and connectivity queries.
/// </summary>
public interface IPathFinder
{
    /// <summary>
    /// Finds the shortest path between two entities.
    /// Returns null if no path exists within constraints.
    /// </summary>
    Task<PathResult?> FindShortestPathAsync(
        Guid sourceId,
        Guid targetId,
        PathOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Finds all paths between two entities up to max length.
    /// Returns list of all unique paths found.
    /// </summary>
    Task<IReadOnlyList<PathResult>> FindAllPathsAsync(
        Guid sourceId,
        Guid targetId,
        int maxLength,
        PathOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if two entities are connected within max depth.
    /// Efficient check that doesn't compute full path.
    /// </summary>
    Task<bool> AreConnectedAsync(
        Guid sourceId,
        Guid targetId,
        int maxDepth = 10,
        CancellationToken ct = default);
}
```

### 4.2 PathResult Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Represents a complete path between two entities.
/// Contains ordered list of nodes and edges forming the path.
/// </summary>
public record PathResult
{
    /// <summary>
    /// Ordered list of nodes in the path from source to target.
    /// </summary>
    public IReadOnlyList<PathNode> Nodes { get; init; } = [];

    /// <summary>
    /// Edges connecting the nodes in the path.
    /// </summary>
    public IReadOnlyList<PathEdge> Edges { get; init; } = [];

    /// <summary>
    /// Length of the path (number of edges/hops).
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// Total weight of the path (sum of edge weights).
    /// Lower weight = more direct/preferred path.
    /// </summary>
    public double TotalWeight { get; init; }

    /// <summary>
    /// Whether this is the shortest path found.
    /// </summary>
    public bool IsShortest { get; init; } = true;

    /// <summary>
    /// Score for this path (0-100).
    /// Used when multiple paths have same length.
    /// Higher score = more relevant path.
    /// </summary>
    public int? RelevanceScore { get; init; }
}
```

### 4.3 PathNode Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Represents a node in a path result.
/// Tracks position and metadata of each entity in the path.
/// </summary>
public record PathNode
{
    /// <summary>
    /// The entity ID.
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Entity name for display.
    /// </summary>
    public string EntityName { get; init; } = "";

    /// <summary>
    /// Entity type (Service, Endpoint, Database, etc.).
    /// </summary>
    public string EntityType { get; init; } = "";

    /// <summary>
    /// Position in the path (0-indexed).
    /// Source = 0, target = Length.
    /// </summary>
    public int Position { get; init; }
}
```

### 4.4 PathEdge Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Represents an edge in a path result.
/// Tracks relationship between consecutive nodes.
/// </summary>
public record PathEdge
{
    /// <summary>
    /// The relationship ID.
    /// </summary>
    public Guid RelationshipId { get; init; }

    /// <summary>
    /// Type of relationship (DEPENDS_ON, CALLS, etc.).
    /// </summary>
    public string RelationshipType { get; init; } = "";

    /// <summary>
    /// Whether the relationship is traversed forward.
    /// True = source->target, False = target->source.
    /// </summary>
    public bool IsForward { get; init; } = true;
}
```

### 4.5 PathOptions Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Options for path-finding operations.
/// Constrains path search by type, direction, and depth.
/// </summary>
public record PathOptions
{
    /// <summary>
    /// Maximum number of hops allowed in path.
    /// 0 = unlimited.
    /// </summary>
    public int MaxHops { get; init; } = 0;

    /// <summary>
    /// Relationship types to follow.
    /// If empty, follows all relationship types.
    /// </summary>
    public IReadOnlyList<string> RelationshipTypeFilter { get; init; } = [];

    /// <summary>
    /// Direction constraint for relationships.
    /// </summary>
    public PathDirection Direction { get; init; } = PathDirection.Any;

    /// <summary>
    /// Whether to allow cycles in paths.
    /// False = simple paths (no node visited twice).
    /// True = allow cycles (more paths found).
    /// </summary>
    public bool AllowCycles { get; init; } = false;

    /// <summary>
    /// Weight calculation method for edge prioritization.
    /// </summary>
    public PathWeightingStrategy Weighting { get; init; } = PathWeightingStrategy.Uniform;

    /// <summary>
    /// Entity types to include in path (intermediate nodes).
    /// If empty, includes all entity types.
    /// </summary>
    public IReadOnlyList<string> EntityTypeFilter { get; init; } = [];

    /// <summary>
    /// Maximum time in milliseconds for path finding.
    /// 0 = unlimited.
    /// </summary>
    public int TimeoutMs { get; init; } = 0;
}
```

### 4.6 Enums

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Direction constraint for relationship traversal.
/// </summary>
public enum PathDirection
{
    /// <summary>Follow relationships in any direction.</summary>
    Any = 1,

    /// <summary>Follow only outgoing relationships (source -> target).</summary>
    Outgoing = 2,

    /// <summary>Follow only incoming relationships (target <- source).</summary>
    Incoming = 3
}

/// <summary>
/// Strategy for computing edge weights in path finding.
/// </summary>
public enum PathWeightingStrategy
{
    /// <summary>All edges have equal weight (1.0).</summary>
    Uniform = 1,

    /// <summary>Weight based on relationship type strength.</summary>
    ByRelationshipType = 2,

    /// <summary>Weight based on inverse of node degree (prefer less-connected paths).</summary>
    ByNodeDegree = 3,

    /// <summary>Custom weight from relationship metadata.</summary>
    Custom = 4
}
```

---

## 5. Implementation Details

### 5.1 Shortest Path Algorithm (BFS)

```csharp
namespace Lexichord.Modules.CKVS.Services.Visualization;

internal class ShortestPathFinder
{
    public async Task<PathResult?> FindAsync(
        Guid sourceId,
        Guid targetId,
        PathOptions options,
        IGraphRepository repository)
    {
        if (sourceId == targetId)
            return CreatePathFromSingleNode(sourceId);

        var queue = new Queue<PathState>();
        var visited = new HashSet<Guid>();

        queue.Enqueue(new PathState { CurrentId = sourceId, Path = new[] { sourceId } });
        visited.Add(sourceId);

        while (queue.Count > 0)
        {
            var state = queue.Dequeue();

            // Check max hops
            if (options.MaxHops > 0 && state.Path.Length > options.MaxHops)
                continue;

            // Get neighbors
            var relationships = await repository.GetRelationshipsAsync(
                state.CurrentId,
                options.Direction,
                options.RelationshipTypeFilter);

            foreach (var relationship in relationships)
            {
                var nextId = relationship.SourceId == state.CurrentId
                    ? relationship.TargetId
                    : relationship.SourceId;

                // Check if already visited (unless cycles allowed)
                if (!options.AllowCycles && visited.Contains(nextId))
                    continue;

                var newPath = state.Path.Append(nextId).ToArray();

                // Found target
                if (nextId == targetId)
                {
                    return await BuildPathResult(newPath, options, repository);
                }

                // Continue search
                visited.Add(nextId);
                queue.Enqueue(new PathState { CurrentId = nextId, Path = newPath });
            }
        }

        return null; // No path found
    }

    private record PathState
    {
        public Guid CurrentId { get; init; }
        public Guid[] Path { get; init; } = [];
    }
}
```

### 5.2 All Paths Algorithm (DFS)

```csharp
internal class AllPathsFinder
{
    private readonly int _maxResults = 1000; // Prevent explosion

    public async Task<IReadOnlyList<PathResult>> FindAsync(
        Guid sourceId,
        Guid targetId,
        int maxLength,
        PathOptions options,
        IGraphRepository repository)
    {
        var results = new List<PathResult>();
        var visited = new HashSet<Guid>();

        await DfsAsync(
            sourceId,
            targetId,
            maxLength,
            new List<Guid> { sourceId },
            visited,
            options,
            repository,
            results);

        // Sort by length, then by weight
        return results
            .OrderBy(p => p.Length)
            .ThenBy(p => p.TotalWeight)
            .Take(_maxResults)
            .ToList();
    }

    private async Task DfsAsync(
        Guid currentId,
        Guid targetId,
        int maxLength,
        List<Guid> currentPath,
        HashSet<Guid> visited,
        PathOptions options,
        IGraphRepository repository,
        List<PathResult> results)
    {
        // Base case: max results reached
        if (results.Count >= _maxResults)
            return;

        // Base case: path too long
        if (currentPath.Count > maxLength)
            return;

        // Check if current is target
        if (currentId == targetId && currentPath.Count > 1)
        {
            var pathResult = await BuildPathResult(currentPath.ToArray(), options, repository);
            results.Add(pathResult);
            return;
        }

        // Get neighbors
        var relationships = await repository.GetRelationshipsAsync(
            currentId,
            options.Direction,
            options.RelationshipTypeFilter);

        foreach (var relationship in relationships)
        {
            var nextId = relationship.SourceId == currentId
                ? relationship.TargetId
                : relationship.SourceId;

            // Avoid cycles unless allowed
            if (!options.AllowCycles && visited.Contains(nextId))
                continue;

            // Mark visited
            bool wasVisited = visited.Contains(nextId);
            if (!wasVisited)
                visited.Add(nextId);

            currentPath.Add(nextId);

            await DfsAsync(
                nextId,
                targetId,
                maxLength,
                currentPath,
                visited,
                options,
                repository,
                results);

            currentPath.RemoveAt(currentPath.Count - 1);

            if (!wasVisited)
                visited.Remove(nextId);
        }
    }
}
```

### 5.3 Connectivity Check (Fast)

```csharp
internal class ConnectivityChecker
{
    public async Task<bool> AreConnectedAsync(
        Guid sourceId,
        Guid targetId,
        int maxDepth,
        IGraphRepository repository)
    {
        if (sourceId == targetId)
            return true;

        var queue = new Queue<(Guid Id, int Depth)>();
        var visited = new HashSet<Guid>();

        queue.Enqueue((sourceId, 0));
        visited.Add(sourceId);

        while (queue.Count > 0)
        {
            var (currentId, depth) = queue.Dequeue();

            if (depth >= maxDepth)
                continue;

            var relationships = await repository.GetRelationshipsAsync(currentId);

            foreach (var rel in relationships)
            {
                var nextId = rel.SourceId == currentId ? rel.TargetId : rel.SourceId;

                if (nextId == targetId)
                    return true;

                if (!visited.Contains(nextId))
                {
                    visited.Add(nextId);
                    queue.Enqueue((nextId, depth + 1));
                }
            }
        }

        return false;
    }
}
```

---

## 6. Testing Strategy

### 6.1 Unit Tests

```csharp
[TestClass]
public class PathFinderTests
{
    private IPathFinder _pathFinder;
    private Mock<IGraphRepository> _repositoryMock;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IGraphRepository>();
        _pathFinder = new PathFinder(_repositoryMock.Object);
    }

    [TestMethod]
    public async Task FindShortestPathAsync_DirectConnection_ReturnsTwoNodePath()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetRelationshipsAsync(sourceId, It.IsAny<PathDirection>(), It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(new[] { new Relationship { Id = relationshipId, SourceId = sourceId, TargetId = targetId } });

        var options = new PathOptions();
        var result = await _pathFinder.FindShortestPathAsync(sourceId, targetId, options);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Nodes.Count);
        Assert.AreEqual(1, result.Length);
    }

    [TestMethod]
    public async Task FindShortestPathAsync_NoPath_ReturnsNull()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetRelationshipsAsync(It.IsAny<Guid>(), It.IsAny<PathDirection>(), It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(new Relationship[0]);

        var options = new PathOptions();
        var result = await _pathFinder.FindShortestPathAsync(sourceId, targetId, options);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task FindShortestPathAsync_WithMaxHops_RespectsLimit()
    {
        // Create chain A -> B -> C -> D
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();

        SetupChainRelationships(_repositoryMock, a, b, c, d);

        var options = new PathOptions { MaxHops = 1 };
        var result = await _pathFinder.FindShortestPathAsync(a, d, options);

        Assert.IsNull(result); // Cannot reach D with only 1 hop
    }

    [TestMethod]
    public async Task FindAllPathsAsync_MultipleValidPaths_ReturnsAllPaths()
    {
        // Create two paths: A->B->D and A->C->D
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();

        SetupDiamondGraph(_repositoryMock, a, b, c, d);

        var options = new PathOptions();
        var results = await _pathFinder.FindAllPathsAsync(a, d, 5, options);

        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    public async Task AreConnectedAsync_ConnectedWithinDepth_ReturnsTrue()
    {
        var source = Guid.NewGuid();
        var target = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetRelationshipsAsync(source))
            .ReturnsAsync(new[] { new Relationship { TargetId = target } });

        var result = await _pathFinder.AreConnectedAsync(source, target, 2);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task AreConnectedAsync_SameEntity_ReturnsTrue()
    {
        var entityId = Guid.NewGuid();

        var result = await _pathFinder.AreConnectedAsync(entityId, entityId, 5);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task FindShortestPathAsync_WithRelationshipTypeFilter_IgnoresOtherTypes()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetRelationshipsAsync(sourceId, It.IsAny<PathDirection>(), new[] { "CALLS" }))
            .ReturnsAsync(new Relationship[0]);

        var options = new PathOptions { RelationshipTypeFilter = new[] { "CALLS" } };
        var result = await _pathFinder.FindShortestPathAsync(sourceId, targetId, options);

        Assert.IsNull(result);
    }
}
```

### 6.2 Performance Tests

```csharp
[TestClass]
public class PathFinderPerformanceTests
{
    [TestMethod]
    public async Task FindShortestPathAsync_LargeGraph_UnderTimeTarget()
    {
        // Create large connected graph with 1000 nodes
        var graph = GenerateLargeConnectedGraph(1000, 3000);

        var options = new PathOptions { MaxHops = 10 };

        var sw = Stopwatch.StartNew();
        var result = await _pathFinder.FindShortestPathAsync(
            graph.Nodes[0].Id,
            graph.Nodes[999].Id,
            options);
        sw.Stop();

        Assert.IsNotNull(result);
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"Path finding took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public async Task AreConnectedAsync_IsFasterThanFindShortestPath()
    {
        var graph = GenerateLargeConnectedGraph(500, 1500);
        var options = new PathOptions();

        var sw1 = Stopwatch.StartNew();
        await _pathFinder.AreConnectedAsync(graph.Nodes[0].Id, graph.Nodes[499].Id, 10);
        sw1.Stop();

        var sw2 = Stopwatch.StartNew();
        await _pathFinder.FindShortestPathAsync(graph.Nodes[0].Id, graph.Nodes[499].Id, options);
        sw2.Stop();

        Assert.IsTrue(sw1.ElapsedMilliseconds < sw2.ElapsedMilliseconds);
    }
}
```

---

## 7. Error Handling

### 7.1 Invalid Entity IDs

**Scenario:** Path finding requested for non-existent entities.

**Handling:**
- Repository returns empty relationships
- Path finder returns null (no path found)
- No exception thrown; graceful degradation

### 7.2 Infinite Loops (Cycles)

**Scenario:** Graph contains cycles and allow cycles is true.

**Handling:**
- DFS tracks visited nodes per branch
- Prevents infinite recursion
- Sets _maxResults limit to prevent explosion

### 7.3 Timeout

**Scenario:** Path finding takes too long.

**Handling:**
- PathOptions.TimeoutMs enforced by task cancellation
- Algorithm exits gracefully with partial results if found

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(options.TimeoutMs));
try
{
    var result = await FindAsync(..., cts.Token);
}
catch (OperationCanceledException)
{
    _logger.LogWarning("Path finding timed out after {TimeoutMs}ms", options.TimeoutMs);
    return null; // Or partial results
}
```

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Shortest path (depth 5) | <1s | BFS, early termination |
| All paths (depth 3) | <2s | DFS with result limit |
| Connectivity check | <500ms | BFS, early termination |
| Path result building | <100ms | Entity lookup and metadata |

---

## 9. Security & Validation

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Excessive recursion | Medium | DFS has max result limit, depth check |
| Memory exhaustion | Medium | PathResult limit, AllowCycles default false |
| Invalid direction | Low | Enum type safety |
| Null reference | Low | Record properties non-null or nullable |

---

## 10. License Gating

```csharp
public class PathFinderLicenseCheck : IPathFinder
{
    private readonly ILicenseContext _licenseContext;
    private readonly IPathFinder _inner;

    public async Task<PathResult?> FindShortestPathAsync(Guid sourceId, Guid targetId, PathOptions options, CancellationToken ct)
    {
        if (!_licenseContext.HasFeatureEnabled(FeatureFlags.CKVS.GraphVisualization))
            throw new LicenseException("Path finding requires WriterPro tier");

        return await _inner.FindShortestPathAsync(sourceId, targetId, options, ct);
    }

    // Similar for FindAllPathsAsync and AreConnectedAsync
}
```

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Two directly connected entities | FindShortestPathAsync | Returns path with 2 nodes, length 1 |
| 2 | No path between entities | FindShortestPathAsync | Returns null |
| 3 | Multiple paths exist | FindAllPathsAsync | Returns all paths sorted by length |
| 4 | MaxHops constraint | FindShortestPathAsync | Respects max hops limit |
| 5 | RelationshipTypeFilter | FindShortestPathAsync | Only follows specified types |
| 6 | Same source and target | AreConnectedAsync | Returns true |
| 7 | Connected within maxDepth | AreConnectedAsync | Returns true in <500ms |
| 8 | Cycles in graph | FindAllPathsAsync with AllowCycles=false | Finds simple paths only |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - IPathFinder, BFS/DFS algorithms, path results |
