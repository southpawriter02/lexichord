// -----------------------------------------------------------------------
// <copyright file="SimplificationPreviewViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Agents.Simplifier.Events;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// ViewModel for the simplification preview/diff UI.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel orchestrates the simplification preview experience,
/// allowing users to:
/// </para>
/// <list type="bullet">
///   <item><description>View original and simplified text side-by-side or inline</description></item>
///   <item><description>Compare readability metrics (before/after)</description></item>
///   <item><description>Review individual changes with explanations</description></item>
///   <item><description>Select/deselect changes for partial acceptance</description></item>
///   <item><description>Accept all or selected changes</description></item>
///   <item><description>Reject changes and close the preview</description></item>
///   <item><description>Re-run simplification with different settings</description></item>
/// </list>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
///   <item><description>Create instance via DI (transient)</description></item>
///   <item><description>Call <see cref="InitializeAsync"/> with the document path</description></item>
///   <item><description>Call <see cref="SetResult"/> with the simplification result</description></item>
///   <item><description>User interacts with commands</description></item>
///   <item><description>Handle <see cref="CloseRequested"/> event to close the view</description></item>
///   <item><description>Call <see cref="Dispose"/> for cleanup</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// The Simplifier Agent requires <see cref="LicenseTier.WriterPro"/> tier or higher.
/// If the license is insufficient, accept commands are disabled and a warning is shown.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating and initializing the preview
/// var viewModel = serviceProvider.GetRequiredService&lt;SimplificationPreviewViewModel&gt;();
/// await viewModel.InitializeAsync(documentPath);
///
/// var result = await pipeline.SimplifyAsync(request, ct);
/// viewModel.SetResult(result, originalText);
///
/// // Handling close request
/// viewModel.CloseRequested += (s, e) =&gt;
/// {
///     if (e.Accepted)
///     {
///         ShowNotification("Changes applied successfully");
///     }
///     ClosePreviewPanel();
/// };
/// </code>
/// </example>
/// <seealso cref="SimplificationChangeViewModel"/>
/// <seealso cref="SimplificationResult"/>
/// <seealso cref="ISimplificationPipeline"/>
public sealed partial class SimplificationPreviewViewModel : DisposableViewModel
{
    #region Fields

    private readonly ISimplificationPipeline _pipeline;
    private readonly IReadabilityTargetService _targetService;
    private readonly IEditorService _editorService;
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<SimplificationPreviewViewModel> _logger;

    private string? _documentPath;
    private SimplificationResult? _currentResult;
    private int _selectionStartBeforePreview;
    private int _selectionLengthBeforePreview;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplificationPreviewViewModel"/> class.
    /// </summary>
    /// <param name="pipeline">The simplification pipeline for re-simplification.</param>
    /// <param name="targetService">The readability target service for preset retrieval.</param>
    /// <param name="editorService">The editor service for applying changes.</param>
    /// <param name="mediator">The MediatR mediator for publishing events.</param>
    /// <param name="licenseContext">The license context for tier validation.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public SimplificationPreviewViewModel(
        ISimplificationPipeline pipeline,
        IReadabilityTargetService targetService,
        IEditorService editorService,
        IMediator mediator,
        ILicenseContext licenseContext,
        ILogger<SimplificationPreviewViewModel> logger)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _targetService = targetService ?? throw new ArgumentNullException(nameof(targetService));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Initialize the presets collection
        AvailablePresets = new ObservableCollection<AudiencePreset>();
        Changes = new ObservableCollection<SimplificationChangeViewModel>();

        _logger.LogDebug(
            "[SimplificationPreviewViewModel] Instance created. License tier: {Tier}",
            _licenseContext.Tier);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the preview should be closed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The view subscribes to this event and closes the preview panel
    /// when raised. Check <see cref="CloseRequestedEventArgs.Accepted"/> to determine
    /// if changes were applied.
    /// </remarks>
    public event EventHandler<CloseRequestedEventArgs>? CloseRequested;

    #endregion

    #region Observable Properties — Text

    /// <summary>
    /// Gets or sets the original text before simplification.
    /// </summary>
    /// <value>The text that was submitted for simplification.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Stored for diff comparison and for undo support when
    /// applying changes to the editor.
    /// </remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResimplifyCommand))]
    private string _originalText = string.Empty;

    /// <summary>
    /// Gets or sets the simplified text after transformation.
    /// </summary>
    /// <value>The result from the LLM simplification.</value>
    [ObservableProperty]
    private string _simplifiedText = string.Empty;

    #endregion

    #region Observable Properties — Metrics

    /// <summary>
    /// Gets or sets the readability metrics of the original text.
    /// </summary>
    /// <value>Metrics including grade level, reading ease, word count, etc.</value>
    [ObservableProperty]
    private ReadabilityMetrics? _originalMetrics;

    /// <summary>
    /// Gets or sets the readability metrics of the simplified text.
    /// </summary>
    /// <value>Metrics after simplification.</value>
    [ObservableProperty]
    private ReadabilityMetrics? _simplifiedMetrics;

    /// <summary>
    /// Gets or sets the grade level reduction achieved.
    /// </summary>
    /// <value>
    /// Positive values indicate improvement (lower grade = easier).
    /// Calculated as original grade level minus simplified grade level.
    /// </value>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGradeReductionBadge))]
    private double _gradeLevelReduction;

    /// <summary>
    /// Gets a value indicating whether the grade reduction badge should be shown.
    /// </summary>
    /// <value>
    /// <c>true</c> if the grade level reduction is greater than zero;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The badge only displays when there is actual improvement
    /// (positive reduction). Zero or negative values indicate no improvement
    /// or regression.
    /// </remarks>
    public bool ShowGradeReductionBadge => GradeLevelReduction > 0;

    /// <summary>
    /// Gets or sets the target readability that was used.
    /// </summary>
    /// <value>The <see cref="ReadabilityTarget"/> from the simplification request.</value>
    [ObservableProperty]
    private ReadabilityTarget? _targetUsed;

    /// <summary>
    /// Gets or sets a value indicating whether the target was achieved.
    /// </summary>
    /// <value>
    /// <c>true</c> if the simplified text meets the target readability;
    /// otherwise, <c>false</c>.
    /// </value>
    [ObservableProperty]
    private bool _targetAchieved;

    #endregion

    #region Observable Properties — Changes

    /// <summary>
    /// Gets or sets the collection of individual changes.
    /// </summary>
    /// <value>
    /// An observable collection of <see cref="SimplificationChangeViewModel"/> instances
    /// wrapping each <see cref="SimplificationChange"/>.
    /// </value>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(AcceptSelectedCommand))]
    private ObservableCollection<SimplificationChangeViewModel> _changes;

    /// <summary>
    /// Gets the count of selected changes.
    /// </summary>
    /// <value>The number of changes currently selected for acceptance.</value>
    public int SelectedChangeCount => Changes.Count(c => c.IsSelected);

    /// <summary>
    /// Gets the total count of changes.
    /// </summary>
    /// <value>The total number of changes available.</value>
    public int TotalChangeCount => Changes.Count;

    /// <summary>
    /// Gets a value indicating whether all changes are selected.
    /// </summary>
    /// <value>
    /// <c>true</c> if all changes are selected; otherwise, <c>false</c>.
    /// </value>
    public bool AllChangesSelected => Changes.All(c => c.IsSelected);

    #endregion

    #region Observable Properties — View State

    /// <summary>
    /// Gets or sets the current diff view mode.
    /// </summary>
    /// <value>
    /// The <see cref="DiffViewMode"/> determining how the diff is displayed.
    /// Defaults to <see cref="DiffViewMode.SideBySide"/>.
    /// </value>
    [ObservableProperty]
    private DiffViewMode _viewMode = DiffViewMode.SideBySide;

    /// <summary>
    /// Gets or sets a value indicating whether an operation is in progress.
    /// </summary>
    /// <value>
    /// <c>true</c> while re-simplification or acceptance is processing;
    /// otherwise, <c>false</c>.
    /// </value>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(AcceptSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(RejectAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResimplifyCommand))]
    private bool _isProcessing;

    /// <summary>
    /// Gets or sets a value indicating whether the result is being loaded.
    /// </summary>
    /// <value>
    /// <c>true</c> while waiting for the initial simplification result;
    /// otherwise, <c>false</c>.
    /// </value>
    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>
    /// Gets or sets the currently selected audience preset.
    /// </summary>
    /// <value>The preset used for simplification or re-simplification.</value>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResimplifyCommand))]
    private AudiencePreset? _selectedPreset;

    /// <summary>
    /// Gets or sets the available audience presets.
    /// </summary>
    /// <value>
    /// A collection of <see cref="AudiencePreset"/> instances available for selection.
    /// </value>
    [ObservableProperty]
    private ObservableCollection<AudiencePreset> _availablePresets;

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    /// <value>
    /// An error message if an operation failed; otherwise, <c>null</c>.
    /// </value>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets or sets a value indicating whether an error is displayed.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ErrorMessage"/> is not null; otherwise, <c>false</c>.
    /// </value>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    #endregion

    #region Observable Properties — License

    /// <summary>
    /// Gets or sets a value indicating whether the feature is licensed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the user has WriterPro tier or higher; otherwise, <c>false</c>.
    /// </value>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(AcceptSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResimplifyCommand))]
    private bool _isLicensed = true;

    /// <summary>
    /// Gets or sets the license warning message.
    /// </summary>
    /// <value>
    /// A message indicating that a license upgrade is required to use this feature;
    /// <c>null</c> if the feature is fully licensed.
    /// </value>
    [ObservableProperty]
    private string? _licenseWarning;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the preview ViewModel for a specific document.
    /// </summary>
    /// <param name="documentPath">
    /// The file path of the document being simplified.
    /// May be <c>null</c> for untitled documents.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method:
    /// </para>
    /// <list type="number">
    ///   <item><description>Stores the document path for events</description></item>
    ///   <item><description>Captures the current editor selection for restore</description></item>
    ///   <item><description>Loads available audience presets</description></item>
    ///   <item><description>Validates the license</description></item>
    /// </list>
    /// </remarks>
    public async Task InitializeAsync(string? documentPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "[SimplificationPreviewViewModel] Initializing for document: {DocumentPath}",
            documentPath ?? "(untitled)");

        _documentPath = documentPath;

        // LOGIC: Capture current selection for potential restore
        _selectionStartBeforePreview = _editorService.SelectionStart;
        _selectionLengthBeforePreview = _editorService.SelectionLength;

        _logger.LogTrace(
            "[SimplificationPreviewViewModel] Captured selection: Start={Start}, Length={Length}",
            _selectionStartBeforePreview,
            _selectionLengthBeforePreview);

        // LOGIC: Load available presets
        try
        {
            var presets = await _targetService.GetAllPresetsAsync(cancellationToken);

            AvailablePresets.Clear();
            foreach (var preset in presets)
            {
                AvailablePresets.Add(preset);
            }

            _logger.LogDebug(
                "[SimplificationPreviewViewModel] Loaded {Count} audience presets",
                AvailablePresets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SimplificationPreviewViewModel] Failed to load presets");
        }

        // LOGIC: Validate license
        ValidateLicense();

        _logger.LogInformation(
            "[SimplificationPreviewViewModel] Initialized. Licensed: {IsLicensed}",
            IsLicensed);
    }

    /// <summary>
    /// Sets the simplification result to display in the preview.
    /// </summary>
    /// <param name="result">The simplification result from the pipeline.</param>
    /// <param name="originalText">The original text that was simplified.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> or <paramref name="originalText"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method populates all observable properties from the result:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Sets original and simplified text</description></item>
    ///   <item><description>Sets before/after metrics</description></item>
    ///   <item><description>Creates change ViewModels for each change</description></item>
    ///   <item><description>Selects the matching preset if available</description></item>
    ///   <item><description>Updates computed properties</description></item>
    /// </list>
    /// </remarks>
    public void SetResult(SimplificationResult result, string originalText)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(originalText);

        _logger.LogDebug(
            "[SimplificationPreviewViewModel] Setting result. Success: {Success}, Changes: {ChangeCount}",
            result.Success,
            result.Changes.Count);

        _currentResult = result;

        // LOGIC: Set text properties
        OriginalText = originalText;
        SimplifiedText = result.SimplifiedText;

        // LOGIC: Set metrics
        OriginalMetrics = result.OriginalMetrics;
        SimplifiedMetrics = result.SimplifiedMetrics;
        GradeLevelReduction = result.GradeLevelReduction;
        TargetUsed = result.TargetUsed;
        TargetAchieved = result.TargetAchieved;

        // LOGIC: Create change ViewModels
        Changes.Clear();
        for (int i = 0; i < result.Changes.Count; i++)
        {
            var changeVm = new SimplificationChangeViewModel(result.Changes[i], i);

            // LOGIC: Subscribe to IsSelected changes to update selection counts
            changeVm.PropertyChanged += OnChangePropertyChanged;
            Track(new ActionDisposable(() => changeVm.PropertyChanged -= OnChangePropertyChanged));

            Changes.Add(changeVm);
        }

        // LOGIC: Try to match the selected preset
        if (TargetUsed is not null && AvailablePresets.Count > 0)
        {
            // LOGIC: Match by grade level (presets may not have IDs in the target)
            SelectedPreset = AvailablePresets.FirstOrDefault(p =>
                Math.Abs(p.TargetGradeLevel - TargetUsed.TargetGradeLevel) < 0.1);
        }

        // LOGIC: Handle error state
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            _logger.LogWarning(
                "[SimplificationPreviewViewModel] Simplification failed: {Error}",
                result.ErrorMessage);
        }
        else
        {
            ErrorMessage = null;
        }

        // LOGIC: Mark loading as complete
        IsLoading = false;

        // LOGIC: Notify computed properties
        OnPropertyChanged(nameof(SelectedChangeCount));
        OnPropertyChanged(nameof(TotalChangeCount));
        OnPropertyChanged(nameof(AllChangesSelected));
        OnPropertyChanged(nameof(HasError));

        _logger.LogInformation(
            "[SimplificationPreviewViewModel] Result set. Grade: {OriginalGrade:F1} → {SimplifiedGrade:F1} ({Reduction:F1} reduction)",
            OriginalMetrics?.FleschKincaidGradeLevel ?? 0,
            SimplifiedMetrics?.FleschKincaidGradeLevel ?? 0,
            GradeLevelReduction);
    }

    #endregion

    #region Commands — Accept/Reject

    /// <summary>
    /// Gets a value indicating whether the Accept All command can execute.
    /// </summary>
    /// <returns>
    /// <c>true</c> if changes exist, the feature is licensed, and no operation is in progress;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool CanAcceptAll() =>
        Changes.Count > 0 &&
        IsLicensed &&
        !IsProcessing;

    /// <summary>
    /// Accepts all changes and applies them to the document.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This command:
    /// </para>
    /// <list type="number">
    ///   <item><description>Begins an undo group for atomic undo</description></item>
    ///   <item><description>Deletes the original selected text</description></item>
    ///   <item><description>Inserts the simplified text</description></item>
    ///   <item><description>Ends the undo group</description></item>
    ///   <item><description>Publishes <see cref="SimplificationAcceptedEvent"/></description></item>
    ///   <item><description>Raises <see cref="CloseRequested"/></description></item>
    /// </list>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanAcceptAll))]
    private async Task AcceptAllAsync()
    {
        _logger.LogDebug("[SimplificationPreviewViewModel] AcceptAll command executing");

        IsProcessing = true;

        try
        {
            // LOGIC: Apply the simplified text to the editor
            ApplyTextToEditor(SimplifiedText);

            // LOGIC: Publish acceptance event
            await _mediator.Publish(new SimplificationAcceptedEvent(
                DocumentPath: _documentPath,
                OriginalText: OriginalText,
                SimplifiedText: SimplifiedText,
                AcceptedChangeCount: TotalChangeCount,
                TotalChangeCount: TotalChangeCount,
                GradeLevelReduction: GradeLevelReduction));

            _logger.LogInformation(
                "[SimplificationPreviewViewModel] Accepted all {Count} changes",
                TotalChangeCount);

            // LOGIC: Close the preview
            CloseRequested?.Invoke(this, CloseRequestedEventArgs.AcceptedClose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SimplificationPreviewViewModel] Failed to accept changes");
            ErrorMessage = $"Failed to apply changes: {ex.Message}";
            OnPropertyChanged(nameof(HasError));
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the Accept Selected command can execute.
    /// </summary>
    /// <returns>
    /// <c>true</c> if at least one change is selected, the feature is licensed,
    /// and no operation is in progress; otherwise, <c>false</c>.
    /// </returns>
    private bool CanAcceptSelected() =>
        SelectedChangeCount > 0 &&
        IsLicensed &&
        !IsProcessing;

    /// <summary>
    /// Accepts only selected changes and applies them to the document.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This command creates a merged text by applying only the selected
    /// changes. For partial acceptance:
    /// </para>
    /// <list type="number">
    ///   <item><description>Collects selected changes in position order</description></item>
    ///   <item><description>Builds merged text by applying selected changes</description></item>
    ///   <item><description>Applies merged text to the editor</description></item>
    ///   <item><description>Publishes <see cref="SimplificationAcceptedEvent"/> with partial counts</description></item>
    /// </list>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanAcceptSelected))]
    private async Task AcceptSelectedAsync()
    {
        _logger.LogDebug(
            "[SimplificationPreviewViewModel] AcceptSelected command executing. Selected: {Count}/{Total}",
            SelectedChangeCount,
            TotalChangeCount);

        IsProcessing = true;

        try
        {
            // LOGIC: Build merged text from selected changes
            var mergedText = BuildMergedText();

            // LOGIC: Apply the merged text to the editor
            ApplyTextToEditor(mergedText);

            // LOGIC: Publish acceptance event with partial counts
            await _mediator.Publish(new SimplificationAcceptedEvent(
                DocumentPath: _documentPath,
                OriginalText: OriginalText,
                SimplifiedText: mergedText,
                AcceptedChangeCount: SelectedChangeCount,
                TotalChangeCount: TotalChangeCount,
                GradeLevelReduction: GradeLevelReduction));

            _logger.LogInformation(
                "[SimplificationPreviewViewModel] Accepted {Accepted}/{Total} changes",
                SelectedChangeCount,
                TotalChangeCount);

            // LOGIC: Close the preview
            CloseRequested?.Invoke(this, CloseRequestedEventArgs.AcceptedClose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SimplificationPreviewViewModel] Failed to accept selected changes");
            ErrorMessage = $"Failed to apply changes: {ex.Message}";
            OnPropertyChanged(nameof(HasError));
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Rejects all changes and closes the preview without applying.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Publishes <see cref="SimplificationRejectedEvent"/> and closes
    /// the preview. No changes are made to the document.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanRejectAll))]
    private async Task RejectAllAsync()
    {
        _logger.LogDebug("[SimplificationPreviewViewModel] RejectAll command executing");

        IsProcessing = true;

        try
        {
            // LOGIC: Publish rejection event
            await _mediator.Publish(SimplificationRejectedEvent.UserCancelled(_documentPath));

            _logger.LogInformation("[SimplificationPreviewViewModel] Rejected all changes");

            // LOGIC: Close the preview
            CloseRequested?.Invoke(this, CloseRequestedEventArgs.RejectedClose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SimplificationPreviewViewModel] Failed to publish rejection event");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the Reject All command can execute.
    /// </summary>
    /// <returns><c>true</c> if no operation is in progress; otherwise, <c>false</c>.</returns>
    private bool CanRejectAll() => !IsProcessing;

    #endregion

    #region Commands — Re-simplification

    /// <summary>
    /// Gets a value indicating whether the Resimplify command can execute.
    /// </summary>
    /// <returns>
    /// <c>true</c> if text exists, a preset is selected, the feature is licensed,
    /// and no operation is in progress; otherwise, <c>false</c>.
    /// </returns>
    private bool CanResimplify() =>
        !string.IsNullOrWhiteSpace(OriginalText) &&
        SelectedPreset is not null &&
        IsLicensed &&
        !IsProcessing;

    /// <summary>
    /// Re-runs simplification with the currently selected preset.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This command:
    /// </para>
    /// <list type="number">
    ///   <item><description>Publishes <see cref="ResimplificationRequestedEvent"/></description></item>
    ///   <item><description>Gets the target from the selected preset</description></item>
    ///   <item><description>Calls the simplification pipeline</description></item>
    ///   <item><description>Updates the result via <see cref="SetResult"/></description></item>
    /// </list>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanResimplify))]
    private async Task ResimplifyAsync()
    {
        if (SelectedPreset is null)
        {
            _logger.LogWarning("[SimplificationPreviewViewModel] Cannot resimplify: no preset selected");
            return;
        }

        _logger.LogDebug(
            "[SimplificationPreviewViewModel] Resimplify command executing. Preset: {PresetId}",
            SelectedPreset.Id);

        IsProcessing = true;
        IsLoading = true;
        ErrorMessage = null;
        OnPropertyChanged(nameof(HasError));

        try
        {
            // LOGIC: Publish resimplification event
            await _mediator.Publish(new ResimplificationRequestedEvent(
                DocumentPath: _documentPath,
                OriginalText: OriginalText,
                NewPresetId: SelectedPreset.Id,
                NewStrategy: _currentResult?.StrategyUsed));

            // LOGIC: Get target from selected preset
            var target = await _targetService.GetTargetAsync(presetId: SelectedPreset.Id);

            // LOGIC: Build request
            var request = new SimplificationRequest
            {
                OriginalText = OriginalText,
                DocumentPath = _documentPath,
                Target = target,
                Strategy = _currentResult?.StrategyUsed ?? SimplificationStrategy.Balanced
            };

            // LOGIC: Run simplification
            var result = await _pipeline.SimplifyAsync(request);

            // LOGIC: Update the preview with new result
            SetResult(result, OriginalText);

            _logger.LogInformation(
                "[SimplificationPreviewViewModel] Resimplification complete. Success: {Success}",
                result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SimplificationPreviewViewModel] Resimplification failed");
            ErrorMessage = $"Re-simplification failed: {ex.Message}";
            OnPropertyChanged(nameof(HasError));
            IsLoading = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    #endregion

    #region Commands — Selection

    /// <summary>
    /// Toggles the selection state of a change.
    /// </summary>
    /// <param name="change">The change ViewModel to toggle.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Inverts the <see cref="SimplificationChangeViewModel.IsSelected"/> property.
    /// </remarks>
    [RelayCommand]
    private void ToggleChange(SimplificationChangeViewModel? change)
    {
        if (change is null) return;

        change.IsSelected = !change.IsSelected;

        _logger.LogTrace(
            "[SimplificationPreviewViewModel] Toggled change {Index} to {Selected}",
            change.Index,
            change.IsSelected);
    }

    /// <summary>
    /// Selects all changes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Sets <see cref="SimplificationChangeViewModel.IsSelected"/> to true
    /// for all changes.
    /// </remarks>
    [RelayCommand]
    private void SelectAllChanges()
    {
        foreach (var change in Changes)
        {
            change.IsSelected = true;
        }

        _logger.LogDebug("[SimplificationPreviewViewModel] Selected all {Count} changes", Changes.Count);
    }

    /// <summary>
    /// Deselects all changes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Sets <see cref="SimplificationChangeViewModel.IsSelected"/> to false
    /// for all changes.
    /// </remarks>
    [RelayCommand]
    private void DeselectAllChanges()
    {
        foreach (var change in Changes)
        {
            change.IsSelected = false;
        }

        _logger.LogDebug("[SimplificationPreviewViewModel] Deselected all changes");
    }

    #endregion

    #region Commands — View Mode

    /// <summary>
    /// Sets the diff view mode.
    /// </summary>
    /// <param name="mode">The view mode to set.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Updates the <see cref="ViewMode"/> property and logs the change.
    /// </remarks>
    [RelayCommand]
    private void SetViewMode(DiffViewMode mode)
    {
        ViewMode = mode;

        _logger.LogDebug(
            "[SimplificationPreviewViewModel] View mode set to {Mode}",
            mode);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates the license and updates license-related properties.
    /// </summary>
    private void ValidateLicense()
    {
        IsLicensed = _licenseContext.IsFeatureEnabled(FeatureCodes.SimplifierAgent);

        if (!IsLicensed)
        {
            LicenseWarning = "Simplifier Agent requires WriterPro tier or higher. " +
                             "Upgrade your license to accept changes.";

            _logger.LogDebug(
                "[SimplificationPreviewViewModel] License check failed. Tier: {Tier}",
                _licenseContext.Tier);
        }
        else
        {
            LicenseWarning = null;
        }
    }

    /// <summary>
    /// Applies text to the editor, replacing the original selection.
    /// </summary>
    /// <param name="text">The text to insert.</param>
    private void ApplyTextToEditor(string text)
    {
        _logger.LogTrace(
            "[SimplificationPreviewViewModel] Applying text to editor. Length: {Length}",
            text.Length);

        // LOGIC: Begin undo group for atomic undo
        _editorService.BeginUndoGroup("Simplify Text");

        try
        {
            // LOGIC: Delete the original selection
            _editorService.DeleteText(_selectionStartBeforePreview, _selectionLengthBeforePreview);

            // LOGIC: Insert the new text
            _editorService.InsertText(_selectionStartBeforePreview, text);

            _logger.LogTrace(
                "[SimplificationPreviewViewModel] Applied text at offset {Offset}",
                _selectionStartBeforePreview);
        }
        finally
        {
            _editorService.EndUndoGroup();
        }
    }

    /// <summary>
    /// Builds merged text by applying only selected changes to the original text.
    /// </summary>
    /// <returns>The merged text with selected changes applied.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> For partial acceptance, this method:
    /// </para>
    /// <list type="number">
    ///   <item><description>Gets selected changes ordered by position (reverse)</description></item>
    ///   <item><description>For each selected change with location, replaces the original text</description></item>
    ///   <item><description>If no changes have locations, falls back to simplified text</description></item>
    /// </list>
    /// </remarks>
    private string BuildMergedText()
    {
        // LOGIC: If all changes are selected, just use the simplified text
        if (AllChangesSelected)
        {
            return SimplifiedText;
        }

        // LOGIC: Get selected changes with locations, sorted by position (descending)
        var selectedChanges = Changes
            .Where(c => c.IsSelected && c.Location is not null)
            .OrderByDescending(c => c.Location!.Start)
            .ToList();

        // LOGIC: If no changes have locations, fall back to simplified text
        // (This shouldn't happen in practice, but handle gracefully)
        if (selectedChanges.Count == 0)
        {
            _logger.LogWarning(
                "[SimplificationPreviewViewModel] No changes with locations found. Using full simplified text.");
            return SimplifiedText;
        }

        // LOGIC: Start with original text and apply changes in reverse order
        var result = OriginalText;

        foreach (var change in selectedChanges)
        {
            var location = change.Location!;

            // LOGIC: Ensure the location is valid for the current result
            if (location.Start >= 0 && location.End <= result.Length)
            {
                result = string.Concat(
                    result.AsSpan(0, location.Start),
                    change.SimplifiedText,
                    result.AsSpan(location.End));
            }
            else
            {
                _logger.LogWarning(
                    "[SimplificationPreviewViewModel] Invalid location for change {Index}: Start={Start}, End={End}, TextLength={Length}",
                    change.Index,
                    location.Start,
                    location.End,
                    result.Length);
            }
        }

        return result;
    }

    /// <summary>
    /// Handles property changes on change ViewModels.
    /// </summary>
    /// <param name="sender">The change ViewModel.</param>
    /// <param name="e">The property changed event args.</param>
    private void OnChangePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SimplificationChangeViewModel.IsSelected))
        {
            // LOGIC: Update selection-dependent computed properties and commands
            OnPropertyChanged(nameof(SelectedChangeCount));
            OnPropertyChanged(nameof(AllChangesSelected));
            AcceptSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    /// <inheritdoc/>
    protected override void OnDisposed()
    {
        _logger.LogDebug("[SimplificationPreviewViewModel] Disposed");
        base.OnDisposed();
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Helper class for creating disposable actions.
    /// </summary>
    private sealed class ActionDisposable : IDisposable
    {
        private Action? _action;

        public ActionDisposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action?.Invoke();
            _action = null;
        }
    }

    #endregion
}
