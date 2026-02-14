// -----------------------------------------------------------------------
// <copyright file="SelectionContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context.Strategies;

/// <summary>
/// Provides the user's selected text with surrounding paragraph context.
/// Designed for context-aware editing suggestions based on what the user
/// has explicitly selected.
/// </summary>
/// <remarks>
/// <para>
/// This strategy extracts the user's current text selection and wraps it with
/// contextual markers (<c>&lt;&lt;SELECTED_TEXT&gt;&gt;</c> / <c>&lt;&lt;/SELECTED_TEXT&gt;&gt;</c>)
/// to clearly delineate the focused text for the AI agent. When a document path and
/// cursor position are available, surrounding paragraphs are included for additional context.
/// </para>
/// <para>
/// <strong>Priority:</strong> <see cref="StrategyPriority.High"/> (80) — Selected text represents
/// the user's explicit focus and is highly relevant to most agent tasks.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.WriterPro"/> or higher.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 1000 — Selections are typically short; surrounding context
/// provides the additional value.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2b as part of the Built-in Context Strategies.
/// </para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro)]
public sealed class SelectionContextStrategy : ContextStrategyBase
{
    private readonly IEditorService _editorService;

    /// <summary>
    /// Number of paragraphs to include before and after the selection.
    /// </summary>
    private const int SurroundingParagraphs = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionContextStrategy"/> class.
    /// </summary>
    /// <param name="editorService">Editor service for surrounding context access.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="editorService"/> is null.
    /// </exception>
    public SelectionContextStrategy(
        IEditorService editorService,
        ITokenCounter tokenCounter,
        ILogger<SelectionContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
    }

    /// <inheritdoc />
    public override string StrategyId => "selection";

    /// <inheritdoc />
    public override string DisplayName => "Selected Text";

    /// <inheritdoc />
    public override int Priority => StrategyPriority.High; // 80

    /// <inheritdoc />
    public override int MaxTokens => 1000;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Selection context gathering:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates that selected text is available in the request.</description></item>
    ///   <item><description>Attempts to gather surrounding paragraph context from the document.</description></item>
    ///   <item><description>Formats selection with context markers and surrounding text.</description></item>
    ///   <item><description>Truncates to fit token budget if necessary.</description></item>
    /// </list>
    /// </remarks>
    public override Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        // LOGIC: Selection is required for this strategy
        if (!ValidateRequest(request, requireSelection: true))
            return Task.FromResult<ContextFragment?>(null);

        _logger.LogDebug(
            "{Strategy} gathering selection ({Length} chars)",
            StrategyId, request.SelectedText!.Length);

        var selection = request.SelectedText!;

        // LOGIC: Attempt to get surrounding context if document is available
        var (before, after) = GetSurroundingContext(request);

        // LOGIC: Format with markers and surrounding context
        var result = FormatSelectionWithContext(selection, before, after);

        // LOGIC: Apply truncation if needed
        result = TruncateToMaxTokens(result);

        _logger.LogInformation(
            "{Strategy} gathered selection with context ({Length} chars)",
            StrategyId, result.Length);

        return Task.FromResult<ContextFragment?>(CreateFragment(result, relevance: 1.0f));
    }

    /// <summary>
    /// Retrieves paragraphs surrounding the user's selection from the document.
    /// </summary>
    /// <param name="request">The context gathering request with document and cursor info.</param>
    /// <returns>A tuple of (before, after) paragraph context. Empty strings if unavailable.</returns>
    /// <remarks>
    /// LOGIC: Surrounding context retrieval:
    /// <list type="number">
    ///   <item><description>Requires both document path and cursor position.</description></item>
    ///   <item><description>Splits document into paragraphs (double newline boundaries).</description></item>
    ///   <item><description>Identifies the paragraph containing the cursor.</description></item>
    ///   <item><description>Returns <see cref="SurroundingParagraphs"/> paragraphs before and after.</description></item>
    /// </list>
    /// </remarks>
    private (string before, string after) GetSurroundingContext(ContextGatheringRequest request)
    {
        // LOGIC: Need both document and cursor to locate selection in document
        if (!request.HasDocument || !request.HasCursor)
            return (string.Empty, string.Empty);

        // LOGIC: Retrieve the full document text
        var manuscript = _editorService.GetDocumentByPath(request.DocumentPath!);
        var fullContent = manuscript?.Content;

        if (string.IsNullOrEmpty(fullContent))
        {
            // LOGIC: Fallback to active document if path matches
            if (string.Equals(request.DocumentPath, _editorService.CurrentDocumentPath, StringComparison.OrdinalIgnoreCase))
                fullContent = _editorService.GetDocumentText();
        }

        if (string.IsNullOrEmpty(fullContent))
            return (string.Empty, string.Empty);

        var cursorPos = request.CursorPosition!.Value;

        // LOGIC: Split document into paragraphs to find selection location
        var paragraphs = fullContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        int charCount = 0;
        int selectionParagraphIndex = 0;

        for (int i = 0; i < paragraphs.Length; i++)
        {
            if (charCount + paragraphs[i].Length >= cursorPos)
            {
                selectionParagraphIndex = i;
                break;
            }
            charCount += paragraphs[i].Length + 2; // +2 for \n\n separator
        }

        // LOGIC: Extract surrounding paragraphs
        var beforeStart = Math.Max(0, selectionParagraphIndex - SurroundingParagraphs);
        var afterEnd = Math.Min(paragraphs.Length - 1, selectionParagraphIndex + SurroundingParagraphs);

        var before = string.Join("\n\n", paragraphs
            .Skip(beforeStart)
            .Take(selectionParagraphIndex - beforeStart));

        var after = string.Join("\n\n", paragraphs
            .Skip(selectionParagraphIndex + 1)
            .Take(afterEnd - selectionParagraphIndex));

        return (before, after);
    }

    /// <summary>
    /// Formats the selection with context markers and surrounding text.
    /// </summary>
    /// <param name="selection">The selected text.</param>
    /// <param name="before">Paragraph(s) before the selection.</param>
    /// <param name="after">Paragraph(s) after the selection.</param>
    /// <returns>Formatted string with clear selection boundaries.</returns>
    /// <remarks>
    /// LOGIC: Formats with XML-style markers so agents can clearly identify:
    /// <list type="bullet">
    ///   <item><description>Context before selection (if available).</description></item>
    ///   <item><description>The exact selected text between markers.</description></item>
    ///   <item><description>Context after selection (if available).</description></item>
    /// </list>
    /// </remarks>
    private static string FormatSelectionWithContext(string selection, string before, string after)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(before))
        {
            sb.AppendLine("[Context before selection]");
            sb.AppendLine(before);
            sb.AppendLine();
        }

        sb.AppendLine("<<SELECTED_TEXT>>");
        sb.AppendLine(selection);
        sb.AppendLine("<</SELECTED_TEXT>>");

        if (!string.IsNullOrWhiteSpace(after))
        {
            sb.AppendLine();
            sb.AppendLine("[Context after selection]");
            sb.AppendLine(after);
        }

        return sb.ToString().TrimEnd();
    }
}
