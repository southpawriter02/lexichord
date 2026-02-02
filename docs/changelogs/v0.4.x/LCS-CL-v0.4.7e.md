# Changelog: v0.4.7e — Entity List View

**Date:** 2026-02-02  
**Author:** Antigravity  
**Status:** Complete

---

## Summary

Implements the Entity List View for the Knowledge Graph Browser, providing a filterable, virtualized list of all extracted entities with type metadata, confidence scores, and relationship counts.

---

## Added

### Abstractions

- **`IGraphRepository`** — Interface for entity queries:
    - `GetAllEntitiesAsync()` — Retrieves all knowledge entities
    - `GetRelationshipCountAsync(Guid)` — Counts relationships for an entity
    - `GetMentionCountAsync(Guid)` — Counts source document mentions

### Repository

- **`GraphRepository`** — Implementation using `IGraphSession` with Cypher queries for Neo4j

### ViewModels

- **`EntityListItemViewModel`** — Wraps `KnowledgeEntity` with display properties:
    - Icon and Color from `EntityTypeSchema`
    - Confidence from `Properties["confidence"]` with fallback to 1.0
    - Pre-fetched RelationshipCount and MentionCount

- **`EntityListViewModel`** — MVVM container with:
    - `LoadEntitiesAsync()` — Loads and wraps entities
    - `FilteredEntities` — Computed collection based on filters
    - `TypeFilter`, `SearchText`, `MinConfidenceFilter`, `DocumentFilter`
    - `ClearFiltersCommand` — Resets all filters

### Converters

- **`ConfidenceToColorConverter`** — Maps confidence to hex color:
    - `≥0.9` → Green (#22c55e)
    - `≥0.7` → Yellow (#eab308)
    - `≥0.5` → Orange (#f97316)
    - `<0.5` → Red (#ef4444)

### Views

- **`EntityListView.axaml`** — Avalonia UserControl with:
    - Filter bar (search, type dropdown, confidence slider, clear button)
    - Virtualized entity list with type icons and confidence badges
    - Status bar showing filtered/total counts

---

## Changed

- **`KnowledgeModule.cs`** — DI registrations for `IGraphRepository` (singleton), `EntityListViewModel` (transient)
- **`Lexichord.Modules.Knowledge.csproj`** — Added Avalonia 11.2.3, CommunityToolkit.Mvvm 8.4.0, version bump to 0.4.7

---

## File Manifest

| File                                                            | Change   |
| --------------------------------------------------------------- | -------- |
| `Abstractions/Contracts/IGraphRepository.cs`                    | NEW      |
| `Modules.Knowledge/Graph/GraphRepository.cs`                    | NEW      |
| `Modules.Knowledge/UI/ViewModels/EntityListItemViewModel.cs`    | NEW      |
| `Modules.Knowledge/UI/ViewModels/EntityListViewModel.cs`        | NEW      |
| `Modules.Knowledge/UI/Converters/ConfidenceToColorConverter.cs` | NEW      |
| `Modules.Knowledge/UI/Views/EntityListView.axaml`               | NEW      |
| `Modules.Knowledge/UI/Views/EntityListView.axaml.cs`            | NEW      |
| `Modules.Knowledge/KnowledgeModule.cs`                          | MODIFIED |
| `Modules.Knowledge/Lexichord.Modules.Knowledge.csproj`          | MODIFIED |

---

## Tests

- `EntityListViewModelTests.cs` — 9 tests (constructor, loading, filtering)
- `ConfidenceToColorConverterTests.cs` — 13 tests (thresholds, edge cases)

**Total:** 22 new tests, all passing
