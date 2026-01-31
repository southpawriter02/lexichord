# LCS-DES-025-INDEX: The Librarian (Terminology Management UI)

## Document Control

| Field            | Value                                                 |
| :--------------- | :---------------------------------------------------- |
| **Document ID**  | LCS-DES-025-INDEX                                     |
| **Version**      | v0.2.5                                                |
| **Codename**     | The Librarian (Terminology Management UI)             |
| **Status**       | Draft                                                 |
| **Last Updated** | 2026-01-27                                            |
| **Owner**        | Lead Architect                                        |
| **Depends On**   | v0.2.2 (Terminology Database), v0.1.1 (Layout Engine) |

---

## Executive Summary

**The Librarian** (v0.2.5) delivers a comprehensive GUI for managing style terminology rules without writing SQL. This release transforms the Terminology Database from a developer-only feature into an end-user capability, enabling WriterPro users to curate custom style guides through an intuitive Avalonia-based interface.

### Business Value

- **Accessibility:** Non-technical users can add, edit, and remove style rules through a visual interface.
- **Efficiency:** DataGrid-based browsing with sorting and filtering replaces manual database queries.
- **Quality Control:** Regex validation prevents invalid patterns from corrupting the lexicon.
- **Team Collaboration:** Import/Export enables sharing terminology libraries between teams.
- **Enterprise Ready:** Custom organizational style guides without developer intervention.

### Success Criteria

1. `LexiconView` displays all terms in a sortable Avalonia.DataGrid.
2. Users can filter terms by search text, severity, and active/deprecated status.
3. Modal dialog validates regex patterns before allowing Save.
4. CSV/Excel import adds terms in bulk; JSON export creates shareable libraries.

---

## Related Documents

| Document ID  | Title                  | Description                                      |
| :----------- | :--------------------- | :----------------------------------------------- |
| LCS-SBD-025  | Scope Breakdown v0.2.5 | Work breakdown and task planningfor this version |
| LCS-DES-025a | Terminology Grid View  | DataGrid UI for term listing and sorting         |
| LCS-DES-025b | Search & Filter        | Real-time filtering with toggle controls         |
| LCS-DES-025c | Term Editor Dialog     | Modal dialog for Add/Edit terms with validation  |
| LCS-DES-025d | Bulk Import/Export     | CSV/Excel import, JSON export for team sharing   |

---

## Architecture Overview

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              The Librarian (v0.2.5)                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                         Presentation Layer                                │  │
│  │  ┌────────────────┐  ┌──────────────┐  ┌───────────────┐                  │  │
│  │  │  LexiconView   │  │ FilterBar    │  │TermEditorDialog│                 │  │
│  │  │  (DataGrid UI) │  │  (Search)    │  │   (Add/Edit)   │                 │  │
│  │  └───────┬────────┘  └──────┬───────┘  └───────┬───────┘                  │  │
│  │          │                  │                  │                          │  │
│  │  ┌───────┴──────────────────┴──────────────────┴───────┐                  │  │
│  │  │                  LexiconViewModel                    │                  │  │
│  │  │    (Grid Logic, Selection, Commands, License)        │                  │  │
│  │  └─────────────────────────┬────────────────────────────┘                  │  │
│  └────────────────────────────┼──────────────────────────────────────────────┘  │
│                               │                                                 │
│  ┌────────────────────────────┼──────────────────────────────────────────────┐  │
│  │                         Service Layer                                     │  │
│  │  ┌─────────────────────────┴───────────────────────────┐                  │  │
│  │  │                                                     │                  │  │
│  │  │  ┌─────────────────┐  ┌─────────────────┐           │                  │  │
│  │  │  │ITermImportService│  │ITermExportService│         │                  │  │
│  │  │  │  (CSV/Excel)     │  │   (JSON/CSV)     │         │                  │  │
│  │  │  └─────────────────┘  └─────────────────┘           │                  │  │
│  │  │                                                     │                  │  │
│  │  │  ┌─────────────────┐  ┌─────────────────┐           │                  │  │
│  │  │  │ITermFilterService│  │IPatternValidator│          │                  │  │
│  │  │  │  (Grid Filter)   │  │  (Regex Check)   │         │                  │  │
│  │  │  └─────────────────┘  └─────────────────┘           │                  │  │
│  │  └─────────────────────────────────────────────────────┘                  │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
└──────────────────────────────────┬──────────────────────────────────────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │  Shared Services (v0.2.2)   │
                    │  ┌──────────────────────┐   │
                    │  │ITerminologyService   │   │
                    │  │ITerminologyRepository│   │
                    │  │StyleTerm Entity      │   │
                    │  └──────────────────────┘   │
                    └─────────────────────────────┘
```

### User Interaction Flow

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                            User Interaction Flow                               │
└────────────────────────────────────────────────────────────────────────────────┘

     ┌─────────┐
     │  User   │
     └────┬────┘
          │
          ▼
┌──────────────────────┐
│ Opens Lexicon Tab    │───────────────────────────────────────────┐
└──────────┬───────────┘                                           │
           │                                                       ▼
           ▼                                             ┌─────────────────────┐
┌───────────────────────────────────────────────────────│  LexiconView loads  │
│        LexiconView.axaml (DataGrid)                   │  ITerminologyService │
│  ┌────────────────────────────────────────────────┐   │  GetAllAsync()       │
│  │ [+ Add] [Edit] [Delete]  [Import▼] [Export▼]   │   └─────────────────────┘
│  ├────────────────────────────────────────────────┤
│  │ [🔍 Search...        ] [✓]Deprecated [✓]Error  │◄── FilterBar (025b)
│  ├────────────────────────────────────────────────┤
│  │ Pattern     │ Recommendation │ Category │ Sev  │
│  │─────────────┼────────────────┼──────────┼──────│
│  │ click on    │ Use 'select'...│ Terms    │ ⚠️   │
│  │ e-mail      │ Use 'email'... │ Terms    │ 💡   │
│  │ utilize     │ Use 'use'...   │ Clarity  │ 💡   │
│  ├────────────────────────────────────────────────┤
│  │ 127 terms                        │ WriterPro  │
│  └────────────────────────────────────────────────┘
│                     │
│     ┌───────────────┼───────────────┐
│     ▼               ▼               ▼
│ [Double-Click]  [Right-Click]  [Filter Text]
│     │               │               │
│     ▼               ▼               ▼
│ TermEditor    ContextMenu     FilterService
│  Dialog        (Edit/Del)      ApplyFilters()
│  (025c)                          (025b)
└────────────────────────────────────────────────────────────────────────────────┘
```

---

## Dependencies

### Upstream Dependencies

| Component                | Source Version | Usage in v0.2.5                           |
| :----------------------- | :------------- | :---------------------------------------- |
| `ITerminologyRepository` | v0.2.2b        | Data access for term CRUD operations      |
| `ITerminologyService`    | v0.2.2d        | Business logic and event publication      |
| `StyleTerm`              | v0.2.2b        | Entity model displayed in DataGrid        |
| `LexiconChangedEvent`    | v0.2.2d        | Refresh grid when terms change            |
| `IRegionManager`         | v0.1.1b        | Register LexiconView in Right dock region |
| `ILayoutService`         | v0.1.1c        | Persist panel state                       |
| `ILicenseContext`        | v0.0.4c        | Read-only license tier check              |
| `ILicenseService`        | v0.1.6c        | Gate WriterPro features                   |
| `IMediator`              | v0.0.7a        | Handle LexiconChangedEvent for refresh    |

### External Dependencies

| Component         | Version | Usage                                 |
| :---------------- | :------ | :------------------------------------ |
| Avalonia.DataGrid | 11.x    | DataGrid control for term listing     |
| CsvHelper         | 31.x    | CSV parsing for import                |
| ClosedXML         | 0.102.x | Excel (.xlsx) file parsing for import |

---

## License Gating Strategy

The Librarian is a **WriterPro** feature. Tier behavior:

| Feature            | Core (Free) | Writer     | WriterPro |
| :----------------- | :---------- | :--------- | :-------- |
| View Terms         | Read-only   | Read-only  | ✅ Full   |
| Search & Filter    | ✅ Yes      | ✅ Yes     | ✅ Yes    |
| Add/Edit Terms     | ❌ Upgrade  | ❌ Upgrade | ✅ Yes    |
| Delete Terms       | ❌ Upgrade  | ❌ Upgrade | ✅ Yes    |
| Import (CSV/Excel) | ❌ Upgrade  | ❌ Upgrade | ✅ Yes    |
| Export (JSON/CSV)  | ❌ Upgrade  | ❌ Upgrade | ✅ Yes    |

---

## Key Interfaces Summary

| Interface            | Purpose                                        | Module                  |
| :------------------- | :--------------------------------------------- | :---------------------- |
| `ITermFilterService` | Apply combined filter criteria to term list    | Lexichord.Modules.Style |
| `IPatternValidator`  | Validate regex patterns for safety/correctness | Lexichord.Modules.Style |
| `ITermImportService` | Parse and import terms from CSV/Excel files    | Lexichord.Modules.Style |
| `ITermExportService` | Export terms to JSON/CSV files                 | Lexichord.Modules.Style |

---

## Implementation Checklist Summary

| Sub-Part  | Focus Area                | Key Deliverables                                     | Est. Hours |
| :-------- | :------------------------ | :--------------------------------------------------- | :--------- |
| v0.2.5a   | Terminology Grid View     | LexiconView, LexiconViewModel, DataGrid columns      | 15h        |
| v0.2.5b   | Search & Filter           | FilterBar, SearchFilterViewModel, ITermFilterService | 12h        |
| v0.2.5c   | Term Editor Dialog        | TermEditorDialog, validation, pattern test           | 14h        |
| v0.2.5d   | Bulk Import/Export        | CSV/Excel import, JSON/CSV export, progress dialog   | 18h        |
|           | **Integration & Testing** | Module registration, integration tests               | 5h         |
| **Total** |                           |                                                      | **64h**    |

---

## Success Criteria Summary

| Metric                     | Target  | Measurement Method        |
| :------------------------- | :------ | :------------------------ |
| Grid load time (500 terms) | < 200ms | Stopwatch timing          |
| Filter response time       | < 100ms | Debounce + refresh timing |
| Import 100 terms           | < 2s    | Progress dialog timing    |
| Export 1000 terms to JSON  | < 1s    | File write timing         |
| Memory usage (1000 rows)   | < 50MB  | Memory profiler           |

---

## Test Coverage Summary

| Test Type         | Focus Area                                   | Coverage Target |
| :---------------- | :------------------------------------------- | :-------------- |
| Unit Tests        | ViewModels, FilterService, PatternValidator  | 90%             |
| Unit Tests        | Import/Export Services                       | 85%             |
| Integration Tests | Full CRUD workflow through UI                | Key paths       |
| Manual Tests      | UI appearance, accessibility, license gating | Full coverage   |

---

## What This Enables

After v0.2.5, Lexichord will support:

- **v0.2.6:** Style Analysis Engine can use user-defined terms.
- **v0.2.7:** Real-time style checking during document editing.
- **v0.3.x:** Custom style profiles (organization-specific).
- **v0.4.x:** Style guide templates marketplace.
- **Future:** AI-assisted term suggestion based on writing patterns.

---

## Risks & Mitigations

| Risk                                    | Impact | Probability | Mitigation                                   |
| :-------------------------------------- | :----- | :---------- | :------------------------------------------- |
| DataGrid performance with 10,000+ terms | Medium | Medium      | Enable virtualization; paginate if needed    |
| Regex validation bypassed via import    | High   | Low         | Validate all imported patterns before insert |
| Excel file corrupted or malformed       | Medium | Medium      | Robust error handling; preview before import |
| Large import blocks UI thread           | High   | Medium      | Use async/await; show progress dialog        |
| User loses unsaved changes              | Medium | Low         | Confirm before closing dirty editor          |

---

## Document History

| Version | Date       | Author         | Changes                         |
| :------ | :--------- | :------------- | :------------------------------ |
| 0.1     | 2026-01-27 | Lead Architect | Initial INDEX creation from SBD |
