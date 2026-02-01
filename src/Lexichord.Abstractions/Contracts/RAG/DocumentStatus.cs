// =============================================================================
// File: DocumentStatus.cs
// Project: Lexichord.Abstractions
// Description: Defines the lifecycle states for indexed documents in the RAG system.
// =============================================================================
// LOGIC: This enum represents the finite state machine for document indexing:
//   Pending → Indexing → Indexed (success) or Failed (error)
//   Indexed → Stale (when source file changes)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the lifecycle status of a document in the RAG indexing system.
/// </summary>
/// <remarks>
/// <para>
/// Documents progress through a well-defined state machine during the indexing process.
/// The typical flow is: <c>Pending → Indexing → Indexed</c>. If an error occurs during
/// indexing (e.g., embedding API failure), the document transitions to <c>Failed</c>.
/// </para>
/// <para>
/// After successful indexing, if the source file is modified (detected via hash comparison),
/// the document transitions to <c>Stale</c> and must be re-indexed.
/// </para>
/// </remarks>
public enum DocumentStatus
{
    /// <summary>
    /// The document has been discovered but indexing has not yet started.
    /// </summary>
    /// <remarks>
    /// Documents enter this state when first added to the repository via
    /// <see cref="IDocumentRepository.AddAsync"/>. They remain in this state
    /// until the indexing service picks them up for processing.
    /// </remarks>
    Pending,

    /// <summary>
    /// The document is currently being processed by the indexing service.
    /// </summary>
    /// <remarks>
    /// This state indicates that chunking and embedding generation are in progress.
    /// Documents should not remain in this state for extended periods; if they do,
    /// it may indicate a crashed indexing job that needs recovery.
    /// </remarks>
    Indexing,

    /// <summary>
    /// The document has been successfully indexed and its chunks are searchable.
    /// </summary>
    /// <remarks>
    /// This is the terminal success state. The document's chunks have been stored
    /// with their embedding vectors and are available for similarity search via
    /// <see cref="IChunkRepository.SearchSimilarAsync"/>.
    /// </remarks>
    Indexed,

    /// <summary>
    /// The indexing process failed due to an error.
    /// </summary>
    /// <remarks>
    /// When a document enters this state, the <see cref="Document.FailureReason"/>
    /// property should contain details about the failure (e.g., API timeout,
    /// embedding service unavailable, content too large). Failed documents can
    /// be retried by resetting their status to <see cref="Pending"/>.
    /// </remarks>
    Failed,

    /// <summary>
    /// The source file has changed since the document was last indexed.
    /// </summary>
    /// <remarks>
    /// This state is set when the file watcher detects a modification and the
    /// new file hash differs from the stored hash. Stale documents should be
    /// re-indexed to ensure search results reflect current content.
    /// </remarks>
    Stale
}
