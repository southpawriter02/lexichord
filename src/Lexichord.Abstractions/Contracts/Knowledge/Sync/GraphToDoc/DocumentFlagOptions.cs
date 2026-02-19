// =============================================================================
// File: DocumentFlagOptions.cs
// Project: Lexichord.Abstractions
// Description: Options for creating document flags.
// =============================================================================
// LOGIC: Provides granular control over individual flag creation including
//   priority override, creator tracking, and contextual metadata.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: FlagPriority
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Options for creating a document flag.
/// </summary>
/// <remarks>
/// <para>
/// Controls how <see cref="IDocumentFlagger"/> creates individual flags:
/// </para>
/// <list type="bullet">
///   <item><b>Priority:</b> Override default priority assignment.</item>
///   <item><b>Tracking:</b> Record who created the flag.</item>
///   <item><b>Actions:</b> Whether to include suggested actions.</item>
///   <item><b>Notification:</b> Whether to send immediate notification.</item>
///   <item><b>Context:</b> Additional metadata for the flag.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var options = new DocumentFlagOptions
/// {
///     Priority = FlagPriority.High,
///     TriggeringEntityId = entityId,
///     SendNotification = true,
///     Tags = ["api-change", "breaking"]
/// };
/// var flag = await flagger.FlagDocumentAsync(docId, reason, options);
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public record DocumentFlagOptions
{
    /// <summary>
    /// Priority for the flag.
    /// </summary>
    /// <value>Flag priority level.</value>
    /// <remarks>
    /// LOGIC: Overrides the default priority that would be derived
    /// from the flag reason. Use when specific context requires
    /// different prioritization.
    /// </remarks>
    public FlagPriority Priority { get; init; } = FlagPriority.Medium;

    /// <summary>
    /// Entity whose change triggered the flag.
    /// </summary>
    /// <value>The triggering entity's GUID.</value>
    /// <remarks>
    /// LOGIC: Required for flags created due to graph changes.
    /// Links the flag to the specific entity for navigation
    /// and detail display.
    /// </remarks>
    public required Guid TriggeringEntityId { get; init; }

    /// <summary>
    /// User creating the flag.
    /// </summary>
    /// <value>Creator's user ID, or null for system-generated flags.</value>
    /// <remarks>
    /// LOGIC: For manual flag creation, records the user who
    /// initiated it. Null for automated graph-triggered flags.
    /// </remarks>
    public Guid? CreatedBy { get; init; }

    /// <summary>
    /// Whether to include suggested actions.
    /// </summary>
    /// <value>True to generate action suggestions.</value>
    /// <remarks>
    /// LOGIC: When true and conditions allow, populates
    /// <see cref="AffectedDocument.SuggestedAction"/> for associated
    /// documents. May increase processing time.
    /// </remarks>
    public bool IncludeSuggestedActions { get; init; } = true;

    /// <summary>
    /// Tags for categorizing the flag.
    /// </summary>
    /// <value>List of tag strings.</value>
    /// <remarks>
    /// LOGIC: Free-form tags for filtering and categorization.
    /// Examples: "breaking-change", "deprecation", "security".
    /// </remarks>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Whether to send notification immediately.
    /// </summary>
    /// <value>True to send notification on flag creation.</value>
    /// <remarks>
    /// LOGIC: When true, publishes <see cref="Events.DocumentFlaggedEvent"/>
    /// to notify document owner. Subject to deduplication settings.
    /// </remarks>
    public bool SendNotification { get; init; } = true;

    /// <summary>
    /// Additional context for the flag.
    /// </summary>
    /// <value>Dictionary of context key-value pairs.</value>
    /// <remarks>
    /// LOGIC: Stores arbitrary metadata about the flag context.
    /// May include old/new values, related entities, etc.
    /// </remarks>
    public Dictionary<string, object> Context { get; init; } = new();
}
