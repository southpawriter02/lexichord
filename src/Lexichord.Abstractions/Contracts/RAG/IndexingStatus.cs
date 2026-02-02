// =============================================================================
// File: IndexingStatus.cs
// Project: Lexichord.Abstractions
// Description: Enum defining document indexing states for the Index Manager.
// Version: v0.4.7a
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines the indexing states for a document in the RAG system.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexingStatus"/> represents the lifecycle states of a document
/// within the indexing pipeline. States transition as documents are discovered,
/// processed, and maintained.
/// </para>
/// <para>
/// <b>State Transitions:</b>
/// <list type="bullet">
/// <item><c>NotIndexed → Pending</c>: Document queued for indexing</item>
/// <item><c>Pending → Indexing</c>: Indexing operation started</item>
/// <item><c>Indexing → Indexed</c>: Indexing completed successfully</item>
/// <item><c>Indexing → Failed</c>: Indexing encountered an error</item>
/// <item><c>Indexed → Stale</c>: File hash changed since last index</item>
/// <item><c>Stale → Pending</c>: Re-indexing queued</item>
/// <item><c>Failed → Pending</c>: Retry requested</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public enum IndexingStatus
{
    /// <summary>
    /// Document has not been indexed yet.
    /// </summary>
    /// <remarks>
    /// Initial state for newly discovered documents that have not
    /// been queued for indexing.
    /// </remarks>
    NotIndexed = 0,

    /// <summary>
    /// Document is queued and waiting to be indexed.
    /// </summary>
    /// <remarks>
    /// The document has been added to the ingestion queue but
    /// processing has not yet started.
    /// </remarks>
    Pending = 1,

    /// <summary>
    /// Document is currently being indexed.
    /// </summary>
    /// <remarks>
    /// Active indexing operation in progress. The document is being
    /// chunked, embedded, and stored in the vector database.
    /// </remarks>
    Indexing = 2,

    /// <summary>
    /// Document has been successfully indexed.
    /// </summary>
    /// <remarks>
    /// The document's chunks and embeddings are available for
    /// semantic search.
    /// </remarks>
    Indexed = 3,

    /// <summary>
    /// Document content has changed since last indexing.
    /// </summary>
    /// <remarks>
    /// File hash comparison detected modifications. The document
    /// should be re-indexed to update the vector representations.
    /// </remarks>
    Stale = 4,

    /// <summary>
    /// Document indexing failed due to an error.
    /// </summary>
    /// <remarks>
    /// An error occurred during indexing (e.g., file access, embedding
    /// API failure). Check <see cref="IndexedDocumentInfo.ErrorMessage"/>
    /// for details.
    /// </remarks>
    Failed = 5
}
