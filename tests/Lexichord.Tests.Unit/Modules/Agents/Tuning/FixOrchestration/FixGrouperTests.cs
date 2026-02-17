// -----------------------------------------------------------------------
// <copyright file="FixGrouperTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// Unit tests for the FixGrouper and CategoryGroup (v0.7.5h).
//   Tests cover category grouping with correct ordering, within-group
//   ordering preservation, flattening, null argument handling, and
//   CategoryGroup.Count property.
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
/// Unit tests for <see cref="FixGrouper"/> and <see cref="CategoryGroup"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>GroupByCategory — null input, empty list, single category, multi-category ordering</description></item>
///   <item><description>GroupByCategory — within-group ordering preservation, all five categories</description></item>
///   <item><description>FlattenGroups — null input, multi-group flattening in order</description></item>
///   <item><description>CategoryGroup — Count property</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5h")]
public class FixGrouperTests
{
    #region Test Setup

    /// <summary>
    /// Creates a test <see cref="UnifiedIssue"/> with the specified category and location.
    /// </summary>
    /// <param name="category">The issue category.</param>
    /// <param name="start">The start offset for the text span.</param>
    /// <param name="length">The length of the text span.</param>
    /// <returns>A configured <see cref="UnifiedIssue"/> instance.</returns>
    private static UnifiedIssue CreateIssue(
        IssueCategory category,
        int start = 10,
        int length = 5)
    {
        var location = new TextSpan(start, length);
        return new UnifiedIssue(
            Guid.NewGuid(), "TEST-001", category, UnifiedSeverity.Warning,
            "Test issue", location, "old",
            new[] { UnifiedFix.Replacement(location, "old", "new", "Test fix", 0.9) },
            "TestSource", null);
    }

    #endregion

    #region GroupByCategory Tests

    [Fact]
    public void GroupByCategory_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<UnifiedIssue> issues = null!;

        // Act
        var act = () => FixGrouper.GroupByCategory(issues);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("issues");
    }

    [Fact]
    public void GroupByCategory_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var issues = Array.Empty<UnifiedIssue>();

        // Act
        var result = FixGrouper.GroupByCategory(issues);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GroupByCategory_SingleCategory_ReturnsSingleGroup()
    {
        // Arrange
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(IssueCategory.Grammar, start: 10),
            CreateIssue(IssueCategory.Grammar, start: 20),
            CreateIssue(IssueCategory.Grammar, start: 30)
        };

        // Act
        var result = FixGrouper.GroupByCategory(issues);

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be(IssueCategory.Grammar);
        result[0].Issues.Should().HaveCount(3);
    }

    [Fact]
    public void GroupByCategory_MultipleCategories_OrderedCorrectly()
    {
        // Arrange — provide issues in non-sorted order to verify ordering
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(IssueCategory.Style, start: 10),
            CreateIssue(IssueCategory.Knowledge, start: 20),
            CreateIssue(IssueCategory.Grammar, start: 30)
        };

        // Act
        var result = FixGrouper.GroupByCategory(issues);

        // Assert — Knowledge (0) before Grammar (2) before Style (3)
        result.Should().HaveCount(3);
        result[0].Category.Should().Be(IssueCategory.Knowledge);
        result[1].Category.Should().Be(IssueCategory.Grammar);
        result[2].Category.Should().Be(IssueCategory.Style);
    }

    [Fact]
    public void GroupByCategory_MaintainsOrderWithinGroup()
    {
        // Arrange — issues within the same category should keep their original order
        var issue1 = CreateIssue(IssueCategory.Grammar, start: 50);
        var issue2 = CreateIssue(IssueCategory.Grammar, start: 30);
        var issue3 = CreateIssue(IssueCategory.Grammar, start: 10);

        var issues = new List<UnifiedIssue> { issue1, issue2, issue3 };

        // Act
        var result = FixGrouper.GroupByCategory(issues);

        // Assert — original insertion order is preserved within the group
        result.Should().HaveCount(1);
        result[0].Issues[0].Location.Start.Should().Be(50);
        result[0].Issues[1].Location.Start.Should().Be(30);
        result[0].Issues[2].Location.Start.Should().Be(10);
    }

    [Fact]
    public void GroupByCategory_AllFiveCategories_CorrectOrder()
    {
        // Arrange — one issue per category, in random order
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(IssueCategory.Custom, start: 50),
            CreateIssue(IssueCategory.Grammar, start: 30),
            CreateIssue(IssueCategory.Knowledge, start: 10),
            CreateIssue(IssueCategory.Style, start: 40),
            CreateIssue(IssueCategory.Structure, start: 20)
        };

        // Act
        var result = FixGrouper.GroupByCategory(issues);

        // Assert — Knowledge(0), Structure(1), Grammar(2), Style(3), Custom(4)
        result.Should().HaveCount(5);
        result[0].Category.Should().Be(IssueCategory.Knowledge);
        result[1].Category.Should().Be(IssueCategory.Structure);
        result[2].Category.Should().Be(IssueCategory.Grammar);
        result[3].Category.Should().Be(IssueCategory.Style);
        result[4].Category.Should().Be(IssueCategory.Custom);
    }

    #endregion

    #region FlattenGroups Tests

    [Fact]
    public void FlattenGroups_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<CategoryGroup> groups = null!;

        // Act
        var act = () => FixGrouper.FlattenGroups(groups);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("groups");
    }

    [Fact]
    public void FlattenGroups_MultipleGroups_FlattensInOrder()
    {
        // Arrange — build groups in the expected application order
        var knowledgeIssue1 = CreateIssue(IssueCategory.Knowledge, start: 50);
        var knowledgeIssue2 = CreateIssue(IssueCategory.Knowledge, start: 10);
        var grammarIssue = CreateIssue(IssueCategory.Grammar, start: 30);
        var styleIssue = CreateIssue(IssueCategory.Style, start: 20);

        var groups = new List<CategoryGroup>
        {
            new(IssueCategory.Knowledge, new List<UnifiedIssue> { knowledgeIssue1, knowledgeIssue2 }),
            new(IssueCategory.Grammar, new List<UnifiedIssue> { grammarIssue }),
            new(IssueCategory.Style, new List<UnifiedIssue> { styleIssue })
        };

        // Act
        var result = FixGrouper.FlattenGroups(groups);

        // Assert — preserves group ordering: Knowledge issues first, then Grammar, then Style
        result.Should().HaveCount(4);
        result[0].Should().BeSameAs(knowledgeIssue1);
        result[1].Should().BeSameAs(knowledgeIssue2);
        result[2].Should().BeSameAs(grammarIssue);
        result[3].Should().BeSameAs(styleIssue);
    }

    #endregion

    #region CategoryGroup Tests

    [Fact]
    public void CategoryGroup_Count_ReturnsIssueCount()
    {
        // Arrange
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(IssueCategory.Style, start: 10),
            CreateIssue(IssueCategory.Style, start: 20),
            CreateIssue(IssueCategory.Style, start: 30)
        };

        var group = new CategoryGroup(IssueCategory.Style, issues);

        // Act
        var count = group.Count;

        // Assert
        count.Should().Be(3);
    }

    #endregion
}
