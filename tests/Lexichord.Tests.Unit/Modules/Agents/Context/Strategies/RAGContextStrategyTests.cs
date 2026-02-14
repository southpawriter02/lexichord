// -----------------------------------------------------------------------
// <copyright file="RAGContextStrategyTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Agents.Context.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context.Strategies;

/// <summary>
/// Unit tests for <see cref="RAGContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify constructor validation, property values, GatherAsync behavior
/// including search query construction, error handling, hint support, and
/// result formatting. Introduced in v0.7.2b.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2b")]
public class RAGContextStrategyTests
{
    #region Helper Factories

    private static RAGContextStrategy CreateStrategy(
        ISemanticSearchService? searchService = null,
        ITokenCounter? tokenCounter = null,
        ILogger<RAGContextStrategy>? logger = null)
    {
        searchService ??= Substitute.For<ISemanticSearchService>();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<RAGContextStrategy>>();
        return new RAGContextStrategy(searchService, tokenCounter, logger);
    }

    private static ITokenCounter CreateDefaultTokenCounter()
    {
        var tc = Substitute.For<ITokenCounter>();
        tc.CountTokens(Arg.Any<string>()).Returns(ci => ci.Arg<string>().Length / 4);
        return tc;
    }

    private static SearchHit CreateSearchHit(
        float score = 0.85f,
        string content = "Relevant content here",
        string? filePath = "/docs/related.md")
    {
        return new SearchHit
        {
            Score = score,
            Chunk = new TextChunk(content, 0, content.Length, new ChunkMetadata(0)),
            Document = new Document(
                Guid.NewGuid(),
                Guid.NewGuid(),
                filePath ?? string.Empty,
                "Related",
                "hash",
                DocumentStatus.Indexed,
                DateTime.UtcNow,
                null)
        };
    }

    private static SearchResult CreateSearchResult(
        bool hasResults = true,
        params SearchHit[] hits)
    {
        if (!hasResults || hits.Length == 0)
        {
            return SearchResult.Empty();
        }

        return new SearchResult
        {
            Hits = hits
        };
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that passing a null search service to the constructor throws
    /// an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullSearchService_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<RAGContextStrategy>>();

        // Act
        var act = () => new RAGContextStrategy(null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("searchService");
    }

    /// <summary>
    /// Verifies that constructing with valid parameters succeeds without error.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<RAGContextStrategy>>();

        // Act
        var sut = new RAGContextStrategy(searchService, tokenCounter, logger);

        // Assert
        sut.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.StrategyId"/> returns "rag".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsRag()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.StrategyId;

        // Assert
        result.Should().Be("rag");
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.DisplayName"/> returns "Related Documentation".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsRelatedDocumentation()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.DisplayName;

        // Assert
        result.Should().Be("Related Documentation");
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.Priority"/> returns
    /// <see cref="StrategyPriority.Medium"/> (60).
    /// </summary>
    [Fact]
    public void Priority_ReturnsMedium()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.Priority;

        // Assert
        result.Should().Be(StrategyPriority.Medium);
        result.Should().Be(60);
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.MaxTokens"/> returns 2000.
    /// </summary>
    [Fact]
    public void MaxTokens_Returns2000()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.MaxTokens;

        // Assert
        result.Should().Be(2000);
    }

    #endregion

    #region GatherAsync Tests

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> returns null
    /// when the request has no selected text and no document path.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoSelectionAndNoDocument_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, null, "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> uses the selected text
    /// as the search query when a selection is provided.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithSelection_UsesSelectionAsQuery()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        var hit = CreateSearchHit();
        var searchResult = CreateSearchResult(true, hit);

        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(searchResult);

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            "/docs/chapter1.md", null, "The old man and the sea", "editor", null);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await searchService.Received(1).SearchAsync(
            "The old man and the sea",
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> uses the document
    /// file name (without extension) as the search query when no selection is provided.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithDocumentOnly_UsesFileNameAsQuery()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        var hit = CreateSearchHit();
        var searchResult = CreateSearchResult(true, hit);

        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(searchResult);

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            "/docs/chapter1.md", null, null, "editor", null);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await searchService.Received(1).SearchAsync(
            "chapter1",
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> returns null
    /// when the search yields no results.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoResults_ReturnsNull()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        var emptyResult = SearchResult.Empty();

        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(emptyResult);

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            null, null, "some query text", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> returns a formatted
    /// fragment containing "## Related Documentation" when search results are found.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithResults_ReturnsFormattedFragment()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        var hit = CreateSearchHit(
            score: 0.85f,
            content: "Relevant documentation content",
            filePath: "/docs/related.md");
        var searchResult = CreateSearchResult(true, hit);

        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(searchResult);

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            null, null, "search query", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("## Related Documentation");
        result.Content.Should().Contain("Relevant documentation content");
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> returns null
    /// when <see cref="ISemanticSearchService.SearchAsync"/> throws a
    /// <see cref="FeatureNotLicensedException"/> (graceful handling, no propagation).
    /// </summary>
    [Fact]
    public async Task GatherAsync_FeatureNotLicensed_ReturnsNull()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new FeatureNotLicensedException(
                "Semantic search requires Teams license.",
                LicenseTier.Teams));

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            null, null, "search text", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> returns null
    /// when <see cref="ISemanticSearchService.SearchAsync"/> throws a general exception
    /// (graceful handling, no propagation).
    /// </summary>
    [Fact]
    public async Task GatherAsync_GeneralException_ReturnsNull()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Search index unavailable"));

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            null, null, "search text", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> re-throws
    /// <see cref="OperationCanceledException"/> instead of swallowing it.
    /// </summary>
    [Fact]
    public async Task GatherAsync_OperationCanceled_Rethrows()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            null, null, "search text", "editor", null);

        // Act
        var act = () => sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> reads the TopK hint
    /// from the request and passes it to <see cref="SearchOptions.TopK"/>.
    /// </summary>
    [Fact]
    public async Task GatherAsync_UsesHintForTopK()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        var hit = CreateSearchHit();
        var searchResult = CreateSearchResult(true, hit);

        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(searchResult);

        var hints = new Dictionary<string, object> { { "TopK", 5 } };
        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            null, null, "search text", "editor", hints);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await searchService.Received(1).SearchAsync(
            Arg.Any<string>(),
            Arg.Is<SearchOptions>(o => o.TopK == 5),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the <see cref="ContextFragment"/> returned by
    /// <see cref="RAGContextStrategy.GatherAsync"/> has SourceId set to "rag".
    /// </summary>
    [Fact]
    public async Task GatherAsync_FragmentHasCorrectSourceId()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        var hit = CreateSearchHit();
        var searchResult = CreateSearchResult(true, hit);

        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(searchResult);

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            null, null, "search text", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("rag");
    }

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.GatherAsync"/> calculates the
    /// aggregate relevance as the average of the hit scores.
    /// </summary>
    [Fact]
    public async Task GatherAsync_AggregatesRelevanceFromHitScores()
    {
        // Arrange
        var searchService = Substitute.For<ISemanticSearchService>();
        var hit1 = CreateSearchHit(score: 0.8f, content: "First result content");
        var hit2 = CreateSearchHit(score: 0.9f, content: "Second result content");
        var searchResult = CreateSearchResult(true, hit1, hit2);

        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(searchResult);

        var sut = CreateStrategy(searchService: searchService);
        var request = new ContextGatheringRequest(
            null, null, "search text", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var expectedRelevance = (0.8f + 0.9f) / 2f;
        result!.Relevance.Should().BeApproximately(expectedRelevance, 0.001f);
    }

    #endregion

    #region FormatSearchResults Tests

    /// <summary>
    /// Verifies that <see cref="RAGContextStrategy.FormatSearchResults"/> formats
    /// search hits with the "## Related Documentation" heading, content from each hit,
    /// and relevance percentage.
    /// </summary>
    [Fact]
    public void FormatSearchResults_WithHits_FormatsCorrectly()
    {
        // Arrange
        var hit = CreateSearchHit(
            score: 0.85f,
            content: "Important documentation content",
            filePath: "/docs/guide.md");
        var hits = new[] { hit } as IReadOnlyList<SearchHit>;

        // Act
        var result = RAGContextStrategy.FormatSearchResults(hits);

        // Assert
        result.Should().Contain("## Related Documentation");
        result.Should().Contain("Important documentation content");
        result.Should().Contain("guide.md");
        result.Should().Contain("Relevance:");
    }

    #endregion
}
