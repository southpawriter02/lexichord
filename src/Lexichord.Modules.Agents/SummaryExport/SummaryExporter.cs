// -----------------------------------------------------------------------
// <copyright file="SummaryExporter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implementation of ISummaryExporter for multi-destination export (v0.7.6c).
//   Exports summaries and metadata to Panel, Frontmatter, File, Clipboard, InlineInsert.
//
//   Export Flow:
//     1. License check (WriterPro required)
//     2. Validate options
//     3. Publish SummaryExportStartedEvent
//     4. Execute destination-specific handler
//     5. Publish SummaryExportedEvent or SummaryExportFailedEvent
//     6. Return ExportResult
//
//   3-Catch Error Pattern:
//     - OperationCanceledException (user cancellation)
//     - TimeoutException (operation timeout)
//     - Exception (generic failure)
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Agents.SummaryExport;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.SummaryExport.Events;
using Lexichord.Modules.Agents.SummaryExport.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Agents.SummaryExport;

/// <summary>
/// Implementation of <see cref="ISummaryExporter"/> for multi-destination summary export.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This service orchestrates summary export to multiple destinations:
/// <list type="bullet">
/// <item><description><see cref="ExportDestination.Panel"/>: Opens Summary Panel UI</description></item>
/// <item><description><see cref="ExportDestination.Frontmatter"/>: Injects into YAML frontmatter</description></item>
/// <item><description><see cref="ExportDestination.File"/>: Creates standalone .summary.md file</description></item>
/// <item><description><see cref="ExportDestination.Clipboard"/>: Copies to system clipboard</description></item>
/// <item><description><see cref="ExportDestination.InlineInsert"/>: Inserts at cursor position</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// All export methods require WriterPro tier. Lower tiers receive
/// <see cref="SummaryExportResult.Failed"/> with an upgrade message.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This service is thread-safe. All state is passed via request/result parameters.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
public sealed partial class SummaryExporter : ISummaryExporter
{
    private readonly IFileService _fileService;
    private readonly IEditorService _editorService;
    private readonly IClipboardService _clipboardService;
    private readonly ISummaryCacheService _cacheService;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<SummaryExporter> _logger;

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryExporter"/> class.
    /// </summary>
    /// <param name="fileService">File service for file operations.</param>
    /// <param name="editorService">Editor service for cursor operations.</param>
    /// <param name="clipboardService">Clipboard service for copy operations.</param>
    /// <param name="cacheService">Cache service for summary persistence.</param>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="mediator">MediatR for event publishing.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public SummaryExporter(
        IFileService fileService,
        IEditorService editorService,
        IClipboardService clipboardService,
        ISummaryCacheService cacheService,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<SummaryExporter> logger)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(licenseContext);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);

        _fileService = fileService;
        _editorService = editorService;
        _clipboardService = clipboardService;
        _cacheService = cacheService;
        _licenseContext = licenseContext;
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SummaryExportResult> ExportAsync(
        SummarizationResult summary,
        string sourceDocumentPath,
        SummaryExportOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(sourceDocumentPath);

        var stopwatch = Stopwatch.StartNew();
        var destination = options.Destination;

        _logger.LogDebug("Starting export to {Destination} for {DocumentPath}", destination, sourceDocumentPath);

        // LOGIC: License check (WriterPro required)
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SummaryExport))
        {
            _logger.LogWarning("Export denied: WriterPro license required");
            return SummaryExportResult.Failed(destination, "Upgrade to WriterPro to use export features.");
        }

        // Validate options
        try
        {
            options.Validate();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid export options");
            return SummaryExportResult.Failed(destination, ex.Message);
        }

        // Publish started event
        await _mediator.Publish(SummaryExportStartedEvent.Create(destination, sourceDocumentPath), ct);

        try
        {
            var result = destination switch
            {
                ExportDestination.Panel => await ExportToPanelAsync(summary, null, sourceDocumentPath, ct),
                ExportDestination.Frontmatter => await ExportToFrontmatterAsync(summary, null, sourceDocumentPath, options, ct),
                ExportDestination.File => await ExportToFileAsync(summary, null, sourceDocumentPath, options, ct),
                ExportDestination.Clipboard => await ExportToClipboardAsync(summary, options, ct),
                ExportDestination.InlineInsert => await ExportToInlineAsync(summary, options, ct),
                _ => SummaryExportResult.Failed(destination, $"Unsupported destination: {destination}")
            };

            stopwatch.Stop();

            if (result.Success)
            {
                _logger.LogInformation(
                    "Export completed: {Destination}, {BytesWritten} bytes, {Duration}ms",
                    destination, result.BytesWritten ?? 0, stopwatch.ElapsedMilliseconds);

                await _mediator.Publish(SummaryExportedEvent.Create(
                    destination,
                    sourceDocumentPath,
                    result.OutputPath,
                    result.BytesWritten,
                    result.CharactersWritten,
                    result.DidOverwrite,
                    stopwatch.Elapsed), ct);
            }
            else
            {
                _logger.LogWarning("Export failed: {Destination}, {Error}", destination, result.ErrorMessage);
                await _mediator.Publish(SummaryExportFailedEvent.Create(destination, sourceDocumentPath, result.ErrorMessage ?? "Unknown error"), ct);
            }

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("Export cancelled by user");
            await _mediator.Publish(SummaryExportFailedEvent.Create(destination, sourceDocumentPath, "Export cancelled."), ct);
            return SummaryExportResult.Failed(destination, "Export cancelled.");
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Export timed out");
            await _mediator.Publish(SummaryExportFailedEvent.Create(destination, sourceDocumentPath, "Export timed out."), ct);
            return SummaryExportResult.Failed(destination, "Export timed out. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed with error");
            await _mediator.Publish(SummaryExportFailedEvent.Create(destination, sourceDocumentPath, ex.Message), ct);
            return SummaryExportResult.Failed(destination, $"Export failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<SummaryExportResult> ExportMetadataAsync(
        DocumentMetadata metadata,
        string sourceDocumentPath,
        SummaryExportOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(sourceDocumentPath);

        var destination = options.Destination;

        _logger.LogDebug("Starting metadata export to {Destination} for {DocumentPath}", destination, sourceDocumentPath);

        // LOGIC: License check
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SummaryExport))
        {
            _logger.LogWarning("Export denied: WriterPro license required");
            return SummaryExportResult.Failed(destination, "Upgrade to WriterPro to use export features.");
        }

        // For metadata-only export, we only support Frontmatter destination
        if (destination != ExportDestination.Frontmatter)
        {
            return SummaryExportResult.Failed(destination, "Metadata-only export is only supported for Frontmatter destination.");
        }

        return await ExportToFrontmatterAsync(null, metadata, sourceDocumentPath, options, ct);
    }

    /// <inheritdoc/>
    public async Task<SummaryExportResult> UpdateFrontmatterAsync(
        string documentPath,
        SummarizationResult? summary,
        DocumentMetadata? metadata,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);

        if (summary == null && metadata == null)
        {
            throw new ArgumentException("At least one of summary or metadata must be provided.");
        }

        _logger.LogDebug("Updating frontmatter for {DocumentPath}", documentPath);

        // LOGIC: License check
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SummaryExport))
        {
            _logger.LogWarning("Frontmatter update denied: WriterPro license required");
            return SummaryExportResult.Failed(ExportDestination.Frontmatter, "Upgrade to WriterPro to use export features.");
        }

        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.Frontmatter,
            Fields = FrontmatterFields.All,
            Overwrite = true
        };

        return await ExportToFrontmatterAsync(summary, metadata, documentPath, options, ct);
    }

    /// <inheritdoc/>
    public async Task<CachedSummary?> GetCachedSummaryAsync(string documentPath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);

        _logger.LogDebug("Looking up cached summary for {DocumentPath}", documentPath);
        return await _cacheService.GetAsync(documentPath, ct);
    }

    /// <inheritdoc/>
    public async Task CacheSummaryAsync(
        string documentPath,
        SummarizationResult summary,
        DocumentMetadata? metadata,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(summary);

        _logger.LogDebug("Caching summary for {DocumentPath}", documentPath);
        await _cacheService.SetAsync(documentPath, summary, metadata, ct);
    }

    /// <inheritdoc/>
    public async Task ClearCacheAsync(string documentPath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);

        _logger.LogDebug("Clearing cache for {DocumentPath}", documentPath);
        await _cacheService.ClearAsync(documentPath, ct);
    }

    /// <inheritdoc/>
    public async Task ShowInPanelAsync(
        SummarizationResult summary,
        DocumentMetadata? metadata,
        string sourceDocumentPath)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(sourceDocumentPath);

        _logger.LogDebug("Opening Summary Panel for {DocumentPath}", sourceDocumentPath);

        // LOGIC: Publish panel opened event (panel display is handled by UI layer)
        await _mediator.Publish(SummaryPanelOpenedEvent.Create(
            sourceDocumentPath,
            summary.Mode,
            metadata != null,
            wasCached: false));
    }

    #region Private Export Handlers

    /// <summary>
    /// Exports summary to the Summary Panel UI.
    /// </summary>
    private async Task<SummaryExportResult> ExportToPanelAsync(
        SummarizationResult summary,
        DocumentMetadata? metadata,
        string sourceDocumentPath,
        CancellationToken ct)
    {
        // Cache the summary for later retrieval
        await _cacheService.SetAsync(sourceDocumentPath, summary, metadata, ct);

        // Show in panel (UI layer handles the actual display)
        await ShowInPanelAsync(summary, metadata, sourceDocumentPath);

        return SummaryExportResult.Succeeded(ExportDestination.Panel);
    }

    /// <summary>
    /// Exports summary and/or metadata to YAML frontmatter.
    /// </summary>
    private async Task<SummaryExportResult> ExportToFrontmatterAsync(
        SummarizationResult? summary,
        DocumentMetadata? metadata,
        string documentPath,
        SummaryExportOptions options,
        CancellationToken ct)
    {
        var loadResult = await _fileService.LoadAsync(documentPath, null, ct);
        if (!loadResult.Success)
        {
            return SummaryExportResult.Failed(ExportDestination.Frontmatter, $"Failed to load document: {loadResult.Error?.Message}");
        }

        var content = loadResult.Content ?? string.Empty;

        // Parse existing frontmatter
        var (existingFrontmatter, bodyContent) = ParseFrontmatter(content);

        // Merge new data
        var mergedFrontmatter = new Dictionary<string, object>(existingFrontmatter);
        var didOverwrite = false;

        if (summary != null && options.Fields.HasFlag(FrontmatterFields.Abstract))
        {
            var summarySection = new Dictionary<string, object>
            {
                ["text"] = summary.Summary,
                ["mode"] = summary.Mode.ToString().ToLowerInvariant(),
                ["word_count"] = summary.SummaryWordCount,
                ["compression_ratio"] = Math.Round(summary.CompressionRatio, 2)
            };

            if (options.Fields.HasFlag(FrontmatterFields.GeneratedAt))
            {
                summarySection["generated_at"] = summary.GeneratedAt.ToString("O");
            }

            if (summary.Model != null)
            {
                summarySection["model"] = summary.Model;
            }

            if (summary.Items != null)
            {
                summarySection["items"] = summary.Items.ToList();
            }

            if (mergedFrontmatter.ContainsKey("summary"))
            {
                didOverwrite = true;
                _logger.LogDebug("Overwriting existing summary in frontmatter");
            }

            mergedFrontmatter["summary"] = summarySection;
        }

        if (metadata != null)
        {
            var metadataSection = new Dictionary<string, object>();

            if (options.Fields.HasFlag(FrontmatterFields.ReadingTime))
            {
                metadataSection["reading_time_minutes"] = metadata.EstimatedReadingMinutes;
            }

            if (options.Fields.HasFlag(FrontmatterFields.Category) && metadata.PrimaryCategory != null)
            {
                metadataSection["category"] = metadata.PrimaryCategory;
            }

            if (options.Fields.HasFlag(FrontmatterFields.Audience) && metadata.TargetAudience != null)
            {
                metadataSection["target_audience"] = metadata.TargetAudience;
            }

            if (options.Fields.HasFlag(FrontmatterFields.Tags) && metadata.SuggestedTags.Count > 0)
            {
                metadataSection["tags"] = metadata.SuggestedTags.ToList();
            }

            if (options.Fields.HasFlag(FrontmatterFields.KeyTerms) && metadata.KeyTerms.Count > 0)
            {
                metadataSection["key_terms"] = metadata.KeyTerms
                    .Take(5)
                    .Select(t => new Dictionary<string, object>
                    {
                        ["term"] = t.Term,
                        ["importance"] = Math.Round(t.Importance, 2)
                    })
                    .ToList();
            }

            metadataSection["complexity_score"] = metadata.ComplexityScore;
            metadataSection["document_type"] = metadata.DocumentType.ToString().ToLowerInvariant();

            if (metadataSection.Count > 0)
            {
                if (mergedFrontmatter.ContainsKey("metadata"))
                {
                    didOverwrite = true;
                    _logger.LogDebug("Overwriting existing metadata in frontmatter");
                }

                mergedFrontmatter["metadata"] = metadataSection;
            }
        }

        // Serialize and write
        var yaml = YamlSerializer.Serialize(mergedFrontmatter);
        var newContent = $"---\n{yaml}---\n{bodyContent}";

        var saveResult = await _fileService.SaveAsync(documentPath, newContent, null, ct);
        if (!saveResult.Success)
        {
            return SummaryExportResult.Failed(ExportDestination.Frontmatter, $"Failed to save document: {saveResult.Error?.Message}");
        }

        _logger.LogInformation("Frontmatter updated: {FieldCount} sections added/updated", mergedFrontmatter.Count);

        return SummaryExportResult.Succeeded(ExportDestination.Frontmatter) with
        {
            BytesWritten = saveResult.BytesWritten,
            DidOverwrite = didOverwrite
        };
    }

    /// <summary>
    /// Exports summary to a standalone file.
    /// </summary>
    private async Task<SummaryExportResult> ExportToFileAsync(
        SummarizationResult summary,
        DocumentMetadata? metadata,
        string sourceDocumentPath,
        SummaryExportOptions options,
        CancellationToken ct)
    {
        // Determine output path
        var outputPath = options.OutputPath;
        if (string.IsNullOrEmpty(outputPath))
        {
            var directory = Path.GetDirectoryName(sourceDocumentPath) ?? ".";
            var fileName = Path.GetFileNameWithoutExtension(sourceDocumentPath);
            outputPath = Path.Combine(directory, $"{fileName}.summary.md");
        }

        // Check if file exists
        var fileExists = _fileService.Exists(outputPath);
        if (fileExists && !options.Overwrite)
        {
            return SummaryExportResult.Failed(ExportDestination.File, $"File already exists: {outputPath}. Enable Overwrite to replace.");
        }

        // Build content using template
        var content = options.ExportTemplate != null
            ? ApplyTemplate(options.ExportTemplate, summary, metadata, sourceDocumentPath)
            : BuildDefaultFileContent(summary, metadata, sourceDocumentPath, options);

        // Write file
        var saveResult = await _fileService.SaveAsAsync(outputPath, content, null, ct);
        if (!saveResult.Success)
        {
            return SummaryExportResult.Failed(ExportDestination.File, $"Failed to save file: {saveResult.Error?.Message}");
        }

        _logger.LogInformation("Summary exported to file: {OutputPath} ({BytesWritten} bytes)", outputPath, saveResult.BytesWritten);

        return SummaryExportResult.Succeeded(ExportDestination.File, outputPath) with
        {
            BytesWritten = saveResult.BytesWritten,
            DidOverwrite = fileExists
        };
    }

    /// <summary>
    /// Exports summary to the clipboard.
    /// </summary>
    private async Task<SummaryExportResult> ExportToClipboardAsync(
        SummarizationResult summary,
        SummaryExportOptions options,
        CancellationToken ct)
    {
        var content = options.ClipboardAsMarkdown
            ? summary.Summary
            : StripMarkdown(summary.Summary);

        await _clipboardService.SetTextAsync(content, ct);

        _logger.LogInformation("Summary copied to clipboard: {CharacterCount} chars", content.Length);

        return SummaryExportResult.Succeeded(ExportDestination.Clipboard) with
        {
            CharactersWritten = content.Length
        };
    }

    /// <summary>
    /// Exports summary to the cursor position.
    /// </summary>
    private async Task<SummaryExportResult> ExportToInlineAsync(
        SummarizationResult summary,
        SummaryExportOptions options,
        CancellationToken ct)
    {
        var content = summary.Summary;

        // Wrap in callout block if requested
        if (options.UseCalloutBlock)
        {
            content = FormatAsCallout(content, options.CalloutType);
        }

        // Insert at cursor position
        _editorService.BeginUndoGroup("Insert Summary");
        try
        {
            var offset = _editorService.CaretOffset;
            _editorService.InsertText(offset, content + "\n\n");
            _editorService.CaretOffset = offset + content.Length + 2;
        }
        finally
        {
            _editorService.EndUndoGroup();
        }

        _logger.LogInformation("Summary inserted at cursor: {CharacterCount} chars", content.Length);

        return await Task.FromResult(SummaryExportResult.Succeeded(ExportDestination.InlineInsert) with
        {
            CharactersWritten = content.Length
        });
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Parses YAML frontmatter from document content.
    /// </summary>
    private (Dictionary<string, object> Frontmatter, string Body) ParseFrontmatter(string content)
    {
        if (!content.StartsWith("---"))
        {
            return (new Dictionary<string, object>(), content);
        }

        var endIndex = content.IndexOf("---", 3);
        if (endIndex < 0)
        {
            return (new Dictionary<string, object>(), content);
        }

        var yamlContent = content.Substring(3, endIndex - 3).Trim();
        var bodyContent = content.Substring(endIndex + 3).TrimStart('\n', '\r');

        try
        {
            var frontmatter = YamlDeserializer.Deserialize<Dictionary<string, object>>(yamlContent)
                ?? new Dictionary<string, object>();
            return (frontmatter, bodyContent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse existing frontmatter, will create new");
            return (new Dictionary<string, object>(), content);
        }
    }

    /// <summary>
    /// Builds the default file export content.
    /// </summary>
    private static string BuildDefaultFileContent(
        SummarizationResult summary,
        DocumentMetadata? metadata,
        string sourceDocumentPath,
        SummaryExportOptions options)
    {
        var sb = new StringBuilder();
        var documentName = Path.GetFileName(sourceDocumentPath);

        sb.AppendLine($"# Summary: {documentName}");
        sb.AppendLine();

        if (options.IncludeSourceReference)
        {
            sb.AppendLine($"**Source:** [{documentName}]({sourceDocumentPath})");
            sb.AppendLine($"**Generated:** {summary.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
            if (summary.Model != null)
            {
                sb.AppendLine($"**Model:** {summary.Model}");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine(summary.Summary);
        sb.AppendLine();

        if (options.IncludeMetadata && metadata != null)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Metadata");
            sb.AppendLine();
            sb.AppendLine("| Property | Value |");
            sb.AppendLine("|:---------|:------|");
            sb.AppendLine($"| Reading Time | {metadata.EstimatedReadingMinutes} min |");
            sb.AppendLine($"| Complexity | {metadata.ComplexityScore}/10 |");
            sb.AppendLine($"| Type | {metadata.DocumentType} |");

            if (metadata.TargetAudience != null)
            {
                sb.AppendLine($"| Audience | {metadata.TargetAudience} |");
            }

            if (metadata.PrimaryCategory != null)
            {
                sb.AppendLine($"| Category | {metadata.PrimaryCategory} |");
            }

            sb.AppendLine();

            if (metadata.KeyTerms.Count > 0)
            {
                sb.AppendLine("### Key Terms");
                sb.AppendLine();
                foreach (var term in metadata.KeyTerms.Take(5))
                {
                    sb.AppendLine($"- **{term.Term}** ({term.Importance:P0})");
                }
                sb.AppendLine();
            }

            if (metadata.SuggestedTags.Count > 0)
            {
                sb.AppendLine("### Tags");
                sb.AppendLine();
                sb.AppendLine(string.Join(" ", metadata.SuggestedTags.Select(t => $"`{t}`")));
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated by Lexichord Summarizer Agent*");

        return sb.ToString();
    }

    /// <summary>
    /// Applies a custom template to the summary.
    /// </summary>
    private static string ApplyTemplate(
        string template,
        SummarizationResult summary,
        DocumentMetadata? metadata,
        string sourceDocumentPath)
    {
        var documentName = Path.GetFileName(sourceDocumentPath);

        var result = template
            .Replace("{{document_title}}", documentName)
            .Replace("{{source_name}}", documentName)
            .Replace("{{source_path}}", sourceDocumentPath)
            .Replace("{{generated_at}}", summary.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{{model}}", summary.Model ?? "unknown")
            .Replace("{{summary}}", summary.Summary)
            .Replace("{{reading_time}}", metadata?.EstimatedReadingMinutes.ToString() ?? "N/A")
            .Replace("{{complexity}}", metadata?.ComplexityScore.ToString() ?? "N/A")
            .Replace("{{document_type}}", metadata?.DocumentType.ToString() ?? "Unknown")
            .Replace("{{target_audience}}", metadata?.TargetAudience ?? "General")
            .Replace("{{category}}", metadata?.PrimaryCategory ?? "Uncategorized");

        return result;
    }

    /// <summary>
    /// Formats content as a callout/admonition block.
    /// </summary>
    private static string FormatAsCallout(string content, string calloutType)
    {
        var lines = content.Split('\n');
        var sb = new StringBuilder();

        sb.AppendLine($"> [!{calloutType}] Summary");
        foreach (var line in lines)
        {
            sb.AppendLine($"> {line}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Strips Markdown formatting from text.
    /// </summary>
    [GeneratedRegex(@"\*\*([^*]+)\*\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"\*([^*]+)\*")]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"^[•\-]\s*", RegexOptions.Multiline)]
    private static partial Regex BulletRegex();

    private static string StripMarkdown(string text)
    {
        var result = BoldRegex().Replace(text, "$1");
        result = ItalicRegex().Replace(result, "$1");
        result = BulletRegex().Replace(result, "- ");
        return result;
    }

    #endregion
}
