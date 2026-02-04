// -----------------------------------------------------------------------
// <copyright file="TokenizerCacheTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.TokenCounting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.TokenCounting;

/// <summary>
/// Unit tests for <see cref="TokenizerCache"/>.
/// </summary>
public class TokenizerCacheTests
{
    private readonly ILogger<TokenizerCache> _logger;
    private readonly TokenizerCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizerCacheTests"/> class.
    /// </summary>
    public TokenizerCacheTests()
    {
        _logger = Substitute.For<ILogger<TokenizerCache>>();
        _cache = new TokenizerCache(_logger);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor with null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new TokenizerCache(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that cache starts empty.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateEmptyCache()
    {
        // Act & Assert
        _cache.Count.Should().Be(0);
    }

    #endregion

    #region GetOrCreate Tests

    /// <summary>
    /// Tests that GetOrCreate creates new tokenizer on first call.
    /// </summary>
    [Fact]
    public void GetOrCreate_FirstCall_ShouldCreateNewTokenizer()
    {
        // Arrange
        var factoryCallCount = 0;
        ITokenizer Factory()
        {
            factoryCallCount++;
            return new ApproximateTokenizer(modelFamily: "test");
        }

        // Act
        var tokenizer = _cache.GetOrCreate("test-model", Factory);

        // Assert
        tokenizer.Should().NotBeNull();
        factoryCallCount.Should().Be(1);
        _cache.Count.Should().Be(1);
    }

    /// <summary>
    /// Tests that GetOrCreate returns cached tokenizer on subsequent calls.
    /// </summary>
    [Fact]
    public void GetOrCreate_SubsequentCalls_ShouldReturnCachedTokenizer()
    {
        // Arrange
        var factoryCallCount = 0;
        ITokenizer Factory()
        {
            factoryCallCount++;
            return new ApproximateTokenizer(modelFamily: "test");
        }

        // Act
        var tokenizer1 = _cache.GetOrCreate("test-model", Factory);
        var tokenizer2 = _cache.GetOrCreate("test-model", Factory);

        // Assert
        tokenizer1.Should().BeSameAs(tokenizer2);
        factoryCallCount.Should().Be(1);
        _cache.Count.Should().Be(1);
    }

    /// <summary>
    /// Tests that different models get different tokenizers.
    /// </summary>
    [Fact]
    public void GetOrCreate_DifferentModels_ShouldCreateDifferentTokenizers()
    {
        // Arrange
        ITokenizer Factory1() => new ApproximateTokenizer(modelFamily: "model1");
        ITokenizer Factory2() => new ApproximateTokenizer(modelFamily: "model2");

        // Act
        var tokenizer1 = _cache.GetOrCreate("claude-3-opus", Factory1);
        var tokenizer2 = _cache.GetOrCreate("gpt-4o", Factory2);

        // Assert
        tokenizer1.Should().NotBeSameAs(tokenizer2);
        _cache.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that model family normalization groups models correctly.
    /// </summary>
    [Fact]
    public void GetOrCreate_SameModelFamily_ShouldShareTokenizer()
    {
        // Arrange
        var factoryCallCount = 0;
        ITokenizer Factory()
        {
            factoryCallCount++;
            return new ApproximateTokenizer(modelFamily: "gpt-4o");
        }

        // Act - gpt-4o and gpt-4o-mini should share the same tokenizer
        var tokenizer1 = _cache.GetOrCreate("gpt-4o", Factory);
        var tokenizer2 = _cache.GetOrCreate("gpt-4o-mini", Factory);

        // Assert
        tokenizer1.Should().BeSameAs(tokenizer2);
        factoryCallCount.Should().Be(1);
        _cache.Count.Should().Be(1);
    }

    /// <summary>
    /// Tests that null model throws ArgumentException.
    /// </summary>
    [Fact]
    public void GetOrCreate_WithNullModel_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _cache.GetOrCreate(null!, () => new ApproximateTokenizer());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("model");
    }

    /// <summary>
    /// Tests that null factory throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetOrCreate_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _cache.GetOrCreate("model", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    #endregion

    #region Clear Tests

    /// <summary>
    /// Tests that Clear removes all cached entries.
    /// </summary>
    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        _cache.GetOrCreate("model1", () => new ApproximateTokenizer());
        _cache.GetOrCreate("model2", () => new ApproximateTokenizer());
        _cache.Count.Should().BeGreaterThan(0);

        // Act
        _cache.Clear();

        // Assert
        _cache.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Clear on empty cache does not throw.
    /// </summary>
    [Fact]
    public void Clear_OnEmptyCache_ShouldNotThrow()
    {
        // Act
        var act = () => _cache.Clear();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region ContainsKey Tests

    /// <summary>
    /// Tests that ContainsKey returns true for cached model.
    /// </summary>
    [Fact]
    public void ContainsKey_ForCachedModel_ShouldReturnTrue()
    {
        // Arrange
        _cache.GetOrCreate("gpt-4o", () => new ApproximateTokenizer(modelFamily: "gpt-4o"));

        // Act
        var contains = _cache.ContainsKey("gpt-4o");

        // Assert
        contains.Should().BeTrue();
    }

    /// <summary>
    /// Tests that ContainsKey returns false for uncached model.
    /// </summary>
    [Fact]
    public void ContainsKey_ForUncachedModel_ShouldReturnFalse()
    {
        // Act
        var contains = _cache.ContainsKey("uncached-model");

        // Assert
        contains.Should().BeFalse();
    }

    /// <summary>
    /// Tests that ContainsKey returns false for null model.
    /// </summary>
    [Fact]
    public void ContainsKey_WithNullModel_ShouldReturnFalse()
    {
        // Act
        var contains = _cache.ContainsKey(null!);

        // Assert
        contains.Should().BeFalse();
    }

    /// <summary>
    /// Tests that ContainsKey returns false for empty model.
    /// </summary>
    [Fact]
    public void ContainsKey_WithEmptyModel_ShouldReturnFalse()
    {
        // Act
        var contains = _cache.ContainsKey(string.Empty);

        // Assert
        contains.Should().BeFalse();
    }

    /// <summary>
    /// Tests that ContainsKey respects model family normalization.
    /// </summary>
    [Fact]
    public void ContainsKey_WithModelFamilyVariant_ShouldReturnTrue()
    {
        // Arrange
        _cache.GetOrCreate("gpt-4o", () => new ApproximateTokenizer(modelFamily: "gpt-4o"));

        // Act - gpt-4o-mini normalizes to gpt-4o
        var contains = _cache.ContainsKey("gpt-4o-mini");

        // Assert
        contains.Should().BeTrue();
    }

    #endregion

    #region Count Tests

    /// <summary>
    /// Tests that Count increases as tokenizers are cached.
    /// </summary>
    [Fact]
    public void Count_ShouldReflectCachedEntries()
    {
        // Assert initial
        _cache.Count.Should().Be(0);

        // Add entries
        _cache.GetOrCreate("claude", () => new ApproximateTokenizer(modelFamily: "claude"));
        _cache.Count.Should().Be(1);

        _cache.GetOrCreate("gpt-4o", () => new ApproximateTokenizer(modelFamily: "gpt-4o"));
        _cache.Count.Should().Be(2);

        // Same family should not increase count
        _cache.GetOrCreate("gpt-4o-mini", () => new ApproximateTokenizer(modelFamily: "gpt-4o"));
        _cache.Count.Should().Be(2);
    }

    #endregion
}
