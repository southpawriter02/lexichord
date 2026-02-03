// =============================================================================
// File: QueryExpander.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of query expansion for the Relevance Tuner feature.
// =============================================================================
// LOGIC: Expands search queries with synonyms and related terms to improve recall.
//   Expansion sources include:
//   - Built-in technical abbreviations (auth → authentication)
//   - Porter stemmer for morphological variants (implementing → implement)
//   - Future: Terminology database integration
// =============================================================================
// VERSION: v0.5.4b (Query Expansion)
// DEPENDENCIES:
//   - QueryAnalysis (v0.5.4a) for keywords to expand
//   - ILicenseContext (v0.0.4c) for Writer Pro gating
//   - ILogger<T> (v0.0.3b) for structured logging
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Expands queries with synonyms and related terms.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="QueryExpander"/> enriches search queries with synonyms from:
/// <list type="bullet">
///   <item><description>Built-in technical abbreviation database</description></item>
///   <item><description>Porter stemming for morphological variants</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. Expansion caches use
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </para>
/// <para>
/// <b>License Gate:</b> Query expansion is gated at Writer Pro tier.
/// Core tier users receive passthrough (no expansion).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4b as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public sealed class QueryExpander : IQueryExpander
{
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<QueryExpander> _logger;

    /// <summary>
    /// Feature flag for Writer Pro gating.
    /// </summary>
    private const string FeatureCode = "RAG.RelevanceTuner";

    #region Built-in Synonyms

    /// <summary>
    /// Technical abbreviations and their expansions.
    /// </summary>
    private static readonly Dictionary<string, List<(string Term, float Weight)>> TechnicalSynonyms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Authentication/Authorization
            ["auth"] = new() { ("authentication", 0.95f), ("authorization", 0.85f), ("login", 0.7f) },
            ["authn"] = new() { ("authentication", 0.95f) },
            ["authz"] = new() { ("authorization", 0.95f) },
            ["login"] = new() { ("authentication", 0.8f), ("sign in", 0.75f), ("signin", 0.75f) },
            ["logout"] = new() { ("sign out", 0.85f), ("signout", 0.85f) },
            ["oauth"] = new() { ("authentication", 0.7f), ("authorization", 0.7f) },
            ["jwt"] = new() { ("json web token", 0.95f), ("token", 0.7f) },

            // Configuration
            ["config"] = new() { ("configuration", 0.95f), ("settings", 0.8f), ("preferences", 0.6f) },
            ["cfg"] = new() { ("configuration", 0.95f), ("config", 0.9f) },
            ["env"] = new() { ("environment", 0.95f), ("environment variable", 0.8f) },
            ["settings"] = new() { ("configuration", 0.85f), ("preferences", 0.75f), ("options", 0.7f) },

            // API/Web
            ["api"] = new() { ("application programming interface", 0.9f), ("endpoint", 0.7f), ("service", 0.6f) },
            ["rest"] = new() { ("restful", 0.95f), ("api", 0.7f), ("http", 0.6f) },
            ["http"] = new() { ("hypertext transfer protocol", 0.85f), ("web", 0.6f), ("request", 0.5f) },
            ["url"] = new() { ("uniform resource locator", 0.9f), ("link", 0.7f), ("address", 0.6f) },
            ["json"] = new() { ("javascript object notation", 0.9f) },
            ["xml"] = new() { ("extensible markup language", 0.9f) },

            // Database
            ["db"] = new() { ("database", 0.95f) },
            ["sql"] = new() { ("structured query language", 0.9f), ("database", 0.6f), ("query", 0.5f) },
            ["nosql"] = new() { ("non-relational database", 0.85f), ("document database", 0.7f) },
            ["crud"] = new() { ("create read update delete", 0.9f) },

            // Development
            ["repo"] = new() { ("repository", 0.95f) },
            ["impl"] = new() { ("implementation", 0.95f), ("implement", 0.9f) },
            ["func"] = new() { ("function", 0.95f) },
            ["var"] = new() { ("variable", 0.95f) },
            ["param"] = new() { ("parameter", 0.95f), ("argument", 0.8f) },
            ["arg"] = new() { ("argument", 0.95f), ("parameter", 0.8f) },
            ["lib"] = new() { ("library", 0.95f) },
            ["pkg"] = new() { ("package", 0.95f) },
            ["dep"] = new() { ("dependency", 0.95f), ("dependencies", 0.9f) },
            ["deps"] = new() { ("dependencies", 0.95f), ("dependency", 0.9f) },
            ["async"] = new() { ("asynchronous", 0.95f) },
            ["sync"] = new() { ("synchronous", 0.95f), ("synchronize", 0.8f) },

            // Infrastructure
            ["k8s"] = new() { ("kubernetes", 0.95f) },
            ["vm"] = new() { ("virtual machine", 0.95f) },
            ["ci"] = new() { ("continuous integration", 0.95f) },
            ["cd"] = new() { ("continuous delivery", 0.9f), ("continuous deployment", 0.9f) },
            ["cicd"] = new() { ("continuous integration", 0.9f), ("continuous delivery", 0.9f) },

            // Error Handling
            ["err"] = new() { ("error", 0.95f) },
            ["ex"] = new() { ("exception", 0.95f), ("error", 0.7f) },
            ["exception"] = new() { ("error", 0.8f), ("failure", 0.6f) },
            ["error"] = new() { ("exception", 0.8f), ("failure", 0.7f), ("issue", 0.5f) },

            // Documentation
            ["doc"] = new() { ("documentation", 0.95f), ("document", 0.85f) },
            ["docs"] = new() { ("documentation", 0.95f), ("documents", 0.85f) },
            ["readme"] = new() { ("documentation", 0.8f), ("getting started", 0.6f) },
            ["spec"] = new() { ("specification", 0.95f) },
            ["specs"] = new() { ("specifications", 0.95f) },

            // Testing
            ["test"] = new() { ("testing", 0.9f), ("unit test", 0.7f) },
            ["qa"] = new() { ("quality assurance", 0.95f), ("testing", 0.7f) },
            ["e2e"] = new() { ("end to end", 0.95f), ("integration test", 0.7f) },

            // Security
            ["sec"] = new() { ("security", 0.95f) },
            ["tls"] = new() { ("transport layer security", 0.95f), ("ssl", 0.8f), ("https", 0.6f) },
            ["ssl"] = new() { ("secure sockets layer", 0.9f), ("tls", 0.85f), ("https", 0.6f) }
        };

    #endregion

    /// <summary>
    /// Creates a new <see cref="QueryExpander"/> instance.
    /// </summary>
    /// <param name="licenseContext">License context for Writer Pro gating.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public QueryExpander(
        ILicenseContext licenseContext,
        ILogger<QueryExpander> logger)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("QueryExpander initialized with {SynonymCount} built-in synonym entries",
            TechnicalSynonyms.Count);
    }

    /// <inheritdoc/>
    public async Task<ExpandedQuery> ExpandAsync(
        QueryAnalysis analysis,
        ExpansionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        options ??= ExpansionOptions.Default;

        // LOGIC: Check license for Writer Pro tier.
        if (!_licenseContext.IsFeatureEnabled(FeatureCode))
        {
            _logger.LogDebug("Query expansion skipped: Writer Pro license required");
            return ExpandedQuery.NoExpansion(analysis);
        }

        // LOGIC: Return passthrough for empty keyword lists.
        if (analysis.Keywords.Count == 0)
        {
            _logger.LogDebug("Query expansion skipped: No keywords to expand");
            return ExpandedQuery.NoExpansion(analysis);
        }

        _logger.LogDebug("Expanding query with {KeywordCount} keywords", analysis.Keywords.Count);

        var expansions = new Dictionary<string, IReadOnlyList<Synonym>>();
        var allKeywords = new List<string>(analysis.Keywords);

        foreach (var keyword in analysis.Keywords)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (databaseSynonyms, algorithmicSynonyms) = await GetSynonymsAsync(keyword, options, cancellationToken);

            // LOGIC: Only add to Expansions dictionary if there are database synonyms.
            // Algorithmic variants are added to keywords but not tracked in Expansions.
            if (databaseSynonyms.Count > 0)
            {
                expansions[keyword] = databaseSynonyms;

                _logger.LogDebug("Expanded '{Term}' to {SynonymCount} database synonyms",
                    keyword, databaseSynonyms.Count);
            }

            // Add all synonym terms (both database and algorithmic) to expanded keywords.
            foreach (var synonym in databaseSynonyms.Concat(algorithmicSynonyms))
            {
                if (!allKeywords.Contains(synonym.Term, StringComparer.OrdinalIgnoreCase))
                {
                    allKeywords.Add(synonym.Term);
                }
            }

            if (algorithmicSynonyms.Count > 0)
            {
                _logger.LogDebug("Added {SynonymCount} algorithmic variants for '{Term}'",
                    algorithmicSynonyms.Count, keyword);
            }
        }

        var result = new ExpandedQuery(
            Original: analysis,
            Expansions: expansions.AsReadOnly(),
            ExpandedKeywords: allKeywords.AsReadOnly(),
            TotalTermCount: allKeywords.Count);

        _logger.LogInformation("Query expanded: {OriginalCount} → {TotalCount} terms",
            analysis.KeywordCount, result.TotalTermCount);

        return result;
    }

    #region Private Methods

    /// <summary>
    /// Gets synonyms for a single keyword, returning database and algorithmic synonyms separately.
    /// </summary>
    private Task<(IReadOnlyList<Synonym> Database, IReadOnlyList<Synonym> Algorithmic)> GetSynonymsAsync(
        string keyword,
        ExpansionOptions options,
        CancellationToken cancellationToken)
    {
        var databaseSynonyms = new List<Synonym>();
        var algorithmicSynonyms = new List<Synonym>();

        // LOGIC: Look up in built-in technical synonyms.
        if (TechnicalSynonyms.TryGetValue(keyword, out var builtInSynonyms))
        {
            foreach (var (term, weight) in builtInSynonyms)
            {
                if (weight >= options.MinSynonymWeight && databaseSynonyms.Count < options.MaxSynonymsPerTerm)
                {
                    databaseSynonyms.Add(new Synonym(term, weight, SynonymSource.TerminologyDatabase));
                }
            }
        }

        // LOGIC: Add algorithmic variants (stemming) if enabled.
        // Only add algorithmic variants if they meet the minimum weight threshold.
        if (options.IncludeAlgorithmic)
        {
            const float algorithmicWeight = 0.75f;
            if (algorithmicWeight >= options.MinSynonymWeight)
            {
                var totalCount = databaseSynonyms.Count;
                var stemmed = GetStemVariants(keyword);
                foreach (var variant in stemmed)
                {
                    if (totalCount >= options.MaxSynonymsPerTerm) break;
                    if (!databaseSynonyms.Any(s => s.Term.Equals(variant, StringComparison.OrdinalIgnoreCase)) &&
                        !algorithmicSynonyms.Any(s => s.Term.Equals(variant, StringComparison.OrdinalIgnoreCase)))
                    {
                        algorithmicSynonyms.Add(new Synonym(variant, algorithmicWeight, SynonymSource.Algorithmic));
                        totalCount++;
                    }
                }
            }
        }

        // LOGIC: Sort by weight descending and limit.
        var dbResult = databaseSynonyms
            .OrderByDescending(s => s.Weight)
            .Take(options.MaxSynonymsPerTerm)
            .ToList()
            .AsReadOnly();

        var algoResult = algorithmicSynonyms
            .OrderByDescending(s => s.Weight)
            .Take(Math.Max(0, options.MaxSynonymsPerTerm - dbResult.Count))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<(IReadOnlyList<Synonym>, IReadOnlyList<Synonym>)>((dbResult, algoResult));
    }

    /// <summary>
    /// Gets stemmed variants of a word using simple suffix rules.
    /// </summary>
    private static IEnumerable<string> GetStemVariants(string word)
    {
        var variants = new List<string>();
        var lower = word.ToLowerInvariant();

        // LOGIC: Common suffix transformations.
        if (lower.EndsWith("ing"))
        {
            // implementing → implement
            var stem = lower[..^3];
            if (stem.Length >= 3)
            {
                variants.Add(stem);
                // running → run (double consonant)
                if (stem.Length >= 2 && stem[^1] == stem[^2])
                {
                    variants.Add(stem[..^1]);
                }
            }
        }
        else if (lower.EndsWith("tion"))
        {
            // authentication → authenticate
            var stem = lower[..^4];
            if (stem.Length >= 3)
            {
                variants.Add(stem + "e");
                variants.Add(stem);
            }
        }
        else if (lower.EndsWith("ation"))
        {
            // configuration → configure
            var stem = lower[..^5];
            if (stem.Length >= 3)
            {
                variants.Add(stem + "e");
                variants.Add(stem);
            }
        }
        else if (lower.EndsWith("ed"))
        {
            // configured → configure
            var stem = lower[..^2];
            if (stem.Length >= 3)
            {
                variants.Add(stem);
                variants.Add(stem + "e");
            }
        }
        else if (lower.EndsWith("es") && lower.Length > 4)
        {
            // services → service
            variants.Add(lower[..^1]);
        }
        else if (lower.EndsWith("s") && !lower.EndsWith("ss") && lower.Length > 3)
        {
            // tokens → token
            variants.Add(lower[..^1]);
        }
        else
        {
            // Add common suffixed forms for base words
            if (!lower.EndsWith("e"))
            {
                variants.Add(lower + "ing");
                variants.Add(lower + "ed");
            }
            variants.Add(lower + "s");
        }

        return variants.Distinct().Where(v => v.Length >= 3);
    }

    #endregion
}
