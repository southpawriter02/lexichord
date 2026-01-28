# LCS-DES-074c: Design Specification — Preview/Diff UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `AGT-074c` | Sub-part of AGT-074 |
| **Feature Name** | `Preview/Diff UI` | Before/after comparison and change review |
| **Target Version** | `v0.7.4c` | Third sub-part of v0.7.4 |
| **Module Scope** | `Lexichord.Host` | Host application UI |
| **Swimlane** | `Ensemble` | Part of Agents vertical |
| **License Tier** | `WriterPro` | Required for simplification features |
| **Feature Gate Key** | `FeatureFlags.Agents.Simplifier` | Shared with parent feature |
| **Author** | Lead Architect | |
| **Reviewer** | | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-074-INDEX](./LCS-DES-074-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-074 Section 3.3](./LCS-SBD-074.md#33-v074c-previewdiff-ui) | |

---

## 2. Executive Summary

### 2.1 The Requirement

After the Simplifier Agent processes text, users need to:

- See the original and simplified text side by side
- Compare readability metrics (before vs. after)
- Review individual changes with explanations
- Accept or reject changes selectively
- Re-run simplification with different settings

Without a proper preview interface, users would blindly accept all changes, losing control over their content.

> **Goal:** Create an interactive preview interface that shows before/after comparison with metrics and enables selective change acceptance.

### 2.2 The Proposed Solution

Implement `SimplificationPreviewView` with:

1. **Metrics Comparison Panel** showing grade level, fog index, and passive voice improvements
2. **Diff View** with side-by-side, inline, and changes-only modes
3. **Change List** with checkboxes for selective acceptance
4. **Action Bar** with Accept All, Accept Selected, Reject, and Re-simplify buttons
5. **Keyboard shortcuts** for efficient review workflow

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `SimplificationResult` | v0.7.4b | Display simplification output |
| `SimplificationChange` | v0.7.4b | Individual change details |
| `ReadabilityMetrics` | v0.3.3c | Metrics comparison |
| `AudiencePreset` | v0.7.4a | Preset selection dropdown |
| `IReadabilityTargetService` | v0.7.4a | Get available presets |
| `ISimplificationPipeline` | v0.7.4b | Re-run simplification |
| `IEditorService` | v0.1.3a | Apply changes to document |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `DiffPlex` | 1.7.x | Text diff calculation and visualization |
| `Avalonia.Controls.DataGrid` | 11.x | Change list display (existing) |

### 3.2 Licensing Behavior

- **Load Behavior:** UI Gate - View loads but actions are disabled for lower tiers
- **Fallback Experience:**
  - Core/Writer tiers see "Upgrade to WriterPro" overlay
  - All action buttons disabled with upgrade tooltip
  - Metrics comparison panel visible (read-only preview)

---

## 4. Data Contract (The API)

### 4.1 ViewModel Interface

```csharp
namespace Lexichord.Host.ViewModels;

/// <summary>
/// ViewModel for the simplification preview and diff interface.
/// </summary>
public partial class SimplificationPreviewViewModel : ViewModelBase, IDisposable
{
    #region Observable Properties

    /// <summary>
    /// The original text before simplification.
    /// </summary>
    [ObservableProperty]
    private string _originalText = string.Empty;

    /// <summary>
    /// The simplified text result.
    /// </summary>
    [ObservableProperty]
    private string _simplifiedText = string.Empty;

    /// <summary>
    /// Readability metrics for the original text.
    /// </summary>
    [ObservableProperty]
    private ReadabilityMetrics? _originalMetrics;

    /// <summary>
    /// Readability metrics for the simplified text.
    /// </summary>
    [ObservableProperty]
    private ReadabilityMetrics? _simplifiedMetrics;

    /// <summary>
    /// List of changes made during simplification.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SimplificationChangeViewModel> _changes = new();

    /// <summary>
    /// Current diff view mode.
    /// </summary>
    [ObservableProperty]
    private DiffViewMode _viewMode = DiffViewMode.SideBySide;

    /// <summary>
    /// Whether a simplification operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing;

    /// <summary>
    /// Progress toward target grade level (0-100).
    /// </summary>
    [ObservableProperty]
    private double _progressToTarget;

    /// <summary>
    /// Currently selected audience preset.
    /// </summary>
    [ObservableProperty]
    private AudiencePreset? _selectedPreset;

    /// <summary>
    /// The diff model for visualization.
    /// </summary>
    [ObservableProperty]
    private SideBySideDiffModel? _diffModel;

    /// <summary>
    /// Whether there are any changes to accept.
    /// </summary>
    [ObservableProperty]
    private bool _hasChanges;

    /// <summary>
    /// Number of selected changes.
    /// </summary>
    [ObservableProperty]
    private int _selectedChangeCount;

    /// <summary>
    /// Error message if simplification failed.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    #endregion

    #region Read-only Properties

    /// <summary>
    /// Available audience presets.
    /// </summary>
    public IReadOnlyList<AudiencePreset> AvailablePresets { get; }

    /// <summary>
    /// Grade level improvement (positive = improvement).
    /// </summary>
    public double GradeLevelImprovement =>
        (OriginalMetrics?.FleschKincaidGradeLevel ?? 0) -
        (SimplifiedMetrics?.FleschKincaidGradeLevel ?? 0);

    /// <summary>
    /// Whether all changes are currently selected.
    /// </summary>
    public bool AllChangesSelected =>
        Changes.Count > 0 && Changes.All(c => c.IsSelected);

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanAcceptAll))]
    private async Task AcceptAllAsync();

    [RelayCommand(CanExecute = nameof(CanAcceptSelected))]
    private async Task AcceptSelectedAsync();

    [RelayCommand]
    private async Task RejectAllAsync();

    [RelayCommand(CanExecute = nameof(CanResimplify))]
    private async Task ResimplifyAsync();

    [RelayCommand]
    private void ToggleChange(SimplificationChangeViewModel change);

    [RelayCommand]
    private void SelectAllChanges();

    [RelayCommand]
    private void DeselectAllChanges();

    [RelayCommand]
    private void SetViewMode(DiffViewMode mode);

    #endregion
}

/// <summary>
/// Diff visualization modes.
/// </summary>
public enum DiffViewMode
{
    /// <summary>
    /// Original and simplified text shown in parallel columns.
    /// </summary>
    SideBySide,

    /// <summary>
    /// Changes shown inline with additions/deletions marked.
    /// </summary>
    Inline,

    /// <summary>
    /// Only changed sections shown (collapsed unchanged).
    /// </summary>
    ChangesOnly
}

/// <summary>
/// ViewModel for a single simplification change.
/// </summary>
public partial class SimplificationChangeViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private bool _isExpanded;

    public SimplificationChange Change { get; }

    public string OriginalText => Change.OriginalText;
    public string SimplifiedText => Change.SimplifiedText;
    public SimplificationChangeType ChangeType => Change.ChangeType;
    public string Explanation => Change.Explanation;
    public string ChangeTypeDisplay => FormatChangeType(Change.ChangeType);
    public string ChangeTypeIcon => GetChangeTypeIcon(Change.ChangeType);

    public SimplificationChangeViewModel(SimplificationChange change)
    {
        Change = change;
    }

    private static string FormatChangeType(SimplificationChangeType type) => type switch
    {
        SimplificationChangeType.SentenceSplit => "Sentence Split",
        SimplificationChangeType.JargonReplacement => "Jargon Replacement",
        SimplificationChangeType.PassiveToActive => "Passive to Active",
        SimplificationChangeType.WordSimplification => "Word Simplification",
        SimplificationChangeType.ClauseReduction => "Clause Reduction",
        SimplificationChangeType.TransitionAdded => "Transition Added",
        SimplificationChangeType.RedundancyRemoved => "Redundancy Removed",
        _ => "Combined"
    };

    private static string GetChangeTypeIcon(SimplificationChangeType type) => type switch
    {
        SimplificationChangeType.SentenceSplit => "scissors",
        SimplificationChangeType.JargonReplacement => "book-open",
        SimplificationChangeType.PassiveToActive => "arrow-right",
        SimplificationChangeType.WordSimplification => "type",
        SimplificationChangeType.ClauseReduction => "minimize-2",
        SimplificationChangeType.TransitionAdded => "link",
        SimplificationChangeType.RedundancyRemoved => "trash-2",
        _ => "edit"
    };
}
```

### 4.2 View Events

```csharp
namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when user accepts simplification changes.
/// </summary>
public record SimplificationAcceptedEvent(
    string DocumentPath,
    string OriginalText,
    string SimplifiedText,
    int AcceptedChangeCount,
    int TotalChangeCount,
    double GradeLevelReduction
) : INotification;

/// <summary>
/// Published when user rejects simplification changes.
/// </summary>
public record SimplificationRejectedEvent(
    string DocumentPath,
    string Reason
) : INotification;

/// <summary>
/// Published when user requests re-simplification.
/// </summary>
public record ResimplificationRequestedEvent(
    string DocumentPath,
    string OriginalText,
    AudiencePreset NewPreset
) : INotification;
```

---

## 5. Implementation Logic

### 5.1 ViewModel Implementation

```csharp
namespace Lexichord.Host.ViewModels;

public partial class SimplificationPreviewViewModel : ViewModelBase, IDisposable
{
    private readonly ISimplificationPipeline _pipeline;
    private readonly IReadabilityTargetService _targetService;
    private readonly IEditorService _editorService;
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<SimplificationPreviewViewModel> _logger;

    private SimplificationResult? _currentResult;
    private string? _documentPath;
    private CancellationTokenSource? _cts;

    public SimplificationPreviewViewModel(
        ISimplificationPipeline pipeline,
        IReadabilityTargetService targetService,
        IEditorService editorService,
        IMediator mediator,
        ILicenseContext licenseContext,
        ILogger<SimplificationPreviewViewModel> logger)
    {
        _pipeline = pipeline;
        _targetService = targetService;
        _editorService = editorService;
        _mediator = mediator;
        _licenseContext = licenseContext;
        _logger = logger;

        AvailablePresets = _targetService.GetAvailablePresets();
        SelectedPreset = AvailablePresets.FirstOrDefault(p => p.PresetId == "general-public");

        // Subscribe to change selection updates
        Changes.CollectionChanged += (_, _) => UpdateSelectionState();
    }

    #region Initialization

    /// <summary>
    /// Initializes the preview with a simplification result.
    /// </summary>
    public void Initialize(SimplificationResult result, string documentPath)
    {
        _currentResult = result;
        _documentPath = documentPath;

        OriginalText = result.SimplifiedText != result.OriginalMetrics.SourceText
            ? result.SimplifiedText  // This is wrong - need original
            : string.Empty;

        // TODO: Need to store original text in result
        SimplifiedText = result.SimplifiedText;
        OriginalMetrics = result.OriginalMetrics;
        SimplifiedMetrics = result.SimplifiedMetrics;

        // Populate changes
        Changes.Clear();
        foreach (var change in result.Changes)
        {
            Changes.Add(new SimplificationChangeViewModel(change));
        }

        // Calculate progress
        if (SelectedPreset != null)
        {
            var target = SelectedPreset.TargetGradeLevel;
            var current = SimplifiedMetrics?.FleschKincaidGradeLevel ?? target;
            var original = OriginalMetrics?.FleschKincaidGradeLevel ?? target;

            if (original > target)
            {
                var totalReduction = original - target;
                var achieved = original - current;
                ProgressToTarget = Math.Min(100, (achieved / totalReduction) * 100);
            }
            else
            {
                ProgressToTarget = 100;
            }
        }

        // Build diff model
        BuildDiffModel();

        HasChanges = Changes.Count > 0;
        UpdateSelectionState();

        _logger.LogDebug(
            "Preview initialized: {ChangeCount} changes, Grade {Before} -> {After}",
            Changes.Count,
            OriginalMetrics?.FleschKincaidGradeLevel,
            SimplifiedMetrics?.FleschKincaidGradeLevel);
    }

    #endregion

    #region Commands Implementation

    private bool CanAcceptAll() =>
        HasChanges &&
        !IsProcessing &&
        _licenseContext.HasFeature(FeatureFlags.Agents.Simplifier);

    private async Task AcceptAllAsyncImpl()
    {
        try
        {
            IsProcessing = true;

            // Apply simplified text to document
            if (_documentPath != null)
            {
                await _editorService.ReplaceSelectionAsync(_documentPath, SimplifiedText);
            }

            // Publish event
            await _mediator.Publish(new SimplificationAcceptedEvent(
                _documentPath ?? string.Empty,
                OriginalText,
                SimplifiedText,
                Changes.Count,
                Changes.Count,
                GradeLevelImprovement));

            _logger.LogInformation(
                "Accepted all {Count} simplification changes",
                Changes.Count);

            // Close the preview
            CloseRequested?.Invoke(this, new CloseRequestedEventArgs(accepted: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept changes");
            ErrorMessage = $"Failed to apply changes: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool CanAcceptSelected() =>
        SelectedChangeCount > 0 &&
        !IsProcessing &&
        _licenseContext.HasFeature(FeatureFlags.Agents.Simplifier);

    private async Task AcceptSelectedAsyncImpl()
    {
        try
        {
            IsProcessing = true;

            // Build text with only selected changes applied
            var selectedChanges = Changes.Where(c => c.IsSelected).Select(c => c.Change).ToList();
            var partialText = ApplySelectedChanges(OriginalText, selectedChanges);

            // Apply to document
            if (_documentPath != null)
            {
                await _editorService.ReplaceSelectionAsync(_documentPath, partialText);
            }

            await _mediator.Publish(new SimplificationAcceptedEvent(
                _documentPath ?? string.Empty,
                OriginalText,
                partialText,
                SelectedChangeCount,
                Changes.Count,
                GradeLevelImprovement));

            _logger.LogInformation(
                "Accepted {Selected} of {Total} simplification changes",
                SelectedChangeCount, Changes.Count);

            CloseRequested?.Invoke(this, new CloseRequestedEventArgs(accepted: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept selected changes");
            ErrorMessage = $"Failed to apply changes: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task RejectAllAsyncImpl()
    {
        await _mediator.Publish(new SimplificationRejectedEvent(
            _documentPath ?? string.Empty,
            "User rejected all changes"));

        _logger.LogInformation("User rejected all simplification changes");

        CloseRequested?.Invoke(this, new CloseRequestedEventArgs(accepted: false));
    }

    private bool CanResimplify() =>
        !IsProcessing &&
        SelectedPreset != null &&
        _licenseContext.HasFeature(FeatureFlags.Agents.Simplifier);

    private async Task ResimplifyAsyncImpl()
    {
        if (SelectedPreset == null || string.IsNullOrEmpty(OriginalText))
            return;

        try
        {
            IsProcessing = true;
            ErrorMessage = null;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var target = _targetService.GetTargetForPreset(SelectedPreset.PresetId);
            var request = new SimplificationRequest
            {
                OriginalText = OriginalText,
                Target = target,
                DocumentPath = _documentPath
            };

            var result = await _pipeline.SimplifyAsync(request, _cts.Token);

            Initialize(result, _documentPath ?? string.Empty);

            await _mediator.Publish(new ResimplificationRequestedEvent(
                _documentPath ?? string.Empty,
                OriginalText,
                SelectedPreset));

            _logger.LogInformation(
                "Re-simplified with preset {Preset}",
                SelectedPreset.PresetId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Re-simplification cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Re-simplification failed");
            ErrorMessage = $"Simplification failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void ToggleChangeImpl(SimplificationChangeViewModel change)
    {
        change.IsSelected = !change.IsSelected;
        UpdateSelectionState();
    }

    private void SelectAllChangesImpl()
    {
        foreach (var change in Changes)
        {
            change.IsSelected = true;
        }
        UpdateSelectionState();
    }

    private void DeselectAllChangesImpl()
    {
        foreach (var change in Changes)
        {
            change.IsSelected = false;
        }
        UpdateSelectionState();
    }

    private void SetViewModeImpl(DiffViewMode mode)
    {
        ViewMode = mode;
        BuildDiffModel();
    }

    #endregion

    #region Helper Methods

    private void UpdateSelectionState()
    {
        SelectedChangeCount = Changes.Count(c => c.IsSelected);
        OnPropertyChanged(nameof(AllChangesSelected));
        AcceptAllCommand.NotifyCanExecuteChanged();
        AcceptSelectedCommand.NotifyCanExecuteChanged();
    }

    private void BuildDiffModel()
    {
        if (string.IsNullOrEmpty(OriginalText) || string.IsNullOrEmpty(SimplifiedText))
        {
            DiffModel = null;
            return;
        }

        var differ = new SideBySideDiffBuilder(new Differ());
        DiffModel = differ.BuildDiffModel(OriginalText, SimplifiedText);
    }

    private string ApplySelectedChanges(
        string originalText,
        IReadOnlyList<SimplificationChange> selectedChanges)
    {
        // Sort changes by location (reverse order to preserve positions)
        var sortedChanges = selectedChanges
            .Where(c => c.Location.HasValue)
            .OrderByDescending(c => c.Location!.Value.Start)
            .ToList();

        var result = originalText;
        foreach (var change in sortedChanges)
        {
            var loc = change.Location!.Value;
            result = result.Remove(loc.Start, loc.Length)
                          .Insert(loc.Start, change.SimplifiedText);
        }

        return result;
    }

    #endregion

    #region Events

    public event EventHandler<CloseRequestedEventArgs>? CloseRequested;

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    #endregion
}

public class CloseRequestedEventArgs : EventArgs
{
    public bool Accepted { get; }
    public CloseRequestedEventArgs(bool accepted) => Accepted = accepted;
}
```

### 5.2 View Structure (AXAML)

```xml
<!-- SimplificationPreviewView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels"
             xmlns:controls="using:Lexichord.Host.Controls"
             x:Class="Lexichord.Host.Views.SimplificationPreviewView"
             x:DataType="vm:SimplificationPreviewViewModel">

    <Design.DataContext>
        <vm:SimplificationPreviewViewModel />
    </Design.DataContext>

    <Grid RowDefinitions="Auto,Auto,*,Auto,Auto">

        <!-- Header: Metrics Comparison -->
        <Border Grid.Row="0" Classes="panel-header">
            <Grid ColumnDefinitions="*,Auto">
                <!-- Metrics Comparison Panel -->
                <controls:ReadabilityComparisonPanel
                    OriginalMetrics="{Binding OriginalMetrics}"
                    SimplifiedMetrics="{Binding SimplifiedMetrics}"
                    ProgressToTarget="{Binding ProgressToTarget}" />

                <!-- Preset Selector -->
                <StackPanel Grid.Column="1" Orientation="Vertical" Spacing="4">
                    <TextBlock Text="Target Audience" Classes="label" />
                    <ComboBox ItemsSource="{Binding AvailablePresets}"
                              SelectedItem="{Binding SelectedPreset}"
                              Width="200">
                        <ComboBox.ItemTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" Spacing="8">
                                    <PathIcon Data="{Binding Icon, Converter={StaticResource IconConverter}}"
                                              Width="16" Height="16" />
                                    <TextBlock Text="{Binding Name}" />
                                    <TextBlock Text="{Binding TargetGradeLevel, StringFormat='Grade {0}'}"
                                               Classes="muted" />
                                </StackPanel>
                            </DataTemplate>
                        </ComboBox.ItemTemplate>
                    </ComboBox>
                    <Button Command="{Binding ResimplifyCommand}"
                            Content="Re-simplify"
                            Classes="secondary small">
                        <Button.IsEnabled>
                            <MultiBinding Converter="{StaticResource BoolAndConverter}">
                                <Binding Path="!IsProcessing" />
                                <Binding Path="SelectedPreset" Converter="{StaticResource IsNotNullConverter}" />
                            </MultiBinding>
                        </Button.IsEnabled>
                    </Button>
                </StackPanel>
            </Grid>
        </Border>

        <!-- View Mode Selector -->
        <Border Grid.Row="1" Classes="toolbar">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <RadioButton GroupName="ViewMode"
                             IsChecked="{Binding ViewMode, Converter={StaticResource EnumBoolConverter}, ConverterParameter=SideBySide}"
                             Command="{Binding SetViewModeCommand}"
                             CommandParameter="{x:Static vm:DiffViewMode.SideBySide}">
                    <StackPanel Orientation="Horizontal" Spacing="4">
                        <PathIcon Data="{StaticResource ColumnsIcon}" Width="16" Height="16" />
                        <TextBlock Text="Side-by-Side" />
                    </StackPanel>
                </RadioButton>

                <RadioButton GroupName="ViewMode"
                             IsChecked="{Binding ViewMode, Converter={StaticResource EnumBoolConverter}, ConverterParameter=Inline}"
                             Command="{Binding SetViewModeCommand}"
                             CommandParameter="{x:Static vm:DiffViewMode.Inline}">
                    <StackPanel Orientation="Horizontal" Spacing="4">
                        <PathIcon Data="{StaticResource AlignLeftIcon}" Width="16" Height="16" />
                        <TextBlock Text="Inline" />
                    </StackPanel>
                </RadioButton>

                <RadioButton GroupName="ViewMode"
                             IsChecked="{Binding ViewMode, Converter={StaticResource EnumBoolConverter}, ConverterParameter=ChangesOnly}"
                             Command="{Binding SetViewModeCommand}"
                             CommandParameter="{x:Static vm:DiffViewMode.ChangesOnly}">
                    <StackPanel Orientation="Horizontal" Spacing="4">
                        <PathIcon Data="{StaticResource ListIcon}" Width="16" Height="16" />
                        <TextBlock Text="Changes Only" />
                    </StackPanel>
                </RadioButton>
            </StackPanel>
        </Border>

        <!-- Diff View -->
        <Grid Grid.Row="2">
            <!-- Side-by-Side View -->
            <Grid ColumnDefinitions="*,Auto,*"
                  IsVisible="{Binding ViewMode, Converter={StaticResource EnumBoolConverter}, ConverterParameter=SideBySide}">

                <!-- Original Text -->
                <Border Grid.Column="0" Classes="diff-panel">
                    <DockPanel>
                        <TextBlock DockPanel.Dock="Top" Text="Original" Classes="panel-title" />
                        <controls:DiffTextBox Text="{Binding OriginalText}"
                                              DiffModel="{Binding DiffModel}"
                                              Side="Old"
                                              IsReadOnly="True" />
                    </DockPanel>
                </Border>

                <GridSplitter Grid.Column="1" Width="4" />

                <!-- Simplified Text -->
                <Border Grid.Column="2" Classes="diff-panel">
                    <DockPanel>
                        <TextBlock DockPanel.Dock="Top" Text="Simplified" Classes="panel-title" />
                        <controls:DiffTextBox Text="{Binding SimplifiedText}"
                                              DiffModel="{Binding DiffModel}"
                                              Side="New"
                                              IsReadOnly="True" />
                    </DockPanel>
                </Border>
            </Grid>

            <!-- Inline View -->
            <Border Classes="diff-panel"
                    IsVisible="{Binding ViewMode, Converter={StaticResource EnumBoolConverter}, ConverterParameter=Inline}">
                <controls:InlineDiffView DiffModel="{Binding DiffModel}" />
            </Border>

            <!-- Changes Only View -->
            <Border Classes="diff-panel"
                    IsVisible="{Binding ViewMode, Converter={StaticResource EnumBoolConverter}, ConverterParameter=ChangesOnly}">
                <controls:ChangesOnlyView Changes="{Binding Changes}" />
            </Border>
        </Grid>

        <!-- Changes List -->
        <Border Grid.Row="3" Classes="changes-panel">
            <Expander Header="{Binding Changes.Count, StringFormat='Changes ({0})'}"
                      IsExpanded="True">
                <DockPanel>
                    <!-- Select All / Deselect All -->
                    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="0,0,0,8">
                        <Button Command="{Binding SelectAllChangesCommand}"
                                Content="Select All"
                                Classes="link small" />
                        <TextBlock Text="|" Classes="muted" />
                        <Button Command="{Binding DeselectAllChangesCommand}"
                                Content="Deselect All"
                                Classes="link small" />
                        <TextBlock Classes="muted">
                            <Run Text="{Binding SelectedChangeCount}" />
                            <Run Text=" of " />
                            <Run Text="{Binding Changes.Count}" />
                            <Run Text=" selected" />
                        </TextBlock>
                    </StackPanel>

                    <!-- Changes List -->
                    <ScrollViewer MaxHeight="200">
                        <ItemsControl ItemsSource="{Binding Changes}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate DataType="vm:SimplificationChangeViewModel">
                                    <Border Classes="change-item" Margin="0,2">
                                        <Grid ColumnDefinitions="Auto,*,Auto">
                                            <CheckBox Grid.Column="0"
                                                      IsChecked="{Binding IsSelected}"
                                                      Margin="0,0,8,0" />

                                            <StackPanel Grid.Column="1" Spacing="2">
                                                <StackPanel Orientation="Horizontal" Spacing="8">
                                                    <Border Classes="change-type-badge">
                                                        <StackPanel Orientation="Horizontal" Spacing="4">
                                                            <PathIcon Data="{Binding ChangeTypeIcon, Converter={StaticResource IconConverter}}"
                                                                      Width="12" Height="12" />
                                                            <TextBlock Text="{Binding ChangeTypeDisplay}"
                                                                       Classes="small" />
                                                        </StackPanel>
                                                    </Border>
                                                    <TextBlock Text="{Binding Explanation}"
                                                               Classes="muted small" />
                                                </StackPanel>

                                                <StackPanel Orientation="Horizontal" Spacing="8">
                                                    <TextBlock Classes="diff-removed">
                                                        <Run Text="&quot;" />
                                                        <Run Text="{Binding OriginalText}" />
                                                        <Run Text="&quot;" />
                                                    </TextBlock>
                                                    <PathIcon Data="{StaticResource ArrowRightIcon}"
                                                              Width="12" Height="12" />
                                                    <TextBlock Classes="diff-added">
                                                        <Run Text="&quot;" />
                                                        <Run Text="{Binding SimplifiedText}" />
                                                        <Run Text="&quot;" />
                                                    </TextBlock>
                                                </StackPanel>
                                            </StackPanel>

                                            <Button Grid.Column="2"
                                                    Command="{Binding $parent[ItemsControl].DataContext.ToggleChangeCommand}"
                                                    CommandParameter="{Binding}"
                                                    Classes="icon-button small">
                                                <PathIcon Data="{StaticResource ChevronDownIcon}"
                                                          Width="16" Height="16" />
                                            </Button>
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </ScrollViewer>
                </DockPanel>
            </Expander>
        </Border>

        <!-- Action Bar -->
        <Border Grid.Row="4" Classes="action-bar">
            <Grid ColumnDefinitions="*,Auto">
                <!-- Error Message -->
                <TextBlock Grid.Column="0"
                           Text="{Binding ErrorMessage}"
                           Classes="error"
                           IsVisible="{Binding ErrorMessage, Converter={StaticResource IsNotNullConverter}}" />

                <!-- Action Buttons -->
                <StackPanel Grid.Column="1"
                            Orientation="Horizontal"
                            Spacing="8"
                            HorizontalAlignment="Right">

                    <Button Command="{Binding AcceptAllCommand}"
                            Classes="primary"
                            HotKey="Ctrl+Return">
                        <StackPanel Orientation="Horizontal" Spacing="4">
                            <PathIcon Data="{StaticResource CheckIcon}" Width="16" Height="16" />
                            <TextBlock Text="Accept All" />
                            <TextBlock Text="(Ctrl+Enter)" Classes="hotkey muted" />
                        </StackPanel>
                    </Button>

                    <Button Command="{Binding AcceptSelectedCommand}"
                            Classes="secondary">
                        <TextBlock Text="Accept Selected" />
                    </Button>

                    <Button Command="{Binding RejectAllCommand}"
                            Classes="secondary">
                        <TextBlock Text="Reject" />
                    </Button>

                    <Button Classes="ghost"
                            Click="OnCancelClick">
                        <TextBlock Text="Cancel" />
                    </Button>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Loading Overlay -->
        <Border Grid.RowSpan="5"
                Classes="loading-overlay"
                IsVisible="{Binding IsProcessing}">
            <StackPanel HorizontalAlignment="Center"
                        VerticalAlignment="Center"
                        Spacing="16">
                <ProgressRing IsIndeterminate="True" Width="48" Height="48" />
                <TextBlock Text="Processing..." Classes="loading-text" />
            </StackPanel>
        </Border>

    </Grid>

</UserControl>
```

---

## 6. UI/UX Specifications

### 6.1 Visual Layout

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  METRICS COMPARISON                                    TARGET AUDIENCE       │
│  ┌─────────────────────────────────────────┐ ┌─────────────────────────────┐ │
│  │ BEFORE              AFTER               │ │ [General Public ▼]          │ │
│  │ ┌──────────┬──────────┐                 │ │                             │ │
│  │ │ Grade 14 │ Grade 8  │  ↓ 6 levels     │ │ ████████████████░░░░ 78%   │ │
│  │ │ Fog 18.2 │ Fog 10.1 │  ↓ 8.1          │ │ Progress to target          │ │
│  │ │ Pass 34% │ Pass 8%  │  ↓ 26%          │ │                             │ │
│  │ └──────────┴──────────┘                 │ │ [↺ Re-simplify]             │ │
│  └─────────────────────────────────────────┘ └─────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────────────────┤
│  (◉) Side-by-Side  ( ) Inline  ( ) Changes Only                              │
├───────────────────────────────────┬──────────────────────────────────────────┤
│  ORIGINAL                         │  SIMPLIFIED                              │
├───────────────────────────────────┼──────────────────────────────────────────┤
│                                   │                                          │
│  The [-implementation-] of the    │  The [+new feature was added to+]        │
│  [-aforementioned feature was-]   │  [+help users work faster.+] It          │
│  [-facilitated-] by the           │  was built by the development            │
│  development team [-in order to-] │  team. Testing showed that               │
│  [-enhance the user experience.-] │  users completed tasks 40%               │
│                                   │  faster than before.                     │
│                                   │                                          │
├───────────────────────────────────┴──────────────────────────────────────────┤
│  ▼ Changes (6)                                     Select All | Deselect All │
├──────────────────────────────────────────────────────────────────────────────┤
│  [✓] 📖 Jargon Replacement                                                   │
│      "implementation...facilitated" → "new feature was added"                │
│      Simplified complex corporate language                                   │
│  [✓] ✂️ Clause Reduction                                                      │
│      "in order to enhance" → "to help users work faster"                     │
│      Removed unnecessary words                                               │
│  [ ] ➡️ Passive to Active                                                     │
│      "was conducted" → "Testing showed"                                      │
│      Converted passive voice                                                 │
├──────────────────────────────────────────────────────────────────────────────┤
│                      [Accept All (Ctrl+⏎)] [Accept Selected] [Reject] [Cancel]│
└──────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 Color Scheme

| Element | Light Theme | Dark Theme | Usage |
| :--- | :--- | :--- | :--- |
| Removed text background | `#FFE5E5` | `#4A2020` | Original text that was changed |
| Removed text foreground | `#B91C1C` | `#FCA5A5` | Strike-through original |
| Added text background | `#DCFCE7` | `#14532D` | New simplified text |
| Added text foreground | `#166534` | `#86EFAC` | Highlight additions |
| Unchanged text | Default | Default | Text that wasn't modified |
| Metric improvement | `#16A34A` | `#4ADE80` | Green for improvements |
| Metric regression | `#EA580C` | `#FB923C` | Orange for worse values |
| Progress bar | `#3B82F6` | `#60A5FA` | Brand blue |

### 6.3 Keyboard Shortcuts

| Shortcut | Action | Context |
| :--- | :--- | :--- |
| `Ctrl+Enter` | Accept All | Global |
| `Ctrl+Shift+Enter` | Accept Selected | Global |
| `Escape` | Cancel/Close | Global |
| `Space` | Toggle selected change | Change list focused |
| `Up/Down` | Navigate changes | Change list focused |
| `Ctrl+A` | Select all changes | Change list focused |
| `Ctrl+D` | Deselect all changes | Change list focused |
| `1` | Side-by-Side view | Global |
| `2` | Inline view | Global |
| `3` | Changes Only view | Global |

### 6.4 Accessibility Requirements

| Requirement | Implementation |
| :--- | :--- |
| Screen reader support | All interactive elements have `AutomationProperties.Name` |
| Keyboard navigation | Full Tab order through all controls |
| Focus indicators | Visible focus ring on all interactive elements |
| Color contrast | Meets WCAG AA contrast ratios |
| Reduced motion | Disable animations when `prefers-reduced-motion` |

---

## 7. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Preview initialized: {ChangeCount} changes, Grade {Before} -> {After}"` |
| Info | `"Accepted all {Count} simplification changes"` |
| Info | `"Accepted {Selected} of {Total} simplification changes"` |
| Info | `"User rejected all simplification changes"` |
| Info | `"Re-simplified with preset {Preset}"` |
| Debug | `"Re-simplification cancelled"` |
| Error | `"Failed to accept changes: {Error}"` |
| Error | `"Re-simplification failed: {Error}"` |

---

## 8. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| XSS via diff display | Low | Sanitize HTML in diff rendering |
| Large text DoS | Low | Virtualize diff view; limit displayed lines |
| Clipboard exposure | Low | Don't auto-copy; require explicit action |

---

## 9. Acceptance Criteria

### 9.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | SimplificationResult loaded | Viewing preview | Original and simplified text displayed |
| 2 | Metrics available | Viewing preview | Before/after metrics comparison shown |
| 3 | Changes available | Viewing preview | All changes listed with types and explanations |
| 4 | Side-by-Side mode selected | Viewing diff | Two columns with synchronized scrolling |
| 5 | Inline mode selected | Viewing diff | Single view with inline additions/deletions |
| 6 | Changes Only mode selected | Viewing diff | Only changed sections visible |
| 7 | Accept All clicked | With changes selected | All changes applied to document |
| 8 | Accept Selected clicked | With some changes selected | Only selected changes applied |
| 9 | Reject clicked | Any state | Preview closes, no changes applied |
| 10 | Re-simplify clicked | With different preset | New simplification with new preset |
| 11 | Core tier user | Viewing preview | Action buttons disabled with upgrade prompt |

### 9.2 Accessibility Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 12 | Keyboard-only user | Navigating preview | All controls accessible via Tab |
| 13 | Screen reader user | Reading preview | All elements have descriptive labels |
| 14 | Ctrl+Enter pressed | Preview focused | Accept All executed |
| 15 | Escape pressed | Preview focused | Preview closes |

---

## 10. Test Scenarios

### 10.1 Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.4c")]
public class SimplificationPreviewViewModelTests
{
    private readonly SimplificationPreviewViewModel _sut;
    private readonly Mock<ISimplificationPipeline> _mockPipeline;
    private readonly Mock<IEditorService> _mockEditor;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILicenseContext> _mockLicense;

    public SimplificationPreviewViewModelTests()
    {
        // ... setup mocks
        _mockLicense.Setup(l => l.HasFeature(It.IsAny<string>())).Returns(true);
        _sut = new SimplificationPreviewViewModel(/* ... */);
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_WithResult_SetsProperties()
    {
        var result = CreateMockResult();

        _sut.Initialize(result, "/path/to/doc.md");

        _sut.SimplifiedText.Should().Be(result.SimplifiedText);
        _sut.OriginalMetrics.Should().Be(result.OriginalMetrics);
        _sut.SimplifiedMetrics.Should().Be(result.SimplifiedMetrics);
        _sut.Changes.Should().HaveCount(result.Changes.Count);
    }

    [Fact]
    public void Initialize_WithChanges_AllSelectedByDefault()
    {
        var result = CreateMockResult();

        _sut.Initialize(result, "/path");

        _sut.Changes.Should().OnlyContain(c => c.IsSelected);
        _sut.AllChangesSelected.Should().BeTrue();
    }

    #endregion

    #region Selection Tests

    [Fact]
    public void ToggleChange_DeselectsChange()
    {
        var result = CreateMockResult();
        _sut.Initialize(result, "/path");
        var change = _sut.Changes.First();

        _sut.ToggleChangeCommand.Execute(change);

        change.IsSelected.Should().BeFalse();
        _sut.SelectedChangeCount.Should().Be(_sut.Changes.Count - 1);
    }

    [Fact]
    public void SelectAllChanges_SelectsAll()
    {
        var result = CreateMockResult();
        _sut.Initialize(result, "/path");
        foreach (var c in _sut.Changes) c.IsSelected = false;

        _sut.SelectAllChangesCommand.Execute(null);

        _sut.Changes.Should().OnlyContain(c => c.IsSelected);
    }

    [Fact]
    public void DeselectAllChanges_DeselectsAll()
    {
        var result = CreateMockResult();
        _sut.Initialize(result, "/path");

        _sut.DeselectAllChangesCommand.Execute(null);

        _sut.Changes.Should().OnlyContain(c => !c.IsSelected);
        _sut.SelectedChangeCount.Should().Be(0);
    }

    #endregion

    #region Accept Tests

    [Fact]
    public async Task AcceptAll_AppliesChangesToDocument()
    {
        var result = CreateMockResult();
        _sut.Initialize(result, "/path/doc.md");

        await _sut.AcceptAllCommand.ExecuteAsync(null);

        _mockEditor.Verify(e => e.ReplaceSelectionAsync(
            "/path/doc.md",
            result.SimplifiedText), Times.Once);
    }

    [Fact]
    public async Task AcceptAll_PublishesEvent()
    {
        var result = CreateMockResult();
        _sut.Initialize(result, "/path");

        await _sut.AcceptAllCommand.ExecuteAsync(null);

        _mockMediator.Verify(m => m.Publish(
            It.IsAny<SimplificationAcceptedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AcceptAll_DisabledForCoreTier()
    {
        _mockLicense.Setup(l => l.HasFeature(It.IsAny<string>())).Returns(false);
        var sut = new SimplificationPreviewViewModel(/* ... */);
        sut.Initialize(CreateMockResult(), "/path");

        sut.AcceptAllCommand.CanExecute(null).Should().BeFalse();
    }

    #endregion

    #region View Mode Tests

    [Fact]
    public void SetViewMode_UpdatesViewMode()
    {
        _sut.SetViewModeCommand.Execute(DiffViewMode.Inline);

        _sut.ViewMode.Should().Be(DiffViewMode.Inline);
    }

    [Theory]
    [InlineData(DiffViewMode.SideBySide)]
    [InlineData(DiffViewMode.Inline)]
    [InlineData(DiffViewMode.ChangesOnly)]
    public void SetViewMode_AllModesSupported(DiffViewMode mode)
    {
        _sut.Initialize(CreateMockResult(), "/path");

        _sut.SetViewModeCommand.Execute(mode);

        _sut.ViewMode.Should().Be(mode);
    }

    #endregion

    private SimplificationResult CreateMockResult() => new()
    {
        SimplifiedText = "The simplified text.",
        OriginalMetrics = new ReadabilityMetrics { FleschKincaidGradeLevel = 14 },
        SimplifiedMetrics = new ReadabilityMetrics { FleschKincaidGradeLevel = 8 },
        Changes = new[]
        {
            new SimplificationChange
            {
                OriginalText = "complex",
                SimplifiedText = "simple",
                ChangeType = SimplificationChangeType.WordSimplification,
                Explanation = "Simplified word"
            },
            new SimplificationChange
            {
                OriginalText = "was written",
                SimplifiedText = "wrote",
                ChangeType = SimplificationChangeType.PassiveToActive,
                Explanation = "Active voice"
            }
        },
        TokenUsage = new UsageMetrics(100, 200, 0),
        ProcessingTime = TimeSpan.FromSeconds(2)
    };
}
```

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `SimplificationPreviewView.axaml` | [ ] |
| 2 | `SimplificationPreviewView.axaml.cs` code-behind | [ ] |
| 3 | `SimplificationPreviewViewModel.cs` | [ ] |
| 4 | `SimplificationChangeViewModel.cs` | [ ] |
| 5 | `DiffViewMode.cs` enum | [ ] |
| 6 | `ReadabilityComparisonPanel.axaml` component | [ ] |
| 7 | `DiffTextBox.axaml` custom control | [ ] |
| 8 | `InlineDiffView.axaml` custom control | [ ] |
| 9 | `ChangesOnlyView.axaml` custom control | [ ] |
| 10 | `SimplificationAcceptedEvent.cs` | [ ] |
| 11 | `SimplificationRejectedEvent.cs` | [ ] |
| 12 | Theme styles for diff highlighting | [ ] |
| 13 | Keyboard shortcut bindings | [ ] |
| 14 | `SimplificationPreviewViewModelTests.cs` | [ ] |
| 15 | Accessibility attributes | [ ] |

---

## 12. Verification Commands

```bash
# Run all v0.7.4c tests
dotnet test --filter "Version=v0.7.4c" --logger "console;verbosity=detailed"

# Run ViewModel tests
dotnet test --filter "FullyQualifiedName~SimplificationPreviewViewModelTests"

# Run with coverage
dotnet test --filter "Version=v0.7.4c" --collect:"XPlat Code Coverage"

# Manual verification:
# a) Invoke simplification on sample text
# b) Verify preview opens with metrics comparison
# c) Test all three view modes (Side-by-Side, Inline, Changes Only)
# d) Toggle individual changes and verify selection count updates
# e) Test Accept All with Ctrl+Enter shortcut
# f) Test Accept Selected with partial selection
# g) Test Re-simplify with different preset
# h) Verify keyboard navigation through all controls
# i) Test with screen reader (Narrator/NVDA)
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
