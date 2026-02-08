// =============================================================================
// File: ConsistencyFindingCodesTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ConsistencyFindingCodes constants.
// =============================================================================
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Consistency;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.5h")]
public class ConsistencyFindingCodesTests
{
    [Fact]
    public void ConsistencyConflict_HasExpectedValue()
    {
        ConsistencyFindingCodes.ConsistencyConflict.Should().Be("CONSISTENCY_CONFLICT");
    }

    [Fact]
    public void ValueContradiction_HasExpectedValue()
    {
        ConsistencyFindingCodes.ValueContradiction.Should().Be("CONSISTENCY_VALUE_CONTRADICTION");
    }

    [Fact]
    public void PropertyConflict_HasExpectedValue()
    {
        ConsistencyFindingCodes.PropertyConflict.Should().Be("CONSISTENCY_PROPERTY_CONFLICT");
    }

    [Fact]
    public void RelationshipConflict_HasExpectedValue()
    {
        ConsistencyFindingCodes.RelationshipConflict.Should().Be("CONSISTENCY_RELATIONSHIP_CONFLICT");
    }

    [Fact]
    public void TemporalConflict_HasExpectedValue()
    {
        ConsistencyFindingCodes.TemporalConflict.Should().Be("CONSISTENCY_TEMPORAL_CONFLICT");
    }

    [Fact]
    public void SemanticConflict_HasExpectedValue()
    {
        ConsistencyFindingCodes.SemanticConflict.Should().Be("CONSISTENCY_SEMANTIC_CONFLICT");
    }

    [Fact]
    public void DuplicateClaim_HasExpectedValue()
    {
        ConsistencyFindingCodes.DuplicateClaim.Should().Be("CONSISTENCY_DUPLICATE");
    }

    [Fact]
    public void AllCodes_UseConsistencyPrefix()
    {
        // Verify all codes follow the CONSISTENCY_ prefix convention
        ConsistencyFindingCodes.ConsistencyConflict.Should().StartWith("CONSISTENCY_");
        ConsistencyFindingCodes.ValueContradiction.Should().StartWith("CONSISTENCY_");
        ConsistencyFindingCodes.PropertyConflict.Should().StartWith("CONSISTENCY_");
        ConsistencyFindingCodes.RelationshipConflict.Should().StartWith("CONSISTENCY_");
        ConsistencyFindingCodes.TemporalConflict.Should().StartWith("CONSISTENCY_");
        ConsistencyFindingCodes.SemanticConflict.Should().StartWith("CONSISTENCY_");
        ConsistencyFindingCodes.DuplicateClaim.Should().StartWith("CONSISTENCY_");
    }
}
