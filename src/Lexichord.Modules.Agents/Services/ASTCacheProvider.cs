// -----------------------------------------------------------------------
// <copyright file="ASTCacheProvider.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Markdig;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Provides cached Markdown AST per document, invalidating on changes.
/// </summary>
/// <remarks>
/// <para>
/// Maintains a <see cref="ConcurrentDictionary{TKey, TValue}"/> of parsed
/// <see cref="MarkdownDocument"/> instances keyed by document path. The cache
/// avoids redundant re-parsing when multiple queries target the same document
/// without intervening edits.
/// </para>
/// <para>
/// The <see cref="Pipeline"/> is configured with <c>UseAdvancedExtensions()</c>
/// for table/pipe-table support and <c>UsePreciseSourceLocation()</c> for
/// accurate <see cref="Markdig.Syntax.MarkdownObject.Span"/> positions.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All public methods are thread-safe due to the use
/// of <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7c as part of the Document-Aware Prompting feature.
/// </para>
/// </remarks>
/// <seealso cref="DocumentContextAnalyzer"/>
/// <seealso cref="IDocumentContextAnalyzer"/>
internal class ASTCacheProvider
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Concurrent cache of parsed Markdown ASTs keyed by document path.
    /// </summary>
    private readonly ConcurrentDictionary<string, MarkdownDocument> _cache = new();

    /// <summary>
    /// Logger for diagnostic output.
    /// </summary>
    private readonly ILogger<ASTCacheProvider> _logger;

    // ─────────────────────────────────────────────────────────────────────
    // Static Pipeline
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shared Markdig pipeline configured with advanced extensions and
    /// precise source location tracking.
    /// </summary>
    /// <remarks>
    /// LOGIC: <c>UseAdvancedExtensions()</c> enables pipe tables, grid tables,
    /// footnotes, task lists, and other extensions needed for accurate content
    /// type detection. <c>UsePreciseSourceLocation()</c> ensures
    /// <see cref="Markdig.Syntax.MarkdownObject.Span"/> positions are accurate
    /// for cursor-based lookups.
    /// </remarks>
    internal static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UsePreciseSourceLocation()
        .Build();

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="ASTCacheProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public ASTCacheProvider(ILogger<ASTCacheProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ASTCacheProvider initialized");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the cached AST or parses the document from disk.
    /// </summary>
    /// <param name="documentPath">Path to the Markdown document.</param>
    /// <param name="ct">Cancellation token for async file I/O.</param>
    /// <returns>
    /// The parsed <see cref="MarkdownDocument"/>, or <c>null</c> if the
    /// file could not be read or parsed.
    /// </returns>
    /// <remarks>
    /// LOGIC: First checks the concurrent cache for an existing parse result.
    /// On cache miss, reads the file content asynchronously and parses it
    /// using the shared <see cref="Pipeline"/>. The result is stored in the
    /// cache for subsequent lookups.
    /// </remarks>
    public async Task<MarkdownDocument?> GetOrParseAsync(
        string documentPath,
        CancellationToken ct = default)
    {
        // LOGIC: Check cache first for a previously parsed AST.
        if (_cache.TryGetValue(documentPath, out var cached))
        {
            _logger.LogDebug("AST cache hit: {Path}", documentPath);
            return cached;
        }

        _logger.LogDebug("AST cache miss, parsing: {Path}", documentPath);

        try
        {
            // LOGIC: Read file content and parse the Markdown AST.
            var content = await File.ReadAllTextAsync(documentPath, ct);
            var document = Markdown.Parse(content, Pipeline);

            // LOGIC: Store in cache for subsequent lookups.
            _cache[documentPath] = document;
            _logger.LogDebug("AST cached for: {Path}", documentPath);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse document: {Path}", documentPath);
            return null;
        }
    }

    /// <summary>
    /// Gets the cached AST without triggering a parse.
    /// </summary>
    /// <param name="documentPath">Path to the Markdown document.</param>
    /// <returns>
    /// The cached <see cref="MarkdownDocument"/>, or <c>null</c> if no
    /// cached version exists for the specified path.
    /// </returns>
    /// <remarks>
    /// LOGIC: Returns only from cache, never reads from disk.
    /// Used by <see cref="DocumentContextAnalyzer.DetectContentType"/>
    /// and <see cref="DocumentContextAnalyzer.GetCurrentSectionHeading"/>
    /// which need fast, synchronous lookups.
    /// </remarks>
    public MarkdownDocument? GetCached(string documentPath)
    {
        return _cache.TryGetValue(documentPath, out var cached) ? cached : null;
    }

    /// <summary>
    /// Invalidates the cache for the specified document.
    /// </summary>
    /// <param name="documentPath">Path to the document to invalidate.</param>
    /// <remarks>
    /// LOGIC: Removes the cached AST so the next access triggers a fresh parse.
    /// Called by <see cref="DocumentContextAnalyzer"/> when it receives a
    /// <c>DocumentChanged</c> event from the editor service.
    /// </remarks>
    public void Invalidate(string documentPath)
    {
        if (_cache.TryRemove(documentPath, out _))
        {
            _logger.LogDebug("AST cache invalidated: {Path}", documentPath);
        }
    }

    /// <summary>
    /// Clears all cached ASTs.
    /// </summary>
    /// <remarks>
    /// LOGIC: Removes all entries from the cache. Useful for memory reclamation
    /// or when the editor context changes substantially (e.g., project switch).
    /// </remarks>
    public void ClearAll()
    {
        _cache.Clear();
        _logger.LogDebug("All AST caches cleared");
    }
}
