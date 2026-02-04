// =============================================================================
// File: CacheKeyGeneratorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CacheKeyGenerator.
// =============================================================================
// VERSION: v0.5.8c (Multi-Layer Caching System)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="CacheKeyGenerator"/>.
/// </summary>
public sealed class CacheKeyGeneratorTests
{
    private readonly Mock<ILogger<CacheKeyGenerator>> _loggerMock;
    private readonly CacheKeyGenerator _sut;

    public CacheKeyGeneratorTests()
    {
        _loggerMock = new Mock<ILogger<CacheKeyGenerator>>();
        _sut = new CacheKeyGenerator(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CacheKeyGenerator(null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region Deterministic Key Generation Tests

    [Fact]
    public void GenerateKey_SameInput_ProducesSameKey()
    {
        var options = new SearchOptions { TopK = 10, MinScore = 0.7f };

        var key1 = _sut.GenerateKey("test query", options);
        var key2 = _sut.GenerateKey("test query", options);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentQuery_ProducesDifferentKey()
    {
        var options = SearchOptions.Default;

        var key1 = _sut.GenerateKey("query one", options);
        var key2 = _sut.GenerateKey("query two", options);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentTopK_ProducesDifferentKey()
    {
        var options1 = new SearchOptions { TopK = 10 };
        var options2 = new SearchOptions { TopK = 20 };

        var key1 = _sut.GenerateKey("test", options1);
        var key2 = _sut.GenerateKey("test", options2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentMinScore_ProducesDifferentKey()
    {
        var options1 = new SearchOptions { MinScore = 0.5f };
        var options2 = new SearchOptions { MinScore = 0.7f };

        var key1 = _sut.GenerateKey("test", options1);
        var key2 = _sut.GenerateKey("test", options2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_WithDocumentFilter_ProducesDifferentKey()
    {
        var options1 = new SearchOptions { DocumentFilter = null };
        var options2 = new SearchOptions { DocumentFilter = Guid.NewGuid() };

        var key1 = _sut.GenerateKey("test", options1);
        var key2 = _sut.GenerateKey("test", options2);

        Assert.NotEqual(key1, key2);
    }

    #endregion

    #region Normalization Tests

    [Fact]
    public void GenerateKey_CaseInsensitive()
    {
        var options = SearchOptions.Default;

        var key1 = _sut.GenerateKey("Test Query", options);
        var key2 = _sut.GenerateKey("test query", options);
        var key3 = _sut.GenerateKey("TEST QUERY", options);

        Assert.Equal(key1, key2);
        Assert.Equal(key2, key3);
    }

    [Fact]
    public void GenerateKey_TrimsWhitespace()
    {
        var options = SearchOptions.Default;

        var key1 = _sut.GenerateKey("test query", options);
        var key2 = _sut.GenerateKey("  test query  ", options);
        var key3 = _sut.GenerateKey("\ttest query\n", options);

        Assert.Equal(key1, key2);
        Assert.Equal(key2, key3);
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public void GenerateKey_WithNullQuery_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _sut.GenerateKey(null!, SearchOptions.Default));

        Assert.Equal("query", ex.ParamName);
    }

    [Fact]
    public void GenerateKey_WithNullOptions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _sut.GenerateKey("test", null!));

        Assert.Equal("options", ex.ParamName);
    }

    #endregion

    #region Key Format Tests

    [Fact]
    public void GenerateKey_ReturnsValidSha256Hash()
    {
        var key = _sut.GenerateKey("test", SearchOptions.Default);

        // SHA256 produces 64 hex characters (256 bits = 32 bytes = 64 hex chars)
        Assert.Equal(64, key.Length);
        Assert.True(key.All(c => "0123456789abcdef".Contains(c)));
    }

    [Fact]
    public void GenerateKey_DefaultOptions_ProducesConsistentKey()
    {
        var key1 = _sut.GenerateKey("hello world");
        var key2 = _sut.GenerateKey("hello world", SearchOptions.Default);

        Assert.Equal(key1, key2);
    }

    #endregion
}
