// -----------------------------------------------------------------------
// <copyright file="DocumentContextAnalyzer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Models;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Implementation of <see cref="IDocumentContextAnalyzer"/> using Markdig.
/// </summary>
/// <remarks>
/// <para>
/// Parses Markdown documents into an AST using Markdig with advanced extensions
/// and precise source locations. Walks the AST to detect content block types,
/// locate section headings, extract local context, and suggest appropriate agents.
/// </para>
/// <para>
/// AST parsing results are cached via <see cref="ASTCacheProvider"/> and
/// invalidated automatically when <see cref="IEditorService.DocumentChanged"/>
/// fires.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7c as part of the Document-Aware Prompting feature.
/// </para>
/// </remarks>
/// <seealso cref="IDocumentContextAnalyzer"/>
/// <seealso cref="ASTCacheProvider"/>
/// <seealso cref="ContextAwarePromptSelector"/>
internal class DocumentContextAnalyzer : IDocumentContextAnalyzer
{
    // ─────────────────────────────────────────────────────────────────────
    // Dependencies
    // ─────────────────────────────────────────────────────────────────────

    private readonly IEditorService _editorService;
    private readonly IAgentRegistry _agentRegistry;
    private readonly ASTCacheProvider _astCache;
    private readonly ILogger<DocumentContextAnalyzer> _logger;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentContextAnalyzer"/> class.
    /// </summary>
    /// <param name="editorService">
    /// The editor service for document access and change notifications.
    /// </param>
    /// <param name="agentRegistry">
    /// The agent registry for agent suggestion lookups.
    /// </param>
    /// <param name="astCache">
    /// The AST cache provider for cached Markdown parsing.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public DocumentContextAnalyzer(
        IEditorService editorService,
        IAgentRegistry agentRegistry,
        ASTCacheProvider astCache,
        ILogger<DocumentContextAnalyzer> logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _astCache = astCache ?? throw new ArgumentNullException(nameof(astCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Subscribe to document changes for automatic cache invalidation.
        _editorService.DocumentChanged += OnDocumentChanged;

        _logger.LogDebug("DocumentContextAnalyzer initialized, subscribed to DocumentChanged");
    }

    // ─────────────────────────────────────────────────────────────────────
    // IDocumentContextAnalyzer Implementation
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Execution flow:
    /// <list type="number">
    ///   <item>Get or parse the Markdown AST from cache</item>
    ///   <item>Read file content for local context extraction</item>
    ///   <item>Detect content block type at cursor position</item>
    ///   <item>Find the nearest section heading before cursor</item>
    ///   <item>Extract local context (500 chars before and after)</item>
    ///   <item>Suggest an appropriate agent based on content type and heading keywords</item>
    /// </list>
    /// </remarks>
    public async Task<DocumentContext> AnalyzeAtPositionAsync(
        string documentPath,
        int cursorPosition,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Analyzing document at position {Position}: {Path}",
            cursorPosition, documentPath);

        // ─────────────────────────────────────────────────────────────────
        // Get or Parse Document AST
        // ─────────────────────────────────────────────────────────────────
        var document = await _astCache.GetOrParseAsync(documentPath, ct);
        if (document == null)
        {
            _logger.LogWarning("Failed to parse document: {Path}", documentPath);
            return DocumentContext.Empty;
        }

        // LOGIC: Read content for local context extraction.
        var content = await File.ReadAllTextAsync(documentPath, ct);

        // ─────────────────────────────────────────────────────────────────
        // Detect Content Type at Position
        // ─────────────────────────────────────────────────────────────────
        var contentType = DetectContentTypeFromAST(document, cursorPosition);
        _logger.LogDebug("Detected content type: {Type}", contentType);

        // ─────────────────────────────────────────────────────────────────
        // Find Section Heading
        // ─────────────────────────────────────────────────────────────────
        var (sectionHeading, sectionLevel) = FindSectionHeading(document, cursorPosition);
        _logger.LogDebug(
            "Current section: {Section} (level {Level})",
            sectionHeading ?? "(root)", sectionLevel);

        // ─────────────────────────────────────────────────────────────────
        // Extract Local Context
        // ─────────────────────────────────────────────────────────────────
        var localContext = ExtractLocalContext(content, cursorPosition, 500, 500);

        // ─────────────────────────────────────────────────────────────────
        // Suggest Agent Based on Content Type
        // ─────────────────────────────────────────────────────────────────
        var suggestedAgentId = SuggestAgentForContentType(contentType, sectionHeading);

        var context = new DocumentContext(
            CurrentSection: sectionHeading,
            ContentType: contentType,
            LocalContext: localContext,
            CursorPosition: cursorPosition,
            SuggestedAgentId: suggestedAgentId,
            SectionLevel: sectionLevel,
            DocumentPath: documentPath);

        _logger.LogInformation(
            "Document context analyzed: Section={Section}, Type={Type}, Agent={Agent}",
            context.CurrentSection ?? "(root)",
            context.ContentType,
            context.SuggestedAgentId ?? "default");

        return context;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Uses the cached AST for the current document. Returns
    /// <see cref="ContentBlockType.Prose"/> if no document is active or
    /// no cached AST is available.
    /// </remarks>
    public ContentBlockType DetectContentType(int position)
    {
        var documentPath = _editorService.CurrentDocumentPath;
        if (string.IsNullOrEmpty(documentPath))
        {
            _logger.LogDebug(
                "DetectContentType: No active document, returning Prose");
            return ContentBlockType.Prose;
        }

        var document = _astCache.GetCached(documentPath);
        if (document == null)
        {
            _logger.LogDebug(
                "DetectContentType: No cached AST for {Path}, returning Prose",
                documentPath);
            return ContentBlockType.Prose;
        }

        return DetectContentTypeFromAST(document, position);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Uses the cached AST for the current document. Returns <c>null</c>
    /// if no document is active or no cached AST is available.
    /// </remarks>
    public string? GetCurrentSectionHeading(int position)
    {
        var documentPath = _editorService.CurrentDocumentPath;
        if (string.IsNullOrEmpty(documentPath))
        {
            _logger.LogDebug(
                "GetCurrentSectionHeading: No active document");
            return null;
        }

        var document = _astCache.GetCached(documentPath);
        if (document == null)
        {
            _logger.LogDebug(
                "GetCurrentSectionHeading: No cached AST for {Path}",
                documentPath);
            return null;
        }

        var (heading, _) = FindSectionHeading(document, position);
        return heading;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Reads the active document's text from the editor service and
    /// extracts a substring centered around the specified position.
    /// </remarks>
    public string GetLocalContext(int position, int charsBefore = 500, int charsAfter = 500)
    {
        var content = _editorService.GetDocumentText();
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogDebug(
                "GetLocalContext: No document text available");
            return string.Empty;
        }

        return ExtractLocalContext(content, position, charsBefore, charsAfter);
    }

    /// <inheritdoc/>
    public void InvalidateCache(string documentPath)
    {
        _logger.LogDebug("Invalidating AST cache for: {Path}", documentPath);
        _astCache.Invalidate(documentPath);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private: AST Content Type Detection
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the content block type at a position within a parsed AST.
    /// </summary>
    /// <param name="document">The parsed Markdown document.</param>
    /// <param name="position">Zero-based cursor position.</param>
    /// <returns>The detected <see cref="ContentBlockType"/>.</returns>
    /// <remarks>
    /// LOGIC: Iterates through all AST descendants and checks if the cursor
    /// position falls within their source span. Uses pattern matching to map
    /// Markdig block types to <see cref="ContentBlockType"/> values.
    /// </remarks>
    private ContentBlockType DetectContentTypeFromAST(MarkdownDocument document, int position)
    {
        foreach (var block in document.Descendants())
        {
            // LOGIC: Skip blocks that don't contain the cursor position.
            if (!IsPositionInBlock(block, position))
                continue;

            // LOGIC: Map Markdig block types to ContentBlockType enum values.
            return block switch
            {
                FencedCodeBlock or CodeBlock => ContentBlockType.CodeBlock,
                Table => ContentBlockType.Table,
                ListBlock or ListItemBlock => ContentBlockType.List,
                HeadingBlock => ContentBlockType.Heading,
                QuoteBlock => ContentBlockType.Blockquote,
                ThematicBreakBlock => ContentBlockType.Prose,
                _ => ContentBlockType.Prose
            };
        }

        // LOGIC: Default to Prose if no matching block found.
        return ContentBlockType.Prose;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private: Section Heading Detection
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the nearest section heading before the cursor position.
    /// </summary>
    /// <param name="document">The parsed Markdown document.</param>
    /// <param name="position">Zero-based cursor position.</param>
    /// <returns>
    /// A tuple of (heading text, heading level). Returns (null, 0) if no
    /// heading precedes the cursor.
    /// </returns>
    /// <remarks>
    /// LOGIC: Iterates through all <see cref="HeadingBlock"/> descendants
    /// and finds the one whose span end is closest to (but before) the
    /// cursor position. Extracts heading text using inline content traversal
    /// (following the pattern from <c>MarkdownHeaderChunkingStrategy</c>).
    /// </remarks>
    private (string? Heading, int Level) FindSectionHeading(
        MarkdownDocument document,
        int position)
    {
        string? nearestHeading = null;
        int nearestHeadingLevel = 0;
        int nearestHeadingEnd = -1;

        foreach (var block in document.Descendants<HeadingBlock>())
        {
            var headingEnd = block.Span.End;

            // LOGIC: Only consider headings that appear before the cursor.
            if (headingEnd > position)
                continue;

            // LOGIC: Track the nearest heading before the cursor position.
            if (headingEnd > nearestHeadingEnd)
            {
                nearestHeadingEnd = headingEnd;
                nearestHeading = ExtractHeadingText(block);
                nearestHeadingLevel = block.Level;
            }
        }

        return (nearestHeading, nearestHeadingLevel);
    }

    /// <summary>
    /// Extracts plain text from a heading block's inline content.
    /// </summary>
    /// <param name="block">The heading block to extract text from.</param>
    /// <returns>The plain text of the heading, with formatting removed.</returns>
    /// <remarks>
    /// LOGIC: Walks inline content recursively to extract text from
    /// <see cref="LiteralInline"/> and <see cref="CodeInline"/> elements,
    /// handling nested containers like bold, italic, and links.
    /// Pattern follows <c>MarkdownHeaderChunkingStrategy.ExtractHeaderText</c>.
    /// </remarks>
    private static string ExtractHeadingText(HeadingBlock block)
    {
        if (block.Inline == null)
            return string.Empty;

        var textBuilder = new System.Text.StringBuilder();

        foreach (var inline in block.Inline)
        {
            ExtractInlineText(inline, textBuilder);
        }

        return textBuilder.ToString().Trim();
    }

    /// <summary>
    /// Recursively extracts text from an inline element.
    /// </summary>
    /// <param name="inline">The inline element to extract from.</param>
    /// <param name="builder">StringBuilder to append text to.</param>
    private static void ExtractInlineText(Inline inline, System.Text.StringBuilder builder)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.Append(literal.Content);
                break;

            case CodeInline code:
                builder.Append(code.Content);
                break;

            case ContainerInline container:
                foreach (var child in container)
                {
                    ExtractInlineText(child, builder);
                }
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private: Local Context Extraction
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts a context window of text centered around the cursor position.
    /// </summary>
    /// <param name="content">The full document text.</param>
    /// <param name="position">Zero-based cursor position.</param>
    /// <param name="charsBefore">Number of characters before cursor.</param>
    /// <param name="charsAfter">Number of characters after cursor.</param>
    /// <returns>The extracted context substring.</returns>
    /// <remarks>
    /// LOGIC: Clamps the extraction window to document boundaries.
    /// Returns up to <paramref name="charsBefore"/> + <paramref name="charsAfter"/>
    /// characters centered on the cursor position.
    /// </remarks>
    private static string ExtractLocalContext(
        string content,
        int position,
        int charsBefore,
        int charsAfter)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var start = Math.Max(0, position - charsBefore);
        var end = Math.Min(content.Length, position + charsAfter);
        var length = end - start;

        return content.Substring(start, length);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private: Position Checking
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether a cursor position falls within a Markdown block's span.
    /// </summary>
    /// <param name="block">The Markdown object to check.</param>
    /// <param name="position">Zero-based cursor position.</param>
    /// <returns>
    /// <c>true</c> if the position is within the block's source span.
    /// </returns>
    private static bool IsPositionInBlock(MarkdownObject block, int position)
    {
        return block.Span.Start <= position && position <= block.Span.End;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private: Agent Suggestion
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Suggests an appropriate agent based on content type and section heading.
    /// </summary>
    /// <param name="contentType">The detected content block type.</param>
    /// <param name="sectionHeading">The current section heading, if any.</param>
    /// <returns>
    /// The suggested agent ID, or <c>null</c> if no specialized agent applies.
    /// </returns>
    /// <remarks>
    /// LOGIC: First checks section heading keywords for specialized agent matches
    /// (e.g., "code", "implementation", "example" → "code-helper"; "test", "spec" →
    /// "test-helper"). Then falls back to content-type-based suggestions (code blocks
    /// → "code-helper", tables → "data-helper"). Only returns IDs for agents that
    /// are actually registered in the <see cref="IAgentRegistry"/>.
    /// </remarks>
    private string? SuggestAgentForContentType(
        ContentBlockType contentType,
        string? sectionHeading)
    {
        // LOGIC: Check section heading for keyword-based agent suggestions.
        if (!string.IsNullOrEmpty(sectionHeading))
        {
            var headingLower = sectionHeading.ToLowerInvariant();

            if (headingLower.Contains("code") ||
                headingLower.Contains("implementation") ||
                headingLower.Contains("example"))
            {
                return TryGetAgent("code-helper");
            }

            if (headingLower.Contains("test") || headingLower.Contains("spec"))
            {
                return TryGetAgent("test-helper");
            }
        }

        // LOGIC: Suggest agent based on content type.
        return contentType switch
        {
            ContentBlockType.CodeBlock => TryGetAgent("code-helper"),
            ContentBlockType.Table => TryGetAgent("data-helper"),
            _ => null
        };
    }

    /// <summary>
    /// Checks if an agent exists in the registry and returns its ID.
    /// </summary>
    /// <param name="agentId">The agent ID to look up.</param>
    /// <returns>
    /// The agent ID if found in the registry, or <c>null</c> if not available.
    /// </returns>
    private string? TryGetAgent(string agentId)
    {
        return _agentRegistry.AvailableAgents.Any(a => a.AgentId == agentId)
            ? agentId
            : null;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private: Event Handlers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles document change events by invalidating the AST cache.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Document change details.</param>
    /// <remarks>
    /// LOGIC: When a document's content changes, the cached AST is stale
    /// and must be invalidated so the next analysis re-parses the document.
    /// </remarks>
    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.DocumentPath))
        {
            _logger.LogDebug(
                "Document changed, invalidating cache: {Path}", e.DocumentPath);
            InvalidateCache(e.DocumentPath);
        }
    }
}
