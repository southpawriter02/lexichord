// =============================================================================
// File: ContradictionResolverTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ContradictionResolver resolution strategies.
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
public class ContradictionResolverTests
{
    private readonly ContradictionResolver _resolver = new();

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static Claim CreateClaim(
        string surfaceForm,
        string predicate,
        string? literalValue = null,
        DateTimeOffset? extractedAt = null)
    {
        var subject = ClaimEntity.Unresolved(surfaceForm, "Concept", 0, surfaceForm.Length);

        return new Claim
        {
            Subject = subject,
            Predicate = predicate,
            Object = ClaimObject.FromString(literalValue ?? "value"),
            DocumentId = Guid.NewGuid(),
            ExtractedAt = extractedAt ?? DateTimeOffset.UtcNow
        };
    }

    // =========================================================================
    // Value Contradiction Tests
    // =========================================================================

    [Fact]
    public void SuggestResolution_ValueContradiction_NewerClaim_ReturnsAcceptNew()
    {
        // Arrange
        var existing = CreateClaim("endpoint", "HAS_DEFAULT", "10",
            extractedAt: DateTimeOffset.UtcNow.AddDays(-1));
        var newer = CreateClaim("endpoint", "HAS_DEFAULT", "20",
            extractedAt: DateTimeOffset.UtcNow);

        // Act
        var resolution = _resolver.SuggestResolution(newer, existing, ConflictType.ValueContradiction);

        // Assert
        resolution.Strategy.Should().Be(ResolutionStrategy.AcceptNew);
        resolution.Confidence.Should().BeApproximately(0.7f, 0.01f);
        resolution.CanAutoApply.Should().BeFalse();
        resolution.Description.Should().Contain("20");
    }

    [Fact]
    public void SuggestResolution_ValueContradiction_OlderClaim_ReturnsManualReview()
    {
        // Arrange
        var existing = CreateClaim("endpoint", "HAS_DEFAULT", "10",
            extractedAt: DateTimeOffset.UtcNow);
        var older = CreateClaim("endpoint", "HAS_DEFAULT", "20",
            extractedAt: DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var resolution = _resolver.SuggestResolution(older, existing, ConflictType.ValueContradiction);

        // Assert
        resolution.Strategy.Should().Be(ResolutionStrategy.ManualReview);
        resolution.Description.Should().Contain("Manual review");
    }

    // =========================================================================
    // Property Conflict Tests
    // =========================================================================

    [Fact]
    public void SuggestResolution_PropertyConflict_ReturnsManualReview()
    {
        // Arrange
        var newClaim = CreateClaim("param", "HAS_PROPERTY", "typeA");
        var existing = CreateClaim("param", "HAS_PROPERTY", "typeB");

        // Act
        var resolution = _resolver.SuggestResolution(newClaim, existing, ConflictType.PropertyConflict);

        // Assert
        resolution.Strategy.Should().Be(ResolutionStrategy.ManualReview);
        resolution.Confidence.Should().BeApproximately(0.3f, 0.01f);
    }

    // =========================================================================
    // Relationship Conflict Tests
    // =========================================================================

    [Fact]
    public void SuggestResolution_RelationshipConflict_WithVersioning_ReturnsVersionExisting()
    {
        // Arrange — subject mentions "v2"
        var newClaim = CreateClaim("endpoint v2", "RETURNS", "JSON");
        var existing = CreateClaim("endpoint v1", "RETURNS", "XML");

        // Act
        var resolution = _resolver.SuggestResolution(
            newClaim, existing, ConflictType.RelationshipContradiction);

        // Assert
        resolution.Strategy.Should().Be(ResolutionStrategy.VersionExisting);
        resolution.Confidence.Should().BeApproximately(0.6f, 0.01f);
    }

    [Fact]
    public void SuggestResolution_RelationshipConflict_NoVersioning_ReturnsManualReview()
    {
        // Arrange — no version mentions
        var newClaim = CreateClaim("endpoint", "RETURNS", "JSON");
        var existing = CreateClaim("endpoint", "RETURNS", "XML");

        // Act
        var resolution = _resolver.SuggestResolution(
            newClaim, existing, ConflictType.RelationshipContradiction);

        // Assert
        resolution.Strategy.Should().Be(ResolutionStrategy.ManualReview);
        resolution.Confidence.Should().BeApproximately(0.4f, 0.01f);
    }

    // =========================================================================
    // Temporal Conflict Tests
    // =========================================================================

    [Fact]
    public void SuggestResolution_TemporalConflict_ReturnsVersionExisting_AutoApplicable()
    {
        // Arrange
        var newClaim = CreateClaim("endpoint", "HAS_STATUS", "active");
        var existing = CreateClaim("endpoint", "HAS_STATUS", "deprecated");

        // Act
        var resolution = _resolver.SuggestResolution(
            newClaim, existing, ConflictType.TemporalConflict);

        // Assert
        resolution.Strategy.Should().Be(ResolutionStrategy.VersionExisting);
        resolution.Confidence.Should().BeApproximately(0.8f, 0.01f);
        resolution.CanAutoApply.Should().BeTrue();
    }

    // =========================================================================
    // Semantic Conflict Tests
    // =========================================================================

    [Fact]
    public void SuggestResolution_SemanticContradiction_ReturnsContextualize()
    {
        // Arrange
        var newClaim = CreateClaim("service", "HAS_PROPERTY", "high availability");
        var existing = CreateClaim("service", "HAS_PROPERTY", "best effort");

        // Act
        var resolution = _resolver.SuggestResolution(
            newClaim, existing, ConflictType.SemanticContradiction);

        // Assert
        resolution.Strategy.Should().Be(ResolutionStrategy.Contextualize);
        resolution.Confidence.Should().BeApproximately(0.5f, 0.01f);
        resolution.Description.Should().Contain("context");
    }

    // =========================================================================
    // Default / Unknown Tests
    // =========================================================================

    [Fact]
    public void SuggestResolution_UnknownType_ReturnsManualReview()
    {
        // Arrange
        var newClaim = CreateClaim("endpoint", "HAS_DEFAULT", "10");
        var existing = CreateClaim("endpoint", "HAS_DEFAULT", "20");

        // Act
        var resolution = _resolver.SuggestResolution(
            newClaim, existing, ConflictType.CardinalityConflict);

        // Assert
        resolution.Strategy.Should().Be(ResolutionStrategy.ManualReview);
        resolution.Confidence.Should().Be(0.0f);
        resolution.CanAutoApply.Should().BeFalse();
    }
}
