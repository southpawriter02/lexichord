using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for FileIndexService.
/// </summary>
public class FileIndexServiceTests : IDisposable
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<FileIndexService>> _mockLogger;
    private readonly FileIndexService _service;
    private readonly string _testWorkspacePath;

    public FileIndexServiceTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<FileIndexService>>();
        
        var settings = new FileIndexSettings
        {
            IgnorePatterns = new List<string> { ".git/**", "node_modules/**" }
        };
        var options = Options.Create(settings);

        _service = new FileIndexService(_mockMediator.Object, _mockLogger.Object, options);

        // Create a temporary test workspace
        _testWorkspacePath = Path.Combine(Path.GetTempPath(), $"FileIndexServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspacePath);
    }

    public void Dispose()
    {
        _service.Dispose();
        
        // Clean up test workspace
        if (Directory.Exists(_testWorkspacePath))
        {
            try
            {
                Directory.Delete(_testWorkspacePath, recursive: true);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }

    #region RebuildIndexAsync Tests

    [Fact]
    public async Task RebuildIndexAsync_EmptyWorkspace_ReturnsZero()
    {
        // Act
        var count = await _service.RebuildIndexAsync(_testWorkspacePath);

        // Assert
        count.Should().Be(0);
        _service.IndexedFileCount.Should().Be(0);
    }

    [Fact]
    public async Task RebuildIndexAsync_WithFiles_IndexesAllFiles()
    {
        // Arrange
        CreateTestFile("file1.cs");
        CreateTestFile("file2.cs");
        CreateTestFile("docs/readme.md");

        // Act
        var count = await _service.RebuildIndexAsync(_testWorkspacePath);

        // Assert
        count.Should().Be(3);
        _service.IndexedFileCount.Should().Be(3);
    }

    [Fact]
    public async Task RebuildIndexAsync_IgnoresGitDirectory()
    {
        // Arrange
        CreateTestFile("src/Program.cs");
        Directory.CreateDirectory(Path.Combine(_testWorkspacePath, ".git"));
        CreateTestFile(".git/config");
        CreateTestFile(".git/HEAD");

        // Act
        var count = await _service.RebuildIndexAsync(_testWorkspacePath);

        // Assert
        count.Should().Be(1); // Only Program.cs
    }

    [Fact]
    public async Task RebuildIndexAsync_SetsWorkspaceRoot()
    {
        // Act
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Assert
        _service.WorkspaceRoot.Should().Be(_testWorkspacePath);
    }

    [Fact]
    public async Task RebuildIndexAsync_PublishesFileIndexRebuiltEvent()
    {
        // Arrange
        CreateTestFile("file.cs");

        // Act
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Assert
        _mockMediator.Verify(m => m.Publish(
            It.Is<FileIndexRebuiltEvent>(e => 
                e.WorkspacePath == _testWorkspacePath && 
                e.FileCount == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RebuildIndexAsync_RaisesIndexChangedEvent()
    {
        // Arrange
        FileIndexChangedEventArgs? eventArgs = null;
        _service.IndexChanged += (_, e) => eventArgs = e;
        CreateTestFile("file.cs");

        // Act
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.ChangeType.Should().Be(FileIndexChangeType.IndexRebuilt);
        eventArgs.TotalFileCount.Should().Be(1);
    }

    [Fact]
    public async Task RebuildIndexAsync_InvalidPath_ThrowsArgumentException()
    {
        // Act & Assert
        await _service.Invoking(s => s.RebuildIndexAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RebuildIndexAsync_NonExistentPath_ThrowsDirectoryNotFoundException()
    {
        // Act & Assert
        await _service.Invoking(s => s.RebuildIndexAsync("/nonexistent/path"))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task Search_EmptyQuery_ReturnsEmpty()
    {
        // Arrange
        CreateTestFile("file.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Act
        var results = _service.Search("");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_MatchingQuery_ReturnsResults()
    {
        // Arrange
        CreateTestFile("MyService.cs");
        CreateTestFile("MyController.cs");
        CreateTestFile("Utilities.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Act
        var results = _service.Search("Service");

        // Assert
        results.Should().Contain(r => r.FileName == "MyService.cs");
    }

    [Fact]
    public async Task Search_RespectsMaxResults()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            CreateTestFile($"File{i:D3}.cs");
        }
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Act
        var results = _service.Search("File", maxResults: 10);

        // Assert
        results.Should().HaveCount(10);
    }

    [Fact]
    public async Task Search_FuzzyMatching_FindsSimilarNames()
    {
        // Arrange
        CreateTestFile("UserController.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Act - searching with partial match
        var results = _service.Search("User");

        // Assert
        results.Should().Contain(r => r.FileName == "UserController.cs");
    }

    #endregion

    #region UpdateFile Tests

    [Fact]
    public async Task UpdateFile_Created_AddsToIndex()
    {
        // Arrange
        await _service.RebuildIndexAsync(_testWorkspacePath);
        var newFilePath = CreateTestFile("newfile.cs");

        // Act
        _service.UpdateFile(newFilePath, FileIndexAction.Created);

        // Assert
        _service.ContainsFile(newFilePath).Should().BeTrue();
        _service.IndexedFileCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateFile_Deleted_RemovesFromIndex()
    {
        // Arrange
        var filePath = CreateTestFile("toDelete.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);
        _service.ContainsFile(filePath).Should().BeTrue();

        // Act
        File.Delete(filePath);
        _service.UpdateFile(filePath, FileIndexAction.Deleted);

        // Assert
        _service.ContainsFile(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateFile_Modified_UpdatesEntry()
    {
        // Arrange
        var filePath = CreateTestFile("modify.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);
        var originalEntry = _service.GetFile(filePath);

        // Modify the file
        await Task.Delay(100); // Ensure timestamp changes
        File.AppendAllText(filePath, "// Modified");

        // Act
        _service.UpdateFile(filePath, FileIndexAction.Modified);

        // Assert
        var updatedEntry = _service.GetFile(filePath);
        updatedEntry.Should().NotBeNull();
        updatedEntry!.FileSize.Should().BeGreaterThan(originalEntry!.FileSize);
    }

    #endregion

    #region UpdateFileRenamed Tests

    [Fact]
    public async Task UpdateFileRenamed_UpdatesIndexCorrectly()
    {
        // Arrange
        var oldPath = CreateTestFile("oldname.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);
        
        var newPath = Path.Combine(_testWorkspacePath, "newname.cs");
        File.Move(oldPath, newPath);

        // Act
        _service.UpdateFileRenamed(oldPath, newPath);

        // Assert
        _service.ContainsFile(oldPath).Should().BeFalse();
        _service.ContainsFile(newPath).Should().BeTrue();
    }

    #endregion

    #region Recent Files Tests

    [Fact]
    public async Task RecordFileAccess_AddsToRecentFiles()
    {
        // Arrange
        var filePath = CreateTestFile("recent.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Act
        _service.RecordFileAccess(filePath);
        var recentFiles = _service.GetRecentFiles();

        // Assert
        recentFiles.Should().ContainSingle();
        recentFiles[0].FullPath.Should().Be(filePath);
    }

    [Fact]
    public async Task RecordFileAccess_MovesToFront()
    {
        // Arrange
        var file1 = CreateTestFile("file1.cs");
        var file2 = CreateTestFile("file2.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);

        _service.RecordFileAccess(file1);
        _service.RecordFileAccess(file2);

        // Act - access file1 again
        _service.RecordFileAccess(file1);

        // Assert
        var recentFiles = _service.GetRecentFiles();
        recentFiles[0].FullPath.Should().Be(file1);
        recentFiles[1].FullPath.Should().Be(file2);
    }

    [Fact]
    public async Task GetRecentFiles_RespectsMaxResults()
    {
        // Arrange
        await _service.RebuildIndexAsync(_testWorkspacePath);
        for (int i = 0; i < 10; i++)
        {
            var file = CreateTestFile($"file{i}.cs");
            _service.UpdateFile(file, FileIndexAction.Created);
            _service.RecordFileAccess(file);
        }

        // Act
        var recentFiles = _service.GetRecentFiles(maxResults: 5);

        // Assert
        recentFiles.Should().HaveCount(5);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_RemovesAllEntriesAndRecentFiles()
    {
        // Arrange
        var file = CreateTestFile("file.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);
        _service.RecordFileAccess(file);

        // Act
        _service.Clear();

        // Assert
        _service.IndexedFileCount.Should().Be(0);
        _service.GetRecentFiles().Should().BeEmpty();
        _service.WorkspaceRoot.Should().BeNull();
    }

    [Fact]
    public async Task Clear_RaisesIndexChangedEvent()
    {
        // Arrange
        CreateTestFile("file.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);

        FileIndexChangedEventArgs? eventArgs = null;
        _service.IndexChanged += (_, e) => eventArgs = e;

        // Act
        _service.Clear();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.ChangeType.Should().Be(FileIndexChangeType.IndexCleared);
    }

    #endregion

    #region GetFile and ContainsFile Tests

    [Fact]
    public async Task GetFile_ExistingFile_ReturnsEntry()
    {
        // Arrange
        var filePath = CreateTestFile("test.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Act
        var entry = _service.GetFile(filePath);

        // Assert
        entry.Should().NotBeNull();
        entry!.FileName.Should().Be("test.cs");
    }

    [Fact]
    public async Task GetFile_NonExistingFile_ReturnsNull()
    {
        // Arrange
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Act
        var entry = _service.GetFile("/some/nonexistent/file.cs");

        // Assert
        entry.Should().BeNull();
    }

    [Fact]
    public async Task GetAllFiles_ReturnsAllIndexedEntries()
    {
        // Arrange
        CreateTestFile("file1.cs");
        CreateTestFile("file2.cs");
        CreateTestFile("file3.cs");
        await _service.RebuildIndexAsync(_testWorkspacePath);

        // Act
        var allFiles = _service.GetAllFiles();

        // Assert
        allFiles.Should().HaveCount(3);
    }

    #endregion

    #region Helper Methods

    private string CreateTestFile(string relativePath, string content = "// Test content")
    {
        var fullPath = Path.Combine(_testWorkspacePath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    #endregion
}
