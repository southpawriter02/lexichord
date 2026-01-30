using FluentAssertions;
using Lexichord.Modules.Style.Services.Linting;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for <see cref="PatternComplexityAnalyzer"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.3c
/// </remarks>
public sealed class PatternComplexityAnalyzerTests
{
    #region Safe Patterns

    [Theory]
    [InlineData(@"\bword\b")]
    [InlineData(@"[a-z]+")]
    [InlineData(@"foo|bar|baz")]
    [InlineData(@"^\d{3}-\d{2}-\d{4}$")]
    [InlineData(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    public void Analyze_SafePatterns_ReturnsSafe(string pattern)
    {
        // Act
        var result = PatternComplexityAnalyzer.Analyze(pattern);

        // Assert
        result.Level.Should().Be(PatternComplexityAnalyzer.ComplexityLevel.Safe);
        result.IsSafe.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public void Analyze_EmptyPattern_ReturnsSafe()
    {
        // Act
        var result = PatternComplexityAnalyzer.Analyze("");

        // Assert
        result.IsSafe.Should().BeTrue();
    }

    [Fact]
    public void Analyze_NullPattern_ReturnsSafe()
    {
        // Act
        var result = PatternComplexityAnalyzer.Analyze(null!);

        // Assert
        result.IsSafe.Should().BeTrue();
    }

    #endregion

    #region Dangerous Patterns

    [Theory]
    [InlineData(@"(a+)+")]      // Classic nested quantifier
    [InlineData(@"(a*)*")]      // Nested star quantifiers
    [InlineData(@"(a+)+$")]     // Nested with anchor
    [InlineData(@"(.*)*")]      // Very dangerous
    [InlineData(@"([a-zA-Z]+)*")]  // Nested with character class
    public void Analyze_NestedQuantifiers_ReturnsDangerous(string pattern)
    {
        // Act
        var result = PatternComplexityAnalyzer.Analyze(pattern);

        // Assert
        result.Level.Should().Be(PatternComplexityAnalyzer.ComplexityLevel.Dangerous);
        result.IsDangerous.Should().BeTrue();
        result.Reason.Should().Contain("nested quantifiers");
    }

    [Fact]
    public void ShouldBlock_WithDangerousPattern_ReturnsTrue()
    {
        // Arrange
        var dangerousPattern = @"(a+)+";

        // Act
        var shouldBlock = PatternComplexityAnalyzer.ShouldBlock(dangerousPattern);

        // Assert
        shouldBlock.Should().BeTrue();
    }

    [Fact]
    public void ShouldBlock_WithSafePattern_ReturnsFalse()
    {
        // Arrange
        var safePattern = @"\bword\b";

        // Act
        var shouldBlock = PatternComplexityAnalyzer.ShouldBlock(safePattern);

        // Assert
        shouldBlock.Should().BeFalse();
    }

    #endregion

    #region Analysis Result Properties

    [Fact]
    public void AnalysisResult_IsSafe_IsTrueWhenSafe()
    {
        // Arrange
        var result = new PatternComplexityAnalyzer.AnalysisResult(
            PatternComplexityAnalyzer.ComplexityLevel.Safe);

        // Assert
        result.IsSafe.Should().BeTrue();
        result.IsDangerous.Should().BeFalse();
    }

    [Fact]
    public void AnalysisResult_IsDangerous_IsTrueWhenDangerous()
    {
        // Arrange
        var result = new PatternComplexityAnalyzer.AnalysisResult(
            PatternComplexityAnalyzer.ComplexityLevel.Dangerous,
            "Test reason");

        // Assert
        result.IsSafe.Should().BeFalse();
        result.IsDangerous.Should().BeTrue();
        result.Reason.Should().Be("Test reason");
    }

    [Fact]
    public void AnalysisResult_Suspicious_IsNeitherSafeNorDangerous()
    {
        // Arrange
        var result = new PatternComplexityAnalyzer.AnalysisResult(
            PatternComplexityAnalyzer.ComplexityLevel.Suspicious,
            "Some warning");

        // Assert
        result.IsSafe.Should().BeFalse();
        result.IsDangerous.Should().BeFalse();
    }

    #endregion
}
