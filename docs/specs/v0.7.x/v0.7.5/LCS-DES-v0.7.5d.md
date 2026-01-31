# LCS-DES-075d: Design Specification — Learning Loop

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `AGT-075d` | Sub-part of AGT-075 |
| **Feature Name** | `Learning Loop` | Feedback system for continuous improvement |
| **Target Version** | `v0.7.5d` | Fourth sub-part of v0.7.5 |
| **Module Scope** | `Lexichord.Modules.Agents` | Agent module |
| **Swimlane** | `Ensemble` | Part of Agents vertical |
| **License Tier** | `Teams` | Learning Loop requires Teams tier |
| **Feature Gate Key** | `FeatureFlags.Agents.LearningLoop` | Separate gate from parent |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-075-INDEX](./LCS-DES-075-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-075 S3.4](./LCS-SBD-075.md#34-v075d-learning-loop) | |

---

## 2. Executive Summary

### 2.1 The Requirement

AI-generated fix suggestions need continuous improvement based on user feedback:

- **Pattern Recognition:** Learn which fix patterns users prefer
- **Confidence Calibration:** Adjust confidence scores based on actual acceptance rates
- **Prompt Enhancement:** Improve prompts using successful patterns
- **Team Learning:** Share learning across team members
- **Privacy Controls:** Allow users to control what data is stored

> **Goal:** Implement a feedback system that captures user decisions and uses them to improve future fix suggestions.

### 2.2 The Proposed Solution

Implement `LearningLoopService` that:

1. Records accept/reject/modify decisions with context
2. Analyzes patterns by rule, category, and user
3. Generates learning context to enhance prompts
4. Provides statistics for monitoring improvement
5. Supports export/import for team sharing
6. Respects privacy preferences

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `FixSuggestion` | v0.7.5b | Suggestion data |
| `StyleDeviation` | v0.7.5a | Deviation data |
| `ISettingsService` | v0.1.6a | Privacy settings |
| `IMediator` | v0.0.7a | Event publishing |
| `ILicenseContext` | v0.0.4c | License checking |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `SQLite-net` | 1.8.x | Local database storage |
| `System.Text.Json` | 8.x | Export/import serialization |

### 3.2 Licensing Behavior

- **Load Behavior:** Hard Gate
  - Service loads but throws `LicenseRequiredException` for non-Teams users
  - UI shows upgrade prompt for Writer Pro users
  - Core Tuning Agent works without Learning Loop

---

## 4. Data Contract (The API)

### 4.1 Primary Interface

```csharp
namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Service for capturing and utilizing user feedback to improve fix suggestions.
/// Requires Teams license for full functionality.
/// </summary>
public interface ILearningLoopService
{
    /// <summary>
    /// Records user feedback for a fix suggestion.
    /// </summary>
    /// <param name="feedback">The feedback record to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordFeedbackAsync(FixFeedback feedback, CancellationToken ct = default);

    /// <summary>
    /// Gets learning context to enhance prompts for a specific rule.
    /// Returns patterns learned from past user decisions.
    /// </summary>
    /// <param name="ruleId">The style rule ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Learning context for prompt enhancement.</returns>
    Task<LearningContext> GetLearningContextAsync(string ruleId, CancellationToken ct = default);

    /// <summary>
    /// Gets aggregated learning statistics.
    /// </summary>
    /// <param name="filter">Optional filter for time period or rules.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated statistics.</returns>
    Task<LearningStatistics> GetStatisticsAsync(
        LearningStatisticsFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Exports learning data for team sharing.
    /// Respects privacy settings.
    /// </summary>
    /// <param name="options">Export options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Exportable learning data.</returns>
    Task<LearningExport> ExportLearningDataAsync(
        LearningExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Imports learning data from team export.
    /// Merges with existing data.
    /// </summary>
    /// <param name="data">The data to import.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ImportLearningDataAsync(LearningExport data, CancellationToken ct = default);

    /// <summary>
    /// Clears learning data with confirmation.
    /// </summary>
    /// <param name="options">Clear options.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ClearLearningDataAsync(ClearLearningDataOptions options, CancellationToken ct = default);

    /// <summary>
    /// Gets current privacy settings for learning data.
    /// </summary>
    LearningPrivacyOptions GetPrivacyOptions();

    /// <summary>
    /// Updates privacy settings.
    /// </summary>
    Task SetPrivacyOptionsAsync(LearningPrivacyOptions options, CancellationToken ct = default);
}
```

### 4.2 Data Records

```csharp
namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Feedback record for a fix suggestion decision.
/// </summary>
public record FixFeedback
{
    /// <summary>
    /// Unique feedback identifier.
    /// </summary>
    public required Guid FeedbackId { get; init; }

    /// <summary>
    /// The suggestion that received feedback.
    /// </summary>
    public required Guid SuggestionId { get; init; }

    /// <summary>
    /// The deviation that was addressed.
    /// </summary>
    public required Guid DeviationId { get; init; }

    /// <summary>
    /// The rule that was violated.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Category of the violation.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// User's decision.
    /// </summary>
    public required FeedbackDecision Decision { get; init; }

    /// <summary>
    /// Original text before fix.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Suggested text from AI.
    /// </summary>
    public required string SuggestedText { get; init; }

    /// <summary>
    /// Final text after user decision (may be modified).
    /// </summary>
    public string? FinalText { get; init; }

    /// <summary>
    /// User's modification if they edited the suggestion.
    /// </summary>
    public string? UserModification { get; init; }

    /// <summary>
    /// Confidence of the original suggestion.
    /// </summary>
    public double OriginalConfidence { get; init; }

    /// <summary>
    /// Timestamp of the feedback.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional user comment explaining their decision.
    /// </summary>
    public string? UserComment { get; init; }

    /// <summary>
    /// Anonymized user identifier for aggregation.
    /// Only stored if privacy settings allow.
    /// </summary>
    public string? AnonymizedUserId { get; init; }

    /// <summary>
    /// Whether this was part of a bulk operation.
    /// </summary>
    public bool IsBulkOperation { get; init; }
}

/// <summary>
/// User's decision on a fix suggestion.
/// </summary>
public enum FeedbackDecision
{
    /// <summary>User accepted the suggestion as-is.</summary>
    Accepted,

    /// <summary>User rejected the suggestion.</summary>
    Rejected,

    /// <summary>User modified the suggestion before accepting.</summary>
    Modified,

    /// <summary>User skipped without feedback.</summary>
    Skipped
}

/// <summary>
/// Context derived from learning data to enhance prompts.
/// </summary>
public record LearningContext
{
    /// <summary>
    /// The rule this context applies to.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Overall acceptance rate for this rule.
    /// </summary>
    public double AcceptanceRate { get; init; }

    /// <summary>
    /// Total feedback samples for this rule.
    /// </summary>
    public int SampleCount { get; init; }

    /// <summary>
    /// Patterns that were frequently accepted.
    /// </summary>
    public IReadOnlyList<AcceptedPattern> AcceptedPatterns { get; init; } =
        Array.Empty<AcceptedPattern>();

    /// <summary>
    /// Patterns that were frequently rejected.
    /// </summary>
    public IReadOnlyList<RejectedPattern> RejectedPatterns { get; init; } =
        Array.Empty<RejectedPattern>();

    /// <summary>
    /// User modifications that improved suggestions.
    /// </summary>
    public IReadOnlyList<UserModificationExample> UsefulModifications { get; init; } =
        Array.Empty<UserModificationExample>();

    /// <summary>
    /// Prompt enhancement text derived from learning.
    /// Ready to inject into prompts.
    /// </summary>
    public string? PromptEnhancement { get; init; }

    /// <summary>
    /// Adjusted confidence baseline for this rule.
    /// Based on actual acceptance rates.
    /// </summary>
    public double? AdjustedConfidenceBaseline { get; init; }

    /// <summary>
    /// Whether there's enough data to be meaningful.
    /// Typically requires 10+ samples.
    /// </summary>
    public bool HasSufficientData => SampleCount >= 10;

    /// <summary>
    /// Creates empty context for rules with no learning data.
    /// </summary>
    public static LearningContext Empty(string ruleId) => new()
    {
        RuleId = ruleId,
        AcceptanceRate = 0,
        SampleCount = 0
    };
}

/// <summary>
/// A pattern that was frequently accepted.
/// </summary>
public record AcceptedPattern(
    string OriginalPattern,
    string SuggestedPattern,
    int AcceptCount,
    double SuccessRate);

/// <summary>
/// A pattern that was frequently rejected.
/// </summary>
public record RejectedPattern(
    string OriginalPattern,
    string SuggestedPattern,
    int RejectCount,
    string? CommonRejectionReason);

/// <summary>
/// An example of a user modification that improved a suggestion.
/// </summary>
public record UserModificationExample(
    string OriginalSuggestion,
    string UserModification,
    string Improvement);

/// <summary>
/// Statistics about learning effectiveness.
/// </summary>
public record LearningStatistics
{
    /// <summary>
    /// Total feedback samples in the dataset.
    /// </summary>
    public int TotalFeedback { get; init; }

    /// <summary>
    /// Overall acceptance rate across all rules.
    /// </summary>
    public double OverallAcceptanceRate { get; init; }

    /// <summary>
    /// Acceptance rate trend (positive = improving).
    /// </summary>
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
    public required DateRange Period { get; init; }

    /// <summary>
    /// Rate of modification vs acceptance.
    /// </summary>
    public double ModificationRate { get; init; }

    /// <summary>
    /// Rate of skip vs total decisions.
    /// </summary>
    public double SkipRate { get; init; }
}

/// <summary>
/// Learning statistics for a specific rule.
/// </summary>
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
public record CategoryLearningStats(
    string Category,
    int FeedbackCount,
    double AcceptanceRate,
    IReadOnlyList<string> TopAcceptedRules,
    IReadOnlyList<string> TopRejectedRules);

/// <summary>
/// Date range for filtering.
/// </summary>
public record DateRange(DateTimeOffset? Start, DateTimeOffset? End)
{
    public static DateRange AllTime => new(null, null);
    public static DateRange Last30Days => new(DateTimeOffset.UtcNow.AddDays(-30), null);
    public static DateRange Last7Days => new(DateTimeOffset.UtcNow.AddDays(-7), null);
}

/// <summary>
/// Filter for learning statistics.
/// </summary>
public record LearningStatisticsFilter
{
    public DateRange? Period { get; init; }
    public IReadOnlyList<string>? RuleIds { get; init; }
    public IReadOnlyList<string>? Categories { get; init; }
    public bool ExcludeSkipped { get; init; }
    public bool ExcludeBulk { get; init; }
}

/// <summary>
/// Options for learning data export.
/// </summary>
public record LearningExportOptions
{
    public DateRange? Period { get; init; }
    public IReadOnlyList<string>? RuleIds { get; init; }
    public bool IncludePatterns { get; init; } = true;
    public bool IncludeStatistics { get; init; } = true;
    public bool AnonymizeAll { get; init; } = true;
    public bool IncludeOriginalText { get; init; } = false;
}

/// <summary>
/// Exportable learning data.
/// </summary>
public record LearningExport
{
    public required string Version { get; init; }
    public required DateTimeOffset ExportedAt { get; init; }
    public IReadOnlyList<ExportedPattern> Patterns { get; init; } = Array.Empty<ExportedPattern>();
    public LearningStatistics? Statistics { get; init; }
    public string? Checksum { get; init; }
}

/// <summary>
/// A pattern in the export format.
/// </summary>
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
public record ClearLearningDataOptions
{
    public DateRange? Period { get; init; }
    public IReadOnlyList<string>? RuleIds { get; init; }
    public bool ClearAll { get; init; }
    public required string ConfirmationToken { get; init; }
}

/// <summary>
/// Privacy options for learning data.
/// </summary>
public record LearningPrivacyOptions
{
    /// <summary>
    /// Whether to anonymize user identifiers.
    /// </summary>
    public bool AnonymizeUsers { get; init; } = true;

    /// <summary>
    /// Whether to store original text (vs. patterns only).
    /// </summary>
    public bool StoreOriginalText { get; init; } = false;

    /// <summary>
    /// Maximum age for learning data before auto-deletion.
    /// </summary>
    public TimeSpan? MaxDataAge { get; init; } = TimeSpan.FromDays(365);

    /// <summary>
    /// Whether to participate in team learning sharing.
    /// </summary>
    public bool ParticipateInTeamLearning { get; init; } = true;

    /// <summary>
    /// Maximum number of feedback records to store.
    /// </summary>
    public int MaxRecords { get; init; } = 10000;
}
```

---

## 5. Implementation Logic

### 5.1 Data Flow Diagram

```mermaid
sequenceDiagram
    participant UI as Accept/Reject UI
    participant Loop as LearningLoopService
    participant Store as FeedbackStore (SQLite)
    participant Analyzer as PatternAnalyzer
    participant Generator as FixSuggestionGenerator

    Note over UI,Generator: Feedback Recording

    UI->>Loop: RecordFeedbackAsync(feedback)
    Loop->>Loop: ApplyPrivacyOptions(feedback)
    Loop->>Store: Insert feedback record
    Store-->>Loop: Stored

    Note over UI,Generator: Pattern Analysis (async)

    Loop->>Analyzer: AnalyzePatternsAsync(ruleId)
    Analyzer->>Store: QueryFeedback(ruleId)
    Store-->>Analyzer: Feedback records
    Analyzer->>Analyzer: ExtractPatterns()
    Analyzer->>Analyzer: CalculateSuccessRates()
    Analyzer->>Store: UpdatePatternCache(patterns)

    Note over UI,Generator: Learning Context Retrieval

    Generator->>Loop: GetLearningContextAsync(ruleId)
    Loop->>Store: GetCachedPatterns(ruleId)
    Store-->>Loop: Patterns
    Loop->>Loop: BuildPromptEnhancement()
    Loop-->>Generator: LearningContext
```

### 5.2 Database Schema

```sql
-- SQLite schema for learning data

-- Feedback records
CREATE TABLE IF NOT EXISTS feedback (
    id TEXT PRIMARY KEY,
    suggestion_id TEXT NOT NULL,
    deviation_id TEXT NOT NULL,
    rule_id TEXT NOT NULL,
    category TEXT NOT NULL,
    decision INTEGER NOT NULL,
    original_text TEXT,
    suggested_text TEXT,
    final_text TEXT,
    user_modification TEXT,
    original_confidence REAL NOT NULL,
    timestamp TEXT NOT NULL,
    user_comment TEXT,
    anonymized_user_id TEXT,
    is_bulk_operation INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_feedback_rule ON feedback(rule_id);
CREATE INDEX IF NOT EXISTS idx_feedback_timestamp ON feedback(timestamp);
CREATE INDEX IF NOT EXISTS idx_feedback_decision ON feedback(decision);

-- Pattern cache (computed from feedback)
CREATE TABLE IF NOT EXISTS pattern_cache (
    rule_id TEXT NOT NULL,
    pattern_type TEXT NOT NULL,
    original_pattern TEXT NOT NULL,
    suggested_pattern TEXT NOT NULL,
    count INTEGER NOT NULL,
    success_rate REAL NOT NULL,
    last_updated TEXT NOT NULL,
    PRIMARY KEY (rule_id, pattern_type, original_pattern, suggested_pattern)
);

-- Statistics cache
CREATE TABLE IF NOT EXISTS statistics_cache (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    computed_at TEXT NOT NULL
);

-- Settings
CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
```

### 5.3 Pattern Extraction

```csharp
/// <summary>
/// Analyzes feedback to extract patterns.
/// </summary>
public class PatternAnalyzer
{
    private readonly IFeedbackStore _store;
    private readonly ILogger<PatternAnalyzer> _logger;

    public async Task<PatternAnalysisResult> AnalyzePatternsAsync(
        string ruleId,
        CancellationToken ct = default)
    {
        var feedback = await _store.GetFeedbackByRuleAsync(ruleId, ct);

        if (feedback.Count < 10)
        {
            return PatternAnalysisResult.InsufficientData(ruleId, feedback.Count);
        }

        // Group by original -> suggested mapping
        var mappings = feedback
            .Where(f => f.Decision != FeedbackDecision.Skipped)
            .GroupBy(f => NormalizeForPattern(f.OriginalText, f.SuggestedText))
            .ToList();

        var acceptedPatterns = new List<AcceptedPattern>();
        var rejectedPatterns = new List<RejectedPattern>();
        var modifications = new List<UserModificationExample>();

        foreach (var group in mappings)
        {
            var total = group.Count();
            var accepted = group.Count(f => f.Decision == FeedbackDecision.Accepted);
            var rejected = group.Count(f => f.Decision == FeedbackDecision.Rejected);
            var modified = group.Where(f => f.Decision == FeedbackDecision.Modified).ToList();

            var successRate = (double)(accepted + modified.Count) / total;

            if (successRate >= 0.7 && total >= 3)
            {
                acceptedPatterns.Add(new AcceptedPattern(
                    group.Key.Original,
                    group.Key.Suggested,
                    accepted + modified.Count,
                    successRate));
            }
            else if (successRate <= 0.3 && total >= 3)
            {
                var commonReason = ExtractCommonRejectionReason(group.Where(f => f.Decision == FeedbackDecision.Rejected));
                rejectedPatterns.Add(new RejectedPattern(
                    group.Key.Original,
                    group.Key.Suggested,
                    rejected,
                    commonReason));
            }

            // Extract useful modifications
            foreach (var mod in modified.Where(m => !string.IsNullOrEmpty(m.UserModification)))
            {
                var improvement = DescribeImprovement(mod.SuggestedText, mod.UserModification!);
                if (!string.IsNullOrEmpty(improvement))
                {
                    modifications.Add(new UserModificationExample(
                        mod.SuggestedText,
                        mod.UserModification!,
                        improvement));
                }
            }
        }

        // Calculate overall statistics
        var totalDecisions = feedback.Count(f => f.Decision != FeedbackDecision.Skipped);
        var totalAccepted = feedback.Count(f => f.Decision is FeedbackDecision.Accepted or FeedbackDecision.Modified);
        var acceptanceRate = totalDecisions > 0 ? (double)totalAccepted / totalDecisions : 0;

        return new PatternAnalysisResult
        {
            RuleId = ruleId,
            AcceptedPatterns = acceptedPatterns.OrderByDescending(p => p.AcceptCount).Take(5).ToList(),
            RejectedPatterns = rejectedPatterns.OrderByDescending(p => p.RejectCount).Take(5).ToList(),
            UsefulModifications = modifications.Take(3).ToList(),
            AcceptanceRate = acceptanceRate,
            SampleCount = feedback.Count,
            AnalyzedAt = DateTimeOffset.UtcNow
        };
    }

    private static (string Original, string Suggested) NormalizeForPattern(string original, string suggested)
    {
        // Normalize text for pattern matching
        // - Lowercase
        // - Remove extra whitespace
        // - Preserve structure but normalize content
        var normalizedOriginal = NormalizeText(original);
        var normalizedSuggested = NormalizeText(suggested);

        return (normalizedOriginal, normalizedSuggested);
    }

    private static string NormalizeText(string text)
    {
        return Regex.Replace(text.ToLowerInvariant().Trim(), @"\s+", " ");
    }

    private static string? ExtractCommonRejectionReason(IEnumerable<FixFeedback> rejections)
    {
        var comments = rejections
            .Where(r => !string.IsNullOrEmpty(r.UserComment))
            .Select(r => r.UserComment!)
            .ToList();

        if (comments.Count == 0)
            return null;

        // Simple: return most common comment
        return comments
            .GroupBy(c => c.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.First();
    }

    private static string? DescribeImprovement(string suggested, string modified)
    {
        // Analyze how the user improved the suggestion
        if (modified.Length < suggested.Length * 0.8)
            return "Made more concise";
        if (modified.Length > suggested.Length * 1.2)
            return "Added more detail";

        // Check for specific changes
        var suggestedWords = suggested.Split(' ');
        var modifiedWords = modified.Split(' ');
        var addedWords = modifiedWords.Except(suggestedWords).ToList();
        var removedWords = suggestedWords.Except(modifiedWords).ToList();

        if (addedWords.Count > 0 && removedWords.Count > 0)
            return $"Replaced '{string.Join(" ", removedWords.Take(2))}' with '{string.Join(" ", addedWords.Take(2))}'";

        return null;
    }
}
```

### 5.4 Prompt Enhancement Generation

```csharp
/// <summary>
/// Generates prompt enhancement text from learning data.
/// </summary>
public string GeneratePromptEnhancement(PatternAnalysisResult analysis)
{
    if (!analysis.HasSufficientData)
        return string.Empty;

    var sb = new StringBuilder();

    // Add accepted patterns guidance
    if (analysis.AcceptedPatterns.Count > 0)
    {
        sb.AppendLine("Based on user feedback, these fix patterns are PREFERRED:");
        foreach (var pattern in analysis.AcceptedPatterns.Take(3))
        {
            sb.AppendLine($"- When seeing '{TruncateForPrompt(pattern.OriginalPattern)}', suggest '{TruncateForPrompt(pattern.SuggestedPattern)}' (accepted {pattern.SuccessRate:P0} of the time)");
        }
        sb.AppendLine();
    }

    // Add rejected patterns guidance
    if (analysis.RejectedPatterns.Count > 0)
    {
        sb.AppendLine("AVOID these fix patterns that users have rejected:");
        foreach (var pattern in analysis.RejectedPatterns.Take(3))
        {
            var reason = !string.IsNullOrEmpty(pattern.CommonRejectionReason)
                ? $" Reason: {pattern.CommonRejectionReason}"
                : "";
            sb.AppendLine($"- Do NOT suggest '{TruncateForPrompt(pattern.SuggestedPattern)}' for '{TruncateForPrompt(pattern.OriginalPattern)}'.{reason}");
        }
        sb.AppendLine();
    }

    // Add modification insights
    if (analysis.UsefulModifications.Count > 0)
    {
        sb.AppendLine("Users often improve suggestions by:");
        foreach (var mod in analysis.UsefulModifications.Take(2))
        {
            sb.AppendLine($"- {mod.Improvement}");
        }
        sb.AppendLine();
    }

    // Add confidence calibration
    if (analysis.AcceptanceRate < 0.5)
    {
        sb.AppendLine("NOTE: This rule has a low acceptance rate. Be more conservative with suggestions.");
    }
    else if (analysis.AcceptanceRate > 0.9)
    {
        sb.AppendLine("NOTE: This rule has a high acceptance rate. You can be confident in standard suggestions.");
    }

    return sb.ToString().Trim();
}

private static string TruncateForPrompt(string text, int maxLength = 50)
{
    if (text.Length <= maxLength)
        return text;
    return text[..(maxLength - 3)] + "...";
}
```

---

## 6. Storage Implementation

### 6.1 SQLite Store

```csharp
namespace Lexichord.Modules.Agents.Storage;

/// <summary>
/// SQLite-based storage for learning data.
/// </summary>
public sealed class SqliteFeedbackStore : IFeedbackStore, IAsyncDisposable
{
    private readonly SQLiteAsyncConnection _db;
    private readonly ILogger<SqliteFeedbackStore> _logger;

    public SqliteFeedbackStore(
        IOptions<LearningStorageOptions> options,
        ILogger<SqliteFeedbackStore> logger)
    {
        _logger = logger;

        var dbPath = GetDatabasePath(options.Value);
        _db = new SQLiteAsyncConnection(dbPath);

        InitializeAsync().GetAwaiter().GetResult();
    }

    private static string GetDatabasePath(LearningStorageOptions options)
    {
        var basePath = options.StoragePath ?? GetDefaultStoragePath();
        Directory.CreateDirectory(basePath);
        return Path.Combine(basePath, "learning.db");
    }

    private static string GetDefaultStoragePath()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lexichord", "Learning");
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Lexichord", "Learning");
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "Lexichord", "Learning");
    }

    private async Task InitializeAsync()
    {
        await _db.CreateTableAsync<FeedbackRecord>();
        await _db.CreateTableAsync<PatternCacheRecord>();
        await _db.CreateTableAsync<SettingRecord>();

        _logger.LogDebug("Learning database initialized");
    }

    public async Task InsertFeedbackAsync(FixFeedback feedback, CancellationToken ct = default)
    {
        var record = new FeedbackRecord
        {
            Id = feedback.FeedbackId.ToString(),
            SuggestionId = feedback.SuggestionId.ToString(),
            DeviationId = feedback.DeviationId.ToString(),
            RuleId = feedback.RuleId,
            Category = feedback.Category,
            Decision = (int)feedback.Decision,
            OriginalText = feedback.OriginalText,
            SuggestedText = feedback.SuggestedText,
            FinalText = feedback.FinalText,
            UserModification = feedback.UserModification,
            OriginalConfidence = feedback.OriginalConfidence,
            Timestamp = feedback.Timestamp.ToString("O"),
            UserComment = feedback.UserComment,
            AnonymizedUserId = feedback.AnonymizedUserId,
            IsBulkOperation = feedback.IsBulkOperation ? 1 : 0
        };

        await _db.InsertAsync(record);
        _logger.LogDebug("Feedback recorded: {FeedbackId}", feedback.FeedbackId);
    }

    public async Task<IReadOnlyList<FixFeedback>> GetFeedbackByRuleAsync(
        string ruleId,
        CancellationToken ct = default)
    {
        var records = await _db.Table<FeedbackRecord>()
            .Where(r => r.RuleId == ruleId)
            .OrderByDescending(r => r.Timestamp)
            .Take(1000) // Limit for performance
            .ToListAsync();

        return records.Select(MapToFeedback).ToList();
    }

    public async Task<LearningStatistics> GetStatisticsAsync(
        LearningStatisticsFilter? filter = null,
        CancellationToken ct = default)
    {
        var query = _db.Table<FeedbackRecord>();

        if (filter?.Period?.Start != null)
            query = query.Where(r => r.Timestamp.CompareTo(filter.Period.Start.Value.ToString("O")) >= 0);
        if (filter?.Period?.End != null)
            query = query.Where(r => r.Timestamp.CompareTo(filter.Period.End.Value.ToString("O")) <= 0);
        if (filter?.ExcludeSkipped == true)
            query = query.Where(r => r.Decision != (int)FeedbackDecision.Skipped);
        if (filter?.ExcludeBulk == true)
            query = query.Where(r => r.IsBulkOperation == 0);

        var records = await query.ToListAsync();

        var total = records.Count;
        var accepted = records.Count(r => r.Decision == (int)FeedbackDecision.Accepted);
        var modified = records.Count(r => r.Decision == (int)FeedbackDecision.Modified);
        var rejected = records.Count(r => r.Decision == (int)FeedbackDecision.Rejected);
        var skipped = records.Count(r => r.Decision == (int)FeedbackDecision.Skipped);

        var nonSkipped = total - skipped;
        var acceptanceRate = nonSkipped > 0 ? (double)(accepted + modified) / nonSkipped : 0;
        var modificationRate = nonSkipped > 0 ? (double)modified / nonSkipped : 0;
        var skipRate = total > 0 ? (double)skipped / total : 0;

        // Calculate by rule
        var byRule = records
            .GroupBy(r => r.RuleId)
            .ToDictionary(
                g => g.Key,
                g => new RuleLearningStats(
                    g.Key,
                    g.Key, // TODO: Get rule name from repository
                    g.Count(),
                    CalculateAcceptanceRate(g),
                    CalculateModificationRate(g),
                    CalculateConfidenceAccuracy(g)));

        // Calculate by category
        var byCategory = records
            .GroupBy(r => r.Category)
            .ToDictionary(
                g => g.Key,
                g => new CategoryLearningStats(
                    g.Key,
                    g.Count(),
                    CalculateAcceptanceRate(g),
                    GetTopRulesByAcceptance(g, 3),
                    GetTopRulesByRejection(g, 3)));

        return new LearningStatistics
        {
            TotalFeedback = total,
            OverallAcceptanceRate = acceptanceRate,
            AcceptanceRateTrend = await CalculateTrendAsync(ct),
            RulesWithFeedback = byRule.Count,
            ByRule = byRule,
            ByCategory = byCategory,
            Period = filter?.Period ?? DateRange.AllTime,
            ModificationRate = modificationRate,
            SkipRate = skipRate
        };
    }

    private static double CalculateAcceptanceRate(IEnumerable<FeedbackRecord> records)
    {
        var list = records.ToList();
        var nonSkipped = list.Count(r => r.Decision != (int)FeedbackDecision.Skipped);
        if (nonSkipped == 0) return 0;
        var accepted = list.Count(r => r.Decision is (int)FeedbackDecision.Accepted or (int)FeedbackDecision.Modified);
        return (double)accepted / nonSkipped;
    }

    private static double CalculateModificationRate(IEnumerable<FeedbackRecord> records)
    {
        var list = records.ToList();
        var nonSkipped = list.Count(r => r.Decision != (int)FeedbackDecision.Skipped);
        if (nonSkipped == 0) return 0;
        var modified = list.Count(r => r.Decision == (int)FeedbackDecision.Modified);
        return (double)modified / nonSkipped;
    }

    private static double CalculateConfidenceAccuracy(IEnumerable<FeedbackRecord> records)
    {
        // Measure how well confidence predicted acceptance
        var list = records.Where(r => r.Decision != (int)FeedbackDecision.Skipped).ToList();
        if (list.Count == 0) return 0;

        var correctPredictions = list.Count(r =>
            (r.OriginalConfidence >= 0.8 && r.Decision is (int)FeedbackDecision.Accepted or (int)FeedbackDecision.Modified) ||
            (r.OriginalConfidence < 0.8 && r.Decision == (int)FeedbackDecision.Rejected));

        return (double)correctPredictions / list.Count;
    }

    private static IReadOnlyList<string> GetTopRulesByAcceptance(IEnumerable<FeedbackRecord> records, int count)
    {
        return records
            .GroupBy(r => r.RuleId)
            .OrderByDescending(g => CalculateAcceptanceRate(g))
            .Take(count)
            .Select(g => g.Key)
            .ToList();
    }

    private static IReadOnlyList<string> GetTopRulesByRejection(IEnumerable<FeedbackRecord> records, int count)
    {
        return records
            .GroupBy(r => r.RuleId)
            .OrderBy(g => CalculateAcceptanceRate(g))
            .Take(count)
            .Select(g => g.Key)
            .ToList();
    }

    private async Task<double> CalculateTrendAsync(CancellationToken ct)
    {
        // Compare last 7 days to previous 7 days
        var now = DateTimeOffset.UtcNow;
        var recent = await GetStatisticsForPeriodAsync(now.AddDays(-7), now, ct);
        var previous = await GetStatisticsForPeriodAsync(now.AddDays(-14), now.AddDays(-7), ct);

        if (previous.TotalFeedback == 0)
            return 0;

        return recent.OverallAcceptanceRate - previous.OverallAcceptanceRate;
    }

    private async Task<(double OverallAcceptanceRate, int TotalFeedback)> GetStatisticsForPeriodAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        var records = await _db.Table<FeedbackRecord>()
            .Where(r => r.Timestamp.CompareTo(start.ToString("O")) >= 0)
            .Where(r => r.Timestamp.CompareTo(end.ToString("O")) <= 0)
            .Where(r => r.Decision != (int)FeedbackDecision.Skipped)
            .ToListAsync();

        if (records.Count == 0)
            return (0, 0);

        var accepted = records.Count(r => r.Decision is (int)FeedbackDecision.Accepted or (int)FeedbackDecision.Modified);
        return ((double)accepted / records.Count, records.Count);
    }

    private static FixFeedback MapToFeedback(FeedbackRecord record) => new()
    {
        FeedbackId = Guid.Parse(record.Id),
        SuggestionId = Guid.Parse(record.SuggestionId),
        DeviationId = Guid.Parse(record.DeviationId),
        RuleId = record.RuleId,
        Category = record.Category,
        Decision = (FeedbackDecision)record.Decision,
        OriginalText = record.OriginalText ?? "",
        SuggestedText = record.SuggestedText ?? "",
        FinalText = record.FinalText,
        UserModification = record.UserModification,
        OriginalConfidence = record.OriginalConfidence,
        Timestamp = DateTimeOffset.Parse(record.Timestamp),
        UserComment = record.UserComment,
        AnonymizedUserId = record.AnonymizedUserId,
        IsBulkOperation = record.IsBulkOperation == 1
    };

    public async ValueTask DisposeAsync()
    {
        await _db.CloseAsync();
    }
}

// SQLite record types
[Table("feedback")]
internal class FeedbackRecord
{
    [PrimaryKey]
    public string Id { get; set; } = "";
    public string SuggestionId { get; set; } = "";
    public string DeviationId { get; set; } = "";
    public string RuleId { get; set; } = "";
    public string Category { get; set; } = "";
    public int Decision { get; set; }
    public string? OriginalText { get; set; }
    public string? SuggestedText { get; set; }
    public string? FinalText { get; set; }
    public string? UserModification { get; set; }
    public double OriginalConfidence { get; set; }
    public string Timestamp { get; set; } = "";
    public string? UserComment { get; set; }
    public string? AnonymizedUserId { get; set; }
    public int IsBulkOperation { get; set; }
}
```

---

## 7. Privacy Controls

### 7.1 Anonymization

```csharp
/// <summary>
/// Applies privacy settings to feedback before storage.
/// </summary>
private FixFeedback ApplyPrivacyOptions(FixFeedback feedback, LearningPrivacyOptions options)
{
    var result = feedback;

    // Anonymize user ID
    if (options.AnonymizeUsers && !string.IsNullOrEmpty(feedback.AnonymizedUserId))
    {
        result = result with
        {
            AnonymizedUserId = HashUserId(feedback.AnonymizedUserId)
        };
    }

    // Strip original text if not allowed
    if (!options.StoreOriginalText)
    {
        result = result with
        {
            OriginalText = ExtractPatternOnly(feedback.OriginalText),
            SuggestedText = ExtractPatternOnly(feedback.SuggestedText),
            FinalText = feedback.FinalText != null ? ExtractPatternOnly(feedback.FinalText) : null,
            UserModification = feedback.UserModification != null ? ExtractPatternOnly(feedback.UserModification) : null
        };
    }

    return result;
}

private static string HashUserId(string userId)
{
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
    return Convert.ToBase64String(hash)[..12];
}

private static string ExtractPatternOnly(string text)
{
    // Replace specific words with placeholders to preserve pattern structure
    // without storing actual content
    var pattern = Regex.Replace(text, @"\b\w{5,}\b", "[WORD]");
    return pattern.Length <= 100 ? pattern : pattern[..100];
}
```

### 7.2 Data Retention

```csharp
/// <summary>
/// Enforces data retention policies.
/// </summary>
public async Task EnforceRetentionPolicyAsync(CancellationToken ct = default)
{
    var options = GetPrivacyOptions();

    // Delete old data
    if (options.MaxDataAge.HasValue)
    {
        var cutoff = DateTimeOffset.UtcNow - options.MaxDataAge.Value;
        var deleted = await _store.DeleteOlderThanAsync(cutoff, ct);
        if (deleted > 0)
        {
            _logger.LogInformation("Deleted {Count} feedback records older than {Cutoff}", deleted, cutoff);
        }
    }

    // Enforce max records
    if (options.MaxRecords > 0)
    {
        var count = await _store.GetCountAsync(ct);
        if (count > options.MaxRecords)
        {
            var toDelete = count - options.MaxRecords;
            await _store.DeleteOldestAsync(toDelete, ct);
            _logger.LogInformation("Deleted {Count} oldest feedback records to enforce limit", toDelete);
        }
    }
}
```

---

## 8. Observability & Logging

| Level | Message Template | Context |
| :--- | :--- | :--- |
| Debug | `"Recording feedback: {Decision} for rule {RuleId}"` | Feedback recorded |
| Debug | `"Learning database initialized"` | Startup |
| Info | `"Learning context generated: {SampleCount} samples for rule {RuleId}"` | Context retrieval |
| Info | `"Pattern analysis completed: {AcceptedCount} accepted, {RejectedCount} rejected patterns"` | Analysis done |
| Info | `"Learning data exported: {PatternCount} patterns"` | Export |
| Info | `"Learning data imported: {PatternCount} patterns merged"` | Import |
| Info | `"Deleted {Count} feedback records older than {Cutoff}"` | Retention |
| Warning | `"Insufficient data for learning context: {SampleCount} samples"` | Low data |
| Error | `"Failed to record feedback: {Error}"` | Storage error |

---

## 9. Acceptance Criteria

### 9.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | User accepts suggestion | Recording feedback | Record stored with Accepted decision |
| 2 | User rejects suggestion | Recording feedback | Record stored with Rejected decision |
| 3 | User modifies suggestion | Recording feedback | Record stored with modification |
| 4 | 10+ feedback samples | Getting learning context | Context has patterns |
| 5 | < 10 feedback samples | Getting learning context | HasSufficientData = false |
| 6 | High acceptance rate | Getting learning context | Positive confidence adjustment |
| 7 | Low acceptance rate | Getting learning context | Warning in prompt enhancement |
| 8 | Export requested | Exporting data | Valid JSON file created |
| 9 | Import file provided | Importing data | Patterns merged |
| 10 | Writer Pro user | Accessing Learning Loop | License error thrown |

### 9.2 Privacy Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 11 | AnonymizeUsers = true | Recording feedback | User ID hashed |
| 12 | StoreOriginalText = false | Recording feedback | Text patterns only |
| 13 | MaxDataAge set | Retention check | Old data deleted |
| 14 | MaxRecords exceeded | Retention check | Oldest records deleted |

---

## 10. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `ILearningLoopService` interface | [ ] |
| 2 | `LearningLoopService` implementation | [ ] |
| 3 | `FixFeedback` record | [ ] |
| 4 | `LearningContext` record | [ ] |
| 5 | `LearningStatistics` record | [ ] |
| 6 | `LearningPrivacyOptions` record | [ ] |
| 7 | `SqliteFeedbackStore` implementation | [ ] |
| 8 | `PatternAnalyzer` implementation | [ ] |
| 9 | Prompt enhancement generation | [ ] |
| 10 | Export/import functionality | [ ] |
| 11 | Privacy controls | [ ] |
| 12 | Retention policy enforcement | [ ] |
| 13 | Unit tests | [ ] |
| 14 | DI registration | [ ] |

---

## 11. Verification Commands

```bash
# Run learning loop unit tests
dotnet test --filter "Version=v0.7.5d" --logger "console;verbosity=detailed"

# Run storage tests
dotnet test --filter "Category=Unit&FullyQualifiedName~FeedbackStore"

# Verify database location
ls -la ~/.config/Lexichord/Learning/  # Linux
ls -la ~/Library/Application\ Support/Lexichord/Learning/  # macOS

# Manual verification:
# 1. Accept several suggestions for a rule
# 2. Call GetLearningContextAsync for that rule
# 3. Verify patterns extracted
# 4. Export learning data
# 5. Clear learning data
# 6. Import exported data
# 7. Verify patterns restored
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
