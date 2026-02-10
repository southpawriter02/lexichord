// -----------------------------------------------------------------------
// <copyright file="DefaultPromptGenerator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Generates contextually appropriate default prompts for text selections.
/// </summary>
/// <remarks>
/// <para>
/// Analyzes selection characteristics (length, content type) to determine
/// the most useful default prompt. The prompt is pre-filled in the Co-pilot
/// chat input to give users a starting point for their interaction.
/// </para>
/// <para>
/// <b>Prompt Selection Logic:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Short selection (&lt;50 chars): "Explain this:" — likely a term or phrase</description></item>
///   <item><description>Code-like selection: "Review this code:" — detected via regex patterns</description></item>
///   <item><description>Long selection (&gt;500 chars): "Summarize this:" — summarization is most helpful</description></item>
///   <item><description>Default: "Improve this:" — general writing improvement</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
public class DefaultPromptGenerator
{
    /// <summary>
    /// Threshold below which a selection is considered "short" (in characters).
    /// </summary>
    internal const int ShortSelectionThreshold = 50;

    /// <summary>
    /// Threshold above which a selection is considered "long" (in characters).
    /// </summary>
    internal const int LongSelectionThreshold = 500;

    /// <summary>
    /// Regex that detects code-like content by matching common programming keywords.
    /// </summary>
    private static readonly Regex CodePattern = new(
        @"^[\s]*(public|private|class|function|def|const|let|var|import|using|#include)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Regex that detects JSON/array-like content by matching leading brackets.
    /// </summary>
    private static readonly Regex JsonPattern = new(
        @"^\s*[\[{]",
        RegexOptions.Compiled);

    /// <summary>
    /// Generates a default prompt based on selection analysis.
    /// </summary>
    /// <param name="selection">The selected text.</param>
    /// <returns>Appropriate default prompt string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selection"/> is null.</exception>
    /// <remarks>
    /// LOGIC: Evaluates the selection in priority order:
    /// 1. Short selections are likely terms/phrases → "Explain this:"
    /// 2. Code-like content benefits from review → "Review this code:"
    /// 3. Long selections benefit from summarization → "Summarize this:"
    /// 4. Medium prose benefits from improvement → "Improve this:"
    /// </remarks>
    public string Generate(string selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var trimmed = selection.Trim();

        // LOGIC: Code-like content — offer code review (checked first because
        // code snippets may be short, and code review is more useful than
        // "Explain this:" for recognized code patterns).
        if (IsCodeLike(trimmed))
        {
            return "Review this code:";
        }

        // LOGIC: Short selection — likely wants explanation of a term or phrase.
        if (trimmed.Length < ShortSelectionThreshold)
        {
            return "Explain this:";
        }

        // LOGIC: Long selection — summarization is most helpful.
        if (trimmed.Length > LongSelectionThreshold)
        {
            return "Summarize this:";
        }

        // LOGIC: Default — general writing improvement for medium prose.
        return "Improve this:";
    }

    /// <summary>
    /// Determines if the text appears to be code-like content.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns><c>true</c> if the text matches code or JSON patterns; otherwise, <c>false</c>.</returns>
    private static bool IsCodeLike(string text)
    {
        return CodePattern.IsMatch(text) || JsonPattern.IsMatch(text);
    }
}
