// =============================================================================
// File: ValidationSummaryGeneratorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ValidationSummaryGenerator.
// =============================================================================
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Modules.Knowledge.Validation.Aggregation;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Aggregation;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.5i")]
public class ValidationSummaryGeneratorTests
{
    [Fact]
    public void Generate_EmptyResult_ReturnsZeroCounts()
    {
        // Arrange
        var result = ValidationResult.Valid();

        // Act
        var summary = ValidationSummaryGenerator.Generate(result);

        // Assert
        summary.TotalFindings.Should().Be(0);
        summary.BySeverity.Should().BeEmpty();
        summary.ByValidator.Should().BeEmpty();
        summary.ByCode.Should().BeEmpty();
        summary.FixableFindings.Should().Be(0);
    }

    [Fact]
    public void Generate_MixedSeverities_CountsCorrectly()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Error 1"),
            ValidationFinding.Error("schema", "SCHEMA_002", "Error 2"),
            ValidationFinding.Warn("axiom", "AXIOM_001", "Warning 1"),
            ValidationFinding.Information("consistency", "CONS_001", "Info 1")
        });

        // Act
        var summary = ValidationSummaryGenerator.Generate(result);

        // Assert
        summary.TotalFindings.Should().Be(4);
        summary.BySeverity[ValidationSeverity.Error].Should().Be(2);
        summary.BySeverity[ValidationSeverity.Warning].Should().Be(1);
        summary.BySeverity[ValidationSeverity.Info].Should().Be(1);
    }

    [Fact]
    public void Generate_ByValidator_CountsCorrectly()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Error("schema", "C001", "A"),
            ValidationFinding.Warn("schema", "C002", "B"),
            ValidationFinding.Error("axiom", "C003", "C")
        });

        // Act
        var summary = ValidationSummaryGenerator.Generate(result);

        // Assert
        summary.ByValidator["schema"].Should().Be(2);
        summary.ByValidator["axiom"].Should().Be(1);
    }

    [Fact]
    public void Generate_ByCode_CountsCorrectly()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Error("v1", "SCHEMA_001", "A"),
            ValidationFinding.Error("v2", "SCHEMA_001", "B"),
            ValidationFinding.Warn("v1", "AXIOM_001", "C")
        });

        // Act
        var summary = ValidationSummaryGenerator.Generate(result);

        // Assert
        summary.ByCode["SCHEMA_001"].Should().Be(2);
        summary.ByCode["AXIOM_001"].Should().Be(1);
    }

    [Fact]
    public void Generate_FixableFindings_CountsCorrectly()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Error("v1", "C001", "Error A", suggestedFix: "Fix it"),
            ValidationFinding.Error("v1", "C002", "Error B"),
            ValidationFinding.Warn("v1", "C003", "Warning", suggestedFix: "Consider fixing")
        });

        // Act
        var summary = ValidationSummaryGenerator.Generate(result);

        // Assert
        summary.FixableFindings.Should().Be(2);
    }
}
