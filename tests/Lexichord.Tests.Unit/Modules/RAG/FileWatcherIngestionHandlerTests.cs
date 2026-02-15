using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Ingestion;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for the <see cref="FileWatcherIngestionHandler"/> service.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the filtering, debouncing, and event publishing logic
/// of the file watcher integration handler.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.2c")]
public class FileWatcherIngestionHandlerTests : IDisposable
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly FileWatcherOptions _options;
    private readonly FileWatcherIngestionHandler _sut;

    public FileWatcherIngestionHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _options = FileWatcherOptions.Default;
        _sut = new FileWatcherIngestionHandler(
            _mediatorMock.Object,
            Options.Create(_options),
            NullLogger<FileWatcherIngestionHandler>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FileWatcherIngestionHandler(
            null!,
            Options.Create(FileWatcherOptions.Default),
            NullLogger<FileWatcherIngestionHandler>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FileWatcherIngestionHandler(
            _mediatorMock.Object,
            null!,
            NullLogger<FileWatcherIngestionHandler>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FileWatcherIngestionHandler(
            _mediatorMock.Object,
            Options.Create(FileWatcherOptions.Default),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Disabled Handler Tests

    [Fact]
    public async Task Handle_WhenDisabled_DoesNotPublishEvents()
    {
        // Arrange
        var disabledOptions = FileWatcherOptions.Default with { Enabled = false };
        using var handler = new FileWatcherIngestionHandler(
            _mediatorMock.Object,
            Options.Create(disabledOptions),
            NullLogger<FileWatcherIngestionHandler>.Instance);

        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Created, "/project/readme.md", null, false));

        // Act
        await handler.Handle(changes, CancellationToken.None);
        await Task.Delay(500); // Wait for any potential debounce

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<FileIndexingRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Extension Filtering Tests

    [Theory]
    [InlineData("/project/readme.md", true)]
    [InlineData("/project/notes.txt", true)]
    [InlineData("/project/config.json", true)]
    [InlineData("/project/settings.yaml", true)]
    [InlineData("/project/code.cs", false)]
    [InlineData("/project/binary.exe", false)]
    [InlineData("/project/image.png", false)]
    public async Task Handle_FiltersByExtension(string filePath, bool shouldPublish)
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Created, filePath, null, false));

        // Act
        await _sut.Handle(changes, CancellationToken.None);

        // Wait for debounce to fire
        await Task.Delay(_options.DebounceDelayMs + 500);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<FileIndexingRequestedEvent>(e => e.FilePath == filePath),
                It.IsAny<CancellationToken>()),
            shouldPublish ? Times.Once() : Times.Never());
    }

    #endregion

    #region Directory Exclusion Tests

    [Theory]
    [InlineData("/project/.git/config.md", false)]
    [InlineData("/project/node_modules/package.json", false)]
    [InlineData("/project/bin/debug/readme.md", false)]
    [InlineData("/project/obj/readme.txt", false)]
    [InlineData("/project/src/readme.md", true)]
    [InlineData("/project/docs/guide.txt", true)]
    public async Task Handle_FiltersByExcludedDirectory(string filePath, bool shouldPublish)
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Created, filePath, null, false));

        // Act
        await _sut.Handle(changes, CancellationToken.None);

        // Wait for debounce
        await Task.Delay(_options.DebounceDelayMs + 500);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<FileIndexingRequestedEvent>(e => 
                    e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) ||
                    e.FilePath.Replace('\\', '/').ToLowerInvariant() == filePath.Replace('\\', '/').ToLowerInvariant()),
                It.IsAny<CancellationToken>()),
            shouldPublish ? Times.Once() : Times.Never());
    }

    #endregion

    #region Directory Change Tests

    [Fact]
    public async Task Handle_DirectoryChanges_AreSkipped()
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Created, "/project/docs", null, IsDirectory: true));

        // Act
        await _sut.Handle(changes, CancellationToken.None);
        await Task.Delay(_options.DebounceDelayMs + 500);
        await Task.Delay(_options.DebounceDelayMs + 500);
        await Task.Delay(_options.DebounceDelayMs + 500);
        await Task.Delay(_options.DebounceDelayMs + 500);
        await Task.Delay(_options.DebounceDelayMs + 500);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<FileIndexingRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Deleted File Tests

    [Fact]
    public async Task Handle_DeletedFiles_AreSkipped()
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Deleted, "/project/readme.md", null, false));

        // Act
        await _sut.Handle(changes, CancellationToken.None);
        await Task.Delay(_options.DebounceDelayMs + 100);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<FileIndexingRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Change Type Mapping Tests

    [Fact]
    public async Task Handle_CreatedFile_PublishesForCreated()
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Created, "/project/readme.md", null, false));

        // Act
        await _sut.Handle(changes, CancellationToken.None);
        await Task.Delay(_options.DebounceDelayMs + 100);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<FileIndexingRequestedEvent>(e => e.ChangeType == FileIndexingChangeType.Created),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ChangedFile_PublishesForChanged()
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Changed, "/project/readme.md", null, false));

        // Act
        await _sut.Handle(changes, CancellationToken.None);
        await Task.Delay(_options.DebounceDelayMs + 100);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<FileIndexingRequestedEvent>(e => e.ChangeType == FileIndexingChangeType.Changed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RenamedFile_PublishesForRenamedWithOldPath()
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(
                FileSystemChangeType.Renamed,
                "/project/new-readme.md",
                "/project/old-readme.md",
                false));

        // Act
        await _sut.Handle(changes, CancellationToken.None);
        await Task.Delay(_options.DebounceDelayMs + 100);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<FileIndexingRequestedEvent>(e =>
                    e.ChangeType == FileIndexingChangeType.Renamed &&
                    e.OldPath != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Debouncing Tests

    [Fact]
    public async Task Handle_RapidChangesToSameFile_PublishesOnce()
    {
        // Arrange - rapid changes to same file
        var filePath = "/project/readme.md";

        // Act - simulate rapid changes
        for (int i = 0; i < 5; i++)
        {
            var changes = CreateChangeBatch(
                new FileSystemChangeInfo(FileSystemChangeType.Changed, filePath, null, false));
            await _sut.Handle(changes, CancellationToken.None);
            await Task.Delay(50); // Less than debounce delay
        }

        // Wait for debounce
        await Task.Delay(_options.DebounceDelayMs + 500);

        // Assert - should publish only once after debounce
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<FileIndexingRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ChangesToDifferentFiles_PublishesForEach()
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Changed, "/project/file1.md", null, false),
            new FileSystemChangeInfo(FileSystemChangeType.Changed, "/project/file2.md", null, false),
            new FileSystemChangeInfo(FileSystemChangeType.Changed, "/project/file3.md", null, false));

        // Act
        await _sut.Handle(changes, CancellationToken.None);
        await Task.Delay(_options.DebounceDelayMs + 500);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<FileIndexingRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    #endregion

    #region Batch Processing Tests

    [Fact]
    public async Task Handle_BatchWithMixedValidAndInvalid_ProcessesOnlyValid()
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Created, "/project/readme.md", null, false), // valid
            new FileSystemChangeInfo(FileSystemChangeType.Created, "/project/.git/config", null, false), // excluded dir
            new FileSystemChangeInfo(FileSystemChangeType.Created, "/project/code.cs", null, false), // wrong ext
            new FileSystemChangeInfo(FileSystemChangeType.Deleted, "/project/notes.txt", null, false), // deleted
            new FileSystemChangeInfo(FileSystemChangeType.Created, "/project/guide.txt", null, false)); // valid

        // Act
        await _sut.Handle(changes, CancellationToken.None);
        await Task.Delay(_options.DebounceDelayMs + 500);

        // Assert - only 2 valid files should be published
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<FileIndexingRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_CancelsPendingTimers()
    {
        // Arrange
        var changes = CreateChangeBatch(
            new FileSystemChangeInfo(FileSystemChangeType.Created, "/project/readme.md", null, false));
        await _sut.Handle(changes, CancellationToken.None);

        // Act - dispose immediately before debounce fires
        _sut.Dispose();
        await Task.Delay(_options.DebounceDelayMs + 500);

        // Assert - no event should be published after disposal
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<FileIndexingRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act
        _sut.Dispose();
        var act = () => _sut.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region INotificationHandler Implementation Tests

    [Fact]
    public void Handler_ImplementsINotificationHandler()
    {
        // Assert
        _sut.Should().BeAssignableTo<INotificationHandler<ExternalFileChangesEvent>>();
    }

    #endregion

    #region Helper Methods

    private static ExternalFileChangesEvent CreateChangeBatch(params FileSystemChangeInfo[] changes)
    {
        return new ExternalFileChangesEvent(changes.ToList().AsReadOnly());
    }

    #endregion
}
