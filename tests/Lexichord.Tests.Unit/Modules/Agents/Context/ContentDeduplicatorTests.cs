// -----------------------------------------------------------------------
// <copyright file="ContentDeduplicatorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Agents.Context;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context;

/// <summary>
/// Unit tests for <see cref="ContentDeduplicator"/>.
/// </summary>
/// <remarks>
/// Tests verify Jaccard word-set similarity calculations including edge cases,
/// punctuation handling, and short word filtering.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2c")]
public class ContentDeduplicatorTests
{
    #region CalculateJaccardSimilarity Tests

    /// <summary>
    /// Verifies that identical texts produce a similarity of 1.0.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_IdenticalTexts_ReturnsOne()
    {
        // Arrange
        var text = "The quick brown fox jumps over the lazy dog";

        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(text, text);

        // Assert
        result.Should().Be(1.0f);
    }

    /// <summary>
    /// Verifies that completely different texts produce zero similarity.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_CompletelyDifferentTexts_ReturnsZero()
    {
        // Arrange
        var textA = "alpha beta gamma delta";
        var textB = "epsilon zeta theta iota";

        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(textA, textB);

        // Assert
        result.Should().Be(0.0f);
    }

    /// <summary>
    /// Verifies partial overlap returns correct similarity.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_PartialOverlap_ReturnsExpectedScore()
    {
        // Arrange — "world" is shared; "hello" and "goodbye" differ
        // After filtering (≤2 chars removed), sets are:
        //   A: { "hello", "world" }
        //   B: { "goodbye", "world" }
        // intersection = 1, union = 3, Jaccard = 1/3 ≈ 0.333
        var textA = "hello world";
        var textB = "goodbye world";

        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(textA, textB);

        // Assert
        result.Should().BeApproximately(1f / 3f, 0.01f);
    }

    /// <summary>
    /// Verifies that empty string returns 0.
    /// </summary>
    [Theory]
    [InlineData("", "hello world")]
    [InlineData("hello world", "")]
    [InlineData("", "")]
    public void CalculateJaccardSimilarity_EmptyInput_ReturnsZero(string textA, string textB)
    {
        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(textA, textB);

        // Assert
        result.Should().Be(0.0f);
    }

    /// <summary>
    /// Verifies that null input returns 0.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_NullInputA_ReturnsZero()
    {
        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(null!, "hello world");

        // Assert
        result.Should().Be(0.0f);
    }

    /// <summary>
    /// Verifies that null input returns 0.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_NullInputB_ReturnsZero()
    {
        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity("hello world", null!);

        // Assert
        result.Should().Be(0.0f);
    }

    /// <summary>
    /// Verifies that punctuation is stripped before comparison.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_WithPunctuation_StripsBeforeComparing()
    {
        // Arrange — same words, different punctuation
        var textA = "hello, world! this (is) great.";
        var textB = "hello world this is great";

        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(textA, textB);

        // Assert — should be identical after stripping
        result.Should().Be(1.0f);
    }

    /// <summary>
    /// Verifies that case is ignored during comparison.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_DifferentCase_TreatsAsEqual()
    {
        // Arrange
        var textA = "Hello World Great Example";
        var textB = "hello world great example";

        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(textA, textB);

        // Assert
        result.Should().Be(1.0f);
    }

    /// <summary>
    /// Verifies that short words (≤2 characters) are filtered out.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_ShortWordsFiltered_DoesNotInflateSimilarity()
    {
        // Arrange — "is", "a", "of" are short words that get filtered
        // After filtering: A = { "cat" }, B = { "dog" }
        // No overlap → 0.0
        var textA = "a cat is a cat";
        var textB = "a dog is a dog";

        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(textA, textB);

        // Assert
        result.Should().Be(0.0f);
    }

    /// <summary>
    /// Verifies that whitespace-only text returns 0.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_WhitespaceOnly_ReturnsZero()
    {
        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity("   ", "hello world");

        // Assert
        result.Should().Be(0.0f);
    }

    /// <summary>
    /// Verifies near-duplicate detection at the 0.85 threshold boundary.
    /// </summary>
    [Fact]
    public void CalculateJaccardSimilarity_NearDuplicate_ExceedsThreshold()
    {
        // Arrange — 7 words match, 1 word differs
        // After filtering: A = { "quick", "brown", "fox", "jumps", "over", "lazy", "dog" }
        //                  B = { "quick", "brown", "fox", "leaps", "over", "lazy", "dog" }
        // intersection = 6, union = 8, Jaccard = 6/8 = 0.75
        var textA = "The quick brown fox jumps over the lazy dog";
        var textB = "The quick brown fox leaps over the lazy dog";

        // Act
        var result = ContentDeduplicator.CalculateJaccardSimilarity(textA, textB);

        // Assert
        result.Should().BeGreaterThan(0.7f);
    }

    #endregion
}
