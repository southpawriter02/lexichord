// =============================================================================
// File: AxiomCacheServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AxiomCacheService.
// =============================================================================
// LOGIC: Tests cache operations, expiration, and invalidation.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.Axioms;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="AxiomCacheService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6f")]
public class AxiomCacheServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<AxiomCacheService>> _mockLogger;
    private readonly AxiomCacheService _sut;

    public AxiomCacheServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<AxiomCacheService>>();
        _sut = new AxiomCacheService(_cache, _mockLogger.Object);
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AxiomCacheService(null!, _mockLogger.Object));

        Assert.Equal("cache", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AxiomCacheService(_cache, null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region TryGet Tests

    [Fact]
    public void TryGet_WhenNotCached_ReturnsFalse()
    {
        // Act
        var result = _sut.TryGet("nonexistent-id", out var axiom);

        // Assert
        Assert.False(result);
        Assert.Null(axiom);
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsTrue()
    {
        // Arrange
        var axiom = CreateTestAxiom("test-001");
        _sut.Set(axiom);

        // Act
        var result = _sut.TryGet("test-001", out var cached);

        // Assert
        Assert.True(result);
        Assert.Equal(axiom, cached);
    }

    #endregion

    #region TryGetByType Tests

    [Fact]
    public void TryGetByType_WhenNotCached_ReturnsFalse()
    {
        // Act
        var result = _sut.TryGetByType("SomeType", out var axioms);

        // Assert
        Assert.False(result);
        Assert.Null(axioms);
    }

    [Fact]
    public void SetByType_ThenTryGetByType_ReturnsTrue()
    {
        // Arrange
        var axioms = new List<Axiom> { CreateTestAxiom("test-001"), CreateTestAxiom("test-002") };
        _sut.SetByType("Endpoint", axioms);

        // Act
        var result = _sut.TryGetByType("Endpoint", out var cached);

        // Assert
        Assert.True(result);
        Assert.Equal(2, cached!.Count);
    }

    #endregion

    #region TryGetAll Tests

    [Fact]
    public void TryGetAll_WhenNotCached_ReturnsFalse()
    {
        // Act
        var result = _sut.TryGetAll(out var axioms);

        // Assert
        Assert.False(result);
        Assert.Null(axioms);
    }

    [Fact]
    public void SetAll_ThenTryGetAll_ReturnsTrue()
    {
        // Arrange
        var axioms = new List<Axiom> { CreateTestAxiom("test-001") };
        _sut.SetAll(axioms);

        // Act
        var result = _sut.TryGetAll(out var cached);

        // Assert
        Assert.True(result);
        Assert.Single(cached!);
    }

    #endregion

    #region Invalidation Tests

    [Fact]
    public void Invalidate_RemovesFromCache()
    {
        // Arrange
        var axiom = CreateTestAxiom("test-001");
        _sut.Set(axiom);
        Assert.True(_sut.TryGet("test-001", out _)); // Verify cached

        // Act
        _sut.Invalidate("test-001");

        // Assert
        Assert.False(_sut.TryGet("test-001", out _));
    }

    [Fact]
    public void InvalidateAll_ClearsAllKey()
    {
        // Arrange
        var axioms = new List<Axiom> { CreateTestAxiom("test-001") };
        _sut.SetAll(axioms);
        Assert.True(_sut.TryGetAll(out _)); // Verify cached

        // Act
        _sut.InvalidateAll();

        // Assert
        Assert.False(_sut.TryGetAll(out _));
    }

    [Fact]
    public void InvalidateByType_RemovesTypeFromCache()
    {
        // Arrange
        var axioms = new List<Axiom> { CreateTestAxiom("test-001") };
        _sut.SetByType("Endpoint", axioms);
        Assert.True(_sut.TryGetByType("Endpoint", out _)); // Verify cached

        // Act
        _sut.InvalidateByType("Endpoint");

        // Assert
        Assert.False(_sut.TryGetByType("Endpoint", out _));
    }

    #endregion

    #region Helpers

    private static Axiom CreateTestAxiom(string id) => new()
    {
        Id = id,
        Name = $"Test Axiom {id}",
        TargetType = "Endpoint",
        Rules = Array.Empty<AxiomRule>()
    };

    #endregion
}
