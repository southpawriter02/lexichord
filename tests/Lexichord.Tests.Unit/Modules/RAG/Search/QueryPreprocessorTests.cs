// =============================================================================
// File: QueryPreprocessorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for QueryPreprocessor normalization, abbreviation
//              expansion, and embedding caching.
// =============================================================================
// LOGIC: Verifies the full query preprocessing pipeline:
//   - Constructor null-parameter validation.
//   - Whitespace trimming and collapsing.
//   - Unicode NFC normalization.
//   - Abbreviation expansion with word boundary matching.
//   - Double-expansion prevention.
//   - SHA256-based embedding cache with case-insensitive keys.
//   - ClearCache() does not throw.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Search;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for <see cref="QueryPreprocessor"/>.
/// Verifies query normalization, abbreviation expansion, and embedding caching.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5c")]
public class QueryPreprocessorTests : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly QueryPreprocessor _sut;

    /// <summary>
    /// Initializes a new test instance with a fresh <see cref="MemoryCache"/>
    /// and <see cref="QueryPreprocessor"/> using a null logger.
    /// </summary>
    public QueryPreprocessorTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new QueryPreprocessor(_cache, NullLogger<QueryPreprocessor>.Instance);
    }

    /// <summary>
    /// Disposes the test-scoped <see cref="MemoryCache"/> instance.
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryPreprocessor(null!, NullLogger<QueryPreprocessor>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryPreprocessor(_cache, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => new QueryPreprocessor(_cache, NullLogger<QueryPreprocessor>.Instance);

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    #endregion

    #region Process Tests — Null/Empty Handling

    [Fact]
    public void Process_NullQuery_ReturnsEmpty()
    {
        // Act
        var result = _sut.Process(null!, SearchOptions.Default);

        // Assert
        result.Should().BeEmpty(
            because: "null input should produce an empty processed query");
    }

    [Fact]
    public void Process_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = _sut.Process(string.Empty, SearchOptions.Default);

        // Assert
        result.Should().BeEmpty(
            because: "empty string input should produce an empty processed query");
    }

    [Fact]
    public void Process_WhitespaceOnly_ReturnsEmpty()
    {
        // Act
        var result = _sut.Process("   ", SearchOptions.Default);

        // Assert
        result.Should().BeEmpty(
            because: "whitespace-only input should produce an empty processed query");
    }

    [Fact]
    public void Process_TabsAndNewlines_ReturnsEmpty()
    {
        // Act
        var result = _sut.Process("\t\n\r  ", SearchOptions.Default);

        // Assert
        result.Should().BeEmpty(
            because: "tabs and newlines should be treated as whitespace");
    }

    #endregion

    #region Process Tests — Whitespace Normalization

    [Fact]
    public void Process_TrimsLeadingAndTrailingWhitespace()
    {
        // Act
        var result = _sut.Process("  test query  ", SearchOptions.Default);

        // Assert
        result.Should().Be("test query",
            because: "leading and trailing whitespace should be removed");
    }

    [Fact]
    public void Process_CollapsesMultipleSpaces()
    {
        // Act
        var result = _sut.Process("test    multiple    spaces", SearchOptions.Default);

        // Assert
        result.Should().Be("test multiple spaces",
            because: "multiple consecutive spaces should collapse to a single space");
    }

    [Fact]
    public void Process_CollapsesTabsAndNewlinesToSpaces()
    {
        // Act
        var result = _sut.Process("test\t\tquery\n\nhere", SearchOptions.Default);

        // Assert
        result.Should().Be("test query here",
            because: "tabs and newlines should be collapsed to single spaces");
    }

    [Fact]
    public void Process_PreservesSingleSpaces()
    {
        // Act
        var result = _sut.Process("already clean query", SearchOptions.Default);

        // Assert
        result.Should().Be("already clean query",
            because: "already-normalized text should pass through unchanged");
    }

    #endregion

    #region Process Tests — Unicode Normalization

    [Fact]
    public void Process_NormalizesDecomposedUnicodeToNFC()
    {
        // Arrange
        // "café" with decomposed é: 'e' (U+0065) + combining acute accent (U+0301)
        var decomposed = "caf\u0065\u0301";

        // Act
        var result = _sut.Process(decomposed, SearchOptions.Default);

        // Assert
        // NFC precomposed form: 'é' (U+00E9)
        result.Should().Be("caf\u00e9",
            because: "decomposed Unicode should be normalized to NFC precomposed form");
    }

    [Fact]
    public void Process_PrecomposedUnicode_RemainsUnchanged()
    {
        // Arrange
        var precomposed = "caf\u00e9";

        // Act
        var result = _sut.Process(precomposed, SearchOptions.Default);

        // Assert
        result.Should().Be("caf\u00e9",
            because: "already NFC-normalized text should pass through unchanged");
    }

    #endregion

    #region Process Tests — Abbreviation Expansion

    [Fact]
    public void Process_WithoutExpansion_KeepsAbbreviations()
    {
        // Arrange
        var options = new SearchOptions { ExpandAbbreviations = false };

        // Act
        var result = _sut.Process("API documentation", options);

        // Assert
        result.Should().Be("API documentation",
            because: "abbreviations should not be expanded when ExpandAbbreviations is false");
        result.Should().NotContain("Application Programming Interface",
            because: "expansion should not occur when disabled");
    }

    [Fact]
    public void Process_WithExpansion_ExpandsAbbreviations()
    {
        // Arrange
        var options = new SearchOptions { ExpandAbbreviations = true };

        // Act
        var result = _sut.Process("API documentation", options);

        // Assert
        result.Should().Contain("Application Programming Interface",
            because: "API should be expanded to its full form when enabled");
    }

    [Fact]
    public void Process_WithExpansion_PreservesOriginalAbbreviation()
    {
        // Arrange
        var options = new SearchOptions { ExpandAbbreviations = true };

        // Act
        var result = _sut.Process("API docs", options);

        // Assert
        result.Should().StartWith("API",
            because: "the original abbreviation should be preserved in the expanded text");
        result.Should().Contain("API (Application Programming Interface)",
            because: "expansion format should be 'ABBR (Full Form)'");
    }

    [Fact]
    public void Process_WithExpansion_HandlesMultipleAbbreviations()
    {
        // Arrange
        var options = new SearchOptions { ExpandAbbreviations = true };

        // Act
        var result = _sut.Process("UI and UX design", options);

        // Assert
        result.Should().Contain("User Interface",
            because: "UI should be expanded");
        result.Should().Contain("User Experience",
            because: "UX should be expanded");
    }

    [Fact]
    public void Process_WithExpansion_DoesNotExpandPartialMatches()
    {
        // Arrange
        var options = new SearchOptions { ExpandAbbreviations = true };

        // Act
        var result = _sut.Process("RAPID development", options);

        // Assert
        result.Should().NotContain("Application Programming Interface",
            because: "word boundary matching should prevent expanding 'API' within 'RAPID'");
    }

    [Fact]
    public void Process_WithExpansion_DoesNotDoubleExpand()
    {
        // Arrange
        var options = new SearchOptions { ExpandAbbreviations = true };
        var alreadyExpanded = "API (Application Programming Interface) docs";

        // Act
        var result = _sut.Process(alreadyExpanded, options);

        // Assert
        var occurrences = result.Split("Application Programming Interface").Length - 1;
        occurrences.Should().Be(1,
            because: "the expansion should not be applied when the full form already exists");
    }

    [Fact]
    public void Process_WithExpansion_CaseInsensitiveMatching()
    {
        // Arrange
        var options = new SearchOptions { ExpandAbbreviations = true };

        // Act
        var result = _sut.Process("api documentation", options);

        // Assert
        result.Should().Contain("Application Programming Interface",
            because: "abbreviation matching should be case-insensitive");
    }

    #endregion

    #region Cache Tests — Store and Retrieve

    [Fact]
    public void CacheEmbedding_StoresAndRetrievesEmbedding()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        _sut.CacheEmbedding("test query", embedding);
        var cached = _sut.GetCachedEmbedding("test query");

        // Assert
        cached.Should().BeEquivalentTo(embedding,
            because: "cached embedding should match the stored value");
    }

    [Fact]
    public void GetCachedEmbedding_CacheMiss_ReturnsNull()
    {
        // Act
        var cached = _sut.GetCachedEmbedding("uncached query");

        // Assert
        cached.Should().BeNull(
            because: "a query with no cached embedding should return null");
    }

    [Fact]
    public void GetCachedEmbedding_CaseInsensitive()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f };

        // Act
        _sut.CacheEmbedding("Test Query", embedding);
        var cached = _sut.GetCachedEmbedding("test query");

        // Assert
        cached.Should().BeEquivalentTo(embedding,
            because: "cache lookup should be case-insensitive via SHA256 of lowered key");
    }

    [Fact]
    public void CacheEmbedding_OverwritesPreviousEntry()
    {
        // Arrange
        var original = new float[] { 0.1f, 0.2f };
        var updated = new float[] { 0.9f, 0.8f };

        // Act
        _sut.CacheEmbedding("query", original);
        _sut.CacheEmbedding("query", updated);
        var cached = _sut.GetCachedEmbedding("query");

        // Assert
        cached.Should().BeEquivalentTo(updated,
            because: "a subsequent cache write should overwrite the previous entry");
    }

    [Fact]
    public void CacheEmbedding_DifferentQueries_StoredSeparately()
    {
        // Arrange
        var embedding1 = new float[] { 0.1f };
        var embedding2 = new float[] { 0.9f };

        // Act
        _sut.CacheEmbedding("query one", embedding1);
        _sut.CacheEmbedding("query two", embedding2);

        // Assert
        _sut.GetCachedEmbedding("query one").Should().BeEquivalentTo(embedding1,
            because: "each query should have its own cache entry");
        _sut.GetCachedEmbedding("query two").Should().BeEquivalentTo(embedding2,
            because: "each query should have its own cache entry");
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.ClearCache();

        act.Should().NotThrow(
            because: "ClearCache should be safe to call at any time");
    }

    [Fact]
    public void ClearCache_AfterCaching_DoesNotThrow()
    {
        // Arrange
        _sut.CacheEmbedding("query", new float[] { 0.1f });

        // Act & Assert
        var act = () => _sut.ClearCache();

        act.Should().NotThrow(
            because: "ClearCache should be safe to call even with cached entries");
    }

    #endregion

    #region IQueryPreprocessor Interface Tests

    [Fact]
    public void QueryPreprocessor_ImplementsIQueryPreprocessor()
    {
        // Assert
        _sut.Should().BeAssignableTo<IQueryPreprocessor>(
            because: "QueryPreprocessor must implement the IQueryPreprocessor contract");
    }

    #endregion
}
