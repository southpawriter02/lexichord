// =============================================================================
// File: QuerySuggestion.cs
// Project: Lexichord.Abstractions
// Description: Query suggestion types for the Relevance Tuner feature (v0.5.4c).
// =============================================================================
// LOGIC: Defines records and enums for query autocomplete suggestions:
//   - QuerySuggestion: A single autocomplete suggestion
//   - SuggestionSource: Origin of the suggestion (history, heading, ngram, term)
// =============================================================================
// VERSION: v0.5.4c (Query Suggestions)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Source of a query autocomplete suggestion.
/// </summary>
/// <remarks>
/// <para>
/// Knowing the source helps with:
/// <list type="bullet">
///   <item><description>Ranking (history may be more relevant)</description></item>
///   <item><description>UI display (show source icon/badge)</description></item>
///   <item><description>Filtering (show only certain sources)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4c as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public enum SuggestionSource
{
    /// <summary>
    /// Previously executed query from history.
    /// </summary>
    /// <remarks>
    /// Highest relevance; user has searched for this before.
    /// </remarks>
    QueryHistory,

    /// <summary>
    /// Extracted from document headings during indexing.
    /// </summary>
    /// <remarks>
    /// High relevance; headings represent key document topics.
    /// </remarks>
    DocumentHeading,

    /// <summary>
    /// Extracted from content n-grams during indexing.
    /// </summary>
    /// <remarks>
    /// Medium relevance; common phrases from document content.
    /// </remarks>
    ContentNgram,

    /// <summary>
    /// Domain term from terminology database.
    /// </summary>
    /// <remarks>
    /// High relevance; domain-specific vocabulary.
    /// </remarks>
    DomainTerm
}

/// <summary>
/// A query autocomplete suggestion.
/// </summary>
/// <param name="Text">The suggested query text.</param>
/// <param name="Frequency">How often this suggestion has been used/seen.</param>
/// <param name="Source">Source of the suggestion (history, heading, ngram, term).</param>
/// <param name="Score">Relevance score for ranking (0.0-1.0).</param>
/// <remarks>
/// <para>
/// <see cref="QuerySuggestion"/> represents a single autocomplete option shown
/// to users as they type in the search box. Suggestions help users:
/// <list type="bullet">
///   <item><description>Discover content they didn't know existed</description></item>
///   <item><description>Search faster with fewer keystrokes</description></item>
///   <item><description>Use consistent terminology</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Ranking Algorithm:</b>
/// Score is computed from frequency, recency, and source weighting:
/// <code>
/// Score = (FrequencyFactor * 0.4) + (RecencyFactor * 0.3) + (SourceWeight * 0.3)
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4c as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // User types "token"
/// // Suggestions returned:
/// var suggestions = new[]
/// {
///     new QuerySuggestion("token refresh", 5, SuggestionSource.QueryHistory, 0.95f),
///     new QuerySuggestion("Token Validation", 10, SuggestionSource.DocumentHeading, 0.85f),
///     new QuerySuggestion("token authentication", 3, SuggestionSource.ContentNgram, 0.75f)
/// };
/// </code>
/// </example>
public record QuerySuggestion(
    string Text,
    int Frequency,
    SuggestionSource Source,
    float Score)
{
    /// <summary>
    /// Gets the score formatted as a percentage string.
    /// </summary>
    public string ScorePercent => $"{Score * 100:F0}%";

    /// <summary>
    /// Gets a display-friendly source label.
    /// </summary>
    public string SourceLabel => Source switch
    {
        SuggestionSource.QueryHistory => "Recent Query",
        SuggestionSource.DocumentHeading => "Document Heading",
        SuggestionSource.ContentNgram => "Suggestion",
        SuggestionSource.DomainTerm => "Domain Term",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the icon identifier for UI display.
    /// </summary>
    public string SourceIcon => Source switch
    {
        SuggestionSource.QueryHistory => "🕐",
        SuggestionSource.DocumentHeading => "📄",
        SuggestionSource.ContentNgram => "💡",
        SuggestionSource.DomainTerm => "📚",
        _ => "❓"
    };
}
