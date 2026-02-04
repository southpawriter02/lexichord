// =============================================================================
// File: CacheInvalidationHandler.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR handler for automatic cache invalidation on document changes.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Subscribes to document lifecycle events and triggers cache invalidation.
//   - DocumentIndexedEvent: Document was re-indexed, invalidate stale cache entries.
//   - DocumentRemovedFromIndexEvent: Document removed, invalidate all references.
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Handles document lifecycle events to invalidate stale cache entries.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CacheInvalidationHandler"/> listens for document indexing and removal
/// events via MediatR, then triggers invalidation on both the query result cache
/// and context expansion cache.
/// </para>
/// <para>
/// <b>Events Handled:</b>
/// <list type="bullet">
///   <item><see cref="DocumentIndexedEvent"/>: Invalidates entries for re-indexed document.</item>
///   <item><see cref="DocumentRemovedFromIndexEvent"/>: Invalidates entries for removed document.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public sealed class CacheInvalidationHandler :
    INotificationHandler<DocumentIndexedEvent>,
    INotificationHandler<DocumentRemovedFromIndexEvent>
{
    private readonly IQueryResultCache _queryCache;
    private readonly IContextExpansionCache _contextCache;
    private readonly ILogger<CacheInvalidationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CacheInvalidationHandler"/>.
    /// </summary>
    /// <param name="queryCache">The query result cache to invalidate.</param>
    /// <param name="contextCache">The context expansion cache to invalidate.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public CacheInvalidationHandler(
        IQueryResultCache queryCache,
        IContextExpansionCache contextCache,
        ILogger<CacheInvalidationHandler> logger)
    {
        _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        _contextCache = contextCache ?? throw new ArgumentNullException(nameof(contextCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("CacheInvalidationHandler initialized");
    }

    /// <summary>
    /// Handles document indexed events by invalidating cache entries.
    /// </summary>
    /// <param name="notification">The document indexed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task Handle(DocumentIndexedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogDebug(
            "Handling DocumentIndexedEvent for document {DocumentId} ({FilePath})",
            notification.DocumentId, notification.FilePath);

        try
        {
            _queryCache.InvalidateForDocument(notification.DocumentId);
            _contextCache.InvalidateForDocument(notification.DocumentId);

            _logger.LogInformation(
                "Cache invalidation completed for re-indexed document {DocumentId}",
                notification.DocumentId);
        }
        catch (Exception ex)
        {
            // LOGIC: Log but don't throw - cache invalidation failure shouldn't break indexing
            _logger.LogWarning(
                ex,
                "Failed to invalidate caches for document {DocumentId}",
                notification.DocumentId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles document removed events by invalidating cache entries.
    /// </summary>
    /// <param name="notification">The document removed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task Handle(DocumentRemovedFromIndexEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogDebug(
            "Handling DocumentRemovedFromIndexEvent for document {DocumentId}",
            notification.DocumentId);

        try
        {
            _queryCache.InvalidateForDocument(notification.DocumentId);
            _contextCache.InvalidateForDocument(notification.DocumentId);

            _logger.LogInformation(
                "Cache invalidation completed for removed document {DocumentId}",
                notification.DocumentId);
        }
        catch (Exception ex)
        {
            // LOGIC: Log but don't throw - cache invalidation failure shouldn't break removal
            _logger.LogWarning(
                ex,
                "Failed to invalidate caches for removed document {DocumentId}",
                notification.DocumentId);
        }

        return Task.CompletedTask;
    }
}
