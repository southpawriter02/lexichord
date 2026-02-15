// -----------------------------------------------------------------------
// <copyright file="SimplificationRequestTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Simplifier;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="SimplificationRequest"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4b")]
public class SimplificationRequestTests
{
    // ── Validation Tests ────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyText_ThrowsArgumentException()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "",
            Target = target
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Original text*required*empty*")
            .WithParameterName("OriginalText");
    }

    [Fact]
    public void Validate_WhitespaceText_ThrowsArgumentException()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "   \t\n  ",
            Target = target
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Original text*required*")
            .WithParameterName("OriginalText");
    }

    [Fact]
    public void Validate_TextExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var target = CreateValidTarget();
        var longText = new string('a', SimplificationRequest.MaxTextLength + 1);
        var request = new SimplificationRequest
        {
            OriginalText = longText,
            Target = target
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum length*")
            .WithParameterName("OriginalText");
    }

    [Fact]
    public void Validate_NullTarget_ThrowsArgumentException()
    {
        // Arrange
        var request = new SimplificationRequest
        {
            OriginalText = "Some valid text",
            Target = null!
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*target*required*")
            .WithParameterName("Target");
    }

    [Fact]
    public void Validate_InvalidStrategy_ThrowsArgumentException()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Some valid text",
            Target = target,
            Strategy = (SimplificationStrategy)999
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid simplification strategy*")
            .WithParameterName("Strategy");
    }

    [Fact]
    public void Validate_ZeroTimeout_ThrowsArgumentException()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Some valid text",
            Target = target,
            Timeout = TimeSpan.Zero
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Timeout must be positive*")
            .WithParameterName("Timeout");
    }

    [Fact]
    public void Validate_NegativeTimeout_ThrowsArgumentException()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Some valid text",
            Target = target,
            Timeout = TimeSpan.FromSeconds(-10)
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Timeout must be positive*")
            .WithParameterName("Timeout");
    }

    [Fact]
    public void Validate_TimeoutExceedsMaximum_ThrowsArgumentException()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Some valid text",
            Target = target,
            Timeout = SimplificationRequest.MaxTimeout + TimeSpan.FromSeconds(1)
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum*")
            .WithParameterName("Timeout");
    }

    [Fact]
    public void Validate_ValidRequest_NoException()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Some valid text that needs simplification.",
            Target = target
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Default Values Tests ────────────────────────────────────────────

    [Fact]
    public void DefaultValues_StrategyIsBalanced()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Text",
            Target = target
        };

        // Assert
        request.Strategy.Should().Be(SimplificationStrategy.Balanced);
    }

    [Fact]
    public void DefaultValues_GenerateGlossaryIsFalse()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Text",
            Target = target
        };

        // Assert
        request.GenerateGlossary.Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_PreserveFormattingIsTrue()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Text",
            Target = target
        };

        // Assert
        request.PreserveFormatting.Should().BeTrue();
    }

    [Fact]
    public void DefaultValues_TimeoutIs60Seconds()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Text",
            Target = target
        };

        // Assert
        request.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void DefaultValues_DocumentPathIsNull()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Text",
            Target = target
        };

        // Assert
        request.DocumentPath.Should().BeNull();
    }

    [Fact]
    public void DefaultValues_AdditionalInstructionsIsNull()
    {
        // Arrange
        var target = CreateValidTarget();
        var request = new SimplificationRequest
        {
            OriginalText = "Text",
            Target = target
        };

        // Assert
        request.AdditionalInstructions.Should().BeNull();
    }

    // ── Constants Tests ────────────────────────────────────────────────

    [Fact]
    public void MaxTextLength_Is50000()
    {
        SimplificationRequest.MaxTextLength.Should().Be(50_000);
    }

    [Fact]
    public void MaxTimeout_Is5Minutes()
    {
        SimplificationRequest.MaxTimeout.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void DefaultTimeout_Is60Seconds()
    {
        SimplificationRequest.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    // ── Helper Methods ────────────────────────────────────────────────

    private static ReadabilityTarget CreateValidTarget() =>
        ReadabilityTarget.FromExplicit(
            targetGradeLevel: 8.0,
            maxSentenceLength: 20,
            avoidJargon: true);
}
