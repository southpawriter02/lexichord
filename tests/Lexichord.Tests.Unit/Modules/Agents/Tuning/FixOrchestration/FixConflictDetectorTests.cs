// -----------------------------------------------------------------------
// <copyright file="FixConflictDetectorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// Unit tests for the FixConflictDetector (v0.7.5h).
//   Tests cover constructor validation, conflict detection heuristics
//   (overlapping positions, contradictory suggestions, dependent fixes),
//   and fix location validation against document bounds.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;
using Lexichord.Modules.Agents.Tuning.FixOrchestration;
using Microsoft.Extensions.Logging.Abstractions;

// LOGIC: Use alias to disambiguate from Knowledge.Validation.Integration.UnifiedFix
using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning.FixOrchestration;

/// <summary>
/// Unit tests for <see cref="FixConflictDetector"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>Constructor tests — Verify null argument handling</description></item>
///   <item><description>Detect — null input, empty list, single issue, no-fix filtering</description></item>
///   <item><description>Detect — overlapping positions, non-overlapping, contradictory suggestions</description></item>
///   <item><description>Detect — same-location same-text (no conflict), dependent Style/Grammar fixes</description></item>
///   <item><description>ValidateLocations — null input, valid locations, out-of-bounds locations</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5h")]
public class FixConflictDetectorTests
{
    #region Test Setup

    /// <summary>
    /// Creates a <see cref="FixConflictDetector"/> with a null logger for testing.
    /// </summary>
    private static FixConflictDetector CreateDetector() =>
        new(NullLogger<FixConflictDetector>.Instance);

    /// <summary>
    /// Creates a test <see cref="UnifiedIssue"/> with a replacement fix at the specified location.
    /// </summary>
    /// <param name="start">The start offset for the text span.</param>
    /// <param name="length">The length of the text span.</param>
    /// <param name="newText">The replacement text for the fix.</param>
    /// <param name="category">The issue category.</param>
    /// <returns>A configured <see cref="UnifiedIssue"/> instance with a single replacement fix.</returns>
    private static UnifiedIssue CreateIssue(
        int start, int length,
        string newText = "replacement",
        IssueCategory category = IssueCategory.Grammar)
    {
        var location = new TextSpan(start, length);
        var fix = UnifiedFix.Replacement(location, "old", newText, "Test fix", 0.9);
        return new UnifiedIssue(
            Guid.NewGuid(), "TEST-001", category, UnifiedSeverity.Warning,
            "Test issue", location, "old", new[] { fix }, "TestSource", null);
    }

    /// <summary>
    /// Creates a test <see cref="UnifiedIssue"/> with no available fixes.
    /// </summary>
    /// <param name="start">The start offset for the text span.</param>
    /// <param name="length">The length of the text span.</param>
    /// <returns>A configured <see cref="UnifiedIssue"/> instance with an empty fix list.</returns>
    private static UnifiedIssue CreateIssueWithNoFix(int start = 10, int length = 5)
    {
        var location = new TextSpan(start, length);
        return new UnifiedIssue(
            Guid.NewGuid(), "TEST-002", IssueCategory.Grammar, UnifiedSeverity.Warning,
            "Test issue without fix", location, "old",
            Array.Empty<UnifiedFix>(), "TestSource", null);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        Microsoft.Extensions.Logging.ILogger<FixConflictDetector> logger = null!;

        // Act
        var act = () => new FixConflictDetector(logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Detect Tests

    [Fact]
    public void Detect_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var detector = CreateDetector();
        IReadOnlyList<UnifiedIssue> issues = null!;

        // Act
        var act = () => detector.Detect(issues);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("issues");
    }

    [Fact]
    public void Detect_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var detector = CreateDetector();
        var issues = Array.Empty<UnifiedIssue>();

        // Act
        var result = detector.Detect(issues);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Detect_SingleIssue_ReturnsEmptyList()
    {
        // Arrange — fewer than 2 fixable issues should short-circuit
        var detector = CreateDetector();
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(start: 10, length: 5)
        };

        // Act
        var result = detector.Detect(issues);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Detect_IssuesWithNoFixes_ReturnsEmptyList()
    {
        // Arrange — issues without BestFix are filtered out, leaving <2 fixable issues
        var detector = CreateDetector();
        var issues = new List<UnifiedIssue>
        {
            CreateIssueWithNoFix(start: 10, length: 5),
            CreateIssueWithNoFix(start: 20, length: 5),
            CreateIssueWithNoFix(start: 30, length: 5)
        };

        // Act
        var result = detector.Detect(issues);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Detect_OverlappingPositions_ReturnsError()
    {
        // Arrange — two fixes with overlapping TextSpans: [10,20) and [15,25)
        var detector = CreateDetector();
        var issue1 = CreateIssue(start: 10, length: 10);
        var issue2 = CreateIssue(start: 15, length: 10);
        var issues = new List<UnifiedIssue> { issue1, issue2 };

        // Act
        var result = detector.Detect(issues);

        // Assert
        result.Should().ContainSingle();
        var conflict = result[0];
        conflict.Type.Should().Be(FixConflictType.OverlappingPositions);
        conflict.Severity.Should().Be(FixConflictSeverity.Error);
        conflict.ConflictingIssueIds.Should().HaveCount(2);
        conflict.ConflictingIssueIds.Should().Contain(issue1.IssueId);
        conflict.ConflictingIssueIds.Should().Contain(issue2.IssueId);
        conflict.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Detect_NonOverlapping_ReturnsEmpty()
    {
        // Arrange — two fixes that don't overlap: [10,15) and [20,25)
        var detector = CreateDetector();
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(start: 10, length: 5),
            CreateIssue(start: 20, length: 5)
        };

        // Act
        var result = detector.Detect(issues);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Detect_ContradictorySuggestions_ReturnsError()
    {
        // Arrange — same location [10,15), different replacement text
        var detector = CreateDetector();
        var issue1 = CreateIssue(start: 10, length: 5, newText: "alpha");
        var issue2 = CreateIssue(start: 10, length: 5, newText: "beta");
        var issues = new List<UnifiedIssue> { issue1, issue2 };

        // Act
        var result = detector.Detect(issues);

        // Assert — should contain an overlapping conflict AND a contradictory suggestion conflict
        var contradictory = result
            .Where(c => c.Type == FixConflictType.ContradictorySuggestions)
            .ToList();
        contradictory.Should().ContainSingle();
        contradictory[0].Severity.Should().Be(FixConflictSeverity.Error);
        contradictory[0].ConflictingIssueIds.Should().Contain(issue1.IssueId);
        contradictory[0].ConflictingIssueIds.Should().Contain(issue2.IssueId);
        contradictory[0].Description.Should().Contain("alpha");
        contradictory[0].Description.Should().Contain("beta");
    }

    [Fact]
    public void Detect_SameLocationSameText_NoConflict()
    {
        // Arrange — same location [10,15), same replacement text (not contradictory)
        var detector = CreateDetector();
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(start: 10, length: 5, newText: "same"),
            CreateIssue(start: 10, length: 5, newText: "same")
        };

        // Act
        var result = detector.Detect(issues);

        // Assert — overlapping conflict still expected, but NOT contradictory
        var contradictory = result
            .Where(c => c.Type == FixConflictType.ContradictorySuggestions)
            .ToList();
        contradictory.Should().BeEmpty();
    }

    [Fact]
    public void Detect_DependentStyleGrammar_ReturnsWarning()
    {
        // Arrange — Style and Grammar fixes within 200 chars proximity
        var detector = CreateDetector();
        var styleIssue = CreateIssue(start: 100, length: 5, category: IssueCategory.Style);
        var grammarIssue = CreateIssue(start: 250, length: 5, category: IssueCategory.Grammar);
        var issues = new List<UnifiedIssue> { styleIssue, grammarIssue };

        // Act
        var result = detector.Detect(issues);

        // Assert — distance is |100 - 250| = 150 < 200, so dependent fix detected
        var dependent = result
            .Where(c => c.Type == FixConflictType.DependentFixes)
            .ToList();
        dependent.Should().ContainSingle();
        dependent[0].Severity.Should().Be(FixConflictSeverity.Warning);
        dependent[0].ConflictingIssueIds.Should().Contain(styleIssue.IssueId);
        dependent[0].ConflictingIssueIds.Should().Contain(grammarIssue.IssueId);
        dependent[0].SuggestedResolution.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Detect_StyleGrammarFarApart_NoConflict()
    {
        // Arrange — Style and Grammar fixes >200 chars apart
        var detector = CreateDetector();
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(start: 10, length: 5, category: IssueCategory.Style),
            CreateIssue(start: 500, length: 5, category: IssueCategory.Grammar)
        };

        // Act
        var result = detector.Detect(issues);

        // Assert — distance is |10 - 500| = 490 >= 200, no dependent fix conflict
        var dependent = result
            .Where(c => c.Type == FixConflictType.DependentFixes)
            .ToList();
        dependent.Should().BeEmpty();
    }

    #endregion

    #region ValidateLocations Tests

    [Fact]
    public void ValidateLocations_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var detector = CreateDetector();
        IReadOnlyList<UnifiedIssue> issues = null!;

        // Act
        var act = () => detector.ValidateLocations(issues, documentLength: 1000);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("issues");
    }

    [Fact]
    public void ValidateLocations_ValidLocations_ReturnsEmpty()
    {
        // Arrange — fix location [10,15) is within document length 1000
        var detector = CreateDetector();
        var issues = new List<UnifiedIssue>
        {
            CreateIssue(start: 10, length: 5)
        };

        // Act
        var result = detector.ValidateLocations(issues, documentLength: 1000);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateLocations_FixBeyondDocumentLength_ReturnsError()
    {
        // Arrange — fix location [90,100) exceeds document length of 95
        var detector = CreateDetector();
        var issue = CreateIssue(start: 90, length: 10);
        var issues = new List<UnifiedIssue> { issue };

        // Act
        var result = detector.ValidateLocations(issues, documentLength: 95);

        // Assert
        result.Should().ContainSingle();
        var conflict = result[0];
        conflict.Type.Should().Be(FixConflictType.InvalidLocation);
        conflict.Severity.Should().Be(FixConflictSeverity.Error);
        conflict.ConflictingIssueIds.Should().ContainSingle()
            .Which.Should().Be(issue.IssueId);
        conflict.Description.Should().Contain("95");
    }

    #endregion
}
