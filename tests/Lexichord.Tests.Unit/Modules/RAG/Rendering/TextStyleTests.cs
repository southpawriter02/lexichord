// =============================================================================
// File: TextStyleTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for TextStyle.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

using FluentAssertions;
using Lexichord.Modules.RAG.Rendering;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Rendering;

/// <summary>
/// Unit tests for <see cref="TextStyle"/>.
/// </summary>
[Trait("Feature", "v0.5.6b")]
[Trait("Category", "Unit")]
public sealed class TextStyleTests
{
    [Fact]
    public void Default_HasNoStyling()
    {
        // Act
        var style = TextStyle.Default;

        // Assert
        style.IsBold.Should().BeFalse();
        style.IsItalic.Should().BeFalse();
        style.ForegroundColor.Should().BeNull();
        style.BackgroundColor.Should().BeNull();
    }

    [Fact]
    public void ExactMatch_IsBold()
    {
        // Act
        var style = TextStyle.ExactMatch;

        // Assert
        style.IsBold.Should().BeTrue();
        style.IsItalic.Should().BeFalse();
    }

    [Fact]
    public void FuzzyMatch_IsItalic()
    {
        // Act
        var style = TextStyle.FuzzyMatch;

        // Assert
        style.IsBold.Should().BeFalse();
        style.IsItalic.Should().BeTrue();
    }

    [Fact]
    public void CustomStyle_CanSetAllProperties()
    {
        // Arrange & Act
        var style = new TextStyle(
            IsBold: true,
            IsItalic: true,
            ForegroundColor: "#ff0000",
            BackgroundColor: "#00ff00");

        // Assert
        style.IsBold.Should().BeTrue();
        style.IsItalic.Should().BeTrue();
        style.ForegroundColor.Should().Be("#ff0000");
        style.BackgroundColor.Should().Be("#00ff00");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var style1 = new TextStyle(true, false, "#aaa", "#bbb");
        var style2 = new TextStyle(true, false, "#aaa", "#bbb");
        var style3 = new TextStyle(false, true, "#aaa", "#bbb");

        // Assert
        style1.Should().Be(style2);
        style1.Should().NotBe(style3);
    }

    [Fact]
    public void DefaultValues_WorkCorrectly()
    {
        // Using default parameter values
        var style = new TextStyle();

        style.IsBold.Should().BeFalse();
        style.IsItalic.Should().BeFalse();
        style.ForegroundColor.Should().BeNull();
        style.BackgroundColor.Should().BeNull();
    }

    [Fact]
    public void With_CanModifyProperties()
    {
        // Arrange
        var original = TextStyle.Default;

        // Act
        var modified = original with { IsBold = true };

        // Assert
        original.IsBold.Should().BeFalse();
        modified.IsBold.Should().BeTrue();
    }
}
