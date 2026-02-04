// =============================================================================
// File: PreviewContentBuilder.cs
// Project: Lexichord.Modules.RAG
// Description: Builds preview content from search hits using context expansion.
// =============================================================================
// LOGIC: Coordinates between IContextExpansionService and ISnippetService to
//   produce complete preview content for the split-view preview pane.
//   - Converts TextChunk to RAG Chunk for context expansion compatibility.
//   - Extracts content from expanded chunks (before/after).
//   - Formats breadcrumb by replacing arrow separators with unicode arrows.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.3a: IContextExpansionService, ExpandedChunk, ContextOptions.
//   - v0.5.6a: ISnippetService, HighlightSpan.
//   - v0.5.7c: PreviewContent, PreviewOptions.
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Models;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Builds preview content from search hits using context expansion.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PreviewContentBuilder"/> implements <see cref="IPreviewContentBuilder"/>
/// to produce complete preview content for the split-view preview pane. It coordinates
/// between the context expansion service and snippet service.
/// </para>
/// <para>
/// <b>Algorithm:</b>
/// <list type="number">
///   <item><description>Convert <see cref="TextChunk"/> to RAG <see cref="Chunk"/></description></item>
///   <item><description>Expand context via <see cref="IContextExpansionService"/></description></item>
///   <item><description>Extract content from before/after chunks</description></item>
///   <item><description>Get highlight spans from snippet service</description></item>
///   <item><description>Format breadcrumb with unicode arrows</description></item>
///   <item><description>Assemble <see cref="PreviewContent"/> record</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Performance:</b> Single preview build typically completes in &lt; 100ms,
/// leveraging context expansion caching for repeated requests.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7c as part of the Preview Pane feature.
/// </para>
/// </remarks>
public sealed class PreviewContentBuilder : IPreviewContentBuilder
{
    private readonly IContextExpansionService _contextService;
    private readonly ISnippetService _snippetService;
    private readonly ILogger<PreviewContentBuilder> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewContentBuilder"/>.
    /// </summary>
    /// <param name="contextService">Service for retrieving surrounding context.</param>
    /// <param name="snippetService">Service for extracting highlight spans.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public PreviewContentBuilder(
        IContextExpansionService contextService,
        ISnippetService snippetService,
        ILogger<PreviewContentBuilder> logger)
    {
        _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
        _snippetService = snippetService ?? throw new ArgumentNullException(nameof(snippetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("PreviewContentBuilder initialized");
    }

    /// <inheritdoc />
    public async Task<PreviewContent> BuildAsync(
        SearchHit hit,
        PreviewOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(hit);

        var opts = options ?? PreviewOptions.Default;

        _logger.LogDebug(
            "Building preview for {DocumentPath} with options: LinesBefore={LinesBefore}, LinesAfter={LinesAfter}",
            hit.Document.FilePath, opts.LinesBefore, opts.LinesAfter);

        // Convert TextChunk to RAG Chunk for context expansion
        var ragChunk = ConvertToRagChunk(hit.Chunk, hit.Document.Id);

        // Build context options
        var contextOptions = new ContextOptions(
            PrecedingChunks: opts.LinesBefore > 0 ? 1 : 0,
            FollowingChunks: opts.LinesAfter > 0 ? 1 : 0,
            IncludeHeadings: opts.IncludeBreadcrumb);

        // Expand context
        var expanded = await _contextService.ExpandAsync(ragChunk, contextOptions, ct);

        // Extract content from chunks
        var precedingContent = ExtractContent(expanded.Before);
        var matchedContent = expanded.Core.Content;
        var followingContent = ExtractContent(expanded.After);

        // Get chunk index as line identifier (metadata doesn't have line number)
        var lineNumber = hit.Chunk.Metadata?.Index ?? 0;

        // Format breadcrumb
        var breadcrumb = opts.IncludeBreadcrumb && expanded.HasBreadcrumb
            ? FormatBreadcrumb(expanded.FormatBreadcrumb())
            : null;

        // Get highlight spans from snippet extraction
        var highlights = ExtractHighlights(hit);

        _logger.LogDebug(
            "Preview built: {BeforeLen} before, {MatchLen} matched, {AfterLen} after, {HighlightCount} highlights",
            precedingContent.Length, matchedContent.Length, followingContent.Length, highlights.Count);

        return new PreviewContent(
            DocumentPath: hit.Document.FilePath,
            DocumentTitle: hit.Document.Title ?? Path.GetFileName(hit.Document.FilePath),
            Breadcrumb: breadcrumb,
            PrecedingContext: precedingContent,
            MatchedContent: matchedContent,
            FollowingContext: followingContent,
            LineNumber: lineNumber,
            HighlightSpans: highlights);
    }

    /// <summary>
    /// Converts a <see cref="TextChunk"/> to a RAG <see cref="Chunk"/>.
    /// </summary>
    private static Chunk ConvertToRagChunk(TextChunk textChunk, Guid documentId)
    {
        var metadata = textChunk.Metadata;
        var chunkId = GenerateDeterministicChunkId(documentId, metadata.Index);

        return new Chunk(
            Id: chunkId,
            DocumentId: documentId,
            Content: textChunk.Content,
            Embedding: null,
            ChunkIndex: metadata.Index,
            StartOffset: textChunk.StartOffset,
            EndOffset: textChunk.EndOffset,
            Heading: metadata.Heading,
            HeadingLevel: metadata.Level);
    }

    /// <summary>
    /// Generates a deterministic GUID from document ID and chunk index.
    /// </summary>
    private static Guid GenerateDeterministicChunkId(Guid documentId, int chunkIndex)
    {
        var input = $"{documentId}:{chunkIndex}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash.Take(16).ToArray());
    }

    /// <summary>
    /// Extracts combined content from a list of chunks.
    /// </summary>
    private static string ExtractContent(IReadOnlyList<Chunk> chunks)
    {
        if (chunks.Count == 0)
            return string.Empty;

        return string.Join("\n", chunks.Select(c => c.Content));
    }

    /// <summary>
    /// Formats the breadcrumb with unicode arrow separators.
    /// </summary>
    /// <param name="breadcrumb">Raw breadcrumb string with " &gt; " separators.</param>
    /// <returns>Formatted breadcrumb with " › " separators.</returns>
    private static string? FormatBreadcrumb(string? breadcrumb)
    {
        if (string.IsNullOrWhiteSpace(breadcrumb))
            return null;

        // Replace common separators with nice unicode arrow
        return breadcrumb
            .Replace(" > ", " › ")
            .Replace(" / ", " › ");
    }

    /// <summary>
    /// Extracts highlight spans from a search hit using snippet service.
    /// </summary>
    private IReadOnlyList<HighlightSpan> ExtractHighlights(SearchHit hit)
    {
        try
        {
            // Use snippet service to extract a snippet with highlights
            var snippet = _snippetService.ExtractSnippet(
                hit.Chunk,
                string.Empty, // Query - use empty since we just want positions
                new SnippetOptions { MaxLength = hit.Chunk.Content.Length });

            return snippet.Highlights;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract highlights for chunk");
            return Array.Empty<HighlightSpan>();
        }
    }
}
