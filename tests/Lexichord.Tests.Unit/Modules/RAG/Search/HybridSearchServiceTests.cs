// =============================================================================
// File: HybridSearchServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for HybridSearchService hybrid fusion pipeline.
// =============================================================================
// LOGIC: Verifies the full hybrid search pipeline orchestration:
//   - Constructor null-parameter validation (7 dependencies).
//   - Input validation (query, TopK, MinScore ranges).
//   - License tier gating via SearchLicenseGuard.
//   - Query preprocessing delegation to IQueryPreprocessor.
//   - Parallel execution of BM25 and semantic sub-searches.
//   - Reciprocal Rank Fusion (RRF) algorithm correctness.
//   - Telemetry event publishing (HybridSearchExecutedEvent).
//
//   NOTE: SQL execution and Dapper mapping are NOT unit-tested here because
//   they require a live PostgreSQL database. The sub-search services are mocked
//   to return predetermined SearchResult instances.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Search;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for <see cref="HybridSearchService"/>.
/// Verifies constructor validation, input validation, license gating,
/// preprocessing delegation, parallel execution, RRF algorithm, and telemetry.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.1c")]
public class HybridSearchServiceTests
{
    private readonly Mock<ISemanticSearchService> _semanticSearchMock;
    private readonly Mock<IBM25SearchService> _bm25SearchMock;
    private readonly Mock<IQueryPreprocessor> _preprocessorMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<HybridSearchService>> _loggerMock;
    private readonly IOptions<HybridSearchOptions> _options;

    // LOGIC: SearchLicenseGuard is a concrete class, so we need its own mocks.
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<SearchLicenseGuard>> _guardLoggerMock;
    private readonly SearchLicenseGuard _licenseGuard;

    // LOGIC: Shared test data for document and chunk construction.
    private static readonly Guid DocId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DocId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public HybridSearchServiceTests()
    {
        _semanticSearchMock = new Mock<ISemanticSearchService>();
        _bm25SearchMock = new Mock<IBM25SearchService>();
        _preprocessorMock = new Mock<IQueryPreprocessor>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<HybridSearchService>>();
        _options = Options.Create(HybridSearchOptions.Default);

        _licenseContextMock = new Mock<ILicenseContext>();
        _guardLoggerMock = new Mock<ILogger<SearchLicenseGuard>>();

        // LOGIC: Default to WriterPro so most tests pass the license check.
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        _licenseGuard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        // LOGIC: Default preprocessor returns trimmed query.
        _preprocessorMock
            .Setup(x => x.Process(It.IsAny<string>(), It.IsAny<SearchOptions>()))
            .Returns<string, SearchOptions>((q, _) => q.Trim());

        // LOGIC: Default sub-search mocks return empty results to avoid NREs
        // in tests that don't configure specific mock behavior.
        SetupEmptySearchResults();
    }

    /// <summary>
    /// Creates a <see cref="HybridSearchService"/> using the test mocks.
    /// </summary>
    private HybridSearchService CreateService() =>
        new(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _preprocessorMock.Object,
            _licenseGuard,
            _options,
            _mediatorMock.Object,
            _loggerMock.Object);

    /// <summary>
    /// Creates a <see cref="HybridSearchService"/> with custom options.
    /// </summary>
    private HybridSearchService CreateServiceWithOptions(HybridSearchOptions opts) =>
        new(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _preprocessorMock.Object,
            _licenseGuard,
            Options.Create(opts),
            _mediatorMock.Object,
            _loggerMock.Object);

    /// <summary>
    /// Sets up both sub-search mocks to return empty results.
    /// </summary>
    private void SetupEmptySearchResults()
    {
        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SearchResult.Empty());

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SearchResult.Empty());
    }

    /// <summary>
    /// Creates a test <see cref="SearchHit"/> with the specified document and chunk index.
    /// </summary>
    private static SearchHit CreateHit(Guid docId, int chunkIndex, float score = 0.8f, string content = "test content")
    {
        return new SearchHit
        {
            Chunk = new TextChunk(
                content,
                StartOffset: chunkIndex * 100,
                EndOffset: (chunkIndex + 1) * 100,
                new ChunkMetadata(chunkIndex)),
            Document = new Document(
                docId,
                ProjectId,
                $"doc-{docId:N}.md",
                $"Document {docId:N}",
                "hash123",
                DocumentStatus.Indexed,
                DateTime.UtcNow,
                null),
            Score = score
        };
    }

    /// <summary>
    /// Creates a <see cref="SearchResult"/> from a list of hits.
    /// </summary>
    private static SearchResult CreateSearchResult(params SearchHit[] hits)
    {
        return new SearchResult
        {
            Hits = hits.ToList(),
            Duration = TimeSpan.FromMilliseconds(50),
            Query = "test query"
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullSemanticSearch_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HybridSearchService(
            null!, _bm25SearchMock.Object, _preprocessorMock.Object,
            _licenseGuard, _options, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("semanticSearch");
    }

    [Fact]
    public void Constructor_NullBM25Search_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HybridSearchService(
            _semanticSearchMock.Object, null!, _preprocessorMock.Object,
            _licenseGuard, _options, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("bm25Search");
    }

    [Fact]
    public void Constructor_NullPreprocessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, null!,
            _licenseGuard, _options, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("preprocessor");
    }

    [Fact]
    public void Constructor_NullLicenseGuard_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            null!, _options, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseGuard");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            _licenseGuard, null!, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            _licenseGuard, _options, null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            _licenseGuard, _options, _mediatorMock.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateService();

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task SearchAsync_NullQuery_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.SearchAsync(null!, SearchOptions.Default);

        await act.Should().ThrowAsync<ArgumentException>(
            because: "null query should be rejected");
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.SearchAsync("", SearchOptions.Default);

        await act.Should().ThrowAsync<ArgumentException>(
            because: "empty query should be rejected");
    }

    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.SearchAsync("   ", SearchOptions.Default);

        await act.Should().ThrowAsync<ArgumentException>(
            because: "whitespace-only query should be rejected");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public async Task SearchAsync_InvalidTopK_ThrowsArgumentOutOfRangeException(int topK)
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { TopK = topK };

        // Act & Assert
        var act = () => service.SearchAsync("test query", options);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>(
            because: $"TopK={topK} is outside the valid range [1, 100]");
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    [InlineData(-1.0f)]
    [InlineData(float.MaxValue)]
    public async Task SearchAsync_InvalidMinScore_ThrowsArgumentOutOfRangeException(float minScore)
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { MinScore = minScore };

        // Act & Assert
        var act = () => service.SearchAsync("test query", options);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>(
            because: $"MinScore={minScore} is outside the valid range [0.0, 1.0]");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task SearchAsync_ValidTopK_DoesNotThrowValidationError(int topK)
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { TopK = topK };

        // Act & Assert — sub-searches return empty results, so no DB needed.
        var act = () => service.SearchAsync("test query", options);

        await act.Should().NotThrowAsync<ArgumentOutOfRangeException>(
            because: $"TopK={topK} is within the valid range [1, 100]");
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(0.7f)]
    [InlineData(1.0f)]
    public async Task SearchAsync_ValidMinScore_DoesNotThrowValidationError(float minScore)
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { MinScore = minScore };

        // Act & Assert — sub-searches return empty results, so no DB needed.
        var act = () => service.SearchAsync("test query", options);

        await act.Should().NotThrowAsync<ArgumentOutOfRangeException>(
            because: $"MinScore={minScore} is within the valid range [0.0, 1.0]");
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public async Task SearchAsync_CoreTier_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            guard, _options, _mediatorMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = () => service.SearchAsync("test query", SearchOptions.Default);

        await act.Should().ThrowAsync<FeatureNotLicensedException>(
            because: "Core tier does not have access to hybrid search");
    }

    [Fact]
    public async Task SearchAsync_CoreTier_ExceptionContainsRequiredTier()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            guard, _options, _mediatorMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = () => service.SearchAsync("test query", SearchOptions.Default);

        (await act.Should().ThrowAsync<FeatureNotLicensedException>())
            .Which.RequiredTier.Should().Be(LicenseTier.WriterPro,
                because: "the exception should indicate WriterPro as the minimum tier");
    }

    [Theory]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public async Task SearchAsync_AuthorizedTier_DoesNotThrowLicenseException(LicenseTier tier)
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(tier);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            guard, _options, _mediatorMock.Object, _loggerMock.Object);

        // Act & Assert — sub-searches return empty results, so no DB needed.
        var act = () => service.SearchAsync("test query", SearchOptions.Default);

        await act.Should().NotThrowAsync<FeatureNotLicensedException>(
            because: $"Tier {tier} should be authorized for hybrid search");
    }

    #endregion

    #region RRF Algorithm Tests

    [Fact]
    public async Task SearchAsync_ChunksInBothLists_RankHigherThanSingleListChunks()
    {
        // Arrange: chunk at (DocId1, 0) appears in both lists;
        //          chunk at (DocId1, 1) only in semantic;
        //          chunk at (DocId2, 0) only in BM25.
        var sharedHit = CreateHit(DocId1, chunkIndex: 0, score: 0.9f, content: "shared chunk");
        var semanticOnlyHit = CreateHit(DocId1, chunkIndex: 1, score: 0.85f, content: "semantic only");
        var bm25OnlyHit = CreateHit(DocId2, chunkIndex: 0, score: 0.8f, content: "bm25 only");

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(sharedHit, semanticOnlyHit));

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(sharedHit, bm25OnlyHit));

        var service = CreateService();

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert: shared chunk should rank first (RRF from both lists).
        result.Hits.Should().NotBeEmpty();
        result.Hits[0].Chunk.Metadata.Index.Should().Be(0,
            because: "chunk appearing in both lists should rank highest via RRF");
        result.Hits[0].Document.Id.Should().Be(DocId1,
            because: "the shared chunk belongs to DocId1");
    }

    [Fact]
    public async Task SearchAsync_SemanticOnlyChunk_GetsSemanticRRFScore()
    {
        // Arrange: one chunk only in semantic results at rank 1.
        var semanticHit = CreateHit(DocId1, chunkIndex: 0);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(semanticHit));

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SearchResult.Empty());

        var service = CreateService();

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert
        result.Hits.Should().HaveCount(1,
            because: "semantic-only chunk should still appear in fused results");
        result.Hits[0].Document.Id.Should().Be(DocId1);
    }

    [Fact]
    public async Task SearchAsync_BM25OnlyChunk_GetsBM25RRFScore()
    {
        // Arrange: one chunk only in BM25 results at rank 1.
        var bm25Hit = CreateHit(DocId2, chunkIndex: 0);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SearchResult.Empty());

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(bm25Hit));

        var service = CreateService();

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert
        result.Hits.Should().HaveCount(1,
            because: "BM25-only chunk should still appear in fused results");
        result.Hits[0].Document.Id.Should().Be(DocId2);
    }

    [Fact]
    public async Task SearchAsync_EqualWeights_ProducesBalancedRanking()
    {
        // Arrange: Two chunks, each ranked #1 in a different search.
        // With equal weights, the one that appears in both lists should rank higher
        // than the one in only one list.
        var chunk1 = CreateHit(DocId1, chunkIndex: 0, content: "chunk one");
        var chunk2 = CreateHit(DocId1, chunkIndex: 1, content: "chunk two");
        var chunk3 = CreateHit(DocId2, chunkIndex: 0, content: "chunk three");

        // Chunk1 is #1 in semantic, chunk2 is #2 in semantic.
        // Chunk2 is #1 in BM25, chunk3 is #2 in BM25.
        // Chunk2 appears in both → should rank first with equal weights.
        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(chunk1, chunk2));

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(chunk2, chunk3));

        var opts = new HybridSearchOptions { SemanticWeight = 0.5f, BM25Weight = 0.5f };
        var service = CreateServiceWithOptions(opts);

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert: chunk2 appears in both lists → highest RRF score.
        result.Hits.Should().HaveCountGreaterOrEqualTo(1);
        result.Hits[0].Chunk.Metadata.Index.Should().Be(1,
            because: "chunk2 (index=1) appears in both lists and should rank highest");
    }

    [Fact]
    public async Task SearchAsync_SemanticWeightOnly_ProducesSemanticOrdering()
    {
        // Arrange: Semantic weight = 1.0, BM25 weight = 0.0.
        // Only semantic ranking should influence the result.
        var semanticFirst = CreateHit(DocId1, chunkIndex: 0, content: "semantic rank 1");
        var semanticSecond = CreateHit(DocId1, chunkIndex: 1, content: "semantic rank 2");
        var bm25First = CreateHit(DocId2, chunkIndex: 0, content: "bm25 rank 1");

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(semanticFirst, semanticSecond));

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(bm25First));

        var opts = new HybridSearchOptions { SemanticWeight = 1.0f, BM25Weight = 0.0f };
        var service = CreateServiceWithOptions(opts);

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert: Semantic #1 should still be first (BM25 contributes zero weight).
        result.Hits.Should().HaveCountGreaterOrEqualTo(2);
        result.Hits[0].Document.Id.Should().Be(DocId1);
        result.Hits[0].Chunk.Metadata.Index.Should().Be(0,
            because: "semantic rank 1 should be first when BM25 weight is zero");
    }

    [Fact]
    public async Task SearchAsync_BM25WeightOnly_ProducesBM25Ordering()
    {
        // Arrange: BM25 weight = 1.0, Semantic weight = 0.0.
        // Only BM25 ranking should influence the result.
        var semanticFirst = CreateHit(DocId1, chunkIndex: 0, content: "semantic rank 1");
        var bm25First = CreateHit(DocId2, chunkIndex: 0, content: "bm25 rank 1");
        var bm25Second = CreateHit(DocId2, chunkIndex: 1, content: "bm25 rank 2");

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(semanticFirst));

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(bm25First, bm25Second));

        var opts = new HybridSearchOptions { SemanticWeight = 0.0f, BM25Weight = 1.0f };
        var service = CreateServiceWithOptions(opts);

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert: BM25 #1 should be first (semantic contributes zero weight).
        result.Hits.Should().HaveCountGreaterOrEqualTo(2);
        result.Hits[0].Document.Id.Should().Be(DocId2);
        result.Hits[0].Chunk.Metadata.Index.Should().Be(0,
            because: "BM25 rank 1 should be first when semantic weight is zero");
    }

    [Fact]
    public async Task SearchAsync_EmptySubSearchResults_ReturnsEmptyResult()
    {
        // Arrange: Both sub-searches return empty results.
        var service = CreateService();

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert
        result.Hits.Should().BeEmpty(
            because: "no matches from either sub-search should produce empty fused results");
        result.HasResults.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_RespectsTopKLimit()
    {
        // Arrange: Sub-searches return many results but TopK=2.
        var hits = Enumerable.Range(0, 10)
            .Select(i => CreateHit(DocId1, chunkIndex: i, content: $"chunk {i}"))
            .ToArray();

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(hits));

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SearchResult.Empty());

        var service = CreateService();
        var options = new SearchOptions { TopK = 2 };

        // Act
        var result = await service.SearchAsync("test query", options);

        // Assert
        result.Hits.Should().HaveCount(2,
            because: "fused results should be trimmed to TopK=2");
        result.WasTruncated.Should().BeTrue(
            because: "result count equals TopK, indicating truncation");
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task SearchAsync_InvokesBothSubSearchesExactlyOnce()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.SearchAsync("test query", SearchOptions.Default);

        // Assert
        _semanticSearchMock.Verify(
            x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "semantic search should be invoked exactly once");

        _bm25SearchMock.Verify(
            x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "BM25 search should be invoked exactly once");
    }

    [Fact]
    public async Task SearchAsync_PassesExpandedTopKToSubSearches()
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { TopK = 5 };

        // Act
        await service.SearchAsync("test query", options);

        // Assert: Both sub-searches should receive TopK=10 (2× the requested 5).
        _semanticSearchMock.Verify(
            x => x.SearchAsync(
                It.IsAny<string>(),
                It.Is<SearchOptions>(o => o.TopK == 10),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "semantic search should receive expanded TopK (2× original)");

        _bm25SearchMock.Verify(
            x => x.SearchAsync(
                It.IsAny<string>(),
                It.Is<SearchOptions>(o => o.TopK == 10),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "BM25 search should receive expanded TopK (2× original)");
    }

    [Fact]
    public async Task SearchAsync_ExpandedTopK_CapsAt100()
    {
        // Arrange: TopK=60 would expand to 120, but should cap at 100.
        var service = CreateService();
        var options = new SearchOptions { TopK = 60 };

        // Act
        await service.SearchAsync("test query", options);

        // Assert: Expanded TopK should cap at 100.
        _semanticSearchMock.Verify(
            x => x.SearchAsync(
                It.IsAny<string>(),
                It.Is<SearchOptions>(o => o.TopK == 100),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "expanded TopK should cap at 100 to respect validation bounds");
    }

    #endregion

    #region Telemetry Event Tests

    [Fact]
    public async Task SearchAsync_Success_PublishesHybridSearchExecutedEvent()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.SearchAsync("test query", SearchOptions.Default);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<HybridSearchExecutedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "HybridSearchExecutedEvent should be published on successful search");
    }

    [Fact]
    public async Task SearchAsync_Success_EventContainsCorrectData()
    {
        // Arrange
        var semanticHit = CreateHit(DocId1, chunkIndex: 0);
        var bm25Hit = CreateHit(DocId2, chunkIndex: 0);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(semanticHit));

        _bm25SearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(bm25Hit));

        HybridSearchExecutedEvent? capturedEvent = null;
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<HybridSearchExecutedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => capturedEvent = (HybridSearchExecutedEvent)e);

        var service = CreateService();

        // Act
        await service.SearchAsync("authentication OAuth", SearchOptions.Default);

        // Assert
        capturedEvent.Should().NotBeNull("event should have been captured");
        capturedEvent!.Query.Should().Be("authentication OAuth");
        capturedEvent.SemanticHitCount.Should().Be(1);
        capturedEvent.BM25HitCount.Should().Be(1);
        capturedEvent.FusedResultCount.Should().Be(2, because: "two distinct chunks from different docs");
        capturedEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        capturedEvent.SemanticWeight.Should().Be(0.7f);
        capturedEvent.BM25Weight.Should().Be(0.3f);
        capturedEvent.RRFConstant.Should().Be(60);
    }

    [Fact]
    public async Task SearchAsync_LicenseDenied_DoesNotPublishEvent()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            guard, _options, _mediatorMock.Object, _loggerMock.Object);

        // Act
        try
        {
            await service.SearchAsync("test query", SearchOptions.Default);
        }
        catch (FeatureNotLicensedException)
        {
            // Expected.
        }

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<HybridSearchExecutedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "HybridSearchExecutedEvent should not be published when license is denied");
    }

    #endregion

    #region Preprocessing Tests

    [Fact]
    public async Task SearchAsync_DelegatesQueryToPreprocessor()
    {
        // Arrange
        var service = CreateService();
        var query = "  test query with spaces  ";

        // Act
        await service.SearchAsync(query, SearchOptions.Default);

        // Assert
        _preprocessorMock.Verify(
            p => p.Process(query, It.IsAny<SearchOptions>()),
            Times.Once,
            "the raw query should be passed to the preprocessor");
    }

    [Fact]
    public async Task SearchAsync_LicenseDenied_DoesNotCallPreprocessor()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new HybridSearchService(
            _semanticSearchMock.Object, _bm25SearchMock.Object, _preprocessorMock.Object,
            guard, _options, _mediatorMock.Object, _loggerMock.Object);

        // Act
        try
        {
            await service.SearchAsync("test query", SearchOptions.Default);
        }
        catch (FeatureNotLicensedException)
        {
            // Expected.
        }

        // Assert
        _preprocessorMock.Verify(
            p => p.Process(It.IsAny<string>(), It.IsAny<SearchOptions>()),
            Times.Never,
            "preprocessing should not occur when the license check fails");
    }

    #endregion

    #region Result Assembly Tests

    [Fact]
    public async Task SearchAsync_PreservesQueryEmbeddingFromSemanticSearch()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult
            {
                Hits = Array.Empty<SearchHit>(),
                QueryEmbedding = queryEmbedding,
                Query = "test"
            });

        var service = CreateService();

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert
        result.QueryEmbedding.Should().BeSameAs(queryEmbedding,
            because: "the query embedding from semantic search should be preserved in the fused result");
    }

    [Fact]
    public async Task SearchAsync_PreservesOriginalQuery()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SearchAsync("original query text", SearchOptions.Default);

        // Assert
        result.Query.Should().Be("original query text",
            because: "the original query should be preserved in the result");
    }

    [Fact]
    public async Task SearchAsync_DurationIsPositive()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SearchAsync("test query", SearchOptions.Default);

        // Assert
        result.Duration.Should().BeGreaterOrEqualTo(TimeSpan.Zero,
            because: "duration should reflect elapsed time");
    }

    #endregion

    #region IHybridSearchService Interface Tests

    [Fact]
    public void Service_ImplementsIHybridSearchService()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IHybridSearchService>(
            because: "HybridSearchService implements the IHybridSearchService interface");
    }

    #endregion
}
