# Changelog: v0.2.7c Frontmatter Ignoring

**Version:** 0.2.7c  
**Codename:** The Turbo (Part 3)  
**Date:** 2026-01-30  
**Design Spec:** [LCS-DES-027c](../../specs/v0.2.x/v0.2.7/LCS-DES-027c.md)

---

## Overview

Implements frontmatter detection and exclusion for the linting system. Frontmatter (YAML, TOML, JSON) at the start of Markdown documents is now automatically detected and excluded from style scanning, preventing false positive violations on metadata content.

---

## Changes

### Abstraction Layer

#### ExclusionReason Enum

- **File:** `Lexichord.Abstractions/Contracts/Linting/ExclusionReason.cs`
- **Changes:**
    - Added `TomlFrontmatter` value for TOML (+++ delimited) frontmatter
    - Added `JsonFrontmatter` value for JSON ({ on first line) frontmatter
    - Updated `Frontmatter` remarks to indicate v0.2.7c implementation
    - Updated version comment from v0.2.7b to v0.2.7c

---

### Implementation Layer

#### YamlFrontmatterOptions (NEW)

- **File:** `Lexichord.Modules.Style/Filters/YamlFrontmatterOptions.cs`
- **Purpose:** Configuration options for the frontmatter filter
- **Properties:**
    - `DetectYamlFrontmatter` (default: true) - Enable YAML (---) detection
    - `DetectTomlFrontmatter` (default: true) - Enable TOML (+++) detection
    - `DetectJsonFrontmatter` (default: true) - Enable JSON ({) detection
    - `StripBom` (default: true) - Handle UTF-8 BOM before detection
    - `StrictPositioning` (default: true) - Only detect at document start
    - `Default` static property for standard configuration
    - `YamlOnly` static property for YAML-only detection

#### YamlFrontmatterFilter (NEW)

- **File:** `Lexichord.Modules.Style/Filters/YamlFrontmatterFilter.cs`
- **Purpose:** Detects and excludes frontmatter from linting
- **Interface:** `IContentFilter`
- **Priority:** 100 (runs before code block filter at 200)
- **Supported Formats:**
    - YAML frontmatter (--- delimited)
    - TOML frontmatter (+++ delimited, Hugo style)
    - JSON frontmatter ({ on first line)
- **Features:**
    - UTF-8 BOM handling (strips BOM before detection)
    - Windows (CRLF) and Unix (LF) line ending support
    - Unclosed frontmatter detection (extends to EOF with warning)
    - Trailing whitespace on delimiters allowed
    - JSON brace counting for nested objects
- **Thread Safety:** Stateless and thread-safe

#### StyleModule Registration

- **File:** `Lexichord.Modules.Style/StyleModule.cs`
- **Changes:**
    - Added DI registration for `YamlFrontmatterFilter` as `IContentFilter`
    - Added DI registration for `YamlFrontmatterFilter` as concrete type
    - Filter runs before `MarkdownCodeBlockFilter` due to priority ordering

---

### Test Coverage

#### YamlFrontmatterFilterTests (NEW)

- **File:** `Lexichord.Tests.Unit/Modules/Style/Filters/YamlFrontmatterFilterTests.cs`
- **Test Count:** 44 tests
- **Coverage Areas:**
    - Empty and null content handling (4 tests)
    - YAML frontmatter detection (5 tests)
    - TOML frontmatter detection (3 tests)
    - JSON frontmatter detection (4 tests)
    - BOM handling edge cases (3 tests)
    - Line ending variations (3 tests)
    - Positioning requirements (2 tests)
    - Unclosed frontmatter (3 tests)
    - Empty and minimal frontmatter (3 tests)
    - CanFilter for file extensions (6 tests)
    - Property tests (3 tests)
    - Constructor validation (2 tests)
    - Performance tests (2 tests)
    - Integration scenario (1 test)

---

## Technical Details

### Priority Ordering

The filter priority system ensures correct detection order:

| Filter                  | Priority | Description                   |
| ----------------------- | -------- | ----------------------------- |
| YamlFrontmatterFilter   | 100      | Frontmatter at document start |
| MarkdownCodeBlockFilter | 200      | Fenced and inline code blocks |

This ordering is critical because:

1. Frontmatter `---` could be mistaken for horizontal rules
2. Code examples in frontmatter might contain backticks
3. Frontmatter must be detected at document start before any other processing

### Detection Algorithm

```
1. Check for UTF-8 BOM (U+FEFF) at start
2. If BOM present and StripBom enabled, skip it for detection
3. Check first line for delimiter:
   - "---" → YAML frontmatter
   - "+++" → TOML frontmatter
   - "{" → JSON frontmatter
4. Scan for closing delimiter or brace
5. Return ExcludedRegion from offset 0 to end of frontmatter
6. If no closing found, extend to EOF and log warning
```

### Edge Cases Handled

| Edge Case                    | Behavior                                 |
| ---------------------------- | ---------------------------------------- |
| BOM before delimiter         | Stripped, frontmatter detected correctly |
| Windows line endings (CRLF)  | Handled transparently                    |
| No trailing newline          | Valid, delimiter at EOF detected         |
| Trailing spaces on delimiter | Treated as valid delimiter               |
| Content before frontmatter   | Not detected (strict positioning)        |
| Unclosed frontmatter         | Excluded to EOF with warning             |
| Empty frontmatter            | Valid, exclusion created                 |
| JSON with nested objects     | Brace counting for correct closing       |

---

## Verification Results

### Build Status

- **Result:** Success
- **Warnings:** 0
- **Errors:** 0

### Test Results

- **Total Tests:** 2346
- **Passed:** 2293
- **Failed:** 0
- **Skipped:** 53 (platform-specific)

### New Test Results

- **YamlFrontmatterFilterTests:** 44/44 passed

---

## Files Changed

### New Files

| File                                                                             | Lines | Purpose               |
| -------------------------------------------------------------------------------- | ----- | --------------------- |
| `src/Lexichord.Modules.Style/Filters/YamlFrontmatterOptions.cs`                  | 93    | Configuration options |
| `src/Lexichord.Modules.Style/Filters/YamlFrontmatterFilter.cs`                   | 484   | Filter implementation |
| `tests/Lexichord.Tests.Unit/Modules/Style/Filters/YamlFrontmatterFilterTests.cs` | 551   | Unit tests            |

### Modified Files

| File                                                              | Change Description                     |
| ----------------------------------------------------------------- | -------------------------------------- |
| `src/Lexichord.Abstractions/Contracts/Linting/ExclusionReason.cs` | Added TomlFrontmatter, JsonFrontmatter |
| `src/Lexichord.Modules.Style/StyleModule.cs`                      | Added filter DI registration           |

---

## Dependencies

### Consumes (from v0.2.7b)

- `IContentFilter` interface
- `FilteredContent` record
- `ExcludedRegion` record
- `ContentFilterOptions` record

### Provides (new in v0.2.7c)

- `YamlFrontmatterFilter` class
- `YamlFrontmatterOptions` record
- `ExclusionReason.TomlFrontmatter` enum value
- `ExclusionReason.JsonFrontmatter` enum value
