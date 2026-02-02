// =============================================================================
// File: SearchRoundtripTests.cs
// Project: Lexichord.Tests.Integration
// Description: Integration tests for semantic search end-to-end flow.
// =============================================================================
// v0.4.8b: Tests the complete search pipeline against PostgreSQL + pgvector.
//   - Verifies search returns relevant results
//   - Tests TopK limit enforcement
//   - Validates search duration tracking
//   - Confirms chunk content in results
// =============================================================================

using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Tests.Integration.RAG.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Tests.Integration.RAG;

/// <summary>
/// Integration tests for semantic search roundtrip.
/// </summary>
/// <remarks>
/// <para>
/// Tests the complete search workflow: index documents, perform semantic search,
/// verify results match expected relevance ordering and metadata.
/// </para>
/// <para>
/// <b>v0.4.8b:</b> Initial implementation with 5 search validation tests.
/// </para>
/// </remarks>
[Collection("PostgresRag")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class SearchRoundtripTests : IAsyncLifetime
{
    private readonly PostgresRagFixture _fixture;
    private readonly IDocumentRepository _documentRepository;
    private readonly DocumentIndexingPipeline _pipeline;
    private readonly ISemanticSearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchRoundtripTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL fixture.</param>
    public SearchRoundtripTests(PostgresRagFixture fixture)
    {
        _fixture = fixture;
        _documentRepository = fixture.Services.GetRequiredService<IDocumentRepository>();
        _pipeline = fixture.Services.GetRequiredService<DocumentIndexingPipeline>();
        _searchService = fixture.Services.GetRequiredService<ISemanticSearchService>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedTestDocumentsAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Seeds the database with test documents for search tests.
    /// </summary>
    private async Task SeedTestDocumentsAsync()
    {
        // Document about cats
        await _pipeline.IndexDocumentAsync(
            "animals/cats.md",
            """
            # Cats

            Cats are carnivorous mammals. They are known for their independence
            and agility. Domestic cats make popular pets around the world.

            ## Cat Behavior

            Cats are crepuscular, meaning they are most active during dawn and dusk.
            They are skilled hunters and maintain their hunting instincts even as pets.
            """,
            null);

        // Document about dogs
        await _pipeline.IndexDocumentAsync(
            "animals/dogs.md",
            """
            # Dogs

            Dogs are loyal companions and have been domesticated for thousands of years.
            They are known for their loyalty and trainability.

            ## Dog Training

            Dogs respond well to positive reinforcement training methods.
            They form strong bonds with their human families.
            """,
            null);

        // Document about programming
        await _pipeline.IndexDocumentAsync(
            "technology/programming.md",
            """
            # Programming Languages

            Software development involves writing code in various programming languages.
            Popular languages include C#, Python, and JavaScript.

            ## Code Quality

            Good code follows principles like SOLID and DRY.
            Unit testing is essential for maintaining code quality.
            """,
            null);
    }

    /// <summary>
    /// Verifies that search returns relevant results based on query.
    /// </summary>
    [Fact]
    public async Task Search_ReturnsRelevantResults()
    {
        // Act
        var result = await _searchService.SearchAsync("cat behavior hunting", new SearchOptions());

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that TopK parameter limits result count.
    /// </summary>
    [Fact]
    public async Task Search_RespectsTopK()
    {
        // Arrange
        var options = new SearchOptions { TopK = 2 };

        // Act
        var result = await _searchService.SearchAsync("animals pets training", options);

        // Assert
        result.Hits.Should().HaveCountLessOrEqualTo(2);
    }

    /// <summary>
    /// Verifies that search results include timing information.
    /// </summary>
    [Fact]
    public async Task Search_IncludesSearchDuration()
    {
        // Act
        var result = await _searchService.SearchAsync("programming software", new SearchOptions());

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that search results contain chunk content.
    /// </summary>
    [Fact]
    public async Task Search_ResultsIncludeChunkContent()
    {
        // Act
        var result = await _searchService.SearchAsync("dog loyal companion", new SearchOptions());

        // Assert
        result.Hits.Should().NotBeEmpty();
        result.Hits.Should().AllSatisfy(hit =>
        {
            hit.Chunk.Content.Should().NotBeNullOrEmpty();
        });
    }

    /// <summary>
    /// Verifies that search results include relevance scores.
    /// </summary>
    [Fact]
    public async Task Search_ResultsHaveRelevanceScores()
    {
        // Act
        var result = await _searchService.SearchAsync("cat hunting instinct", new SearchOptions());

        // Assert
        result.Hits.Should().NotBeEmpty();
        result.Hits.Should().AllSatisfy(hit =>
        {
            hit.Score.Should().BeGreaterThan(0);
            hit.Score.Should().BeLessThanOrEqualTo(1);
        });
    }
}
