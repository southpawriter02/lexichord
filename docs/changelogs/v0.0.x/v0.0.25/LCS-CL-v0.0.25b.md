# LCS-CL-025b: Search & Filter Changelog

> **Version**: v0.2.5b  
> **Category**: Feature (The Librarian)  
> **Status**: Complete  
> **Completed**: 2026-01-30

## Overview

Implements real-time search and filter functionality for the Lexicon terminology grid, allowing users to efficiently find and narrow down style terms.

## Changes

### Abstraction Layer (`Lexichord.Abstractions`)

| File                                                                                                                              | Change                                                               |
| --------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| [FilterOptions.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/FilterOptions.cs)           | [NEW] Configuration options for filter behavior (debounce, defaults) |
| [ITermFilterService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/ITermFilterService.cs) | [NEW] Service contract for filtering StyleTerm collections           |

### Implementation Layer (`Lexichord.Modules.Style`)

| File                                                                                                                            | Change                                                        |
| ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------- |
| [TermFilterService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Services/TermFilterService.cs) | [NEW] LINQ-based filtering with AND logic                     |
| [FilterViewModel.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/ViewModels/FilterViewModel.cs)   | [NEW] Debounced search, toggle, and dropdown state management |
| [FilterBar.axaml](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Views/FilterBar.axaml)              | [NEW] Filter bar UI with search, toggles, and dropdowns       |
| [LexiconViewModel.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/ViewModels/LexiconViewModel.cs) | [MODIFY] Integrated FilterViewModel and ITermFilterService    |
| [LexiconView.axaml](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Views/LexiconView.axaml)          | [MODIFY] Added FilterBar below toolbar                        |
| [StyleModule.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/StyleModule.cs)                      | [MODIFY] Registered filter services                           |

### Test Layer (`Lexichord.Tests.Unit`)

| File                                                                                                                                          | Change                                    |
| --------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------- |
| [FilterViewModelTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/FilterViewModelTests.cs)     | [NEW] 17 unit tests for FilterViewModel   |
| [TermFilterServiceTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/TermFilterServiceTests.cs) | [NEW] 21 unit tests for TermFilterService |
| [LexiconViewModelTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/LexiconViewModelTests.cs)   | [MODIFY] Updated for new dependencies     |

## Features

1. **Debounced Text Search** (300ms default)
    - Searches Term, Replacement, and Notes fields
    - Case-insensitive substring matching

2. **Show Inactive Toggle**
    - Hides inactive terms by default
    - Toggle to include deactivated terms

3. **Category Dropdown**
    - Filters by exact category match
    - Populated dynamically from loaded terms

4. **Severity Dropdown**
    - Filters by severity level
    - Options: Error, Warning, Suggestion, Info

5. **Clear Filters Button**
    - Resets all filters to defaults
    - Only visible when filters are active

## Verification

```
Test summary: total: 59, failed: 0, succeeded: 59, skipped: 0
```
