# LCS-SBD-024: Scope Breakdown — Red Pen (Editor Integration)

## Document Control

| Field            | Value                                                     |
| :--------------- | :-------------------------------------------------------- |
| **Document ID**  | LCS-SBD-024                                               |
| **Version**      | v0.2.4                                                    |
| **Status**       | Draft                                                     |
| **Last Updated** | 2026-01-26                                                |
| **Depends On**   | v0.2.3 (Linter Engine), v0.1.3 (Editor Module/AvalonEdit) |

---

## 1. Executive Summary

### 1.1 The Vision

Red Pen (Editor Integration) is the **visual manifestation of the Style system** — the user-facing component that transforms abstract style violations into actionable visual feedback. By rendering wavy underlines, hover tooltips, and context menu quick-fixes directly in the AvalonEdit editor, Lexichord provides writers with real-time style guidance without interrupting their creative flow.

This module bridges the gap between the v0.2.3 Linter Engine (which analyzes text and produces violations) and the writer's experience, making style recommendations visible, understandable, and immediately actionable.

### 1.2 Business Value

- **Visual Feedback:** Writers see style issues directly in their text, like spell-check but for writing style.
- **Immediate Context:** Hover tooltips explain what's wrong and why, building writer understanding.
- **One-Click Fixes:** Right-click menu enables instant corrections, reducing friction in the editing process.
- **Professional Polish:** Familiar visual language (colored squiggly lines) matches industry-standard tools.
- **Premium Differentiation:** Core feature for WriterPro tier; demonstrates AI-powered value.

### 1.3 Dependencies on Previous Versions

| Component                   | Source  | Usage                                                                 |
| :-------------------------- | :------ | :-------------------------------------------------------------------- |
| StyleViolation              | v0.2.1b | Data model for violations with coordinates, severity, recommendations |
| ILintingOrchestrator        | v0.2.3a | Service that produces violations from document analysis               |
| LintResult                  | v0.2.3c | Container for multiple violations from a lint operation               |
| ManuscriptView              | v0.1.3a | Host control for the TextEditor where violations are rendered         |
| ManuscriptViewModel         | v0.1.3a | ViewModel that triggers linting on document changes                   |
| TextEditor (AvalonEdit)     | v0.1.3a | Target control for DocumentColorizingTransformer                      |
| IEditorConfigurationService | v0.1.3d | Editor settings that may affect rendering                             |
| IThemeManager               | v0.0.2c | Theme-aware colors for violation severity levels                      |

---

## 2. Sub-Part Specifications

### 2.1 v0.2.4a: StyleViolationRenderer (Colorizing Transformer)

| Field            | Value                                                 |
| :--------------- | :---------------------------------------------------- |
| **Sub-Part ID**  | INF-024a                                              |
| **Title**        | Rendering Transformer                                 |
| **Module**       | `Lexichord.Modules.Editor`, `Lexichord.Modules.Style` |
| **License Tier** | Core                                                  |

**Goal:** Create `StyleViolationRenderer` inheriting from AvalonEdit's `DocumentColorizingTransformer` to render violations as colored underlines during text rendering.

**Key Deliverables:**

- Create `StyleViolationRenderer` class extending `DocumentColorizingTransformer`
- Implement `ColorizeLine()` to apply decorations at violation coordinates
- Create `IViolationProvider` interface for supplying violations to the renderer
- Wire renderer into TextArea.TextView.LineTransformers collection
- Support dynamic updates when violations change (add/remove/modify)

**Key Interfaces:**

```csharp
public interface IViolationProvider
{
    IReadOnlyList<StyleViolation> GetViolationsInRange(int startOffset, int endOffset);
    event EventHandler<ViolationsChangedEventArgs> ViolationsChanged;
}
```

**Dependencies:**

- v0.2.3a: StyleViolation (data model)
- v0.1.3a: ManuscriptView (TextArea access)

---

### 2.2 v0.2.4b: Wavy Underline Rendering (The Squiggly Line)

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-024b                  |
| **Title**        | The Squiggly Line         |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Implement `DrawContext` rendering logic that draws wavy/squiggly underlines at violation coordinates with severity-based colors (Red=Error, Orange=Warning, Blue=Info).

**Key Deliverables:**

- Implement custom `VisualLineElementGenerator` or background renderer for wave drawing
- Define color palette: Error=#E51400 (Red), Warning=#F0A30A (Orange), Info=#0078D4 (Blue)
- Calculate wave geometry based on text bounds from VisualLine
- Support dark theme variants: Error=#FF6B6B, Warning=#FFB347, Info=#4FC3F7
- Optimize drawing for minimal performance impact during scrolling

**Key Interfaces:**

```csharp
public interface IViolationColorProvider
{
    Color GetUnderlineColor(ViolationSeverity severity);
    Color GetBackgroundColor(ViolationSeverity severity);
}
```

**Dependencies:**

- v0.2.4a: StyleViolationRenderer (provides rendering context)
- v0.0.2c: IThemeManager (light/dark colors)

---

### 2.3 v0.2.4c: Hover Tooltip Integration

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-024c                  |
| **Title**        | Hover Tooltips            |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Implement mouse hover logic that calculates the document offset under the cursor and displays a tooltip containing the violation message and recommendation when hovering over a squiggly-underlined region.

**Key Deliverables:**

- Hook into TextArea.MouseHover and MouseHoverStopped events
- Implement offset calculation from mouse position using GetPositionFromPoint
- Query IViolationProvider for violations at the hovered offset
- Display custom ToolTip with violation details (Rule, Message, Recommendation)
- Support keyboard-only tooltip trigger (Ctrl+K, Ctrl+I or similar)
- Style tooltip to match application theme

**Key Interfaces:**

```csharp
public interface IViolationTooltipService
{
    void ShowTooltip(TextArea textArea, Point position, StyleViolation violation);
    void HideTooltip();
    bool IsTooltipVisible { get; }
}
```

**Dependencies:**

- v0.2.4a: IViolationProvider (query violations at offset)
- v0.2.3a: StyleViolation.Message, StyleViolation.Recommendation
- v0.1.3a: TextArea (hover events)

---

### 2.4 v0.2.4d: Context Menu Quick-Fix

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-024d                  |
| **Title**        | Right-Click Fix Menu      |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Extend the editor's context menu to show quick-fix options when right-clicking on a style violation. If the violation has a `Recommendation`, add a "Change to '{Suggestion}'" menu item that replaces the violating text with the suggested fix.

**Key Deliverables:**

- Hook into TextArea.ContextMenuOpening event
- Query violations at the right-click position
- Dynamically build menu items for violations with recommendations
- Implement text replacement logic respecting document change tracking
- Support multiple violations at same position (nested menu or multiple items)
- Add "Ignore this issue" and "Ignore rule" options (optional, future)

**Key Interfaces:**

```csharp
public interface IQuickFixService
{
    IReadOnlyList<QuickFixAction> GetQuickFixes(int offset);
    Task ApplyFixAsync(QuickFixAction fix, CancellationToken cancellationToken = default);
}

public record QuickFixAction(
    string DisplayText,
    StyleViolation Violation,
    int ReplacementStart,
    int ReplacementLength,
    string ReplacementText
);
```

**Dependencies:**

- v0.2.4a: IViolationProvider (query violations at offset)
- v0.2.3a: StyleViolation.Recommendation
- v0.1.3a: TextArea.ContextMenu, TextDocument

---

## 3. Implementation Checklist

| #         | Sub-Part | Task                                                          | Est. Hours   |
| :-------- | :------- | :------------------------------------------------------------ | :----------- |
| 1         | v0.2.4a  | Define IViolationProvider interface in Abstractions           | 1            |
| 2         | v0.2.4a  | Create StyleViolationRenderer : DocumentColorizingTransformer | 4            |
| 3         | v0.2.4a  | Implement ColorizeLine() with violation lookup                | 4            |
| 4         | v0.2.4a  | Wire renderer into ManuscriptView.TextArea                    | 2            |
| 5         | v0.2.4a  | Implement ViolationsChanged event handling                    | 2            |
| 6         | v0.2.4a  | Unit tests for StyleViolationRenderer                         | 3            |
| 7         | v0.2.4b  | Define IViolationColorProvider interface                      | 1            |
| 8         | v0.2.4b  | Implement WavyUnderlineRenderer (DrawingVisual)               | 6            |
| 9         | v0.2.4b  | Define severity color palette (light/dark)                    | 1            |
| 10        | v0.2.4b  | Implement wave geometry calculation                           | 3            |
| 11        | v0.2.4b  | Optimize draw calls for scroll performance                    | 2            |
| 12        | v0.2.4b  | Unit tests for color provider                                 | 2            |
| 13        | v0.2.4c  | Define IViolationTooltipService interface                     | 1            |
| 14        | v0.2.4c  | Implement MouseHover event handler                            | 3            |
| 15        | v0.2.4c  | Implement GetPositionFromPoint offset calculation             | 2            |
| 16        | v0.2.4c  | Create ViolationTooltipView.axaml                             | 2            |
| 17        | v0.2.4c  | Implement tooltip positioning logic                           | 2            |
| 18        | v0.2.4c  | Unit tests for offset calculation                             | 2            |
| 19        | v0.2.4d  | Define IQuickFixService interface                             | 1            |
| 20        | v0.2.4d  | Define QuickFixAction record                                  | 1            |
| 21        | v0.2.4d  | Implement ContextMenuOpening handler                          | 3            |
| 22        | v0.2.4d  | Implement menu item generation                                | 2            |
| 23        | v0.2.4d  | Implement text replacement logic                              | 3            |
| 24        | v0.2.4d  | Unit tests for QuickFixService                                | 2            |
| 25        | All      | Integration tests: violation -> render -> tooltip -> fix      | 4            |
| 26        | All      | Performance testing with 100+ violations                      | 2            |
| **Total** |          |                                                               | **60 hours** |

---

## 4. Risks & Mitigations

| Risk                                         | Impact | Probability | Mitigation                                                     |
| :------------------------------------------- | :----- | :---------- | :------------------------------------------------------------- |
| AvalonEdit rendering API changes             | High   | Low         | Pin AvaloniaEdit version; abstract rendering                   |
| Performance degradation with many violations | High   | Medium      | Virtualize visible violations; throttle re-render              |
| Tooltip flickering on rapid mouse movement   | Medium | Medium      | Debounce hover events; smooth hide transitions                 |
| Color accessibility for colorblind users     | Medium | Low         | Use patterns/icons in addition to colors; test with simulators |
| Context menu collision with system menu      | Low    | Low         | Merge with existing menu; use standard patterns                |
| Wave drawing clipping at line boundaries     | Medium | Medium      | Handle line wrapping; test edge cases                          |

---

## 5. Success Metrics

| Metric                                | Target      | Measurement                              |
| :------------------------------------ | :---------- | :--------------------------------------- |
| Render time per line (100 violations) | < 2ms       | Stopwatch in ColorizeLine                |
| Tooltip display latency               | < 100ms     | Time from hover to visible               |
| Quick-fix application time            | < 50ms      | Time from click to text replaced         |
| Memory per violation marker           | < 100 bytes | Memory profiler                          |
| Scroll FPS with 100 violations        | 60fps       | Frame counter during scroll              |
| Tooltip accuracy                      | 100%        | Manual testing - correct violation shown |

---

## 6. What This Enables

After v0.2.4, Lexichord will support:

- **Visual Style Feedback:** Writers see style issues as colored underlines in real-time
- **Contextual Help:** Hover tooltips explain issues and provide recommendations
- **One-Click Fixes:** Right-click menu enables instant corrections
- **Professional UX:** Familiar visual language matching IDEs and word processors
- **Foundation for v0.2.5:** Style Panel can show aggregated violation list
- **Foundation for v0.2.6:** Style settings can toggle violation display
- **Premium Value:** Clear differentiation for WriterPro tier
