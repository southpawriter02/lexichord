// -----------------------------------------------------------------------
// <copyright file="SummarizationOptionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Summarizer;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Summarizer;

/// <summary>
/// Unit tests for <see cref="SummarizationOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6a")]
public class SummarizationOptionsTests
{
    // ── Default Values ──────────────────────────────────────────────────

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SummarizationOptions();

        // Assert
        options.Mode.Should().Be(SummarizationMode.BulletPoints);
        options.MaxItems.Should().Be(5);
        options.TargetWordCount.Should().BeNull();
        options.CustomPrompt.Should().BeNull();
        options.IncludeSectionSummaries.Should().BeFalse();
        options.TargetAudience.Should().BeNull();
        options.PreserveTechnicalTerms.Should().BeTrue();
        options.MaxResponseTokens.Should().Be(2048);
    }

    // ── Validate: MaxItems ──────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void Validate_MaxItemsOutOfRange_ThrowsArgumentException(int maxItems)
    {
        // Arrange
        var options = new SummarizationOptions { MaxItems = maxItems };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxItems")
            .WithMessage("*between 1 and 10*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_MaxItemsInRange_DoesNotThrow(int maxItems)
    {
        // Arrange
        var options = new SummarizationOptions { MaxItems = maxItems };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: TargetWordCount ───────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(1001)]
    [InlineData(5000)]
    public void Validate_TargetWordCountOutOfRange_ThrowsArgumentException(int wordCount)
    {
        // Arrange
        var options = new SummarizationOptions { TargetWordCount = wordCount };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("TargetWordCount")
            .WithMessage("*between 10 and 1000*");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(200)]
    [InlineData(1000)]
    public void Validate_TargetWordCountInRange_DoesNotThrow(int wordCount)
    {
        // Arrange
        var options = new SummarizationOptions { TargetWordCount = wordCount };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_TargetWordCountNull_DoesNotThrow()
    {
        // Arrange
        var options = new SummarizationOptions { TargetWordCount = null };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: Custom Mode ───────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_CustomModeWithoutPrompt_ThrowsArgumentException(string? customPrompt)
    {
        // Arrange
        var options = new SummarizationOptions
        {
            Mode = SummarizationMode.Custom,
            CustomPrompt = customPrompt
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("CustomPrompt")
            .WithMessage("*required when Mode is Custom*");
    }

    [Fact]
    public void Validate_CustomModeWithPrompt_DoesNotThrow()
    {
        // Arrange
        var options = new SummarizationOptions
        {
            Mode = SummarizationMode.Custom,
            CustomPrompt = "Summarize focusing on security"
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(SummarizationMode.Abstract)]
    [InlineData(SummarizationMode.TLDR)]
    [InlineData(SummarizationMode.BulletPoints)]
    [InlineData(SummarizationMode.KeyTakeaways)]
    [InlineData(SummarizationMode.Executive)]
    public void Validate_NonCustomModeWithoutPrompt_DoesNotThrow(SummarizationMode mode)
    {
        // Arrange — CustomPrompt is null but Mode is not Custom
        var options = new SummarizationOptions { Mode = mode };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: Combined Valid Options ─────────────────────────────────

    [Fact]
    public void Validate_ValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new SummarizationOptions
        {
            Mode = SummarizationMode.BulletPoints,
            MaxItems = 3,
            TargetWordCount = null,
            TargetAudience = "developers",
            PreserveTechnicalTerms = true,
            MaxResponseTokens = 1024
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
