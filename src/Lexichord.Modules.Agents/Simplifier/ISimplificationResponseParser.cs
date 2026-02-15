// -----------------------------------------------------------------------
// <copyright file="ISimplificationResponseParser.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Simplifier;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Contract for parsing structured responses from the Simplifier Agent's LLM invocation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The LLM response from the Simplifier Agent contains structured blocks:
/// </para>
/// <list type="bullet">
///   <item><description><c>```simplified</c>: The simplified text</description></item>
///   <item><description><c>```changes</c>: List of changes made (original → simplified with type and reason)</description></item>
///   <item><description><c>```glossary</c>: Optional term mappings (when requested)</description></item>
/// </list>
/// <para>
/// <b>Response Format:</b>
/// The parser expects responses in this format:
/// <code>
/// ```simplified
/// [simplified text here]
/// ```
///
/// ```changes
/// - "original text" → "simplified text" | Type: WordSimplification | Reason: explanation
/// - "another original" → "another simplified" | Type: SentenceSplit | Reason: explanation
/// ```
///
/// ```glossary
/// - "technical term" → "plain language equivalent"
/// - "jargon" → "simple word"
/// ```
/// </code>
/// </para>
/// <para>
/// <b>Fallback Handling:</b>
/// If the response does not contain the expected code blocks, the parser should
/// extract the raw content as the simplified text and return an empty changes list.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var parser = serviceProvider.GetRequiredService&lt;ISimplificationResponseParser&gt;();
/// var llmResponse = await chatService.CompleteAsync(request, ct);
///
/// var parseResult = parser.Parse(llmResponse.Content);
///
/// Console.WriteLine($"Simplified text: {parseResult.SimplifiedText}");
/// Console.WriteLine($"Changes: {parseResult.Changes.Count}");
///
/// if (parseResult.Glossary is not null)
/// {
///     foreach (var (term, replacement) in parseResult.Glossary)
///     {
///         Console.WriteLine($"  {term} → {replacement}");
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SimplificationParseResult"/>
/// <seealso cref="SimplificationChange"/>
public interface ISimplificationResponseParser
{
    /// <summary>
    /// Parses a structured LLM response into its component parts.
    /// </summary>
    /// <param name="llmResponse">
    /// The raw text response from the LLM. May contain markdown code blocks
    /// with <c>simplified</c>, <c>changes</c>, and <c>glossary</c> sections.
    /// </param>
    /// <returns>
    /// A <see cref="SimplificationParseResult"/> containing the extracted
    /// simplified text, list of changes, and optional glossary.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="llmResponse"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>Parsing Behavior:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Simplified Text:</b> Extracted from the <c>```simplified</c> block.
    ///     If not found, the entire response is used as the simplified text.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Changes:</b> Extracted from the <c>```changes</c> block. Each line
    ///     matching the pattern is parsed into a <see cref="SimplificationChange"/>.
    ///     Malformed lines are skipped.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Glossary:</b> Extracted from the <c>```glossary</c> block if present.
    ///     Returns <c>null</c> if no glossary block is found.
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Robustness:</b>
    /// The parser is designed to be resilient to formatting variations:
    /// <list type="bullet">
    ///   <item><description>Case-insensitive block names (```SIMPLIFIED is accepted)</description></item>
    ///   <item><description>Extra whitespace is trimmed</description></item>
    ///   <item><description>Arrow variations (→, ->, =>) are accepted</description></item>
    ///   <item><description>Missing or malformed sections don't cause failures</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    SimplificationParseResult Parse(string llmResponse);
}
