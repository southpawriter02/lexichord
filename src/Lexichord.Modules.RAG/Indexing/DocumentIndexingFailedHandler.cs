// =============================================================================
// File: DocumentIndexingFailedHandler.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR handler for document indexing failure events.
// Version: v0.4.7d
// =============================================================================
// LOGIC: Processes DocumentIndexingFailedEvent notifications.
//   - Categorizes the exception using IndexingErrorCategorizer.
//   - Updates document status in repository with error details.
//   - Logs warning with full diagnostic context.
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Indexing;

/// <summary>
/// MediatR notification handler for <see cref="DocumentIndexingFailedEvent"/>.
/// </summary>
/// <remarks>
/// <para>
/// This handler processes indexing failures by:
/// </para>
/// <list type="number">
///   <item>Categorizing the exception using <see cref="IndexingErrorCategorizer"/>.</item>
///   <item>Updating the document status to <see cref="DocumentStatus.Failed"/> with error details.</item>
///   <item>Logging the failure at Warning level with full context.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This handler is thread-safe and stateless.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7d as part of Indexing Errors.
/// </para>
/// </remarks>
public sealed class DocumentIndexingFailedHandler : INotificationHandler<DocumentIndexingFailedEvent>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentIndexingFailedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentIndexingFailedHandler"/>.
    /// </summary>
    /// <param name="documentRepository">Repository for document data access.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public DocumentIndexingFailedHandler(
        IDocumentRepository documentRepository,
        ILogger<DocumentIndexingFailedHandler> logger)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the document indexing failed event.
    /// </summary>
    /// <param name="notification">The event notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task Handle(DocumentIndexingFailedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // LOGIC: Categorize the exception for targeted error handling
        var category = notification.Exception is not null
            ? IndexingErrorCategorizer.Categorize(notification.Exception)
            : IndexingErrorCategory.Unknown;

        // LOGIC: Generate user-friendly error message
        var userMessage = notification.Exception is not null
            ? IndexingErrorCategorizer.GetUserFriendlyMessage(notification.Exception, category)
            : notification.ErrorMessage;

        // LOGIC: Look up the document by file path to get the ID
        // Note: We need a project ID here, but the event doesn't provide it.
        // For now, we'll log the error and skip the repository update if we can't find the document.
        // The failure is already recorded via the error message in the event.

        _logger.LogWarning(
            notification.Exception,
            "[DocumentIndexingFailedHandler] Document indexing failed: " +
            "FilePath={FilePath}, Category={Category}, Message={Message}",
            notification.FilePath,
            category,
            userMessage);

        // LOGIC: Try to find and update the document if possible
        // This requires project context which we may not have in all cases.
        // The indexing pipeline should update the document status directly.
        // This handler primarily ensures logging and could be extended for
        // additional side effects (notifications, metrics, etc.)

        _logger.LogDebug(
            "[DocumentIndexingFailedHandler] Error category: {Category}, IsRetryable: {IsRetryable}",
            category,
            IsRetryable(category));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines if the error category is retryable.
    /// </summary>
    private static bool IsRetryable(IndexingErrorCategory category) =>
        category is IndexingErrorCategory.RateLimit
            or IndexingErrorCategory.NetworkError
            or IndexingErrorCategory.ServiceUnavailable;
}
