# v0.3.1b: Repository Update

**Version:** v0.3.1b  
**Feature:** The Fuzzy Engine (v0.3.1)  
**Released:** 2026-01-30

## Summary

Added fuzzy matching properties to the StyleTerm entity and repository layer, enabling per-term fuzzy matching configuration. Includes database migration, repository caching, and default inclusive language terms.

## Changes

### New Files

| File                                                                                         | Purpose                                      |
| -------------------------------------------------------------------------------------------- | -------------------------------------------- |
| `Lexichord.Infrastructure/Migrations/Migration_20260126_1500_AddFuzzyColumnsToStyleTerms.cs` | Adds FuzzyEnabled and FuzzyThreshold columns |

### Modified Files

| File                                                          | Change                                                             |
| ------------------------------------------------------------- | ------------------------------------------------------------------ |
| `Lexichord.Abstractions/Entities/StyleTerm.cs`                | Added `FuzzyEnabled` and `FuzzyThreshold` properties               |
| `Lexichord.Abstractions/Contracts/ITerminologyRepository.cs`  | Added `GetFuzzyEnabledTermsAsync()`, `InvalidateFuzzyTermsCache()` |
| `Lexichord.Abstractions/Contracts/TerminologyCacheOptions.cs` | Added `FuzzyTermsCacheKey` property                                |
| `Lexichord.Modules.Style/Data/TerminologyRepository.cs`       | Implemented fuzzy methods with caching, updated SQL                |
| `Lexichord.Modules.Style/Services/TerminologySeeder.cs`       | Added 5 fuzzy-enabled inclusive language terms                     |

## Technical Details

### New Entity Properties

```csharp
public bool FuzzyEnabled { get; init; } = false;
public double FuzzyThreshold { get; init; } = 0.80;
```

### New Repository Methods

| Method                        | Description                                       |
| ----------------------------- | ------------------------------------------------- |
| `GetFuzzyEnabledTermsAsync()` | Returns cached list of fuzzy-enabled active terms |
| `InvalidateFuzzyTermsCache()` | Explicitly clears the fuzzy terms cache           |

### Database Migration

| Column           | Type         | Default |
| ---------------- | ------------ | ------- |
| `FuzzyEnabled`   | BOOLEAN      | FALSE   |
| `FuzzyThreshold` | DECIMAL(3,2) | 0.80    |

Includes partial index `IX_style_terms_fuzzy_enabled` for efficient queries.

### Default Fuzzy Terms (Inclusive Language)

| Term         | Replacement      | Threshold |
| ------------ | ---------------- | --------- |
| whitelist    | allowlist        | 0.85      |
| blacklist    | denylist         | 0.85      |
| master       | main             | 0.90      |
| slave        | replica          | 0.90      |
| sanity check | confidence check | 0.80      |

## Verification

```bash
# Build verified
dotnet build --no-restore
# 2295 unit tests passed, 52 integration tests passed
dotnet test
```
