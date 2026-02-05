// -----------------------------------------------------------------------
// <copyright file="IContextFormatter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Templates.Formatters;

/// <summary>
/// Defines formatting operations for context data before injection into prompt templates.
/// </summary>
/// <remarks>
/// <para>
/// The context formatter transforms raw data structures (style rules, RAG search hits)
/// into formatted strings suitable for inclusion in LLM prompts. This separation allows
/// for different formatting strategies without modifying the context providers.
/// </para>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Decouples data retrieval (providers) from presentation (formatter).</description></item>
///   <item><description>Enables future customization of output format (Markdown, plain text, XML).</description></item>
///   <item><description>Centralizes formatting logic for consistency across all providers.</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong>
/// Implementations should be stateless and thread-safe for concurrent use.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var formatter = new DefaultContextFormatter();
///
/// // Format style rules
/// var rules = styleSheet.GetEnabledRules();
/// string formattedRules = formatter.FormatStyleRules(rules);
/// // Output:
/// // - Avoid Jargon: Technical jargon reduces accessibility
/// // - Use Active Voice: Prefer active voice for clarity
///
/// // Format RAG search results
/// var hits = searchResult.Hits;
/// string formattedContext = formatter.FormatRAGChunks(hits, maxChunkLength: 500);
/// // Output:
/// // [Source: docs/api-guide.md]
/// // The API provides endpoints for user management...
/// //
/// // [Source: docs/authentication.md]
/// // Authentication is handled via JWT tokens...
/// </code>
/// </example>
/// <seealso cref="DefaultContextFormatter"/>
public interface IContextFormatter
{
    /// <summary>
    /// Formats a collection of style rules into a string suitable for prompt injection.
    /// </summary>
    /// <param name="rules">The style rules to format.</param>
    /// <returns>
    /// A formatted string containing the style rules, or an empty string if <paramref name="rules"/>
    /// is null or empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Style rules are formatted as a bullet list with the rule name and description.
    /// This format is concise and readable by LLMs for following writing guidelines.
    /// </para>
    /// <para>
    /// Output format:
    /// </para>
    /// <code>
    /// - {Rule.Name}: {Rule.Description}
    /// - {Rule.Name}: {Rule.Description}
    /// ...
    /// </code>
    /// </remarks>
    string FormatStyleRules(IReadOnlyList<StyleRule> rules);

    /// <summary>
    /// Formats a collection of RAG search hits into a string suitable for prompt injection.
    /// </summary>
    /// <param name="hits">The search hits to format.</param>
    /// <param name="maxChunkLength">
    /// Maximum number of characters to include from each chunk.
    /// Default: 1000. Chunks longer than this are truncated with "...".
    /// </param>
    /// <returns>
    /// A formatted string containing the RAG context, or an empty string if <paramref name="hits"/>
    /// is null or empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Each search hit is formatted with its source document path as a header,
    /// followed by the chunk content. Chunks are separated by blank lines for clarity.
    /// </para>
    /// <para>
    /// Output format:
    /// </para>
    /// <code>
    /// [Source: {Document.Path}]
    /// {Chunk.Content}
    ///
    /// [Source: {Document.Path}]
    /// {Chunk.Content}
    /// ...
    /// </code>
    /// <para>
    /// The <paramref name="maxChunkLength"/> parameter prevents excessively long chunks
    /// from consuming too much of the prompt token budget.
    /// </para>
    /// </remarks>
    string FormatRAGChunks(IReadOnlyList<SearchHit> hits, int maxChunkLength = 1000);
}
