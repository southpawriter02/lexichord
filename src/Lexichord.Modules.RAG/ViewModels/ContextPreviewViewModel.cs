// =============================================================================
// File: ContextPreviewViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel managing context expansion state for search result items.
// =============================================================================
// LOGIC: Manages the expand/collapse state for context preview in search results.
//   - IsExpanded: Toggle for showing/hiding expanded context.
//   - IsLoading: True while fetching context from IContextExpansionService.
//   - IsLicensed: True if user has Writer Pro or higher license tier.
//   - ExpandedChunk: The fetched context data (before/after chunks, breadcrumb).
//   - Breadcrumb: Formatted heading path for the chunk's location.
//   - ShowUpgradePromptRequested: Flag for View to show upgrade dialog.
//   - Caches expanded data to avoid redundant service calls.
// =============================================================================
// VERSION: v0.5.3d (Context Preview UI)
// =============================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for managing context expansion state in search result items.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ContextPreviewViewModel"/> manages the expand/collapse state for context
/// preview functionality in search results. It coordinates with
/// <see cref="IContextExpansionService"/> to retrieve surrounding chunks and heading
/// breadcrumbs for a given search result.
/// </para>
/// <para>
/// <b>License Gating:</b> Context expansion is a Writer Pro feature. When a user
/// without the required license attempts to expand context, the ViewModel signals
/// the View to display an upgrade prompt via <see cref="ShowUpgradePromptRequested"/>.
/// </para>
/// <para>
/// <b>Caching:</b> Once context is fetched for a chunk, it is cached in the ViewModel
/// to avoid redundant service calls when collapsing and re-expanding.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3d as part of The Context Window feature.
/// </para>
/// </remarks>
public partial class ContextPreviewViewModel : ObservableObject
{
    private readonly Chunk _chunk;
    private readonly IContextExpansionService _contextExpansionService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<ContextPreviewViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ContextPreviewViewModel"/>.
    /// </summary>
    /// <param name="chunk">
    /// The RAG chunk for which to manage context expansion.
    /// Obtained by converting <see cref="SearchHit"/> data via the factory.
    /// </param>
    /// <param name="contextExpansionService">
    /// Service for retrieving surrounding context and heading breadcrumbs.
    /// </param>
    /// <param name="licenseContext">
    /// License context for checking Writer Pro feature access.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public ContextPreviewViewModel(
        Chunk chunk,
        IContextExpansionService contextExpansionService,
        ILicenseContext licenseContext,
        ILogger<ContextPreviewViewModel> logger)
    {
        _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        _contextExpansionService = contextExpansionService ?? throw new ArgumentNullException(nameof(contextExpansionService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize license state
        _isLicensed = CheckLicenseState();

        // Initialize breadcrumb from chunk heading if available
        _breadcrumb = chunk.HasHeading ? (chunk.Heading ?? string.Empty) : string.Empty;

        _logger.LogDebug(
            "[ContextPreviewViewModel] Initialized for chunk {ChunkId} in document {DocumentId}, IsLicensed={IsLicensed}",
            chunk.Id,
            chunk.DocumentId,
            _isLicensed);
    }

    #region Observable Properties

    /// <summary>
    /// Gets or sets whether the context preview is expanded.
    /// </summary>
    /// <remarks>
    /// When toggled to <c>true</c>, triggers context fetching via
    /// <see cref="IContextExpansionService"/> if not already cached.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandButtonIcon))]
    [NotifyPropertyChangedFor(nameof(ExpandButtonText))]
    private bool _isExpanded;

    /// <summary>
    /// Gets or sets whether context is currently being loaded.
    /// </summary>
    /// <remarks>
    /// <c>true</c> while awaiting <see cref="IContextExpansionService.ExpandAsync"/>.
    /// The View should display a loading indicator during this state.
    /// </remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToggleExpandedCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshContextCommand))]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets whether the user has the required license for context expansion.
    /// </summary>
    /// <remarks>
    /// <c>true</c> if the user has Writer Pro tier or higher.
    /// When <c>false</c>, the expand button shows a lock icon and triggers
    /// an upgrade prompt instead of expanding.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLockIcon))]
    private bool _isLicensed;

    /// <summary>
    /// Gets or sets the expanded chunk data including before/after context.
    /// </summary>
    /// <remarks>
    /// <c>null</c> until context is successfully fetched. Once populated,
    /// cached for subsequent expand/collapse operations.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrecedingContext))]
    [NotifyPropertyChangedFor(nameof(HasFollowingContext))]
    [NotifyPropertyChangedFor(nameof(HasExpandedData))]
    private ExpandedChunk? _expandedChunk;

    /// <summary>
    /// Gets or sets the formatted heading breadcrumb trail.
    /// </summary>
    /// <remarks>
    /// Initially set from the chunk's heading (if available). Updated with
    /// the full breadcrumb path when context is expanded.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBreadcrumb))]
    private string _breadcrumb = string.Empty;

    /// <summary>
    /// Gets or sets the error message if context expansion failed.
    /// </summary>
    /// <remarks>
    /// Set when <see cref="IContextExpansionService.ExpandAsync"/> throws an exception.
    /// Cleared on successful expansion or retry.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>
    /// Gets or sets whether the View should show an upgrade prompt.
    /// </summary>
    /// <remarks>
    /// Set to <c>true</c> when an unlicensed user attempts to expand context.
    /// The View should observe this property and show an upgrade dialog,
    /// then call <see cref="AcknowledgeUpgradePrompt"/> when the dialog closes.
    /// </remarks>
    [ObservableProperty]
    private bool _showUpgradePromptRequested;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether there is preceding (before) context available.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ExpandedChunk"/> has before chunks; otherwise, <c>false</c>.
    /// </value>
    public bool HasPrecedingContext => ExpandedChunk?.HasBefore ?? false;

    /// <summary>
    /// Gets whether there is following (after) context available.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ExpandedChunk"/> has after chunks; otherwise, <c>false</c>.
    /// </value>
    public bool HasFollowingContext => ExpandedChunk?.HasAfter ?? false;

    /// <summary>
    /// Gets whether expanded data has been loaded.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ExpandedChunk"/> is not null; otherwise, <c>false</c>.
    /// </value>
    public bool HasExpandedData => ExpandedChunk is not null;

    /// <summary>
    /// Gets whether a breadcrumb is available for display.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Breadcrumb"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    public bool HasBreadcrumb => !string.IsNullOrEmpty(Breadcrumb);

    /// <summary>
    /// Gets whether an error occurred during context expansion.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ErrorMessage"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets the icon for the expand/collapse button.
    /// </summary>
    /// <value>
    /// Returns "expand_more" when collapsed, "expand_less" when expanded.
    /// </value>
    public string ExpandButtonIcon => IsExpanded ? "expand_less" : "expand_more";

    /// <summary>
    /// Gets the text for the expand/collapse button.
    /// </summary>
    /// <value>
    /// Returns "Show Less" when expanded, "Show More Context" when collapsed.
    /// </value>
    public string ExpandButtonText => IsExpanded ? "Show Less" : "Show More Context";

    /// <summary>
    /// Gets whether to show a lock icon on the expand button.
    /// </summary>
    /// <value>
    /// <c>true</c> if user is not licensed; otherwise, <c>false</c>.
    /// </value>
    public bool ShowLockIcon => !IsLicensed;

    #endregion

    #region Commands

    /// <summary>
    /// Toggles the expanded state, fetching context if needed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the user is not licensed, sets <see cref="ShowUpgradePromptRequested"/>
    /// to <c>true</c> instead of expanding.
    /// </para>
    /// <para>
    /// If already expanded, simply collapses without re-fetching.
    /// If cached data exists, expands without service call.
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanToggleExpanded))]
    private async Task ToggleExpandedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "[ContextPreviewViewModel] ToggleExpandedAsync called, current IsExpanded={IsExpanded}, IsLicensed={IsLicensed}",
            IsExpanded,
            IsLicensed);

        // If currently expanded, just collapse
        if (IsExpanded)
        {
            _logger.LogDebug("[ContextPreviewViewModel] Collapsing context preview");
            IsExpanded = false;
            return;
        }

        // Check license before expanding
        if (!IsLicensed)
        {
            _logger.LogInformation(
                "[ContextPreviewViewModel] Unlicensed user attempted context expansion, showing upgrade prompt");
            ShowUpgradePromptRequested = true;
            return;
        }

        // Expand - fetch context if not cached
        if (ExpandedChunk is null)
        {
            await FetchContextAsync(cancellationToken);
        }
        else
        {
            _logger.LogDebug("[ContextPreviewViewModel] Using cached context data");
        }

        // Only expand if we have data (or if we want to show the error state)
        IsExpanded = true;
    }

    /// <summary>
    /// Gets whether the toggle command can execute.
    /// </summary>
    private bool CanToggleExpanded => !IsLoading;

    /// <summary>
    /// Refreshes the context data by re-fetching from the service.
    /// </summary>
    /// <remarks>
    /// Clears the cached <see cref="ExpandedChunk"/> and fetches fresh data.
    /// Useful for retry after error or when document has been re-indexed.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanRefreshContext))]
    private async Task RefreshContextAsync(CancellationToken cancellationToken = default)
    {
        if (!IsLicensed)
        {
            _logger.LogDebug("[ContextPreviewViewModel] RefreshContext called but user is not licensed");
            return;
        }

        _logger.LogDebug("[ContextPreviewViewModel] Refreshing context for chunk {ChunkId}", _chunk.Id);

        // Clear cached data
        ExpandedChunk = null;
        ErrorMessage = null;

        await FetchContextAsync(cancellationToken);
    }

    /// <summary>
    /// Gets whether the refresh command can execute.
    /// </summary>
    private bool CanRefreshContext => !IsLoading && IsLicensed && IsExpanded;

    #endregion

    #region Public Methods

    /// <summary>
    /// Acknowledges that the upgrade prompt has been shown.
    /// </summary>
    /// <remarks>
    /// Call this from the View after the upgrade dialog closes.
    /// Resets <see cref="ShowUpgradePromptRequested"/> to <c>false</c>.
    /// </remarks>
    public void AcknowledgeUpgradePrompt()
    {
        _logger.LogDebug("[ContextPreviewViewModel] Upgrade prompt acknowledged");
        ShowUpgradePromptRequested = false;
    }

    /// <summary>
    /// Refreshes the license state from the license context.
    /// </summary>
    /// <remarks>
    /// Call this if the license state may have changed (e.g., after upgrade).
    /// </remarks>
    public void RefreshLicenseState()
    {
        var wasLicensed = IsLicensed;
        IsLicensed = CheckLicenseState();

        if (wasLicensed != IsLicensed)
        {
            _logger.LogInformation(
                "[ContextPreviewViewModel] License state changed: {OldState} -> {NewState}",
                wasLicensed,
                IsLicensed);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Fetches context from the expansion service.
    /// </summary>
    private async Task FetchContextAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "[ContextPreviewViewModel] Fetching context for chunk {ChunkId} in document {DocumentId}",
            _chunk.Id,
            _chunk.DocumentId);

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var options = new ContextOptions(
                PrecedingChunks: 2,
                FollowingChunks: 2,
                IncludeHeadings: true);

            var expanded = await _contextExpansionService.ExpandAsync(
                _chunk,
                options,
                cancellationToken);

            ExpandedChunk = expanded;

            // Update breadcrumb from expanded data
            if (expanded.HasBreadcrumb)
            {
                Breadcrumb = expanded.FormatBreadcrumb();
            }

            _logger.LogInformation(
                "[ContextPreviewViewModel] Context fetched for chunk {ChunkId}: {BeforeCount} before, {AfterCount} after, HasBreadcrumb={HasBreadcrumb}",
                _chunk.Id,
                expanded.Before.Count,
                expanded.After.Count,
                expanded.HasBreadcrumb);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[ContextPreviewViewModel] Context fetch cancelled for chunk {ChunkId}", _chunk.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[ContextPreviewViewModel] Failed to fetch context for chunk {ChunkId}: {Message}",
                _chunk.Id,
                ex.Message);

            ErrorMessage = "Failed to load context. Click refresh to retry.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Checks the current license state for context expansion feature.
    /// </summary>
    /// <returns><c>true</c> if user has Writer Pro or higher; otherwise, <c>false</c>.</returns>
    private bool CheckLicenseState()
    {
        // Check both tier and feature code for flexibility
        var tier = _licenseContext.GetCurrentTier();
        var hasFeature = _licenseContext.IsFeatureEnabled(FeatureCodes.ContextExpansion);

        _logger.LogDebug(
            "[ContextPreviewViewModel] License check: Tier={Tier}, FeatureEnabled={FeatureEnabled}",
            tier,
            hasFeature);

        // Require at least WriterPro tier AND the feature must be enabled
        return tier >= LicenseTier.WriterPro && hasFeature;
    }

    #endregion
}
