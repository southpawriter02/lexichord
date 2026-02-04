// =============================================================================
// File: ISearchActionsService.cs
// Project: Lexichord.Abstractions
// Description: Interface for search result actions (copy, export, open all).
// =============================================================================
// LOGIC: Defines the service contract for search result operations (v0.5.7d).
//   - CopyResultsAsync: Copies formatted results to clipboard.
//   - ExportResultsAsync: Exports results to a file in various formats.
//   - OpenAllDocumentsAsync: Opens all unique source documents in editor tabs.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.7b: GroupedSearchResults (input for actions).
//   - v0.5.2a: ICitationService (for formatted citations).
//   - v0.1.3a: IEditorService (for opening documents).
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service for performing bulk actions on search results.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ISearchActionsService"/> provides copy, export, and open-all
/// operations for grouped search results from the Reference Panel. It integrates
/// with <see cref="ICitationService"/> for formatted output and
/// <see cref="Editor.IEditorService"/> for document navigation.
/// </para>
/// <para>
/// <b>License Gating:</b> Export and citation-formatted copy operations require
/// Writer Pro tier. Plain text copy is available to all users. The service checks
/// <see cref="ILicenseContext"/> for feature access before executing gated operations.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent access.
/// The service is registered as a singleton in the DI container.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7d as part of the Search Actions feature.
/// </para>
/// </remarks>
public interface ISearchActionsService
{
    /// <summary>
    /// Copies search results to the clipboard in the specified format.
    /// </summary>
    /// <param name="results">
    /// The grouped search results to copy. Must not be null.
    /// </param>
    /// <param name="format">
    /// The desired output format as a <see cref="SearchActionCopyFormat"/> value.
    /// <see cref="SearchActionCopyFormat.CitationFormatted"/> requires Writer Pro tier.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// A <see cref="SearchActionResult"/> indicating success and the number of
    /// results copied, or an error message on failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="results"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>LOGIC: Copy operation flow:</para>
    /// <list type="number">
    ///   <item><description>Validate that results is not null.</description></item>
    ///   <item><description>Check license for CitationFormatted format.</description></item>
    ///   <item><description>Format each result according to the format.</description></item>
    ///   <item><description>Concatenate and copy to system clipboard.</description></item>
    ///   <item><description>Return SearchActionResult with item count and duration.</description></item>
    /// </list>
    /// </remarks>
    Task<SearchActionResult> CopyResultsAsync(
        SearchResultSet results,
        SearchActionCopyFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Exports search results to a file in the specified format.
    /// </summary>
    /// <param name="results">
    /// The grouped search results to export. Must not be null.
    /// </param>
    /// <param name="options">
    /// Export configuration including format, content options, and output path.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// A <see cref="SearchExportResult"/> indicating success, the output path,
    /// bytes written, or an error message on failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="results"/> or <paramref name="options"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>LOGIC: Export operation flow:</para>
    /// <list type="number">
    ///   <item><description>Validate parameters are not null.</description></item>
    ///   <item><description>Check Writer Pro license for export feature.</description></item>
    ///   <item><description>Generate output path if not specified in options.</description></item>
    ///   <item><description>Serialize results according to the format.</description></item>
    ///   <item><description>Write to file and publish SearchResultsExportedEvent.</description></item>
    ///   <item><description>Return SearchExportResult with path and bytes written.</description></item>
    /// </list>
    /// </remarks>
    Task<SearchExportResult> ExportResultsAsync(
        SearchResultSet results,
        SearchExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Opens all unique source documents from the search results in editor tabs.
    /// </summary>
    /// <param name="results">
    /// The grouped search results containing documents to open. Must not be null.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// A <see cref="SearchOpenAllResult"/> indicating how many documents were opened,
    /// skipped (already open), or failed to open.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="results"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>LOGIC: Open-all operation flow:</para>
    /// <list type="number">
    ///   <item><description>Validate that results is not null.</description></item>
    ///   <item><description>Extract unique document paths from groups.</description></item>
    ///   <item><description>For each path, check if already open via IEditorService.</description></item>
    ///   <item><description>Open documents progressively with short delay to avoid UI freeze.</description></item>
    ///   <item><description>Track opened, skipped, and failed counts.</description></item>
    ///   <item><description>Return SearchOpenAllResult with counts and any errors.</description></item>
    /// </list>
    /// <para>
    /// <b>Performance:</b> Documents are opened sequentially with a configurable
    /// delay (default 50ms) to prevent UI freezing when opening many documents.
    /// </para>
    /// </remarks>
    Task<SearchOpenAllResult> OpenAllDocumentsAsync(
        SearchResultSet results,
        CancellationToken ct = default);
}

// =============================================================================
// Types used by ISearchActionsService.
// These are defined here in Abstractions to avoid cross-project dependencies.
// =============================================================================

/// <summary>
/// Copy format options for clipboard operations.
/// </summary>
/// <remarks>
/// <b>Introduced in:</b> v0.5.7d as part of the Search Actions feature.
/// </remarks>
public enum SearchActionCopyFormat
{
    /// <summary>Plain text format with paths and snippets.</summary>
    PlainText = 0,
    /// <summary>Citation-formatted output (Writer Pro required).</summary>
    CitationFormatted = 1,
    /// <summary>Markdown format with file links.</summary>
    Markdown = 2,
    /// <summary>JSON format for programmatic use.</summary>
    Json = 3
}

/// <summary>
/// Export format options for file export operations.
/// </summary>
/// <remarks>
/// <b>Introduced in:</b> v0.5.7d as part of the Search Actions feature.
/// </remarks>
public enum SearchActionExportFormat
{
    /// <summary>JSON format with full metadata.</summary>
    Json = 0,
    /// <summary>CSV format for spreadsheets.</summary>
    Csv = 1,
    /// <summary>Markdown document format.</summary>
    Markdown = 2,
    /// <summary>BibTeX format for references.</summary>
    BibTeX = 3
}

/// <summary>
/// Configuration options for export operations.
/// </summary>
/// <param name="Format">The export file format.</param>
/// <param name="IncludeSnippets">Whether to include matched text snippets.</param>
/// <param name="IncludeCitations">Whether to include formatted citations (Writer Pro).</param>
/// <param name="OutputPath">Optional explicit output path; auto-generated if null.</param>
public record SearchExportOptions(
    SearchActionExportFormat Format,
    bool IncludeSnippets = true,
    bool IncludeCitations = false,
    string? OutputPath = null);

/// <summary>
/// Base result record for search action outcomes.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="ItemCount">Number of items processed.</param>
/// <param name="ElapsedTime">Duration of the operation.</param>
/// <param name="ErrorMessage">Error description, or null if successful.</param>
public record SearchActionResult(
    bool Success,
    int ItemCount,
    TimeSpan ElapsedTime,
    string? ErrorMessage = null)
{
    /// <summary>Creates a successful result.</summary>
    public static SearchActionResult Succeeded(int itemCount, TimeSpan elapsed) =>
        new(true, itemCount, elapsed);

    /// <summary>Creates a failed result.</summary>
    public static SearchActionResult Failed(string error, int itemCount = 0, TimeSpan elapsed = default) =>
        new(false, itemCount, elapsed, error);
}

/// <summary>
/// Result record for export operations.
/// </summary>
/// <param name="Success">Whether the export succeeded.</param>
/// <param name="ItemCount">Number of results exported.</param>
/// <param name="ElapsedTime">Duration of the operation.</param>
/// <param name="OutputPath">File path where results were written.</param>
/// <param name="BytesWritten">Size of the exported file in bytes.</param>
/// <param name="ErrorMessage">Error description, or null if successful.</param>
public record SearchExportResult(
    bool Success,
    int ItemCount,
    TimeSpan ElapsedTime,
    string OutputPath,
    long BytesWritten,
    string? ErrorMessage = null) : SearchActionResult(Success, ItemCount, ElapsedTime, ErrorMessage)
{
    /// <summary>Creates a successful export result.</summary>
    public static SearchExportResult Succeeded(int itemCount, TimeSpan elapsed, string path, long bytes) =>
        new(true, itemCount, elapsed, path, bytes);

    /// <summary>Creates a failed export result.</summary>
    public static SearchExportResult Failed(string error, string path = "", TimeSpan elapsed = default) =>
        new(false, 0, elapsed, path, 0, error);
}

/// <summary>
/// Result record for open-all operations.
/// </summary>
/// <param name="Success">Whether any documents were opened or all were already open.</param>
/// <param name="OpenedCount">Number of documents newly opened.</param>
/// <param name="SkippedCount">Number of documents already open.</param>
/// <param name="FailedPaths">Dictionary of failed paths to error messages.</param>
/// <param name="ElapsedTime">Duration of the operation.</param>
public record SearchOpenAllResult(
    bool Success,
    int OpenedCount,
    int SkippedCount,
    IReadOnlyDictionary<string, string> FailedPaths,
    TimeSpan ElapsedTime)
{
    /// <summary>Gets total documents processed.</summary>
    public int TotalProcessed => OpenedCount + SkippedCount + FailedPaths.Count;

    /// <summary>Gets whether any documents failed to open.</summary>
    public bool HasErrors => FailedPaths.Count > 0;

    /// <summary>Creates a successful result with no failures.</summary>
    public static SearchOpenAllResult Succeeded(int opened, int skipped, TimeSpan elapsed) =>
        new(true, opened, skipped, new Dictionary<string, string>(), elapsed);

    /// <summary>Creates a partial result with some failures.</summary>
    public static SearchOpenAllResult Partial(int opened, int skipped, Dictionary<string, string> failed, TimeSpan elapsed) =>
        new(opened > 0 || skipped > 0, opened, skipped, failed, elapsed);

    /// <summary>Creates a failed result.</summary>
    public static SearchOpenAllResult Failed(string error, TimeSpan elapsed = default) =>
        new(false, 0, 0, new Dictionary<string, string> { { "error", error } }, elapsed);
}

/// <summary>
/// Container for search results to be acted upon.
/// </summary>
/// <remarks>
/// This is a lightweight abstraction over the grouped search results,
/// providing the data needed for copy, export, and open-all operations.
/// </remarks>
/// <param name="Groups">Document groups containing search hits.</param>
/// <param name="TotalHits">Total number of hits across all groups.</param>
/// <param name="TotalDocuments">Number of unique documents in results.</param>
/// <param name="Query">The original search query.</param>
public record SearchResultSet(
    IReadOnlyList<SearchResultGroup> Groups,
    int TotalHits,
    int TotalDocuments,
    string? Query = null);

/// <summary>
/// Group of search results from a single document.
/// </summary>
/// <param name="DocumentPath">Full path to the source document.</param>
/// <param name="DocumentTitle">Display title for the document.</param>
/// <param name="Hits">Search hits from this document.</param>
public record SearchResultGroup(
    string DocumentPath,
    string DocumentTitle,
    IReadOnlyList<SearchHit> Hits);
