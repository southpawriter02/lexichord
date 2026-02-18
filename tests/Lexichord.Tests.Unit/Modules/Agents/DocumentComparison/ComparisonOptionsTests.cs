// -----------------------------------------------------------------------
// <copyright file="ComparisonOptionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.DocumentComparison;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.DocumentComparison;

/// <summary>
/// Unit tests for <see cref="ComparisonOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6d")]
public class ComparisonOptionsTests
{
    // ── Default Values ──────────────────────────────────────────────────

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ComparisonOptions();

        // Assert
        options.SignificanceThreshold.Should().Be(0.2);
        options.IncludeFormattingChanges.Should().BeFalse();
        options.GroupBySection.Should().BeTrue();
        options.MaxChanges.Should().Be(20);
        options.FocusSections.Should().BeNull();
        options.IncludeTextDiff.Should().BeFalse();
        options.IdentifyRelatedChanges.Should().BeTrue();
        options.OriginalVersionLabel.Should().BeNull();
        options.NewVersionLabel.Should().BeNull();
        options.MaxResponseTokens.Should().Be(4096);
    }

    [Fact]
    public void Default_ReturnsSameValuesAsNewInstance()
    {
        // Act
        var defaultOptions = ComparisonOptions.Default;
        var newOptions = new ComparisonOptions();

        // Assert
        defaultOptions.SignificanceThreshold.Should().Be(newOptions.SignificanceThreshold);
        defaultOptions.IncludeFormattingChanges.Should().Be(newOptions.IncludeFormattingChanges);
        defaultOptions.GroupBySection.Should().Be(newOptions.GroupBySection);
        defaultOptions.MaxChanges.Should().Be(newOptions.MaxChanges);
        defaultOptions.IncludeTextDiff.Should().Be(newOptions.IncludeTextDiff);
        defaultOptions.OriginalVersionLabel.Should().Be(newOptions.OriginalVersionLabel);
        defaultOptions.NewVersionLabel.Should().Be(newOptions.NewVersionLabel);
    }

    // ── SignificanceThreshold Property ───────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void SignificanceThreshold_CanBeSetToValidValues(double threshold)
    {
        // Arrange & Act
        var options = new ComparisonOptions { SignificanceThreshold = threshold };

        // Assert
        options.SignificanceThreshold.Should().Be(threshold);
    }

    // ── MaxChanges Property ──────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void MaxChanges_CanBeSetToPositiveValues(int maxChanges)
    {
        // Arrange & Act
        var options = new ComparisonOptions { MaxChanges = maxChanges };

        // Assert
        options.MaxChanges.Should().Be(maxChanges);
    }

    // ── FocusSections Property ───────────────────────────────────────────

    [Fact]
    public void FocusSections_CanBeNull()
    {
        // Arrange & Act
        var options = new ComparisonOptions { FocusSections = null };

        // Assert
        options.FocusSections.Should().BeNull();
    }

    [Fact]
    public void FocusSections_CanContainSections()
    {
        // Arrange
        var sections = new List<string> { "Introduction", "Methods", "Results" };

        // Act
        var options = new ComparisonOptions { FocusSections = sections };

        // Assert
        options.FocusSections.Should().BeEquivalentTo(sections);
    }

    // ── Version Labels ───────────────────────────────────────────────────

    [Fact]
    public void VersionLabels_CanBeCustomized()
    {
        // Arrange & Act
        var options = new ComparisonOptions
        {
            OriginalVersionLabel = "HEAD~1",
            NewVersionLabel = "HEAD"
        };

        // Assert
        options.OriginalVersionLabel.Should().Be("HEAD~1");
        options.NewVersionLabel.Should().Be("HEAD");
    }

    [Fact]
    public void VersionLabels_DefaultToNull()
    {
        // Arrange & Act
        var options = new ComparisonOptions();

        // Assert
        options.OriginalVersionLabel.Should().BeNull();
        options.NewVersionLabel.Should().BeNull();
    }

    [Fact]
    public void WithLabels_CreatesNewInstanceWithLabels()
    {
        // Arrange
        var options = new ComparisonOptions { MaxChanges = 15 };

        // Act
        var newOptions = options.WithLabels("v1.0", "v2.0");

        // Assert
        newOptions.OriginalVersionLabel.Should().Be("v1.0");
        newOptions.NewVersionLabel.Should().Be("v2.0");
        newOptions.MaxChanges.Should().Be(15); // Original values preserved
    }

    // ── Validate: SignificanceThreshold ──────────────────────────────────

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.0)]
    public void Validate_NegativeSignificanceThreshold_ThrowsArgumentException(double threshold)
    {
        // Arrange
        var options = new ComparisonOptions { SignificanceThreshold = threshold };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("SignificanceThreshold");
    }

    [Theory]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Validate_SignificanceThresholdAboveOne_ThrowsArgumentException(double threshold)
    {
        // Arrange
        var options = new ComparisonOptions { SignificanceThreshold = threshold };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("SignificanceThreshold");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Validate_ValidSignificanceThreshold_DoesNotThrow(double threshold)
    {
        // Arrange
        var options = new ComparisonOptions { SignificanceThreshold = threshold };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: MaxChanges ─────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_NonPositiveMaxChanges_ThrowsArgumentException(int maxChanges)
    {
        // Arrange
        var options = new ComparisonOptions { MaxChanges = maxChanges };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxChanges");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(50)]
    public void Validate_PositiveMaxChanges_DoesNotThrow(int maxChanges)
    {
        // Arrange
        var options = new ComparisonOptions { MaxChanges = maxChanges };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: MaxResponseTokens ──────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_NonPositiveMaxResponseTokens_ThrowsArgumentException(int tokens)
    {
        // Arrange
        var options = new ComparisonOptions { MaxResponseTokens = tokens };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxResponseTokens");
    }

    // ── Full Options Construction ────────────────────────────────────────

    [Fact]
    public void FullOptions_SetsAllProperties()
    {
        // Arrange
        var focusSections = new List<string> { "Abstract", "Conclusion" };

        // Act
        var options = new ComparisonOptions
        {
            SignificanceThreshold = 0.3,
            IncludeFormattingChanges = true,
            GroupBySection = false,
            MaxChanges = 30,
            FocusSections = focusSections,
            IncludeTextDiff = false,
            IdentifyRelatedChanges = false,
            OriginalVersionLabel = "Before",
            NewVersionLabel = "After",
            MaxResponseTokens = 8192
        };

        // Assert
        options.SignificanceThreshold.Should().Be(0.3);
        options.IncludeFormattingChanges.Should().BeTrue();
        options.GroupBySection.Should().BeFalse();
        options.MaxChanges.Should().Be(30);
        options.FocusSections.Should().BeEquivalentTo(focusSections);
        options.IncludeTextDiff.Should().BeFalse();
        options.IdentifyRelatedChanges.Should().BeFalse();
        options.OriginalVersionLabel.Should().Be("Before");
        options.NewVersionLabel.Should().Be("After");
        options.MaxResponseTokens.Should().Be(8192);
    }

    // ── Edge Cases ───────────────────────────────────────────────────────

    [Fact]
    public void FocusSections_EmptyList_IsValid()
    {
        // Arrange
        var options = new ComparisonOptions { FocusSections = new List<string>() };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
        options.FocusSections.Should().BeEmpty();
    }
}
