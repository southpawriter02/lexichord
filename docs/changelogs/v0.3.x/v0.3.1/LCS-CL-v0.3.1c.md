# v0.3.1c: The Fuzzy Scanner Changelog

**Version**: v0.3.1c  
**Date**: 2026-01-31  
**Scope**: Fuzzy terminology scanning with license-gated activation

## Summary

Introduces the Fuzzy Scanner for approximate terminology matching, completing the v0.3.1 Fuzzy Engine feature set. The scanner tokenizes documents, matches tokens against fuzzy-enabled terminology, and integrates violations into the existing linting pipeline while preventing double-counting of words already flagged by exact regex matches.

## Added

### Contracts (`Lexichord.Abstractions.Contracts`)

- **`IDocumentTokenizer`**: Interface for text tokenization with position tracking
    - `Tokenize(string content)`: Returns unique lowercase tokens as `IReadOnlySet<string>`
    - `TokenizeWithPositions(string content)`: Returns all token occurrences with offset info
- **`DocumentToken`**: Record for tokens with `Token`, `StartOffset`, `EndOffset`
- **`IFuzzyScanner`**: Service interface for fuzzy terminology scanning
    - `ScanAsync(content, regexFlaggedWords, cancellationToken)`: Returns fuzzy violations

### Service Implementations (`Lexichord.Modules.Style.Services`)

- **`DocumentTokenizer`**: Regex-based tokenizer preserving hyphenated words
    - Pattern: `[\w]+(?:-[\w]+)*` captures "self-aware", "state-of-the-art"
    - Case normalization to lowercase
    - Duplicate elimination for `Tokenize()`, all occurrences for `TokenizeWithPositions()`
- **`FuzzyScanner`**: Fuzzy terminology scanner with:
    - License gating (requires `LicenseTier.WriterPro` or higher)
    - Per-term threshold support via `StyleTerm.FuzzyThreshold`
    - Double-count prevention via `regexFlaggedWords` exclusion
    - Line/column calculation from byte offsets

### Type Modifications

- **`StyleViolation`**: Added optional positional parameters:
    - `IsFuzzyMatch` (bool, default: false)
    - `FuzzyRatio` (int?, default: null)

### Integration

- **`LintingOrchestrator`**: Extended scanning pipeline:
    - Collects exact-match words from regex violations
    - Invokes `IFuzzyScanner.ScanAsync()` with exclusion set
    - Merges fuzzy violations into result set
- **`StyleModule`**: DI registration for `IDocumentTokenizer` and `IFuzzyScanner`

## Testing

- **`DocumentTokenizerTests`**: 15 tests covering tokenization, hyphenation, positions
- **`FuzzyScannerTests`**: 13 tests covering license gates, thresholds, double-counting
- **`LintingOrchestratorTests`**: Updated 27 existing tests with `IFuzzyScanner` mock

## Verification

- Full test suite: **2,326 unit tests + 52 integration tests passed**
- Build: Success with 0 warnings, 0 errors
