using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for the semantic search abstraction types defined in v0.4.5a:
/// <see cref="SearchOptions"/>, <see cref="SearchResult"/>, <see cref="SearchHit"/>,
/// and <see cref="ISemanticSearchService"/>.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the search abstraction records' properties,
/// computed values, factory methods, record equality, and formatting helpers.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5a")]
public class SearchAbstractionsTests
{
    #region Test Helpers

    private static TextChunk CreateTestChunk(string content = "Test chunk content for semantic search.")
    {
        return new TextChunk(
            content,
            StartOffset: 0,
            EndOffset: content.Length,
            new ChunkMetadata(Index: 0) { TotalChunks = 1 });
    }

    private static Document CreateTestDocument()
    {
        return new Document(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ProjectId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            FilePath: "docs/test-document.md",
            Title: "Test Document",
            Hash: "abc123def456",
            Status: DocumentStatus.Indexed,
            IndexedAt: new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            FailureReason: null);
    }

    private static SearchHit CreateTestHit(float score, string? content = null)
    {
        return new SearchHit
        {
            Chunk = CreateTestChunk(content ?? "Test chunk content for semantic search."),
            Document = CreateTestDocument(),
            Score = score
        };
    }

    #endregion

    #region SearchOptions Tests

    [Fact]
    public void SearchOptions_Default_HasExpectedValues()
    {
        // Arrange & Act
        var options = SearchOptions.Default;

        // Assert
        options.TopK.Should().Be(10, because: "default TopK is 10 results");
        options.MinScore.Should().Be(0.7f, because: "default MinScore is 0.7 (medium relevance)");
        options.DocumentFilter.Should().BeNull(because: "default searches all documents");
        options.ExpandAbbreviations.Should().BeFalse(because: "abbreviation expansion is off by default");
        options.UseCache.Should().BeTrue(because: "query embedding caching is on by default");
    }

    [Fact]
    public void SearchOptions_DefaultStaticProperty_MatchesNewInstance()
    {
        // Arrange & Act
        var fromDefault = SearchOptions.Default;
        var fromNew = new SearchOptions();

        // Assert
        fromDefault.Should().Be(fromNew,
            because: "static Default property should match a fresh instance with all defaults");
    }

    [Fact]
    public void SearchOptions_CustomValues_ArePreserved()
    {
        // Arrange
        var docId = Guid.NewGuid();

        // Act
        var options = new SearchOptions
        {
            TopK = 25,
            MinScore = 0.85f,
            DocumentFilter = docId,
            ExpandAbbreviations = true,
            UseCache = false
        };

        // Assert
        options.TopK.Should().Be(25, because: "custom TopK should be preserved");
        options.MinScore.Should().Be(0.85f, because: "custom MinScore should be preserved");
        options.DocumentFilter.Should().Be(docId, because: "custom DocumentFilter should be preserved");
        options.ExpandAbbreviations.Should().BeTrue(because: "custom ExpandAbbreviations should be preserved");
        options.UseCache.Should().BeFalse(because: "custom UseCache should be preserved");
    }

    [Fact]
    public void SearchOptions_WithExpression_ProducesNewInstanceWithChanges()
    {
        // Arrange
        var original = new SearchOptions { TopK = 10, MinScore = 0.7f };

        // Act
        var modified = original with { TopK = 20 };

        // Assert
        modified.TopK.Should().Be(20, because: "with-expression should update TopK");
        modified.MinScore.Should().Be(0.7f, because: "with-expression should preserve unchanged properties");
        original.TopK.Should().Be(10, because: "original should not be mutated");
    }

    [Fact]
    public void SearchOptions_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var options1 = new SearchOptions { TopK = 15, MinScore = 0.8f };
        var options2 = new SearchOptions { TopK = 15, MinScore = 0.8f };

        // Assert
        options1.Should().Be(options2,
            because: "records with same property values should be equal");
    }

    [Fact]
    public void SearchOptions_RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var options1 = new SearchOptions { TopK = 10 };
        var options2 = new SearchOptions { TopK = 20 };

        // Assert
        options1.Should().NotBe(options2,
            because: "records with different TopK should not be equal");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public void SearchOptions_TopK_OutOfRange_StillAccepted(int topK)
    {
        // Arrange & Act
        // Record does not validate; validation is deferred to the implementation.
        var options = new SearchOptions { TopK = topK };

        // Assert
        options.TopK.Should().Be(topK,
            because: "the record accepts any value; validation is deferred to the implementation");
    }

    #endregion

    #region SearchResult Tests

    [Fact]
    public void SearchResult_Empty_HasNoHits()
    {
        // Arrange & Act
        var result = SearchResult.Empty("test query");

        // Assert
        result.Hits.Should().BeEmpty(because: "Empty factory produces zero results");
        result.HasResults.Should().BeFalse(because: "no hits means HasResults is false");
        result.Count.Should().Be(0, because: "no hits means Count is 0");
        result.Query.Should().Be("test query", because: "query text should be preserved");
        result.WasTruncated.Should().BeFalse(because: "empty result is not truncated");
    }

    [Fact]
    public void SearchResult_Empty_WithNullQuery_HasNullQuery()
    {
        // Arrange & Act
        var result = SearchResult.Empty();

        // Assert
        result.Query.Should().BeNull(because: "default parameter is null");
        result.Hits.Should().BeEmpty();
    }

    [Fact]
    public void SearchResult_HasResults_WhenHitsExist_ReturnsTrue()
    {
        // Arrange & Act
        var result = new SearchResult
        {
            Hits = new[] { CreateTestHit(0.9f) }
        };

        // Assert
        result.HasResults.Should().BeTrue(because: "result contains one hit");
        result.Count.Should().Be(1, because: "result contains one hit");
    }

    [Fact]
    public void SearchResult_Count_ReflectsHitCount()
    {
        // Arrange
        var hits = new[]
        {
            CreateTestHit(0.95f),
            CreateTestHit(0.85f),
            CreateTestHit(0.75f)
        };

        // Act
        var result = new SearchResult { Hits = hits };

        // Assert
        result.Count.Should().Be(3, because: "three hits were provided");
        result.HasResults.Should().BeTrue();
    }

    [Fact]
    public void SearchResult_Duration_TracksSearchTime()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        var result = new SearchResult
        {
            Hits = Array.Empty<SearchHit>(),
            Duration = duration
        };

        // Assert
        result.Duration.Should().Be(duration, because: "duration should be preserved");
    }

    [Fact]
    public void SearchResult_WasTruncated_WhenSet_ReturnsTrue()
    {
        // Arrange & Act
        var result = new SearchResult
        {
            Hits = new[] { CreateTestHit(0.9f) },
            WasTruncated = true
        };

        // Assert
        result.WasTruncated.Should().BeTrue(because: "truncation flag was explicitly set");
    }

    [Fact]
    public void SearchResult_QueryEmbedding_StoresVector()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var result = new SearchResult
        {
            Hits = Array.Empty<SearchHit>(),
            QueryEmbedding = embedding
        };

        // Assert
        result.QueryEmbedding.Should().BeSameAs(embedding,
            because: "QueryEmbedding stores the reference to the embedding array");
    }

    #endregion

    #region SearchHit Tests

    [Theory]
    [InlineData(0.87f, "87%")]
    [InlineData(0.5f, "50%")]
    [InlineData(1.0f, "100%")]
    [InlineData(0.0f, "0%")]
    [InlineData(0.999f, "100%")]
    [InlineData(0.001f, "0%")]
    public void SearchHit_ScorePercent_FormatsCorrectly(float score, string expected)
    {
        // Arrange
        var hit = CreateTestHit(score);

        // Assert
        hit.ScorePercent.Should().Be(expected,
            because: $"score {score} formatted as F0 percentage should be \"{expected}\"");
    }

    [Theory]
    [InlineData(0.87f, "0.87")]
    [InlineData(0.5f, "0.50")]
    [InlineData(1.0f, "1.00")]
    [InlineData(0.0f, "0.00")]
    [InlineData(0.123f, "0.12")]
    public void SearchHit_ScoreDecimal_FormatsCorrectly(float score, string expected)
    {
        // Arrange
        var hit = CreateTestHit(score);

        // Assert
        hit.ScoreDecimal.Should().Be(expected,
            because: $"score {score} formatted as F2 decimal should be \"{expected}\"");
    }

    [Fact]
    public void SearchHit_GetPreview_ShortContent_ReturnsFullContent()
    {
        // Arrange
        var hit = CreateTestHit(0.9f, "Short content");

        // Act
        var preview = hit.GetPreview(200);

        // Assert
        preview.Should().Be("Short content",
            because: "content shorter than maxLength is returned in full");
    }

    [Fact]
    public void SearchHit_GetPreview_LongContent_Truncates()
    {
        // Arrange
        var longContent = new string('x', 300);
        var hit = CreateTestHit(0.9f, longContent);

        // Act
        var preview = hit.GetPreview(200);

        // Assert
        preview.Should().EndWith("...", because: "truncated previews end with ellipsis");
        preview.Length.Should().Be(203,
            because: "200 characters + 3 for '...' (no trailing whitespace to trim)");
    }

    [Fact]
    public void SearchHit_GetPreview_ExactLength_ReturnsFullContent()
    {
        // Arrange
        var content = new string('a', 200);
        var hit = CreateTestHit(0.9f, content);

        // Act
        var preview = hit.GetPreview(200);

        // Assert
        preview.Should().Be(content,
            because: "content at exactly maxLength is not truncated");
        preview.Should().NotEndWith("...");
    }

    [Fact]
    public void SearchHit_GetPreview_CustomMaxLength_Respects()
    {
        // Arrange
        var content = new string('b', 100);
        var hit = CreateTestHit(0.9f, content);

        // Act
        var preview = hit.GetPreview(50);

        // Assert
        preview.Should().EndWith("...", because: "content exceeds custom maxLength of 50");
        preview.Length.Should().Be(53, because: "50 characters + 3 for '...'");
    }

    [Fact]
    public void SearchHit_GetPreview_DefaultMaxLength_Is200()
    {
        // Arrange
        var content = new string('c', 201);
        var hit = CreateTestHit(0.9f, content);

        // Act
        var previewDefault = hit.GetPreview();
        var preview200 = hit.GetPreview(200);

        // Assert
        previewDefault.Should().Be(preview200,
            because: "default maxLength parameter should be 200");
    }

    [Fact]
    public void SearchHit_GetPreview_TruncationTrimsTrailingWhitespace()
    {
        // Arrange — content where truncation lands on trailing spaces
        var content = new string('d', 195) + "     " + new string('e', 100); // 195 + 5 spaces + 100 = 300
        var hit = CreateTestHit(0.9f, content);

        // Act
        var preview = hit.GetPreview(200);

        // Assert
        preview.Should().EndWith("...", because: "truncated previews end with ellipsis");
        // The first 200 chars = 195 'd's + 5 spaces. TrimEnd removes the 5 spaces.
        preview.Should().Be(new string('d', 195) + "...",
            because: "trailing whitespace at truncation boundary is trimmed before appending ellipsis");
    }

    [Fact]
    public void SearchHit_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var doc = CreateTestDocument();

        var hit1 = new SearchHit { Chunk = chunk, Document = doc, Score = 0.85f };
        var hit2 = new SearchHit { Chunk = chunk, Document = doc, Score = 0.85f };

        // Assert
        hit1.Should().Be(hit2, because: "hits with same chunk, document, and score should be equal");
    }

    [Fact]
    public void SearchHit_RecordEquality_DifferentScores_AreNotEqual()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var doc = CreateTestDocument();

        var hit1 = new SearchHit { Chunk = chunk, Document = doc, Score = 0.85f };
        var hit2 = new SearchHit { Chunk = chunk, Document = doc, Score = 0.75f };

        // Assert
        hit1.Should().NotBe(hit2,
            because: "hits with different scores should not be equal");
    }

    [Fact]
    public void SearchHit_WithExpression_ProducesNewInstance()
    {
        // Arrange
        var original = CreateTestHit(0.85f);

        // Act
        var modified = original with { Score = 0.95f };

        // Assert
        modified.Score.Should().Be(0.95f, because: "with-expression should update Score");
        modified.Chunk.Should().Be(original.Chunk,
            because: "with-expression should preserve unchanged properties");
        original.Score.Should().Be(0.85f, because: "original should not be mutated");
    }

    [Fact]
    public void SearchHit_GetHashCode_SameValues_AreEqual()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var doc = CreateTestDocument();

        var hit1 = new SearchHit { Chunk = chunk, Document = doc, Score = 0.85f };
        var hit2 = new SearchHit { Chunk = chunk, Document = doc, Score = 0.85f };

        // Assert
        hit1.GetHashCode().Should().Be(hit2.GetHashCode(),
            because: "equal records should have equal hash codes");
    }

    [Fact]
    public void SearchHit_Score_DefaultsToZero()
    {
        // Arrange & Act
        var hit = new SearchHit
        {
            Chunk = CreateTestChunk(),
            Document = CreateTestDocument()
        };

        // Assert
        hit.Score.Should().Be(0.0f,
            because: "Score defaults to 0.0f when not explicitly set");
        hit.ScorePercent.Should().Be("0%");
        hit.ScoreDecimal.Should().Be("0.00");
    }

    #endregion

    #region ISemanticSearchService Contract Tests

    [Fact]
    public void ISemanticSearchService_InterfaceExists_And_HasSearchAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(ISemanticSearchService);

        // Assert
        interfaceType.IsInterface.Should().BeTrue(because: "ISemanticSearchService is an interface");

        var method = interfaceType.GetMethod("SearchAsync");
        method.Should().NotBeNull(because: "ISemanticSearchService defines SearchAsync method");

        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(3, because: "SearchAsync takes query, options, and ct parameters");
        parameters[0].ParameterType.Should().Be(typeof(string));
        parameters[1].ParameterType.Should().Be(typeof(SearchOptions));
        parameters[2].ParameterType.Should().Be(typeof(CancellationToken));
    }

    [Fact]
    public void ISemanticSearchService_SearchAsync_ReturnsTaskOfSearchResult()
    {
        // Arrange
        var method = typeof(ISemanticSearchService).GetMethod("SearchAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<SearchResult>),
            because: "SearchAsync returns Task<SearchResult>");
    }

    #endregion
}
