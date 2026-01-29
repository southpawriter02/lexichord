namespace Lexichord.Tests.Unit.Modules.Workspace;

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Workspace.Models;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FileTreeNode"/>.
/// </summary>
public class FileTreeNodeTests
{
    #region IconKind Tests

    [Fact]
    public void IconKind_ForDirectory_WhenExpanded_ReturnsFolderOpen()
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = "TestFolder",
            FullPath = "/test/TestFolder",
            IsDirectory = true,
            Parent = null
        };
        node.IsExpanded = true;

        // Act
        var iconKind = node.IconKind;

        // Assert
        iconKind.Should().Be("FolderOpen");
    }

    [Fact]
    public void IconKind_ForDirectory_WhenCollapsed_ReturnsFolder()
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = "TestFolder",
            FullPath = "/test/TestFolder",
            IsDirectory = true,
            Parent = null
        };
        node.IsExpanded = false;

        // Act
        var iconKind = node.IconKind;

        // Assert
        iconKind.Should().Be("Folder");
    }

    [Theory]
    [InlineData("Program.cs", "LanguageCsharp")]
    [InlineData("script.py", "LanguagePython")]
    [InlineData("app.ts", "LanguageTypescript")]
    [InlineData("index.js", "LanguageJavascript")]
    [InlineData("Main.java", "LanguageJava")]
    [InlineData("main.go", "LanguageGo")]
    [InlineData("lib.rs", "LanguageRust")]
    public void IconKind_ForProgrammingFiles_ReturnsCorrectIcon(string fileName, string expectedIcon)
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = fileName,
            FullPath = $"/test/{fileName}",
            IsDirectory = false,
            Parent = null
        };

        // Act
        var iconKind = node.IconKind;

        // Assert
        iconKind.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData("README.md", "LanguageMarkdown")]
    [InlineData("notes.txt", "FileDocumentOutline")]
    [InlineData("config.json", "CodeJson")]
    [InlineData("data.xml", "Xml")]
    public void IconKind_ForDocumentFiles_ReturnsCorrectIcon(string fileName, string expectedIcon)
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = fileName,
            FullPath = $"/test/{fileName}",
            IsDirectory = false,
            Parent = null
        };

        // Act
        var iconKind = node.IconKind;

        // Assert
        iconKind.Should().Be(expectedIcon);
    }

    [Fact]
    public void IconKind_ForUnknownExtension_ReturnsFileOutline()
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = "unknown.xyz",
            FullPath = "/test/unknown.xyz",
            IsDirectory = false,
            Parent = null
        };

        // Act
        var iconKind = node.IconKind;

        // Assert
        iconKind.Should().Be("FileOutline");
    }

    [Fact]
    public void IconKind_ForPlaceholder_ReturnsLoading()
    {
        // Arrange
        var node = FileTreeNode.CreateLoadingPlaceholder();

        // Act
        var iconKind = node.IconKind;

        // Assert
        iconKind.Should().Be("Loading");
    }

    #endregion

    #region SortKey Tests

    [Fact]
    public void SortKey_DirectoriesHavePrefix0()
    {
        // Arrange
        var directory = new FileTreeNode
        {
            Name = "Folder",
            FullPath = "/test/Folder",
            IsDirectory = true,
            Parent = null
        };

        // Act
        var sortKey = directory.SortKey;

        // Assert
        sortKey.Should().StartWith("0_");
    }

    [Fact]
    public void SortKey_FilesHavePrefix1()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "file.txt",
            FullPath = "/test/file.txt",
            IsDirectory = false,
            Parent = null
        };

        // Act
        var sortKey = file.SortKey;

        // Assert
        sortKey.Should().StartWith("1_");
    }

    [Fact]
    public void SortKey_DirectoriesOrderBeforeFiles()
    {
        // Arrange
        var directory = new FileTreeNode
        {
            Name = "Zebra",
            FullPath = "/test/Zebra",
            IsDirectory = true,
            Parent = null
        };

        var file = new FileTreeNode
        {
            Name = "Alpha.txt",
            FullPath = "/test/Alpha.txt",
            IsDirectory = false,
            Parent = null
        };

        // Act & Assert
        directory.SortKey.CompareTo(file.SortKey).Should().BeLessThan(0);
    }

    [Fact]
    public void SortKey_AlphabeticalWithinCategory()
    {
        // Arrange
        var fileA = new FileTreeNode
        {
            Name = "Alpha.txt",
            FullPath = "/test/Alpha.txt",
            IsDirectory = false,
            Parent = null
        };

        var fileZ = new FileTreeNode
        {
            Name = "Zebra.txt",
            FullPath = "/test/Zebra.txt",
            IsDirectory = false,
            Parent = null
        };

        // Act & Assert
        fileA.SortKey.CompareTo(fileZ.SortKey).Should().BeLessThan(0);
    }

    [Fact]
    public void SortKey_IsCaseInsensitive()
    {
        // Arrange
        var lower = new FileTreeNode
        {
            Name = "alpha.txt",
            FullPath = "/test/alpha.txt",
            IsDirectory = false,
            Parent = null
        };

        var upper = new FileTreeNode
        {
            Name = "ALPHA.txt",
            FullPath = "/test/ALPHA.txt",
            IsDirectory = false,
            Parent = null
        };

        // Act & Assert
        lower.SortKey.Should().Be(upper.SortKey);
    }

    #endregion

    #region FindByPath Tests

    [Fact]
    public void FindByPath_SelfMatch_ReturnsSelf()
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = "TestFolder",
            FullPath = "/test/TestFolder",
            IsDirectory = true,
            Parent = null
        };

        // Act
        var found = node.FindByPath("/test/TestFolder");

        // Assert
        found.Should().BeSameAs(node);
    }

    [Fact]
    public void FindByPath_ChildMatch_ReturnsChild()
    {
        // Arrange
        var parent = new FileTreeNode
        {
            Name = "Parent",
            FullPath = "/test/Parent",
            IsDirectory = true,
            Parent = null
        };

        var child = new FileTreeNode
        {
            Name = "Child",
            FullPath = "/test/Parent/Child",
            IsDirectory = true,
            Parent = parent
        };

        parent.Children.Add(child);

        // Act
        var found = parent.FindByPath("/test/Parent/Child");

        // Assert
        found.Should().BeSameAs(child);
    }

    [Fact]
    public void FindByPath_GrandchildMatch_ReturnsGrandchild()
    {
        // Arrange
        var grandparent = new FileTreeNode
        {
            Name = "Grandparent",
            FullPath = "/test/Grandparent",
            IsDirectory = true,
            Parent = null
        };

        var parent = new FileTreeNode
        {
            Name = "Parent",
            FullPath = "/test/Grandparent/Parent",
            IsDirectory = true,
            Parent = grandparent
        };

        var child = new FileTreeNode
        {
            Name = "Child",
            FullPath = "/test/Grandparent/Parent/Child",
            IsDirectory = false,
            Parent = parent
        };

        grandparent.Children.Add(parent);
        parent.Children.Add(child);

        // Act
        var found = grandparent.FindByPath("/test/Grandparent/Parent/Child");

        // Assert
        found.Should().BeSameAs(child);
    }

    [Fact]
    public void FindByPath_NonExistent_ReturnsNull()
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = "TestFolder",
            FullPath = "/test/TestFolder",
            IsDirectory = true,
            Parent = null
        };

        // Act
        var found = node.FindByPath("/test/NonExistent");

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public void FindByPath_SkipsPlaceholders()
    {
        // Arrange
        var parent = new FileTreeNode
        {
            Name = "Parent",
            FullPath = "/test/Parent",
            IsDirectory = true,
            Parent = null
        };

        parent.Children.Add(FileTreeNode.CreateLoadingPlaceholder());

        // Act
        var found = parent.FindByPath("/test/Parent/SomeChild");

        // Assert
        found.Should().BeNull();
    }

    #endregion

    #region LoadChildrenAsync Tests

    [Fact]
    public async Task LoadChildrenAsync_PopulatesFromFileSystem()
    {
        // Arrange
        var mockFs = new Mock<IFileSystemAccess>();
        mockFs.Setup(f => f.GetDirectoryContentsAsync("/test/Parent"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("File1.txt", "/test/Parent/File1.txt", false),
                new("Folder1", "/test/Parent/Folder1", true),
                new("File2.cs", "/test/Parent/File2.cs", false)
            });

        var parent = new FileTreeNode
        {
            Name = "Parent",
            FullPath = "/test/Parent",
            IsDirectory = true,
            Parent = null
        };

        // Simulate placeholder
        parent.Children.Add(FileTreeNode.CreateLoadingPlaceholder());

        // Act
        await parent.LoadChildrenAsync(mockFs.Object);

        // Assert
        parent.Children.Should().HaveCount(3);
        parent.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task LoadChildrenAsync_SortsDirectoriesFirst()
    {
        // Arrange
        var mockFs = new Mock<IFileSystemAccess>();
        mockFs.Setup(f => f.GetDirectoryContentsAsync("/test/Parent"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("Zebra.txt", "/test/Parent/Zebra.txt", false),
                new("Alpha", "/test/Parent/Alpha", true),
                new("Beta.cs", "/test/Parent/Beta.cs", false)
            });

        var parent = new FileTreeNode
        {
            Name = "Parent",
            FullPath = "/test/Parent",
            IsDirectory = true,
            Parent = null
        };

        // Act
        await parent.LoadChildrenAsync(mockFs.Object);

        // Assert
        parent.Children[0].Name.Should().Be("Alpha"); // Directory first
        parent.Children[0].IsDirectory.Should().BeTrue();
    }

    [Fact]
    public async Task LoadChildrenAsync_DirectoriesGetPlaceholder()
    {
        // Arrange
        var mockFs = new Mock<IFileSystemAccess>();
        mockFs.Setup(f => f.GetDirectoryContentsAsync("/test/Parent"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("SubFolder", "/test/Parent/SubFolder", true)
            });

        var parent = new FileTreeNode
        {
            Name = "Parent",
            FullPath = "/test/Parent",
            IsDirectory = true,
            Parent = null
        };

        // Act
        await parent.LoadChildrenAsync(mockFs.Object);

        // Assert
        var subFolder = parent.Children[0];
        subFolder.IsDirectory.Should().BeTrue();
        subFolder.Children.Should().HaveCount(1);
        subFolder.Children[0].IsPlaceholder.Should().BeTrue();
    }

    [Fact]
    public async Task LoadChildrenAsync_DoesNotReloadIfAlreadyLoaded()
    {
        // Arrange
        var mockFs = new Mock<IFileSystemAccess>();
        mockFs.Setup(f => f.GetDirectoryContentsAsync("/test/Parent"))
            .ReturnsAsync(new List<DirectoryEntry>
            {
                new("File.txt", "/test/Parent/File.txt", false)
            });

        var parent = new FileTreeNode
        {
            Name = "Parent",
            FullPath = "/test/Parent",
            IsDirectory = true,
            Parent = null
        };

        // First load
        await parent.LoadChildrenAsync(mockFs.Object);

        // Reset mock to track calls
        mockFs.Invocations.Clear();

        // Act - Second load
        await parent.LoadChildrenAsync(mockFs.Object);

        // Assert
        mockFs.Verify(f => f.GetDirectoryContentsAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task LoadChildrenAsync_FileNodesIgnored()
    {
        // Arrange
        var mockFs = new Mock<IFileSystemAccess>();

        var file = new FileTreeNode
        {
            Name = "File.txt",
            FullPath = "/test/File.txt",
            IsDirectory = false,
            Parent = null
        };

        // Act
        await file.LoadChildrenAsync(mockFs.Object);

        // Assert
        file.Children.Should().BeEmpty();
        mockFs.Verify(f => f.GetDirectoryContentsAsync(It.IsAny<string>()), Times.Never());
    }

    #endregion

    #region Edit Mode Tests

    [Fact]
    public void BeginEdit_SetsEditingMode()
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = "Test.txt",
            FullPath = "/test/Test.txt",
            IsDirectory = false,
            Parent = null
        };

        // Act
        node.BeginEdit();

        // Assert
        node.IsEditing.Should().BeTrue();
        node.EditName.Should().Be("Test.txt");
    }

    [Fact]
    public void CancelEdit_ClearsEditingMode()
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = "Test.txt",
            FullPath = "/test/Test.txt",
            IsDirectory = false,
            Parent = null
        };
        node.BeginEdit();

        // Act
        node.CancelEdit();

        // Assert
        node.IsEditing.Should().BeFalse();
        node.EditName.Should().BeEmpty();
    }

    #endregion

    #region Depth Tests

    [Fact]
    public void Depth_RootNode_ReturnsZero()
    {
        // Arrange
        var root = new FileTreeNode
        {
            Name = "Root",
            FullPath = "/Root",
            IsDirectory = true,
            Parent = null
        };

        // Act & Assert
        root.Depth.Should().Be(0);
    }

    [Fact]
    public void Depth_ChildNode_ReturnsOne()
    {
        // Arrange
        var root = new FileTreeNode
        {
            Name = "Root",
            FullPath = "/Root",
            IsDirectory = true,
            Parent = null
        };

        var child = new FileTreeNode
        {
            Name = "Child",
            FullPath = "/Root/Child",
            IsDirectory = false,
            Parent = root
        };

        // Act & Assert
        child.Depth.Should().Be(1);
    }

    [Fact]
    public void Depth_GrandchildNode_ReturnsTwo()
    {
        // Arrange
        var root = new FileTreeNode
        {
            Name = "Root",
            FullPath = "/Root",
            IsDirectory = true,
            Parent = null
        };

        var parent = new FileTreeNode
        {
            Name = "Parent",
            FullPath = "/Root/Parent",
            IsDirectory = true,
            Parent = root
        };

        var grandchild = new FileTreeNode
        {
            Name = "Grandchild",
            FullPath = "/Root/Parent/Grandchild",
            IsDirectory = false,
            Parent = parent
        };

        // Act & Assert
        grandchild.Depth.Should().Be(2);
    }

    #endregion

    #region Placeholder Tests

    [Fact]
    public void CreateLoadingPlaceholder_HasCorrectProperties()
    {
        // Act
        var placeholder = FileTreeNode.CreateLoadingPlaceholder();

        // Assert
        placeholder.Name.Should().Be(FileTreeNode.LoadingPlaceholderName);
        placeholder.FullPath.Should().BeEmpty();
        placeholder.IsDirectory.Should().BeFalse();
        placeholder.Parent.Should().BeNull();
        placeholder.IsPlaceholder.Should().BeTrue();
    }

    [Fact]
    public void IsPlaceholder_ForNormalNode_ReturnsFalse()
    {
        // Arrange
        var node = new FileTreeNode
        {
            Name = "Normal",
            FullPath = "/test/Normal",
            IsDirectory = false,
            Parent = null
        };

        // Act & Assert
        node.IsPlaceholder.Should().BeFalse();
    }

    #endregion
}
