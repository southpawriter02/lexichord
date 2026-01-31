# LCS-DES-v0.10.4-KG-f: Design Specification — Search UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-104f` | Graph Visualization sub-part f |
| **Feature Name** | `Search UI` | Unified search and visualization interface |
| **Target Version** | `v0.10.4f` | Sixth sub-part of v0.10.4-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | CKVS knowledge graph module |
| **Swimlane** | `Graph Visualization` | Visualization vertical |
| **License Tier** | `WriterPro` | Available in WriterPro tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVisualization` | Graph visualization feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.4-KG](./LCS-SBD-v0.10.4-KG.md) | Graph Visualization & Search scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.4-KG S2.1](./LCS-SBD-v0.10.4-KG.md#21-sub-parts) | f = Search UI |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.10.4-KG requires a unified search interface combining multiple search modalities into a cohesive user experience. The Search UI must:

1. Provide a single search entry point with query type detection
2. Support four search modes: keyword search, CKVS-QL, path finding, semantic search
3. Display results with visualization and action panels
4. Enable filtering, clustering, and export operations
5. Be responsive with <500ms interaction latency
6. Adapt UI based on license tier and feature availability

### 2.2 The Proposed Solution

Implement a comprehensive Search UI component with:

1. **Search input with mode selector** for query type selection
2. **Query builder with autocomplete** for CKVS-QL syntax assistance
3. **Results display panel** with result cards and actions
4. **Visualization panel** with graph rendering and controls
5. **Path finder dialog** for dedicated path-finding UI
6. **Filter and cluster controls** for result refinement
7. **Export integration** with format selection

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRenderer` | v0.10.4a | Visualization panel rendering |
| `IPathFinder` | v0.10.4b | Path finding operations |
| `IGraphQueryService` | v0.10.4c | CKVS-QL query execution |
| `ISemanticGraphSearch` | v0.10.4d | Semantic search capabilities |
| `IGraphExporter` | v0.10.4e | Export functionality |
| `IGraphRepository` | v0.4.5e | Graph data access |
| `ILicenseContext` | v0.9.2 | License tier validation |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Text.RegularExpressions` | Built-in | Input validation and sanitization |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Basic search UI with visualization and path finding
- **Teams Tier:** WriterPro + CKVS-QL query builder
- **Enterprise Tier:** All features + semantic search + advanced export

---

## 4. Data Contract (The API)

### 4.1 ISearchUIService Interface

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Main interface for the unified search UI service.
/// Orchestrates interaction between search modes and result presentation.
/// </summary>
public interface ISearchUIService
{
    /// <summary>
    /// Executes a search in the specified mode and returns results.
    /// Auto-detects search mode based on query content if mode not specified.
    /// </summary>
    Task<SearchUIResult> SearchAsync(
        string query,
        SearchMode? explicitMode = null,
        SearchUIOptions options = default,
        CancellationToken ct = default);

    /// <summary>
    /// Gets autocomplete suggestions for the current input.
    /// Suggestions vary based on detected search mode.
    /// </summary>
    Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string partialInput,
        int cursorPosition,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a search query without executing it.
    /// Returns validation errors and warnings.
    /// </summary>
    Task<SearchValidationResult> ValidateAsync(
        string query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets available search modes based on current license tier.
    /// Modes returned depend on FeatureFlags and licensing.
    /// </summary>
    Task<IReadOnlyList<SearchModeInfo>> GetAvailableModesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets display information for available UI controls.
    /// Adapts based on license tier and feature gates.
    /// </summary>
    Task<SearchUILayout> GetUILayoutAsync(CancellationToken ct = default);
}
```

### 4.2 SearchMode Enum

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Search modes available in the unified search interface.
/// </summary>
public enum SearchMode
{
    /// <summary>
    /// Simple keyword search across entity names and properties.
    /// Fastest, least powerful, always available.
    /// Example: "auth service"
    /// </summary>
    Keyword = 1,

    /// <summary>
    /// Structured query using CKVS-QL syntax.
    /// Most powerful, requires Teams tier or above.
    /// Example: "FIND Entity WHERE type = \"Service\""
    /// </summary>
    StructuredQuery = 2,

    /// <summary>
    /// Find paths between two entities.
    /// Requires WriterPro tier or above.
    /// Example: "UserService -> PaymentGateway"
    /// </summary>
    PathFinding = 3,

    /// <summary>
    /// Semantic search using natural language understanding.
    /// Returns ranked results, requires Enterprise tier.
    /// Example: "endpoints that handle authentication"
    /// </summary>
    SemanticSearch = 4
}
```

### 4.3 SearchUIResult Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Combined results from a search operation in the UI.
/// Contains results, visualization, and metadata.
/// </summary>
public record SearchUIResult
{
    /// <summary>
    /// The search mode that was executed.
    /// </summary>
    public required SearchMode ModeUsed { get; init; }

    /// <summary>
    /// The original search query.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Number of total results (may exceed displayed results).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Result items to display in the UI.
    /// May be paginated (show first N results).
    /// </summary>
    public IReadOnlyList<SearchResultItem> Items { get; init; } = [];

    /// <summary>
    /// Visualization of the result set (if applicable).
    /// Null for Keyword search, populated for structured/path/semantic.
    /// </summary>
    public GraphVisualization? Visualization { get; init; }

    /// <summary>
    /// Path results (for PathFinding mode).
    /// Null for other modes.
    /// </summary>
    public IReadOnlyList<PathResult>? Paths { get; init; }

    /// <summary>
    /// Time taken to execute the search (in milliseconds).
    /// Used for performance feedback in UI.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Whether there are more results beyond what's displayed.
    /// If true, UI should show "Load More" button.
    /// </summary>
    public bool HasMoreResults { get; init; }

    /// <summary>
    /// Suggested related searches based on results.
    /// Helps users refine their search.
    /// </summary>
    public IReadOnlyList<string>? RelatedSearches { get; init; }
}
```

### 4.4 SearchResultItem Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// A single item in search results, with flexible content type.
/// </summary>
public record SearchResultItem
{
    /// <summary>
    /// Unique identifier for this result.
    /// Usually the entity ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display title/name for the result.
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// Type of the result (Entity, Relationship, Path, etc.).
    /// </summary>
    public string ResultType { get; init; } = "";

    /// <summary>
    /// Entity type (Service, Endpoint, Database, etc.).
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Relevance or match score (0.0 to 1.0).
    /// Used for sorting and highlighting.
    /// </summary>
    public float RelevanceScore { get; init; } = 1.0f;

    /// <summary>
    /// Preview snippet or excerpt from the entity.
    /// Usually first 100 chars of description.
    /// </summary>
    public string? Snippet { get; init; }

    /// <summary>
    /// Which property matched the search term.
    /// For keyword search: "name", "description", etc.
    /// For semantic search: the matched property.
    /// </summary>
    public string? MatchedProperty { get; init; }

    /// <summary>
    /// Custom metadata about the result.
    /// May contain tags, links, or other UI-relevant info.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Available actions for this result.
    /// Examples: "View", "View in Graph", "Export", "Open Document".
    /// </summary>
    public IReadOnlyList<SearchResultAction>? Actions { get; init; }
}
```

### 4.5 SearchResultAction Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// An action that can be performed on a search result.
/// </summary>
public record SearchResultAction
{
    /// <summary>
    /// Unique identifier for this action.
    /// Used to route the action to appropriate handler.
    /// </summary>
    public string ActionId { get; init; } = "";

    /// <summary>
    /// Display label for the action button.
    /// Examples: "View", "View in Graph", "Export".
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// Icon name for the action (optional).
    /// Used to display visual indicators.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Whether this action is available in current context.
    /// May be false due to license restrictions or missing data.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Tooltip or help text for the action.
    /// Explains what the action does.
    /// </summary>
    public string? Description { get; init; }
}
```

### 4.6 SearchSuggestion Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Autocomplete suggestion for search input.
/// </summary>
public record SearchSuggestion
{
    /// <summary>
    /// The suggested text to insert.
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// Type of suggestion (Keyword, Property, Entity, etc.).
    /// </summary>
    public SuggestionType Type { get; init; }

    /// <summary>
    /// Description or preview of the suggestion.
    /// Shows context about what the suggestion represents.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Position in the input where this suggestion applies.
    /// Used for inline replacement vs appending.
    /// </summary>
    public int CursorPosition { get; init; }

    /// <summary>
    /// Icon for visual representation in dropdown.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Priority for ordering suggestions (higher = higher priority).
    /// </summary>
    public int Priority { get; init; } = 0;
}
```

### 4.7 SuggestionType Enum

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Types of autocomplete suggestions.
/// </summary>
public enum SuggestionType
{
    /// <summary>Keyword like FIND, WHERE, AND, OR.</summary>
    Keyword = 1,

    /// <summary>Entity type like Service, Endpoint, Database.</summary>
    EntityType = 2,

    /// <summary>Relationship type like DEPENDS_ON, CALLS.</summary>
    RelationshipType = 3,

    /// <summary>Property name like name, type, method.</summary>
    Property = 4,

    /// <summary>Specific entity name from the graph.</summary>
    Entity = 5,

    /// <summary>Common search term or saved search.</summary>
    SavedSearch = 6,

    /// <summary>Function or operation like EXPAND, GROUP BY.</summary>
    Function = 7
}
```

### 4.8 SearchUIOptions Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Configuration options for search UI operations.
/// </summary>
public record SearchUIOptions
{
    /// <summary>
    /// Maximum number of results to return.
    /// Default: 50.
    /// </summary>
    public int MaxResults { get; init; } = 50;

    /// <summary>
    /// Whether to include visualization in results.
    /// Default: true.
    /// </summary>
    public bool IncludeVisualization { get; init; } = true;

    /// <summary>
    /// Maximum nodes/edges in visualization.
    /// Larger results are shown as snippets.
    /// Default: 100 nodes.
    /// </summary>
    public int MaxVisualizationNodes { get; init; } = 100;

    /// <summary>
    /// Include related search suggestions.
    /// Default: true.
    /// </summary>
    public bool IncludeRelatedSearches { get; init; } = true;

    /// <summary>
    /// Timeout for search execution (milliseconds).
    /// Default: 5000ms.
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Whether to include snippets/previews in results.
    /// Default: true.
    /// </summary>
    public bool IncludeSnippets { get; init; } = true;

    /// <summary>
    /// Snippet length (characters).
    /// Default: 150.
    /// </summary>
    public int SnippetLength { get; init; } = 150;
}
```

### 4.9 SearchValidationResult Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Results of query validation without execution.
/// </summary>
public record SearchValidationResult
{
    /// <summary>
    /// Whether the query is valid and executable.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Detected search mode based on query content.
    /// </summary>
    public SearchMode DetectedMode { get; init; }

    /// <summary>
    /// Human-readable errors preventing execution.
    /// Empty if IsValid is true.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Warnings or suggestions for improvement.
    /// Does not prevent execution.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Estimated execution time for this query (milliseconds).
    /// Based on query complexity and graph size.
    /// </summary>
    public long EstimatedExecutionTimeMs { get; init; }

    /// <summary>
    /// Whether this query requires a specific license tier.
    /// Null if available on current tier.
    /// </summary>
    public string? RequiredLicenseTier { get; init; }
}
```

### 4.10 SearchModeInfo Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Information about an available search mode for UI display.
/// </summary>
public record SearchModeInfo
{
    /// <summary>
    /// The search mode.
    /// </summary>
    public SearchMode Mode { get; init; }

    /// <summary>
    /// Display name for the mode.
    /// </summary>
    public string DisplayName { get; init; } = "";

    /// <summary>
    /// Description of what this mode does.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Placeholder text for the search input.
    /// Mode-specific examples.
    /// </summary>
    public string Placeholder { get; init; } = "";

    /// <summary>
    /// Whether this mode is available on current license.
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// If not available, the required tier.
    /// </summary>
    public string? RequiredTier { get; init; }

    /// <summary>
    /// Icon for visual representation.
    /// </summary>
    public string? Icon { get; init; }
}
```

### 4.11 SearchUILayout Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Information about UI layout and available controls.
/// Adapts based on license tier and feature gates.
/// </summary>
public record SearchUILayout
{
    /// <summary>
    /// Available search modes for the current user.
    /// </summary>
    public IReadOnlyList<SearchModeInfo> AvailableModes { get; init; } = [];

    /// <summary>
    /// Whether the visualization panel is available.
    /// </summary>
    public bool ShowVisualizationPanel { get; init; }

    /// <summary>
    /// Whether the path finder dialog is available.
    /// </summary>
    public bool ShowPathFinderDialog { get; init; }

    /// <summary>
    /// Whether the export button is available.
    /// </summary>
    public bool ShowExportButton { get; init; }

    /// <summary>
    /// Available export formats.
    /// Empty if export not available.
    /// </summary>
    public IReadOnlyList<ExportFormat> AvailableExportFormats { get; init; } = [];

    /// <summary>
    /// Whether filter controls are shown.
    /// </summary>
    public bool ShowFilterControls { get; init; }

    /// <summary>
    /// Whether clustering controls are shown.
    /// </summary>
    public bool ShowClusteringControls { get; init; }

    /// <summary>
    /// Current license tier affecting feature availability.
    /// </summary>
    public string CurrentLicenseTier { get; init; } = "";

    /// <summary>
    /// Feature flags status (enabled/disabled).
    /// </summary>
    public IReadOnlyDictionary<string, bool>? FeatureFlags { get; init; }
}
```

---

## 5. Implementation Details

### 5.1 Search Mode Detection

```csharp
namespace Lexichord.Modules.CKVS.Implementations.Visualization;

/// <summary>
/// Detects search mode from query content.
/// </summary>
internal class SearchModeDetector
{
    public SearchMode DetectMode(string query)
    {
        var trimmed = query.Trim().ToUpperInvariant();

        // Structured query patterns
        if (trimmed.StartsWith("FIND ") ||
            trimmed.StartsWith("SELECT ") ||
            trimmed.StartsWith("WHERE "))
        {
            return SearchMode.StructuredQuery;
        }

        // Path finding patterns
        if (trimmed.Contains(" -> ") ||
            trimmed.StartsWith("FIND PATH") ||
            query.Contains("PATH FROM"))
        {
            return SearchMode.PathFinding;
        }

        // Keyword search patterns
        if (trimmed.Length > 0 && !trimmed.StartsWith("@") && !trimmed.StartsWith("#"))
        {
            return SearchMode.Keyword;
        }

        // Semantic search with special prefix
        if (trimmed.StartsWith("@"))
        {
            return SearchMode.SemanticSearch;
        }

        return SearchMode.Keyword;
    }
}
```

### 5.2 Search Execution Router

```csharp
namespace Lexichord.Modules.CKVS.Implementations.Visualization;

/// <summary>
/// Routes search queries to appropriate handler based on mode.
/// </summary>
internal class SearchExecutor
{
    private readonly IGraphQueryService _queryService;
    private readonly IPathFinder _pathFinder;
    private readonly ISemanticGraphSearch _semanticSearch;
    private readonly IGraphRenderer _renderer;

    public async Task<SearchUIResult> ExecuteAsync(
        string query,
        SearchMode mode,
        SearchUIOptions options,
        CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        SearchUIResult result = mode switch
        {
            SearchMode.StructuredQuery => await ExecuteStructuredQuery(query, options, ct),
            SearchMode.PathFinding => await ExecutePathFinding(query, options, ct),
            SearchMode.SemanticSearch => await ExecuteSemanticSearch(query, options, ct),
            SearchMode.Keyword => await ExecuteKeywordSearch(query, options, ct),
            _ => throw new InvalidOperationException($"Unknown search mode: {mode}")
        };

        sw.Stop();
        return result with { ExecutionTimeMs = sw.ElapsedMilliseconds };
    }

    private async Task<SearchUIResult> ExecuteStructuredQuery(
        string query,
        SearchUIOptions options,
        CancellationToken ct)
    {
        var queryResult = await _queryService.QueryAsync(query, new(), ct);
        var items = queryResult.Rows.Select((row, idx) => new SearchResultItem
        {
            Id = row.Values.ContainsKey("id") ? (Guid)row.Values["id"]! : Guid.NewGuid(),
            Title = row.Values.ContainsKey("name") ? row.Values["name"]?.ToString() ?? "" : $"Result {idx + 1}",
            ResultType = "Entity",
            RelevanceScore = 1.0f
        }).ToList();

        return new SearchUIResult
        {
            ModeUsed = SearchMode.StructuredQuery,
            Query = query,
            TotalCount = queryResult.TotalCount,
            Items = items,
            ExecutionTimeMs = (long)queryResult.ExecutionTime.TotalMilliseconds
        };
    }

    private async Task<SearchUIResult> ExecutePathFinding(
        string query,
        SearchUIOptions options,
        CancellationToken ct)
    {
        // Parse path query: "EntityA -> EntityB"
        var parts = query.Split("->", StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException("Path query must be in format: Source -> Target");

        // Find source and target entities, then find path
        // Implementation uses PathFinder service
        // Returns path visualization

        return new SearchUIResult { /* ... */ };
    }

    private async Task<SearchUIResult> ExecuteSemanticSearch(
        string query,
        SearchUIOptions options,
        CancellationToken ct)
    {
        var cleanedQuery = query.TrimStart('@').Trim();
        var results = await _semanticSearch.SearchAsync(cleanedQuery, new(), ct);

        var items = results.Hits.Select(hit => new SearchResultItem
        {
            Id = hit.EntityId,
            Title = hit.EntityName,
            ResultType = hit.EntityType,
            RelevanceScore = hit.RelevanceScore,
            Snippet = hit.Snippet,
            MatchedProperty = hit.MatchedProperty,
            Actions = new[]
            {
                new SearchResultAction
                {
                    ActionId = "view",
                    Label = "View",
                    Icon = "eye"
                },
                new SearchResultAction
                {
                    ActionId = "view_in_graph",
                    Label = "View in Graph",
                    Icon = "network"
                }
            }.ToList()
        }).ToList();

        return new SearchUIResult
        {
            ModeUsed = SearchMode.SemanticSearch,
            Query = cleanedQuery,
            TotalCount = results.TotalCount,
            Items = items,
            ExecutionTimeMs = (long)results.SearchTime.TotalMilliseconds
        };
    }

    private async Task<SearchUIResult> ExecuteKeywordSearch(
        string query,
        SearchUIOptions options,
        CancellationToken ct)
    {
        // Simple keyword search using graph repository
        // Searches entity names and descriptions
        // Returns matching entities

        return new SearchUIResult { /* ... */ };
    }
}
```

---

## 6. Error Handling

### 6.1 Invalid Query Syntax

**Scenario:** User enters syntactically invalid CKVS-QL query.

**Handling:**
- Validation returns errors without execution
- UI shows error message with suggestion
- Autocomplete helps user correct syntax

**Code:**
```csharp
var validation = await _searchUI.ValidateAsync(query);
if (!validation.IsValid)
{
    return new QueryErrorResponse
    {
        Errors = validation.Errors,
        Warnings = validation.Warnings,
        SuggestedCorrection = SuggestFix(query)
    };
}
```

### 6.2 License Tier Restricted Feature

**Scenario:** User attempts semantic search without Enterprise tier.

**Handling:**
- Check license before allowing query mode
- Return SearchUILayout with feature disabled
- Show upgrade message in UI

**Code:**
```csharp
var layout = await _searchUI.GetUILayoutAsync();
if (!layout.AvailableModes.Any(m => m.Mode == SearchMode.SemanticSearch))
{
    // UI disables semantic search option
    // Shows message: "Semantic search available in Enterprise tier"
}
```

### 6.3 Search Timeout

**Scenario:** Complex query exceeds timeout.

**Handling:**
- Enforce timeout in search executor
- Return partial results if available
- Log timeout event for monitoring

**Code:**
```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromMilliseconds(options.TimeoutMs));

try
{
    return await ExecuteQueryWithTimeout(query, cts.Token);
}
catch (OperationCanceledException)
{
    throw new SearchTimeoutException(
        $"Search exceeded timeout of {options.TimeoutMs}ms");
}
```

---

## 7. Testing

### 7.1 Unit Tests

```csharp
[TestClass]
public class SearchUIServiceTests
{
    private ISearchUIService _searchUI;
    private Mock<IGraphQueryService> _mockQueryService;
    private Mock<IPathFinder> _mockPathFinder;
    private Mock<ISemanticGraphSearch> _mockSemanticSearch;

    [TestInitialize]
    public void Setup()
    {
        _mockQueryService = new Mock<IGraphQueryService>();
        _mockPathFinder = new Mock<IPathFinder>();
        _mockSemanticSearch = new Mock<ISemanticGraphSearch>();

        _searchUI = new SearchUIService(
            _mockQueryService.Object,
            _mockPathFinder.Object,
            _mockSemanticSearch.Object);
    }

    [TestMethod]
    public void SearchModeDetector_StructuredQuery_DetectsCorrectly()
    {
        var detector = new SearchModeDetector();

        Assert.AreEqual(SearchMode.StructuredQuery,
            detector.DetectMode("FIND Entity WHERE type = \"Service\""));
        Assert.AreEqual(SearchMode.StructuredQuery,
            detector.DetectMode("SELECT * FROM entities"));
    }

    [TestMethod]
    public void SearchModeDetector_PathFinding_DetectsCorrectly()
    {
        var detector = new SearchModeDetector();

        Assert.AreEqual(SearchMode.PathFinding,
            detector.DetectMode("UserService -> PaymentGateway"));
        Assert.AreEqual(SearchMode.PathFinding,
            detector.DetectMode("FIND PATH FROM A TO B"));
    }

    [TestMethod]
    public void SearchModeDetector_SemanticSearch_DetectsWithPrefix()
    {
        var detector = new SearchModeDetector();

        Assert.AreEqual(SearchMode.SemanticSearch,
            detector.DetectMode("@endpoints that handle auth"));
    }

    [TestMethod]
    public void SearchModeDetector_Keyword_IsDefault()
    {
        var detector = new SearchModeDetector();

        Assert.AreEqual(SearchMode.Keyword,
            detector.DetectMode("auth service"));
    }

    [TestMethod]
    public async Task SearchAsync_KeywordQuery_ReturnsResults()
    {
        var result = await _searchUI.SearchAsync("auth", SearchMode.Keyword);

        Assert.IsNotNull(result);
        Assert.AreEqual(SearchMode.Keyword, result.ModeUsed);
        Assert.IsTrue(result.Items.Count > 0);
    }

    [TestMethod]
    public async Task ValidateAsync_StructuredQuery_ReturnsValidation()
    {
        var result = await _searchUI.ValidateAsync(
            "FIND Entity WHERE type = \"Service\"");

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(SearchMode.StructuredQuery, result.DetectedMode);
    }

    [TestMethod]
    public async Task GetAvailableModesAsync_WriterProTier_ExcludesSemanticSearch()
    {
        // Mock license context for WriterPro tier
        var modes = await _searchUI.GetAvailableModesAsync();

        var semanticMode = modes.FirstOrDefault(m => m.Mode == SearchMode.SemanticSearch);
        Assert.IsNotNull(semanticMode);
        Assert.IsFalse(semanticMode.IsAvailable);
        Assert.AreEqual("Enterprise", semanticMode.RequiredTier);
    }
}
```

### 7.2 Integration Tests

```csharp
[TestClass]
public class SearchUIIntegrationTests
{
    [TestMethod]
    public async Task FullWorkflow_KeywordToVisualization_CompleteFlow()
    {
        // 1. Get available modes
        var modes = await _searchUI.GetAvailableModesAsync();
        Assert.IsTrue(modes.Any(m => m.Mode == SearchMode.Keyword));

        // 2. Validate query
        var validation = await _searchUI.ValidateAsync("auth service");
        Assert.IsTrue(validation.IsValid);

        // 3. Execute search
        var result = await _searchUI.SearchAsync("auth service");
        Assert.IsTrue(result.Items.Count > 0);

        // 4. Visualization available
        Assert.IsNotNull(result.Visualization);
    }

    [TestMethod]
    public async Task FullWorkflow_StructuredQueryWithAutocomplete()
    {
        // 1. Get suggestions while typing
        var suggestions = await _searchUI.GetSuggestionsAsync("FIND E", 7);
        Assert.IsTrue(suggestions.Any(s => s.Type == SuggestionType.Keyword));

        // 2. Complete query
        var validation = await _searchUI.ValidateAsync("FIND Entity WHERE type = \"Service\"");
        Assert.IsTrue(validation.IsValid);

        // 3. Execute
        var result = await _searchUI.SearchAsync(
            "FIND Entity WHERE type = \"Service\"",
            SearchMode.StructuredQuery);
        Assert.AreEqual(SearchMode.StructuredQuery, result.ModeUsed);
    }
}
```

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Search execution | <500ms | Index-based lookups for keyword, structured queries |
| Autocomplete suggestions | <100ms | Cached suggestion list, prefix matching |
| UI rendering | <100ms | React/Vue virtual scrolling, lazy loading |
| Visualization render | <500ms | Graph Renderer sub-part |
| Mode detection | <1ms | Regex pattern matching |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Query injection | High | Parser validation, parameterized queries |
| XSS via result snippets | High | HTML escaping, sanitization |
| License bypass | High | Check license on every mode access |
| DoS via complex queries | Medium | Query timeout, resource limits |
| Information disclosure | Medium | Filter results by user permissions |

---

## 10. License Gating

| Tier | Keyword | Structured | Path | Semantic | Export |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Core | ✗ | ✗ | ✗ | ✗ | ✗ |
| WriterPro | ✓ | ✗ | ✓ | ✗ | ✗ |
| Teams | ✓ | ✓ | ✓ | ✗ | ✓ (SVG/PNG) |
| Enterprise | ✓ | ✓ | ✓ | ✓ | ✓ (all) |

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Search UI loaded | User enters keyword query | Results display in <500ms |
| 2 | Keyword query results | User enters visualization | Graph renders with <500ms |
| 3 | CKVS-QL query | User types "FIND" | Autocomplete shows suggestions |
| 4 | Path query "A -> B" | User clicks "Find Path" | Path dialog shows results |
| 5 | Semantic search available | Enterprise user types "@" | Semantic search suggestions appear |
| 6 | WriterPro tier user | Attempts semantic search | Feature shown as unavailable |
| 7 | Invalid CKVS-QL query | User submits | Validation errors shown |
| 8 | Result with 50 hits | User views results | Pagination/Load More available |

---

## 12. UI Mockup

```
┌────────────────────────────────────────────────────────────────┐
│ Knowledge Graph Search                                    [?]    │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Search Mode: [Keyword ▼]  [Structured ▼]  [Path ▼]  [@Search] │
│                                                                │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ 🔍 Search or enter CKVS-QL query...                     │  │
│ │ FIND Entity WHERE type = "Service" EXPAND -[DEPENDS... │  │
│ │ ┌─────────────────────────────────────────────────────┐ │  │
│ │ │ FIND (keyword)                                      │ │  │
│ │ │ FIND Entity (entity type query)                     │ │  │
│ │ │ FIND e1 -[CALLS]-> e2 (relationship query)         │ │  │
│ │ │ FIND PATH (path finding)                           │ │  │
│ │ └─────────────────────────────────────────────────────┘ │  │
│ └──────────────────────────────────────────────────────────┘  │
│                                                                │
│ ┌─────────────────────────┬──────────────────────────────────┐ │
│ │ Results (23 found)      │ Visualization                    │ │
│ │ 0.3s                    │                                  │ │
│ ├─────────────────────────┼──────────────────────────────────┤ │
│ │ ┌─────────────────────┐ │    ┌─────────┐                  │ │
│ │ │ POST /auth/login    │ │    │ AuthSvc │                  │ │
│ │ │ Endpoint            │ │    └────┬────┘                  │ │
│ │ │ "Authenticates..." │ │         │                       │ │
│ │ │ [View] [In Graph]  │ │  ┌──────┴──────┐                │ │
│ │ ├─────────────────────┤ │  │             │                │ │
│ │ │ POST /auth/validate │ │┌─▼──┐    ┌────▼──┐            │ │
│ │ │ Endpoint            │ ││User│    │Order  │            │ │
│ │ │ "Validates token..." │ │└──┬─┘    └─┬─────┘            │ │
│ │ │ [View] [In Graph]  │ │   │        │                    │ │
│ │ ├─────────────────────┤ │   └────┬───┘                   │ │
│ │ │ GET /auth/me        │ │        │                       │ │
│ │ │ Endpoint            │ │    ┌───▼────┐                  │ │
│ │ │ "Returns current..." │ │    │Database│                  │ │
│ │ │ [View] [In Graph]  │ │    └────────┘                  │ │
│ │ └─────────────────────┘ │                                  │ │
│ │                         │    [Zoom +] [Zoom -] [Fit]     │ │
│ │ [Load More]             │    Layout: [Force ▼]            │ │
│ └─────────────────────────┴──────────────────────────────────┘ │
│                                                                │
│ [Filter] [Cluster] [Export ▼]                                 │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - Search UI service, search modes, result items, autocomplete, validation |
