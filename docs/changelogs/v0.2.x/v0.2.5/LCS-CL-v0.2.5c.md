# v0.2.5c: Term Editor Dialog

**Version:** v0.2.5c  
**Codename:** The Librarian - Term Editor  
**Release Date:** 2026-01-30

---

## Summary

Implements the modal dialog for adding and editing style terminology rules with real-time regex validation, pattern testing, and WriterPro license gating.

---

## Abstraction Layer

### New Contracts

| File                                                                                                                                          | Description                                                   |
| --------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------- |
| [ITermEditorDialogService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/ITermEditorDialogService.cs) | Interface for showing term editor dialogs with license checks |

### Modified Contracts

| File                                                                                                                          | Changes                                                                                                         |
| ----------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| [StyleDomainTypes.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/StyleDomainTypes.cs) | Added `PatternTestResult`, `PatternMatch` records; added `MatchCase` to `CreateTermCommand`/`UpdateTermCommand` |
| [StyleTerm.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Entities/StyleTerm.cs)                | Added `MatchCase` property for case-sensitive matching                                                          |

---

## Implementation Layer

### New Components

| File                                                                                                                                        | Description                                                                  |
| ------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| [TermEditorViewModel.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/ViewModels/TermEditorViewModel.cs)       | ViewModel with form state, validation, pattern testing, dirty state tracking |
| [TermEditorDialog.axaml](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Views/TermEditorDialog.axaml)            | Modal dialog UI with form fields, validation display, pattern test section   |
| [TermEditorDialogService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Services/TermEditorDialogService.cs) | Service managing dialog lifecycle and WriterPro license enforcement          |

### Modified Components

| File                                                                                                                            | Changes                                                            |
| ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| [StyleModule.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/StyleModule.cs)                      | Registered `TermEditorViewModel` and `ITermEditorDialogService`    |
| [LexiconViewModel.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/ViewModels/LexiconViewModel.cs) | Wired `AddTermAsync()` and `EditSelectedAsync()` to dialog service |

---

## Test Coverage

### New Test File

| File                                                                                                                                              | Tests | Coverage                                                                       |
| ------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ------------------------------------------------------------------------------ |
| [TermEditorViewModelTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/TermEditorViewModelTests.cs) | 18    | Initialization, validation, dirty state, pattern testing, save/cancel commands |

### Modified Test File

| File                                                                                                                                        | Changes                               |
| ------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------- |
| [LexiconViewModelTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/LexiconViewModelTests.cs) | Added `ITermEditorDialogService` mock |

---

## Technical Notes

### Pattern Validation

- Uses `TermPatternValidator.Validate()` for regex syntax checking
- Includes ReDoS protection with 100ms timeout during pattern testing
- Supports both literal text and regex patterns via `LooksLikeRegex()` detection

### MatchCase Field

- Added to `StyleTerm` entity (default: `false` for case-insensitive)
- Propagated to `CreateTermCommand` and `UpdateTermCommand`
- Affects pattern testing results in real-time

### License Enforcement

- `ITermEditorDialogService` checks `ILicenseContext.GetCurrentTier()`
- WriterPro tier required for Add/Edit operations
- Returns `false` immediately if license check fails

---

## Verification Summary

```
Test Run Successful.
Total tests: 164
     Passed: 164
Total time: 0.67 Seconds
```

Build: ✅ Success  
All Style module tests: ✅ 164/164 passing
