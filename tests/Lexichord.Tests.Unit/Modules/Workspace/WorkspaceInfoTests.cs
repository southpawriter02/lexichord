using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Workspace;

/// <summary>
/// Unit tests for WorkspaceInfo record.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the WorkspaceInfo record correctly:
/// - Contains valid paths within the workspace
/// - Rejects paths outside the workspace
/// - Prevents path traversal attacks
/// - Handles edge cases (root paths, null inputs)
/// </remarks>
public class WorkspaceInfoTests : IDisposable
{
    private readonly string _tempDir;
    private readonly WorkspaceInfo _sut;

    public WorkspaceInfoTests()
    {
        // Create a real temp directory for testing
        _tempDir = Path.Combine(Path.GetTempPath(), $"lexichord-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _sut = new WorkspaceInfo(
            RootPath: _tempDir,
            Name: "test-workspace",
            OpenedAt: DateTimeOffset.UtcNow
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region ContainsPath Tests

    [Fact]
    public void ContainsPath_ReturnsTrue_ForPathWithinWorkspace()
    {
        // Arrange
        var childPath = Path.Combine(_tempDir, "subfolder", "file.txt");

        // Act
        var result = _sut.ContainsPath(childPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsPath_ReturnsTrue_ForRootPath()
    {
        // Act
        var result = _sut.ContainsPath(_tempDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsPath_ReturnsFalse_ForPathOutsideWorkspace()
    {
        // Arrange
        var outsidePath = Path.Combine(Path.GetTempPath(), "other-folder", "file.txt");

        // Act
        var result = _sut.ContainsPath(outsidePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsPath_ReturnsFalse_ForPathTraversalAttempt()
    {
        // Arrange - try to escape workspace with ..
        var traversalPath = Path.Combine(_tempDir, "..", "other-folder", "file.txt");

        // Act
        var result = _sut.ContainsPath(traversalPath);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsPath_ReturnsFalse_ForNullOrWhitespacePath(string? invalidPath)
    {
        // Act
        var result = _sut.ContainsPath(invalidPath!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsPath_ReturnsFalse_ForSimilarButDifferentPath()
    {
        // Arrange - path that starts like workspace but is different directory
        // e.g., workspace is "/tmp/test" and path is "/tmp/test-other"
        var similarPath = _tempDir + "-other";

        // Act
        var result = _sut.ContainsPath(similarPath);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Directory Property Tests

    [Fact]
    public void Directory_ReturnsDirectoryInfo_ForRootPath()
    {
        // Act
        var directory = _sut.Directory;

        // Assert
        directory.Should().NotBeNull();
        directory.FullName.Should().Be(_tempDir);
        directory.Exists.Should().BeTrue();
    }

    #endregion

    #region Name Extraction Tests

    [Fact]
    public void Name_IsSetCorrectly()
    {
        // Assert
        _sut.Name.Should().Be("test-workspace");
    }

    [Fact]
    public void WorkspaceInfo_ExtractsName_FromPath()
    {
        // Arrange
        var folderName = Path.GetFileName(_tempDir);
        var workspace = new WorkspaceInfo(
            RootPath: _tempDir,
            Name: folderName!,
            OpenedAt: DateTimeOffset.UtcNow
        );

        // Assert
        workspace.Name.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region OpenedAt Tests

    [Fact]
    public void OpenedAt_IsPreserved()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 1, 28, 12, 0, 0, TimeSpan.Zero);
        var workspace = new WorkspaceInfo(
            RootPath: _tempDir,
            Name: "test",
            OpenedAt: timestamp
        );

        // Assert
        workspace.OpenedAt.Should().Be(timestamp);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void WorkspaceInfo_IsImmutable_WithExpression()
    {
        // Arrange
        var original = new WorkspaceInfo(
            RootPath: _tempDir,
            Name: "original",
            OpenedAt: DateTimeOffset.UtcNow
        );

        // Act - create new instance with modified name using with expression
        var modified = original with { Name = "modified" };

        // Assert
        original.Name.Should().Be("original");
        modified.Name.Should().Be("modified");
        modified.RootPath.Should().Be(original.RootPath);
    }

    #endregion
}
