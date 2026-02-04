# LCS-DES-056-KG-i: Claim Diff Service

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-056-KG-i |
| **Feature ID** | KG-056i |
| **Feature Name** | Claim Diff Service |
| **Target Version** | v0.5.6i |
| **Module Scope** | `Lexichord.Knowledge.Claims` |
| **Swimlane** | NLU Pipeline |
| **License Tier** | Teams (full), Enterprise (full) |
| **Feature Gate Key** | `knowledge.claims.diff.enabled` |
| **Status** | Implemented |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

When documents are updated, the extracted claims may change. The **Claim Diff Service** compares claims between document versions to identify what's new, changed, or removed. This enables change tracking, review workflows, and contradiction detection.

### 2.2 The Proposed Solution

Implement a diff service that:

- Compares claim sets between document versions
- Identifies added, removed, and modified claims
- Detects semantic equivalence for fuzzy matching
- Provides detailed diff views with evidence highlighting
- Supports batch diff for multiple documents
- Tracks claim history for audit trails

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.5.6e: Claim data model
- v0.5.6h: `IClaimRepository` — Claim storage

**NuGet Packages:**
- `DiffPlex` — Text diff utilities
- `Microsoft.Extensions.Caching.Memory` — Diff caching

### 3.2 Module Placement

```
Lexichord.Knowledge/
├── Claims/
│   └── Diff/
│       ├── IClaimDiffService.cs
│       ├── ClaimDiffService.cs
│       ├── ClaimDiffResult.cs
│       ├── ClaimChange.cs
│       └── SemanticMatcher.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Load with claim extraction
- **Fallback Experience:** WriterPro: view-only; Teams+: full diff

---

## 4. Data Contract (The API)

### 4.1 Core Interface

```csharp
namespace Lexichord.Knowledge.Claims.Diff;

/// <summary>
/// Service for comparing claims between document versions.
/// </summary>
public interface IClaimDiffService
{
    /// <summary>
    /// Compares claims between two sets (old and new).
    /// </summary>
    /// <param name="oldClaims">Claims from old version.</param>
    /// <param name="newClaims">Claims from new version.</param>
    /// <param name="options">Diff options.</param>
    /// <returns>Diff result with changes.</returns>
    ClaimDiffResult Diff(
        IReadOnlyList<Claim> oldClaims,
        IReadOnlyList<Claim> newClaims,
        DiffOptions? options = null);

    /// <summary>
    /// Compares claims between document versions.
    /// </summary>
    /// <param name="documentId">Document ID.</param>
    /// <param name="oldVersion">Old version number or timestamp.</param>
    /// <param name="newVersion">New version number or timestamp.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Diff result.</returns>
    Task<ClaimDiffResult> DiffDocumentVersionsAsync(
        Guid documentId,
        int oldVersion,
        int newVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Compares current claims to a baseline snapshot.
    /// </summary>
    Task<ClaimDiffResult> DiffFromBaselineAsync(
        Guid documentId,
        Guid baselineSnapshotId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a baseline snapshot of current claims.
    /// </summary>
    Task<ClaimSnapshot> CreateSnapshotAsync(
        Guid documentId,
        string? label = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets claim history for a document.
    /// </summary>
    Task<IReadOnlyList<ClaimHistoryEntry>> GetHistoryAsync(
        Guid documentId,
        int limit = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Finds potential contradictions across documents.
    /// </summary>
    Task<IReadOnlyList<ClaimContradiction>> FindContradictionsAsync(
        Guid projectId,
        CancellationToken ct = default);
}

/// <summary>
/// Options for claim diff.
/// </summary>
public record DiffOptions
{
    /// <summary>Use semantic matching for fuzzy comparison.</summary>
    public bool UseSemanticMatching { get; init; } = true;

    /// <summary>Minimum similarity for semantic match (0.0-1.0).</summary>
    public float SemanticMatchThreshold { get; init; } = 0.8f;

    /// <summary>Ignore confidence changes below this threshold.</summary>
    public float IgnoreConfidenceChangeBelow { get; init; } = 0.05f;

    /// <summary>Consider validation status changes.</summary>
    public bool TrackValidationChanges { get; init; } = true;

    /// <summary>Include evidence text in changes.</summary>
    public bool IncludeEvidence { get; init; } = true;

    /// <summary>Group related changes together.</summary>
    public bool GroupRelatedChanges { get; init; } = true;
}
```

### 4.2 ClaimDiffResult

```csharp
namespace Lexichord.Knowledge.Claims.Diff;

/// <summary>
/// Result of comparing claims.
/// </summary>
public record ClaimDiffResult
{
    /// <summary>Claims added in new version.</summary>
    public required IReadOnlyList<ClaimChange> Added { get; init; }

    /// <summary>Claims removed from old version.</summary>
    public required IReadOnlyList<ClaimChange> Removed { get; init; }

    /// <summary>Claims modified between versions.</summary>
    public required IReadOnlyList<ClaimModification> Modified { get; init; }

    /// <summary>Claims unchanged.</summary>
    public required IReadOnlyList<Claim> Unchanged { get; init; }

    /// <summary>Total claims in old version.</summary>
    public int OldClaimCount { get; init; }

    /// <summary>Total claims in new version.</summary>
    public int NewClaimCount { get; init; }

    /// <summary>Summary statistics.</summary>
    public DiffStats Stats { get; init; } = new();

    /// <summary>Whether there are any changes.</summary>
    public bool HasChanges => Added.Any() || Removed.Any() || Modified.Any();

    /// <summary>Change groups (if grouped).</summary>
    public IReadOnlyList<ClaimChangeGroup>? Groups { get; init; }

    /// <summary>Creates an empty diff (no changes).</summary>
    public static ClaimDiffResult NoChanges(IReadOnlyList<Claim> claims) => new()
    {
        Added = Array.Empty<ClaimChange>(),
        Removed = Array.Empty<ClaimChange>(),
        Modified = Array.Empty<ClaimModification>(),
        Unchanged = claims,
        OldClaimCount = claims.Count,
        NewClaimCount = claims.Count
    };
}

/// <summary>
/// Diff statistics.
/// </summary>
public record DiffStats
{
    public int AddedCount { get; init; }
    public int RemovedCount { get; init; }
    public int ModifiedCount { get; init; }
    public int UnchangedCount { get; init; }
    public int TotalChanges => AddedCount + RemovedCount + ModifiedCount;

    public IReadOnlyDictionary<string, int>? ChangesByPredicate { get; init; }
    public IReadOnlyDictionary<ClaimChangeType, int>? ChangesByType { get; init; }
}

/// <summary>
/// Types of claim changes.
/// </summary>
public enum ClaimChangeType
{
    /// <summary>Claim was added.</summary>
    Added,

    /// <summary>Claim was removed.</summary>
    Removed,

    /// <summary>Claim subject changed.</summary>
    SubjectChanged,

    /// <summary>Claim predicate changed.</summary>
    PredicateChanged,

    /// <summary>Claim object changed.</summary>
    ObjectChanged,

    /// <summary>Claim confidence changed significantly.</summary>
    ConfidenceChanged,

    /// <summary>Claim validation status changed.</summary>
    ValidationChanged,

    /// <summary>Claim evidence/location changed.</summary>
    EvidenceChanged
}
```

### 4.3 ClaimChange and ClaimModification

```csharp
namespace Lexichord.Knowledge.Claims.Diff;

/// <summary>
/// A claim change (added or removed).
/// </summary>
public record ClaimChange
{
    /// <summary>The claim that changed.</summary>
    public required Claim Claim { get; init; }

    /// <summary>Type of change.</summary>
    public required ClaimChangeType ChangeType { get; init; }

    /// <summary>Human-readable description.</summary>
    public required string Description { get; init; }

    /// <summary>Related evidence text.</summary>
    public string? Evidence { get; init; }

    /// <summary>Impact assessment.</summary>
    public ChangeImpact Impact { get; init; }

    /// <summary>Matched claim (for semantic matches).</summary>
    public Claim? MatchedClaim { get; init; }

    /// <summary>Match similarity (if matched).</summary>
    public float? MatchSimilarity { get; init; }
}

/// <summary>
/// A modification to an existing claim.
/// </summary>
public record ClaimModification
{
    /// <summary>Old claim version.</summary>
    public required Claim OldClaim { get; init; }

    /// <summary>New claim version.</summary>
    public required Claim NewClaim { get; init; }

    /// <summary>Types of changes detected.</summary>
    public required IReadOnlyList<ClaimChangeType> ChangeTypes { get; init; }

    /// <summary>Detailed field changes.</summary>
    public required IReadOnlyList<FieldChange> FieldChanges { get; init; }

    /// <summary>Human-readable description.</summary>
    public required string Description { get; init; }

    /// <summary>Impact assessment.</summary>
    public ChangeImpact Impact { get; init; }

    /// <summary>Whether this is a semantic match (not exact ID match).</summary>
    public bool IsSemanticMatch { get; init; }

    /// <summary>Similarity score (for semantic matches).</summary>
    public float Similarity { get; init; } = 1.0f;
}

/// <summary>
/// Change to a specific field.
/// </summary>
public record FieldChange
{
    /// <summary>Field name.</summary>
    public required string FieldName { get; init; }

    /// <summary>Old value.</summary>
    public object? OldValue { get; init; }

    /// <summary>New value.</summary>
    public object? NewValue { get; init; }

    /// <summary>Change description.</summary>
    public required string Description { get; init; }
}

/// <summary>
/// Impact level of a change.
/// </summary>
public enum ChangeImpact
{
    /// <summary>Low impact - minor changes.</summary>
    Low,

    /// <summary>Medium impact - notable changes.</summary>
    Medium,

    /// <summary>High impact - significant changes.</summary>
    High,

    /// <summary>Critical impact - potential breaking changes.</summary>
    Critical
}

/// <summary>
/// Group of related changes.
/// </summary>
public record ClaimChangeGroup
{
    /// <summary>Group identifier.</summary>
    public required string GroupId { get; init; }

    /// <summary>Group label (e.g., entity name).</summary>
    public required string Label { get; init; }

    /// <summary>Changes in this group.</summary>
    public required IReadOnlyList<ClaimChange> Changes { get; init; }

    /// <summary>Modifications in this group.</summary>
    public required IReadOnlyList<ClaimModification> Modifications { get; init; }

    /// <summary>Maximum impact in group.</summary>
    public ChangeImpact MaxImpact { get; init; }
}
```

### 4.4 History and Contradictions

```csharp
namespace Lexichord.Knowledge.Claims.Diff;

/// <summary>
/// Snapshot of claims at a point in time.
/// </summary>
public record ClaimSnapshot
{
    /// <summary>Snapshot ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Document ID.</summary>
    public Guid DocumentId { get; init; }

    /// <summary>Label for the snapshot.</summary>
    public string? Label { get; init; }

    /// <summary>Number of claims in snapshot.</summary>
    public int ClaimCount { get; init; }

    /// <summary>When the snapshot was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Claim IDs in the snapshot.</summary>
    public IReadOnlyList<Guid>? ClaimIds { get; init; }
}

/// <summary>
/// Entry in claim history.
/// </summary>
public record ClaimHistoryEntry
{
    /// <summary>Entry ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Document ID.</summary>
    public Guid DocumentId { get; init; }

    /// <summary>Action type.</summary>
    public ClaimHistoryAction Action { get; init; }

    /// <summary>Claim ID (if single claim).</summary>
    public Guid? ClaimId { get; init; }

    /// <summary>Number of claims affected.</summary>
    public int AffectedCount { get; init; }

    /// <summary>Description of the change.</summary>
    public string? Description { get; init; }

    /// <summary>When the action occurred.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>User who made the change.</summary>
    public string? UserId { get; init; }
}

public enum ClaimHistoryAction
{
    Created,
    Updated,
    Deleted,
    Validated,
    Reviewed,
    BulkExtraction,
    SnapshotCreated
}

/// <summary>
/// A potential contradiction between claims.
/// </summary>
public record ClaimContradiction
{
    /// <summary>Contradiction ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>First claim.</summary>
    public required Claim Claim1 { get; init; }

    /// <summary>Second claim.</summary>
    public required Claim Claim2 { get; init; }

    /// <summary>Type of contradiction.</summary>
    public ContradictionType Type { get; init; }

    /// <summary>Confidence that this is a contradiction (0.0-1.0).</summary>
    public float Confidence { get; init; }

    /// <summary>Description of the contradiction.</summary>
    public required string Description { get; init; }

    /// <summary>Suggested resolution.</summary>
    public string? SuggestedResolution { get; init; }

    /// <summary>When detected.</summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Resolution status.</summary>
    public ContradictionStatus Status { get; init; } = ContradictionStatus.Open;
}

public enum ContradictionType
{
    /// <summary>Same subject, same predicate, different objects.</summary>
    DirectContradiction,

    /// <summary>Conflicting deprecation status.</summary>
    DeprecationConflict,

    /// <summary>Conflicting requirement claims.</summary>
    RequirementConflict,

    /// <summary>Value out of expected range.</summary>
    ValueConflict,

    /// <summary>Temporal inconsistency.</summary>
    TemporalConflict
}

public enum ContradictionStatus
{
    Open,
    UnderReview,
    Resolved,
    Ignored
}
```

---

## 5. Implementation Logic

### 5.1 ClaimDiffService

```csharp
namespace Lexichord.Knowledge.Claims.Diff;

/// <summary>
/// Service for comparing claims.
/// </summary>
public class ClaimDiffService : IClaimDiffService
{
    private readonly IClaimRepository _repository;
    private readonly SemanticMatcher _semanticMatcher;
    private readonly ILogger<ClaimDiffService> _logger;

    public ClaimDiffResult Diff(
        IReadOnlyList<Claim> oldClaims,
        IReadOnlyList<Claim> newClaims,
        DiffOptions? options = null)
    {
        options ??= new DiffOptions();

        var oldById = oldClaims.ToDictionary(c => c.Id);
        var newById = newClaims.ToDictionary(c => c.Id);

        var added = new List<ClaimChange>();
        var removed = new List<ClaimChange>();
        var modified = new List<ClaimModification>();
        var unchanged = new List<Claim>();

        // Find removed claims
        foreach (var oldClaim in oldClaims)
        {
            if (!newById.ContainsKey(oldClaim.Id))
            {
                // Check for semantic match in new claims
                Claim? semanticMatch = null;
                float matchSimilarity = 0;

                if (options.UseSemanticMatching)
                {
                    (semanticMatch, matchSimilarity) = _semanticMatcher.FindMatch(
                        oldClaim, newClaims, options.SemanticMatchThreshold);
                }

                if (semanticMatch != null)
                {
                    // Moved/modified claim
                    var modification = CreateModification(oldClaim, semanticMatch, options);
                    modification = modification with { IsSemanticMatch = true, Similarity = matchSimilarity };
                    modified.Add(modification);
                }
                else
                {
                    removed.Add(new ClaimChange
                    {
                        Claim = oldClaim,
                        ChangeType = ClaimChangeType.Removed,
                        Description = $"Removed: {oldClaim.ToCanonicalForm()}",
                        Evidence = options.IncludeEvidence ? oldClaim.Evidence.Sentence : null,
                        Impact = AssessImpact(oldClaim, ClaimChangeType.Removed)
                    });
                }
            }
        }

        // Find added and modified claims
        foreach (var newClaim in newClaims)
        {
            if (!oldById.TryGetValue(newClaim.Id, out var oldClaim))
            {
                // Check if this was already matched semantically
                var alreadyMatched = modified.Any(m =>
                    m.NewClaim.Id == newClaim.Id);

                if (!alreadyMatched)
                {
                    added.Add(new ClaimChange
                    {
                        Claim = newClaim,
                        ChangeType = ClaimChangeType.Added,
                        Description = $"Added: {newClaim.ToCanonicalForm()}",
                        Evidence = options.IncludeEvidence ? newClaim.Evidence.Sentence : null,
                        Impact = AssessImpact(newClaim, ClaimChangeType.Added)
                    });
                }
            }
            else
            {
                // Same ID - check for modifications
                if (HasSignificantChanges(oldClaim, newClaim, options))
                {
                    modified.Add(CreateModification(oldClaim, newClaim, options));
                }
                else
                {
                    unchanged.Add(newClaim);
                }
            }
        }

        var result = new ClaimDiffResult
        {
            Added = added,
            Removed = removed,
            Modified = modified,
            Unchanged = unchanged,
            OldClaimCount = oldClaims.Count,
            NewClaimCount = newClaims.Count,
            Stats = new DiffStats
            {
                AddedCount = added.Count,
                RemovedCount = removed.Count,
                ModifiedCount = modified.Count,
                UnchangedCount = unchanged.Count,
                ChangesByPredicate = ComputeChangesByPredicate(added, removed, modified)
            }
        };

        // Group changes if requested
        if (options.GroupRelatedChanges)
        {
            result = result with { Groups = GroupChanges(result) };
        }

        return result;
    }

    public async Task<ClaimDiffResult> DiffDocumentVersionsAsync(
        Guid documentId,
        int oldVersion,
        int newVersion,
        CancellationToken ct = default)
    {
        // Get claims from each version
        var oldClaims = await _repository.SearchAsync(new ClaimSearchCriteria
        {
            DocumentId = documentId
            // Note: Version filtering would need document versioning integration
        }, ct);

        var newClaims = await _repository.GetByDocumentAsync(documentId, ct);

        return Diff(oldClaims.Claims, newClaims);
    }

    public async Task<IReadOnlyList<ClaimContradiction>> FindContradictionsAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        var contradictions = new List<ClaimContradiction>();

        // Get all active claims for project
        var claims = await _repository.SearchAsync(new ClaimSearchCriteria
        {
            ProjectId = projectId,
            IsActive = true
        }, ct);

        // Group by subject entity
        var bySubject = claims.Claims
            .Where(c => c.Subject.EntityId.HasValue)
            .GroupBy(c => c.Subject.EntityId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (subjectId, subjectClaims) in bySubject)
        {
            // Find claims with same predicate but different objects
            var byPredicate = subjectClaims.GroupBy(c => c.Predicate);

            foreach (var predicateGroup in byPredicate)
            {
                var claimsList = predicateGroup.ToList();
                if (claimsList.Count < 2) continue;

                // Check for contradictions
                for (int i = 0; i < claimsList.Count; i++)
                {
                    for (int j = i + 1; j < claimsList.Count; j++)
                    {
                        var contradiction = CheckContradiction(claimsList[i], claimsList[j]);
                        if (contradiction != null)
                        {
                            contradictions.Add(contradiction);
                        }
                    }
                }
            }
        }

        return contradictions;
    }

    private ClaimModification CreateModification(
        Claim oldClaim,
        Claim newClaim,
        DiffOptions options)
    {
        var changeTypes = new List<ClaimChangeType>();
        var fieldChanges = new List<FieldChange>();

        // Check subject changes
        if (oldClaim.Subject.SurfaceForm != newClaim.Subject.SurfaceForm ||
            oldClaim.Subject.EntityId != newClaim.Subject.EntityId)
        {
            changeTypes.Add(ClaimChangeType.SubjectChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "Subject",
                OldValue = oldClaim.Subject.SurfaceForm,
                NewValue = newClaim.Subject.SurfaceForm,
                Description = $"Subject changed from '{oldClaim.Subject.SurfaceForm}' to '{newClaim.Subject.SurfaceForm}'"
            });
        }

        // Check object changes
        if (!ObjectsEqual(oldClaim.Object, newClaim.Object))
        {
            changeTypes.Add(ClaimChangeType.ObjectChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "Object",
                OldValue = oldClaim.Object.ToCanonicalForm(),
                NewValue = newClaim.Object.ToCanonicalForm(),
                Description = $"Object changed from '{oldClaim.Object.ToCanonicalForm()}' to '{newClaim.Object.ToCanonicalForm()}'"
            });
        }

        // Check confidence changes
        if (Math.Abs(oldClaim.Confidence - newClaim.Confidence) >= options.IgnoreConfidenceChangeBelow)
        {
            changeTypes.Add(ClaimChangeType.ConfidenceChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "Confidence",
                OldValue = oldClaim.Confidence,
                NewValue = newClaim.Confidence,
                Description = $"Confidence changed from {oldClaim.Confidence:P0} to {newClaim.Confidence:P0}"
            });
        }

        // Check validation status
        if (options.TrackValidationChanges && oldClaim.ValidationStatus != newClaim.ValidationStatus)
        {
            changeTypes.Add(ClaimChangeType.ValidationChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "ValidationStatus",
                OldValue = oldClaim.ValidationStatus,
                NewValue = newClaim.ValidationStatus,
                Description = $"Validation changed from {oldClaim.ValidationStatus} to {newClaim.ValidationStatus}"
            });
        }

        return new ClaimModification
        {
            OldClaim = oldClaim,
            NewClaim = newClaim,
            ChangeTypes = changeTypes,
            FieldChanges = fieldChanges,
            Description = string.Join("; ", fieldChanges.Select(f => f.Description)),
            Impact = AssessModificationImpact(changeTypes)
        };
    }

    private bool HasSignificantChanges(Claim oldClaim, Claim newClaim, DiffOptions options)
    {
        if (oldClaim.Subject.SurfaceForm != newClaim.Subject.SurfaceForm) return true;
        if (oldClaim.Predicate != newClaim.Predicate) return true;
        if (!ObjectsEqual(oldClaim.Object, newClaim.Object)) return true;

        if (Math.Abs(oldClaim.Confidence - newClaim.Confidence) >= options.IgnoreConfidenceChangeBelow)
            return true;

        if (options.TrackValidationChanges && oldClaim.ValidationStatus != newClaim.ValidationStatus)
            return true;

        return false;
    }

    private bool ObjectsEqual(ClaimObject a, ClaimObject b)
    {
        if (a.Type != b.Type) return false;

        if (a.Type == ClaimObjectType.Entity)
        {
            return a.Entity?.EntityId == b.Entity?.EntityId ||
                   a.Entity?.SurfaceForm == b.Entity?.SurfaceForm;
        }

        return Equals(a.LiteralValue, b.LiteralValue);
    }

    private ClaimContradiction? CheckContradiction(Claim claim1, Claim claim2)
    {
        // Different documents, same subject and predicate
        if (claim1.DocumentId != claim2.DocumentId &&
            claim1.Subject.EntityId == claim2.Subject.EntityId &&
            claim1.Predicate == claim2.Predicate)
        {
            // Check if objects are different
            if (!ObjectsEqual(claim1.Object, claim2.Object))
            {
                return new ClaimContradiction
                {
                    Claim1 = claim1,
                    Claim2 = claim2,
                    Type = ContradictionType.DirectContradiction,
                    Confidence = 0.8f,
                    Description = $"Conflicting claims about {claim1.Subject.SurfaceForm}: " +
                                  $"'{claim1.Object.ToCanonicalForm()}' vs '{claim2.Object.ToCanonicalForm()}'",
                    SuggestedResolution = "Review both documents and reconcile the claims"
                };
            }
        }

        return null;
    }

    private ChangeImpact AssessImpact(Claim claim, ClaimChangeType changeType)
    {
        // Deprecation changes are high impact
        if (claim.Predicate == ClaimPredicate.IS_DEPRECATED)
            return ChangeImpact.High;

        // Requirement changes are high impact
        if (claim.Predicate == ClaimPredicate.REQUIRES)
            return ChangeImpact.High;

        // Type changes are critical
        if (claim.Predicate == ClaimPredicate.HAS_TYPE)
            return ChangeImpact.Critical;

        return changeType switch
        {
            ClaimChangeType.Removed => ChangeImpact.Medium,
            ClaimChangeType.Added => ChangeImpact.Low,
            _ => ChangeImpact.Low
        };
    }
}
```

### 5.2 SemanticMatcher

```csharp
namespace Lexichord.Knowledge.Claims.Diff;

/// <summary>
/// Matches claims by semantic similarity.
/// </summary>
public class SemanticMatcher
{
    public (Claim? Match, float Similarity) FindMatch(
        Claim target,
        IReadOnlyList<Claim> candidates,
        float threshold)
    {
        Claim? bestMatch = null;
        float bestSimilarity = 0;

        foreach (var candidate in candidates)
        {
            // Skip if different predicate
            if (target.Predicate != candidate.Predicate)
                continue;

            var similarity = ComputeSimilarity(target, candidate);

            if (similarity >= threshold && similarity > bestSimilarity)
            {
                bestMatch = candidate;
                bestSimilarity = similarity;
            }
        }

        return (bestMatch, bestSimilarity);
    }

    private float ComputeSimilarity(Claim a, Claim b)
    {
        // Weight factors
        const float subjectWeight = 0.4f;
        const float predicateWeight = 0.2f;
        const float objectWeight = 0.4f;

        // Subject similarity
        float subjectSim = ComputeEntitySimilarity(a.Subject, b.Subject);

        // Predicate similarity (exact match)
        float predicateSim = a.Predicate == b.Predicate ? 1.0f : 0.0f;

        // Object similarity
        float objectSim = ComputeObjectSimilarity(a.Object, b.Object);

        return subjectWeight * subjectSim +
               predicateWeight * predicateSim +
               objectWeight * objectSim;
    }

    private float ComputeEntitySimilarity(ClaimEntity a, ClaimEntity b)
    {
        // Same entity ID = exact match
        if (a.EntityId.HasValue && a.EntityId == b.EntityId)
            return 1.0f;

        // Compare surface forms using Jaro-Winkler
        return JaroWinkler(a.NormalizedForm, b.NormalizedForm);
    }

    private float ComputeObjectSimilarity(ClaimObject a, ClaimObject b)
    {
        if (a.Type != b.Type)
            return 0.0f;

        if (a.Type == ClaimObjectType.Entity)
        {
            return ComputeEntitySimilarity(a.Entity!, b.Entity!);
        }

        // Literal comparison
        if (Equals(a.LiteralValue, b.LiteralValue))
            return 1.0f;

        // String similarity for string literals
        if (a.LiteralType == "string" && b.LiteralType == "string")
        {
            return JaroWinkler(
                a.LiteralValue?.ToString() ?? "",
                b.LiteralValue?.ToString() ?? "");
        }

        return 0.0f;
    }

    private static float JaroWinkler(string s1, string s2)
    {
        // Jaro-Winkler implementation
        // ... (using SimMetrics or similar)
        return new SimMetrics.Net.Metric.JaroWinkler().GetSimilarity(s1, s2);
    }
}
```

---

## 6. Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      Claim Diff Flow                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────┐        ┌─────────────────┐                 │
│  │   Old Claims    │        │   New Claims    │                 │
│  │   (Version N)   │        │  (Version N+1)  │                 │
│  └────────┬────────┘        └────────┬────────┘                 │
│           │                          │                           │
│           └──────────┬───────────────┘                          │
│                      │                                           │
│                      ▼                                           │
│           ┌─────────────────────┐                               │
│           │   Build ID Index    │                               │
│           │   Old: {id → claim} │                               │
│           │   New: {id → claim} │                               │
│           └──────────┬──────────┘                               │
│                      │                                           │
│    ┌─────────────────┼─────────────────┐                        │
│    │                 │                 │                         │
│    ▼                 ▼                 ▼                         │
│ ┌───────────┐  ┌───────────┐  ┌───────────┐                    │
│ │  Find     │  │  Find     │  │  Find     │                    │
│ │  Added    │  │  Removed  │  │  Modified │                    │
│ │           │  │           │  │           │                    │
│ │ In new,   │  │ In old,   │  │ Same ID,  │                    │
│ │ not old   │  │ not new   │  │ different │                    │
│ └─────┬─────┘  └─────┬─────┘  └─────┬─────┘                    │
│       │              │              │                            │
│       │              ▼              │                            │
│       │      ┌─────────────┐        │                            │
│       │      │  Semantic   │        │                            │
│       │      │  Matching   │        │                            │
│       │      │  (optional) │        │                            │
│       │      └──────┬──────┘        │                            │
│       │             │               │                            │
│       └─────────────┼───────────────┘                           │
│                     │                                            │
│                     ▼                                            │
│           ┌─────────────────────┐                               │
│           │  Assess Impact &    │                               │
│           │  Build Descriptions │                               │
│           └──────────┬──────────┘                               │
│                      │                                           │
│                      ▼                                           │
│           ┌─────────────────────┐                               │
│           │  Group Changes      │                               │
│           │  (by entity/doc)    │                               │
│           └──────────┬──────────┘                               │
│                      │                                           │
│                      ▼                                           │
│           ┌─────────────────────┐                               │
│           │   ClaimDiffResult   │                               │
│           │   - Added           │                               │
│           │   - Removed         │                               │
│           │   - Modified        │                               │
│           │   - Unchanged       │                               │
│           │   - Stats           │                               │
│           └─────────────────────┘                               │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6i")]
public class ClaimDiffServiceTests
{
    private readonly IClaimDiffService _service;

    [Fact]
    public void Diff_AddedClaim_DetectedCorrectly()
    {
        // Arrange
        var oldClaims = new[] { CreateClaim("A", "ACCEPTS", "B") };
        var newClaims = new[]
        {
            CreateClaim("A", "ACCEPTS", "B"),
            CreateClaim("C", "RETURNS", "D")
        };

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert
        result.Added.Should().HaveCount(1);
        result.Added[0].Claim.Subject.SurfaceForm.Should().Be("C");
    }

    [Fact]
    public void Diff_RemovedClaim_DetectedCorrectly()
    {
        // Arrange
        var oldClaims = new[]
        {
            CreateClaim("A", "ACCEPTS", "B"),
            CreateClaim("C", "RETURNS", "D")
        };
        var newClaims = new[] { CreateClaim("A", "ACCEPTS", "B") };

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert
        result.Removed.Should().HaveCount(1);
        result.Removed[0].Claim.Subject.SurfaceForm.Should().Be("C");
    }

    [Fact]
    public void Diff_ModifiedClaim_DetectedWithFieldChanges()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var oldClaims = new[] { CreateClaim("A", "ACCEPTS", "B", id: claimId, confidence: 0.8f) };
        var newClaims = new[] { CreateClaim("A", "ACCEPTS", "C", id: claimId, confidence: 0.9f) };

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert
        result.Modified.Should().HaveCount(1);
        result.Modified[0].ChangeTypes.Should().Contain(ClaimChangeType.ObjectChanged);
        result.Modified[0].FieldChanges.Should().Contain(f => f.FieldName == "Object");
    }

    [Fact]
    public void Diff_SemanticMatch_MatchesSimilarClaims()
    {
        // Arrange
        var oldClaims = new[] { CreateClaim("GET /users endpoint", "ACCEPTS", "limit") };
        var newClaims = new[] { CreateClaim("users endpoint", "ACCEPTS", "limit") };
        var options = new DiffOptions
        {
            UseSemanticMatching = true,
            SemanticMatchThreshold = 0.7f
        };

        // Act
        var result = _service.Diff(oldClaims, newClaims, options);

        // Assert
        result.Modified.Should().HaveCount(1);
        result.Modified[0].IsSemanticMatch.Should().BeTrue();
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEmpty();
    }

    [Fact]
    public void Diff_UnchangedClaims_NotIncludedInChanges()
    {
        // Arrange
        var claims = new[]
        {
            CreateClaim("A", "ACCEPTS", "B"),
            CreateClaim("C", "RETURNS", "D")
        };

        // Act
        var result = _service.Diff(claims, claims);

        // Assert
        result.HasChanges.Should().BeFalse();
        result.Unchanged.Should().HaveCount(2);
    }

    [Fact]
    public void Diff_GroupsChanges_BySubject()
    {
        // Arrange
        var oldClaims = Array.Empty<Claim>();
        var newClaims = new[]
        {
            CreateClaim("GET /users", "ACCEPTS", "limit"),
            CreateClaim("GET /users", "RETURNS", "200 OK"),
            CreateClaim("POST /orders", "REQUIRES", "auth")
        };
        var options = new DiffOptions { GroupRelatedChanges = true };

        // Act
        var result = _service.Diff(oldClaims, newClaims, options);

        // Assert
        result.Groups.Should().HaveCount(2); // One for /users, one for /orders
    }

    [Fact]
    public async Task FindContradictionsAsync_DirectContradiction_Detected()
    {
        // Arrange - Same subject, same predicate, different objects
        // Setup claims in repository with contradicting values

        // Act
        var contradictions = await _service.FindContradictionsAsync(Guid.NewGuid());

        // Assert
        contradictions.Should().Contain(c =>
            c.Type == ContradictionType.DirectContradiction);
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Added claims detected when in new but not old. |
| 2 | Removed claims detected when in old but not new. |
| 3 | Modified claims detected with field-level changes. |
| 4 | Semantic matching finds similar claims without same ID. |
| 5 | Change impact assessed correctly per predicate type. |
| 6 | Changes grouped by entity when option enabled. |
| 7 | Contradictions detected across documents. |
| 8 | History tracking records all claim changes. |
| 9 | Snapshots can be created and compared against. |
| 10 | Diff performance <1s for 1000 claims. |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `IClaimDiffService` interface | [ ] |
| 2 | `ClaimDiffService` implementation | [ ] |
| 3 | `ClaimDiffResult` record | [ ] |
| 4 | `ClaimChange` record | [ ] |
| 5 | `ClaimModification` record | [ ] |
| 6 | `FieldChange` record | [ ] |
| 7 | `SemanticMatcher` implementation | [ ] |
| 8 | `ClaimSnapshot` record | [ ] |
| 9 | `ClaimHistoryEntry` record | [ ] |
| 10 | `ClaimContradiction` record | [ ] |
| 11 | Contradiction detection | [ ] |
| 12 | Unit tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.5.6i)

- `IClaimDiffService` for comparing claim sets
- `ClaimDiffResult` with added/removed/modified/unchanged
- Semantic matching for fuzzy claim comparison
- Field-level change tracking in `ClaimModification`
- Change impact assessment
- Change grouping by entity
- Claim contradiction detection across documents
- Claim snapshot support for baselines
- History tracking for audit trails
```

---
