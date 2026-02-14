// -----------------------------------------------------------------------
// <copyright file="CursorContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context.Strategies;

/// <summary>
/// Provides a text window around the cursor position as context.
/// Useful for suggestions at the current writing position when no
/// explicit selection is made.
/// </summary>
/// <remarks>
/// <para>
/// This strategy extracts a configurable window of text centered on the cursor
/// position, expanding to word boundaries for readability. A visible cursor
/// marker (<c>▌</c>) is inserted at the exact cursor offset to help agents
/// understand the user's precise position.
/// </para>
/// <para>
/// <strong>Priority:</strong> 80 (High) — Cursor context is important for
/// position-aware suggestions, especially when no text is selected.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.WriterPro"/> or higher.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 500 — The cursor window is intentionally compact
/// to focus on the immediate writing context.
/// </para>
/// <para>
/// <strong>Configurable Window Size:</strong>
/// The character radius can be customized via the <c>WindowSize</c> hint in the
/// <see cref="ContextGatheringRequest"/>. Default is 500 characters (250 before + 250 after).
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2b as part of the Built-in Context Strategies.
/// </para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro)]
public sealed class CursorContextStrategy : ContextStrategyBase
{
    private readonly IEditorService _editorService;

    /// <summary>
    /// Default number of characters to include in the cursor window (total, split before/after).
    /// Can be overridden via the <c>WindowSize</c> hint.
    /// </summary>
    private const int DefaultWindowSize = 500;

    /// <summary>
    /// Hint key for customizing the cursor window size.
    /// </summary>
    internal const string WindowSizeHintKey = "WindowSize";

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorContextStrategy"/> class.
    /// </summary>
    /// <param name="editorService">Editor service for document content access.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="editorService"/> is null.
    /// </exception>
    public CursorContextStrategy(
        IEditorService editorService,
        ITokenCounter tokenCounter,
        ILogger<CursorContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
    }

    /// <inheritdoc />
    public override string StrategyId => "cursor";

    /// <inheritdoc />
    public override string DisplayName => "Cursor Context";

    /// <inheritdoc />
    public override int Priority => StrategyPriority.High; // 80

    /// <inheritdoc />
    public override int MaxTokens => 500;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Cursor context gathering:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates that both document and cursor position are available.</description></item>
    ///   <item><description>Retrieves document content via <see cref="IEditorService"/>.</description></item>
    ///   <item><description>Extracts a text window centered on the cursor position.</description></item>
    ///   <item><description>Expands window boundaries to word boundaries.</description></item>
    ///   <item><description>Inserts a visible cursor marker at the exact offset.</description></item>
    ///   <item><description>Calculates relevance based on cursor position within the document.</description></item>
    /// </list>
    /// </remarks>
    public override Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        // LOGIC: Both document and cursor position are required
        if (!ValidateRequest(request, requireDocument: true, requireCursor: true))
            return Task.FromResult<ContextFragment?>(null);

        // LOGIC: Get configurable window size from hints
        var windowSize = request.GetHint(WindowSizeHintKey, DefaultWindowSize);

        _logger.LogDebug(
            "{Strategy} gathering {WindowSize} chars around position {Pos}",
            StrategyId, windowSize, request.CursorPosition);

        // LOGIC: Retrieve document content
        var manuscript = _editorService.GetDocumentByPath(request.DocumentPath!);
        var content = manuscript?.Content;

        // LOGIC: Fallback to active document text
        if (string.IsNullOrEmpty(content) &&
            string.Equals(request.DocumentPath, _editorService.CurrentDocumentPath, StringComparison.OrdinalIgnoreCase))
        {
            content = _editorService.GetDocumentText();
        }

        if (string.IsNullOrEmpty(content))
        {
            _logger.LogDebug("{Strategy} document content is empty or unavailable", StrategyId);
            return Task.FromResult<ContextFragment?>(null);
        }

        var cursorPos = request.CursorPosition!.Value;

        // LOGIC: Validate cursor position is within document bounds
        if (cursorPos < 0 || cursorPos > content.Length)
        {
            _logger.LogDebug(
                "{Strategy} cursor position {Pos} is out of range (document length: {Length})",
                StrategyId, cursorPos, content.Length);
            return Task.FromResult<ContextFragment?>(null);
        }

        // LOGIC: Calculate window boundaries centered on cursor
        var halfWindow = windowSize / 2;
        var startPos = Math.Max(0, cursorPos - halfWindow);
        var endPos = Math.Min(content.Length, cursorPos + halfWindow);

        // LOGIC: Expand to word boundaries for cleaner extraction
        startPos = ExpandToWordBoundary(content, startPos, Direction.Left);
        endPos = ExpandToWordBoundary(content, endPos, Direction.Right);

        // LOGIC: Extract the text window
        var window = content[startPos..endPos];

        // LOGIC: Insert cursor marker at the correct offset within the window
        var cursorOffset = cursorPos - startPos;
        var result = FormatWithCursorMarker(window, cursorOffset);

        // LOGIC: Apply token truncation if needed
        result = TruncateToMaxTokens(result);

        // LOGIC: Calculate relevance based on document position
        // Higher relevance when cursor is in the middle of the document
        var relevance = CalculateRelevance(cursorPos, content.Length);

        _logger.LogInformation(
            "{Strategy} gathered cursor context ({Length} chars, relevance: {Relevance:F2})",
            StrategyId, result.Length, relevance);

        return Task.FromResult<ContextFragment?>(CreateFragment(result, relevance));
    }

    /// <summary>
    /// Expands a position to the nearest word boundary in the specified direction.
    /// </summary>
    /// <param name="content">The full document content.</param>
    /// <param name="pos">The position to expand.</param>
    /// <param name="direction">Direction to search for word boundary.</param>
    /// <returns>The adjusted position at a word boundary.</returns>
    /// <remarks>
    /// LOGIC: Prevents cutting words in half by expanding to whitespace boundaries.
    /// Handles edge cases at document start/end by returning the original position.
    /// </remarks>
    internal static int ExpandToWordBoundary(string content, int pos, Direction direction)
    {
        if (pos <= 0 || pos >= content.Length) return pos;

        var step = direction == Direction.Left ? -1 : 1;

        // LOGIC: Walk in the specified direction until whitespace is found
        while (pos > 0 && pos < content.Length && !char.IsWhiteSpace(content[pos]))
        {
            pos += step;
        }

        return pos;
    }

    /// <summary>
    /// Inserts a visible cursor marker at the specified offset within the window.
    /// </summary>
    /// <param name="window">The text window around the cursor.</param>
    /// <param name="cursorOffset">The cursor's offset within the window.</param>
    /// <returns>The window text with cursor marker inserted.</returns>
    /// <remarks>
    /// LOGIC: The Unicode vertical bar character (▌) provides a clear visual indicator
    /// of the exact cursor position without being confused with document content.
    /// </remarks>
    internal static string FormatWithCursorMarker(string window, int cursorOffset)
    {
        // LOGIC: Guard against invalid offsets
        if (cursorOffset < 0 || cursorOffset > window.Length)
            return window;

        return window.Insert(cursorOffset, "▌");
    }

    /// <summary>
    /// Calculates relevance score based on cursor position within the document.
    /// </summary>
    /// <param name="cursorPos">The cursor's absolute position.</param>
    /// <param name="contentLength">The total document length.</param>
    /// <returns>A relevance score between 0.6 and 1.0.</returns>
    /// <remarks>
    /// LOGIC: Cursor context near the middle of a document is more likely to be
    /// surrounded by useful context. Very beginning or end positions have slightly
    /// lower relevance since there's less surrounding text available.
    /// </remarks>
    internal static float CalculateRelevance(int cursorPos, int contentLength)
    {
        if (contentLength == 0) return 0.5f;

        // LOGIC: Higher relevance in the middle of the document
        var relativePos = (float)cursorPos / contentLength;
        var distanceFromCenter = Math.Abs(0.5f - relativePos);

        return Math.Max(0.6f, 1.0f - distanceFromCenter);
    }

    /// <summary>
    /// Direction for word boundary expansion.
    /// </summary>
    internal enum Direction
    {
        /// <summary>Expand to the left (toward document start).</summary>
        Left,

        /// <summary>Expand to the right (toward document end).</summary>
        Right
    }
}
