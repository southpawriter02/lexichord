// =============================================================================
// File: ExtractionLineageStoreTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ExtractionLineageStore.
// =============================================================================
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Modules.Knowledge.Sync.DocToGraph;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.DocToGraph;

/// <summary>
/// Unit tests for <see cref="ExtractionLineageStore"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6f")]
public class ExtractionLineageStoreTests
{
    private readonly Mock<ILogger<ExtractionLineageStore>> _mockLogger;
    private readonly ExtractionLineageStore _sut;

    public ExtractionLineageStoreTests()
    {
        _mockLogger = new Mock<ILogger<ExtractionLineageStore>>();
        _sut = new ExtractionLineageStore(_mockLogger.Object);
    }

    #region RecordExtractionAsync Tests

    [Fact]
    public async Task RecordExtractionAsync_StoresRecord()
    {
        // Arrange
        var record = CreateTestRecord();

        // Act
        await _sut.RecordExtractionAsync(record);
        var lineage = await _sut.GetLineageAsync(record.DocumentId);

        // Assert
        Assert.Single(lineage);
        Assert.Equal(record.ExtractionId, lineage[0].ExtractionId);
    }

    [Fact]
    public async Task RecordExtractionAsync_MultipleRecords_MaintainsChronologicalOrder()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var record1 = CreateTestRecord(documentId, DateTimeOffset.UtcNow.AddHours(-2));
        var record2 = CreateTestRecord(documentId, DateTimeOffset.UtcNow.AddHours(-1));
        var record3 = CreateTestRecord(documentId, DateTimeOffset.UtcNow);

        // Act - Insert in reverse order
        await _sut.RecordExtractionAsync(record1);
        await _sut.RecordExtractionAsync(record2);
        await _sut.RecordExtractionAsync(record3);

        var lineage = await _sut.GetLineageAsync(documentId);

        // Assert - Most recent first
        Assert.Equal(3, lineage.Count);
        Assert.Equal(record3.ExtractionId, lineage[0].ExtractionId);
        Assert.Equal(record2.ExtractionId, lineage[1].ExtractionId);
        Assert.Equal(record1.ExtractionId, lineage[2].ExtractionId);
    }

    #endregion

    #region GetLineageAsync Tests

    [Fact]
    public async Task GetLineageAsync_WithNoRecords_ReturnsEmptyList()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetLineageAsync(documentId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region FindExtractionAtAsync Tests

    [Fact]
    public async Task FindExtractionAtAsync_FindsRecordAtOrBeforeTarget()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var targetTime = DateTimeOffset.UtcNow.AddHours(-1);
        var record1 = CreateTestRecord(documentId, DateTimeOffset.UtcNow.AddHours(-2)); // Before target
        var record2 = CreateTestRecord(documentId, DateTimeOffset.UtcNow); // After target

        await _sut.RecordExtractionAsync(record1);
        await _sut.RecordExtractionAsync(record2);

        // Act
        var result = await _sut.FindExtractionAtAsync(documentId, targetTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(record1.ExtractionId, result.ExtractionId);
    }

    [Fact]
    public async Task FindExtractionAtAsync_WithNoMatchingRecord_ReturnsNull()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var veryOldTarget = DateTimeOffset.UtcNow.AddYears(-1);
        var record = CreateTestRecord(documentId, DateTimeOffset.UtcNow);

        await _sut.RecordExtractionAsync(record);

        // Act
        var result = await _sut.FindExtractionAtAsync(documentId, veryOldTarget);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetExtractionsAfterAsync Tests

    [Fact]
    public async Task GetExtractionsAfterAsync_ReturnsNewerExtractions()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var record1 = CreateTestRecord(documentId, DateTimeOffset.UtcNow.AddHours(-2));
        var record2 = CreateTestRecord(documentId, DateTimeOffset.UtcNow.AddHours(-1));
        var record3 = CreateTestRecord(documentId, DateTimeOffset.UtcNow);

        await _sut.RecordExtractionAsync(record1);
        await _sut.RecordExtractionAsync(record2);
        await _sut.RecordExtractionAsync(record3);

        // Act
        var result = await _sut.GetExtractionsAfterAsync(documentId, record1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.ExtractionId == record2.ExtractionId);
        Assert.Contains(result, r => r.ExtractionId == record3.ExtractionId);
    }

    #endregion

    #region GetLatestExtractionHashAsync Tests

    [Fact]
    public async Task GetLatestExtractionHashAsync_ReturnsHashFromMostRecentRecord()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var record1 = CreateTestRecord(documentId, DateTimeOffset.UtcNow.AddHours(-1), "old-hash");
        var record2 = CreateTestRecord(documentId, DateTimeOffset.UtcNow, "new-hash");

        await _sut.RecordExtractionAsync(record1);
        await _sut.RecordExtractionAsync(record2);

        // Act
        var result = await _sut.GetLatestExtractionHashAsync(documentId);

        // Assert
        Assert.Equal("new-hash", result);
    }

    [Fact]
    public async Task GetLatestExtractionHashAsync_WithNoRecords_ReturnsNull()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetLatestExtractionHashAsync(documentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Helper Methods

    private static ExtractionRecord CreateTestRecord(
        Guid? documentId = null,
        DateTimeOffset? extractedAt = null,
        string? hash = null)
    {
        return new ExtractionRecord
        {
            ExtractionId = Guid.NewGuid(),
            DocumentId = documentId ?? Guid.NewGuid(),
            DocumentHash = "doc-hash",
            ExtractedAt = extractedAt ?? DateTimeOffset.UtcNow,
            ExtractedBy = null,
            EntityIds = [Guid.NewGuid()],
            ClaimIds = [],
            RelationshipIds = [],
            ExtractionHash = hash ?? "test-extraction-hash"
        };
    }

    #endregion
}
