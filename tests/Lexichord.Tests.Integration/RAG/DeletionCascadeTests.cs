// =============================================================================
// File: DeletionCascadeTests.cs
// Project: Lexichord.Tests.Integration
// Description: Integration tests for cascade delete behavior.
// =============================================================================
// v0.4.8b: Tests that document deletion properly cascades to chunks.
// =============================================================================

using Dapper;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Tests.Integration.RAG.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Tests.Integration.RAG;

/// <summary>
/// Integration tests for cascade delete behavior.
/// </summary>
/// <remarks>
/// <para>
/// Tests that the database foreign key constraints properly cascade document
/// deletions to their associated chunks.
/// </para>
/// <para>
/// <b>v0.4.8b:</b> Initial implementation with cascade delete verification.
/// </para>
/// </remarks>
[Collection("PostgresRag")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class DeletionCascadeTests : IAsyncLifetime
{
    private readonly PostgresRagFixture _fixture;
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly DocumentIndexingPipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeletionCascadeTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL fixture.</param>
    public DeletionCascadeTests(PostgresRagFixture fixture)
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
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Verifies that deleting a document cascades to delete all its chunks.
    /// </summary>
    [Fact]
    public async Task RemoveDocument_DeletesChunks()
    {
        // Arrange
        var filePath = "cascade/document-to-delete.md";
        var content = """
            # Document for Deletion Test
            
            This document will be deleted to verify cascade behavior.
            It needs enough content to generate multiple chunks.
            
            ## First Section
            
            Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            
            ## Second Section
            
            Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            """;

        await _pipeline.IndexDocumentAsync(filePath, content, null);

        var documents = await _documentRepository.GetByProjectAsync(Guid.Empty);
        var doc = documents.First(d => d.FilePath == filePath);
        var docId = doc.Id;

        // Verify chunks exist before deletion
        var chunksBefore = (await _chunkRepository.GetByDocumentIdAsync(docId)).ToList();
        chunksBefore.Should().NotBeEmpty("document should have chunks before deletion");

        // Act - Delete the document
        await _documentRepository.DeleteAsync(docId);

        // Assert - Chunks should be deleted via cascade
        var chunksAfter = (await _chunkRepository.GetByDocumentIdAsync(docId)).ToList();
        chunksAfter.Should().BeEmpty("chunks should be deleted when document is deleted");

        // Also verify via direct SQL
        await using var connection = await _fixture.CreateConnectionAsync();
        var chunkCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM \"Chunks\" WHERE \"DocumentId\" = @DocId",
            new { DocId = docId });
        chunkCount.Should().Be(0, "no chunks should remain in database");
    }
}
