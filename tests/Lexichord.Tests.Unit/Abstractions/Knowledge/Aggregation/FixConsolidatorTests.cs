// =============================================================================
// File: FixConsolidatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for FixConsolidator.
// =============================================================================
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;
using Lexichord.Modules.Knowledge.Validation.Aggregation;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Aggregation;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.5i")]
public class FixConsolidatorTests
{
    private readonly FixConsolidator _consolidator = new();

    // =========================================================================
    // ConsolidateFixes tests
    // =========================================================================

    [Fact]
    public void ConsolidateFixes_NoFindings_ReturnsEmpty()
    {
        // Arrange
        var findings = Array.Empty<ValidationFinding>().ToList();

        // Act
        var result = _consolidator.ConsolidateFixes(findings);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConsolidateFixes_NoFixableFindings_ReturnsEmpty()
    {
        // Arrange — findings with no SuggestedFix
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title"),
            ValidationFinding.Warn("axiom", "AXIOM_001", "Weak evidence")
        };

        // Act
        var result = _consolidator.ConsolidateFixes(findings);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConsolidateFixes_SameFixText_GroupedIntoOne()
    {
        // Arrange — two findings with the same SuggestedFix
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title", suggestedFix: "Add a title"),
            ValidationFinding.Error("schema", "SCHEMA_002", "Title required", suggestedFix: "Add a title")
        };

        // Act
        var result = _consolidator.ConsolidateFixes(findings);

        // Assert
        result.Should().HaveCount(1);
        result[0].AffectedFindings.Should().HaveCount(2);
        result[0].Description.Should().Be("Add a title");
        result[0].FindingsResolved.Should().Be(2);
    }

    [Fact]
    public void ConsolidateFixes_DifferentFixTexts_SeparateConsolidatedFixes()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title", suggestedFix: "Add a title"),
            ValidationFinding.Warn("axiom", "AXIOM_001", "Weak evidence", suggestedFix: "Strengthen citation")
        };

        // Act
        var result = _consolidator.ConsolidateFixes(findings);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void ConsolidateFixes_OrderedByFindingsResolvedDescending()
    {
        // Arrange — first fix resolves 2 findings, second resolves 1
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Error A", suggestedFix: "Fix A"),
            ValidationFinding.Error("schema", "SCHEMA_002", "Error B", suggestedFix: "Fix A"),
            ValidationFinding.Warn("axiom", "AXIOM_001", "Warning C", suggestedFix: "Fix B")
        };

        // Act
        var result = _consolidator.ConsolidateFixes(findings);

        // Assert — "Fix A" resolves 2, should come first
        result.Should().HaveCount(2);
        result[0].FindingsResolved.Should().Be(2);
        result[1].FindingsResolved.Should().Be(1);
    }

    [Fact]
    public void ConsolidateFixes_DefaultsConfidenceAndCanAutoApply()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Error", suggestedFix: "Apply fix")
        };

        // Act
        var result = _consolidator.ConsolidateFixes(findings);

        // Assert
        result.Should().HaveCount(1);
        result[0].Confidence.Should().Be(1.0f);
        result[0].CanAutoApply.Should().BeTrue();
        result[0].Edits.Should().BeEmpty();
    }

    // =========================================================================
    // CreateFixAllAction tests
    // =========================================================================

    [Fact]
    public void CreateFixAllAction_EmptyFixes_ReturnsNoFixesAction()
    {
        // Arrange
        var fixes = Array.Empty<ConsolidatedFix>().ToList();

        // Act
        var action = _consolidator.CreateFixAllAction(fixes);

        // Assert
        action.Description.Should().Contain("No fixes");
        action.Fixes.Should().BeEmpty();
        action.TotalFindingsResolved.Should().Be(0);
        action.AllAutoApplicable.Should().BeTrue();
    }

    [Fact]
    public void CreateFixAllAction_AllAutoApplicable_NoWarnings()
    {
        // Arrange
        var fixes = new List<ConsolidatedFix>
        {
            new()
            {
                Description = "Fix A",
                AffectedFindings = new List<ValidationFinding>
                {
                    ValidationFinding.Error("schema", "SCHEMA_001", "Error A")
                },
                CanAutoApply = true
            },
            new()
            {
                Description = "Fix B",
                AffectedFindings = new List<ValidationFinding>
                {
                    ValidationFinding.Warn("axiom", "AXIOM_001", "Warning B")
                },
                CanAutoApply = true
            }
        };

        // Act
        var action = _consolidator.CreateFixAllAction(fixes);

        // Assert
        action.Fixes.Should().HaveCount(2);
        action.TotalFindingsResolved.Should().Be(2);
        action.AllAutoApplicable.Should().BeTrue();
        action.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void CreateFixAllAction_MixedApplicability_IncludesWarning()
    {
        // Arrange — one auto-applicable, one manual
        var fixes = new List<ConsolidatedFix>
        {
            new()
            {
                Description = "Fix A",
                AffectedFindings = new List<ValidationFinding>
                {
                    ValidationFinding.Error("schema", "SCHEMA_001", "Error A")
                },
                CanAutoApply = true
            },
            new()
            {
                Description = "Fix B",
                AffectedFindings = new List<ValidationFinding>
                {
                    ValidationFinding.Warn("axiom", "AXIOM_001", "Warning B")
                },
                CanAutoApply = false
            }
        };

        // Act
        var action = _consolidator.CreateFixAllAction(fixes);

        // Assert — only auto-applicable fixes are included
        action.Fixes.Should().HaveCount(1);
        action.TotalFindingsResolved.Should().Be(1);
        action.AllAutoApplicable.Should().BeFalse();
        action.Warnings.Should().ContainSingle()
            .Which.Should().Contain("manual review");
    }
}
