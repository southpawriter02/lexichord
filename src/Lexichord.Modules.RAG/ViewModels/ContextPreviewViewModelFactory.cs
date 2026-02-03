// =============================================================================
// File: ContextPreviewViewModelFactory.cs
// Project: Lexichord.Modules.RAG
// Description: Factory implementation for creating ContextPreviewViewModel instances.
// =============================================================================
// LOGIC: Creates ContextPreviewViewModel instances with injected dependencies.
//   - Converts SearchHit (TextChunk + Document) to RAG Chunk for service compatibility.
//   - Injects IContextExpansionService, ILicenseContext, and ILogger.
//   - Chunk ID is generated as deterministic GUID from DocumentId + ChunkIndex.
// =============================================================================
// VERSION: v0.5.3d (Context Preview UI)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// Factory for creating <see cref="ContextPreviewViewModel"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This factory encapsulates the dependency resolution and data transformation
/// required to create <see cref="ContextPreviewViewModel"/> instances from
/// <see cref="SearchHit"/> data.
/// </para>
/// <para>
/// <b>Data Transformation:</b> The factory converts the <see cref="TextChunk"/>
/// contained in <see cref="SearchHit"/> into a RAG <see cref="Chunk"/> that is
/// compatible with <see cref="IContextExpansionService"/>. The transformation
/// extracts:
/// <list type="bullet">
///   <item><description>Content from <see cref="TextChunk.Content"/></description></item>
///   <item><description>Offsets from <see cref="TextChunk.StartOffset"/> and <see cref="TextChunk.EndOffset"/></description></item>
///   <item><description>Chunk index from <see cref="ChunkMetadata.Index"/></description></item>
///   <item><description>Heading metadata from <see cref="ChunkMetadata.Heading"/> and <see cref="ChunkMetadata.Level"/></description></item>
///   <item><description>Document ID from <see cref="Document.Id"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Chunk ID Generation:</b> Since <see cref="TextChunk"/> does not carry a
/// database ID, the factory generates a deterministic GUID by combining the
/// document ID and chunk index. This ensures consistent caching in
/// <see cref="IContextExpansionService"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3d as part of The Context Window feature.
/// </para>
/// </remarks>
public sealed class ContextPreviewViewModelFactory : IContextPreviewViewModelFactory
{
    private readonly IContextExpansionService _contextExpansionService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ContextPreviewViewModelFactory> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ContextPreviewViewModelFactory"/>.
    /// </summary>
    /// <param name="contextExpansionService">
    /// Service for retrieving surrounding context and heading breadcrumbs.
    /// </param>
    /// <param name="licenseContext">
    /// License context for checking Writer Pro feature access.
    /// </param>
    /// <param name="loggerFactory">
    /// Logger factory for creating ViewModel loggers.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public ContextPreviewViewModelFactory(
        IContextExpansionService contextExpansionService,
        ILicenseContext licenseContext,
        ILoggerFactory loggerFactory)
    {
        _contextExpansionService = contextExpansionService ?? throw new ArgumentNullException(nameof(contextExpansionService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<ContextPreviewViewModelFactory>();

        _logger.LogDebug("[ContextPreviewViewModelFactory] Factory initialized");
    }

    /// <inheritdoc />
    public ContextPreviewViewModel Create(SearchHit searchHit)
    {
        ArgumentNullException.ThrowIfNull(searchHit);

        var textChunk = searchHit.Chunk;
        var document = searchHit.Document;

        _logger.LogDebug(
            "[ContextPreviewViewModelFactory] Creating ContextPreviewViewModel for document {DocumentId}, chunk index {ChunkIndex}",
            document.Id,
            textChunk.Metadata.Index);

        // Convert TextChunk to RAG Chunk
        var chunk = ConvertToRagChunk(textChunk, document.Id);

        // Create the ViewModel with injected dependencies
        var viewModelLogger = _loggerFactory.CreateLogger<ContextPreviewViewModel>();

        return new ContextPreviewViewModel(
            chunk,
            _contextExpansionService,
            _licenseContext,
            viewModelLogger);
    }

    /// <summary>
    /// Converts a <see cref="TextChunk"/> to a RAG <see cref="Chunk"/>.
    /// </summary>
    /// <param name="textChunk">The source text chunk from search results.</param>
    /// <param name="documentId">The parent document's identifier.</param>
    /// <returns>A RAG <see cref="Chunk"/> compatible with <see cref="IContextExpansionService"/>.</returns>
    /// <remarks>
    /// <para>
    /// <b>ID Generation:</b> The chunk ID is generated deterministically from the
    /// document ID and chunk index using <see cref="GenerateDeterministicChunkId"/>.
    /// This ensures that the same search result always produces the same chunk ID,
    /// enabling consistent caching in the expansion service.
    /// </para>
    /// <para>
    /// <b>Embedding:</b> The embedding array is set to <c>null</c> since context
    /// expansion does not require embedding data.
    /// </para>
    /// </remarks>
    private static Chunk ConvertToRagChunk(TextChunk textChunk, Guid documentId)
    {
        var metadata = textChunk.Metadata;

        // Generate deterministic ID for consistent caching
        var chunkId = GenerateDeterministicChunkId(documentId, metadata.Index);

        return new Chunk(
            Id: chunkId,
            DocumentId: documentId,
            Content: textChunk.Content,
            Embedding: null,
            ChunkIndex: metadata.Index,
            StartOffset: textChunk.StartOffset,
            EndOffset: textChunk.EndOffset,
            Heading: metadata.Heading,
            HeadingLevel: metadata.Level);
    }

    /// <summary>
    /// Generates a deterministic GUID from document ID and chunk index.
    /// </summary>
    /// <param name="documentId">The document's identifier.</param>
    /// <param name="chunkIndex">The zero-based chunk index.</param>
    /// <returns>A deterministic GUID that is consistent for the same inputs.</returns>
    /// <remarks>
    /// <para>
    /// The algorithm creates a version 5 UUID-style hash by combining the
    /// document ID bytes with the chunk index. This ensures:
    /// <list type="bullet">
    ///   <item><description>Same document + index always produces the same ID.</description></item>
    ///   <item><description>Different documents or indices produce different IDs.</description></item>
    ///   <item><description>No collisions within a document (indices are unique).</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static Guid GenerateDeterministicChunkId(Guid documentId, int chunkIndex)
    {
        // Combine document ID bytes with chunk index for deterministic hashing
        var docBytes = documentId.ToByteArray();
        var indexBytes = BitConverter.GetBytes(chunkIndex);

        // XOR the first 4 bytes of the document ID with the index
        // This preserves the document relationship while incorporating the index
        var resultBytes = new byte[16];
        Array.Copy(docBytes, resultBytes, 16);

        resultBytes[0] ^= indexBytes[0];
        resultBytes[1] ^= indexBytes[1];
        resultBytes[2] ^= indexBytes[2];
        resultBytes[3] ^= indexBytes[3];

        // Also incorporate index into bytes 12-15 for additional uniqueness
        resultBytes[12] ^= indexBytes[0];
        resultBytes[13] ^= indexBytes[1];
        resultBytes[14] ^= indexBytes[2];
        resultBytes[15] ^= indexBytes[3];

        return new Guid(resultBytes);
    }
}
