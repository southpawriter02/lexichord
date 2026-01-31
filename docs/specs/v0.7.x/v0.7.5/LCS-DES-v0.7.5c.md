# LCS-DES-075c: Design Specification — Accept/Reject UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `AGT-075c` | Sub-part of AGT-075 |
| **Feature Name** | `Accept/Reject UI` | Review interface with diff preview |
| **Target Version** | `v0.7.5c` | Third sub-part of v0.7.5 |
| **Module Scope** | `Lexichord.Host` | Host application UI |
| **Swimlane** | `Ensemble` | Part of Agents vertical |
| **License Tier** | `Writer Pro` | Minimum tier for access |
| **Feature Gate Key** | `FeatureFlags.Agents.TuningAgent` | Shared gate with parent feature |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-075-INDEX](./LCS-DES-075-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-075 S3.3](./LCS-SBD-075.md#33-v075c-acceptreject-ui) | |

---

## 2. Executive Summary

### 2.1 The Requirement

Users need an efficient interface to review, accept, reject, or modify AI-generated fix suggestions:

- **Quick Review:** Single-click actions for common decisions
- **Visual Diff:** Clear visualization of what changes
- **Keyboard Navigation:** Power users need fast keyboard-based workflow
- **Bulk Actions:** Process multiple high-confidence fixes at once
- **Undo Support:** Ability to revert accepted changes
- **Progress Tracking:** Clear indication of review progress

> **Goal:** Create a streamlined review interface that enables efficient processing of fix suggestions while maintaining user control.

### 2.2 The Proposed Solution

Implement a Tuning Panel with:

1. **Suggestion List:** Scrollable list of pending suggestions with filters
2. **Suggestion Cards:** Expandable cards showing original, suggested, diff, and explanation
3. **Action Buttons:** Accept, Reject, Modify, Skip with clear visual states
4. **Bulk Actions:** Accept All High Confidence with progress feedback
5. **Keyboard Shortcuts:** Full keyboard navigation (j/k/a/r/m/s)
6. **Undo Integration:** Stack-based undo for all accepted changes

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IStyleDeviationScanner` | v0.7.5a | Deviation detection |
| `StyleDeviation` | v0.7.5a | Deviation data |
| `IFixSuggestionGenerator` | v0.7.5b | Fix generation |
| `FixSuggestion` | v0.7.5b | Suggestion data |
| `TextDiff` | v0.7.5b | Diff visualization |
| `ILearningLoopService` | v0.7.5d | Feedback recording |
| `IEditorService` | v0.1.3a | Document editing |
| `IUndoRedoService` | v0.1.5a | Undo support |
| `ILicenseContext` | v0.0.4c | License checking |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `CommunityToolkit.Mvvm` | 8.x | ViewModel generation |
| `Avalonia` | 11.x | UI framework |

### 3.2 Licensing Behavior

- **Load Behavior:** UI Gate
  - Panel loads for all tiers
  - Core tier: Shows upgrade prompt
  - Writer Pro+: Full functionality
  - Learning Loop features require Teams tier

---

## 4. Data Contract (The API)

### 4.1 ViewModels

```csharp
namespace Lexichord.Host.ViewModels.Agents;

/// <summary>
/// ViewModel for the Tuning Panel that displays and manages fix suggestions.
/// </summary>
public partial class TuningPanelViewModel : ViewModelBase, IDisposable
{
    private readonly IStyleDeviationScanner _scanner;
    private readonly IFixSuggestionGenerator _suggestionGenerator;
    private readonly ILearningLoopService? _learningLoop;
    private readonly IEditorService _editorService;
    private readonly IUndoRedoService _undoService;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<TuningPanelViewModel> _logger;

    private CancellationTokenSource? _scanCts;
    private bool _disposed;

    #region Observable Properties

    /// <summary>
    /// Collection of suggestion cards to display.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SuggestionCardViewModel> _suggestions = new();

    /// <summary>
    /// Currently selected suggestion for expanded view.
    /// </summary>
    [ObservableProperty]
    private SuggestionCardViewModel? _selectedSuggestion;

    /// <summary>
    /// Whether a scan is currently in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanDocumentCommand))]
    private bool _isScanning;

    /// <summary>
    /// Whether fix generation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isGeneratingFixes;

    /// <summary>
    /// Whether bulk accept is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptAllHighConfidenceCommand))]
    private bool _isBulkProcessing;

    /// <summary>
    /// Total number of deviations found.
    /// </summary>
    [ObservableProperty]
    private int _totalDeviations;

    /// <summary>
    /// Number of suggestions reviewed.
    /// </summary>
    [ObservableProperty]
    private int _reviewedCount;

    /// <summary>
    /// Number of suggestions accepted.
    /// </summary>
    [ObservableProperty]
    private int _acceptedCount;

    /// <summary>
    /// Number of suggestions rejected.
    /// </summary>
    [ObservableProperty]
    private int _rejectedCount;

    /// <summary>
    /// Current filter selection.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredSuggestions))]
    private SuggestionFilter _currentFilter = SuggestionFilter.All;

    /// <summary>
    /// Status message for display.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready to scan";

    /// <summary>
    /// Progress percentage for operations.
    /// </summary>
    [ObservableProperty]
    private int _progressPercent;

    /// <summary>
    /// Whether the user has Writer Pro license.
    /// </summary>
    [ObservableProperty]
    private bool _hasWriterProLicense;

    /// <summary>
    /// Whether the user has Teams license (for Learning Loop).
    /// </summary>
    [ObservableProperty]
    private bool _hasTeamsLicense;

    /// <summary>
    /// Number of high confidence suggestions.
    /// </summary>
    public int HighConfidenceCount =>
        Suggestions.Count(s => s.IsHighConfidence && s.Status == SuggestionStatus.Pending);

    /// <summary>
    /// Number of remaining suggestions.
    /// </summary>
    public int RemainingCount =>
        Suggestions.Count(s => s.Status == SuggestionStatus.Pending);

    /// <summary>
    /// Filtered suggestions based on current filter.
    /// </summary>
    public IEnumerable<SuggestionCardViewModel> FilteredSuggestions =>
        CurrentFilter switch
        {
            SuggestionFilter.Pending => Suggestions.Where(s => s.Status == SuggestionStatus.Pending),
            SuggestionFilter.HighConfidence => Suggestions.Where(s => s.IsHighConfidence),
            SuggestionFilter.HighPriority => Suggestions.Where(s => s.Priority >= DeviationPriority.High),
            _ => Suggestions
        };

    #endregion

    #region Commands

    /// <summary>
    /// Scans the current document for style deviations.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanScanDocument))]
    private async Task ScanDocumentAsync()
    {
        if (!HasWriterProLicense)
        {
            await ShowUpgradePromptAsync(LicenseTier.WriterPro);
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        try
        {
            IsScanning = true;
            StatusMessage = "Scanning document for style deviations...";
            ProgressPercent = 0;
            Suggestions.Clear();

            var documentPath = await _editorService.GetActiveDocumentPathAsync(ct);
            if (string.IsNullOrEmpty(documentPath))
            {
                StatusMessage = "No document open";
                return;
            }

            // Scan for deviations
            var scanResult = await _scanner.ScanDocumentAsync(documentPath, ct);
            TotalDeviations = scanResult.TotalCount;
            ProgressPercent = 25;

            if (scanResult.TotalCount == 0)
            {
                StatusMessage = "No style deviations found!";
                return;
            }

            StatusMessage = $"Found {scanResult.TotalCount} deviations. Generating fixes...";
            IsGeneratingFixes = true;
            ProgressPercent = 50;

            // Generate fixes
            var autoFixable = scanResult.Deviations.Where(d => d.IsAutoFixable).ToList();
            var suggestions = await _suggestionGenerator.GenerateFixesAsync(autoFixable, ct: ct);

            ProgressPercent = 90;

            // Create view models
            for (var i = 0; i < suggestions.Count; i++)
            {
                var suggestion = suggestions[i];
                var deviation = autoFixable[i];

                Suggestions.Add(new SuggestionCardViewModel(deviation, suggestion));
            }

            // Select first suggestion
            if (Suggestions.Count > 0)
            {
                SelectedSuggestion = Suggestions[0];
                SelectedSuggestion.IsExpanded = true;
            }

            ProgressPercent = 100;
            StatusMessage = $"Ready to review {Suggestions.Count} suggestions";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed");
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            IsGeneratingFixes = false;
        }
    }

    private bool CanScanDocument() => !IsScanning;

    /// <summary>
    /// Accepts the specified suggestion and applies it to the document.
    /// </summary>
    [RelayCommand]
    private async Task AcceptSuggestionAsync(SuggestionCardViewModel? suggestion)
    {
        if (suggestion == null || suggestion.Status != SuggestionStatus.Pending)
            return;

        try
        {
            var documentPath = await _editorService.GetActiveDocumentPathAsync();

            // Create undo point
            await _undoService.BeginUndoGroupAsync("Accept Tuning Suggestion");

            // Apply the fix
            await _editorService.ReplaceTextAsync(
                documentPath,
                suggestion.Deviation.Location,
                suggestion.Suggestion.SuggestedText);

            await _undoService.EndUndoGroupAsync();

            // Update state
            suggestion.Status = SuggestionStatus.Accepted;
            suggestion.IsReviewed = true;
            AcceptedCount++;
            ReviewedCount++;

            // Record feedback (if Learning Loop available)
            if (_learningLoop != null && HasTeamsLicense)
            {
                await _learningLoop.RecordFeedbackAsync(new FixFeedback
                {
                    FeedbackId = Guid.NewGuid(),
                    SuggestionId = suggestion.Suggestion.SuggestionId,
                    DeviationId = suggestion.Deviation.DeviationId,
                    RuleId = suggestion.Deviation.RuleId,
                    Category = suggestion.Deviation.Category,
                    Decision = FeedbackDecision.Accepted,
                    OriginalText = suggestion.Deviation.OriginalText,
                    SuggestedText = suggestion.Suggestion.SuggestedText,
                    FinalText = suggestion.Suggestion.SuggestedText,
                    OriginalConfidence = suggestion.Suggestion.Confidence
                });
            }

            // Navigate to next
            NavigateNext();

            await _mediator.Publish(new SuggestionAcceptedEvent(suggestion.Deviation, suggestion.Suggestion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept suggestion");
            StatusMessage = $"Failed to accept: {ex.Message}";
        }
    }

    /// <summary>
    /// Rejects the specified suggestion.
    /// </summary>
    [RelayCommand]
    private async Task RejectSuggestionAsync(SuggestionCardViewModel? suggestion)
    {
        if (suggestion == null || suggestion.Status != SuggestionStatus.Pending)
            return;

        suggestion.Status = SuggestionStatus.Rejected;
        suggestion.IsReviewed = true;
        RejectedCount++;
        ReviewedCount++;

        // Record feedback
        if (_learningLoop != null && HasTeamsLicense)
        {
            await _learningLoop.RecordFeedbackAsync(new FixFeedback
            {
                FeedbackId = Guid.NewGuid(),
                SuggestionId = suggestion.Suggestion.SuggestionId,
                DeviationId = suggestion.Deviation.DeviationId,
                RuleId = suggestion.Deviation.RuleId,
                Category = suggestion.Deviation.Category,
                Decision = FeedbackDecision.Rejected,
                OriginalText = suggestion.Deviation.OriginalText,
                SuggestedText = suggestion.Suggestion.SuggestedText,
                OriginalConfidence = suggestion.Suggestion.Confidence
            });
        }

        NavigateNext();

        await _mediator.Publish(new SuggestionRejectedEvent(suggestion.Deviation, suggestion.Suggestion));
    }

    /// <summary>
    /// Opens the modify dialog for the suggestion.
    /// </summary>
    [RelayCommand]
    private async Task ModifySuggestionAsync(SuggestionCardViewModel? suggestion)
    {
        if (suggestion == null || suggestion.Status != SuggestionStatus.Pending)
            return;

        // Show modification dialog
        var modifyDialog = new ModifySuggestionDialog(suggestion.Suggestion.SuggestedText);
        var result = await modifyDialog.ShowDialogAsync();

        if (result.IsConfirmed)
        {
            // Apply modified text
            var documentPath = await _editorService.GetActiveDocumentPathAsync();

            await _undoService.BeginUndoGroupAsync("Accept Modified Suggestion");

            await _editorService.ReplaceTextAsync(
                documentPath,
                suggestion.Deviation.Location,
                result.ModifiedText);

            await _undoService.EndUndoGroupAsync();

            suggestion.Status = SuggestionStatus.Modified;
            suggestion.ModifiedText = result.ModifiedText;
            suggestion.IsReviewed = true;
            AcceptedCount++;
            ReviewedCount++;

            // Record feedback with modification
            if (_learningLoop != null && HasTeamsLicense)
            {
                await _learningLoop.RecordFeedbackAsync(new FixFeedback
                {
                    FeedbackId = Guid.NewGuid(),
                    SuggestionId = suggestion.Suggestion.SuggestionId,
                    DeviationId = suggestion.Deviation.DeviationId,
                    RuleId = suggestion.Deviation.RuleId,
                    Category = suggestion.Deviation.Category,
                    Decision = FeedbackDecision.Modified,
                    OriginalText = suggestion.Deviation.OriginalText,
                    SuggestedText = suggestion.Suggestion.SuggestedText,
                    FinalText = result.ModifiedText,
                    UserModification = result.ModifiedText,
                    OriginalConfidence = suggestion.Suggestion.Confidence
                });
            }

            NavigateNext();
        }
    }

    /// <summary>
    /// Skips the current suggestion without recording feedback.
    /// </summary>
    [RelayCommand]
    private void SkipSuggestion(SuggestionCardViewModel? suggestion)
    {
        if (suggestion == null || suggestion.Status != SuggestionStatus.Pending)
            return;

        suggestion.Status = SuggestionStatus.Skipped;
        suggestion.IsReviewed = true;
        ReviewedCount++;

        NavigateNext();
    }

    /// <summary>
    /// Accepts all high-confidence suggestions at once.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAcceptAllHighConfidence))]
    private async Task AcceptAllHighConfidenceAsync()
    {
        var highConfidence = Suggestions
            .Where(s => s.IsHighConfidence && s.Status == SuggestionStatus.Pending)
            .OrderBy(s => s.Deviation.Location.Start)
            .ToList();

        if (highConfidence.Count == 0)
        {
            StatusMessage = "No high-confidence suggestions to accept";
            return;
        }

        IsBulkProcessing = true;
        StatusMessage = $"Accepting {highConfidence.Count} high-confidence suggestions...";
        ProgressPercent = 0;

        try
        {
            var documentPath = await _editorService.GetActiveDocumentPathAsync();

            // Create single undo group for all changes
            await _undoService.BeginUndoGroupAsync("Accept All High Confidence");

            // Apply in reverse document order to preserve positions
            for (var i = highConfidence.Count - 1; i >= 0; i--)
            {
                var suggestion = highConfidence[i];

                await _editorService.ReplaceTextAsync(
                    documentPath,
                    suggestion.Deviation.Location,
                    suggestion.Suggestion.SuggestedText);

                suggestion.Status = SuggestionStatus.Accepted;
                suggestion.IsReviewed = true;
                AcceptedCount++;
                ReviewedCount++;

                ProgressPercent = (int)((highConfidence.Count - i) * 100.0 / highConfidence.Count);

                // Record feedback
                if (_learningLoop != null && HasTeamsLicense)
                {
                    await _learningLoop.RecordFeedbackAsync(new FixFeedback
                    {
                        FeedbackId = Guid.NewGuid(),
                        SuggestionId = suggestion.Suggestion.SuggestionId,
                        DeviationId = suggestion.Deviation.DeviationId,
                        RuleId = suggestion.Deviation.RuleId,
                        Category = suggestion.Deviation.Category,
                        Decision = FeedbackDecision.Accepted,
                        OriginalText = suggestion.Deviation.OriginalText,
                        SuggestedText = suggestion.Suggestion.SuggestedText,
                        FinalText = suggestion.Suggestion.SuggestedText,
                        OriginalConfidence = suggestion.Suggestion.Confidence
                    });
                }
            }

            await _undoService.EndUndoGroupAsync();

            StatusMessage = $"Applied {highConfidence.Count} fixes. Press Ctrl+Z to undo all.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk accept failed");
            StatusMessage = $"Bulk accept failed: {ex.Message}";
            await _undoService.CancelUndoGroupAsync();
        }
        finally
        {
            IsBulkProcessing = false;
            ProgressPercent = 100;
        }
    }

    private bool CanAcceptAllHighConfidence() => !IsBulkProcessing && HighConfidenceCount > 0;

    /// <summary>
    /// Regenerates the suggestion with user guidance.
    /// </summary>
    [RelayCommand]
    private async Task RegenerateSuggestionAsync(SuggestionCardViewModel? suggestion)
    {
        if (suggestion == null)
            return;

        var dialog = new RegenerationGuidanceDialog();
        var result = await dialog.ShowDialogAsync();

        if (!result.IsConfirmed || string.IsNullOrEmpty(result.Guidance))
            return;

        suggestion.IsRegenerating = true;
        StatusMessage = "Regenerating suggestion...";

        try
        {
            var newSuggestion = await _suggestionGenerator.RegenerateFixAsync(
                suggestion.Deviation,
                result.Guidance);

            suggestion.UpdateSuggestion(newSuggestion);
            StatusMessage = "Suggestion regenerated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Regeneration failed");
            StatusMessage = $"Regeneration failed: {ex.Message}";
        }
        finally
        {
            suggestion.IsRegenerating = false;
        }
    }

    /// <summary>
    /// Navigates to the next suggestion.
    /// </summary>
    [RelayCommand]
    private void NavigateNext()
    {
        if (SelectedSuggestion == null || Suggestions.Count == 0)
            return;

        var currentIndex = Suggestions.IndexOf(SelectedSuggestion);
        var nextIndex = (currentIndex + 1) % Suggestions.Count;

        // Find next pending suggestion
        for (var i = 0; i < Suggestions.Count; i++)
        {
            var checkIndex = (nextIndex + i) % Suggestions.Count;
            if (Suggestions[checkIndex].Status == SuggestionStatus.Pending)
            {
                SelectSuggestion(Suggestions[checkIndex]);
                return;
            }
        }

        // No pending suggestions left
        StatusMessage = "All suggestions reviewed!";
    }

    /// <summary>
    /// Navigates to the previous suggestion.
    /// </summary>
    [RelayCommand]
    private void NavigatePrevious()
    {
        if (SelectedSuggestion == null || Suggestions.Count == 0)
            return;

        var currentIndex = Suggestions.IndexOf(SelectedSuggestion);
        var prevIndex = currentIndex == 0 ? Suggestions.Count - 1 : currentIndex - 1;

        SelectSuggestion(Suggestions[prevIndex]);
    }

    #endregion

    #region Private Methods

    private void SelectSuggestion(SuggestionCardViewModel suggestion)
    {
        if (SelectedSuggestion != null)
            SelectedSuggestion.IsExpanded = false;

        SelectedSuggestion = suggestion;
        SelectedSuggestion.IsExpanded = true;
    }

    private async Task ShowUpgradePromptAsync(LicenseTier requiredTier)
    {
        // Show upgrade dialog
        await _mediator.Publish(new ShowUpgradePromptEvent(requiredTier, "Tuning Agent"));
    }

    #endregion
}

/// <summary>
/// ViewModel for an individual suggestion card.
/// </summary>
public partial class SuggestionCardViewModel : ViewModelBase
{
    private readonly StyleDeviation _deviation;
    private FixSuggestion _suggestion;

    public SuggestionCardViewModel(StyleDeviation deviation, FixSuggestion suggestion)
    {
        _deviation = deviation;
        _suggestion = suggestion;
    }

    public StyleDeviation Deviation => _deviation;
    public FixSuggestion Suggestion => _suggestion;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isReviewed;

    [ObservableProperty]
    private SuggestionStatus _status = SuggestionStatus.Pending;

    [ObservableProperty]
    private string? _modifiedText;

    [ObservableProperty]
    private bool _showAlternatives;

    [ObservableProperty]
    private AlternativeSuggestion? _selectedAlternative;

    [ObservableProperty]
    private bool _isRegenerating;

    // Convenience properties
    public string RuleName => Deviation.ViolatedRule.Name;
    public string RuleCategory => Deviation.Category;
    public DeviationPriority Priority => Deviation.Priority;
    public double Confidence => Suggestion.Confidence;
    public double QualityScore => Suggestion.QualityScore;
    public bool IsHighConfidence => Suggestion.IsHighConfidence;
    public TextDiff Diff => Suggestion.Diff;
    public string OriginalText => Deviation.OriginalText;
    public string SuggestedText => Suggestion.SuggestedText;
    public string Explanation => Suggestion.Explanation;
    public IReadOnlyList<AlternativeSuggestion>? Alternatives => Suggestion.Alternatives;
    public bool HasAlternatives => Alternatives?.Count > 0;

    public string ConfidenceDisplay => $"{Confidence * 100:F0}%";
    public string QualityDisplay => $"{QualityScore * 100:F0}%";

    public string PriorityDisplay => Priority switch
    {
        DeviationPriority.Critical => "Critical",
        DeviationPriority.High => "High",
        DeviationPriority.Normal => "Normal",
        DeviationPriority.Low => "Low",
        _ => "Unknown"
    };

    public void UpdateSuggestion(FixSuggestion newSuggestion)
    {
        _suggestion = newSuggestion;
        OnPropertyChanged(nameof(Suggestion));
        OnPropertyChanged(nameof(Confidence));
        OnPropertyChanged(nameof(QualityScore));
        OnPropertyChanged(nameof(IsHighConfidence));
        OnPropertyChanged(nameof(Diff));
        OnPropertyChanged(nameof(SuggestedText));
        OnPropertyChanged(nameof(Explanation));
        OnPropertyChanged(nameof(Alternatives));
    }
}

/// <summary>
/// Status of a suggestion in the review workflow.
/// </summary>
public enum SuggestionStatus
{
    Pending,
    Accepted,
    Rejected,
    Modified,
    Skipped
}

/// <summary>
/// Filter options for suggestion display.
/// </summary>
public enum SuggestionFilter
{
    All,
    Pending,
    HighConfidence,
    HighPriority
}
```

---

## 5. UI Layout Specifications

### 5.1 TuningPanelView Layout

```text
+===========================================================================+
|  Tuning Agent                                             [?] [Settings]  | <- Header
+===========================================================================+
|  [Scan Document]  [Accept All High Confidence (3)]  | Filter: [All    v]  | <- Toolbar
+---------------------------------------------------------------------------+
|                                                                           |
|  +---------------------------------------------------------------------+ |
|  | [v] Avoid Passive Voice                               Priority: High | | <- Card Header
|  |---------------------------------------------------------------------| |
|  |                                                                     | |
|  | Original:                                                           | |
|  | +------------------------------------------------------------------+| |
|  | | The report was submitted by the team yesterday.                  || |
|  | +------------------------------------------------------------------+| |
|  |                                                                     | |
|  | Suggested:                                                          | |
|  | +------------------------------------------------------------------+| |
|  | | The team submitted the report yesterday.                         || |
|  | +------------------------------------------------------------------+| |
|  |                                                                     | |
|  | Diff:                                                               | |
|  | +------------------------------------------------------------------+| |
|  | | [-The report was submitted by the team-]                         || |
|  | | [+The team submitted the report+] yesterday.                     || |
|  | +------------------------------------------------------------------+| |
|  |                                                                     | |
|  | Explanation:                                                        | |
|  | Converted passive voice to active voice by making "team" the       | |
|  | subject of the sentence. This improves clarity and directness.     | |
|  |                                                                     | |
|  | +-------------------+  +-------------------+                        | |
|  | | Confidence: 92%   |  | Quality: 95%      |                        | |
|  | | [=========-]      |  | [=========]       |                        | |
|  | +-------------------+  +-------------------+                        | |
|  |                                                                     | |
|  |---------------------------------------------------------------------| |
|  | [Accept]  [Reject]  [Modify...]  [Skip]    |  [v 2 alternatives]   | | <- Actions
|  +---------------------------------------------------------------------+ |
|                                                                           |
|  +---------------------------------------------------------------------+ |
|  | [>] Use Inclusive Terminology   "whitelist" -> "allowlist"  [Acc.] | | <- Collapsed
|  +---------------------------------------------------------------------+ |
|                                                                           |
|  +---------------------------------------------------------------------+ |
|  | [>] Remove Weasel Words         "very important" -> "critical"     | |
|  |                                                               [Acc.] | |
|  +---------------------------------------------------------------------+ |
|                                                                           |
+---------------------------------------------------------------------------+
|  Reviewed: 3/8  |  Accepted: 2  |  Rejected: 1  |  Remaining: 5          | <- Footer
+===========================================================================+
```

### 5.2 AXAML Structure

```xml
<!-- TuningPanelView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels.Agents"
             xmlns:controls="using:Lexichord.Host.Controls"
             x:Class="Lexichord.Host.Views.Agents.TuningPanelView"
             x:DataType="vm:TuningPanelViewModel">

    <UserControl.KeyBindings>
        <KeyBinding Gesture="j" Command="{Binding NavigateNextCommand}" />
        <KeyBinding Gesture="Down" Command="{Binding NavigateNextCommand}" />
        <KeyBinding Gesture="k" Command="{Binding NavigatePreviousCommand}" />
        <KeyBinding Gesture="Up" Command="{Binding NavigatePreviousCommand}" />
        <KeyBinding Gesture="a" Command="{Binding AcceptSuggestionCommand}"
                    CommandParameter="{Binding SelectedSuggestion}" />
        <KeyBinding Gesture="r" Command="{Binding RejectSuggestionCommand}"
                    CommandParameter="{Binding SelectedSuggestion}" />
        <KeyBinding Gesture="m" Command="{Binding ModifySuggestionCommand}"
                    CommandParameter="{Binding SelectedSuggestion}" />
        <KeyBinding Gesture="s" Command="{Binding SkipSuggestionCommand}"
                    CommandParameter="{Binding SelectedSuggestion}" />
        <KeyBinding Gesture="Ctrl+A" Command="{Binding AcceptAllHighConfidenceCommand}" />
    </UserControl.KeyBindings>

    <DockPanel>
        <!-- Header -->
        <Border DockPanel.Dock="Top" Classes="panel-header">
            <Grid ColumnDefinitions="*,Auto,Auto">
                <TextBlock Text="Tuning Agent" Classes="header-text" />
                <Button Grid.Column="1" ToolTip.Tip="Help" Classes="icon-button">
                    <PathIcon Data="{StaticResource HelpIcon}" />
                </Button>
                <Button Grid.Column="2" ToolTip.Tip="Settings" Classes="icon-button">
                    <PathIcon Data="{StaticResource SettingsIcon}" />
                </Button>
            </Grid>
        </Border>

        <!-- Toolbar -->
        <Border DockPanel.Dock="Top" Classes="toolbar">
            <Grid ColumnDefinitions="Auto,Auto,*,Auto">
                <Button Command="{Binding ScanDocumentCommand}"
                        IsEnabled="{Binding !IsScanning}"
                        Classes="primary-button">
                    <StackPanel Orientation="Horizontal" Spacing="4">
                        <PathIcon Data="{StaticResource ScanIcon}" />
                        <TextBlock Text="Scan Document" />
                    </StackPanel>
                </Button>

                <Button Grid.Column="1"
                        Command="{Binding AcceptAllHighConfidenceCommand}"
                        IsEnabled="{Binding HighConfidenceCount}"
                        Margin="8,0,0,0">
                    <StackPanel Orientation="Horizontal" Spacing="4">
                        <PathIcon Data="{StaticResource AcceptAllIcon}" />
                        <TextBlock>
                            <TextBlock.Text>
                                <MultiBinding StringFormat="Accept All ({0})">
                                    <Binding Path="HighConfidenceCount" />
                                </MultiBinding>
                            </TextBlock.Text>
                        </TextBlock>
                    </StackPanel>
                </Button>

                <StackPanel Grid.Column="3" Orientation="Horizontal" Spacing="8">
                    <TextBlock Text="Filter:" VerticalAlignment="Center" />
                    <ComboBox SelectedItem="{Binding CurrentFilter}"
                              ItemsSource="{Binding Source={x:Type vm:SuggestionFilter}}" />
                </StackPanel>
            </Grid>
        </Border>

        <!-- Progress Indicator -->
        <ProgressBar DockPanel.Dock="Top"
                     Value="{Binding ProgressPercent}"
                     IsVisible="{Binding IsScanning}"
                     Height="4" />

        <!-- Footer -->
        <Border DockPanel.Dock="Bottom" Classes="footer">
            <StackPanel Orientation="Horizontal" Spacing="16">
                <TextBlock>
                    <Run Text="Reviewed: " />
                    <Run Text="{Binding ReviewedCount}" />
                    <Run Text="/" />
                    <Run Text="{Binding TotalDeviations}" />
                </TextBlock>
                <TextBlock>
                    <Run Text="Accepted: " />
                    <Run Text="{Binding AcceptedCount}" Classes="accepted-count" />
                </TextBlock>
                <TextBlock>
                    <Run Text="Rejected: " />
                    <Run Text="{Binding RejectedCount}" Classes="rejected-count" />
                </TextBlock>
                <TextBlock>
                    <Run Text="Remaining: " />
                    <Run Text="{Binding RemainingCount}" />
                </TextBlock>
            </StackPanel>
        </Border>

        <!-- Suggestion List -->
        <ScrollViewer>
            <ItemsControl ItemsSource="{Binding FilteredSuggestions}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate DataType="vm:SuggestionCardViewModel">
                        <controls:SuggestionCard DataContext="{Binding}"
                                                  AcceptCommand="{Binding $parent[ItemsControl].DataContext.AcceptSuggestionCommand}"
                                                  RejectCommand="{Binding $parent[ItemsControl].DataContext.RejectSuggestionCommand}"
                                                  ModifyCommand="{Binding $parent[ItemsControl].DataContext.ModifySuggestionCommand}"
                                                  SkipCommand="{Binding $parent[ItemsControl].DataContext.SkipSuggestionCommand}" />
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- Empty State -->
        <Panel IsVisible="{Binding !Suggestions.Count}">
            <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Spacing="16">
                <PathIcon Data="{StaticResource TuningIcon}" Width="64" Height="64"
                          Opacity="0.5" />
                <TextBlock Text="No suggestions to review"
                           HorizontalAlignment="Center"
                           Classes="empty-state-text" />
                <TextBlock Text="Click 'Scan Document' to find style deviations"
                           HorizontalAlignment="Center"
                           Opacity="0.7" />
            </StackPanel>
        </Panel>
    </DockPanel>
</UserControl>
```

### 5.3 SuggestionCardView

```xml
<!-- SuggestionCard.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels.Agents"
             x:Class="Lexichord.Host.Controls.SuggestionCard"
             x:DataType="vm:SuggestionCardViewModel">

    <Border Classes="suggestion-card"
            Classes.expanded="{Binding IsExpanded}"
            Classes.accepted="{Binding Status, Converter={StaticResource EqualityConverter}, ConverterParameter=Accepted}"
            Classes.rejected="{Binding Status, Converter={StaticResource EqualityConverter}, ConverterParameter=Rejected}">

        <DockPanel>
            <!-- Header (always visible) -->
            <Border DockPanel.Dock="Top" Classes="card-header"
                    PointerPressed="OnHeaderPressed">
                <Grid ColumnDefinitions="Auto,*,Auto,Auto">
                    <PathIcon Grid.Column="0"
                              Data="{Binding IsExpanded, Converter={StaticResource ExpanderIconConverter}}"
                              Classes="expander-icon" />

                    <TextBlock Grid.Column="1" Text="{Binding RuleName}"
                               Classes="rule-name"
                               Margin="8,0,0,0" />

                    <Border Grid.Column="2" Classes="priority-badge"
                            Classes.critical="{Binding Priority, Converter={StaticResource EqualityConverter}, ConverterParameter=Critical}"
                            Classes.high="{Binding Priority, Converter={StaticResource EqualityConverter}, ConverterParameter=High}">
                        <TextBlock Text="{Binding PriorityDisplay}" />
                    </Border>

                    <!-- Quick Accept (collapsed view) -->
                    <Button Grid.Column="3"
                            Command="{Binding $parent[UserControl].AcceptCommand}"
                            CommandParameter="{Binding}"
                            IsVisible="{Binding !IsExpanded}"
                            Classes="quick-accept">
                        <PathIcon Data="{StaticResource CheckIcon}" />
                    </Button>
                </Grid>
            </Border>

            <!-- Collapsed Preview -->
            <TextBlock DockPanel.Dock="Top"
                       IsVisible="{Binding !IsExpanded}"
                       Margin="32,4,8,8"
                       Opacity="0.7">
                <Run Text="{Binding OriginalText, Converter={StaticResource TruncateConverter}}" />
                <Run Text=" -> " />
                <Run Text="{Binding SuggestedText, Converter={StaticResource TruncateConverter}}" />
            </TextBlock>

            <!-- Expanded Content -->
            <StackPanel IsVisible="{Binding IsExpanded}" Spacing="16" Margin="16">
                <!-- Original Text -->
                <StackPanel Spacing="4">
                    <TextBlock Text="Original:" Classes="section-label" />
                    <Border Classes="text-box original">
                        <TextBlock Text="{Binding OriginalText}" TextWrapping="Wrap" />
                    </Border>
                </StackPanel>

                <!-- Suggested Text -->
                <StackPanel Spacing="4">
                    <TextBlock Text="Suggested:" Classes="section-label" />
                    <Border Classes="text-box suggested">
                        <TextBlock Text="{Binding SuggestedText}" TextWrapping="Wrap" />
                    </Border>
                </StackPanel>

                <!-- Diff View -->
                <StackPanel Spacing="4">
                    <TextBlock Text="Diff:" Classes="section-label" />
                    <Border Classes="diff-box">
                        <controls:DiffView Diff="{Binding Diff}" />
                    </Border>
                </StackPanel>

                <!-- Explanation -->
                <StackPanel Spacing="4">
                    <TextBlock Text="Explanation:" Classes="section-label" />
                    <TextBlock Text="{Binding Explanation}"
                               TextWrapping="Wrap"
                               Opacity="0.85" />
                </StackPanel>

                <!-- Metrics -->
                <Grid ColumnDefinitions="*,*" RowDefinitions="Auto">
                    <StackPanel Grid.Column="0" Spacing="4">
                        <TextBlock Text="Confidence:" Classes="metric-label" />
                        <Grid ColumnDefinitions="*,Auto">
                            <ProgressBar Value="{Binding Confidence}"
                                         Minimum="0" Maximum="1"
                                         Classes="confidence-bar" />
                            <TextBlock Grid.Column="1" Text="{Binding ConfidenceDisplay}"
                                       Margin="8,0,0,0" />
                        </Grid>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Spacing="4" Margin="16,0,0,0">
                        <TextBlock Text="Quality:" Classes="metric-label" />
                        <Grid ColumnDefinitions="*,Auto">
                            <ProgressBar Value="{Binding QualityScore}"
                                         Minimum="0" Maximum="1"
                                         Classes="quality-bar" />
                            <TextBlock Grid.Column="1" Text="{Binding QualityDisplay}"
                                       Margin="8,0,0,0" />
                        </Grid>
                    </StackPanel>
                </Grid>

                <!-- Actions -->
                <Border Classes="actions-bar">
                    <Grid ColumnDefinitions="Auto,Auto,Auto,Auto,*,Auto">
                        <Button Grid.Column="0"
                                Command="{Binding $parent[UserControl].AcceptCommand}"
                                CommandParameter="{Binding}"
                                Classes="accept-button">
                            <StackPanel Orientation="Horizontal" Spacing="4">
                                <PathIcon Data="{StaticResource CheckIcon}" />
                                <TextBlock Text="Accept" />
                            </StackPanel>
                        </Button>

                        <Button Grid.Column="1"
                                Command="{Binding $parent[UserControl].RejectCommand}"
                                CommandParameter="{Binding}"
                                Classes="reject-button"
                                Margin="8,0,0,0">
                            <StackPanel Orientation="Horizontal" Spacing="4">
                                <PathIcon Data="{StaticResource XIcon}" />
                                <TextBlock Text="Reject" />
                            </StackPanel>
                        </Button>

                        <Button Grid.Column="2"
                                Command="{Binding $parent[UserControl].ModifyCommand}"
                                CommandParameter="{Binding}"
                                Margin="8,0,0,0">
                            <StackPanel Orientation="Horizontal" Spacing="4">
                                <PathIcon Data="{StaticResource EditIcon}" />
                                <TextBlock Text="Modify..." />
                            </StackPanel>
                        </Button>

                        <Button Grid.Column="3"
                                Command="{Binding $parent[UserControl].SkipCommand}"
                                CommandParameter="{Binding}"
                                Margin="8,0,0,0"
                                Classes="secondary">
                            <TextBlock Text="Skip" />
                        </Button>

                        <Button Grid.Column="5"
                                IsVisible="{Binding HasAlternatives}"
                                Command="{Binding ToggleAlternativesCommand}">
                            <StackPanel Orientation="Horizontal" Spacing="4">
                                <PathIcon Data="{Binding ShowAlternatives, Converter={StaticResource ExpanderIconConverter}}" />
                                <TextBlock>
                                    <Run Text="{Binding Alternatives.Count}" />
                                    <Run Text=" alternatives" />
                                </TextBlock>
                            </StackPanel>
                        </Button>
                    </Grid>
                </Border>

                <!-- Alternatives (expandable) -->
                <ItemsControl IsVisible="{Binding ShowAlternatives}"
                              ItemsSource="{Binding Alternatives}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border Classes="alternative-card" Margin="0,4,0,0">
                                <Grid RowDefinitions="Auto,Auto,Auto">
                                    <TextBlock Grid.Row="0" Text="{Binding Text}"
                                               TextWrapping="Wrap" />
                                    <TextBlock Grid.Row="1" Text="{Binding Explanation}"
                                               Opacity="0.7" Margin="0,4,0,0" />
                                    <StackPanel Grid.Row="2" Orientation="Horizontal"
                                                Margin="0,8,0,0">
                                        <TextBlock Text="Confidence: " />
                                        <TextBlock Text="{Binding Confidence, StringFormat={}{0:P0}}" />
                                        <Button Content="Use This"
                                                Command="{Binding $parent[ItemsControl].DataContext.UseAlternativeCommand}"
                                                CommandParameter="{Binding}"
                                                Margin="16,0,0,0"
                                                Classes="small" />
                                    </StackPanel>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </DockPanel>
    </Border>
</UserControl>
```

---

## 6. Keyboard Navigation

### 6.1 Keyboard Shortcuts

| Key | Action | Description |
| :--- | :--- | :--- |
| `j` or `Down` | Navigate Next | Move to next suggestion |
| `k` or `Up` | Navigate Previous | Move to previous suggestion |
| `a` | Accept | Accept current suggestion |
| `r` | Reject | Reject current suggestion |
| `m` | Modify | Open modify dialog |
| `s` | Skip | Skip without feedback |
| `Enter` or `Space` | Expand/Collapse | Toggle card expansion |
| `Ctrl+A` | Accept All High | Accept all high-confidence |
| `Ctrl+Z` | Undo | Undo last accepted fix |
| `?` | Help | Show keyboard shortcuts |

### 6.2 Focus Management

```csharp
/// <summary>
/// Handles keyboard navigation and focus.
/// </summary>
public partial class TuningPanelView : UserControl
{
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not TuningPanelViewModel vm)
            return;

        switch (e.Key)
        {
            case Key.J:
            case Key.Down:
                vm.NavigateNextCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.K:
            case Key.Up:
                vm.NavigatePreviousCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.A when e.KeyModifiers == KeyModifiers.None:
                vm.AcceptSuggestionCommand.Execute(vm.SelectedSuggestion);
                e.Handled = true;
                break;

            case Key.R:
                vm.RejectSuggestionCommand.Execute(vm.SelectedSuggestion);
                e.Handled = true;
                break;

            case Key.M:
                vm.ModifySuggestionCommand.Execute(vm.SelectedSuggestion);
                e.Handled = true;
                break;

            case Key.S:
                vm.SkipSuggestionCommand.Execute(vm.SelectedSuggestion);
                e.Handled = true;
                break;

            case Key.Enter:
            case Key.Space:
                if (vm.SelectedSuggestion != null)
                {
                    vm.SelectedSuggestion.IsExpanded = !vm.SelectedSuggestion.IsExpanded;
                    e.Handled = true;
                }
                break;

            case Key.A when e.KeyModifiers == KeyModifiers.Control:
                vm.AcceptAllHighConfidenceCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.OemQuestion:
                ShowKeyboardHelp();
                e.Handled = true;
                break;
        }
    }

    private void ShowKeyboardHelp()
    {
        var dialog = new KeyboardHelpDialog();
        dialog.ShowDialog(this);
    }
}
```

---

## 7. Observability & Logging

| Level | Message Template | Context |
| :--- | :--- | :--- |
| Debug | `"Scanning document: {Path}"` | Scan start |
| Info | `"Found {Count} deviations, generating fixes"` | Deviations found |
| Info | `"Suggestion accepted: {RuleId}"` | Acceptance |
| Info | `"Suggestion rejected: {RuleId}"` | Rejection |
| Info | `"Bulk accept: {Count} suggestions applied"` | Bulk action |
| Warning | `"No document open"` | No active document |
| Error | `"Failed to accept suggestion: {Error}"` | Accept failed |
| Error | `"Bulk accept failed: {Error}"` | Bulk failed |

---

## 8. Acceptance Criteria

### 8.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Document with violations | Clicking Scan | Suggestions displayed |
| 2 | Suggestion displayed | Clicking Accept | Document updated, status changed |
| 3 | Suggestion displayed | Clicking Reject | Status changed, document unchanged |
| 4 | Suggestion displayed | Clicking Modify | Dialog opens |
| 5 | Modify dialog open | Entering text and confirming | Modified text applied |
| 6 | Suggestion displayed | Clicking Skip | Status changed, no feedback |
| 7 | Accepted suggestion | Pressing Ctrl+Z | Change reverted |
| 8 | Multiple high-confidence | Clicking Accept All | All applied in reverse order |
| 9 | Suggestion selected | Pressing j/k | Next/previous selected |
| 10 | Suggestion selected | Pressing a/r | Accepted/rejected |

### 8.2 Accessibility Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 11 | Panel focused | Pressing Tab | All controls reachable |
| 12 | Screen reader active | Navigating | All elements announced |
| 13 | High contrast mode | Viewing | All text readable |
| 14 | Keyboard only | Full workflow | All actions possible |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `TuningPanelView.axaml` | [ ] |
| 2 | `TuningPanelView.axaml.cs` | [ ] |
| 3 | `TuningPanelViewModel` | [ ] |
| 4 | `SuggestionCard.axaml` | [ ] |
| 5 | `SuggestionCard.axaml.cs` | [ ] |
| 6 | `SuggestionCardViewModel` | [ ] |
| 7 | `DiffView` control | [ ] |
| 8 | `ModifySuggestionDialog` | [ ] |
| 9 | Keyboard shortcut handlers | [ ] |
| 10 | Undo integration | [ ] |
| 11 | Filter implementation | [ ] |
| 12 | Unit tests for ViewModels | [ ] |
| 13 | Style resources | [ ] |
| 14 | Accessibility attributes | [ ] |

---

## 10. Verification Commands

```bash
# Run UI unit tests
dotnet test --filter "Version=v0.7.5c" --logger "console;verbosity=detailed"

# Run ViewModel tests
dotnet test --filter "Category=Unit&FullyQualifiedName~TuningPanel"

# Manual verification:
# 1. Open document with style violations
# 2. Click Scan Document
# 3. Verify suggestions appear
# 4. Accept a suggestion (click and keyboard 'a')
# 5. Verify document updated
# 6. Press Ctrl+Z to undo
# 7. Verify document reverted
# 8. Reject a suggestion (click and keyboard 'r')
# 9. Navigate with j/k keys
# 10. Click Accept All High Confidence
# 11. Test filter dropdown
# 12. Verify accessibility with screen reader
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
