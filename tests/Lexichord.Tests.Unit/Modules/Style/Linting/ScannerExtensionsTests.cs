using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for <see cref="ScannerExtensions"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.7b
/// </remarks>
public sealed class ScannerExtensionsTests
{
    private static StyleRule CreateTestRule() => new(
        Id: "TEST001",
        Name: "Test Rule",
        Description: "Test description",
        Category: RuleCategory.Terminology,
        DefaultSeverity: ViolationSeverity.Warning,
        Pattern: "test",
        PatternType: PatternType.Literal,
        Suggestion: "replacement",
        IsEnabled: true);

    private static ScanMatch CreateMatch(int startOffset, int length, string text = "test") =>
        new(
            RuleId: "TEST001",
            StartOffset: startOffset,
            Length: length,
            MatchedText: text,
            Rule: CreateTestRule());

    #region FilterByExclusions Basic Tests

    [Fact]
    public void FilterByExclusions_NoExclusions_ReturnsAll()
    {
        // Arrange
        var matches = new[]
        {
            CreateMatch(0, 4),
            CreateMatch(10, 4)
        };

        // Act
        var result = matches.FilterByExclusions(Array.Empty<ExcludedRegion>()).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void FilterByExclusions_EmptyMatches_ReturnsEmpty()
    {
        // Arrange
        var matches = Array.Empty<ScanMatch>();
        var exclusions = new[]
        {
            new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock)
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterByExclusions_MatchInExclusion_Removed()
    {
        // Arrange
        var matches = new[]
        {
            CreateMatch(5, 4)  // Inside exclusion
        };
        var exclusions = new[]
        {
            new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock)
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterByExclusions_MatchOutsideExclusion_Kept()
    {
        // Arrange
        var matches = new[]
        {
            CreateMatch(25, 4)  // Outside exclusion
        };
        var exclusions = new[]
        {
            new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock)
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public void FilterByExclusions_MatchAtExclusionBoundary_Kept()
    {
        // Arrange - Match starts exactly at exclusion end
        var matches = new[]
        {
            CreateMatch(20, 4)  // Starts at exclusion end (exclusive)
        };
        var exclusions = new[]
        {
            new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock)
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert - Should be kept (end offset is exclusive)
        result.Should().HaveCount(1);
    }

    [Fact]
    public void FilterByExclusions_MatchAtExclusionStart_Removed()
    {
        // Arrange - Match starts exactly at exclusion start
        var matches = new[]
        {
            CreateMatch(0, 4)  // Starts at exclusion start
        };
        var exclusions = new[]
        {
            new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock)
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Multiple Exclusions

    [Fact]
    public void FilterByExclusions_MixedMatches_FiltersCorrectly()
    {
        // Arrange
        var matches = new[]
        {
            CreateMatch(5, 4, "code1"),    // In first exclusion
            CreateMatch(25, 5, "prose"),   // Outside
            CreateMatch(35, 5, "code2")    // In second exclusion
        };
        var exclusions = new[]
        {
            new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock),
            new ExcludedRegion(30, 50, ExclusionReason.InlineCode)
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].MatchedText.Should().Be("prose");
    }

    [Fact]
    public void FilterByExclusions_UnsortedExclusions_HandlesCorrectly()
    {
        // Arrange - Exclusions not in order by start offset
        var matches = new[]
        {
            CreateMatch(15, 4),  // In second (by position) exclusion
            CreateMatch(45, 4)   // In first (by position) exclusion
        };
        var exclusions = new[]
        {
            new ExcludedRegion(40, 60, ExclusionReason.InlineCode),    // Later
            new ExcludedRegion(10, 25, ExclusionReason.FencedCodeBlock) // Earlier
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert - Both should be filtered despite unsorted input
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterByExclusions_ManyExclusions_UsesEfficientSearch()
    {
        // Arrange - Many exclusions to exercise binary search
        var exclusions = Enumerable.Range(0, 100)
            .Select(i => new ExcludedRegion(i * 100, i * 100 + 50, ExclusionReason.FencedCodeBlock))
            .ToArray();

        var matches = new[]
        {
            CreateMatch(25, 4),    // In exclusion 0 (0-50)
            CreateMatch(75, 4),    // Between exclusions
            CreateMatch(125, 4),   // In exclusion 1 (100-150)
            CreateMatch(175, 4),   // Between exclusions
            CreateMatch(9925, 4)   // In exclusion 99 (9900-9950)
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().HaveCount(2);  // Only the two between exclusions
    }

    #endregion

    #region FilterByExclusions with FilteredContent

    [Fact]
    public void FilterByExclusions_WithFilteredContent_Works()
    {
        // Arrange
        var matches = new[]
        {
            CreateMatch(5, 4),   // In exclusion
            CreateMatch(25, 4)   // Outside
        };
        var filteredContent = new FilteredContent(
            ProcessedContent: "content",
            ExcludedRegions: new[] { new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock) },
            OriginalContent: "content");

        // Act
        var result = matches.FilterByExclusions(filteredContent).ToList();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public void FilterByExclusions_FilteredContentWithNoExclusions_ReturnsAll()
    {
        // Arrange
        var matches = new[]
        {
            CreateMatch(5, 4),
            CreateMatch(25, 4)
        };
        var filteredContent = FilteredContent.None("content");

        // Act
        var result = matches.FilterByExclusions(filteredContent).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region CountExcludedMatches

    [Fact]
    public void CountExcludedMatches_NoExclusions_ReturnsZero()
    {
        // Arrange
        var matches = new[]
        {
            CreateMatch(0, 4),
            CreateMatch(10, 4)
        };

        // Act
        var count = matches.CountExcludedMatches(Array.Empty<ExcludedRegion>());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void CountExcludedMatches_SomeExcluded_ReturnsCorrectCount()
    {
        // Arrange
        var matches = new[]
        {
            CreateMatch(5, 4),   // In exclusion
            CreateMatch(25, 4),  // Outside
            CreateMatch(35, 4)   // In exclusion
        };
        var exclusions = new[]
        {
            new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock),
            new ExcludedRegion(30, 50, ExclusionReason.InlineCode)
        };

        // Act
        var count = matches.CountExcludedMatches(exclusions);

        // Assert
        count.Should().Be(2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FilterByExclusions_AdjacentExclusions_HandlesCorrectly()
    {
        // Arrange - Exclusions that are adjacent (no gap)
        var matches = new[]
        {
            CreateMatch(5, 4),   // In first exclusion
            CreateMatch(25, 4),  // In second exclusion
            CreateMatch(45, 4)   // Outside
        };
        var exclusions = new[]
        {
            new ExcludedRegion(0, 20, ExclusionReason.FencedCodeBlock),
            new ExcludedRegion(20, 40, ExclusionReason.InlineCode)  // Starts where first ends
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].StartOffset.Should().Be(45);
    }

    [Fact]
    public void FilterByExclusions_OverlappingExclusions_HandlesCorrectly()
    {
        // Arrange - Overlapping exclusions (unusual but possible)
        var matches = new[]
        {
            CreateMatch(15, 4),  // In both exclusions
            CreateMatch(35, 4)   // Only in second
        };
        var exclusions = new[]
        {
            new ExcludedRegion(0, 30, ExclusionReason.FencedCodeBlock),
            new ExcludedRegion(10, 50, ExclusionReason.InlineCode)
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().BeEmpty();  // Both should be excluded
    }

    [Fact]
    public void FilterByExclusions_ZeroLengthExclusion_DoesNotMatch()
    {
        // Arrange - Zero-length exclusion
        var matches = new[]
        {
            CreateMatch(10, 4)  // At the zero-length exclusion point
        };
        var exclusions = new[]
        {
            new ExcludedRegion(10, 10, ExclusionReason.FencedCodeBlock)  // Zero length
        };

        // Act
        var result = matches.FilterByExclusions(exclusions).ToList();

        // Assert
        result.Should().HaveCount(1);  // Should not match zero-length
    }

    #endregion
}
