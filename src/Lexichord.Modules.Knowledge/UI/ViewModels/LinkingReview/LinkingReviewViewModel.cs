// =============================================================================
// File: LinkingReviewViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: ViewModel for the Linking Review Panel (v0.5.5c-i).
// =============================================================================
// LOGIC: Manages the entity linking review workflow:
//   - Loads pending link items from ILinkingReviewService
//   - Handles Accept/Reject/SelectAlternate/CreateNew/Skip/NotAnEntity actions
//   - Supports group decisions for similar mentions
//   - Tracks review statistics
//   - Provides keyboard shortcuts for efficient review
// =============================================================================
// VERSION: v0.5.5c-i (Linking Review UI)
// DEPENDENCIES: v0.5.5g (ILinkingReviewService), v0.4.7f (EntityDetailView)
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.UI.ViewModels.LinkingReview;

/// <summary>
/// ViewModel for the Linking Review Panel.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LinkingReviewViewModel"/> orchestrates the entity link review
/// workflow, managing pending items, review decisions, and statistics.
/// </para>
/// <para>
/// <b>Review Flow:</b>
/// <list type="number">
///   <item><description>Load pending items with optional filtering.</description></item>
///   <item><description>Select an item to review.</description></item>
///   <item><description>View mention context and candidate entities.</description></item>
///   <item><description>Make a decision (Accept, Reject, SelectAlternate, CreateNew, Skip, NotAnEntity).</description></item>
///   <item><description>Item is removed from queue and next item is selected.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Group Decisions:</b> When <see cref="ApplyToGroup"/> is true and the
/// selected item is part of a group, the decision is applied to all similar
/// mentions in the group.
/// </para>
/// <para>
/// <b>Keyboard Shortcuts:</b>
/// <list type="bullet">
///   <item><description>Ctrl+A: Accept</description></item>
///   <item><description>Ctrl+R: Reject</description></item>
///   <item><description>Ctrl+S: Skip</description></item>
///   <item><description>Ctrl+N: Create New Entity</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
public partial class LinkingReviewViewModel : ObservableObject
{
    // =========================================================================
    // DEPENDENCIES
    // =========================================================================

    private readonly ILinkingReviewService _reviewService;
    private readonly ILogger<LinkingReviewViewModel> _logger;

    // =========================================================================
    // OBSERVABLE PROPERTIES
    // =========================================================================

    /// <summary>
    /// Gets the collection of pending link items.
    /// </summary>
    public ObservableCollection<PendingLinkItem> PendingItems { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    [NotifyCanExecuteChangedFor(nameof(RejectCommand))]
    [NotifyCanExecuteChangedFor(nameof(SkipCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateNewEntityCommand))]
    [NotifyCanExecuteChangedFor(nameof(MarkNotEntityCommand))]
    private PendingLinkItem? _selectedItem;

    /// <summary>
    /// Gets or sets the current review statistics.
    /// </summary>
    [ObservableProperty]
    private ReviewStats? _stats;

    /// <summary>
    /// Gets or sets whether data is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the current filter settings.
    /// </summary>
    [ObservableProperty]
    private ReviewFilter _filter = ReviewFilter.Default;

    /// <summary>
    /// Gets or sets whether to apply decisions to all similar mentions.
    /// </summary>
    [ObservableProperty]
    private bool _applyToGroup;

    /// <summary>
    /// Gets or sets the error message if any operation failed.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets or sets whether review capability is available (Teams+ license).
    /// </summary>
    [ObservableProperty]
    private bool _canReview = true;

    /// <summary>
    /// Gets the available entity types for filtering.
    /// </summary>
    public ObservableCollection<string> EntityTypes { get; } = new() { "(All)" };

    /// <summary>
    /// Gets or sets the selected entity type filter.
    /// </summary>
    [ObservableProperty]
    private string? _selectedEntityType;

    /// <summary>
    /// Gets or sets the selected sort order.
    /// </summary>
    [ObservableProperty]
    private ReviewSortOrder _selectedSortOrder = ReviewSortOrder.Priority;

    // =========================================================================
    // CONSTRUCTOR
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkingReviewViewModel"/> class.
    /// </summary>
    /// <param name="reviewService">The review service for managing the queue.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public LinkingReviewViewModel(
        ILinkingReviewService reviewService,
        ILogger<LinkingReviewViewModel> logger)
    {
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Subscribe to queue changes for automatic refresh.
        _reviewService.QueueChanged += OnQueueChanged;

        _logger.LogDebug("LinkingReviewViewModel initialized");
    }

    // =========================================================================
    // COMMANDS
    // =========================================================================

    /// <summary>
    /// Loads pending items from the review service.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Loading pending review items");

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // LOGIC: Build filter from current UI state.
            var filter = BuildCurrentFilter();

            var items = await _reviewService.GetPendingAsync(filter, ct);

            // LOGIC: Clear and repopulate to trigger UI update.
            PendingItems.Clear();
            foreach (var item in items)
            {
                PendingItems.Add(item);
            }

            _logger.LogInformation(
                "Loaded {Count} pending review items",
                items.Count);

            // LOGIC: Auto-select first item if none selected.
            if (PendingItems.Count > 0 && SelectedItem == null)
            {
                SelectedItem = PendingItems[0];
            }

            // LOGIC: Load statistics.
            Stats = await _reviewService.GetStatsAsync(ct);

            // LOGIC: Update entity types for filter dropdown.
            UpdateEntityTypes();
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Load cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pending items");
            ErrorMessage = "Failed to load review queue. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Accepts the proposed link for the selected item.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteReviewAction))]
    private async Task AcceptAsync(CancellationToken ct = default)
    {
        if (SelectedItem == null) return;

        _logger.LogDebug(
            "Accepting link for item {ItemId}, ProposedEntity: {EntityId}",
            SelectedItem.Id,
            SelectedItem.ProposedEntityId);

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.Accept,
            SelectedEntityId = SelectedItem.ProposedEntityId,
            ApplyToGroup = ApplyToGroup && SelectedItem.IsGrouped
        }, ct);
    }

    /// <summary>
    /// Rejects the proposed link for the selected item.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteReviewAction))]
    private async Task RejectAsync(CancellationToken ct = default)
    {
        if (SelectedItem == null) return;

        _logger.LogDebug(
            "Rejecting link for item {ItemId}",
            SelectedItem.Id);

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.Reject,
            Reason = "Incorrect link",
            ApplyToGroup = ApplyToGroup && SelectedItem.IsGrouped
        }, ct);
    }

    /// <summary>
    /// Selects an alternate candidate entity.
    /// </summary>
    /// <param name="candidate">The candidate to select.</param>
    [RelayCommand]
    private async Task SelectAlternateAsync(LinkCandidate? candidate, CancellationToken ct = default)
    {
        if (SelectedItem == null || candidate == null) return;

        _logger.LogDebug(
            "Selecting alternate for item {ItemId}, Candidate: {CandidateId}",
            SelectedItem.Id,
            candidate.EntityId);

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.SelectAlternate,
            SelectedEntityId = candidate.EntityId,
            ApplyToGroup = ApplyToGroup && SelectedItem.IsGrouped
        }, ct);
    }

    /// <summary>
    /// Creates a new entity for the selected item.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteReviewAction))]
    private async Task CreateNewEntityAsync(CancellationToken ct = default)
    {
        if (SelectedItem == null) return;

        _logger.LogDebug(
            "Creating new entity for item {ItemId}",
            SelectedItem.Id);

        // LOGIC: Prepare entity properties from the mention.
        var properties = new Dictionary<string, object>
        {
            ["name"] = SelectedItem.Mention.Value,
            ["type"] = SelectedItem.Mention.EntityType
        };

        // LOGIC: Merge mention properties.
        foreach (var prop in SelectedItem.Mention.Properties)
        {
            properties[prop.Key] = prop.Value;
        }

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.CreateNew,
            NewEntityProperties = properties
        }, ct);
    }

    /// <summary>
    /// Skips the selected item (defer for later review).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteReviewAction))]
    private async Task SkipAsync(CancellationToken ct = default)
    {
        if (SelectedItem == null) return;

        _logger.LogDebug(
            "Skipping item {ItemId}",
            SelectedItem.Id);

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.Skip
        }, ct);
    }

    /// <summary>
    /// Marks the mention as not being an entity (false positive).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteReviewAction))]
    private async Task MarkNotEntityAsync(CancellationToken ct = default)
    {
        if (SelectedItem == null) return;

        _logger.LogDebug(
            "Marking as not entity for item {ItemId}",
            SelectedItem.Id);

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.NotAnEntity,
            Reason = "False positive - not an entity",
            ApplyToGroup = ApplyToGroup && SelectedItem.IsGrouped
        }, ct);
    }

    /// <summary>
    /// Refreshes the review queue.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct = default)
    {
        await LoadAsync(ct);
    }

    /// <summary>
    /// Applies the current filter settings.
    /// </summary>
    [RelayCommand]
    private async Task ApplyFilterAsync(CancellationToken ct = default)
    {
        SelectedItem = null;
        await LoadAsync(ct);
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private async Task ClearFiltersAsync(CancellationToken ct = default)
    {
        SelectedEntityType = null;
        SelectedSortOrder = ReviewSortOrder.Priority;
        Filter = ReviewFilter.Default;
        await LoadAsync(ct);
    }

    // =========================================================================
    // PRIVATE METHODS
    // =========================================================================

    /// <summary>
    /// Determines whether review actions can be executed.
    /// </summary>
    private bool CanExecuteReviewAction() =>
        SelectedItem != null && CanReview && !IsLoading;

    /// <summary>
    /// Submits a review decision and updates the UI.
    /// </summary>
    private async Task SubmitDecisionAsync(LinkReviewDecision decision, CancellationToken ct)
    {
        if (SelectedItem == null) return;

        var currentItem = SelectedItem;
        var currentIndex = PendingItems.IndexOf(currentItem);

        try
        {
            await _reviewService.SubmitDecisionAsync(decision, ct);

            _logger.LogInformation(
                "Submitted decision {Action} for item {ItemId}",
                decision.Action,
                decision.PendingLinkId);

            // LOGIC: Remove the item from the local list.
            PendingItems.Remove(currentItem);

            // LOGIC: If group decision, remove all group members.
            if (decision.ApplyToGroup && currentItem.GroupId != null)
            {
                var groupItems = PendingItems
                    .Where(i => i.GroupId == currentItem.GroupId)
                    .ToList();

                foreach (var item in groupItems)
                {
                    PendingItems.Remove(item);
                }

                _logger.LogDebug(
                    "Removed {Count} additional group items",
                    groupItems.Count);
            }

            // LOGIC: Select next item.
            if (PendingItems.Count > 0)
            {
                var nextIndex = Math.Min(currentIndex, PendingItems.Count - 1);
                SelectedItem = PendingItems[nextIndex];
            }
            else
            {
                SelectedItem = null;
            }

            // LOGIC: Update statistics.
            Stats = await _reviewService.GetStatsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit decision");
            ErrorMessage = $"Failed to submit decision: {ex.Message}";
        }
    }

    /// <summary>
    /// Builds a <see cref="ReviewFilter"/> from current UI state.
    /// </summary>
    private ReviewFilter BuildCurrentFilter()
    {
        return new ReviewFilter(
            DocumentId: null,
            EntityType: SelectedEntityType == "(All)" ? null : SelectedEntityType,
            MinConfidence: null,
            MaxConfidence: null,
            SortBy: SelectedSortOrder,
            Limit: 50);
    }

    /// <summary>
    /// Updates the entity type dropdown options.
    /// </summary>
    private void UpdateEntityTypes()
    {
        var currentTypes = PendingItems
            .Select(i => i.Mention.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        // LOGIC: Only update if types changed.
        var existingTypes = EntityTypes.Skip(1).ToList(); // Skip "(All)"
        if (!existingTypes.SequenceEqual(currentTypes))
        {
            EntityTypes.Clear();
            EntityTypes.Add("(All)");
            foreach (var type in currentTypes)
            {
                EntityTypes.Add(type);
            }
        }
    }

    /// <summary>
    /// Handles queue change events from the service.
    /// </summary>
    private void OnQueueChanged(object? sender, ReviewQueueChangedEventArgs e)
    {
        _logger.LogDebug(
            "Queue changed: {ChangeType}, Pending: {PendingCount}",
            e.ChangeType,
            e.PendingCount);

        // LOGIC: Refresh the queue on external changes.
        _ = LoadAsync();
    }

    // =========================================================================
    // PROPERTY CHANGE HANDLERS
    // =========================================================================

    partial void OnSelectedEntityTypeChanged(string? value)
    {
        // LOGIC: Trigger filter reload when entity type changes.
        _ = ApplyFilterAsync();
    }

    partial void OnSelectedSortOrderChanged(ReviewSortOrder value)
    {
        // LOGIC: Trigger filter reload when sort order changes.
        _ = ApplyFilterAsync();
    }
}
