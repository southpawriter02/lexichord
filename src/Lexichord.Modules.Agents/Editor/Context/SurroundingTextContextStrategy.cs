// -----------------------------------------------------------------------
// <copyright file="SurroundingTextContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Gathers surrounding paragraph context for the Editor Agent's
//   rewrite operations (v0.7.3c). Provides text before and after the
//   user's selection to help the LLM maintain tone and topic consistency
//   in its rewrites.
//
//   Flow:
//     1. Validate that DocumentPath and CursorPosition are present
//     2. Retrieve document content via IEditorService
//     3. Split document into paragraphs (double newline boundaries)
//     4. Locate the paragraph containing the cursor position
//     5. Collect up to PreferredParagraphsBefore/After paragraphs
//     6. Format with [SELECTION IS HERE] marker between before/after
//     7. Truncate to MaxTokens and return as ContextFragment
//
//   The SourceId "surrounding-text" maps to the {{surrounding_context}}
//   Mustache variable in EditorAgent.BuildPromptVariables() (v0.7.3b).
//
//   Thread safety:
//     - No shared mutable state; all variables are per-invocation
//     - IEditorService calls are thread-safe per contract
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Context;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor.Context;

/// <summary>
/// Gathers surrounding paragraph context for the Editor Agent's rewrite operations.
/// Provides text before and after the user's selection to maintain tone and topic
/// consistency in AI-generated rewrites.
/// </summary>
/// <remarks>
/// <para>
/// This strategy retrieves the full document text via <see cref="IEditorService"/>,
/// splits it into paragraphs, locates the paragraph containing the cursor, and
/// returns the surrounding paragraphs formatted with a <c>[SELECTION IS HERE]</c>
/// marker. This gives the LLM context about what comes before and after the
/// text being rewritten.
/// </para>
/// <para>
/// <strong>Priority:</strong> <see cref="StrategyPriority.Critical"/> (100) —
/// Surrounding text is the most important context for producing rewrites that
/// match the document's existing tone and flow.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.WriterPro"/> or higher.
/// Context-aware rewriting is a WriterPro feature.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 1500 — Sufficient for 2-3 paragraphs of surrounding
/// context while leaving room for style rules and terminology in the budget.
/// </para>
/// <para>
/// <strong>Document Access Pattern:</strong> Follows the same pattern as
/// <see cref="Lexichord.Modules.Agents.Context.Strategies.SelectionContextStrategy"/>:
/// attempts <see cref="IEditorService.GetDocumentByPath"/> first, falls back to
/// <see cref="IEditorService.GetDocumentText()"/> for the active document.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.3c as part of Context-Aware Rewriting.
/// </para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro)]
public sealed class SurroundingTextContextStrategy : ContextStrategyBase
{
    private readonly IEditorService _editorService;

    /// <summary>
    /// Maximum character count to include before token counting.
    /// Acts as a fast pre-filter to avoid expensive token counting on very large documents.
    /// </summary>
    private const int MaxSurroundingChars = 3000;

    /// <summary>
    /// Number of paragraphs to include before the selection paragraph.
    /// </summary>
    private const int PreferredParagraphsBefore = 2;

    /// <summary>
    /// Number of paragraphs to include after the selection paragraph.
    /// </summary>
    private const int PreferredParagraphsAfter = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurroundingTextContextStrategy"/> class.
    /// </summary>
    /// <param name="editorService">Editor service for document content access.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="editorService"/> is null.
    /// </exception>
    public SurroundingTextContextStrategy(
        IEditorService editorService,
        ITokenCounter tokenCounter,
        ILogger<SurroundingTextContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
    }

    /// <inheritdoc />
    public override string StrategyId => "surrounding-text";

    /// <inheritdoc />
    public override string DisplayName => "Surrounding Text";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Critical priority (100) because surrounding text is the most important
    /// context for producing rewrites that match the document's existing tone and flow.
    /// Higher priority than style rules (50) and terminology (60) ensures surrounding
    /// context is never trimmed before those strategies.
    /// </remarks>
    public override int Priority => StrategyPriority.Critical; // 100

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: 1500 tokens accommodates 2-3 paragraphs of typical prose while leaving
    /// room in the 4000-token context budget for style rules and terminology.
    /// </remarks>
    public override int MaxTokens => 1500;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Surrounding text context gathering:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates that document path and cursor position are present.</description></item>
    ///   <item><description>Retrieves document content via <see cref="IEditorService"/>.</description></item>
    ///   <item><description>Splits document into paragraphs (double newline boundaries).</description></item>
    ///   <item><description>Locates the paragraph containing the cursor position.</description></item>
    ///   <item><description>Collects up to <see cref="PreferredParagraphsBefore"/> paragraphs before
    ///   and <see cref="PreferredParagraphsAfter"/> paragraphs after, respecting
    ///   <see cref="MaxSurroundingChars"/>.</description></item>
    ///   <item><description>Formats with <c>[SELECTION IS HERE]</c> marker.</description></item>
    ///   <item><description>Truncates to fit <see cref="MaxTokens"/> and returns fragment.</description></item>
    /// </list>
    /// <para>
    /// <strong>Graceful Degradation:</strong> Returns <c>null</c> if:
    /// <list type="bullet">
    ///   <item><description>No document path or cursor position in request</description></item>
    ///   <item><description>Document content is unavailable or empty</description></item>
    ///   <item><description>Cursor position doesn't fall within any paragraph</description></item>
    ///   <item><description>An exception occurs during gathering</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public override Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        // LOGIC: Both document and cursor are required to locate the selection
        // within the document and extract surrounding paragraphs.
        if (!ValidateRequest(request, requireDocument: true, requireCursor: true))
            return Task.FromResult<ContextFragment?>(null);

        _logger.LogDebug(
            "{Strategy} gathering surrounding text for cursor position {CursorPos}",
            StrategyId, request.CursorPosition!.Value);

        try
        {
            // LOGIC: Retrieve document content following SelectionContextStrategy pattern:
            // Try GetDocumentByPath first, fall back to GetDocumentText for active document.
            var fullContent = GetDocumentContent(request.DocumentPath!);

            if (string.IsNullOrEmpty(fullContent))
            {
                _logger.LogDebug(
                    "{Strategy} document content unavailable for path '{Path}'",
                    StrategyId, request.DocumentPath);
                return Task.FromResult<ContextFragment?>(null);
            }

            // LOGIC: Split document into paragraphs by double newline.
            // This matches the paragraph splitting convention used throughout the codebase
            // (SelectionContextStrategy, DocumentContextAnalyzer).
            var paragraphs = fullContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

            if (paragraphs.Length == 0)
            {
                _logger.LogDebug("{Strategy} no paragraphs found in document", StrategyId);
                return Task.FromResult<ContextFragment?>(null);
            }

            // LOGIC: Find the paragraph index containing the cursor position.
            var cursorPos = request.CursorPosition!.Value;
            var selectionParagraphIndex = FindParagraphIndex(paragraphs, cursorPos);

            if (selectionParagraphIndex < 0)
            {
                _logger.LogDebug(
                    "{Strategy} cursor position {CursorPos} not found in any paragraph",
                    StrategyId, cursorPos);
                return Task.FromResult<ContextFragment?>(null);
            }

            // LOGIC: Build surrounding context with character budget guard.
            var content = BuildSurroundingContext(paragraphs, selectionParagraphIndex);

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogDebug("{Strategy} no surrounding context available", StrategyId);
                return Task.FromResult<ContextFragment?>(null);
            }

            // LOGIC: Apply token-based truncation from base class.
            content = TruncateToMaxTokens(content);

            _logger.LogInformation(
                "{Strategy} gathered surrounding text: {ParagraphCount} total paragraphs, selection at index {Index}, {ContentLength} chars",
                StrategyId, paragraphs.Length, selectionParagraphIndex, content.Length);

            return Task.FromResult<ContextFragment?>(CreateFragment(content, relevance: 0.9f));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "{Strategy} failed to gather surrounding text context",
                StrategyId);
            return Task.FromResult<ContextFragment?>(null);
        }
    }

    /// <summary>
    /// Retrieves the full document text content from the editor service.
    /// </summary>
    /// <param name="documentPath">The document path to retrieve content for.</param>
    /// <returns>The document text content, or <c>null</c> if unavailable.</returns>
    /// <remarks>
    /// LOGIC: Follows the <see cref="Lexichord.Modules.Agents.Context.Strategies.SelectionContextStrategy"/>
    /// pattern: attempts to get the document by path first (supports multi-tab editing),
    /// then falls back to the active document text if the path matches.
    /// </remarks>
    private string? GetDocumentContent(string documentPath)
    {
        // LOGIC: Try path-based lookup first (works for non-active documents)
        var manuscript = _editorService.GetDocumentByPath(documentPath);
        var content = manuscript?.Content;

        if (!string.IsNullOrEmpty(content))
            return content;

        // LOGIC: Fallback to active document text if path matches
        if (string.Equals(documentPath, _editorService.CurrentDocumentPath, StringComparison.OrdinalIgnoreCase))
        {
            content = _editorService.GetDocumentText();
        }

        return content;
    }

    /// <summary>
    /// Finds the index of the paragraph containing the given cursor position.
    /// </summary>
    /// <param name="paragraphs">The document paragraphs (split by double newline).</param>
    /// <param name="cursorPosition">The 0-based cursor offset in the document.</param>
    /// <returns>
    /// The 0-based index of the paragraph containing the cursor, or -1 if not found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Iterates through paragraphs, accumulating character offsets to determine
    /// which paragraph contains the cursor. The +2 accounts for the "\n\n" separator
    /// that was removed during <see cref="string.Split"/>.
    /// </remarks>
    internal static int FindParagraphIndex(string[] paragraphs, int cursorPosition)
    {
        var charCount = 0;

        for (var i = 0; i < paragraphs.Length; i++)
        {
            // LOGIC: If cursor falls within this paragraph's character range
            if (charCount + paragraphs[i].Length >= cursorPosition)
                return i;

            charCount += paragraphs[i].Length + 2; // +2 for \n\n separator
        }

        // LOGIC: Cursor is beyond the last paragraph — return last paragraph index.
        // This handles cursors at the very end of the document.
        return paragraphs.Length > 0 ? paragraphs.Length - 1 : -1;
    }

    /// <summary>
    /// Builds the formatted surrounding context string with before/after paragraphs
    /// and the <c>[SELECTION IS HERE]</c> marker.
    /// </summary>
    /// <param name="paragraphs">All document paragraphs.</param>
    /// <param name="selectionIndex">Index of the paragraph containing the selection.</param>
    /// <returns>Formatted surrounding context string.</returns>
    /// <remarks>
    /// LOGIC: Collects paragraphs before and after the selection paragraph,
    /// respecting <see cref="MaxSurroundingChars"/> to avoid excessive content.
    /// The <c>[SELECTION IS HERE]</c> marker tells the LLM where the text being
    /// rewritten sits within the document flow.
    /// </remarks>
    private static string BuildSurroundingContext(string[] paragraphs, int selectionIndex)
    {
        var sb = new StringBuilder();
        var totalChars = 0;

        // LOGIC: Collect paragraphs before the selection
        var beforeStart = Math.Max(0, selectionIndex - PreferredParagraphsBefore);
        var includedBefore = 0;

        for (var i = beforeStart; i < selectionIndex && totalChars < MaxSurroundingChars; i++)
        {
            var para = paragraphs[i];
            if (totalChars + para.Length > MaxSurroundingChars)
                break;

            sb.AppendLine(para);
            sb.AppendLine();
            totalChars += para.Length;
            includedBefore++;
        }

        // LOGIC: Insert the selection marker so the LLM knows where the
        // rewritten text sits relative to the surrounding context.
        sb.AppendLine("[SELECTION IS HERE]");
        sb.AppendLine();

        // LOGIC: Collect paragraphs after the selection
        var afterEnd = Math.Min(paragraphs.Length - 1, selectionIndex + PreferredParagraphsAfter);
        var includedAfter = 0;

        for (var i = selectionIndex + 1; i <= afterEnd && totalChars < MaxSurroundingChars; i++)
        {
            var para = paragraphs[i];
            if (totalChars + para.Length > MaxSurroundingChars)
                break;

            sb.AppendLine(para);
            sb.AppendLine();
            totalChars += para.Length;
            includedAfter++;
        }

        return sb.ToString().TrimEnd();
    }
}
