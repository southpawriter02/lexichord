using Lexichord.Abstractions.Contracts.Ingestion;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Unit tests for the <see cref="IngestionResult"/> record.
/// </summary>
public class IngestionResultRecordTests
{
    private static readonly Guid TestDocumentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly TimeSpan TestDuration = TimeSpan.FromSeconds(5);

    #region Record Equality Tests

    [Fact]
    public void IngestionResult_WithSameValues_AreEqual()
    {
        // Arrange
        var result1 = new IngestionResult(
            Success: true,
            DocumentId: TestDocumentId,
            ChunkCount: 10,
            Duration: TestDuration,
            SkippedFiles: [],
            Errors: []);

        var result2 = new IngestionResult(
            Success: true,
            DocumentId: TestDocumentId,
            ChunkCount: 10,
            Duration: TestDuration,
            SkippedFiles: [],
            Errors: []);

        // Assert
        result1.Should().Be(result2, because: "records with identical values should be equal");
    }

    [Fact]
    public void IngestionResult_WithDifferentSuccess_AreNotEqual()
    {
        // Arrange
        var result1 = new IngestionResult(true, TestDocumentId, 10, TestDuration, [], []);
        var result2 = result1 with { Success = false };

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void IngestionResult_SupportsWithExpression()
    {
        // Arrange
        var original = new IngestionResult(true, TestDocumentId, 10, TestDuration, [], []);

        // Act
        var updated = original with { ChunkCount = 20 };

        // Assert
        updated.ChunkCount.Should().Be(20);
        updated.DocumentId.Should().Be(TestDocumentId, because: "unchanged properties should be preserved");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void CreateSuccess_SetsCorrectValues()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var chunkCount = 15;
        var duration = TimeSpan.FromSeconds(3);

        // Act
        var result = IngestionResult.CreateSuccess(documentId, chunkCount, duration);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentId.Should().Be(documentId);
        result.ChunkCount.Should().Be(chunkCount);
        result.Duration.Should().Be(duration);
        result.SkippedFiles.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateBatchSuccess_SetsCorrectValues()
    {
        // Arrange
        var chunkCount = 100;
        var duration = TimeSpan.FromMinutes(2);
        var skipped = new List<string> { "file1.bin", "file2.exe" };

        // Act
        var result = IngestionResult.CreateBatchSuccess(chunkCount, duration, skipped);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentId.Should().BeNull(because: "batch operations don't have a single document ID");
        result.ChunkCount.Should().Be(chunkCount);
        result.Duration.Should().Be(duration);
        result.SkippedFiles.Should().BeEquivalentTo(skipped);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateBatchSuccess_WithoutSkippedFiles_HasEmptyList()
    {
        // Act
        var result = IngestionResult.CreateBatchSuccess(50, TimeSpan.FromSeconds(10));

        // Assert
        result.SkippedFiles.Should().BeEmpty();
    }

    [Fact]
    public void CreateFailure_SingleError_SetsCorrectValues()
    {
        // Arrange
        var error = "File not found";
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        var result = IngestionResult.CreateFailure(error, duration);

        // Assert
        result.Success.Should().BeFalse();
        result.DocumentId.Should().BeNull();
        result.ChunkCount.Should().Be(0);
        result.Duration.Should().Be(duration);
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void CreateFailure_MultipleErrors_SetsCorrectValues()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };
        var duration = TimeSpan.FromSeconds(1);

        // Act
        var result = IngestionResult.CreateFailure(errors, duration);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void CreateSkipped_SetsCorrectValues()
    {
        // Arrange
        var filePath = "docs/large-file.pdf";
        var reason = "File exceeds maximum size limit";

        // Act
        var result = IngestionResult.CreateSkipped(filePath, reason);

        // Assert
        result.Success.Should().BeFalse();
        result.DocumentId.Should().BeNull();
        result.ChunkCount.Should().Be(0);
        result.Duration.Should().Be(TimeSpan.Zero);
        result.SkippedFiles.Should().ContainSingle().Which.Should().Be(filePath);
        result.Errors.Should().ContainSingle().Which.Should().Be(reason);
    }

    #endregion

    #region HashCode Tests

    [Fact]
    public void IngestionResult_EqualRecords_HaveEqualHashCodes()
    {
        // Arrange
        var result1 = IngestionResult.CreateSuccess(TestDocumentId, 10, TestDuration);
        var result2 = IngestionResult.CreateSuccess(TestDocumentId, 10, TestDuration);

        // Assert
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    #endregion
}
