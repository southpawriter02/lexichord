# LCS-DES-v0.10.3-KG-d: Design Specification — Resolution Learning

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-v0.10.3d` | Entity Resolution sub-part d |
| **Feature Name** | `Resolution Learning` | Learn from disambiguation choices |
| **Target Version** | `v0.10.3d` | Fourth sub-part of v0.10.3-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Canonical Knowledge & Versioned Store module |
| **Swimlane** | `Entity Resolution` | Knowledge graph vertical |
| **License Tier** | `Enterprise` | Available in Enterprise tier only |
| **Feature Gate Key** | `FeatureFlags.CKVS.EntityResolution` | Entity resolution feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.3-KG](./LCS-SBD-v0.10.3-KG.md) | Entity Resolution scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.3-KG S2.1](./LCS-SBD-v0.10.3-KG.md#21-sub-parts) | d = Resolution Learning |

---

## 2. Executive Summary

### 2.1 The Requirement

Learn from user disambiguation choices to improve future entity matching and resolution:

1. Receive disambiguation choice events from Part A
2. Extract patterns from user selections
3. Update confidence scores for similar mentions
4. Provide admin feedback on pattern quality
5. Improve entity linker calibration over time

### 2.2 The Proposed Solution

Implement `IResolutionLearningService` with:

1. **RecordFeedbackAsync:** Store disambiguation feedback
2. **ExtractPatternsAsync:** Learn patterns from choices
3. **UpdateModelAsync:** Update linker confidence adjustments
4. **GetLearningStatsAsync:** Report on learning progress
5. Integration with disambiguation service (Part A)

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IDisambiguationService` | v0.10.3a | Receives disambiguation choice events |
| `IEntityLinkingService` | v0.5.5-KG | Updates linker with learned patterns |
| `IGraphRepository` | v0.4.5e | Query historical choices |
| `IMediator` | v0.0.7a | Event publishing for learning updates |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Learning uses standard .NET libraries |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Not available
- **Teams Tier:** Not available
- **Enterprise Tier:** Full learning with pattern extraction and model updates

---

## 4. Data Contract (The API)

### 4.1 IResolutionLearningService Interface

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Learns from user disambiguation choices to improve future matching.
/// Extracts patterns and updates linker confidence adjustments.
/// </summary>
public interface IResolutionLearningService
{
    /// <summary>
    /// Records a disambiguation choice for learning.
    /// Triggered when user selects entity for mention.
    /// </summary>
    /// <param name="choice">The disambiguation choice to record</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion task</returns>
    Task RecordFeedbackAsync(
        DisambiguationChoiceRecorded choice,
        CancellationToken ct = default);

    /// <summary>
    /// Extracts patterns from recorded feedback.
    /// Analyzes all recent choices to identify patterns.
    /// </summary>
    /// <param name="options">Pattern extraction configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Extracted patterns and quality metrics</returns>
    Task<PatternExtractionResult> ExtractPatternsAsync(
        PatternExtractionOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Updates entity linker with learned patterns.
    /// Adjusts confidence scores based on learning.
    /// </summary>
    /// <param name="patterns">Patterns to apply to linker</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Update result with affected entities</returns>
    Task<ModelUpdateResult> UpdateModelAsync(
        IReadOnlyList<LearnedPattern> patterns,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics on learning progress.
    /// Reports on feedback volume and pattern quality.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Learning statistics</returns>
    Task<LearningStatistics> GetLearningStatsAsync(
        CancellationToken ct = default);
}
```

### 4.2 DisambiguationChoiceRecorded Event

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Event published when user makes a disambiguation choice.
/// Consumed by learning service for pattern extraction.
/// </summary>
public record DisambiguationChoiceRecorded
{
    /// <summary>
    /// Unique ID for this choice event.
    /// </summary>
    public required Guid ChoiceId { get; init; }

    /// <summary>
    /// ID of the mention being resolved.
    /// </summary>
    public required Guid MentionId { get; init; }

    /// <summary>
    /// The text of the mention.
    /// </summary>
    public required string MentionText { get; init; }

    /// <summary>
    /// Document context for the mention.
    /// </summary>
    public required string DocumentContext { get; init; }

    /// <summary>
    /// ID of the entity selected by user.
    /// </summary>
    public required Guid SelectedEntityId { get; init; }

    /// <summary>
    /// Initial candidates presented to user.
    /// Used to understand why this choice was correct.
    /// </summary>
    public required IReadOnlyList<DisambiguationCandidate> Candidates { get; init; }

    /// <summary>
    /// Position of selected entity in candidates list (0-indexed).
    /// 0 = first candidate (likely top confidence).
    /// </summary>
    public required int SelectionPosition { get; init; }

    /// <summary>
    /// Confidence score of selected candidate.
    /// Initial linker confidence.
    /// </summary>
    public required float InitialConfidence { get; init; }

    /// <summary>
    /// User confidence in their choice (0.0 to 1.0).
    /// 1.0 = very sure, 0.0 = guess.
    /// </summary>
    public required float UserConfidence { get; init; }

    /// <summary>
    /// Should this choice be used for learning?
    /// User may opt-out of pattern learning.
    /// </summary>
    public required bool RememberForFuture { get; init; }

    /// <summary>
    /// User ID who made the choice.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Timestamp when choice was made.
    /// </summary>
    public required DateTimeOffset ChosenAt { get; init; }
}
```

### 4.3 LearnedPattern Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// A pattern extracted from user disambiguation choices.
/// Represents a rule to adjust linker confidence for future matches.
/// </summary>
public record LearnedPattern
{
    /// <summary>
    /// Unique ID for this pattern.
    /// </summary>
    public required Guid PatternId { get; init; }

    /// <summary>
    /// The type of pattern (mention-based, context-based, etc.).
    /// </summary>
    public required PatternType PatternType { get; init; }

    /// <summary>
    /// Pattern trigger (what to match on).
    /// Examples: "mention contains 'endpoint'", "context contains 'API'"
    /// </summary>
    public required string PatternTrigger { get; init; }

    /// <summary>
    /// Entity ID this pattern tends to match.
    /// When pattern triggers, boost confidence for this entity.
    /// </summary>
    public required Guid TargetEntityId { get; init; }

    /// <summary>
    /// Confidence adjustment to apply (e.g., +0.1 or -0.05).
    /// </summary>
    public required float ConfidenceAdjustment { get; init; }

    /// <summary>
    /// Number of times this pattern was observed.
    /// Higher count = stronger signal.
    /// </summary>
    public required int ObservationCount { get; init; }

    /// <summary>
    /// Accuracy of pattern (0.0 to 1.0).
    /// Percentage of choices that matched pattern.
    /// </summary>
    public required float PatternAccuracy { get; init; }

    /// <summary>
    /// Quality assessment (High, Medium, Low).
    /// Based on accuracy and observation count.
    /// </summary>
    public required PatternQuality Quality { get; init; }

    /// <summary>
    /// When this pattern was extracted.
    /// </summary>
    public required DateTimeOffset ExtractedAt { get; init; }

    /// <summary>
    /// Is this pattern active?
    /// Admin may disable low-quality patterns.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
```

### 4.4 PatternType Enum

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Categories of learnable patterns.
/// </summary>
public enum PatternType
{
    /// <summary>
    /// Pattern based on mention text content.
    /// Example: "authentication" → AuthenticationService
    /// </summary>
    MentionContent = 1,

    /// <summary>
    /// Pattern based on document context around mention.
    /// Example: Context contains "validate tokens" → specific endpoint
    /// </summary>
    DocumentContext = 2,

    /// <summary>
    /// Pattern based on entity type.
    /// Example: Type=Endpoint + name contains "POST" → specific endpoint
    /// </summary>
    EntityType = 3,

    /// <summary>
    /// Pattern based on relationship prevalence.
    /// Example: Entities with similar relationship patterns are duplicates
    /// </summary>
    RelationshipPattern = 4,

    /// <summary>
    /// Pattern based on property/claim similarity.
    /// Example: Similar descriptions suggest same entity
    /// </summary>
    PropertySimilarity = 5
}
```

### 4.5 PatternQuality Enum

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Quality assessment of a learned pattern.
/// </summary>
public enum PatternQuality
{
    /// <summary>
    /// High quality: >0.9 accuracy, >50 observations.</summary>
    High = 1,

    /// <summary>
    /// Medium quality: >0.75 accuracy, >20 observations.</summary>
    Medium = 2,

    /// <summary>
    /// Low quality: <0.75 accuracy or <20 observations.</summary>
    Low = 3,

    /// <summary>
    /// Insufficient data: <10 observations.</summary>
    Insufficient = 4
}
```

### 4.6 PatternExtractionResult Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Result of pattern extraction operation.
/// </summary>
public record PatternExtractionResult
{
    /// <summary>
    /// Patterns extracted from feedback.
    /// </summary>
    public required IReadOnlyList<LearnedPattern> ExtractedPatterns { get; init; }

    /// <summary>
    /// Number of feedback records analyzed.
    /// </summary>
    public required int FeedbackAnalyzed { get; init; }

    /// <summary>
    /// Number of valid patterns produced.
    /// Some feedback may not produce patterns.
    /// </summary>
    public required int PatternsProduced { get; init; }

    /// <summary>
    /// Distribution of pattern quality.
    /// </summary>
    public required PatternQualityDistribution QualityDistribution { get; init; }

    /// <summary>
    /// Time taken for extraction.
    /// </summary>
    public required TimeSpan ExtractionDuration { get; init; }
}
```

### 4.7 PatternQualityDistribution Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Distribution of pattern quality levels.
/// </summary>
public record PatternQualityDistribution
{
    /// <summary>Number of high-quality patterns.</summary>
    public required int HighQuality { get; init; }

    /// <summary>Number of medium-quality patterns.</summary>
    public required int MediumQuality { get; init; }

    /// <summary>Number of low-quality patterns.</summary>
    public required int LowQuality { get; init; }

    /// <summary>Number of insufficient-data patterns.</summary>
    public required int InsufficientData { get; init; }
}
```

### 4.8 ModelUpdateResult Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Result of applying learned patterns to entity linker model.
/// </summary>
public record ModelUpdateResult
{
    /// <summary>
    /// Number of patterns applied to model.
    /// </summary>
    public required int PatternsApplied { get; init; }

    /// <summary>
    /// Number of entities with confidence adjustments.
    /// </summary>
    public required int EntitiesAffected { get; init; }

    /// <summary>
    /// Total confidence adjustment applied (sum of all adjustments).
    /// </summary>
    public required float TotalAdjustment { get; init; }

    /// <summary>
    /// Whether model update was successful.
    /// False if linker rejected update.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if update failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Timestamp when model was updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}
```

### 4.9 LearningStatistics Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Statistics on the learning process.
/// Used for monitoring and admin dashboard.
/// </summary>
public record LearningStatistics
{
    /// <summary>
    /// Total feedback records collected.
    /// </summary>
    public required int TotalFeedbackRecords { get; init; }

    /// <summary>
    /// Feedback collected in last 24 hours.
    /// Measures learning velocity.
    /// </summary>
    public required int FeedbackLast24Hours { get; init; }

    /// <summary>
    /// Number of active learned patterns.
    /// Patterns currently used to adjust linker.
    /// </summary>
    public required int ActivePatterns { get; init; }

    /// <summary>
    /// Average pattern accuracy across all patterns.
    /// Indicates overall learning quality.
    /// </summary>
    public required float AveragePatternAccuracy { get; init; }

    /// <summary>
    /// Distribution of entities receiving patterns.
    /// Top entities influenced by learning.
    /// </summary>
    public required IReadOnlyList<EntityPatternStat> TopEntitiesLearned { get; init; }

    /// <summary>
    /// Most common pattern types extracted.
    /// </summary>
    public required IReadOnlyDictionary<PatternType, int> PatternTypeDistribution { get; init; }

    /// <summary>
    /// Last model update timestamp.
    /// When linker was last updated with patterns.
    /// </summary>
    public DateTimeOffset? LastModelUpdateAt { get; init; }

    /// <summary>
    /// Estimated improvement in disambiguation accuracy.
    /// Based on pattern quality and linker calibration.
    /// </summary>
    public float? EstimatedAccuracyImprovement { get; init; }
}
```

### 4.10 EntityPatternStat Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Statistics for entity-specific patterns.
/// </summary>
public record EntityPatternStat
{
    /// <summary>Entity ID being learned about.</summary>
    public required Guid EntityId { get; init; }

    /// <summary>Entity name.</summary>
    public required string EntityName { get; init; }

    /// <summary>Number of patterns learned for this entity.</summary>
    public required int PatternCount { get; init; }

    /// <summary>Average accuracy of patterns for this entity.</summary>
    public required float AverageAccuracy { get; init; }

    /// <summary>Cumulative confidence adjustment for this entity.</summary>
    public required float CumulativeAdjustment { get; init; }
}
```

### 4.11 PatternExtractionOptions Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Configuration for pattern extraction.
/// </summary>
public record PatternExtractionOptions
{
    /// <summary>
    /// Minimum accuracy threshold for pattern inclusion (default: 0.75).
    /// Patterns below this are not extracted.
    /// </summary>
    public float MinAccuracyThreshold { get; init; } = 0.75f;

    /// <summary>
    /// Minimum observation count for pattern (default: 5).
    /// Patterns with fewer observations are not extracted.
    /// </summary>
    public int MinObservationCount { get; init; } = 5;

    /// <summary>
    /// Look-back period for pattern extraction (default: 30 days).
    /// Only feedback in this period is analyzed.
    /// </summary>
    public TimeSpan LookBackPeriod { get; init; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Maximum patterns to extract (default: 100).
    /// Limits computational load.
    /// </summary>
    public int MaxPatternsToExtract { get; init; } = 100;

    /// <summary>
    /// Apply confidence cap to adjustments (default: true).
    /// Prevents single pattern from over-adjusting.
    /// </summary>
    public bool CapConfidenceAdjustments { get; init; } = true;

    /// <summary>
    /// Maximum confidence adjustment per pattern (default: 0.2).
    /// Adjustments capped at this value.
    /// </summary>
    public float MaxAdjustmentPerPattern { get; init; } = 0.2f;
}
```

---

## 5. Implementation Details

### 5.1 RecordFeedbackAsync Flow

```
Input: DisambiguationChoiceRecorded event
  ↓
1. Validate event data:
   - MentionId and SelectedEntityId exist
   - UserConfidence in valid range [0.0, 1.0]
   - Candidates list matches stored state
  ↓
2. Check RememberForFuture flag:
   - If false, store feedback but don't use for learning
   - If true, mark for pattern extraction
  ↓
3. Store feedback record:
   - Save to learning database
   - Index by mention text, entity, timestamp
  ↓
4. Update entity statistics:
   - Increment feedback count for selected entity
   - Update moving average of selection position
   - Calculate confidence consistency
  ↓
Output: Feedback recorded
```

### 5.2 ExtractPatternsAsync Flow

```
Input: PatternExtractionOptions
  ↓
1. Load feedback records matching criteria:
   - Within look-back period
   - RememberForFuture = true
   - UserConfidence > 0.5 (high confidence choices only)
  ↓
2. For each pattern type (MentionContent, DocumentContext, etc.):
   ↓
   a. Extract mention-based patterns:
      - Find common mention text substrings
      - Count associations with selected entities
      - Calculate accuracy: correct choices / total choices
   ↓
   b. Extract context-based patterns:
      - Find common context phrases
      - Correlate with selected entities
      - Calculate accuracy
   ↓
   c. Extract entity-type patterns:
      - Group by entity type
      - Find mention characteristics for each type
   ↓
   d. Extract relationship patterns:
      - Analyze relationship prevalence in feedback
   ↓
3. Filter patterns:
   - Remove patterns below MinAccuracyThreshold
   - Remove patterns with < MinObservationCount
   - Cap adjustment magnitude (CapConfidenceAdjustments)
  ↓
4. Rank patterns by quality score:
   - Quality = Accuracy × ObservationCount
  ↓
5. Select top MaxPatternsToExtract
  ↓
6. Calculate QualityDistribution
  ↓
Output: PatternExtractionResult with patterns
```

### 5.3 UpdateModelAsync Flow

```
Input: LearnedPattern list
  ↓
1. Validate patterns:
   - All required fields present
   - Confidence adjustments within bounds
  ↓
2. Group patterns by TargetEntityId:
   - Multiple patterns may affect same entity
   - Sum adjustments per entity
  ↓
3. For each affected entity:
   - Calculate new confidence adjustment
   - Apply to entity linker via IEntityLinkingService
   - Cap adjustment (default: ±0.2)
  ↓
4. Publish PatternApplied events:
   - One event per applied pattern
   - Audit trail for admin dashboard
  ↓
5. Store applied patterns in database:
   - Mark patterns as applied
   - Record timestamp and user ID
  ↓
Output: ModelUpdateResult with statistics
```

---

## 6. Pattern Extraction Algorithm

### 6.1 Mention-Based Pattern

```csharp
public LearnedPattern ExtractMentionPattern(
    List<DisambiguationChoiceRecorded> feedback,
    string mentionSubstring)
{
    var matching = feedback.Where(f =>
        f.MentionText.Contains(mentionSubstring, StringComparison.OrdinalIgnoreCase)).ToList();

    var correct = matching.Count(f =>
        f.SelectedEntityId == expectedEntityId);

    var accuracy = correct / (float)matching.Count;

    return new LearnedPattern
    {
        PatternId = Guid.NewGuid(),
        PatternType = PatternType.MentionContent,
        PatternTrigger = $"mention contains '{mentionSubstring}'",
        TargetEntityId = expectedEntityId,
        ConfidenceAdjustment = accuracy > 0.9f ? 0.15f : 0.1f,
        ObservationCount = matching.Count,
        PatternAccuracy = accuracy,
        Quality = AssessQuality(accuracy, matching.Count)
    };
}
```

### 6.2 Context-Based Pattern

```csharp
public LearnedPattern ExtractContextPattern(
    List<DisambiguationChoiceRecorded> feedback,
    string contextPhrase)
{
    var matching = feedback.Where(f =>
        f.DocumentContext.Contains(contextPhrase, StringComparison.OrdinalIgnoreCase)).ToList();

    var correct = matching.Count(f =>
        f.SelectedEntityId == expectedEntityId);

    var accuracy = correct / (float)matching.Count;

    return new LearnedPattern
    {
        PatternId = Guid.NewGuid(),
        PatternType = PatternType.DocumentContext,
        PatternTrigger = $"context contains '{contextPhrase}'",
        TargetEntityId = expectedEntityId,
        ConfidenceAdjustment = accuracy > 0.85f ? 0.12f : 0.08f,
        ObservationCount = matching.Count,
        PatternAccuracy = accuracy,
        Quality = AssessQuality(accuracy, matching.Count)
    };
}
```

---

## 7. Error Handling

### 7.1 Invalid Feedback Event

**Scenario:** DisambiguationChoiceRecorded has missing or invalid data.

**Handling:**
- Validate in RecordFeedbackAsync
- Log validation errors
- Reject invalid feedback gracefully

**Code:**
```csharp
if (string.IsNullOrWhiteSpace(choice.MentionText))
    throw new InvalidFeedbackException("Mention text is required");
```

### 7.2 Pattern Extraction Timeout

**Scenario:** Pattern extraction on large feedback set exceeds timeout.

**Handling:**
- Implement timeout in ExtractPatternsAsync
- Return partial results if timeout occurs
- Log warning with partial result counts

**Code:**
```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromMinutes(5));

try
{
    patterns = await ExtractPatternsInternalAsync(options, cts.Token);
}
catch (OperationCanceledException)
{
    _logger.LogWarning("Pattern extraction timed out, returning partial results");
    return partialResult;
}
```

### 7.3 Model Update Rejection

**Scenario:** Entity linker rejects pattern adjustments.

**Handling:**
- Catch rejection from IEntityLinkingService
- Log reason for rejection
- Return failed ModelUpdateResult with error message

**Code:**
```csharp
try
{
    await _linkingService.ApplyConfidenceAdjustmentAsync(entityId, adjustment);
}
catch (InvalidAdjustmentException ex)
{
    return new ModelUpdateResult
    {
        Success = false,
        ErrorMessage = ex.Message
    };
}
```

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class ResolutionLearningServiceTests
{
    private ResolutionLearningService _service;
    private Mock<IGraphRepository> _graphRepositoryMock;
    private Mock<IEntityLinkingService> _linkingServiceMock;
    private Mock<IMediator> _mediatorMock;

    [TestInitialize]
    public void Setup()
    {
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _linkingServiceMock = new Mock<IEntityLinkingService>();
        _mediatorMock = new Mock<IMediator>();

        _service = new ResolutionLearningService(
            _graphRepositoryMock.Object,
            _linkingServiceMock.Object,
            _mediatorMock.Object);
    }

    [TestMethod]
    public async Task RecordFeedbackAsync_WithValidChoice_StoresFeedback()
    {
        var choice = new DisambiguationChoiceRecorded
        {
            ChoiceId = Guid.NewGuid(),
            MentionId = Guid.NewGuid(),
            MentionText = "authentication endpoint",
            DocumentContext = "The authentication endpoint validates tokens",
            SelectedEntityId = Guid.NewGuid(),
            Candidates = new List<DisambiguationCandidate>(),
            SelectionPosition = 0,
            InitialConfidence = 0.82f,
            UserConfidence = 0.95f,
            RememberForFuture = true,
            ChosenAt = DateTimeOffset.UtcNow
        };

        await _service.RecordFeedbackAsync(choice);

        // Verify feedback stored
        // (Detailed verification depends on storage implementation)
    }

    [TestMethod]
    public async Task ExtractPatternsAsync_WithMultipleFeedback_ExtractsPatterns()
    {
        // Setup mock feedback data
        var feedback1 = new DisambiguationChoiceRecorded
        {
            MentionText = "POST endpoint",
            SelectedEntityId = Guid.NewGuid(),
            UserConfidence = 0.9f,
            RememberForFuture = true,
            ChosenAt = DateTimeOffset.UtcNow
        };

        var feedback2 = new DisambiguationChoiceRecorded
        {
            MentionText = "POST request handler",
            SelectedEntityId = feedback1.SelectedEntityId, // Same entity
            UserConfidence = 0.85f,
            RememberForFuture = true,
            ChosenAt = DateTimeOffset.UtcNow
        };

        _graphRepositoryMock.Setup(x => x.GetDisambiguationFeedback(
            It.IsAny<TimeSpan>()))
            .Returns(new List<DisambiguationChoiceRecorded> { feedback1, feedback2 });

        var options = new PatternExtractionOptions();
        var result = await _service.ExtractPatternsAsync(options);

        Assert.IsTrue(result.ExtractedPatterns.Count > 0);
        Assert.IsTrue(result.PatternsProduced > 0);
    }

    [TestMethod]
    public async Task ExtractPatternsAsync_WithLowAccuracyFeedback_FiltersPatternsbyThreshold()
    {
        // Create feedback with 60% accuracy (below 0.75 threshold)
        var feedback = CreateFeedbackWithAccuracy(0.6f, 10);

        _graphRepositoryMock.Setup(x => x.GetDisambiguationFeedback(
            It.IsAny<TimeSpan>()))
            .Returns(feedback);

        var options = new PatternExtractionOptions { MinAccuracyThreshold = 0.75f };
        var result = await _service.ExtractPatternsAsync(options);

        // Should filter out low-accuracy patterns
        Assert.IsFalse(result.ExtractedPatterns.Any(p => p.PatternAccuracy < 0.75f));
    }

    [TestMethod]
    public async Task UpdateModelAsync_WithValidPatterns_AppliesAdjustments()
    {
        var entityId = Guid.NewGuid();
        var patterns = new List<LearnedPattern>
        {
            new()
            {
                PatternId = Guid.NewGuid(),
                PatternType = PatternType.MentionContent,
                PatternTrigger = "mention contains 'endpoint'",
                TargetEntityId = entityId,
                ConfidenceAdjustment = 0.1f,
                ObservationCount = 15,
                PatternAccuracy = 0.93f,
                Quality = PatternQuality.High
            }
        };

        var result = await _service.UpdateModelAsync(patterns);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.PatternsApplied);
        _linkingServiceMock.Verify(
            x => x.ApplyConfidenceAdjustmentAsync(entityId, It.IsAny<float>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetLearningStatsAsync_ReturnsStats()
    {
        _graphRepositoryMock.Setup(x => x.GetFeedbackCount()).Returns(150);
        _graphRepositoryMock.Setup(x => x.GetActivePatternCount()).Returns(25);
        _graphRepositoryMock.Setup(x => x.GetAveragePatternAccuracy()).Returns(0.87f);

        var stats = await _service.GetLearningStatsAsync();

        Assert.AreEqual(150, stats.TotalFeedbackRecords);
        Assert.AreEqual(25, stats.ActivePatterns);
        Assert.AreEqual(0.87f, stats.AveragePatternAccuracy, 0.01f);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class ResolutionLearningIntegrationTests
{
    [TestMethod]
    public async Task E2E_RecordFeedback_ExtractPatterns_UpdateModel()
    {
        // Step 1: Record multiple feedback entries
        var entityId = Guid.NewGuid();
        var choiceEvents = new List<DisambiguationChoiceRecorded>();

        for (int i = 0; i < 10; i++)
        {
            var choice = new DisambiguationChoiceRecorded
            {
                MentionText = "authentication endpoint",
                SelectedEntityId = entityId,
                UserConfidence = 0.9f,
                RememberForFuture = true,
                ChosenAt = DateTimeOffset.UtcNow.AddHours(-i)
            };
            choiceEvents.Add(choice);
            await _service.RecordFeedbackAsync(choice);
        }

        // Step 2: Extract patterns
        var extractOptions = new PatternExtractionOptions();
        var extractResult = await _service.ExtractPatternsAsync(extractOptions);

        Assert.IsTrue(extractResult.ExtractedPatterns.Count > 0);

        // Step 3: Update model with extracted patterns
        var updateResult = await _service.UpdateModelAsync(extractResult.ExtractedPatterns);

        Assert.IsTrue(updateResult.Success);
        Assert.IsTrue(updateResult.PatternsApplied > 0);

        // Step 4: Verify learning stats updated
        var stats = await _service.GetLearningStatsAsync();

        Assert.AreEqual(10, stats.TotalFeedbackRecords);
        Assert.IsTrue(stats.ActivePatterns > 0);
        Assert.IsTrue(stats.AveragePatternAccuracy > 0.0f);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| RecordFeedbackAsync | <10ms (P95) | Async batch insert |
| ExtractPatternsAsync | <5s (P95) | Efficient feedback scanning, early stopping |
| UpdateModelAsync | <100ms (P95) | Batch linker updates |
| GetLearningStatsAsync | <100ms (P95) | Cached statistics |

### 9.1 Optimization Strategy

- Batch feedback storage (append-only log design)
- Incremental pattern extraction (only new feedback)
- Cache learning statistics (1-hour TTL)
- Background model update (async, non-blocking)
- Limit pattern set size

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Learning bad patterns | High | Quality filtering, admin review, manual override |
| Over-fitting to user errors | Medium | Minimum observation count, accuracy thresholds |
| Feedback tampering | Medium | Audit trail, user attribution, validation |
| Model poisoning | Low | Pattern size limits, confidence caps |

---

## 11. License Gating

```csharp
public class ResolutionLearningService : IResolutionLearningService
{
    private readonly ILicenseContext _licenseContext;

    public async Task<PatternExtractionResult> ExtractPatternsAsync(
        PatternExtractionOptions options,
        CancellationToken ct = default)
    {
        if (!_licenseContext.IsAvailable(LicenseTier.Enterprise))
            throw new LicenseRequiredException("Resolution learning requires Enterprise tier");

        // Implementation...
    }
}
```

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | DisambiguationChoiceRecorded event | RecordFeedbackAsync called | Feedback stored and indexed |
| 2 | 10 feedback entries for same entity | ExtractPatternsAsync | Patterns extracted with observation count >= 10 |
| 3 | Pattern with 0.95 accuracy | ExtractPatternsAsync with 0.75 threshold | Pattern included (accuracy > threshold) |
| 4 | Pattern with 0.60 accuracy | ExtractPatternsAsync with 0.75 threshold | Pattern excluded (accuracy < threshold) |
| 5 | Extracted patterns | UpdateModelAsync | Patterns applied to linker without error |
| 6 | 150 feedback records | GetLearningStatsAsync | Returns stats with TotalFeedbackRecords = 150 |
| 7 | Low-quality pattern | UpdateModelAsync with quality filter | Pattern filtered and not applied |
| 8 | Enterprise license | Any learning operation | Operation succeeds |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - IResolutionLearningService, pattern extraction, model updates |
