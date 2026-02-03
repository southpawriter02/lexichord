// =============================================================================
// File: HighlightThemeTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for HighlightTheme.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

using FluentAssertions;
using Lexichord.Modules.RAG.Rendering;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Rendering;

/// <summary>
/// Unit tests for <see cref="HighlightTheme"/>.
/// </summary>
[Trait("Feature", "v0.5.6b")]
[Trait("Category", "Unit")]
public sealed class HighlightThemeTests
{
    [Fact]
    public void Light_HasValidColors()
    {
        // Act
        var theme = HighlightTheme.Light;

        // Assert
        theme.ExactMatchForeground.Should().StartWith("#");
        theme.ExactMatchBackground.Should().StartWith("#");
        theme.FuzzyMatchForeground.Should().StartWith("#");
        theme.FuzzyMatchBackground.Should().StartWith("#");
        theme.KeyPhraseForeground.Should().StartWith("#");
        theme.EllipsisColor.Should().StartWith("#");
    }

    [Fact]
    public void Dark_HasValidColors()
    {
        // Act
        var theme = HighlightTheme.Dark;

        // Assert
        theme.ExactMatchForeground.Should().StartWith("#");
        theme.ExactMatchBackground.Should().StartWith("#");
        theme.FuzzyMatchForeground.Should().StartWith("#");
        theme.FuzzyMatchBackground.Should().StartWith("#");
        theme.KeyPhraseForeground.Should().StartWith("#");
        theme.EllipsisColor.Should().StartWith("#");
    }

    [Fact]
    public void Light_ColorsDifferFromDark()
    {
        // Act
        var light = HighlightTheme.Light;
        var dark = HighlightTheme.Dark;

        // Assert
        light.ExactMatchForeground.Should().NotBe(dark.ExactMatchForeground);
        light.ExactMatchBackground.Should().NotBe(dark.ExactMatchBackground);
        light.FuzzyMatchForeground.Should().NotBe(dark.FuzzyMatchForeground);
    }

    [Fact]
    public void CustomTheme_CanBeCreated()
    {
        // Arrange & Act
        var customTheme = new HighlightTheme(
            ExactMatchForeground: "#ff0000",
            ExactMatchBackground: "#ffeeee",
            FuzzyMatchForeground: "#00ff00",
            FuzzyMatchBackground: "#eeffee",
            KeyPhraseForeground: "#0000ff",
            EllipsisColor: "#cccccc");

        // Assert
        customTheme.ExactMatchForeground.Should().Be("#ff0000");
        customTheme.FuzzyMatchBackground.Should().Be("#eeffee");
    }

    [Fact]
    public void Light_ExactMatch_HasBlueHue()
    {
        // Light theme uses blue for exact matches
        var theme = HighlightTheme.Light;

        // The foreground should contain blue component (ends in db for #1a56db)
        theme.ExactMatchForeground.Should().Be("#1a56db");
    }

    [Fact]
    public void Light_FuzzyMatch_HasPurpleHue()
    {
        // Light theme uses purple for fuzzy matches
        var theme = HighlightTheme.Light;

        // Purple color check
        theme.FuzzyMatchForeground.Should().Be("#7c3aed");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var theme1 = new HighlightTheme("#a", "#b", "#c", "#d", "#e", "#f");
        var theme2 = new HighlightTheme("#a", "#b", "#c", "#d", "#e", "#f");
        var theme3 = new HighlightTheme("#x", "#b", "#c", "#d", "#e", "#f");

        // Assert
        theme1.Should().Be(theme2);
        theme1.Should().NotBe(theme3);
    }
}
