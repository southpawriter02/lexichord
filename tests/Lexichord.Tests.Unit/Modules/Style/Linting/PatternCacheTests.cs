using FluentAssertions;
using Lexichord.Modules.Style.Services.Linting;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for <see cref="PatternCache{TKey, TValue}"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.3c
/// </remarks>
public sealed class PatternCacheTests
{
    #region Basic Operations

    [Fact]
    public void TryGet_WithCachedValue_ReturnsTrue()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);
        cache.Set("key1", 42);

        // Act
        var found = cache.TryGet("key1", out var value);

        // Assert
        found.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGet_WithMissingKey_ReturnsFalse()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);

        // Act
        var found = cache.TryGet("missing", out var value);

        // Assert
        found.Should().BeFalse();
        value.Should().Be(default);
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);
        cache.Set("key1", 1);

        // Act
        cache.Set("key1", 2);
        cache.TryGet("key1", out var value);

        // Assert
        value.Should().Be(2);
    }

    [Fact]
    public void GetOrAdd_WithMissingKey_InvokesFactory()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);
        var factoryCalled = false;

        // Act
        var value = cache.GetOrAdd("key1", _ =>
        {
            factoryCalled = true;
            return 42;
        });

        // Assert
        factoryCalled.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void GetOrAdd_WithExistingKey_DoesNotInvokeFactory()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);
        cache.Set("key1", 42);
        var factoryCalled = false;

        // Act
        var value = cache.GetOrAdd("key1", _ =>
        {
            factoryCalled = true;
            return 100;
        });

        // Assert
        factoryCalled.Should().BeFalse();
        value.Should().Be(42);
    }

    #endregion

    #region LRU Eviction

    [Fact]
    public void Set_WhenAtCapacity_EvictsLeastRecentlyUsed()
    {
        // Arrange
        var cache = new PatternCache<string, int>(3);
        cache.Set("a", 1);
        Thread.Sleep(2); // Ensure distinct timestamps
        cache.Set("b", 2);
        Thread.Sleep(2); // Ensure distinct timestamps
        cache.Set("c", 3);

        // Access 'a' to make it recently used
        cache.TryGet("a", out _);
        Thread.Sleep(2); // Ensure timestamp is updated

        // Act - Add new entry, should evict 'b' (oldest untouched)
        cache.Set("d", 4);

        // Assert
        cache.TryGet("a", out _).Should().BeTrue();
        cache.TryGet("b", out _).Should().BeFalse(); // evicted
        cache.TryGet("c", out _).Should().BeTrue();
        cache.TryGet("d", out _).Should().BeTrue();
    }

    [Fact]
    public void Count_ReflectsCurrentSize()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);

        // Act
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        // Assert
        cache.Count.Should().Be(3);
    }

    [Fact]
    public void MaxSize_ReturnsConfiguredCapacity()
    {
        // Arrange
        var cache = new PatternCache<string, int>(50);

        // Assert
        cache.MaxSize.Should().Be(50);
    }

    #endregion

    #region Statistics

    [Fact]
    public void Hits_TracksSuccessfulLookups()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);
        cache.Set("key1", 1);

        // Act
        cache.TryGet("key1", out _);
        cache.TryGet("key1", out _);

        // Assert
        cache.Hits.Should().Be(2);
    }

    [Fact]
    public void Misses_TracksFailedLookups()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);

        // Act
        cache.TryGet("missing1", out _);
        cache.TryGet("missing2", out _);

        // Assert
        cache.Misses.Should().Be(2);
    }

    [Fact]
    public void Clear_ResetsEverything()
    {
        // Arrange
        var cache = new PatternCache<string, int>(10);
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.TryGet("a", out _);
        cache.TryGet("missing", out _);

        // Act
        cache.Clear();

        // Assert
        cache.Count.Should().Be(0);
        cache.Hits.Should().Be(0);
        cache.Misses.Should().Be(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithZeroSize_ThrowsArgumentException()
    {
        // Act
        var action = () => new PatternCache<string, int>(0);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithNegativeSize_ThrowsArgumentException()
    {
        // Act
        var action = () => new PatternCache<string, int>(-1);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion
}
