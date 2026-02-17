// -----------------------------------------------------------------------
// <copyright file="FixPositionSorterTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// Unit tests for FixPositionSorter (v0.7.5h).
//   Tests cover null handling, empty input, auto-fix filtering, bottom-to-top
//   position sorting (descending Start, then descending End), and the
//   unfiltered sorting variant.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;
using Lexichord.Modules.Agents.Tuning.FixOrchestration;

// LOGIC: Use alias to disambiguate from Knowledge.Validation.Integration.UnifiedFix
using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning.FixOrchestration;

/// <summary>
/// Unit tests for <see cref="FixPositionSorter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>SortBottomToTop — Null input, empty list, filtering, sorting, mixed scenarios</description></item>
///   <item><description>SortBottomToTopUnfiltered — Null input, no filtering, sorting, single item</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5h")]
public class FixPositionSorterTests
{
    #region Test Setup

    /// <summary>
    /// Creates a <see cref="UnifiedIssue"/> at the specified position with configurable fix presence
    /// and auto-apply capability.
    /// </summary>
    /// <param name="start">The zero-based starting character position of the issue.</param>
    /// <param name="length">The length of the text span.</param>
    /// <param name="hasFix">Whether the issue should have a fix attached.</param>
    /// <param name="canAutoApply">Whether the fix can be automatically applied.</param>
    /// <param name="category">The issue category (defaults to Grammar).</param>
    /// <returns>A new <see cref="UnifiedIssue"/> configured for testing.</returns>
    private static UnifiedIssue CreateIssue(
        int start, int length,
        bool hasFix = true,
        bool canAutoApply = true,
        IssueCategory category = IssueCategory.Grammar)
    {
        var location = new TextSpan(start, length);
        var fixes = hasFix
            ? new[] { UnifiedFix.Replacement(location, "old", "new", "Test fix", 0.9) with { CanAutoApply = canAutoApply } }
            : Array.Empty<UnifiedFix>();

        return new UnifiedIssue(
            Guid.NewGuid(), "TEST-001", category, UnifiedSeverity.Warning,
            "Test issue", location, "old", fixes, "TestSource", null);
    }

    #endregion

    #region SortBottomToTop Tests

    [Fact]
    public void SortBottomToTop_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<UnifiedIssue> issues = null!;

        // Act
        var act = () => FixPositionSorter.SortBottomToTop(issues);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SortBottomToTop_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var issues = Array.Empty<UnifiedIssue>();

        // Act
        var result = FixPositionSorter.SortBottomToTop(issues);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SortBottomToTop_FiltersToAutoFixableOnly()
    {
        // Arrange
        var fixableIssue = CreateIssue(start: 10, length: 5, hasFix: true, canAutoApply: true);
        var noFixIssue = CreateIssue(start: 20, length: 5, hasFix: false);
        var nonAutoApplyIssue = CreateIssue(start: 30, length: 5, hasFix: true, canAutoApply: false);
        var issues = new[] { fixableIssue, noFixIssue, nonAutoApplyIssue };

        // Act
        var result = FixPositionSorter.SortBottomToTop(issues);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(fixableIssue);
    }

    [Fact]
    public void SortBottomToTop_SortsByStartDescending()
    {
        // Arrange
        var issueAt10 = CreateIssue(start: 10, length: 5);
        var issueAt20 = CreateIssue(start: 20, length: 5);
        var issueAt30 = CreateIssue(start: 30, length: 5);
        var issues = new[] { issueAt10, issueAt30, issueAt20 };

        // Act
        var result = FixPositionSorter.SortBottomToTop(issues);

        // Assert
        result.Should().HaveCount(3);
        result[0].Location.Start.Should().Be(30);
        result[1].Location.Start.Should().Be(20);
        result[2].Location.Start.Should().Be(10);
    }

    [Fact]
    public void SortBottomToTop_EqualStart_SortsByEndDescending()
    {
        // Arrange — all issues start at position 10 but have different lengths
        var shortSpan = CreateIssue(start: 10, length: 3);   // End = 13
        var mediumSpan = CreateIssue(start: 10, length: 7);  // End = 17
        var longSpan = CreateIssue(start: 10, length: 12);   // End = 22
        var issues = new[] { shortSpan, longSpan, mediumSpan };

        // Act
        var result = FixPositionSorter.SortBottomToTop(issues);

        // Assert
        result.Should().HaveCount(3);
        result[0].Location.End.Should().Be(22); // longest span first
        result[1].Location.End.Should().Be(17);
        result[2].Location.End.Should().Be(13); // shortest span last
    }

    [Fact]
    public void SortBottomToTop_MixedFilterAndSort()
    {
        // Arrange — mix of fixable/non-fixable at various positions
        var fixableAt50 = CreateIssue(start: 50, length: 5, hasFix: true, canAutoApply: true);
        var noFixAt100 = CreateIssue(start: 100, length: 5, hasFix: false);
        var fixableAt10 = CreateIssue(start: 10, length: 5, hasFix: true, canAutoApply: true);
        var nonAutoAt75 = CreateIssue(start: 75, length: 5, hasFix: true, canAutoApply: false);
        var fixableAt30 = CreateIssue(start: 30, length: 5, hasFix: true, canAutoApply: true);
        var issues = new[] { fixableAt50, noFixAt100, fixableAt10, nonAutoAt75, fixableAt30 };

        // Act
        var result = FixPositionSorter.SortBottomToTop(issues);

        // Assert — only fixable issues, sorted bottom-to-top
        result.Should().HaveCount(3);
        result[0].Location.Start.Should().Be(50);
        result[1].Location.Start.Should().Be(30);
        result[2].Location.Start.Should().Be(10);
    }

    #endregion

    #region SortBottomToTopUnfiltered Tests

    [Fact]
    public void SortBottomToTopUnfiltered_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<UnifiedIssue> issues = null!;

        // Act
        var act = () => FixPositionSorter.SortBottomToTopUnfiltered(issues);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SortBottomToTopUnfiltered_IncludesAllIssues()
    {
        // Arrange — includes issues with no fix and with CanAutoApply=false
        var fixableIssue = CreateIssue(start: 10, length: 5, hasFix: true, canAutoApply: true);
        var noFixIssue = CreateIssue(start: 20, length: 5, hasFix: false);
        var nonAutoApplyIssue = CreateIssue(start: 30, length: 5, hasFix: true, canAutoApply: false);
        var issues = new[] { fixableIssue, noFixIssue, nonAutoApplyIssue };

        // Act
        var result = FixPositionSorter.SortBottomToTopUnfiltered(issues);

        // Assert — all three issues should be present (no filtering)
        result.Should().HaveCount(3);
        result.Should().Contain(fixableIssue);
        result.Should().Contain(noFixIssue);
        result.Should().Contain(nonAutoApplyIssue);
    }

    [Fact]
    public void SortBottomToTopUnfiltered_SortsByStartDescending()
    {
        // Arrange
        var issueAt5 = CreateIssue(start: 5, length: 3);
        var issueAt15 = CreateIssue(start: 15, length: 3);
        var issueAt25 = CreateIssue(start: 25, length: 3);
        var issues = new[] { issueAt15, issueAt5, issueAt25 };

        // Act
        var result = FixPositionSorter.SortBottomToTopUnfiltered(issues);

        // Assert
        result.Should().HaveCount(3);
        result[0].Location.Start.Should().Be(25);
        result[1].Location.Start.Should().Be(15);
        result[2].Location.Start.Should().Be(5);
    }

    [Fact]
    public void SortBottomToTopUnfiltered_SingleIssue_ReturnsSingleItem()
    {
        // Arrange
        var singleIssue = CreateIssue(start: 42, length: 10);
        var issues = new[] { singleIssue };

        // Act
        var result = FixPositionSorter.SortBottomToTopUnfiltered(issues);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(singleIssue);
    }

    #endregion
}
