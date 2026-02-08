// =============================================================================
// File: ResultAggregatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ResultAggregator.
// =============================================================================
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;
using Lexichord.Modules.Knowledge.Validation.Aggregation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Aggregation;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.5i")]
public class ResultAggregatorTests
{
    private readonly IFindingDeduplicator _deduplicator;
    private readonly IFixConsolidator _fixConsolidator;
    private readonly ResultAggregator _aggregator;

    public ResultAggregatorTests()
    {
        _deduplicator = Substitute.For<IFindingDeduplicator>();
        _fixConsolidator = Substitute.For<IFixConsolidator>();
        var logger = Substitute.For<ILogger<ResultAggregator>>();

        _aggregator = new ResultAggregator(_deduplicator, _fixConsolidator, logger);
    }

    // =========================================================================
    // Aggregate tests
    // =========================================================================

    [Fact]
    public void Aggregate_EmptyFindings_ReturnsValidResult()
    {
        // Arrange
        var findings = Array.Empty<ValidationFinding>();
        var options = ValidationOptions.Default();
        _deduplicator.Deduplicate(Arg.Any<IEnumerable<ValidationFinding>>())
            .Returns(new List<ValidationFinding>());

        // Act
        var result = _aggregator.Aggregate(findings, TimeSpan.FromMilliseconds(100), options);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Findings.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_CallsDeduplicator()
    {
        // Arrange
        var findings = new[]
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Error A"),
            ValidationFinding.Error("schema", "SCHEMA_001", "Error A")
        };
        var options = ValidationOptions.Default();
        _deduplicator.Deduplicate(Arg.Any<IEnumerable<ValidationFinding>>())
            .Returns(new List<ValidationFinding>
            {
                ValidationFinding.Error("schema", "SCHEMA_001", "Error A")
            });

        // Act
        var result = _aggregator.Aggregate(findings, TimeSpan.FromMilliseconds(100), options);

        // Assert
        _deduplicator.Received(1).Deduplicate(Arg.Any<IEnumerable<ValidationFinding>>());
        result.Findings.Should().HaveCount(1);
    }

    [Fact]
    public void Aggregate_SortsBySeverityDescending()
    {
        // Arrange — Info, Error, Warning (unsorted)
        var findings = new[]
        {
            ValidationFinding.Information("v1", "C001", "Info message"),
            ValidationFinding.Error("v1", "C002", "Error message"),
            ValidationFinding.Warn("v1", "C003", "Warning message")
        };
        var options = ValidationOptions.Default();
        _deduplicator.Deduplicate(Arg.Any<IEnumerable<ValidationFinding>>())
            .Returns(findings.ToList());

        // Act
        var result = _aggregator.Aggregate(findings, TimeSpan.FromMilliseconds(50), options);

        // Assert — Error (2) > Warning (1) > Info (0)
        result.Findings[0].Severity.Should().Be(ValidationSeverity.Error);
        result.Findings[1].Severity.Should().Be(ValidationSeverity.Warning);
        result.Findings[2].Severity.Should().Be(ValidationSeverity.Info);
    }

    [Fact]
    public void Aggregate_LimitsToMaxFindings()
    {
        // Arrange
        var findings = new[]
        {
            ValidationFinding.Error("v1", "C001", "Error 1"),
            ValidationFinding.Error("v1", "C002", "Error 2"),
            ValidationFinding.Warn("v1", "C003", "Warning 1")
        };
        var options = ValidationOptions.Default() with { MaxFindings = 2 };
        _deduplicator.Deduplicate(Arg.Any<IEnumerable<ValidationFinding>>())
            .Returns(findings.ToList());

        // Act
        var result = _aggregator.Aggregate(findings, TimeSpan.FromMilliseconds(50), options);

        // Assert
        result.Findings.Should().HaveCount(2);
    }

    [Fact]
    public void Aggregate_SetsValidatorsRunAndSkipped()
    {
        // Arrange
        var findings = Array.Empty<ValidationFinding>();
        var options = ValidationOptions.Default();
        _deduplicator.Deduplicate(Arg.Any<IEnumerable<ValidationFinding>>())
            .Returns(new List<ValidationFinding>());

        // Act
        var result = _aggregator.Aggregate(
            findings, TimeSpan.FromMilliseconds(100), options,
            validatorsRun: 3, validatorsSkipped: 1);

        // Assert
        result.ValidatorsRun.Should().Be(3);
        result.ValidatorsSkipped.Should().Be(1);
    }

    [Fact]
    public void Aggregate_SetsDuration()
    {
        // Arrange
        var findings = Array.Empty<ValidationFinding>();
        var options = ValidationOptions.Default();
        var duration = TimeSpan.FromSeconds(2.5);
        _deduplicator.Deduplicate(Arg.Any<IEnumerable<ValidationFinding>>())
            .Returns(new List<ValidationFinding>());

        // Act
        var result = _aggregator.Aggregate(findings, duration, options);

        // Assert
        result.Duration.Should().Be(duration);
    }

    // =========================================================================
    // FilterFindings tests
    // =========================================================================

    [Fact]
    public void FilterFindings_MinSeverity_FiltersCorrectly()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Information("v1", "C001", "Info"),
            ValidationFinding.Warn("v1", "C002", "Warning"),
            ValidationFinding.Error("v1", "C003", "Error")
        };
        var filter = new FindingFilter { MinSeverity = ValidationSeverity.Warning };

        // Act
        var result = _aggregator.FilterFindings(findings, filter);

        // Assert — Warning (1) and Error (2), not Info (0)
        result.Should().HaveCount(2);
        result.Should().OnlyContain(f =>
            f.Severity == ValidationSeverity.Warning ||
            f.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void FilterFindings_ByValidatorIds_FiltersCorrectly()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("schema", "C001", "Error A"),
            ValidationFinding.Error("axiom", "C002", "Error B"),
            ValidationFinding.Warn("consistency", "C003", "Warning C")
        };
        var filter = new FindingFilter
        {
            ValidatorIds = new HashSet<string> { "schema", "axiom" }
        };

        // Act
        var result = _aggregator.FilterFindings(findings, filter);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(f =>
            f.ValidatorId == "schema" || f.ValidatorId == "axiom");
    }

    [Fact]
    public void FilterFindings_ByCodes_FiltersCorrectly()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("v1", "SCHEMA_001", "Error A"),
            ValidationFinding.Error("v1", "SCHEMA_002", "Error B"),
            ValidationFinding.Error("v1", "AXIOM_001", "Error C")
        };
        var filter = new FindingFilter
        {
            Codes = new HashSet<string> { "SCHEMA_001" }
        };

        // Act
        var result = _aggregator.FilterFindings(findings, filter);

        // Assert
        result.Should().HaveCount(1);
        result[0].Code.Should().Be("SCHEMA_001");
    }

    [Fact]
    public void FilterFindings_FixableOnly_FiltersCorrectly()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("v1", "C001", "Error A", suggestedFix: "Fix it"),
            ValidationFinding.Error("v1", "C002", "Error B"),
            ValidationFinding.Warn("v1", "C003", "Warning", suggestedFix: "Consider fixing")
        };
        var filter = new FindingFilter { FixableOnly = true };

        // Act
        var result = _aggregator.FilterFindings(findings, filter);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(f => f.SuggestedFix != null);
    }

    [Fact]
    public void FilterFindings_EmptyFilter_ReturnsAll()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("v1", "C001", "Error"),
            ValidationFinding.Warn("v1", "C002", "Warning"),
            ValidationFinding.Information("v1", "C003", "Info")
        };
        var filter = new FindingFilter();

        // Act
        var result = _aggregator.FilterFindings(findings, filter);

        // Assert
        result.Should().HaveCount(3);
    }

    // =========================================================================
    // GroupFindings tests
    // =========================================================================

    [Fact]
    public void GroupFindings_ByValidator_GroupsCorrectly()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("schema", "C001", "Error A"),
            ValidationFinding.Error("schema", "C002", "Error B"),
            ValidationFinding.Warn("axiom", "C003", "Warning C")
        };

        // Act
        var result = _aggregator.GroupFindings(findings, FindingGroupBy.Validator);

        // Assert
        result.Should().HaveCount(2);
        result["schema"].Should().HaveCount(2);
        result["axiom"].Should().HaveCount(1);
    }

    [Fact]
    public void GroupFindings_BySeverity_GroupsCorrectly()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("v1", "C001", "Error"),
            ValidationFinding.Warn("v1", "C002", "Warning 1"),
            ValidationFinding.Warn("v1", "C003", "Warning 2"),
            ValidationFinding.Information("v1", "C004", "Info")
        };

        // Act
        var result = _aggregator.GroupFindings(findings, FindingGroupBy.Severity);

        // Assert
        result.Should().HaveCount(3);
        result["Error"].Should().HaveCount(1);
        result["Warning"].Should().HaveCount(2);
        result["Info"].Should().HaveCount(1);
    }

    [Fact]
    public void GroupFindings_ByCode_GroupsCorrectly()
    {
        // Arrange
        var findings = new List<ValidationFinding>
        {
            ValidationFinding.Error("v1", "SCHEMA_001", "A"),
            ValidationFinding.Error("v2", "SCHEMA_001", "B"),
            ValidationFinding.Warn("v1", "AXIOM_001", "C")
        };

        // Act
        var result = _aggregator.GroupFindings(findings, FindingGroupBy.Code);

        // Assert
        result.Should().HaveCount(2);
        result["SCHEMA_001"].Should().HaveCount(2);
        result["AXIOM_001"].Should().HaveCount(1);
    }
}
