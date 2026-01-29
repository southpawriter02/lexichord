using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Event published when a document's dirty state changes.
/// </summary>
/// <remarks>
/// LOGIC: Published via MediatR when IsDirty transitions between true/false.
/// Subscribers can use this for:
/// - Auto-save triggers
/// - Status bar updates
/// - Window title updates
/// - Save confirmation workflows
/// </remarks>
/// <param name="DocumentId">Unique identifier for the document.</param>
/// <param name="FilePath">File path if document has been saved, null for new documents.</param>
/// <param name="IsDirty">The new dirty state (true = has unsaved changes).</param>
/// <param name="OccurredAt">When the state change occurred.</param>
public record DocumentDirtyChangedEvent(
    string DocumentId,
    string? FilePath,
    bool IsDirty,
    DateTimeOffset OccurredAt
) : INotification;
