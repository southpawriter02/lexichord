using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for the <see cref="DeduplicatedSearchResult"/> record.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: These tests verify the deduplication-aware search result wrapper's
/// properties, including helper properties for canonical/variant detection
/// and the factory method for converting from basic results.
/// </para>
/// <para>
/// v0.5.9f: Retrieval Integration
/// </para>
/// </remarks>
public class DeduplicatedSearchResultTests
{
    private static readonly Guid TestChunkId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid TestDocumentId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid TestCanonicalId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static Chunk CreateTestChunk(Guid? id = null)
    {
        return new Chunk(
            Id: id ?? TestChunkId,
            DocumentId: TestDocumentId,
            Content: "Test chunk content for deduplication results.",
            Embedding: new float[] { 0.1f, 0.2f, 0.3f },
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 45);
    }

    private static ChunkProvenance CreateTestProvenance(Guid? chunkId = null, string? sourceLocation = null)
    {
        return ChunkProvenance.Create(
            chunkId ?? TestChunkId,
            TestDocumentId,
            sourceLocation ?? "/path/to/document.md");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_StoresAllProperties()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var provenance = new List<ChunkProvenance> { CreateTestProvenance() };

        // Act
        var result = new DeduplicatedSearchResult(
            Chunk: chunk,
            SimilarityScore: 0.95,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 3,
            HasContradictions: true,
            Provenance: provenance);

        // Assert
        result.Chunk.Should().Be(chunk);
        result.SimilarityScore.Should().Be(0.95);
        result.CanonicalRecordId.Should().Be(TestCanonicalId);
        result.VariantCount.Should().Be(3);
        result.HasContradictions.Should().BeTrue();
        result.Provenance.Should().BeEquivalentTo(provenance);
    }

    [Fact]
    public void Constructor_AllowsNullProvenance()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Act
        var result = new DeduplicatedSearchResult(
            Chunk: chunk,
            SimilarityScore: 0.85,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.Provenance.Should().BeNull();
    }

    [Fact]
    public void Constructor_AllowsNullCanonicalRecordId()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Act
        var result = new DeduplicatedSearchResult(
            Chunk: chunk,
            SimilarityScore: 0.85,
            CanonicalRecordId: null,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.CanonicalRecordId.Should().BeNull();
    }

    #endregion

    #region Helper Property Tests

    [Fact]
    public void IsCanonical_ReturnsTrue_WhenCanonicalRecordIdIsSet()
    {
        // Arrange
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.95,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 2,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.IsCanonical.Should().BeTrue(
            because: "chunk with a canonical record ID is a canonical chunk");
    }

    [Fact]
    public void IsCanonical_ReturnsFalse_WhenCanonicalRecordIdIsNull()
    {
        // Arrange
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.85,
            CanonicalRecordId: null,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.IsCanonical.Should().BeFalse(
            because: "chunk without a canonical record ID is not a canonical chunk");
    }

    [Fact]
    public void IsStandalone_ReturnsTrue_WhenNotCanonicalAndNoVariants()
    {
        // Arrange
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.85,
            CanonicalRecordId: null,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.IsStandalone.Should().BeTrue(
            because: "chunk without canonical record and no variants is standalone");
    }

    [Fact]
    public void IsStandalone_ReturnsFalse_WhenHasCanonicalRecordId()
    {
        // Arrange
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.95,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.IsStandalone.Should().BeFalse(
            because: "chunk with a canonical record ID is not standalone");
    }

    [Fact]
    public void HasVariants_ReturnsTrue_WhenVariantCountGreaterThanZero()
    {
        // Arrange
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.95,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 5,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.HasVariants.Should().BeTrue(
            because: "variant count of 5 indicates variants exist");
    }

    [Fact]
    public void HasVariants_ReturnsFalse_WhenVariantCountIsZero()
    {
        // Arrange
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.85,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.HasVariants.Should().BeFalse(
            because: "variant count of 0 indicates no variants");
    }

    [Fact]
    public void HasProvenance_ReturnsTrue_WhenProvenanceListIsNotEmpty()
    {
        // Arrange
        var provenance = new List<ChunkProvenance> { CreateTestProvenance() };
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.95,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: provenance);

        // Assert
        result.HasProvenance.Should().BeTrue(
            because: "provenance list with entries indicates provenance exists");
    }

    [Fact]
    public void HasProvenance_ReturnsFalse_WhenProvenanceIsNull()
    {
        // Arrange
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.85,
            CanonicalRecordId: null,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.HasProvenance.Should().BeFalse(
            because: "null provenance indicates no provenance loaded");
    }

    [Fact]
    public void HasProvenance_ReturnsFalse_WhenProvenanceIsEmpty()
    {
        // Arrange
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.85,
            CanonicalRecordId: null,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: new List<ChunkProvenance>());

        // Assert
        result.HasProvenance.Should().BeFalse(
            because: "empty provenance list indicates no provenance loaded");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void FromBasicResult_CreatesStandaloneResult()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var basicResult = new ChunkSearchResult(chunk, 0.85);

        // Act
        var result = DeduplicatedSearchResult.FromBasicResult(basicResult);

        // Assert
        result.Chunk.Should().Be(chunk);
        result.SimilarityScore.Should().Be(0.85);
        result.CanonicalRecordId.Should().BeNull("standalone conversion has no canonical");
        result.VariantCount.Should().Be(0, "standalone conversion has no variants");
        result.HasContradictions.Should().BeFalse("standalone conversion assumes no contradictions");
        result.Provenance.Should().BeNull("standalone conversion has no provenance");
        result.IsStandalone.Should().BeTrue();
    }

    [Fact]
    public void FromBasicResult_PreservesChunkAndScore()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var basicResult = new ChunkSearchResult(chunk, 0.92);

        // Act
        var result = DeduplicatedSearchResult.FromBasicResult(basicResult);

        // Assert
        result.Chunk.Should().BeSameAs(chunk, because: "chunk reference should be preserved");
        result.SimilarityScore.Should().Be(0.92, because: "score should be preserved exactly");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var provenance = new List<ChunkProvenance>();

        var result1 = new DeduplicatedSearchResult(
            Chunk: chunk,
            SimilarityScore: 0.9,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 2,
            HasContradictions: false,
            Provenance: provenance);

        var result2 = new DeduplicatedSearchResult(
            Chunk: chunk,
            SimilarityScore: 0.9,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 2,
            HasContradictions: false,
            Provenance: provenance);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void Records_WithDifferentVariantCounts_AreNotEqual()
    {
        // Arrange
        var chunk = CreateTestChunk();

        var result1 = new DeduplicatedSearchResult(
            Chunk: chunk,
            SimilarityScore: 0.9,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 2,
            HasContradictions: false,
            Provenance: null);

        var result2 = new DeduplicatedSearchResult(
            Chunk: chunk,
            SimilarityScore: 0.9,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 3,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Record_SupportsWithExpression()
    {
        // Arrange
        var original = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.85,
            CanonicalRecordId: null,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);

        // Act
        var updated = original with
        {
            CanonicalRecordId = TestCanonicalId,
            VariantCount = 3
        };

        // Assert
        updated.CanonicalRecordId.Should().Be(TestCanonicalId);
        updated.VariantCount.Should().Be(3);
        updated.Chunk.Should().Be(original.Chunk);
        updated.SimilarityScore.Should().Be(original.SimilarityScore);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void CanonicalChunk_WithMultipleVariants_ReportsCorrectly()
    {
        // Arrange - A canonical chunk with 10 merged variants
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.98,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 10,
            HasContradictions: false,
            Provenance: null);

        // Assert
        result.IsCanonical.Should().BeTrue();
        result.IsStandalone.Should().BeFalse();
        result.HasVariants.Should().BeTrue();
        result.VariantCount.Should().Be(10);
    }

    [Fact]
    public void CanonicalChunk_WithContradictions_FlagsCorrectly()
    {
        // Arrange - A canonical chunk that has active contradictions
        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.92,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 2,
            HasContradictions: true,
            Provenance: null);

        // Assert
        result.HasContradictions.Should().BeTrue();
        result.IsCanonical.Should().BeTrue();
    }

    [Fact]
    public void MultipleProvenance_RecordsOrderPreserved()
    {
        // Arrange - Multiple provenance records from different sources
        var provenance1 = ChunkProvenance.Create(TestChunkId, TestDocumentId, "/doc1.md");
        var provenance2 = ChunkProvenance.Create(TestChunkId, TestDocumentId, "/doc2.md");
        var provenance3 = ChunkProvenance.Create(TestChunkId, TestDocumentId, "/doc3.md");
        var provenanceList = new List<ChunkProvenance> { provenance1, provenance2, provenance3 };

        var result = new DeduplicatedSearchResult(
            Chunk: CreateTestChunk(),
            SimilarityScore: 0.95,
            CanonicalRecordId: TestCanonicalId,
            VariantCount: 3,
            HasContradictions: false,
            Provenance: provenanceList);

        // Assert
        result.Provenance.Should().HaveCount(3);
        result.Provenance![0].SourceLocation.Should().Be("/doc1.md");
        result.Provenance![1].SourceLocation.Should().Be("/doc2.md");
        result.Provenance![2].SourceLocation.Should().Be("/doc3.md");
    }

    #endregion
}
