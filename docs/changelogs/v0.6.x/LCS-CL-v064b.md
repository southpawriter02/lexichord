# LCS-CL-064b: Detailed Changelog — Issues by Category Bar Chart

## Metadata

| Field        | Value                                                        |
| :----------- | :----------------------------------------------------------- |
| **Version**  | v0.6.4b                                                      |
| **Released** | 2026-02-04                                                   |
| **Category** | Modules / Style                                              |
| **Parent**   | [v0.6.4 Changelog](../CHANGELOG.md#v064)                     |
| **Spec**     | P1 Component Implementation                                  |

---

## Summary

This release implements the Issues by Category bar chart component in the `Lexichord.Modules.Style` module. The component provides a horizontal bar chart visualization of style violations grouped by category (Terminology, Passive Voice, Sentence Length, Readability, etc.). Features real-time updates via MediatR events, LiveCharts2 integration, category selection for filtering, and comprehensive logging.

---

## New Features

### 1. IIssueCategoryChartViewModel Interface

Added `IIssueCategoryChartViewModel` interface defining the contract for the bar chart component:

```csharp
public interface IIssueCategoryChartViewModel : INotifyPropertyChanged, IDisposable
{
    bool HasData { get; }
    bool IsLoading { get; }
    int TotalIssueCount { get; }
    ReadOnlyObservableCollection<IssueCategoryData> Categories { get; }
    IssueCategoryData? SelectedCategory { get; }

    void Refresh();
    event EventHandler<IssueCategoryData>? CategorySelected;
}
```

**Features:**
- Category collection with count aggregation
- Total issue count across all categories
- Category selection event for filtering integration
- Manual refresh capability

**File:** `src/Lexichord.Abstractions/Contracts/Linting/IIssueCategoryChartViewModel.cs`

### 2. IssueCategoryData Record

Added `IssueCategoryData` record for category statistics:

```csharp
public record IssueCategoryData(
    string CategoryName,
    string CategoryId,
    int IssueCount,
    string Color)
{
    public bool HasIssues { get; }
    public static IssueCategoryData Create(string categoryName, string categoryId, int count);
}
```

**Predefined Category Colors:**
| Category | Color | Hex |
|----------|-------|-----|
| Terminology | Red | #F87171 |
| Passive Voice | Amber | #FBBF24 |
| Sentence Length | Blue | #60A5FA |
| Readability | Green | #34D399 |
| Grammar | Purple | #A78BFA |
| Style | Orange | #FB923C |
| Structure | Teal | #2DD4BF |
| Custom | Slate | #94A3B8 |

**File:** `src/Lexichord.Abstractions/Contracts/Linting/IIssueCategoryChartViewModel.cs`

### 3. IssueCategoryChartViewModel Implementation

Implemented `IIssueCategoryChartViewModel` with LiveCharts2 integration:

```csharp
public partial class IssueCategoryChartViewModel : ObservableObject,
    IIssueCategoryChartViewModel,
    INotificationHandler<LintingCompletedEvent>
{
    // Dependencies
    private readonly IViolationAggregator _violationAggregator;
    private readonly ILogger<IssueCategoryChartViewModel> _logger;

    // LiveCharts2 properties
    public ISeries[] Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }

    // Category inference and aggregation methods...
}
```

**Features:**
- MediatR event handling for real-time updates
- Category inference from rule IDs and metadata
- Sorted categories by count (descending)
- Horizontal bar chart (RowSeries) with accent color
- Category selection command with event raising
- Comprehensive structured logging (8 log events, IDs 2100-2107)

**Category Inference Logic:**
```csharp
private static string InferCategory(AggregatedStyleViolation violation)
{
    var ruleId = violation.RuleId.ToLowerInvariant();

    if (ruleId.Contains("term") || ruleId.Contains("vocab")) return "terminology";
    if (ruleId.Contains("passive")) return "passive-voice";
    if (ruleId.Contains("sentence") || ruleId.Contains("length")) return "sentence-length";
    if (ruleId.Contains("read") || ruleId.Contains("flesch") || ruleId.Contains("grade")) return "readability";
    if (ruleId.Contains("grammar") || ruleId.Contains("spell")) return "grammar";
    if (ruleId.Contains("struct") || ruleId.Contains("heading")) return "structure";

    return "style"; // Default
}
```

**Log Event IDs:**
| ID | Name | Description |
|----|------|-------------|
| 2100 | Initialized | ViewModel construction complete |
| 2101 | DataUpdated | Chart data rebuilt |
| 2102 | CategorySelected | Category clicked |
| 2103 | RefreshRequested | Manual refresh triggered |
| 2104 | LintingEventReceived | Linting completed event received |
| 2105 | ChartSeriesBuilt | LiveCharts series constructed |
| 2106 | CategoryAggregated | Violations grouped by category |
| 2107 | Disposed | ViewModel disposed |

**File:** `src/Lexichord.Modules.Style/ViewModels/IssueCategoryChartViewModel.cs`

### 4. IssueCategoryChartView AXAML

Implemented the Avalonia UI view with LiveCharts2 CartesianChart:

**Layout Structure:**
- Header row with title and total count badge
- Chart content area with loading/empty/data states

**UI Components:**
- Section header with "ISSUES BY CATEGORY" title
- Total count badge (accent-colored pill)
- Loading indicator with spinner
- Empty state with checkmark icon ("No issues found - Your document is in harmony")
- LiveCharts2 horizontal bar chart (CartesianChart with RowSeries)

**LiveCharts2 Configuration:**
```xml
<lvc:CartesianChart
    Series="{Binding Series}"
    XAxes="{Binding XAxes}"
    YAxes="{Binding YAxes}"
    TooltipPosition="Hidden"
    ZoomMode="None" />
```

**Styling:**
- `.section-header` - Muted uppercase text
- `.total-badge` - Accent-colored count pill
- `.empty-state` - Centered success message
- `.loading` - Translucent spinner overlay

**Files:**
- `src/Lexichord.Modules.Style/Views/IssueCategoryChartView.axaml`
- `src/Lexichord.Modules.Style/Views/IssueCategoryChartView.axaml.cs`

---

## New Files

| File Path | Description |
| --------- | ----------- |
| `src/Lexichord.Abstractions/Contracts/Linting/IIssueCategoryChartViewModel.cs` | Interface definition with IssueCategoryData record |
| `src/Lexichord.Modules.Style/ViewModels/IssueCategoryChartViewModel.cs` | ViewModel implementation |
| `src/Lexichord.Modules.Style/Views/IssueCategoryChartView.axaml` | AXAML view definition |
| `src/Lexichord.Modules.Style/Views/IssueCategoryChartView.axaml.cs` | View code-behind |

---

## Unit Tests

Added comprehensive unit tests covering all bar chart functionality:

| Test File | Test Count | Coverage |
| --------- | ---------- | -------- |
| `IssueCategoryChartViewModelTests.cs` | ~20 | Constructor, event handling, category aggregation, selection, chart building, disposal |

**Test Categories:**
- Constructor tests (initialization, axes setup)
- Event handling tests (LintingCompletedEvent)
- Category aggregation tests (grouping, sorting)
- Category selection tests (command, event)
- Refresh tests (active document handling)
- Chart series tests (LiveCharts building)
- Dispose tests (cleanup, idempotency)
- IssueCategoryData record tests

Test file location: `tests/Lexichord.Tests.Unit/Modules/Style/IssueCategoryChartViewModelTests.cs`

---

## Dependencies

### Internal Dependencies

| Interface | Version | Usage |
| --------- | ------- | ----- |
| `IViolationAggregator` | v0.2.6a | Violation retrieval |
| `LintingCompletedEvent` | v0.2.3a | Real-time updates |
| `AggregatedStyleViolation` | v0.2.6a | Violation data |
| `ViolationSeverity` | v0.2.1a | Severity enum |

### External Dependencies

| Package | Version | Usage |
| ------- | ------- | ----- |
| `LiveChartsCore.SkiaSharpView.Avalonia` | 2.0.0-rc6.1 | Chart rendering |
| `SkiaSharp` | (transitive) | Graphics backend |

---

## Breaking Changes

None. This is a new feature addition with no impact on existing code.

---

## Design Rationale

### Why a Horizontal Bar Chart?

| Design Choice | Rationale |
| ------------- | --------- |
| Horizontal layout | Category labels read naturally left-to-right |
| Sorted by count | Most impactful categories shown first |
| Color coding | Quick visual identification of category types |
| Compact design | Fits in dashboard widgets and sidebars |

### Why Category Inference?

| Reason | Explanation |
| ------ | ----------- |
| Rule ID patterns | Consistent naming makes inference reliable |
| Fallback handling | Unknown rules grouped under "Style" |
| Metadata support | Category property takes precedence when available |
| Future extensibility | Easy to add new category patterns |

---

## Integration Points

### Problems Panel Integration

The bar chart can be embedded in the Resonance Dashboard or Problems Panel:

```xml
<!-- Example: Embedding in ResonanceDashboardView -->
<views:IssueCategoryChartView
    DataContext="{Binding IssueCategoryChartViewModel}"
    Margin="0,16,0,0" />
```

### Category Selection Filtering

Connect to Problems Panel for filtered view:

```csharp
_chartVm.CategorySelected += (_, category) =>
{
    _problemsPanelVm.FilterByCategory(category.CategoryId);
};
```

---

## Performance Characteristics

| Operation | Expected Time | Notes |
| --------- | ------------- | ----- |
| Event handling | < 10ms | LINQ grouping over violations |
| Series building | < 5ms | LiveCharts RowSeries construction |
| Refresh | < 15ms | Full rebuild from aggregator |

---

## Verification Commands

```bash
# Build the Style module
dotnet build src/Lexichord.Modules.Style

# Run IssueCategoryChart tests
dotnet test --filter "FullyQualifiedName~IssueCategoryChartViewModel"

# Run v0.6.4b tests by trait
dotnet test --filter "Feature=v0.6.4b"

# Run all Style module tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Modules.Style"
```
