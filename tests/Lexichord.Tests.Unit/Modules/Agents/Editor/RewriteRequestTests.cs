// -----------------------------------------------------------------------
// <copyright file="RewriteRequestTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Editor;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor;

/// <summary>
/// Unit tests for <see cref="RewriteRequest"/> validation and computed properties.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3b")]
public class RewriteRequestTests
{
    // ── Validation Tests ────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyText_ThrowsArgumentException()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "",
            SelectionSpan = new TextSpan(0, 0),
            Intent = RewriteIntent.Formal
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("SelectedText");
    }

    [Fact]
    public void Validate_WhitespaceText_ThrowsArgumentException()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "   \t\n  ",
            SelectionSpan = new TextSpan(0, 7),
            Intent = RewriteIntent.Formal
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("SelectedText");
    }

    [Fact]
    public void Validate_ExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var longText = new string('A', RewriteRequest.MaxSelectedTextLength + 1);
        var request = new RewriteRequest
        {
            SelectedText = longText,
            SelectionSpan = new TextSpan(0, longText.Length),
            Intent = RewriteIntent.Formal
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("SelectedText")
            .WithMessage("*50,000*");
    }

    [Fact]
    public void Validate_CustomWithoutInstruction_ThrowsArgumentException()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "Hello world",
            SelectionSpan = new TextSpan(0, 11),
            Intent = RewriteIntent.Custom,
            CustomInstruction = null
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("CustomInstruction");
    }

    [Fact]
    public void Validate_CustomWithEmptyInstruction_ThrowsArgumentException()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "Hello world",
            SelectionSpan = new TextSpan(0, 11),
            Intent = RewriteIntent.Custom,
            CustomInstruction = "   "
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("CustomInstruction");
    }

    [Fact]
    public void Validate_ValidFormalRequest_NoException()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "Hello world",
            SelectionSpan = new TextSpan(0, 11),
            Intent = RewriteIntent.Formal
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ValidCustomRequest_NoException()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "Hello world",
            SelectionSpan = new TextSpan(0, 11),
            Intent = RewriteIntent.Custom,
            CustomInstruction = "Make it sound like Shakespeare"
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ExactMaxLength_NoException()
    {
        // Arrange
        var maxText = new string('A', RewriteRequest.MaxSelectedTextLength);
        var request = new RewriteRequest
        {
            SelectedText = maxText,
            SelectionSpan = new TextSpan(0, maxText.Length),
            Intent = RewriteIntent.Formal
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── EstimatedTokens Tests ───────────────────────────────────────────

    [Fact]
    public void EstimatedTokens_CalculatesCorrectly()
    {
        // Arrange — 100 chars / 4 = 25 tokens
        var request = new RewriteRequest
        {
            SelectedText = new string('A', 100),
            SelectionSpan = new TextSpan(0, 100),
            Intent = RewriteIntent.Formal
        };

        // Act & Assert
        request.EstimatedTokens.Should().Be(25);
    }

    [Fact]
    public void EstimatedTokens_IncludesCustomInstruction()
    {
        // Arrange — 100 chars / 4 + 40 chars / 4 = 25 + 10 = 35 tokens
        var request = new RewriteRequest
        {
            SelectedText = new string('A', 100),
            SelectionSpan = new TextSpan(0, 100),
            Intent = RewriteIntent.Custom,
            CustomInstruction = new string('B', 40)
        };

        // Act & Assert
        request.EstimatedTokens.Should().Be(35);
    }

    [Fact]
    public void EstimatedTokens_NullCustomInstruction_ReturnsTextOnly()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = new string('A', 80),
            SelectionSpan = new TextSpan(0, 80),
            Intent = RewriteIntent.Formal,
            CustomInstruction = null
        };

        // Act & Assert
        request.EstimatedTokens.Should().Be(20);
    }

    // ── Default Timeout Tests ───────────────────────────────────────────

    [Fact]
    public void Timeout_DefaultsTo30Seconds()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "Test",
            SelectionSpan = new TextSpan(0, 4),
            Intent = RewriteIntent.Formal
        };

        // Act & Assert
        request.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }
}
