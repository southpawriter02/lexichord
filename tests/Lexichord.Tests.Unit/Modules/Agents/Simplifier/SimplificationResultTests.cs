// -----------------------------------------------------------------------
// <copyright file="SimplificationResultTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="SimplificationResult"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4b")]
public class SimplificationResultTests
{
    // ── Computed Properties Tests ────────────────────────────────────────

    [Fact]
    public void GradeLevelReduction_ComputedCorrectly()
    {
        // Arrange
        var originalMetrics = CreateMetricsWithGrade(14.5);
        var simplifiedMetrics = CreateMetricsWithGrade(8.0);
        var result = CreateSuccessResult(originalMetrics, simplifiedMetrics);

        // Act & Assert
        result.GradeLevelReduction.Should().BeApproximately(6.5, 0.01);
    }

    [Fact]
    public void GradeLevelReduction_NegativeWhenSimplifiedIsHigher()
    {
        // Arrange
        var originalMetrics = CreateMetricsWithGrade(8.0);
        var simplifiedMetrics = CreateMetricsWithGrade(10.0);
        var result = CreateSuccessResult(originalMetrics, simplifiedMetrics);

        // Act & Assert
        result.GradeLevelReduction.Should().BeApproximately(-2.0, 0.01);
    }

    [Fact]
    public void WordCountDifference_PositiveWhenReduced()
    {
        // Arrange
        var originalMetrics = CreateMetricsWithWordCount(200);
        var simplifiedMetrics = CreateMetricsWithWordCount(150);
        var result = CreateSuccessResult(originalMetrics, simplifiedMetrics);

        // Act & Assert
        result.WordCountDifference.Should().Be(50);
    }

    [Fact]
    public void WordCountDifference_NegativeWhenExpanded()
    {
        // Arrange
        var originalMetrics = CreateMetricsWithWordCount(100);
        var simplifiedMetrics = CreateMetricsWithWordCount(120);
        var result = CreateSuccessResult(originalMetrics, simplifiedMetrics);

        // Act & Assert
        result.WordCountDifference.Should().Be(-20);
    }

    [Fact]
    public void TargetAchieved_TrueWhenWithinTolerance()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true, 2.0); // 6-10 acceptable
        var originalMetrics = CreateMetricsWithGrade(14.0);
        var simplifiedMetrics = CreateMetricsWithGrade(8.5); // Within 8±2
        var result = CreateSuccessResultWithTarget(originalMetrics, simplifiedMetrics, target);

        // Act & Assert
        result.TargetAchieved.Should().BeTrue();
    }

    [Fact]
    public void TargetAchieved_FalseWhenOutsideTolerance()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true, 1.0); // 7-9 acceptable
        var originalMetrics = CreateMetricsWithGrade(14.0);
        var simplifiedMetrics = CreateMetricsWithGrade(10.5); // Outside 8±1
        var result = CreateSuccessResultWithTarget(originalMetrics, simplifiedMetrics, target);

        // Act & Assert
        result.TargetAchieved.Should().BeFalse();
    }

    [Fact]
    public void TargetAchieved_FalseWhenNotSuccessful()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var result = SimplificationResult.Failed(
            "Original text",
            SimplificationStrategy.Balanced,
            target,
            "Some error",
            TimeSpan.FromSeconds(1));

        // Act & Assert
        result.TargetAchieved.Should().BeFalse();
    }

    // ── Failed Factory Tests ────────────────────────────────────────────

    [Fact]
    public void Failed_ReturnsFailedResult()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var processingTime = TimeSpan.FromSeconds(5);

        // Act
        var result = SimplificationResult.Failed(
            "Original text",
            SimplificationStrategy.Aggressive,
            target,
            "LLM timeout",
            processingTime);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("LLM timeout");
        result.SimplifiedText.Should().BeEmpty();
        result.StrategyUsed.Should().Be(SimplificationStrategy.Aggressive);
        result.TargetUsed.Should().Be(target);
        result.ProcessingTime.Should().Be(processingTime);
        result.TokenUsage.Should().Be(UsageMetrics.Zero);
        result.OriginalMetrics.Should().Be(ReadabilityMetrics.Empty);
        result.SimplifiedMetrics.Should().Be(ReadabilityMetrics.Empty);
        result.Changes.Should().BeEmpty();
        result.Glossary.Should().BeNull();
    }

    [Fact]
    public void Failed_NullOriginalText_ThrowsArgumentNullException()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);

        // Act
        var act = () => SimplificationResult.Failed(
            null!,
            SimplificationStrategy.Balanced,
            target,
            "Error",
            TimeSpan.FromSeconds(1));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("originalText");
    }

    [Fact]
    public void Failed_NullTarget_ThrowsArgumentNullException()
    {
        // Act
        var act = () => SimplificationResult.Failed(
            "Original text",
            SimplificationStrategy.Balanced,
            null!,
            "Error",
            TimeSpan.FromSeconds(1));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    [Fact]
    public void Failed_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);

        // Act
        var act = () => SimplificationResult.Failed(
            "Original text",
            SimplificationStrategy.Balanced,
            target,
            null!,
            TimeSpan.FromSeconds(1));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("errorMessage");
    }

    // ── Success Result Tests ────────────────────────────────────────────

    [Fact]
    public void SuccessResult_HasCorrectDefaults()
    {
        // Arrange
        var originalMetrics = CreateMetricsWithGrade(12.0);
        var simplifiedMetrics = CreateMetricsWithGrade(8.0);
        var result = CreateSuccessResult(originalMetrics, simplifiedMetrics);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SuccessResult_ChangesAreReadOnly()
    {
        // Arrange
        var changes = new List<SimplificationChange>
        {
            new("original", "simplified", SimplificationChangeType.WordSimplification, "Reason")
        };

        var result = new SimplificationResult
        {
            SimplifiedText = "Simplified",
            OriginalMetrics = CreateMetricsWithGrade(12.0),
            SimplifiedMetrics = CreateMetricsWithGrade(8.0),
            Changes = changes.AsReadOnly(),
            TokenUsage = UsageMetrics.Zero,
            ProcessingTime = TimeSpan.FromSeconds(1),
            StrategyUsed = SimplificationStrategy.Balanced,
            TargetUsed = ReadabilityTarget.FromExplicit(8.0, 20, true)
        };

        // Assert
        result.Changes.Should().BeAssignableTo<IReadOnlyList<SimplificationChange>>();
        result.Changes.Should().HaveCount(1);
    }

    // ── Helper Methods ────────────────────────────────────────────────

    private static ReadabilityMetrics CreateMetricsWithGrade(double gradeLevel) =>
        new()
        {
            FleschKincaidGradeLevel = gradeLevel,
            GunningFogIndex = gradeLevel + 1,
            FleschReadingEase = 70,
            WordCount = 100,
            SentenceCount = 5,
            SyllableCount = 150,
            ComplexWordCount = 10
        };

    private static ReadabilityMetrics CreateMetricsWithWordCount(int wordCount) =>
        new()
        {
            FleschKincaidGradeLevel = 10.0,
            GunningFogIndex = 11.0,
            FleschReadingEase = 60,
            WordCount = wordCount,
            SentenceCount = wordCount / 15,
            SyllableCount = wordCount * 2,
            ComplexWordCount = wordCount / 10
        };

    private static SimplificationResult CreateSuccessResult(
        ReadabilityMetrics originalMetrics,
        ReadabilityMetrics simplifiedMetrics) =>
        CreateSuccessResultWithTarget(
            originalMetrics,
            simplifiedMetrics,
            ReadabilityTarget.FromExplicit(8.0, 20, true));

    private static SimplificationResult CreateSuccessResultWithTarget(
        ReadabilityMetrics originalMetrics,
        ReadabilityMetrics simplifiedMetrics,
        ReadabilityTarget target) =>
        new()
        {
            SimplifiedText = "Simplified text",
            OriginalMetrics = originalMetrics,
            SimplifiedMetrics = simplifiedMetrics,
            Changes = Array.Empty<SimplificationChange>(),
            TokenUsage = new UsageMetrics(1000, 500, 0.025m),
            ProcessingTime = TimeSpan.FromSeconds(5),
            StrategyUsed = SimplificationStrategy.Balanced,
            TargetUsed = target,
            Success = true
        };
}
