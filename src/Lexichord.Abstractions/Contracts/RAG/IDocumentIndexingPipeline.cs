// =============================================================================
// File: IDocumentIndexingPipeline.cs
// Project: Lexichord.Abstractions
// Description: Interface for the document indexing pipeline.
// Version: v0.4.7b
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines the contract for a document indexing pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are responsible for orchestrating the complete document
/// indexing workflow: chunking, embedding, and storing indexed content.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7b to enable testing of <c>IndexManagementService</c>.
/// </para>
/// </remarks>
public interface IDocumentIndexingPipeline
{
    /// <summary>
    /// Indexes a document by chunking, generating embeddings, and storing chunks.
    /// </summary>
    /// <param name="filePath">The relative file path of the document within the project.</param>
    /// <param name="content">The full text content of the document.</param>
    /// <param name="chunkingMode">
    /// Optional mode specifying which chunking strategy to use.
    /// When null, the default strategy is selected based on file extension.
    /// </param>
    /// <param name="ct">Cancellation token to cancel the indexing operation.</param>
    /// <returns>
    /// An <see cref="IndexingResult"/> indicating success or failure with details.
    /// </returns>
    Task<IndexingResult> IndexDocumentAsync(
        string filePath,
        string content,
        ChunkingMode? chunkingMode = null,
        CancellationToken ct = default);
}
