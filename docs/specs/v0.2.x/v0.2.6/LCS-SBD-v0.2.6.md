# LCS-SBD-026: Scope Breakdown — The Sidebar (Real-Time Feedback)

## Document Control

| Field            | Value                                                                                           |
| :--------------- | :---------------------------------------------------------------------------------------------- |
| **Document ID**  | LCS-SBD-026                                                                                     |
| **Version**      | v0.2.6                                                                                          |
| **Status**       | Draft                                                                                           |
| **Last Updated** | 2026-01-26                                                                                      |
| **Depends On**   | v0.2.3 (Linter Engine), v0.2.4 (Editor Integration), v0.1.1 (Layout Engine), v0.0.7 (Event Bus) |

---

## 1. Executive Summary

### 1.1 The Vision

The Sidebar (Real-Time Feedback) is the **command center for document health monitoring** in Lexichord. Inspired by VS Code's "Problems" panel, this module provides writers with a dedicated dockable panel that aggregates all style violations, enabling at-a-glance assessment of document quality and efficient navigation to specific issues.

This module transforms passive violation display (squiggly underlines from v0.2.4) into an active, navigable interface. Writers can see all issues in one place, jump directly to problems, track their compliance score, and filter violations by scope.

### 1.2 Business Value

- **Centralized Overview:** Writers see all violations in a single, organized panel rather than hunting through the document.
- **Efficient Navigation:** Double-click any issue to jump directly to its location in the editor.
- **Gamification:** Compliance Score motivates writers to address issues and track improvement over time.
- **Flexible Scope:** Toggle between Current File, Open Files, and Project-wide to manage multi-document projects.
- **Professional UX:** Familiar VS Code-like interface reduces learning curve for technical writers.
- **Enterprise Ready:** Foundation for team-wide style compliance reporting and governance.

### 1.3 Dependencies on Previous Versions

| Component             | Source  | Usage                                                 |
| :-------------------- | :------ | :---------------------------------------------------- |
| LintingCompletedEvent | v0.2.3d | Primary data source for violation updates             |
| StyleViolation        | v0.2.1b | Violation data model with position, severity, message |
| ViolationSeverity     | v0.2.1b | Error, Warning, Info severity levels                  |
| ILintingOrchestrator  | v0.2.3a | Access to linting state and trigger linting           |
| IViolationAggregator  | v0.2.3d | Get violations for specific documents                 |
| IStyleScanner         | v0.2.3c | File scanning for project-wide linting                |
| ILintingConfiguration | v0.2.3b | Debounce and configuration settings                   |
| ManuscriptView        | v0.2.4a | Editor integration for scroll-to-line                 |
| IViolationProvider    | v0.2.4a | Current document violations                           |
| IRegionManager        | v0.1.1b | Register sidebar in Bottom dock region                |
| ILayoutService        | v0.1.1c | Persist panel visibility and size                     |
| ShellRegion.Bottom    | v0.1.1a | Target dock region                                    |
| IMediator             | v0.0.7a | Event subscription for lint completions               |
| IEditorService        | v0.1.3a | Access to open documents and active document          |

---

## 2. Sub-Part Specifications

### 2.1 v0.2.6a: Problems Panel

| Field             | Value                                |
| :---------------- | :----------------------------------- |
| **Sub-Part ID**   | INF-026a                             |
| **Title**         | Problems Panel                       |
| **Spec Document** | [LCS-INF-026a.md](./LCS-INF-026a.md) |
| **Module**        | `Lexichord.Modules.Style`            |
| **License Tier**  | Core                                 |

**Goal:** Create a VS Code-style Problems Panel sidebar view that displays all style violations. Bind the panel to `LintingCompletedEvent` for real-time updates.

**Key Deliverables:**

- Create `ProblemsPanel.axaml` UserControl with grouped TreeView/ItemsControl
- Create `ProblemsPanelViewModel` with observable violation collection
- Create `ProblemItemViewModel` for individual violation display
- Subscribe to `LintingCompletedEvent` via MediatR notification handler
- Group violations by file (when showing multiple files) or by severity
- Display violation icon, message, file name, and line:column
- Support sorting by severity, file, or position
- Register panel in `ShellRegion.Bottom` via IRegionManager

**Key Interfaces:**

```csharp
public interface IProblemsPanelViewModel
{
    IReadOnlyList<ProblemGroupViewModel> ProblemGroups { get; }
    IReadOnlyList<ProblemItemViewModel> AllProblems { get; }
    int TotalCount { get; }
    int ErrorCount { get; }
    int WarningCount { get; }
    int InfoCount { get; }
    ProblemScopeMode ScopeMode { get; set; }
    IScorecardViewModel Scorecard { get; }
    ICommand NavigateToViolationCommand { get; }
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
```

**Dependencies:**

- v0.2.3d: LintingCompletedEvent (subscription)
- v0.0.7a: IMediator (event bus)
- v0.1.1b: IRegionManager (dock registration)

---

### 2.2 v0.2.6b: Navigation Sync

| Field             | Value                                |
| :---------------- | :----------------------------------- |
| **Sub-Part ID**   | INF-026b                             |
| **Title**         | Navigation Sync                      |
| **Spec Document** | [LCS-INF-026b.md](./LCS-INF-026b.md) |
| **Module**        | `Lexichord.Modules.Style`            |
| **License Tier**  | Core                                 |

**Goal:** Implement double-click behavior on Problems Panel items that scrolls the Editor to the specific line and column of the selected violation, placing the cursor at the exact violation position.

**Key Deliverables:**

- Define `IEditorNavigationService` interface for cross-module navigation
- Implement `EditorNavigationService` that resolves document and scrolls to offset
- Create `NavigateToViolationCommand` with document ID, line, and column parameters
- Add double-click handler to ProblemItemView
- Support cross-document navigation (activate document first if not active)
- Highlight the violation span after navigation with brief animation
- Support keyboard navigation (Enter key on selected item)

**Key Interfaces:**

```csharp
public interface IEditorNavigationService
{
    Task<bool> NavigateToAsync(
        string documentId,
        int line,
        int column,
        int? length = null,
        CancellationToken cancellationToken = default);

    Task<bool> NavigateToViolationAsync(
        StyleViolation violation,
        TimeSpan? highlightDuration = null,
        CancellationToken cancellationToken = default);

    bool CanNavigateTo(string documentId);
}
```

**Dependencies:**

- v0.2.6a: ProblemsPanelViewModel (command binding)
- v0.2.4a: ManuscriptView (scroll control)
- v0.1.3a: IEditorService (document activation)

---

### 2.3 v0.2.6c: Scorecard Widget

| Field             | Value                                |
| :---------------- | :----------------------------------- |
| **Sub-Part ID**   | INF-026c                             |
| **Title**         | Scorecard Widget                     |
| **Spec Document** | [LCS-INF-026c.md](./LCS-INF-026c.md) |
| **Module**        | `Lexichord.Modules.Style`            |
| **License Tier**  | Core                                 |

**Goal:** Create a Scorecard widget at the top of the Problems Panel showing "Total Errors: X" and a gamification "Compliance Score" calculated as 100% minus penalty points based on violation count and severity.

**Key Deliverables:**

- Create `ScorecardWidget.axaml` UserControl with score display
- Create `ScorecardViewModel` with reactive score calculation
- Implement scoring algorithm: 100 - (errors*5 + warnings*2 + info\*0.5)
- Create animated score gauge (circular progress or horizontal bar)
- Display trend indicator (improving/declining based on recent history)
- Color-code score: Green (90-100), Yellow (70-89), Orange (50-69), Red (0-49)
- Store score history for trend analysis
- Integrate widget at top of ProblemsPanel

**Key Interfaces:**

```csharp
public interface IScorecardViewModel
{
    int TotalErrors { get; }
    int TotalWarnings { get; }
    int TotalInfo { get; }
    int TotalCount { get; }
    double ComplianceScore { get; }
    string ComplianceScoreDisplay { get; }
    double PreviousScore { get; }
    ScoreTrend Trend { get; }
    string ScoreGrade { get; }
    string ScoreColor { get; }
    void Update(int errors, int warnings, int info);
    void Reset();
}

public enum ScoreTrend { Improving, Stable, Declining }
```

**Dependencies:**

- v0.2.6a: ProblemsPanelViewModel (violation counts)
- v0.2.3d: LintingCompletedEvent (score updates)

---

### 2.4 v0.2.6d: Filter Scope

| Field             | Value                                |
| :---------------- | :----------------------------------- |
| **Sub-Part ID**   | INF-026d                             |
| **Title**         | Filter Scope                         |
| **Spec Document** | [LCS-INF-026d.md](./LCS-INF-026d.md) |
| **Module**        | `Lexichord.Modules.Style`            |
| **License Tier**  | Core                                 |

**Goal:** Implement a toggle to switch between "Current File", "Open Files", and "Project" scope. Project-wide linting runs as a background Task with progress indication.

**Key Deliverables:**

- Create `ProblemScopeMode` enum: CurrentFile, OpenFiles, Project
- Add scope toggle buttons/dropdown to Problems Panel header
- Define `IProjectLintingService` interface for background project scanning
- Implement `ProjectLintingService` for background linting
- Create `ProjectLintingTask` that queues files for background analysis
- Display progress indicator during project-wide linting
- Cache project-wide violations with file-based invalidation
- Implement efficient incremental updates on file save
- Respect `.lexichordignore` patterns for project scope

**Key Interfaces:**

```csharp
public interface IProjectLintingService
{
    Task<ProjectLintResult> LintProjectAsync(
        string projectPath,
        IProgress<ProjectLintProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<MultiLintResult> LintOpenDocumentsAsync(
        CancellationToken cancellationToken = default);

    void InvalidateFile(string filePath);
    void InvalidateAll();
    IReadOnlyList<StyleViolation> GetCachedViolations();
    bool IsLintingInProgress { get; }
    event EventHandler<ProjectLintProgressEventArgs> ProgressChanged;
}

public enum ProblemScopeMode
{
    CurrentFile,
    OpenFiles,
    Project
}
```

**Dependencies:**

- v0.2.6a: ProblemsPanelViewModel (UI binding)
- v0.2.3b: ILintingConfiguration (debounce settings)
- v0.2.3c: IStyleScanner (file scanning)
- v0.0.7a: IMediator (progress events)
- v0.1.3a: IEditorService (open documents list)

---

## 3. Implementation Checklist

| #         | Sub-Part | Task                                        | Est. Hours     |
| :-------- | :------- | :------------------------------------------ | :------------- |
| 1         | v0.2.6a  | Define IProblemsPanelViewModel interface    | 1              |
| 2         | v0.2.6a  | Create ProblemItemViewModel                 | 2              |
| 3         | v0.2.6a  | Create ProblemGroupViewModel                | 1              |
| 4         | v0.2.6a  | Create ProblemsPanel.axaml layout           | 3              |
| 5         | v0.2.6a  | Implement ProblemsPanelViewModel            | 4              |
| 6         | v0.2.6a  | Implement TreeView with grouping            | 4              |
| 7         | v0.2.6a  | Subscribe to LintingCompletedEvent          | 2              |
| 8         | v0.2.6a  | Register in Bottom dock region              | 2              |
| 9         | v0.2.6a  | Unit tests for ProblemsPanelViewModel       | 3              |
| 10        | v0.2.6b  | Define IEditorNavigationService interface   | 1              |
| 11        | v0.2.6b  | Implement EditorNavigationService           | 3              |
| 12        | v0.2.6b  | Implement NavigateToViolationCommand        | 2              |
| 13        | v0.2.6b  | Add scroll-to-line-column in ManuscriptView | 3              |
| 14        | v0.2.6b  | Implement violation highlight animation     | 2              |
| 15        | v0.2.6b  | Handle cross-document navigation            | 2              |
| 16        | v0.2.6b  | Add keyboard navigation (Enter key)         | 1              |
| 17        | v0.2.6b  | Unit tests for navigation                   | 3              |
| 18        | v0.2.6c  | Define IScorecardViewModel interface        | 1              |
| 19        | v0.2.6c  | Create ScorecardWidget.axaml                | 2              |
| 20        | v0.2.6c  | Implement ScorecardViewModel                | 3              |
| 21        | v0.2.6c  | Implement scoring algorithm                 | 2              |
| 22        | v0.2.6c  | Implement visual score indicators           | 2              |
| 23        | v0.2.6c  | Add trend calculation                       | 2              |
| 24        | v0.2.6c  | Unit tests for score calculation            | 3              |
| 25        | v0.2.6d  | Define ProblemScopeMode enum                | 0.5            |
| 26        | v0.2.6d  | Add scope toggle UI                         | 2              |
| 27        | v0.2.6d  | Define IProjectLintingService interface     | 1              |
| 28        | v0.2.6d  | Implement ProjectLintingService             | 5              |
| 29        | v0.2.6d  | Implement background task execution         | 3              |
| 30        | v0.2.6d  | Add file grouping in TreeView               | 3              |
| 31        | v0.2.6d  | Add severity filtering                      | 2              |
| 32        | v0.2.6d  | Implement .lexichordignore support          | 2              |
| 33        | v0.2.6d  | Unit tests for project linting              | 3              |
| 34        | All      | Integration tests for full pipeline         | 5              |
| 35        | All      | Performance tests with 1000+ violations     | 3              |
| **Total** |          |                                             | **83.5 hours** |

---

## 4. Risks & Mitigations

| Risk                                  | Impact | Probability | Mitigation                                                    |
| :------------------------------------ | :----- | :---------- | :------------------------------------------------------------ |
| Performance with many violations      | High   | Medium      | Virtualize TreeView; limit displayed items; pagination        |
| Background linting blocks UI          | High   | Medium      | Use Task.Run with proper async patterns; respect cancellation |
| Memory pressure with large projects   | Medium | Medium      | Clear stale results; LRU cache with max size                  |
| Navigation fails for closed documents | Medium | Medium      | Verify document open before navigate; show toast error        |
| Score calculation complexity          | Low    | Low         | Simple weighted formula; configurable weights                 |
| TreeView sorting performance          | Medium | Low         | Use ObservableRangeCollection with batch updates              |
| Event subscription memory leaks       | Medium | Low         | Proper IDisposable; weak event references                     |

---

## 5. Success Metrics

| Metric                        | Target  | Measurement                               |
| :---------------------------- | :------ | :---------------------------------------- |
| Panel render time             | < 100ms | Stopwatch from event to display           |
| Navigation latency            | < 200ms | Time from double-click to scroll complete |
| Score calculation time        | < 10ms  | Stopwatch in CalculateScore method        |
| Background lint (10 files)    | < 5s    | Total time for multi-document lint        |
| Project lint (100 files)      | < 30s   | Total time for project-wide lint          |
| Memory per 1000 violations    | < 10MB  | Memory profiler measurement               |
| UI responsiveness during lint | 60fps   | Frame counter during background lint      |

---

## 6. What This Enables

After v0.2.6, Lexichord will support:

- **Centralized Problem View:** All violations visible in one dockable panel
- **Quick Navigation:** Jump to any violation with double-click
- **Gamification:** Compliance Score motivates continuous improvement
- **Multi-Document Support:** View violations across open files
- **Project-Wide Linting:** Background scanning of entire project
- **Professional Workflow:** VS Code-familiar interface for technical writers
- **Foundation for v0.2.7:** Quick-fix actions from Problems Panel
- **Foundation for v0.3.x:** AI-powered fix suggestions in sidebar
- **Foundation for Enterprise:** Team compliance dashboards and reporting
