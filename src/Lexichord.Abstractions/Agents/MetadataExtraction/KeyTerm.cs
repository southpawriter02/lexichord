// -----------------------------------------------------------------------
// <copyright file="KeyTerm.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Defines the KeyTerm record for representing extracted key terms (v0.7.6b).
//   Key terms are words or phrases central to a document's topic, extracted by
//   the MetadataExtractor using LLM analysis.
//
//   Each key term includes:
//   - Importance score (0.0-1.0) indicating centrality to the document
//   - Frequency count of occurrences
//   - Technical flag for domain-specific terminology
//   - Optional definition if found in document
//   - Optional category for hierarchical organization
//   - Optional related terms for semantic connections
//
//   Introduced in: v0.7.6b
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.MetadataExtraction;

/// <summary>
/// Represents a key term extracted from a document with importance scoring and metadata.
/// </summary>
/// <remarks>
/// <para>
/// Key terms are extracted by the <see cref="IMetadataExtractor"/> and represent
/// words or phrases that are central to understanding the document's content.
/// Terms are ranked by importance, with higher scores indicating greater
/// relevance to the document's main topic.
/// </para>
/// <para>
/// Importance scoring considers:
/// </para>
/// <list type="bullet">
///   <item><description>Frequency of occurrence in the document</description></item>
///   <item><description>Position in the document (title, headings, body)</description></item>
///   <item><description>Semantic significance to the main topic</description></item>
///   <item><description>Uniqueness relative to common vocabulary</description></item>
/// </list>
/// <para><b>Introduced in:</b> v0.7.6b</para>
/// </remarks>
/// <param name="Term">The extracted term or phrase (may be multiple words).</param>
/// <param name="Importance">
/// Importance score ranging from 0.0 (tangential) to 1.0 (core topic).
/// Terms with scores above 0.7 are typically central to the document's main theme.
/// </param>
/// <param name="Frequency">
/// Number of times the term appears in the document.
/// Higher frequency often correlates with importance, but not always.
/// </param>
/// <param name="IsTechnical">
/// Whether this is a technical or domain-specific term that may require
/// specialized knowledge to understand. Technical terms are preserved
/// in summaries and simplifications.
/// </param>
/// <param name="Definition">
/// Brief definition of the term if it is explicitly defined within the document.
/// Null if no definition is found or the term is common vocabulary.
/// </param>
/// <param name="Category">
/// Optional category classification for the term (e.g., "programming", "finance", "legal").
/// Enables hierarchical organization and filtering of extracted terms.
/// </param>
/// <param name="RelatedTerms">
/// List of semantically related terms also found in the document.
/// Enables building a term graph for advanced document analysis.
/// </param>
public record KeyTerm(
    string Term,
    double Importance,
    int Frequency,
    bool IsTechnical,
    string? Definition,
    string? Category,
    IReadOnlyList<string>? RelatedTerms)
{
    /// <summary>
    /// Creates a minimal key term with only required fields.
    /// </summary>
    /// <param name="term">The extracted term.</param>
    /// <param name="importance">Importance score (0.0-1.0).</param>
    /// <returns>A new <see cref="KeyTerm"/> with default values for optional fields.</returns>
    /// <remarks>
    /// LOGIC: Factory method for creating key terms when only the term and importance
    /// are known. Useful for testing and simple extraction scenarios.
    /// </remarks>
    public static KeyTerm Create(string term, double importance) =>
        new(
            Term: term,
            Importance: importance,
            Frequency: 1,
            IsTechnical: false,
            Definition: null,
            Category: null,
            RelatedTerms: null);

    /// <summary>
    /// Creates a technical key term with frequency information.
    /// </summary>
    /// <param name="term">The extracted term.</param>
    /// <param name="importance">Importance score (0.0-1.0).</param>
    /// <param name="frequency">Number of occurrences in the document.</param>
    /// <param name="category">Optional category classification.</param>
    /// <returns>A new <see cref="KeyTerm"/> marked as technical.</returns>
    /// <remarks>
    /// LOGIC: Factory method for creating technical terms identified during
    /// domain-specific document analysis.
    /// </remarks>
    public static KeyTerm CreateTechnical(
        string term,
        double importance,
        int frequency,
        string? category = null) =>
        new(
            Term: term,
            Importance: importance,
            Frequency: frequency,
            IsTechnical: true,
            Definition: null,
            Category: category,
            RelatedTerms: null);
}
