// =============================================================================
// File: IndexManagementEvents.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR notification events for manual index management operations.
// Version: v0.4.7b
// =============================================================================
// LOGIC: Three events covering manual index management outcomes for telemetry.
//   - DocumentReindexedEvent: Published after successful single document re-index.
//   - DocumentRemovedFromIndexEvent: Published after document removal.
//   - AllDocumentsReindexedEvent: Published after bulk re-index operation.
//   - All implement INotification for MediatR pub/sub pattern.
// =============================================================================

using MediatR;

namespace Lexichord.Modules.RAG.Indexing;

/// <summary>
/// Published when a document has been successfully re-indexed via manual operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DocumentReindexedEvent"/> is a MediatR notification published
/// after a document has been successfully re-indexed through the manual indexing
/// controls in the Settings UI.
/// </para>
/// <para>
/// <b>Usage:</b> Handlers can subscribe to this event to:
/// </para>
/// <list type="bullet">
///   <item><description>Log re-indexing metrics for performance analysis.</description></item>
///   <item><description>Update UI to reflect re-indexing completion.</description></item>
///   <item><description>Track user-initiated index maintenance operations.</description></item>
///   <item><description>Collect telemetry for monitoring.</description></item>
/// </list>
/// <para>
/// <b>Differentiation:</b> This event differs from <see cref="DocumentIndexedEvent"/>
/// in that it specifically indicates a user-initiated re-index operation rather than
/// an automatic indexing triggered by file changes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7b as part of Manual Indexing Controls.
/// </para>
/// </remarks>
/// <param name="DocumentId">
/// The unique identifier of the re-indexed document.
/// </param>
/// <param name="FilePath">
/// The relative file path of the re-indexed document.
/// </param>
/// <param name="ChunkCount">
/// The number of chunks created during re-indexing.
/// </param>
/// <param name="Duration">
/// The total time spent on the re-indexing operation.
/// </param>
public record DocumentReindexedEvent(
    Guid DocumentId,
    string FilePath,
    int ChunkCount,
    TimeSpan Duration) : INotification;

/// <summary>
/// Published when a document has been removed from the index via manual operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DocumentRemovedFromIndexEvent"/> is a MediatR notification published
/// after a document and its chunks have been successfully removed from the index
/// through the manual indexing controls in the Settings UI.
/// </para>
/// <para>
/// <b>Usage:</b> Handlers can subscribe to this event to:
/// </para>
/// <list type="bullet">
///   <item><description>Log removal operations for auditing.</description></item>
///   <item><description>Update UI to reflect document removal.</description></item>
///   <item><description>Track user-initiated index cleanup operations.</description></item>
///   <item><description>Collect telemetry for monitoring.</description></item>
/// </list>
/// <para>
/// <b>Note:</b> This event indicates the document was removed from the index only.
/// The original file on disk is NOT affected by this operation.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7b as part of Manual Indexing Controls.
/// </para>
/// </remarks>
/// <param name="DocumentId">
/// The unique identifier of the removed document.
/// </param>
/// <param name="FilePath">
/// The relative file path of the removed document.
/// </param>
public record DocumentRemovedFromIndexEvent(
    Guid DocumentId,
    string FilePath) : INotification;

/// <summary>
/// Published when a bulk re-index operation of all documents has completed.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AllDocumentsReindexedEvent"/> is a MediatR notification published
/// after a bulk re-index operation (re-index all documents) has completed through
/// the manual indexing controls in the Settings UI.
/// </para>
/// <para>
/// <b>Usage:</b> Handlers can subscribe to this event to:
/// </para>
/// <list type="bullet">
///   <item><description>Log bulk operation metrics for performance analysis.</description></item>
///   <item><description>Update UI to show completion status with statistics.</description></item>
///   <item><description>Track corpus-wide maintenance operations.</description></item>
///   <item><description>Alert on partial failures.</description></item>
/// </list>
/// <para>
/// <b>Partial Failures:</b> If some documents failed to re-index, both
/// <see cref="SuccessCount"/> and <see cref="FailedCount"/> will be non-zero.
/// Handlers should check <see cref="FailedCount"/> to determine if user attention
/// is needed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7b as part of Manual Indexing Controls.
/// </para>
/// </remarks>
/// <param name="SuccessCount">
/// The number of documents successfully re-indexed.
/// </param>
/// <param name="FailedCount">
/// The number of documents that failed to re-index.
/// </param>
/// <param name="TotalDuration">
/// The total time spent on the bulk re-indexing operation.
/// </param>
public record AllDocumentsReindexedEvent(
    int SuccessCount,
    int FailedCount,
    TimeSpan TotalDuration) : INotification;
