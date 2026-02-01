using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for the <see cref="Chunk"/> record.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify record equality semantics, embedding vector handling,
/// and the CreateWithoutEmbedding factory method. Special attention is given to
/// the computed properties (ContentLength, EmbeddingDimensions).
/// </remarks>
public class ChunkRecordTests
{
    private static readonly Guid TestId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestDocumentId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly float[] TestEmbedding = Enumerable.Range(0, 1536).Select(i => (float)i / 1536f).ToArray();

    [Fact]
    public void Chunk_WithSameValues_AreEqual()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        var chunk1 = new Chunk(
            Id: TestId,
            DocumentId: TestDocumentId,
            Content: "Test content",
            Embedding: embedding,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 12);

        var chunk2 = new Chunk(
            Id: TestId,
            DocumentId: TestDocumentId,
            Content: "Test content",
            Embedding: embedding,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 12);

        // Assert
        // Note: Records compare array references, not content
        chunk1.Should().Be(chunk2, because: "identical records should be equal");
    }

    [Fact]
    public void Chunk_WithDifferentContent_AreNotEqual()
    {
        // Arrange
        var chunk1 = new Chunk(
            Id: TestId,
            DocumentId: TestDocumentId,
            Content: "First content",
            Embedding: null,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 13);

        var chunk2 = chunk1 with { Content = "Different content" };

        // Assert
        chunk1.Should().NotBe(chunk2, because: "chunks with different content should not be equal");
    }

    [Fact]
    public void CreateWithoutEmbedding_SetsCorrectDefaults()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var content = "This is a test chunk of text for indexing.";

        // Act
        var chunk = Chunk.CreateWithoutEmbedding(
            documentId: documentId,
            content: content,
            chunkIndex: 5,
            startOffset: 1000,
            endOffset: 1042);

        // Assert
        chunk.Id.Should().Be(Guid.Empty, because: "database will assign the ID");
        chunk.DocumentId.Should().Be(documentId);
        chunk.Content.Should().Be(content);
        chunk.Embedding.Should().BeNull(because: "embedding not yet generated");
        chunk.ChunkIndex.Should().Be(5);
        chunk.StartOffset.Should().Be(1000);
        chunk.EndOffset.Should().Be(1042);
    }

    [Fact]
    public void ContentLength_ReturnsCorrectValue()
    {
        // Arrange
        var content = "Hello, World!";
        var chunk = Chunk.CreateWithoutEmbedding(TestDocumentId, content, 0, 0, content.Length);

        // Act & Assert
        chunk.ContentLength.Should().Be(13);
    }

    [Fact]
    public void ContentLength_WithEmptyContent_ReturnsZero()
    {
        // Arrange
        var chunk = Chunk.CreateWithoutEmbedding(TestDocumentId, "", 0, 0, 0);

        // Act & Assert
        chunk.ContentLength.Should().Be(0);
    }

    [Fact]
    public void EmbeddingDimensions_WithEmbedding_ReturnsLength()
    {
        // Arrange
        var chunk = new Chunk(
            Id: TestId,
            DocumentId: TestDocumentId,
            Content: "Test",
            Embedding: TestEmbedding,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 4);

        // Act & Assert
        chunk.EmbeddingDimensions.Should().Be(1536,
            because: "OpenAI embeddings have 1536 dimensions");
    }

    [Fact]
    public void EmbeddingDimensions_WithNullEmbedding_ReturnsZero()
    {
        // Arrange
        var chunk = Chunk.CreateWithoutEmbedding(TestDocumentId, "Test", 0, 0, 4);

        // Act & Assert
        chunk.EmbeddingDimensions.Should().Be(0,
            because: "null embedding has no dimensions");
    }

    [Fact]
    public void Chunk_SupportsWithExpression_ForEmbedding()
    {
        // Arrange
        var original = Chunk.CreateWithoutEmbedding(TestDocumentId, "Test", 0, 0, 4);
        var newEmbedding = new float[] { 0.5f, 0.5f, 0.5f };

        // Act
        var withEmbedding = original with { Embedding = newEmbedding };

        // Assert
        withEmbedding.Embedding.Should().BeEquivalentTo(newEmbedding);
        withEmbedding.Content.Should().Be(original.Content);
    }

    [Fact]
    public void Chunk_WithValidOffsets_HasCorrectRange()
    {
        // Arrange
        var chunk = new Chunk(
            Id: TestId,
            DocumentId: TestDocumentId,
            Content: "Sample text",
            Embedding: null,
            ChunkIndex: 2,
            StartOffset: 100,
            EndOffset: 111);

        // Assert
        chunk.StartOffset.Should().BeLessThan(chunk.EndOffset);
        (chunk.EndOffset - chunk.StartOffset).Should().Be(chunk.ContentLength);
    }

    [Fact]
    public void Chunk_HasGetHashCode()
    {
        // Arrange
        var embedding = new float[] { 0.1f };

        var chunk1 = new Chunk(
            Id: TestId,
            DocumentId: TestDocumentId,
            Content: "Test",
            Embedding: embedding,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 4);

        var chunk2 = new Chunk(
            Id: TestId,
            DocumentId: TestDocumentId,
            Content: "Test",
            Embedding: embedding,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 4);

        // Assert
        chunk1.GetHashCode().Should().Be(chunk2.GetHashCode(),
            because: "equal records should have equal hash codes");
    }
}
