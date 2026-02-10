# v0.6.6h — Entity Citation Renderer

**Released:** 2026-02-10  
**Spec:** LCS-DES-v0.6.6-KG-h  
**Component:** Co-pilot Agent (CKVS Phase 3b — Knowledge-Aware Transparency)

---

## Overview

Adds the Entity Citation Renderer — the transparency layer between validated LLM output and the user. Generates citation markup showing which Knowledge Graph entities informed each Co-pilot response, with verification status, type icons, and multiple display formats.

## What's New

### Abstractions (`Lexichord.Abstractions`)

| Type | File | Description |
|------|------|-------------|
| `ValidatedGenerationResult` | `Contracts/Knowledge/Copilot/ValidatedGenerationResult.cs` | Aggregated result record: `Content`, `SourceEntities`, `PostValidation` |
| `IEntityCitationRenderer` | `Contracts/Knowledge/Copilot/IEntityCitationRenderer.cs` | Interface: `GenerateCitations`, `GetCitationDetail` |
| `CitationMarkup` | `Contracts/Knowledge/Copilot/CitationMarkup.cs` | Output record: `Citations`, `ValidationStatus`, `Icon`, `FormattedMarkup` |
| `EntityCitation` | `Contracts/Knowledge/Copilot/EntityCitation.cs` | Single citation: `EntityId`, `EntityType`, `EntityName`, `DisplayLabel`, `Confidence`, `IsVerified`, `TypeIcon` |
| `CitationOptions` | `Contracts/Knowledge/Copilot/CitationOptions.cs` | Options: `Format`, `MaxCitations`, `ShowValidationStatus`, `ShowConfidence`, `GroupByType` |
| `EntityCitationDetail` | `Contracts/Knowledge/Copilot/EntityCitationDetail.cs` | Detail record: `Entity`, `UsedProperties`, `CitedRelationships`, `DerivedClaims`, `BrowserLink` |
| `ValidationIcon` | `Contracts/Knowledge/Copilot/ValidationIcon.cs` | Enum: `CheckMark`, `Warning`, `Error`, `Question` |
| `CitationFormat` | `Contracts/Knowledge/Copilot/CitationFormat.cs` | Enum: `Compact`, `Detailed`, `TreeView`, `Inline` |

### Implementation (`Lexichord.Modules.Knowledge`)

| Type | File | Description |
|------|------|-------------|
| `EntityCitationRenderer` | `Copilot/UI/EntityCitationRenderer.cs` | Implements `IEntityCitationRenderer` with type icons, entity verification, display label formatting, and Compact/Detailed/TreeView output formats |
| `CitationPanel` | `Copilot/UI/CitationPanel.axaml` | Avalonia UserControl with citations list, validation status footer |
| `CitationPanel` (code-behind) | `Copilot/UI/CitationPanel.axaml.cs` | Minimal code-behind |
| `CitationViewModel` | `Copilot/UI/CitationViewModel.cs` | Observable ViewModel bridging `CitationMarkup` to UI bindings |
| `CitationItemViewModel` | `Copilot/UI/CitationItemViewModel.cs` | Item ViewModel for individual citation rows |

### Unit Tests

| Test Class | Count | File |
|-----------|-------|------|
| `EntityCitationRendererTests` | 22 | `Abstractions/Knowledge/Copilot/EntityCitationRendererTests.cs` |

Test coverage includes: compact/detailed/tree-view formats, type grouping, max citation limiting, type icon mapping, display label formatting, validation icon mapping, entity verification logic, citation detail (used properties, derived claims, browser link), and data record defaults.

## Spec Deviations

| Deviation | Reason |
|-----------|--------|
| `ValidatedGenerationResult` created as new type | Not found in codebase; required as input to renderer |
| Namespace `Lexichord.Modules.Knowledge.Copilot.UI` | Spec used `Lexichord.KnowledgeGraph.Copilot.UI`; adapted to match project convention |
| Avalonia AXAML instead of WPF XAML | Project uses Avalonia, not WPF |
| `PostValidationStatus` instead of `ValidationStatus` | Spec's `ValidationStatus` enum does not exist; reused existing `PostValidationStatus` |
| `IBrush` instead of `Brush` | Avalonia uses `IBrush` interface |

## Dependencies

| Dependency | Version | Usage |
|-----------|---------|-------|
| `KnowledgeEntity` | v0.4.5e | Cited entity data |
| `KnowledgeRelationship` | v0.4.5e | Cited relationships |
| `PostValidationResult` | v0.6.6g | Validation findings, status |
| `PostValidationStatus` | v0.6.6g | Validation status mapping |
| `Claim` / `ClaimEntity` | v0.5.6e | Derived claims extraction |
| `ValidationFinding` | v0.6.5e | Entity verification check |
| `CommunityToolkit.Mvvm` | — | `ObservableObject`, `[ObservableProperty]` |
| `Avalonia.Media` | — | `IBrush`, `Brushes` |
