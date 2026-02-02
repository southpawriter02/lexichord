// =============================================================================
// File: ConcurrentAccessTests.cs
// Project: Lexichord.Tests.Integration
// Description: Integration tests for concurrent access scenarios.
// =============================================================================
// v0.4.8b: Tests thread safety of RAG operations.
//   - Verifies parallel ingestion doesn't cause deadlocks
//   - Confirms concurrent search is thread-safe
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Tests.Integration.RAG.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Tests.Integration.RAG;

/// <summary>
/// Integration tests for concurrent access scenarios.
/// </summary>
/// <remarks>
/// <para>
/// Tests thread safety of RAG operations under concurrent load. These tests
/// verify that the repositories and services handle parallel access correctly
/// without deadlocks or data corruption.
/// </para>
/// <para>
/// <b>v0.4.8b:</b> Initial implementation with 2 concurrency tests.
/// </para>
/// </remarks>
[Collection("PostgresRag")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class ConcurrentAccessTests : IAsyncLifetime
{
    private readonly PostgresRagFixture _fixture;
    private readonly DocumentIndexingPipeline _pipeline;
    private readonly ISemanticSearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentAccessTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL fixture.</param>
    public ConcurrentAccessTests(PostgresRagFixture fixture)
    {
        _fixture = fixture;
        _pipeline = fixture.Services.GetRequiredService<DocumentIndexingPipeline>();
        _searchService = fixture.Services.GetRequiredService<ISemanticSearchService>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Verifies that concurrent document indexing doesn't cause deadlocks.
    /// </summary>
    [Fact]
    public async Task ConcurrentIngestion_NoDeadlocks()
    {
        // Arrange
        const int documentCount = 10;
        var indexingTasks = new List<Task<IndexingResult>>();

        // Act - Index multiple documents concurrently
        for (int i = 0; i < documentCount; i++)
        {
            var filePath = $"concurrent/doc-{i}.md";
            var content = $"Document number {i} with unique content for concurrency testing.";
            indexingTasks.Add(_pipeline.IndexDocumentAsync(filePath, content, null));
        }

        // Wait for all with timeout to detect deadlocks
        var completedWithinTimeout = await Task.WhenAll(indexingTasks)
            .WaitAsync(TimeSpan.FromSeconds(60));

        // Assert
        foreach (var result in completedWithinTimeout)
        {
            result.Success.Should().BeTrue("all concurrent indexing operations should succeed");
        }
    }

    /// <summary>
    /// Verifies that concurrent search operations are thread-safe.
    /// </summary>
    [Fact]
    public async Task ConcurrentSearch_ThreadSafe()
    {
        // Arrange - Seed some documents first
        await _pipeline.IndexDocumentAsync(
            "concurrent-search/doc1.md",
            "Content about software development and programming.",
            null);

        await _pipeline.IndexDocumentAsync(
            "concurrent-search/doc2.md",
            "Information about machine learning and artificial intelligence.",
            null);

        // Act - Run multiple concurrent searches
        const int searchCount = 10;
        var searchTasks = new List<Task<SearchResult>>();
        var queries = new[] { "software", "machine learning", "development", "artificial intelligence" };

        for (int i = 0; i < searchCount; i++)
        {
            var query = queries[i % queries.Length];
            searchTasks.Add(_searchService.SearchAsync(query, new SearchOptions()));
        }

        var results = await Task.WhenAll(searchTasks);

        // Assert
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            // All searches should complete without exceptions
        }
    }
}
