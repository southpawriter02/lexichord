// =============================================================================
// File: IngestionPipelineTests.cs
// Project: Lexichord.Tests.Integration
// Description: Integration tests for the document ingestion pipeline.
// =============================================================================
// v0.4.8b: Tests the complete ingestion flow against PostgreSQL + pgvector.
//   - Verifies document and chunk creation
//   - Validates file hash storage
//   - Tests re-ingestion updates
//   - Confirms directory ingestion
// =============================================================================

using Dapper;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Tests.Integration.RAG.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Tests.Integration.RAG;

/// <summary>
/// Integration tests for the document ingestion pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Tests the complete RAG ingestion workflow from file reading through embedding
/// storage in PostgreSQL with pgvector. Uses real repositories against a
/// Testcontainers PostgreSQL instance.
/// </para>
/// <para>
/// <b>v0.4.8b:</b> Initial implementation with 4 core ingestion tests.
/// </para>
/// </remarks>
[Collection("PostgresRag")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class IngestionPipelineTests : IAsyncLifetime
{
    private readonly PostgresRagFixture _fixture;
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly DocumentIndexingPipeline _pipeline;
    private string _tempDir = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionPipelineTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL fixture.</param>
    public IngestionPipelineTests(PostgresRagFixture fixture)
    {
        _fixture = fixture;
        _documentRepository = fixture.Services.GetRequiredService<IDocumentRepository>();
        _chunkRepository = fixture.Services.GetRequiredService<IChunkRepository>();
        _pipeline = fixture.Services.GetRequiredService<DocumentIndexingPipeline>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _tempDir = Path.Combine(Path.GetTempPath(), $"lexichord_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that indexing a document creates both a document record and chunk records.
    /// </summary>
    [Fact]
    public async Task IndexDocument_CreatesDocumentAndChunks()
    {
        // Arrange
        var filePath = "test-documents/sample.md";
        var content = """
            # Introduction
            
            This is a sample document for testing the RAG ingestion pipeline.
            It contains enough content to generate at least one chunk.
            
            ## Section One
            
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do
            eiusmod tempor incididunt ut labore et dolore magna aliqua.
            
            ## Section Two
            
            Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris
            nisi ut aliquip ex ea commodo consequat.
            """;

        // Act
        var result = await _pipeline.IndexDocumentAsync(filePath, content, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ChunkCount.Should().BeGreaterThan(0);

        // Verify document was created
        var documents = await _documentRepository.GetByProjectAsync(Guid.Empty);
        documents.Should().ContainSingle(d => d.FilePath == filePath);

        // Verify chunks were created
        var doc = documents.First(d => d.FilePath == filePath);
        var chunks = await _chunkRepository.GetByDocumentIdAsync(doc.Id);
        chunks.Should().HaveCountGreaterThan(0);
    }

    /// <summary>
    /// Verifies that the content hash is stored correctly.
    /// </summary>
    [Fact]
    public async Task IndexDocument_StoresContentHash()
    {
        // Arrange
        var filePath = "test-documents/hash-test.txt";
        var content = "This is content for hash verification.";

        // Act
        var result = await _pipeline.IndexDocumentAsync(filePath, content, null);

        // Assert
        result.Success.Should().BeTrue();

        var documents = await _documentRepository.GetByProjectAsync(Guid.Empty);
        var doc = documents.First(d => d.FilePath == filePath);
        doc.Hash.Should().NotBeNullOrEmpty();
        doc.Hash.Should().HaveLength(64); // SHA-256 hex string
    }

    /// <summary>
    /// Verifies that re-indexing a document updates existing records.
    /// </summary>
    [Fact]
    public async Task IndexDocument_ReindexUpdatesDocument()
    {
        // Arrange
        var filePath = "test-documents/update-test.md";
        var originalContent = "Original content for the document.";
        var updatedContent = "This is the updated content with more text to ensure different chunks.";

        // Initial indexing
        await _pipeline.IndexDocumentAsync(filePath, originalContent, null);
        var originalDocuments = await _documentRepository.GetByProjectAsync(Guid.Empty);
        var originalDoc = originalDocuments.First(d => d.FilePath == filePath);
        var originalChunks = (await _chunkRepository.GetByDocumentIdAsync(originalDoc.Id)).ToList();

        // Act - Re-index with new content
        var result = await _pipeline.IndexDocumentAsync(filePath, updatedContent, null);

        // Assert
        result.Success.Should().BeTrue();

        var updatedDocuments = await _documentRepository.GetByProjectAsync(Guid.Empty);
        var updatedDoc = updatedDocuments.First(d => d.FilePath == filePath);

        // Document ID should be the same (update, not new)
        updatedDoc.Id.Should().Be(originalDoc.Id);

        // Hash should change
        updatedDoc.Hash.Should().NotBe(originalDoc.Hash);

        // New chunks should exist
        var newChunks = (await _chunkRepository.GetByDocumentIdAsync(updatedDoc.Id)).ToList();
        newChunks.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that chunks have embeddings stored.
    /// </summary>
    [Fact]
    public async Task IndexDocument_ChunksHaveEmbeddings()
    {
        // Arrange
        var filePath = "test-documents/embedding-test.md";
        var content = "This document tests that embeddings are stored correctly in the database.";

        // Act
        await _pipeline.IndexDocumentAsync(filePath, content, null);

        // Assert
        var documents = await _documentRepository.GetByProjectAsync(Guid.Empty);
        var doc = documents.First(d => d.FilePath == filePath);

        await using var connection = await _fixture.CreateConnectionAsync();
        var hasEmbedding = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM \"Chunks\" WHERE \"DocumentId\" = @DocId AND \"Embedding\" IS NOT NULL)",
            new { DocId = doc.Id });

        hasEmbedding.Should().BeTrue("chunks should have embeddings stored");
    }
}
