// -----------------------------------------------------------------------
// <copyright file="SimplificationResponseParser.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implements parsing of structured LLM responses for the Simplifier Agent.
//   Extracts simplified text, changes list, and optional glossary from markdown
//   code blocks in the response.
//
//   Parsing Strategy:
//     1. Extract ```simplified block → SimplifiedText
//     2. Extract ```changes block → List of SimplificationChange
//     3. Extract ```glossary block → Dictionary<string, string> (optional)
//
//   Fallback Handling:
//     - If no ```simplified block, use entire response as simplified text
//     - If no ```changes block, return empty changes list
//     - If no ```glossary block, return null glossary
//
//   Robustness Features:
//     - Case-insensitive block name matching
//     - Multiple arrow formats (→, ->, =>)
//     - Whitespace trimming
//     - Graceful handling of malformed lines
//
//   Introduced in: v0.7.4b
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Agents.Simplifier;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Parses structured responses from the Simplifier Agent's LLM invocation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Uses regex patterns to extract content from markdown code blocks.
/// The parser is designed to be resilient to formatting variations while still
/// extracting structured data when available.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is stateless and thread-safe. The regex
/// patterns are compiled and cached for performance.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
public sealed class SimplificationResponseParser : ISimplificationResponseParser
{
    private readonly ILogger<SimplificationResponseParser> _logger;

    // ── Regex Patterns ─────────────────────────────────────────────────────

    /// <summary>
    /// Pattern to extract content from ```simplified code block.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches ```simplified (case-insensitive) followed by content until closing ```.
    /// Uses non-greedy matching and singleline mode for multiline content.
    /// </remarks>
    private static readonly Regex SimplifiedBlockPattern = new(
        @"```simplified\s*\n(.*?)\n\s*```",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Pattern to extract content from ```changes code block.
    /// </summary>
    private static readonly Regex ChangesBlockPattern = new(
        @"```changes\s*\n(.*?)\n\s*```",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Pattern to extract content from ```glossary code block.
    /// </summary>
    private static readonly Regex GlossaryBlockPattern = new(
        @"```glossary\s*\n(.*?)\n\s*```",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Pattern to parse individual change lines.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches format: - "original" → "simplified" | Type: ChangeType | Reason: explanation
    /// Supports arrow variations: →, ->, =>
    /// Supports multi-hyphen change types like "passive-to-active" via (\w+(?:-\w+)*).
    /// </remarks>
    private static readonly Regex ChangeLinePattern = new(
        @"^\s*-\s*""([^""]+)""\s*(?:→|->|=>)\s*""([^""]+)""\s*\|\s*Type:\s*(\w+(?:-\w+)*)\s*\|\s*Reason:\s*(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Pattern to parse glossary lines.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches format: - "term" → "replacement"
    /// </remarks>
    private static readonly Regex GlossaryLinePattern = new(
        @"^\s*-\s*""([^""]+)""\s*(?:→|->|=>)\s*""([^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of <see cref="SimplificationResponseParser"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public SimplificationResponseParser(ILogger<SimplificationResponseParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public SimplificationParseResult Parse(string llmResponse)
    {
        ArgumentNullException.ThrowIfNull(llmResponse);

        _logger.LogDebug(
            "Parsing LLM response of {Length} characters",
            llmResponse.Length);

        // LOGIC: Handle empty or whitespace-only responses
        if (string.IsNullOrWhiteSpace(llmResponse))
        {
            _logger.LogWarning("LLM response is empty or whitespace");
            return SimplificationParseResult.Empty();
        }

        // 1. Extract simplified text
        var simplifiedText = ExtractSimplifiedText(llmResponse);

        // 2. Extract changes
        var changes = ExtractChanges(llmResponse);

        // 3. Extract glossary (optional)
        var glossary = ExtractGlossary(llmResponse);

        _logger.LogDebug(
            "Parse complete: {TextLength} chars, {ChangeCount} changes, glossary: {HasGlossary}",
            simplifiedText.Length,
            changes.Count,
            glossary is not null);

        return new SimplificationParseResult(
            SimplifiedText: simplifiedText,
            Changes: changes,
            Glossary: glossary);
    }

    /// <summary>
    /// Extracts simplified text from the ```simplified code block.
    /// </summary>
    /// <param name="response">The full LLM response.</param>
    /// <returns>
    /// The content of the simplified block, or the entire response if no block is found.
    /// </returns>
    private string ExtractSimplifiedText(string response)
    {
        var match = SimplifiedBlockPattern.Match(response);

        if (match.Success)
        {
            _logger.LogTrace("Found ```simplified block");
            return match.Groups[1].Value.Trim();
        }

        // LOGIC: Fallback — use the entire response, but strip any code blocks
        _logger.LogDebug("No ```simplified block found, using raw response as fallback");

        // Remove any code blocks that might be present
        var stripped = ChangesBlockPattern.Replace(response, string.Empty);
        stripped = GlossaryBlockPattern.Replace(stripped, string.Empty);

        return stripped.Trim();
    }

    /// <summary>
    /// Extracts changes from the ```changes code block.
    /// </summary>
    /// <param name="response">The full LLM response.</param>
    /// <returns>
    /// A list of parsed <see cref="SimplificationChange"/> records.
    /// Returns an empty list if no changes block is found or all lines are malformed.
    /// </returns>
    private IReadOnlyList<SimplificationChange> ExtractChanges(string response)
    {
        var blockMatch = ChangesBlockPattern.Match(response);

        if (!blockMatch.Success)
        {
            _logger.LogDebug("No ```changes block found");
            return Array.Empty<SimplificationChange>();
        }

        var changesContent = blockMatch.Groups[1].Value;
        var changes = new List<SimplificationChange>();

        foreach (Match lineMatch in ChangeLinePattern.Matches(changesContent))
        {
            var originalText = lineMatch.Groups[1].Value.Trim();
            var simplifiedText = lineMatch.Groups[2].Value.Trim();
            var changeTypeStr = lineMatch.Groups[3].Value.Trim();
            var explanation = lineMatch.Groups[4].Value.Trim();

            var changeType = ParseChangeType(changeTypeStr);

            changes.Add(new SimplificationChange(
                OriginalText: originalText,
                SimplifiedText: simplifiedText,
                ChangeType: changeType,
                Explanation: explanation,
                Location: null,
                Confidence: 1.0));
        }

        _logger.LogDebug("Extracted {Count} changes from ```changes block", changes.Count);

        return changes.AsReadOnly();
    }

    /// <summary>
    /// Parses a change type string into a <see cref="SimplificationChangeType"/> enum value.
    /// </summary>
    /// <param name="typeString">The change type string (e.g., "SentenceSplit", "sentence-split").</param>
    /// <returns>
    /// The matching <see cref="SimplificationChangeType"/>, or
    /// <see cref="SimplificationChangeType.Combined"/> if no match is found.
    /// </returns>
    private SimplificationChangeType ParseChangeType(string typeString)
    {
        // LOGIC: Normalize the string by removing hyphens and converting to lowercase
        var normalized = typeString.Replace("-", "").ToLowerInvariant();

        return normalized switch
        {
            "sentencesplit" => SimplificationChangeType.SentenceSplit,
            "jargonreplacement" => SimplificationChangeType.JargonReplacement,
            "passivetoactive" => SimplificationChangeType.PassiveToActive,
            "wordsimplification" => SimplificationChangeType.WordSimplification,
            "clausereduction" => SimplificationChangeType.ClauseReduction,
            "transitionadded" => SimplificationChangeType.TransitionAdded,
            "redundancyremoved" => SimplificationChangeType.RedundancyRemoved,
            "combined" => SimplificationChangeType.Combined,
            _ => SimplificationChangeType.Combined // LOGIC: Fallback for unknown types
        };
    }

    /// <summary>
    /// Extracts glossary from the ```glossary code block.
    /// </summary>
    /// <param name="response">The full LLM response.</param>
    /// <returns>
    /// A dictionary mapping terms to replacements, or <c>null</c> if no glossary block is found.
    /// </returns>
    private IReadOnlyDictionary<string, string>? ExtractGlossary(string response)
    {
        var blockMatch = GlossaryBlockPattern.Match(response);

        if (!blockMatch.Success)
        {
            _logger.LogTrace("No ```glossary block found");
            return null;
        }

        var glossaryContent = blockMatch.Groups[1].Value;
        var glossary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match lineMatch in GlossaryLinePattern.Matches(glossaryContent))
        {
            var term = lineMatch.Groups[1].Value.Trim();
            var replacement = lineMatch.Groups[2].Value.Trim();

            // LOGIC: Don't overwrite existing entries (first one wins)
            glossary.TryAdd(term, replacement);
        }

        if (glossary.Count == 0)
        {
            _logger.LogDebug("```glossary block found but no entries parsed");
            return null;
        }

        _logger.LogDebug("Extracted {Count} glossary entries", glossary.Count);

        return glossary.AsReadOnly();
    }
}
