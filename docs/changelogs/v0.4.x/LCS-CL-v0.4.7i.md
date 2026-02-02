# Changelog: v0.4.7i - Axiom Viewer

**Release Date:** 2026-02-02  
**Component:** Knowledge Graph Browser  
**Status:** Complete

---

## Summary

Implements the Axiom Viewer panel for the Knowledge Graph Browser, providing a read-only view of all axioms (validation rules) with filtering, grouping, and human-readable rule formatting.

---

## Added

### ViewModels

- **`AxiomItemViewModel`** - Wraps an `Axiom` for display with:
    - Property mapping (Id, Name, Description, TargetType, Severity, etc.)
    - `FormattedRules` - Human-readable rule descriptions
    - `FormatRuleDescription()` - Formats all 14 constraint types
    - `FormatCondition()` - Formats all 9 condition operators

- **`AxiomViewerViewModel`** - Main panel ViewModel with:
    - 5-dimension filtering (SearchText, TypeFilter, SeverityFilter, CategoryFilter, ShowDisabled)
    - Grouping by TargetType, Category, or Severity
    - `LoadAxiomsAsync()` - Loads from `IAxiomStore`
    - `ClearFilters()` - Resets all filters
    - Dynamic `AvailableTypes` and `AvailableCategories` dropdowns

### Views

- **`AxiomViewerView.axaml`** - XAML layout with:
    - Header with count display and loading indicator
    - Filter bar with search, dropdowns, and clear button
    - Axiom cards with severity badges, rules list, tags, and metadata
    - Status bar footer

### DI Registration

- Registered `AxiomViewerViewModel` as transient in `KnowledgeModule.cs`

---

## Testing

### AxiomItemViewModelTests (35+ tests)

- Constructor validation and property mapping
- All 14 `AxiomConstraintType` formatting tests
- All 9 `ConditionOperator` formatting tests
- `FormattedRules` lazy evaluation

### AxiomViewerViewModelTests (28 tests)

- Constructor null-check and initialization tests
- `LoadAxiomsAsync` loading state and collection population
- Filter tests for each dimension (SearchText, TypeFilter, SeverityFilter, CategoryFilter, ShowDisabled)
- Combined filter AND logic
- `ClearFilters` functionality
- Cancellation handling

**Test Command:**

```bash
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~AxiomViewer"
```

---

## Dependencies

| Dependency            | Source Version |
| --------------------- | -------------- |
| `IAxiomStore`         | v0.4.6h        |
| `ISchemaRegistry`     | v0.4.5f        |
| `Axiom`, `AxiomRule`  | v0.4.6e        |
| `AxiomConstraintType` | v0.4.6e        |
| `ConditionOperator`   | v0.4.6e        |

---

## Technical Notes

1. **Rule Formatting**: The `FormatRuleDescription()` method handles all 14 constraint types with context-aware human-readable output (e.g., range with min-only vs max-only vs both).

2. **Lazy Evaluation**: `FormattedRules` uses `Lazy<T>` for performance - rules are only formatted when accessed.

3. **Filter Combination**: All filters combine with AND logic - an axiom must pass all active filters to appear.

4. **UI Thread**: `LoadAxiomsAsync` uses `await Task.Yield()` to allow UI updates before heavy processing.

---

## Files Changed

| File                                                                    | Change       |
| ----------------------------------------------------------------------- | ------------ |
| `src/Lexichord.Modules.Knowledge/UI/ViewModels/AxiomItemViewModel.cs`   | **New**      |
| `src/Lexichord.Modules.Knowledge/UI/ViewModels/AxiomViewerViewModel.cs` | **New**      |
| `src/Lexichord.Modules.Knowledge/UI/Views/AxiomViewerView.axaml`        | **New**      |
| `src/Lexichord.Modules.Knowledge/UI/Views/AxiomViewerView.axaml.cs`     | **New**      |
| `src/Lexichord.Modules.Knowledge/KnowledgeModule.cs`                    | **Modified** |
| `tests/.../ViewModels/AxiomItemViewModelTests.cs`                       | **New**      |
| `tests/.../ViewModels/AxiomViewerViewModelTests.cs`                     | **New**      |
