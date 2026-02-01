// =============================================================================
// File: IngestionPhase.cs
// Project: Lexichord.Abstractions
// Description: Enum defining the phases of the document ingestion pipeline.
// =============================================================================
// LOGIC: Each phase represents a discrete step in the ingestion workflow.
//   - Phases are ordered from initial discovery through final storage.
//   - Used by IngestionProgressEventArgs to report current pipeline position.
//   - Enables granular progress reporting and phase-specific error handling.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Represents the phases of the document ingestion pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The ingestion pipeline processes files through a series of discrete phases,
/// from initial discovery through final storage in the vector database.
/// </para>
/// <para>
/// <b>Phase Flow:</b>
/// </para>
/// <list type="number">
///   <item><description><see cref="Scanning"/>: File/directory enumeration.</description></item>
///   <item><description><see cref="Hashing"/>: Content hash computation for change detection.</description></item>
///   <item><description><see cref="Reading"/>: File content extraction.</description></item>
///   <item><description><see cref="Chunking"/>: Content segmentation into chunks.</description></item>
///   <item><description><see cref="Embedding"/>: Vector embedding generation via LLM API.</description></item>
///   <item><description><see cref="Storing"/>: Persistence to vector database.</description></item>
///   <item><description><see cref="Complete"/>: Terminal state indicating success.</description></item>
/// </list>
/// </remarks>
public enum IngestionPhase
{
    /// <summary>
    /// Scanning file system for eligible files.
    /// </summary>
    /// <remarks>
    /// During this phase, the ingestion service enumerates files in the target
    /// directory, applying extension filters and exclusion patterns from
    /// <see cref="IngestionOptions"/>.
    /// </remarks>
    Scanning = 0,

    /// <summary>
    /// Computing content hashes for change detection.
    /// </summary>
    /// <remarks>
    /// SHA-256 hashes are computed for each file to enable efficient comparison
    /// against previously indexed versions. Files with unchanged hashes may be
    /// skipped to optimize re-indexing operations.
    /// </remarks>
    Hashing = 1,

    /// <summary>
    /// Reading file content from disk.
    /// </summary>
    /// <remarks>
    /// File content is loaded into memory, respecting the
    /// <see cref="IngestionOptions.MaxFileSizeBytes"/> limit. Files exceeding
    /// the limit will be skipped with an appropriate error message.
    /// </remarks>
    Reading = 2,

    /// <summary>
    /// Segmenting content into chunks for embedding.
    /// </summary>
    /// <remarks>
    /// Content is split into semantic chunks suitable for embedding. Chunk size
    /// and overlap are configured by the chunking strategy (defined in v0.4.3).
    /// </remarks>
    Chunking = 3,

    /// <summary>
    /// Generating vector embeddings via LLM API.
    /// </summary>
    /// <remarks>
    /// Each chunk is sent to the embedding provider to generate a vector
    /// representation. This phase may involve network latency and is often
    /// the most time-consuming step in the pipeline.
    /// </remarks>
    Embedding = 4,

    /// <summary>
    /// Persisting documents and chunks to the vector database.
    /// </summary>
    /// <remarks>
    /// Documents and their associated chunks (with embeddings) are written to
    /// PostgreSQL via the <see cref="RAG.IDocumentRepository"/> and
    /// <see cref="RAG.IChunkRepository"/> interfaces.
    /// </remarks>
    Storing = 5,

    /// <summary>
    /// Ingestion completed successfully.
    /// </summary>
    /// <remarks>
    /// This terminal state indicates that all files have been processed
    /// without critical errors. Individual file failures may still be
    /// reported in the <see cref="IngestionResult.Errors"/> collection.
    /// </remarks>
    Complete = 6
}
