// =============================================================================
// File: ConflictDetectorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ConflictDetector claim conflict detection.
// =============================================================================
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Modules.Knowledge.Validation.Validators.Consistency;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Consistency;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.5h")]
public class ConflictDetectorTests
{
    private readonly ConflictDetector _detector = new();

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static Claim CreateClaim(
        string surfaceForm,
        string predicate,
        string? literalValue = null,
        ClaimEntity? objectEntity = null,
        Guid? subjectEntityId = null)
    {
        var subjectId = subjectEntityId ?? Guid.NewGuid();
        var subject = new ClaimEntity
        {
            SurfaceForm = surfaceForm,
            EntityType = "Concept",
            NormalizedForm = surfaceForm.ToLowerInvariant(),
            EntityId = subjectId,
            StartOffset = 0,
            EndOffset = surfaceForm.Length
        };

        ClaimObject obj;
        if (objectEntity != null)
        {
            obj = ClaimObject.FromEntity(objectEntity);
        }
        else
        {
            obj = ClaimObject.FromString(literalValue ?? "default");
        }

        return new Claim
        {
            Subject = subject,
            Predicate = predicate,
            Object = obj,
            DocumentId = Guid.NewGuid()
        };
    }

    private static ClaimEntity CreateEntity(string name, Guid? entityId = null)
    {
        return new ClaimEntity
        {
            SurfaceForm = name,
            EntityType = "Concept",
            NormalizedForm = name.ToLowerInvariant(),
            EntityId = entityId ?? Guid.NewGuid(),
            StartOffset = 0,
            EndOffset = name.Length
        };
    }

    // =========================================================================
    // Subject Tests
    // =========================================================================

    [Fact]
    public void DetectConflict_DifferentSubjects_ReturnsNoConflict()
    {
        // Arrange
        var claim1 = CreateClaim("EndpointA", "HAS_DEFAULT", literalValue: "10");
        var claim2 = CreateClaim("EndpointB", "HAS_DEFAULT", literalValue: "20");

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeFalse();
        result.ConflictType.Should().Be(ConflictType.None);
    }

    // =========================================================================
    // Object Conflict Tests
    // =========================================================================

    [Fact]
    public void DetectConflict_SameSubjectSamePredicateDifferentLiteralValue_ReturnsValueContradiction()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var claim1 = CreateClaim("endpoint", "HAS_DEFAULT", literalValue: "10", subjectEntityId: entityId);
        var claim2 = CreateClaim("endpoint", "HAS_DEFAULT", literalValue: "20", subjectEntityId: entityId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeTrue();
        result.ConflictType.Should().Be(ConflictType.ValueContradiction);
        result.Confidence.Should().BeApproximately(0.9f, 0.01f);
        result.Description.Should().Contain("Conflicting values");
    }

    [Fact]
    public void DetectConflict_SameSubjectSamePredicateSameLiteralValue_ReturnsNoConflict()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var claim1 = CreateClaim("endpoint", "HAS_DEFAULT", literalValue: "10", subjectEntityId: entityId);
        var claim2 = CreateClaim("endpoint", "HAS_DEFAULT", literalValue: "10", subjectEntityId: entityId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeFalse();
    }

    [Fact]
    public void DetectConflict_NumericValueEquality_TreatsAsNoConflict()
    {
        // Arrange — "10" and "10.0" are numerically equal
        var entityId = Guid.NewGuid();
        var claim1 = CreateClaim("endpoint", "HAS_DEFAULT", literalValue: "10", subjectEntityId: entityId);
        var claim2 = CreateClaim("endpoint", "HAS_DEFAULT", literalValue: "10.0", subjectEntityId: entityId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeFalse();
    }

    [Fact]
    public void DetectConflict_DifferentEntityRefs_SingleValuePredicate_ReturnsRelationshipContradiction()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var entity1 = CreateEntity("TypeA");
        var entity2 = CreateEntity("TypeB");
        var claim1 = CreateClaim("endpoint", "RETURNS", objectEntity: entity1, subjectEntityId: subjectId);
        var claim2 = CreateClaim("endpoint", "RETURNS", objectEntity: entity2, subjectEntityId: subjectId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeTrue();
        result.ConflictType.Should().Be(ConflictType.RelationshipContradiction);
        result.Confidence.Should().BeApproximately(0.85f, 0.01f);
    }

    [Fact]
    public void DetectConflict_DifferentEntityRefs_MultiValuePredicate_ReturnsNoConflict()
    {
        // Arrange — "ACCEPTS" allows multiple objects
        var subjectId = Guid.NewGuid();
        var entity1 = CreateEntity("ParamA");
        var entity2 = CreateEntity("ParamB");
        var claim1 = CreateClaim("endpoint", "ACCEPTS", objectEntity: entity1, subjectEntityId: subjectId);
        var claim2 = CreateClaim("endpoint", "ACCEPTS", objectEntity: entity2, subjectEntityId: subjectId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeFalse();
    }

    [Fact]
    public void DetectConflict_ObjectTypeMismatch_LiteralVsEntity_ReturnsValueContradiction()
    {
        // Arrange — one literal, one entity
        var subjectId = Guid.NewGuid();
        var entity = CreateEntity("SomeEntity");
        var claim1 = CreateClaim("endpoint", "RETURNS", literalValue: "200", subjectEntityId: subjectId);
        var claim2 = CreateClaim("endpoint", "RETURNS", objectEntity: entity, subjectEntityId: subjectId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeTrue();
        result.ConflictType.Should().Be(ConflictType.ValueContradiction);
        result.Confidence.Should().BeApproximately(0.7f, 0.01f);
    }

    // =========================================================================
    // Predicate Conflict Tests
    // =========================================================================

    [Fact]
    public void DetectConflict_ContradictoryPredicates_IS_REQUIRED_vs_IS_OPTIONAL_ReturnsRelationshipContradiction()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var entity = CreateEntity("Param");
        var claim1 = CreateClaim("param", "IS_REQUIRED", objectEntity: entity, subjectEntityId: subjectId);
        var claim2 = CreateClaim("param", "IS_OPTIONAL", objectEntity: entity, subjectEntityId: subjectId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeTrue();
        result.ConflictType.Should().Be(ConflictType.RelationshipContradiction);
        result.Confidence.Should().BeApproximately(0.95f, 0.01f);
        result.Description.Should().Contain("Contradictory predicates");
    }

    [Fact]
    public void DetectConflict_ContradictoryPredicates_Reversed_ReturnsRelationshipContradiction()
    {
        // Arrange — reversed direction (existing has the "key" predicate)
        var subjectId = Guid.NewGuid();
        var entity = CreateEntity("Feature");
        var claim1 = CreateClaim("feature", "IS_OPTIONAL", objectEntity: entity, subjectEntityId: subjectId);
        var claim2 = CreateClaim("feature", "IS_REQUIRED", objectEntity: entity, subjectEntityId: subjectId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeTrue();
        result.ConflictType.Should().Be(ConflictType.RelationshipContradiction);
    }

    [Fact]
    public void DetectConflict_NonContradictoryPredicates_ReturnsNoConflict()
    {
        // Arrange — "ACCEPTS" and "RETURNS" are not contradictory
        var subjectId = Guid.NewGuid();
        var entity1 = CreateEntity("ParamA");
        var entity2 = CreateEntity("TypeB");
        var claim1 = CreateClaim("endpoint", "ACCEPTS", objectEntity: entity1, subjectEntityId: subjectId);
        var claim2 = CreateClaim("endpoint", "RETURNS", objectEntity: entity2, subjectEntityId: subjectId);

        // Act
        var result = _detector.DetectConflict(claim1, claim2);

        // Assert
        result.HasConflict.Should().BeFalse();
    }
}
