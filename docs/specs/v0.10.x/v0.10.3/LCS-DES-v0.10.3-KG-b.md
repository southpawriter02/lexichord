# LCS-DES-v0.10.3-KG-b: Design Specification — Duplicate Detector

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-v0.10.3b` | Entity Resolution sub-part b |
| **Feature Name** | `Duplicate Detector` | Find potential duplicate entities |
| **Target Version** | `v0.10.3b` | Second sub-part of v0.10.3-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Canonical Knowledge & Versioned Store module |
| **Swimlane** | `Entity Resolution` | Knowledge graph vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.EntityResolution` | Entity resolution feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.3-KG](./LCS-SBD-v0.10.3-KG.md) | Entity Resolution scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.3-KG S2.1](./LCS-SBD-v0.10.3-KG.md#21-sub-parts) | b = Duplicate Detector |

---

## 2. Executive Summary

### 2.1 The Requirement

Detect and group potential duplicate entities across the knowledge graph using:

1. Name similarity (Levenshtein, Jaro-Winkler)
2. Type matching
3. Property and claim overlap
4. Relationship pattern analysis
5. Cross-document occurrence analysis

The detector must produce groups with confidence scores and suggestions for merging.

### 2.2 The Proposed Solution

Implement `IDuplicateDetector` with:

1. **ScanForDuplicatesAsync:** Full-graph duplicate scan
2. **FindDuplicatesOfAsync:** Find duplicates for specific entity
3. **GetDuplicateGroupsAsync:** Retrieve duplicate groups with filtering
4. Similarity engines for name, property, and relationship matching
5. Configurable similarity thresholds

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Query all entities and relationships |
| `IEntityBrowser` | v0.4.7-KG | Entity metadata and properties |
| `IMediator` | v0.0.7a | Event publishing for scan completion |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `SimMetrics.Net` | Latest | String similarity algorithms |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Not available
- **Teams Tier:** Full duplicate detection
- **Enterprise Tier:** Full duplicate detection with advanced analytics

---

## 4. Data Contract (The API)

### 4.1 IDuplicateDetector Interface

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Detects and manages duplicate entities across the knowledge graph.
/// Provides scanning, grouping, and duplicate identification.
/// </summary>
public interface IDuplicateDetector
{
    /// <summary>
    /// Scans entire graph for potential duplicates.
    /// Performs pairwise similarity analysis on all entities.
    /// </summary>
    /// <param name="options">Scan configuration (thresholds, filters)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Scan result with duplicate groups</returns>
    Task<DuplicateScanResult> ScanForDuplicatesAsync(
        DuplicateScanOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a specific entity has potential duplicates.
    /// Returns ranked list of candidate duplicates.
    /// </summary>
    /// <param name="entityId">ID of entity to check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of duplicate candidates for this entity</returns>
    Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesOfAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets duplicate groups for review and resolution.
    /// Applies filters and pagination.
    /// </summary>
    /// <param name="filter">Filtering and sorting options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of duplicate groups matching filter criteria</returns>
    Task<IReadOnlyList<DuplicateGroup>> GetDuplicateGroupsAsync(
        DuplicateGroupFilter filter,
        CancellationToken ct = default);
}
```

### 4.2 DuplicateScanResult Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Result of a full duplicate detection scan.
/// Contains groups and scanning statistics.
/// </summary>
public record DuplicateScanResult
{
    /// <summary>
    /// Total number of entities scanned.
    /// </summary>
    public required int EntitiesScanned { get; init; }

    /// <summary>
    /// Number of duplicate groups found.
    /// Groups with 2+ entities identified as potential duplicates.
    /// </summary>
    public required int DuplicateGroupsFound { get; init; }

    /// <summary>
    /// Total number of entities in duplicate groups.
    /// Sum of all entities across all groups.
    /// </summary>
    public required int TotalDuplicates { get; init; }

    /// <summary>
    /// Time taken to complete the scan.
    /// Used for performance monitoring.
    /// </summary>
    public required TimeSpan ScanDuration { get; init; }

    /// <summary>
    /// Scan start timestamp.
    /// </summary>
    public required DateTimeOffset ScanStartedAt { get; init; }

    /// <summary>
    /// Scan completion timestamp.
    /// </summary>
    public required DateTimeOffset ScanCompletedAt { get; init; }

    /// <summary>
    /// The duplicate groups discovered.
    /// Sorted by GroupConfidence (highest first).
    /// </summary>
    public required IReadOnlyList<DuplicateGroup> Groups { get; init; }

    /// <summary>
    /// Similarities threshold used in scan.
    /// Stored for reference.
    /// </summary>
    public required float SimilarityThreshold { get; init; }

    /// <summary>
    /// Number of pairwise comparisons performed.
    /// Useful for performance analysis.
    /// </summary>
    public required long PairwiseComparisonsPerformed { get; init; }
}
```

### 4.3 DuplicateGroup Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// A group of entities identified as potential duplicates.
/// </summary>
public record DuplicateGroup
{
    /// <summary>
    /// Unique ID for this duplicate group.
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Entities in this duplicate group.
    /// All share sufficient similarity to be grouped.
    /// </summary>
    public required IReadOnlyList<DuplicateCandidate> Entities { get; init; }

    /// <summary>
    /// Overall confidence that this is a real duplicate group.
    /// Based on pairwise similarities within group (0.0 to 1.0).
    /// </summary>
    public required float GroupConfidence { get; init; }

    /// <summary>
    /// The type of duplication detected.
    /// Helps explain why entities were grouped.
    /// </summary>
    public required DuplicateType DuplicateType { get; init; }

    /// <summary>
    /// ID of the entity recommended as primary.
    /// The entity to merge others into.
    /// Null if no clear winner.
    /// </summary>
    public Guid? SuggestedPrimaryId { get; init; }

    /// <summary>
    /// Human-readable reason for the suggestion.
    /// Example: "Most referenced entity" or "Earliest created"
    /// </summary>
    public string? SuggestionReason { get; init; }

    /// <summary>
    /// Status of this group (Unreviewed, Approved, Rejected, Merged).
    /// </summary>
    public DuplicateGroupStatus Status { get; init; } = DuplicateGroupStatus.Unreviewed;

    /// <summary>
    /// User ID who last reviewed/actioned this group.
    /// </summary>
    public Guid? ReviewedBy { get; init; }

    /// <summary>
    /// Timestamp when group was last reviewed.
    /// </summary>
    public DateTimeOffset? ReviewedAt { get; init; }

    /// <summary>
    /// Notes from reviewer (approval reason or rejection reason).
    /// </summary>
    public string? ReviewNotes { get; init; }
}
```

### 4.4 DuplicateCandidate Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// A candidate entity identified as potential duplicate.
/// </summary>
public record DuplicateCandidate
{
    /// <summary>
    /// Unique ID of this entity.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Display name of the entity.
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Entity type (e.g., "Endpoint", "Service").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Number of relationships this entity participates in.
    /// High values suggest central, important entities.
    /// </summary>
    public required int RelationshipCount { get; init; }

    /// <summary>
    /// Number of claims made about this entity.
    /// Indicates how well-documented the entity is.
    /// </summary>
    public required int ClaimCount { get; init; }

    /// <summary>
    /// Number of documents referencing this entity.
    /// Higher = more established entity.
    /// </summary>
    public required int DocumentCount { get; init; }

    /// <summary>
    /// When this entity was created.
    /// Earlier creation may indicate priority in merge.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When this entity was last modified.
    /// Recently updated entities may be more authoritative.
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// Optional aliases for this entity.
    /// Used in duplicate detection.
    /// </summary>
    public IReadOnlyList<string>? Aliases { get; init; }
}
```

### 4.5 DuplicateType Enum

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Categorizes the type of duplication detected.
/// Explains why entities were grouped together.
/// </summary>
public enum DuplicateType
{
    /// <summary>
    /// Entities have identical names (case-insensitive).
    /// Highest confidence for duplication.
    /// </summary>
    ExactName = 1,

    /// <summary>
    /// Entity names are very similar (fuzzy match).
    /// Suggests typo or variant naming.
    /// </summary>
    SimilarName = 2,

    /// <summary>
    /// One entity name is in another's aliases.
    /// Clear indication of duplication.
    /// </summary>
    AliasMatch = 3,

    /// <summary>
    /// Entities have similar properties and claims.
    /// Suggests same real-world concept with different names.
    /// </summary>
    ContentSimilarity = 4,

    /// <summary>
    /// Entities participate in similar relationships.
    /// Suggests same role/purpose in knowledge graph.
    /// </summary>
    RelationshipPattern = 5,

    /// <summary>
    /// Same entity appears in different documents.
    /// Suggests incomplete cross-document linking.
    /// </summary>
    CrossDocument = 6
}
```

### 4.6 DuplicateGroupStatus Enum

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Status of a duplicate group in resolution workflow.
/// </summary>
public enum DuplicateGroupStatus
{
    /// <summary>Group discovered but not yet reviewed.</summary>
    Unreviewed = 1,

    /// <summary>Group reviewed and approved for merging.</summary>
    Approved = 2,

    /// <summary>Group reviewed and rejected (not duplicates).</summary>
    Rejected = 3,

    /// <summary>Group has been merged.</summary>
    Merged = 4,

    /// <summary>Group was merged but then unmerged.</summary>
    Unmerged = 5
}
```

### 4.7 DuplicateScanOptions Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Configuration options for duplicate scanning.
/// </summary>
public record DuplicateScanOptions
{
    /// <summary>
    /// Minimum similarity score for grouping (default: 0.75).
    /// Entities below this are not grouped.
    /// Higher = stricter, fewer false positives.
    /// </summary>
    public float SimilarityThreshold { get; init; } = 0.75f;

    /// <summary>
    /// Entity type filter (optional).
    /// If set, only scan entities of this type.
    /// </summary>
    public string? EntityTypeFilter { get; init; }

    /// <summary>
    /// Maximum entities to scan (optional).
    /// If set, scan stops after this many entities.
    /// Useful for large graphs.
    /// </summary>
    public int? MaxEntitiesToScan { get; init; }

    /// <summary>
    /// Enable name similarity checking (default: true).
    /// </summary>
    public bool CheckNameSimilarity { get; init; } = true;

    /// <summary>
    /// Enable property/content similarity checking (default: true).
    /// </summary>
    public bool CheckContentSimilarity { get; init; } = true;

    /// <summary>
    /// Enable relationship pattern analysis (default: true).
    /// </summary>
    public bool CheckRelationshipPatterns { get; init; } = true;

    /// <summary>
    /// Enable cross-document analysis (default: true).
    /// </summary>
    public bool CheckCrossDocumentOccurrences { get; init; } = true;

    /// <summary>
    /// Skip entities created in last N minutes (optional).
    /// Avoids flagging recently created entities as duplicates.
    /// </summary>
    public int? SkipRecentlyCreatedMinutes { get; init; }

    /// <summary>
    /// Run scan in background (default: true).
    /// If false, scan is synchronous.
    /// </summary>
    public bool RunInBackground { get; init; } = true;
}
```

### 4.8 DuplicateGroupFilter Record

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Filtering and pagination options for retrieving duplicate groups.
/// </summary>
public record DuplicateGroupFilter
{
    /// <summary>
    /// Filter by duplicate type (optional).
    /// If set, only groups of this type are returned.
    /// </summary>
    public DuplicateType? DuplicateTypeFilter { get; init; }

    /// <summary>
    /// Filter by group status (optional).
    /// If set, only groups with this status are returned.
    /// </summary>
    public DuplicateGroupStatus? StatusFilter { get; init; }

    /// <summary>
    /// Minimum group confidence (default: 0.0).
    /// Groups below this threshold are excluded.
    /// </summary>
    public float MinConfidence { get; init; } = 0.0f;

    /// <summary>
    /// Entity type filter (optional).
    /// If set, only groups containing this entity type.
    /// </summary>
    public string? EntityTypeFilter { get; init; }

    /// <summary>
    /// Page number for pagination (default: 1).
    /// First page is 1.
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Number of groups per page (default: 20).
    /// Must be between 1 and 100.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Sort order (default: ConfidenceDescending).
    /// </summary>
    public DuplicateGroupSortOrder SortOrder { get; init; } = DuplicateGroupSortOrder.ConfidenceDescending;

    /// <summary>
    /// Search term (optional).
    /// If set, filter groups by entity name or type containing this term.
    /// </summary>
    public string? SearchTerm { get; init; }
}
```

### 4.9 DuplicateGroupSortOrder Enum

```csharp
namespace Lexichord.Modules.CKVS.EntityResolution.Contracts;

/// <summary>
/// Sort order for duplicate groups.
/// </summary>
public enum DuplicateGroupSortOrder
{
    /// <summary>Highest confidence first.</summary>
    ConfidenceDescending = 1,

    /// <summary>Lowest confidence first.</summary>
    ConfidenceAscending = 2,

    /// <summary>Newest groups first (most recently found).</summary>
    MostRecent = 3,

    /// <summary>Oldest groups first.</summary>
    Oldest = 4,

    /// <summary>Most entities in group first.</summary>
    SizeDescending = 5,

    /// <summary>Fewest entities in group first.</summary>
    SizeAscending = 6
}
```

---

## 5. Implementation Details

### 5.1 ScanForDuplicatesAsync Flow

```
Input: DuplicateScanOptions
  ↓
1. Load all entities from IGraphRepository
   - Apply type filter if specified
   - Apply recent creation filter if specified
   - Cap to MaxEntitiesToScan if specified
  ↓
2. Build pairwise comparison queue:
   - Total pairs = N × (N-1) / 2
   - Example: 1000 entities = 499,500 pairs
  ↓
3. For each pair (EntityA, EntityB):
   - Calculate NameSimilarity (if CheckNameSimilarity)
   - Calculate ContentSimilarity (if CheckContentSimilarity)
   - Calculate RelationshipSimilarity (if CheckRelationshipPatterns)
   - Calculate CrossDocumentScore (if CheckCrossDocumentOccurrences)
   ↓
4. Composite Similarity Score:
   - TotalScore = (0.4 × NameSim) + (0.3 × ContentSim) +
                  (0.2 × RelationshipSim) + (0.1 × CrossDocSim)
  ↓
5. Group entities by similarity:
   - Entities scoring >= SimilarityThreshold are grouped
   - Use union-find algorithm for transitive grouping
   - Example: If A~B and B~C, all three in one group
  ↓
6. For each group:
   - Calculate GroupConfidence = avg pairwise similarity
   - Detect DuplicateType (exact name → similar → content)
   - Suggest primary entity (most refs, earliest created)
   - Create DuplicateGroup record
  ↓
7. Store groups and publish ScanCompleted event
  ↓
Output: DuplicateScanResult
```

### 5.2 Similarity Calculation Details

#### 5.2.1 Name Similarity

```csharp
public float CalculateNameSimilarity(string name1, string name2)
{
    // Exact match (case-insensitive)
    if (name1.Equals(name2, StringComparison.OrdinalIgnoreCase))
        return 1.0f;

    // Check if one is substring of other
    if (name1.Contains(name2, StringComparison.OrdinalIgnoreCase) ||
        name2.Contains(name1, StringComparison.OrdinalIgnoreCase))
        return 0.9f;

    // Levenshtein distance
    int distance = LevenshteinDistance(name1.ToLower(), name2.ToLower());
    int maxLength = Math.Max(name1.Length, name2.Length);
    float levenSimilarity = 1.0f - (distance / (float)maxLength);

    // Jaro-Winkler similarity
    float jaroWinkler = JaroWinklerSimilarity(name1.ToLower(), name2.ToLower());

    // Return weighted average
    return (0.6f * levenSimilarity) + (0.4f * jaroWinkler);
}
```

#### 5.2.2 Content Similarity

```csharp
public float CalculateContentSimilarity(DuplicateCandidate entity1, DuplicateCandidate entity2)
{
    // Type match
    float typeScore = entity1.EntityType == entity2.EntityType ? 1.0f : 0.0f;

    // Property overlap (claims count similarity)
    int maxClaims = Math.Max(entity1.ClaimCount, entity2.ClaimCount);
    float claimScore = maxClaims > 0
        ? Math.Min(entity1.ClaimCount, entity2.ClaimCount) / (float)maxClaims
        : 0.5f;

    // Document reference overlap
    int maxDocs = Math.Max(entity1.DocumentCount, entity2.DocumentCount);
    float docScore = maxDocs > 0
        ? Math.Min(entity1.DocumentCount, entity2.DocumentCount) / (float)maxDocs
        : 0.5f;

    return (0.4f * typeScore) + (0.3f * claimScore) + (0.3f * docScore);
}
```

#### 5.2.3 Relationship Pattern Similarity

```csharp
public float CalculateRelationshipSimilarity(Guid id1, Guid id2)
{
    var rels1 = _graphRepository.GetRelationships(id1);
    var rels2 = _graphRepository.GetRelationships(id2);

    if (rels1.Count == 0 || rels2.Count == 0)
        return 0.0f;

    // Find common targets
    var targets1 = rels1.Select(r => r.TargetId).ToHashSet();
    var targets2 = rels2.Select(r => r.TargetId).ToHashSet();

    targets1.IntersectWith(targets2);
    int commonTargets = targets1.Count;

    // Jaccard similarity
    return commonTargets / (float)(rels1.Count + rels2.Count - commonTargets);
}
```

---

## 6. Error Handling

### 6.1 Large Graph Performance

**Scenario:** Scanning graph with 100K entities.

**Handling:**
- Use MaxEntitiesToScan to limit scope
- Run in background if RunInBackground = true
- Publish progress events every 1000 comparisons
- Allow scan cancellation via CancellationToken

**Code:**
```csharp
if (options.RunInBackground)
{
    _ = Task.Run(() => ScanInBackgroundAsync(options, ct), ct);
    return new DuplicateScanResult { /* stub */ };
}
```

### 6.2 Invalid Threshold

**Scenario:** SimilarityThreshold not between 0.0 and 1.0.

**Handling:**
- Validate in ScanForDuplicatesAsync
- Throw ArgumentException with clear message

**Code:**
```csharp
if (options.SimilarityThreshold < 0 || options.SimilarityThreshold > 1.0)
    throw new ArgumentException(
        "SimilarityThreshold must be between 0.0 and 1.0",
        nameof(options));
```

### 6.3 Scan Timeout

**Scenario:** Scan takes longer than expected.

**Handling:**
- Implement timeout handling via CancellationToken
- Return partial results on timeout
- Log warning with entities processed so far

---

## 7. Testing

### 7.1 Unit Tests

```csharp
[TestClass]
public class DuplicateDetectorTests
{
    private DuplicateDetector _detector;
    private Mock<IGraphRepository> _graphRepositoryMock;
    private Mock<IEntityBrowser> _entityBrowserMock;

    [TestInitialize]
    public void Setup()
    {
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _entityBrowserMock = new Mock<IEntityBrowser>();

        _detector = new DuplicateDetector(
            _graphRepositoryMock.Object,
            _entityBrowserMock.Object);
    }

    [TestMethod]
    public void CalculateNameSimilarity_ExactMatch_ReturnsOne()
    {
        var similarity = _detector.CalculateNameSimilarity("User", "user");
        Assert.AreEqual(1.0f, similarity, 0.01f);
    }

    [TestMethod]
    public void CalculateNameSimilarity_MinorTypo_ReturnHighScore()
    {
        var similarity = _detector.CalculateNameSimilarity("UserEntity", "UserEntitiy");
        Assert.IsTrue(similarity > 0.85f);
    }

    [TestMethod]
    public void CalculateNameSimilarity_Unrelated_ReturnLowScore()
    {
        var similarity = _detector.CalculateNameSimilarity("User", "Service");
        Assert.IsTrue(similarity < 0.5f);
    }

    [TestMethod]
    public async Task FindDuplicatesOfAsync_WithExactNameMatch_ReturnsCandidates()
    {
        var entityId = Guid.NewGuid();
        var candidates = new List<DuplicateCandidate>
        {
            new()
            {
                EntityId = Guid.NewGuid(),
                EntityName = "User",
                EntityType = "Entity",
                RelationshipCount = 10,
                ClaimCount = 5,
                DocumentCount = 3,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
                LastModified = DateTimeOffset.UtcNow
            }
        };

        // Setup mock to return entity and candidates
        _entityBrowserMock.Setup(x => x.GetAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entity { EntityId = entityId, Name = "User", Type = "Entity" });

        var results = await _detector.FindDuplicatesOfAsync(entityId);

        Assert.IsTrue(results.Count > 0);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task ScanForDuplicatesAsync_WithInvalidThreshold_ThrowsArgumentException()
    {
        var invalidOptions = new DuplicateScanOptions
        {
            SimilarityThreshold = 1.5f
        };

        await _detector.ScanForDuplicatesAsync(invalidOptions);
    }

    [TestMethod]
    public async Task GetDuplicateGroupsAsync_WithConfidenceFilter_ReturnsFilteredGroups()
    {
        var filter = new DuplicateGroupFilter
        {
            MinConfidence = 0.85f,
            PageNumber = 1,
            PageSize = 10
        };

        var results = await _detector.GetDuplicateGroupsAsync(filter);

        Assert.IsTrue(results.All(g => g.GroupConfidence >= 0.85f));
    }
}
```

### 7.2 Integration Tests

```csharp
[TestClass]
public class DuplicateDetectorIntegrationTests
{
    [TestMethod]
    public async Task E2E_ScanForDuplicates_IdentifiesExactNameMatches()
    {
        // Create test entities with exact name match
        var entity1 = new Entity { Name = "User", Type = "Entity" };
        var entity2 = new Entity { Name = "user", Type = "Entity" };

        // Run scan
        var options = new DuplicateScanOptions();
        var result = await _detector.ScanForDuplicatesAsync(options);

        // Verify group created
        Assert.IsTrue(result.Groups.Any(g =>
            g.DuplicateType == DuplicateType.ExactName &&
            g.GroupConfidence >= 0.95f));
    }

    [TestMethod]
    public async Task E2E_ScanForDuplicates_IdentifiesSimilarNames()
    {
        // Create test entities with similar names (typo)
        var entity1 = new Entity { Name = "UserService", Type = "Service" };
        var entity2 = new Entity { Name = "UserServise", Type = "Service" };

        var options = new DuplicateScanOptions();
        var result = await _detector.ScanForDuplicatesAsync(options);

        Assert.IsTrue(result.Groups.Any(g =>
            g.DuplicateType == DuplicateType.SimilarName));
    }
}
```

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| ScanForDuplicatesAsync (1K entities) | <30s (P95) | Parallel pairwise comparison, early stopping |
| FindDuplicatesOfAsync | <500ms (P95) | Index-based lookup |
| GetDuplicateGroupsAsync | <100ms (P95) | Paginated result set |
| Single similarity calc | <1ms | Vectorized operations |

### 8.1 Optimization Strategy

- Use parallel processing for pairwise comparisons
- Cache entity index for fast lookups
- Early stopping if score falls below threshold
- Batch database queries
- Use union-find for efficient grouping

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| False positive duplicates | High | Conservative thresholds, manual review before merge |
| Performance DoS | Medium | Scan limits, cancellation support, rate limiting |
| Incorrect grouping | Medium | Multi-algorithm approach, threshold calibration |

---

## 10. License Gating

```csharp
public class DuplicateDetector : IDuplicateDetector
{
    private readonly ILicenseContext _licenseContext;

    public async Task<DuplicateScanResult> ScanForDuplicatesAsync(
        DuplicateScanOptions options,
        CancellationToken ct = default)
    {
        if (!_licenseContext.IsAvailable(LicenseTier.Teams))
            throw new LicenseRequiredException("Duplicate detection requires Teams tier");

        // Implementation...
    }
}
```

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Two entities with exact name (different case) | ScanForDuplicatesAsync | Grouped as ExactName duplicates |
| 2 | Two entities with similar names (typo) | ScanForDuplicatesAsync | Grouped as SimilarName duplicates |
| 3 | Entity with 10 relationships and 5 claims | FindDuplicatesOfAsync | Similar entities with comparable relationships returned |
| 4 | Scan with SimilarityThreshold = 0.85 | ScanForDuplicatesAsync | Only groups >= 0.85 confidence included |
| 5 | 1000 entities | ScanForDuplicatesAsync | Completes in <30s |
| 6 | DuplicateGroupFilter with MinConfidence = 0.9 | GetDuplicateGroupsAsync | All returned groups have confidence >= 0.9 |
| 7 | Invalid threshold (1.5) | ScanForDuplicatesAsync | ArgumentException thrown |
| 8 | Teams license | Any method | Operation succeeds |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - IDuplicateDetector, ScanForDuplicatesAsync, FindDuplicatesOfAsync, similarity algorithms |
