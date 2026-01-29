using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for TerminologyRepository.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify constructor validation and cache behavior patterns.
/// Full database testing is done in integration tests.
/// </remarks>
[Trait("Category", "Unit")]
public class TerminologyRepositoryTests
{
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IMemoryCache> _cacheMock = new();
    private readonly IOptions<TerminologyCacheOptions> _cacheOptions;
    private readonly ILogger<TerminologyRepository> _logger;

    public TerminologyRepositoryTests()
    {
        _cacheOptions = Options.Create(new TerminologyCacheOptions());
        _logger = NullLogger<TerminologyRepository>.Instance;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologyRepository(null!, _cacheMock.Object, _cacheOptions, _logger));
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologyRepository(_connectionFactoryMock.Object, null!, _cacheOptions, _logger));
    }

    [Fact]
    public void Constructor_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologyRepository(_connectionFactoryMock.Object, _cacheMock.Object, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologyRepository(_connectionFactoryMock.Object, _cacheMock.Object, _cacheOptions, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var repository = CreateRepository();

        // Assert
        repository.Should().NotBeNull();
    }

    #endregion

    #region Cache Behavior Tests

    [Fact]
    public async Task GetAllActiveTermsAsync_CacheHit_ReturnsCachedData()
    {
        // Arrange
        var cachedTerms = new HashSet<StyleTerm>
        {
            new() { Id = Guid.NewGuid(), Term = "test", IsActive = true }
        };
        
        object? outValue = cachedTerms;
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out outValue))
            .Returns(true);

        var repository = CreateRepository();

        // Act
        var result = await repository.GetAllActiveTermsAsync();

        // Assert
        result.Should().BeSameAs(cachedTerms);
        _connectionFactoryMock.Verify(
            c => c.CreateConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not hit database on cache hit");
    }

    [Fact]
    public async Task InvalidateCacheAsync_RemovesCacheEntry()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        await repository.InvalidateCacheAsync();

        // Assert
        _cacheMock.Verify(
            c => c.Remove(It.Is<object>(k => k.ToString()!.Contains("ActiveTerms"))),
            Times.Once);
    }

    #endregion

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_EmptyCategory_ThrowsArgumentException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.GetByCategoryAsync(""));
    }

    [Fact]
    public async Task GetByCategoryAsync_WhitespaceCategory_ThrowsArgumentException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.GetByCategoryAsync("   "));
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_EmptySearchTerm_ReturnsEmpty()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.SearchAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WhitespaceSearchTerm_ReturnsEmpty()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.SearchAsync("   ");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private TerminologyRepository CreateRepository()
    {
        return new TerminologyRepository(
            _connectionFactoryMock.Object,
            _cacheMock.Object,
            _cacheOptions,
            _logger);
    }

    #endregion
}
