// =============================================================================
// File: CitationService.cs
// Project: Lexichord.Modules.RAG
// Description: Creates, formats, and validates source citations from search hits.
// =============================================================================
// LOGIC: Implements ICitationService to provide the core Citation Engine logic.
//   - CreateCitation: Builds a Citation record from a SearchHit by extracting
//     document/chunk data, calculating line numbers from character offsets, and
//     publishing a CitationCreatedEvent via MediatR.
//   - CreateCitations: Batch wrapper that iterates over multiple hits.
//   - FormatCitation: Delegates to style-specific private methods. License gating
//     is applied here: Core users receive DocumentPath only.
//   - ValidateCitationAsync: Compares file LastWriteTimeUtc against IndexedAt.
//   - CalculateLineNumber: Reads file content and counts newlines up to offset.
//     Returns null on any failure (graceful degradation).
//   - FormatInline: [filename.md, §Heading]
//   - FormatFootnote: [^XXXXXXXX]: /path/to/doc.md:line
//   - FormatMarkdown: [Title](file:///path#Lline)
//   - Relative path is computed when workspace root is available.
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Creates, formats, and validates source citations from search results.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CitationService"/> implements <see cref="ICitationService"/> and provides
/// the core logic for the Citation Engine (v0.5.2a). It transforms <see cref="SearchHit"/>
/// instances into <see cref="Citation"/> records with complete provenance information.
/// </para>
/// <para>
/// <b>Line Number Calculation:</b> Line numbers are calculated by reading the source file
/// and counting newline characters from the start of the file to the chunk's start offset.
/// This is a synchronous file I/O operation. If the file is inaccessible or the offset
/// exceeds the file length, the line number is set to null (graceful degradation).
/// </para>
/// <para>
/// <b>License Gating:</b> Citation creation is always performed. License checking occurs
/// at the formatting layer via <see cref="ILicenseContext"/>. Core users see only the
/// document path; Writer Pro+ users see the full formatted citation.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This service is registered as a singleton and is thread-safe.
/// File I/O operations for line number calculation are stateless and isolated.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2a as part of the Citation Engine.
/// </para>
/// </remarks>
public sealed class CitationService : ICitationService
{
    private readonly IWorkspaceService _workspace;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<CitationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CitationService"/> class.
    /// </summary>
    /// <param name="workspace">
    /// Workspace service for resolving relative paths from the workspace root.
    /// </param>
    /// <param name="licenseContext">
    /// License context for tier-based gating of formatted citations.
    /// </param>
    /// <param name="mediator">
    /// MediatR mediator for publishing <see cref="CitationCreatedEvent"/> notifications.
    /// </param>
    /// <param name="logger">
    /// Logger for structured diagnostic output during citation operations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public CitationService(
        IWorkspaceService workspace,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<CitationService> logger)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Citation creation flow:
    /// <list type="number">
    ///   <item><description>Validate that hit is not null.</description></item>
    ///   <item><description>Extract document and chunk references from the hit.</description></item>
    ///   <item><description>Determine document title (frontmatter title or filename fallback).</description></item>
    ///   <item><description>Extract heading from chunk metadata when available.</description></item>
    ///   <item><description>Calculate line number from chunk offset by reading the source file.</description></item>
    ///   <item><description>Compute relative path from workspace root when workspace is open.</description></item>
    ///   <item><description>Build and return the Citation record.</description></item>
    ///   <item><description>Publish CitationCreatedEvent via MediatR (fire-and-forget).</description></item>
    /// </list>
    /// </remarks>
    public Citation CreateCitation(SearchHit hit)
    {
        ArgumentNullException.ThrowIfNull(hit);

        var stopwatch = Stopwatch.StartNew();

        var document = hit.Document;
        var chunk = hit.Chunk;

        _logger.LogDebug(
            "Creating citation for chunk {ChunkId} from {DocumentPath}",
            chunk.Metadata.Index, document.FilePath);

        // LOGIC: Calculate line number from the chunk's character offset.
        // This reads the source file and counts newline characters.
        // Returns null if the file is inaccessible or offset is out of bounds.
        var lineNumber = CalculateLineNumber(document.FilePath, chunk.StartOffset);

        if (lineNumber.HasValue)
        {
            _logger.LogDebug(
                "Calculated line number {LineNumber} from offset {Offset} for {FilePath}",
                lineNumber, chunk.StartOffset, document.FilePath);
        }

        // LOGIC: Determine the document title.
        // Prefer the document's Title field (from frontmatter or metadata).
        // Fall back to the filename extracted from FilePath.
        var documentTitle = document.Title ?? Path.GetFileName(document.FilePath);

        // LOGIC: Extract heading from chunk metadata when present.
        // The Heading field is populated by MarkdownHeaderChunkingStrategy (v0.4.3d).
        var heading = chunk.Metadata?.Heading;

        // LOGIC: Compute workspace-relative path when a workspace is open.
        // This provides a shorter, more readable path for UI display.
        string? relativePath = null;
        if (_workspace.IsWorkspaceOpen && _workspace.CurrentWorkspace is not null)
        {
            var rootPath = _workspace.CurrentWorkspace.RootPath;
            if (document.FilePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = document.FilePath[rootPath.Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        var citation = new Citation(
            ChunkId: document.Id,
            DocumentPath: document.FilePath,
            DocumentTitle: documentTitle,
            StartOffset: chunk.StartOffset,
            EndOffset: chunk.EndOffset,
            Heading: heading,
            LineNumber: lineNumber,
            IndexedAt: document.IndexedAt ?? DateTime.UtcNow)
        {
            RelativePath = relativePath
        };

        stopwatch.Stop();

        _logger.LogInformation(
            "Citation created for {DocumentTitle} in {ElapsedMs}ms",
            documentTitle, stopwatch.ElapsedMilliseconds);

        // LOGIC: Publish CitationCreatedEvent for downstream consumers.
        // Fire-and-forget to avoid blocking the citation creation flow.
        _ = _mediator.Publish(new CitationCreatedEvent(citation, DateTime.UtcNow));

        return citation;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Iterates over input hits, delegating to <see cref="CreateCitation"/>
    /// for each non-null entry. Null entries are skipped with a warning log.
    /// </remarks>
    public IReadOnlyList<Citation> CreateCitations(IEnumerable<SearchHit> hits)
    {
        ArgumentNullException.ThrowIfNull(hits);

        var citations = new List<Citation>();

        foreach (var hit in hits)
        {
            if (hit is null)
            {
                _logger.LogWarning("Skipping null SearchHit in batch citation creation");
                continue;
            }

            citations.Add(CreateCitation(hit));
        }

        _logger.LogDebug(
            "Created {CitationCount} citations from batch of search hits",
            citations.Count);

        return citations.AsReadOnly();
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: License gating is applied at the formatting layer.
    /// Core users receive only the document path (basic attribution).
    /// Writer Pro+ users receive the full formatted citation in the requested style.
    /// </remarks>
    public string FormatCitation(Citation citation, CitationStyle style)
    {
        ArgumentNullException.ThrowIfNull(citation);

        _logger.LogDebug(
            "Formatting citation for {DocumentPath} as {Style}",
            citation.DocumentPath, style);

        // LOGIC: License gate — Core users see document path only.
        // Writer Pro+ users get the formatted citation in the requested style.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.Citation))
        {
            _logger.LogDebug(
                "Citation formatting gated — returning document path for {DocumentPath}",
                citation.DocumentPath);
            return citation.DocumentPath;
        }

        return style switch
        {
            CitationStyle.Inline => FormatInline(citation),
            CitationStyle.Footnote => FormatFootnote(citation),
            CitationStyle.Markdown => FormatMarkdown(citation),
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Basic validation for v0.5.2a:
    /// <list type="number">
    ///   <item><description>Check if the source file exists at DocumentPath.</description></item>
    ///   <item><description>Compare file's LastWriteTimeUtc against Citation.IndexedAt.</description></item>
    /// </list>
    /// Full validation with hash comparison is deferred to ICitationValidator (v0.5.2c).
    /// </remarks>
    public async Task<bool> ValidateCitationAsync(Citation citation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(citation);

        _logger.LogDebug(
            "Validating citation for {DocumentPath}",
            citation.DocumentPath);

        if (!File.Exists(citation.DocumentPath))
        {
            _logger.LogWarning(
                "Citation invalid: {DocumentPath} not found",
                citation.DocumentPath);
            return false;
        }

        var fileInfo = new FileInfo(citation.DocumentPath);
        var isValid = fileInfo.LastWriteTimeUtc <= citation.IndexedAt;

        if (!isValid)
        {
            _logger.LogWarning(
                "Citation stale: {DocumentPath} modified at {ModifiedAt}, indexed at {IndexedAt}",
                citation.DocumentPath, fileInfo.LastWriteTimeUtc, citation.IndexedAt);
        }
        else
        {
            _logger.LogDebug(
                "Citation valid: {DocumentPath} unchanged since {IndexedAt}",
                citation.DocumentPath, citation.IndexedAt);
        }

        return await Task.FromResult(isValid);
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    /// <summary>
    /// Calculates the 1-indexed line number for a given character offset in a file.
    /// </summary>
    /// <param name="filePath">
    /// Absolute path to the source file to read.
    /// </param>
    /// <param name="charOffset">
    /// Zero-based character offset from the start of the file.
    /// </param>
    /// <returns>
    /// The 1-indexed line number, or null if the file cannot be read or the
    /// offset exceeds the file length.
    /// </returns>
    /// <remarks>
    /// LOGIC: Reads the entire file content into memory and counts newline characters
    /// from position 0 to <paramref name="charOffset"/>. Line numbering starts at 1.
    /// Any exception (file not found, access denied, I/O error) results in null
    /// with a warning-level log entry.
    /// </remarks>
    internal int? CalculateLineNumber(string filePath, int charOffset)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning(
                    "Cannot calculate line number: file not found at {FilePath}",
                    filePath);
                return null;
            }

            // LOGIC: Read entire file content for line counting.
            // This is a synchronous operation suitable for the citation creation flow
            // where the file is expected to be a text document (typically < 1MB).
            var content = File.ReadAllText(filePath);

            if (charOffset >= content.Length)
            {
                _logger.LogWarning(
                    "Offset {Offset} exceeds file length {Length} for {FilePath}",
                    charOffset, content.Length, filePath);
                return null;
            }

            // LOGIC: Count newlines from start of file to the offset position.
            // Line numbers are 1-indexed: the first line is line 1.
            var lineNumber = 1;
            for (var i = 0; i < charOffset && i < content.Length; i++)
            {
                if (content[i] == '\n')
                    lineNumber++;
            }

            return lineNumber;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to calculate line number for {FilePath} at offset {Offset}",
                filePath, charOffset);
            return null;
        }
    }

    /// <summary>
    /// Formats a citation in the inline style: <c>[filename.md, §Heading]</c>.
    /// </summary>
    /// <param name="citation">The citation to format.</param>
    /// <returns>
    /// The inline-formatted string. When the citation has no heading context,
    /// the §Heading suffix is omitted: <c>[filename.md]</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Uses the short filename (not full path) for conciseness.
    /// The section symbol (§) precedes the heading when present.
    /// </remarks>
    private static string FormatInline(Citation citation)
    {
        // LOGIC: Format: [document.md, §Heading] or [document.md]
        var heading = citation.HasHeading
            ? $", §{citation.Heading}"
            : string.Empty;
        return $"[{citation.FileName}{heading}]";
    }

    /// <summary>
    /// Formats a citation in the footnote style: <c>[^XXXXXXXX]: /path/to/doc.md:line</c>.
    /// </summary>
    /// <param name="citation">The citation to format.</param>
    /// <returns>
    /// The footnote-formatted string. Uses the first 8 characters of the ChunkId
    /// as the footnote identifier. When the citation has no line number, the
    /// <c>:line</c> suffix is omitted.
    /// </returns>
    /// <remarks>
    /// LOGIC: The footnote identifier uses the "N" format specifier to produce
    /// a hex string without hyphens, then takes the first 8 characters for brevity.
    /// </remarks>
    private static string FormatFootnote(Citation citation)
    {
        // LOGIC: Format: [^XXXXXXXX]: /path/to/doc.md:line or [^XXXXXXXX]: /path/to/doc.md
        var line = citation.HasLineNumber
            ? $":{citation.LineNumber}"
            : string.Empty;
        var shortId = citation.ChunkId.ToString("N")[..8];
        return $"[^{shortId}]: {citation.DocumentPath}{line}";
    }

    /// <summary>
    /// Formats a citation in the Markdown link style: <c>[Title](file:///path#Lline)</c>.
    /// </summary>
    /// <param name="citation">The citation to format.</param>
    /// <returns>
    /// The Markdown-formatted string. Uses the document title as link text.
    /// When the citation has a line number, appends <c>#Lline</c> as a fragment.
    /// Spaces in the path are percent-encoded as <c>%20</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Produces a standard Markdown link with a <c>file://</c> scheme URI.
    /// The fragment identifier follows the convention used by GitHub and VS Code
    /// for line-level linking (<c>#L42</c>).
    /// </remarks>
    private static string FormatMarkdown(Citation citation)
    {
        // LOGIC: Format: [Title](file:///path#Lline) or [Title](file:///path)
        var fragment = citation.HasLineNumber
            ? $"#L{citation.LineNumber}"
            : string.Empty;
        var escapedPath = citation.DocumentPath.Replace(" ", "%20");
        return $"[{citation.DocumentTitle}](file://{escapedPath}{fragment})";
    }
}
