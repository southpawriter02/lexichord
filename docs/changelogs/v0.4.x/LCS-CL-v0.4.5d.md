# Changelog: v0.4.5d - License Gating

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.5d](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5d.md)

---

## Summary

Enhances the `SearchLicenseGuard` (v0.4.5b) with publicly accessible constants and a `GetUpgradeMessage()` method for UI upgrade prompts. Adds `UsedCachedEmbedding` telemetry flag to `SemanticSearchExecutedEvent` and updates `PgVectorSearchService` to track and report embedding cache hits. Introduces v0.4.5d-tagged unit tests for all new functionality.

---

## Changes

### Modified

#### Lexichord.Modules.RAG/Search/

| File                        | Change                                                                                    |
| :-------------------------- | :---------------------------------------------------------------------------------------- |
| `SearchLicenseGuard.cs`     | Made `FeatureName` and `RequiredTier` public; added `GetUpgradeMessage()` method           |
| `SearchEvents.cs`           | Added `UsedCachedEmbedding` property to `SemanticSearchExecutedEvent`                     |
| `PgVectorSearchService.cs`  | Changed `GetQueryEmbeddingAsync` return type to tuple; passes cache flag to telemetry event |

### Added

#### Lexichord.Tests.Unit/Modules/RAG/Search/

| File                               | Tests | Coverage                                                                     |
| :--------------------------------- | :---- | :--------------------------------------------------------------------------- |
| `SearchLicenseGuardV045dTests.cs`  | 10    | Public constants, GetUpgradeMessage for all tiers, non-empty message checks  |
| `SearchEventsV045dTests.cs`        | 6     | UsedCachedEmbedding default, set, equality, inequality, all-properties, SearchDeniedEvent unchanged |

---

## Technical Details

### SearchLicenseGuard Enhancements

| Member | v0.4.5b | v0.4.5d | Purpose |
| :----- | :------ | :------ | :------ |
| `FeatureName` | `private const` | `public const` | External consumers can reference canonical feature name |
| `RequiredTier` | `private static readonly` | `public const` | UI can display required tier in upgrade prompts |
| `GetUpgradeMessage()` | — | New method | Returns tier-specific upgrade guidance for UI |

### GetUpgradeMessage() Return Values

| Tier | Message Content |
| :--- | :-------------- |
| Core | "Upgrade to Writer Pro to unlock semantic search. Search your documents using natural language!" |
| WriterPro+ | "Semantic search is available with your license." |

### SemanticSearchExecutedEvent Enhancement

| Property | Type | Default | Description |
| :------- | :--- | :------ | :---------- |
| `UsedCachedEmbedding` | `bool` | `false` | Whether the query embedding was served from cache |

### PgVectorSearchService Cache Tracking

| Method | v0.4.5b Return | v0.4.5d Return | Change |
| :----- | :------------- | :------------- | :----- |
| `GetQueryEmbeddingAsync` | `float[]` | `(float[] Embedding, bool UsedCache)` | Returns cache hit status |

The `SearchAsync` method now destructures the tuple and passes `UsedCachedEmbedding` to the `SemanticSearchExecutedEvent` for telemetry tracking.

---

## Spec Adaptations

The design specification (LCS-DES-045d) was written before the v0.4.5b implementation. The following adaptations were made to align with the actual codebase:

| Spec Reference | Adaptation | Reason |
| :------------- | :--------- | :----- |
| `LicenseTier.Writer` in switch | Omitted | `Writer` tier does not exist in `LicenseTier` enum (Core=0, WriterPro=1, Teams=2, Enterprise=3) |
| `_licenseContext.Tier` property | `_licenseContext.GetCurrentTier()` method | `ILicenseContext` uses method, not property |
| 3-arg `FeatureNotLicensedException` constructor | Not modified | Exception is from v0.4.4d with 2-arg constructor; reused as-is |
| `RequiredTier` as `public const` | `public const LicenseTier` | Enums support `const` in C# |

---

## Verification

```bash
# Build solution
dotnet build
# Result: Build succeeded, 0 warnings, 0 errors

# Run v0.4.5d tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.5d"
# Result: 16 tests passed

# Run full regression
dotnet test tests/Lexichord.Tests.Unit
# Result: 4060 passed, 0 failed, 33 skipped (4093 total)
```

---

## Test Coverage

| Category                                 | Tests |
| :--------------------------------------- | ----: |
| FeatureName public const                 |     1 |
| RequiredTier public const                |     1 |
| GetUpgradeMessage — Core tier            |     1 |
| GetUpgradeMessage — WriterPro tier       |     1 |
| GetUpgradeMessage — Teams tier           |     1 |
| GetUpgradeMessage — Enterprise tier      |     1 |
| GetUpgradeMessage — all tiers non-empty  |     4 |
| UsedCachedEmbedding — default false      |     1 |
| UsedCachedEmbedding — set true           |     1 |
| UsedCachedEmbedding — record equality    |     1 |
| UsedCachedEmbedding — record inequality  |     1 |
| All event properties                     |     1 |
| SearchDeniedEvent unchanged              |     1 |
| **Total**                                | **16** |

---

## Dependencies

- v0.4.5b: `SearchLicenseGuard` class (enhanced), `SearchEvents.cs` (enhanced), `PgVectorSearchService` (enhanced)
- v0.0.4c: `ILicenseContext` for tier checks, `LicenseTier` enum
- v0.4.4d: `FeatureNotLicensedException` (reused, not modified)
- v0.0.7a: `IMediator` for event publishing

## Dependents

- v0.4.6: Reference Panel (consumes `GetUpgradeMessage()` for upgrade prompt UI, `UsedCachedEmbedding` for analytics)

---

## Related Documents

- [LCS-DES-v0.4.5d](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5d.md) - Design specification
- [LCS-SBD-v0.4.5](../../specs/v0.4.x/v0.4.5/LCS-SBD-v0.4.5.md) - Scope breakdown
- [LCS-DES-v0.4.5-INDEX](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-INDEX.md) - Version index
- [LCS-CL-v0.4.5c](./LCS-CL-v0.4.5c.md) - Previous version (Query Preprocessing)
