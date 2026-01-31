# v0.3.3a: Abbreviation-Aware Sentence Tokenizer

> **Release Date**: 2026-01-30  
> **Spec**: [LCS-DES-033a](../../specs/v0.3.x/v0.3.3/LCS-DES-033a.md)  
> **License Tier**: Writer Pro

## Overview

Implementation of the Sentence Tokenizer sub-part of the Readability Engine. This service
splits text into sentences while respecting common English abbreviations to avoid false
sentence breaks at periods used in titles (Mr., Dr.), business names (Inc., Corp.),
or period-embedded abbreviations (U.S.A., Ph.D.).

## Added

### Abstractions (`Lexichord.Abstractions`)

- **`SentenceInfo`** record struct
    - `Text`: The sentence text including terminal punctuation
    - `StartIndex`: Character position where sentence begins
    - `EndIndex`: Character position where sentence ends (exclusive)
    - `WordCount`: Number of words in the sentence

- **`ISentenceTokenizer`** interface
    - `Tokenize(string text)` → `IReadOnlyList<SentenceInfo>`
    - Thread-safe, produces immutable results

### Services (`Lexichord.Modules.Style`)

- **`SentenceTokenizer`** implementation
    - **50+ standard abbreviations**: Mr, Mrs, Dr, Prof, Inc, Corp, Ltd, etc.
    - **Period-embedded abbreviations**: Ph.D, U.S.A, a.m, p.m, e.g, i.e, etc.
    - **Ellipsis detection**: `...` not treated as sentence boundary
    - **Initials detection**: J.F.K. patterns kept together
    - **Next-char case analysis**: Lowercase following period indicates continuation
    - **Compiled regex patterns**: Performance optimized
    - **Comprehensive logging**: Debug and Trace level diagnostics

### Unit Tests

- **48 test cases** covering:
    - Empty/null/whitespace inputs (3 tests)
    - Simple sentences with `.!?` (4 tests)
    - Title abbreviations (6 tests)
    - Business abbreviations (4 tests)
    - Geographic abbreviations (3 tests)
    - Latin abbreviations (4 tests)
    - Period-embedded abbreviations (4 tests)
    - Ellipsis handling (2 tests)
    - Initials handling (2 tests)
    - Position tracking (4 tests)
    - Word count accuracy (5 tests)
    - Sentences without terminal punctuation (2 tests)
    - Reference texts (2 tests)
    - Month abbreviations (3 tests)

## Technical Notes

### Abbreviation Dictionary

The tokenizer maintains two static HashSets:

1. **StandardAbbreviations**: Single-word abbreviations (Mr, Dr, Inc, etc.)
2. **PeriodAbbreviations**: Multi-part abbreviations (U.S.A, Ph.D, etc.)

Both use `StringComparer.OrdinalIgnoreCase` for case-insensitive matching.

### Decision Logic

The `IsRealSentenceBreak` method implements the following decision tree:

1. Non-period punctuation (`!`, `?`) → Always sentence break
2. Ellipsis (3+ periods) → Not a sentence break
3. Period-embedded abbreviation → Only break if followed by uppercase
4. Standard abbreviation → Not a sentence break
5. Single letter (initial) → Check for initials pattern
6. Default → Break if followed by uppercase or end-of-text

### Performance

- Uses `System.Text.RegularExpressions` source generators for compiled patterns
- Stateless design suitable for concurrent access
- Word counting reuses `DocumentTokenizer` pattern: `\b[\w-]+\b`

## Dependencies

| Dependency           | Version | Purpose                             |
| -------------------- | ------- | ----------------------------------- |
| `IDocumentTokenizer` | v0.3.1c | Word tokenization pattern reference |

## Files Changed

| File                                                           | Change     |
| -------------------------------------------------------------- | ---------- |
| `Lexichord.Abstractions/Contracts/SentenceInfo.cs`             | [NEW]      |
| `Lexichord.Abstractions/Contracts/ISentenceTokenizer.cs`       | [NEW]      |
| `Lexichord.Modules.Style/Services/SentenceTokenizer.cs`        | [NEW]      |
| `Lexichord.Modules.Style/StyleModule.cs`                       | [MODIFIED] |
| `Lexichord.Tests.Unit/Modules/Style/SentenceTokenizerTests.cs` | [NEW]      |

## Verification

```bash
# Build verification
dotnet build --no-restore
# Build succeeded. 0 Warning(s), 0 Error(s)

# Unit tests
dotnet test --filter "FullyQualifiedName~SentenceTokenizerTests" --no-build
# Passed! 48/48 tests

# Full regression
dotnet test --no-build
# Passed! 2,374/2,374 unit tests, 52/52 integration tests
```
