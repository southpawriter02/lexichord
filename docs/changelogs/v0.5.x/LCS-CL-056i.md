# v0.5.6i — Claim Diff Service

**Release Date:** 2026-02-03  
**Status:** Implemented  
**Feature ID:** `KG-056i`  
**Specification:** [LCS-DES-v0.5.6-KG-i.md](../../specs/v0.5.x/v0.5.6/LCS-DES-v0.5.6-KG-i.md)

---

## Summary

Implements the Claim Diff Service for comparing claims between document versions, identifying changes, and detecting contradictions. Enables version tracking and change history for extracted claims.

---

## New Files

### Data Contracts (16 files)

| File | Description |
|------|-------------|
| `DiffOptions.cs` | Configuration for diff behavior (semantic matching, thresholds) |
| `ClaimDiffResult.cs` | Result container with added/removed/modified/unchanged claims |
| `DiffStats.cs` | Aggregate statistics (counts, by-predicate, by-type) |
| `ClaimChangeType.cs` | Enum: Added, Removed, SubjectChanged, etc. |
| `ClaimChange.cs` | Record for added/removed claims with impact |
| `ClaimModification.cs` | Record for modified claims with field changes |
| `FieldChange.cs` | Individual field change details |
| `ChangeImpact.cs` | Enum: Low, Medium, High, Critical |
| `ClaimChangeGroup.cs` | Related changes grouped by subject entity |
| `ClaimSnapshot.cs` | Point-in-time baseline snapshot |
| `ClaimHistoryEntry.cs` | Audit trail entry |
| `ClaimHistoryAction.cs` | Enum: Created, Updated, Deleted, etc. |
| `ClaimContradiction.cs` | Detected contradiction between claims |
| `ContradictionType.cs` | Enum: DirectContradiction, DeprecationConflict, etc. |
| `ContradictionStatus.cs` | Enum: Open, UnderReview, Resolved, Ignored |
| `IClaimDiffService.cs` | Service interface |

All files located in: `src/Lexichord.Abstractions/Contracts/Knowledge/Claims/Diff/`

### Implementation (2 files)

| File | Description |
|------|-------------|
| `ClaimDiffService.cs` | Main service implementation |
| `SemanticMatcher.cs` | Jaro-Winkler similarity matching |

Located in: `src/Lexichord.Modules.Knowledge/Claims/Diff/`

### Tests (1 file)

| File | Description |
|------|-------------|
| `ClaimDiffServiceTests.cs` | 34 unit tests |

Located in: `tests/Lexichord.Tests.Unit/Modules/Knowledge/`

---

## API Summary

### IClaimDiffService

```csharp
public interface IClaimDiffService
{
    ClaimDiffResult Diff(IReadOnlyList<Claim> oldClaims, IReadOnlyList<Claim> newClaims, DiffOptions? options = null);
    Task<ClaimDiffResult> DiffDocumentVersionsAsync(Guid documentId, int oldVersion, int newVersion, CancellationToken ct = default);
    Task<ClaimDiffResult> DiffFromBaselineAsync(Guid documentId, Guid baselineSnapshotId, CancellationToken ct = default);
    Task<ClaimSnapshot> CreateSnapshotAsync(Guid documentId, string? label = null, CancellationToken ct = default);
    Task<IReadOnlyList<ClaimHistoryEntry>> GetHistoryAsync(Guid documentId, int limit = 20, CancellationToken ct = default);
    Task<IReadOnlyList<ClaimContradiction>> FindContradictionsAsync(Guid projectId, CancellationToken ct = default);
}
```

---

## Features

### Claim Comparison
- **ID Matching**: Exact ID comparison for identical claims
- **Semantic Matching**: Jaro-Winkler fuzzy matching for re-extracted claims
- **Field-Level Diffs**: Subject, predicate, object, confidence, validation, evidence
- **Configurable Thresholds**: Ignore small confidence changes

### Change Classification
- **Impact Assessment**: Low/Medium/High/Critical based on predicate type
- **Change Grouping**: Related changes grouped by subject entity
- **Statistics**: Aggregated counts by predicate and change type

### Contradiction Detection
- **Cross-Document Analysis**: Find conflicting claims across documents
- **Direct Contradictions**: Same subject/predicate, different objects
- **Resolution Workflow**: Open → UnderReview → Resolved/Ignored

### Snapshot & History
- **Baseline Snapshots**: Create named checkpoints for comparison
- **Audit Trail**: Track all claim operations with timestamps

---

## Test Results

| Category | Tests | Status |
|----------|-------|--------|
| Constructor | 3 | ✅ Pass |
| DiffOptions | 4 | ✅ Pass |
| DiffStats | 2 | ✅ Pass |
| ClaimDiffResult | 5 | ✅ Pass |
| ClaimDiffService.Diff | 8 | ✅ Pass |
| SemanticMatcher | 8 | ✅ Pass |
| ClaimSnapshot | 1 | ✅ Pass |
| ClaimContradiction | 1 | ✅ Pass |
| ChangeImpact | 1 | ✅ Pass |
| **Total** | **34** | ✅ **All Pass** |

---

## Dependencies

### Upstream
- v0.5.6e: Claim Data Model (`Claim`, `ClaimEntity`, `ClaimObject`)
- v0.5.6h: Claim Repository (`IClaimRepository`)

### Internal
- `Microsoft.Extensions.Logging.Abstractions`

---

## Usage Example

```csharp
// Compare two claim sets
var result = diffService.Diff(oldClaims, newClaims);

if (result.HasChanges)
{
    Console.WriteLine($"Added: {result.Stats.AddedCount}");
    Console.WriteLine($"Removed: {result.Stats.RemovedCount}");
    Console.WriteLine($"Modified: {result.Stats.ModifiedCount}");
    
    foreach (var mod in result.Modified)
    {
        Console.WriteLine($"  {mod.Description}");
    }
}

// Create a baseline snapshot
var snapshot = await diffService.CreateSnapshotAsync(documentId, "v1.0-release");

// Find contradictions across a project
var contradictions = await diffService.FindContradictionsAsync(projectId);
```

---

## License

Teams tier required for full functionality. Free tier limited to basic diff operations.
