// =============================================================================
// File: QueryAnalyzer.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of query analysis for the Relevance Tuner feature.
// =============================================================================
// LOGIC: Analyzes search queries to extract keywords, entities, intent, and
//   specificity score. Processing includes:
//   - Stop-word filtering (English common words + technical noise words)
//   - Entity recognition via regex patterns (code, paths, versions, errors)
//   - Intent detection via keyword heuristics
//   - Specificity scoring based on keyword density and entity presence
// =============================================================================
// VERSION: v0.5.4a (Query Analyzer)
// DEPENDENCIES:
//   - ILicenseContext (v0.0.4c) for advanced feature gating
//   - ILogger<T> (v0.0.3b) for structured logging
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Analyzes search queries to extract structure, intent, and metadata.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="QueryAnalyzer"/> is the entry point to the Relevance Tuner pipeline.
/// It performs synchronous analysis (typically &lt; 5ms) on query text to identify:
/// <list type="bullet">
///   <item><description>Keywords: Meaningful terms after stop-word removal</description></item>
///   <item><description>Entities: Code identifiers, file paths, versions, error codes</description></item>
///   <item><description>Intent: Factual, Procedural, Conceptual, or Navigational</description></item>
///   <item><description>Specificity: Score indicating query precision (0.0-1.0)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. All pattern matching uses
/// pre-compiled immutable regex instances.
/// </para>
/// <para>
/// <b>Performance:</b> Analysis typically completes in &lt; 5ms for queries up to
/// 1000 characters. Target P95 latency is &lt; 20ms.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4a as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public sealed class QueryAnalyzer : IQueryAnalyzer
{
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<QueryAnalyzer> _logger;

    #region Stop Words

    /// <summary>
    /// Common English stop words that add no semantic value to search queries.
    /// </summary>
    private static readonly HashSet<string> EnglishStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Articles
        "a", "an", "the",
        // Prepositions
        "in", "on", "at", "to", "for", "of", "with", "by", "from", "up", "about",
        "into", "over", "after", "beneath", "under", "above",
        // Conjunctions
        "and", "or", "but", "nor", "so", "yet", "both", "either", "neither",
        // Pronouns
        "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them",
        "my", "your", "his", "its", "our", "their", "this", "that", "these", "those",
        // Auxiliary verbs
        "is", "am", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "having",
        "do", "does", "did", "doing",
        "will", "would", "shall", "should", "may", "might", "must", "can", "could",
        // Common verbs
        "get", "got", "getting",
        // Question words (kept for intent detection, filtered from keywords)
        "what", "when", "where", "which", "who", "whom", "whose", "why", "how",
        // Other common words
        "if", "then", "than", "because", "while", "although", "though",
        "just", "only", "also", "very", "really", "actually", "even",
        "any", "some", "all", "each", "every", "no", "not", "yes",
        "here", "there", "now", "then", "today", "yesterday", "tomorrow",
        "more", "most", "less", "least", "much", "many", "few", "several",
        "first", "second", "third", "last", "next", "previous",
        "new", "old", "good", "bad", "best", "worst", "other", "same", "different"
    };

    /// <summary>
    /// Technical noise words that are too common in documentation.
    /// </summary>
    private static readonly HashSet<string> TechnicalStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "using", "use", "used", "uses",
        "example", "examples",
        "see", "look", "check", "review",
        "please", "need", "want", "like",
        "way", "ways", "thing", "things",
        "work", "works", "working",
        "make", "makes", "making",
        "create", "creates", "creating",
        "add", "adds", "adding",
        "set", "sets", "setting",
        "run", "runs", "running",
        "show", "shows", "showing",
        "help", "helps", "helping"
    };

    #endregion

    #region Entity Patterns

    /// <summary>
    /// Matches PascalCase or camelCase identifiers (e.g., IQueryAnalyzer, handleAsync).
    /// </summary>
    private static readonly Regex CodeIdentifierPattern = new(
        @"\b([A-Z][a-z0-9]+(?:[A-Z][a-z0-9]+)+|[a-z]+(?:[A-Z][a-z0-9]+)+)\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches file paths (e.g., src/Services/MyService.cs, ./config.json).
    /// </summary>
    private static readonly Regex FilePathPattern = new(
        @"(?:\.{0,2}/)?(?:[\w-]+/)+[\w.-]+(?:\.\w+)?|\*\.\w+|[\w-]+\.\w{2,4}\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches semantic version numbers (e.g., v1.2.3, 2.0, v0.5.4a).
    /// </summary>
    private static readonly Regex VersionPattern = new(
        @"\bv?\d+(?:\.\d+)+[a-z]?\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches error/status codes (e.g., HTTP 404, ERR_001, E0001).
    /// </summary>
    private static readonly Regex ErrorCodePattern = new(
        @"\b(?:HTTP\s*)?\d{3}\b|\b(?:ERR|ERROR|WARN|INFO|E|W)\s*[\-_]?\d{3,5}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #endregion

    #region Intent Detection Patterns

    /// <summary>
    /// Patterns indicating Procedural intent (how-to, step-by-step).
    /// </summary>
    private static readonly string[] ProceduralSignals =
    {
        "how to", "how do", "how can", "how should",
        "steps to", "steps for", "step by step",
        "tutorial", "guide", "walkthrough",
        "setup", "set up", "configure", "install", "deploy",
        "implement", "create", "build", "make"
    };

    /// <summary>
    /// Patterns indicating Factual intent (definition, specific fact).
    /// </summary>
    private static readonly string[] FactualSignals =
    {
        "what is", "what are", "what does", "what's",
        "definition", "meaning", "define",
        "syntax", "format", "schema",
        "list of", "types of", "kinds of"
    };

    /// <summary>
    /// Patterns indicating Conceptual intent (understanding, explanation).
    /// </summary>
    private static readonly string[] ConceptualSignals =
    {
        "why", "explain", "understand", "understanding",
        "concept", "architecture", "design", "pattern",
        "difference between", "compare", "versus", "vs",
        "best practice", "recommendation", "when to use"
    };

    /// <summary>
    /// Patterns indicating Navigational intent (finding specific location).
    /// </summary>
    private static readonly string[] NavigationalSignals =
    {
        "where is", "where are", "where can",
        "find", "locate", "search for", "look for",
        "path to", "location of", "file", "folder", "directory"
    };

    #endregion

    /// <summary>
    /// Creates a new <see cref="QueryAnalyzer"/> instance.
    /// </summary>
    /// <param name="licenseContext">License context for advanced feature gating.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public QueryAnalyzer(
        ILicenseContext licenseContext,
        ILogger<QueryAnalyzer> logger)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("QueryAnalyzer initialized");
    }

    /// <inheritdoc/>
    public QueryAnalysis Analyze(string? query)
    {
        // LOGIC: Handle null/empty queries early.
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("Empty query received, returning empty analysis");
            return QueryAnalysis.Empty(query ?? string.Empty);
        }

        var normalizedQuery = NormalizeQuery(query);
        _logger.LogDebug("Analyzing query: {Query}", normalizedQuery);

        // LOGIC: Extract keywords by tokenizing and filtering stop words.
        var tokens = Tokenize(normalizedQuery);
        var keywords = FilterStopWords(tokens);

        // LOGIC: Recognize entities via pattern matching.
        var entities = RecognizeEntities(query);

        // LOGIC: Detect intent from query signals.
        var intent = DetectIntent(normalizedQuery);

        // LOGIC: Calculate specificity score.
        var specificity = CalculateSpecificity(keywords, entities, normalizedQuery);

        var analysis = new QueryAnalysis(
            OriginalQuery: query,
            Keywords: keywords.ToList().AsReadOnly(),
            Entities: entities.ToList().AsReadOnly(),
            Intent: intent,
            Specificity: specificity);

        _logger.LogDebug(
            "Query analyzed: Intent={Intent}, Keywords={KeywordCount}, Entities={EntityCount}, Specificity={Specificity:F2}",
            analysis.Intent,
            analysis.KeywordCount,
            analysis.Entities.Count,
            analysis.Specificity);

        return analysis;
    }

    #region Private Methods

    /// <summary>
    /// Normalizes a query string for processing.
    /// </summary>
    private static string NormalizeQuery(string query)
    {
        // LOGIC: Normalize whitespace and trim.
        return Regex.Replace(query.Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Tokenizes a query into individual words.
    /// </summary>
    private static IEnumerable<string> Tokenize(string query)
    {
        // LOGIC: Split on whitespace and punctuation, keeping alphanumeric tokens.
        return Regex.Split(query, @"[\s\p{P}]+")
            .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 1);
    }

    /// <summary>
    /// Filters stop words from tokens.
    /// </summary>
    private static IEnumerable<string> FilterStopWords(IEnumerable<string> tokens)
    {
        return tokens
            .Where(t => !EnglishStopWords.Contains(t) && !TechnicalStopWords.Contains(t))
            .Select(t => t.ToLowerInvariant())
            .Distinct();
    }

    /// <summary>
    /// Recognizes entities in the query via pattern matching.
    /// </summary>
    private List<QueryEntity> RecognizeEntities(string query)
    {
        var entities = new List<QueryEntity>();

        // LOGIC: Match code identifiers (PascalCase, camelCase).
        foreach (Match match in CodeIdentifierPattern.Matches(query))
        {
            // Avoid false positives for common words that happen to match the pattern.
            if (!EnglishStopWords.Contains(match.Value) && match.Value.Length >= 3)
            {
                entities.Add(new QueryEntity(match.Value, EntityType.CodeIdentifier, match.Index));
                _logger.LogDebug("Entity recognized: {Text} ({Type})", match.Value, EntityType.CodeIdentifier);
            }
        }

        // LOGIC: Match file paths.
        foreach (Match match in FilePathPattern.Matches(query))
        {
            if (match.Value.Contains('/') || match.Value.Contains('\\') || match.Value.Contains('.'))
            {
                entities.Add(new QueryEntity(match.Value, EntityType.FilePath, match.Index));
                _logger.LogDebug("Entity recognized: {Text} ({Type})", match.Value, EntityType.FilePath);
            }
        }

        // LOGIC: Match version numbers.
        foreach (Match match in VersionPattern.Matches(query))
        {
            entities.Add(new QueryEntity(match.Value, EntityType.VersionNumber, match.Index));
            _logger.LogDebug("Entity recognized: {Text} ({Type})", match.Value, EntityType.VersionNumber);
        }

        // LOGIC: Match error codes.
        foreach (Match match in ErrorCodePattern.Matches(query))
        {
            entities.Add(new QueryEntity(match.Value, EntityType.ErrorCode, match.Index));
            _logger.LogDebug("Entity recognized: {Text} ({Type})", match.Value, EntityType.ErrorCode);
        }

        // LOGIC: Remove duplicates (same text at same position).
        return entities
            .GroupBy(e => (e.Text, e.StartIndex))
            .Select(g => g.First())
            .OrderBy(e => e.StartIndex)
            .ToList();
    }

    /// <summary>
    /// Detects query intent from keyword signals.
    /// </summary>
    private QueryIntent DetectIntent(string query)
    {
        var lowerQuery = query.ToLowerInvariant();

        // LOGIC: Check intent signals in priority order.
        // Procedural intent is checked first as it's often the most actionable.
        if (ProceduralSignals.Any(s => lowerQuery.Contains(s)))
        {
            return QueryIntent.Procedural;
        }

        // LOGIC: Navigational intent for location-based queries.
        if (NavigationalSignals.Any(s => lowerQuery.Contains(s)) ||
            FilePathPattern.IsMatch(query))
        {
            return QueryIntent.Navigational;
        }

        // LOGIC: Conceptual intent for understanding queries.
        if (ConceptualSignals.Any(s => lowerQuery.Contains(s)))
        {
            return QueryIntent.Conceptual;
        }

        // LOGIC: Factual intent for definition queries.
        if (FactualSignals.Any(s => lowerQuery.Contains(s)))
        {
            return QueryIntent.Factual;
        }

        // LOGIC: Default to Factual for simple keyword queries.
        return QueryIntent.Factual;
    }

    /// <summary>
    /// Calculates specificity score based on query characteristics.
    /// </summary>
    private static float CalculateSpecificity(
        IEnumerable<string> keywords,
        IEnumerable<QueryEntity> entities,
        string query)
    {
        var keywordList = keywords.ToList();
        var entityList = entities.ToList();

        // LOGIC: Base score from keyword count (more keywords = more specific).
        float keywordScore = keywordList.Count switch
        {
            <= 0 => 0.0f,
            1 => 0.2f,
            2 => 0.4f,
            3 => 0.6f,
            >= 4 => 0.7f
        };

        // LOGIC: Entity bonus (entities indicate specific technical queries).
        float entityBonus = entityList.Count switch
        {
            <= 0 => 0.0f,
            1 => 0.15f,
            >= 2 => 0.25f
        };

        // LOGIC: Query length factor (longer queries tend to be more specific).
        float lengthFactor = query.Length switch
        {
            < 10 => 0.0f,
            < 20 => 0.05f,
            < 50 => 0.1f,
            _ => 0.15f
        };

        // LOGIC: Combine factors and clamp to 0.0-1.0 range.
        var specificity = Math.Min(1.0f, keywordScore + entityBonus + lengthFactor);

        return (float)Math.Round(specificity, 2);
    }

    #endregion
}
