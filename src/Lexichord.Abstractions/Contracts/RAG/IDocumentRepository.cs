// =============================================================================
// File: IDocumentRepository.cs
// Project: Lexichord.Abstractions
// Description: Repository interface for managing indexed documents.
// =============================================================================
// LOGIC: Defines the contract for document CRUD operations in the RAG system.
//   - All methods are async for database I/O.
//   - CancellationToken support enables graceful shutdown.
//   - Implementations will use Dapper or EF Core against PostgreSQL.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Repository interface for managing indexed documents in the RAG system.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the data access contract for <see cref="Document"/> entities.
/// Implementations are responsible for persisting documents to the PostgreSQL database
/// and managing the document lifecycle states.
/// </para>
/// <para>
/// All methods accept a <see cref="CancellationToken"/> to support graceful cancellation
/// during application shutdown or user-initiated cancellation of long-running operations.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations should be thread-safe and suitable for use
/// in concurrent scenarios (e.g., multiple file watchers triggering simultaneous updates).
/// </para>
/// </remarks>
public interface IDocumentRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves a document by its unique identifier.
    /// </summary>
    /// <param name="id">The document's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="Document"/> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This is the primary lookup method for retrieving a specific document
    /// when the ID is known (e.g., from a chunk's <see cref="Chunk.DocumentId"/>).
    /// </remarks>
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all documents belonging to a specific project.
    /// </summary>
    /// <param name="projectId">The project's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A collection of documents in the specified project. May be empty if no
    /// documents have been indexed for the project.
    /// </returns>
    /// <remarks>
    /// Use this method to display the indexing status of all files in a project.
    /// Results are not guaranteed to be in any particular order.
    /// </remarks>
    Task<IEnumerable<Document>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a document by its file path within a project.
    /// </summary>
    /// <param name="projectId">The project's unique identifier.</param>
    /// <param name="filePath">The relative path to the file within the project.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="Document"/> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method is used by the file watcher to look up documents when file
    /// changes are detected. The combination of <paramref name="projectId"/>
    /// and <paramref name="filePath"/> forms a unique key.
    /// </remarks>
    Task<Document?> GetByFilePathAsync(Guid projectId, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all documents with a specific status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A collection of documents with the specified status. May be empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Common use cases:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="DocumentStatus.Pending"/>: Find documents queued for indexing.
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="DocumentStatus.Failed"/>: Find documents that need retry.
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="DocumentStatus.Stale"/>: Find documents that need re-indexing.
    ///   </description></item>
    /// </list>
    /// </remarks>
    Task<IEnumerable<Document>> GetByStatusAsync(DocumentStatus status, CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Adds a new document to the repository.
    /// </summary>
    /// <param name="document">The document to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The added document with its assigned <see cref="Document.Id"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// The document should be created via <see cref="Document.CreatePending"/> to ensure
    /// correct initial state. The returned document will have a database-assigned ID.
    /// </remarks>
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document in the repository.
    /// </summary>
    /// <param name="document">The document with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// This method performs a full update of all document properties. For status-only
    /// updates, prefer <see cref="UpdateStatusAsync"/> for better performance.
    /// </remarks>
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status (and optionally failure reason) of a document.
    /// </summary>
    /// <param name="id">The document's unique identifier.</param>
    /// <param name="status">The new status to set.</param>
    /// <param name="failureReason">
    /// Optional failure reason. Should be provided when <paramref name="status"/>
    /// is <see cref="DocumentStatus.Failed"/>.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This is an optimized method for status transitions that avoids loading
    /// and saving the full document record. When transitioning to
    /// <see cref="DocumentStatus.Indexed"/>, the <see cref="Document.IndexedAt"/>
    /// timestamp should also be updated by the implementation.
    /// </para>
    /// </remarks>
    Task UpdateStatusAsync(
        Guid id,
        DocumentStatus status,
        string? failureReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document and all its associated chunks from the repository.
    /// </summary>
    /// <param name="id">The document's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This operation cascades to delete all <see cref="Chunk"/> records associated
    /// with the document (enforced by the database foreign key constraint).
    /// </remarks>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion
}
