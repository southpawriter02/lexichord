// -----------------------------------------------------------------------
// <copyright file="DocumentContextStrategy.cs" company="Lexichord">
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
/// Provides the current document content as context for AI agents.
/// Implements smart truncation to respect token limits while preserving
/// complete paragraphs and document structure.
/// </summary>
/// <remarks>
/// <para>
/// This strategy is the most fundamental context source, providing agents with
/// the full text of the user's active document (or as much as fits within the
/// token budget). It uses a two-tier document access approach:
/// </para>
/// <list type="number">
///   <item><description>Attempt to retrieve the document by path via
///     <see cref="IEditorService.GetDocumentByPath"/> for non-active document support.</description></item>
///   <item><description>Fall back to <see cref="IEditorService.GetDocumentText"/> if the
///     requested path matches the currently active document.</description></item>
/// </list>
/// <para>
/// <strong>Priority:</strong> <see cref="StrategyPriority.Critical"/> (100) — Document content
/// is almost always essential for editing, analysis, and suggestion tasks.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.WriterPro"/> or higher.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 4000 — Generous allocation for full document context.
/// Content exceeding this limit is truncated using the base class paragraph-aware
/// truncation algorithm (<see cref="ContextStrategyBase.TruncateToMaxTokens"/>).
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2b as part of the Built-in Context Strategies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Strategy is resolved from the DI container via IContextStrategyFactory
/// var factory = serviceProvider.GetRequiredService&lt;IContextStrategyFactory&gt;();
/// var strategy = factory.CreateStrategy("document");
///
/// var request = new ContextGatheringRequest(
///     DocumentPath: "/docs/chapter1.md",
///     CursorPosition: null,
///     SelectedText: null,
///     AgentId: "editor",
///     Hints: null);
///
/// var fragment = await strategy!.GatherAsync(request, CancellationToken.None);
/// // fragment.SourceId == "document"
/// // fragment.Label == "Document Content"
/// // fragment.Content contains the document text (truncated if necessary)
/// </code>
/// </example>
[RequiresLicense(LicenseTier.WriterPro)]
public sealed class DocumentContextStrategy : ContextStrategyBase
{
    private readonly IEditorService _editorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentContextStrategy"/> class.
    /// </summary>
    /// <param name="editorService">Editor service for document content access.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="editorService"/> is null.
    /// </exception>
    public DocumentContextStrategy(
        IEditorService editorService,
        ITokenCounter tokenCounter,
        ILogger<DocumentContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
    }

    /// <inheritdoc />
    public override string StrategyId => "document";

    /// <inheritdoc />
    public override string DisplayName => "Document Content";

    /// <inheritdoc />
    public override int Priority => StrategyPriority.Critical; // 100

    /// <inheritdoc />
    public override int MaxTokens => 4000;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Document content retrieval follows a two-tier approach:
    /// </para>
    /// <list type="number">
    ///   <item><description>Primary: Resolve by path via <see cref="IEditorService.GetDocumentByPath"/>
    ///     and read <c>Content</c> property from the manuscript ViewModel.</description></item>
    ///   <item><description>Fallback: If path matches the active document, use
    ///     <see cref="IEditorService.GetDocumentText"/> for direct access.</description></item>
    /// </list>
    /// <para>
    /// Returns <c>null</c> when no document path is provided, the document cannot be found,
    /// or the document content is empty.
    /// </para>
    /// </remarks>
    public override Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        // LOGIC: Validate that a document path is available
        if (!ValidateRequest(request, requireDocument: true))
            return Task.FromResult<ContextFragment?>(null);

        _logger.LogDebug(
            "{Strategy} gathering content from {Path}",
            StrategyId, request.DocumentPath);

        // LOGIC: Primary path — resolve document by path (supports non-active documents)
        var manuscript = _editorService.GetDocumentByPath(request.DocumentPath!);
        var content = manuscript?.Content;

        // LOGIC: Fallback — if GetDocumentByPath returns null but path matches active doc
        if (string.IsNullOrEmpty(content) &&
            string.Equals(request.DocumentPath, _editorService.CurrentDocumentPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "{Strategy} falling back to GetDocumentText for active document",
                StrategyId);
            content = _editorService.GetDocumentText();
        }

        // LOGIC: Return null if no content could be retrieved
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogDebug("{Strategy} document content is empty or unavailable", StrategyId);
            return Task.FromResult<ContextFragment?>(null);
        }

        // LOGIC: Apply smart truncation if content exceeds token budget
        content = TruncateSmartly(content);

        _logger.LogInformation(
            "{Strategy} gathered document content ({Length} chars)",
            StrategyId, content.Length);

        return Task.FromResult<ContextFragment?>(CreateFragment(content, relevance: 1.0f));
    }

    /// <summary>
    /// Truncates document content while preserving structural integrity.
    /// Prefers breaking at heading and paragraph boundaries over mid-paragraph cuts.
    /// </summary>
    /// <param name="content">The document content to potentially truncate.</param>
    /// <returns>The original or truncated content.</returns>
    /// <remarks>
    /// LOGIC: Enhanced truncation that respects document structure:
    /// <list type="number">
    ///   <item><description>Returns unchanged if within token limit.</description></item>
    ///   <item><description>Splits by lines and accumulates until budget reached.</description></item>
    ///   <item><description>Prefers breaking at headings or blank lines.</description></item>
    ///   <item><description>Appends truncation indicator when content is cut.</description></item>
    /// </list>
    /// </remarks>
    private string TruncateSmartly(string content)
    {
        // LOGIC: Early return if content fits within budget
        var tokens = _tokenCounter.CountTokens(content);
        if (tokens <= MaxTokens) return content;

        _logger.LogInformation(
            "{Strategy} truncating document from {Original} to ~{Max} tokens",
            StrategyId, tokens, MaxTokens);

        // LOGIC: Split by lines to preserve structure
        var lines = content.Split('\n');
        var result = new StringBuilder();
        var currentTokens = 0;

        foreach (var line in lines)
        {
            var lineTokens = _tokenCounter.CountTokens(line);

            // LOGIC: Stop if adding this line would exceed budget
            if (currentTokens + lineTokens > MaxTokens)
            {
                // LOGIC: Prefer breaking at structural boundaries
                if (IsBreakingPoint(line) || currentTokens > MaxTokens / 2)
                {
                    result.AppendLine("...");
                    result.AppendLine("[Content truncated to fit token budget]");
                    break;
                }
            }

            result.AppendLine(line);
            currentTokens += lineTokens;
        }

        return result.ToString().TrimEnd();
    }

    /// <summary>
    /// Determines whether a line represents a good breaking point for truncation.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns><c>true</c> if the line is a blank line or a heading.</returns>
    private static bool IsBreakingPoint(string line)
        => string.IsNullOrWhiteSpace(line) || line.StartsWith('#');
}
