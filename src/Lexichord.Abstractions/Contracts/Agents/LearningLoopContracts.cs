// -----------------------------------------------------------------------
// <copyright file="LearningLoopContracts.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// User's decision on a fix suggestion.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Represents the three meaningful feedback decisions a user can make
/// when reviewing a fix suggestion in the Tuning Panel. <c>Skipped</c> suggestions
/// (via <see cref="SuggestionStatus.Skipped"/>) are NOT recorded as feedback since
/// they represent deferral, not a quality signal.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="FixFeedback"/>
/// <seealso cref="SuggestionStatus"/>
public enum FeedbackDecision
{
    /// <summary>
    /// User accepted the suggestion as-is.
    /// </summary>
    Accepted = 0,

    /// <summary>
    /// User rejected the suggestion.
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// User modified the suggestion before accepting.
    /// </summary>
    Modified = 2,

    /// <summary>
    /// User skipped without feedback.
    /// </summary>
    Skipped = 3
}

/// <summary>
/// Feedback record for a fix suggestion decision.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Captures all relevant context when a user makes a decision on a
/// fix suggestion. This data feeds into the Learning Loop's pattern analysis and
/// prompt enhancement pipeline. Records are persisted to SQLite and aggregated
/// to extract accepted/rejected patterns.
/// </para>
/// <para>
/// <b>Privacy:</b> When anonymization is enabled via <see cref="LearningPrivacyOptions"/>,
/// text fields are reduced to structural patterns and user identifiers are hashed
/// using SHA256 before storage.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="ILearningLoopService"/>
/// <seealso cref="FeedbackDecision"/>
public record FixFeedback
{
    /// <summary>
    /// Unique feedback identifier.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Generated at creation time. Used as the primary key in SQLite storage.
    /// </remarks>
    public required Guid FeedbackId { get; init; }

    /// <summary>
    /// The suggestion that received feedback.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Links back to <see cref="FixSuggestion.SuggestionId"/> for traceability.
    /// </remarks>
    public required Guid SuggestionId { get; init; }

    /// <summary>
    /// The deviation that was addressed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Links back to <see cref="StyleDeviation.DeviationId"/> for traceability.
    /// </remarks>
    public required Guid DeviationId { get; init; }

    /// <summary>
    /// The rule that was violated.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Sourced from <see cref="StyleDeviation.RuleId"/>. Used as the primary
    /// grouping key for pattern analysis and learning context retrieval.
    /// </remarks>
    public required string RuleId { get; init; }

    /// <summary>
    /// Category of the violation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Sourced from <see cref="StyleDeviation.Category"/>. Used for
    /// category-level statistics aggregation.
    /// </remarks>
    public required string Category { get; init; }

    /// <summary>
    /// User's decision.
    /// </summary>
    public required FeedbackDecision Decision { get; init; }

    /// <summary>
    /// Original text before fix.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Sourced from <see cref="StyleDeviation.OriginalText"/>. May be
    /// anonymized to structural patterns when <see cref="LearningPrivacyOptions.StoreOriginalText"/>
    /// is <c>false</c>.
    /// </remarks>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Suggested text from AI.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Sourced from <see cref="FixSuggestion.SuggestedText"/>. Used with
    /// <see cref="OriginalText"/> to form the pattern key for grouping.
    /// </remarks>
    public required string SuggestedText { get; init; }

    /// <summary>
    /// Final text after user decision (may be modified).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> For <see cref="FeedbackDecision.Modified"/> decisions, this contains
    /// the user-edited text. For <see cref="FeedbackDecision.Accepted"/>, this matches
    /// <see cref="SuggestedText"/>. For <see cref="FeedbackDecision.Rejected"/>, this is <c>null</c>.
    /// </remarks>
    public string? FinalText { get; init; }

    /// <summary>
    /// User's modification if they edited the suggestion.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Only populated when <see cref="Decision"/> is <see cref="FeedbackDecision.Modified"/>.
    /// Contains the exact text the user typed. Valuable for learning user preferences.
    /// </remarks>
    public string? UserModification { get; init; }

    /// <summary>
    /// Confidence of the original suggestion.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Sourced from <see cref="FixSuggestion.Confidence"/>. Used to measure
    /// confidence calibration accuracy (how well confidence predicts acceptance).
    /// </remarks>
    public double OriginalConfidence { get; init; }

    /// <summary>
    /// Timestamp of the feedback.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Uses <see cref="DateTime"/> (UTC) to match the codebase convention
    /// established by <see cref="Events.SuggestionAcceptedEvent.Timestamp"/>.
    /// </remarks>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional user comment explaining their decision.
    /// </summary>
    public string? UserComment { get; init; }

    /// <summary>
    /// Anonymized user identifier for aggregation.
    /// Only stored if privacy settings allow.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When <see cref="LearningPrivacyOptions.AnonymizeUsers"/> is enabled,
    /// this is hashed via SHA256 before storage.
    /// </remarks>
    public string? AnonymizedUserId { get; init; }

    /// <summary>
    /// Whether this was part of a bulk operation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Set to <c>true</c> for feedback from "Accept All High Confidence"
    /// bulk operations. Can be filtered out in statistics for individual decision analysis.
    /// </remarks>
    public bool IsBulkOperation { get; init; }
}

/// <summary>
/// Context derived from learning data to enhance prompts.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Aggregates patterns from past user feedback for a specific rule.
/// The <see cref="PromptEnhancement"/> string is injected into fix generation prompts
/// to guide the LLM toward patterns the user prefers and away from patterns they reject.
/// </para>
/// <para>
/// <b>Sufficient Data:</b> Requires at least 10 feedback samples (<see cref="HasSufficientData"/>)
/// before patterns are considered statistically meaningful.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="ILearningLoopService.GetLearningContextAsync"/>
/// <seealso cref="AcceptedPattern"/>
/// <seealso cref="RejectedPattern"/>
public record LearningContext
{
    /// <summary>
    /// The rule this context applies to.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Overall acceptance rate for this rule.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as (Accepted + Modified) / (Total - Skipped).
    /// Used for confidence calibration and prompt enhancement tone.
    /// </remarks>
    public double AcceptanceRate { get; init; }

    /// <summary>
    /// Total feedback samples for this rule.
    /// </summary>
    public int SampleCount { get; init; }

    /// <summary>
    /// Patterns that were frequently accepted.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Patterns where the success rate (accepted+modified / total) >= 0.7
    /// and at least 3 occurrences. Ordered by frequency descending, limited to top 5.
    /// </remarks>
    public IReadOnlyList<AcceptedPattern> AcceptedPatterns { get; init; } =
        Array.Empty<AcceptedPattern>();

    /// <summary>
    /// Patterns that were frequently rejected.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Patterns where the success rate <= 0.3 and at least 3 occurrences.
    /// Ordered by rejection count descending, limited to top 5.
    /// </remarks>
    public IReadOnlyList<RejectedPattern> RejectedPatterns { get; init; } =
        Array.Empty<RejectedPattern>();

    /// <summary>
    /// User modifications that improved suggestions.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Extracted from <see cref="FeedbackDecision.Modified"/> feedback
    /// where the user's edit provides useful improvement signals. Limited to top 3.
    /// </remarks>
    public IReadOnlyList<UserModificationExample> UsefulModifications { get; init; } =
        Array.Empty<UserModificationExample>();

    /// <summary>
    /// Prompt enhancement text derived from learning.
    /// Ready to inject into prompts.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Generated by <c>PatternAnalyzer.GeneratePromptEnhancement()</c>.
    /// Contains structured guidance text with accepted patterns, rejected patterns,
    /// modification insights, and confidence calibration notes.
    /// </remarks>
    public string? PromptEnhancement { get; init; }

    /// <summary>
    /// Adjusted confidence baseline for this rule.
    /// Based on actual acceptance rates.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When <see cref="HasSufficientData"/> is true, this provides
    /// a calibrated confidence baseline derived from actual user behavior.
    /// </remarks>
    public double? AdjustedConfidenceBaseline { get; init; }

    /// <summary>
    /// Whether there's enough data to be meaningful.
    /// Typically requires 10+ samples.
    /// </summary>
    public bool HasSufficientData => SampleCount >= 10;

    /// <summary>
    /// Creates empty context for rules with no learning data.
    /// </summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <returns>An empty <see cref="LearningContext"/> with no patterns or enhancement.</returns>
    public static LearningContext Empty(string ruleId) => new()
    {
        RuleId = ruleId,
        AcceptanceRate = 0,
        SampleCount = 0
    };
}

/// <summary>
/// A pattern that was frequently accepted by users.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Represents a (OriginalPattern → SuggestedPattern) mapping that users
/// accepted at a high rate. Used in prompt enhancement to tell the LLM "suggest patterns
/// like this, they work well."
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <param name="OriginalPattern">The original text pattern (may be normalized).</param>
/// <param name="SuggestedPattern">The accepted replacement text pattern.</param>
/// <param name="AcceptCount">Number of times this pattern was accepted or modified.</param>
/// <param name="SuccessRate">Acceptance rate for this specific pattern (0.0–1.0).</param>
public record AcceptedPattern(
    string OriginalPattern,
    string SuggestedPattern,
    int AcceptCount,
    double SuccessRate);

/// <summary>
/// A pattern that was frequently rejected by users.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Represents a (OriginalPattern → SuggestedPattern) mapping that users
/// rejected at a high rate. Used in prompt enhancement to tell the LLM "avoid suggesting
/// patterns like this."
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <param name="OriginalPattern">The original text pattern (may be normalized).</param>
/// <param name="SuggestedPattern">The rejected replacement text pattern.</param>
/// <param name="RejectCount">Number of times this pattern was rejected.</param>
/// <param name="CommonRejectionReason">Most common user comment explaining rejection (nullable).</param>
public record RejectedPattern(
    string OriginalPattern,
    string SuggestedPattern,
    int RejectCount,
    string? CommonRejectionReason);

/// <summary>
/// An example of a user modification that improved a suggestion.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Captures how users improve AI suggestions, providing signals
/// like "make more concise", "add more detail", or specific word replacements.
/// Used in prompt enhancement to guide the LLM toward user-preferred patterns.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <param name="OriginalSuggestion">The AI's original suggested text.</param>
/// <param name="UserModification">The user's modified version of the text.</param>
/// <param name="Improvement">A description of how the user improved the suggestion.</param>
public record UserModificationExample(
    string OriginalSuggestion,
    string UserModification,
    string Improvement);

/// <summary>
/// Statistics about learning effectiveness.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Provides aggregated statistics about user feedback patterns. These
/// statistics help users and administrators understand how well the Tuning Agent
/// is performing and whether it is improving over time.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="ILearningLoopService.GetStatisticsAsync"/>
/// <seealso cref="RuleLearningStats"/>
/// <seealso cref="CategoryLearningStats"/>
public record LearningStatistics
{
    /// <summary>
    /// Total feedback samples in the dataset.
    /// </summary>
    public int TotalFeedback { get; init; }

    /// <summary>
    /// Overall acceptance rate across all rules.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as (Accepted + Modified) / (Total - Skipped).
    /// </remarks>
    public double OverallAcceptanceRate { get; init; }

    /// <summary>
    /// Acceptance rate trend (positive = improving).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated by comparing the acceptance rate of the last 7 days
    /// against the previous 7 days. Positive values indicate improvement.
    /// </remarks>
    public double AcceptanceRateTrend { get; init; }

    /// <summary>
    /// Number of distinct rules with feedback.
    /// </summary>
    public int RulesWithFeedback { get; init; }

    /// <summary>
    /// Statistics by rule.
    /// </summary>
    public IReadOnlyDictionary<string, RuleLearningStats> ByRule { get; init; } =
        new Dictionary<string, RuleLearningStats>();

    /// <summary>
    /// Statistics by category.
    /// </summary>
    public IReadOnlyDictionary<string, CategoryLearningStats> ByCategory { get; init; } =
        new Dictionary<string, CategoryLearningStats>();

    /// <summary>
    /// Time period for these statistics.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Uses the existing <see cref="DateRange"/> type from
    /// <c>Lexichord.Abstractions.Contracts</c> (v0.5.5a). Null values indicate
    /// an open-ended range (all-time).
    /// </remarks>
    public required DateRange Period { get; init; }

    /// <summary>
    /// Rate of modification vs acceptance.
    /// </summary>
    public double ModificationRate { get; init; }

    /// <summary>
    /// Rate of skip vs total decisions.
    /// </summary>
    public double SkipRate { get; init; }

    /// <summary>
    /// Creates empty statistics with default values.
    /// </summary>
    /// <returns>An empty <see cref="LearningStatistics"/> instance.</returns>
    public static LearningStatistics Empty => new()
    {
        Period = new DateRange(null, null)
    };
}

/// <summary>
/// Learning statistics for a specific rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Provides per-rule feedback statistics including acceptance rate,
/// modification rate, and confidence accuracy. Helps identify rules where the
/// AI performs well or poorly.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <param name="RuleId">The style rule identifier.</param>
/// <param name="RuleName">The display name of the rule.</param>
/// <param name="FeedbackCount">Total number of feedback records for this rule.</param>
/// <param name="AcceptanceRate">Acceptance rate (0.0–1.0).</param>
/// <param name="ModificationRate">Rate of modifications vs total decisions (0.0–1.0).</param>
/// <param name="ConfidenceAccuracy">How well confidence predicted acceptance (0.0–1.0).</param>
public record RuleLearningStats(
    string RuleId,
    string RuleName,
    int FeedbackCount,
    double AcceptanceRate,
    double ModificationRate,
    double ConfidenceAccuracy);

/// <summary>
/// Learning statistics for a category.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Provides per-category feedback statistics. Helps identify
/// which categories of rules produce the best/worst suggestions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <param name="Category">The rule category name.</param>
/// <param name="FeedbackCount">Total feedback records in this category.</param>
/// <param name="AcceptanceRate">Acceptance rate for this category (0.0–1.0).</param>
/// <param name="TopAcceptedRules">Rule IDs with highest acceptance rate in this category.</param>
/// <param name="TopRejectedRules">Rule IDs with lowest acceptance rate in this category.</param>
public record CategoryLearningStats(
    string Category,
    int FeedbackCount,
    double AcceptanceRate,
    IReadOnlyList<string> TopAcceptedRules,
    IReadOnlyList<string> TopRejectedRules);

/// <summary>
/// Filter for learning statistics queries.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> All filter properties are optional. When a property is null,
/// no filtering is applied for that criterion. Multiple criteria are combined with AND logic.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="ILearningLoopService.GetStatisticsAsync"/>
public record LearningStatisticsFilter
{
    /// <summary>
    /// Time period to filter by.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Uses existing <see cref="DateRange"/> from v0.5.5a.
    /// Null means no temporal restriction.
    /// </remarks>
    public DateRange? Period { get; init; }

    /// <summary>
    /// Specific rule IDs to include.
    /// </summary>
    public IReadOnlyList<string>? RuleIds { get; init; }

    /// <summary>
    /// Specific categories to include.
    /// </summary>
    public IReadOnlyList<string>? Categories { get; init; }

    /// <summary>
    /// Whether to exclude skipped feedback from statistics.
    /// </summary>
    public bool ExcludeSkipped { get; init; }

    /// <summary>
    /// Whether to exclude bulk operation feedback from statistics.
    /// </summary>
    public bool ExcludeBulk { get; init; }
}

/// <summary>
/// Options for learning data export.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Controls what data is included in a learning data export.
/// Exports can be shared across team members to bootstrap learning data.
/// Privacy settings control whether original text is included.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="ILearningLoopService.ExportLearningDataAsync"/>
/// <seealso cref="LearningExport"/>
public record LearningExportOptions
{
    /// <summary>
    /// Time period to export.
    /// </summary>
    public DateRange? Period { get; init; }

    /// <summary>
    /// Specific rule IDs to export.
    /// </summary>
    public IReadOnlyList<string>? RuleIds { get; init; }

    /// <summary>
    /// Whether to include pattern data in the export.
    /// </summary>
    public bool IncludePatterns { get; init; } = true;

    /// <summary>
    /// Whether to include statistics in the export.
    /// </summary>
    public bool IncludeStatistics { get; init; } = true;

    /// <summary>
    /// Whether to anonymize all content in the export.
    /// </summary>
    public bool AnonymizeAll { get; init; } = true;

    /// <summary>
    /// Whether to include original text content.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When <c>false</c> (default), only pattern structure
    /// is exported without verbatim text content.
    /// </remarks>
    public bool IncludeOriginalText { get; init; }
}

/// <summary>
/// Exportable learning data.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The output format for learning data exports. Contains patterns
/// and optionally statistics. Can be imported by another instance to bootstrap
/// learning data for a team.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="LearningExportOptions"/>
/// <seealso cref="ExportedPattern"/>
public record LearningExport
{
    /// <summary>
    /// Version identifier for export format compatibility.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// When the export was created.
    /// </summary>
    public required DateTime ExportedAt { get; init; }

    /// <summary>
    /// Exported pattern data.
    /// </summary>
    public IReadOnlyList<ExportedPattern> Patterns { get; init; } = Array.Empty<ExportedPattern>();

    /// <summary>
    /// Exported statistics, if included.
    /// </summary>
    public LearningStatistics? Statistics { get; init; }

    /// <summary>
    /// Integrity checksum for the export.
    /// </summary>
    public string? Checksum { get; init; }
}

/// <summary>
/// A pattern in the export format.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Represents a single accept/reject pattern in a portable export format.
/// The <see cref="PatternType"/> indicates whether this is an "Accepted" or "Rejected" pattern.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <param name="RuleId">The rule this pattern applies to.</param>
/// <param name="PatternType">"Accepted" or "Rejected".</param>
/// <param name="OriginalPattern">The original text pattern.</param>
/// <param name="SuggestedPattern">The suggested replacement pattern.</param>
/// <param name="Count">Number of occurrences of this pattern.</param>
/// <param name="SuccessRate">Success rate for this pattern (0.0–1.0).</param>
public record ExportedPattern(
    string RuleId,
    string PatternType,
    string OriginalPattern,
    string SuggestedPattern,
    int Count,
    double SuccessRate);

/// <summary>
/// Options for clearing learning data.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Provides granular control over what learning data to clear.
/// Requires a confirmation token to prevent accidental data loss.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="ILearningLoopService.ClearLearningDataAsync"/>
public record ClearLearningDataOptions
{
    /// <summary>
    /// Time period to clear (null for all time).
    /// </summary>
    public DateRange? Period { get; init; }

    /// <summary>
    /// Specific rule IDs to clear.
    /// </summary>
    public IReadOnlyList<string>? RuleIds { get; init; }

    /// <summary>
    /// Whether to clear all data regardless of other filters.
    /// </summary>
    public bool ClearAll { get; init; }

    /// <summary>
    /// Confirmation token required to execute the clear operation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Must be set to "CONFIRM" to prevent accidental data loss.
    /// </remarks>
    public required string ConfirmationToken { get; init; }
}

/// <summary>
/// Privacy options for learning data storage.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Controls how learning data is stored and retained.
/// Persisted via <see cref="ISettingsService"/> under the "Learning:Privacy:" key prefix.
/// </para>
/// <para>
/// <b>Defaults:</b> Privacy-first defaults — users are anonymized, original text is not
/// stored, and data is retained for 365 days maximum.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="ILearningLoopService.GetPrivacyOptions"/>
/// <seealso cref="ILearningLoopService.SetPrivacyOptionsAsync"/>
public record LearningPrivacyOptions
{
    /// <summary>
    /// Whether to anonymize user identifiers.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When true, user IDs are hashed via SHA256 before storage.
    /// Default is <c>true</c> (privacy-first).
    /// </remarks>
    public bool AnonymizeUsers { get; init; } = true;

    /// <summary>
    /// Whether to store original text (vs. patterns only).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When <c>false</c> (default), original text is replaced with
    /// structural patterns (e.g., long words replaced with "[WORD]") before storage.
    /// </remarks>
    public bool StoreOriginalText { get; init; }

    /// <summary>
    /// Maximum age for learning data before auto-deletion.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Data older than this is deleted during retention enforcement.
    /// Null means no age-based retention. Default is 365 days.
    /// </remarks>
    public TimeSpan? MaxDataAge { get; init; } = TimeSpan.FromDays(365);

    /// <summary>
    /// Whether to participate in team learning sharing.
    /// </summary>
    public bool ParticipateInTeamLearning { get; init; } = true;

    /// <summary>
    /// Maximum number of feedback records to store.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When exceeded, oldest records are deleted to enforce the limit.
    /// Default is 10,000 records.
    /// </remarks>
    public int MaxRecords { get; init; } = 10000;
}
