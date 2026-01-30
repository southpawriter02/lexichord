using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// Service for navigating to specific locations in documents.
/// </summary>
/// <remarks>
/// LOGIC: Coordinates navigation from external sources (Problems Panel,
/// search results, bookmarks) to editor locations.
///
/// Navigation Flow:
/// 1. Resolve document by ID from IEditorService
/// 2. Activate document tab if not active
/// 3. Scroll to target line (centered in viewport)
/// 4. Position caret at target column
/// 5. Apply temporary highlight animation
/// 6. Focus editor for immediate editing
///
/// Thread Safety: All operations marshal to UI thread via ManuscriptViewModel.
///
/// Version: v0.2.6b
/// </remarks>
public sealed class EditorNavigationService : IEditorNavigationService
{
    private readonly IEditorService _editorService;
    private readonly ILogger<EditorNavigationService> _logger;

    /// <summary>
    /// Default highlight duration for navigation targets.
    /// </summary>
    private static readonly TimeSpan DefaultHighlightDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorNavigationService"/> class.
    /// </summary>
    /// <param name="editorService">The editor service for document lookup and activation.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when editorService or logger is null.</exception>
    public EditorNavigationService(
        IEditorService editorService,
        ILogger<EditorNavigationService> logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("EditorNavigationService initialized");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Orchestrates the complete navigation flow:
    /// 1. Validate parameters
    /// 2. Get document from IEditorService.GetDocumentById
    /// 3. Activate document tab via IEditorService.ActivateDocumentAsync
    /// 4. Set caret to target line/column (includes scroll)
    /// 5. Calculate offset and apply highlight animation
    /// 6. Return success/failure result
    ///
    /// Version: v0.2.6b
    /// </remarks>
    public async Task<NavigationResult> NavigateToViolationAsync(
        string documentId,
        int line,
        int column,
        int length,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "NavigateToViolationAsync called: DocumentId={DocumentId}, Line={Line}, Column={Column}, Length={Length}",
            documentId, line, column, length);

        // LOGIC: Validate parameters
        if (string.IsNullOrEmpty(documentId))
        {
            _logger.LogWarning("Navigation failed: DocumentId is null or empty");
            return NavigationResult.Failed("Document ID is required");
        }

        if (line < 1)
        {
            _logger.LogWarning("Navigation failed: Invalid line number {Line}", line);
            return NavigationResult.Failed($"Invalid line number: {line}");
        }

        if (column < 1)
        {
            _logger.LogWarning("Navigation failed: Invalid column number {Column}", column);
            return NavigationResult.Failed($"Invalid column number: {column}");
        }

        // LOGIC: Check for cancellation
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Navigation cancelled before starting");
            return NavigationResult.Failed("Navigation was cancelled");
        }

        // LOGIC: Step 1 - Get document by ID
        var document = _editorService.GetDocumentById(documentId);
        if (document is null)
        {
            _logger.LogWarning("Navigation failed: Document not found for ID {DocumentId}", documentId);
            return NavigationResult.Failed($"Document not found: {documentId}");
        }

        _logger.LogDebug("Found document: {Title}", document.Title);

        // LOGIC: Step 2 - Activate document tab
        var activated = await _editorService.ActivateDocumentAsync(document);
        if (!activated)
        {
            _logger.LogWarning("Navigation failed: Could not activate document {DocumentId}", documentId);
            return NavigationResult.Failed($"Could not activate document: {documentId}");
        }

        _logger.LogDebug("Document activated: {Title}", document.Title);

        // LOGIC: Check for cancellation after activation
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Navigation cancelled after activation");
            return NavigationResult.Failed("Navigation was cancelled");
        }

        // LOGIC: Step 3 - Set caret position (includes scroll)
        await document.SetCaretPositionAsync(line, column);
        _logger.LogDebug("Caret positioned at {Line}:{Column}", line, column);

        // LOGIC: Step 4 - Calculate offset and apply highlight
        if (length > 0)
        {
            var startOffset = CalculateOffset(document.Content, line, column);
            if (startOffset >= 0 && startOffset + length <= document.Content.Length)
            {
                await document.HighlightSpanAsync(startOffset, length, DefaultHighlightDuration);
                _logger.LogDebug("Highlight applied at offset {Offset} for {Length} characters", startOffset, length);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping highlight: offset {Offset} + length {Length} exceeds content length {ContentLength}",
                    startOffset, length, document.Content.Length);
            }
        }

        _logger.LogInformation(
            "Navigation successful to {DocumentId} at {Line}:{Column}",
            documentId, line, column);

        return NavigationResult.Succeeded(documentId);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Alternative navigation using absolute offsets.
    /// Converts offset to line/column and delegates to NavigateToViolationAsync.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    public async Task<NavigationResult> NavigateToOffsetAsync(
        string documentId,
        int startOffset,
        int length,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "NavigateToOffsetAsync called: DocumentId={DocumentId}, StartOffset={StartOffset}, Length={Length}",
            documentId, startOffset, length);

        // LOGIC: Validate parameters
        if (string.IsNullOrEmpty(documentId))
        {
            return NavigationResult.Failed("Document ID is required");
        }

        if (startOffset < 0)
        {
            return NavigationResult.Failed($"Invalid start offset: {startOffset}");
        }

        // LOGIC: Get document to calculate line/column from offset
        var document = _editorService.GetDocumentById(documentId);
        if (document is null)
        {
            return NavigationResult.Failed($"Document not found: {documentId}");
        }

        // LOGIC: Convert offset to line/column
        var (line, column) = CalculatePositionFromOffset(document.Content, startOffset);

        // LOGIC: Delegate to main navigation method
        return await NavigateToViolationAsync(documentId, line, column, length, cancellationToken);
    }

    #region Private Helpers

    /// <summary>
    /// Calculates the character offset for a given line and column.
    /// </summary>
    /// <param name="content">The document content.</param>
    /// <param name="line">Target line (1-indexed).</param>
    /// <param name="column">Target column (1-indexed).</param>
    /// <returns>The 0-indexed character offset.</returns>
    private static int CalculateOffset(string content, int line, int column)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var offset = 0;
        var currentLine = 1;

        foreach (var ch in content)
        {
            if (currentLine == line)
            {
                // LOGIC: We're on the target line, add column offset (1-indexed)
                return offset + column - 1;
            }

            if (ch == '\n')
            {
                currentLine++;
            }
            offset++;
        }

        // LOGIC: If we reached end without finding line, return end of content
        return offset;
    }

    /// <summary>
    /// Calculates line and column from a character offset.
    /// </summary>
    /// <param name="content">The document content.</param>
    /// <param name="offset">The 0-indexed character offset.</param>
    /// <returns>Tuple of (line, column), both 1-indexed.</returns>
    private static (int Line, int Column) CalculatePositionFromOffset(string content, int offset)
    {
        if (string.IsNullOrEmpty(content) || offset <= 0)
            return (1, 1);

        var line = 1;
        var column = 1;
        var currentOffset = 0;

        foreach (var ch in content)
        {
            if (currentOffset >= offset)
                break;

            if (ch == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            currentOffset++;
        }

        return (line, column);
    }

    #endregion
}
