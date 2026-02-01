using Lexichord.Abstractions.Contracts;
using Moq;

namespace Lexichord.Tests.Unit.Abstractions.Contracts;

/// <summary>
/// Contract tests for the <see cref="IFileHashService"/> interface.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify that the interface can be mocked and that
/// consumers can interact with it correctly. They establish the expected
/// behavior patterns without testing implementation details.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.2b")]
public class IFileHashServiceContractTests
{
    private readonly Mock<IFileHashService> _mockService;

    public IFileHashServiceContractTests()
    {
        _mockService = new Mock<IFileHashService>();
    }

    #region Interface Mockability Tests

    [Fact]
    public void IFileHashService_CanBeMocked()
    {
        // Assert
        _mockService.Object.Should().NotBeNull();
        _mockService.Object.Should().BeAssignableTo<IFileHashService>();
    }

    #endregion

    #region ComputeHashAsync Contract Tests

    [Fact]
    public async Task ComputeHashAsync_ReturnsHexString()
    {
        // Arrange
        var expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        _mockService
            .Setup(s => s.ComputeHashAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHash);

        // Act
        var result = await _mockService.Object.ComputeHashAsync("/path/to/file.md");

        // Assert
        result.Should().Be(expectedHash);
        result.Should().HaveLength(64, because: "SHA-256 hex is always 64 characters");
    }

    [Fact]
    public async Task ComputeHashAsync_SupportsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockService
            .Setup(s => s.ComputeHashAsync(
                It.IsAny<string>(),
                It.Is<CancellationToken>(ct => ct.IsCancellationRequested)))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await _mockService.Object.ComputeHashAsync("/file.md", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ComputeHashAsync_ThrowsFileNotFoundException()
    {
        // Arrange
        _mockService
            .Setup(s => s.ComputeHashAsync(
                It.Is<string>(p => p.Contains("nonexistent")),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var act = async () => await _mockService.Object.ComputeHashAsync("/nonexistent/file.md");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region HasChangedAsync Contract Tests

    [Fact]
    public async Task HasChangedAsync_ReturnsBool()
    {
        // Arrange
        _mockService
            .Setup(s => s.HasChangedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockService.Object.HasChangedAsync(
            "/path/to/file.md",
            "oldhash",
            1024,
            DateTimeOffset.UtcNow.AddDays(-1));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasChangedAsync_AcceptsNullTimestamp()
    {
        // Arrange
        _mockService
            .Setup(s => s.HasChangedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _mockService.Object.HasChangedAsync(
            "/path/to/file.md",
            "hash",
            1024,
            null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetMetadata Contract Tests

    [Fact]
    public void GetMetadata_ReturnsFileMetadata()
    {
        // Arrange
        var expectedMetadata = new FileMetadata
        {
            Exists = true,
            Size = 1024,
            LastModified = DateTimeOffset.UtcNow
        };

        _mockService
            .Setup(s => s.GetMetadata(It.IsAny<string>()))
            .Returns(expectedMetadata);

        // Act
        var result = _mockService.Object.GetMetadata("/path/to/file.md");

        // Assert
        result.Should().Be(expectedMetadata);
        result.Exists.Should().BeTrue();
    }

    [Fact]
    public void GetMetadata_ReturnsNotFoundForMissingFile()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetMetadata(It.Is<string>(p => p.Contains("missing"))))
            .Returns(FileMetadata.NotFound);

        // Act
        var result = _mockService.Object.GetMetadata("/missing/file.md");

        // Assert
        result.Exists.Should().BeFalse();
        result.Size.Should().Be(0);
    }

    #endregion

    #region GetMetadataWithHashAsync Contract Tests

    [Fact]
    public async Task GetMetadataWithHashAsync_ReturnsMetadataWithHash()
    {
        // Arrange
        var expectedMetadata = new FileMetadataWithHash
        {
            Exists = true,
            Size = 1024,
            LastModified = DateTimeOffset.UtcNow,
            Hash = "abc123"
        };

        _mockService
            .Setup(s => s.GetMetadataWithHashAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        // Act
        var result = await _mockService.Object.GetMetadataWithHashAsync("/path/to/file.md");

        // Assert
        result.Should().Be(expectedMetadata);
        result.Hash.Should().Be("abc123");
    }

    [Fact]
    public async Task GetMetadataWithHashAsync_ReturnsNullHashForMissingFile()
    {
        // Arrange
        var metadata = new FileMetadataWithHash
        {
            Exists = false,
            Size = 0,
            LastModified = default,
            Hash = null
        };

        _mockService
            .Setup(s => s.GetMetadataWithHashAsync(
                It.Is<string>(p => p.Contains("missing")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        // Act
        var result = await _mockService.Object.GetMetadataWithHashAsync("/missing/file.md");

        // Assert
        result.Exists.Should().BeFalse();
        result.Hash.Should().BeNull();
    }

    #endregion
}
