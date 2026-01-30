using FluentAssertions;
using Lexichord.Modules.Style.Services.Linting;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for <see cref="PositionIndex"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.3d
/// </remarks>
public sealed class PositionIndexTests
{
    #region Basic Position Calculation

    [Fact]
    public void GetPosition_FirstCharacter_ReturnsLine1Column1()
    {
        // Arrange
        var index = new PositionIndex("Hello, world!");

        // Act
        var (line, column) = index.GetPosition(0);

        // Assert
        line.Should().Be(1);
        column.Should().Be(1);
    }

    [Fact]
    public void GetPosition_MiddleOfFirstLine_ReturnsCorrectColumn()
    {
        // Arrange
        var content = "Hello, world!";
        var index = new PositionIndex(content);

        // Act - 'w' is at index 7
        var (line, column) = index.GetPosition(7);

        // Assert
        line.Should().Be(1);
        column.Should().Be(8); // 1-indexed
    }

    [Fact]
    public void GetPosition_SecondLine_ReturnsLine2()
    {
        // Arrange
        var content = "Line one\nLine two";
        //            012345678 9...
        var index = new PositionIndex(content);

        // Act - 'L' of "Line two" is at index 9
        var (line, column) = index.GetPosition(9);

        // Assert
        line.Should().Be(2);
        column.Should().Be(1);
    }

    [Fact]
    public void GetPosition_MiddleOfSecondLine_ReturnsCorrectPosition()
    {
        // Arrange
        var content = "Line one\nLine two";
        //            012345678 901234567
        var index = new PositionIndex(content);

        // Act - 't' of "two" is at index 14
        var (line, column) = index.GetPosition(14);

        // Assert
        line.Should().Be(2);
        column.Should().Be(6); // Position 14 - 9 (line start) + 1 = 6
    }

    [Fact]
    public void GetPosition_ThirdLine_ReturnsCorrectLine()
    {
        // Arrange
        var content = "One\nTwo\nThree";
        //            0123 4567 89...
        var index = new PositionIndex(content);

        // Act - 'T' of "Three" is at index 8
        var (line, column) = index.GetPosition(8);

        // Assert
        line.Should().Be(3);
        column.Should().Be(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetPosition_EmptyContent_ReturnsLine1Column1()
    {
        // Arrange
        var index = new PositionIndex("");

        // Act
        var (line, column) = index.GetPosition(0);

        // Assert
        line.Should().Be(1);
        column.Should().Be(1);
    }

    [Fact]
    public void GetPosition_NegativeOffset_ReturnsLine1Column1()
    {
        // Arrange
        var index = new PositionIndex("Hello");

        // Act
        var (line, column) = index.GetPosition(-5);

        // Assert
        line.Should().Be(1);
        column.Should().Be(1);
    }

    [Fact]
    public void GetPosition_OffsetBeyondContent_ClampsToEnd()
    {
        // Arrange
        var content = "Short";
        var index = new PositionIndex(content);

        // Act
        var (line, column) = index.GetPosition(100);

        // Assert
        line.Should().Be(1);
        column.Should().Be(6); // 5 chars + 1 = column 6 (at end)
    }

    [Fact]
    public void GetPosition_NullContent_ReturnsLine1Column1()
    {
        // Arrange - null treated as empty
        var index = new PositionIndex(null!);

        // Act
        var (line, column) = index.GetPosition(0);

        // Assert
        line.Should().Be(1);
        column.Should().Be(1);
    }

    [Fact]
    public void GetPosition_AtNewline_ReturnsEndOfLine()
    {
        // Arrange
        var content = "Line one\nLine two";
        //            012345678
        var index = new PositionIndex(content);

        // Act - newline is at index 8
        var (line, column) = index.GetPosition(8);

        // Assert
        line.Should().Be(1);
        column.Should().Be(9); // The newline is column 9 of line 1
    }

    #endregion

    #region LineCount Property

    [Fact]
    public void LineCount_SingleLine_ReturnsOne()
    {
        // Arrange
        var index = new PositionIndex("No newlines here");

        // Assert
        index.LineCount.Should().Be(1);
    }

    [Fact]
    public void LineCount_MultipleLines_ReturnsCorrectCount()
    {
        // Arrange
        var content = "One\nTwo\nThree\nFour";
        var index = new PositionIndex(content);

        // Assert
        index.LineCount.Should().Be(4);
    }

    [Fact]
    public void LineCount_EmptyContent_ReturnsOne()
    {
        // Arrange
        var index = new PositionIndex("");

        // Assert
        index.LineCount.Should().Be(1);
    }

    [Fact]
    public void LineCount_TrailingNewline_IncludesEmptyLastLine()
    {
        // Arrange
        var content = "One\nTwo\n";
        var index = new PositionIndex(content);

        // Assert - trailing newline creates an empty line 3
        index.LineCount.Should().Be(3);
    }

    #endregion

    #region GetSpanPositions

    [Fact]
    public void GetSpanPositions_SingleLine_ReturnsCorrectPositions()
    {
        // Arrange
        var content = "Hello world foo bar";
        //            0123456789012345678
        var index = new PositionIndex(content);

        // Act - "foo" is at indices 12-14
        var (start, end) = index.GetSpanPositions(12, 15);

        // Assert
        start.Line.Should().Be(1);
        start.Column.Should().Be(13);
        end.Line.Should().Be(1);
        end.Column.Should().Be(16);
    }

    [Fact]
    public void GetSpanPositions_MultiLine_ReturnsCorrectPositions()
    {
        // Arrange
        var content = "Line one\nLine two here";
        //            012345678 901234567890123
        var index = new PositionIndex(content);

        // Act - Span from line 1 to line 2
        var (start, end) = index.GetSpanPositions(5, 14);

        // Assert
        start.Line.Should().Be(1);
        start.Column.Should().Be(6);
        end.Line.Should().Be(2);
        // Line 2 starts at index 9, offset 14 - 9 = 5, column = 5 + 1 = 6
        end.Column.Should().Be(6);
    }

    #endregion

    #region Performance / Large Content

    [Fact]
    public void GetPosition_LargeDocument_PerformsEfficiently()
    {
        // Arrange - 10,000 lines of content
        var lines = Enumerable.Range(1, 10_000)
            .Select(i => $"Line {i}: This is some content to make the line longer.");
        var content = string.Join("\n", lines);
        var index = new PositionIndex(content);

        // Act - Query various positions
        var positions = Enumerable.Range(0, 1000)
            .Select(i => i * 100)
            .Where(offset => offset < content.Length)
            .Select(offset => index.GetPosition(offset))
            .ToList();

        // Assert - All positions should be valid
        positions.Should().NotBeEmpty();
        positions.Should().AllSatisfy(p =>
        {
            p.Line.Should().BeGreaterThan(0);
            p.Column.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void LineCount_LargeDocument_IsAccurate()
    {
        // Arrange
        var lineCount = 5_000;
        var content = string.Join("\n", Enumerable.Range(1, lineCount).Select(i => $"Line {i}"));
        var index = new PositionIndex(content);

        // Assert
        index.LineCount.Should().Be(lineCount);
    }

    #endregion
}
