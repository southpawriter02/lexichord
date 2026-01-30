# v0.3.1a: Algorithm Integration

**Version:** v0.3.1a  
**Feature:** The Fuzzy Engine (v0.3.1)  
**Released:** 2026-01-30

## Summary

Integrated the FuzzySharp NuGet package to provide fuzzy string matching capabilities via `IFuzzyMatchService`. This enables detection of typos and variations of forbidden terminology.

## Changes

### New Files

| File                                                           | Purpose                                                             |
| -------------------------------------------------------------- | ------------------------------------------------------------------- |
| `Lexichord.Abstractions/Contracts/IFuzzyMatchService.cs`       | Interface with `CalculateRatio`, `CalculatePartialRatio`, `IsMatch` |
| `Lexichord.Modules.Style/Services/FuzzyMatchService.cs`        | FuzzySharp wrapper with input normalization                         |
| `Lexichord.Tests.Unit/Modules/Style/FuzzyMatchServiceTests.cs` | 27 unit tests covering all methods and edge cases                   |

### Modified Files

| File                             | Change                                                          |
| -------------------------------- | --------------------------------------------------------------- |
| `Lexichord.Modules.Style.csproj` | Added `FuzzySharp` 2.0.2 package reference                      |
| `StyleModule.cs`                 | Registered `IFuzzyMatchService` → `FuzzyMatchService` singleton |

## Technical Details

### IFuzzyMatchService Interface

```csharp
public interface IFuzzyMatchService
{
    int CalculateRatio(string source, string target);
    int CalculatePartialRatio(string source, string target);
    bool IsMatch(string source, string target, double threshold);
}
```

### Input Normalization

All inputs are normalized before comparison:

1. Whitespace trimmed
2. Converted to lowercase invariant

### Edge Cases

| Scenario                       | Result                               |
| ------------------------------ | ------------------------------------ |
| Both empty after normalization | Returns 100 (identical)              |
| One empty, one not             | Returns 0 (no match)                 |
| Null inputs                    | Throws `ArgumentNullException`       |
| Invalid threshold (<0 or >1)   | Throws `ArgumentOutOfRangeException` |

## Test Coverage

- 27 unit tests covering:
    - `CalculateRatio`: 12 tests (identical, variations, empty, null)
    - `CalculatePartialRatio`: 6 tests (substring, empty, null)
    - `IsMatch`: 9 tests (threshold validation, edge cases)

## Dependencies

| Dependency | Version | Purpose                         |
| ---------- | ------- | ------------------------------- |
| FuzzySharp | 2.0.2   | Levenshtein distance algorithms |

## Verification

```bash
# Build verified
dotnet build --no-restore
# 27/27 tests passed
dotnet test --filter "FullyQualifiedName~FuzzyMatchServiceTests"
```
