# LCS-DES-047c: Indexing Progress

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-047c                             |
| **Version**      | v0.4.7c                                  |
| **Title**        | Indexing Progress                        |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | WriterPro                                |

---

## 1. Overview

### 1.1 Purpose

This specification defines `IndexingProgressView` and `IndexingProgressViewModel`, a toast overlay that shows real-time progress during indexing operations. Users can see the current file being processed, overall progress, and cancel long-running operations.

### 1.2 Goals

- Create toast overlay for indexing progress
- Display current file being processed
- Show progress bar with percentage
- Support cancellation of operations
- Auto-dismiss on completion
- Throttle UI updates to prevent performance issues

### 1.3 Non-Goals

- Detailed per-file progress (shows document-level only)
- Progress persistence across sessions
- Multiple concurrent progress displays

---

## 2. Design

### 2.1 IndexingProgressViewModel

```csharp
namespace Lexichord.Modules.RAG.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for the indexing progress toast overlay.
/// </summary>
public partial class IndexingProgressViewModel : ViewModelBase, IDisposable
{
    private readonly IMediator _mediator;
    private readonly ILogger<IndexingProgressViewModel> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private IDisposable? _progressSubscription;
    private DateTime _lastUpdate = DateTime.MinValue;
    private const int ThrottleMs = 100; // Update UI at most every 100ms

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _currentDocument = string.Empty;

    [ObservableProperty]
    private int _processedCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _percentComplete;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private bool _wasCancelled;

    [ObservableProperty]
    private string _operationTitle = "Indexing documents...";

    [ObservableProperty]
    private TimeSpan _elapsedTime;

    private Stopwatch? _stopwatch;

    public IndexingProgressViewModel(
        IMediator mediator,
        ILogger<IndexingProgressViewModel> logger)
    {
        _mediator = mediator;
        _logger = logger;

        // Subscribe to progress events
        _progressSubscription = _mediator
            .CreateStream<IndexingProgressUpdatedEvent>()
            .Subscribe(OnProgressUpdated);
    }

    public bool CanCancel => IsVisible && !IsComplete && !WasCancelled;

    public string ProgressText => $"{ProcessedCount}/{TotalCount} ({PercentComplete}%)";

    public string ElapsedTimeDisplay => _stopwatch != null
        ? FormatElapsedTime(_stopwatch.Elapsed)
        : string.Empty;

    /// <summary>
    /// Shows the progress overlay for a new operation.
    /// </summary>
    public void Show(string title, int totalDocuments, CancellationTokenSource cts)
    {
        _cancellationTokenSource = cts;
        OperationTitle = title;
        TotalCount = totalDocuments;
        ProcessedCount = 0;
        PercentComplete = 0;
        IsComplete = false;
        WasCancelled = false;
        CurrentDocument = string.Empty;

        _stopwatch = Stopwatch.StartNew();
        IsVisible = true;

        _logger.LogDebug("Progress overlay shown: {Title}, {Total} documents", title, totalDocuments);
    }

    /// <summary>
    /// Updates progress from an event.
    /// </summary>
    private void OnProgressUpdated(IndexingProgressUpdatedEvent evt)
    {
        // Throttle UI updates
        var now = DateTime.UtcNow;
        if ((now - _lastUpdate).TotalMilliseconds < ThrottleMs && !evt.Progress.IsComplete)
        {
            return;
        }
        _lastUpdate = now;

        // Update on UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var info = evt.Progress;

            if (!string.IsNullOrEmpty(info.CurrentDocument))
            {
                CurrentDocument = Path.GetFileName(info.CurrentDocument);
            }

            ProcessedCount = info.ProcessedCount;
            TotalCount = info.TotalCount;
            PercentComplete = info.PercentComplete;
            WasCancelled = info.WasCancelled;

            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ElapsedTimeDisplay));
            OnPropertyChanged(nameof(CanCancel));

            if (info.IsComplete || info.WasCancelled)
            {
                CompleteOperation();
            }
        });
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            return;

        _logger.LogInformation("User cancelled indexing operation");
        _cancellationTokenSource.Cancel();
        WasCancelled = true;
        OnPropertyChanged(nameof(CanCancel));
    }

    private void CompleteOperation()
    {
        _stopwatch?.Stop();
        IsComplete = true;
        OnPropertyChanged(nameof(CanCancel));

        _logger.LogDebug(
            "Progress complete: {Processed}/{Total}, cancelled={Cancelled}",
            ProcessedCount, TotalCount, WasCancelled);

        // Auto-hide after delay
        _ = AutoHideAsync();
    }

    private async Task AutoHideAsync()
    {
        await Task.Delay(WasCancelled ? 2000 : 3000);

        if (IsComplete)
        {
            IsVisible = false;
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void Dismiss()
    {
        if (IsComplete)
        {
            IsVisible = false;
        }
    }

    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 60)
            return $"{elapsed.Seconds}s";
        if (elapsed.TotalMinutes < 60)
            return $"{elapsed.Minutes}m {elapsed.Seconds}s";
        return $"{elapsed.Hours}h {elapsed.Minutes}m";
    }

    public void Dispose()
    {
        _progressSubscription?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
```

### 2.2 IndexingProgressUpdatedEvent

```csharp
namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// Event for progress updates during indexing.
/// </summary>
public record IndexingProgressUpdatedEvent : INotification
{
    public required IndexingProgressInfo Progress { get; init; }
}
```

### 2.3 IndexingProgressView.axaml

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Modules.RAG.ViewModels"
             x:Class="Lexichord.Modules.RAG.Views.IndexingProgressView"
             x:DataType="vm:IndexingProgressViewModel"
             IsVisible="{Binding IsVisible}">

    <UserControl.Styles>
        <Style Selector="Border.progress-toast">
            <Setter Property="Background" Value="{DynamicResource SurfaceBackground}"/>
            <Setter Property="BorderBrush" Value="{DynamicResource BorderBrush}"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="CornerRadius" Value="8"/>
            <Setter Property="BoxShadow" Value="0 4 12 0 #40000000"/>
        </Style>

        <Style Selector="ProgressBar.indexing">
            <Setter Property="Foreground" Value="{DynamicResource PrimaryBrush}"/>
            <Setter Property="Background" Value="{DynamicResource ProgressBackground}"/>
            <Setter Property="Height" Value="6"/>
            <Setter Property="CornerRadius" Value="3"/>
        </Style>
    </UserControl.Styles>

    <!-- Toast positioned at bottom-right -->
    <Panel HorizontalAlignment="Right"
           VerticalAlignment="Bottom"
           Margin="0,0,24,24">

        <Border Classes="progress-toast"
                Width="360"
                Padding="16">
            <Grid RowDefinitions="Auto,Auto,Auto,Auto">
                <!-- Header -->
                <Grid Grid.Row="0" ColumnDefinitions="*,Auto">
                    <TextBlock Grid.Column="0"
                               Text="{Binding OperationTitle}"
                               FontWeight="SemiBold"
                               FontSize="14"/>

                    <Button Grid.Column="1"
                            Classes="icon-button"
                            Command="{Binding DismissCommand}"
                            IsVisible="{Binding IsComplete}"
                            ToolTip.Tip="Dismiss">
                        <PathIcon Data="{StaticResource CloseIcon}" Width="12" Height="12"/>
                    </Button>
                </Grid>

                <!-- Current File -->
                <TextBlock Grid.Row="1"
                           Text="{Binding CurrentDocument}"
                           Foreground="{DynamicResource SecondaryTextBrush}"
                           FontSize="13"
                           TextTrimming="CharacterEllipsis"
                           Margin="0,4,0,8"
                           IsVisible="{Binding CurrentDocument, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"/>

                <!-- Progress Bar -->
                <ProgressBar Grid.Row="2"
                             Classes="indexing"
                             Minimum="0"
                             Maximum="100"
                             Value="{Binding PercentComplete}"
                             IsIndeterminate="{Binding !TotalCount}"/>

                <!-- Footer -->
                <Grid Grid.Row="3" ColumnDefinitions="*,Auto,Auto" Margin="0,8,0,0">
                    <!-- Progress Text -->
                    <TextBlock Grid.Column="0"
                               Foreground="{DynamicResource SecondaryTextBrush}"
                               FontSize="12">
                        <TextBlock.Text>
                            <MultiBinding StringFormat="{}{0}/{1} ({2}%)">
                                <Binding Path="ProcessedCount"/>
                                <Binding Path="TotalCount"/>
                                <Binding Path="PercentComplete"/>
                            </MultiBinding>
                        </TextBlock.Text>
                    </TextBlock>

                    <!-- Elapsed Time -->
                    <TextBlock Grid.Column="1"
                               Text="{Binding ElapsedTimeDisplay}"
                               Foreground="{DynamicResource SecondaryTextBrush}"
                               FontSize="12"
                               Margin="0,0,12,0"/>

                    <!-- Cancel Button -->
                    <Button Grid.Column="2"
                            Content="Cancel"
                            Command="{Binding CancelCommand}"
                            IsVisible="{Binding CanCancel}"
                            Classes="text-button"
                            Foreground="{DynamicResource ErrorForeground}"/>

                    <!-- Status Icons -->
                    <StackPanel Grid.Column="2"
                                Orientation="Horizontal"
                                IsVisible="{Binding IsComplete}">
                        <!-- Success -->
                        <PathIcon IsVisible="{Binding !WasCancelled}"
                                  Data="{StaticResource CheckIcon}"
                                  Width="16" Height="16"
                                  Foreground="{DynamicResource SuccessForeground}"/>
                        <!-- Cancelled -->
                        <PathIcon IsVisible="{Binding WasCancelled}"
                                  Data="{StaticResource WarningIcon}"
                                  Width="16" Height="16"
                                  Foreground="{DynamicResource WarningForeground}"/>
                    </StackPanel>
                </Grid>
            </Grid>
        </Border>
    </Panel>
</UserControl>
```

### 2.4 Integration with Shell

```csharp
// In ShellView.axaml, add overlay layer
<Grid>
    <!-- Main content -->
    <ContentControl Content="{Binding Content}"/>

    <!-- Overlay layer for toasts -->
    <Panel Name="OverlayLayer" IsHitTestVisible="False">
        <views:IndexingProgressView DataContext="{Binding IndexingProgressViewModel}"/>
    </Panel>
</Grid>
```

### 2.5 Service Integration

```csharp
// Extend IndexManagementService to use progress ViewModel
public async Task<IndexOperationResult> ReindexAllWithProgressAsync(
    IndexingProgressViewModel progressVm,
    CancellationToken ct = default)
{
    var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    var documents = await _documentRepo.GetAllAsync();

    progressVm.Show("Re-indexing all documents...", documents.Count, cts);

    var progress = new Progress<IndexingProgressInfo>(info =>
    {
        _mediator.Publish(new IndexingProgressUpdatedEvent { Progress = info });
    });

    try
    {
        return await ReindexAllAsync(progress, cts.Token);
    }
    finally
    {
        cts.Dispose();
    }
}
```

---

## 3. Visual Design

### 3.1 Progress Toast Layout

```
┌──────────────────────────────────────────┐
│ Indexing documents...               [×]  │
│ Processing: chapter-03.md                │
│ ████████████░░░░░░░░                    │
│ 12/20 (60%)              1m 23s [Cancel] │
└──────────────────────────────────────────┘
```

### 3.2 Completion States

**Success:**
```
┌──────────────────────────────────────────┐
│ Indexing documents...               [×]  │
│ Complete                                 │
│ ████████████████████████████████████████ │
│ 20/20 (100%)             2m 15s      ✓  │
└──────────────────────────────────────────┘
```

**Cancelled:**
```
┌──────────────────────────────────────────┐
│ Indexing documents...               [×]  │
│ Cancelled                                │
│ ████████████████░░░░░░░░░░░░░░░░░░░░░░░ │
│ 12/20 (60%)              1m 23s      ⚠  │
└──────────────────────────────────────────┘
```

### 3.3 Position and Animation

| Property | Value |
| :------- | :---- |
| Position | Bottom-right, 24px margin |
| Width | 360px |
| Entrance | Slide up + fade in (200ms) |
| Exit | Fade out (150ms) |
| Auto-dismiss | 3s after completion, 2s after cancel |

---

## 4. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7c")]
public class IndexingProgressViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IndexingProgressViewModel _sut;

    public IndexingProgressViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new IndexingProgressViewModel(
            _mediatorMock.Object,
            NullLogger<IndexingProgressViewModel>.Instance);
    }

    [Fact]
    public void Show_SetsInitialState()
    {
        var cts = new CancellationTokenSource();

        _sut.Show("Test Operation", 10, cts);

        _sut.IsVisible.Should().BeTrue();
        _sut.OperationTitle.Should().Be("Test Operation");
        _sut.TotalCount.Should().Be(10);
        _sut.ProcessedCount.Should().Be(0);
        _sut.PercentComplete.Should().Be(0);
        _sut.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void ProgressText_FormatsCorrectly()
    {
        var cts = new CancellationTokenSource();
        _sut.Show("Test", 20, cts);

        _sut.ProcessedCount = 12;
        _sut.PercentComplete = 60;

        _sut.ProgressText.Should().Be("12/20 (60%)");
    }

    [Fact]
    public void Cancel_RequestsCancellation()
    {
        var cts = new CancellationTokenSource();
        _sut.Show("Test", 10, cts);

        _sut.CancelCommand.Execute(null);

        cts.Token.IsCancellationRequested.Should().BeTrue();
        _sut.WasCancelled.Should().BeTrue();
    }

    [Fact]
    public void CanCancel_FalseWhenComplete()
    {
        var cts = new CancellationTokenSource();
        _sut.Show("Test", 10, cts);
        _sut.IsComplete = true;

        _sut.CanCancel.Should().BeFalse();
    }

    [Fact]
    public void CanCancel_FalseWhenAlreadyCancelled()
    {
        var cts = new CancellationTokenSource();
        _sut.Show("Test", 10, cts);
        _sut.WasCancelled = true;

        _sut.CanCancel.Should().BeFalse();
    }

    [Fact]
    public void CanCancel_FalseWhenNotVisible()
    {
        _sut.IsVisible = false;

        _sut.CanCancel.Should().BeFalse();
    }

    [Theory]
    [InlineData(30, "30s")]
    [InlineData(90, "1m 30s")]
    [InlineData(3700, "1h 1m")]
    public void ElapsedTimeDisplay_FormatsCorrectly(int seconds, string expected)
    {
        var elapsed = TimeSpan.FromSeconds(seconds);
        var formatted = IndexingProgressViewModel.FormatElapsedTime(elapsed);

        formatted.Should().Be(expected);
    }
}
```

---

## 5. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Debug | "Progress overlay shown: {Title}, {Total} documents" | On show |
| Debug | "Progress complete: {Processed}/{Total}, cancelled={Cancelled}" | On complete |
| Information | "User cancelled indexing operation" | On cancel |

---

## 6. File Locations

| File | Path |
| :--- | :--- |
| ViewModel | `src/Lexichord.Modules.RAG/ViewModels/IndexingProgressViewModel.cs` |
| View | `src/Lexichord.Modules.RAG/Views/IndexingProgressView.axaml` |
| Event | `src/Lexichord.Modules.RAG/Events/IndexingProgressUpdatedEvent.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/ViewModels/IndexingProgressViewModelTests.cs` |

---

## 7. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Progress toast appears when indexing starts | [ ] |
| 2 | Current document name displays | [ ] |
| 3 | Progress bar reflects completion percentage | [ ] |
| 4 | Progress text shows processed/total (X%) | [ ] |
| 5 | Elapsed time displays and updates | [ ] |
| 6 | Cancel button stops operation | [ ] |
| 7 | Success icon shows on completion | [ ] |
| 8 | Warning icon shows on cancellation | [ ] |
| 9 | Toast auto-dismisses after completion | [ ] |
| 10 | Dismiss button hides toast when complete | [ ] |
| 11 | UI updates throttled to prevent lag | [ ] |
| 12 | All unit tests pass | [ ] |

---

## 8. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
