using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the Problems Panel sidebar view.
/// </summary>
/// <remarks>
/// LOGIC: The Problems Panel displays style violations grouped by severity.
/// It handles <see cref="LintingCompletedEvent"/> notifications and transforms
/// violations into displayable problem items.
///
/// Data Flow:
/// 1. LintingOrchestrator completes scan
/// 2. Publishes LintingCompletedEvent via IMediator
/// 3. Handle() receives event
/// 4. Checks ScopeMode == CurrentFile
/// 5. Verifies event.DocumentId matches active document
/// 6. Maps violations to ProblemItemViewModels
/// 7. Groups by severity
/// 8. Updates TotalCount, ErrorCount, WarningCount, InfoCount
/// 9. UI refreshes via PropertyChanged
///
/// Threading: All updates occur on the calling thread.
/// The orchestrator publishes from a background thread, so UI
/// dispatch may be needed in the View layer.
///
/// Version: v0.2.6b
/// </remarks>
public sealed partial class ProblemsPanelViewModel : ObservableObject,
    IProblemsPanelViewModel,
    INotificationHandler<LintingCompletedEvent>
{
    private readonly IViolationAggregator _violationAggregator;
    private readonly IEditorNavigationService _navigationService;
    private readonly ILogger<ProblemsPanelViewModel> _logger;
    private readonly ObservableCollection<IProblemGroup> _groups;
    private readonly Dictionary<ViolationSeverity, ProblemGroupViewModel> _groupsBySeverity;
    private bool _isDisposed;

    /// <summary>
    /// Gets the Readability HUD ViewModel for displaying readability metrics.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.3d - Injected via constructor for display in the Problems Panel header.
    /// License checked internally by the ReadabilityHudViewModel.
    /// </remarks>
    public IReadabilityHudViewModel ReadabilityHud { get; }

    #region Observable Properties

    /// <summary>
    /// Gets or sets whether the panel is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the currently selected problem item.
    /// </summary>
    [ObservableProperty]
    private IProblemItem? _selectedItem;

    /// <summary>
    /// Gets or sets the active document identifier.
    /// </summary>
    [ObservableProperty]
    private string? _activeDocumentId;

    #endregion

    #region Computed Properties

    /// <inheritdoc/>
    public ScopeModeType ScopeMode => ScopeModeType.CurrentFile;

    /// <inheritdoc/>
    public int TotalCount => ErrorCount + WarningCount + InfoCount + HintCount;

    /// <inheritdoc/>
    public int ErrorCount => GetGroupCount(ViolationSeverity.Error);

    /// <inheritdoc/>
    public int WarningCount => GetGroupCount(ViolationSeverity.Warning);

    /// <inheritdoc/>
    public int InfoCount => GetGroupCount(ViolationSeverity.Info);

    /// <inheritdoc/>
    public int HintCount => GetGroupCount(ViolationSeverity.Hint);

    /// <inheritdoc/>
    public ReadOnlyObservableCollection<IProblemGroup> Groups { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemsPanelViewModel"/> class.
    /// </summary>
    /// <param name="violationAggregator">The violation aggregator for accessing violations.</param>
    /// <param name="navigationService">The navigation service for navigating to violations.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null.</exception>
    /// <remarks>
    /// LOGIC: Initializes empty groups for each severity level.
    /// Groups are pre-created so they appear in consistent order in the UI.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    public ProblemsPanelViewModel(
        IViolationAggregator violationAggregator,
        IEditorNavigationService navigationService,
        IReadabilityHudViewModel readabilityHud,
        ILogger<ProblemsPanelViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(violationAggregator);
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(readabilityHud);
        ArgumentNullException.ThrowIfNull(logger);

        _violationAggregator = violationAggregator;
        _navigationService = navigationService;
        ReadabilityHud = readabilityHud;
        _logger = logger;

        // LOGIC: Pre-create groups in severity order (Error first, Hint last)
        _groupsBySeverity = new Dictionary<ViolationSeverity, ProblemGroupViewModel>
        {
            [ViolationSeverity.Error] = new ProblemGroupViewModel(ViolationSeverity.Error),
            [ViolationSeverity.Warning] = new ProblemGroupViewModel(ViolationSeverity.Warning),
            [ViolationSeverity.Info] = new ProblemGroupViewModel(ViolationSeverity.Info),
            [ViolationSeverity.Hint] = new ProblemGroupViewModel(ViolationSeverity.Hint)
        };

        _groups = new ObservableCollection<IProblemGroup>(_groupsBySeverity.Values);
        Groups = new ReadOnlyObservableCollection<IProblemGroup>(_groups);

        _logger.LogDebug("ProblemsPanelViewModel initialized with {GroupCount} severity groups", _groups.Count);
    }

    #endregion

    #region Event Handler

    /// <summary>
    /// Handles the LintingCompletedEvent notification.
    /// </summary>
    /// <param name="notification">The linting completed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// LOGIC: Event handling flow:
    /// 1. Set IsLoading = true
    /// 2. Clear existing items from all groups
    /// 3. Get aggregated violations for the document
    /// 4. Transform each to ProblemItemViewModel
    /// 5. Add to appropriate severity group
    /// 6. Raise PropertyChanged for all count properties
    /// 7. Set IsLoading = false
    ///
    /// Note: v0.2.6a only supports CurrentFile mode, so we always
    /// update with the event's document ID regardless of current view.
    ///
    /// Version: v0.2.6a
    /// </remarks>
    public Task Handle(LintingCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (_isDisposed)
        {
            _logger.LogTrace("Handle called after dispose, ignoring");
            return Task.CompletedTask;
        }

        ArgumentNullException.ThrowIfNull(notification);

        var result = notification.Result;

        _logger.LogDebug(
            "Handling LintingCompletedEvent for document {DocumentId} with {ViolationCount} violations",
            result.DocumentId, result.Violations.Count);

        try
        {
            IsLoading = true;
            ActiveDocumentId = result.DocumentId;

            // LOGIC: Clear all existing items
            ClearAllGroups();

            // LOGIC: Skip if cancelled or error
            if (result.WasCancelled || !result.IsSuccess)
            {
                _logger.LogDebug(
                    "Linting was cancelled or failed for {DocumentId}, showing empty panel",
                    result.DocumentId);
                RaiseCountChangedEvents();
                return Task.CompletedTask;
            }

            // LOGIC: Get aggregated violations from the aggregator
            var violations = _violationAggregator.GetViolations(result.DocumentId);

            _logger.LogDebug(
                "Retrieved {Count} aggregated violations for {DocumentId}",
                violations.Count, result.DocumentId);

            // LOGIC: Transform and group violations
            foreach (var violation in violations)
            {
                var item = new ProblemItemViewModel(violation);

                if (_groupsBySeverity.TryGetValue(violation.Severity, out var group))
                {
                    group.AddItem(item);
                }
                else
                {
                    _logger.LogWarning(
                        "Unknown severity {Severity} for violation {ViolationId}",
                        violation.Severity, violation.Id);
                }
            }

            RaiseCountChangedEvents();

            _logger.LogInformation(
                "Problems panel updated: {ErrorCount} errors, {WarningCount} warnings, {InfoCount} info, {HintCount} hints",
                ErrorCount, WarningCount, InfoCount, HintCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling LintingCompletedEvent for {DocumentId}", result.DocumentId);
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Clears all items from all severity groups.
    /// </summary>
    private void ClearAllGroups()
    {
        foreach (var group in _groupsBySeverity.Values)
        {
            group.Clear();
        }
    }

    /// <summary>
    /// Gets the count for a specific severity group.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>The count of items in that group.</returns>
    private int GetGroupCount(ViolationSeverity severity) =>
        _groupsBySeverity.TryGetValue(severity, out var group) ? group.Count : 0;

    /// <summary>
    /// Raises PropertyChanged for all count properties.
    /// </summary>
    private void RaiseCountChangedEvents()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(InfoCount));
        OnPropertyChanged(nameof(HintCount));
    }

    #endregion

    #region v0.2.6b Navigation Support

    /// <inheritdoc/>
    public event EventHandler<IProblemItem>? NavigateToViolationRequested;

    /// <summary>
    /// Navigates to the specified problem item in the editor.
    /// </summary>
    /// <param name="item">The problem item to navigate to.</param>
    /// <remarks>
    /// LOGIC: Invoked via double-click on a problem item.
    /// 1. Raises NavigateToViolationRequested event
    /// 2. Calls IEditorNavigationService.NavigateToViolationAsync
    /// 3. Logs result
    ///
    /// Version: v0.2.6b
    /// </remarks>
    [RelayCommand]
    private async Task NavigateToViolation(IProblemItem? item)
    {
        if (item is null)
        {
            _logger.LogDebug("NavigateToViolation called with null item, ignoring");
            return;
        }

        _logger.LogDebug(
            "Navigating to violation {ViolationId} at {Line}:{Column} in document {DocumentId}",
            item.Id, item.Line, item.Column, item.DocumentId);

        // LOGIC: Raise event for external subscribers
        NavigateToViolationRequested?.Invoke(this, item);

        // LOGIC: Perform navigation via service
        var result = await _navigationService.NavigateToViolationAsync(
            item.DocumentId,
            item.Line,
            item.Column,
            item.ViolatingText.Length);

        if (result.Success)
        {
            _logger.LogInformation(
                "Successfully navigated to violation {ViolationId} at {Line}:{Column}",
                item.Id, item.Line, item.Column);
        }
        else
        {
            _logger.LogWarning(
                "Failed to navigate to violation {ViolationId}: {Error}",
                item.Id, result.ErrorMessage);
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the ViewModel and clears all state.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears all groups and marks as disposed to prevent
    /// further event handling.
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        ClearAllGroups();
        SelectedItem = null;
        ActiveDocumentId = null;

        _logger.LogDebug("ProblemsPanelViewModel disposed");
    }

    #endregion
}
