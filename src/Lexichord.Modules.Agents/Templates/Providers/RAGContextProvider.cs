// -----------------------------------------------------------------------
// <copyright file="RAGContextProvider.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Templates.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Templates.Providers;

/// <summary>
/// Context provider that supplies RAG (Retrieval-Augmented Generation) context from semantic search.
/// </summary>
/// <remarks>
/// <para>
/// This provider executes a semantic search query using <see cref="ISemanticSearchService"/>
/// and formats the results for injection into prompts. RAG context provides relevant
/// background information from the indexed document corpus.
/// </para>
/// <para>
/// <strong>Variables Produced:</strong>
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Variable</term>
///     <description>Description</description>
///   </listheader>
///   <item>
///     <term><c>context</c></term>
///     <description>Formatted RAG chunks with source attribution.</description>
///   </item>
///   <item>
///     <term><c>context_source_count</c></term>
///     <description>Number of unique source documents in the results.</description>
///   </item>
///   <item>
///     <term><c>context_sources</c></term>
///     <description>Comma-separated list of source document paths.</description>
///   </item>
/// </list>
/// <para>
/// <strong>License Requirements:</strong> Requires <see cref="FeatureCodes.RAGContext"/> (WriterPro+).
/// </para>
/// <para>
/// <strong>Priority:</strong> 200 (highest - RAG context may override other context values).
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.6.3d as part of the Context Injection Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var provider = new RAGContextProvider(searchService, formatter, options, logger);
/// var request = ContextRequest.RAGOnly("What is dependency injection?", maxChunks: 3);
///
/// if (provider.IsEnabled(request))
/// {
///     var result = await provider.GetContextAsync(request, CancellationToken.None);
///     if (result.Success &amp;&amp; result.HasData)
///     {
///         // result.Data["context"] = "[Source: docs/di-guide.md]\nDependency injection is..."
///         // result.Data["context_source_count"] = 2
///         // result.Data["context_sources"] = "docs/di-guide.md, docs/patterns.md"
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IContextProvider"/>
/// <seealso cref="ISemanticSearchService"/>
/// <seealso cref="IContextFormatter"/>
public sealed class RAGContextProvider : IContextProvider
{
    private readonly ISemanticSearchService _searchService;
    private readonly IContextFormatter _formatter;
    private readonly ContextInjectorOptions _options;
    private readonly ILogger<RAGContextProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RAGContextProvider"/> class.
    /// </summary>
    /// <param name="searchService">The semantic search service for RAG queries.</param>
    /// <param name="formatter">The formatter for converting search hits to strings.</param>
    /// <param name="options">Configuration options for context injection.</param>
    /// <param name="logger">The logger for provider diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public RAGContextProvider(
        ISemanticSearchService searchService,
        IContextFormatter formatter,
        IOptions<ContextInjectorOptions> options,
        ILogger<RAGContextProvider> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "RAGContextProvider initialized (MinRelevanceScore={MinScore}, MaxChunkLength={MaxChunkLength})",
            _options.MinRAGRelevanceScore,
            _options.MaxChunkLength);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: This provider is identified as "RAG" in logs and result tracking.
    /// </remarks>
    public string ProviderName => "RAG";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Priority 200 is the highest, meaning RAG context is processed last
    /// and can override values from lower-priority providers.
    /// </remarks>
    public int Priority => 200;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: RAG context requires the WriterPro tier. The <see cref="FeatureCodes.RAGContext"/>
    /// feature code is checked by the <see cref="IContextInjector"/> before executing this provider.
    /// </remarks>
    public string? RequiredLicenseFeature => FeatureCodes.RAGContext;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: This provider is enabled when the request explicitly includes RAG context
    /// via the <see cref="ContextRequest.IncludeRAGContext"/> flag AND has a valid query.
    /// </para>
    /// <para>
    /// A valid query requires that at least one of these sources be present:
    /// <list type="bullet">
    ///   <item><description>Selected text (used as the search query).</description></item>
    ///   <item><description>Document context that can be derived from the request.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool IsEnabled(ContextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // LOGIC: RAG is enabled when explicitly requested AND there's something to search for
        var isEnabled = request.IncludeRAGContext && request.HasSelectedText;

        _logger.LogDebug(
            "RAGContextProvider.IsEnabled: {IsEnabled} (IncludeRAGContext={IncludeRAG}, HasSelectedText={HasSel})",
            isEnabled,
            request.IncludeRAGContext,
            request.HasSelectedText);

        return isEnabled;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Executes a semantic search using the selected text as the query.
    /// The search is configured with:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>TopK: Limited by <see cref="ContextRequest.MaxRAGChunks"/>.</description></item>
    ///   <item><description>MinScore: Configured via <see cref="ContextInjectorOptions.MinRAGRelevanceScore"/>.</description></item>
    ///   <item><description>Caching enabled for repeated queries.</description></item>
    /// </list>
    /// <para>
    /// Results are formatted using the injected <see cref="IContextFormatter"/> and
    /// source paths are extracted for the context_sources variable.
    /// </para>
    /// </remarks>
    public async Task<ContextResult> GetContextAsync(ContextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        ct.ThrowIfCancellationRequested();

        var sw = Stopwatch.StartNew();

        _logger.LogDebug(
            "RAGContextProvider.GetContextAsync starting (MaxChunks={MaxChunks}, Query='{Query}')",
            request.MaxRAGChunks,
            TruncateForLog(request.SelectedText, 50));

        try
        {
            // LOGIC: Use selected text as the search query
            var query = request.SelectedText;
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogDebug("No query text available for RAG search");
                sw.Stop();
                return ContextResult.Empty(ProviderName, sw.Elapsed);
            }

            // LOGIC: Configure search options based on request and options
            var searchOptions = new SearchOptions
            {
                TopK = request.MaxRAGChunks,
                MinScore = _options.MinRAGRelevanceScore,
                UseCache = true,
                ExpandAbbreviations = false,
                RespectCanonicals = true,
                IncludeArchived = false
            };

            _logger.LogDebug(
                "Executing semantic search with TopK={TopK}, MinScore={MinScore}",
                searchOptions.TopK,
                searchOptions.MinScore);

            // LOGIC: Execute the semantic search
            var searchResult = await _searchService.SearchAsync(query, searchOptions, ct);

            if (searchResult is null || searchResult.Hits.Count == 0)
            {
                _logger.LogDebug("Semantic search returned no results");
                sw.Stop();
                return ContextResult.Empty(ProviderName, sw.Elapsed);
            }

            _logger.LogDebug(
                "Semantic search returned {HitCount} hits",
                searchResult.Hits.Count);

            // LOGIC: Filter hits below the minimum relevance score
            var filteredHits = searchResult.Hits
                .Where(h => h.Score >= _options.MinRAGRelevanceScore)
                .ToList();

            if (filteredHits.Count == 0)
            {
                _logger.LogDebug(
                    "All {HitCount} hits filtered out by MinRelevanceScore={MinScore}",
                    searchResult.Hits.Count,
                    _options.MinRAGRelevanceScore);
                sw.Stop();
                return ContextResult.Empty(ProviderName, sw.Elapsed);
            }

            _logger.LogDebug(
                "{FilteredCount} hits remaining after relevance filtering",
                filteredHits.Count);

            // LOGIC: Format the search results using the injected formatter
            var formattedContext = _formatter.FormatRAGChunks(filteredHits, _options.MaxChunkLength);

            if (string.IsNullOrWhiteSpace(formattedContext))
            {
                _logger.LogDebug("Formatter produced empty output");
                sw.Stop();
                return ContextResult.Empty(ProviderName, sw.Elapsed);
            }

            // LOGIC: Extract unique source paths
            var uniqueSources = filteredHits
                .Where(h => h.Document?.FilePath != null)
                .Select(h => h.Document.FilePath!)
                .Distinct()
                .ToList();

            // LOGIC: Assemble the context data
            var data = new Dictionary<string, object>
            {
                ["context"] = formattedContext,
                ["context_source_count"] = uniqueSources.Count,
                ["context_sources"] = string.Join(", ", uniqueSources)
            };

            sw.Stop();

            _logger.LogInformation(
                "RAGContextProvider produced {VariableCount} variables ({HitCount} hits, {SourceCount} sources) in {Duration}ms",
                data.Count,
                filteredHits.Count,
                uniqueSources.Count,
                sw.ElapsedMilliseconds);

            return ContextResult.Ok(ProviderName, data, sw.Elapsed);
        }
        catch (FeatureNotLicensedException ex)
        {
            sw.Stop();

            _logger.LogDebug(
                ex,
                "RAG search skipped due to license restriction: {Message}",
                ex.Message);

            // LOGIC: License failures should be silent from the provider's perspective
            // The IContextInjector handles license checking before invoking the provider
            return ContextResult.Empty(ProviderName, sw.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();

            _logger.LogWarning(
                ex,
                "RAGContextProvider encountered error: {ErrorMessage}",
                ex.Message);

            return ContextResult.Failure(ProviderName, ex.Message);
        }
    }

    /// <summary>
    /// Truncates a string for logging purposes.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum length before truncation.</param>
    /// <returns>The truncated string or original if shorter than maxLength.</returns>
    private static string? TruncateForLog(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text.Length <= maxLength
            ? text
            : text[..maxLength] + "...";
    }
}
