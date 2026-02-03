// =============================================================================
// File: StaleIndicatorViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for the stale citation indicator component.
// =============================================================================
// LOGIC: Manages state for stale citation detection UI.
//   - ValidateAsync: Calls ICitationValidator.ValidateIfLicensedAsync and updates
//     display state. If unlicensed, hides the indicator entirely.
//   - ReverifyAsync: Re-indexes the source document via IIndexManagementService
//     using the Citation's ChunkId (which stores the document's GUID as set in
//     CitationService.CreateCitation), then re-validates to update status.
//   - DismissCommand: Hides the indicator without re-validation.
//   - IsVisible is true only when validation detects a non-valid status.
//   - Computed properties (IsStale, IsMissing, StatusIcon, StatusMessage) are
//     driven by the ValidationResult observable property.
//   - Registered as transient: each search result item gets its own instance.
// =============================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for the stale citation indicator component.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="StaleIndicatorViewModel"/> manages the display state for
/// stale citation indicators shown on search result items. It provides
/// validation, re-verification, and dismissal functionality.
/// </para>
/// <para>
/// <b>Display Logic:</b>
/// <list type="bullet">
///   <item><description><see cref="IsVisible"/> is <c>false</c> when validation is valid or user is unlicensed.</description></item>
///   <item><description><see cref="StatusIcon"/> shows ⚠️ for stale citations and ❌ for missing files.</description></item>
///   <item><description><see cref="StatusMessage"/> provides a tooltip-friendly description.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Re-verification:</b> The <see cref="ReverifyCommand"/> re-indexes the source
/// document through <see cref="IIndexManagementService.ReindexDocumentAsync"/> and
/// then re-validates the citation. The document ID is obtained from
/// <see cref="Citation.ChunkId"/> which stores the document's GUID
/// (set by <c>CitationService.CreateCitation</c> at v0.5.2a).
/// </para>
/// <para>
/// <b>Lifecycle:</b> Registered as transient in DI. Each search result item
/// creates its own <see cref="StaleIndicatorViewModel"/> instance.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2c as part of the Citation Engine (Stale Detection).
/// </para>
/// </remarks>
public partial class StaleIndicatorViewModel : ObservableObject
{
    private readonly ICitationValidator _validator;
    private readonly IIndexManagementService _indexService;
    private readonly ILogger<StaleIndicatorViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaleIndicatorViewModel"/> class.
    /// </summary>
    /// <param name="validator">
    /// Citation validator for checking freshness against source files.
    /// </param>
    /// <param name="indexService">
    /// Index management service for re-indexing stale documents.
    /// </param>
    /// <param name="logger">
    /// Logger for structured diagnostic output during validation operations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public StaleIndicatorViewModel(
        ICitationValidator validator,
        IIndexManagementService indexService,
        ILogger<StaleIndicatorViewModel> logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Observable Properties

    /// <summary>
    /// Gets or sets the validation result for the current citation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Set by <see cref="ValidateCommand"/> after validation completes.
    /// Drives all computed properties (IsStale, IsMissing, StatusIcon, StatusMessage).
    /// When null, the indicator has not yet been validated.
    /// </remarks>
    [ObservableProperty]
    private CitationValidationResult? _validationResult;

    /// <summary>
    /// Gets or sets whether the stale indicator is visible.
    /// </summary>
    /// <remarks>
    /// LOGIC: Visible only when validation detects a non-valid status (Stale, Missing, Error).
    /// Hidden when: validation returns Valid, user is unlicensed, or user dismisses.
    /// </remarks>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Gets or sets whether a re-verification operation is in progress.
    /// </summary>
    /// <remarks>
    /// LOGIC: Set to true during ReverifyCommand execution to show a loading state.
    /// Reset to false when re-verification completes (success or failure).
    /// </remarks>
    [ObservableProperty]
    private bool _isVerifying;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether the citation is stale (source modified after indexing).
    /// </summary>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="CitationValidationResult.IsStale"/>.
    /// Returns false when ValidationResult is null (not yet validated).
    /// </remarks>
    public bool IsStale => ValidationResult?.IsStale ?? false;

    /// <summary>
    /// Gets whether the source file is missing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="CitationValidationResult.IsMissing"/>.
    /// Returns false when ValidationResult is null (not yet validated).
    /// </remarks>
    public bool IsMissing => ValidationResult?.IsMissing ?? false;

    /// <summary>
    /// Gets the status icon for the indicator (⚠️ for stale, ❌ for missing).
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses ❌ for missing files (more severe) and ⚠️ for all other
    /// non-valid states (stale, error). Bound to the indicator icon in the view.
    /// </remarks>
    public string StatusIcon => IsMissing ? "❌" : "⚠️";

    /// <summary>
    /// Gets the user-friendly status message for tooltip display.
    /// </summary>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="CitationValidationResult.StatusMessage"/>.
    /// Returns empty string when ValidationResult is null.
    /// </remarks>
    public string StatusMessage => ValidationResult?.StatusMessage ?? string.Empty;

    #endregion

    #region Commands

    /// <summary>
    /// Validates the citation and updates the display state.
    /// </summary>
    /// <param name="citation">
    /// The citation to validate against its source file.
    /// </param>
    /// <remarks>
    /// LOGIC: Validation flow:
    /// <list type="number">
    ///   <item><description>Call <see cref="ICitationValidator.ValidateIfLicensedAsync"/>.</description></item>
    ///   <item><description>If result is null (unlicensed): hide indicator.</description></item>
    ///   <item><description>Set <see cref="ValidationResult"/> from the result.</description></item>
    ///   <item><description>Show indicator only for non-valid results.</description></item>
    /// </list>
    /// </remarks>
    [RelayCommand]
    private async Task ValidateAsync(Citation citation)
    {
        _logger.LogDebug(
            "Validating citation for stale indicator: {DocumentPath}",
            citation.DocumentPath);

        var result = await _validator.ValidateIfLicensedAsync(citation);

        if (result is null)
        {
            // LOGIC: User is not licensed for citation validation.
            // Hide the indicator entirely rather than showing as valid.
            IsVisible = false;
            return;
        }

        ValidationResult = result;
        IsVisible = !result.IsValid; // Show only for stale/missing/error
    }

    /// <summary>
    /// Re-indexes the source document and re-validates the citation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Re-verification flow:
    /// <list type="number">
    ///   <item><description>Guard: return early if no ValidationResult.</description></item>
    ///   <item><description>Set <see cref="IsVerifying"/> to true for loading state.</description></item>
    ///   <item><description>Call <see cref="IIndexManagementService.ReindexDocumentAsync"/>
    ///     with the document ID from <see cref="Citation.ChunkId"/>.</description></item>
    ///   <item><description>Re-validate the citation to update status.</description></item>
    ///   <item><description>Reset <see cref="IsVerifying"/> to false.</description></item>
    /// </list>
    /// The document ID is obtained from <see cref="Citation.ChunkId"/> which
    /// stores the document's GUID as set by CitationService.CreateCitation (v0.5.2a).
    /// </remarks>
    [RelayCommand]
    private async Task ReverifyAsync()
    {
        if (ValidationResult is null)
            return;

        _logger.LogDebug(
            "Re-verifying citation for {DocumentPath}",
            ValidationResult.Citation.DocumentPath);

        IsVerifying = true;

        try
        {
            // LOGIC: Re-index the document using the document ID stored in ChunkId.
            // CitationService.CreateCitation sets ChunkId = document.Id (v0.5.2a),
            // so Citation.ChunkId is the correct GUID for IIndexManagementService.
            await _indexService.ReindexDocumentAsync(ValidationResult.Citation.ChunkId);

            // LOGIC: Re-validate after re-indexing to update the stale indicator.
            // The citation's IndexedAt will still reference the old timestamp,
            // but the source file should now be up-to-date.
            await ValidateAsync(ValidationResult.Citation);
        }
        finally
        {
            IsVerifying = false;
        }
    }

    /// <summary>
    /// Dismisses the stale indicator without re-validation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Hides the indicator UI. The user has acknowledged the stale state
    /// and chosen not to re-verify. The ValidationResult is preserved in case
    /// the indicator needs to be shown again later.
    /// </remarks>
    [RelayCommand]
    private void Dismiss()
    {
        _logger.LogDebug(
            "Stale indicator dismissed for {DocumentPath}",
            ValidationResult?.Citation.DocumentPath ?? "unknown");

        IsVisible = false;
    }

    #endregion

    #region Property Change Notifications

    /// <summary>
    /// Called when <see cref="ValidationResult"/> changes.
    /// Raises property changed notifications for computed properties.
    /// </summary>
    /// <param name="value">The new validation result value.</param>
    /// <remarks>
    /// LOGIC: CommunityToolkit.Mvvm generates this partial method from
    /// [ObservableProperty]. We use it to notify the UI that computed
    /// properties (IsStale, IsMissing, StatusIcon, StatusMessage) have
    /// changed, since they depend on ValidationResult.
    /// </remarks>
    partial void OnValidationResultChanged(CitationValidationResult? value)
    {
        OnPropertyChanged(nameof(IsStale));
        OnPropertyChanged(nameof(IsMissing));
        OnPropertyChanged(nameof(StatusIcon));
        OnPropertyChanged(nameof(StatusMessage));
    }

    #endregion
}
