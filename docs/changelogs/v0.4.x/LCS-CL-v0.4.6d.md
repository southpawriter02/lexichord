# Changelog: v0.4.6d - Search History Enhancement

**Version:** v0.4.6d  
**Codename:** The Reference Panel  
**Status:** ✅ Complete  
**Date:** 2026-02-03

## Summary

Enhanced the Search History Service with persistence, removal capability, and change notifications. This enables search history to persist across sessions and provides reactive updates to the UI.

## Changes

### Lexichord.Abstractions

#### `Contracts/RAG/ISearchHistoryService.cs`

- **Added** `RecentQueries` property for direct access to full history
- **Added** `RemoveQuery(string)` method for user-initiated item removal
- **Added** `SaveAsync()` for persisting history to settings
- **Added** `LoadAsync()` for restoring history from settings
- **Added** `HistoryChanged` event with `SearchHistoryChangedEventArgs`
- **Added** `SearchHistoryChangeType` enum (`Added`, `Removed`, `Cleared`, `Loaded`)
- **Added** `SearchHistoryChangedEventArgs` class

### Lexichord.Modules.RAG

#### `Search/SearchHistoryService.cs`

- **Enhanced** with `ISystemSettingsRepository` dependency for persistence
- **Implemented** `RecentQueries` property with thread-safe snapshot
- **Implemented** `RemoveQuery()` with case-insensitive matching
- **Implemented** `SaveAsync()` with dirty state tracking
- **Implemented** `LoadAsync()` with JSON deserialization and max size enforcement
- **Implemented** `IDisposable` for automatic save on dispose
- **Added** `HistoryChanged` event firing on all mutations

#### `RAGModule.cs`

- **Updated** DI registration to use factory with `ISystemSettingsRepository`
- **Added** automatic history loading on service initialization

### Lexichord.Tests.Unit

#### `Modules/RAG/Search/SearchHistoryServiceTests.cs`

- **Added** 27 new tests for v0.4.6d features
- Tests cover: `RecentQueries`, `RemoveQuery`, `SaveAsync`, `LoadAsync`, `HistoryChanged` events, thread safety

## Test Results

| Category           | Passed | Failed | Total |
| ------------------ | ------ | ------ | ----- |
| v0.4.6a (original) | 20     | 0      | 20    |
| v0.4.6d (enhanced) | 27     | 0      | 27    |
| **Total**          | 47     | 0      | 47    |

## Dependencies

| Package          | Version    | Purpose                            |
| ---------------- | ---------- | ---------------------------------- |
| System.Text.Json | (built-in) | JSON serialization for persistence |

## Breaking Changes

None. All v0.4.6a interface members are preserved.

## Related Specifications

- [LCS-DES-v0.4.6d](../../specs/v0.4.x/v0.4.6/LCS-DES-v0.4.6d.md) - Design Specification
- [LCS-SBD-v0.4.6](../../specs/v0.4.x/v0.4.6/LCS-SBD-v0.4.6.md) - Scope Breakdown
