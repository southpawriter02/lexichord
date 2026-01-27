# LCS-DES-026-INDEX: The Sidebar (Real-Time Feedback)

## Document Control

| Field            | Value                                                                       |
| :--------------- | :-------------------------------------------------------------------------- |
| **Document ID**  | LCS-DES-026-INDEX                                                           |
| **Version**      | v0.2.6                                                                      |
| **Codename**     | The Sidebar (Real-Time Feedback)                                            |
| **Status**       | Draft                                                                       |
| **Last Updated** | 2026-01-27                                                                  |
| **Owner**        | Lead Architect                                                              |
| **Depends On**   | v0.2.3 (Linter Engine), v0.2.4 (Editor Integration), v0.1.1 (Layout Engine) |

---

## Executive Summary

**The Sidebar** (v0.2.6) delivers a VS Code-style Problems Panel that provides centralized, real-time monitoring of document health. This module transforms the passive violation display from v0.2.4 (squiggly underlines) into an active, navigable command center for style compliance.

### Business Value

- **Centralized Overview:** All violations displayed in a single, organized panel.
- **Efficient Navigation:** Double-click any issue to jump directly to its location.
- **Gamification:** Compliance Score motivates writers to address issues and track progress.
- **Flexible Scope:** Toggle between Current File, Open Files, and Project-wide views.
- **Professional UX:** Familiar VS Code-like interface reduces learning curve.
- **Enterprise Ready:** Foundation for team-wide style compliance reporting.

### Success Criteria

1. Problems Panel displays all violations grouped by severity.
2. Double-click navigation scrolls editor to violation location.
3. Compliance Score updates in real-time with gamification elements.
4. Scope toggle enables Current File, Open Files, and Project views.

---

## Related Documents

| Document ID  | Title            | Description                                   |
| :----------- | :--------------- | :-------------------------------------------- |
| LCS-SBD-026  | Scope Breakdown  | Work breakdown and task planning              |
| LCS-DES-026a | Problems Panel   | VS Code-style sidebar with grouped violations |
| LCS-DES-026b | Navigation Sync  | Double-click to jump to violation location    |
| LCS-DES-026c | Scorecard Widget | Compliance score with gamification            |
| LCS-DES-026d | Filter Scope     | Current File / Open Files / Project toggle    |

---

## Architecture Overview

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              The Sidebar (v0.2.6)                               │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                         Presentation Layer                                │  │
│  │  ┌────────────────┐  ┌──────────────┐  ┌───────────────┐                  │  │
│  │  │ ProblemsPanel  │  │ FilterBar    │  │ ScorecardWidget│                 │  │
│  │  │  (TreeView)    │  │ (Scope Tog.) │  │   (Score Disp) │                 │  │
│  │  └───────┬────────┘  └──────┬───────┘  └───────┬───────┘                  │  │
│  │          │                  │                  │                          │  │
│  │  ┌───────┴──────────────────┴──────────────────┴───────┐                  │  │
│  │  │               ProblemsPanelViewModel                │                  │  │
│  │  │    (Violations, Groups, Navigation, Scorecard)      │                  │  │
│  │  └─────────────────────────┬────────────────────────────┘                  │  │
│  └────────────────────────────┼──────────────────────────────────────────────┘  │
│                               │                                                 │
│  ┌────────────────────────────┼──────────────────────────────────────────────┐  │
│  │                         Service Layer                                     │  │
│  │  ┌─────────────────────────┴───────────────────────────┐                  │  │
│  │  │                                                     │                  │  │
│  │  │  ┌─────────────────────┐  ┌────────────────────┐    │                  │  │
│  │  │  │IEditorNavigationSvc │  │IProjectLintingService│   │                  │  │
│  │  │  │  (scroll-to-line)   │  │  (background lint)   │   │                  │  │
│  │  │  └─────────────────────┘  └────────────────────┘    │                  │  │
│  │  │                                                     │                  │  │
│  │  │  ┌─────────────────────┐  ┌────────────────────┐    │                  │  │
│  │  │  │IScorecardViewModel  │  │IgnorePatternMatcher│    │                  │  │
│  │  │  │  (score calc)       │  │ (.lexichordignore) │    │                  │  │
│  │  │  └─────────────────────┘  └────────────────────┘    │                  │  │
│  │  └─────────────────────────────────────────────────────┘                  │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
└──────────────────────────────────┬──────────────────────────────────────────────┘
                                   │
          ┌────────────────────────┼─────────────────────────┐
          │                        │                         │
┌─────────┴─────────┐  ┌───────────┴──────────┐  ┌──────────┴──────────┐
│  Linting Engine   │  │   Editor Module      │  │   Event Bus         │
│     (v0.2.3)      │  │     (v0.1.3)         │  │    (v0.0.7)         │
│  ┌─────────────┐  │  │  ┌─────────────┐     │  │  ┌─────────────┐    │
│  │ILintingOrch │  │  │  │IEditorSvc   │     │  │  │IMediator    │    │
│  │IStyleScanner│  │  │  │Manuscript   │     │  │  │LintingEvent │    │
│  │StyleViolation│ │  │  │ActiveDoc    │     │  │  │ProgressEvent│    │
│  └─────────────┘  │  │  └─────────────┘     │  │  └─────────────┘    │
└───────────────────┘  └──────────────────────┘  └─────────────────────┘
```

### Event-Driven Architecture Flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            Real-Time Update Flow                                │
└─────────────────────────────────────────────────────────────────────────────────┘

    User Types               Linter Runs              Event Published
        │                        │                         │
        ▼                        ▼                         ▼
  ┌──────────┐            ┌──────────────┐         ┌───────────────────┐
  │ Document │────────────│LintingOrch.  │─────────│LintingCompletedEvt│
  │  Change  │  debounce  │LintAsync()   │ publish │   (via IMediator) │
  └──────────┘            └──────────────┘         └─────────┬─────────┘
                                                             │
                                          ┌──────────────────┼──────────────┐
                                          │                  │              │
                                          ▼                  ▼              ▼
                                 ┌──────────────┐   ┌───────────────┐ ┌──────────┐
                                 │ProblemsPanel │   │ScorecardWidget│ │Editor    │
                                 │ViewModel    │   │  ViewModel    │ │Squiggles │
                                 │ UpdateList() │   │  Update()    │ │(v0.2.4)  │
                                 └──────────────┘   └───────────────┘ └──────────┘
```

---

## Dependencies

### Upstream Dependencies

| Component               | Source Version | Usage in v0.2.6                              |
| :---------------------- | :------------- | :------------------------------------------- |
| `LintingCompletedEvent` | v0.2.3d        | Primary data source for violation updates    |
| `StyleViolation`        | v0.2.1b        | Violation data model with position, severity |
| `ViolationSeverity`     | v0.2.1b        | Error, Warning, Info severity levels         |
| `ILintingOrchestrator`  | v0.2.3a        | Access to linting state and trigger linting  |
| `IViolationAggregator`  | v0.2.3d        | Get violations for specific documents        |
| `IStyleScanner`         | v0.2.3c        | File scanning for project-wide linting       |
| `ManuscriptView`        | v0.2.4a        | Editor integration for scroll-to-line        |
| `IRegionManager`        | v0.1.1b        | Register sidebar in Bottom dock region       |
| `ILayoutService`        | v0.1.1c        | Persist panel visibility and size            |
| `IMediator`             | v0.0.7a        | Event subscription for lint completions      |
| `IEditorService`        | v0.1.3a        | Access to open documents and active document |

### External Dependencies

| Component         | Version | Usage                                    |
| :---------------- | :------ | :--------------------------------------- |
| Avalonia TreeView | 11.x    | Grouped violation display                |
| CommunityToolkit  | 8.x     | MVVM infrastructure (ObservableProperty) |

---

## License Gating Strategy

The Sidebar is a **Core** feature available to all tiers:

| Feature              | Core (Free) | Writer      | WriterPro   |
| :------------------- | :---------- | :---------- | :---------- |
| Problems Panel       | ✅ Yes      | ✅ Yes      | ✅ Yes      |
| Navigation (Click)   | ✅ Yes      | ✅ Yes      | ✅ Yes      |
| Scorecard Widget     | ✅ Yes      | ✅ Yes      | ✅ Yes      |
| Scope: Current File  | ✅ Yes      | ✅ Yes      | ✅ Yes      |
| Scope: Open Files    | ✅ Yes      | ✅ Yes      | ✅ Yes      |
| Scope: Project-wide  | ✅ Yes      | ✅ Yes      | ✅ Yes      |
| Quick-Fix from Panel | ❌ (v0.2.7) | ❌ (v0.2.7) | ✅ (v0.2.7) |

---

## Key Interfaces Summary

| Interface                  | Purpose                                     | Module                  |
| :------------------------- | :------------------------------------------ | :---------------------- |
| `IProblemsPanelViewModel`  | Main ViewModel for Problems Panel           | Lexichord.Modules.Style |
| `IEditorNavigationService` | Navigate editor to specific line/column     | Lexichord.Modules.Style |
| `IScorecardViewModel`      | Calculate and display compliance score      | Lexichord.Modules.Style |
| `IProjectLintingService`   | Background linting for Open Files / Project | Lexichord.Modules.Style |

---

## Implementation Checklist Summary

| Sub-Part  | Focus Area                | Key Deliverables                                   | Est. Hours |
| :-------- | :------------------------ | :------------------------------------------------- | :--------- |
| v0.2.6a   | Problems Panel            | ProblemsPanel, ProblemItemVM, TreeView grouping    | 22h        |
| v0.2.6b   | Navigation Sync           | EditorNavigationService, scroll-to-line, highlight | 17h        |
| v0.2.6c   | Scorecard Widget          | ScorecardWidget, score calculation, trend          | 15h        |
| v0.2.6d   | Filter Scope              | Scope toggle, ProjectLintingService, caching       | 21.5h      |
|           | **Integration & Testing** | Integration tests, performance benchmarks          | 8h         |
| **Total** |                           |                                                    | **83.5h**  |

---

## Success Criteria Summary

| Metric                     | Target  | Measurement Method             |
| :------------------------- | :------ | :----------------------------- |
| Panel render time          | < 100ms | Stopwatch from event to render |
| Navigation latency         | < 200ms | Double-click to scroll         |
| Score calculation time     | < 10ms  | Stopwatch in CalculateScore    |
| Background lint (10 files) | < 5s    | Multi-document lint            |
| Project lint (100 files)   | < 30s   | Project-wide lint              |
| Memory per 1000 violations | < 10MB  | Memory profiler                |

---

## Test Coverage Summary

| Test Type         | Focus Area                                       | Coverage Target |
| :---------------- | :----------------------------------------------- | :-------------- |
| Unit Tests        | ProblemsPanelViewModel, ScorecardViewModel       | 90%             |
| Unit Tests        | EditorNavigationService, ProjectLintingService   | 85%             |
| Integration Tests | Full navigation flow, event subscription         | Key paths       |
| Manual Tests      | UI appearance, keyboard navigation, scope toggle | Full coverage   |

---

## What This Enables

After v0.2.6, Lexichord will support:

- **v0.2.7:** Quick-fix actions directly from Problems Panel
- **v0.3.x:** AI-powered fix suggestions in sidebar
- **v0.4.x:** Team compliance dashboards
- **Enterprise:** Exportable compliance reports

---

## Risks & Mitigations

| Risk                                  | Impact | Probability | Mitigation                                   |
| :------------------------------------ | :----- | :---------- | :------------------------------------------- |
| Performance with many violations      | High   | Medium      | Virtualize TreeView; limit displayed items   |
| Background linting blocks UI          | High   | Medium      | Use Task.Run with async patterns             |
| Memory pressure with large projects   | Medium | Medium      | LRU cache with max size                      |
| Navigation fails for closed documents | Medium | Medium      | Verify document open; show toast error       |
| TreeView sorting performance          | Medium | Low         | Batch updates with ObservableRangeCollection |

---

## Document History

| Version | Date       | Author         | Changes                         |
| :------ | :--------- | :------------- | :------------------------------ |
| 0.1     | 2026-01-27 | Lead Architect | Initial INDEX creation from SBD |
