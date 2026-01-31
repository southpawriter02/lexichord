# LCS-DES-075-KG-g: Design Specification — Unified Issues Panel

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `VAL-075-KG-g` | Unified validation sub-part g |
| **Feature Name** | `Unified Issues Panel` | Single UI panel for all validation issues |
| **Target Version** | `v0.7.5g` | Seventh sub-part of v0.7.5-KG |
| **Module Scope** | `Lexichord.UI.Panels` | UI panel module |
| **Swimlane** | `Presentation` | UI vertical |
| **License Tier** | `Core` | Available across all tiers |
| **Feature Gate Key** | `FeatureFlags.UI.UnifiedIssuesPanel` | Panel feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-075-KG](./LCS-SBD-075-KG.md) | Unified Validation scope |
| **Scope Breakdown** | [LCS-SBD-075-KG S3.1](./LCS-SBD-075-KG.md#31-sub-parts) | g = UI Panel |

---

## 2. Executive Summary

### 2.1 The Requirement

Current state: Style issues, grammar errors, and knowledge violations appear in separate panels, forcing users to:

- Check multiple panels
- Cross-reference issues by location
- Apply fixes from different toolbars
- See inconsistent severity levels

Business requirement: Single, unified panel that:

1. **Displays all issues** from all validators in one place
2. **Groups by severity** for quick prioritization
3. **Shows expandable details** for each issue
4. **Provides fix buttons** for individual and bulk operations
5. **Allows filtering** by type and severity
6. **Real-time updates** as document changes

### 2.2 The Proposed Solution

Implement `IUnifiedIssuesPanel` (WPF/XAML control) that:

1. Subscribes to `IUnifiedValidationService` for real-time results
2. Displays issues grouped by severity (Errors, Warnings, Info, Hints)
3. Each issue shows: Code, Message, Location, Validator name
4. Expandable details reveal: Full context, suggested fix, auto-fixability
5. Fix button applies individual fixes or triggers bulk fix
6. Filter dropdown for category/severity

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IUnifiedValidationService` | v0.7.5f | Get validation results |
| `UnifiedValidationResult` | v0.7.5e | Result data |
| `UnifiedIssue` | v0.7.5e | Issue representation |
| `IEditorService` | v0.1.3a | Navigate to issue location |
| `IFixOrchestrator` | v0.7.5h | Apply fixes |
| `ILogger` | .NET | Observability |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `PresentationFramework` | 4.x | WPF base |
| `Prism.Wpf` | 8.x | MVVM framework |
| `MvvmLightLibsStd10` | 5.4.x | MVVM Toolkit |

### 3.2 Licensing Behavior

- **Load Behavior:** Always available
  - Panel loads for all license tiers
  - Content filtered based on license tier (upstream)
  - Aggregator provides only issues user is licensed for

---

## 4. Data Contract (The API)

### 4.1 Primary Interface

```csharp
namespace Lexichord.Abstractions.Contracts.Validation.UI;

/// <summary>
/// WPF panel UI for displaying unified validation issues.
/// Provides grouping, filtering, and bulk fix operations.
/// </summary>
public interface IUnifiedIssuesPanel
{
    /// <summary>
    /// Gets the panel view model.
    /// </summary>
    UnifiedIssuesPanelViewModel ViewModel { get; }

    /// <summary>
    /// Refreshes the panel with new validation results.
    /// </summary>
    /// <param name="result">The validation result to display.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RefreshAsync(UnifiedValidationResult result, CancellationToken ct = default);

    /// <summary>
    /// Clears all issues from the panel.
    /// </summary>
    void Clear();

    /// <summary>
    /// Shows an error message in the panel.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional additional details.</param>
    void ShowError(string message, string? details = null);

    /// <summary>
    /// Gets the currently selected issue (if any).
    /// </summary>
    /// <returns>The selected issue or null.</returns>
    UnifiedIssue? GetSelectedIssue();

    /// <summary>
    /// Navigates to the specified issue location in the document.
    /// </summary>
    /// <param name="issue">The issue to navigate to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NavigateToIssueAsync(UnifiedIssue issue, CancellationToken ct = default);

    /// <summary>
    /// Event raised when user clicks "Fix" button on an issue.
    /// </summary>
    event EventHandler<FixRequestedEventArgs>? FixRequested;

    /// <summary>
    /// Event raised when user clicks "Fix All" button.
    /// </summary>
    event EventHandler<EventArgs>? FixAllRequested;

    /// <summary>
    /// Event raised when user dismisses an issue.
    /// </summary>
    event EventHandler<IssueEventArgs>? IssueDismissed;
}
```

### 4.2 View Model

```csharp
namespace Lexichord.UI.Panels.Validation;

/// <summary>
/// MVVM view model for the unified issues panel.
/// </summary>
public class UnifiedIssuesPanelViewModel : ViewModelBase
{
    private readonly IUnifiedValidationService _validationService;
    private readonly IEditorService _editorService;
    private readonly IFixOrchestrator _fixOrchestrator;
    private readonly ILogger<UnifiedIssuesPanelViewModel> _logger;

    private UnifiedValidationResult _currentResult;
    private ObservableCollection<IssuePresentationGroup> _issueGroups;
    private IssueCategory? _selectedCategoryFilter;
    private UnifiedSeverity? _selectedSeverityFilter;
    private bool _isLoading;
    private string _statusMessage;

    public UnifiedIssuesPanelViewModel(
        IUnifiedValidationService validationService,
        IEditorService editorService,
        IFixOrchestrator fixOrchestrator,
        ILogger<UnifiedIssuesPanelViewModel> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _fixOrchestrator = fixOrchestrator ?? throw new ArgumentNullException(nameof(fixOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _issueGroups = new ObservableCollection<IssuePresentationGroup>();
        _statusMessage = "Ready";

        // Subscribe to validation events
        _validationService.ValidationCompleted += OnValidationCompleted;
    }

    /// <summary>
    /// Groups of issues organized by severity.
    /// </summary>
    public ObservableCollection<IssuePresentationGroup> IssueGroups
    {
        get => _issueGroups;
        set => SetProperty(ref _issueGroups, value);
    }

    /// <summary>
    /// Total number of issues.
    /// </summary>
    public int TotalIssueCount => _currentResult?.TotalIssueCount ?? 0;

    /// <summary>
    /// Number of auto-fixable issues.
    /// </summary>
    public int AutoFixableCount => _currentResult?.AutoFixableCount ?? 0;

    /// <summary>
    /// Whether document can be published (no errors).
    /// </summary>
    public bool CanPublish => _currentResult?.CanPublish ?? true;

    /// <summary>
    /// Current loading state.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Status message to display.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Available categories for filtering.
    /// </summary>
    public List<IssueCategory> AvailableCategories { get; } = new()
    {
        IssueCategory.Style,
        IssueCategory.Grammar,
        IssueCategory.Knowledge,
        IssueCategory.Structure,
        IssueCategory.Custom
    };

    /// <summary>
    /// Available severity levels for filtering.
    /// </summary>
    public List<UnifiedSeverity> AvailableSeverities { get; } = new()
    {
        UnifiedSeverity.Error,
        UnifiedSeverity.Warning,
        UnifiedSeverity.Info,
        UnifiedSeverity.Hint
    };

    /// <summary>
    /// Selected category filter (null = all).
    /// </summary>
    public IssueCategory? SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set
        {
            if (SetProperty(ref _selectedCategoryFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    /// Selected severity filter (null = all).
    /// </summary>
    public UnifiedSeverity? SelectedSeverityFilter
    {
        get => _selectedSeverityFilter;
        set
        {
            if (SetProperty(ref _selectedSeverityFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    /// Command to fix all auto-fixable issues.
    /// </summary>
    public ICommand FixAllCommand { get; private set; }

    /// <summary>
    /// Command to fix errors only.
    /// </summary>
    public ICommand FixErrorsOnlyCommand { get; private set; }

    /// <summary>
    /// Command to dismiss all info issues.
    /// </summary>
    public ICommand DismissAllInfoCommand { get; private set; }

    /// <summary>
    /// Refreshes the panel with new validation results.
    /// </summary>
    public async Task RefreshAsync(UnifiedValidationResult result)
    {
        _currentResult = result;
        IsLoading = false;

        RebuildIssueGroups(result);
        UpdateStatusMessage(result);

        OnPropertyChanged(nameof(TotalIssueCount));
        OnPropertyChanged(nameof(AutoFixableCount));
        OnPropertyChanged(nameof(CanPublish));

        _logger.LogInformation(
            "Issues panel refreshed: {Count} issues",
            result.TotalIssueCount);
    }

    /// <summary>
    /// Clears all issues from the panel.
    /// </summary>
    public void Clear()
    {
        IssueGroups.Clear();
        _currentResult = null;
        StatusMessage = "No issues detected";
    }

    #region Private Methods

    private void RebuildIssueGroups(UnifiedValidationResult result)
    {
        IssueGroups.Clear();

        var groups = new[]
        {
            (UnifiedSeverity.Error, "Errors"),
            (UnifiedSeverity.Warning, "Warnings"),
            (UnifiedSeverity.Info, "Info"),
            (UnifiedSeverity.Hint, "Hints")
        };

        foreach (var (severity, label) in groups)
        {
            var issues = result.BySeverity
                .TryGetValue(severity, out var severityIssues)
                ? severityIssues
                : new List<UnifiedIssue>();

            if (issues.Count > 0)
            {
                var group = new IssuePresentationGroup(severity, label, issues);
                IssueGroups.Add(group);
            }
        }
    }

    private void ApplyFilters()
    {
        if (_currentResult == null)
            return;

        var filtered = _currentResult.Issues.AsEnumerable();

        if (_selectedCategoryFilter.HasValue)
        {
            filtered = filtered.Where(i => i.Category == _selectedCategoryFilter.Value);
        }

        if (_selectedSeverityFilter.HasValue)
        {
            filtered = filtered.Where(i => i.Severity == _selectedSeverityFilter.Value);
        }

        var filteredResult = _currentResult with
        {
            Issues = filtered.ToList().AsReadOnly()
        };

        RebuildIssueGroups(filteredResult);
    }

    private void UpdateStatusMessage(UnifiedValidationResult result)
    {
        var parts = new List<string>();

        if (result.ErrorCount > 0)
            parts.Add($"{result.ErrorCount} error{(result.ErrorCount == 1 ? "" : "s")}");

        if (result.WarningCount > 0)
            parts.Add($"{result.WarningCount} warning{(result.WarningCount == 1 ? "" : "s")}");

        var otherCount = result.TotalIssueCount - result.ErrorCount - result.WarningCount;
        if (otherCount > 0)
            parts.Add($"{otherCount} other");

        if (result.AutoFixableCount > 0)
            parts.Add($"{result.AutoFixableCount} auto-fixable");

        StatusMessage = parts.Count > 0
            ? string.Join(" • ", parts)
            : "All good!";
    }

    private async Task FixAllAsync()
    {
        if (_currentResult == null)
            return;

        IsLoading = true;
        StatusMessage = "Applying fixes...";

        try
        {
            var fixableIssues = _currentResult.Issues
                .Where(i => i.Fix?.CanAutoApply == true)
                .ToList();

            await _fixOrchestrator.ApplyFixesAsync(fixableIssues);

            StatusMessage = $"Applied {fixableIssues.Count} fixes";
            _logger.LogInformation("Applied {Count} fixes via Fix All", fixableIssues.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = "Error applying fixes";
            _logger.LogError(ex, "Failed to apply fixes");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnValidationCompleted(object? sender, ValidationCompletedEventArgs e)
    {
        // This event is raised by aggregator when validation completes
        // Panel subscribes to stay current
        _logger.LogDebug("Validation completed, refreshing panel");
    }

    #endregion
}
```

### 4.3 Presentation Models

```csharp
namespace Lexichord.UI.Panels.Validation;

/// <summary>
/// Represents a group of issues (by severity).
/// </summary>
public class IssuePresentationGroup : ObservableObject
{
    private bool _isExpanded = true;
    private ObservableCollection<IssuePresentation> _items;

    public IssuePresentationGroup(
        UnifiedSeverity severity,
        string label,
        IReadOnlyList<UnifiedIssue> issues)
    {
        Severity = severity;
        Label = label;
        Icon = GetIconForSeverity(severity);

        _items = new ObservableCollection<IssuePresentation>(
            issues.Select(i => new IssuePresentation(i)));
    }

    /// <summary>
    /// Severity level for this group.
    /// </summary>
    public UnifiedSeverity Severity { get; }

    /// <summary>
    /// Display label (e.g., "Errors", "Warnings").
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Icon name for visual representation.
    /// </summary>
    public string Icon { get; }

    /// <summary>
    /// Whether this group is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// Issues in this group.
    /// </summary>
    public ObservableCollection<IssuePresentation> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    /// <summary>
    /// Count of issues in this group.
    /// </summary>
    public int Count => Items.Count;

    private string GetIconForSeverity(UnifiedSeverity severity) =>
        severity switch
        {
            UnifiedSeverity.Error => "ErrorIcon",
            UnifiedSeverity.Warning => "WarningIcon",
            UnifiedSeverity.Info => "InfoIcon",
            UnifiedSeverity.Hint => "HintIcon",
            _ => "DefaultIcon"
        };
}

/// <summary>
/// Presentation model for a single issue.
/// </summary>
public class IssuePresentation : ObservableObject
{
    private bool _isExpanded;
    private bool _isSuppressed;

    public IssuePresentation(UnifiedIssue issue)
    {
        Issue = issue ?? throw new ArgumentNullException(nameof(issue));
    }

    /// <summary>
    /// The underlying issue.
    /// </summary>
    public UnifiedIssue Issue { get; }

    /// <summary>
    /// Whether details are expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// Whether this issue is suppressed (dismissed).
    /// </summary>
    public bool IsSuppressed
    {
        get => _isSuppressed;
        set => SetProperty(ref _isSuppressed, value);
    }

    /// <summary>
    /// Human-readable category label.
    /// </summary>
    public string CategoryLabel => Issue.Category switch
    {
        IssueCategory.Style => "Style",
        IssueCategory.Grammar => "Grammar",
        IssueCategory.Knowledge => "Knowledge",
        IssueCategory.Structure => "Structure",
        IssueCategory.Custom => "Custom",
        _ => "Unknown"
    };

    /// <summary>
    /// Location display string (e.g., "Line 5, Column 12").
    /// </summary>
    public string LocationDisplay
    {
        get
        {
            if (Issue.Location?.LineNumber.HasValue == true)
            {
                var col = Issue.Location.ColumnNumber.HasValue
                    ? $", Column {Issue.Location.ColumnNumber}"
                    : "";
                return $"Line {Issue.Location.LineNumber}{col}";
            }

            return "Document level";
        }
    }

    /// <summary>
    /// Whether this issue has a suggested fix.
    /// </summary>
    public bool HasFix => Issue.Fix != null;

    /// <summary>
    /// Whether the fix can be applied automatically.
    /// </summary>
    public bool CanAutoApply => Issue.Fix?.CanAutoApply == true;

    /// <summary>
    /// Fix description or empty string.
    /// </summary>
    public string FixDescription => Issue.Fix?.Description ?? "";

    /// <summary>
    /// Original text being fixed.
    /// </summary>
    public string OriginalText => Issue.Fix?.OldText ?? "";

    /// <summary>
    /// Suggested replacement text.
    /// </summary>
    public string SuggestedText => Issue.Fix?.NewText ?? "";
}

/// <summary>
/// Event args for fix requests.
/// </summary>
public class FixRequestedEventArgs : EventArgs
{
    public required UnifiedIssue Issue { get; init; }
    public required UnifiedFix Fix { get; init; }
}

/// <summary>
/// Event args for issue events.
/// </summary>
public class IssueEventArgs : EventArgs
{
    public required UnifiedIssue Issue { get; init; }
}
```

---

## 5. UI Layout & Interaction

### 5.1 XAML Control Structure

```xaml
<!-- UnifiedIssuesPanel.xaml -->
<UserControl x:Class="Lexichord.UI.Panels.UnifiedIssuesPanel"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Background="{DynamicResource {x:Static SystemColors.WindowBrushKey}}"
    Foreground="{DynamicResource {x:Static SystemColors.WindowTextBrushKey}}">

    <Grid RowDefinitions="Auto,Auto,*,Auto">

        <!-- Header -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="12" Gap="8">
            <TextBlock Text="Issues" FontSize="14" FontWeight="Bold" VerticalAlignment="Center" />
            <TextBlock Text="{Binding TotalIssueCount, StringFormat='({0})'}"
                FontSize="14" VerticalAlignment="Center" Foreground="Gray" />
            <Button Content="Fix All" Command="{Binding FixAllCommand}"
                IsEnabled="{Binding AutoFixableCount, Converter={StaticResource IsGreaterThanZeroConverter}}" />
        </StackPanel>

        <!-- Filters -->
        <Grid Grid.Row="1" Margin="12,0,12,8">
            <StackPanel Orientation="Horizontal" Gap="8">
                <TextBlock Text="Filter:" VerticalAlignment="Center" />
                <ComboBox ItemsSource="{Binding AvailableCategories}"
                    SelectedItem="{Binding SelectedCategoryFilter}" Width="100" />
                <ComboBox ItemsSource="{Binding AvailableSeverities}"
                    SelectedItem="{Binding SelectedSeverityFilter}" Width="100" />
            </StackPanel>
        </Grid>

        <!-- Issues List -->
        <ScrollViewer Grid.Row="2">
            <ItemsControl ItemsSource="{Binding IssueGroups}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate DataType="{x:Type local:IssuePresentationGroup}">
                        <Expander Header="{Binding Label}" IsExpanded="{Binding IsExpanded}">
                            <ItemsControl ItemsSource="{Binding Items}">
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate DataType="{x:Type local:IssuePresentation}">
                                        <Border BorderBrush="LightGray" BorderThickness="0,1,0,0">
                                            <StackPanel Margin="16,8">
                                                <!-- Issue Summary -->
                                                <Grid ColumnDefinitions="*,Auto">
                                                    <StackPanel Orientation="Horizontal" Grid.Column="0" Gap="8">
                                                        <TextBlock Text="{Binding Issue.Code}" FontWeight="Bold" />
                                                        <TextBlock Text="{Binding Issue.Message}" />
                                                        <TextBlock Text="{Binding CategoryLabel}" Foreground="Gray" FontSize="11" />
                                                    </StackPanel>
                                                    <StackPanel Orientation="Horizontal" Grid.Column="1" Gap="4">
                                                        <TextBlock Text="{Binding LocationDisplay}" Foreground="Gray" />
                                                        <Button Content="..." Command="{Binding ToggleExpandCommand}" />
                                                    </StackPanel>
                                                </Grid>

                                                <!-- Expanded Details -->
                                                <StackPanel Margin="0,8,0,0" Visibility="{Binding IsExpanded, Converter={StaticResource BoolToVisibilityConverter}}">
                                                    <!-- Fix Section -->
                                                    <Expander Header="Suggested Fix" IsExpanded="{Binding HasFix}" Visibility="{Binding HasFix, Converter={StaticResource BoolToVisibilityConverter}}">
                                                        <StackPanel Margin="12,8" Gap="8">
                                                            <TextBlock Text="{Binding FixDescription}" FontStyle="Italic" />
                                                            <TextBlock Text="Original:" FontWeight="Bold" FontSize="11" />
                                                            <TextBlock Text="{Binding OriginalText}" Background="#FFF0F0" Padding="4" TextWrapping="Wrap" />
                                                            <TextBlock Text="Suggested:" FontWeight="Bold" FontSize="11" Margin="0,8,0,0" />
                                                            <TextBlock Text="{Binding SuggestedText}" Background="#F0FFF0" Padding="4" TextWrapping="Wrap" />
                                                            <Button Content="Apply Fix" IsEnabled="{Binding CanAutoApply}" />
                                                        </StackPanel>
                                                    </Expander>

                                                    <!-- Context -->
                                                    <TextBlock Text="Validator:" FontWeight="Bold" FontSize="11" Margin="0,8,0,0" />
                                                    <TextBlock Text="{Binding Issue.ValidatorName}" Foreground="Gray" />
                                                </StackPanel>
                                            </StackPanel>
                                        </Border>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </Expander>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- Status Bar -->
        <StatusBar Grid.Row="3" Padding="12,8">
            <TextBlock Text="{Binding StatusMessage}" />
            <ProgressBar IsIndeterminate="True" Width="100" Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}" />
        </StatusBar>

    </Grid>
</UserControl>
```

### 5.2 Visual Styling

```
┌────────────────────────────────────────────────────────────────┐
│ Issues (12)                              [Fix All] [Fix Errors] │
├────────────────────────────────────────────────────────────────┤
│ Filter: [Category ▼] [Severity ▼]                              │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ ⛔ ERRORS (2)                                                   │
│  ├─ [Expand] STYLE_001: Line 15: Endpoint missing method      │
│  │   └─ [Apply Fix] [Dismiss]                                 │
│  │   ├─ Suggested Fix:                                        │
│  │   │  Original:  'GET /users'                              │
│  │   │  Suggested: 'method: GET\nGET /users'                 │
│  │   └─ Validator: Style Linter                              │
│  │                                                             │
│  └─ [Expand] AXIOM_001: Endpoint conflict with knowledge base │
│      └─ Validator: CKVS Validation Engine                     │
│                                                                │
│ ⚠️ WARNINGS (4)                                                │
│  ├─ [Expand] PASSIVE_VOICE: Line 8: Passive voice detected   │
│  │   └─ [Apply Fix] [Dismiss]                                 │
│  │   ├─ Suggested Fix:                                        │
│  │   │  Original:  'The API returns...'                      │
│  │   │  Suggested: 'Returns...'                              │
│  │   └─ Validator: Grammar Linter                            │
│  │                                                             │
│  └─ ... 3 more                                                │
│                                                                │
│ 💡 INFO (6)                                                     │
│  └─ ... 6 suggestions                                         │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│ 2 errors, 4 warnings, 6 info • 6 auto-fixable                 │
└────────────────────────────────────────────────────────────────┘
```

---

## 6. Interaction Flows

### 6.1 View Issue Details

**User Action:** Clicks on issue row to expand

**Flow:**
1. `IssuePresentation.IsExpanded = true`
2. Details section becomes visible
3. If has fix, show "Suggested Fix" expander
4. Show validator name and category
5. Log interaction to telemetry

### 6.2 Apply Individual Fix

**User Action:** Clicks "Apply Fix" button on issue

**Flow:**
1. Validate fix is auto-applicable
2. Emit `FixRequested` event
3. Orchestrator applies fix
4. Re-validate document
5. Panel refreshes with new results
6. Dismiss the now-fixed issue

### 6.3 Fix All Issues

**User Action:** Clicks "Fix All" button

**Flow:**
1. Collect all `issue.Fix?.CanAutoApply == true`
2. Disable button, show "Applying fixes..."
3. Call `IFixOrchestrator.ApplyFixesAsync(issues)`
4. Await completion
5. Show "Applied N fixes"
6. Panel auto-refreshes when validation completes
7. Re-enable button

### 6.4 Dismiss Issue

**User Action:** Clicks "Dismiss" or "X" on issue

**Flow:**
1. Mark `IssuePresentation.IsSuppressed = true`
2. Visually gray out issue
3. Emit `IssueDismissed` event
4. Persist suppression (optional, in user config)
5. Do not remove from list (user can see what's dismissed)

### 6.5 Filter by Category

**User Action:** Selects category from dropdown

**Flow:**
1. Set `SelectedCategoryFilter = IssueCategory.Style`
2. `ApplyFilters()` rebuilds groups
3. Only Style issues shown
4. Other categories hidden
5. Issue counts update

---

## 7. Implementation

### 7.1 Code-Behind Basics

```csharp
namespace Lexichord.UI.Panels;

/// <summary>
/// WPF user control for the unified issues panel.
/// </summary>
public partial class UnifiedIssuesPanel : UserControl, IUnifiedIssuesPanel
{
    private UnifiedIssuesPanelViewModel _viewModel;
    private readonly IUnifiedValidationService _validationService;
    private readonly IEditorService _editorService;
    private readonly IFixOrchestrator _fixOrchestrator;

    public UnifiedIssuesPanel()
    {
        InitializeComponent();
    }

    public UnifiedIssuesPanelViewModel ViewModel => _viewModel;

    public void Initialize(
        IUnifiedValidationService validationService,
        IEditorService editorService,
        IFixOrchestrator fixOrchestrator,
        ILogger<UnifiedIssuesPanelViewModel> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _fixOrchestrator = fixOrchestrator ?? throw new ArgumentNullException(nameof(fixOrchestrator));

        _viewModel = new UnifiedIssuesPanelViewModel(
            validationService,
            editorService,
            fixOrchestrator,
            logger);

        DataContext = _viewModel;

        // Wire up events
        FixRequested += OnFixRequested;
        FixAllRequested += OnFixAllRequested;
    }

    public async Task RefreshAsync(UnifiedValidationResult result, CancellationToken ct = default)
    {
        await _viewModel.RefreshAsync(result);
    }

    public void Clear()
    {
        _viewModel.Clear();
    }

    public void ShowError(string message, string? details = null)
    {
        _viewModel.StatusMessage = $"Error: {message}";
        if (details != null)
        {
            MessageBox.Show(details, message, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public UnifiedIssue? GetSelectedIssue()
    {
        // Would return selected item from list
        return null;
    }

    public async Task NavigateToIssueAsync(UnifiedIssue issue, CancellationToken ct = default)
    {
        if (issue.Location == null)
            return;

        await _editorService.GoToLineAsync(issue.Location.LineNumber ?? 1, ct);
    }

    public event EventHandler<FixRequestedEventArgs>? FixRequested;
    public event EventHandler<EventArgs>? FixAllRequested;
    public event EventHandler<IssueEventArgs>? IssueDismissed;

    private async void OnFixRequested(object? sender, FixRequestedEventArgs e)
    {
        try
        {
            await _fixOrchestrator.ApplyFixAsync(e.Issue, e.Fix);
        }
        catch (Exception ex)
        {
            ShowError("Failed to apply fix", ex.Message);
        }
    }

    private async void OnFixAllRequested(object? sender, EventArgs e)
    {
        await _viewModel.FixAllAsync();
    }
}
```

---

## 8. Error Handling

### 8.1 Fix Application Failure

**Scenario:** Applying fix throws exception.

**Handling:**
- Catch exception
- Show error message in panel: "Failed to apply fix"
- Show exception details in tooltip
- Do not dismiss issue
- Log error with stack trace

### 8.2 Navigation Failure

**Scenario:** Cannot navigate to issue location.

**Handling:**
- Log warning
- Highlight issue row in red border
- Show tooltip: "Could not navigate to location"
- Still allow user to see issue details

### 8.3 Empty Result

**Scenario:** Validation returns no issues.

**Handling:**
- Clear all groups
- Show: "All good!" in status
- Disable "Fix All" button
- Optional: Show checkmark icon

---

## 9. Testing

### 9.1 Unit Tests

```csharp
[TestClass]
public class UnifiedIssuesPanelTests
{
    [TestMethod]
    public async Task RefreshAsync_PopulatesIssueGroups()
    {
        // Arrange
        var result = CreateMockValidationResult(
            new[] { /* Errors */ },
            new[] { /* Warnings */ },
            new[] { /* Info */ });

        var panel = CreatePanel();

        // Act
        await panel.RefreshAsync(result);

        // Assert
        Assert.AreEqual(3, panel.ViewModel.IssueGroups.Count);
        Assert.IsTrue(panel.ViewModel.IssueGroups.Any(g => g.Severity == UnifiedSeverity.Error));
    }

    [TestMethod]
    public void SelectedCategoryFilter_FiltersIssues()
    {
        // Arrange
        var result = CreateMockValidationResult(
            styleCount: 5,
            grammarCount: 3,
            knowledgeCount: 2);

        var panel = CreatePanel();
        panel.ViewModel.RefreshAsync(result);

        // Act
        panel.ViewModel.SelectedCategoryFilter = IssueCategory.Style;

        // Assert
        var styleIssues = panel.ViewModel.IssueGroups
            .SelectMany(g => g.Items)
            .Where(i => i.Issue.Category == IssueCategory.Style);

        Assert.AreEqual(5, styleIssues.Count());
    }

    [TestMethod]
    public void CanPublish_FalseWhenErrorsPresent()
    {
        // Arrange
        var result = CreateMockValidationResult(
            errorCount: 1);

        var panel = CreatePanel();
        panel.ViewModel.RefreshAsync(result);

        // Assert
        Assert.IsFalse(panel.ViewModel.CanPublish);
    }

    [TestMethod]
    public void CanPublish_TrueWhenNoErrors()
    {
        // Arrange
        var result = CreateMockValidationResult(
            errorCount: 0,
            warningCount: 3);

        var panel = CreatePanel();
        panel.ViewModel.RefreshAsync(result);

        // Assert
        Assert.IsTrue(panel.ViewModel.CanPublish);
    }
}
```

### 9.2 Integration Tests

```csharp
[TestClass]
public class UnifiedIssuesPanelIntegrationTests
{
    [TestMethod]
    public async Task FixRequested_UpdatesPanelAfterValidation()
    {
        // Full integration with aggregator and fix orchestrator
    }

    [TestMethod]
    public async Task RealTimeUpdates_RefreshesWhenValidationCompletes()
    {
        // Subscribe to validation events and verify panel updates
    }
}
```

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| UI thread blocking | Medium | Use async/await, keep UI responsive |
| Memory from large lists | Medium | Virtualize list (UI framework handles) |
| XSS in messages | Low | All text from internal code, no user input |
| Accidental fix application | Low | Require explicit click confirmation |

---

## 11. Acceptance Criteria

### 11.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Validation result with issues | Refreshing panel | Issues appear grouped by severity |
| 2 | Issue with fix | Clicking expand | Fix section becomes visible |
| 3 | Auto-fixable issue | Clicking "Apply Fix" | Fix applied, panel refreshes |
| 4 | Multiple fixable issues | Clicking "Fix All" | All fixes applied |
| 5 | Category filter selected | Filtering issues | Only selected category shown |
| 6 | Issue in document | Clicking on issue | Navigation to issue location |
| 7 | Empty validation result | Refreshing panel | "All good!" message shown |
| 8 | Error during fix | Applying fix | Error message shown, issue remains |

### 11.2 UI Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 9 | Panel with 100 issues | Rendering | Completes in < 200ms |
| 10 | Issue group | Collapsing | Smooth animation, state persisted |
| 11 | "Fix All" button | No auto-fixable issues | Button disabled |

---

## 12. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `IUnifiedIssuesPanel` interface | [ ] |
| 2 | `UnifiedIssuesPanelViewModel` class | [ ] |
| 3 | `IssuePresentationGroup` class | [ ] |
| 4 | `IssuePresentation` class | [ ] |
| 5 | `UnifiedIssuesPanel.xaml` (UI) | [ ] |
| 6 | `UnifiedIssuesPanel.xaml.cs` (code-behind) | [ ] |
| 7 | Event handlers for fix operations | [ ] |
| 8 | Filter logic | [ ] |
| 9 | Unit tests | [ ] |
| 10 | Integration tests | [ ] |
| 11 | Dark/light theme support | [ ] |
| 12 | Accessibility (keyboard nav, screen reader) | [ ] |

---

## 13. Verification Commands

```bash
# Run UI tests
dotnet test --filter "Version=v0.7.5g" --logger "console;verbosity=detailed"

# Run integration tests
dotnet test --filter "Category=Integration&Version=v0.7.5g"

# Manual verification:
# 1. Open panel with validation results
# 2. Verify grouping by severity
# 3. Test expand/collapse
# 4. Test filtering by category
# 5. Apply single fix, verify refresh
# 6. Apply "Fix All", verify all fixes
# 7. Test navigation to issue location
# 8. Test with empty result set
# 9. Test with very large issue count (100+)
# 10. Test keyboard navigation (Tab, Enter, Arrow keys)
# 11. Test with screen reader (Narrator)
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Lead Architect | Initial draft |
