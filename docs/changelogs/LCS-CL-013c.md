# LCS-CL-013c: Search & Replace Overlay

**Version**: v0.1.3c  
**Date**: 2026-01-29  
**Status**: ✅ Complete

## Overview

Implements an inline search and replace overlay for the Manuscript editor with live highlighting, navigation, and replace functionality. This sub-part provides essential text finding capabilities while adhering to security best practices for regex operations.

## New Files

### Abstractions Layer

| File                     | Description                                                              |
| ------------------------ | ------------------------------------------------------------------------ |
| `SearchContracts.cs`     | `SearchOptions`, `SearchResult`, `SearchResultsChangedEventArgs` records |
| `SearchExecutedEvent.cs` | MediatR domain event for analytics                                       |

### Editor Module - Services

| File                   | Description                                               |
| ---------------------- | --------------------------------------------------------- |
| `ISearchService.cs`    | Interface defining search service contract                |
| `SearchService.cs`     | Implementation with regex matching, highlighting, replace |
| `TextMarkerService.cs` | AvalonEdit background renderer for match highlighting     |

### Editor Module - ViewModels

| File                        | Description                                    |
| --------------------------- | ---------------------------------------------- |
| `SearchOverlayViewModel.cs` | MVVM ViewModel with debounced search, commands |

### Editor Module - Views

| File                         | Description                        |
| ---------------------------- | ---------------------------------- |
| `SearchOverlayView.axaml`    | XAML UI for search/replace overlay |
| `SearchOverlayView.axaml.cs` | Code-behind with focus helper      |

### Tests

| File                    | Description                                        |
| ----------------------- | -------------------------------------------------- |
| `SearchServiceTests.cs` | 13 new unit tests for search contracts and service |

## Modified Files

| File                          | Changes                                                                                        |
| ----------------------------- | ---------------------------------------------------------------------------------------------- |
| `ManuscriptViewModel.cs`      | Added `ISearchService`, `IMediator`, `ILoggerFactory` dependencies; `SearchViewModel` property |
| `ManuscriptView.axaml`        | Integrated `SearchOverlayView` component                                                       |
| `ManuscriptView.axaml.cs`     | Added F3/Shift+F3/Ctrl+H handlers; SearchService attachment                                    |
| `EditorModule.cs`             | Registered `ISearchService` as transient                                                       |
| `ManuscriptViewModelTests.cs` | Updated constructor for new dependencies                                                       |

## New Contracts

### SearchOptions

```csharp
public record SearchOptions(
    bool MatchCase = false,
    bool WholeWord = false,
    bool UseRegex = false
);
```

### SearchResult

```csharp
public record SearchResult(
    int StartOffset,
    int Length,
    string MatchedText,
    int Line,
    int Column
);
```

### ISearchService

Primary methods:

- `FindNext()`, `FindPrevious()` - Navigate between matches
- `FindAll()` - Get all matches
- `ReplaceCurrent()`, `ReplaceAll()` - Replace operations
- `HighlightAllMatches()`, `ClearHighlights()` - Visual feedback

## Implementation Details

### Security: ReDoS Mitigation

All regex operations use a 1-second timeout to prevent catastrophic backtracking:

```csharp
return new Regex(pattern, regexOptions, TimeSpan.FromSeconds(1));
```

### Debounced Search

SearchOverlayViewModel debounces search text input by 150ms:

```csharp
await Task.Delay(DebounceDelayMs, cancellationToken);
```

### Replace All Algorithm

Replaces matches from end to start to preserve document offsets:

```csharp
for (var i = matches.Count - 1; i >= 0; i--)
{
    var match = matches[i];
    _document.Replace(match.StartOffset, match.Length, replaceText);
}
```

### Keyboard Shortcuts

| Shortcut      | Action                        |
| ------------- | ----------------------------- |
| `Ctrl+F`      | Show search overlay           |
| `F3`          | Find next match               |
| `Shift+F3`    | Find previous match           |
| `Escape`      | Hide search overlay           |
| `Ctrl+H`      | Toggle replace visibility     |
| `Enter`       | Find next (in search box)     |
| `Shift+Enter` | Find previous (in search box) |

## Test Summary

17 total tests passing:

- 17 existing ManuscriptViewModelTests updated for new dependencies
- 13 new SearchServiceTests covering:
    - SearchOptions default values and immutability
    - SearchResult properties
    - SearchResultsChangedEventArgs properties
    - Regex behavior (case, whole word, regex mode)
    - Service visibility (Show/Hide)
    - Match count properties
    - Event subscription

## Dependencies

- **v0.1.3a**: AvalonEdit integration (TextEditor control)
- **v0.0.7a**: MediatR bootstrap (event publishing)
- **CommunityToolkit.Mvvm**: ViewModel support

## Design Notes

1. **TextMarkerService**: Custom AvalonEdit background renderer that tracks document changes and updates marker positions accordingly.

2. **Lazy SearchViewModel Creation**: SearchOverlayViewModel is created lazily on first search invocation to avoid unnecessary allocations.

3. **Parent DataContext Binding**: SearchOverlay's IsVisible binds to ManuscriptViewModel while its own DataContext is SearchOverlayViewModel, requiring code-behind wiring.

## Related Specifications

- [LCS-DES-013c](../specs/v0.1.x/v0.1.3/LCS-DES-013c.md) - Design Specification
- [LCS-SBD-013](../specs/v0.1.x/v0.1.3/LCS-SBD-013.md) - Scope Breakdown
