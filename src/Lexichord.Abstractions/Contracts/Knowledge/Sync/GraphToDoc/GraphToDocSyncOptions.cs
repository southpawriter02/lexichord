// =============================================================================
// File: GraphToDocSyncOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for graph-to-document sync operations.
// =============================================================================
// LOGIC: Provides fine-grained control over sync behavior including automatic
//   flagging, notification settings, batching, and deduplication.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: FlagReason, FlagPriority
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Options for graph-to-document synchronization operations.
/// </summary>
/// <remarks>
/// <para>
/// Controls how <see cref="IGraphToDocumentSyncProvider"/> handles graph changes:
/// </para>
/// <list type="bullet">
///   <item><b>Flagging:</b> Whether to automatically create document flags.</item>
///   <item><b>Notifications:</b> Whether to send alerts to document owners.</item>
///   <item><b>Prioritization:</b> How to assign flag priorities by reason.</item>
///   <item><b>Batching:</b> Limits for processing large change sets.</item>
///   <item><b>Deduplication:</b> Preventing notification spam.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var options = new GraphToDocSyncOptions
/// {
///     AutoFlagDocuments = true,
///     SendNotifications = true,
///     ReasonPriorities = new Dictionary&lt;FlagReason, FlagPriority&gt;
///     {
///         [FlagReason.EntityDeleted] = FlagPriority.Critical,
///         [FlagReason.EntityValueChanged] = FlagPriority.High
///     },
///     MaxDocumentsPerChange = 500
/// };
/// var result = await provider.OnGraphChangeAsync(change, options);
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public record GraphToDocSyncOptions
{
    /// <summary>
    /// Whether to automatically flag affected documents.
    /// </summary>
    /// <value>True to create flags, false to only detect affected documents.</value>
    /// <remarks>
    /// LOGIC: When true, flags are created for each affected document
    /// with priority based on <see cref="ReasonPriorities"/>. When false,
    /// <see cref="GraphToDocSyncResult.AffectedDocuments"/> is populated
    /// but <see cref="GraphToDocSyncResult.FlagsCreated"/> is empty.
    /// </remarks>
    public bool AutoFlagDocuments { get; init; } = true;

    /// <summary>
    /// Whether to send notifications to document owners.
    /// </summary>
    /// <value>True to send notifications, false to suppress them.</value>
    /// <remarks>
    /// LOGIC: When true and <see cref="AutoFlagDocuments"/> is true,
    /// a <see cref="Events.DocumentFlaggedEvent"/> is published for each
    /// flag, enabling notification handlers to alert document owners.
    /// Subject to <see cref="DeduplicateNotifications"/>.
    /// </remarks>
    public bool SendNotifications { get; init; } = true;

    /// <summary>
    /// Priority mappings for flag reasons.
    /// </summary>
    /// <value>Dictionary mapping reasons to priorities.</value>
    /// <remarks>
    /// LOGIC: Overrides default priority assignment. Reasons not in
    /// the dictionary use <see cref="FlagPriority.Medium"/> as default.
    /// </remarks>
    public Dictionary<FlagReason, FlagPriority> ReasonPriorities { get; init; } = new()
    {
        [FlagReason.EntityValueChanged] = FlagPriority.High,
        [FlagReason.EntityDeleted] = FlagPriority.Critical,
        [FlagReason.EntityPropertiesUpdated] = FlagPriority.Medium,
        [FlagReason.NewRelationship] = FlagPriority.Medium,
        [FlagReason.RelationshipRemoved] = FlagPriority.Medium,
        [FlagReason.ConflictDetected] = FlagPriority.High
    };

    /// <summary>
    /// Batch size for processing multiple changes.
    /// </summary>
    /// <value>Number of changes to process per batch.</value>
    /// <remarks>
    /// LOGIC: Used when processing batched graph change events.
    /// Limits memory usage and enables progress reporting.
    /// </remarks>
    public int BatchSize { get; init; } = 100;

    /// <summary>
    /// Maximum documents to flag per change.
    /// </summary>
    /// <value>Upper limit on affected documents to process.</value>
    /// <remarks>
    /// LOGIC: Prevents runaway processing when a heavily-referenced
    /// entity changes. Excess documents are logged but not flagged.
    /// </remarks>
    public int MaxDocumentsPerChange { get; init; } = 1000;

    /// <summary>
    /// Minimum confidence for suggested actions.
    /// </summary>
    /// <value>Threshold between 0.0 and 1.0.</value>
    /// <remarks>
    /// LOGIC: Suggested actions with confidence below this threshold
    /// are excluded from <see cref="AffectedDocument.SuggestedAction"/>.
    /// </remarks>
    public float MinActionConfidence { get; init; } = 0.6f;

    /// <summary>
    /// Whether to include suggested actions in flags.
    /// </summary>
    /// <value>True to generate action suggestions.</value>
    /// <remarks>
    /// LOGIC: When false, <see cref="AffectedDocument.SuggestedAction"/>
    /// is always null, reducing processing overhead.
    /// </remarks>
    public bool IncludeSuggestedActions { get; init; } = true;

    /// <summary>
    /// Whether to deduplicate notifications.
    /// </summary>
    /// <value>True to prevent duplicate notifications.</value>
    /// <remarks>
    /// LOGIC: When true, notifications for the same document within
    /// <see cref="DeduplicationWindow"/> are suppressed.
    /// </remarks>
    public bool DeduplicateNotifications { get; init; } = true;

    /// <summary>
    /// Time window for notification deduplication.
    /// </summary>
    /// <value>Duration within which duplicate notifications are suppressed.</value>
    /// <remarks>
    /// LOGIC: If a document was notified within this window, new
    /// notifications are suppressed to prevent spam during batch updates.
    /// </remarks>
    public TimeSpan DeduplicationWindow { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Operation timeout.
    /// </summary>
    /// <value>Maximum duration for the sync operation.</value>
    /// <remarks>
    /// LOGIC: Operations exceeding this timeout throw
    /// <see cref="TimeoutException"/>. Default is 5 minutes.
    /// </remarks>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
}
