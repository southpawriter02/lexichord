// -----------------------------------------------------------------------
// <copyright file="HeadingContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context.Strategies;

/// <summary>
/// Provides the document heading hierarchy (outline) as context.
/// Helps agents understand the document organization and the user's
/// current location within the structure.
/// </summary>
/// <remarks>
/// <para>
/// This strategy builds a text representation of the document's heading tree
/// using <see cref="IHeadingHierarchyService"/> from the RAG subsystem. Since
/// the heading service works with document GUIDs, this strategy first resolves
/// the file path to a <see cref="Document"/> record via <see cref="IDocumentRepository"/>.
/// </para>
/// <para>
/// <strong>Note:</strong> This strategy requires the document to be indexed in the RAG
/// system. If the document has not been indexed, the strategy gracefully returns <c>null</c>.
/// </para>
/// <para>
/// <strong>Priority:</strong> 70 (between <see cref="StrategyPriority.Medium"/> and
/// <see cref="StrategyPriority.High"/>) — Document structure provides useful organizational
/// context for agents performing targeted edits or analysis.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.WriterPro"/> or higher.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 300 — Heading outlines are compact and rarely exceed
/// this limit even for large documents.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2b as part of the Built-in Context Strategies.
/// </para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro)]
public sealed class HeadingContextStrategy : ContextStrategyBase
{
    private readonly IHeadingHierarchyService _headingService;
    private readonly IDocumentRepository _documentRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadingContextStrategy"/> class.
    /// </summary>
    /// <param name="headingService">Service for building heading hierarchies.</param>
    /// <param name="documentRepository">Repository for resolving file paths to document records.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="headingService"/> or <paramref name="documentRepository"/> is null.
    /// </exception>
    public HeadingContextStrategy(
        IHeadingHierarchyService headingService,
        IDocumentRepository documentRepository,
        ITokenCounter tokenCounter,
        ILogger<HeadingContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _headingService = headingService ?? throw new ArgumentNullException(nameof(headingService));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
    }

    /// <inheritdoc />
    public override string StrategyId => "heading";

    /// <inheritdoc />
    public override string DisplayName => "Heading Hierarchy";

    /// <inheritdoc />
    public override int Priority => StrategyPriority.Medium + 10; // 70

    /// <inheritdoc />
    public override int MaxTokens => 300;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Heading context gathering:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates that a document path is available.</description></item>
    ///   <item><description>Resolves file path to a <see cref="Document"/> via
    ///     <see cref="IDocumentRepository.GetByFilePathAsync"/>. Uses <see cref="Guid.Empty"/>
    ///     for projectId (matching the <c>DocumentIndexingPipeline</c> pattern).</description></item>
    ///   <item><description>Builds the heading tree via <see cref="IHeadingHierarchyService.BuildHeadingTreeAsync"/>.</description></item>
    ///   <item><description>Formats the heading tree as an indented outline.</description></item>
    ///   <item><description>Optionally includes a breadcrumb if cursor position is available.</description></item>
    /// </list>
    /// </remarks>
    public override async Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        // LOGIC: Document path is required to resolve headings
        if (!ValidateRequest(request, requireDocument: true))
            return null;

        _logger.LogDebug(
            "{Strategy} gathering structure from {Path}",
            StrategyId, request.DocumentPath);

        // LOGIC: Resolve file path to document GUID
        // COMPAT: Uses Guid.Empty for projectId, matching DocumentIndexingPipeline pattern.
        // When a proper IProjectContext is added, update both locations.
        var document = await _documentRepository.GetByFilePathAsync(
            Guid.Empty, request.DocumentPath!, ct);

        if (document is null)
        {
            _logger.LogDebug(
                "{Strategy} document not indexed, skipping heading context",
                StrategyId);
            return null;
        }

        // LOGIC: Build the heading tree for the document
        var headingTree = await _headingService.BuildHeadingTreeAsync(document.Id, ct);

        if (headingTree is null)
        {
            _logger.LogDebug("{Strategy} no headings found in document", StrategyId);
            return null;
        }

        // LOGIC: Format heading tree into a readable outline
        var content = FormatHeadingTree(headingTree);

        // LOGIC: Add breadcrumb for current position if cursor is available
        if (request.HasCursor)
        {
            var breadcrumb = await _headingService.GetBreadcrumbAsync(
                document.Id,
                EstimateChunkIndex(request.CursorPosition!.Value),
                ct);

            if (breadcrumb.Count > 0)
            {
                content += "\n\nCurrent Location: " + string.Join(" > ", breadcrumb);
            }
        }

        if (string.IsNullOrWhiteSpace(content))
            return null;

        // LOGIC: Apply truncation if needed
        content = TruncateToMaxTokens(content);

        // LOGIC: Higher relevance when we can show current location
        var relevance = request.HasCursor ? 0.8f : 0.6f;

        _logger.LogInformation(
            "{Strategy} gathered heading hierarchy ({Length} chars)",
            StrategyId, content.Length);

        return CreateFragment(content, relevance);
    }

    /// <summary>
    /// Formats a heading tree node and its children into an indented outline.
    /// </summary>
    /// <param name="node">The root heading node.</param>
    /// <param name="indent">Current indentation level (default: 0).</param>
    /// <returns>Formatted heading outline string.</returns>
    /// <remarks>
    /// LOGIC: Recursive depth-first traversal producing an indented outline:
    /// <code>
    /// - Chapter 1 (H1)
    ///   - Section 1.1 (H2)
    ///     - Subsection 1.1.1 (H3)
    ///   - Section 1.2 (H2)
    /// </code>
    /// </remarks>
    internal static string FormatHeadingTree(HeadingNode node, int indent = 0)
    {
        var sb = new StringBuilder();
        var prefix = new string(' ', indent * 2);

        sb.AppendLine($"{prefix}- {node.Text} (H{node.Level})");

        foreach (var child in node.Children)
        {
            sb.Append(FormatHeadingTree(child, indent + 1));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Estimates a chunk index from a cursor character position.
    /// Uses a rough estimate assuming ~1000 characters per chunk.
    /// </summary>
    /// <param name="cursorPosition">The cursor's character offset.</param>
    /// <returns>An estimated chunk index.</returns>
    /// <remarks>
    /// LOGIC: This is a best-effort estimation for breadcrumb lookup.
    /// The heading hierarchy service uses chunk indices for navigation.
    /// A more precise mapping would require chunk boundary data.
    /// </remarks>
    private static int EstimateChunkIndex(int cursorPosition)
    {
        // LOGIC: Rough estimate assuming ~1000 characters per chunk
        const int estimatedCharsPerChunk = 1000;
        return cursorPosition / estimatedCharsPerChunk;
    }
}
