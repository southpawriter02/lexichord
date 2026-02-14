// -----------------------------------------------------------------------
// <copyright file="ContextFragmentTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents.Context;

/// <summary>
/// Unit tests for <see cref="ContextFragment"/> record.
/// </summary>
/// <remarks>
/// Tests cover factory methods, computed properties, truncation logic,
/// and record equality behavior. Introduced in v0.7.2a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2a")]
public class ContextFragmentTests
{
    #region Factory Methods

    [Fact]
    public void Empty_CreatesFragmentWithNoContent()
    {
        // Arrange & Act
        var fragment = ContextFragment.Empty("test-source", "Test Label");

        // Assert
        fragment.SourceId.Should().Be("test-source");
        fragment.Label.Should().Be("Test Label");
        fragment.Content.Should().BeEmpty();
        fragment.TokenEstimate.Should().Be(0);
        fragment.Relevance.Should().Be(0f);
    }

    [Fact]
    public void Empty_ThrowsWhenSourceIdNull()
    {
        // Act
        var act = () => ContextFragment.Empty(null!, "Test");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sourceId");
    }

    [Fact]
    public void Empty_ThrowsWhenLabelNull()
    {
        // Act
        var act = () => ContextFragment.Empty("test", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("label");
    }

    #endregion

    #region Computed Properties

    [Theory]
    [InlineData("Some content", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void HasContent_ReflectsContentPresence(string? content, bool expected)
    {
        // Arrange
        var fragment = new ContextFragment("test", "Test", content ?? "", 10, 1.0f);

        // Act & Assert
        fragment.HasContent.Should().Be(expected);
    }

    #endregion

    #region TruncateTo Method

    [Fact]
    public void TruncateTo_ReturnsUnchangedWhenWithinLimit()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        var fragment = CreateTestFragment("Short content", 10);

        // Act
        var result = fragment.TruncateTo(50, tokenCounter);

        // Assert
        result.Should().BeSameAs(fragment); // Same instance
        tokenCounter.DidNotReceive().CountTokens(Arg.Any<string>());
    }

    [Fact]
    public void TruncateTo_TruncatesContentWhenOverLimit()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();

        // LOGIC: Mock the token counter to simulate paragraph-aware truncation
        tokenCounter.CountTokens("Paragraph 1").Returns(8);
        tokenCounter.CountTokens("Paragraph 1\n\nParagraph 2").Returns(15);
        tokenCounter.CountTokens("Paragraph 1\n\nParagraph 2\n\nParagraph 3").Returns(22);

        var fragment = CreateTestFragment("Paragraph 1\n\nParagraph 2\n\nParagraph 3", 22);

        // Act
        var result = fragment.TruncateTo(10, tokenCounter);

        // Assert
        result.Content.Should().Be("Paragraph 1");
        result.TokenEstimate.Should().Be(8);
        result.SourceId.Should().Be(fragment.SourceId);
        result.Label.Should().Be(fragment.Label);
        result.Relevance.Should().Be(fragment.Relevance);
    }

    [Fact]
    public void TruncateTo_PreservesParagraphBoundaries()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();

        // LOGIC: First paragraph fits, second doesn't
        tokenCounter.CountTokens("Para 1 content here").Returns(8);
        tokenCounter.CountTokens("Para 1 content here\n\nPara 2 content here").Returns(15);

        var fragment = CreateTestFragment("Para 1 content here\n\nPara 2 content here", 15);

        // Act
        var result = fragment.TruncateTo(10, tokenCounter);

        // Assert
        result.Content.Should().Be("Para 1 content here");
        result.Content.Should().NotContain("Para 2");
    }

    [Fact]
    public void TruncateTo_ThrowsWhenTokenCounterNull()
    {
        // Arrange
        var fragment = CreateTestFragment("Content", 10);

        // Act
        var act = () => fragment.TruncateTo(100, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tokenCounter");
    }

    #endregion

    #region Record Equality

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var fragment1 = new ContextFragment("test", "Label", "Content", 10, 0.8f);
        var fragment2 = new ContextFragment("test", "Label", "Content", 10, 0.8f);

        // Assert
        fragment1.Should().Be(fragment2);
    }

    [Fact]
    public void WithExpression_CreatesNewInstanceWithChangedProperty()
    {
        // Arrange
        var original = CreateTestFragment("Original", 10);

        // Act
        var modified = original with { Content = "Modified" };

        // Assert
        modified.Content.Should().Be("Modified");
        modified.SourceId.Should().Be(original.SourceId);
        original.Content.Should().Be("Original"); // Unchanged
    }

    #endregion

    #region Helper Methods

    private static ContextFragment CreateTestFragment(string content, int tokens)
        => new("test-source", "Test Label", content, tokens, 1.0f);

    #endregion
}
