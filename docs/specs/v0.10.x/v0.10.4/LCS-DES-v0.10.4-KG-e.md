# LCS-DES-v0.10.4-KG-e: Design Specification — Graph Export

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-104e` | Graph Visualization sub-part e |
| **Feature Name** | `Graph Export` | Export visualizations to multiple formats |
| **Target Version** | `v0.10.4e` | Fifth sub-part of v0.10.4-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | CKVS knowledge graph module |
| **Swimlane** | `Graph Visualization` | Visualization vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVisualization` | Graph visualization feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.4-KG](./LCS-SBD-v0.10.4-KG.md) | Graph Visualization & Search scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.4-KG S2.1](./LCS-SBD-v0.10.4-KG.md#21-sub-parts) | e = Graph Export |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.10.4-KG requires the ability to export graph visualizations to multiple formats for documentation and presentation use. The Graph Export must:

1. Export visualizations to SVG, PNG, PDF, and JSON formats
2. Preserve layout, colors, node sizes, and edge styling
3. Include metadata (title, date, bounds information)
4. Support customizable dimensions and quality settings
5. Complete exports in <5 seconds for typical graphs (100 nodes)
6. Maintain interactivity in SVG exports

### 2.2 The Proposed Solution

Implement a comprehensive Graph Export service with:

1. **IGraphExporter interface:** Main contract for export operations
2. **ExportFormat enum:** Supported output formats (SVG, PNG, PDF, JSON)
3. **ExportOptions record:** Configuration for export behavior and quality
4. **Format-specific exporters:** SVG builder, PNG rasterizer, PDF generator
5. **Metadata preservation:** Title, description, timestamps, bounds

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRenderer` | v0.10.4a | Visualization data from Graph Renderer |
| `IGraphRepository` | v0.4.5e | Entity and relationship metadata |
| `ILicenseContext` | v0.9.2 | License tier validation for Teams+ |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Drawing.Common` | Latest | PNG/PDF image generation |
| `PdfSharp` | Latest | PDF document creation |
| `SkiaSharp` | Latest | High-quality image rendering |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Not available
- **Teams Tier:** Export to SVG and PNG formats
- **Enterprise Tier:** All formats (SVG, PNG, PDF, JSON) with advanced options

---

## 4. Data Contract (The API)

### 4.1 IGraphExporter Interface

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Exports graph visualizations to various file formats.
/// Supports SVG, PNG, PDF, and JSON formats with customizable options.
/// </summary>
public interface IGraphExporter
{
    /// <summary>
    /// Exports a visualization to the specified format.
    /// </summary>
    Task<byte[]> ExportAsync(
        GraphVisualization visualization,
        ExportFormat format,
        ExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Exports visualization and saves directly to a file.
    /// Returns the file path where the export was saved.
    /// </summary>
    Task<string> ExportToFileAsync(
        GraphVisualization visualization,
        ExportFormat format,
        string outputPath,
        ExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the recommended file extension for a given format.
    /// </summary>
    string GetFileExtension(ExportFormat format);

    /// <summary>
    /// Gets MIME type for the specified format.
    /// </summary>
    string GetMimeType(ExportFormat format);
}
```

### 4.2 ExportFormat Enum

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Supported export formats for graph visualizations.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Scalable Vector Graphics format.
    /// Preserves full interactivity and zoom capability.
    /// Smallest file size, best for web and documents.
    /// </summary>
    SVG = 1,

    /// <summary>
    /// Portable Network Graphics raster format.
    /// Fixed resolution, smaller than PDF, suitable for presentations.
    /// </summary>
    PNG = 2,

    /// <summary>
    /// Portable Document Format.
    /// Prints reliably, embeds fonts and colors.
    /// Largest file size but most compatible.
    /// </summary>
    PDF = 3,

    /// <summary>
    /// JavaScript Object Notation format.
    /// Contains graph structure and metadata, loadable by web clients.
    /// Most compact representation of graph data.
    /// </summary>
    JSON = 4
}
```

### 4.3 ExportOptions Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Configuration options for graph export operations.
/// Allows customization of dimensions, quality, metadata, and content.
/// </summary>
public record ExportOptions
{
    /// <summary>
    /// Title to include in the exported visualization.
    /// Used in document metadata and SVG title elements.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Description or subtitle for the visualization.
    /// Included in PDF/SVG metadata.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Export width in pixels (for raster formats) or points (for PDF).
    /// Default: 1920 for PNG, 800pt for PDF.
    /// </summary>
    public int Width { get; init; } = 1920;

    /// <summary>
    /// Export height in pixels (for raster formats) or points (for PDF).
    /// Default: 1080 for PNG, 1200pt for PDF.
    /// </summary>
    public int Height { get; init; } = 1080;

    /// <summary>
    /// DPI (dots per inch) for raster export.
    /// Standard values: 72 (screen), 150 (web), 300 (print).
    /// Default: 150 for PNG.
    /// </summary>
    public int DPI { get; init; } = 150;

    /// <summary>
    /// Background color as hex code (#RRGGBB).
    /// Default: white (#FFFFFF).
    /// </summary>
    public string BackgroundColor { get; init; } = "#FFFFFF";

    /// <summary>
    /// Whether to include a legend in the export.
    /// Shows entity types and relationship types with colors.
    /// Default: true.
    /// </summary>
    public bool IncludeLegend { get; init; } = true;

    /// <summary>
    /// Whether to include a timestamp in the export metadata.
    /// Useful for tracking export date.
    /// Default: true.
    /// </summary>
    public bool IncludeTimestamp { get; init; } = true;

    /// <summary>
    /// Whether to include bounds information in exports.
    /// Bounds define the visible area of the graph.
    /// Default: true.
    /// </summary>
    public bool IncludeBounds { get; init; } = true;

    /// <summary>
    /// Compression level for PNG (0-9, 0=no compression, 9=max compression).
    /// Higher compression increases export time but reduces file size.
    /// Default: 6 (balanced).
    /// </summary>
    public int CompressionLevel { get; init; } = 6;

    /// <summary>
    /// Whether to include node labels in the export.
    /// If false, nodes appear without text labels.
    /// Default: true.
    /// </summary>
    public bool IncludeNodeLabels { get; init; } = true;

    /// <summary>
    /// Whether to include edge labels (relationship types) in the export.
    /// If false, relationships appear unlabeled.
    /// Default: true.
    /// </summary>
    public bool IncludeEdgeLabels { get; init; } = true;

    /// <summary>
    /// Scale factor for the visualization in the export (0.5 to 2.0).
    /// 1.0 = original size, 0.5 = 50% of original, 2.0 = 200%.
    /// Default: 1.0.
    /// </summary>
    public double Scale { get; init; } = 1.0;

    /// <summary>
    /// Whether to apply node size scaling to the export.
    /// If false, all nodes appear with uniform size.
    /// Default: true.
    /// </summary>
    public bool ApplyNodeSizing { get; init; } = true;

    /// <summary>
    /// Whether to apply edge weight styling (thickness) in the export.
    /// If false, all edges appear with uniform thickness.
    /// Default: true.
    /// </summary>
    public bool ApplyEdgeWeighting { get; init; } = true;
}
```

### 4.4 GraphVisualization Record (Reused)

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// A rendered visualization of a knowledge graph subgraph.
/// Contains nodes, edges, layout bounds, and rendering information.
/// </summary>
public record GraphVisualization
{
    /// <summary>
    /// Visual nodes in the visualization, positioned by layout algorithm.
    /// </summary>
    public IReadOnlyList<VisualNode> Nodes { get; init; } = [];

    /// <summary>
    /// Visual edges (relationships) in the visualization.
    /// </summary>
    public IReadOnlyList<VisualEdge> Edges { get; init; } = [];

    /// <summary>
    /// Bounds of the visualization area (min/max coordinates).
    /// </summary>
    public VisualizationBounds Bounds { get; init; } = new();

    /// <summary>
    /// Layout algorithm used to position nodes.
    /// </summary>
    public LayoutAlgorithm Layout { get; init; } = LayoutAlgorithm.ForceDirected;

    /// <summary>
    /// Title of the visualization (optional).
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Description of the visualization (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Timestamp when visualization was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

### 4.5 VisualNode Record (Referenced)

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// A node in a graph visualization with position, styling, and metadata.
/// </summary>
public record VisualNode
{
    /// <summary>Unique identifier of the entity this node represents.</summary>
    public Guid EntityId { get; init; }

    /// <summary>Display label for the node.</summary>
    public string Label { get; init; } = "";

    /// <summary>Type of entity (Service, Endpoint, Database, etc.).</summary>
    public string EntityType { get; init; } = "";

    /// <summary>X coordinate of node position (canvas coordinates).</summary>
    public double X { get; init; }

    /// <summary>Y coordinate of node position (canvas coordinates).</summary>
    public double Y { get; init; }

    /// <summary>Visual size of node, relative to standard size (1.0 = normal).</summary>
    public double Size { get; init; } = 1.0;

    /// <summary>Color for node rendering (hex code #RRGGBB).</summary>
    public string Color { get; init; } = "#3b82f6";

    /// <summary>Additional metadata for the node (custom attributes, tags, etc.).</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
```

### 4.6 VisualEdge Record (Referenced)

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// An edge (relationship) in a graph visualization with styling and metadata.
/// </summary>
public record VisualEdge
{
    /// <summary>Unique identifier of this relationship.</summary>
    public Guid RelationshipId { get; init; }

    /// <summary>Entity ID of the source node.</summary>
    public Guid SourceId { get; init; }

    /// <summary>Entity ID of the target node.</summary>
    public Guid TargetId { get; init; }

    /// <summary>Type of relationship (DEPENDS_ON, CALLS, etc.).</summary>
    public string RelationshipType { get; init; } = "";

    /// <summary>Optional label for the edge (e.g., "async call").</summary>
    public string? Label { get; init; }

    /// <summary>Weight of the edge (affects thickness in rendering, 0.5 to 3.0).</summary>
    public double Weight { get; init; } = 1.0;

    /// <summary>Color for edge rendering (hex code #RRGGBB).</summary>
    public string Color { get; init; } = "#94a3b8";
}
```

### 4.7 VisualizationBounds Record (Referenced)

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Bounds of a visualization area for coordinate mapping and scaling.
/// </summary>
public record VisualizationBounds
{
    /// <summary>Minimum X coordinate of any node.</summary>
    public double MinX { get; init; } = 0;

    /// <summary>Maximum X coordinate of any node.</summary>
    public double MaxX { get; init; } = 1000;

    /// <summary>Minimum Y coordinate of any node.</summary>
    public double MinY { get; init; } = 0;

    /// <summary>Maximum Y coordinate of any node.</summary>
    public double MaxY { get; init; } = 1000;

    /// <summary>Width of the bounds (MaxX - MinX).</summary>
    public double Width => MaxX - MinX;

    /// <summary>Height of the bounds (MaxY - MinY).</summary>
    public double Height => MaxY - MinY;
}
```

---

## 5. Implementation Details

### 5.1 SVG Export Strategy

SVG exports preserve full interactivity and scalability:

```csharp
namespace Lexichord.Modules.CKVS.Implementations.Visualization;

/// <summary>
/// Exports graph visualizations to SVG format with preserved interactivity.
/// </summary>
internal class SvgExporter
{
    private readonly IGraphRepository _graphRepository;

    public async Task<byte[]> ExportAsync(
        GraphVisualization visualization,
        ExportOptions options,
        CancellationToken ct)
    {
        var svg = new System.Text.StringBuilder();

        // SVG root element
        svg.AppendLine($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" " +
                       $"width=\"{options.Width}\" " +
                       $"height=\"{options.Height}\" " +
                       $"viewBox=\"{visualization.Bounds.MinX} {visualization.Bounds.MinY} " +
                       $"{visualization.Bounds.Width} {visualization.Bounds.Height}\">");

        // Background
        svg.AppendLine($"<rect width=\"{options.Width}\" height=\"{options.Height}\" " +
                       $"fill=\"{options.BackgroundColor}\"/>");

        // Title
        if (options.Title != null)
        {
            svg.AppendLine($"<title>{System.Web.HttpUtility.HtmlEncode(options.Title)}</title>");
        }

        // Draw edges first (behind nodes)
        foreach (var edge in visualization.Edges)
        {
            var sourceNode = visualization.Nodes.FirstOrDefault(n => n.EntityId == edge.SourceId);
            var targetNode = visualization.Nodes.FirstOrDefault(n => n.EntityId == edge.TargetId);

            if (sourceNode != null && targetNode != null)
            {
                // Arrow marker definition
                svg.AppendLine($"<defs><marker id=\"arrow-{edge.RelationshipId}\" " +
                               $"markerWidth=\"10\" markerHeight=\"10\" refX=\"9\" refY=\"3\" " +
                               $"orient=\"auto\" markerUnits=\"strokeWidth\">" +
                               $"<path d=\"M0,0 L0,6 L9,3 z\" fill=\"{edge.Color}\" />" +
                               $"</marker></defs>");

                // Line element
                svg.AppendLine($"<line x1=\"{sourceNode.X}\" y1=\"{sourceNode.Y}\" " +
                               $"x2=\"{targetNode.X}\" y2=\"{targetNode.Y}\" " +
                               $"stroke=\"{edge.Color}\" stroke-width=\"{edge.Weight * 2}\" " +
                               $"marker-end=\"url(#arrow-{edge.RelationshipId})\" />");

                // Edge label
                if (options.IncludeEdgeLabels && edge.Label != null)
                {
                    var midX = (sourceNode.X + targetNode.X) / 2;
                    var midY = (sourceNode.Y + targetNode.Y) / 2;
                    svg.AppendLine($"<text x=\"{midX}\" y=\"{midY}\" text-anchor=\"middle\" " +
                                   $"font-size=\"12\" fill=\"#333\">" +
                                   $"{System.Web.HttpUtility.HtmlEncode(edge.Label)}</text>");
                }
            }
        }

        // Draw nodes
        foreach (var node in visualization.Nodes)
        {
            var radius = node.Size * 20;
            svg.AppendLine($"<circle cx=\"{node.X}\" cy=\"{node.Y}\" r=\"{radius}\" " +
                           $"fill=\"{node.Color}\" stroke=\"#333\" stroke-width=\"2\" />");

            // Node label
            if (options.IncludeNodeLabels)
            {
                svg.AppendLine($"<text x=\"{node.X}\" y=\"{node.Y}\" text-anchor=\"middle\" " +
                               $"dominant-baseline=\"middle\" font-size=\"14\" font-weight=\"bold\" " +
                               $"fill=\"#fff\">{System.Web.HttpUtility.HtmlEncode(node.Label)}</text>");
            }
        }

        // Legend
        if (options.IncludeLegend)
        {
            // Draw legend with entity types and colors
            var legendX = visualization.Bounds.MinX + 20;
            var legendY = visualization.Bounds.MaxY - 100;

            svg.AppendLine($"<text x=\"{legendX}\" y=\"{legendY}\" font-size=\"14\" font-weight=\"bold\">Legend:</text>");
            // Add legend entries for each entity type
        }

        // Metadata comment
        if (options.IncludeTimestamp || options.Description != null)
        {
            svg.AppendLine($"<!-- Exported at {DateTimeOffset.UtcNow:O} -->");
            if (options.Description != null)
            {
                svg.AppendLine($"<!-- {System.Web.HttpUtility.HtmlEncode(options.Description)} -->");
            }
        }

        svg.AppendLine("</svg>");

        return System.Text.Encoding.UTF8.GetBytes(svg.ToString());
    }
}
```

### 5.2 PNG Export Strategy

PNG exports use rasterization for fixed-resolution output:

```csharp
namespace Lexichord.Modules.CKVS.Implementations.Visualization;

/// <summary>
/// Exports graph visualizations to PNG format with rasterization.
/// </summary>
internal class PngExporter
{
    public async Task<byte[]> ExportAsync(
        GraphVisualization visualization,
        ExportOptions options,
        CancellationToken ct)
    {
        // Use SkiaSharp for high-quality rendering
        using (var surface = SkiaSharp.SKSurface.Create(
            new SkiaSharp.SKImageInfo(options.Width, options.Height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SkiaSharp.SKColor.Parse(options.BackgroundColor));

            // Draw edges
            var edgePaint = new SkiaSharp.SKPaint
            {
                IsAntialias = true,
                Style = SkiaSharp.SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            foreach (var edge in visualization.Edges)
            {
                // Find source and target nodes, draw line
                // ... drawing logic ...
            }

            // Draw nodes
            var nodePaint = new SkiaSharp.SKPaint
            {
                IsAntialias = true,
                Style = SkiaSharp.SKPaintStyle.Fill
            };

            foreach (var node in visualization.Nodes)
            {
                nodePaint.Color = SkiaSharp.SKColor.Parse(node.Color);
                var radius = (float)(node.Size * 20);
                canvas.DrawCircle((float)node.X, (float)node.Y, radius, nodePaint);

                // Draw label
                if (options.IncludeNodeLabels)
                {
                    // ... label drawing logic ...
                }
            }

            // Encode to PNG with compression
            using (var image = surface.Snapshot())
            using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, options.CompressionLevel))
            {
                return data.ToArray();
            }
        }
    }
}
```

### 5.3 JSON Export Strategy

JSON exports provide graph structure for web clients:

```csharp
namespace Lexichord.Modules.CKVS.Implementations.Visualization;

/// <summary>
/// Exports graph visualizations to JSON format for web consumption.
/// </summary>
internal class JsonExporter
{
    public async Task<byte[]> ExportAsync(
        GraphVisualization visualization,
        ExportOptions options,
        CancellationToken ct)
    {
        var json = new
        {
            metadata = new
            {
                title = options.Title,
                description = options.Description,
                exportedAt = DateTimeOffset.UtcNow.ToString("O"),
                layout = visualization.Layout.ToString()
            },
            bounds = new
            {
                minX = visualization.Bounds.MinX,
                maxX = visualization.Bounds.MaxX,
                minY = visualization.Bounds.MinY,
                maxY = visualization.Bounds.MaxY
            },
            nodes = visualization.Nodes.Select(n => new
            {
                id = n.EntityId,
                label = n.Label,
                type = n.EntityType,
                x = n.X,
                y = n.Y,
                size = n.Size,
                color = n.Color,
                metadata = n.Metadata
            }).ToList(),
            edges = visualization.Edges.Select(e => new
            {
                id = e.RelationshipId,
                source = e.SourceId,
                target = e.TargetId,
                type = e.RelationshipType,
                label = e.Label,
                weight = e.Weight,
                color = e.Color
            }).ToList()
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var jsonString = System.Text.Json.JsonSerializer.Serialize(json, options);
        return System.Text.Encoding.UTF8.GetBytes(jsonString);
    }
}
```

---

## 6. Error Handling

### 6.1 Invalid Export Format

**Scenario:** Requesting export with unsupported format on restricted tier.

**Handling:**
- Check license tier before allowing export
- Throw `InvalidOperationException` with clear message
- Log the authorization failure

**Code:**
```csharp
if (!licenseContext.IsAvailable("FeatureFlags.CKVS.GraphExport.PDF"))
{
    throw new InvalidOperationException(
        "PDF export requires Enterprise tier. Current tier: " +
        licenseContext.CurrentTier);
}
```

### 6.2 Export Size Exceeds Limits

**Scenario:** Attempting to export a visualization with >10,000 nodes.

**Handling:**
- Validate node count before export
- Suggest clustering or filtering
- Provide alternative (JSON export as data)

**Code:**
```csharp
if (visualization.Nodes.Count > 10000)
{
    throw new InvalidOperationException(
        $"Graph too large for export ({visualization.Nodes.Count} nodes). " +
        "Maximum 10,000 nodes. Consider filtering or clustering.");
}
```

### 6.3 Invalid Export Options

**Scenario:** Scale factor or DPI values outside acceptable ranges.

**Handling:**
- Validate options in constructor
- Clamp invalid values to acceptable ranges
- Log warnings for clamped values

**Code:**
```csharp
public record ExportOptions
{
    private int _dpi;
    public int DPI
    {
        get => _dpi;
        init => _dpi = Math.Clamp(value, 72, 600);
    }
}
```

---

## 7. Testing

### 7.1 Unit Tests

```csharp
[TestClass]
public class GraphExportTests
{
    private IGraphExporter _exporter;

    [TestInitialize]
    public void Setup()
    {
        _exporter = new GraphExporter(/* dependencies */);
    }

    [TestMethod]
    public async Task ExportToSVG_ValidVisualization_ReturnsValidSvgBytes()
    {
        var visualization = CreateTestVisualization();
        var options = new ExportOptions { Title = "Test Graph" };

        var result = await _exporter.ExportAsync(visualization, ExportFormat.SVG, options);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
        Assert.IsTrue(System.Text.Encoding.UTF8.GetString(result).Contains("<svg"));
    }

    [TestMethod]
    public async Task ExportToPNG_ValidVisualization_ReturnsValidPngBytes()
    {
        var visualization = CreateTestVisualization();
        var options = new ExportOptions { Width = 800, Height = 600 };

        var result = await _exporter.ExportAsync(visualization, ExportFormat.PNG, options);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
        // PNG signature bytes: 89 50 4E 47
        Assert.AreEqual(0x89, result[0]);
    }

    [TestMethod]
    public async Task ExportToJSON_ValidVisualization_ReturnsValidJsonBytes()
    {
        var visualization = CreateTestVisualization();
        var options = new ExportOptions { Title = "Test Graph" };

        var result = await _exporter.ExportAsync(visualization, ExportFormat.JSON, options);
        var jsonString = System.Text.Encoding.UTF8.GetString(result);
        var json = System.Text.Json.JsonDocument.Parse(jsonString);

        Assert.IsNotNull(json);
        Assert.IsTrue(json.RootElement.TryGetProperty("nodes", out _));
        Assert.IsTrue(json.RootElement.TryGetProperty("edges", out _));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task ExportLargeGraph_MoreThan10000Nodes_ThrowsException()
    {
        var largeViz = CreateLargeVisualization(15000);
        var options = new ExportOptions();

        await _exporter.ExportAsync(largeViz, ExportFormat.PNG, options);
    }

    [TestMethod]
    public void ExportOptions_DPIValidation_ClampsOutOfRangeValues()
    {
        var options1 = new ExportOptions { DPI = 50 }; // Below min
        var options2 = new ExportOptions { DPI = 1000 }; // Above max

        Assert.AreEqual(72, options1.DPI);
        Assert.AreEqual(600, options2.DPI);
    }

    [TestMethod]
    public void GetFileExtension_AllFormats_ReturnsCorrectExtensions()
    {
        Assert.AreEqual(".svg", _exporter.GetFileExtension(ExportFormat.SVG));
        Assert.AreEqual(".png", _exporter.GetFileExtension(ExportFormat.PNG));
        Assert.AreEqual(".pdf", _exporter.GetFileExtension(ExportFormat.PDF));
        Assert.AreEqual(".json", _exporter.GetFileExtension(ExportFormat.JSON));
    }
}
```

### 7.2 Performance Tests

```csharp
[TestClass]
public class GraphExportPerformanceTests
{
    private IGraphExporter _exporter;

    [TestMethod]
    public async Task ExportSVG_100Nodes_CompletesUnder5Seconds()
    {
        var visualization = CreateTestVisualization(nodeCount: 100);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = await _exporter.ExportAsync(visualization, ExportFormat.SVG, new());

        sw.Stop();
        Assert.IsTrue(sw.ElapsedMilliseconds < 5000,
            $"SVG export took {sw.ElapsedMilliseconds}ms, expected <5000ms");
    }

    [TestMethod]
    public async Task ExportPNG_Medium_Resolution_CompletesFastly()
    {
        var visualization = CreateTestVisualization(nodeCount: 500);
        var options = new ExportOptions { Width = 1920, Height = 1080 };
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = await _exporter.ExportAsync(visualization, ExportFormat.PNG, options);

        sw.Stop();
        Assert.IsTrue(sw.ElapsedMilliseconds < 5000);
    }
}
```

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| SVG export (100 nodes) | <1s | String building, DOM generation |
| PNG export (100 nodes, 1920x1080) | <3s | SkiaSharp rasterization |
| PDF export (100 nodes) | <2s | PDF generation library |
| JSON export (any size) | <500ms | Serialization only |
| Large graph (1000+ nodes) | <5s | Streaming, progressive rendering |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| File path traversal | High | Validate output paths, use safe APIs |
| Large file DoS | Medium | Enforce node/edge limits, timeouts |
| License bypass | High | Check license before enabling formats |
| SVG injection | Medium | Sanitize node labels and metadata |

---

## 10. License Gating

| Tier | SVG | PNG | PDF | JSON |
| :--- | :--- | :--- | :--- | :--- |
| Core | ✗ | ✗ | ✗ | ✗ |
| WriterPro | ✗ | ✗ | ✗ | ✗ |
| Teams | ✓ | ✓ | ✗ | ✓ |
| Enterprise | ✓ | ✓ | ✓ | ✓ |

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Valid visualization (50 nodes) | Export to SVG | Valid SVG file returned in <1s |
| 2 | Valid visualization | Export to PNG with 1920x1080 | Valid PNG file with correct dimensions |
| 3 | Valid visualization | Export with title and description | Metadata included in output |
| 4 | Visualization with edge labels | Export with IncludeEdgeLabels=true | Edge labels appear in output |
| 5 | Visualization | Export with custom colors | Colors preserved in output |
| 6 | WriterPro tier user | Request PDF export | InvalidOperationException thrown |
| 7 | Teams tier user | Export to SVG and PNG | Both formats succeed |
| 8 | Enterprise tier user | Export all formats | All formats SVG/PNG/PDF/JSON succeed |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - IGraphExporter interface, ExportFormat enum, ExportOptions record, SVG/PNG/JSON export strategies |
