# LCS-DES-v0.10.3-KG-a: Design Specification — Disambiguation Service

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-v0.10.3a` | Entity Resolution sub-part a |
| **Feature Name** | `Disambiguation Service` | Handle uncertain entity matches |
| **Target Version** | `v0.10.3a` | First sub-part of v0.10.3-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Canonical Knowledge & Versioned Store module |
| **Swimlane** | `Entity Resolution` | Knowledge graph vertical |
| **License Tier** | `WriterPro` | Available in WriterPro tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.EntityResolution` | Entity resolution feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.3-KG](./LCS-SBD-v0.10.3-KG.md) | Entity Resolution scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.3-KG S2.1](./LCS-SBD-v0.10.3-KG.md#21-sub-parts) | a = Disambiguation Service |

---

## 2. Executive Summary

### 2.1 The Requirement

When entity linking produces multiple candidate matches for a mention with confidence below threshold (< 0.8), users must be able to:

1. View all candidates with confidence scores and context
2. Select the correct entity from candidates
3. Create a new entity if none match
4. Record their choice for system learning

The service must auto-resolve matches with high confidence (> 0.9) without user intervention.

### 2.2 The Proposed Solution

Implement `IDisambiguationService` with:

1. **GetCandidatesAsync:** Score and rank disambiguation candidates
2. **RecordChoiceAsync:** Store user selections for learning
3. **CreateFromMentionAsync:** Create new entities from unmatched mentions
4. Auto-resolution logic for high-confidence scenarios
5. Integration with entity linking workflow

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IEntityLinkingService` | v0.5.5-KG | Entity linking integration and confidence scores |
| `IEntityBrowser` | v0.4.7-KG | Entity CRUD operations |
| `IGraphRepository` | v0.4.5e | Graph storage and querying |
| `IMediator` | v0.0.7a | Event publishing for learning service |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Disambiguation uses standard .NET libraries |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Full disambiguation service (basic)
- **Teams Tier:** Full disambiguation service
- **Enterprise Tier:** Full disambiguation service + learning integration

---

## 4. Data Contract (The API)

### 4.1 IDisambiguationService Interface

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Handles entity disambiguation when linking is uncertain.
/// Provides candidate selection, entity creation, and feedback recording.
/// </summary>
public interface IDisambiguationService
{
    /// <summary>
    /// Gets disambiguation candidates for a mention.
    /// Scores and ranks candidates based on confidence and relevance.
    /// </summary>
    /// <param name="mention">The entity mention requiring disambiguation</param>
    /// <param name="options">Disambiguation options (thresholds, scoring)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Disambiguation result with candidates and auto-resolution status</returns>
    Task<DisambiguationResult> GetCandidatesAsync(
        EntityMention mention,
        DisambiguationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Records user's disambiguation choice for learning.
    /// Publishes event for learning service integration.
    /// </summary>
    /// <param name="mentionId">ID of the mention being resolved</param>
    /// <param name="chosenEntityId">ID of the entity selected by user</param>
    /// <param name="feedback">Additional feedback (confidence, notes)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion task</returns>
    Task RecordChoiceAsync(
        Guid mentionId,
        Guid chosenEntityId,
        DisambiguationFeedback feedback,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new entity from an unmatched mention.
    /// Used when no suitable candidates exist.
    /// </summary>
    /// <param name="mention">The mention to create entity from</param>
    /// <param name="options">Entity creation options (type, properties)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Newly created entity</returns>
    Task<Entity> CreateFromMentionAsync(
        EntityMention mention,
        EntityCreationOptions options,
        CancellationToken ct = default);
}
```

### 4.2 DisambiguationResult Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Result of disambiguation analysis for a mention.
/// Contains candidates and auto-resolution status.
/// </summary>
public record DisambiguationResult
{
    /// <summary>
    /// Unique ID of the mention being disambiguated.
    /// </summary>
    public required Guid MentionId { get; init; }

    /// <summary>
    /// The text of the mention (e.g., "authentication endpoint").
    /// </summary>
    public required string MentionText { get; init; }

    /// <summary>
    /// Document context where mention appears.
    /// </summary>
    public required string DocumentContext { get; init; }

    /// <summary>
    /// Ranked list of candidate entities.
    /// Sorted by confidence score (highest first).
    /// </summary>
    public required IReadOnlyList<DisambiguationCandidate> Candidates { get; init; }

    /// <summary>
    /// True if user input is required to resolve.
    /// False if auto-resolved by high confidence.
    /// </summary>
    public required bool RequiresUserInput { get; init; }

    /// <summary>
    /// ID of auto-selected entity if RequiresUserInput is false.
    /// Null if user input required or no suitable candidates.
    /// </summary>
    public Guid? AutoSelectedId { get; init; }

    /// <summary>
    /// Reason for auto-selection (if applied).
    /// Example: "Top candidate confidence 0.95 exceeds threshold"
    /// </summary>
    public string? AutoResolutionReason { get; init; }
}
```

### 4.3 DisambiguationCandidate Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// A candidate entity for disambiguation.
/// Includes confidence score and match reasoning.
/// </summary>
public record DisambiguationCandidate
{
    /// <summary>
    /// Unique ID of the candidate entity.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Display name of the candidate entity.
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Entity type (e.g., "Endpoint", "Service", "Concept").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Confidence score for this candidate (0.0 to 1.0).
    /// Higher = better match.
    /// </summary>
    public required float ConfidenceScore { get; init; }

    /// <summary>
    /// Reasons why this entity matches.
    /// Examples: "Name match", "Type match", "Context match"
    /// </summary>
    public required IReadOnlyList<string> MatchReasons { get; init; }

    /// <summary>
    /// Snippet of context from the entity's definition.
    /// Used to help user make selection.
    /// </summary>
    public string? ContextSnippet { get; init; }

    /// <summary>
    /// URL or reference to the entity in the knowledge base.
    /// </summary>
    public string? EntityReference { get; init; }

    /// <summary>
    /// Number of times this entity appears in documents.
    /// Helps assess prevalence.
    /// </summary>
    public int DocumentFrequency { get; init; }
}
```

### 4.4 DisambiguationOptions Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Configuration options for disambiguation.
/// Controls scoring thresholds and behavior.
/// </summary>
public record DisambiguationOptions
{
    /// <summary>
    /// Minimum confidence to auto-resolve without user input (default: 0.9).
    /// Must be between 0.0 and 1.0.
    /// </summary>
    public float AutoResolutionThreshold { get; init; } = 0.9f;

    /// <summary>
    /// Minimum confidence to include candidate in results (default: 0.5).
    /// Candidates below this are filtered out.
    /// </summary>
    public float MinimumCandidateThreshold { get; init; } = 0.5f;

    /// <summary>
    /// Maximum number of candidates to return (default: 5).
    /// Ranked by confidence, lowest-scoring candidates dropped.
    /// </summary>
    public int MaxCandidatesToReturn { get; init; } = 5;

    /// <summary>
    /// Include context snippet in candidates (default: true).
    /// If false, ContextSnippet will be null.
    /// </summary>
    public bool IncludeContextSnippet { get; init; } = true;

    /// <summary>
    /// Entity type filter (optional).
    /// If set, only candidates of this type are considered.
    /// </summary>
    public string? EntityTypeFilter { get; init; }

    /// <summary>
    /// Scope filter (optional).
    /// If set, only entities in this document/scope are considered.
    /// </summary>
    public string? ScopeFilter { get; init; }

    /// <summary>
    /// Enable case-insensitive name matching (default: true).
    /// </summary>
    public bool CaseInsensitiveMatching { get; init; } = true;

    /// <summary>
    /// Enable fuzzy name matching (Levenshtein) (default: true).
    /// </summary>
    public bool EnableFuzzyMatching { get; init; } = true;
}
```

### 4.5 DisambiguationFeedback Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// User feedback on a disambiguation choice.
/// Used for learning and improving future matches.
/// </summary>
public record DisambiguationFeedback
{
    /// <summary>
    /// User's confidence in their choice (0.0 to 1.0).
    /// 1.0 = very confident, 0.0 = guess.
    /// </summary>
    public float UserConfidence { get; init; } = 1.0f;

    /// <summary>
    /// Optional user notes about the choice.
    /// Example: "This is the API endpoint, not the request validator"
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Timestamp when user made the choice.
    /// </summary>
    public DateTimeOffset ChosenAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// ID of the user making the choice.
    /// </summary>
    public Guid? ChosenBy { get; init; }

    /// <summary>
    /// Remember this choice for similar mentions (default: true).
    /// If true, learning service should extract patterns.
    /// </summary>
    public bool RememberForFuture { get; init; } = true;
}
```

### 4.6 EntityCreationOptions Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Options for creating a new entity from a mention.
/// </summary>
public record EntityCreationOptions
{
    /// <summary>
    /// Entity type for the new entity (required).
    /// Examples: "Endpoint", "Service", "Concept"
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Display name for the new entity (required).
    /// Typically derived from the mention text.
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Optional description for the new entity.
    /// Can be populated from mention context.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional aliases for the new entity.
    /// Derived from related mentions.
    /// </summary>
    public IReadOnlyList<string>? Aliases { get; init; }

    /// <summary>
    /// Mark the entity as draft/pending review (default: false).
    /// If true, entity is flagged for human review.
    /// </summary>
    public bool IsPendingReview { get; init; } = false;

    /// <summary>
    /// Source document(s) where entity was discovered.
    /// </summary>
    public IReadOnlyList<string>? SourceDocuments { get; init; }
}
```

### 4.7 EntityMention Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// A mention of an entity in a document.
/// Used as input to disambiguation service.
/// </summary>
public record EntityMention
{
    /// <summary>
    /// Unique ID for this mention.
    /// </summary>
    public required Guid MentionId { get; init; }

    /// <summary>
    /// The text of the mention.
    /// </summary>
    public required string MentionText { get; init; }

    /// <summary>
    /// Document where mention appears.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// Character offset of mention in document.
    /// </summary>
    public int CharacterOffset { get; init; }

    /// <summary>
    /// Paragraph/section context around the mention.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// Initial entity type hint from linking.
    /// </summary>
    public string? HintedEntityType { get; init; }

    /// <summary>
    /// Initial confidence from entity linker.
    /// </summary>
    public float LinkingConfidence { get; init; }

    /// <summary>
    /// Timestamp when mention was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

---

## 5. Implementation Details

### 5.1 GetCandidatesAsync Flow

```
Input: EntityMention + DisambiguationOptions
  ↓
1. Query entity linker for candidates
  ↓
2. Apply entity type filter (if specified)
  ↓
3. Score candidates based on:
   - Name similarity (string matching)
   - Type match (exact match = +0.1)
   - Context relevance (TF-IDF on context)
   - Document frequency (popularity signal)
  ↓
4. Rank by confidence score (highest first)
  ↓
5. Apply minimum threshold filter
  ↓
6. Truncate to MaxCandidatesToReturn
  ↓
7. Check auto-resolution threshold:
   - If top candidate ≥ 0.9 AND others <0.8 → AutoSelectedId set
   - RequiresUserInput = false
  ↓
8. Populate ContextSnippet if requested
  ↓
Output: DisambiguationResult
```

### 5.2 RecordChoiceAsync Flow

```
Input: MentionId + ChosenEntityId + Feedback
  ↓
1. Validate mention and entity exist
  ↓
2. Create DisambiguationChoiceRecorded event:
   - MentionId, ChosenEntityId, UserConfidence
   - ChosenBy, ChosenAt, RememberForFuture
  ↓
3. Store in audit service (Part F)
  ↓
4. Publish event via IMediator:
   - Subscribing learning service (Part D) receives event
   - Learning service extracts patterns if RememberForFuture=true
  ↓
5. Update entity linking statistics:
   - Increment feedback count for this entity
   - Log choice for future calibration
  ↓
Output: Task completion
```

### 5.3 CreateFromMentionAsync Flow

```
Input: EntityMention + EntityCreationOptions
  ↓
1. Validate options (name, type required)
  ↓
2. Create Entity via IEntityBrowser:
   - Name = EntityCreationOptions.EntityName
   - Type = EntityCreationOptions.EntityType
   - Description = EntityCreationOptions.Description
   - IsPendingReview = EntityCreationOptions.IsPendingReview
  ↓
3. Add aliases if provided:
   - Create one alias per MentionText variation
   - Mark mention text as alias
  ↓
4. Add source documents:
   - Create relationship to source document
   - Record creation reason: "Created from mention"
  ↓
5. Publish EntityCreatedFromMention event
  ↓
6. Return created Entity
  ↓
Output: Entity
```

---

## 6. Scoring Algorithm

### 6.1 Confidence Composition

```
TotalConfidence = (0.4 × NameScore) +
                  (0.3 × TypeScore) +
                  (0.2 × ContextScore) +
                  (0.1 × FrequencyScore)

Where:
  NameScore ∈ [0, 1]       = Levenshtein or exact match
  TypeScore ∈ [0, 1]       = 1.0 if types match, else 0.0
  ContextScore ∈ [0, 1]    = TF-IDF similarity
  FrequencyScore ∈ [0, 1]  = min(DocumentFrequency / MaxFrequency, 1.0)
```

### 6.2 Name Matching

- **Exact match (case-insensitive):** NameScore = 1.0
- **Fuzzy match (Levenshtein < 2):** NameScore = 0.95
- **Fuzzy match (Levenshtein < 4):** NameScore = 0.8
- **Fuzzy match (Levenshtein < 6):** NameScore = 0.6
- **No name match:** NameScore = 0.0

---

## 7. Error Handling

### 7.1 Invalid Disambiguation Options

**Scenario:** Options contain invalid thresholds (e.g., AutoResolutionThreshold > 1.0).

**Handling:**
- Validation in GetCandidatesAsync
- Throw `ArgumentException` with clear message
- Log validation error for debugging

**Code:**
```csharp
if (options.AutoResolutionThreshold < 0 || options.AutoResolutionThreshold > 1.0)
    throw new ArgumentException(
        "AutoResolutionThreshold must be between 0.0 and 1.0",
        nameof(options));
```

### 7.2 No Candidates Found

**Scenario:** No entities match the mention criteria.

**Handling:**
- Return DisambiguationResult with empty Candidates list
- RequiresUserInput = true
- Suggest CreateFromMentionAsync to caller

**Code:**
```csharp
return new DisambiguationResult
{
    MentionId = mention.MentionId,
    MentionText = mention.MentionText,
    Candidates = [],
    RequiresUserInput = true,
    AutoSelectedId = null
};
```

### 7.3 Entity Creation Failure

**Scenario:** IEntityBrowser.CreateAsync fails.

**Handling:**
- Catch and wrap in `EntityCreationException`
- Log the underlying error
- Rethrow with helpful message

**Code:**
```csharp
try
{
    return await _entityBrowser.CreateAsync(entity, ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to create entity from mention {MentionId}", mention.MentionId);
    throw new EntityCreationException($"Failed to create entity: {ex.Message}", ex);
}
```

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class DisambiguationServiceTests
{
    private DisambiguationService _service;
    private Mock<IEntityLinkingService> _linkingServiceMock;
    private Mock<IEntityBrowser> _entityBrowserMock;
    private Mock<IGraphRepository> _graphRepositoryMock;
    private Mock<IMediator> _mediatorMock;

    [TestInitialize]
    public void Setup()
    {
        _linkingServiceMock = new Mock<IEntityLinkingService>();
        _entityBrowserMock = new Mock<IEntityBrowser>();
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _mediatorMock = new Mock<IMediator>();

        _service = new DisambiguationService(
            _linkingServiceMock.Object,
            _entityBrowserMock.Object,
            _graphRepositoryMock.Object,
            _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetCandidatesAsync_WithHighConfidenceCandidate_AutoResolvesIfAboveThreshold()
    {
        var mention = new EntityMention
        {
            MentionId = Guid.NewGuid(),
            MentionText = "authentication endpoint",
            DocumentId = "doc1",
            Context = "The authentication endpoint validates tokens"
        };

        var candidates = new List<DisambiguationCandidate>
        {
            new()
            {
                EntityId = Guid.NewGuid(),
                EntityName = "POST /auth/validate",
                EntityType = "Endpoint",
                ConfidenceScore = 0.95f,
                MatchReasons = ["Name match", "Type match"],
                ContextSnippet = "Validates tokens"
            },
            new()
            {
                EntityId = Guid.NewGuid(),
                EntityName = "POST /auth/login",
                EntityType = "Endpoint",
                ConfidenceScore = 0.7f,
                MatchReasons = ["Name match"],
                ContextSnippet = "Authenticates user"
            }
        };

        _linkingServiceMock.Setup(x => x.GetCandidatesAsync(
            It.IsAny<EntityMention>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

        var options = new DisambiguationOptions
        {
            AutoResolutionThreshold = 0.9f
        };

        var result = await _service.GetCandidatesAsync(mention, options);

        Assert.IsFalse(result.RequiresUserInput);
        Assert.AreEqual(candidates[0].EntityId, result.AutoSelectedId);
        Assert.IsNotNull(result.AutoResolutionReason);
    }

    [TestMethod]
    public async Task GetCandidatesAsync_WithLowConfidenceCandidates_RequiresUserInput()
    {
        var mention = new EntityMention
        {
            MentionId = Guid.NewGuid(),
            MentionText = "user",
            DocumentId = "doc1",
            Context = "The user service manages user accounts"
        };

        var candidates = new List<DisambiguationCandidate>
        {
            new()
            {
                EntityId = Guid.NewGuid(),
                EntityName = "User",
                EntityType = "Entity",
                ConfidenceScore = 0.75f,
                MatchReasons = ["Name match"],
                ContextSnippet = "Represents a user"
            }
        };

        _linkingServiceMock.Setup(x => x.GetCandidatesAsync(
            It.IsAny<EntityMention>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

        var options = new DisambiguationOptions
        {
            AutoResolutionThreshold = 0.9f
        };

        var result = await _service.GetCandidatesAsync(mention, options);

        Assert.IsTrue(result.RequiresUserInput);
        Assert.IsNull(result.AutoSelectedId);
    }

    [TestMethod]
    public async Task RecordChoiceAsync_PublishesDisambiguationChoiceEvent()
    {
        var mentionId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var feedback = new DisambiguationFeedback
        {
            UserConfidence = 0.95f,
            RememberForFuture = true
        };

        await _service.RecordChoiceAsync(mentionId, entityId, feedback);

        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<DisambiguationChoiceRecorded>()),
            Times.Once);
    }

    [TestMethod]
    public async Task CreateFromMentionAsync_CreatesEntityWithMentionName()
    {
        var mention = new EntityMention
        {
            MentionId = Guid.NewGuid(),
            MentionText = "API Gateway",
            DocumentId = "doc1",
            Context = "The API Gateway routes requests"
        };

        var options = new EntityCreationOptions
        {
            EntityType = "Service",
            EntityName = "API Gateway",
            Description = "Main API entry point"
        };

        var expectedEntity = new Entity
        {
            EntityId = Guid.NewGuid(),
            Name = "API Gateway",
            Type = "Service"
        };

        _entityBrowserMock.Setup(x => x.CreateAsync(
            It.Is<Entity>(e => e.Name == "API Gateway" && e.Type == "Service"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);

        var result = await _service.CreateFromMentionAsync(mention, options);

        Assert.AreEqual(expectedEntity.EntityId, result.EntityId);
        Assert.AreEqual("API Gateway", result.Name);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetCandidatesAsync_WithInvalidThreshold_ThrowsArgumentException()
    {
        var mention = new EntityMention
        {
            MentionId = Guid.NewGuid(),
            MentionText = "test",
            DocumentId = "doc1",
            Context = "test"
        };

        var invalidOptions = new DisambiguationOptions
        {
            AutoResolutionThreshold = 1.5f // Invalid!
        };

        await _service.GetCandidatesAsync(mention, invalidOptions);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class DisambiguationServiceIntegrationTests
{
    [TestMethod]
    public async Task E2E_Disambiguation_WithUserChoice_RecordsAndPublishesEvent()
    {
        // Setup test data
        var mention = new EntityMention
        {
            MentionId = Guid.NewGuid(),
            MentionText = "authentication service",
            DocumentId = "test-doc",
            Context = "The authentication service validates credentials"
        };

        // Get candidates
        var options = new DisambiguationOptions();
        var result = await _service.GetCandidatesAsync(mention, options);

        // Verify candidates returned
        Assert.IsTrue(result.Candidates.Count > 0);

        // Simulate user selection
        var selectedEntityId = result.Candidates[0].EntityId;
        var feedback = new DisambiguationFeedback
        {
            UserConfidence = 0.95f,
            RememberForFuture = true
        };

        await _service.RecordChoiceAsync(mention.MentionId, selectedEntityId, feedback);

        // Verify choice was recorded (via audit service)
        // (Detailed verification depends on audit service implementation)
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| GetCandidatesAsync | <500ms (P95) | Cached entity index, efficient scoring |
| RecordChoiceAsync | <50ms (P95) | Async event publishing |
| CreateFromMentionAsync | <200ms (P95) | Direct graph write |
| Candidate scoring | <1ms per candidate | Vectorized operations |

### 9.1 Caching Strategy

- Cache entity index (name, type, aliases) in memory
- Invalidate on entity create/update
- Use LRU cache with 10K entity entries
- Cache expiry: 1 hour or on graph update

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Incorrect disambiguation | Medium | User feedback drives learning, undo via version service |
| Entity creation DoS | Medium | Rate limiting, validation of entity properties |
| Malicious pattern learning | Medium | Admin review of learning patterns (Part D) |

---

## 11. License Gating

```csharp
public class DisambiguationService : IDisambiguationService
{
    private readonly ILicenseContext _licenseContext;

    public async Task<DisambiguationResult> GetCandidatesAsync(
        EntityMention mention,
        DisambiguationOptions options,
        CancellationToken ct = default)
    {
        if (!_licenseContext.IsAvailable(LicenseTier.WriterPro))
            throw new LicenseRequiredException("Entity disambiguation requires WriterPro tier");

        // Implementation...
    }
}
```

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | EntityMention with multiple candidates | GetCandidatesAsync called | Candidates ranked by confidence |
| 2 | Top candidate with 0.95 confidence | GetCandidatesAsync with 0.9 threshold | RequiresUserInput = false, AutoSelectedId set |
| 3 | User selects entity | RecordChoiceAsync called | Event published to learning service |
| 4 | EntityCreationOptions with valid data | CreateFromMentionAsync called | Entity created with correct name/type |
| 5 | Invalid threshold (>1.0) | GetCandidatesAsync called | ArgumentException thrown |
| 6 | WriterPro license | Any method called | Operation succeeds |
| 7 | Core tier license | GetCandidatesAsync called | LicenseRequiredException thrown |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - IDisambiguationService, GetCandidatesAsync, RecordChoiceAsync, CreateFromMentionAsync |
