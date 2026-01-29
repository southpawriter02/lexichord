using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Workspace.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Workspace;

/// <summary>
/// Unit tests for WorkspaceService.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the workspace service correctly:
/// - Opens workspaces with valid paths
/// - Rejects invalid paths
/// - Closes workspaces properly
/// - Manages recent workspaces
/// - Publishes MediatR events
/// - Raises local events
/// </remarks>
public class WorkspaceServiceTests : IDisposable
{
    private readonly IFileSystemWatcher _fileWatcher;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<WorkspaceService> _logger;
    private readonly WorkspaceService _sut;
    private readonly string _tempDir;

    public WorkspaceServiceTests()
    {
        _fileWatcher = Substitute.For<IFileSystemWatcher>();
        _settingsRepository = Substitute.For<ISystemSettingsRepository>();
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<WorkspaceService>>();

        // Default: no recent workspaces
        _settingsRepository.GetValueAsync("workspace:recent", "[]")
            .Returns("[]");

        _sut = new WorkspaceService(_fileWatcher, _settingsRepository, _mediator, _logger);

        // Create a real temp directory for testing
        _tempDir = Path.Combine(Path.GetTempPath(), $"lexichord-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _sut.Dispose();

        // Clean up temp directory
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region OpenWorkspaceAsync Tests

    [Fact]
    public async Task OpenWorkspaceAsync_ReturnsTrue_WhenPathExists()
    {
        // Act
        var result = await _sut.OpenWorkspaceAsync(_tempDir);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentWorkspace.Should().NotBeNull();
        _sut.CurrentWorkspace!.RootPath.Should().Be(Path.GetFullPath(_tempDir));
        _sut.IsWorkspaceOpen.Should().BeTrue();
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ReturnsFalse_WhenPathDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent");

        // Act
        var result = await _sut.OpenWorkspaceAsync(nonExistentPath);

        // Assert
        result.Should().BeFalse();
        _sut.CurrentWorkspace.Should().BeNull();
        _sut.IsWorkspaceOpen.Should().BeFalse();
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ThrowsArgumentNullException_WhenPathIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.OpenWorkspaceAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OpenWorkspaceAsync_ThrowsArgumentException_WhenPathIsEmptyOrWhitespace(string invalidPath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.OpenWorkspaceAsync(invalidPath));
    }

    [Fact]
    public async Task OpenWorkspaceAsync_StartsFileWatcher()
    {
        // Act
        await _sut.OpenWorkspaceAsync(_tempDir);

        // Assert
        _fileWatcher.Received(1).StartWatching(Arg.Any<string>(), "*.*", true);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_PublishesWorkspaceOpenedEvent()
    {
        // Act
        await _sut.OpenWorkspaceAsync(_tempDir);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<WorkspaceOpenedEvent>(e =>
                e.WorkspaceRootPath == Path.GetFullPath(_tempDir)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenWorkspaceAsync_RaisesWorkspaceChangedEvent()
    {
        // Arrange
        WorkspaceChangedEventArgs? raisedArgs = null;
        _sut.WorkspaceChanged += (_, args) => raisedArgs = args;

        // Act
        await _sut.OpenWorkspaceAsync(_tempDir);

        // Assert
        raisedArgs.Should().NotBeNull();
        raisedArgs!.ChangeType.Should().Be(WorkspaceChangeType.Opened);
        raisedArgs.PreviousWorkspace.Should().BeNull();
        raisedArgs.NewWorkspace.Should().NotBeNull();
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ClosePreviousWorkspace_WhenAlreadyOpen()
    {
        // Arrange
        var secondDir = Path.Combine(_tempDir, "second");
        Directory.CreateDirectory(secondDir);
        await _sut.OpenWorkspaceAsync(_tempDir);
        var firstWorkspace = _sut.CurrentWorkspace;

        // Act
        await _sut.OpenWorkspaceAsync(secondDir);

        // Assert
        _sut.CurrentWorkspace!.RootPath.Should().Be(Path.GetFullPath(secondDir));

        // Should have published WorkspaceClosedEvent for first workspace
        await _mediator.Received(1).Publish(
            Arg.Is<WorkspaceClosedEvent>(e => e.WorkspaceRootPath == firstWorkspace!.RootPath),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenWorkspaceAsync_AddsToRecentWorkspaces()
    {
        // Act
        await _sut.OpenWorkspaceAsync(_tempDir);

        // Assert
        await _settingsRepository.Received(1).SetValueAsync(
            "workspace:recent",
            Arg.Is<string>(s => s.Contains(Path.GetFullPath(_tempDir))),
            Arg.Any<string>());
    }

    #endregion

    #region CloseWorkspaceAsync Tests

    [Fact]
    public async Task CloseWorkspaceAsync_ClosesOpenWorkspace()
    {
        // Arrange
        await _sut.OpenWorkspaceAsync(_tempDir);

        // Act
        await _sut.CloseWorkspaceAsync();

        // Assert
        _sut.CurrentWorkspace.Should().BeNull();
        _sut.IsWorkspaceOpen.Should().BeFalse();
    }

    [Fact]
    public async Task CloseWorkspaceAsync_StopsFileWatcher()
    {
        // Arrange
        await _sut.OpenWorkspaceAsync(_tempDir);

        // Act
        await _sut.CloseWorkspaceAsync();

        // Assert
        _fileWatcher.Received(1).StopWatching();
    }

    [Fact]
    public async Task CloseWorkspaceAsync_PublishesWorkspaceClosedEvent()
    {
        // Arrange
        await _sut.OpenWorkspaceAsync(_tempDir);
        var workspace = _sut.CurrentWorkspace;

        // Act
        await _sut.CloseWorkspaceAsync();

        // Assert
        await _mediator.Received().Publish(
            Arg.Is<WorkspaceClosedEvent>(e => e.WorkspaceRootPath == workspace!.RootPath),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseWorkspaceAsync_RaisesWorkspaceChangedEvent()
    {
        // Arrange
        await _sut.OpenWorkspaceAsync(_tempDir);
        WorkspaceChangedEventArgs? raisedArgs = null;
        _sut.WorkspaceChanged += (_, args) => raisedArgs = args;

        // Act
        await _sut.CloseWorkspaceAsync();

        // Assert
        raisedArgs.Should().NotBeNull();
        raisedArgs!.ChangeType.Should().Be(WorkspaceChangeType.Closed);
        raisedArgs.PreviousWorkspace.Should().NotBeNull();
        raisedArgs.NewWorkspace.Should().BeNull();
    }

    [Fact]
    public async Task CloseWorkspaceAsync_IsIdempotent_WhenNoWorkspaceOpen()
    {
        // Act - should not throw
        await _sut.CloseWorkspaceAsync();

        // Assert
        _sut.CurrentWorkspace.Should().BeNull();
    }

    #endregion

    #region GetRecentWorkspaces Tests

    [Fact]
    public void GetRecentWorkspaces_ReturnsEmptyList_WhenNoneStored()
    {
        // Act
        var result = _sut.GetRecentWorkspaces();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetRecentWorkspaces_ReturnsStoredWorkspaces()
    {
        // Arrange
        var workspaces = new[] { "/path/one", "/path/two" };
        var json = System.Text.Json.JsonSerializer.Serialize(workspaces);
        _settingsRepository.GetValueAsync("workspace:recent", "[]").Returns(json);

        // Act
        var result = _sut.GetRecentWorkspaces();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("/path/one");
        result.Should().Contain("/path/two");
    }

    [Fact]
    public void GetRecentWorkspaces_ReturnsEmptyList_OnJsonParseError()
    {
        // Arrange
        _settingsRepository.GetValueAsync("workspace:recent", "[]").Returns("not valid json");

        // Act
        var result = _sut.GetRecentWorkspaces();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ClearRecentWorkspacesAsync Tests

    [Fact]
    public async Task ClearRecentWorkspacesAsync_ClearsPersistedList()
    {
        // Act
        await _sut.ClearRecentWorkspacesAsync();

        // Assert
        await _settingsRepository.Received(1).SetValueAsync(
            "workspace:recent",
            "[]",
            Arg.Any<string>());
    }

    #endregion

    #region Recent Workspaces Limit Tests

    [Fact]
    public async Task OpenWorkspaceAsync_LimitsRecentWorkspacesToTen()
    {
        // Arrange - create 10 existing workspaces
        var existingPaths = Enumerable.Range(1, 10)
            .Select(i => $"/path/{i}")
            .ToList();
        var json = System.Text.Json.JsonSerializer.Serialize(existingPaths);
        _settingsRepository.GetValueAsync("workspace:recent", "[]").Returns(json);

        string? capturedJson = null;
        _settingsRepository.SetValueAsync("workspace:recent", Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask)
            .AndDoes(call => capturedJson = call.ArgAt<string>(1));

        // Act - open a new workspace (11th)
        await _sut.OpenWorkspaceAsync(_tempDir);

        // Assert - verify the captured JSON has max 10 paths
        capturedJson.Should().NotBeNull();
        var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(capturedJson!);
        paths.Should().NotBeNull();
        paths!.Count.Should().BeLessOrEqualTo(10);
    }

    #endregion
}
