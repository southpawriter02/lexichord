using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Tests.Unit.Modules.Editor;

/// <summary>
/// Unit tests for <see cref="EditorSettings"/> record.
/// </summary>
public class EditorSettingsTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var settings = new EditorSettings();

        // Assert
        settings.FontFamily.Should().Be("Cascadia Code");
        settings.FontSize.Should().Be(14.0);
        settings.MinFontSize.Should().Be(8.0);
        settings.MaxFontSize.Should().Be(72.0);
        settings.ZoomIncrement.Should().Be(2.0);
        settings.UseSpacesForTabs.Should().BeTrue();
        settings.TabSize.Should().Be(4);
        settings.IndentSize.Should().Be(4);
        settings.AutoIndent.Should().BeTrue();
        settings.ShowLineNumbers.Should().BeTrue();
        settings.WordWrap.Should().BeTrue();
        settings.HighlightCurrentLine.Should().BeTrue();
        settings.ShowWhitespace.Should().BeFalse();
        settings.ShowEndOfLine.Should().BeFalse();
        settings.HighlightMatchingBrackets.Should().BeTrue();
        settings.VerticalRulerPosition.Should().Be(80);
        settings.ShowVerticalRuler.Should().BeFalse();
        settings.SmoothScrolling.Should().BeTrue();
        settings.BlinkCursor.Should().BeTrue();
        settings.CursorBlinkRate.Should().Be(530);
    }

    [Fact]
    public void SectionName_IsEditor()
    {
        // Assert
        EditorSettings.SectionName.Should().Be("Editor");
    }

    [Fact]
    public void FallbackFonts_ContainsCascadiaCode()
    {
        // Assert
        EditorSettings.FallbackFonts.Should().Contain("Cascadia Code");
        EditorSettings.FallbackFonts.Should().Contain("Consolas");
        EditorSettings.FallbackFonts.Should().Contain("monospace");
    }

    #endregion

    #region Validated Tests

    [Fact]
    public void Validated_ClampsFontSizeToMax()
    {
        // Arrange
        var settings = new EditorSettings { FontSize = 100.0 };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.FontSize.Should().Be(72.0);
    }

    [Fact]
    public void Validated_ClampsFontSizeToMin()
    {
        // Arrange
        var settings = new EditorSettings { FontSize = 2.0 };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.FontSize.Should().Be(8.0);
    }

    [Fact]
    public void Validated_ClampsTabSizeToMax()
    {
        // Arrange
        var settings = new EditorSettings { TabSize = 100 };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.TabSize.Should().Be(16);
    }

    [Fact]
    public void Validated_ClampsTabSizeToMin()
    {
        // Arrange
        var settings = new EditorSettings { TabSize = 0 };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.TabSize.Should().Be(1);
    }

    [Fact]
    public void Validated_ClampsIndentSizeToMax()
    {
        // Arrange
        var settings = new EditorSettings { IndentSize = 50 };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.IndentSize.Should().Be(16);
    }

    [Fact]
    public void Validated_ClampsVerticalRulerPosition()
    {
        // Arrange
        var settings = new EditorSettings { VerticalRulerPosition = 500 };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.VerticalRulerPosition.Should().Be(200);
    }

    [Fact]
    public void Validated_ClampsCursorBlinkRateToMin()
    {
        // Arrange
        var settings = new EditorSettings { CursorBlinkRate = 10 };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.CursorBlinkRate.Should().Be(100);
    }

    [Fact]
    public void Validated_ClampsCursorBlinkRateToMax()
    {
        // Arrange
        var settings = new EditorSettings { CursorBlinkRate = 5000 };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.CursorBlinkRate.Should().Be(2000);
    }

    [Fact]
    public void Validated_PreservesValidValues()
    {
        // Arrange
        var settings = new EditorSettings 
        { 
            FontSize = 16.0,
            TabSize = 2,
            WordWrap = false 
        };

        // Act
        var validated = settings.Validated();

        // Assert
        validated.FontSize.Should().Be(16.0);
        validated.TabSize.Should().Be(2);
        validated.WordWrap.Should().BeFalse();
    }

    #endregion

    #region Record Immutability Tests

    [Fact]
    public void WithExpression_CreatesNewInstance()
    {
        // Arrange
        var original = new EditorSettings { FontSize = 14.0 };

        // Act
        var modified = original with { FontSize = 16.0 };

        // Assert
        original.FontSize.Should().Be(14.0);
        modified.FontSize.Should().Be(16.0);
    }

    #endregion
}
