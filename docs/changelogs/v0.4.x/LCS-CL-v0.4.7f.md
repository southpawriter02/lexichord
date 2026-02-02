# Changelog: v0.4.7f — Entity Detail View

**Date:** 2026-02-02  
**Author:** Antigravity  
**Status:** Complete

---

## Summary

Implements the Entity Detail View for the Knowledge Graph Browser, displaying comprehensive information about a selected entity including properties, relationships, and source documents with navigation support.

---

## Added

### Abstractions

- **`IGraphRepository.GetRelationshipsForEntityAsync()`** — Retrieves all relationships connected to an entity (both directions)
- **`IGraphRepository.GetMentionCountAsync(entityId, docId)`** — Counts mentions of an entity in a specific document

### Repository

- **`GraphRepository`** — Implements new methods using Cypher queries and document tracking

### ViewModels

- **`PropertyItemViewModel`** — Record with Name, Value, Type, Description, IsRequired
- **`RelationshipItemViewModel`** — Record with Id, Type, Direction, OtherEntity details, Icon
- **`SourceDocumentItemViewModel`** — Record with DocumentId, Title, Path, MentionCount
- **`EntityDetailViewModel`** — Main ViewModel with:
    - Observable properties: Entity, Name, Type, Icon, Confidence, IsLoading, CanEdit
    - Collections: Properties, Relationships, SourceDocuments
    - Commands: NavigateToSource, NavigateToRelatedEntity, CopyPropertyValue
    - OnEntityChanged async loading
    - License-gated edit permissions (Teams tier required)

### Views

- **`EntityDetailView.axaml`** — Avalonia UserControl with:
    - Header section: Entity icon, name, type, confidence badge
    - Properties section: Expandable list with copy-to-clipboard
    - Relationships section: Clickable links to related entities
    - Source Documents section: Clickable links to open in editor
    - Action buttons: Edit/Merge/Delete (license-gated)
    - Loading indicator and empty state

---

## Changed

- **`KnowledgeModule.cs`** — DI registration for `EntityDetailViewModel` (transient)

---

## File Manifest

| File                                                             | Change   |
| ---------------------------------------------------------------- | -------- |
| `Abstractions/Contracts/IGraphRepository.cs`                     | MODIFIED |
| `Modules.Knowledge/Graph/GraphRepository.cs`                     | MODIFIED |
| `Modules.Knowledge/UI/ViewModels/PropertyItemViewModel.cs`       | NEW      |
| `Modules.Knowledge/UI/ViewModels/RelationshipItemViewModel.cs`   | NEW      |
| `Modules.Knowledge/UI/ViewModels/SourceDocumentItemViewModel.cs` | NEW      |
| `Modules.Knowledge/UI/ViewModels/EntityDetailViewModel.cs`       | NEW      |
| `Modules.Knowledge/UI/Views/EntityDetailView.axaml`              | NEW      |
| `Modules.Knowledge/UI/Views/EntityDetailView.axaml.cs`           | NEW      |
| `Modules.Knowledge/KnowledgeModule.cs`                           | MODIFIED |

---

## Tests

- `EntityDetailViewModelTests.cs` — 15 tests covering:
    - Constructor null guard tests (6 tests)
    - Default initialization test
    - License tier tests (WriterPro, Teams, Enterprise)
    - Entity loading tests (properties, relationships, source documents)
    - Clear on null entity test
    - Navigate commands tests

**Total:** 15 new tests
