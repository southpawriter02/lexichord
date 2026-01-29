using MediatR;

namespace Lexichord.Abstractions.Messaging;

/// <summary>
/// Notification published before a document closes.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="Force">Whether the close was forced.</param>
public sealed record DocumentClosingNotification(string DocumentId, bool Force) : INotification;

/// <summary>
/// Notification published after a document has closed.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
public sealed record DocumentClosedNotification(string DocumentId) : INotification;

/// <summary>
/// Notification published when a document's pinned state changes.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="IsPinned">The new pinned state.</param>
public sealed record DocumentPinnedNotification(string DocumentId, bool IsPinned) : INotification;

/// <summary>
/// Notification published when a document's dirty state changes.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="IsDirty">The new dirty state.</param>
public sealed record DocumentDirtyNotification(string DocumentId, bool IsDirty) : INotification;
