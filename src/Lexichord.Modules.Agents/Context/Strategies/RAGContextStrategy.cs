// -----------------------------------------------------------------------
// <copyright file="RAGContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context.Strategies;

/// <summary>
/// Provides semantically related content from the RAG (Retrieval-Augmented Generation)
/// index as context. Searches the indexed documentation corpus for snippets related
/// to the user's selected text or current document.
/// </summary>
/// <remarks>
/// <para>
/// This strategy leverages the semantic search subsystem to find related documentation
/// that can inform the AI agent's suggestions. It uses the user's selected text as
/// the primary search query, with configurable result limits and relevance thresholds.
/// </para>
/// <para>
/// <strong>Priority:</strong> <see cref="StrategyPriority.Medium"/> (60) — RAG results provide
/// valuable background knowledge but are supplementary to document and selection context.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.Teams"/> or higher.
/// RAG search requires indexed corpus infrastructure that is part of the Teams tier.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 2000 — RAG chunks can be substantial; this allocation
/// allows for 2-3 meaningful search results with surrounding context.
/// </para>
/// <para>
/// <strong>Error Handling:</strong>
/// The strategy catches <see cref="FeatureNotLicensedException"/> and general exceptions
/// gracefully, returning <c>null</c> instead of propagating errors. Only
/// <see cref="OperationCanceledException"/> is re-thrown to respect cancellation.
/// </para>
/// <para>
/// <strong>Configurable Hints:</strong>
/// <list type="bullet">
///   <item><description><c>TopK</c> (int, default: 3): Maximum number of search results.</description></item>
///   <item><description><c>MinScore</c> (float, default: 0.7): Minimum relevance score threshold.</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2b as part of the Built-in Context Strategies.
/// </para>
/// </remarks>
[RequiresLicense(LicenseTier.Teams)]
public sealed class RAGContextStrategy : ContextStrategyBase
{
    private readonly ISemanticSearchService _searchService;

    /// <summary>
    /// Default number of top search results to include.
    /// </summary>
    private const int DefaultTopK = 3;

    /// <summary>
    /// Default minimum relevance score for included results.
    /// </summary>
    private const float DefaultMinScore = 0.7f;

    /// <summary>
    /// Hint key for configuring maximum search results.
    /// </summary>
    internal const string TopKHintKey = "TopK";

    /// <summary>
    /// Hint key for configuring minimum relevance score.
    /// </summary>
    internal const string MinScoreHintKey = "MinScore";

    /// <summary>
    /// Initializes a new instance of the <see cref="RAGContextStrategy"/> class.
    /// </summary>
    /// <param name="searchService">Semantic search service for querying the indexed corpus.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="searchService"/> is null.
    /// </exception>
    public RAGContextStrategy(
        ISemanticSearchService searchService,
        ITokenCounter tokenCounter,
        ILogger<RAGContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <inheritdoc />
    public override string StrategyId => "rag";

    /// <inheritdoc />
    public override string DisplayName => "Related Documentation";

    /// <inheritdoc />
    public override int Priority => StrategyPriority.Medium; // 60

    /// <inheritdoc />
    public override int MaxTokens => 2000;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: RAG context gathering:
    /// </para>
    /// <list type="number">
    ///   <item><description>Determines search query from selected text (preferred) or falls back
    ///     to document path as a last resort.</description></item>
    ///   <item><description>Configures <see cref="SearchOptions"/> with TopK and MinScore from hints.</description></item>
    ///   <item><description>Executes semantic search via <see cref="ISemanticSearchService"/>.</description></item>
    ///   <item><description>Formats results with source attribution and content.</description></item>
    ///   <item><description>Calculates aggregate relevance from hit scores.</description></item>
    /// </list>
    /// <para>
    /// Error handling: <see cref="FeatureNotLicensedException"/> and general exceptions
    /// are caught and result in <c>null</c> return. <see cref="OperationCanceledException"/>
    /// is re-thrown.
    /// </para>
    /// </remarks>
    public override async Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        // LOGIC: Need either selection or document to derive a search query
        if (!request.HasSelection && !request.HasDocument)
        {
            _logger.LogDebug("{Strategy} no query source available (no selection or document)", StrategyId);
            return null;
        }

        // LOGIC: Build search query from available context
        var query = BuildSearchQuery(request);
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("{Strategy} could not build search query", StrategyId);
            return null;
        }

        // LOGIC: Get configurable search parameters from hints
        var topK = request.GetHint(TopKHintKey, DefaultTopK);
        var minScore = request.GetHint(MinScoreHintKey, DefaultMinScore);

        _logger.LogDebug(
            "{Strategy} searching for: '{Query}' (top {K}, min score {MinScore:F2})",
            StrategyId, Truncate(query, 50), topK, minScore);

        // LOGIC: Configure search options
        var searchOptions = new SearchOptions
        {
            TopK = topK,
            MinScore = minScore,
            UseCache = true,
            RespectCanonicals = true,
            IncludeArchived = false
        };

        try
        {
            // LOGIC: Execute semantic search
            var result = await _searchService.SearchAsync(query, searchOptions, ct);

            if (!result.HasResults)
            {
                _logger.LogDebug("{Strategy} no search results found", StrategyId);
                return null;
            }

            _logger.LogDebug(
                "{Strategy} found {Count} results",
                StrategyId, result.Hits.Count);

            // LOGIC: Format search results into context content
            var content = FormatSearchResults(result.Hits);
            if (string.IsNullOrWhiteSpace(content))
                return null;

            // LOGIC: Apply token truncation
            content = TruncateToMaxTokens(content);

            // LOGIC: Aggregate relevance from hit scores
            var avgRelevance = result.Hits.Count > 0
                ? result.Hits.Average(h => h.Score)
                : 0.5f;

            _logger.LogInformation(
                "{Strategy} gathered {Count} search results (avg relevance: {Relevance:F2})",
                StrategyId, result.Hits.Count, avgRelevance);

            return CreateFragment(content, avgRelevance);
        }
        catch (FeatureNotLicensedException ex)
        {
            // LOGIC: Graceful handling when semantic search is not licensed
            _logger.LogDebug(ex, "{Strategy} semantic search not licensed", StrategyId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // LOGIC: Graceful handling of search failures
            // OperationCanceledException is re-thrown to respect cancellation
            _logger.LogWarning(ex, "{Strategy} semantic search failed", StrategyId);
            return null;
        }
    }

    /// <summary>
    /// Builds a search query from the context gathering request.
    /// </summary>
    /// <param name="request">The context gathering request.</param>
    /// <returns>A search query string, or null if no suitable query source is available.</returns>
    /// <remarks>
    /// LOGIC: Query source priority:
    /// <list type="number">
    ///   <item><description>Selected text (highest signal — user's explicit focus).</description></item>
    ///   <item><description>Document path as a fallback (file name may provide topic hints).</description></item>
    /// </list>
    /// Query text is truncated to 200 characters to keep search efficient.
    /// </remarks>
    private static string? BuildSearchQuery(ContextGatheringRequest request)
    {
        // LOGIC: Prefer selected text — highest signal for user intent
        if (request.HasSelection)
        {
            return Truncate(request.SelectedText!, 200);
        }

        // LOGIC: Fall back to document path (file name can hint at topic)
        if (request.HasDocument)
        {
            return Path.GetFileNameWithoutExtension(request.DocumentPath!);
        }

        return null;
    }

    /// <summary>
    /// Formats search results into a readable context string.
    /// </summary>
    /// <param name="hits">The search hits to format.</param>
    /// <returns>Formatted search results with source attribution.</returns>
    /// <remarks>
    /// LOGIC: Each hit is formatted with:
    /// <list type="bullet">
    ///   <item><description>Source file path (if available) for attribution.</description></item>
    ///   <item><description>Relevance score as a percentage.</description></item>
    ///   <item><description>The chunk content.</description></item>
    /// </list>
    /// </remarks>
    internal static string FormatSearchResults(IReadOnlyList<SearchHit> hits)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Related Documentation");
        sb.AppendLine();

        foreach (var hit in hits)
        {
            // LOGIC: Include source attribution when document info is available
            if (hit.Document?.FilePath is not null)
            {
                sb.AppendLine($"### From: {Path.GetFileName(hit.Document.FilePath)}");
            }

            sb.AppendLine($"*Relevance: {hit.Score:P0}*");
            sb.AppendLine();
            sb.AppendLine(hit.Chunk.Content);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Truncates a string to a maximum length.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum character count.</param>
    /// <returns>The original or truncated text.</returns>
    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text[..maxLength];
    }
}
