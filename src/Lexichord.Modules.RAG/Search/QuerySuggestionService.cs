// =============================================================================
// File: QuerySuggestionService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of query autocomplete suggestions (v0.5.4c).
// =============================================================================
// LOGIC: Provides intelligent autocomplete suggestions from multiple sources:
//   - Query history (previously executed searches)
//   - Document headings (extracted during indexing)
//   - Content n-grams (common phrases)
//   - Domain terms (from terminology database)
// =============================================================================
// VERSION: v0.5.4c (Query Suggestions)
// DEPENDENCIES:
//   - IDbConnectionFactory (v0.0.5b) for database access
//   - ILicenseContext (v0.0.4c) for Writer Pro gating
//   - ILogger<T> (v0.0.3b) for structured logging
// =============================================================================

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Provides query autocomplete suggestions based on indexed content.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="QuerySuggestionService"/> powers the search box autocomplete,
/// ranking suggestions by frequency, recency, and source type.
/// </para>
/// <para>
/// <b>Performance:</b> Suggestions use database prefix index for O(log n) lookup.
/// Results are cached for 30 seconds per prefix.
/// </para>
/// <para>
/// <b>License Gate:</b> Autocomplete is gated at Writer Pro tier.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4c as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public sealed class QuerySuggestionService : IQuerySuggestionService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<QuerySuggestionService> _logger;

    /// <summary>
    /// Cache for suggestions (prefix → suggestions).
    /// </summary>
    private readonly ConcurrentDictionary<string, (DateTime Expiry, IReadOnlyList<QuerySuggestion> Suggestions)> _cache = new();

    /// <summary>
    /// Cache duration in seconds.
    /// </summary>
    private const int CacheDurationSeconds = 30;

    /// <summary>
    /// Feature flag for Writer Pro gating.
    /// </summary>
    private const string FeatureCode = "RAG.RelevanceTuner";

    /// <summary>
    /// Minimum prefix length for suggestions.
    /// </summary>
    private const int MinPrefixLength = 2;

    #region N-gram Extraction Patterns

    /// <summary>
    /// Matches Markdown headings (# ## ### etc.).
    /// </summary>
    private static readonly Regex HeadingPattern = new(
        @"^#{1,6}\s+(.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Matches bold text (**text** or __text__).
    /// </summary>
    private static readonly Regex BoldPattern = new(
        @"\*\*([^*]+)\*\*|__([^_]+)__",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches code identifiers in backticks.
    /// </summary>
    private static readonly Regex CodePattern = new(
        @"`([A-Z][a-zA-Z0-9]+)`",
        RegexOptions.Compiled);

    #endregion

    /// <summary>
    /// Creates a new <see cref="QuerySuggestionService"/> instance.
    /// </summary>
    /// <param name="connectionFactory">Database connection factory.</param>
    /// <param name="licenseContext">License context for Writer Pro gating.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public QuerySuggestionService(
        IDbConnectionFactory connectionFactory,
        ILicenseContext licenseContext,
        ILogger<QuerySuggestionService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("QuerySuggestionService initialized");
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QuerySuggestion>> GetSuggestionsAsync(
        string? prefix,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: Validate input.
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < MinPrefixLength)
        {
            return Array.Empty<QuerySuggestion>();
        }

        // LOGIC: Check license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCode))
        {
            _logger.LogDebug("Query suggestions skipped: Writer Pro license required");
            return Array.Empty<QuerySuggestion>();
        }

        var normalizedPrefix = prefix.Trim().ToLowerInvariant();
        var cacheKey = $"{normalizedPrefix}:{maxResults}";

        // LOGIC: Check cache.
        if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
        {
            _logger.LogDebug("Returning {Count} cached suggestions for prefix '{Prefix}'",
                cached.Suggestions.Count, prefix);
            return cached.Suggestions;
        }

        _logger.LogDebug("Getting suggestions for prefix '{Prefix}'", prefix);

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            // LOGIC: Query suggestions with prefix matching, ordered by score.
            const string sql = """
                SELECT
                    text,
                    frequency,
                    source,
                    -- Calculate score: frequency (40%) + recency (30%) + source weight (30%)
                    (
                        (LEAST(frequency, 100) / 100.0) * 0.4 +
                        (1.0 - LEAST(EXTRACT(EPOCH FROM (NOW() - last_seen_at)) / 604800.0, 1.0)) * 0.3 +
                        CASE source
                            WHEN 'query_history' THEN 0.3
                            WHEN 'heading' THEN 0.25
                            WHEN 'term' THEN 0.2
                            WHEN 'ngram' THEN 0.15
                            ELSE 0.1
                        END
                    ) as score
                FROM query_suggestions
                WHERE normalized_text LIKE @Prefix || '%'
                ORDER BY score DESC, frequency DESC
                LIMIT @MaxResults
                """;

            var rows = await connection.QueryAsync<SuggestionRow>(
                sql,
                new { Prefix = normalizedPrefix, MaxResults = maxResults });

            var suggestions = rows.Select(r => new QuerySuggestion(
                Text: r.text,
                Frequency: r.frequency,
                Source: ParseSource(r.source),
                Score: (float)r.score))
                .ToList()
                .AsReadOnly();

            // LOGIC: Cache results.
            _cache[cacheKey] = (DateTime.UtcNow.AddSeconds(CacheDurationSeconds), suggestions);

            _logger.LogDebug("Returned {Count} suggestions for prefix '{Prefix}'",
                suggestions.Count, prefix);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get suggestions for prefix '{Prefix}'", prefix);
            return Array.Empty<QuerySuggestion>();
        }
    }

    /// <inheritdoc/>
    public async Task RecordQueryAsync(
        string query,
        int resultCount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        // LOGIC: Check license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCode))
            return;

        var normalizedText = query.Trim().ToLowerInvariant();

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            // LOGIC: Upsert: increment frequency if exists, otherwise insert.
            const string sql = """
                INSERT INTO query_suggestions (text, normalized_text, source, frequency, last_seen_at)
                VALUES (@Text, @NormalizedText, 'query_history', 1, NOW())
                ON CONFLICT (normalized_text, source)
                DO UPDATE SET
                    frequency = query_suggestions.frequency + 1,
                    last_seen_at = NOW()
                """;

            await connection.ExecuteAsync(
                sql,
                new { Text = query.Trim(), NormalizedText = normalizedText });

            _logger.LogDebug("Recorded query: '{Query}' ({ResultCount} results)", query, resultCount);

            // LOGIC: Invalidate cache entries that might include this query.
            InvalidateCacheForText(normalizedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record query: '{Query}'", query);
        }
    }

    /// <inheritdoc/>
    public async Task ExtractSuggestionsAsync(
        Guid documentId,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        // LOGIC: Check license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCode))
            return;

        _logger.LogDebug("Extracting suggestions from document {DocumentId}", documentId);

        var suggestions = new List<(string Text, string Source)>();

        // LOGIC: Extract headings.
        foreach (Match match in HeadingPattern.Matches(content))
        {
            var heading = match.Groups[1].Value.Trim();
            if (heading.Length >= 3 && heading.Length <= 100)
            {
                suggestions.Add((heading, "heading"));
            }
        }

        // LOGIC: Extract bold text.
        foreach (Match match in BoldPattern.Matches(content))
        {
            var text = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Trim();
            if (text.Length >= 3 && text.Length <= 50)
            {
                suggestions.Add((text, "ngram"));
            }
        }

        // LOGIC: Extract code identifiers.
        foreach (Match match in CodePattern.Matches(content))
        {
            var code = match.Groups[1].Value;
            if (code.Length >= 3)
            {
                suggestions.Add((code, "term"));
            }
        }

        if (suggestions.Count == 0)
        {
            _logger.LogDebug("No suggestions extracted from document {DocumentId}", documentId);
            return;
        }

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            // LOGIC: Batch upsert suggestions.
            const string sql = """
                INSERT INTO query_suggestions (text, normalized_text, source, frequency, last_seen_at, document_id)
                VALUES (@Text, @NormalizedText, @Source, 1, NOW(), @DocumentId)
                ON CONFLICT (normalized_text, source)
                DO UPDATE SET
                    frequency = query_suggestions.frequency + 1,
                    last_seen_at = NOW()
                """;

            foreach (var (text, source) in suggestions.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();

                await connection.ExecuteAsync(
                    sql,
                    new
                    {
                        Text = text,
                        NormalizedText = text.ToLowerInvariant(),
                        Source = source,
                        DocumentId = documentId
                    });
            }

            _logger.LogInformation("Extracted {Count} suggestions from document {DocumentId}",
                suggestions.Distinct().Count(), documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract suggestions from document {DocumentId}", documentId);
        }
    }

    /// <inheritdoc/>
    public async Task ClearDocumentSuggestionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = """
                DELETE FROM query_suggestions
                WHERE document_id = @DocumentId
                """;

            var deleted = await connection.ExecuteAsync(sql, new { DocumentId = documentId });

            _logger.LogDebug("Cleared {Count} suggestions for document {DocumentId}", deleted, documentId);

            // LOGIC: Clear entire cache as any prefix might be affected.
            _cache.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear suggestions for document {DocumentId}", documentId);
        }
    }

    #region Private Methods

    /// <summary>
    /// Parses source string to enum.
    /// </summary>
    private static SuggestionSource ParseSource(string source)
    {
        return source switch
        {
            "query_history" => SuggestionSource.QueryHistory,
            "heading" => SuggestionSource.DocumentHeading,
            "ngram" => SuggestionSource.ContentNgram,
            "term" => SuggestionSource.DomainTerm,
            _ => SuggestionSource.ContentNgram
        };
    }

    /// <summary>
    /// Invalidates cache entries that might contain the given text.
    /// </summary>
    private void InvalidateCacheForText(string text)
    {
        // LOGIC: Invalidate all cache entries where the prefix could match this text.
        var keysToRemove = _cache.Keys
            .Where(k => text.StartsWith(k.Split(':')[0], StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Row type for Dapper query.
    /// </summary>
    private record SuggestionRow(string text, int frequency, string source, double score);

    #endregion
}
