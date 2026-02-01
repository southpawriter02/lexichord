using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Contracts;

/// <summary>
/// Unit tests for the <see cref="FileMetadata"/> and <see cref="FileMetadataWithHash"/> records.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify record equality semantics, with-expression support,
/// and the NotFound static property. They ensure the records behave correctly
/// as immutable data transfer objects.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.2b")]
public class FileMetadataRecordTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);

    #region FileMetadata Tests

    [Fact]
    public void FileMetadata_WithSameValues_AreEqual()
    {
        // Arrange
        var meta1 = new FileMetadata
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp
        };

        var meta2 = new FileMetadata
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp
        };

        // Assert
        meta1.Should().Be(meta2, because: "records with identical values should be equal");
    }

    [Fact]
    public void FileMetadata_WithDifferentSize_AreNotEqual()
    {
        // Arrange
        var meta1 = new FileMetadata
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp
        };

        var meta2 = meta1 with { Size = 2048 };

        // Assert
        meta1.Should().NotBe(meta2, because: "records with different sizes should not be equal");
    }

    [Fact]
    public void FileMetadata_SupportsWithExpression()
    {
        // Arrange
        var original = new FileMetadata
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp
        };

        // Act
        var updated = original with { Size = 2048 };

        // Assert
        updated.Size.Should().Be(2048);
        updated.Exists.Should().Be(original.Exists, because: "unchanged properties should be preserved");
        updated.LastModified.Should().Be(original.LastModified);
    }

    [Fact]
    public void FileMetadata_NotFound_HasCorrectDefaults()
    {
        // Act
        var notFound = FileMetadata.NotFound;

        // Assert
        notFound.Exists.Should().BeFalse(because: "NotFound indicates file does not exist");
        notFound.Size.Should().Be(0, because: "non-existent files have no size");
        notFound.LastModified.Should().Be(default, because: "non-existent files have no timestamp");
    }

    [Fact]
    public void FileMetadata_NotFound_IsConsistentAcrossCalls()
    {
        // Act
        var notFound1 = FileMetadata.NotFound;
        var notFound2 = FileMetadata.NotFound;

        // Assert
        notFound1.Should().Be(notFound2, because: "NotFound should always return equivalent instances");
    }

    [Fact]
    public void FileMetadata_HasGetHashCode()
    {
        // Arrange
        var meta1 = new FileMetadata
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp
        };

        var meta2 = new FileMetadata
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp
        };

        // Assert
        meta1.GetHashCode().Should().Be(meta2.GetHashCode(),
            because: "equal records should have equal hash codes");
    }

    #endregion

    #region FileMetadataWithHash Tests

    [Fact]
    public void FileMetadataWithHash_WithSameValues_AreEqual()
    {
        // Arrange
        var meta1 = new FileMetadataWithHash
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp,
            Hash = "abc123"
        };

        var meta2 = new FileMetadataWithHash
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp,
            Hash = "abc123"
        };

        // Assert
        meta1.Should().Be(meta2, because: "records with identical values should be equal");
    }

    [Fact]
    public void FileMetadataWithHash_WithDifferentHash_AreNotEqual()
    {
        // Arrange
        var meta1 = new FileMetadataWithHash
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp,
            Hash = "abc123"
        };

        var meta2 = meta1 with { Hash = "def456" };

        // Assert
        meta1.Should().NotBe(meta2, because: "records with different hashes should not be equal");
    }

    [Fact]
    public void FileMetadataWithHash_InheritsFromFileMetadata()
    {
        // Arrange
        var metadata = new FileMetadataWithHash
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp,
            Hash = "abc123"
        };

        // Assert
        metadata.Should().BeAssignableTo<FileMetadata>(
            because: "FileMetadataWithHash should inherit from FileMetadata");
    }

    [Fact]
    public void FileMetadataWithHash_CanHaveNullHash()
    {
        // Arrange
        var metadata = new FileMetadataWithHash
        {
            Exists = false,
            Size = 0,
            LastModified = default,
            Hash = null
        };

        // Assert
        metadata.Hash.Should().BeNull(because: "non-existent files have no hash");
    }

    [Fact]
    public void FileMetadataWithHash_HashFormat_Is64CharHex()
    {
        // Arrange - SHA-256 produces 64 hex characters
        var validHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        var metadata = new FileMetadataWithHash
        {
            Exists = true,
            Size = 0,
            LastModified = TestTimestamp,
            Hash = validHash
        };

        // Assert
        metadata.Hash.Should().HaveLength(64, because: "SHA-256 hex is always 64 characters");
        metadata.Hash.Should().MatchRegex("^[a-f0-9]{64}$", because: "SHA-256 is lowercase hex");
    }

    [Fact]
    public void FileMetadataWithHash_SupportsWithExpression()
    {
        // Arrange
        var original = new FileMetadataWithHash
        {
            Exists = true,
            Size = 1024,
            LastModified = TestTimestamp,
            Hash = "abc123"
        };

        // Act
        var updated = original with { Hash = "def456" };

        // Assert
        updated.Hash.Should().Be("def456");
        updated.Size.Should().Be(original.Size, because: "unchanged properties should be preserved");
    }

    #endregion
}
