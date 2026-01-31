# LCS-SBD-v0.14.4-STU: Scope Overview — Prompt Engineering Studio

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-SBD-v0.14.4-STU                                          |
| **Version**      | v0.14.4                                                      |
| **Codename**     | Prompt Engineering Studio (Agent Studio Phase 4)             |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-01-31                                                   |
| **Owner**        | Studio Architecture Lead                                     |
| **Depends On**   | v0.12.1-AGT (Agent Definition), v0.6.1a (LLM Integration), v0.2.1a (License Service) |

---

## 1. Executive Summary

### 1.1 The Vision

**v0.14.4-STU** delivers **Prompt Engineering Studio** — a dedicated environment for crafting, testing, and versioning prompts used by agents. This establishes:

- A prompt editor with syntax highlighting, variable autocomplete, and live preview
- A prompt testing framework for evaluating prompt performance with sample inputs
- A prompt versioning system for tracking changes and managing prompt evolution
- An A/B testing capability for comparing prompt variants in production
- A prompt analytics dashboard showing usage, quality scores, and efficiency metrics
- A prompt library UI for organizing, searching, and sharing prompts

This is essential for prompt quality—without structured prompt engineering, agents produce inconsistent results.

### 1.2 Business Value

- **Quality:** Structured prompt engineering improves agent output consistency.
- **Efficiency:** Testing identifies optimal prompts that use fewer tokens.
- **Iteration:** Version control enables safe experimentation without losing working prompts.
- **Optimization:** A/B testing provides data-driven prompt selection.
- **Collaboration:** Shared prompt library enables team-wide best practices.
- **Governance:** Analytics provide visibility into prompt performance across the system.

### 1.3 Success Criteria

1. Prompt editor provides real-time preview within 500ms of changes.
2. Prompt tests complete within 3 seconds per test run.
3. Version control tracks all prompt changes with diff visualization.
4. A/B tests reach statistical significance within configured sample sizes.
5. Analytics display accurate usage and quality metrics.
6. Prompt library supports search returning results in <100ms.
7. All prompt operations complete with <200ms UI latency.

---

## 2. Key Deliverables

### 2.1 Sub-Parts

| Sub-Part | Title | Description | Est. Hours |
|:---------|:------|:------------|:-----------|
| v0.14.4e | Prompt Editor | Rich editor with syntax highlighting, autocomplete, preview | 10 |
| v0.14.4f | Prompt Testing | Test framework with sample inputs and evaluation | 12 |
| v0.14.4g | Prompt Versioning | Version control with history, diff, and rollback | 8 |
| v0.14.4h | A/B Testing | Split testing with statistical analysis | 10 |
| v0.14.4i | Prompt Analytics | Usage metrics, quality scores, efficiency tracking | 6 |
| v0.14.4j | Prompt Library UI | Browse, search, organize, and share prompts | 4 |
| **Total** | | | **50 hours** |

### 2.2 Core Interfaces

```csharp
/// <summary>
/// Studio for engineering and managing prompts.
/// Provides editing, testing, versioning, and analytics for prompts.
/// </summary>
public interface IPromptStudio
{
    /// <summary>
    /// Create a new prompt.
    /// </summary>
    Task<Prompt> CreatePromptAsync(
        CreatePromptRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Update an existing prompt (creates new draft version).
    /// </summary>
    Task<Prompt> UpdatePromptAsync(
        PromptId promptId,
        UpdatePromptRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get a prompt by ID, optionally at a specific version.
    /// </summary>
    Task<Prompt?> GetPromptAsync(
        PromptId promptId,
        PromptVersion? version = null,
        CancellationToken ct = default);

    /// <summary>
    /// Search prompts by name, tags, or content.
    /// </summary>
    Task<IReadOnlyList<Prompt>> SearchPromptsAsync(
        PromptSearchQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a prompt.
    /// </summary>
    Task DeletePromptAsync(
        PromptId promptId,
        CancellationToken ct = default);

    /// <summary>
    /// Test a prompt with sample inputs.
    /// </summary>
    Task<PromptTestResult> TestAsync(
        PromptId promptId,
        PromptTestRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Publish a prompt version for production use.
    /// </summary>
    Task<PromptVersion> PublishVersionAsync(
        PromptId promptId,
        string versionName,
        string? releaseNotes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get version history for a prompt.
    /// </summary>
    Task<IReadOnlyList<PromptVersionInfo>> GetVersionsAsync(
        PromptId promptId,
        CancellationToken ct = default);

    /// <summary>
    /// Compare two versions of a prompt.
    /// </summary>
    Task<PromptDiff> CompareVersionsAsync(
        PromptId promptId,
        PromptVersion versionA,
        PromptVersion versionB,
        CancellationToken ct = default);

    /// <summary>
    /// Rollback to a previous version.
    /// </summary>
    Task<Prompt> RollbackAsync(
        PromptId promptId,
        PromptVersion targetVersion,
        CancellationToken ct = default);
}

/// <summary>
/// Strongly-typed identifier for a prompt.
/// </summary>
public readonly record struct PromptId(Guid Value)
{
    public static PromptId New() => new(Guid.NewGuid());
    public static PromptId Parse(string s) => new(Guid.Parse(s));
    public override string ToString() => $"prompt:{Value:N}";
}

/// <summary>
/// Prompt version identifier.
/// </summary>
public readonly record struct PromptVersion(int Major, int Minor, int Patch)
{
    public static PromptVersion Parse(string s)
    {
        var parts = s.Split('.');
        return new PromptVersion(
            int.Parse(parts[0]),
            int.Parse(parts[1]),
            int.Parse(parts[2]));
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    public PromptVersion IncrementPatch() => new(Major, Minor, Patch + 1);
    public PromptVersion IncrementMinor() => new(Major, Minor + 1, 0);
    public PromptVersion IncrementMajor() => new(Major + 1, 0, 0);
}

/// <summary>
/// A managed prompt with templates and variables.
/// </summary>
public record Prompt
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public PromptId Id { get; init; }

    /// <summary>
    /// Prompt name (unique within namespace).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The prompt template with variable placeholders.
    /// </summary>
    public required string Template { get; init; }

    /// <summary>
    /// System prompt (optional).
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Variables that can be substituted in the template.
    /// </summary>
    public IReadOnlyList<PromptVariable> Variables { get; init; } = [];

    /// <summary>
    /// Prompt metadata.
    /// </summary>
    public PromptMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Current published version.
    /// </summary>
    public PromptVersion CurrentVersion { get; init; }

    /// <summary>
    /// Whether there are unpublished changes.
    /// </summary>
    public bool HasDraft { get; init; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Recommended model for this prompt.
    /// </summary>
    public string? RecommendedModel { get; init; }

    /// <summary>
    /// Recommended temperature setting.
    /// </summary>
    public float? RecommendedTemperature { get; init; }

    /// <summary>
    /// Maximum tokens for response.
    /// </summary>
    public int? MaxTokens { get; init; }
}

/// <summary>
/// A variable in a prompt template.
/// </summary>
public record PromptVariable
{
    /// <summary>
    /// Variable name (used in template as {{name}}).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Variable data type.
    /// </summary>
    public PromptVariableType Type { get; init; } = PromptVariableType.String;

    /// <summary>
    /// Whether this variable is required.
    /// </summary>
    public bool Required { get; init; } = true;

    /// <summary>
    /// Default value if not provided.
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Validation regex pattern.
    /// </summary>
    public string? ValidationPattern { get; init; }

    /// <summary>
    /// Example value for documentation.
    /// </summary>
    public string? Example { get; init; }
}

/// <summary>
/// Types of prompt variables.
/// </summary>
public enum PromptVariableType
{
    String, Number, Boolean, List, Object, Document, Entity, Date, Enum
}

/// <summary>
/// Metadata about a prompt.
/// </summary>
public record PromptMetadata
{
    /// <summary>
    /// Category for organization.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Author/creator.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// When the prompt was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the prompt was last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; init; }

    /// <summary>
    /// Number of times this prompt has been used.
    /// </summary>
    public int UsageCount { get; init; }

    /// <summary>
    /// Average quality score (0.0-1.0).
    /// </summary>
    public float? AverageQualityScore { get; init; }

    /// <summary>
    /// Average input tokens.
    /// </summary>
    public int? AverageInputTokens { get; init; }

    /// <summary>
    /// Average output tokens.
    /// </summary>
    public int? AverageOutputTokens { get; init; }
}

/// <summary>
/// Request to create a new prompt.
/// </summary>
public record CreatePromptRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Template { get; init; }
    public string? SystemPrompt { get; init; }
    public IReadOnlyList<PromptVariable>? Variables { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? RecommendedModel { get; init; }
    public float? RecommendedTemperature { get; init; }
    public int? MaxTokens { get; init; }
}

/// <summary>
/// Request to update an existing prompt.
/// </summary>
public record UpdatePromptRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Template { get; init; }
    public string? SystemPrompt { get; init; }
    public IReadOnlyList<PromptVariable>? Variables { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? RecommendedModel { get; init; }
    public float? RecommendedTemperature { get; init; }
    public int? MaxTokens { get; init; }
}

/// <summary>
/// Search query for prompts.
/// </summary>
public record PromptSearchQuery
{
    /// <summary>
    /// Text to search in name and description.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Filter by category.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Filter by tags.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Filter by author.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Include only prompts with quality score above this.
    /// </summary>
    public float? MinQualityScore { get; init; }

    /// <summary>
    /// Sort order.
    /// </summary>
    public PromptSortOrder SortBy { get; init; } = PromptSortOrder.Name;

    /// <summary>
    /// Maximum results.
    /// </summary>
    public int Limit { get; init; } = 50;

    /// <summary>
    /// Skip for pagination.
    /// </summary>
    public int Offset { get; init; }
}

/// <summary>
/// Sort order for prompt search.
/// </summary>
public enum PromptSortOrder
{
    Name, CreatedAt, ModifiedAt, UsageCount, QualityScore
}

/// <summary>
/// Prompt test request.
/// </summary>
public record PromptTestRequest
{
    /// <summary>
    /// Variable values to substitute.
    /// </summary>
    public IReadOnlyDictionary<string, object> Variables { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Model to use for testing (null = use recommended).
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Number of test runs.
    /// </summary>
    public int Runs { get; init; } = 1;

    /// <summary>
    /// Test options.
    /// </summary>
    public PromptTestOptions Options { get; init; } = new();
}

/// <summary>
/// Options for prompt testing.
/// </summary>
public record PromptTestOptions
{
    /// <summary>
    /// Include token counts in results.
    /// </summary>
    public bool IncludeTokenCounts { get; init; } = true;

    /// <summary>
    /// Include latency measurements.
    /// </summary>
    public bool IncludeLatency { get; init; } = true;

    /// <summary>
    /// Include raw LLM response.
    /// </summary>
    public bool IncludeRawResponse { get; init; }

    /// <summary>
    /// Temperature override.
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// Max tokens override.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Expected output for validation.
    /// </summary>
    public string? ExpectedOutput { get; init; }

    /// <summary>
    /// Validation criteria.
    /// </summary>
    public IReadOnlyList<ValidationCriterion>? ValidationCriteria { get; init; }
}

/// <summary>
/// Validation criterion for prompt testing.
/// </summary>
public record ValidationCriterion
{
    /// <summary>
    /// Criterion name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Criterion type.
    /// </summary>
    public required ValidationCriterionType Type { get; init; }

    /// <summary>
    /// Expected value or pattern.
    /// </summary>
    public required string Expected { get; init; }
}

/// <summary>
/// Types of validation criteria.
/// </summary>
public enum ValidationCriterionType
{
    Contains, NotContains, Matches, StartsWith, EndsWith, JsonSchema, MinLength, MaxLength
}

/// <summary>
/// Result of testing a prompt.
/// </summary>
public record PromptTestResult
{
    /// <summary>
    /// Prompt that was tested.
    /// </summary>
    public PromptId PromptId { get; init; }

    /// <summary>
    /// Version that was tested.
    /// </summary>
    public PromptVersion Version { get; init; }

    /// <summary>
    /// Individual test runs.
    /// </summary>
    public IReadOnlyList<PromptTestRun> Runs { get; init; } = [];

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public PromptTestSummary Summary { get; init; } = new();

    /// <summary>
    /// When the test was run.
    /// </summary>
    public DateTimeOffset TestedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A single test run.
/// </summary>
public record PromptTestRun
{
    /// <summary>
    /// Run number.
    /// </summary>
    public int RunNumber { get; init; }

    /// <summary>
    /// The rendered prompt that was sent.
    /// </summary>
    public string RenderedPrompt { get; init; } = "";

    /// <summary>
    /// The response from the LLM.
    /// </summary>
    public string Response { get; init; } = "";

    /// <summary>
    /// Input tokens used.
    /// </summary>
    public int InputTokens { get; init; }

    /// <summary>
    /// Output tokens used.
    /// </summary>
    public int OutputTokens { get; init; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// Latency of the request.
    /// </summary>
    public TimeSpan Latency { get; init; }

    /// <summary>
    /// Error message if the run failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Whether the run was successful.
    /// </summary>
    public bool Success => Error == null;

    /// <summary>
    /// Validation results.
    /// </summary>
    public IReadOnlyList<ValidationResult>? ValidationResults { get; init; }
}

/// <summary>
/// Result of a validation criterion.
/// </summary>
public record ValidationResult
{
    public required string CriterionName { get; init; }
    public bool Passed { get; init; }
    public string? Details { get; init; }
}

/// <summary>
/// Summary of test results.
/// </summary>
public record PromptTestSummary
{
    /// <summary>
    /// Total number of runs.
    /// </summary>
    public int TotalRuns { get; init; }

    /// <summary>
    /// Number of successful runs.
    /// </summary>
    public int SuccessfulRuns { get; init; }

    /// <summary>
    /// Success rate (0.0-1.0).
    /// </summary>
    public float SuccessRate => TotalRuns > 0 ? (float)SuccessfulRuns / TotalRuns : 0;

    /// <summary>
    /// Average input tokens.
    /// </summary>
    public int AverageInputTokens { get; init; }

    /// <summary>
    /// Average output tokens.
    /// </summary>
    public int AverageOutputTokens { get; init; }

    /// <summary>
    /// Average total tokens.
    /// </summary>
    public int AverageTotalTokens => AverageInputTokens + AverageOutputTokens;

    /// <summary>
    /// Average latency.
    /// </summary>
    public TimeSpan AverageLatency { get; init; }

    /// <summary>
    /// Validation pass rate.
    /// </summary>
    public float? ValidationPassRate { get; init; }
}

/// <summary>
/// Information about a prompt version.
/// </summary>
public record PromptVersionInfo
{
    /// <summary>
    /// Version number.
    /// </summary>
    public PromptVersion Version { get; init; }

    /// <summary>
    /// Version name/label.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Release notes.
    /// </summary>
    public string? ReleaseNotes { get; init; }

    /// <summary>
    /// Who published this version.
    /// </summary>
    public string? PublishedBy { get; init; }

    /// <summary>
    /// When this version was published.
    /// </summary>
    public DateTimeOffset PublishedAt { get; init; }

    /// <summary>
    /// Whether this is the current version.
    /// </summary>
    public bool IsCurrent { get; init; }
}

/// <summary>
/// Diff between two prompt versions.
/// </summary>
public record PromptDiff
{
    /// <summary>
    /// Version A (before).
    /// </summary>
    public PromptVersion VersionA { get; init; }

    /// <summary>
    /// Version B (after).
    /// </summary>
    public PromptVersion VersionB { get; init; }

    /// <summary>
    /// Template diff.
    /// </summary>
    public TextDiff TemplateDiff { get; init; } = new();

    /// <summary>
    /// System prompt diff.
    /// </summary>
    public TextDiff? SystemPromptDiff { get; init; }

    /// <summary>
    /// Variables added.
    /// </summary>
    public IReadOnlyList<PromptVariable> VariablesAdded { get; init; } = [];

    /// <summary>
    /// Variables removed.
    /// </summary>
    public IReadOnlyList<PromptVariable> VariablesRemoved { get; init; } = [];

    /// <summary>
    /// Variables modified.
    /// </summary>
    public IReadOnlyList<VariableChange> VariablesModified { get; init; } = [];
}

/// <summary>
/// Text diff representation.
/// </summary>
public record TextDiff
{
    /// <summary>
    /// Diff hunks.
    /// </summary>
    public IReadOnlyList<DiffHunk> Hunks { get; init; } = [];
}

/// <summary>
/// A hunk in a text diff.
/// </summary>
public record DiffHunk
{
    public int OldStart { get; init; }
    public int OldCount { get; init; }
    public int NewStart { get; init; }
    public int NewCount { get; init; }
    public IReadOnlyList<DiffLine> Lines { get; init; } = [];
}

/// <summary>
/// A line in a diff.
/// </summary>
public record DiffLine
{
    public DiffLineType Type { get; init; }
    public string Content { get; init; } = "";
}

/// <summary>
/// Type of diff line.
/// </summary>
public enum DiffLineType
{
    Context, Added, Removed
}

/// <summary>
/// Change to a variable between versions.
/// </summary>
public record VariableChange
{
    public required string VariableName { get; init; }
    public PromptVariable Before { get; init; } = null!;
    public PromptVariable After { get; init; } = null!;
}

/// <summary>
/// A/B testing for prompts.
/// </summary>
public interface IPromptABTesting
{
    /// <summary>
    /// Create a new A/B test.
    /// </summary>
    Task<ABTest> CreateTestAsync(
        ABTestDefinition definition,
        CancellationToken ct = default);

    /// <summary>
    /// Start an A/B test.
    /// </summary>
    Task<ABTest> StartTestAsync(
        ABTestId testId,
        CancellationToken ct = default);

    /// <summary>
    /// Stop an A/B test.
    /// </summary>
    Task<ABTest> StopTestAsync(
        ABTestId testId,
        CancellationToken ct = default);

    /// <summary>
    /// Get current results of an A/B test.
    /// </summary>
    Task<ABTestResults> GetResultsAsync(
        ABTestId testId,
        CancellationToken ct = default);

    /// <summary>
    /// Select a winner and apply it.
    /// </summary>
    Task<PromptId> SelectWinnerAsync(
        ABTestId testId,
        PromptId winnerId,
        CancellationToken ct = default);

    /// <summary>
    /// Get all A/B tests.
    /// </summary>
    Task<IReadOnlyList<ABTest>> GetTestsAsync(
        ABTestStatus? status = null,
        CancellationToken ct = default);
}

/// <summary>
/// Strongly-typed identifier for an A/B test.
/// </summary>
public readonly record struct ABTestId(Guid Value)
{
    public static ABTestId New() => new(Guid.NewGuid());
}

/// <summary>
/// Definition of an A/B test.
/// </summary>
public record ABTestDefinition
{
    /// <summary>
    /// Test name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Test description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Control prompt ID (baseline).
    /// </summary>
    public required PromptId ControlPromptId { get; init; }

    /// <summary>
    /// Variant prompt IDs to test against control.
    /// </summary>
    public required IReadOnlyList<PromptId> VariantPromptIds { get; init; }

    /// <summary>
    /// Primary metric to optimize.
    /// </summary>
    public required ABTestMetric PrimaryMetric { get; init; }

    /// <summary>
    /// Target sample size for statistical significance.
    /// </summary>
    public int TargetSampleSize { get; init; } = 100;

    /// <summary>
    /// Traffic split (fraction going to variants).
    /// </summary>
    public float TrafficSplit { get; init; } = 0.5f;

    /// <summary>
    /// Maximum test duration.
    /// </summary>
    public TimeSpan? MaxDuration { get; init; }

    /// <summary>
    /// Confidence level for significance (e.g., 0.95).
    /// </summary>
    public float ConfidenceLevel { get; init; } = 0.95f;
}

/// <summary>
/// Metrics for A/B testing.
/// </summary>
public enum ABTestMetric
{
    QualityScore, TokenEfficiency, Latency, SuccessRate, UserSatisfaction
}

/// <summary>
/// An A/B test.
/// </summary>
public record ABTest
{
    public ABTestId Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public PromptId ControlPromptId { get; init; }
    public IReadOnlyList<PromptId> VariantPromptIds { get; init; } = [];
    public ABTestMetric PrimaryMetric { get; init; }
    public ABTestStatus Status { get; init; }
    public int TargetSampleSize { get; init; }
    public int CurrentSampleSize { get; init; }
    public float TrafficSplit { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public PromptId? WinnerId { get; init; }
}

/// <summary>
/// Status of an A/B test.
/// </summary>
public enum ABTestStatus
{
    Draft, Running, Paused, Completed, Cancelled
}

/// <summary>
/// Results of an A/B test.
/// </summary>
public record ABTestResults
{
    /// <summary>
    /// Test identifier.
    /// </summary>
    public ABTestId TestId { get; init; }

    /// <summary>
    /// Results per variant.
    /// </summary>
    public IReadOnlyList<VariantResult> Results { get; init; } = [];

    /// <summary>
    /// Statistically significant winner (if any).
    /// </summary>
    public PromptId? StatisticalWinner { get; init; }

    /// <summary>
    /// Confidence level achieved.
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Whether the test has reached statistical significance.
    /// </summary>
    public bool IsSignificant { get; init; }
}

/// <summary>
/// Result for a variant in an A/B test.
/// </summary>
public record VariantResult
{
    /// <summary>
    /// Prompt ID.
    /// </summary>
    public PromptId PromptId { get; init; }

    /// <summary>
    /// Whether this is the control.
    /// </summary>
    public bool IsControl { get; init; }

    /// <summary>
    /// Sample size for this variant.
    /// </summary>
    public int SampleSize { get; init; }

    /// <summary>
    /// Average metric value.
    /// </summary>
    public float AverageMetricValue { get; init; }

    /// <summary>
    /// Standard deviation.
    /// </summary>
    public float StandardDeviation { get; init; }

    /// <summary>
    /// Whether this variant is the winner.
    /// </summary>
    public bool IsWinner { get; init; }

    /// <summary>
    /// Improvement over control (percentage).
    /// </summary>
    public float? ImprovementOverControl { get; init; }

    /// <summary>
    /// P-value for this variant vs control.
    /// </summary>
    public float? PValue { get; init; }
}
```

---

## 3. Architecture

### 3.1 Component Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Prompt Engineering Studio                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        IPromptStudio                                 │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │   │
│  │  │   Create/    │  │    Test      │  │   Version    │              │   │
│  │  │   Update     │  │   Prompts    │  │   Control    │              │   │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │   │
│  │         │                 │                 │                       │   │
│  │         ▼                 ▼                 ▼                       │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Prompt Store                              │   │   │
│  │  │  ┌───────────┐  ┌───────────┐  ┌───────────┐                │   │   │
│  │  │  │ Prompt 1  │  │ Prompt 2  │  │ Prompt N  │                │   │   │
│  │  │  │ Versions  │  │ Versions  │  │ Versions  │                │   │   │
│  │  │  │ Tests     │  │ Tests     │  │ Tests     │                │   │   │
│  │  │  └───────────┘  └───────────┘  └───────────┘                │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                     │                                       │
│              ┌──────────────────────┼──────────────────────┐               │
│              ▼                      ▼                      ▼               │
│  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────────┐  │
│  │  Prompt Editor    │  │ IPromptABTesting  │  │  Prompt Analytics     │  │
│  │                   │  │                   │  │                       │  │
│  │ • Syntax highlight│  │ • Create tests    │  │ • Usage tracking      │  │
│  │ • Autocomplete    │  │ • Traffic split   │  │ • Quality scores      │  │
│  │ • Live preview    │  │ • Statistics      │  │ • Token efficiency    │  │
│  └───────────────────┘  └───────────────────┘  └───────────────────────┘  │
│              │                      │                      │               │
│              └──────────────────────┼──────────────────────┘               │
│                                     ▼                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        LLM Integration                               │   │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │   │
│  │  │   Template      │  │  Test Runner    │  │   Token Counter     │  │   │
│  │  │   Renderer      │  │                 │  │                     │  │   │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      Prompt Library UI                               │   │
│  │  ┌──────────┐  ┌─────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐ │   │
│  │  │ Editor   │  │ Tester  │  │ Versions │  │ A/B Test │  │Analytics│ │   │
│  │  │  Panel   │  │  Panel  │  │  Panel   │  │  Panel   │  │  Panel │ │   │
│  │  └──────────┘  └─────────┘  └──────────┘  └──────────┘  └────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Data Model

### 4.1 Database Schema

```sql
-- Prompts
CREATE TABLE prompts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    tags JSONB,
    recommended_model VARCHAR(100),
    recommended_temperature REAL,
    max_tokens INT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(owner_id, name),
    INDEX idx_prompts_owner ON prompts(owner_id),
    INDEX idx_prompts_category ON prompts(category),
    INDEX idx_prompts_tags ON prompts USING gin(tags)
);

-- Prompt versions
CREATE TABLE prompt_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    prompt_id UUID NOT NULL REFERENCES prompts(id) ON DELETE CASCADE,
    version_major INT NOT NULL,
    version_minor INT NOT NULL,
    version_patch INT NOT NULL,
    version_name VARCHAR(100),
    template TEXT NOT NULL,
    system_prompt TEXT,
    variables_json JSONB NOT NULL DEFAULT '[]',
    release_notes TEXT,
    published_by VARCHAR(200),
    published_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_current BOOLEAN NOT NULL DEFAULT FALSE,

    UNIQUE(prompt_id, version_major, version_minor, version_patch),
    INDEX idx_prompt_versions_prompt ON prompt_versions(prompt_id),
    INDEX idx_prompt_versions_current ON prompt_versions(prompt_id, is_current) WHERE is_current = TRUE
);

-- Prompt drafts (unpublished changes)
CREATE TABLE prompt_drafts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    prompt_id UUID NOT NULL UNIQUE REFERENCES prompts(id) ON DELETE CASCADE,
    template TEXT NOT NULL,
    system_prompt TEXT,
    variables_json JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Prompt test results
CREATE TABLE prompt_test_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    prompt_id UUID NOT NULL REFERENCES prompts(id) ON DELETE CASCADE,
    version_major INT NOT NULL,
    version_minor INT NOT NULL,
    version_patch INT NOT NULL,
    variables_json JSONB NOT NULL,
    model VARCHAR(100),
    runs_json JSONB NOT NULL,
    summary_json JSONB NOT NULL,
    tested_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_prompt_tests_prompt ON prompt_test_results(prompt_id, tested_at DESC)
);

-- Prompt usage analytics
CREATE TABLE prompt_usage (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    prompt_id UUID NOT NULL REFERENCES prompts(id) ON DELETE CASCADE,
    version_major INT NOT NULL,
    version_minor INT NOT NULL,
    version_patch INT NOT NULL,
    used_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    input_tokens INT NOT NULL,
    output_tokens INT NOT NULL,
    latency_ms INT NOT NULL,
    quality_score REAL,
    agent_id UUID,
    execution_id UUID,

    INDEX idx_prompt_usage_prompt ON prompt_usage(prompt_id, used_at DESC),
    INDEX idx_prompt_usage_time ON prompt_usage(used_at DESC)
);

-- A/B tests
CREATE TABLE prompt_ab_tests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    control_prompt_id UUID NOT NULL REFERENCES prompts(id),
    variant_prompt_ids JSONB NOT NULL,
    primary_metric VARCHAR(50) NOT NULL,
    target_sample_size INT NOT NULL DEFAULT 100,
    traffic_split REAL NOT NULL DEFAULT 0.5,
    confidence_level REAL NOT NULL DEFAULT 0.95,
    max_duration_ms BIGINT,
    status VARCHAR(20) NOT NULL DEFAULT 'Draft',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    started_at TIMESTAMPTZ,
    ended_at TIMESTAMPTZ,
    winner_prompt_id UUID REFERENCES prompts(id),

    INDEX idx_ab_tests_owner ON prompt_ab_tests(owner_id),
    INDEX idx_ab_tests_status ON prompt_ab_tests(status) WHERE status = 'Running'
);

-- A/B test samples
CREATE TABLE prompt_ab_samples (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    test_id UUID NOT NULL REFERENCES prompt_ab_tests(id) ON DELETE CASCADE,
    prompt_id UUID NOT NULL REFERENCES prompts(id),
    metric_value REAL NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_ab_samples_test ON prompt_ab_samples(test_id, prompt_id)
);
```

---

## 5. Prompt Editor Features

| Feature | Description |
|:--------|:------------|
| Syntax Highlighting | Variables highlighted: `{{variable}}` |
| Variable Autocomplete | Suggest variables as you type `{{` |
| Live Preview | Real-time rendered preview with sample values |
| Token Counter | Estimate input tokens before testing |
| Error Detection | Highlight undefined variables, syntax errors |
| Diff View | Compare versions side-by-side |
| Import/Export | YAML/JSON format support |
| Templates | Start from pre-built templates |

---

## 6. Prompt Library UI

```
┌────────────────────────────────────────────────────────────────────────────┐
│ Prompt Engineering Studio                              [+ New Prompt]       │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─ Library ─────────────────────────────────────────────────────────────┐  │
│ │ Search: [________________________________] [🔍] Filter: [All ▼]        │  │
│ │                                                                        │  │
│ │ ▼ Research                                                             │  │
│ │   ├─ 📝 competitor-analysis          v2.1.0   ★ 4.5  Used: 1,234      │  │
│ │   ├─ 📝 market-research              v1.3.2   ★ 4.2  Used: 892        │  │
│ │   └─ 📝 product-research             v1.0.1   ★ 4.8  Used: 456        │  │
│ │                                                                        │  │
│ │ ▼ Writing                                                              │  │
│ │   ├─ 📝 blog-post-generator          v3.0.0   ★ 4.6  Used: 2,341      │  │
│ │   ├─ 📝 email-composer               v2.2.1   ★ 4.3  Used: 1,567      │  │
│ │   └─ 📝 report-writer                v1.5.0   ★ 4.7  Used: 789        │  │
│ │                                                                        │  │
│ └────────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ ┌─ Editor: "competitor-analysis" v2.1.0 ────────────────────────────────┐  │
│ │                                                                        │  │
│ │ System Prompt:                                                         │  │
│ │ ┌──────────────────────────────────────────────────────────────────┐  │  │
│ │ │ You are a market research analyst specializing in competitive     │  │  │
│ │ │ intelligence. Your analysis should be data-driven and objective. │  │  │
│ │ └──────────────────────────────────────────────────────────────────┘  │  │
│ │                                                                        │  │
│ │ Template:                                                              │  │
│ │ ┌──────────────────────────────────────────────────────────────────┐  │  │
│ │ │ Analyze the following competitor: {{competitor_name}}             │  │  │
│ │ │                                                                   │  │  │
│ │ │ Focus areas:                                                      │  │  │
│ │ │ {{#each focus_areas}}                                             │  │  │
│ │ │ - {{this}}                                                        │  │  │
│ │ │ {{/each}}                                                         │  │  │
│ │ │                                                                   │  │  │
│ │ │ Include market positioning, strengths, weaknesses, and           │  │  │
│ │ │ opportunities for {{our_company}}.                               │  │  │
│ │ └──────────────────────────────────────────────────────────────────┘  │  │
│ │                                                                        │  │
│ │ Variables:                              Est. Tokens: ~850              │  │
│ │ ├─ competitor_name (String) *required                                 │  │
│ │ ├─ focus_areas (List) *required                                       │  │
│ │ └─ our_company (String) default: "Acme Corp"                          │  │
│ │                                                                        │  │
│ │ [Save Draft] [Test] [Publish v2.2.0] [History] [A/B Test]             │  │
│ └────────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ ┌─ Test Results ─────────────────────────────────────────────────────────┐ │
│ │ Last Test: 5 min ago │ 3 runs │ 100% success │ Avg: 1,245 tokens      │ │
│ │                                                                        │ │
│ │ Run 1: ✓ 1,234 tokens │ 2.1s │ Quality: 4.5/5                         │ │
│ │ Run 2: ✓ 1,256 tokens │ 2.3s │ Quality: 4.6/5                         │ │
│ │ Run 3: ✓ 1,245 tokens │ 2.0s │ Quality: 4.4/5                         │ │
│ │                                                                        │ │
│ │ [Run New Test] [Export Results]                                       │ │
│ └────────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Dependencies

| Component | Source | Usage |
|:----------|:-------|:------|
| `IChatCompletionService` | v0.6.1a | Prompt testing execution |
| `IAgentRegistry` | v0.12.1-AGT | Agent prompt integration |
| `ILicenseService` | v0.2.1a | Feature gating |
| `IMediator` | v0.0.7a | Prompt events |
| `ISettingsService` | v0.1.6a | User preferences |

---

## 8. License Gating

| Tier | Features |
|:-----|:---------|
| **Core** | Basic editing; 5 prompts max |
| **WriterPro** | + Testing; versioning; 20 prompts |
| **Teams** | + A/B testing; analytics; shared library; unlimited prompts |
| **Enterprise** | + API access; custom validation; export |

---

## 9. Performance Targets

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Live preview render | <500ms | P95 from keystroke |
| Prompt test (single) | <3s | P95 including LLM call |
| Version history load | <200ms | P95 for 50 versions |
| Search | <100ms | P95 for 1000 prompts |
| Publish version | <500ms | P95 timing |
| A/B test assignment | <10ms | P95 timing |

---

## 10. Testing Strategy

### 10.1 Unit Tests

- Template variable extraction
- Variable substitution
- Diff generation
- Statistical significance calculation

### 10.2 Integration Tests

- Full prompt lifecycle (create, edit, test, publish)
- A/B test traffic routing
- Analytics aggregation
- Version rollback

### 10.3 Performance Tests

- 1000+ prompts in library
- 100+ test runs per prompt
- A/B test with 10k samples

---

## 11. Risks & Mitigations

| Risk | Impact | Mitigation |
|:-----|:-------|:-----------|
| Test cost (LLM calls) | High costs | Test run limits; caching; mock mode |
| A/B test pollution | Invalid results | Proper randomization; user segmentation |
| Version sprawl | Confusion | Auto-cleanup; archive old versions |
| Prompt injection | Security | Input sanitization; variable escaping |

---

## 12. MediatR Events

| Event | Description |
|:------|:------------|
| `PromptCreatedEvent` | New prompt created |
| `PromptUpdatedEvent` | Prompt modified |
| `PromptPublishedEvent` | Version published |
| `PromptTestedEvent` | Test completed |
| `ABTestStartedEvent` | A/B test started |
| `ABTestCompletedEvent` | A/B test ended |
| `PromptUsedEvent` | Prompt used in execution |

---

**Document End**
