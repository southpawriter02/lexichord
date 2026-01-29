namespace Lexichord.Tests.Unit.Modules.Workspace;

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Workspace.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FileOperationService"/>.
/// </summary>
public class FileOperationServiceTests : IDisposable
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<FileOperationService>> _mockLogger;
    private readonly FileOperationService _service;
    private readonly string _testDirectory;

    public FileOperationServiceTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<FileOperationService>>();
        _service = new FileOperationService(_mockMediator.Object, _mockLogger.Object);

        // Create a unique test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"lexichord_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region ValidateName Tests

    [Fact]
    public void ValidateName_ValidName_ReturnsValid()
    {
        // Act
        var result = _service.ValidateName("valid-file.txt");

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateName_EmptyOrNull_ReturnsInvalid(string? name)
    {
        // Act
        var result = _service.ValidateName(name!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot be empty");
    }

    [Fact]
    public void ValidateName_WhitespaceOnly_ReturnsInvalid()
    {
        // Act
        var result = _service.ValidateName("   ");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("whitespace");
    }

    [Theory]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData(" both ")]
    public void ValidateName_LeadingTrailingWhitespace_ReturnsInvalid(string name)
    {
        // Act
        var result = _service.ValidateName(name);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("leading or trailing whitespace");
    }

    [Theory]
    [InlineData("file/name")]
    [InlineData("file\\name")]
    public void ValidateName_ContainsPathSeparators_ReturnsInvalid(string name)
    {
        // Act
        var result = _service.ValidateName(name);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("path separator");
    }

    [Theory]
    [InlineData("..")]
    [InlineData("folder..name")]
    public void ValidateName_ContainsDirectoryTraversal_ReturnsInvalid(string name)
    {
        // Act
        var result = _service.ValidateName(name);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("directory traversal");
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    [InlineData("con.txt")]
    [InlineData("nul.md")]
    public void ValidateName_ReservedName_ReturnsInvalid(string name)
    {
        // Act
        var result = _service.ValidateName(name);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("reserved name");
    }

    [Fact]
    public void ValidateName_TooLong_ReturnsInvalid()
    {
        // Arrange
        var longName = new string('a', 256);

        // Act
        var result = _service.ValidateName(longName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("255 characters");
    }

    #endregion

    #region GenerateUniqueName Tests

    [Fact]
    public void GenerateUniqueName_NoConflict_ReturnsSameName()
    {
        // Act
        var result = _service.GenerateUniqueName(_testDirectory, "new-file.txt");

        // Assert
        result.Should().Be("new-file.txt");
    }

    [Fact]
    public void GenerateUniqueName_FileConflict_ReturnsSuffixedName()
    {
        // Arrange
        var existingFile = Path.Combine(_testDirectory, "existing.txt");
        File.WriteAllText(existingFile, "content");

        // Act
        var result = _service.GenerateUniqueName(_testDirectory, "existing.txt");

        // Assert
        result.Should().Be("existing (1).txt");
    }

    [Fact]
    public void GenerateUniqueName_MultipleConflicts_IncrementsCounter()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "file.txt"), "");
        File.WriteAllText(Path.Combine(_testDirectory, "file (1).txt"), "");
        File.WriteAllText(Path.Combine(_testDirectory, "file (2).txt"), "");

        // Act
        var result = _service.GenerateUniqueName(_testDirectory, "file.txt");

        // Assert
        result.Should().Be("file (3).txt");
    }

    [Fact]
    public void GenerateUniqueName_FolderConflict_ReturnsSuffixedName()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testDirectory, "New Folder"));

        // Act
        var result = _service.GenerateUniqueName(_testDirectory, "New Folder");

        // Assert
        result.Should().Be("New Folder (1)");
    }

    [Fact]
    public void GenerateUniqueName_EmptyBaseName_UsesUntitled()
    {
        // Act
        var result = _service.GenerateUniqueName(_testDirectory, "");

        // Assert
        result.Should().Be("untitled");
    }

    #endregion

    #region CreateFileAsync Tests

    [Fact]
    public async Task CreateFileAsync_ValidInput_CreatesFile()
    {
        // Act
        var result = await _service.CreateFileAsync(_testDirectory, "new-file.txt", "content");

        // Assert
        result.Success.Should().BeTrue();
        result.ResultPath.Should().NotBeNull();
        File.Exists(result.ResultPath).Should().BeTrue();
        File.ReadAllText(result.ResultPath!).Should().Be("content");
    }

    [Fact]
    public async Task CreateFileAsync_ValidInput_PublishesEvent()
    {
        // Act
        await _service.CreateFileAsync(_testDirectory, "new-file.txt");

        // Assert
        _mockMediator.Verify(
            m => m.Publish(
                It.Is<FileCreatedEvent>(e =>
                    e.FileName == "new-file.txt" &&
                    e.IsDirectory == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateFileAsync_InvalidName_ReturnsError()
    {
        // Act
        var result = await _service.CreateFileAsync(_testDirectory, "invalid/name.txt");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.InvalidName);
    }

    [Fact]
    public async Task CreateFileAsync_ParentNotFound_ReturnsError()
    {
        // Act
        var result = await _service.CreateFileAsync("/nonexistent/path", "file.txt");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.PathNotFound);
    }

    [Fact]
    public async Task CreateFileAsync_AlreadyExists_ReturnsError()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "existing.txt"), "");

        // Act
        var result = await _service.CreateFileAsync(_testDirectory, "existing.txt");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.AlreadyExists);
    }

    #endregion

    #region CreateFolderAsync Tests

    [Fact]
    public async Task CreateFolderAsync_ValidInput_CreatesFolder()
    {
        // Act
        var result = await _service.CreateFolderAsync(_testDirectory, "new-folder");

        // Assert
        result.Success.Should().BeTrue();
        result.ResultPath.Should().NotBeNull();
        Directory.Exists(result.ResultPath).Should().BeTrue();
    }

    [Fact]
    public async Task CreateFolderAsync_ValidInput_PublishesEvent()
    {
        // Act
        await _service.CreateFolderAsync(_testDirectory, "new-folder");

        // Assert
        _mockMediator.Verify(
            m => m.Publish(
                It.Is<FileCreatedEvent>(e =>
                    e.FileName == "new-folder" &&
                    e.IsDirectory == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateFolderAsync_InvalidName_ReturnsError()
    {
        // Act
        var result = await _service.CreateFolderAsync(_testDirectory, "CON");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.InvalidName);
    }

    #endregion

    #region RenameAsync Tests

    [Fact]
    public async Task RenameAsync_ValidFile_RenamesFile()
    {
        // Arrange
        var oldPath = Path.Combine(_testDirectory, "old-name.txt");
        File.WriteAllText(oldPath, "content");

        // Act
        var result = await _service.RenameAsync(oldPath, "new-name.txt");

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(oldPath).Should().BeFalse();
        File.Exists(Path.Combine(_testDirectory, "new-name.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task RenameAsync_ValidFolder_RenamesFolder()
    {
        // Arrange
        var oldPath = Path.Combine(_testDirectory, "old-folder");
        Directory.CreateDirectory(oldPath);

        // Act
        var result = await _service.RenameAsync(oldPath, "new-folder");

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(oldPath).Should().BeFalse();
        Directory.Exists(Path.Combine(_testDirectory, "new-folder")).Should().BeTrue();
    }

    [Fact]
    public async Task RenameAsync_PublishesEvent()
    {
        // Arrange
        var oldPath = Path.Combine(_testDirectory, "old.txt");
        File.WriteAllText(oldPath, "");

        // Act
        await _service.RenameAsync(oldPath, "new.txt");

        // Assert
        _mockMediator.Verify(
            m => m.Publish(
                It.Is<FileRenamedEvent>(e =>
                    e.OldName == "old.txt" &&
                    e.NewName == "new.txt"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RenameAsync_PathNotFound_ReturnsError()
    {
        // Act
        var result = await _service.RenameAsync(Path.Combine(_testDirectory, "nonexistent.txt"), "new.txt");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.PathNotFound);
    }

    [Fact]
    public async Task RenameAsync_TargetExists_ReturnsError()
    {
        // Arrange
        var oldPath = Path.Combine(_testDirectory, "old.txt");
        var newPath = Path.Combine(_testDirectory, "new.txt");
        File.WriteAllText(oldPath, "");
        File.WriteAllText(newPath, "");

        // Act
        var result = await _service.RenameAsync(oldPath, "new.txt");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.AlreadyExists);
    }

    [Fact]
    public async Task RenameAsync_GitFolder_ReturnsProtectedError()
    {
        // Arrange
        var gitPath = Path.Combine(_testDirectory, ".git");
        Directory.CreateDirectory(gitPath);

        // Act
        var result = await _service.RenameAsync(gitPath, "not-git");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.ProtectedPath);
    }

    [Fact]
    public async Task RenameAsync_SameName_ReturnsSuccess()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "same.txt");
        File.WriteAllText(path, "");

        // Act
        var result = await _service.RenameAsync(path, "same.txt");

        // Assert
        result.Success.Should().BeTrue();
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<FileRenamedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_File_DeletesFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "to-delete.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = await _service.DeleteAsync(filePath);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_EmptyFolder_DeletesFolder()
    {
        // Arrange
        var folderPath = Path.Combine(_testDirectory, "empty-folder");
        Directory.CreateDirectory(folderPath);

        // Act
        var result = await _service.DeleteAsync(folderPath);

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(folderPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonEmptyFolderWithoutRecursive_ReturnsError()
    {
        // Arrange
        var folderPath = Path.Combine(_testDirectory, "non-empty");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "child.txt"), "");

        // Act
        var result = await _service.DeleteAsync(folderPath, recursive: false);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.DirectoryNotEmpty);
    }

    [Fact]
    public async Task DeleteAsync_NonEmptyFolderWithRecursive_DeletesFolder()
    {
        // Arrange
        var folderPath = Path.Combine(_testDirectory, "non-empty");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "child.txt"), "");

        // Act
        var result = await _service.DeleteAsync(folderPath, recursive: true);

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(folderPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_PublishesEvent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "delete-me.txt");
        File.WriteAllText(filePath, "");

        // Act
        await _service.DeleteAsync(filePath);

        // Assert
        _mockMediator.Verify(
            m => m.Publish(
                It.Is<FileDeletedEvent>(e =>
                    e.FileName == "delete-me.txt" &&
                    e.IsDirectory == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PathNotFound_ReturnsError()
    {
        // Act
        var result = await _service.DeleteAsync(Path.Combine(_testDirectory, "nonexistent.txt"));

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.PathNotFound);
    }

    [Fact]
    public async Task DeleteAsync_GitFolder_ReturnsProtectedError()
    {
        // Arrange
        var gitPath = Path.Combine(_testDirectory, ".git");
        Directory.CreateDirectory(gitPath);

        // Act
        var result = await _service.DeleteAsync(gitPath);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(FileOperationError.ProtectedPath);
    }

    #endregion
}
