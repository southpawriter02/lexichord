// =============================================================================
// File: AxiomRepositoryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AxiomRepository constructor validation.
// =============================================================================
// LOGIC: Tests constructor null checks per established repository patterns.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Axioms;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="AxiomRepository"/> constructor validation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6f")]
public class AxiomRepositoryTests
{
    private readonly Mock<IDbConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IAxiomCacheService> _mockCache;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<ILogger<AxiomRepository>> _mockLogger;

    public AxiomRepositoryTests()
    {
        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockCache = new Mock<IAxiomCacheService>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockLogger = new Mock<ILogger<AxiomRepository>>();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var repository = new AxiomRepository(
            _mockConnectionFactory.Object,
            _mockCache.Object,
            _mockLicenseContext.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(repository);
    }

    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomRepository(
            null!,
            _mockCache.Object,
            _mockLicenseContext.Object,
            _mockLogger.Object));

        Assert.Equal("connectionFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomRepository(
            _mockConnectionFactory.Object,
            null!,
            _mockLicenseContext.Object,
            _mockLogger.Object));

        Assert.Equal("cache", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomRepository(
            _mockConnectionFactory.Object,
            _mockCache.Object,
            null!,
            _mockLogger.Object));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomRepository(
            _mockConnectionFactory.Object,
            _mockCache.Object,
            _mockLicenseContext.Object,
            null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion
}
