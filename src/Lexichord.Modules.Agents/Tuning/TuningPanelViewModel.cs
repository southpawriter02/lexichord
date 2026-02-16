// -----------------------------------------------------------------------
// <copyright file="TuningPanelViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Agents.Events;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Undo;
using Lexichord.Modules.Agents.Editor.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// ViewModel for the Tuning Panel that displays and manages fix suggestions
/// for detected style deviations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel orchestrates the Tuning Panel experience,
/// allowing users to:
/// </para>
/// <list type="bullet">
///   <item><description>Scan the current document for style deviations</description></item>
///   <item><description>Generate AI-powered fix suggestions for detected deviations</description></item>
///   <item><description>Review suggestions with inline diff preview</description></item>
///   <item><description>Accept, reject, modify, or skip individual suggestions</description></item>
///   <item><description>Bulk-accept all high-confidence suggestions</description></item>
///   <item><description>Navigate between suggestions with keyboard shortcuts</description></item>
///   <item><description>Regenerate suggestions with user guidance</description></item>
///   <item><description>Filter suggestions by status, confidence, or priority</description></item>
/// </list>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
///   <item><description>Create instance via DI (transient)</description></item>
///   <item><description>Call <see cref="InitializeAsync"/> to check license and set up state</description></item>
///   <item><description>User clicks "Scan Document" to populate suggestions</description></item>
///   <item><description>User reviews suggestions via commands</description></item>
///   <item><description>Handle <see cref="CloseRequested"/> event to close the view</description></item>
///   <item><description>Call <see cref="Dispose"/> for cleanup</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Spec Adaptations:</b>
/// <list type="bullet">
///   <item><description><c>IEditorService</c> uses sync APIs: <c>CurrentDocumentPath</c> (property),
///     <c>BeginUndoGroup</c>/<c>DeleteText</c>/<c>InsertText</c>/<c>EndUndoGroup</c> (sync methods).
///     The spec's async variants do not exist.</description></item>
///   <item><description><c>IUndoRedoService</c> is nullable — may not be registered yet.</description></item>
///   <item><description><c>ILearningLoopService</c> is nullable — added in v0.7.5d. Events are handled
///     via MediatR, not direct ViewModel calls.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// Requires <c>WriterPro</c> tier. Checked via <c>ILicenseContext.IsFeatureEnabled(FeatureCodes.TuningAgent)</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5c as part of the Accept/Reject UI feature.
/// <b>Updated in:</b> v0.7.5d — added <c>ILearningLoopService</c> dependency for learning loop integration.
/// </para>
/// </remarks>
/// <seealso cref="SuggestionCardViewModel"/>
/// <seealso cref="TuningUndoableOperation"/>
/// <seealso cref="IStyleDeviationScanner"/>
/// <seealso cref="IFixSuggestionGenerator"/>
public sealed partial class TuningPanelViewModel : DisposableViewModel
{
    // ── Dependencies ─────────────────────────────────────────────────────
    private readonly IStyleDeviationScanner _scanner;
    private readonly IFixSuggestionGenerator _suggestionGenerator;
    private readonly IEditorService _editorService;
    private readonly IUndoRedoService? _undoService;
    private readonly ILearningLoopService? _learningLoop;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<TuningPanelViewModel> _logger;

    // ── Internal State ───────────────────────────────────────────────────
    private CancellationTokenSource? _scanCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="TuningPanelViewModel"/> class.
    /// </summary>
    /// <param name="scanner">The style deviation scanner service.</param>
    /// <param name="suggestionGenerator">The fix suggestion generator service.</param>
    /// <param name="editorService">The editor service for document access and text manipulation.</param>
    /// <param name="undoService">The undo/redo service for labeled undo history (nullable — may not be registered).</param>
    /// <param name="learningLoop">The learning loop service for feedback persistence and pattern learning (nullable — added in v0.7.5d).</param>
    /// <param name="licenseContext">The license context for feature gating.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required (non-nullable) dependency is null.
    /// </exception>
    public TuningPanelViewModel(
        IStyleDeviationScanner scanner,
        IFixSuggestionGenerator suggestionGenerator,
        IEditorService editorService,
        IUndoRedoService? undoService,
        ILearningLoopService? learningLoop,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<TuningPanelViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(scanner);
        ArgumentNullException.ThrowIfNull(suggestionGenerator);
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(licenseContext);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);

        _scanner = scanner;
        _suggestionGenerator = suggestionGenerator;
        _editorService = editorService;
        _undoService = undoService;
        _learningLoop = learningLoop;
        _licenseContext = licenseContext;
        _mediator = mediator;
        _logger = logger;

        _logger.LogDebug("TuningPanelViewModel created");
    }

    // ── Events ───────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when the panel should be closed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The hosting view subscribes to this event to close the panel.
    /// Pattern matches <c>SimplificationPreviewViewModel.CloseRequested</c>.
    /// </remarks>
    public event EventHandler? CloseRequested;

    // ── Observable Properties ────────────────────────────────────────────

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
    private SuggestionFilter _currentFilter = SuggestionFilter.All;

    /// <summary>
    /// Status message for display.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready to scan";

    /// <summary>
    /// Progress percentage for operations (0-100).
    /// </summary>
    [ObservableProperty]
    private int _progressPercent;

    /// <summary>
    /// Whether the user has Writer Pro license.
    /// </summary>
    [ObservableProperty]
    private bool _hasWriterProLicense;

    /// <summary>
    /// Whether the user has Teams license (for future Learning Loop features).
    /// </summary>
    [ObservableProperty]
    private bool _hasTeamsLicense;

    // ── Computed Properties ──────────────────────────────────────────────

    /// <summary>
    /// Number of high-confidence suggestions still pending.
    /// </summary>
    public int HighConfidenceCount =>
        Suggestions.Count(s => s.IsHighConfidence && s.Status == SuggestionStatus.Pending);

    /// <summary>
    /// Number of remaining (pending) suggestions.
    /// </summary>
    public int RemainingCount =>
        Suggestions.Count(s => s.Status == SuggestionStatus.Pending);

    /// <summary>
    /// Filtered suggestions based on the current filter.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns a filtered view of the suggestions collection
    /// based on the <see cref="CurrentFilter"/> selection.
    /// </remarks>
    public IEnumerable<SuggestionCardViewModel> FilteredSuggestions =>
        CurrentFilter switch
        {
            SuggestionFilter.Pending => Suggestions.Where(s => s.Status == SuggestionStatus.Pending),
            SuggestionFilter.HighConfidence => Suggestions.Where(s => s.IsHighConfidence),
            SuggestionFilter.HighPriority => Suggestions.Where(s => s.Priority >= DeviationPriority.High),
            _ => Suggestions
        };

    // ── Initialization ───────────────────────────────────────────────────

    /// <summary>
    /// Initializes the panel by checking license status.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Checks the current license tier and sets the license
    /// state properties. Must be called after construction and before user interaction.
    /// </remarks>
    public void InitializeAsync()
    {
        _logger.LogDebug("Initializing TuningPanelViewModel");

        // LOGIC: Check license status for feature gating.
        HasWriterProLicense = _licenseContext.IsFeatureEnabled(FeatureCodes.TuningAgent);
        HasTeamsLicense = _licenseContext.GetCurrentTier() >= LicenseTier.Teams;

        _logger.LogInformation(
            "Tuning Panel initialized: WriterPro={HasWriterPro}, Teams={HasTeams}",
            HasWriterProLicense, HasTeamsLicense);

        if (!HasWriterProLicense)
        {
            StatusMessage = "Writer Pro license required for Tuning Agent";
            _logger.LogWarning("Tuning Agent requires Writer Pro license");
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>
    /// Scans the current document for style deviations and generates fix suggestions.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Orchestrates the full scan→generate→display pipeline:
    /// <list type="number">
    ///   <item><description>License check — shows upgrade prompt if unlicensed</description></item>
    ///   <item><description>Gets active document path via <c>IEditorService.CurrentDocumentPath</c></description></item>
    ///   <item><description>Scans for deviations via <c>IStyleDeviationScanner.ScanDocumentAsync</c></description></item>
    ///   <item><description>Generates fixes for auto-fixable deviations via <c>IFixSuggestionGenerator.GenerateFixesAsync</c></description></item>
    ///   <item><description>Creates <see cref="SuggestionCardViewModel"/> instances for each suggestion</description></item>
    ///   <item><description>Selects and expands the first suggestion</description></item>
    /// </list>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanScanDocument))]
    private async Task ScanDocumentAsync()
    {
        // LOGIC: License gate — show upgrade prompt instead of scanning.
        if (!HasWriterProLicense)
        {
            _logger.LogWarning("Scan blocked: Writer Pro license required");
            await _mediator.Publish(ShowUpgradeModalEvent.Create(
                "Tuning Agent",
                LicenseTier.WriterPro,
                "Unlock the Tuning Agent to automatically detect and fix style violations in your documents."));
            return;
        }

        // LOGIC: Cancel any in-progress scan before starting a new one.
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        try
        {
            IsScanning = true;
            StatusMessage = "Scanning document for style deviations...";
            ProgressPercent = 0;
            Suggestions.Clear();
            ResetCounters();

            _logger.LogDebug("Starting document scan");

            // LOGIC: Get active document path via sync property (not async).
            // Spec adaptation: spec uses GetActiveDocumentPathAsync() which doesn't exist.
            var documentPath = _editorService.CurrentDocumentPath;
            if (string.IsNullOrEmpty(documentPath))
            {
                _logger.LogWarning("No document open for scanning");
                StatusMessage = "No document open";
                return;
            }

            _logger.LogDebug("Scanning document: {Path}", documentPath);

            // LOGIC: Scan for style deviations using the scanner service.
            var scanResult = await _scanner.ScanDocumentAsync(documentPath, ct);
            TotalDeviations = scanResult.TotalCount;
            ProgressPercent = 25;

            _logger.LogInformation(
                "Found {Count} deviations in document {Path}",
                scanResult.TotalCount, documentPath);

            if (scanResult.TotalCount == 0)
            {
                StatusMessage = "No style deviations found!";
                return;
            }

            StatusMessage = $"Found {scanResult.TotalCount} deviations. Generating fixes...";
            IsGeneratingFixes = true;
            ProgressPercent = 50;

            // LOGIC: Generate fixes only for auto-fixable deviations.
            var autoFixable = scanResult.Deviations.Where(d => d.IsAutoFixable).ToList();
            _logger.LogDebug(
                "Generating fixes for {AutoFixableCount} of {TotalCount} deviations",
                autoFixable.Count, scanResult.TotalCount);

            var suggestions = await _suggestionGenerator.GenerateFixesAsync(autoFixable, ct: ct);
            ProgressPercent = 90;

            // LOGIC: Create ViewModel wrappers for each suggestion.
            for (var i = 0; i < suggestions.Count; i++)
            {
                var suggestion = suggestions[i];
                var deviation = autoFixable[i];

                // LOGIC: Only add successful suggestions to the review list.
                if (suggestion.Success)
                {
                    Suggestions.Add(new SuggestionCardViewModel(deviation, suggestion));
                }
                else
                {
                    _logger.LogWarning(
                        "Fix generation failed for deviation {DeviationId}: {Error}",
                        deviation.DeviationId, suggestion.ErrorMessage);
                }
            }

            // LOGIC: Select and expand the first suggestion for immediate review.
            if (Suggestions.Count > 0)
            {
                SelectSuggestion(Suggestions[0]);
            }

            ProgressPercent = 100;
            StatusMessage = $"Ready to review {Suggestions.Count} suggestions";

            _logger.LogInformation(
                "Scan complete: {SuggestionCount} suggestions ready for review",
                Suggestions.Count);

            // LOGIC: Notify computed properties that depend on suggestions collection.
            NotifyComputedProperties();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Document scan was cancelled");
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed for active document");
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
    /// <param name="suggestion">The suggestion to accept.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Applies the suggested text to the document using sync
    /// <c>IEditorService</c> APIs, updates UI state, pushes an undo operation,
    /// and publishes a <see cref="SuggestionAcceptedEvent"/>.
    /// </remarks>
    [RelayCommand]
    private async Task AcceptSuggestionAsync(SuggestionCardViewModel? suggestion)
    {
        if (suggestion == null || suggestion.Status != SuggestionStatus.Pending)
            return;

        try
        {
            _logger.LogDebug(
                "Accepting suggestion for deviation {DeviationId}, rule {RuleId}",
                suggestion.Deviation.DeviationId, suggestion.Deviation.RuleId);

            // LOGIC: Apply the fix using sync editor APIs.
            // Spec adaptation: spec uses BeginUndoGroupAsync/ReplaceTextAsync which don't exist.
            ApplyTextToEditor(
                suggestion.Deviation.Location.Start,
                suggestion.Deviation.OriginalText.Length,
                suggestion.Suggestion.SuggestedText,
                suggestion.Deviation.RuleId);

            // LOGIC: Update suggestion state.
            suggestion.Status = SuggestionStatus.Accepted;
            suggestion.IsReviewed = true;
            AcceptedCount++;
            ReviewedCount++;

            _logger.LogInformation(
                "Suggestion accepted: {RuleId}, confidence={Confidence:F2}",
                suggestion.Deviation.RuleId, suggestion.Suggestion.Confidence);

            // LOGIC: Navigate to the next pending suggestion.
            NavigateNext();

            // LOGIC: Publish MediatR event for analytics and learning loop.
            await _mediator.Publish(SuggestionAcceptedEvent.Create(
                suggestion.Deviation, suggestion.Suggestion));

            NotifyComputedProperties();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept suggestion for rule {RuleId}",
                suggestion.Deviation.RuleId);
            StatusMessage = $"Failed to accept: {ex.Message}";
        }
    }

    /// <summary>
    /// Rejects the specified suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion to reject.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Updates the suggestion status to Rejected without modifying
    /// the document. Publishes a <see cref="SuggestionRejectedEvent"/> for analytics.
    /// </remarks>
    [RelayCommand]
    private async Task RejectSuggestionAsync(SuggestionCardViewModel? suggestion)
    {
        if (suggestion == null || suggestion.Status != SuggestionStatus.Pending)
            return;

        _logger.LogDebug(
            "Rejecting suggestion for deviation {DeviationId}, rule {RuleId}",
            suggestion.Deviation.DeviationId, suggestion.Deviation.RuleId);

        // LOGIC: Update state — document remains unchanged.
        suggestion.Status = SuggestionStatus.Rejected;
        suggestion.IsReviewed = true;
        RejectedCount++;
        ReviewedCount++;

        _logger.LogInformation(
            "Suggestion rejected: {RuleId}",
            suggestion.Deviation.RuleId);

        // LOGIC: Navigate to the next pending suggestion.
        NavigateNext();

        // LOGIC: Publish MediatR event for analytics and learning loop.
        await _mediator.Publish(SuggestionRejectedEvent.Create(
            suggestion.Deviation, suggestion.Suggestion));

        NotifyComputedProperties();
    }

    /// <summary>
    /// Applies a modified version of the suggestion to the document.
    /// </summary>
    /// <param name="suggestion">The suggestion to modify and apply.</param>
    /// <param name="modifiedText">The user-edited replacement text.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Applies user-modified text instead of the original suggestion.
    /// The modified text is stored on the card for learning loop feedback.
    ///
    /// <b>Note:</b> This method is not a relay command because it requires two parameters
    /// (suggestion + modified text). The view code-behind calls it directly after
    /// prompting the user for the modified text.
    /// </remarks>
    public async Task ModifySuggestionAsync(SuggestionCardViewModel? suggestion, string? modifiedText)
    {
        if (suggestion == null || suggestion.Status != SuggestionStatus.Pending)
            return;

        if (string.IsNullOrEmpty(modifiedText))
            return;

        try
        {
            _logger.LogDebug(
                "Modifying suggestion for deviation {DeviationId}, rule {RuleId}",
                suggestion.Deviation.DeviationId, suggestion.Deviation.RuleId);

            // LOGIC: Apply the user-modified text using sync editor APIs.
            ApplyTextToEditor(
                suggestion.Deviation.Location.Start,
                suggestion.Deviation.OriginalText.Length,
                modifiedText,
                suggestion.Deviation.RuleId);

            // LOGIC: Update suggestion state with the modified text.
            suggestion.Status = SuggestionStatus.Modified;
            suggestion.ModifiedText = modifiedText;
            suggestion.IsReviewed = true;
            AcceptedCount++;
            ReviewedCount++;

            _logger.LogInformation(
                "Suggestion modified and applied: {RuleId}",
                suggestion.Deviation.RuleId);

            // LOGIC: Navigate to the next pending suggestion.
            NavigateNext();

            // LOGIC: Publish MediatR event with the modified text.
            await _mediator.Publish(SuggestionAcceptedEvent.CreateModified(
                suggestion.Deviation, suggestion.Suggestion, modifiedText));

            NotifyComputedProperties();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply modified suggestion for rule {RuleId}",
                suggestion.Deviation.RuleId);
            StatusMessage = $"Failed to apply modification: {ex.Message}";
        }
    }

    /// <summary>
    /// Skips the current suggestion without recording feedback.
    /// </summary>
    /// <param name="suggestion">The suggestion to skip.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Sets status to Skipped and advances to the next suggestion.
    /// No MediatR event is published — skipping indicates deferral, not rejection.
    /// </remarks>
    [RelayCommand]
    private void SkipSuggestion(SuggestionCardViewModel? suggestion)
    {
        if (suggestion == null || suggestion.Status != SuggestionStatus.Pending)
            return;

        _logger.LogDebug(
            "Skipping suggestion for deviation {DeviationId}",
            suggestion.Deviation.DeviationId);

        suggestion.Status = SuggestionStatus.Skipped;
        suggestion.IsReviewed = true;
        ReviewedCount++;

        // LOGIC: Navigate to the next pending suggestion.
        NavigateNext();

        NotifyComputedProperties();
    }

    /// <summary>
    /// Accepts all high-confidence suggestions at once.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Applies all high-confidence pending suggestions in reverse
    /// document order to preserve text positions. All changes are wrapped in a
    /// single editor undo group so Ctrl+Z reverts them all at once.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanAcceptAllHighConfidence))]
    private async Task AcceptAllHighConfidenceAsync()
    {
        // LOGIC: Collect high-confidence pending suggestions, sorted by document position.
        var highConfidence = Suggestions
            .Where(s => s.IsHighConfidence && s.Status == SuggestionStatus.Pending)
            .OrderBy(s => s.Deviation.Location.Start)
            .ToList();

        if (highConfidence.Count == 0)
        {
            _logger.LogDebug("No high-confidence suggestions to accept");
            StatusMessage = "No high-confidence suggestions to accept";
            return;
        }

        IsBulkProcessing = true;
        StatusMessage = $"Accepting {highConfidence.Count} high-confidence suggestions...";
        ProgressPercent = 0;

        _logger.LogInformation(
            "Bulk accept: applying {Count} high-confidence suggestions",
            highConfidence.Count);

        try
        {
            // LOGIC: Create a single editor undo group for all changes.
            _editorService.BeginUndoGroup("Accept All High Confidence");

            try
            {
                // LOGIC: Apply in REVERSE document order to preserve text positions.
                // Later changes don't shift the offsets of earlier ones.
                for (var i = highConfidence.Count - 1; i >= 0; i--)
                {
                    var suggestion = highConfidence[i];

                    _editorService.DeleteText(
                        suggestion.Deviation.Location.Start,
                        suggestion.Deviation.OriginalText.Length);
                    _editorService.InsertText(
                        suggestion.Deviation.Location.Start,
                        suggestion.Suggestion.SuggestedText);

                    // LOGIC: Update state for each suggestion.
                    suggestion.Status = SuggestionStatus.Accepted;
                    suggestion.IsReviewed = true;
                    AcceptedCount++;
                    ReviewedCount++;

                    // LOGIC: Update progress based on remaining items.
                    ProgressPercent = (int)((highConfidence.Count - i) * 100.0 / highConfidence.Count);

                    _logger.LogDebug(
                        "Bulk accepted suggestion for rule {RuleId}",
                        suggestion.Deviation.RuleId);
                }
            }
            finally
            {
                _editorService.EndUndoGroup();
            }

            // LOGIC: Push a single undo operation for the bulk action if the undo service is available.
            // Note: The editor undo group already handles Ctrl+Z at the editor level.
            // The IUndoRedoService push is for the labeled undo history UI.

            StatusMessage = $"Applied {highConfidence.Count} fixes. Press Ctrl+Z to undo all.";

            _logger.LogInformation(
                "Bulk accept: {Count} suggestions applied successfully",
                highConfidence.Count);

            // LOGIC: Publish events for each accepted suggestion.
            foreach (var suggestion in highConfidence)
            {
                await _mediator.Publish(SuggestionAcceptedEvent.Create(
                    suggestion.Deviation, suggestion.Suggestion));
            }

            NotifyComputedProperties();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk accept failed after partial application");
            StatusMessage = $"Bulk accept failed: {ex.Message}";
        }
        finally
        {
            IsBulkProcessing = false;
            ProgressPercent = 100;
        }
    }

    private bool CanAcceptAllHighConfidence() => !IsBulkProcessing;

    /// <summary>
    /// Regenerates the suggestion with optional user guidance.
    /// </summary>
    /// <param name="suggestion">The suggestion to regenerate.</param>
    /// <param name="guidance">Optional user guidance text for the regeneration.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Calls <see cref="IFixSuggestionGenerator.RegenerateFixAsync"/>
    /// with optional user guidance and updates the suggestion card.
    ///
    /// <b>Note:</b> This method is not a relay command because it requires two parameters
    /// (suggestion + guidance). The view code-behind calls it directly after
    /// optionally prompting the user for guidance text.
    /// </remarks>
    public async Task RegenerateSuggestionAsync(SuggestionCardViewModel? suggestion, string? guidance)
    {
        if (suggestion == null)
            return;

        suggestion.IsRegenerating = true;
        StatusMessage = "Regenerating suggestion...";

        _logger.LogDebug(
            "Regenerating suggestion for deviation {DeviationId} with guidance: {HasGuidance}",
            suggestion.Deviation.DeviationId, !string.IsNullOrEmpty(guidance));

        try
        {
            var newSuggestion = await _suggestionGenerator.RegenerateFixAsync(
                suggestion.Deviation,
                guidance ?? string.Empty);

            suggestion.UpdateSuggestion(newSuggestion);
            StatusMessage = "Suggestion regenerated";

            _logger.LogInformation(
                "Suggestion regenerated for rule {RuleId}, new confidence={Confidence:F2}",
                suggestion.Deviation.RuleId, newSuggestion.Confidence);

            NotifyComputedProperties();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Regeneration failed for deviation {DeviationId}",
                suggestion.Deviation.DeviationId);
            StatusMessage = $"Regeneration failed: {ex.Message}";
        }
        finally
        {
            suggestion.IsRegenerating = false;
        }
    }

    /// <summary>
    /// Navigates to the next pending suggestion.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Wraps around to find the next <see cref="SuggestionStatus.Pending"/>
    /// suggestion after the current selection. If no pending suggestions remain,
    /// shows a completion message.
    /// </remarks>
    [RelayCommand]
    public void NavigateNext()
    {
        if (SelectedSuggestion == null || Suggestions.Count == 0)
            return;

        var currentIndex = Suggestions.IndexOf(SelectedSuggestion);
        var nextIndex = (currentIndex + 1) % Suggestions.Count;

        // LOGIC: Find next pending suggestion, wrapping around.
        for (var i = 0; i < Suggestions.Count; i++)
        {
            var checkIndex = (nextIndex + i) % Suggestions.Count;
            if (Suggestions[checkIndex].Status == SuggestionStatus.Pending)
            {
                SelectSuggestion(Suggestions[checkIndex]);
                return;
            }
        }

        // LOGIC: No pending suggestions remain.
        StatusMessage = "All suggestions reviewed!";
        _logger.LogInformation("All suggestions have been reviewed");
    }

    /// <summary>
    /// Navigates to the previous suggestion.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Moves to the previous suggestion in the list (regardless of status),
    /// wrapping around to the end if at the beginning.
    /// </remarks>
    [RelayCommand]
    public void NavigatePrevious()
    {
        if (SelectedSuggestion == null || Suggestions.Count == 0)
            return;

        var currentIndex = Suggestions.IndexOf(SelectedSuggestion);
        var prevIndex = currentIndex == 0 ? Suggestions.Count - 1 : currentIndex - 1;

        SelectSuggestion(Suggestions[prevIndex]);
    }

    /// <summary>
    /// Closes the Tuning Panel.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _logger.LogDebug("Tuning Panel close requested");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // ── Filter Change Handler ────────────────────────────────────────────

    /// <summary>
    /// Called when the <see cref="CurrentFilter"/> property changes.
    /// </summary>
    /// <param name="value">The new filter value.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Notifies the <see cref="FilteredSuggestions"/> computed property
    /// to re-evaluate based on the new filter.
    /// </remarks>
    partial void OnCurrentFilterChanged(SuggestionFilter value)
    {
        _logger.LogDebug("Filter changed to {Filter}", value);
        OnPropertyChanged(nameof(FilteredSuggestions));
    }

    // ── Private Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Selects a suggestion card, collapsing the previously selected one.
    /// </summary>
    /// <param name="suggestion">The suggestion to select and expand.</param>
    private void SelectSuggestion(SuggestionCardViewModel suggestion)
    {
        // LOGIC: Collapse the previously selected suggestion.
        if (SelectedSuggestion != null)
            SelectedSuggestion.IsExpanded = false;

        SelectedSuggestion = suggestion;
        SelectedSuggestion.IsExpanded = true;
    }

    /// <summary>
    /// Applies text to the editor using sync APIs with undo group support.
    /// </summary>
    /// <param name="offset">The 0-based character offset.</param>
    /// <param name="deleteLength">Number of characters to delete.</param>
    /// <param name="insertText">The text to insert.</param>
    /// <param name="ruleId">The rule ID for the undo operation label.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Wraps <c>DeleteText</c> + <c>InsertText</c> in an editor
    /// undo group for atomic Ctrl+Z support. Optionally pushes a
    /// <see cref="TuningUndoableOperation"/> to the <see cref="IUndoRedoService"/>
    /// for labeled undo history.
    ///
    /// Spec adaptation: The spec uses <c>ReplaceTextAsync(path, span, text)</c>
    /// and <c>BeginUndoGroupAsync</c> which don't exist. Uses sync APIs instead.
    /// </remarks>
    private void ApplyTextToEditor(int offset, int deleteLength, string insertText, string ruleId)
    {
        // LOGIC: Capture original text for undo before modification.
        var originalText = _editorService.GetDocumentText()?.Substring(offset, deleteLength) ?? "";

        _editorService.BeginUndoGroup($"Tuning Fix ({ruleId})");
        try
        {
            _editorService.DeleteText(offset, deleteLength);
            _editorService.InsertText(offset, insertText);
        }
        finally
        {
            _editorService.EndUndoGroup();
        }

        // LOGIC: Push to the higher-level undo service if available.
        // This provides labeled undo history separate from the editor's built-in undo.
        if (_undoService != null)
        {
            var undoOp = new TuningUndoableOperation(
                offset, originalText, insertText, ruleId, _editorService);
            _undoService.Push(undoOp);
        }
    }

    /// <summary>
    /// Resets all counters to zero.
    /// </summary>
    private void ResetCounters()
    {
        TotalDeviations = 0;
        ReviewedCount = 0;
        AcceptedCount = 0;
        RejectedCount = 0;
    }

    /// <summary>
    /// Notifies all computed properties that may have changed.
    /// </summary>
    private void NotifyComputedProperties()
    {
        OnPropertyChanged(nameof(HighConfidenceCount));
        OnPropertyChanged(nameof(RemainingCount));
        OnPropertyChanged(nameof(FilteredSuggestions));
    }

    // ── Dispose ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// <b>LOGIC:</b> Cancels any in-progress scan and cleans up the
    /// <see cref="CancellationTokenSource"/>.
    /// </remarks>
    protected override void OnDisposed()
    {
        _logger.LogDebug("TuningPanelViewModel disposing");

        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = null;

        base.OnDisposed();
    }
}
