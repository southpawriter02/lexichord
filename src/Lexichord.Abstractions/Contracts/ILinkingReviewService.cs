// =============================================================================
// File: ILinkingReviewService.cs
// Project: Lexichord.Abstractions
// Description: Interface and records for entity linking review (v0.5.5c-i).
// =============================================================================
// LOGIC: Defines the contract for managing entity linking review queue:
//   - ILinkingReviewService: Service interface for review operations
//   - PendingLinkItem: Entity link awaiting human review
//   - LinkReviewDecision: Decision made during review
//   - ReviewStats: Statistics about review progress
//   - ReviewFilter: Filter criteria for the review queue
//   Review allows accepting, rejecting, or modifying entity links with
//   confidence below the auto-accept threshold.
// =============================================================================
// VERSION: v0.5.5c-i (Linking Review UI)
// DEPENDENCIES: v0.5.5g (IEntityLinkingService), v0.4.7f (EntityDetailView)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Actions available during entity link review.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReviewAction"/> defines the possible decisions a reviewer can make
/// when reviewing an entity link. Each action affects how the link is processed
/// and stored in the knowledge graph.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
public enum ReviewAction
{
    /// <summary>
    /// Accept the proposed entity link.
    /// </summary>
    /// <remarks>
    /// The link is confirmed and the confidence is updated to reflect human verification.
    /// </remarks>
    Accept,

    /// <summary>
    /// Reject the entity link (unlink the mention from the entity).
    /// </summary>
    /// <remarks>
    /// The link is removed. The mention may be re-linked to a different entity
    /// or left unlinked.
    /// </remarks>
    Reject,

    /// <summary>
    /// Select a different candidate entity for linking.
    /// </summary>
    /// <remarks>
    /// The original link is replaced with a link to the selected alternative.
    /// </remarks>
    SelectAlternate,

    /// <summary>
    /// Create a new entity and link the mention to it.
    /// </summary>
    /// <remarks>
    /// Used when no existing entity matches the mention.
    /// </remarks>
    CreateNew,

    /// <summary>
    /// Skip this item (defer for later review).
    /// </summary>
    /// <remarks>
    /// The item remains in the queue and can be reviewed later.
    /// </remarks>
    Skip,

    /// <summary>
    /// Mark the mention as not being an entity (false positive).
    /// </summary>
    /// <remarks>
    /// The mention is removed from the linking queue and marked as a false positive
    /// for improving future extraction.
    /// </remarks>
    NotAnEntity
}

/// <summary>
/// Sort order for the review queue.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
public enum ReviewSortOrder
{
    /// <summary>Sort by review priority (highest first).</summary>
    Priority,

    /// <summary>Sort by confidence ascending (lowest confidence first).</summary>
    ConfidenceAsc,

    /// <summary>Sort by confidence descending (highest confidence first).</summary>
    ConfidenceDesc,

    /// <summary>Sort by creation date ascending (oldest first).</summary>
    CreatedAtAsc,

    /// <summary>Sort by creation date descending (newest first).</summary>
    CreatedAtDesc,

    /// <summary>Sort by document order (group by document).</summary>
    DocumentOrder
}

/// <summary>
/// Filter criteria for the review queue.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ReviewFilter"/> specifies criteria for filtering the pending
/// link review queue. All properties are optional; null values indicate no
/// filtering for that criterion.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
/// <param name="DocumentId">Filter by source document.</param>
/// <param name="EntityType">Filter by entity type (e.g., "Endpoint", "Concept").</param>
/// <param name="MinConfidence">Minimum confidence threshold (exclusive).</param>
/// <param name="MaxConfidence">Maximum confidence threshold (exclusive).</param>
/// <param name="SortBy">Sort order for results.</param>
/// <param name="Limit">Maximum number of items to return.</param>
public record ReviewFilter(
    Guid? DocumentId = null,
    string? EntityType = null,
    float? MinConfidence = null,
    float? MaxConfidence = null,
    ReviewSortOrder SortBy = ReviewSortOrder.Priority,
    int Limit = 50)
{
    /// <summary>
    /// Returns a default filter with no criteria.
    /// </summary>
    public static ReviewFilter Default => new();
}

/// <summary>
/// Suggested review decision based on patterns.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ReviewSuggestion"/> provides AI-assisted guidance for review
/// decisions based on patterns observed in previous reviews and entity similarity.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
/// <param name="SuggestedAction">The suggested action to take.</param>
/// <param name="SuggestedEntityId">Suggested entity ID if accepting an alternate.</param>
/// <param name="Reason">Human-readable reason for the suggestion.</param>
/// <param name="Confidence">Confidence in the suggestion (0.0 to 1.0).</param>
public record ReviewSuggestion(
    ReviewAction SuggestedAction,
    Guid? SuggestedEntityId = null,
    string? Reason = null,
    float Confidence = 0.5f);

/// <summary>
/// An entity link pending human review.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="PendingLinkItem"/> represents an entity link that has been
/// created by the automatic linking system but requires human verification
/// due to insufficient confidence.
/// </para>
/// <para>
/// <b>Grouping:</b> Similar mentions (same value and entity type) can be
/// grouped together for bulk review. When <see cref="IsGrouped"/> is true,
/// the <see cref="GroupId"/> identifies the group and <see cref="GroupCount"/>
/// indicates how many similar mentions exist.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
public record PendingLinkItem
{
    /// <summary>
    /// Unique identifier for this pending link item.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The entity mention that was detected.
    /// </summary>
    /// <value>
    /// An <see cref="EntityMention"/> containing the text, type, and position
    /// of the detected entity.
    /// </value>
    public required EntityMention Mention { get; init; }

    /// <summary>
    /// The proposed entity to link to (may be null if no candidate found).
    /// </summary>
    /// <value>
    /// The GUID of the proposed entity, or <c>null</c> if the linking system
    /// could not find a suitable candidate.
    /// </value>
    public Guid? ProposedEntityId { get; init; }

    /// <summary>
    /// Name of the proposed entity for display.
    /// </summary>
    public string? ProposedEntityName { get; init; }

    /// <summary>
    /// Confidence score of the proposed link (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Alternative candidate entities with their scores.
    /// </summary>
    /// <value>
    /// A list of alternative entities that could be linked to, sorted by score.
    /// </value>
    public IReadOnlyList<LinkCandidate>? Candidates { get; init; }

    /// <summary>
    /// Document containing the mention.
    /// </summary>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Document title for display.
    /// </summary>
    public string? DocumentTitle { get; init; }

    /// <summary>
    /// Extended context around the mention.
    /// </summary>
    /// <value>
    /// Text surrounding the mention to help reviewers understand the context.
    /// Typically 100-200 characters before and after.
    /// </value>
    public string? ExtendedContext { get; init; }

    /// <summary>
    /// When the link was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Priority for review (higher = more urgent).
    /// </summary>
    /// <value>
    /// An integer priority value. Default is 0. Higher values indicate
    /// items that should be reviewed sooner.
    /// </value>
    public int Priority { get; init; }

    /// <summary>
    /// Whether this item is part of a group of similar mentions.
    /// </summary>
    public bool IsGrouped { get; init; }

    /// <summary>
    /// Group ID if this item is grouped with similar mentions.
    /// </summary>
    public string? GroupId { get; init; }

    /// <summary>
    /// Count of similar mentions in the group.
    /// </summary>
    public int GroupCount { get; init; }

    /// <summary>
    /// AI-suggested decision based on patterns.
    /// </summary>
    public ReviewSuggestion? Suggestion { get; init; }
}

/// <summary>
/// A candidate entity for linking.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="LinkCandidate"/> represents a potential entity that a mention
/// could be linked to. Candidates are ranked by their similarity score.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
/// <param name="EntityId">Unique identifier of the candidate entity.</param>
/// <param name="EntityName">Display name of the candidate.</param>
/// <param name="EntityType">Type of the candidate (e.g., "Endpoint", "Concept").</param>
/// <param name="Score">Similarity score (0.0 to 1.0).</param>
/// <param name="Description">Optional description of the entity.</param>
public record LinkCandidate(
    Guid EntityId,
    string EntityName,
    string EntityType,
    float Score,
    string? Description = null);

/// <summary>
/// A decision made during link review.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="LinkReviewDecision"/> captures the decision made by a reviewer
/// for a pending link item. The decision specifies the action taken, any
/// selected entity, and optional metadata.
/// </para>
/// <para>
/// <b>Group Decisions:</b> When <see cref="ApplyToGroup"/> is true and the
/// pending item is part of a group, the decision is applied to all similar
/// mentions in the group.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
public record LinkReviewDecision
{
    /// <summary>
    /// ID of the pending link item being reviewed.
    /// </summary>
    public required Guid PendingLinkId { get; init; }

    /// <summary>
    /// Action taken by the reviewer.
    /// </summary>
    public required ReviewAction Action { get; init; }

    /// <summary>
    /// Selected entity ID (for Accept, SelectAlternate actions).
    /// </summary>
    public Guid? SelectedEntityId { get; init; }

    /// <summary>
    /// New entity to create (for CreateNew action).
    /// </summary>
    /// <value>
    /// A dictionary of entity properties for the new entity.
    /// Keys are property names, values are property values.
    /// </value>
    public IReadOnlyDictionary<string, object>? NewEntityProperties { get; init; }

    /// <summary>
    /// Reason for the decision (for Reject, Skip, NotAnEntity).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Whether to apply this decision to all similar mentions.
    /// </summary>
    /// <value>
    /// <c>true</c> to apply the decision to all mentions in the group;
    /// <c>false</c> to apply only to this specific mention.
    /// </value>
    public bool ApplyToGroup { get; init; }

    /// <summary>
    /// Reviewer's identifier.
    /// </summary>
    public string? ReviewerId { get; init; }

    /// <summary>
    /// When the decision was made.
    /// </summary>
    public DateTimeOffset DecidedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Statistics about a reviewer's activity.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
/// <param name="ReviewerId">Unique identifier of the reviewer.</param>
/// <param name="ReviewerName">Display name of the reviewer.</param>
/// <param name="ReviewCount">Total number of reviews completed.</param>
/// <param name="AcceptanceRate">Acceptance rate (0.0 to 1.0).</param>
public record ReviewerStats(
    string ReviewerId,
    string ReviewerName,
    int ReviewCount,
    float AcceptanceRate);

/// <summary>
/// Review queue statistics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReviewStats"/> provides aggregate statistics about the review
/// queue and reviewer activity. Used to display progress dashboards.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
public record ReviewStats
{
    /// <summary>
    /// Total items pending review.
    /// </summary>
    public int PendingCount { get; init; }

    /// <summary>
    /// Items reviewed today.
    /// </summary>
    public int ReviewedToday { get; init; }

    /// <summary>
    /// Total items ever reviewed.
    /// </summary>
    public int TotalReviewed { get; init; }

    /// <summary>
    /// Acceptance rate (0.0 to 1.0).
    /// </summary>
    public float AcceptanceRate { get; init; }

    /// <summary>
    /// Average time to review an item.
    /// </summary>
    public TimeSpan AverageReviewTime { get; init; }

    /// <summary>
    /// Breakdown of decisions by action type.
    /// </summary>
    public IReadOnlyDictionary<ReviewAction, int>? ByAction { get; init; }

    /// <summary>
    /// Breakdown of pending items by entity type.
    /// </summary>
    public IReadOnlyDictionary<string, int>? ByEntityType { get; init; }

    /// <summary>
    /// Top reviewers by review count.
    /// </summary>
    public IReadOnlyList<ReviewerStats>? TopReviewers { get; init; }

    /// <summary>
    /// Returns empty statistics.
    /// </summary>
    public static ReviewStats Empty => new()
    {
        PendingCount = 0,
        ReviewedToday = 0,
        TotalReviewed = 0,
        AcceptanceRate = 0f,
        AverageReviewTime = TimeSpan.Zero,
        ByAction = new Dictionary<ReviewAction, int>(),
        ByEntityType = new Dictionary<string, int>(),
        TopReviewers = Array.Empty<ReviewerStats>()
    };
}

/// <summary>
/// Event args for review queue changes.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReviewQueueChangedEventArgs"/> is raised when the review queue
/// changes due to new items being added, items being reviewed, or external
/// updates.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
/// <param name="PendingCount">New pending count after the change.</param>
/// <param name="AddedCount">Number of items added.</param>
/// <param name="RemovedCount">Number of items removed.</param>
/// <param name="ChangeType">Type of change that occurred.</param>
public record ReviewQueueChangedEventArgs(
    int PendingCount,
    int AddedCount = 0,
    int RemovedCount = 0,
    ReviewQueueChangeType ChangeType = ReviewQueueChangeType.Updated);

/// <summary>
/// Types of changes to the review queue.
/// </summary>
public enum ReviewQueueChangeType
{
    /// <summary>Items were added to the queue.</summary>
    ItemsAdded,

    /// <summary>Items were removed (reviewed) from the queue.</summary>
    ItemsRemoved,

    /// <summary>Queue was refreshed from external source.</summary>
    Refreshed,

    /// <summary>General update notification.</summary>
    Updated
}

/// <summary>
/// Service for managing entity linking review queue.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ILinkingReviewService"/> provides operations for managing the
/// human review queue for entity links. It supports:
/// <list type="bullet">
///   <item><description>Retrieving pending items with filtering and sorting.</description></item>
///   <item><description>Submitting review decisions (single and batch).</description></item>
///   <item><description>Tracking review statistics.</description></item>
///   <item><description>Queue change notifications.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item><description>WriterPro: View queue and statistics (read-only).</description></item>
///   <item><description>Teams+: Full review capabilities.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent
/// access from multiple UI threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get pending items
/// var pending = await reviewService.GetPendingAsync(
///     new ReviewFilter { EntityType = "Endpoint", SortBy = ReviewSortOrder.ConfidenceAsc },
///     ct);
///
/// // Submit a decision
/// await reviewService.SubmitDecisionAsync(new LinkReviewDecision
/// {
///     PendingLinkId = pending[0].Id,
///     Action = ReviewAction.Accept,
///     SelectedEntityId = pending[0].ProposedEntityId
/// }, ct);
///
/// // Get statistics
/// var stats = await reviewService.GetStatsAsync(ct);
/// Console.WriteLine($"Pending: {stats.PendingCount}, Acceptance rate: {stats.AcceptanceRate:P0}");
/// </code>
/// </example>
public interface ILinkingReviewService
{
    /// <summary>
    /// Gets pending links needing review.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of pending link items.</returns>
    /// <remarks>
    /// <para>
    /// Returns items sorted according to the filter's <see cref="ReviewFilter.SortBy"/>
    /// setting, up to <see cref="ReviewFilter.Limit"/> items.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<PendingLinkItem>> GetPendingAsync(
        ReviewFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the count of pending reviews.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of items awaiting review.</returns>
    Task<int> GetPendingCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Submits a review decision.
    /// </summary>
    /// <param name="decision">The review decision.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// If <see cref="LinkReviewDecision.ApplyToGroup"/> is true and the item
    /// is part of a group, the decision is applied to all similar mentions.
    /// </para>
    /// </remarks>
    Task SubmitDecisionAsync(LinkReviewDecision decision, CancellationToken ct = default);

    /// <summary>
    /// Submits multiple review decisions in batch.
    /// </summary>
    /// <param name="decisions">The review decisions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// Batch submission is more efficient for reviewing multiple items
    /// at once, as it reduces round-trips to the database.
    /// </para>
    /// </remarks>
    Task SubmitDecisionsBatchAsync(
        IReadOnlyList<LinkReviewDecision> decisions,
        CancellationToken ct = default);

    /// <summary>
    /// Gets review statistics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current review queue statistics.</returns>
    Task<ReviewStats> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when the queue changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this event to refresh the UI when items are added or
    /// removed from the queue.
    /// </para>
    /// </remarks>
    event EventHandler<ReviewQueueChangedEventArgs>? QueueChanged;
}
