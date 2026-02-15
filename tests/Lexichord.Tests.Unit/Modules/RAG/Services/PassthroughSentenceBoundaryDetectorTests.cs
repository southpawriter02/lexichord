// =============================================================================
// File: PassthroughSentenceBoundaryDetectorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for PassthroughSentenceBoundaryDetector.
// =============================================================================

using System;
using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="PassthroughSentenceBoundaryDetector"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6a")]
public class PassthroughSentenceBoundaryDetectorTests
{
    private readonly PassthroughSentenceBoundaryDetector _sut;

    public PassthroughSentenceBoundaryDetectorTests()
    {
        _sut = new PassthroughSentenceBoundaryDetector(
            NullLogger<PassthroughSentenceBoundaryDetector>.Instance);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new PassthroughSentenceBoundaryDetector(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region FindSentenceStart Tests

    [Theory]
    [InlineData("Some text", 0, 0)]
    [InlineData("Some text", 5, 5)]
    [InlineData("Some text", 100, 100)] // Beyond length is allowed by Math.Max logic
    public void FindSentenceStart_ReturnsInputPosition(string content, int position, int expected)
    {
        var result = _sut.FindSentenceStart(content, position);
        result.Should().Be(expected);
    }

    [Fact]
    public void FindSentenceStart_ClampsNegativePosition()
    {
        var result = _sut.FindSentenceStart("content", -5);
        result.Should().Be(0);
    }

    #endregion

    #region FindSentenceEnd Tests

    [Theory]
    [InlineData("Some text", 0, 0)]
    [InlineData("Some text", 5, 5)]
    public void FindSentenceEnd_ReturnsInputPosition(string content, int position, int expected)
    {
        var result = _sut.FindSentenceEnd(content, position);
        result.Should().Be(expected);
    }

    [Fact]
    public void FindSentenceEnd_ClampsToLength()
    {
        var content = "12345";
        var result = _sut.FindSentenceEnd(content, 10);
        result.Should().Be(5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FindSentenceEnd_ReturnsZeroForEmpty(string? content)
    {
        var result = _sut.FindSentenceEnd(content!, 5);
        result.Should().Be(0);
    }

    #endregion

    #region GetBoundaries Tests

    [Fact]
    public void GetBoundaries_ReturnsSingleBoundary()
    {
        var content = "First sentence. Second sentence.";
        var result = _sut.GetBoundaries(content);

        result.Should().ContainSingle()
            .Which.Should().Be(new SentenceBoundary(0, content.Length));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetBoundaries_ReturnsEmptyForEmptyString(string? content)
    {
        var result = _sut.GetBoundaries(content!);
        result.Should().BeEmpty();
    }

    #endregion

    #region Word Boundary Tests

    [Theory]
    [InlineData("word", 0, 0)]
    [InlineData("word", 2, 2)]
    public void FindWordStart_ReturnsInputPosition(string text, int position, int expected)
    {
        var result = _sut.FindWordStart(text, position);
        result.Should().Be(expected);
    }

    [Fact]
    public void FindWordStart_ClampsNegativePosition()
    {
        var result = _sut.FindWordStart("text", -5);
        result.Should().Be(0);
    }

    [Theory]
    [InlineData("word", 0, 0)]
    [InlineData("word", 2, 2)]
    public void FindWordEnd_ReturnsInputPosition(string text, int position, int expected)
    {
        var result = _sut.FindWordEnd(text, position);
        result.Should().Be(expected);
    }

    [Fact]
    public void FindWordEnd_ClampsToLength()
    {
        var text = "123";
        var result = _sut.FindWordEnd(text, 10);
        result.Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FindWordEnd_ReturnsZeroForEmpty(string? text)
    {
        var result = _sut.FindWordEnd(text!, 5);
        result.Should().Be(0);
    }

    #endregion
}
