// -----------------------------------------------------------------------
// <copyright file="DocumentComparerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.DocumentComparison;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.DocumentComparison;

/// <summary>
/// Unit tests for Document Comparison abstractions and data models.
/// Note: Full service integration tests require the complete DI setup.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6d")]
public class DocumentComparerTests
{
    // ── ComparisonResult Factory Tests ───────────────────────────────────

    [Fact]
    public void ComparisonResult_Failed_CreatesFailureResult()
    {
        // Act
        var result = ComparisonResult.Failed("/original.md", "/new.md", "Test error");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Test error");
        result.Changes.Should().BeEmpty();
    }

    [Fact]
    public void ComparisonResult_Failed_IncludesPaths()
    {
        // Act
        var result = ComparisonResult.Failed(
            "/path/original.md",
            "/path/new.md",
            "Error occurred");

        // Assert
        result.Success.Should().BeFalse();
        result.OriginalPath.Should().Be("/path/original.md");
        result.NewPath.Should().Be("/path/new.md");
    }

    [Fact]
    public void ComparisonResult_Identical_CreatesIdenticalResult()
    {
        // Act
        var result = ComparisonResult.Identical("/original.md", "/new.md", wordCount: 500);

        // Assert
        result.Success.Should().BeTrue();
        result.AreIdentical.Should().BeTrue();
        result.Changes.Should().BeEmpty();
        result.ChangeMagnitude.Should().Be(0);
        result.OriginalWordCount.Should().Be(500);
        result.NewWordCount.Should().Be(500);
    }

    // ── Computed Properties ──────────────────────────────────────────────

    [Fact]
    public void ComparisonResult_AreIdentical_WhenNoChanges_ReturnsTrue()
    {
        // Arrange
        var result = CreateSuccessResult(changes: new List<DocumentChange>(), changeMagnitude: 0.0);

        // Assert
        result.AreIdentical.Should().BeTrue();
    }

    [Fact]
    public void ComparisonResult_AreIdentical_WhenChangesExist_ReturnsFalse()
    {
        // Arrange
        var changes = new List<DocumentChange>
        {
            CreateTestChange(ChangeCategory.Modified)
        };
        var result = CreateSuccessResult(changes: changes, changeMagnitude: 0.3);

        // Assert
        result.AreIdentical.Should().BeFalse();
    }

    [Fact]
    public void ComparisonResult_AdditionCount_CountsAddedChanges()
    {
        // Arrange
        var changes = new List<DocumentChange>
        {
            CreateTestChange(ChangeCategory.Added),
            CreateTestChange(ChangeCategory.Added),
            CreateTestChange(ChangeCategory.Modified)
        };
        var result = CreateSuccessResult(changes: changes);

        // Assert
        result.AdditionCount.Should().Be(2);
    }

    [Fact]
    public void ComparisonResult_DeletionCount_CountsRemovedChanges()
    {
        // Arrange
        var changes = new List<DocumentChange>
        {
            CreateTestChange(ChangeCategory.Removed),
            CreateTestChange(ChangeCategory.Added),
            CreateTestChange(ChangeCategory.Removed)
        };
        var result = CreateSuccessResult(changes: changes);

        // Assert
        result.DeletionCount.Should().Be(2);
    }

    [Fact]
    public void ComparisonResult_ModificationCount_CountsModifiedChanges()
    {
        // Arrange
        var changes = new List<DocumentChange>
        {
            CreateTestChange(ChangeCategory.Modified),
            CreateTestChange(ChangeCategory.Modified),
            CreateTestChange(ChangeCategory.Added)
        };
        var result = CreateSuccessResult(changes: changes);

        // Assert
        result.ModificationCount.Should().Be(2);
    }

    [Theory]
    [InlineData(100, 150, 50)]
    [InlineData(200, 100, -100)]
    [InlineData(100, 100, 0)]
    public void ComparisonResult_WordCountDelta_CalculatesCorrectly(int original, int newCount, int expectedDelta)
    {
        // Arrange
        var result = CreateSuccessResult(
            changes: new List<DocumentChange>(),
            originalWordCount: original,
            newWordCount: newCount);

        // Assert
        result.WordCountDelta.Should().Be(expectedDelta);
    }

    // ── Required Properties ──────────────────────────────────────────────

    [Fact]
    public void ComparisonResult_Usage_CanBeUsageMetricsZero()
    {
        // Arrange
        var result = ComparisonResult.Failed("/original.md", "/new.md", "Error");

        // Assert
        result.Usage.Should().Be(UsageMetrics.Zero);
        result.Usage.PromptTokens.Should().Be(0);
        result.Usage.CompletionTokens.Should().Be(0);
    }

    [Fact]
    public void ComparisonResult_AffectedSections_IsEmpty_ForFailedResult()
    {
        // Arrange
        var result = ComparisonResult.Failed("/original.md", "/new.md", "Error");

        // Assert
        result.AffectedSections.Should().BeEmpty();
    }

    [Fact]
    public void ComparisonResult_ComparedAt_HasValue()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = ComparisonResult.Identical("/original.md", "/new.md", 100);

        var after = DateTimeOffset.UtcNow;

        // Assert
        result.ComparedAt.Should().BeOnOrAfter(before);
        result.ComparedAt.Should().BeOnOrBefore(after);
    }

    // ── Labels ───────────────────────────────────────────────────────────

    [Fact]
    public void ComparisonResult_Labels_CanBeSet()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OriginalPath = "/original.md",
            NewPath = "/new.md",
            OriginalLabel = "v1.0",
            NewLabel = "v2.0",
            Summary = "Test summary",
            Changes = new List<DocumentChange>(),
            AffectedSections = new List<string>(),
            Usage = UsageMetrics.Zero
        };

        // Assert
        result.OriginalLabel.Should().Be("v1.0");
        result.NewLabel.Should().Be("v2.0");
    }

    // ── TextDiff ─────────────────────────────────────────────────────────

    [Fact]
    public void ComparisonResult_TextDiff_CanBeNull()
    {
        // Arrange
        var result = ComparisonResult.Failed("/original.md", "/new.md", "Error");

        // Assert
        result.TextDiff.Should().BeNull();
    }

    [Fact]
    public void ComparisonResult_TextDiff_CanContainDiff()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OriginalPath = "/original.md",
            NewPath = "/new.md",
            Summary = "Changes detected",
            Changes = new List<DocumentChange>(),
            AffectedSections = new List<string>(),
            Usage = UsageMetrics.Zero,
            TextDiff = "- old line\n+ new line"
        };

        // Assert
        result.TextDiff.Should().Be("- old line\n+ new line");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static ComparisonResult CreateSuccessResult(
        IReadOnlyList<DocumentChange>? changes = null,
        double changeMagnitude = 0.3,
        int originalWordCount = 100,
        int newWordCount = 100)
    {
        return new ComparisonResult
        {
            OriginalPath = "/original.md",
            NewPath = "/new.md",
            Summary = "Test summary",
            Changes = changes ?? new List<DocumentChange>(),
            ChangeMagnitude = changeMagnitude,
            OriginalWordCount = originalWordCount,
            NewWordCount = newWordCount,
            AffectedSections = new List<string>(),
            Usage = UsageMetrics.Zero,
            Success = true
        };
    }

    private static DocumentChange CreateTestChange(ChangeCategory category)
    {
        return new DocumentChange
        {
            Category = category,
            Section = "Test",
            Description = "Test change",
            Significance = 0.5
        };
    }
}
