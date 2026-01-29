using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for FileIndexEntry record.
/// </summary>
public class FileIndexEntryTests
{
    #region Extension Property Tests

    [Theory]
    [InlineData("test.cs", ".cs")]
    [InlineData("file.TXT", ".txt")]
    [InlineData("document.PDF", ".pdf")]
    [InlineData("noextension", "")]
    [InlineData("file.tar.gz", ".gz")]
    public void Extension_ReturnsLowercaseExtension(string fileName, string expectedExtension)
    {
        // Arrange
        var entry = CreateEntry(fileName);

        // Act
        var extension = entry.Extension;

        // Assert
        extension.Should().Be(expectedExtension);
    }

    #endregion

    #region IconKind Property Tests

    [Theory]
    [InlineData(".cs", "LanguageCsharp")]
    [InlineData(".js", "LanguageJavascript")]
    [InlineData(".ts", "LanguageTypescript")]
    [InlineData(".py", "LanguagePython")]
    [InlineData(".java", "LanguageJava")]
    [InlineData(".go", "LanguageGo")]
    [InlineData(".rs", "LanguageRust")]
    [InlineData(".md", "LanguageMarkdown")]
    [InlineData(".json", "CodeJson")]
    [InlineData(".xml", "FileXml")]
    [InlineData(".html", "LanguageHtml5")]
    [InlineData(".css", "LanguageCss3")]
    [InlineData(".sql", "Database")]
    [InlineData(".unknown", "FileDocument")]
    public void IconKind_ReturnsCorrectIconForExtension(string extension, string expectedIcon)
    {
        // Arrange
        var entry = CreateEntry($"file{extension}");

        // Act
        var iconKind = entry.IconKind;

        // Assert
        iconKind.Should().Be(expectedIcon);
    }

    [Fact]
    public void IconKind_DefaultsToFileDocumentForUnknownExtension()
    {
        // Arrange
        var entry = CreateEntry("file.xyz");

        // Act
        var iconKind = entry.IconKind;

        // Assert
        iconKind.Should().Be("FileDocument");
    }

    #endregion

    #region DirectoryName Property Tests

    [Theory]
    [InlineData("src/Services/MyService.cs", "src/Services")]
    [InlineData("Models/User.cs", "Models")]
    [InlineData("Program.cs", "")]
    public void DirectoryName_ReturnsParentDirectory(string relativePath, string expectedDir)
    {
        // Arrange
        var entry = new FileIndexEntry(
            Path.GetFileName(relativePath),
            $"/workspace/{relativePath}",
            relativePath,
            DateTime.UtcNow,
            1024
        );

        // Act
        var directoryName = entry.DirectoryName;

        // Assert
        directoryName.Should().Be(expectedDir);
    }

    #endregion

    #region FileSizeDisplay Property Tests

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1024 * 1024, "1.0 MB")]
    [InlineData(1024 * 1024 * 1.5, "1.5 MB")]
    [InlineData(1024L * 1024 * 1024, "1.0 GB")]
    [InlineData(1024L * 1024 * 1024 * 2.5, "2.5 GB")]
    public void FileSizeDisplay_FormatsCorrectly(long bytes, string expectedDisplay)
    {
        // Arrange
        var entry = CreateEntry("test.txt", bytes);

        // Act
        var display = entry.FileSizeDisplay;

        // Assert
        display.Should().Be(expectedDisplay);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var entry1 = new FileIndexEntry("file.cs", "/path/file.cs", "file.cs", timestamp, 1024);
        var entry2 = new FileIndexEntry("file.cs", "/path/file.cs", "file.cs", timestamp, 1024);

        // Assert
        entry1.Should().Be(entry2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var entry1 = CreateEntry("file1.cs");
        var entry2 = CreateEntry("file2.cs");

        // Assert
        entry1.Should().NotBe(entry2);
    }

    #endregion

    #region Helper Methods

    private static FileIndexEntry CreateEntry(string fileName, long fileSize = 1024)
    {
        return new FileIndexEntry(
            fileName,
            $"/workspace/{fileName}",
            fileName,
            DateTime.UtcNow,
            fileSize
        );
    }

    #endregion
}
