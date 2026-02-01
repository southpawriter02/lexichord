using Lexichord.Abstractions.Contracts.Ingestion;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Unit tests for the <see cref="FileIndexingRequestedEvent"/> record and
/// <see cref="FileIndexingChangeType"/> enum.
/// </summary>
public class FileIndexingRequestedEventTests
{
    #region Enum Value Tests

    [Fact]
    public void FileIndexingChangeType_HasExpectedValues()
    {
        // Assert - enum should have exactly 3 values
        Enum.GetValues<FileIndexingChangeType>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(FileIndexingChangeType.Created, 0)]
    [InlineData(FileIndexingChangeType.Changed, 1)]
    [InlineData(FileIndexingChangeType.Renamed, 2)]
    public void FileIndexingChangeType_HasCorrectOrdinalValues(
        FileIndexingChangeType changeType,
        int expectedValue)
    {
        // Assert
        ((int)changeType).Should().Be(expectedValue);
    }

    #endregion

    #region Record Construction Tests

    [Fact]
    public void Constructor_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var evt = new FileIndexingRequestedEvent(
            "/path/to/file.md",
            FileIndexingChangeType.Changed,
            null,
            timestamp);

        // Assert
        evt.FilePath.Should().Be("/path/to/file.md");
        evt.ChangeType.Should().Be(FileIndexingChangeType.Changed);
        evt.OldPath.Should().BeNull();
        evt.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void Constructor_WithOldPath_SetsOldPathCorrectly()
    {
        // Act
        var evt = new FileIndexingRequestedEvent(
            "/path/to/new.md",
            FileIndexingChangeType.Renamed,
            "/path/to/old.md",
            DateTimeOffset.UtcNow);

        // Assert
        evt.OldPath.Should().Be("/path/to/old.md");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void ForCreated_SetsCorrectChangeType()
    {
        // Act
        var evt = FileIndexingRequestedEvent.ForCreated("/path/to/file.md");

        // Assert
        evt.ChangeType.Should().Be(FileIndexingChangeType.Created);
        evt.FilePath.Should().Be("/path/to/file.md");
        evt.OldPath.Should().BeNull();
    }

    [Fact]
    public void ForCreated_SetsTimestampToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = FileIndexingRequestedEvent.ForCreated("/file.md");
        var after = DateTimeOffset.UtcNow;

        // Assert
        evt.Timestamp.Should().BeOnOrAfter(before);
        evt.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void ForChanged_SetsCorrectChangeType()
    {
        // Act
        var evt = FileIndexingRequestedEvent.ForChanged("/path/to/file.md");

        // Assert
        evt.ChangeType.Should().Be(FileIndexingChangeType.Changed);
        evt.FilePath.Should().Be("/path/to/file.md");
        evt.OldPath.Should().BeNull();
    }

    [Fact]
    public void ForChanged_SetsTimestampToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = FileIndexingRequestedEvent.ForChanged("/file.md");
        var after = DateTimeOffset.UtcNow;

        // Assert
        evt.Timestamp.Should().BeOnOrAfter(before);
        evt.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void ForRenamed_SetsCorrectChangeTypeAndPaths()
    {
        // Act
        var evt = FileIndexingRequestedEvent.ForRenamed("/new/path.md", "/old/path.md");

        // Assert
        evt.ChangeType.Should().Be(FileIndexingChangeType.Renamed);
        evt.FilePath.Should().Be("/new/path.md");
        evt.OldPath.Should().Be("/old/path.md");
    }

    [Fact]
    public void ForRenamed_SetsTimestampToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = FileIndexingRequestedEvent.ForRenamed("/new.md", "/old.md");
        var after = DateTimeOffset.UtcNow;

        // Assert
        evt.Timestamp.Should().BeOnOrAfter(before);
        evt.Timestamp.Should().BeOnOrBefore(after);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void RecordsWithSameValues_AreEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evt1 = new FileIndexingRequestedEvent("/file.md", FileIndexingChangeType.Changed, null, timestamp);
        var evt2 = new FileIndexingRequestedEvent("/file.md", FileIndexingChangeType.Changed, null, timestamp);

        // Assert
        evt1.Should().Be(evt2);
    }

    [Fact]
    public void RecordsWithDifferentPaths_AreNotEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evt1 = new FileIndexingRequestedEvent("/file1.md", FileIndexingChangeType.Changed, null, timestamp);
        var evt2 = new FileIndexingRequestedEvent("/file2.md", FileIndexingChangeType.Changed, null, timestamp);

        // Assert
        evt1.Should().NotBe(evt2);
    }

    [Fact]
    public void RecordsWithDifferentChangeTypes_AreNotEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evt1 = new FileIndexingRequestedEvent("/file.md", FileIndexingChangeType.Created, null, timestamp);
        var evt2 = new FileIndexingRequestedEvent("/file.md", FileIndexingChangeType.Changed, null, timestamp);

        // Assert
        evt1.Should().NotBe(evt2);
    }

    [Fact]
    public void Record_SupportsWithExpression()
    {
        // Arrange
        var original = FileIndexingRequestedEvent.ForCreated("/file.md");

        // Act
        var updated = original with { ChangeType = FileIndexingChangeType.Changed };

        // Assert
        updated.ChangeType.Should().Be(FileIndexingChangeType.Changed);
        updated.FilePath.Should().Be(original.FilePath);
    }

    #endregion

    #region INotification Implementation Tests

    [Fact]
    public void FileIndexingRequestedEvent_ImplementsINotification()
    {
        // Arrange
        var evt = FileIndexingRequestedEvent.ForCreated("/file.md");

        // Assert
        evt.Should().BeAssignableTo<MediatR.INotification>();
    }

    #endregion
}
