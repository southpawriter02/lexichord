// =============================================================================
// File: SearchActionsService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of bulk actions on search results (copy, export, open).
// =============================================================================
// LOGIC: Provides copy, export, and open-all operations for search results (v0.5.7d).
//   - CopyResultsAsync: Formats results and copies to system clipboard.
//   - ExportResultsAsync: Serializes results to file in various formats.
//   - OpenAllDocumentsAsync: Opens unique documents via IEditorService.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.7d: ISearchActionsService, SearchResultSet, search result types.
//   - v0.5.2a: ICitationService (for formatted citations).
//   - v0.1.3a: IEditorService (for opening documents).
//   - v0.2.3a: ILicenseContext (for feature gating).
// =============================================================================

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of <see cref="ISearchActionsService"/> providing copy, export,
/// and open-all operations for grouped search results.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchActionsService"/> integrates with <see cref="ICitationService"/>
/// for formatted citations, <see cref="IEditorService"/> for document navigation,
/// and the system clipboard for copy operations.
/// </para>
/// <para>
/// <b>License Gating:</b> Export and citation-formatted copy require Writer Pro.
/// Plain text copy and open-all are available to all users.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe and registered as singleton.
/// Clipboard operations must be invoked on the UI thread.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7d as part of the Search Actions feature.
/// </para>
/// </remarks>
public sealed class SearchActionsService : ISearchActionsService
{
    private readonly ILicenseContext _licenseContext;
    private readonly ICitationService _citationService;
    private readonly IEditorService _editorService;
    private readonly IMediator _mediator;
    private readonly ILogger<SearchActionsService> _logger;

    // Configuration constants
    private const int OpenDelayMs = 50;
    private const string ExportFeatureCode = "RAG-SEARCH-ACTIONS";

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchActionsService"/> class.
    /// </summary>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="citationService">Citation service for formatted output.</param>
    /// <param name="editorService">Editor service for opening documents.</param>
    /// <param name="mediator">MediatR for event publishing.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public SearchActionsService(
        ILicenseContext licenseContext,
        ICitationService citationService,
        IEditorService editorService,
        IMediator mediator,
        ILogger<SearchActionsService> logger)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _citationService = citationService ?? throw new ArgumentNullException(nameof(citationService));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("SearchActionsService initialized");
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Copy operation implementation:
    /// <list type="number">
    ///   <item><description>Validate results is not null.</description></item>
    ///   <item><description>Check license for CitationFormatted format.</description></item>
    ///   <item><description>Format each hit according to the format.</description></item>
    ///   <item><description>Write to system clipboard.</description></item>
    ///   <item><description>Return result with item count and duration.</description></item>
    /// </list>
    /// </remarks>
    public async Task<SearchActionResult> CopyResultsAsync(
        SearchResultSet results,
        SearchActionCopyFormat format,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(results);

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "CopyResultsAsync starting: {TotalHits} hits, format={Format}",
            results.TotalHits, format);

        try
        {
            // LOGIC: CitationFormatted requires Writer Pro license.
            if (format == SearchActionCopyFormat.CitationFormatted)
            {
            if (!_licenseContext.IsFeatureEnabled(ExportFeatureCode))
                {
                    _logger.LogWarning("Copy with citation format denied - license check failed");
                    return SearchActionResult.Failed(
                        "Citation formatting requires Writer Pro subscription",
                        0, sw.Elapsed);
                }
            }

            // LOGIC: Format all hits according to the requested format.
            var content = FormatResults(results, format);
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogDebug("No content to copy - results empty");
                return SearchActionResult.Succeeded(0, sw.Elapsed);
            }

            // LOGIC: Copy to system clipboard.
            var clipboard = GetClipboard();
            if (clipboard is null)
            {
                _logger.LogWarning("Clipboard not available - no main window");
                return SearchActionResult.Failed("Clipboard not available", 0, sw.Elapsed);
            }

            await clipboard.SetTextAsync(content);
            sw.Stop();

            _logger.LogInformation(
                "CopyResultsAsync completed: {TotalHits} hits copied in {Elapsed}ms",
                results.TotalHits, sw.ElapsedMilliseconds);

            return SearchActionResult.Succeeded(results.TotalHits, sw.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CopyResultsAsync failed");
            return SearchActionResult.Failed(ex.Message, 0, sw.Elapsed);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Export operation implementation:
    /// <list type="number">
    ///   <item><description>Validate parameters.</description></item>
    ///   <item><description>Check Writer Pro license.</description></item>
    ///   <item><description>Generate output path if not specified.</description></item>
    ///   <item><description>Serialize results to the target format.</description></item>
    ///   <item><description>Write to file and publish event.</description></item>
    /// </list>
    /// </remarks>
    public async Task<SearchExportResult> ExportResultsAsync(
        SearchResultSet results,
        SearchExportOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(options);

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "ExportResultsAsync starting: {TotalHits} hits, format={Format}",
            results.TotalHits, options.Format);

        // LOGIC: Export requires Writer Pro license.
        if (!_licenseContext.IsFeatureEnabled(ExportFeatureCode))
        {
            _logger.LogWarning("Export denied - license check failed");
            var failedResult = SearchExportResult.Failed(
                "Export requires Writer Pro subscription",
                options.OutputPath ?? "", sw.Elapsed);

            await PublishExportEventAsync(failedResult, options.Format, results.Query);
            return failedResult;
        }

        try
        {
            // LOGIC: Generate output path if not specified.
            var outputPath = options.OutputPath ?? GenerateOutputPath(results.Query, options.Format);

            // LOGIC: Serialize results according to format.
            var content = SerializeResults(results, options);

            // LOGIC: Write to file.
            var bytes = Encoding.UTF8.GetBytes(content);
            await File.WriteAllBytesAsync(outputPath, bytes, ct);

            sw.Stop();
            var result = SearchExportResult.Succeeded(
                results.TotalHits, sw.Elapsed, outputPath, bytes.Length);

            _logger.LogInformation(
                "ExportResultsAsync completed: {TotalHits} hits exported to {Path} ({Bytes} bytes) in {Elapsed}ms",
                results.TotalHits, outputPath, bytes.Length, sw.ElapsedMilliseconds);

            await PublishExportEventAsync(result, options.Format, results.Query);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "ExportResultsAsync failed");
            var result = SearchExportResult.Failed(
                ex.Message, options.OutputPath ?? "", sw.Elapsed);

            await PublishExportEventAsync(result, options.Format, results.Query);
            return result;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Open-all operation implementation:
    /// <list type="number">
    ///   <item><description>Validate results.</description></item>
    ///   <item><description>Extract unique document paths.</description></item>
    ///   <item><description>Check which are already open.</description></item>
    ///   <item><description>Open remaining documents progressively.</description></item>
    ///   <item><description>Return counts of opened, skipped, and failed.</description></item>
    /// </list>
    /// </remarks>
    public async Task<SearchOpenAllResult> OpenAllDocumentsAsync(
        SearchResultSet results,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(results);

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "OpenAllDocumentsAsync starting: {TotalDocuments} documents",
            results.TotalDocuments);

        if (results.Groups.Count == 0)
        {
            _logger.LogDebug("No documents to open - results empty");
            return SearchOpenAllResult.Succeeded(0, 0, sw.Elapsed);
        }

        // LOGIC: Extract unique document paths from groups.
        var uniquePaths = results.Groups
            .Select(g => g.DocumentPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var opened = 0;
        var skipped = 0;
        var failed = new Dictionary<string, string>();

        try
        {
            foreach (var path in uniquePaths)
            {
                ct.ThrowIfCancellationRequested();

                // LOGIC: Check if document is already open.
                var existing = _editorService.GetDocumentByPath(path);
                if (existing is not null)
                {
                    _logger.LogDebug("Skipping already-open document: {Path}", path);
                    skipped++;
                    continue;
                }

                try
                {
                    // LOGIC: Open document with progressive delay to avoid UI freeze.
                    await _editorService.OpenDocumentAsync(path);
                    opened++;
                    _logger.LogDebug("Opened document: {Path}", path);

                    // LOGIC: Small delay between opens to prevent UI freezing.
                    if (opened < uniquePaths.Count)
                    {
                        await Task.Delay(OpenDelayMs, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to open document: {Path}", path);
                    failed[path] = ex.Message;
                }
            }

            sw.Stop();
            _logger.LogInformation(
                "OpenAllDocumentsAsync completed: {Opened} opened, {Skipped} skipped, {Failed} failed in {Elapsed}ms",
                opened, skipped, failed.Count, sw.ElapsedMilliseconds);

            if (failed.Count > 0)
            {
                return SearchOpenAllResult.Partial(opened, skipped, failed, sw.Elapsed);
            }

            return SearchOpenAllResult.Succeeded(opened, skipped, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OpenAllDocumentsAsync cancelled after opening {Opened} documents", opened);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAllDocumentsAsync failed unexpectedly");
            return SearchOpenAllResult.Failed(ex.Message, sw.Elapsed);
        }
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Formats search results according to the specified copy format.
    /// </summary>
    private string FormatResults(SearchResultSet results, SearchActionCopyFormat format)
    {
        var sb = new StringBuilder();

        foreach (var group in results.Groups)
        {
            foreach (var hit in group.Hits)
            {
                switch (format)
                {
                    case SearchActionCopyFormat.PlainText:
                        sb.AppendLine(group.DocumentPath);
                        sb.AppendLine($"  {hit.Chunk.Content.Trim()}");
                        sb.AppendLine();
                        break;

                    case SearchActionCopyFormat.CitationFormatted:
                        var citation = _citationService.CreateCitation(hit);
                        var formatted = _citationService.FormatCitation(citation, CitationStyle.Markdown);
                        sb.AppendLine(formatted);
                        break;

                    case SearchActionCopyFormat.Markdown:
                        var lineRef = hit.Chunk.StartOffset > 0 ? $"#L{hit.Chunk.Metadata.Index + 1}" : "";
                        sb.AppendLine($"[{group.DocumentTitle}](file://{group.DocumentPath}{lineRef})");
                        sb.AppendLine($"> {hit.Chunk.Content.Trim()}");
                        sb.AppendLine();
                        break;

                    case SearchActionCopyFormat.Json:
                        // JSON format handled separately
                        break;
                }
            }
        }

        // LOGIC: JSON format returns the entire result set as a single JSON object.
        if (format == SearchActionCopyFormat.Json)
        {
            return SerializeToJson(results, false, false);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Serializes search results for export according to format.
    /// </summary>
    private string SerializeResults(SearchResultSet results, SearchExportOptions options)
    {
        return options.Format switch
        {
            SearchActionExportFormat.Json => SerializeToJson(results, options.IncludeSnippets, options.IncludeCitations),
            SearchActionExportFormat.Csv => SerializeToCsv(results, options.IncludeSnippets),
            SearchActionExportFormat.Markdown => SerializeToMarkdown(results, options.IncludeSnippets, options.IncludeCitations),
            SearchActionExportFormat.BibTeX => SerializeToBibTeX(results),
            _ => SerializeToJson(results, options.IncludeSnippets, options.IncludeCitations)
        };
    }

    /// <summary>
    /// Serializes results to JSON format.
    /// </summary>
    private string SerializeToJson(SearchResultSet results, bool includeSnippets, bool includeCitations)
    {
        var exportData = new
        {
            query = results.Query,
            exportedAt = DateTime.UtcNow.ToString("O"),
            totalHits = results.TotalHits,
            totalDocuments = results.TotalDocuments,
            results = results.Groups.SelectMany(g => g.Hits.Select(h => new
            {
                documentPath = g.DocumentPath,
                documentTitle = g.DocumentTitle,
                score = h.Score,
                snippet = includeSnippets ? h.Chunk.Content.Trim() : null,
                citation = includeCitations ? _citationService.FormatCitation(
                    _citationService.CreateCitation(h), CitationStyle.Inline) : null
            })).ToList()
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Serializes results to CSV format.
    /// </summary>
    private string SerializeToCsv(SearchResultSet results, bool includeSnippets)
    {
        var sb = new StringBuilder();
        sb.AppendLine(includeSnippets ? "Path,Title,Score,Snippet" : "Path,Title,Score");

        foreach (var group in results.Groups)
        {
            foreach (var hit in group.Hits)
            {
                var path = EscapeCsv(group.DocumentPath);
                var title = EscapeCsv(group.DocumentTitle);
                var score = hit.Score.ToString("F4");

                if (includeSnippets)
                {
                    var snippet = EscapeCsv(hit.Chunk.Content.Trim());
                    sb.AppendLine($"{path},{title},{score},{snippet}");
                }
                else
                {
                    sb.AppendLine($"{path},{title},{score}");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a value for CSV format per RFC 4180.
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    /// <summary>
    /// Serializes results to Markdown format.
    /// </summary>
    private string SerializeToMarkdown(SearchResultSet results, bool includeSnippets, bool includeCitations)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Search Results Export");
        sb.AppendLine();
        sb.AppendLine($"**Query:** {results.Query ?? "(none)"}");
        sb.AppendLine($"**Exported:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Total Hits:** {results.TotalHits}");
        sb.AppendLine($"**Documents:** {results.TotalDocuments}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var group in results.Groups)
        {
            sb.AppendLine($"## [{group.DocumentTitle}](file://{group.DocumentPath})");
            sb.AppendLine();

            foreach (var hit in group.Hits)
            {
                sb.AppendLine($"- **Score:** {hit.Score:F4}");

                if (includeSnippets)
                {
                    sb.AppendLine($"  > {hit.Chunk.Content.Trim()}");
                }

                if (includeCitations)
                {
                    var citation = _citationService.CreateCitation(hit);
                    var formatted = _citationService.FormatCitation(citation, CitationStyle.Inline);
                    sb.AppendLine($"  - Citation: {formatted}");
                }

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Serializes results to BibTeX format.
    /// </summary>
    private static string SerializeToBibTeX(SearchResultSet results)
    {
        var sb = new StringBuilder();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
        var index = 1;

        foreach (var group in results.Groups)
        {
            var key = $"lexichord{index++}";
            var filename = Path.GetFileNameWithoutExtension(group.DocumentPath);

            sb.AppendLine($"@misc{{{key},");
            sb.AppendLine($"  title = {{{filename}}},");
            sb.AppendLine($"  howpublished = {{\\url{{file://{group.DocumentPath}}}}},");
            sb.AppendLine($"  note = {{Accessed {timestamp}}},");
            sb.AppendLine($"  year = {{{DateTime.Now.Year}}}");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates an output path for export based on query and format.
    /// </summary>
    private static string GenerateOutputPath(string? query, SearchActionExportFormat format)
    {
        var sanitizedQuery = string.IsNullOrWhiteSpace(query)
            ? "search-results"
            : new string(query.Take(30).Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray())
                .Trim().Replace(' ', '-');

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var extension = format switch
        {
            SearchActionExportFormat.Json => ".json",
            SearchActionExportFormat.Csv => ".csv",
            SearchActionExportFormat.Markdown => ".md",
            SearchActionExportFormat.BibTeX => ".bib",
            _ => ".txt"
        };

        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var exportDir = Path.Combine(downloadsPath, "Downloads");

        if (!Directory.Exists(exportDir))
        {
            exportDir = downloadsPath;
        }

        return Path.Combine(exportDir, $"{sanitizedQuery}-{timestamp}{extension}");
    }

    /// <summary>
    /// Publishes a SearchResultsExportedEvent via MediatR.
    /// </summary>
    private async Task PublishExportEventAsync(SearchExportResult result, SearchActionExportFormat format, string? query)
    {
        try
        {
            var evt = SearchResultsExportedEvent.FromResult(result, format, query);
            await _mediator.Publish(evt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SearchResultsExportedEvent");
        }
    }

    /// <summary>
    /// Gets the system clipboard from the application lifetime.
    /// </summary>
    /// <returns>The clipboard, or null if unavailable.</returns>
    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }
        return null;
    }
}
