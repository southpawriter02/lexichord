// =============================================================================
// File: BM25SearchServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for BM25SearchService keyword search pipeline.
// =============================================================================
// LOGIC: Verifies the full search pipeline orchestration:
//   - Constructor null-parameter validation (6 dependencies).
//   - Input validation (query, TopK, MinScore ranges).
//   - License tier gating via SearchLicenseGuard.
//   - Query preprocessing delegation to IQueryPreprocessor.
//
//   NOTE: SQL execution and Dapper mapping are NOT unit-tested here because
//   they require a live PostgreSQL database. Integration tests for the SQL
//   layer will be delivered in a future sub-part.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Search;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for <see cref="BM25SearchService"/>.
/// Verifies constructor validation, input validation, license gating,
/// and preprocessing delegation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.1b")]
public class BM25SearchServiceTests
{
    private readonly Mock<IDbConnectionFactory> _dbFactoryMock;
    private readonly Mock<IQueryPreprocessor> _preprocessorMock;
    private readonly Mock<IDocumentRepository> _docRepoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<BM25SearchService>> _loggerMock;

    // LOGIC: SearchLicenseGuard is a concrete class, so we need its own mocks.
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<SearchLicenseGuard>> _guardLoggerMock;
    private readonly SearchLicenseGuard _licenseGuard;

    public BM25SearchServiceTests()
    {
        _dbFactoryMock = new Mock<IDbConnectionFactory>();
        _preprocessorMock = new Mock<IQueryPreprocessor>();
        _docRepoMock = new Mock<IDocumentRepository>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<BM25SearchService>>();

        _licenseContextMock = new Mock<ILicenseContext>();
        _guardLoggerMock = new Mock<ILogger<SearchLicenseGuard>>();

        // LOGIC: Default to WriterPro so most tests pass the license check.
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        _licenseGuard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        // LOGIC: Default preprocessor returns trimmed query.
        _preprocessorMock
            .Setup(x => x.Process(It.IsAny<string>(), It.IsAny<SearchOptions>()))
            .Returns<string, SearchOptions>((q, _) => q.Trim());
    }

    /// <summary>
    /// Creates a <see cref="BM25SearchService"/> using the test mocks.
    /// </summary>
    private BM25SearchService CreateService() =>
        new(
            _dbFactoryMock.Object,
            _preprocessorMock.Object,
            _docRepoMock.Object,
            _licenseGuard,
            _mediatorMock.Object,
            _loggerMock.Object);

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDbFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BM25SearchService(
            null!, _preprocessorMock.Object,
            _docRepoMock.Object, _licenseGuard, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dbFactory");
    }

    [Fact]
    public void Constructor_NullPreprocessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BM25SearchService(
            _dbFactoryMock.Object, null!,
            _docRepoMock.Object, _licenseGuard, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("preprocessor");
    }

    [Fact]
    public void Constructor_NullDocRepo_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BM25SearchService(
            _dbFactoryMock.Object, _preprocessorMock.Object,
            null!, _licenseGuard, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("docRepo");
    }

    [Fact]
    public void Constructor_NullLicenseGuard_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BM25SearchService(
            _dbFactoryMock.Object, _preprocessorMock.Object,
            _docRepoMock.Object, null!, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseGuard");
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BM25SearchService(
            _dbFactoryMock.Object, _preprocessorMock.Object,
            _docRepoMock.Object, _licenseGuard, null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BM25SearchService(
            _dbFactoryMock.Object, _preprocessorMock.Object,
            _docRepoMock.Object, _licenseGuard, _mediatorMock.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateService();

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task SearchAsync_NullQuery_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.SearchAsync(null!, SearchOptions.Default);

        await act.Should().ThrowAsync<ArgumentException>(
            because: "null query should be rejected");
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.SearchAsync("", SearchOptions.Default);

        await act.Should().ThrowAsync<ArgumentException>(
            because: "empty query should be rejected");
    }

    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.SearchAsync("   ", SearchOptions.Default);

        await act.Should().ThrowAsync<ArgumentException>(
            because: "whitespace-only query should be rejected");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public async Task SearchAsync_InvalidTopK_ThrowsArgumentOutOfRangeException(int topK)
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { TopK = topK };

        // Act & Assert
        var act = () => service.SearchAsync("test query", options);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>(
            because: $"TopK={topK} is outside the valid range [1, 100]");
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    [InlineData(-1.0f)]
    [InlineData(float.MaxValue)]
    public async Task SearchAsync_InvalidMinScore_ThrowsArgumentOutOfRangeException(float minScore)
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { MinScore = minScore };

        // Act & Assert
        var act = () => service.SearchAsync("test query", options);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>(
            because: $"MinScore={minScore} is outside the valid range [0.0, 1.0]");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task SearchAsync_ValidTopK_DoesNotThrowValidationError(int topK)
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { TopK = topK };

        // Act & Assert — will fail at DB layer but not at validation.
        // We only care that validation passes.
        try
        {
            await service.SearchAsync("test query", options);
        }
        catch (Exception ex) when (ex is not ArgumentOutOfRangeException and not ArgumentException)
        {
            // Expected: DB connection will fail in unit tests. That's fine.
        }
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(0.7f)]
    [InlineData(1.0f)]
    public async Task SearchAsync_ValidMinScore_DoesNotThrowValidationError(float minScore)
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { MinScore = minScore };

        // Act & Assert — will fail at DB layer but not at validation.
        try
        {
            await service.SearchAsync("test query", options);
        }
        catch (Exception ex) when (ex is not ArgumentOutOfRangeException and not ArgumentException)
        {
            // Expected: DB connection will fail in unit tests. That's fine.
        }
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public async Task SearchAsync_CoreTier_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new BM25SearchService(
            _dbFactoryMock.Object, _preprocessorMock.Object,
            _docRepoMock.Object, guard, _mediatorMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = () => service.SearchAsync("test query", SearchOptions.Default);

        await act.Should().ThrowAsync<FeatureNotLicensedException>(
            because: "Core tier does not have access to BM25 search");
    }

    [Fact]
    public async Task SearchAsync_CoreTier_ExceptionContainsRequiredTier()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new BM25SearchService(
            _dbFactoryMock.Object, _preprocessorMock.Object,
            _docRepoMock.Object, guard, _mediatorMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = () => service.SearchAsync("test query", SearchOptions.Default);

        (await act.Should().ThrowAsync<FeatureNotLicensedException>())
            .Which.RequiredTier.Should().Be(LicenseTier.WriterPro,
                because: "the exception should indicate WriterPro as the minimum tier");
    }

    [Theory]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public async Task SearchAsync_AuthorizedTier_DoesNotThrowLicenseException(LicenseTier tier)
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(tier);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new BM25SearchService(
            _dbFactoryMock.Object, _preprocessorMock.Object,
            _docRepoMock.Object, guard, _mediatorMock.Object, _loggerMock.Object);

        // Act & Assert — will fail at DB layer, but NOT with FeatureNotLicensedException.
        try
        {
            await service.SearchAsync("test query", SearchOptions.Default);
        }
        catch (FeatureNotLicensedException)
        {
            Assert.Fail($"Tier {tier} should be authorized for BM25 search");
        }
        catch
        {
            // Expected: DB connection will fail in unit tests. That's fine.
        }
    }

    #endregion

    #region Preprocessing Tests

    [Fact]
    public async Task SearchAsync_DelegatesQueryToPreprocessor()
    {
        // Arrange
        var service = CreateService();
        var query = "  test query with spaces  ";

        // Act — will fail at DB layer, but preprocessing should have been called.
        try
        {
            await service.SearchAsync(query, SearchOptions.Default);
        }
        catch (Exception ex) when (ex is not ArgumentException and not FeatureNotLicensedException)
        {
            // Expected: DB connection will fail in unit tests.
        }

        // Assert
        _preprocessorMock.Verify(
            p => p.Process(query, It.IsAny<SearchOptions>()),
            Times.Once,
            "the raw query should be passed to the preprocessor");
    }

    [Fact]
    public async Task SearchAsync_PassesSearchOptionsToPreprocessor()
    {
        // Arrange
        var service = CreateService();
        var options = new SearchOptions { TopK = 5, MinScore = 0.5f };

        // Act
        try
        {
            await service.SearchAsync("test query", options);
        }
        catch (Exception ex) when (ex is not ArgumentException and not FeatureNotLicensedException)
        {
            // Expected: DB connection will fail in unit tests.
        }

        // Assert
        _preprocessorMock.Verify(
            p => p.Process(It.IsAny<string>(), options),
            Times.Once,
            "the search options should be forwarded to the preprocessor");
    }

    #endregion

    #region License Check Ordering Tests

    [Fact]
    public async Task SearchAsync_LicenseDenied_DoesNotCallPreprocessor()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object, _mediatorMock.Object, _guardLoggerMock.Object);
        var service = new BM25SearchService(
            _dbFactoryMock.Object, _preprocessorMock.Object,
            _docRepoMock.Object, guard, _mediatorMock.Object, _loggerMock.Object);

        // Act
        try
        {
            await service.SearchAsync("test query", SearchOptions.Default);
        }
        catch (FeatureNotLicensedException)
        {
            // Expected.
        }

        // Assert
        _preprocessorMock.Verify(
            p => p.Process(It.IsAny<string>(), It.IsAny<SearchOptions>()),
            Times.Never,
            "preprocessing should not occur when the license check fails");
    }

    #endregion

    #region IBM25SearchService Interface Tests

    [Fact]
    public void Service_ImplementsIBM25SearchService()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IBM25SearchService>(
            because: "BM25SearchService implements the IBM25SearchService interface");
    }

    #endregion
}
