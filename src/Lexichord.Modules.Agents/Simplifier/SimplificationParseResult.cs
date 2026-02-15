// -----------------------------------------------------------------------
// <copyright file="SimplificationParseResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Simplifier;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Represents the parsed output from the Simplifier Agent's LLM response.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="SimplificationParseResult"/> is an intermediate
/// data structure produced by <see cref="ISimplificationResponseParser.Parse"/>
/// and consumed by the SimplifierAgent to build the final <see cref="SimplificationResult"/>.
/// </para>
/// <para>
/// <b>Contents:</b>
/// <list type="bullet">
///   <item><description><see cref="SimplifiedText"/>: The transformed text</description></item>
///   <item><description><see cref="Changes"/>: List of individual transformations</description></item>
///   <item><description><see cref="Glossary"/>: Optional term mappings (may be null)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <param name="SimplifiedText">
/// The simplified text extracted from the <c>```simplified</c> block.
/// If no block was found, this contains the raw LLM response.
/// </param>
/// <param name="Changes">
/// List of <see cref="SimplificationChange"/> records extracted from the
/// <c>```changes</c> block. May be empty if the block was missing or malformed.
/// </param>
/// <param name="Glossary">
/// Dictionary mapping technical terms to their simplified equivalents,
/// extracted from the <c>```glossary</c> block. <c>null</c> if no glossary
/// was requested or found.
/// </param>
/// <example>
/// <code>
/// // Using the parse result
/// var parseResult = parser.Parse(llmResponse);
///
/// // Build the final result
/// var result = new SimplificationResult
/// {
///     SimplifiedText = parseResult.SimplifiedText,
///     Changes = parseResult.Changes,
///     Glossary = parseResult.Glossary,
///     // ... other properties
/// };
/// </code>
/// </example>
/// <seealso cref="ISimplificationResponseParser"/>
/// <seealso cref="SimplificationChange"/>
/// <seealso cref="SimplificationResult"/>
public record SimplificationParseResult(
    string SimplifiedText,
    IReadOnlyList<SimplificationChange> Changes,
    IReadOnlyDictionary<string, string>? Glossary)
{
    /// <summary>
    /// Gets a value indicating whether any changes were parsed.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Changes"/> contains one or more items; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for checking if the changes block was successfully parsed.
    /// </remarks>
    public bool HasChanges => Changes.Count > 0;

    /// <summary>
    /// Gets a value indicating whether a glossary was parsed.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Glossary"/> is not null and contains one or more entries;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for checking if the glossary block was found and parsed.
    /// </remarks>
    public bool HasGlossary => Glossary is not null && Glossary.Count > 0;

    /// <summary>
    /// Creates an empty parse result with the specified text.
    /// </summary>
    /// <param name="simplifiedText">The simplified text (or raw response).</param>
    /// <returns>A <see cref="SimplificationParseResult"/> with no changes or glossary.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for fallback parsing when no structured blocks are found.
    /// </remarks>
    public static SimplificationParseResult WithTextOnly(string simplifiedText) =>
        new(
            SimplifiedText: simplifiedText,
            Changes: Array.Empty<SimplificationChange>(),
            Glossary: null);

    /// <summary>
    /// Creates an empty parse result for failed parsing.
    /// </summary>
    /// <returns>A <see cref="SimplificationParseResult"/> with empty text, no changes, and no glossary.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for complete parsing failures.
    /// </remarks>
    public static SimplificationParseResult Empty() =>
        new(
            SimplifiedText: string.Empty,
            Changes: Array.Empty<SimplificationChange>(),
            Glossary: null);
}
