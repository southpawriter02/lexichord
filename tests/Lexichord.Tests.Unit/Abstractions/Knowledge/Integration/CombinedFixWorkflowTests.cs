// =============================================================================
// File: CombinedFixWorkflowTests.cs
// Description: Unit tests for CombinedFixWorkflow (v0.6.5j).
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Modules.Knowledge.Validation.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Integration;

/// <summary>
/// Tests for <see cref="CombinedFixWorkflow"/>.
/// </summary>
/// <remarks>Feature: v0.6.5j — Linter Integration.</remarks>
[Trait("Feature", "v0.6.5j")]
public sealed class CombinedFixWorkflowTests
{
    // =========================================================================
    // Fields
    // =========================================================================

    private readonly CombinedFixWorkflow _workflow;

    // =========================================================================
    // Constructor
    // =========================================================================

    public CombinedFixWorkflowTests()
    {
        var logger = Substitute.For<ILogger<CombinedFixWorkflow>>();
        _workflow = new CombinedFixWorkflow(logger);
    }

    // =========================================================================
    // CheckForConflicts Tests
    // =========================================================================

    [Fact]
    public void CheckForConflicts_EmptyList_ReturnsNoConflicts()
    {
        // Arrange
        var fixes = Array.Empty<UnifiedFix>();

        // Act
        var result = _workflow.CheckForConflicts(fixes);

        // Assert
        result.HasConflicts.Should().BeFalse();
        result.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public void CheckForConflicts_SingleFix_ReturnsNoConflicts()
    {
        // Arrange
        var fixes = new[]
        {
            new UnifiedFix { Source = FindingSource.Validation, Description = "Fix A" }
        };

        // Act
        var result = _workflow.CheckForConflicts(fixes);

        // Assert
        result.HasConflicts.Should().BeFalse();
    }

    [Fact]
    public void CheckForConflicts_DifferentFindingIds_ReturnsNoConflicts()
    {
        // Arrange
        var findingId1 = Guid.NewGuid();
        var findingId2 = Guid.NewGuid();
        var fixes = new[]
        {
            new UnifiedFix { Source = FindingSource.Validation, Description = "Fix A", FindingId = findingId1 },
            new UnifiedFix { Source = FindingSource.StyleLinter, Description = "Fix B", FindingId = findingId2 }
        };

        // Act
        var result = _workflow.CheckForConflicts(fixes);

        // Assert
        result.HasConflicts.Should().BeFalse();
    }

    [Fact]
    public void CheckForConflicts_SameFindingId_ReturnsConflict()
    {
        // Arrange
        var sharedFindingId = Guid.NewGuid();
        var fixes = new[]
        {
            new UnifiedFix { Source = FindingSource.Validation, Description = "Fix A", FindingId = sharedFindingId },
            new UnifiedFix { Source = FindingSource.StyleLinter, Description = "Fix B", FindingId = sharedFindingId }
        };

        // Act
        var result = _workflow.CheckForConflicts(fixes);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.Conflicts.Should().HaveCount(1);
        result.Conflicts[0].FixA.Description.Should().Be("Fix A");
        result.Conflicts[0].FixB.Description.Should().Be("Fix B");
        result.Conflicts[0].Reason.Should().Contain(sharedFindingId.ToString());
    }

    [Fact]
    public void CheckForConflicts_ThreeFixesSameFindingId_ReturnsThreeConflicts()
    {
        // Arrange — 3 fixes on the same finding → 3 pairs (A-B, A-C, B-C)
        var sharedFindingId = Guid.NewGuid();
        var fixes = new[]
        {
            new UnifiedFix { Source = FindingSource.Validation, Description = "Fix A", FindingId = sharedFindingId },
            new UnifiedFix { Source = FindingSource.Validation, Description = "Fix B", FindingId = sharedFindingId },
            new UnifiedFix { Source = FindingSource.StyleLinter, Description = "Fix C", FindingId = sharedFindingId }
        };

        // Act
        var result = _workflow.CheckForConflicts(fixes);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.Conflicts.Should().HaveCount(3);
    }

    // =========================================================================
    // OrderFixesForApplication Tests
    // =========================================================================

    [Fact]
    public void OrderFixesForApplication_EmptyList_ReturnsEmpty()
    {
        // Act
        var result = _workflow.OrderFixesForApplication([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void OrderFixesForApplication_SingleFix_ReturnsSameList()
    {
        // Arrange
        var fixes = new List<UnifiedFix>
        {
            new() { Source = FindingSource.Validation, Description = "Only fix" }
        };

        // Act
        var result = _workflow.OrderFixesForApplication(fixes);

        // Assert
        result.Should().HaveCount(1);
        result[0].Description.Should().Be("Only fix");
    }

    [Fact]
    public void OrderFixesForApplication_MultipleFixes_OrdersByFindingId()
    {
        // Arrange — create fixes with known FindingIds out of order
        var id1 = new Guid("00000000-0000-0000-0000-000000000001");
        var id2 = new Guid("00000000-0000-0000-0000-000000000002");
        var id3 = new Guid("00000000-0000-0000-0000-000000000003");
        var fixes = new List<UnifiedFix>
        {
            new() { Source = FindingSource.Validation, Description = "Fix C", FindingId = id3 },
            new() { Source = FindingSource.StyleLinter, Description = "Fix A", FindingId = id1 },
            new() { Source = FindingSource.Validation, Description = "Fix B", FindingId = id2 }
        };

        // Act
        var result = _workflow.OrderFixesForApplication(fixes);

        // Assert — should be ordered by FindingId ascending
        result[0].FindingId.Should().Be(id1);
        result[1].FindingId.Should().Be(id2);
        result[2].FindingId.Should().Be(id3);
    }
}
