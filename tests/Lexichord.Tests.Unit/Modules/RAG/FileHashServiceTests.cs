using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for the <see cref="FileHashService"/> implementation.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the SHA-256 hash computation, tiered change detection,
/// and metadata retrieval. Temporary files are used to test actual file I/O.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.2b")]
public class FileHashServiceTests : IDisposable
{
    private readonly FileHashService _sut;
    private readonly List<string> _tempFiles = new();

    public FileHashServiceTests()
    {
        _sut = new FileHashService(NullLogger<FileHashService>.Instance);
    }

    public void Dispose()
    {
        // LOGIC: Clean up all temporary files created during tests.
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    #region ComputeHashAsync Tests

    [Fact]
    public async Task ComputeHashAsync_ValidFile_ReturnsSha256Hex()
    {
        // Arrange
        var tempFile = CreateTempFile("Hello, World!");

        // Act
        var hash = await _sut.ComputeHashAsync(tempFile);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64, because: "SHA-256 produces 64 hex characters");
        hash.Should().MatchRegex("^[a-f0-9]{64}$", because: "hash should be lowercase hex");
    }

    [Fact]
    public async Task ComputeHashAsync_SameContent_ReturnsSameHash()
    {
        // Arrange
        var content = "Identical content for testing";
        var file1 = CreateTempFile(content);
        var file2 = CreateTempFile(content);

        // Act
        var hash1 = await _sut.ComputeHashAsync(file1);
        var hash2 = await _sut.ComputeHashAsync(file2);

        // Assert
        hash1.Should().Be(hash2, because: "identical content should produce identical hashes");
    }

    [Fact]
    public async Task ComputeHashAsync_DifferentContent_ReturnsDifferentHash()
    {
        // Arrange
        var file1 = CreateTempFile("Content A");
        var file2 = CreateTempFile("Content B");

        // Act
        var hash1 = await _sut.ComputeHashAsync(file1);
        var hash2 = await _sut.ComputeHashAsync(file2);

        // Assert
        hash1.Should().NotBe(hash2, because: "different content should produce different hashes");
    }

    [Fact]
    public async Task ComputeHashAsync_EmptyFile_ReturnsKnownHash()
    {
        // Arrange - SHA-256 of empty file is a known value
        var tempFile = CreateTempFile("");
        var expectedEmptyHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var hash = await _sut.ComputeHashAsync(tempFile);

        // Assert
        hash.Should().Be(expectedEmptyHash, because: "empty file has a known SHA-256 hash");
    }

    [Fact]
    public async Task ComputeHashAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var act = async () => await _sut.ComputeHashAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ComputeHashAsync_SupportsCancellation()
    {
        // Arrange
        var tempFile = CreateTempFile("Some content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _sut.ComputeHashAsync(tempFile, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region HasChangedAsync Tests

    [Fact]
    public async Task HasChangedAsync_SameHashSameSizeSameTimestamp_ReturnsFalse()
    {
        // Arrange
        var content = "Test content";
        var tempFile = CreateTempFile(content);
        var hash = await _sut.ComputeHashAsync(tempFile);
        var metadata = _sut.GetMetadata(tempFile);

        // Act
        var hasChanged = await _sut.HasChangedAsync(
            tempFile,
            hash,
            metadata.Size,
            metadata.LastModified);

        // Assert
        hasChanged.Should().BeFalse(because: "file with matching hash, size, and timestamp is unchanged");
    }

    [Fact]
    public async Task HasChangedAsync_DifferentSize_ReturnsTrue_WithoutHashComputation()
    {
        // Arrange
        var tempFile = CreateTempFile("New longer content");

        // Act - stored size is smaller than current
        var hasChanged = await _sut.HasChangedAsync(
            tempFile,
            "oldhash", // This hash won't be checked
            5, // Much smaller than actual content
            DateTimeOffset.UtcNow.AddDays(-1));

        // Assert
        hasChanged.Should().BeTrue(because: "size difference indicates change without needing hash");
    }

    [Fact]
    public async Task HasChangedAsync_SameTimestamp_ReturnsFalse_WithoutHashComputation()
    {
        // Arrange
        var tempFile = CreateTempFile("Content");
        var metadata = _sut.GetMetadata(tempFile);

        // Act - same size, same timestamp
        var hasChanged = await _sut.HasChangedAsync(
            tempFile,
            "anyhash", // This hash won't be checked
            metadata.Size,
            metadata.LastModified);

        // Assert
        hasChanged.Should().BeFalse(because: "unchanged timestamp indicates no change");
    }

    [Fact]
    public async Task HasChangedAsync_DeletedFile_ReturnsTrue()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var hasChanged = await _sut.HasChangedAsync(
            nonExistentPath,
            "somehash",
            1000,
            DateTimeOffset.UtcNow.AddDays(-1));

        // Assert
        hasChanged.Should().BeTrue(because: "deleted file is considered changed");
    }

    [Fact]
    public async Task HasChangedAsync_DifferentHash_ReturnsTrue()
    {
        // Arrange
        var tempFile = CreateTempFile("New content");
        var metadata = _sut.GetMetadata(tempFile);

        // Use null timestamp to force hash comparison
        var hasChanged = await _sut.HasChangedAsync(
            tempFile,
            "differenthash",
            metadata.Size,
            null); // No stored timestamp forces hash check

        // Assert
        hasChanged.Should().BeTrue(because: "different hash indicates content change");
    }

    [Fact]
    public async Task HasChangedAsync_NullStoredTimestamp_ComputesHash()
    {
        // Arrange
        var tempFile = CreateTempFile("Test content");
        var hash = await _sut.ComputeHashAsync(tempFile);
        var metadata = _sut.GetMetadata(tempFile);

        // Act - null timestamp should force hash comparison
        var hasChanged = await _sut.HasChangedAsync(
            tempFile,
            hash, // Same hash
            metadata.Size,
            null); // No stored timestamp

        // Assert
        hasChanged.Should().BeFalse(because: "hash matches even without timestamp");
    }

    [Fact]
    public async Task HasChangedAsync_TimestampWithinTolerance_ReturnsFalse()
    {
        // Arrange
        var tempFile = CreateTempFile("Test");
        var metadata = _sut.GetMetadata(tempFile);

        // Use timestamp within 1 second tolerance
        var slightlyDifferentTimestamp = metadata.LastModified.AddMilliseconds(500);

        // Act
        var hasChanged = await _sut.HasChangedAsync(
            tempFile,
            "anyhash",
            metadata.Size,
            slightlyDifferentTimestamp);

        // Assert
        hasChanged.Should().BeFalse(because: "timestamp within tolerance is considered unchanged");
    }

    #endregion

    #region GetMetadata Tests

    [Fact]
    public void GetMetadata_ExistingFile_ReturnsCorrectData()
    {
        // Arrange
        var tempFile = CreateTempFile("Test");

        // Act
        var metadata = _sut.GetMetadata(tempFile);

        // Assert
        metadata.Exists.Should().BeTrue();
        metadata.Size.Should().Be(4, because: "'Test' is 4 bytes");
        metadata.LastModified.Should().BeCloseTo(
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetMetadata_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var metadata = _sut.GetMetadata(nonExistentPath);

        // Assert
        metadata.Exists.Should().BeFalse();
        metadata.Size.Should().Be(0);
        metadata.LastModified.Should().Be(default);
    }

    [Fact]
    public void GetMetadata_ReturnsUtcTimestamp()
    {
        // Arrange
        var tempFile = CreateTempFile("Content");

        // Act
        var metadata = _sut.GetMetadata(tempFile);

        // Assert
        metadata.LastModified.Offset.Should().Be(TimeSpan.Zero,
            because: "timestamps should be in UTC");
    }

    #endregion

    #region GetMetadataWithHashAsync Tests

    [Fact]
    public async Task GetMetadataWithHashAsync_ExistingFile_ReturnsMetadataAndHash()
    {
        // Arrange
        var tempFile = CreateTempFile("Test content");

        // Act
        var metadata = await _sut.GetMetadataWithHashAsync(tempFile);

        // Assert
        metadata.Exists.Should().BeTrue();
        metadata.Size.Should().BeGreaterThan(0);
        metadata.Hash.Should().NotBeNullOrEmpty();
        metadata.Hash.Should().HaveLength(64);
    }

    [Fact]
    public async Task GetMetadataWithHashAsync_NonExistentFile_ReturnsNullHash()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var metadata = await _sut.GetMetadataWithHashAsync(nonExistentPath);

        // Assert
        metadata.Exists.Should().BeFalse();
        metadata.Size.Should().Be(0);
        metadata.Hash.Should().BeNull();
    }

    [Fact]
    public async Task GetMetadataWithHashAsync_HashMatchesComputeHash()
    {
        // Arrange
        var tempFile = CreateTempFile("Consistent content");
        var expectedHash = await _sut.ComputeHashAsync(tempFile);

        // Act
        var metadata = await _sut.GetMetadataWithHashAsync(tempFile);

        // Assert
        metadata.Hash.Should().Be(expectedHash,
            because: "GetMetadataWithHashAsync should use same hash algorithm");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FileHashService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion
}
