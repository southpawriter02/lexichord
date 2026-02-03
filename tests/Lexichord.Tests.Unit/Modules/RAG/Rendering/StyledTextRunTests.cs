// =============================================================================
// File: StyledTextRunTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for StyledTextRun.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

using FluentAssertions;
using Lexichord.Modules.RAG.Rendering;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Rendering;

/// <summary>
/// Unit tests for <see cref="StyledTextRun"/>.
/// </summary>
[Trait("Feature", "v0.5.6b")]
[Trait("Category", "Unit")]
public sealed class StyledTextRunTests
{
    [Fact]
    public void HasContent_NonEmptyText_ReturnsTrue()
    {
        // Arrange
        var run = new StyledTextRun("Hello", TextStyle.Default);

        // Assert
        run.HasContent.Should().BeTrue();
    }

    [Fact]
    public void HasContent_EmptyText_ReturnsFalse()
    {
        // Arrange
        var run = new StyledTextRun("", TextStyle.Default);

        // Assert
        run.HasContent.Should().BeFalse();
    }

    [Fact]
    public void HasContent_NullText_ReturnsFalse()
    {
        // Arrange
        var run = new StyledTextRun(null!, TextStyle.Default);

        // Assert
        run.HasContent.Should().BeFalse();
    }

    [Fact]
    public void Length_ReturnsTextLength()
    {
        // Arrange
        var run = new StyledTextRun("Hello World", TextStyle.Default);

        // Assert
        run.Length.Should().Be(11);
    }

    [Fact]
    public void Length_EmptyText_ReturnsZero()
    {
        // Arrange
        var run = new StyledTextRun("", TextStyle.Default);

        // Assert
        run.Length.Should().Be(0);
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var style = TextStyle.Default;
        var run1 = new StyledTextRun("Test", style);
        var run2 = new StyledTextRun("Test", style);
        var run3 = new StyledTextRun("Different", style);

        // Assert
        run1.Should().Be(run2);
        run1.Should().NotBe(run3);
    }

    [Fact]
    public void Constructor_PreservesTextAndStyle()
    {
        // Arrange
        var style = TextStyle.ExactMatch;

        // Act
        var run = new StyledTextRun("Highlighted", style);

        // Assert
        run.Text.Should().Be("Highlighted");
        run.Style.Should().Be(style);
    }
}
