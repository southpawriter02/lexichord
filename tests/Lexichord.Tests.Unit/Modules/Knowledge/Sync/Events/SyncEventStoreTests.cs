// =============================================================================
// File: SyncEventStoreTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SyncEventStore.
// =============================================================================
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;
using Lexichord.Modules.Knowledge.Sync.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Events;

/// <summary>
/// Unit tests for <see cref="SyncEventStore"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6j")]
public class SyncEventStoreTests
{
    private readonly Mock<ILogger<SyncEventStore>> _mockLogger;
    private readonly SyncEventStore _sut;

    public SyncEventStoreTests()
    {
        _mockLogger = new Mock<ILogger<SyncEventStore>>();
        _sut = new SyncEventStore(_mockLogger.Object);
    }

    #region Store Tests

    [Fact]
    public async Task StoreAsync_NewRecord_StoresSuccessfully()
    {
        // Arrange
        var record = CreateRecord();

        // Act
        await _sut.StoreAsync(record);

        // Assert
        var stored = await _sut.GetAsync(record.EventId);
        Assert.NotNull(stored);
        Assert.Equal(record.EventId, stored.EventId);
        Assert.Equal(record.EventType, stored.EventType);
        Assert.Equal(record.DocumentId, stored.DocumentId);
    }

    [Fact]
    public async Task StoreAsync_DuplicateEventId_UpdatesExisting()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var record1 = CreateRecord(eventId: eventId, eventType: "Type1");
        var record2 = CreateRecord(eventId: eventId, eventType: "Type2");

        // Act
        await _sut.StoreAsync(record1);
        await _sut.StoreAsync(record2);

        // Assert
        var stored = await _sut.GetAsync(eventId);
        Assert.NotNull(stored);
        Assert.Equal("Type2", stored.EventType);
    }

    [Fact]
    public async Task StoreAsync_MultipleRecords_AllRetrievable()
    {
        // Arrange
        var records = Enumerable.Range(0, 5)
            .Select(_ => CreateRecord())
            .ToList();

        // Act
        foreach (var record in records)
        {
            await _sut.StoreAsync(record);
        }

        // Assert
        Assert.Equal(5, _sut.GetEventCount());
        foreach (var record in records)
        {
            var stored = await _sut.GetAsync(record.EventId);
            Assert.NotNull(stored);
        }
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task GetAsync_NonExistentEventId_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = await _sut.GetAsync(eventId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ExistingEventId_ReturnsRecord()
    {
        // Arrange
        var record = CreateRecord();
        await _sut.StoreAsync(record);

        // Act
        var result = await _sut.GetAsync(record.EventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(record.EventId, result.EventId);
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task QueryAsync_NoFilters_ReturnsAllEvents()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _sut.StoreAsync(CreateRecord());
        }

        var query = new SyncEventQuery();

        // Act
        var results = await _sut.QueryAsync(query);

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task QueryAsync_FilterByEventType_ReturnsMatching()
    {
        // Arrange
        await _sut.StoreAsync(CreateRecord(eventType: "SyncCompletedEvent"));
        await _sut.StoreAsync(CreateRecord(eventType: "SyncCompletedEvent"));
        await _sut.StoreAsync(CreateRecord(eventType: "SyncFailedEvent"));

        var query = new SyncEventQuery { EventType = "SyncCompletedEvent" };

        // Act
        var results = await _sut.QueryAsync(query);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("SyncCompletedEvent", r.EventType));
    }

    [Fact]
    public async Task QueryAsync_FilterByDocumentId_ReturnsMatching()
    {
        // Arrange
        var targetDocumentId = Guid.NewGuid();
        await _sut.StoreAsync(CreateRecord(documentId: targetDocumentId));
        await _sut.StoreAsync(CreateRecord(documentId: targetDocumentId));
        await _sut.StoreAsync(CreateRecord()); // Different document

        var query = new SyncEventQuery { DocumentId = targetDocumentId };

        // Act
        var results = await _sut.QueryAsync(query);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(targetDocumentId, r.DocumentId));
    }

    [Fact]
    public async Task QueryAsync_FilterByDateRange_ReturnsMatching()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-3)));
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-2)));
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-1)));

        var query = new SyncEventQuery
        {
            PublishedAfter = now.AddHours(-2.5),
            PublishedBefore = now.AddMinutes(-30)
        };

        // Act
        var results = await _sut.QueryAsync(query);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task QueryAsync_FilterBySuccessful_ReturnsMatching()
    {
        // Arrange
        await _sut.StoreAsync(CreateRecord(allSucceeded: true));
        await _sut.StoreAsync(CreateRecord(allSucceeded: true));
        await _sut.StoreAsync(CreateRecord(allSucceeded: false));

        var query = new SyncEventQuery { SuccessfulOnly = true };

        // Act
        var results = await _sut.QueryAsync(query);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.AllHandlersSucceeded));
    }

    [Fact]
    public async Task QueryAsync_SortByPublishedDescending_ReturnsNewestFirst()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-3)));
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-1)));
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-2)));

        var query = new SyncEventQuery { SortOrder = EventSortOrder.ByPublishedDescending };

        // Act
        var results = await _sut.QueryAsync(query);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results[0].PublishedAt > results[1].PublishedAt);
        Assert.True(results[1].PublishedAt > results[2].PublishedAt);
    }

    [Fact]
    public async Task QueryAsync_SortByPublishedAscending_ReturnsOldestFirst()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-3)));
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-1)));
        await _sut.StoreAsync(CreateRecord(publishedAt: now.AddHours(-2)));

        var query = new SyncEventQuery { SortOrder = EventSortOrder.ByPublishedAscending };

        // Act
        var results = await _sut.QueryAsync(query);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results[0].PublishedAt < results[1].PublishedAt);
        Assert.True(results[1].PublishedAt < results[2].PublishedAt);
    }

    [Fact]
    public async Task QueryAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _sut.StoreAsync(CreateRecord());
        }

        var query = new SyncEventQuery { PageSize = 3, PageOffset = 3 };

        // Act
        var results = await _sut.QueryAsync(query);

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task QueryAsync_PageSizeCappedAt1000()
    {
        // Arrange
        var query = new SyncEventQuery { PageSize = 2000 };

        // Act & Assert - should not throw, will cap at 1000 internally
        var results = await _sut.QueryAsync(query);
        Assert.NotNull(results);
    }

    #endregion

    #region GetByDocument Tests

    [Fact]
    public async Task GetByDocumentAsync_ExistingDocument_ReturnsEvents()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        await _sut.StoreAsync(CreateRecord(documentId: documentId));
        await _sut.StoreAsync(CreateRecord(documentId: documentId));
        await _sut.StoreAsync(CreateRecord()); // Different document

        // Act
        var results = await _sut.GetByDocumentAsync(documentId);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(documentId, r.DocumentId));
    }

    [Fact]
    public async Task GetByDocumentAsync_NonExistentDocument_ReturnsEmpty()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var results = await _sut.GetByDocumentAsync(documentId);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByDocumentAsync_WithLimit_RespectsLimit()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        for (int i = 0; i < 10; i++)
        {
            await _sut.StoreAsync(CreateRecord(documentId: documentId));
        }

        // Act
        var results = await _sut.GetByDocumentAsync(documentId, limit: 5);

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task GetByDocumentAsync_ReturnsNewestFirst()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await _sut.StoreAsync(CreateRecord(documentId: documentId, publishedAt: now.AddHours(-3)));
        await _sut.StoreAsync(CreateRecord(documentId: documentId, publishedAt: now.AddHours(-1)));
        await _sut.StoreAsync(CreateRecord(documentId: documentId, publishedAt: now.AddHours(-2)));

        // Act
        var results = await _sut.GetByDocumentAsync(documentId);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results[0].PublishedAt > results[1].PublishedAt);
        Assert.True(results[1].PublishedAt > results[2].PublishedAt);
    }

    #endregion

    #region Utility Methods

    [Fact]
    public async Task GetEventCount_AfterStoring_ReturnsCorrectCount()
    {
        // Arrange
        await _sut.StoreAsync(CreateRecord());
        await _sut.StoreAsync(CreateRecord());
        await _sut.StoreAsync(CreateRecord());

        // Act
        var count = _sut.GetEventCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetDocumentCount_MultipleDocuments_ReturnsCorrectCount()
    {
        // Arrange
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        await _sut.StoreAsync(CreateRecord(documentId: doc1));
        await _sut.StoreAsync(CreateRecord(documentId: doc1));
        await _sut.StoreAsync(CreateRecord(documentId: doc2));

        // Act
        var count = _sut.GetDocumentCount();

        // Assert
        Assert.Equal(2, count);
    }

    #endregion

    #region Helper Methods

    private static SyncEventRecord CreateRecord(
        Guid? eventId = null,
        string eventType = "TestEvent",
        Guid? documentId = null,
        DateTimeOffset? publishedAt = null,
        bool allSucceeded = true)
    {
        return new SyncEventRecord
        {
            EventId = eventId ?? Guid.NewGuid(),
            EventType = eventType,
            DocumentId = documentId ?? Guid.NewGuid(),
            PublishedAt = publishedAt ?? DateTimeOffset.UtcNow,
            Payload = "{}",
            HandlersExecuted = 1,
            HandlersFailed = allSucceeded ? 0 : 1,
            TotalDuration = TimeSpan.FromMilliseconds(50),
            AllHandlersSucceeded = allSucceeded,
            HandlerErrors = allSucceeded ? [] : ["Test error"]
        };
    }

    #endregion
}
