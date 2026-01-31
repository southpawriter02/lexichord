# LCS-CL-034c: Weak Word Scanner

**Version:** v0.3.4c  
**Release Date:** 2026-01-30  
**Status:** ✅ Complete  
**Specification:** [LCS-DES-034c](../../../specs/v0.3.x/v0.3.4/LCS-DES-034c.md)

---

## Summary

Implements the Weak Word Scanner sub-part of the Writing Coach feature. The scanner detects adverbs, weasel words, and filler expressions based on Voice Profile settings, providing targeted suggestions for improvement.

---

## Added

### Abstractions (`Lexichord.Abstractions`)

- **`WeakWordCategory`** enum: Classifies weak words as `Adverb`, `WeaselWord`, or `Filler`
- **`WeakWordMatch`** record: Represents a detected weak word with position tracking
- **`WeakWordStats`** record: Aggregate statistics including counts by category and percentage
- **`IWeakWordScanner`** interface: Contract for `Scan()`, `GetStatistics()`, and `GetSuggestion()`

### Services (`Lexichord.Modules.Style`)

- **`WeakWordLists`** static class: Curated word lists containing ~85 weak words:
    - `Adverbs`: ~40 intensifiers and manner adverbs (very, really, extremely, etc.)
    - `WeaselWords`: ~30 hedges and vague qualifiers (perhaps, maybe, probably, etc.)
    - `Fillers`: ~15 meaningless words (basically, essentially, literally, etc.)
    - `Suggestions`: Dictionary mapping weak words to specific improvement advice

- **`WeakWordScanner`** service:
    - Tokenizes text using word boundary regex
    - Respects `VoiceProfile.FlagAdverbs` and `FlagWeaselWords` settings
    - **Fillers are always flagged** regardless of profile settings
    - Returns matches sorted by position for editor integration
    - Provides specific or generic suggestions for each weak word

### DI Registration

- Registered `IWeakWordScanner` as singleton in `StyleModule.cs`

---

## Technical Notes

### Detection Algorithm

1. Text is tokenized using `\b\w+\b` regex pattern
2. Each word is normalized to lowercase for case-insensitive matching
3. Words are categorized in priority order: **Filler → Adverb → WeaselWord**
4. Matches include original position for editor squiggly rendering

### Profile Constraints

| Profile Setting           | Behavior                              |
| :------------------------ | :------------------------------------ |
| `FlagAdverbs = true`      | Adverbs are detected and flagged      |
| `FlagAdverbs = false`     | Adverbs are ignored                   |
| `FlagWeaselWords = true`  | Weasel words are detected and flagged |
| `FlagWeaselWords = false` | Weasel words are ignored              |
| (N/A)                     | **Fillers are always flagged**        |

### Thread Safety

- `WeakWordScanner` is stateless and thread-safe
- Word lists are static readonly HashSets with O(1) lookup
- Suitable for parallel document scanning

---

## Test Coverage

| Test Class             | Tests | Coverage                                                                                             |
| :--------------------- | :---- | :--------------------------------------------------------------------------------------------------- |
| `WeakWordScannerTests` | 51    | Constructor validation, adverb/weasel/filler detection, profile constraints, statistics, suggestions |

---

## Dependencies

| Component          | Dependency     | Version |
| :----------------- | :------------- | :------ |
| `IWeakWordScanner` | `VoiceProfile` | v0.3.4a |

---

## Files Changed

### New Files

- `src/Lexichord.Abstractions/Contracts/WeakWordTypes.cs`
- `src/Lexichord.Modules.Style/Services/WeakWordLists.cs`
- `src/Lexichord.Modules.Style/Services/WeakWordScanner.cs`
- `tests/Lexichord.Tests.Unit/Modules/Style/WeakWordScannerTests.cs`

### Modified Files

- `src/Lexichord.Modules.Style/StyleModule.cs` (DI registration)

---

## Migration Guide

No migration required. This is a new feature with no breaking changes.

---

## Known Limitations

1. Word boundary tokenization may miss hyphenated compound words
2. Context-aware analysis (e.g., "quick" as adjective vs adverb) not implemented
3. Custom word list editing requires Teams+ license (future feature)
