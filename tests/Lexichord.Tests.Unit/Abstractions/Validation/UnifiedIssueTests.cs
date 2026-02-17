// -----------------------------------------------------------------------
// <copyright file="UnifiedIssueTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;

// Alias to disambiguate from Integration.UnifiedFix
using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Tests.Unit.Abstractions.Validation;

/// <summary>
/// Unit tests for <see cref="UnifiedIssue"/> and <see cref="UnifiedFix"/> records.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>UnifiedIssue record — Constructor, properties, computed properties</description></item>
///   <item><description>UnifiedFix record — Constructor, factory methods, computed properties</description></item>
///   <item><description>IssueCategory enum — Value verification</description></item>
///   <item><description>FixType enum — Value verification</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5e")]
public class UnifiedIssueTests
{
    #region IssueCategory Enum Tests

    /// <summary>
    /// Verifies that IssueCategory enum has the expected values.
    /// </summary>
    [Fact]
    public void IssueCategory_HasExpectedValues()
    {
        // Assert
        ((int)IssueCategory.Style).Should().Be(0);
        ((int)IssueCategory.Grammar).Should().Be(1);
        ((int)IssueCategory.Knowledge).Should().Be(2);
        ((int)IssueCategory.Structure).Should().Be(3);
        ((int)IssueCategory.Custom).Should().Be(4);
    }

    /// <summary>
    /// Verifies that IssueCategory default value is Style.
    /// </summary>
    [Fact]
    public void IssueCategory_DefaultValue_IsStyle()
    {
        // Arrange & Act
        var defaultValue = default(IssueCategory);

        // Assert
        defaultValue.Should().Be(IssueCategory.Style);
    }

    #endregion

    #region FixType Enum Tests

    /// <summary>
    /// Verifies that FixType enum has the expected values.
    /// </summary>
    [Fact]
    public void FixType_HasExpectedValues()
    {
        // Assert
        ((int)FixType.Replacement).Should().Be(0);
        ((int)FixType.Insertion).Should().Be(1);
        ((int)FixType.Deletion).Should().Be(2);
        ((int)FixType.Rewrite).Should().Be(3);
        ((int)FixType.NoFix).Should().Be(4);
    }

    /// <summary>
    /// Verifies that FixType default value is Replacement.
    /// </summary>
    [Fact]
    public void FixType_DefaultValue_IsReplacement()
    {
        // Arrange & Act
        var defaultValue = default(FixType);

        // Assert
        defaultValue.Should().Be(FixType.Replacement);
    }

    #endregion

    #region UnifiedFix Record Tests

    /// <summary>
    /// Verifies that UnifiedFix.Replacement creates a replacement fix correctly.
    /// </summary>
    [Fact]
    public void UnifiedFix_Replacement_CreatesCorrectFix()
    {
        // Arrange
        var location = new TextSpan(10, 5);
        var oldText = "hello";
        var newText = "Hello";
        var description = "Capitalize first letter";

        // Act
        var fix = UnifiedFix.Replacement(location, oldText, newText, description, 0.9);

        // Assert
        fix.FixId.Should().NotBeEmpty();
        fix.Location.Should().Be(location);
        fix.OldText.Should().Be(oldText);
        fix.NewText.Should().Be(newText);
        fix.Type.Should().Be(FixType.Replacement);
        fix.Description.Should().Be(description);
        fix.Confidence.Should().Be(0.9);
        fix.CanAutoApply.Should().BeTrue(); // 0.9 >= 0.7
    }

    /// <summary>
    /// Verifies that UnifiedFix.Insertion creates an insertion fix correctly.
    /// </summary>
    [Fact]
    public void UnifiedFix_Insertion_CreatesCorrectFix()
    {
        // Arrange
        var insertionPoint = 20;
        var textToInsert = "missing ";
        var description = "Add missing word";

        // Act
        var fix = UnifiedFix.Insertion(insertionPoint, textToInsert, description, 0.85);

        // Assert
        fix.FixId.Should().NotBeEmpty();
        fix.Location.Start.Should().Be(insertionPoint);
        fix.Location.Length.Should().Be(0);
        fix.OldText.Should().BeNull();
        fix.NewText.Should().Be(textToInsert);
        fix.Type.Should().Be(FixType.Insertion);
        fix.Confidence.Should().Be(0.85);
    }

    /// <summary>
    /// Verifies that UnifiedFix.Deletion creates a deletion fix correctly.
    /// </summary>
    [Fact]
    public void UnifiedFix_Deletion_CreatesCorrectFix()
    {
        // Arrange
        var location = new TextSpan(30, 10);
        var textToDelete = "extra text";
        var description = "Remove extra text";

        // Act
        var fix = UnifiedFix.Deletion(location, textToDelete, description, 0.75);

        // Assert
        fix.FixId.Should().NotBeEmpty();
        fix.Location.Should().Be(location);
        fix.OldText.Should().Be(textToDelete);
        fix.NewText.Should().BeNull();
        fix.Type.Should().Be(FixType.Deletion);
        fix.Confidence.Should().Be(0.75);
        fix.CanAutoApply.Should().BeTrue(); // 0.75 >= 0.7
    }

    /// <summary>
    /// Verifies that UnifiedFix.Rewrite creates a rewrite fix correctly.
    /// </summary>
    [Fact]
    public void UnifiedFix_Rewrite_CreatesCorrectFix()
    {
        // Arrange
        var location = new TextSpan(0, 100);
        var originalText = "The quick brown fox jumps.";
        var rewrittenText = "A swift brown fox leaps gracefully.";
        var description = "AI-generated rewrite";

        // Act
        var fix = UnifiedFix.Rewrite(location, originalText, rewrittenText, description, 0.8);

        // Assert
        fix.FixId.Should().NotBeEmpty();
        fix.Type.Should().Be(FixType.Rewrite);
        fix.CanAutoApply.Should().BeFalse(); // Rewrites always require review
        fix.OldText.Should().Be(originalText);
        fix.NewText.Should().Be(rewrittenText);
    }

    /// <summary>
    /// Verifies that UnifiedFix.NoFixAvailable creates a NoFix correctly.
    /// </summary>
    [Fact]
    public void UnifiedFix_NoFixAvailable_CreatesCorrectFix()
    {
        // Arrange
        var location = new TextSpan(50, 25);
        var originalText = "problematic text";
        var reason = "Requires manual restructuring";

        // Act
        var fix = UnifiedFix.NoFixAvailable(location, originalText, reason);

        // Assert
        fix.Type.Should().Be(FixType.NoFix);
        fix.Description.Should().Be(reason);
        fix.Confidence.Should().Be(0.0);
        fix.CanAutoApply.Should().BeFalse();
        fix.NewText.Should().BeNull();
    }

    /// <summary>
    /// Verifies that IsHighConfidence returns true for confidence >= 0.8.
    /// </summary>
    [Theory]
    [InlineData(0.8, true)]
    [InlineData(0.9, true)]
    [InlineData(1.0, true)]
    [InlineData(0.79, false)]
    [InlineData(0.5, false)]
    public void UnifiedFix_IsHighConfidence_ReturnsCorrectValue(double confidence, bool expected)
    {
        // Arrange
        var fix = UnifiedFix.Replacement(
            new TextSpan(0, 5), "old", "new", "test", confidence);

        // Act & Assert
        fix.IsHighConfidence.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that HasChanges correctly identifies when changes exist.
    /// </summary>
    [Fact]
    public void UnifiedFix_HasChanges_ReturnsFalseForNoFix()
    {
        // Arrange
        var fix = UnifiedFix.NoFixAvailable(new TextSpan(0, 5), "text", "reason");

        // Act & Assert
        fix.HasChanges.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that HasChanges returns false when old and new text are the same.
    /// </summary>
    [Fact]
    public void UnifiedFix_HasChanges_ReturnsFalseWhenNoActualChange()
    {
        // Arrange
        var fix = new UnifiedFix(
            Guid.NewGuid(),
            new TextSpan(0, 5),
            "same",
            "same",
            FixType.Replacement,
            "No actual change",
            0.8,
            true);

        // Act & Assert
        fix.HasChanges.Should().BeFalse();
    }

    #endregion

    #region UnifiedIssue Record Tests

    /// <summary>
    /// Verifies that UnifiedIssue can be constructed with all properties.
    /// </summary>
    [Fact]
    public void UnifiedIssue_Constructor_SetsAllProperties()
    {
        // Arrange
        var issueId = Guid.NewGuid();
        var location = new TextSpan(100, 20);
        var fixes = new[]
        {
            UnifiedFix.Replacement(location, "old", "new", "Fix it", 0.9)
        };

        // Act
        var issue = new UnifiedIssue(
            issueId,
            "RULE001",
            IssueCategory.Style,
            UnifiedSeverity.Warning,
            "Style violation detected",
            location,
            "old",
            fixes,
            "StyleLinter",
            null);

        // Assert
        issue.IssueId.Should().Be(issueId);
        issue.SourceId.Should().Be("RULE001");
        issue.Category.Should().Be(IssueCategory.Style);
        issue.Severity.Should().Be(UnifiedSeverity.Warning);
        issue.Message.Should().Be("Style violation detected");
        issue.Location.Should().Be(location);
        issue.OriginalText.Should().Be("old");
        issue.Fixes.Should().HaveCount(1);
        issue.SourceType.Should().Be("StyleLinter");
        issue.OriginalSource.Should().BeNull();
    }

    /// <summary>
    /// Verifies that HasFixes returns true when fixes exist.
    /// </summary>
    [Fact]
    public void UnifiedIssue_HasFixes_ReturnsTrueWhenFixesExist()
    {
        // Arrange
        var location = new TextSpan(0, 10);
        var fixes = new[] { UnifiedFix.Replacement(location, "a", "b", "fix", 0.8) };
        var issue = CreateIssue(fixes: fixes);

        // Act & Assert
        issue.HasFixes.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that HasFixes returns false when only NoFix fixes exist.
    /// </summary>
    [Fact]
    public void UnifiedIssue_HasFixes_ReturnsFalseWhenOnlyNoFixExists()
    {
        // Arrange
        var location = new TextSpan(0, 10);
        var fixes = new[] { UnifiedFix.NoFixAvailable(location, "text", "reason") };
        var issue = CreateIssue(fixes: fixes);

        // Act & Assert
        issue.HasFixes.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that BestFix returns the highest confidence fix.
    /// </summary>
    [Fact]
    public void UnifiedIssue_BestFix_ReturnsHighestConfidenceFix()
    {
        // Arrange
        var location = new TextSpan(0, 10);
        var fixes = new[]
        {
            UnifiedFix.Replacement(location, "a", "b", "low", 0.5),
            UnifiedFix.Replacement(location, "a", "c", "high", 0.9),
            UnifiedFix.Replacement(location, "a", "d", "medium", 0.7)
        };
        // Fixes should be sorted by confidence (highest first)
        var sortedFixes = fixes.OrderByDescending(f => f.Confidence).ToList();
        var issue = CreateIssue(fixes: sortedFixes);

        // Act
        var bestFix = issue.BestFix;

        // Assert
        bestFix.Should().NotBeNull();
        bestFix!.Confidence.Should().Be(0.9);
        bestFix.NewText.Should().Be("c");
    }

    /// <summary>
    /// Verifies that BestFix returns null when no fixes exist.
    /// </summary>
    [Fact]
    public void UnifiedIssue_BestFix_ReturnsNullWhenNoFixes()
    {
        // Arrange
        var issue = CreateIssue(fixes: Array.Empty<UnifiedFix>());

        // Act & Assert
        issue.BestFix.Should().BeNull();
    }

    /// <summary>
    /// Verifies that HasHighConfidenceFix returns correct value.
    /// </summary>
    [Theory]
    [InlineData(0.9, true)]
    [InlineData(0.8, true)]
    [InlineData(0.79, false)]
    public void UnifiedIssue_HasHighConfidenceFix_ReturnsCorrectValue(
        double confidence, bool expected)
    {
        // Arrange
        var location = new TextSpan(0, 10);
        var fixes = new[] { UnifiedFix.Replacement(location, "a", "b", "fix", confidence) };
        var issue = CreateIssue(fixes: fixes);

        // Act & Assert
        issue.HasHighConfidenceFix.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that CanAutoFix returns correct value.
    /// </summary>
    [Fact]
    public void UnifiedIssue_CanAutoFix_ReturnsTrueForAutoApplyableFix()
    {
        // Arrange
        var location = new TextSpan(0, 10);
        var fixes = new[] { UnifiedFix.Replacement(location, "a", "b", "fix", 0.9) };
        var issue = CreateIssue(fixes: fixes);

        // Act & Assert
        issue.CanAutoFix.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CanAutoFix returns false for rewrites.
    /// </summary>
    [Fact]
    public void UnifiedIssue_CanAutoFix_ReturnsFalseForRewrites()
    {
        // Arrange
        var location = new TextSpan(0, 10);
        var fixes = new[] { UnifiedFix.Rewrite(location, "a", "b", "rewrite", 0.9) };
        var issue = CreateIssue(fixes: fixes);

        // Act & Assert
        issue.CanAutoFix.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that SeverityOrder returns correct numeric order.
    /// </summary>
    [Theory]
    [InlineData(UnifiedSeverity.Error, 0)]
    [InlineData(UnifiedSeverity.Warning, 1)]
    [InlineData(UnifiedSeverity.Info, 2)]
    [InlineData(UnifiedSeverity.Hint, 3)]
    public void UnifiedIssue_SeverityOrder_ReturnsCorrectOrder(
        UnifiedSeverity severity, int expectedOrder)
    {
        // Arrange
        var issue = CreateIssue(severity: severity);

        // Act & Assert
        issue.SeverityOrder.Should().Be(expectedOrder);
    }

    /// <summary>
    /// Verifies source type properties return correct values.
    /// </summary>
    [Theory]
    [InlineData("StyleLinter", true, false, false)]
    [InlineData("GrammarLinter", false, true, false)]
    [InlineData("Validation", false, false, true)]
    public void UnifiedIssue_SourceTypeProperties_ReturnCorrectValues(
        string sourceType, bool isStyle, bool isGrammar, bool isValidation)
    {
        // Arrange
        var issue = CreateIssue(sourceType: sourceType);

        // Act & Assert
        issue.IsFromStyleLinter.Should().Be(isStyle);
        issue.IsFromGrammarLinter.Should().Be(isGrammar);
        issue.IsFromValidation.Should().Be(isValidation);
    }

    /// <summary>
    /// Verifies WithFixes creates a copy with new fixes.
    /// </summary>
    [Fact]
    public void UnifiedIssue_WithFixes_CreatesUpdatedCopy()
    {
        // Arrange
        var originalIssue = CreateIssue(fixes: Array.Empty<UnifiedFix>());
        var newFixes = new[]
        {
            UnifiedFix.Replacement(new TextSpan(0, 5), "a", "b", "new fix", 0.9)
        };

        // Act
        var updatedIssue = originalIssue.WithFixes(newFixes);

        // Assert
        originalIssue.Fixes.Should().BeEmpty();
        updatedIssue.Fixes.Should().HaveCount(1);
        updatedIssue.IssueId.Should().Be(originalIssue.IssueId);
    }

    /// <summary>
    /// Verifies WithAdditionalFix adds a fix and maintains sort order.
    /// </summary>
    [Fact]
    public void UnifiedIssue_WithAdditionalFix_AddsSortedFix()
    {
        // Arrange
        var location = new TextSpan(0, 5);
        var initialFix = UnifiedFix.Replacement(location, "a", "b", "first", 0.6);
        var originalIssue = CreateIssue(fixes: new[] { initialFix });
        var additionalFix = UnifiedFix.Replacement(location, "a", "c", "second", 0.9);

        // Act
        var updatedIssue = originalIssue.WithAdditionalFix(additionalFix);

        // Assert
        updatedIssue.Fixes.Should().HaveCount(2);
        updatedIssue.Fixes[0].Confidence.Should().Be(0.9); // Higher confidence first
        updatedIssue.Fixes[1].Confidence.Should().Be(0.6);
    }

    /// <summary>
    /// Verifies that UnifiedIssue.Empty returns a valid empty instance.
    /// </summary>
    [Fact]
    public void UnifiedIssue_Empty_ReturnsValidEmptyInstance()
    {
        // Act
        var empty = UnifiedIssue.Empty;

        // Assert
        empty.IssueId.Should().Be(Guid.Empty);
        empty.SourceId.Should().BeEmpty();
        empty.Category.Should().Be(IssueCategory.Custom);
        empty.Severity.Should().Be(UnifiedSeverity.Info);
        empty.Message.Should().BeEmpty();
        empty.Location.Should().Be(TextSpan.Empty);
        empty.OriginalText.Should().BeNull();
        empty.Fixes.Should().BeEmpty();
        empty.SourceType.Should().BeNull();
        empty.OriginalSource.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test UnifiedIssue with optional overrides.
    /// </summary>
    private static UnifiedIssue CreateIssue(
        Guid? issueId = null,
        string sourceId = "TEST001",
        IssueCategory category = IssueCategory.Style,
        UnifiedSeverity severity = UnifiedSeverity.Warning,
        string message = "Test issue",
        TextSpan? location = null,
        string? originalText = "test",
        IReadOnlyList<UnifiedFix>? fixes = null,
        string? sourceType = "StyleLinter",
        object? originalSource = null)
    {
        return new UnifiedIssue(
            issueId ?? Guid.NewGuid(),
            sourceId,
            category,
            severity,
            message,
            location ?? new TextSpan(0, 10),
            originalText,
            fixes ?? Array.Empty<UnifiedFix>(),
            sourceType,
            originalSource);
    }

    #endregion
}
