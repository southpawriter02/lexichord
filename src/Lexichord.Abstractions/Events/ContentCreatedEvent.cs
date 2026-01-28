namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when new content is created in the system.
/// </summary>
/// <remarks>
/// LOGIC: This event enables loose coupling between content producers
/// (e.g., Documents module) and content consumers (e.g., RAG, Analytics).
///
/// **When to Publish:**
/// - After a document, chapter, project, or other content is persisted.
/// - The content should be in a stable, queryable state.
///
/// **Expected Handlers:**
/// - RAG Module: Index content for semantic search.
/// - Analytics Module: Track content creation metrics.
/// - Notification Service: Notify collaborators.
/// - Backup Service: Trigger incremental backup.
///
/// **Handler Responsibilities:**
/// - Handlers SHOULD be idempotent (safe to process same event twice).
/// - Handlers SHOULD NOT modify the source content.
/// - Handlers MAY query additional data using ContentId.
/// </remarks>
/// <example>
/// Publishing the event:
/// <code>
/// await _mediator.Publish(new ContentCreatedEvent
/// {
///     ContentId = document.Id.ToString(),
///     ContentType = ContentType.Document,
///     Title = document.Title,
///     Description = document.Synopsis,
///     CreatedBy = currentUser.Id,
///     CorrelationId = correlationId
/// });
/// </code>
/// </example>
public record ContentCreatedEvent : DomainEventBase
{
    /// <summary>
    /// Unique identifier of the created content.
    /// </summary>
    /// <remarks>
    /// LOGIC: String representation of the content ID to avoid type
    /// dependencies. Handlers can parse to their expected ID type.
    /// </remarks>
    public required string ContentId { get; init; }

    /// <summary>
    /// The type of content that was created.
    /// </summary>
    /// <remarks>
    /// LOGIC: Allows handlers to filter events. A handler that only
    /// processes Documents can check this before proceeding.
    /// </remarks>
    public required ContentType ContentType { get; init; }

    /// <summary>
    /// Human-readable title of the content.
    /// </summary>
    /// <remarks>
    /// LOGIC: Included directly in the event to avoid requiring handlers
    /// to query the database for basic display/logging purposes.
    /// </remarks>
    public required string Title { get; init; }

    /// <summary>
    /// Optional description or synopsis of the content.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Identifier of the user who created the content.
    /// </summary>
    /// <remarks>
    /// LOGIC: String representation to avoid user model dependencies.
    /// Could be a GUID, email, or username depending on the system.
    /// </remarks>
    public required string CreatedBy { get; init; }

    /// <summary>
    /// Optional metadata associated with the content creation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Dictionary allows attaching arbitrary metadata without
    /// schema changes. Examples: "wordCount", "language", "tags".
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
