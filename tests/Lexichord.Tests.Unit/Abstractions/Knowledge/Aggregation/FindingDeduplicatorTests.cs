// =============================================================================
// File: FindingDeduplicatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for FindingDeduplicator.
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
public class FindingDeduplicatorTests
{
    private readonly FindingDeduplicator _deduplicator = new();

    // =========================================================================
    // Deduplicate tests
    // =========================================================================

    [Fact]
    public void Deduplicate_EmptyInput_ReturnsEmpty()
    {
        // Arrange
        var findings = Array.Empty<ValidationFinding>();

        // Act
        var result = _deduplicator.Deduplicate(findings);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Deduplicate_NoDuplicates_ReturnsAll()
    {
        // Arrange
        var findings = new[]
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title"),
            ValidationFinding.Warn("axiom", "AXIOM_001", "Weak evidence"),
            ValidationFinding.Information("consistency", "CONS_001", "Redundant claim")
        };

        // Act
        var result = _deduplicator.Deduplicate(findings);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void Deduplicate_ExactDuplicates_RetainsFirst()
    {
        // Arrange — same code, same validator, same message
        var findings = new[]
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title"),
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title"),
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title")
        };

        // Act
        var result = _deduplicator.Deduplicate(findings);

        // Assert
        result.Should().HaveCount(1);
        result[0].Message.Should().Be("Missing title");
    }

    [Fact]
    public void Deduplicate_SameCodeAndValidatorWithSamePropertyPath_AreDeduped()
    {
        // Arrange
        var findings = new[]
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title", "metadata.title"),
            ValidationFinding.Error("schema", "SCHEMA_001", "Title is required", "metadata.title")
        };

        // Act
        var result = _deduplicator.Deduplicate(findings);

        // Assert — same code + validator + propertyPath → duplicate
        result.Should().HaveCount(1);
    }

    [Fact]
    public void Deduplicate_DifferentCodes_NotDuplicates()
    {
        // Arrange
        var findings = new[]
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title"),
            ValidationFinding.Error("schema", "SCHEMA_002", "Missing title")
        };

        // Act
        var result = _deduplicator.Deduplicate(findings);

        // Assert — different codes, not duplicates
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Deduplicate_DifferentValidators_NotDuplicates()
    {
        // Arrange
        var findings = new[]
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Conflict detected"),
            ValidationFinding.Error("consistency", "SCHEMA_001", "Conflict detected")
        };

        // Act
        var result = _deduplicator.Deduplicate(findings);

        // Assert — different validators, not duplicates
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Deduplicate_ContainmentMessages_AreDuplicates()
    {
        // Arrange — one message contains the other
        var findings = new[]
        {
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title"),
            ValidationFinding.Error("schema", "SCHEMA_001", "Missing title in metadata section")
        };

        // Act
        var result = _deduplicator.Deduplicate(findings);

        // Assert — "Missing title" is contained in the second messages
        result.Should().HaveCount(1);
    }

    // =========================================================================
    // AreDuplicates tests
    // =========================================================================

    [Fact]
    public void AreDuplicates_SameCodeValidatorAndPropertyPath_ReturnsTrue()
    {
        // Arrange
        var a = ValidationFinding.Error("schema", "SCHEMA_001", "Error A", "metadata.title");
        var b = ValidationFinding.Error("schema", "SCHEMA_001", "Error B", "metadata.title");

        // Act & Assert
        _deduplicator.AreDuplicates(a, b).Should().BeTrue();
    }

    [Fact]
    public void AreDuplicates_SameCodeValidatorDifferentMessages_ReturnsFalse()
    {
        // Arrange — different messages with no containment, no shared PropertyPath
        var a = ValidationFinding.Error("schema", "SCHEMA_001", "Alpha error");
        var b = ValidationFinding.Error("schema", "SCHEMA_001", "Beta issue");

        // Act & Assert — no propertyPath, messages are not similar
        _deduplicator.AreDuplicates(a, b).Should().BeFalse();
    }

    [Fact]
    public void AreDuplicates_CaseInsensitiveMessages_ReturnsTrue()
    {
        // Arrange
        var a = ValidationFinding.Error("schema", "SCHEMA_001", "MISSING TITLE");
        var b = ValidationFinding.Error("schema", "SCHEMA_001", "missing title");

        // Act & Assert
        _deduplicator.AreDuplicates(a, b).Should().BeTrue();
    }
}
