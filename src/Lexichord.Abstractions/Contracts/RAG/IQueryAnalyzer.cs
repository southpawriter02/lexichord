// =============================================================================
// File: IQueryAnalyzer.cs
// Project: Lexichord.Abstractions
// Description: Interface for query analysis in the Relevance Tuner feature.
// =============================================================================
// LOGIC: Defines the contract for analyzing search queries to extract:
//   - Keywords (with stop-word filtering)
//   - Entities (code identifiers, file paths, domain terms)
//   - Intent (factual, procedural, conceptual, navigational)
//   - Specificity score (0.0-1.0)
// =============================================================================
// VERSION: v0.5.4a (Query Analyzer)
// DEPENDENCIES:
//   - ITerminologyRepository (v0.2.2b) for domain term recognition
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Analyzes search queries to extract structure, intent, and metadata.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IQueryAnalyzer"/> is the entry point to the Relevance Tuner pipeline.
/// It parses raw user queries to identify keywords, recognize entities, detect
/// intent, and score specificity — enabling downstream components like
/// <see cref="IQueryExpander"/> to enhance search quality.
/// </para>
/// <para>
/// <b>Processing Steps:</b>
/// <list type="number">
///   <item><description>Tokenize query into words</description></item>
///   <item><description>Filter stop-words (common words like "the", "is", "a")</description></item>
///   <item><description>Recognize entities via pattern matching and terminology lookup</description></item>
///   <item><description>Detect intent based on keyword heuristics</description></item>
///   <item><description>Calculate specificity score based on keyword/entity density</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the analyzer
/// is registered as a singleton and may be called concurrently.
/// </para>
/// <para>
/// <b>Performance Target:</b> Analysis should complete in &lt; 20ms (P95).
/// </para>
/// <para>
/// <b>License Gate:</b> Basic analysis available to all tiers; advanced features
/// (entity recognition, specificity scoring) gated at Writer Pro via
/// <c>FeatureFlags.RAG.RelevanceTuner</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4a as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SearchOrchestrator(IQueryAnalyzer analyzer)
/// {
///     public async Task&lt;SearchResult&gt; SearchAsync(string query)
///     {
///         var analysis = analyzer.Analyze(query);
///
///         // Log analysis for debugging
///         _logger.LogDebug(
///             "Query analyzed: Intent={Intent}, Keywords={Count}, Specificity={Specificity}",
///             analysis.Intent,
///             analysis.KeywordCount,
///             analysis.Specificity);
///
///         // Use analysis for expansion, search, etc.
///         // ...
///     }
/// }
/// </code>
/// </example>
public interface IQueryAnalyzer
{
    /// <summary>
    /// Analyzes a search query to extract keywords, entities, and intent.
    /// </summary>
    /// <param name="query">
    /// The raw search query string. May be null, empty, or whitespace.
    /// </param>
    /// <returns>
    /// Analysis results including keywords, entities, intent, and specificity.
    /// Returns <see cref="QueryAnalysis.Empty"/> for null/empty/whitespace queries.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Analysis is performed synchronously as it operates on in-memory data
    /// (stop-word lists, regex patterns) and should complete in &lt; 20ms.
    /// </para>
    /// <para>
    /// <b>Processing Order:</b>
    /// <list type="number">
    ///   <item><description>Normalize whitespace and case</description></item>
    ///   <item><description>Extract tokens</description></item>
    ///   <item><description>Filter stop-words</description></item>
    ///   <item><description>Recognize entities via patterns</description></item>
    ///   <item><description>Check terminology database for domain terms</description></item>
    ///   <item><description>Detect intent from heuristic signals</description></item>
    ///   <item><description>Calculate specificity score</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var analysis = analyzer.Analyze("how to configure OAuth authentication");
    ///
    /// // Result:
    /// // OriginalQuery: "how to configure OAuth authentication"
    /// // Keywords: ["configure", "OAuth", "authentication"]
    /// // Entities: [("OAuth", DomainTerm, 17)]
    /// // Intent: Procedural
    /// // Specificity: 0.65
    /// </code>
    /// </example>
    QueryAnalysis Analyze(string? query);
}
