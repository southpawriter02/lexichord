using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for RecentFilesService.
/// </summary>
[Trait("Category", "Unit")]
public class RecentFilesServiceTests
{
    private readonly Mock<IRecentFilesRepository> _repositoryMock = new();
    private readonly Mock<ILogger<RecentFilesService>> _loggerMock = new();
    private readonly RecentFilesOptions _options = new() { MaxEntries = 10 };

    private RecentFilesService CreateService() =>
        new(_repositoryMock.Object, Options.Create(_options), _loggerMock.Object);

    [Fact]
    public async Task GetRecentFilesAsync_ReturnsEntriesFromRepository()
    {
        // Arrange
        var entries = new List<RecentFileEntry>
        {
            new("path1.txt", "file1.txt", DateTimeOffset.UtcNow, 1, Exists: true),
            new("path2.txt", "file2.txt", DateTimeOffset.UtcNow, 2, Exists: true)
        };
        _repositoryMock
            .Setup(r => r.GetRecentFilesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        var service = CreateService();

        // Act
        var result = await service.GetRecentFilesAsync();

        // Assert
        result.Should().HaveCount(2);
        _repositoryMock.Verify(r => r.GetRecentFilesAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecentFilesAsync_CapsToMaxEntries()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetRecentFilesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentFileEntry>());
        var service = CreateService();

        // Act
        await service.GetRecentFilesAsync(maxCount: 100);

        // Assert - should cap at MaxEntries (10)
        _repositoryMock.Verify(r => r.GetRecentFilesAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRecentFileAsync_UpsertsThenTrims()
    {
        // Arrange
        var service = CreateService();
        var filePath = "/path/to/file.txt";

        // Act
        await service.AddRecentFileAsync(filePath);

        // Assert
        _repositoryMock.Verify(
            r => r.UpsertAsync(It.Is<RecentFileEntry>(e => e.FilePath == filePath), It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(
            r => r.TrimToCountAsync(10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddRecentFileAsync_RaisesAddedEvent()
    {
        // Arrange
        var service = CreateService();
        RecentFilesChangedEventArgs? eventArgs = null;
        service.RecentFilesChanged += (_, args) => eventArgs = args;

        // Act
        await service.AddRecentFileAsync("/path/to/file.txt");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.ChangeType.Should().Be(RecentFilesChangeType.Added);
        eventArgs.FilePath.Should().Be("/path/to/file.txt");
    }

    [Fact]
    public async Task AddRecentFileAsync_ThrowsOnEmptyPath()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddRecentFileAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddRecentFileAsync("   "));
    }

    [Fact]
    public async Task RemoveRecentFileAsync_DeletesAndRaisesEvent()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService();
        RecentFilesChangedEventArgs? eventArgs = null;
        service.RecentFilesChanged += (_, args) => eventArgs = args;

        // Act
        await service.RemoveRecentFileAsync("/path/to/file.txt");

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync("/path/to/file.txt", It.IsAny<CancellationToken>()), Times.Once);
        eventArgs.Should().NotBeNull();
        eventArgs!.ChangeType.Should().Be(RecentFilesChangeType.Removed);
    }

    [Fact]
    public async Task RemoveRecentFileAsync_DoesNotRaiseEventWhenNotFound()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService();
        var eventRaised = false;
        service.RecentFilesChanged += (_, _) => eventRaised = true;

        // Act
        await service.RemoveRecentFileAsync("/nonexistent");

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task ClearHistoryAsync_ClearsAllAndRaisesEvent()
    {
        // Arrange
        var service = CreateService();
        RecentFilesChangedEventArgs? eventArgs = null;
        service.RecentFilesChanged += (_, args) => eventArgs = args;

        // Act
        await service.ClearHistoryAsync();

        // Assert
        _repositoryMock.Verify(r => r.ClearAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        eventArgs.Should().NotBeNull();
        eventArgs!.ChangeType.Should().Be(RecentFilesChangeType.Cleared);
    }

    [Fact]
    public async Task Handle_FileOpenedEvent_AddsToRecent()
    {
        // Arrange
        var service = CreateService();
        var notification = new FileOpenedEvent("/path/to/file.txt", DateTimeOffset.UtcNow, FileOpenSource.Menu);

        // Act
        await service.Handle(notification, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.UpsertAsync(It.Is<RecentFileEntry>(e => e.FilePath == "/path/to/file.txt"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void MaxEntries_ReturnsConfiguredValue()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.MaxEntries.Should().Be(10);
    }
}
