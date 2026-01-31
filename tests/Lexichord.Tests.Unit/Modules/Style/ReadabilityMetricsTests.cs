using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="ReadabilityMetrics"/>.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.3c - Verifies computed properties, empty metrics,
/// and reading ease interpretation strings.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.3c")]
public class ReadabilityMetricsTests
{
    #region Computed Properties Tests

    [Fact]
    public void AverageWordsPerSentence_WithValidCounts_ReturnsCorrectValue()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            WordCount = 100,
            SentenceCount = 5
        };

        // Act & Assert
        metrics.AverageWordsPerSentence.Should().Be(20);
    }

    [Fact]
    public void AverageWordsPerSentence_WithZeroSentences_ReturnsZero()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            WordCount = 100,
            SentenceCount = 0
        };

        // Act & Assert
        metrics.AverageWordsPerSentence.Should().Be(0);
    }

    [Fact]
    public void AverageSyllablesPerWord_WithValidCounts_ReturnsCorrectValue()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            SyllableCount = 150,
            WordCount = 100
        };

        // Act & Assert
        metrics.AverageSyllablesPerWord.Should().Be(1.5);
    }

    [Fact]
    public void AverageSyllablesPerWord_WithZeroWords_ReturnsZero()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            SyllableCount = 150,
            WordCount = 0
        };

        // Act & Assert
        metrics.AverageSyllablesPerWord.Should().Be(0);
    }

    [Fact]
    public void ComplexWordRatio_WithValidCounts_ReturnsCorrectValue()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            ComplexWordCount = 25,
            WordCount = 100
        };

        // Act & Assert
        metrics.ComplexWordRatio.Should().Be(0.25);
    }

    [Fact]
    public void ComplexWordRatio_WithZeroWords_ReturnsZero()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            ComplexWordCount = 25,
            WordCount = 0
        };

        // Act & Assert
        metrics.ComplexWordRatio.Should().Be(0);
    }

    #endregion

    #region Reading Ease Interpretation Tests

    [Theory]
    [InlineData(95, "Very Easy")]
    [InlineData(90, "Very Easy")]
    [InlineData(85, "Easy")]
    [InlineData(80, "Easy")]
    [InlineData(75, "Fairly Easy")]
    [InlineData(70, "Fairly Easy")]
    [InlineData(65, "Standard")]
    [InlineData(60, "Standard")]
    [InlineData(55, "Fairly Difficult")]
    [InlineData(50, "Fairly Difficult")]
    [InlineData(40, "Difficult")]
    [InlineData(30, "Difficult")]
    [InlineData(25, "Very Confusing")]
    [InlineData(0, "Very Confusing")]
    public void ReadingEaseInterpretation_ReturnsCorrectLabel(
        double fleschReadingEase, string expectedLabel)
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            FleschReadingEase = fleschReadingEase
        };

        // Act & Assert
        metrics.ReadingEaseInterpretation.Should().Be(expectedLabel);
    }

    #endregion

    #region Empty Metrics Tests

    [Fact]
    public void Empty_ReturnsMetricsWithZeroValues()
    {
        // Act
        var empty = ReadabilityMetrics.Empty;

        // Assert
        empty.FleschKincaidGradeLevel.Should().Be(0);
        empty.GunningFogIndex.Should().Be(0);
        empty.FleschReadingEase.Should().Be(0);
        empty.WordCount.Should().Be(0);
        empty.SentenceCount.Should().Be(0);
        empty.SyllableCount.Should().Be(0);
        empty.ComplexWordCount.Should().Be(0);
    }

    [Fact]
    public void Empty_ComputedProperties_ReturnZero()
    {
        // Act
        var empty = ReadabilityMetrics.Empty;

        // Assert
        empty.AverageWordsPerSentence.Should().Be(0);
        empty.AverageSyllablesPerWord.Should().Be(0);
        empty.ComplexWordRatio.Should().Be(0);
    }

    [Fact]
    public void Empty_ReadingEaseInterpretation_ReturnsVeryConfusing()
    {
        // Act
        var empty = ReadabilityMetrics.Empty;

        // Assert
        empty.ReadingEaseInterpretation.Should().Be("Very Confusing");
    }

    [Fact]
    public void Empty_IsSingletonInstance()
    {
        // Act
        var empty1 = ReadabilityMetrics.Empty;
        var empty2 = ReadabilityMetrics.Empty;

        // Assert
        ReferenceEquals(empty1, empty2).Should().BeTrue();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var metrics1 = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 8.5,
            GunningFogIndex = 10.2,
            FleschReadingEase = 65.0,
            WordCount = 100,
            SentenceCount = 5,
            SyllableCount = 150,
            ComplexWordCount = 20
        };

        var metrics2 = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 8.5,
            GunningFogIndex = 10.2,
            FleschReadingEase = 65.0,
            WordCount = 100,
            SentenceCount = 5,
            SyllableCount = 150,
            ComplexWordCount = 20
        };

        // Act & Assert
        metrics1.Should().Be(metrics2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var metrics1 = new ReadabilityMetrics { FleschKincaidGradeLevel = 8.5 };
        var metrics2 = new ReadabilityMetrics { FleschKincaidGradeLevel = 9.0 };

        // Act & Assert
        metrics1.Should().NotBe(metrics2);
    }

    #endregion
}
