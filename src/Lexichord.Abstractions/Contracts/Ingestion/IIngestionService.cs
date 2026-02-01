// =============================================================================
// File: IIngestionService.cs
// Project: Lexichord.Abstractions
// Description: Interface defining the contract for the file ingestion service.
// =============================================================================
// LOGIC: Central abstraction for document ingestion into the RAG pipeline.
//   - IngestFileAsync handles single file processing.
//   - IngestDirectoryAsync handles recursive directory scanning.
//   - ProgressChanged event enables real-time UI feedback.
//   - All methods support cancellation for graceful shutdown.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Defines the contract for the file ingestion service.
/// </summary>
/// <remarks>
/// <para>
/// The ingestion service is responsible for processing files and directories,
/// extracting content, generating embeddings, and storing documents in the
/// vector database for semantic search.
/// </para>
/// <para>
/// <b>Usage:</b> Inject <see cref="IIngestionService"/> via DI and call
/// <see cref="IngestFileAsync"/> for single files or <see cref="IngestDirectoryAsync"/>
/// for entire directories. Subscribe to <see cref="ProgressChanged"/> for
/// real-time progress updates during long-running operations.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. The
/// <see cref="ProgressChanged"/> event may be raised from background threads;
/// subscribers should marshal to the UI thread if necessary.
/// </para>
/// </remarks>
public interface IIngestionService
{
    /// <summary>
    /// Occurs when ingestion progress changes.
    /// </summary>
    /// <remarks>
    /// This event is raised periodically during <see cref="IngestDirectoryAsync"/>
    /// operations to provide progress feedback. For single-file operations,
    /// the event is raised at each phase transition.
    /// </remarks>
    event EventHandler<IngestionProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// Ingests a single file into the RAG system.
    /// </summary>
    /// <param name="projectId">The project to associate the document with.</param>
    /// <param name="filePath">The absolute path to the file to ingest.</param>
    /// <param name="options">
    /// Optional configuration for the ingestion process.
    /// If null, <see cref="IngestionOptions.Default"/> is used.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task containing the <see cref="IngestionResult"/> with the outcome.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="filePath"/> is null, empty, or the file does not exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method processes a single file through the complete ingestion pipeline:
    /// hash computation, content reading, chunking, embedding, and storage.
    /// </para>
    /// <para>
    /// If the file has already been indexed and the content hash matches,
    /// the operation may return early with a skipped result (depending on
    /// implementation configuration).
    /// </para>
    /// </remarks>
    Task<IngestionResult> IngestFileAsync(
        Guid projectId,
        string filePath,
        IngestionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests all eligible files in a directory (recursively).
    /// </summary>
    /// <param name="projectId">The project to associate documents with.</param>
    /// <param name="directoryPath">The absolute path to the directory to scan.</param>
    /// <param name="options">
    /// Optional configuration for the ingestion process.
    /// If null, <see cref="IngestionOptions.Default"/> is used.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task containing the <see cref="IngestionResult"/> with aggregated outcomes.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directoryPath"/> is null, empty, or the
    /// directory does not exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method recursively scans the directory for files matching the
    /// extensions in <see cref="IngestionOptions.SupportedExtensions"/>,
    /// excluding directories listed in <see cref="IngestionOptions.ExcludedDirectories"/>.
    /// </para>
    /// <para>
    /// Files are processed in parallel up to <see cref="IngestionOptions.MaxConcurrency"/>.
    /// The <see cref="ProgressChanged"/> event is raised as each file completes.
    /// </para>
    /// </remarks>
    Task<IngestionResult> IngestDirectoryAsync(
        Guid projectId,
        string directoryPath,
        IngestionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document and all its chunks from the RAG system.
    /// </summary>
    /// <param name="documentId">The ID of the document to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This operation cascades to delete all associated <see cref="RAG.Chunk"/>
    /// records. If the document does not exist, the operation completes silently.
    /// </remarks>
    Task RemoveDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
