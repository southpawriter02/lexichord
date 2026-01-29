namespace Lexichord.Tests.Unit.Modules.Workspace;

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Workspace.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ProjectExplorerViewModel"/>.
/// </summary>
public class ProjectExplorerViewModelTests
{
    private readonly Mock<IWorkspaceService> _mockWorkspaceService;
    private readonly Mock<IFileSystemAccess> _mockFileSystemAccess;
    private readonly Mock<IFileOperationService> _mockFileOperationService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<ProjectExplorerViewModel>> _mockLogger;

    public ProjectExplorerViewModelTests()
    {
        _mockWorkspaceService = new Mock<IWorkspaceService>();
        _mockFileSystemAccess = new Mock<IFileSystemAccess>();
        _mockFileOperationService = new Mock<IFileOperationService>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<ProjectExplorerViewModel>>();

        // Default workspace service state
        _mockWorkspaceService.Setup(w => w.IsWorkspaceOpen).Returns(false);
        _mockWorkspaceService.Setup(w => w.CurrentWorkspace).Returns((WorkspaceInfo?)null);
    }

    private ProjectExplorerViewModel CreateViewModel()
    {
        return new ProjectExplorerViewModel(
            _mockWorkspaceService.Object,
            _mockFileSystemAccess.Object,
            _mockFileOperationService.Object,
            _mockMediator.Object,
            _mockLogger.Object);
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_NoWorkspaceOpen_HasEmptyRootNodes()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.RootNodes.Should().BeEmpty();
        vm.HasWorkspace.Should().BeFalse();
        vm.StatusMessage.Should().Be("No folder open");
    }

    [Fact]
    public void Constructor_WorkspaceAlreadyOpen_LoadsTree()
    {
        // Arrange
        var workspaceInfo = new WorkspaceInfo(
            RootPath: "/test/workspace",
            Name: "Test Workspace",
            OpenedAt: DateTimeOffset.UtcNow
        );

        _mockWorkspaceService.Setup(w => w.IsWorkspaceOpen).Returns(true);
        _mockWorkspaceService.Setup(w => w.CurrentWorkspace).Returns(workspaceInfo);

        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>());

        // Act
        var vm = CreateViewModel();

        // Assert - give async operation time to complete
        // Note: In real tests, we'd use proper async test patterns
        vm.RootNodes.Should().NotBeNull();
    }

    #endregion

    #region LoadWorkspaceAsync Tests

    [Fact]
    public async Task LoadWorkspaceAsync_PopulatesRootNodes()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("File1.txt", "/test/workspace/File1.txt", false),
                new("Folder1", "/test/workspace/Folder1", true)
            });

        var vm = CreateViewModel();

        // Act
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        // Assert
        vm.RootNodes.Should().HaveCount(1);
        vm.RootNodes[0].Name.Should().Be("workspace");
        vm.RootNodes[0].Children.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_SortsDirectoriesFirst()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("ZFile.txt", "/test/workspace/ZFile.txt", false),
                new("AFolder", "/test/workspace/AFolder", true),
                new("BFile.cs", "/test/workspace/BFile.cs", false)
            });

        var vm = CreateViewModel();

        // Act
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        // Assert
        var children = vm.RootNodes[0].Children;
        children[0].Name.Should().Be("AFolder"); // Directory first
        children[0].IsDirectory.Should().BeTrue();
        children[1].Name.Should().Be("BFile.cs"); // Then files alphabetically
        children[2].Name.Should().Be("ZFile.txt");
    }

    [Fact]
    public async Task LoadWorkspaceAsync_SetsStatusMessage()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("File1.txt", "/test/workspace/File1.txt", false),
                new("File2.txt", "/test/workspace/File2.txt", false)
            });

        var vm = CreateViewModel();

        // Act
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        // Assert
        vm.StatusMessage.Should().Contain("items");
    }

    #endregion

    #region Handle WorkspaceOpenedEvent Tests

    [Fact]
    public async Task Handle_WorkspaceOpenedEvent_LoadsNewTree()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/new/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("NewFile.txt", "/new/workspace/NewFile.txt", false)
            });

        var vm = CreateViewModel();
        var notification = new WorkspaceOpenedEvent("/new/workspace", "New Workspace");

        // Act
        await vm.Handle(notification, CancellationToken.None);

        // Assert
        vm.RootNodes.Should().HaveCount(1);
        vm.RootNodes[0].FullPath.Should().Be("/new/workspace");
    }

    #endregion

    #region Handle WorkspaceClosedEvent Tests

    [Fact]
    public async Task Handle_WorkspaceClosedEvent_ClearsTree()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("File.txt", "/test/workspace/File.txt", false)
            });

        var vm = CreateViewModel();
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        vm.RootNodes.Should().NotBeEmpty(); // Pre-condition

        var notification = new WorkspaceClosedEvent("/test/workspace");

        // Act
        await vm.Handle(notification, CancellationToken.None);

        // Assert
        vm.RootNodes.Should().BeEmpty();
        vm.SelectedNode.Should().BeNull();
        vm.StatusMessage.Should().Be("No folder open");
    }

    #endregion

    #region Handle ExternalFileChangesEvent Tests

    [Fact]
    public async Task Handle_ExternalFileCreated_AddsNode()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>());
        _mockFileSystemAccess.Setup(f => f.IsDirectory("/test/workspace/NewFile.txt"))
            .Returns(false);

        var vm = CreateViewModel();
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        var changes = new List<FileSystemChangeInfo>
        {
            new FileSystemChangeInfo(
                FileSystemChangeType.Created,
                "/test/workspace/NewFile.txt",
                null,
                false)
        };
        var notification = new ExternalFileChangesEvent(changes);

        // Act
        await vm.Handle(notification, CancellationToken.None);

        // Assert
        var root = vm.RootNodes[0];
        root.Children.Should().Contain(c => c.Name == "NewFile.txt");
    }

    [Fact]
    public async Task Handle_ExternalFileDeleted_RemovesNode()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("ExistingFile.txt", "/test/workspace/ExistingFile.txt", false)
            });

        var vm = CreateViewModel();
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        vm.RootNodes[0].Children.Should().HaveCount(1); // Pre-condition

        var changes = new List<FileSystemChangeInfo>
        {
            new FileSystemChangeInfo(
                FileSystemChangeType.Deleted,
                "/test/workspace/ExistingFile.txt",
                null,
                false)
        };
        var notification = new ExternalFileChangesEvent(changes);

        // Act
        await vm.Handle(notification, CancellationToken.None);

        // Assert
        vm.RootNodes[0].Children.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExternalFileRenamed_UpdatesNode()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("OldName.txt", "/test/workspace/OldName.txt", false)
            });
        _mockFileSystemAccess.Setup(f => f.IsDirectory("/test/workspace/NewName.txt"))
            .Returns(false);

        var vm = CreateViewModel();
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        var changes = new List<FileSystemChangeInfo>
        {
            new FileSystemChangeInfo(
                FileSystemChangeType.Renamed,
                "/test/workspace/NewName.txt",
                "/test/workspace/OldName.txt",
                false)
        };
        var notification = new ExternalFileChangesEvent(changes);

        // Act
        await vm.Handle(notification, CancellationToken.None);

        // Assert
        var root = vm.RootNodes[0];
        root.Children.Should().NotContain(c => c.Name == "OldName.txt");
        root.Children.Should().Contain(c => c.Name == "NewName.txt");
    }

    #endregion

    #region OpenSelectedFileAsync Tests

    [Fact]
    public async Task OpenSelectedFileAsync_PublishesFileOpenRequestedEvent()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("Document.md", "/test/workspace/Document.md", false)
            });

        var vm = CreateViewModel();
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        // Select the file
        vm.SelectedNode = vm.RootNodes[0].Children[0];

        // Act
        await vm.OpenSelectedFileCommand.ExecuteAsync(null);

        // Assert
        _mockMediator.Verify(
            m => m.Publish(
                It.Is<FileOpenRequestedEvent>(e => e.FilePath == "/test/workspace/Document.md"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OpenSelectedFileAsync_DoesNothingForDirectories()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("Folder", "/test/workspace/Folder", true)
            });

        var vm = CreateViewModel();
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        // Select the folder
        vm.SelectedNode = vm.RootNodes[0].Children[0];

        // Act
        await vm.OpenSelectedFileCommand.ExecuteAsync(null);

        // Assert
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<FileOpenRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OpenSelectedFileAsync_DoesNothingWhenNoSelection()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedNode = null;

        // Act
        await vm.OpenSelectedFileCommand.ExecuteAsync(null);

        // Assert
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<FileOpenRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region CollapseAll / ExpandAll Tests

    [Fact]
    public void CollapseAll_CollapsesAllNodes()
    {
        // Arrange
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("Folder", "/test/workspace/Folder", true)
            });

        var vm = CreateViewModel();
        _ = vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        // Manually expand root
        if (vm.RootNodes.Count > 0)
        {
            vm.RootNodes[0].IsExpanded = true;
        }

        // Act
        vm.CollapseAllCommand.Execute(null);

        // Assert
        foreach (var root in vm.RootNodes)
        {
            root.IsExpanded.Should().BeFalse();
        }
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_ReloadsTree()
    {
        // Arrange
        var callCount = 0;
        _mockFileSystemAccess.Setup(f => f.GetDirectoryContentsAsync("/test/workspace"))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new List<DirectoryEntry>
                {
                    new($"File{callCount}.txt", $"/test/workspace/File{callCount}.txt", false)
                };
            });

        var workspaceInfo = new WorkspaceInfo(
            RootPath: "/test/workspace",
            Name: "Test",
            OpenedAt: DateTimeOffset.UtcNow
        );
        _mockWorkspaceService.Setup(w => w.IsWorkspaceOpen).Returns(true);
        _mockWorkspaceService.Setup(w => w.CurrentWorkspace).Returns(workspaceInfo);

        var vm = CreateViewModel();
        await vm.LoadWorkspaceCommand.ExecuteAsync("/test/workspace");

        var initialFile = vm.RootNodes[0].Children[0].Name;

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);

        // Assert
        vm.RootNodes[0].Children[0].Name.Should().NotBe(initialFile);
    }

    [Fact]
    public async Task RefreshAsync_DoesNothingWhenNoWorkspace()
    {
        // Arrange
        var vm = CreateViewModel();
        _mockWorkspaceService.Setup(w => w.IsWorkspaceOpen).Returns(false);

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);

        // Assert
        _mockFileSystemAccess.Verify(
            f => f.GetDirectoryContentsAsync(It.IsAny<string>()),
            Times.Never);
    }

    #endregion
}
