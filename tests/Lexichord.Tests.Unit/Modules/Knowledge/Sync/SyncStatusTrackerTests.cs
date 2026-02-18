// =============================================================================
// File: SyncStatusTrackerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SyncStatusTracker.
// =============================================================================
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Modules.Knowledge.Sync;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync;

/// <summary>
/// Unit tests for <see cref="SyncStatusTracker"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6e")]
public class SyncStatusTrackerTests
{
    private readonly Mock<ILogger<SyncStatusTracker>> _mockLogger;
    private readonly SyncStatusTracker _sut;

    public SyncStatusTrackerTests()
    {
        _mockLogger = new Mock<ILogger<SyncStatusTracker>>();
        _sut = new SyncStatusTracker(_mockLogger.Object);
    }

    [Fact]
    public async Task GetStatusAsync_NewDocument_ReturnsNeverSyncedStatus()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetStatusAsync(documentId);

        // Assert
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(SyncState.NeverSynced, result.State);
        Assert.Null(result.LastSyncAt);
        Assert.Equal(0, result.PendingChanges);
        Assert.False(result.IsSyncInProgress);
    }

    [Fact]
    public async Task UpdateStatusAsync_StoresStatus()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var status = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync,
            LastSyncAt = DateTimeOffset.UtcNow,
            PendingChanges = 0
        };

        // Act
        await _sut.UpdateStatusAsync(documentId, status);
        var result = await _sut.GetStatusAsync(documentId);

        // Assert
        Assert.Equal(SyncState.InSync, result.State);
        Assert.NotNull(result.LastSyncAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithMismatchedId_ThrowsArgumentException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var status = new SyncStatus
        {
            DocumentId = Guid.NewGuid(), // Different ID
            State = SyncState.InSync
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateStatusAsync(documentId, status));
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsSameInstance_ForSameDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result1 = await _sut.GetStatusAsync(documentId);
        var result2 = await _sut.GetStatusAsync(documentId);

        // Assert
        Assert.Equal(result1.DocumentId, result2.DocumentId);
        Assert.Equal(result1.State, result2.State);
    }

    [Fact]
    public async Task UpdateStatusAsync_TransitionsStateCorrectly()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act - Initial state
        var initial = await _sut.GetStatusAsync(documentId);
        Assert.Equal(SyncState.NeverSynced, initial.State);

        // Act - Update to PendingSync
        await _sut.UpdateStatusAsync(documentId, new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.PendingSync,
            IsSyncInProgress = true
        });
        var pending = await _sut.GetStatusAsync(documentId);
        Assert.Equal(SyncState.PendingSync, pending.State);
        Assert.True(pending.IsSyncInProgress);

        // Act - Update to InSync
        await _sut.UpdateStatusAsync(documentId, new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync,
            LastSyncAt = DateTimeOffset.UtcNow,
            IsSyncInProgress = false
        });
        var synced = await _sut.GetStatusAsync(documentId);
        Assert.Equal(SyncState.InSync, synced.State);
        Assert.False(synced.IsSyncInProgress);
    }
}
