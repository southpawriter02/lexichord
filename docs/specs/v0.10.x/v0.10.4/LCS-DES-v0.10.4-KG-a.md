# LCS-DES-v0.10.4-KG-a: Design Specification — Graph Renderer

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-104a` | Graph Visualization sub-part a |
| **Feature Name** | `Graph Renderer` | Force-directed visualization engine |
| **Target Version** | `v0.10.4a` | First sub-part of v0.10.4-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | CKVS knowledge graph module |
| **Swimlane** | `Graph Visualization` | Visualization vertical |
| **License Tier** | `WriterPro` | Available in WriterPro tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVisualization` | Graph visualization feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.4-KG](./LCS-SBD-v0.10.4-KG.md) | Graph Visualization & Search scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.4-KG S2.1](./LCS-SBD-v0.10.4-KG.md#21-sub-parts) | a = Graph Renderer |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.10.4-KG requires interactive visualization of knowledge graphs with force-directed layout algorithms. The Graph Renderer must:

1. Convert graph data (entities, relationships) into visual coordinates
2. Support multiple layout algorithms (ForceDirected, Hierarchical, Circular, Radial, Grid)
3. Render entity neighborhoods with configurable depth
4. Return visualization data suitable for D3.js/Cytoscape rendering
5. Support visual customization (colors, sizes, labels, metadata)

### 2.2 The Proposed Solution

Implement a comprehensive Graph Renderer service with:

1. **IGraphRenderer interface:** Main contract for rendering operations
2. **GraphVisualization record:** Complete visualization result with nodes and edges
3. **VisualNode/VisualEdge records:** Individual node and edge visual properties
4. **Layout algorithm implementations:** ForceDirected, Hierarchical, Circular, Radial, Grid
5. **Export support:** Integration point for exporting visualizations to image formats

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Graph data access (entities, relationships) |
| `IEntityBrowser` | v0.4.7-KG | Entity details and metadata for labeling |
| `ILicenseContext` | v0.9.2 | License tier validation for WriterPro+ |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Collections.Generic` | Built-in | Collections for node/edge lists |

#### 3.1.3 External Rendering Libraries

- **D3.js** (frontend) or **Cytoscape.js** (frontend) for actual SVG rendering
- Graph Renderer provides data contract only; browser renders visualization

### 3.2 Licensing Behavior

- **Core Tier:** Feature not available
- **WriterPro Tier:** Basic rendering with force-directed and hierarchical layouts
- **Teams Tier:** All layouts enabled, full neighborhood rendering
- **Enterprise Tier:** Teams + export capabilities (delegated to v0.10.4e)

---

## 4. Data Contract (The API)

### 4.1 IGraphRenderer Interface

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Renders knowledge graph visualizations with various layout algorithms.
/// Converts graph structure into visual coordinates and properties.
/// </summary>
public interface IGraphRenderer
{
    /// <summary>
    /// Generates a visualization for a subgraph defined by the request.
    /// Applies the requested layout algorithm and returns visual coordinates.
    /// </summary>
    Task<GraphVisualization> RenderAsync(
        GraphRenderRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the neighborhood of an entity at a specified depth.
    /// Returns entity and its related entities (1 to N hops away).
    /// </summary>
    Task<GraphVisualization> RenderNeighborhoodAsync(
        Guid entityId,
        int depth,
        NeighborhoodOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a visualization to image format.
    /// Delegates to export service but exposed here for convenience.
    /// </summary>
    Task<byte[]> ExportAsync(
        GraphVisualization visualization,
        ExportFormat format,
        ExportOptions options,
        CancellationToken ct = default);
}
```

### 4.2 GraphRenderRequest Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Request parameters for rendering a subgraph visualization.
/// </summary>
public record GraphRenderRequest
{
    /// <summary>
    /// List of entity IDs to include in visualization.
    /// If empty, renders all entities.
    /// </summary>
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];

    /// <summary>
    /// List of relationship IDs to include.
    /// If empty, includes all relationships between EntityIds.
    /// </summary>
    public IReadOnlyList<Guid> RelationshipIds { get; init; } = [];

    /// <summary>
    /// Layout algorithm to use for positioning.
    /// </summary>
    public LayoutAlgorithm Layout { get; init; } = LayoutAlgorithm.ForceDirected;

    /// <summary>
    /// Maximum width of the visualization canvas in pixels.
    /// </summary>
    public int MaxWidth { get; init; } = 1200;

    /// <summary>
    /// Maximum height of the visualization canvas in pixels.
    /// </summary>
    public int MaxHeight { get; init; } = 800;

    /// <summary>
    /// Minimum width of the visualization canvas in pixels.
    /// </summary>
    public int MinWidth { get; init; } = 400;

    /// <summary>
    /// Minimum height of the visualization canvas in pixels.
    /// </summary>
    public int MinHeight { get; init; } = 300;

    /// <summary>
    /// Filter for relationship types to include.
    /// If empty, includes all relationship types.
    /// </summary>
    public IReadOnlyList<string> RelationshipTypeFilter { get; init; } = [];

    /// <summary>
    /// Color scheme customization.
    /// </summary>
    public ColorScheme? ColorScheme { get; init; }

    /// <summary>
    /// Whether to include relationship labels.
    /// </summary>
    public bool ShowLabels { get; init; } = true;

    /// <summary>
    /// Padding around the visualization in pixels.
    /// </summary>
    public int Padding { get; init; } = 40;
}
```

### 4.3 GraphVisualization Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Complete visualization of a graph with positioned nodes and edges.
/// </summary>
public record GraphVisualization
{
    /// <summary>
    /// Nodes in the visualization (entities with positions).
    /// </summary>
    public IReadOnlyList<VisualNode> Nodes { get; init; } = [];

    /// <summary>
    /// Edges in the visualization (relationships).
    /// </summary>
    public IReadOnlyList<VisualEdge> Edges { get; init; } = [];

    /// <summary>
    /// Bounding box of the entire visualization.
    /// </summary>
    public VisualizationBounds Bounds { get; init; } = new();

    /// <summary>
    /// Layout algorithm used to generate positions.
    /// </summary>
    public LayoutAlgorithm Layout { get; init; } = LayoutAlgorithm.ForceDirected;

    /// <summary>
    /// Timestamp when visualization was generated.
    /// </summary>
    public DateTimeOffset RenderedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Color scheme used in visualization.
    /// </summary>
    public ColorScheme ColorScheme { get; init; } = ColorScheme.Default;
}
```

### 4.4 VisualNode Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Represents a node (entity) in a visualization with position and styling.
/// </summary>
public record VisualNode
{
    /// <summary>
    /// The entity ID being visualized.
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Display label for the node (typically entity name or ID).
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// Entity type (Service, Endpoint, Database, etc.).
    /// Used for styling and filtering.
    /// </summary>
    public string EntityType { get; init; } = "";

    /// <summary>
    /// X-coordinate in visualization space.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y-coordinate in visualization space.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Visual size of the node relative to default.
    /// 1.0 = standard size, 2.0 = double size, etc.
    /// Typically based on degree (number of connections).
    /// </summary>
    public double Size { get; init; } = 1.0;

    /// <summary>
    /// RGB hex color for the node (#RRGGBB).
    /// Typically mapped by entity type.
    /// </summary>
    public string Color { get; init; } = "#3b82f6";

    /// <summary>
    /// Additional metadata for rendering (degree, centrality, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
```

### 4.5 VisualEdge Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Represents an edge (relationship) in a visualization.
/// </summary>
public record VisualEdge
{
    /// <summary>
    /// The relationship ID being visualized.
    /// </summary>
    public Guid RelationshipId { get; init; }

    /// <summary>
    /// Entity ID of the source node.
    /// </summary>
    public Guid SourceId { get; init; }

    /// <summary>
    /// Entity ID of the target node.
    /// </summary>
    public Guid TargetId { get; init; }

    /// <summary>
    /// Type of relationship (DEPENDS_ON, CALLS, DEFINES, etc.).
    /// </summary>
    public string RelationshipType { get; init; } = "";

    /// <summary>
    /// Optional label for the edge (shown on line).
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Weight of the edge (affects layout attraction/repulsion).
    /// 1.0 = standard weight.
    /// </summary>
    public double Weight { get; init; } = 1.0;

    /// <summary>
    /// RGB hex color for the edge.
    /// Typically mapped by relationship type.
    /// </summary>
    public string Color { get; init; } = "#94a3b8";

    /// <summary>
    /// Whether the edge is directed (arrow from source to target).
    /// </summary>
    public bool IsDirected { get; init; } = true;

    /// <summary>
    /// Line style (solid, dashed, dotted).
    /// </summary>
    public LineStyle LineStyle { get; init; } = LineStyle.Solid;
}
```

### 4.6 VisualizationBounds Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Bounding box for visualization coordinates.
/// </summary>
public record VisualizationBounds
{
    public double MinX { get; init; } = 0;
    public double MinY { get; init; } = 0;
    public double MaxX { get; init; } = 1200;
    public double MaxY { get; init; } = 800;

    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public double CenterX => MinX + Width / 2;
    public double CenterY => MinY + Height / 2;
}
```

### 4.7 Enums

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Available layout algorithms for graph visualization.
/// </summary>
public enum LayoutAlgorithm
{
    /// <summary>
    /// Force-directed (spring) layout.
    /// Best for general exploration and relationship discovery.
    /// Nodes repel each other; edges act as springs.
    /// </summary>
    ForceDirected = 1,

    /// <summary>
    /// Hierarchical (layered) layout.
    /// Best for dependency trees and acyclic graphs.
    /// Places nodes in hierarchical levels.
    /// </summary>
    Hierarchical = 2,

    /// <summary>
    /// Circular layout.
    /// Best for ring relationships and symmetric graphs.
    /// Places nodes on concentric circles.
    /// </summary>
    Circular = 3,

    /// <summary>
    /// Radial layout.
    /// Best for entity neighborhoods with central focus.
    /// Places root at center, others in circles around it.
    /// </summary>
    Radial = 4,

    /// <summary>
    /// Grid layout.
    /// Best for large flat graphs.
    /// Places nodes in regular grid pattern.
    /// </summary>
    Grid = 5
}

/// <summary>
/// Export formats supported by graph renderer.
/// </summary>
public enum ExportFormat
{
    /// <summary>Scalable Vector Graphics (lossless).</summary>
    SVG = 1,

    /// <summary>Portable Network Graphics (raster, high quality).</summary>
    PNG = 2,

    /// <summary>Portable Document Format (document quality).</summary>
    PDF = 3,

    /// <summary>JavaScript Object Notation (data export).</summary>
    JSON = 4
}

/// <summary>
/// Line styles for edges.
/// </summary>
public enum LineStyle
{
    /// <summary>Solid line.</summary>
    Solid = 1,

    /// <summary>Dashed line.</summary>
    Dashed = 2,

    /// <summary>Dotted line.</summary>
    Dotted = 3
}
```

### 4.8 Supporting Records

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Options for rendering entity neighborhoods.
/// </summary>
public record NeighborhoodOptions
{
    /// <summary>
    /// Maximum depth to traverse from root entity.
    /// 1 = direct neighbors only, 2 = neighbors and their neighbors, etc.
    /// </summary>
    public int MaxDepth { get; init; } = 2;

    /// <summary>
    /// Filter for relationship types to follow.
    /// If empty, follows all relationship types.
    /// </summary>
    public IReadOnlyList<string> FollowRelationshipTypes { get; init; } = [];

    /// <summary>
    /// Whether to include incoming relationships (edges pointing to root).
    /// </summary>
    public bool IncludeIncoming { get; init; } = true;

    /// <summary>
    /// Whether to include outgoing relationships (edges from root).
    /// </summary>
    public bool IncludeOutgoing { get; init; } = true;

    /// <summary>
    /// Maximum number of neighbors to include at each level.
    /// Prevents explosion of nodes in highly connected neighborhoods.
    /// 0 = unlimited.
    /// </summary>
    public int MaxNeighborsPerLevel { get; init; } = 0;

    /// <summary>
    /// Layout algorithm for neighborhood visualization.
    /// </summary>
    public LayoutAlgorithm Layout { get; init; } = LayoutAlgorithm.Radial;
}

/// <summary>
/// Color scheme for visualization.
/// </summary>
public record ColorScheme
{
    /// <summary>
    /// Mapping from entity type to color code.
    /// </summary>
    public IReadOnlyDictionary<string, string> EntityTypeColors { get; init; } =
        new Dictionary<string, string>
        {
            { "Service", "#3b82f6" },
            { "Endpoint", "#10b981" },
            { "Database", "#f59e0b" },
            { "Message", "#8b5cf6" }
        };

    /// <summary>
    /// Mapping from relationship type to color code.
    /// </summary>
    public IReadOnlyDictionary<string, string> RelationshipTypeColors { get; init; } =
        new Dictionary<string, string>
        {
            { "DEPENDS_ON", "#94a3b8" },
            { "CALLS", "#6366f1" },
            { "DEFINES", "#ec4899" }
        };

    /// <summary>
    /// Background color for canvas.
    /// </summary>
    public string BackgroundColor { get; init; } = "#ffffff";

    /// <summary>
    /// Text color for labels.
    /// </summary>
    public string TextColor { get; init; } = "#1f2937";

    /// <summary>
    /// Predefined color scheme (light, dark, colorblind-safe).
    /// </summary>
    public static ColorScheme Default { get; } = new();

    public static ColorScheme Dark { get; } = new()
    {
        BackgroundColor = "#1f2937",
        TextColor = "#f3f4f6"
    };

    public static ColorScheme ColorblindSafe { get; } = new()
    {
        EntityTypeColors = new Dictionary<string, string>
        {
            { "Service", "#0173b2" },
            { "Endpoint", "#de8f05" },
            { "Database", "#ca9161" },
            { "Message", "#56b4e9" }
        }
    };
}

/// <summary>
/// Options for exporting visualization to file format.
/// </summary>
public record ExportOptions
{
    /// <summary>Width in pixels for raster exports (PNG, PDF).</summary>
    public int? Width { get; init; } = 1200;

    /// <summary>Height in pixels for raster exports (PNG, PDF).</summary>
    public int? Height { get; init; } = 800;

    /// <summary>DPI for PDF export.</summary>
    public int? DPI { get; init; } = 300;

    /// <summary>Compression level for PNG (0-9).</summary>
    public int? PngCompressionLevel { get; init; } = 6;

    /// <summary>Whether to include node labels in export.</summary>
    public bool IncludeLabels { get; init; } = true;

    /// <summary>Whether to include edge labels in export.</summary>
    public bool IncludeEdgeLabels { get; init; } = false;

    /// <summary>Background color for export.</summary>
    public string? BackgroundColor { get; init; }
}
```

---

## 5. Implementation Details

### 5.1 Force-Directed Layout Algorithm

The force-directed algorithm is the most complex and widely used:

```csharp
namespace Lexichord.Modules.CKVS.Services.Visualization;

internal class ForceDirectedLayoutEngine
{
    private const double StiffnessCoeff = 100.0;
    private const double RepulsionCoeff = 100.0;
    private const double DampingFactor = 0.95;
    private const int MaxIterations = 100;
    private const double ConvergenceThreshold = 0.01;

    public void ComputeLayout(
        IReadOnlyList<VisualNode> nodes,
        IReadOnlyList<VisualEdge> edges,
        VisualizationBounds bounds)
    {
        // Initialize positions randomly within bounds
        InitializePositions(nodes, bounds);

        // Iterate force computations
        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            double maxForce = 0;

            // Compute repulsive forces between all nodes
            foreach (var node1 in nodes)
            {
                foreach (var node2 in nodes)
                {
                    if (node1.EntityId == node2.EntityId) continue;

                    var force = ComputeRepulsion(node1, node2);
                    maxForce = Math.Max(maxForce, force.Length);
                }
            }

            // Compute attractive forces along edges
            foreach (var edge in edges)
            {
                var sourceNode = nodes.First(n => n.EntityId == edge.SourceId);
                var targetNode = nodes.First(n => n.EntityId == edge.TargetId);

                ComputeAttraction(sourceNode, targetNode, edge.Weight);
            }

            // Apply damping and check convergence
            if (maxForce < ConvergenceThreshold)
                break;
        }

        // Normalize positions to fit bounds
        NormalizePositions(nodes, bounds);
    }
}
```

### 5.2 Node Sizing Strategy

Nodes are typically sized by degree (number of connections):

```csharp
internal class NodeSizingStrategy
{
    public static double ComputeSize(Guid nodeId, IReadOnlyList<VisualEdge> edges)
    {
        int degree = edges.Count(e => e.SourceId == nodeId || e.TargetId == nodeId);

        // Size ranges from 0.5 to 3.0 based on degree
        double minSize = 0.5;
        double maxSize = 3.0;
        double maxDegree = 20.0;

        double normalizedDegree = Math.Min(degree / maxDegree, 1.0);
        return minSize + (maxSize - minSize) * normalizedDegree;
    }
}
```

---

## 6. Testing Strategy

### 6.1 Unit Tests

```csharp
[TestClass]
public class GraphRendererTests
{
    private IGraphRenderer _renderer;
    private Mock<IGraphRepository> _repositoryMock;
    private Mock<IEntityBrowser> _browserMock;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IGraphRepository>();
        _browserMock = new Mock<IEntityBrowser>();
        _renderer = new GraphRenderer(_repositoryMock.Object, _browserMock.Object);
    }

    [TestMethod]
    public async Task RenderAsync_EmptyGraph_ReturnsEmptyVisualization()
    {
        _repositoryMock.Setup(r => r.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var request = new GraphRenderRequest { Layout = LayoutAlgorithm.ForceDirected };
        var result = await _renderer.RenderAsync(request);

        Assert.AreEqual(0, result.Nodes.Count);
        Assert.AreEqual(0, result.Edges.Count);
    }

    [TestMethod]
    public async Task RenderAsync_SingleEntity_ReturnsSingleNode()
    {
        var entityId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Entity { Id = entityId, Name = "Service1" } });

        var request = new GraphRenderRequest { EntityIds = [entityId] };
        var result = await _renderer.RenderAsync(request);

        Assert.AreEqual(1, result.Nodes.Count);
        Assert.AreEqual("Service1", result.Nodes[0].Label);
    }

    [TestMethod]
    public async Task RenderAsync_WithLayoutAlgorithm_AppliesCorrectLayout()
    {
        var request = new GraphRenderRequest { Layout = LayoutAlgorithm.Hierarchical };
        var result = await _renderer.RenderAsync(request);

        Assert.AreEqual(LayoutAlgorithm.Hierarchical, result.Layout);
    }

    [TestMethod]
    public async Task RenderNeighborhoodAsync_WithDepth2_IncludesAllHops()
    {
        // Create a chain: A -> B -> C
        var entityA = Guid.NewGuid();
        var entityB = Guid.NewGuid();
        var entityC = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetEntityNeighborhoodAsync(entityA, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { entityA, entityB, entityC });

        var options = new NeighborhoodOptions { MaxDepth = 2 };
        var result = await _renderer.RenderNeighborhoodAsync(entityA, 2, options);

        Assert.AreEqual(3, result.Nodes.Count);
    }

    [TestMethod]
    public async Task ExportAsync_ToSVG_DelegatesToExportService()
    {
        var visualization = new GraphVisualization();
        var exportOptions = new ExportOptions { Width = 800, Height = 600 };

        var result = await _renderer.ExportAsync(visualization, ExportFormat.SVG, exportOptions);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }
}
```

### 6.2 Integration Tests

```csharp
[TestClass]
public class GraphRendererIntegrationTests
{
    [TestMethod]
    public async Task RenderAsync_CompleteGraph_PositionsAllNodes()
    {
        // Create test graph with 5 entities and relationships
        var entities = CreateTestEntities(5);
        var relationships = CreateTestRelationships(entities);

        var request = new GraphRenderRequest
        {
            EntityIds = entities.Select(e => e.Id).ToList(),
            Layout = LayoutAlgorithm.ForceDirected
        };

        var result = await _renderer.RenderAsync(request);

        // All nodes should have positions
        foreach (var node in result.Nodes)
        {
            Assert.IsTrue(node.X >= result.Bounds.MinX && node.X <= result.Bounds.MaxX);
            Assert.IsTrue(node.Y >= result.Bounds.MinY && node.Y <= result.Bounds.MaxY);
        }
    }

    [TestMethod]
    public async Task RenderAsync_VerifyConnectedNodes_NodesConnected()
    {
        // Verify edges connect correct nodes
        var result = await _renderer.RenderAsync(new GraphRenderRequest());

        foreach (var edge in result.Edges)
        {
            var sourceExists = result.Nodes.Any(n => n.EntityId == edge.SourceId);
            var targetExists = result.Nodes.Any(n => n.EntityId == edge.TargetId);

            Assert.IsTrue(sourceExists, "Source node missing");
            Assert.IsTrue(targetExists, "Target node missing");
        }
    }

    [TestMethod]
    public async Task LayoutAlgorithms_Performance_ForceDirectedUnder500ms()
    {
        var request = new GraphRenderRequest
        {
            Layout = LayoutAlgorithm.ForceDirected,
            EntityIds = CreateTestEntities(50).Select(e => e.Id).ToList()
        };

        var sw = Stopwatch.StartNew();
        var result = await _renderer.RenderAsync(request);
        sw.Stop();

        Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"Layout took {sw.ElapsedMilliseconds}ms");
    }
}
```

---

## 7. Error Handling

### 7.1 Invalid Graph Data

**Scenario:** Graph contains cycles or disconnected components.

**Handling:**
- Force-directed layout handles cycles naturally
- Disconnected components positioned separately
- No error thrown; visualization shows all components

**Code:**
```csharp
try
{
    var result = await _renderer.RenderAsync(request);
    // Force-directed handles all topologies
}
catch (InvalidGraphException ex)
{
    _logger.LogError("Graph validation failed: {Message}", ex.Message);
    throw new VisualizationException("Cannot render invalid graph", ex);
}
```

### 7.2 Out of Bounds Positions

**Scenario:** Layout algorithm places nodes outside canvas bounds.

**Handling:**
- All positions normalized to fit canvas after layout
- Bounds expanded if needed to fit all nodes
- Padding applied around visualization

### 7.3 Missing Entity Metadata

**Scenario:** Entity exists but has no name or type.

**Handling:**
- Use entity ID as fallback label
- Use default color if type unknown
- Log warning for missing metadata

```csharp
var label = entity.Name ?? entity.Id.ToString();
var color = colorScheme.EntityTypeColors.TryGetValue(entity.Type, out var c)
    ? c
    : "#3b82f6";
```

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Force-directed layout (100 nodes) | <500ms | Iterative convergence, damping |
| Hierarchical layout (100 nodes) | <200ms | Single-pass ranking |
| Circular layout (100 nodes) | <100ms | Angle calculation only |
| Node sizing | <10ms | Degree computation |
| Position normalization | <50ms | Min/max computation |

---

## 9. Security & Validation

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Extremely large graphs (10K+ nodes) | Medium | Pagination, clustering, progressive loading |
| Malformed entity data | Low | Type safety via records, validation at boundary |
| Invalid color codes | Low | Validation in ColorScheme, fallback colors |
| Memory exhaustion (export to large PNG) | Medium | Export options validation, size limits |

---

## 10. License Gating

```csharp
public class GraphRendererLicenseCheck : IGraphRenderer
{
    private readonly ILicenseContext _licenseContext;
    private readonly IGraphRenderer _inner;

    public async Task<GraphVisualization> RenderAsync(GraphRenderRequest request, CancellationToken ct)
    {
        if (!_licenseContext.HasFeatureEnabled(FeatureFlags.CKVS.GraphVisualization))
            throw new LicenseException("Graph visualization requires WriterPro tier");

        return await _inner.RenderAsync(request, ct);
    }
}
```

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Small graph (10 nodes, 15 edges) | RenderAsync called | All nodes positioned within bounds |
| 2 | Entity without name | RenderAsync called | Node label is entity ID |
| 3 | Neighborhood render request | RenderNeighborhoodAsync called | Correct depth traversal |
| 4 | Force-directed layout | RenderAsync called | Layout converges in <500ms |
| 5 | Hierarchical layout | RenderAsync called | Layout applied in <200ms |
| 6 | ColorScheme provided | RenderAsync called | Entity/relationship colors applied |
| 7 | Export request | ExportAsync called | Bytes returned (delegated to export service) |
| 8 | Cycle in graph | RenderAsync called | Force-directed handles without error |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - IGraphRenderer, layout algorithms, visualization records |
