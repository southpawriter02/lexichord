// -----------------------------------------------------------------------
// <copyright file="PatternAnalyzer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Modules.Agents.Tuning.Storage;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Analyzes feedback records to extract patterns and generate prompt enhancements.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The PatternAnalyzer is responsible for two key operations:
/// <list type="bullet">
///   <item><description>
///     <b>Pattern Extraction:</b> Groups feedback by (OriginalText, SuggestedText) pairs,
///     counts frequencies, and classifies patterns as accepted (success rate >= 0.7, count >= 3)
///     or rejected (success rate &lt;= 0.3, count >= 3).
///   </description></item>
///   <item><description>
///     <b>Prompt Enhancement:</b> Generates structured text from extracted patterns that can
///     be injected into fix generation prompts to guide the LLM toward user-preferred patterns.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Thread-safe. All methods are stateless and operate on
/// immutable input data.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
internal sealed class PatternAnalyzer
{
    private readonly ILogger<PatternAnalyzer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternAnalyzer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public PatternAnalyzer(ILogger<PatternAnalyzer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Extracts accepted patterns from feedback records.
    /// </summary>
    /// <param name="records">The feedback records to analyze.</param>
    /// <returns>
    /// Accepted patterns where success rate >= 0.7 and count >= 3,
    /// ordered by acceptance count descending, limited to top 5.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Groups feedback by normalized (OriginalText, SuggestedText) pairs,
    /// then filters for patterns with a high acceptance rate (>= 70%) and sufficient
    /// data (>= 3 occurrences). Success rate includes both Accepted and Modified decisions.
    /// </remarks>
    public IReadOnlyList<AcceptedPattern> ExtractAcceptedPatterns(
        IReadOnlyList<FeedbackRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var groups = GroupByPattern(records);
        var accepted = new List<AcceptedPattern>();

        foreach (var ((original, suggested), (acceptedCount, _, total)) in groups)
        {
            if (total < 3) continue;

            var successRate = (double)acceptedCount / total;
            if (successRate >= 0.7)
            {
                accepted.Add(new AcceptedPattern(
                    original,
                    suggested,
                    acceptedCount,
                    successRate));
            }
        }

        var result = accepted
            .OrderByDescending(p => p.AcceptCount)
            .Take(5)
            .ToList();

        _logger.LogDebug(
            "Extracted {Count} accepted patterns from {Total} records",
            result.Count, records.Count);

        return result;
    }

    /// <summary>
    /// Extracts rejected patterns from feedback records.
    /// </summary>
    /// <param name="records">The feedback records to analyze.</param>
    /// <returns>
    /// Rejected patterns where success rate &lt;= 0.3 and count >= 3,
    /// ordered by rejection count descending, limited to top 5.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Groups feedback by normalized (OriginalText, SuggestedText) pairs,
    /// then filters for patterns with a low acceptance rate (&lt;= 30%) and sufficient
    /// data (>= 3 occurrences). Extracts the most common user comment as rejection reason.
    /// </remarks>
    public IReadOnlyList<RejectedPattern> ExtractRejectedPatterns(
        IReadOnlyList<FeedbackRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var groups = GroupByPattern(records);
        var rejected = new List<RejectedPattern>();

        // LOGIC: Pre-group records by pattern for comment extraction
        var recordsByPattern = records
            .Where(r => r.OriginalText is not null && r.SuggestedText is not null
                        && r.Decision != (int)FeedbackDecision.Skipped)
            .GroupBy(r => NormalizeForPattern(r.OriginalText!, r.SuggestedText!))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var ((original, suggested), (_, rejectedCount, total)) in groups)
        {
            if (total < 3) continue;

            var successRate = (double)(total - rejectedCount) / total;
            if (successRate <= 0.3)
            {
                // LOGIC: Find the most common user comment for this pattern's rejections
                string? commonReason = null;
                if (recordsByPattern.TryGetValue((original, suggested), out var patternRecords))
                {
                    commonReason = ExtractCommonRejectionReason(
                        patternRecords.Where(r => r.Decision == (int)FeedbackDecision.Rejected));
                }

                rejected.Add(new RejectedPattern(
                    original,
                    suggested,
                    rejectedCount,
                    commonReason));
            }
        }

        var result = rejected
            .OrderByDescending(p => p.RejectCount)
            .Take(5)
            .ToList();

        _logger.LogDebug(
            "Extracted {Count} rejected patterns from {Total} records",
            result.Count, records.Count);

        return result;
    }

    /// <summary>
    /// Extracts user modification examples from feedback records.
    /// </summary>
    /// <param name="records">The feedback records to analyze.</param>
    /// <returns>
    /// User modification examples where the user edited the suggestion,
    /// limited to top 3 with useful improvement descriptions.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Filters for Modified decisions with non-empty UserModification text,
    /// then attempts to describe how the user improved the suggestion (more concise,
    /// added detail, or specific word replacements).
    /// </remarks>
    public IReadOnlyList<UserModificationExample> ExtractUserModifications(
        IReadOnlyList<FeedbackRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var modifications = new List<UserModificationExample>();

        foreach (var record in records.Where(r =>
                     r.Decision == (int)FeedbackDecision.Modified &&
                     !string.IsNullOrEmpty(r.UserModification) &&
                     !string.IsNullOrEmpty(r.SuggestedText)))
        {
            var improvement = DescribeImprovement(record.SuggestedText!, record.UserModification!);
            if (!string.IsNullOrEmpty(improvement))
            {
                modifications.Add(new UserModificationExample(
                    record.SuggestedText!,
                    record.UserModification!,
                    improvement));
            }
        }

        var result = modifications.Take(3).ToList();

        _logger.LogDebug(
            "Extracted {Count} user modification examples from {Total} records",
            result.Count, records.Count);

        return result;
    }

    /// <summary>
    /// Generates prompt enhancement text from extracted patterns.
    /// </summary>
    /// <param name="acceptedPatterns">Patterns frequently accepted by users.</param>
    /// <param name="rejectedPatterns">Patterns frequently rejected by users.</param>
    /// <param name="modifications">User modification examples.</param>
    /// <param name="acceptanceRate">Overall acceptance rate for the rule.</param>
    /// <param name="sampleCount">Total feedback samples for the rule.</param>
    /// <returns>
    /// Structured prompt enhancement text, or <c>null</c> if insufficient data (fewer than 10 samples).
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Builds a structured text block with sections for:
    /// <list type="bullet">
    ///   <item><description>Preferred patterns — tells the LLM "suggest patterns like this"</description></item>
    ///   <item><description>Avoided patterns — tells the LLM "do NOT suggest patterns like this"</description></item>
    ///   <item><description>Modification insights — tells the LLM how users improve suggestions</description></item>
    ///   <item><description>Confidence calibration — adjusts tone based on acceptance rate</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? GeneratePromptEnhancement(
        IReadOnlyList<AcceptedPattern> acceptedPatterns,
        IReadOnlyList<RejectedPattern> rejectedPatterns,
        IReadOnlyList<UserModificationExample> modifications,
        double acceptanceRate,
        int sampleCount)
    {
        // LOGIC: Require minimum 10 samples for meaningful patterns
        if (sampleCount < 10)
        {
            _logger.LogDebug(
                "Insufficient data for prompt enhancement: {SampleCount} samples (need 10+)",
                sampleCount);
            return null;
        }

        var sb = new StringBuilder();

        // LOGIC: Add accepted patterns guidance (top 3)
        if (acceptedPatterns.Count > 0)
        {
            sb.AppendLine("Based on user feedback, these fix patterns are PREFERRED:");
            foreach (var pattern in acceptedPatterns.Take(3))
            {
                sb.AppendLine(
                    $"- When seeing '{TruncateForPrompt(pattern.OriginalPattern)}', " +
                    $"suggest '{TruncateForPrompt(pattern.SuggestedPattern)}' " +
                    $"(accepted {pattern.SuccessRate:P0} of the time)");
            }
            sb.AppendLine();
        }

        // LOGIC: Add rejected patterns guidance (top 3)
        if (rejectedPatterns.Count > 0)
        {
            sb.AppendLine("AVOID these fix patterns that users have rejected:");
            foreach (var pattern in rejectedPatterns.Take(3))
            {
                var reason = !string.IsNullOrEmpty(pattern.CommonRejectionReason)
                    ? $" Reason: {pattern.CommonRejectionReason}"
                    : "";
                sb.AppendLine(
                    $"- Do NOT suggest '{TruncateForPrompt(pattern.SuggestedPattern)}' " +
                    $"for '{TruncateForPrompt(pattern.OriginalPattern)}'.{reason}");
            }
            sb.AppendLine();
        }

        // LOGIC: Add modification insights (top 2)
        if (modifications.Count > 0)
        {
            sb.AppendLine("Users often improve suggestions by:");
            foreach (var mod in modifications.Take(2))
            {
                sb.AppendLine($"- {mod.Improvement}");
            }
            sb.AppendLine();
        }

        // LOGIC: Add confidence calibration note
        if (acceptanceRate < 0.5)
        {
            sb.AppendLine("NOTE: This rule has a low acceptance rate. Be more conservative with suggestions.");
        }
        else if (acceptanceRate > 0.9)
        {
            sb.AppendLine("NOTE: This rule has a high acceptance rate. You can be confident in standard suggestions.");
        }

        var result = sb.ToString().Trim();

        if (string.IsNullOrEmpty(result))
        {
            _logger.LogDebug("No prompt enhancement generated despite sufficient data");
            return null;
        }

        _logger.LogInformation(
            "Prompt enhancement generated: {Length} chars from {SampleCount} samples",
            result.Length, sampleCount);

        return result;
    }

    #region Private Helpers

    /// <summary>
    /// Groups feedback records by (OriginalText, SuggestedText) pattern and computes counts.
    /// </summary>
    private static Dictionary<(string Original, string Suggested), (int Accepted, int Rejected, int Total)>
        GroupByPattern(IReadOnlyList<FeedbackRecord> records)
    {
        var groups = new Dictionary<(string Original, string Suggested), (int Accepted, int Rejected, int Total)>();

        foreach (var record in records.Where(r =>
                     r.OriginalText is not null && r.SuggestedText is not null
                     && r.Decision != (int)FeedbackDecision.Skipped))
        {
            var key = NormalizeForPattern(record.OriginalText!, record.SuggestedText!);

            if (!groups.TryGetValue(key, out var counts))
            {
                counts = (0, 0, 0);
            }

            var isAccepted = record.Decision is (int)FeedbackDecision.Accepted
                or (int)FeedbackDecision.Modified;
            var isRejected = record.Decision == (int)FeedbackDecision.Rejected;

            groups[key] = (
                counts.Accepted + (isAccepted ? 1 : 0),
                counts.Rejected + (isRejected ? 1 : 0),
                counts.Total + 1);
        }

        return groups;
    }

    /// <summary>
    /// Normalizes text for pattern matching by lowercasing and collapsing whitespace.
    /// </summary>
    private static (string Original, string Suggested) NormalizeForPattern(
        string original, string suggested)
    {
        return (NormalizeText(original), NormalizeText(suggested));
    }

    /// <summary>
    /// Normalizes text: lowercase, trim, collapse whitespace.
    /// </summary>
    private static string NormalizeText(string text)
    {
        return Regex.Replace(text.ToLowerInvariant().Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Extracts the most common user comment from rejected feedback.
    /// </summary>
    private static string? ExtractCommonRejectionReason(IEnumerable<FeedbackRecord> rejections)
    {
        var comments = rejections
            .Where(r => !string.IsNullOrEmpty(r.UserComment))
            .Select(r => r.UserComment!)
            .ToList();

        if (comments.Count == 0)
            return null;

        // LOGIC: Return the most frequently occurring comment
        return comments
            .GroupBy(c => c.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.First();
    }

    /// <summary>
    /// Describes how a user improved an AI suggestion.
    /// </summary>
    private static string? DescribeImprovement(string suggested, string modified)
    {
        // LOGIC: Classify the improvement based on length changes
        if (modified.Length < suggested.Length * 0.8)
            return "Made more concise";
        if (modified.Length > suggested.Length * 1.2)
            return "Added more detail";

        // LOGIC: Check for specific word replacements
        var suggestedWords = suggested.Split(' ');
        var modifiedWords = modified.Split(' ');
        var addedWords = modifiedWords.Except(suggestedWords).ToList();
        var removedWords = suggestedWords.Except(modifiedWords).ToList();

        if (addedWords.Count > 0 && removedWords.Count > 0)
        {
            return $"Replaced '{string.Join(" ", removedWords.Take(2))}' " +
                   $"with '{string.Join(" ", addedWords.Take(2))}'";
        }

        return null;
    }

    /// <summary>
    /// Truncates text for inclusion in prompts.
    /// </summary>
    private static string TruncateForPrompt(string text, int maxLength = 50)
    {
        if (text.Length <= maxLength)
            return text;
        return text[..(maxLength - 3)] + "...";
    }

    #endregion
}
