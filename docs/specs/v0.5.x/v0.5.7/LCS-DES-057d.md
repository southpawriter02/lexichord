# LCS-DES-057d: Design Specification — Search Actions

## 1. Metadata & Categorization

| Field                | Value                                                        | Description                   |
| :------------------- | :----------------------------------------------------------- | :---------------------------- |
| **Feature ID**       | `RAG-057d`                                                   | Sub-part of RAG-057           |
| **Feature Name**     | `Search Result Actions`                                      | Copy, export, bulk operations |
| **Target Version**   | `v0.5.7d`                                                    | Fourth sub-part of v0.5.7     |
| **Module Scope**     | `Lexichord.Modules.RAG`                                      | RAG module                    |
| **Swimlane**         | `Memory`                                                     | Retrieval swimlane            |
| **License Tier**     | `Writer Pro`                                                 | Paid feature                  |
| **Feature Gate Key** | `FeatureFlags.RAG.ReferenceDock`                             | Soft gate                     |
| **Author**           | Lead Architect                                               |                               |
| **Status**           | `Draft`                                                      |                               |
| **Last Updated**     | `2026-01-27`                                                 |                               |
| **Parent Document**  | [LCS-DES-057-INDEX](./LCS-DES-057-INDEX.md)                  |                               |
| **Scope Breakdown**  | [LCS-SBD-057 §3.4](./LCS-SBD-057.md#34-v057d-search-actions) |                               |

---

## 2. Executive Summary

### 2.1 The Requirement

Search results are currently "view only". Users have no way to:

- **Share results:** Copy formatted results for documentation or collaboration
- **Archive research:** Export results for offline analysis
- **Bulk operations:** Open all matching documents at once
- **Reference:** Generate citations for found content

> **Problem:** Writers need to capture and share research findings. Manually copying results one-by-one is tedious and error-prone.

### 2.2 The Proposed Solution

Implement an `ISearchActionsService` that:

1. "Copy All Results" → Formats results as Markdown and copies to clipboard
2. "Export Results" → Saves results as JSON/CSV/Markdown files
3. "Open All in Editor" → Opens all unique source documents as tabs
4. Publishes `SearchResultsExportedEvent` for analytics/audit
5. Supports format selection via dialog

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface              | Source Version | Purpose                     |
| :--------------------- | :------------- | :-------------------------- |
| `GroupedSearchResults` | v0.5.7b        | Result data to export       |
| `ICitationService`     | v0.5.2a        | Citation formatting         |
| `IEditorService`       | v0.1.3a        | Open documents              |
| `IFileService`         | v0.1.4b        | File export                 |
| `IDialogService`       | v0.1.2a        | Save file dialog            |
| `IToastService`        | v0.1.6c        | Success/error notifications |
| `IMediator`            | v0.0.7a        | Event publishing            |
| `ILicenseContext`      | v0.0.4c        | License gating              |

#### 3.1.2 NuGet Packages

| Package            | Version | Purpose          |
| :----------------- | :------ | :--------------- |
| `CsvHelper`        | 31.x    | CSV export       |
| `System.Text.Json` | 9.x     | JSON export      |
| `MediatR`          | 12.x    | Event publishing |

### 3.2 Licensing Behavior

All search actions require Writer Pro+ license. Unlicensed users see disabled buttons with "Upgrade" tooltip.

```csharp
public bool IsExportEnabled =>
    _licenseContext.HasFeature(FeatureFlags.RAG.ReferenceDock);
```

---

## 4. Data Contract (The API)

### 4.1 ISearchActionsService Interface

```csharp
namespace Lexichord.Modules.RAG.Contracts;

/// <summary>
/// Provides copy, export, and bulk-open actions for search results.
/// </summary>
/// <remarks>
/// <para>All actions require Writer Pro license. Unlicensed calls return
/// appropriate error results without performing the action.</para>
/// <para>Export operations are performed asynchronously with progress
/// reporting for large result sets.</para>
/// </remarks>
public interface ISearchActionsService
{
    /// <summary>
    /// Copies all results to clipboard as formatted text.
    /// </summary>
    /// <param name="results">The grouped results to copy.</param>
    /// <param name="format">The text format (Markdown or PlainText).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure reason.</returns>
    Task<ActionResult> CopyResultsToClipboardAsync(
        GroupedSearchResults results,
        CopyFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Exports results to a file.
    /// </summary>
    /// <param name="results">The grouped results to export.</param>
    /// <param name="options">Export configuration including format and path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with export details or failure reason.</returns>
    Task<ExportResult> ExportResultsAsync(
        GroupedSearchResults results,
        ExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Opens all unique source documents in the editor.
    /// </summary>
    /// <param name="results">The grouped results to open.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with count of opened documents.</returns>
    Task<OpenAllResult> OpenAllDocumentsAsync(
        GroupedSearchResults results,
        CancellationToken ct = default);
}
```

### 4.2 Action Result Types

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Result of a copy operation.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="CharacterCount">Number of characters copied.</param>
/// <param name="ErrorMessage">Error description if failed.</param>
public record ActionResult(
    bool Success,
    int CharacterCount = 0,
    string? ErrorMessage = null)
{
    public static ActionResult LicenseRequired() =>
        new(false, ErrorMessage: "Writer Pro license required");

    public static ActionResult NoResults() =>
        new(false, ErrorMessage: "No results to copy");

    public static ActionResult Ok(int chars) =>
        new(true, CharacterCount: chars);
}

/// <summary>
/// Format for clipboard copy operations.
/// </summary>
public enum CopyFormat
{
    /// <summary>Markdown with headers and formatting.</summary>
    Markdown,

    /// <summary>Plain text without special formatting.</summary>
    PlainText
}
```

### 4.3 Export Types

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Configuration for export operations.
/// </summary>
/// <param name="FilePath">Destination file path.</param>
/// <param name="Format">Export format.</param>
/// <param name="IncludeSnippets">Whether to include result snippets.</param>
/// <param name="IncludeCitations">Whether to include formatted citations.</param>
public record ExportOptions(
    string FilePath,
    ExportFormat Format,
    bool IncludeSnippets = true,
    bool IncludeCitations = true)
{
    /// <summary>
    /// Gets the appropriate file extension for the format.
    /// </summary>
    public string FileExtension => Format switch
    {
        ExportFormat.JSON => ".json",
        ExportFormat.CSV => ".csv",
        ExportFormat.Markdown => ".md",
        _ => ".txt"
    };
}

/// <summary>
/// Export file format.
/// </summary>
public enum ExportFormat
{
    /// <summary>JSON with full structured data.</summary>
    JSON,

    /// <summary>CSV for spreadsheet import.</summary>
    CSV,

    /// <summary>Markdown for documentation.</summary>
    Markdown
}

/// <summary>
/// Result of an export operation.
/// </summary>
/// <param name="Success">Whether the export succeeded.</param>
/// <param name="FilePath">Path to the created file (if successful).</param>
/// <param name="DocumentCount">Number of unique documents exported.</param>
/// <param name="HitCount">Number of results exported.</param>
/// <param name="BytesWritten">Size of the exported file.</param>
/// <param name="ErrorMessage">Error description if failed.</param>
public record ExportResult(
    bool Success,
    string? FilePath = null,
    int DocumentCount = 0,
    int HitCount = 0,
    long BytesWritten = 0,
    string? ErrorMessage = null)
{
    public static ExportResult LicenseRequired() =>
        new(false, ErrorMessage: "Writer Pro license required");

    public static ExportResult NoResults() =>
        new(false, ErrorMessage: "No results to export");

    public static ExportResult Error(string message) =>
        new(false, ErrorMessage: message);
}

/// <summary>
/// Result of an open-all operation.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="DocumentsOpened">Number of documents opened.</param>
/// <param name="ErrorMessage">Error description if failed.</param>
public record OpenAllResult(
    bool Success,
    int DocumentsOpened = 0,
    string? ErrorMessage = null)
{
    public static OpenAllResult LicenseRequired() =>
        new(false, ErrorMessage: "Writer Pro license required");

    public static OpenAllResult NoResults() =>
        new(false, ErrorMessage: "No results to open");
}
```

### 4.4 MediatR Event

```csharp
namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// Published when search results are exported to a file.
/// </summary>
/// <remarks>
/// <para>This event can be used for analytics, audit logging, or
/// triggering follow-up actions (e.g., adding to recent files).</para>
/// </remarks>
/// <param name="Query">The original search query.</param>
/// <param name="Format">The export format used.</param>
/// <param name="DocumentCount">Number of unique documents exported.</param>
/// <param name="HitCount">Total number of results exported.</param>
/// <param name="FilePath">Path to the exported file.</param>
/// <param name="Timestamp">When the export occurred.</param>
public record SearchResultsExportedEvent(
    string Query,
    ExportFormat Format,
    int DocumentCount,
    int HitCount,
    string? FilePath,
    DateTimeOffset Timestamp) : INotification
{
    public SearchResultsExportedEvent(
        string query,
        ExportFormat format,
        int documentCount,
        int hitCount,
        string? filePath)
        : this(query, format, documentCount, hitCount, filePath, DateTimeOffset.UtcNow)
    {
    }
}
```

---

## 5. Implementation Logic

### 5.1 SearchActionsService Implementation

```csharp
namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implements copy, export, and bulk-open actions for search results.
/// </summary>
public sealed class SearchActionsService : ISearchActionsService
{
    private readonly ILicenseContext _licenseContext;
    private readonly ICitationService _citationService;
    private readonly IEditorService _editorService;
    private readonly IFileService _fileService;
    private readonly IMediator _mediator;
    private readonly ILogger<SearchActionsService> _logger;

    public SearchActionsService(
        ILicenseContext licenseContext,
        ICitationService citationService,
        IEditorService editorService,
        IFileService fileService,
        IMediator mediator,
        ILogger<SearchActionsService> logger)
    {
        _licenseContext = licenseContext;
        _citationService = citationService;
        _editorService = editorService;
        _fileService = fileService;
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ActionResult> CopyResultsToClipboardAsync(
        GroupedSearchResults results,
        CopyFormat format,
        CancellationToken ct)
    {
        if (!CheckLicense())
            return ActionResult.LicenseRequired();

        if (results.TotalHits == 0)
            return ActionResult.NoResults();

        var content = format switch
        {
            CopyFormat.Markdown => FormatAsMarkdown(results),
            CopyFormat.PlainText => FormatAsPlainText(results),
            _ => FormatAsMarkdown(results)
        };

        await ClipboardService.SetTextAsync(content);

        _logger.LogDebug("Copied {Count} results to clipboard as {Format}",
            results.TotalHits, format);

        return ActionResult.Ok(content.Length);
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportResultsAsync(
        GroupedSearchResults results,
        ExportOptions options,
        CancellationToken ct)
    {
        if (!CheckLicense())
            return ExportResult.LicenseRequired();

        if (results.TotalHits == 0)
            return ExportResult.NoResults();

        try
        {
            var content = options.Format switch
            {
                ExportFormat.JSON => FormatAsJson(results, options),
                ExportFormat.CSV => FormatAsCsv(results, options),
                ExportFormat.Markdown => FormatAsMarkdown(results),
                _ => throw new ArgumentOutOfRangeException(nameof(options.Format))
            };

            await _fileService.WriteTextAsync(options.FilePath, content, ct);

            var fileInfo = new FileInfo(options.FilePath);

            // Publish event for analytics/audit
            await _mediator.Publish(new SearchResultsExportedEvent(
                Query: results.Query ?? "",
                Format: options.Format,
                DocumentCount: results.TotalDocuments,
                HitCount: results.TotalHits,
                FilePath: options.FilePath), ct);

            _logger.LogInformation("Exported {Count} results to {Format} at {Path}",
                results.TotalHits, options.Format, options.FilePath);

            return new ExportResult(
                Success: true,
                FilePath: options.FilePath,
                DocumentCount: results.TotalDocuments,
                HitCount: results.TotalHits,
                BytesWritten: fileInfo.Length);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to export results to {Path}", options.FilePath);
            return ExportResult.Error($"File write failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during export");
            return ExportResult.Error($"Export failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<OpenAllResult> OpenAllDocumentsAsync(
        GroupedSearchResults results,
        CancellationToken ct)
    {
        if (!CheckLicense())
            return OpenAllResult.LicenseRequired();

        if (results.TotalHits == 0)
            return OpenAllResult.NoResults();

        var uniquePaths = results.Groups
            .Select(g => g.DocumentPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var openedCount = 0;
        foreach (var path in uniquePaths)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                await _editorService.OpenFileAsync(path);
                openedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to open document: {Path}", path);
            }
        }

        _logger.LogDebug("Opened {Count} documents in editor", openedCount);

        return new OpenAllResult(true, openedCount);
    }

    private bool CheckLicense() =>
        _licenseContext.HasFeature(FeatureFlags.RAG.ReferenceDock);

    // ========================================================================
    // Formatters
    // ========================================================================

    private string FormatAsMarkdown(GroupedSearchResults results)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# Search Results: \"{results.Query}\"");
        sb.AppendLine();
        sb.AppendLine($"*{results.TotalHits} results in {results.TotalDocuments} documents • Generated {DateTime.Now:yyyy-MM-dd HH:mm}*");
        sb.AppendLine();

        // Groups
        foreach (var group in results.Groups)
        {
            sb.AppendLine($"## {group.DocumentTitle} ({group.MatchCount} matches)");
            sb.AppendLine();

            var index = 1;
            foreach (var hit in group.Hits)
            {
                var snippet = TruncateSnippet(hit.Chunk.Content, 200);
                sb.AppendLine($"{index}. **{GetHighlightTitle(hit)}** (Score: {hit.Score:F2})");
                sb.AppendLine($"   > {snippet}");
                sb.AppendLine($"   [{group.FileName}#L{hit.Chunk.LineNumber}]({FormatFileLink(hit)})");
                sb.AppendLine();
                index++;
            }

            if (group.HasMoreHits)
            {
                sb.AppendLine($"*...and {group.HiddenHitCount} more matches*");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private string FormatAsPlainText(GroupedSearchResults results)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Search Results: \"{results.Query}\"");
        sb.AppendLine($"{results.TotalHits} results in {results.TotalDocuments} documents");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();

        foreach (var group in results.Groups)
        {
            sb.AppendLine($"[{group.DocumentTitle}] ({group.MatchCount} matches)");
            sb.AppendLine(new string('-', 40));

            foreach (var hit in group.Hits)
            {
                var snippet = TruncateSnippet(hit.Chunk.Content, 150);
                sb.AppendLine($"  Line {hit.Chunk.LineNumber}: {snippet}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatAsJson(GroupedSearchResults results, ExportOptions options)
    {
        var export = new
        {
            query = results.Query,
            timestamp = DateTimeOffset.UtcNow,
            searchDuration = results.SearchDuration.TotalMilliseconds,
            totalHits = results.TotalHits,
            totalDocuments = results.TotalDocuments,
            groups = results.Groups.Select(g => new
            {
                documentPath = g.DocumentPath,
                documentTitle = g.DocumentTitle,
                matchCount = g.MatchCount,
                maxScore = g.MaxScore,
                hits = g.Hits.Select(h => new
                {
                    score = h.Score,
                    lineNumber = h.Chunk.LineNumber,
                    snippet = options.IncludeSnippets ? h.Chunk.Content : null,
                    citation = options.IncludeCitations
                        ? _citationService.Format(h, CitationStyle.Short)
                        : null
                })
            })
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(export, jsonOptions);
    }

    private string FormatAsCsv(GroupedSearchResults results, ExportOptions options)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("DocumentPath,DocumentTitle,Score,LineNumber,Snippet,Citation");

        // Rows
        foreach (var group in results.Groups)
        {
            foreach (var hit in group.Hits)
            {
                var snippet = options.IncludeSnippets
                    ? EscapeCsv(TruncateSnippet(hit.Chunk.Content, 200))
                    : "";

                var citation = options.IncludeCitations
                    ? EscapeCsv(_citationService.Format(hit, CitationStyle.Short))
                    : "";

                sb.AppendLine(string.Join(",",
                    EscapeCsv(group.DocumentPath),
                    EscapeCsv(group.DocumentTitle),
                    hit.Score.ToString("F4"),
                    hit.Chunk.LineNumber,
                    snippet,
                    citation));
            }
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return $"\"{value}\"";
    }

    private static string TruncateSnippet(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return "";

        var normalized = content.Replace("\n", " ").Replace("\r", "").Trim();

        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..(maxLength - 3)] + "...";
    }

    private static string GetHighlightTitle(SearchHit hit) =>
        hit.Chunk.Metadata?.Heading ?? "Match";

    private static string FormatFileLink(SearchHit hit) =>
        $"file://{hit.Document.FilePath}#L{hit.Chunk.LineNumber}";
}
```

### 5.2 Export Flow Diagram

```mermaid
flowchart TD
    A[User clicks Export] --> B{Any results?}
    B -->|No| C[Show "No results" message]
    B -->|Yes| D{User licensed?}
    D -->|No| E[Show upgrade prompt]
    D -->|Yes| F[Show format selection dialog]
    F --> G[User selects format]
    G --> H[Show save file dialog]
    H --> I{User confirmed?}
    I -->|No| J[Cancel]
    I -->|Yes| K[Format results]
    K --> L[Write to file]
    L --> M{Success?}
    M -->|No| N[Show error toast]
    M -->|Yes| O[Publish SearchResultsExportedEvent]
    O --> P[Show success toast]
```

### 5.3 Export Format Decision Tree

```text
INPUT: ExportFormat format, ExportOptions options, GroupedSearchResults results
OUTPUT: Formatted string content

DECISION TREE:
┌─ Is format = JSON?
│   └─ Build JSON object:
│       ├─ query, timestamp, searchDuration
│       ├─ totalHits, totalDocuments
│       └─ groups[] with:
│           ├─ documentPath, documentTitle, matchCount, maxScore
│           └─ hits[] with:
│               ├─ score, lineNumber
│               ├─ snippet (if IncludeSnippets)
│               └─ citation (if IncludeCitations)
│
├─ Is format = CSV?
│   └─ Build CSV:
│       ├─ Header: DocumentPath,DocumentTitle,Score,LineNumber,Snippet,Citation
│       └─ Rows: One per hit, escape special chars
│
├─ Is format = Markdown?
│   └─ Build Markdown:
│       ├─ # Search Results: "{query}"
│       ├─ *N results in M documents • Generated YYYY-MM-DD*
│       └─ For each group:
│           ├─ ## DocumentTitle (N matches)
│           └─ 1. **HighlightTitle** (Score: X.XX)
│              > Snippet...
│              [Link](file://...)
│
└─ DEFAULT: Use Markdown format
```

---

## 6. Data Persistence

**None required.** Exports are written to user-specified files. No internal state needs persistence.

---

## 7. UI/UX Specifications

### 7.1 Action Button Layout

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│  [📋 Copy All ▾]  [📤 Export ▾]  [📂 Open All]                              │
└─────────────────────────────────────────────────────────────────────────────┘
         ↑                 ↑              ↑
    Copy dropdown    Export dropdown  Direct action
    - Markdown       - JSON
    - Plain Text     - CSV
                     - Markdown
```

### 7.2 Export Dialog

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│  Export Search Results                                               [×]   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Format:  ○ JSON (Full structured data)                                    │
│           ● Markdown (Human-readable document)                             │
│           ○ CSV (For spreadsheets)                                         │
│                                                                             │
│  Options:                                                                   │
│           ☑ Include snippets                                               │
│           ☑ Include citations                                              │
│                                                                             │
│  Exporting: 47 results from 8 documents                                    │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                               [Cancel]  [Choose Location…] │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.3 Toast Notifications

| Action            | Success Message                   | Error Message               |
| :---------------- | :-------------------------------- | :-------------------------- |
| Copy to clipboard | "Copied 47 results to clipboard"  | "Failed to copy results"    |
| Export to file    | "Exported to search-results.json" | "Export failed: {reason}"   |
| Open all          | "Opened 8 documents in editor"    | "Failed to open some files" |

### 7.4 Keyboard Shortcuts

| Action   | Shortcut | Scope                |
| :------- | :------- | :------------------- |
| Copy All | `Ctrl+C` | When results focused |
| Export   | `Ctrl+E` | When panel focused   |
| Open All | `Ctrl+O` | When panel focused   |

---

## 8. Observability & Logging

| Level   | Source               | Message Template                                    |
| :------ | :------------------- | :-------------------------------------------------- |
| Debug   | SearchActionsService | `"Copied {Count} results to clipboard as {Format}"` |
| Info    | SearchActionsService | `"Exported {Count} results to {Format} at {Path}"`  |
| Debug   | SearchActionsService | `"Opened {Count} documents in editor"`              |
| Warning | SearchActionsService | `"Failed to open document: {Path}"`                 |
| Error   | SearchActionsService | `"Failed to export results to {Path}"`              |
| Error   | SearchActionsService | `"Unexpected error during export"`                  |

---

## 9. Security & Safety

| Risk                    | Level  | Mitigation                                  |
| :---------------------- | :----- | :------------------------------------------ |
| Path traversal          | Medium | Validate export path is within allowed dirs |
| Large export size       | Low    | Warn if exporting >1000 results             |
| Overwrite existing file | Low    | Confirm before overwriting                  |
| Clipboard permissions   | Low    | Handled by OS                               |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| #   | Given                                   | When                                | Then                                         |
| :-- | :-------------------------------------- | :---------------------------------- | :------------------------------------------- |
| 1   | Search with 10 results                  | User clicks "Copy All → Markdown"   | Clipboard contains Markdown with all results |
| 2   | Search with 10 results                  | User clicks "Copy All → Plain Text" | Clipboard contains plain text                |
| 3   | Search with 10 results                  | User clicks "Export → JSON"         | JSON file created with structured data       |
| 4   | Search with 10 results                  | User clicks "Export → CSV"          | CSV file created with tabular data           |
| 5   | Search with 10 results                  | User clicks "Export → Markdown"     | Markdown file created                        |
| 6   | Results from 5 documents                | User clicks "Open All"              | 5 document tabs open in editor               |
| 7   | Duplicate document in results           | User clicks "Open All"              | Document opens only once                     |
| 8   | Export succeeds                         | Export completes                    | Success toast shown                          |
| 9   | Export fails (permission denied)        | Export attempted                    | Error toast shown with reason                |
| 10  | No search results                       | User clicks any action              | "No results" message shown                   |
| 11  | User is unlicensed                      | User clicks any action              | Upgrade prompt shown                         |
| 12  | Export with "Include citations" checked | JSON exported                       | Citations present in output                  |

### 10.2 Performance Criteria

| #   | Given        | When              | Then                 |
| :-- | :----------- | :---------------- | :------------------- |
| 13  | 100 results  | Copy to clipboard | Completes in < 100ms |
| 14  | 500 results  | Export to JSON    | Completes in < 500ms |
| 15  | 50 documents | Open all          | Opens progressively  |

---

## 11. Test Scenarios

### 11.1 Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.7d")]
public class SearchActionsServiceTests
{
    private readonly Mock<ILicenseContext> _licenseMock;
    private readonly Mock<ICitationService> _citationMock;
    private readonly Mock<IEditorService> _editorMock;
    private readonly Mock<IFileService> _fileMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<SearchActionsService>> _loggerMock;
    private readonly SearchActionsService _sut;

    public SearchActionsServiceTests()
    {
        _licenseMock = new Mock<ILicenseContext>();
        _licenseMock.Setup(l => l.HasFeature(It.IsAny<string>())).Returns(true);

        _citationMock = new Mock<ICitationService>();
        _citationMock
            .Setup(c => c.Format(It.IsAny<SearchHit>(), It.IsAny<CitationStyle>()))
            .Returns("[doc.md, §Section]");

        _editorMock = new Mock<IEditorService>();
        _fileMock = new Mock<IFileService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SearchActionsService>>();

        _sut = new SearchActionsService(
            _licenseMock.Object,
            _citationMock.Object,
            _editorMock.Object,
            _fileMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    // ========================================================================
    // Copy Tests
    // ========================================================================

    [Fact]
    public async Task CopyResultsToClipboardAsync_WithResults_CopiesMarkdown()
    {
        // Arrange
        var results = CreateTestResults(3, 10);

        // Act
        var result = await _sut.CopyResultsToClipboardAsync(
            results, CopyFormat.Markdown, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.CharacterCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CopyResultsToClipboardAsync_WhenUnlicensed_ReturnsLicenseRequired()
    {
        // Arrange
        _licenseMock.Setup(l => l.HasFeature(It.IsAny<string>())).Returns(false);
        var results = CreateTestResults(1, 1);

        // Act
        var result = await _sut.CopyResultsToClipboardAsync(
            results, CopyFormat.Markdown, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("license required");
    }

    [Fact]
    public async Task CopyResultsToClipboardAsync_WithNoResults_ReturnsNoResults()
    {
        // Arrange
        var results = CreateTestResults(0, 0);

        // Act
        var result = await _sut.CopyResultsToClipboardAsync(
            results, CopyFormat.Markdown, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No results");
    }

    // ========================================================================
    // Export Tests
    // ========================================================================

    [Fact]
    public async Task ExportResultsAsync_CreatesJsonFile()
    {
        // Arrange
        var results = CreateTestResults(2, 5);
        var options = new ExportOptions("/tmp/export.json", ExportFormat.JSON, true, true);
        string? capturedContent = null;
        _fileMock
            .Setup(f => f.WriteTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, content, _) => capturedContent = content)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExportResultsAsync(results, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.FilePath.Should().Be("/tmp/export.json");
        result.DocumentCount.Should().Be(2);
        result.HitCount.Should().Be(5);
        capturedContent.Should().Contain("\"query\"");
        capturedContent.Should().Contain("\"groups\"");
    }

    [Fact]
    public async Task ExportResultsAsync_CreatesCsvFile()
    {
        // Arrange
        var results = CreateTestResults(1, 3);
        var options = new ExportOptions("/tmp/export.csv", ExportFormat.CSV, true, false);
        string? capturedContent = null;
        _fileMock
            .Setup(f => f.WriteTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, content, _) => capturedContent = content)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExportResultsAsync(results, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        capturedContent.Should().Contain("DocumentPath,DocumentTitle,Score,LineNumber");
    }

    [Fact]
    public async Task ExportResultsAsync_WhenUnlicensed_ReturnsLicenseRequired()
    {
        // Arrange
        _licenseMock.Setup(l => l.HasFeature(It.IsAny<string>())).Returns(false);
        var results = CreateTestResults(1, 1);
        var options = new ExportOptions("/tmp/x.json", ExportFormat.JSON, false, false);

        // Act
        var result = await _sut.ExportResultsAsync(results, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("license required");
    }

    [Fact]
    public async Task ExportResultsAsync_PublishesEvent()
    {
        // Arrange
        var results = CreateTestResults(2, 5);
        results = results with { Query = "test query" };
        var options = new ExportOptions("/tmp/test.json", ExportFormat.JSON, false, false);

        // Act
        await _sut.ExportResultsAsync(results, options, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<SearchResultsExportedEvent>(e =>
                    e.Query == "test query" &&
                    e.Format == ExportFormat.JSON &&
                    e.DocumentCount == 2 &&
                    e.HitCount == 5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportResultsAsync_WhenWriteFails_ReturnsError()
    {
        // Arrange
        var results = CreateTestResults(1, 1);
        var options = new ExportOptions("/readonly/file.json", ExportFormat.JSON, false, false);
        _fileMock
            .Setup(f => f.WriteTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Permission denied"));

        // Act
        var result = await _sut.ExportResultsAsync(results, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Permission denied");
    }

    // ========================================================================
    // Open All Tests
    // ========================================================================

    [Fact]
    public async Task OpenAllDocumentsAsync_OpensUniqueDocuments()
    {
        // Arrange
        var results = CreateTestResultsWithDuplicateDocs();

        // Act
        var result = await _sut.OpenAllDocumentsAsync(results, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentsOpened.Should().Be(3); // Only unique docs
        _editorMock.Verify(e => e.OpenFileAsync(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task OpenAllDocumentsAsync_WhenUnlicensed_ReturnsLicenseRequired()
    {
        // Arrange
        _licenseMock.Setup(l => l.HasFeature(It.IsAny<string>())).Returns(false);
        var results = CreateTestResults(1, 1);

        // Act
        var result = await _sut.OpenAllDocumentsAsync(results, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("license required");
    }

    [Fact]
    public async Task OpenAllDocumentsAsync_ContinuesOnPartialFailure()
    {
        // Arrange
        var results = CreateTestResults(3, 3);
        var callCount = 0;
        _editorMock
            .Setup(e => e.OpenFileAsync(It.IsAny<string>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 2)
                    throw new FileNotFoundException();
                return Task.CompletedTask;
            });

        // Act
        var result = await _sut.OpenAllDocumentsAsync(results, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentsOpened.Should().Be(2); // 1 failed, 2 succeeded
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static GroupedSearchResults CreateTestResults(int docCount, int totalHits)
    {
        var groups = Enumerable.Range(1, docCount)
            .Select(i => new DocumentResultGroup(
                $"/path/doc{i}.md",
                $"Doc {i}",
                totalHits / Math.Max(docCount, 1),
                0.9f - i * 0.1f,
                CreateHits(totalHits / Math.Max(docCount, 1)),
                true))
            .ToList();

        return new GroupedSearchResults(groups, totalHits, docCount);
    }

    private static GroupedSearchResults CreateTestResultsWithDuplicateDocs()
    {
        var groups = new[]
        {
            new DocumentResultGroup("/path/doc1.md", "Doc 1", 2, 0.9f, CreateHits(2), true),
            new DocumentResultGroup("/path/doc2.md", "Doc 2", 1, 0.8f, CreateHits(1), true),
            new DocumentResultGroup("/path/doc3.md", "Doc 3", 1, 0.7f, CreateHits(1), true)
        };

        return new GroupedSearchResults(groups, 4, 3);
    }

    private static IReadOnlyList<SearchHit> CreateHits(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new SearchHit(
                new Document { FilePath = $"/path/doc.md" },
                new Chunk { Content = $"Content {i}", LineNumber = i * 10 },
                0.9f - i * 0.05f))
            .ToList();
}
```

---

## 12. DI Registration

```csharp
// In RAGModule.cs ConfigureServices method
services.AddSingleton<ISearchActionsService, SearchActionsService>();
```

---

## 13. Implementation Checklist

| #         | Task                                                   | Est. Hours | Status |
| :-------- | :----------------------------------------------------- | :--------- | :----- |
| 1         | Create `ISearchActionsService` interface               | 0.5        | [ ]    |
| 2         | Create `ActionResult`, `ExportResult`, `OpenAllResult` | 0.5        | [ ]    |
| 3         | Create `ExportOptions` and `CopyFormat`                | 0.5        | [ ]    |
| 4         | Create `SearchResultsExportedEvent`                    | 0.5        | [ ]    |
| 5         | Implement `SearchActionsService`                       | 2          | [ ]    |
| 6         | Implement Markdown formatter                           | 1.5        | [ ]    |
| 7         | Implement JSON formatter                               | 1          | [ ]    |
| 8         | Implement CSV formatter                                | 1          | [ ]    |
| 9         | Import Plain text formatter                            | 0.5        | [ ]    |
| 10        | Create export format selection dialog                  | 1.5        | [ ]    |
| 11        | Wire up action buttons to panel toolbar                | 1          | [ ]    |
| 12        | Add keyboard shortcuts                                 | 0.5        | [ ]    |
| 13        | Add toast notifications                                | 0.5        | [ ]    |
| 14        | Unit tests for `SearchActionsService`                  | 2          | [ ]    |
| **Total** |                                                        | **13.5**   |        |

---

## 14. Verification Commands

```bash
# ═══════════════════════════════════════════════════════════════════════════
# v0.5.7d Verification
# ═══════════════════════════════════════════════════════════════════════════

# 1. Build solution
dotnet build

# 2. Run unit tests for v0.5.7d
dotnet test --filter "Category=Unit&FullyQualifiedName~v0.5.7d"

# 3. Run SearchActionsService tests
dotnet test --filter "Category=Unit&FullyQualifiedName~SearchActionsServiceTests"

# 4. Manual verification checklist:
# a) Execute search with results
# b) Click "Copy All → Markdown" → Paste in editor, verify formatting
# c) Click "Copy All → Plain Text" → Paste, verify simpler format
# d) Click "Export → JSON" → Select location, verify JSON file created
# e) Click "Export → CSV" → Verify CSV opens in spreadsheet app
# f) Click "Export → Markdown" → Verify .md file created
# g) Click "Open All" → Verify all unique documents open as tabs
# h) Test with no results → Verify "No results" message
# i) Test with unlicensed user → Verify upgrade prompt
# j) Export with options toggled → Verify snippets/citations included/excluded
# k) Test keyboard shortcuts: Ctrl+C, Ctrl+E
```

---

## Document History

| Version | Date       | Author         | Changes                             |
| :------ | :--------- | :------------- | :---------------------------------- |
| 1.0     | 2026-01-27 | Lead Architect | Initial draft                       |
| 1.1     | 2026-01-27 | Lead Architect | Expanded to match project standards |
