// =============================================================================
// File: UnifiedIssuesPanelViewModel.cs
// Project: Lexichord.Modules.Agents
// Description: ViewModel for the Unified Issues Panel displaying all validation issues.
// =============================================================================
// LOGIC: Orchestrates the Unified Issues Panel experience, providing:
//   - Subscription to IUnifiedValidationService for real-time results
//   - Issue grouping by severity with expand/collapse behavior
//   - Filtering by category and severity
//   - Individual and bulk fix operations
//   - Navigation to issue locations in the editor
//
// v0.7.5g: Unified Issues Panel
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Undo;
using Lexichord.Abstractions.Contracts.Validation;
using Lexichord.Abstractions.Contracts.Validation.Events;
using MediatR;
using Microsoft.Extensions.Logging;

// Type aliases to resolve ambiguity with Knowledge.Validation.Integration namespace
using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// ViewModel for the Unified Issues Panel that displays validation issues
/// from all sources (Style Linter, Grammar Linter, CKVS Validation Engine).
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel orchestrates the Unified Issues Panel experience,
/// allowing users to:
/// </para>
/// <list type="bullet">
///   <item><description>View all validation issues in one place, grouped by severity</description></item>
///   <item><description>Filter issues by category (Style, Grammar, Knowledge, etc.)</description></item>
///   <item><description>Filter issues by severity (Error, Warning, Info, Hint)</description></item>
///   <item><description>Apply individual fixes via the "Apply Fix" button</description></item>
///   <item><description>Apply all auto-fixable issues via "Fix All"</description></item>
///   <item><description>Apply only error-level fixes via "Fix Errors Only"</description></item>
///   <item><description>Dismiss issues to suppress them from the active list</description></item>
///   <item><description>Navigate to issue locations in the editor</description></item>
/// </list>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
///   <item><description>Create instance via DI (transient)</description></item>
///   <item><description>Call <see cref="InitializeAsync"/> to subscribe to validation events</description></item>
///   <item><description>Panel auto-refreshes when <see cref="IUnifiedValidationService.ValidationCompleted"/> fires</description></item>
///   <item><description>User interacts with issues via commands</description></item>
///   <item><description>Handle <see cref="CloseRequested"/> event to close the view</description></item>
///   <item><description>Call <see cref="Dispose"/> for cleanup</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Spec Adaptations:</b>
/// <list type="bullet">
///   <item><description><c>IUnifiedFixWorkflow</c> (v0.7.5h) is nullable — when available,
///     bulk fix operations delegate to the orchestrator for conflict detection, category
///     ordering, and re-validation. When null, falls back to direct <c>IEditorService</c>
///     calls for fix application.</description></item>
///   <item><description><c>IEditorService</c> uses sync APIs: <c>BeginUndoGroup</c>,
///     <c>DeleteText</c>, <c>InsertText</c>, <c>EndUndoGroup</c>.</description></item>
///   <item><description><c>IUndoRedoService</c> is nullable — may not be registered.</description></item>
///   <item><description>Location uses <c>TextSpan.Start/Length</c> (character offsets),
///     not line/column numbers.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// Available at Core tier. Issue availability is controlled by
/// <see cref="IUnifiedValidationService"/> based on license tier.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// <para>
/// <b>Updated in:</b> v0.7.5h — Integrated <see cref="IUnifiedFixWorkflow"/> for
/// orchestrated fix application with conflict detection, category ordering, and
/// re-validation. Falls back to direct <see cref="IEditorService"/> calls when
/// the workflow is not available.
/// </para>
/// </remarks>
/// <seealso cref="IssuePresentationGroup"/>
/// <seealso cref="IssuePresentation"/>
/// <seealso cref="IUnifiedValidationService"/>
/// <seealso cref="UnifiedValidationResult"/>
/// <seealso cref="IUnifiedFixWorkflow"/>
public sealed partial class UnifiedIssuesPanelViewModel : DisposableViewModel
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IUnifiedValidationService _validationService;
    private readonly IEditorService _editorService;
    private readonly IUndoRedoService? _undoService;
    private readonly IUnifiedFixWorkflow? _fixWorkflow;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<UnifiedIssuesPanelViewModel> _logger;

    // ── Internal State ──────────────────────────────────────────────────
    private UnifiedValidationResult? _currentResult;
    private CancellationTokenSource? _refreshCts;
    private bool _isEventSubscribed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedIssuesPanelViewModel"/> class.
    /// </summary>
    /// <param name="validationService">The unified validation service for getting results.</param>
    /// <param name="editorService">The editor service for document navigation and editing.</param>
    /// <param name="undoService">The undo/redo service for labeled undo history (nullable).</param>
    /// <param name="fixWorkflow">
    /// The unified fix workflow for orchestrated fix application (nullable, v0.7.5h).
    /// When available, bulk fix operations delegate to this service for conflict detection,
    /// category ordering, and re-validation. When null, falls back to direct
    /// <see cref="IEditorService"/> calls.
    /// </param>
    /// <param name="licenseContext">The license context for feature gating.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required (non-nullable) dependency is null.
    /// </exception>
    public UnifiedIssuesPanelViewModel(
        IUnifiedValidationService validationService,
        IEditorService editorService,
        IUndoRedoService? undoService,
        IUnifiedFixWorkflow? fixWorkflow,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<UnifiedIssuesPanelViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(validationService);
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(licenseContext);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);

        _validationService = validationService;
        _editorService = editorService;
        _undoService = undoService;
        _fixWorkflow = fixWorkflow;
        _licenseContext = licenseContext;
        _mediator = mediator;
        _logger = logger;

        _logger.LogDebug(
            "UnifiedIssuesPanelViewModel created (FixWorkflow={FixWorkflowAvailable})",
            _fixWorkflow is not null);
    }

    // ── Events ──────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when the panel should be closed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The hosting view subscribes to this event to close the panel.
    /// Pattern matches <c>TuningPanelViewModel.CloseRequested</c>.
    /// </remarks>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Raised when a fix is requested for an issue.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Can be raised by UI controls that need to communicate
    /// fix requests back to the ViewModel (e.g., custom button controls).
    /// </remarks>
    public event EventHandler<FixRequestedEventArgs>? FixRequested;

    /// <summary>
    /// Raised when an issue is dismissed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Published for analytics and potential persistence of
    /// dismissal preferences.
    /// </remarks>
    public event EventHandler<IssueDismissedEventArgs>? IssueDismissed;

    // ── Observable Properties ───────────────────────────────────────────

    /// <summary>
    /// Collection of issue groups organized by severity.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Groups are ordered by severity: Error, Warning, Info, Hint.
    /// Empty groups are excluded from the collection.
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<IssuePresentationGroup> _issueGroups = new();

    /// <summary>
    /// The currently selected issue for expanded view.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FixIssueCommand))]
    [NotifyCanExecuteChangedFor(nameof(NavigateToIssueCommand))]
    [NotifyCanExecuteChangedFor(nameof(DismissIssueCommand))]
    private IssuePresentation? _selectedIssue;

    /// <summary>
    /// Whether a refresh operation is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isLoading;

    /// <summary>
    /// Whether a bulk fix operation is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FixAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(FixErrorsOnlyCommand))]
    private bool _isBulkProcessing;

    /// <summary>
    /// Status message for display in the panel.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Progress percentage for bulk operations (0-100).
    /// </summary>
    [ObservableProperty]
    private int _progressPercent;

    /// <summary>
    /// The selected category filter (null = all categories).
    /// </summary>
    [ObservableProperty]
    private IssueCategory? _selectedCategoryFilter;

    /// <summary>
    /// The selected severity filter (null = all severities).
    /// </summary>
    [ObservableProperty]
    private UnifiedSeverity? _selectedSeverityFilter;

    /// <summary>
    /// Error message to display when an operation fails.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    // ── Computed Properties ─────────────────────────────────────────────

    /// <summary>
    /// Gets the total number of issues across all groups.
    /// </summary>
    public int TotalIssueCount => _currentResult?.TotalIssueCount ?? 0;

    /// <summary>
    /// Gets the number of error-level issues.
    /// </summary>
    public int ErrorCount => _currentResult?.ErrorCount ?? 0;

    /// <summary>
    /// Gets the number of warning-level issues.
    /// </summary>
    public int WarningCount => _currentResult?.WarningCount ?? 0;

    /// <summary>
    /// Gets the number of info-level issues.
    /// </summary>
    public int InfoCount => _currentResult?.InfoCount ?? 0;

    /// <summary>
    /// Gets the number of hint-level issues.
    /// </summary>
    public int HintCount => _currentResult?.HintCount ?? 0;

    /// <summary>
    /// Gets the number of auto-fixable issues.
    /// </summary>
    public int AutoFixableCount => _currentResult?.AutoFixableCount ?? 0;

    /// <summary>
    /// Gets whether the document can be published (no errors).
    /// </summary>
    public bool CanPublish => _currentResult?.CanPublish ?? true;

    /// <summary>
    /// Gets whether any issues are present.
    /// </summary>
    public bool HasIssues => _currentResult?.HasIssues ?? false;

    /// <summary>
    /// Gets whether validation results are from cache.
    /// </summary>
    public bool IsCached => _currentResult?.IsCached ?? false;

    /// <summary>
    /// Gets the validation duration as a display string.
    /// </summary>
    public string DurationDisplay =>
        _currentResult is not null
            ? $"{_currentResult.Duration.TotalMilliseconds:F0}ms"
            : string.Empty;

    /// <summary>
    /// Gets the document path being validated.
    /// </summary>
    public string? DocumentPath => _currentResult?.DocumentPath;

    /// <summary>
    /// Gets available categories for filter dropdown.
    /// </summary>
    public IReadOnlyList<IssueCategory> AvailableCategories { get; } = new[]
    {
        IssueCategory.Style,
        IssueCategory.Grammar,
        IssueCategory.Knowledge,
        IssueCategory.Structure,
        IssueCategory.Custom
    };

    /// <summary>
    /// Gets available severities for filter dropdown.
    /// </summary>
    public IReadOnlyList<UnifiedSeverity> AvailableSeverities { get; } = new[]
    {
        UnifiedSeverity.Error,
        UnifiedSeverity.Warning,
        UnifiedSeverity.Info,
        UnifiedSeverity.Hint
    };

    // ── Initialization ──────────────────────────────────────────────────

    /// <summary>
    /// Initializes the panel by subscribing to validation events.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Subscribes to <see cref="IUnifiedValidationService.ValidationCompleted"/>
    /// to receive real-time updates when validation completes. Must be called
    /// after construction and before user interaction.
    /// </remarks>
    public void InitializeAsync()
    {
        _logger.LogDebug("Initializing UnifiedIssuesPanelViewModel");

        // LOGIC: Subscribe to validation completed events for real-time updates.
        if (!_isEventSubscribed)
        {
            _validationService.ValidationCompleted += OnValidationCompleted;
            _isEventSubscribed = true;
            _logger.LogDebug("Subscribed to ValidationCompleted event");
        }

        StatusMessage = "Ready — waiting for validation results";
    }

    // ── Commands ────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the panel with the latest validation results.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Triggers a new validation via <see cref="IUnifiedValidationService"/>
    /// and updates the panel with the results.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync(CancellationToken ct)
    {
        _refreshCts?.Cancel();
        _refreshCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var linkedCt = _refreshCts.Token;

        var documentPath = _editorService.CurrentDocumentPath;
        if (string.IsNullOrEmpty(documentPath))
        {
            _logger.LogWarning("No document open for validation");
            StatusMessage = "No document open";
            ErrorMessage = "Open a document to see validation issues";
            return;
        }

        var content = _editorService.GetDocumentText();
        if (content is null)
        {
            _logger.LogWarning("Could not read document content");
            StatusMessage = "Could not read document";
            ErrorMessage = "Failed to read document content";
            return;
        }

        IsLoading = true;
        StatusMessage = "Validating document...";
        ErrorMessage = null;

        _logger.LogDebug("Starting validation refresh for: {DocumentPath}", documentPath);

        try
        {
            var result = await _validationService.ValidateAsync(
                documentPath,
                content,
                UnifiedValidationOptions.Default,
                linkedCt);

            await RefreshWithResultAsync(result);

            _logger.LogInformation(
                "Validation refresh completed: {Count} issues in {Duration}ms",
                result.TotalIssueCount, result.Duration.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Validation refresh was cancelled");
            StatusMessage = "Validation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation refresh failed");
            StatusMessage = "Validation failed";
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRefresh() => !IsLoading;

    /// <summary>
    /// Applies the best fix for the selected issue.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Applies the fix using sync <c>IEditorService</c> APIs with
    /// undo group support. Updates the issue state and notifies computed properties.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanFixIssue))]
    private async Task FixIssueAsync(IssuePresentation? issue)
    {
        issue ??= SelectedIssue;
        if (issue is null)
            return;

        var fix = issue.Issue.BestFix;
        if (fix is null || !fix.CanAutoApply)
        {
            _logger.LogWarning(
                "Cannot fix issue {IssueId}: no auto-applicable fix",
                issue.Issue.IssueId);
            return;
        }

        _logger.LogDebug(
            "Applying fix for issue {IssueId}, rule {SourceId} (via {Method})",
            issue.Issue.IssueId, issue.Issue.SourceId,
            _fixWorkflow is not null ? "IUnifiedFixWorkflow" : "direct IEditorService");

        try
        {
            // LOGIC: Delegate to IUnifiedFixWorkflow when available (v0.7.5h).
            // This provides conflict detection, undo transaction recording, and
            // re-validation. Falls back to direct IEditorService calls when not available.
            if (_fixWorkflow is not null && _currentResult is not null)
            {
                var result = await _fixWorkflow.FixByIdAsync(
                    _currentResult.DocumentPath,
                    _currentResult,
                    [issue.Issue.IssueId]);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "Fix workflow returned failure for issue {IssueId}: {FailedCount} failed",
                        issue.Issue.IssueId, result.FailedCount);
                    StatusMessage = $"Fix failed for {issue.Issue.SourceId}";
                    return;
                }
            }
            else
            {
                // LOGIC: Fallback to direct editor calls when orchestrator is not available.
                ApplyFix(issue.Issue, fix);
            }

            // LOGIC: Mark the issue as fixed in the UI.
            issue.MarkAsFixed();

            // LOGIC: Raise fix requested event for external listeners.
            FixRequested?.Invoke(this, new FixRequestedEventArgs(issue.Issue, fix));

            StatusMessage = $"Fixed: {issue.Issue.SourceId}";

            _logger.LogInformation(
                "Fix applied for issue {IssueId}: {SourceId}",
                issue.Issue.IssueId, issue.Issue.SourceId);

            // LOGIC: Navigate to next actionable issue.
            await NavigateToNextActionableIssueAsync();

            NotifyComputedProperties();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply fix for issue {IssueId}", issue.Issue.IssueId);
            StatusMessage = $"Fix failed: {ex.Message}";
            ErrorMessage = ex.Message;
        }
    }

    private bool CanFixIssue(IssuePresentation? issue)
    {
        issue ??= SelectedIssue;
        return issue is not null
            && issue.IsActionable
            && issue.CanAutoApply;
    }

    /// <summary>
    /// Applies fixes for all auto-fixable issues.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <see cref="IUnifiedFixWorkflow"/> is available (v0.7.5h),
    /// delegates to <see cref="IUnifiedFixWorkflow.FixAllAsync"/> for orchestrated
    /// application with conflict detection, category ordering, and re-validation.
    /// </para>
    /// <para>
    /// Falls back to direct editor calls: applies fixes in reverse document order
    /// to preserve text offsets, all wrapped in a single editor undo group.
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanFixAll))]
    private async Task FixAllAsync()
    {
        // LOGIC: Delegate to IUnifiedFixWorkflow when available (v0.7.5h).
        if (_fixWorkflow is not null && _currentResult is not null)
        {
            _logger.LogDebug("Delegating Fix All to IUnifiedFixWorkflow");
            IsBulkProcessing = true;
            StatusMessage = "Applying all fixes via orchestrator...";

            try
            {
                var result = await _fixWorkflow.FixAllAsync(
                    _currentResult.DocumentPath,
                    _currentResult,
                    FixWorkflowOptions.Default);

                if (result.Success)
                {
                    StatusMessage = $"Applied {result.AppliedCount} fixes" +
                        (result.SkippedCount > 0 ? $" ({result.SkippedCount} skipped)" : "") +
                        ". Press Ctrl+Z to undo.";

                    // LOGIC: Mark all applied issues as fixed in the UI.
                    foreach (var resolvedIssue in result.ResolvedIssues)
                    {
                        var presentation = GetAllActionableIssues()
                            .FirstOrDefault(i => i.Issue.IssueId == resolvedIssue.IssueId);
                        presentation?.MarkAsFixed();
                    }

                    _logger.LogInformation(
                        "Fix All via orchestrator: {Applied} applied, {Skipped} skipped, {Failed} failed",
                        result.AppliedCount, result.SkippedCount, result.FailedCount);
                }
                else
                {
                    StatusMessage = $"Fix All failed: {result.FailedCount} failures";
                    _logger.LogWarning("Fix All via orchestrator failed: {Result}", result);
                }

                foreach (var group in IssueGroups)
                    group.NotifyCountChanged();
                NotifyComputedProperties();
            }
            catch (FixConflictException ex)
            {
                _logger.LogWarning(ex, "Fix All aborted due to conflicts: {Count} conflicts", ex.Conflicts.Count);
                StatusMessage = $"Fix All aborted: {ex.Conflicts.Count} conflicts detected";
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fix All via orchestrator failed");
                StatusMessage = $"Fix All failed: {ex.Message}";
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBulkProcessing = false;
                ProgressPercent = 100;
            }

            return;
        }

        // LOGIC: Fallback to direct editor calls when orchestrator is not available.
        var fixable = GetAllActionableIssues()
            .Where(i => i.CanAutoApply)
            .OrderByDescending(i => i.Issue.Location.Start) // LOGIC: Reverse order!
            .ToList();

        if (fixable.Count == 0)
        {
            _logger.LogDebug("No fixable issues to apply");
            StatusMessage = "No auto-fixable issues";
            return;
        }

        await ApplyBulkFixesAsync(fixable, "Fix All Issues");
    }

    private bool CanFixAll() => !IsBulkProcessing && AutoFixableCount > 0;

    /// <summary>
    /// Applies fixes for error-level issues only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <see cref="IUnifiedFixWorkflow"/> is available (v0.7.5h),
    /// delegates to <see cref="IUnifiedFixWorkflow.FixBySeverityAsync"/> filtered to
    /// <see cref="UnifiedSeverity.Error"/> only.
    /// </para>
    /// <para>
    /// Falls back to direct editor calls filtered to error severity.
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanFixErrorsOnly))]
    private async Task FixErrorsOnlyAsync()
    {
        // LOGIC: Delegate to IUnifiedFixWorkflow when available (v0.7.5h).
        if (_fixWorkflow is not null && _currentResult is not null)
        {
            _logger.LogDebug("Delegating Fix Errors Only to IUnifiedFixWorkflow");
            IsBulkProcessing = true;
            StatusMessage = "Fixing errors via orchestrator...";

            try
            {
                var result = await _fixWorkflow.FixBySeverityAsync(
                    _currentResult.DocumentPath,
                    _currentResult,
                    UnifiedSeverity.Error);

                if (result.Success)
                {
                    StatusMessage = $"Applied {result.AppliedCount} error fixes" +
                        (result.SkippedCount > 0 ? $" ({result.SkippedCount} skipped)" : "") +
                        ". Press Ctrl+Z to undo.";

                    foreach (var resolvedIssue in result.ResolvedIssues)
                    {
                        var presentation = GetAllActionableIssues()
                            .FirstOrDefault(i => i.Issue.IssueId == resolvedIssue.IssueId);
                        presentation?.MarkAsFixed();
                    }

                    _logger.LogInformation(
                        "Fix Errors Only via orchestrator: {Applied} applied, {Skipped} skipped",
                        result.AppliedCount, result.SkippedCount);
                }
                else
                {
                    StatusMessage = $"Fix Errors Only failed: {result.FailedCount} failures";
                }

                foreach (var group in IssueGroups)
                    group.NotifyCountChanged();
                NotifyComputedProperties();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fix Errors Only via orchestrator failed");
                StatusMessage = $"Fix Errors Only failed: {ex.Message}";
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBulkProcessing = false;
                ProgressPercent = 100;
            }

            return;
        }

        // LOGIC: Fallback to direct editor calls when orchestrator is not available.
        var fixable = GetAllActionableIssues()
            .Where(i => i.CanAutoApply && i.Issue.Severity == UnifiedSeverity.Error)
            .OrderByDescending(i => i.Issue.Location.Start)
            .ToList();

        if (fixable.Count == 0)
        {
            _logger.LogDebug("No fixable error-level issues");
            StatusMessage = "No auto-fixable errors";
            return;
        }

        await ApplyBulkFixesAsync(fixable, "Fix Errors Only");
    }

    private bool CanFixErrorsOnly() => !IsBulkProcessing && ErrorCount > 0;

    /// <summary>
    /// Dismisses (suppresses) the selected issue.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Marks the issue as suppressed without modifying the document.
    /// The issue remains visible but grayed out.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanDismissIssue))]
    private async Task DismissIssueAsync(IssuePresentation? issue)
    {
        issue ??= SelectedIssue;
        if (issue is null)
            return;

        _logger.LogDebug(
            "Dismissing issue {IssueId}: {SourceId}",
            issue.Issue.IssueId, issue.Issue.SourceId);

        issue.IsSuppressed = true;

        // LOGIC: Raise dismissed event for analytics and persistence.
        IssueDismissed?.Invoke(this, IssueDismissedEventArgs.Create(issue.Issue));

        StatusMessage = $"Dismissed: {issue.Issue.SourceId}";

        // LOGIC: Update group counts.
        foreach (var group in IssueGroups)
        {
            group.NotifyCountChanged();
        }

        // LOGIC: Navigate to next actionable issue.
        await NavigateToNextActionableIssueAsync();

        NotifyComputedProperties();
    }

    private bool CanDismissIssue(IssuePresentation? issue)
    {
        issue ??= SelectedIssue;
        return issue is not null && issue.IsActionable;
    }

    /// <summary>
    /// Navigates to the selected issue location in the editor.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Activates the document and sets the caret position to the
    /// issue's start location.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanNavigateToIssue))]
    private async Task NavigateToIssueAsync(IssuePresentation? issue)
    {
        issue ??= SelectedIssue;
        if (issue is null)
            return;

        _logger.LogDebug(
            "Navigating to issue {IssueId} at position {Position}",
            issue.Issue.IssueId, issue.Issue.Location.Start);

        try
        {
            // LOGIC: Ensure the document is active and visible.
            var document = _editorService.GetDocumentByPath(DocumentPath!);
            if (document is not null)
            {
                await _editorService.ActivateDocumentAsync(document);
            }

            // LOGIC: Set caret to issue location.
            _editorService.CaretOffset = issue.Issue.Location.Start;

            // LOGIC: Select the issue text for visibility.
            // Note: IEditorService doesn't have a SetSelection method, so we just position the caret.

            StatusMessage = $"Navigated to: {issue.Issue.SourceId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to issue {IssueId}", issue.Issue.IssueId);
            StatusMessage = $"Navigation failed: {ex.Message}";
        }
    }

    private bool CanNavigateToIssue(IssuePresentation? issue)
    {
        issue ??= SelectedIssue;
        return issue is not null && !string.IsNullOrEmpty(DocumentPath);
    }

    /// <summary>
    /// Clears all issues from the panel.
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        _logger.LogDebug("Clearing all issues from panel");

        IssueGroups.Clear();
        _currentResult = null;
        SelectedIssue = null;
        StatusMessage = "Cleared";
        ErrorMessage = null;

        NotifyComputedProperties();
    }

    /// <summary>
    /// Closes the Unified Issues Panel.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _logger.LogDebug("Unified Issues Panel close requested");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // ── Filter Change Handlers ──────────────────────────────────────────

    /// <summary>
    /// Called when the category filter changes.
    /// </summary>
    partial void OnSelectedCategoryFilterChanged(IssueCategory? value)
    {
        _logger.LogDebug("Category filter changed to: {Category}", value);
        ApplyFilters();
    }

    /// <summary>
    /// Called when the severity filter changes.
    /// </summary>
    partial void OnSelectedSeverityFilterChanged(UnifiedSeverity? value)
    {
        _logger.LogDebug("Severity filter changed to: {Severity}", value);
        ApplyFilters();
    }

    // ── Public Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the panel with the specified validation result.
    /// </summary>
    /// <param name="result">The validation result to display.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Rebuilds the issue groups and updates all computed properties.
    /// Called by <see cref="RefreshAsync"/> and <see cref="OnValidationCompleted"/>.
    /// </remarks>
    public Task RefreshWithResultAsync(UnifiedValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _logger.LogDebug(
            "Refreshing panel with result: {Count} issues, cached={IsCached}",
            result.TotalIssueCount, result.IsCached);

        _currentResult = result;
        ErrorMessage = null;

        RebuildIssueGroups(result);
        UpdateStatusMessage(result);
        NotifyComputedProperties();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows an error message in the panel.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional additional details.</param>
    public void ShowError(string message, string? details = null)
    {
        StatusMessage = $"Error: {message}";
        ErrorMessage = details ?? message;
        _logger.LogWarning("Panel error displayed: {Message}, Details: {Details}", message, details);
    }

    // ── Private Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the issue groups from a validation result.
    /// </summary>
    private void RebuildIssueGroups(UnifiedValidationResult result)
    {
        IssueGroups.Clear();

        var groupDefinitions = new[]
        {
            (Severity: UnifiedSeverity.Error, Label: "Errors"),
            (Severity: UnifiedSeverity.Warning, Label: "Warnings"),
            (Severity: UnifiedSeverity.Info, Label: "Info"),
            (Severity: UnifiedSeverity.Hint, Label: "Hints")
        };

        foreach (var (severity, label) in groupDefinitions)
        {
            if (!result.BySeverity.TryGetValue(severity, out var issues) || issues.Count == 0)
                continue;

            // LOGIC: Apply filters if set.
            var filteredIssues = issues.AsEnumerable();

            if (SelectedCategoryFilter.HasValue)
            {
                filteredIssues = filteredIssues.Where(i => i.Category == SelectedCategoryFilter.Value);
            }

            var issueList = filteredIssues.ToList();
            if (issueList.Count == 0)
                continue;

            var group = new IssuePresentationGroup(severity, label, issueList, _logger);
            IssueGroups.Add(group);
        }

        _logger.LogDebug(
            "Rebuilt {GroupCount} issue groups with {TotalCount} issues",
            IssueGroups.Count, IssueGroups.Sum(g => g.Count));
    }

    /// <summary>
    /// Applies current filters to rebuild the issue groups.
    /// </summary>
    private void ApplyFilters()
    {
        if (_currentResult is null)
            return;

        RebuildIssueGroups(_currentResult);
        NotifyComputedProperties();
    }

    /// <summary>
    /// Updates the status message based on validation result.
    /// </summary>
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

        if (result.IsCached)
            StatusMessage += " (cached)";
    }

    /// <summary>
    /// Applies a single fix to the document.
    /// </summary>
    private void ApplyFix(UnifiedIssue issue, UnifiedFix fix)
    {
        _logger.LogDebug(
            "Applying fix: Type={Type}, Location={Start}+{Length}",
            fix.Type, fix.Location.Start, fix.Location.Length);

        // LOGIC: Capture original text for undo before modification.
        var originalText = _editorService.GetDocumentText()
            ?.Substring(fix.Location.Start, fix.Location.Length) ?? "";

        _editorService.BeginUndoGroup($"Fix: {issue.SourceId}");
        try
        {
            // LOGIC: Delete old text if present.
            if (fix.Location.Length > 0)
            {
                _editorService.DeleteText(fix.Location.Start, fix.Location.Length);
            }

            // LOGIC: Insert new text if present.
            if (!string.IsNullOrEmpty(fix.NewText))
            {
                _editorService.InsertText(fix.Location.Start, fix.NewText);
            }
        }
        finally
        {
            _editorService.EndUndoGroup();
        }

        // LOGIC: Push to higher-level undo service if available.
        _undoService?.Push(new UnifiedIssuesUndoOperation(
            fix.Location.Start,
            originalText,
            fix.NewText ?? "",
            issue.SourceId ?? "Unknown",
            _editorService));
    }

    /// <summary>
    /// Applies multiple fixes in a single undo group.
    /// </summary>
    private Task ApplyBulkFixesAsync(IReadOnlyList<IssuePresentation> issues, string operationName)
    {
        IsBulkProcessing = true;
        StatusMessage = $"Applying {issues.Count} fixes...";
        ProgressPercent = 0;

        _logger.LogInformation(
            "Bulk fix operation '{Operation}': applying {Count} fixes",
            operationName, issues.Count);

        try
        {
            _editorService.BeginUndoGroup(operationName);

            try
            {
                for (var i = 0; i < issues.Count; i++)
                {
                    var issue = issues[i];
                    var fix = issue.Issue.BestFix;

                    if (fix is null || !fix.CanAutoApply)
                        continue;

                    // LOGIC: Apply fix (already in reverse document order).
                    if (fix.Location.Length > 0)
                    {
                        _editorService.DeleteText(fix.Location.Start, fix.Location.Length);
                    }

                    if (!string.IsNullOrEmpty(fix.NewText))
                    {
                        _editorService.InsertText(fix.Location.Start, fix.NewText);
                    }

                    issue.MarkAsFixed();

                    // LOGIC: Update progress.
                    ProgressPercent = (int)((i + 1) * 100.0 / issues.Count);

                    _logger.LogDebug(
                        "Bulk fixed issue {Index}/{Total}: {SourceId}",
                        i + 1, issues.Count, issue.Issue.SourceId);
                }
            }
            finally
            {
                _editorService.EndUndoGroup();
            }

            StatusMessage = $"Applied {issues.Count} fixes. Press Ctrl+Z to undo.";

            _logger.LogInformation(
                "Bulk fix operation '{Operation}' completed: {Count} fixes applied",
                operationName, issues.Count);

            // LOGIC: Update group counts.
            foreach (var group in IssueGroups)
            {
                group.NotifyCountChanged();
            }

            NotifyComputedProperties();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk fix operation '{Operation}' failed", operationName);
            StatusMessage = $"Bulk fix failed: {ex.Message}";
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBulkProcessing = false;
            ProgressPercent = 100;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all actionable issues from all groups.
    /// </summary>
    private IEnumerable<IssuePresentation> GetAllActionableIssues() =>
        IssueGroups.SelectMany(g => g.Items).Where(i => i.IsActionable);

    /// <summary>
    /// Navigates to the next actionable issue after the current selection.
    /// </summary>
    private Task NavigateToNextActionableIssueAsync()
    {
        var allIssues = IssueGroups.SelectMany(g => g.Items).ToList();
        if (allIssues.Count == 0)
        {
            SelectedIssue = null;
            return Task.CompletedTask;
        }

        var currentIndex = SelectedIssue is not null
            ? allIssues.IndexOf(SelectedIssue)
            : -1;

        // LOGIC: Find next actionable issue, wrapping around.
        for (var i = 1; i <= allIssues.Count; i++)
        {
            var checkIndex = (currentIndex + i) % allIssues.Count;
            if (allIssues[checkIndex].IsActionable)
            {
                SelectIssue(allIssues[checkIndex]);
                return Task.CompletedTask;
            }
        }

        // LOGIC: No actionable issues remaining.
        SelectedIssue = null;
        StatusMessage = "All issues addressed!";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Selects an issue, updating expansion state.
    /// </summary>
    private void SelectIssue(IssuePresentation issue)
    {
        // LOGIC: Collapse previously selected issue.
        if (SelectedIssue is not null)
        {
            SelectedIssue.IsExpanded = false;
            SelectedIssue.IsSelected = false;
        }

        SelectedIssue = issue;
        SelectedIssue.IsExpanded = true;
        SelectedIssue.IsSelected = true;
    }

    /// <summary>
    /// Notifies all computed properties that may have changed.
    /// </summary>
    private void NotifyComputedProperties()
    {
        OnPropertyChanged(nameof(TotalIssueCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(InfoCount));
        OnPropertyChanged(nameof(HintCount));
        OnPropertyChanged(nameof(AutoFixableCount));
        OnPropertyChanged(nameof(CanPublish));
        OnPropertyChanged(nameof(HasIssues));
        OnPropertyChanged(nameof(IsCached));
        OnPropertyChanged(nameof(DurationDisplay));
        OnPropertyChanged(nameof(DocumentPath));
    }

    /// <summary>
    /// Handles validation completed events from the service.
    /// </summary>
    private void OnValidationCompleted(object? sender, ValidationCompletedEventArgs e)
    {
        _logger.LogDebug(
            "ValidationCompleted event received for {DocumentPath}: {Count} issues",
            e.DocumentPath, e.Result.TotalIssueCount);

        // LOGIC: Only refresh if this is for our current document.
        if (!string.IsNullOrEmpty(DocumentPath) && e.DocumentPath != DocumentPath)
            return;

        // LOGIC: Update the panel on the UI thread.
        // Note: Assuming this runs on UI thread. If not, would need Dispatcher.
        _ = RefreshWithResultAsync(e.Result);
    }

    // ── Dispose ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        _logger.LogDebug("UnifiedIssuesPanelViewModel disposing");

        // LOGIC: Unsubscribe from validation events.
        if (_isEventSubscribed)
        {
            _validationService.ValidationCompleted -= OnValidationCompleted;
            _isEventSubscribed = false;
        }

        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;

        base.OnDisposed();
    }
}

// =============================================================================
// Internal: Undo operation for unified issues fix
// =============================================================================

/// <summary>
/// Undoable operation for fixes applied from the Unified Issues Panel.
/// </summary>
/// <remarks>
/// <b>LOGIC:</b> Provides atomic undo/redo for individual or bulk fix operations.
/// Similar to <see cref="TuningUndoableOperation"/> from v0.7.5c.
/// </remarks>
internal sealed class UnifiedIssuesUndoOperation : IUndoableOperation
{
    private readonly int _offset;
    private readonly string _originalText;
    private readonly string _newText;
    private readonly string _sourceId;
    private readonly IEditorService _editorService;

    public UnifiedIssuesUndoOperation(
        int offset,
        string originalText,
        string newText,
        string sourceId,
        IEditorService editorService)
    {
        _offset = offset;
        _originalText = originalText;
        _newText = newText;
        _sourceId = sourceId;
        _editorService = editorService;

        Id = Guid.NewGuid().ToString();
        DisplayName = $"Issues Fix ({_sourceId})";
        Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct = default)
    {
        _editorService.BeginUndoGroup(DisplayName);
        try
        {
            _editorService.DeleteText(_offset, _originalText.Length);
            _editorService.InsertText(_offset, _newText);
        }
        finally
        {
            _editorService.EndUndoGroup();
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UndoAsync(CancellationToken ct = default)
    {
        _editorService.BeginUndoGroup($"Undo: {DisplayName}");
        try
        {
            _editorService.DeleteText(_offset, _newText.Length);
            _editorService.InsertText(_offset, _originalText);
        }
        finally
        {
            _editorService.EndUndoGroup();
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RedoAsync(CancellationToken ct = default) => ExecuteAsync(ct);
}
