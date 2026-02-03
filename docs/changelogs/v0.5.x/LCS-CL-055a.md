# LCS-CL-055a: Filter Model

**Version:** v0.5.5a
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented the foundational Filter Model for the Filter System feature, providing immutable records for search filtering criteria, temporal filtering, and saved presets. This sub-part establishes the data structures used by all subsequent Filter System components (v0.5.5b-d).

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                  | Change                                                                  |
| --------------------- | ----------------------------------------------------------------------- |
| `SearchFilter.cs`     | `DateRange` record with temporal bounds and factory methods             |
| `SearchFilter.cs`     | `SearchFilter` record with path, extension, date, heading filters       |
| `SearchFilter.cs`     | `FilterPreset` record for saved configurations with identity management |
| `IFilterValidator.cs` | `IFilterValidator` interface for filter validation                      |
| `IFilterValidator.cs` | `FilterValidationError` record for validation error details             |
| `FilterValidator.cs`  | `FilterValidator` implementation with security and structural checks    |

### DateRange Factory Methods

| Method       | Description                                      |
| ------------ | ------------------------------------------------ |
| `LastDays`   | Creates open-ended range from N days ago         |
| `LastHours`  | Creates open-ended range from N hours ago        |
| `Today`      | Creates range covering the current UTC day       |
| `ForMonth`   | Creates range covering a specific year/month     |

### SearchFilter Factory Methods

| Method             | Description                               |
| ------------------ | ----------------------------------------- |
| `Empty`            | Static property returning no-criteria filter |
| `ForPath`          | Creates filter with single glob pattern   |
| `ForExtensions`    | Creates filter with specified file types  |
| `RecentlyModified` | Creates filter with date range            |

### FilterPreset Methods

| Method         | Description                               |
| -------------- | ----------------------------------------- |
| `Create`       | Factory method generating ID and timestamp |
| `Rename`       | Creates copy with new name                |
| `UpdateFilter` | Creates copy with new filter criteria     |

### Validation Rules

| Rule             | Code              | Description                               |
| ---------------- | ----------------- | ----------------------------------------- |
| Empty pattern    | `PatternEmpty`    | Path patterns cannot be empty/whitespace  |
| Null bytes       | `PatternNullByte` | Prevents null byte injection attacks      |
| Path traversal   | `PatternTraversal`| Blocks ".." directory escape attempts     |
| Empty extension  | `ExtensionEmpty`  | Extensions cannot be empty/whitespace     |
| Path separators  | `ExtensionInvalid`| Extensions cannot contain / or \          |
| Invalid dates    | `DateRangeInvalid`| Start date cannot be after end date       |

## Tests

| File                     | Tests                                              |
| ------------------------ | -------------------------------------------------- |
| `SearchFilterTests.cs`   | 12 tests - SearchFilter record and factory methods |
| `SearchFilterTests.cs`   | 12 tests - DateRange record and factory methods    |
| `SearchFilterTests.cs`   | 10 tests - FilterPreset record and methods         |
| `FilterValidatorTests.cs`| 10 tests - Path pattern validation                 |
| `FilterValidatorTests.cs`| 6 tests - Extension validation                     |
| `FilterValidatorTests.cs`| 4 tests - Date range validation                    |
| `FilterValidatorTests.cs`| 4 tests - Multiple error scenarios                 |
| `FilterValidatorTests.cs`| 2 tests - FilterValidationError record             |

**Total: 60 unit tests**

## License Gating

- Feature Code: None (Core feature)
- Minimum Tier: Core (available to all users)

The filter model itself is license-agnostic. License gating occurs at the UI (v0.5.5b) and service layers (v0.5.5d):
- Date range filtering: WriterPro+
- Saved presets: WriterPro+
- Team-shared presets: Teams+

## Dependencies

- None (foundational abstractions)

## Dependents

| Version  | Component            | Uses                                    |
| -------- | -------------------- | --------------------------------------- |
| v0.5.5b  | Filter UI Component  | `SearchFilter`, `DateRange`, `FilterPreset` |
| v0.5.5c  | Filter Query Builder | `SearchFilter`, `IFilterValidator`      |
| v0.5.5d  | Saved Filters        | `FilterPreset`, `SearchFilter`          |

## Technical Notes

### Thread Safety

`FilterValidator` is stateless and thread-safe. It should be registered as a singleton in dependency injection.

### Record Equality

Note that `SearchFilter` uses array properties which use reference equality by default in C# records. Two filters created with different array instances (even with identical content) will not be equal. For value comparison, use `SequenceEqual` on the collections.

### Security Mitigations

The validator implements several security checks:
- **Path Traversal**: Rejects patterns containing ".." to prevent escaping the intended directory scope
- **Null Byte Injection**: Rejects patterns containing null bytes which could truncate paths in some file systems
- **Extension Abuse**: Rejects extensions with path separators that could manipulate file paths

## Related Documents

- [LCS-DES-055a](../../specs/v0.5.x/v0.5.5/LCS-DES-v0.5.5a.md) - Design Specification
- [LCS-SBD-055](../../specs/v0.5.x/v0.5.5/LCS-SBD-v0.5.5.md) - Scope Breakdown
- [LCS-DES-055-INDEX](../../specs/v0.5.x/v0.5.5/LCS-DES-v0.5.5-INDEX.md) - Version Index
