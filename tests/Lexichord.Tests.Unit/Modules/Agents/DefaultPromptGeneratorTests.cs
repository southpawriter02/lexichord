// -----------------------------------------------------------------------
// <copyright file="DefaultPromptGeneratorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Services;

namespace Lexichord.Tests.Unit.Modules.Agents;

/// <summary>
/// Unit tests for <see cref="DefaultPromptGenerator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Validates prompt generation logic across selection characteristics:
/// </para>
/// <list type="bullet">
///   <item><description>Short selections (&lt;50 chars) → "Explain this:"</description></item>
///   <item><description>Code-like selections → "Review this code:"</description></item>
///   <item><description>Long selections (&gt;500 chars) → "Summarize this:"</description></item>
///   <item><description>Medium prose → "Improve this:"</description></item>
/// </list>
/// <para>
/// <b>Spec reference:</b> LCS-DES-v0.6.7a §9.2
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.6.7a")]
public class DefaultPromptGeneratorTests
{
    private readonly DefaultPromptGenerator _sut = new();

    /// <summary>
    /// Verifies short selections produce "Explain this:" prompt.
    /// </summary>
    [Theory]
    [InlineData("hi", "Explain this:")]
    [InlineData("test", "Explain this:")]
    public void Generate_WithShortSelection_ReturnsExplainPrompt(string selection, string expected)
    {
        // Act
        var result = _sut.Generate(selection);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies code-like selections produce "Review this code:" prompt.
    /// </summary>
    [Theory]
    [InlineData("public class Foo { }", "Review this code:")]
    [InlineData("function test() { return 1; }", "Review this code:")]
    [InlineData("{ \"key\": \"value\" }", "Review this code:")]
    [InlineData("import React from 'react';", "Review this code:")]
    public void Generate_WithCodeLikeSelection_ReturnsReviewPrompt(string selection, string expected)
    {
        // Act
        var result = _sut.Generate(selection);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies long selections produce "Summarize this:" prompt.
    /// </summary>
    [Fact]
    public void Generate_WithLongSelection_ReturnsSummarizePrompt()
    {
        // Arrange
        var selection = new string('x', 501);

        // Act
        var result = _sut.Generate(selection);

        // Assert
        result.Should().Be("Summarize this:");
    }

    /// <summary>
    /// Verifies medium prose selections produce "Improve this:" prompt.
    /// </summary>
    [Fact]
    public void Generate_WithMediumProseSelection_ReturnsImprovePrompt()
    {
        // Arrange
        var selection = "The quick brown fox jumps over the lazy dog. " +
                        "This is a somewhat longer selection that should " +
                        "trigger the default improve prompt.";

        // Act
        var result = _sut.Generate(selection);

        // Assert
        result.Should().Be("Improve this:");
    }
}
