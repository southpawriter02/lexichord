// =============================================================================
// File: SyncEventPublisherTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SyncEventPublisher.
// =============================================================================
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;
using Lexichord.Modules.Knowledge.Sync.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Events;

/// <summary>
/// Unit tests for <see cref="SyncEventPublisher"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6j")]
public class SyncEventPublisherTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IEventStore> _mockEventStore;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<ILogger<SyncEventPublisher>> _mockLogger;
    private readonly SyncEventPublisher _sut;

    public SyncEventPublisherTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockEventStore = new Mock<IEventStore>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockLogger = new Mock<ILogger<SyncEventPublisher>>();

        // Default to Teams tier (full access)
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);
        _mockLicenseContext.Setup(x => x.IsFeatureEnabled(FeatureCodes.SyncEventPublisher))
            .Returns(true);

        _sut = new SyncEventPublisher(
            _mockMediator.Object,
            _mockEventStore.Object,
            _mockLicenseContext.Object,
            _mockLogger.Object);
    }

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_ValidEvent_InvokesMediator()
    {
        // Arrange
        var testEvent = CreateTestEvent();

        // Act
        await _sut.PublishAsync(testEvent);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(testEvent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_StoreInHistoryEnabled_StoresEvent()
    {
        // Arrange
        var testEvent = CreateTestEvent();
        var options = new SyncEventOptions { StoreInHistory = true };

        // Act
        await _sut.PublishAsync(testEvent, options);

        // Assert
        _mockEventStore.Verify(
            x => x.StoreAsync(It.Is<SyncEventRecord>(r =>
                r.EventId == testEvent.EventId &&
                r.DocumentId == testEvent.DocumentId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_StoreInHistoryDisabled_DoesNotStore()
    {
        // Arrange
        var testEvent = CreateTestEvent();
        var options = new SyncEventOptions { StoreInHistory = false };

        // Act
        await _sut.PublishAsync(testEvent, options);

        // Assert
        _mockEventStore.Verify(
            x => x.StoreAsync(It.IsAny<SyncEventRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishAsync_CoreTier_ThrowsUnauthorized()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.IsFeatureEnabled(FeatureCodes.SyncEventPublisher))
            .Returns(false);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Core);

        var testEvent = CreateTestEvent();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.PublishAsync(testEvent));
    }

    [Fact]
    public async Task PublishAsync_WriterProTier_Succeeds()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);
        _mockLicenseContext.Setup(x => x.IsFeatureEnabled(FeatureCodes.SyncEventPublisher))
            .Returns(true);

        var testEvent = CreateTestEvent();

        // Act
        await _sut.PublishAsync(testEvent);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(testEvent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_HandlerException_CatchEnabled_Continues()
    {
        // Arrange
        var testEvent = CreateTestEvent();
        var options = new SyncEventOptions { CatchHandlerExceptions = true };

        _mockMediator.Setup(x => x.Publish(testEvent, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler error"));

        // Act - should not throw
        await _sut.PublishAsync(testEvent, options);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(testEvent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_HandlerException_CatchDisabled_Throws()
    {
        // Arrange
        var testEvent = CreateTestEvent();
        var options = new SyncEventOptions { CatchHandlerExceptions = false };

        _mockMediator.Setup(x => x.Publish(testEvent, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PublishAsync(testEvent, options));
    }

    [Fact]
    public async Task PublishAsync_Cancelled_ThrowsOperationCanceled()
    {
        // Arrange
        var testEvent = CreateTestEvent();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.PublishAsync(testEvent, null, cts.Token));
    }

    #endregion

    #region PublishBatchAsync Tests

    [Fact]
    public async Task PublishBatchAsync_MultipleEvents_PublishesAll()
    {
        // Arrange
        var events = new List<TestSyncEvent>
        {
            CreateTestEvent(),
            CreateTestEvent(),
            CreateTestEvent()
        };

        // Act
        await _sut.PublishBatchAsync(events);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(It.IsAny<TestSyncEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task PublishBatchAsync_WriterProTier_ThrowsUnauthorized()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        var events = new List<TestSyncEvent> { CreateTestEvent() };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.PublishBatchAsync(events));
    }

    [Fact]
    public async Task PublishBatchAsync_TeamsTier_Succeeds()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        var events = new List<TestSyncEvent> { CreateTestEvent(), CreateTestEvent() };

        // Act
        await _sut.PublishBatchAsync(events);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(It.IsAny<TestSyncEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region SubscribeAsync Tests

    [Fact]
    public async Task SubscribeAsync_TeamsTier_ReturnsSubscriptionId()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        // Act
        var subscriptionId = await _sut.SubscribeAsync<TestSyncEvent>(
            async (e, ct) => await Task.CompletedTask);

        // Assert
        Assert.NotEqual(Guid.Empty, subscriptionId);
    }

    [Fact]
    public async Task SubscribeAsync_WriterProTier_ThrowsUnauthorized()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.SubscribeAsync<TestSyncEvent>(async (e, ct) => await Task.CompletedTask));
    }

    #endregion

    #region UnsubscribeAsync Tests

    [Fact]
    public async Task UnsubscribeAsync_ExistingSubscription_ReturnsTrue()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        var subscriptionId = await _sut.SubscribeAsync<TestSyncEvent>(
            async (e, ct) => await Task.CompletedTask);

        // Act
        var result = await _sut.UnsubscribeAsync<TestSyncEvent>(subscriptionId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UnsubscribeAsync_NonExistentSubscription_ReturnsFalse()
    {
        // Act
        var result = await _sut.UnsubscribeAsync<TestSyncEvent>(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetEventsAsync Tests

    [Fact]
    public async Task GetEventsAsync_ValidQuery_QueriesStore()
    {
        // Arrange
        var query = new SyncEventQuery { DocumentId = Guid.NewGuid() };
        var expectedRecords = new List<SyncEventRecord>();

        _mockEventStore.Setup(x => x.QueryAsync(It.IsAny<SyncEventQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRecords);

        // Act
        var results = await _sut.GetEventsAsync(query);

        // Assert
        _mockEventStore.Verify(
            x => x.QueryAsync(It.IsAny<SyncEventQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEventsAsync_CoreTier_ThrowsUnauthorized()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.IsFeatureEnabled(FeatureCodes.SyncEventPublisher))
            .Returns(false);

        var query = new SyncEventQuery();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetEventsAsync(query));
    }

    [Fact]
    public async Task GetEventsAsync_WriterProTier_AppliesRetentionLimit()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);
        _mockLicenseContext.Setup(x => x.IsFeatureEnabled(FeatureCodes.SyncEventPublisher))
            .Returns(true);

        var query = new SyncEventQuery
        {
            PublishedAfter = DateTimeOffset.UtcNow.AddDays(-30) // Outside retention
        };

        _mockEventStore.Setup(x => x.QueryAsync(It.IsAny<SyncEventQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncEventRecord>());

        // Act
        await _sut.GetEventsAsync(query);

        // Assert - should adjust query to 7-day retention
        _mockEventStore.Verify(
            x => x.QueryAsync(
                It.Is<SyncEventQuery>(q => q.PublishedAfter > DateTimeOffset.UtcNow.AddDays(-8)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Helper Classes

    private record TestSyncEvent : ISyncEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
        public required Guid DocumentId { get; init; }
        public IReadOnlyDictionary<string, object> Metadata { get; init; } =
            new Dictionary<string, object>();
    }

    private static TestSyncEvent CreateTestEvent()
    {
        return new TestSyncEvent { DocumentId = Guid.NewGuid() };
    }

    #endregion
}
