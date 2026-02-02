// =============================================================================
// File: IndexingEvents.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR notification events for document indexing lifecycle.
// =============================================================================
// LOGIC: Two events covering successful and failed indexing outcomes.
//   - DocumentIndexedEvent: Published after successful indexing.
//   - DocumentIndexingFailedEvent: Published after indexing failure.
//   - Both implement INotification for MediatR pub/sub pattern.
//   - Events enable loose coupling between pipeline and consumers (UI, logging, etc).
// =============================================================================

using MediatR;

namespace Lexichord.Modules.RAG.Indexing;

/// <summary>
/// Published when a document has been successfully indexed.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DocumentIndexedEvent"/> is a MediatR notification published
/// after a document has been successfully processed through the indexing pipeline.
/// It signals completion and provides details about the indexed document.
/// </para>
/// <para>
/// <b>Usage:</b> Handlers can subscribe to this event to:
/// </para>
/// <list type="bullet">
///   <item><description>Update UI to reflect indexing completion.</description></item>
///   <item><description>Log indexing metrics for performance analysis.</description></item>
///   <item><description>Trigger dependent operations (e.g., search index refresh).</description></item>
///   <item><description>Update document status in application state.</description></item>
/// </list>
/// <para>
/// <b>MediatR Pattern:</b> This is a <see cref="INotification"/> (fire-and-forget),
/// not a <see cref="IRequest{TResponse}"/>. Handlers do not return values.
/// </para>
/// </remarks>
/// <param name="DocumentId">
/// The unique identifier of the successfully indexed document.
/// </param>
/// <param name="FilePath">
/// The relative file path of the indexed document.
/// Useful for display and correlation with file system events.
/// </param>
/// <param name="ChunkCount">
/// The number of chunks created from the document content.
/// Indicates the granularity of the index.
/// </param>
/// <param name="Duration">
/// The total time spent on the indexing operation.
/// Useful for performance monitoring and optimization.
/// </param>
public record DocumentIndexedEvent(
    Guid DocumentId,
    string FilePath,
    int ChunkCount,
    TimeSpan Duration) : INotification;

/// <summary>
/// Published when document indexing has failed.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DocumentIndexingFailedEvent"/> is a MediatR notification published
/// when an attempt to index a document fails at any stage of the pipeline (chunking,
/// token validation, embedding, storage, etc.).
/// </para>
/// <para>
/// <b>Usage:</b> Handlers can subscribe to this event to:
/// </para>
/// <list type="bullet">
///   <item><description>Log the failure with diagnostic context.</description></item>
///   <item><description>Update UI to show indexing errors.</description></item>
///   <item><description>Trigger retry logic with backoff.</description></item>
///   <item><description>Alert users to problematic documents.</description></item>
///   <item><description>Collect failure metrics for monitoring.</description></item>
/// </list>
/// <para>
/// <b>Error Categories:</b> The <see cref="ErrorMessage"/> may indicate:
/// </para>
/// <list type="bullet">
///   <item><description>License errors (feature not licensed).</description></item>
///   <item><description>Chunking errors (invalid content structure).</description></item>
///   <item><description>Token errors (truncation or overflow).</description></item>
///   <item><description>Embedding errors (API failures, timeouts).</description></item>
///   <item><description>Storage errors (database failures).</description></item>
/// </list>
/// <para>
/// <b>MediatR Pattern:</b> This is a <see cref="INotification"/> (fire-and-forget),
/// not a <see cref="IRequest{TResponse}"/>. Handlers do not return values.
/// </para>
/// <para>
/// <b>v0.4.7d:</b> Added <see cref="Exception"/> property for error categorization.
/// </para>
/// </remarks>
/// <param name="FilePath">
/// The relative file path of the document that failed to index.
/// Useful for identifying which file had issues.
/// </param>
/// <param name="ErrorMessage">
/// The diagnostic message explaining why indexing failed.
/// Contains actionable information for troubleshooting.
/// </param>
/// <param name="Exception">
/// The exception that caused the failure. Used for error categorization.
/// May be null for legacy callers. Introduced in v0.4.7d.
/// </param>
public record DocumentIndexingFailedEvent(
    string FilePath,
    string ErrorMessage,
    Exception? Exception = null) : INotification;
