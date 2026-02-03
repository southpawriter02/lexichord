// =============================================================================
// File: QueryAnalysis.cs
// Project: Lexichord.Abstractions
// Description: Query analysis types for the Relevance Tuner feature (v0.5.4a).
// =============================================================================
// LOGIC: Defines records and enums for query analysis results including:
//   - QueryAnalysis: Complete analysis result with keywords, entities, intent
//   - QueryEntity: Recognized entity within a query
//   - EntityType: Types of entities that can be recognized
//   - QueryIntent: Detected intent category for queries
// =============================================================================
// VERSION: v0.5.4a (Query Analyzer)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Types of entities that can be recognized in search queries.
/// </summary>
/// <remarks>
/// <para>
/// Entity types help the search system understand the semantic meaning
/// of query terms and adjust ranking accordingly.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4a as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public enum EntityType
{
    /// <summary>
    /// Code identifier like function or class name (e.g., "IQueryAnalyzer", "HandleAsync").
    /// </summary>
    /// <remarks>
    /// Detected via PascalCase, camelCase, or snake_case patterns.
    /// </remarks>
    CodeIdentifier,

    /// <summary>
    /// File path or glob pattern (e.g., "src/Models/User.cs", "*.md").
    /// </summary>
    /// <remarks>
    /// Detected via path separator characters (/, \) or extension patterns.
    /// </remarks>
    FilePath,

    /// <summary>
    /// Domain-specific term from terminology database (e.g., "OAuth", "JWT").
    /// </summary>
    /// <remarks>
    /// Detected via lookup against the terminology repository.
    /// </remarks>
    DomainTerm,

    /// <summary>
    /// Version number or reference (e.g., "v1.2.3", "2.0").
    /// </summary>
    /// <remarks>
    /// Detected via semantic version patterns.
    /// </remarks>
    VersionNumber,

    /// <summary>
    /// Error code or status code (e.g., "HTTP 404", "ERR_001").
    /// </summary>
    /// <remarks>
    /// Detected via error code patterns (alphanumeric prefixes with numbers).
    /// </remarks>
    ErrorCode
}

/// <summary>
/// Detected intent category for search queries.
/// </summary>
/// <remarks>
/// <para>
/// Intent detection helps the search system prioritize certain document types
/// and adjust result ranking based on what the user is looking for.
/// </para>
/// <para>
/// <b>Detection Heuristics:</b>
/// <list type="table">
///   <listheader>
///     <term>Signal</term>
///     <description>Intent</description>
///   </listheader>
///   <item>
///     <term>"what is", "definition", "meaning"</term>
///     <description>Factual</description>
///   </item>
///   <item>
///     <term>"how to", "steps", "tutorial"</term>
///     <description>Procedural</description>
///   </item>
///   <item>
///     <term>"why", "explain", "understand"</term>
///     <description>Conceptual</description>
///   </item>
///   <item>
///     <term>"where is", "find", "locate", paths</term>
///     <description>Navigational</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4a as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public enum QueryIntent
{
    /// <summary>
    /// Looking for a specific fact or definition.
    /// </summary>
    /// <remarks>
    /// Example: "what is OAuth", "JWT definition"
    /// </remarks>
    Factual,

    /// <summary>
    /// Looking for step-by-step instructions.
    /// </summary>
    /// <remarks>
    /// Example: "how to configure authentication", "setup tutorial"
    /// </remarks>
    Procedural,

    /// <summary>
    /// Looking to understand a concept or architecture.
    /// </summary>
    /// <remarks>
    /// Example: "why use dependency injection", "explain CQRS pattern"
    /// </remarks>
    Conceptual,

    /// <summary>
    /// Looking to navigate to a specific location.
    /// </summary>
    /// <remarks>
    /// Example: "where is the config file", "find UserService.cs"
    /// </remarks>
    Navigational
}

/// <summary>
/// An entity recognized within a search query.
/// </summary>
/// <param name="Text">The entity text as it appears in the query.</param>
/// <param name="Type">The type of entity (code, path, domain term, etc.).</param>
/// <param name="StartIndex">Start position in the original query string.</param>
/// <remarks>
/// <para>
/// Entity recognition helps the search system understand query structure
/// and can be used for targeted expansion or highlighting.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4a as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query: "find IQueryAnalyzer in src/Services"
/// // Entities:
/// //   - ("IQueryAnalyzer", CodeIdentifier, 5)
/// //   - ("src/Services", FilePath, 22)
/// var entity = new QueryEntity("IQueryAnalyzer", EntityType.CodeIdentifier, 5);
/// </code>
/// </example>
public record QueryEntity(string Text, EntityType Type, int StartIndex);

/// <summary>
/// Results of query analysis including keywords, entities, intent, and specificity.
/// </summary>
/// <param name="OriginalQuery">The original query string as submitted.</param>
/// <param name="Keywords">Extracted meaningful keywords (stop-words removed).</param>
/// <param name="Entities">Recognized entities (code patterns, file paths, domain terms).</param>
/// <param name="Intent">Detected query intent category.</param>
/// <param name="Specificity">Score from 0.0 (vague) to 1.0 (highly specific).</param>
/// <remarks>
/// <para>
/// QueryAnalysis is the output of <see cref="IQueryAnalyzer.Analyze"/> and serves
/// as input to subsequent pipeline stages including expansion and search execution.
/// </para>
/// <para>
/// <b>Specificity Scoring:</b>
/// <list type="bullet">
///   <item><description>0.0-0.3: Vague query (few keywords, no entities)</description></item>
///   <item><description>0.3-0.7: Moderate specificity (some keywords, partial entities)</description></item>
///   <item><description>0.7-1.0: Highly specific (many keywords, clear entities)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4a as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var analysis = analyzer.Analyze("how to configure OAuth authentication");
/// // OriginalQuery: "how to configure OAuth authentication"
/// // Keywords: ["configure", "OAuth", "authentication"]
/// // Intent: Procedural
/// // Specificity: 0.65
/// </code>
/// </example>
public record QueryAnalysis(
    string OriginalQuery,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<QueryEntity> Entities,
    QueryIntent Intent,
    float Specificity)
{
    /// <summary>
    /// Gets whether the query has any recognized entities.
    /// </summary>
    public bool HasEntities => Entities.Count > 0;

    /// <summary>
    /// Gets the number of extracted keywords.
    /// </summary>
    public int KeywordCount => Keywords.Count;

    /// <summary>
    /// Creates an empty analysis result for an empty or invalid query.
    /// </summary>
    /// <param name="query">The original query string.</param>
    /// <returns>An analysis with no keywords, entities, factual intent, and zero specificity.</returns>
    public static QueryAnalysis Empty(string query = "") =>
        new(
            OriginalQuery: query,
            Keywords: Array.Empty<string>(),
            Entities: Array.Empty<QueryEntity>(),
            Intent: QueryIntent.Factual,
            Specificity: 0.0f);
}
