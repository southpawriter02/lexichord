// =============================================================================
// File: ContextOptionsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ContextOptions record and validation.
// =============================================================================
// VERSION: v0.5.3a (Context Expansion Service)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Models;

/// <summary>
/// Unit tests for the <see cref="ContextOptions"/> record.
/// </summary>
public sealed class ContextOptionsTests
{
    #region Default Values

    [Fact]
    public void Constructor_WithDefaults_UsesExpectedValues()
    {
        // Act
        var options = new ContextOptions();

        // Assert
        Assert.Equal(1, options.PrecedingChunks);
        Assert.Equal(1, options.FollowingChunks);
        Assert.True(options.IncludeHeadings);
    }

    #endregion

    #region Validated() Method

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    public void Validated_WithValidPreceding_ReturnsSameValue(int input, int expected)
    {
        // Arrange
        var options = new ContextOptions(PrecedingChunks: input);

        // Act
        var validated = options.Validated();

        // Assert
        Assert.Equal(expected, validated.PrecedingChunks);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(-10, 0)]
    [InlineData(int.MinValue, 0)]
    public void Validated_WithNegativePreceding_ClampsToZero(int input, int expected)
    {
        // Arrange
        var options = new ContextOptions(PrecedingChunks: input);

        // Act
        var validated = options.Validated();

        // Assert
        Assert.Equal(expected, validated.PrecedingChunks);
    }

    [Theory]
    [InlineData(6, 5)]
    [InlineData(10, 5)]
    [InlineData(100, 5)]
    [InlineData(int.MaxValue, 5)]
    public void Validated_WithExcessivePreceding_ClampsToMax(int input, int expected)
    {
        // Arrange
        var options = new ContextOptions(PrecedingChunks: input);

        // Act
        var validated = options.Validated();

        // Assert
        Assert.Equal(expected, validated.PrecedingChunks);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    public void Validated_WithValidFollowing_ReturnsSameValue(int input, int expected)
    {
        // Arrange
        var options = new ContextOptions(FollowingChunks: input);

        // Act
        var validated = options.Validated();

        // Assert
        Assert.Equal(expected, validated.FollowingChunks);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(6, 5)]
    [InlineData(int.MaxValue, 5)]
    public void Validated_WithInvalidFollowing_ClampsToRange(int input, int expected)
    {
        // Arrange
        var options = new ContextOptions(FollowingChunks: input);

        // Act
        var validated = options.Validated();

        // Assert
        Assert.Equal(expected, validated.FollowingChunks);
    }

    [Fact]
    public void Validated_PreservesIncludeHeadings_WhenTrue()
    {
        // Arrange
        var options = new ContextOptions(IncludeHeadings: true);

        // Act
        var validated = options.Validated();

        // Assert
        Assert.True(validated.IncludeHeadings);
    }

    [Fact]
    public void Validated_PreservesIncludeHeadings_WhenFalse()
    {
        // Arrange
        var options = new ContextOptions(IncludeHeadings: false);

        // Act
        var validated = options.Validated();

        // Assert
        Assert.False(validated.IncludeHeadings);
    }

    #endregion

    #region MaxChunkWindow Constant

    [Fact]
    public void MaxChunkWindow_Equals5()
    {
        // Assert
        Assert.Equal(5, ContextOptions.MaxChunkWindow);
    }

    #endregion

    #region Record Equality

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var a = new ContextOptions(2, 3, true);
        var b = new ContextOptions(2, 3, true);

        // Assert
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var a = new ContextOptions(2, 3, true);
        var b = new ContextOptions(2, 3, false);

        // Assert
        Assert.NotEqual(a, b);
    }

    #endregion
}
