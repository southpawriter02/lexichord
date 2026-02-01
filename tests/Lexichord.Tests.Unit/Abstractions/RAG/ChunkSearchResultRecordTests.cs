using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for the <see cref="ChunkSearchResult"/> record.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the search result wrapper's properties,
/// including the confidence threshold checks.
/// </remarks>
public class ChunkSearchResultRecordTests
{
    private static readonly Guid TestChunkId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid TestDocumentId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static Chunk CreateTestChunk()
    {
        return new Chunk(
            Id: TestChunkId,
            DocumentId: TestDocumentId,
            Content: "Test chunk content for search results.",
            Embedding: new float[] { 0.1f, 0.2f, 0.3f },
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 38);
    }

    [Fact]
    public void ChunkSearchResult_StoresChunkAndScore()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var score = 0.95;

        // Act
        var result = new ChunkSearchResult(chunk, score);

        // Assert
        result.Chunk.Should().Be(chunk);
        result.SimilarityScore.Should().Be(0.95);
    }

    [Fact]
    public void ChunkSearchResult_WithSameValues_AreEqual()
    {
        // Arrange
        var chunk = CreateTestChunk();

        var result1 = new ChunkSearchResult(chunk, 0.85);
        var result2 = new ChunkSearchResult(chunk, 0.85);

        // Assert
        result1.Should().Be(result2, because: "results with same chunk and score should be equal");
    }

    [Fact]
    public void ChunkSearchResult_WithDifferentScores_AreNotEqual()
    {
        // Arrange
        var chunk = CreateTestChunk();

        var result1 = new ChunkSearchResult(chunk, 0.85);
        var result2 = new ChunkSearchResult(chunk, 0.75);

        // Assert
        result1.Should().NotBe(result2, because: "results with different scores should not be equal");
    }

    [Theory]
    [InlineData(0.80, true)]
    [InlineData(0.85, true)]
    [InlineData(0.99, true)]
    [InlineData(1.0, true)]
    [InlineData(0.79, false)]
    [InlineData(0.50, false)]
    [InlineData(0.0, false)]
    public void IsHighConfidence_ReturnsCorrectValue(double score, bool expected)
    {
        // Arrange
        var result = new ChunkSearchResult(CreateTestChunk(), score);

        // Act & Assert
        result.IsHighConfidence.Should().Be(expected,
            because: $"score {score} should {(expected ? "" : "not ")}be high confidence (>=0.8)");
    }

    [Theory]
    [InlineData(0.50, true)]
    [InlineData(0.60, true)]
    [InlineData(0.99, true)]
    [InlineData(1.0, true)]
    [InlineData(0.49, false)]
    [InlineData(0.30, false)]
    [InlineData(0.0, false)]
    public void MeetsMinimumThreshold_ReturnsCorrectValue(double score, bool expected)
    {
        // Arrange
        var result = new ChunkSearchResult(CreateTestChunk(), score);

        // Act & Assert
        result.MeetsMinimumThreshold.Should().Be(expected,
            because: $"score {score} should {(expected ? "" : "not ")}meet minimum threshold (>=0.5)");
    }

    [Fact]
    public void ChunkSearchResult_SupportsWithExpression()
    {
        // Arrange
        var originalResult = new ChunkSearchResult(CreateTestChunk(), 0.75);

        // Act
        var updatedResult = originalResult with { SimilarityScore = 0.90 };

        // Assert
        updatedResult.SimilarityScore.Should().Be(0.90);
        updatedResult.Chunk.Should().Be(originalResult.Chunk);
    }

    [Fact]
    public void ChunkSearchResult_HasGetHashCode()
    {
        // Arrange
        var chunk = CreateTestChunk();

        var result1 = new ChunkSearchResult(chunk, 0.85);
        var result2 = new ChunkSearchResult(chunk, 0.85);

        // Assert
        result1.GetHashCode().Should().Be(result2.GetHashCode(),
            because: "equal results should have equal hash codes");
    }

    [Fact]
    public void ChunkSearchResult_BoundaryCase_ExactlyAtHighConfidence()
    {
        // Arrange
        var result = new ChunkSearchResult(CreateTestChunk(), 0.80);

        // Assert
        result.IsHighConfidence.Should().BeTrue(
            because: "0.80 is exactly at the high confidence threshold");
    }

    [Fact]
    public void ChunkSearchResult_BoundaryCase_ExactlyAtMinimumThreshold()
    {
        // Arrange
        var result = new ChunkSearchResult(CreateTestChunk(), 0.50);

        // Assert
        result.MeetsMinimumThreshold.Should().BeTrue(
            because: "0.50 is exactly at the minimum threshold");
    }

    [Fact]
    public void ChunkSearchResult_PerfectScore_MeetsBothThresholds()
    {
        // Arrange
        var result = new ChunkSearchResult(CreateTestChunk(), 1.0);

        // Assert
        result.IsHighConfidence.Should().BeTrue();
        result.MeetsMinimumThreshold.Should().BeTrue();
    }
}
