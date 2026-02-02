// =============================================================================
// File: ChangeDetectionTests.cs
// Project: Lexichord.Tests.Integration
// Description: Integration tests for document change detection.
// =============================================================================
// v0.4.8b: Tests hash-based change detection for incremental indexing.
//   - Verifies unchanged files are skipped
//   - Confirms changed files trigger re-indexing
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Tests.Integration.RAG.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Tests.Integration.RAG;

/// <summary>
/// Integration tests for document change detection.
/// </summary>
/// <remarks>
/// <para>
/// Tests the hash-based change detection system that enables efficient incremental
/// re-indexing. Documents with unchanged content should be skipped to save
/// embedding generation costs.
/// </para>
/// <para>
/// <b>v0.4.8b:</b> Initial implementation with 2 change detection tests.
/// </para>
/// </remarks>
[Collection("PostgresRag")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class ChangeDetectionTests : IAsyncLifetime
{
    private readonly PostgresRagFixture _fixture;
    private readonly IDocumentRepository _documentRepository;
    private readonly DocumentIndexingPipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeDetectionTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL fixture.</param>
    public ChangeDetectionTests(PostgresRagFixture fixture)
    {
        _fixture = fixture;
        _documentRepository = fixture.Services.GetRequiredService<IDocumentRepository>();
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
    /// Verifies that re-indexing with same content produces same hash.
    /// </summary>
    [Fact]
    public async Task IndexDocument_SameContent_ProducesSameHash()
    {
        // Arrange
        var filePath = "change-detection/stable.md";
        var content = "This content will not change between indexing operations.";

        // Act - Index twice with same content
        await _pipeline.IndexDocumentAsync(filePath, content, null);
        var hashAfterFirst = (await _documentRepository.GetByProjectAsync(Guid.Empty))
            .First(d => d.FilePath == filePath).Hash;

        await _pipeline.IndexDocumentAsync(filePath, content, null);
        var hashAfterSecond = (await _documentRepository.GetByProjectAsync(Guid.Empty))
            .First(d => d.FilePath == filePath).Hash;

        // Assert
        hashAfterFirst.Should().Be(hashAfterSecond, "hash should be identical for unchanged content");
    }

    /// <summary>
    /// Verifies that changed content produces different hash.
    /// </summary>
    [Fact]
    public async Task IndexDocument_ChangedContent_ProducesDifferentHash()
    {
        // Arrange
        var filePath = "change-detection/mutable.md";
        var originalContent = "Original version of the document.";
        var modifiedContent = "Modified version with completely different text.";

        // Act
        await _pipeline.IndexDocumentAsync(filePath, originalContent, null);
        var originalHash = (await _documentRepository.GetByProjectAsync(Guid.Empty))
            .First(d => d.FilePath == filePath).Hash;

        await _pipeline.IndexDocumentAsync(filePath, modifiedContent, null);
        var modifiedHash = (await _documentRepository.GetByProjectAsync(Guid.Empty))
            .First(d => d.FilePath == filePath).Hash;

        // Assert
        originalHash.Should().NotBe(modifiedHash, "hash should differ for changed content");
    }
}
