// -----------------------------------------------------------------------
// <copyright file="EditorTerminologyContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Gathers relevant terminology context for the Editor Agent's
//   rewrite operations (v0.7.3c). Scans the user's selected text against
//   the active terminology database (ITerminologyRepository) and provides
//   matching terms with their preferred replacements.
//
//   Flow:
//     1. Validate that SelectedText is present in the request
//     2. Retrieve all active terms via ITerminologyRepository.GetAllActiveTermsAsync()
//     3. Filter to terms whose Term appears in the selected text
//     4. Limit to MaxTerms and format as structured guidance
//     5. Truncate to MaxTokens and return as ContextFragment
//
//   The SourceId "terminology" maps to the {{terminology}} Mustache
//   variable in EditorAgent.BuildPromptVariables() (v0.7.3b).
//
//   Thread safety:
//     - No shared mutable state; all variables are per-invocation
//     - ITerminologyRepository is thread-safe per contract
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Agents.Context;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor.Context;

/// <summary>
/// Gathers relevant terminology context for the Editor Agent's rewrite operations.
/// Scans the user's selected text against the active terminology database and
/// provides matching terms with their preferred replacements.
/// </summary>
/// <remarks>
/// <para>
/// This strategy retrieves all active <see cref="StyleTerm"/> entries from the
/// <see cref="ITerminologyRepository"/> and filters them to terms that appear in
/// the user's selected text. Matching terms are formatted as structured guidance
/// for the LLM, including replacement suggestions, categories, and notes.
/// </para>
/// <para>
/// <strong>Priority:</strong> <see cref="StrategyPriority.Medium"/> (60) —
/// Terminology is helpful for accurate technical language but not essential for
/// basic rewrite quality. Lower priority than surrounding text (100) and style
/// rules (50 + Teams tier) ensures terminology is trimmed before more critical
/// context when budget is constrained.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.WriterPro"/> or higher.
/// Context-aware rewriting is a WriterPro feature.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 800 — Sufficient for 10-15 terminology entries
/// with descriptions. Terminology is compact and high-value-per-token.
/// </para>
/// <para>
/// <strong>Case Sensitivity:</strong> Each <see cref="StyleTerm"/> has a
/// <see cref="StyleTerm.MatchCase"/> property. When <c>true</c>, the term match
/// is case-sensitive; when <c>false</c> (default), matching is case-insensitive.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.3c as part of Context-Aware Rewriting.
/// </para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro)]
public sealed class EditorTerminologyContextStrategy : ContextStrategyBase
{
    private readonly ITerminologyRepository _terminologyRepository;

    /// <summary>
    /// Maximum number of matching terms to include in the context fragment.
    /// </summary>
    /// <remarks>
    /// LOGIC: Limits context size and LLM cognitive load. 15 terms provide
    /// substantial coverage without overwhelming the prompt.
    /// </remarks>
    private const int MaxTerms = 15;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorTerminologyContextStrategy"/> class.
    /// </summary>
    /// <param name="terminologyRepository">Repository for accessing active terminology entries.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="terminologyRepository"/> is null.
    /// </exception>
    public EditorTerminologyContextStrategy(
        ITerminologyRepository terminologyRepository,
        ITokenCounter tokenCounter,
        ILogger<EditorTerminologyContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _terminologyRepository = terminologyRepository ?? throw new ArgumentNullException(nameof(terminologyRepository));
    }

    /// <inheritdoc />
    public override string StrategyId => "terminology";

    /// <inheritdoc />
    public override string DisplayName => "Editor Terminology";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Medium priority (60) because terminology is helpful for accurate
    /// technical language but not essential for basic rewrite quality. This ensures
    /// surrounding text (Critical, 100) is always preserved before terminology
    /// when the token budget is constrained.
    /// </remarks>
    public override int Priority => StrategyPriority.Medium; // 60

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: 800 tokens is sufficient for 10-15 terminology entries with descriptions.
    /// Terminology entries are compact and high value per token.
    /// </remarks>
    public override int MaxTokens => 800;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Terminology context gathering:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates that selected text is present in the request.</description></item>
    ///   <item><description>Retrieves all active terms from <see cref="ITerminologyRepository"/>.</description></item>
    ///   <item><description>Filters to terms whose <see cref="StyleTerm.Term"/> appears in
    ///   the selected text, respecting <see cref="StyleTerm.MatchCase"/>.</description></item>
    ///   <item><description>Limits to <see cref="MaxTerms"/> and formats as structured guidance.</description></item>
    ///   <item><description>Truncates to fit <see cref="MaxTokens"/> and returns fragment.</description></item>
    /// </list>
    /// <para>
    /// <strong>Graceful Degradation:</strong> Returns <c>null</c> if:
    /// <list type="bullet">
    ///   <item><description>No selected text in request</description></item>
    ///   <item><description>No active terms in repository</description></item>
    ///   <item><description>No matching terms found in selection</description></item>
    ///   <item><description>An exception occurs during gathering</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public override async Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        // LOGIC: Selected text is required to match terms against.
        if (!ValidateRequest(request, requireSelection: true))
            return null;

        _logger.LogDebug(
            "{Strategy} scanning selected text ({Length} chars) for terminology matches",
            StrategyId, request.SelectedText!.Length);

        try
        {
            // LOGIC: Retrieve the cached set of all active terms.
            var allTerms = await _terminologyRepository.GetAllActiveTermsAsync(ct);

            if (allTerms is null || allTerms.Count == 0)
            {
                _logger.LogDebug("{Strategy} no active terms in terminology repository", StrategyId);
                return null;
            }

            // LOGIC: Find terms that appear in the selected text.
            var matchingTerms = FindMatchingTerms(request.SelectedText!, allTerms);

            if (matchingTerms.Count == 0)
            {
                _logger.LogDebug(
                    "{Strategy} no matching terms found in selection (checked {TotalTerms} terms)",
                    StrategyId, allTerms.Count);
                return null;
            }

            // LOGIC: Format matching terms as structured guidance for the LLM.
            var content = FormatTermsAsContext(matchingTerms);

            // LOGIC: Apply token-based truncation from base class.
            content = TruncateToMaxTokens(content);

            _logger.LogInformation(
                "{Strategy} found {MatchCount} matching terms in selection (from {TotalTerms} active terms)",
                StrategyId, matchingTerms.Count, allTerms.Count);

            return CreateFragment(content, relevance: 0.8f);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "{Strategy} failed to gather terminology context",
                StrategyId);
            return null;
        }
    }

    /// <summary>
    /// Finds terminology entries that appear in the selected text.
    /// </summary>
    /// <param name="selectedText">The user's selected text to scan for term matches.</param>
    /// <param name="allTerms">All active terms from the terminology repository.</param>
    /// <returns>
    /// A list of matching <see cref="StyleTerm"/> entries, ordered by severity
    /// (Error first, then Warning, then Suggestion) and limited to <see cref="MaxTerms"/>.
    /// </returns>
    /// <remarks>
    /// LOGIC: For each term, checks if the term text appears in the selection.
    /// Respects <see cref="StyleTerm.MatchCase"/>: when <c>true</c>, uses ordinal
    /// (case-sensitive) comparison; when <c>false</c>, uses case-insensitive comparison.
    /// Results are ordered by severity to prioritize the most important terms.
    /// </remarks>
    internal static List<StyleTerm> FindMatchingTerms(
        string selectedText,
        IEnumerable<StyleTerm> allTerms)
    {
        var matchingTerms = new List<StyleTerm>();

        foreach (var term in allTerms)
        {
            if (string.IsNullOrEmpty(term.Term))
                continue;

            // LOGIC: Respect the MatchCase flag on each term.
            var comparison = term.MatchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (selectedText.Contains(term.Term, comparison))
            {
                matchingTerms.Add(term);
            }
        }

        // LOGIC: Order by severity (Error > Warning > Suggestion) to prioritize
        // the most important terminology guidance, then limit to MaxTerms.
        return matchingTerms
            .OrderBy(t => t.Severity switch
            {
                "Error" => 0,
                "Warning" => 1,
                "Suggestion" => 2,
                _ => 3
            })
            .Take(MaxTerms)
            .ToList();
    }

    /// <summary>
    /// Formats matching terminology entries as structured context for the LLM prompt.
    /// </summary>
    /// <param name="terms">The matching terms to format.</param>
    /// <returns>Formatted terminology guidance string.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Formats each term as a structured entry with:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Term name (bold) with replacement suggestion when available</description></item>
    ///   <item><description>Category grouping for context</description></item>
    ///   <item><description>Notes when present for additional guidance</description></item>
    /// </list>
    /// <para>
    /// Terms with <see cref="StyleTerm.Replacement"/> are formatted as replacement rules;
    /// terms without replacements are formatted as awareness entries.
    /// </para>
    /// </remarks>
    internal static string FormatTermsAsContext(List<StyleTerm> terms)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Terminology Guidelines");
        sb.AppendLine();
        sb.AppendLine("Apply these terminology rules in your rewrite:");
        sb.AppendLine();

        // LOGIC: Group terms by category for organized output.
        var byCategory = terms.GroupBy(t => t.Category);

        foreach (var category in byCategory)
        {
            sb.AppendLine($"### {category.Key}");

            foreach (var term in category)
            {
                // LOGIC: Format based on whether a replacement is specified.
                if (!string.IsNullOrEmpty(term.Replacement))
                {
                    sb.AppendLine($"- **{term.Term}**: Use \"{term.Replacement}\" instead");
                }
                else
                {
                    sb.AppendLine($"- **{term.Term}**: Flagged ({term.Severity})");
                }

                // LOGIC: Include notes when available for additional guidance.
                if (!string.IsNullOrEmpty(term.Notes))
                {
                    sb.AppendLine($"  - Note: {term.Notes}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
