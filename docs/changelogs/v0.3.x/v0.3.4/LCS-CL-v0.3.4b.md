# LCS-CL-034b: Passive Voice Detector

**Version:** v0.3.4b  
**Release Date:** 2026-01-30  
**Status:** ✅ Complete  
**Specification:** [LCS-DES-034b](../../../specs/v0.3.x/v0.3.4/LCS-DES-034b.md)

---

## Summary

Implements the Passive Voice Detection Analyzer for the Writing Coach feature. Uses regex-based pattern matching with confidence scoring to identify passive voice constructions while distinguishing them from predicate adjectives (e.g., "The door is closed").

---

## Added

### Abstractions (`Lexichord.Abstractions`)

- **`PassiveType`** enum categorizing passive constructions:
    - `ToBe` — Standard passive (was written, is deleted)
    - `Modal` — Modal passive (will be deleted, can be done)
    - `Progressive` — Progressive passive (is being reviewed)
    - `Get` — Get-passive (got fired)

- **`PassiveVoiceMatch`** record with:
    - `Sentence` — The analyzed sentence
    - `PassiveConstruction` — The matched phrase (e.g., "was written")
    - `Type` — PassiveType classification
    - `Confidence` — Score 0.0-1.0 for disambiguation
    - `StartIndex`, `EndIndex` — Position in original text
    - `IsPassiveVoice` property (Confidence ≥ 0.5)
    - `Length` property (EndIndex - StartIndex)

- **`IPassiveVoiceDetector`** interface defining:
    - `ContainsPassiveVoice(string)` — Quick boolean check
    - `Detect(string)` — Full analysis returning all matches
    - `DetectInSentence(string)` — Single sentence analysis
    - `GetPassiveVoicePercentage(...)` — Document-level metrics

### Services (`Lexichord.Modules.Style`)

- **`PassivePatterns`** static class with compiled regex patterns:

| Pattern     | Example Match       | Regex    |
| ----------- | ------------------- | -------- | ----- | ------------------------ | ------------------------- | --------------------- | ------ | ------ | ------ |
| Progressive | "is being reviewed" | `\b(is   | are   | was                      | were)\s+being\s+(\w+(?:ed | en                    | t      | d))\b` |
| Perfect     | "has been read"     | `\b(has  | had   | have)\s+been\s+(\w+(?:ed | en                        | t                     | d))\b` |
| Modal       | "will be deleted"   | `\b(will | would | can                      | could                     | ...)\s+be\s+(\w+(?:ed | en     | t      | d))\b` |
| ToBe        | "was written"       | `\b(am   | is    | are                      | was                       | were)\s+(\w+(?:ed     | en     | t      | d))\b` |
| Get         | "got fired"         | `\b(get  | gets  | got                      | gotten                    | getting)\s+(\w+(?:ed  | en     | t      | d))\b` |

- **`CommonStateAdjectives`** HashSet (~50 words):
    - closed, tired, broken, worried, concerned, etc.
    - Used for adjective disambiguation

- **`StateContextVerbs`** for context detection:
    - seems, appears, looks, remains, stays, feels, sounds, becomes

- **`PassiveVoiceDetector`** implementation with:
    - Pattern matching in priority order (Progressive → Perfect → Modal → ToBe → Get)
    - Confidence scoring algorithm:
        - Base: 0.75
        - +0.25 for "by [agent]" phrase
        - -0.30 for common state adjective
        - +0.15 for progressive passive
        - -0.20 for state context verb
    - Exhaustive logging (Trace/Debug levels per §8)
    - `ISentenceTokenizer` integration for multi-sentence text

### Unit Tests (`Lexichord.Tests.Unit`)

- **`PassiveVoiceDetectorTests.cs`** with 41 test cases:
    - Constructor null checks
    - Passive sentence detection (7 InlineData cases)
    - Active sentence negative testing (5 cases)
    - Adjective disambiguation (5 cases)
    - Pattern type verification (ToBe, Modal, Progressive, Get)
    - Confidence scoring (by-agent boost, adjective penalty)
    - Percentage calculation tests
    - Edge cases (null, empty, whitespace)
    - PassiveVoiceMatch record behavior

---

## Changed

### Services (`Lexichord.Modules.Style`)

- **`StyleModule.cs`** — Added singleton registration:
    - `services.AddSingleton<IPassiveVoiceDetector, PassiveVoiceDetector>()`

---

## Technical Notes

### Confidence Scoring Algorithm

The detector uses a confidence-based approach to distinguish true passive voice from predicate adjectives:

```
Base confidence = 0.75

Adjustments:
  + 0.25  if sentence contains "by [agent]" (strong passive indicator)
  - 0.30  if participle is a common state adjective
  + 0.15  if progressive passive (more definitive)
  - 0.20  if state context verb present (seems, appears, etc.)

Final = clamp(result, 0.0, 1.0)
```

### Pattern Priority

Patterns are evaluated in order of specificity:

1. **Progressive** — Most specific (is being + participle)
2. **Perfect** — Has/had been + participle
3. **Modal** — Modal + be + participle
4. **ToBe** — Standard to-be + participle
5. **Get** — Get-passive

---

## Dependencies

- **ISentenceTokenizer** (v0.3.3a) — Sentence boundary detection
- **ILogger<PassiveVoiceDetector>** — Structured logging

---

## Related Changes

- Completes Phase 2 of v0.3.4 Voice Profiler feature
- Next: v0.3.4c — Style Enforcement Pipeline integration
