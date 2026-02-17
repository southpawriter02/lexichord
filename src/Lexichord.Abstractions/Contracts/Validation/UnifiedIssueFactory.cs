// =============================================================================
// File: UnifiedIssueFactory.cs
// Project: Lexichord.Abstractions
// Description: Factory methods for creating UnifiedIssue from various sources.
// =============================================================================
// LOGIC: Provides conversion methods from source-specific types to UnifiedIssue.
//   Each factory method handles the specific structure of its source type and
//   maps fields appropriately.
//
// v0.7.5e: Unified Issue Model (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Factory methods for creating <see cref="UnifiedIssue"/> instances from various source types.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides conversion methods for each supported source type:
/// <list type="bullet">
///   <item><description><see cref="StyleDeviation"/>: From the Tuning Agent (v0.7.5a)</description></item>
///   <item><description><see cref="StyleViolation"/>: From the Style Linter</description></item>
///   <item><description><see cref="UnifiedFinding"/>: From CKVS Validation Integration (v0.6.5j)</description></item>
///   <item><description><see cref="ValidationFinding"/>: From CKVS Validation Engine</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> All methods are stateless and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
public static class UnifiedIssueFactory
{
    /// <summary>
    /// Creates a <see cref="UnifiedIssue"/> from a <see cref="StyleDeviation"/>.
    /// </summary>
    /// <param name="deviation">The style deviation from the Tuning Agent scanner.</param>
    /// <returns>A new <see cref="UnifiedIssue"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deviation"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> StyleDeviation is the richest source type, already containing:
    /// <list type="bullet">
    ///   <item><description>Location (TextSpan)</description></item>
    ///   <item><description>Priority (maps to severity)</description></item>
    ///   <item><description>Original text and surrounding context</description></item>
    ///   <item><description>Linter suggested fix (if available)</description></item>
    /// </list>
    /// </remarks>
    public static UnifiedIssue FromStyleDeviation(StyleDeviation deviation)
    {
        ArgumentNullException.ThrowIfNull(deviation);

        // LOGIC: Map priority back to severity (reverse of the scanner's mapping)
        var severity = SeverityMapper.FromDeviationPriority(deviation.Priority);

        // LOGIC: Create fix from linter suggestion if available
        var fixes = CreateFixesFromDeviation(deviation);

        return new UnifiedIssue(
            IssueId: deviation.DeviationId,
            SourceId: deviation.RuleId,
            Category: MapRuleCategoryToIssueCategory(deviation.ViolatedRule.Category),
            Severity: severity,
            Message: deviation.Message,
            Location: deviation.Location,
            OriginalText: deviation.OriginalText,
            Fixes: fixes,
            SourceType: "StyleLinter",
            OriginalSource: deviation);
    }

    /// <summary>
    /// Creates a <see cref="UnifiedIssue"/> from a <see cref="StyleViolation"/>.
    /// </summary>
    /// <param name="violation">The style violation from the linter.</param>
    /// <returns>A new <see cref="UnifiedIssue"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="violation"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> StyleViolation provides offset-based positioning which is
    /// converted to a TextSpan for the unified model.
    /// </remarks>
    public static UnifiedIssue FromStyleViolation(StyleViolation violation)
    {
        ArgumentNullException.ThrowIfNull(violation);

        // LOGIC: Create TextSpan from start/end offsets
        var location = TextSpan.FromStartEnd(violation.StartOffset, violation.EndOffset);

        // LOGIC: Map ViolationSeverity to UnifiedSeverity
        var severity = SeverityMapper.FromViolationSeverity(violation.Severity);

        // LOGIC: Create fix from suggestion if available
        var fixes = CreateFixesFromViolation(violation, location);

        return new UnifiedIssue(
            IssueId: Guid.NewGuid(),
            SourceId: violation.Rule.Id,
            Category: MapRuleCategoryToIssueCategory(violation.Rule.Category),
            Severity: severity,
            Message: violation.Message,
            Location: location,
            OriginalText: violation.MatchedText,
            Fixes: fixes,
            SourceType: "StyleLinter",
            OriginalSource: violation);
    }

    /// <summary>
    /// Creates a <see cref="UnifiedIssue"/> from a <see cref="UnifiedFinding"/>.
    /// </summary>
    /// <param name="finding">The unified finding from CKVS integration.</param>
    /// <returns>A new <see cref="UnifiedIssue"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="finding"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> UnifiedFinding uses PropertyPath (string) for location.
    /// Since UnifiedIssue requires a TextSpan, we attempt to extract offsets
    /// from the original source if available, otherwise use a zero-length span.
    /// </remarks>
    public static UnifiedIssue FromUnifiedFinding(UnifiedFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        // LOGIC: Try to get location from original source
        var location = ExtractLocationFromFinding(finding);

        // LOGIC: Map FindingCategory to IssueCategory
        var category = MapFindingCategoryToIssueCategory(finding.Category);

        // LOGIC: Determine source type string
        var sourceType = finding.Source switch
        {
            FindingSource.Validation => "Validation",
            FindingSource.StyleLinter => "StyleLinter",
            FindingSource.GrammarLinter => "GrammarLinter",
            _ => "Unknown"
        };

        // LOGIC: Extract original text from source if available
        var originalText = finding.OriginalStyleViolation?.MatchedText;

        // LOGIC: Convert suggested fix if available
        var fixes = CreateFixesFromUnifiedFinding(finding, location);

        return new UnifiedIssue(
            IssueId: finding.Id,
            SourceId: finding.Code,
            Category: category,
            Severity: finding.Severity,
            Message: finding.Message,
            Location: location,
            OriginalText: originalText,
            Fixes: fixes,
            SourceType: sourceType,
            OriginalSource: finding);
    }

    /// <summary>
    /// Creates a <see cref="UnifiedIssue"/> from a <see cref="ValidationFinding"/>.
    /// </summary>
    /// <param name="finding">The validation finding from CKVS.</param>
    /// <returns>A new <see cref="UnifiedIssue"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="finding"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> ValidationFinding uses PropertyPath for location and has
    /// a different severity enum that requires mapping.
    /// </remarks>
    public static UnifiedIssue FromValidationFinding(ValidationFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        // LOGIC: Map ValidationSeverity to UnifiedSeverity
        var severity = SeverityMapper.FromValidationSeverity(finding.Severity);

        // LOGIC: Create a zero-length span since ValidationFinding uses PropertyPath
        var location = TextSpan.Empty;

        // LOGIC: Create fix from suggested fix if available
        var fixes = CreateFixesFromValidationFinding(finding, location);

        return new UnifiedIssue(
            IssueId: Guid.NewGuid(),
            SourceId: finding.Code,
            Category: IssueCategory.Knowledge, // All validation findings are Knowledge category
            Severity: severity,
            Message: finding.Message,
            Location: location,
            OriginalText: null, // ValidationFinding doesn't have matched text
            Fixes: fixes,
            SourceType: "Validation",
            OriginalSource: finding);
    }

    /// <summary>
    /// Creates multiple <see cref="UnifiedIssue"/> instances from a collection of deviations.
    /// </summary>
    /// <param name="deviations">The style deviations to convert.</param>
    /// <returns>A list of <see cref="UnifiedIssue"/> instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deviations"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Batch conversion for efficiency when processing scan results.
    /// </remarks>
    public static IReadOnlyList<UnifiedIssue> FromDeviations(IEnumerable<StyleDeviation> deviations)
    {
        ArgumentNullException.ThrowIfNull(deviations);

        return deviations.Select(FromStyleDeviation).ToList();
    }

    /// <summary>
    /// Creates multiple <see cref="UnifiedIssue"/> instances from a collection of violations.
    /// </summary>
    /// <param name="violations">The style violations to convert.</param>
    /// <returns>A list of <see cref="UnifiedIssue"/> instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="violations"/> is null.</exception>
    public static IReadOnlyList<UnifiedIssue> FromViolations(IEnumerable<StyleViolation> violations)
    {
        ArgumentNullException.ThrowIfNull(violations);

        return violations.Select(FromStyleViolation).ToList();
    }

    /// <summary>
    /// Creates multiple <see cref="UnifiedIssue"/> instances from a collection of findings.
    /// </summary>
    /// <param name="findings">The unified findings to convert.</param>
    /// <returns>A list of <see cref="UnifiedIssue"/> instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="findings"/> is null.</exception>
    public static IReadOnlyList<UnifiedIssue> FromFindings(IEnumerable<UnifiedFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        return findings.Select(FromUnifiedFinding).ToList();
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a <see cref="RuleCategory"/> to an <see cref="IssueCategory"/>.
    /// </summary>
    private static IssueCategory MapRuleCategoryToIssueCategory(RuleCategory category)
    {
        // LOGIC: RuleCategory values (Terminology, Formatting, Syntax) all map to Style
        return category switch
        {
            RuleCategory.Formatting => IssueCategory.Structure, // Formatting is structural
            _ => IssueCategory.Style // Terminology and Syntax are style issues
        };
    }

    /// <summary>
    /// Maps a <see cref="FindingCategory"/> to an <see cref="IssueCategory"/>.
    /// </summary>
    private static IssueCategory MapFindingCategoryToIssueCategory(FindingCategory category)
    {
        return category switch
        {
            FindingCategory.Schema => IssueCategory.Knowledge,
            FindingCategory.Axiom => IssueCategory.Knowledge,
            FindingCategory.Consistency => IssueCategory.Knowledge,
            FindingCategory.Style => IssueCategory.Style,
            FindingCategory.Grammar => IssueCategory.Grammar,
            FindingCategory.Spelling => IssueCategory.Grammar, // Spelling is a Grammar subcategory
            _ => IssueCategory.Custom
        };
    }

    /// <summary>
    /// Extracts a <see cref="TextSpan"/> from a <see cref="UnifiedFinding"/>.
    /// </summary>
    private static TextSpan ExtractLocationFromFinding(UnifiedFinding finding)
    {
        // LOGIC: Try to get location from original StyleViolation if available
        if (finding.OriginalStyleViolation is { } violation)
        {
            return TextSpan.FromStartEnd(violation.StartOffset, violation.EndOffset);
        }

        // LOGIC: For validation findings, we don't have offset information
        return TextSpan.Empty;
    }

    /// <summary>
    /// Creates fixes from a <see cref="StyleDeviation"/>.
    /// </summary>
    private static IReadOnlyList<UnifiedFix> CreateFixesFromDeviation(StyleDeviation deviation)
    {
        // LOGIC: If linter provides a suggestion, create a fix from it
        if (!string.IsNullOrEmpty(deviation.LinterSuggestedFix))
        {
            var fix = UnifiedFix.Replacement(
                deviation.Location,
                deviation.OriginalText,
                deviation.LinterSuggestedFix,
                $"Replace with: {deviation.LinterSuggestedFix}",
                confidence: 0.7); // Linter suggestions have moderate confidence

            return new[] { fix };
        }

        // LOGIC: If no suggestion but auto-fixable, return empty (AI will generate)
        if (deviation.IsAutoFixable)
        {
            return Array.Empty<UnifiedFix>();
        }

        // LOGIC: Not auto-fixable, return NoFix placeholder
        return new[]
        {
            UnifiedFix.NoFixAvailable(
                deviation.Location,
                deviation.OriginalText,
                "This issue requires manual review")
        };
    }

    /// <summary>
    /// Creates fixes from a <see cref="StyleViolation"/>.
    /// </summary>
    private static IReadOnlyList<UnifiedFix> CreateFixesFromViolation(
        StyleViolation violation,
        TextSpan location)
    {
        // LOGIC: If linter provides a suggestion, create a fix from it
        if (!string.IsNullOrEmpty(violation.Suggestion))
        {
            var fix = UnifiedFix.Replacement(
                location,
                violation.MatchedText,
                violation.Suggestion,
                $"Replace with: {violation.Suggestion}",
                confidence: 0.7);

            return new[] { fix };
        }

        return Array.Empty<UnifiedFix>();
    }

    /// <summary>
    /// Creates fixes from a <see cref="UnifiedFinding"/>.
    /// </summary>
    private static IReadOnlyList<UnifiedFix> CreateFixesFromUnifiedFinding(
        UnifiedFinding finding,
        TextSpan location)
    {
        // LOGIC: Convert the v0.6.5j UnifiedFix to the v0.7.5e UnifiedFix format
        if (finding.SuggestedFix is { } suggestedFix &&
            !string.IsNullOrEmpty(suggestedFix.ReplacementText))
        {
            var originalText = finding.OriginalStyleViolation?.MatchedText;

            var fix = new UnifiedFix(
                suggestedFix.Id,
                location,
                originalText,
                suggestedFix.ReplacementText,
                FixType.Replacement,
                suggestedFix.Description,
                suggestedFix.Confidence,
                suggestedFix.CanAutoApply);

            return new[] { fix };
        }

        return Array.Empty<UnifiedFix>();
    }

    /// <summary>
    /// Creates fixes from a <see cref="ValidationFinding"/>.
    /// </summary>
    private static IReadOnlyList<UnifiedFix> CreateFixesFromValidationFinding(
        ValidationFinding finding,
        TextSpan location)
    {
        // LOGIC: If validation finding has a suggested fix text, create a fix
        if (!string.IsNullOrEmpty(finding.SuggestedFix))
        {
            // LOGIC: Validation fixes are typically rewrites since we don't have
            // the original text or precise location
            var fix = new UnifiedFix(
                Guid.NewGuid(),
                location,
                OldText: null,
                finding.SuggestedFix,
                FixType.Rewrite,
                finding.SuggestedFix,
                Confidence: 0.5, // Lower confidence without precise location
                CanAutoApply: false);

            return new[] { fix };
        }

        return Array.Empty<UnifiedFix>();
    }

    #endregion
}
