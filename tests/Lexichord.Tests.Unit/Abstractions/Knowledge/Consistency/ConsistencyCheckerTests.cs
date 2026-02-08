// =============================================================================
// File: ConsistencyCheckerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ConsistencyChecker (IValidator + IConsistencyChecker).
// =============================================================================
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Modules.Knowledge.Validation.Validators.Consistency;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Consistency;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.5h")]
public class ConsistencyCheckerTests
{
    private readonly IClaimRepository _claimRepository;
    private readonly IConflictDetector _conflictDetector;
    private readonly IContradictionResolver _resolver;
    private readonly ConsistencyChecker _checker;

    public ConsistencyCheckerTests()
    {
        _claimRepository = Substitute.For<IClaimRepository>();
        _conflictDetector = Substitute.For<IConflictDetector>();
        _resolver = Substitute.For<IContradictionResolver>();
        var logger = Substitute.For<ILogger<ConsistencyChecker>>();

        _checker = new ConsistencyChecker(
            _claimRepository,
            _conflictDetector,
            _resolver,
            logger);
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static Claim CreateClaim(
        string surfaceForm = "endpoint",
        string predicate = "HAS_DEFAULT",
        string literalValue = "10",
        Guid? subjectEntityId = null)
    {
        var entityId = subjectEntityId ?? Guid.NewGuid();
        var subject = new ClaimEntity
        {
            SurfaceForm = surfaceForm,
            EntityType = "Concept",
            NormalizedForm = surfaceForm.ToLowerInvariant(),
            EntityId = entityId,
            StartOffset = 0,
            EndOffset = surfaceForm.Length
        };

        return new Claim
        {
            Subject = subject,
            Predicate = predicate,
            Object = ClaimObject.FromString(literalValue),
            DocumentId = Guid.NewGuid()
        };
    }

    // =========================================================================
    // IValidator Property Tests
    // =========================================================================

    [Fact]
    public void Id_ReturnsExpectedValue()
    {
        _checker.Id.Should().Be("consistency-checker");
    }

    [Fact]
    public void DisplayName_ReturnsExpectedValue()
    {
        _checker.DisplayName.Should().Be("Consistency Checker");
    }

    [Fact]
    public void SupportedModes_ReturnsAll()
    {
        _checker.SupportedModes.Should().Be(ValidationMode.All);
    }

    [Fact]
    public void RequiredLicenseTier_ReturnsTeams()
    {
        _checker.RequiredLicenseTier.Should().Be(LicenseTier.Teams);
    }

    // =========================================================================
    // ValidateAsync Tests
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_NoClaims_ReturnsEmpty()
    {
        // Arrange
        var context = new ValidationContext(
            DocumentId: Guid.NewGuid().ToString(),
            DocumentType: "markdown",
            Content: "test content",
            Metadata: new Dictionary<string, object>(),
            Options: ValidationOptions.Default());

        // Act
        var findings = await _checker.ValidateAsync(context);

        // Assert
        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_EmptyClaimsList_ReturnsEmpty()
    {
        // Arrange
        var context = new ValidationContext(
            DocumentId: Guid.NewGuid().ToString(),
            DocumentType: "markdown",
            Content: "test content",
            Metadata: new Dictionary<string, object>
            {
                ["claims"] = (IReadOnlyList<Claim>)new List<Claim>()
            },
            Options: ValidationOptions.Default());

        // Act
        var findings = await _checker.ValidateAsync(context);

        // Assert
        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithClaims_DelegatesToCheckClaimsConsistencyAsync()
    {
        // Arrange
        var claim = CreateClaim();
        var context = new ValidationContext(
            DocumentId: Guid.NewGuid().ToString(),
            DocumentType: "markdown",
            Content: "test content",
            Metadata: new Dictionary<string, object>
            {
                ["claims"] = (IReadOnlyList<Claim>)new List<Claim> { claim }
            },
            Options: ValidationOptions.Default());

        _claimRepository.GetByEntityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Claim>());

        // Act
        var findings = await _checker.ValidateAsync(context);

        // Assert — should have queried the repository
        await _claimRepository.Received().GetByEntityAsync(
            claim.Subject.EntityId!.Value,
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // CheckClaimConsistencyAsync Tests
    // =========================================================================

    [Fact]
    public async Task CheckClaimConsistencyAsync_NoConflicts_ReturnsEmpty()
    {
        // Arrange
        var claim = CreateClaim();
        _claimRepository.GetByEntityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Claim>());

        // Act
        var findings = await _checker.CheckClaimConsistencyAsync(claim);

        // Assert
        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckClaimConsistencyAsync_WithConflict_ReturnsConsistencyFinding()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var newClaim = CreateClaim(subjectEntityId: entityId);
        var existingClaim = CreateClaim(literalValue: "20", subjectEntityId: entityId);

        _claimRepository.GetByEntityAsync(entityId, Arg.Any<CancellationToken>())
            .Returns(new[] { existingClaim });

        _conflictDetector.DetectConflict(newClaim, existingClaim)
            .Returns(new ConflictResult
            {
                HasConflict = true,
                ConflictType = ConflictType.ValueContradiction,
                Confidence = 0.9f,
                Description = "Conflicting values"
            });

        _resolver.SuggestResolution(newClaim, existingClaim, ConflictType.ValueContradiction)
            .Returns(new ConflictResolution
            {
                Strategy = ResolutionStrategy.AcceptNew,
                Description = "Accept new value",
                Confidence = 0.7f
            });

        // Act
        var findings = await _checker.CheckClaimConsistencyAsync(newClaim);

        // Assert
        findings.Should().HaveCount(1);
        var finding = findings[0];
        finding.Should().BeOfType<ConsistencyFinding>();
        finding.ValidatorId.Should().Be("consistency-checker");
        finding.Code.Should().Be(ConsistencyFindingCodes.ValueContradiction);
        finding.Severity.Should().Be(ValidationSeverity.Error); // 0.9 > 0.8

        var cf = (ConsistencyFinding)finding;
        cf.ExistingClaim.Should().Be(existingClaim);
        cf.ConflictType.Should().Be(ConflictType.ValueContradiction);
        cf.ConflictConfidence.Should().BeApproximately(0.9f, 0.01f);
        cf.Resolution!.Strategy.Should().Be(ResolutionStrategy.AcceptNew);
    }

    [Fact]
    public async Task CheckClaimConsistencyAsync_HighConfidence_ReturnsSeverityError()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var newClaim = CreateClaim(subjectEntityId: entityId);
        var existingClaim = CreateClaim(literalValue: "20", subjectEntityId: entityId);

        _claimRepository.GetByEntityAsync(entityId, Arg.Any<CancellationToken>())
            .Returns(new[] { existingClaim });

        _conflictDetector.DetectConflict(newClaim, existingClaim)
            .Returns(new ConflictResult
            {
                HasConflict = true,
                ConflictType = ConflictType.RelationshipContradiction,
                Confidence = 0.95f,
                Description = "Contradictory predicates"
            });

        _resolver.SuggestResolution(Arg.Any<Claim>(), Arg.Any<Claim>(), Arg.Any<ConflictType>())
            .Returns(new ConflictResolution { Description = "Review", Strategy = ResolutionStrategy.ManualReview });

        // Act
        var findings = await _checker.CheckClaimConsistencyAsync(newClaim);

        // Assert
        findings.Should().HaveCount(1);
        findings[0].Severity.Should().Be(ValidationSeverity.Error);
    }

    [Fact]
    public async Task CheckClaimConsistencyAsync_LowConfidence_ReturnsSeverityWarning()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var newClaim = CreateClaim(subjectEntityId: entityId);
        var existingClaim = CreateClaim(literalValue: "20", subjectEntityId: entityId);

        _claimRepository.GetByEntityAsync(entityId, Arg.Any<CancellationToken>())
            .Returns(new[] { existingClaim });

        _conflictDetector.DetectConflict(newClaim, existingClaim)
            .Returns(new ConflictResult
            {
                HasConflict = true,
                ConflictType = ConflictType.ValueContradiction,
                Confidence = 0.6f,
                Description = "Minor conflict"
            });

        _resolver.SuggestResolution(Arg.Any<Claim>(), Arg.Any<Claim>(), Arg.Any<ConflictType>())
            .Returns(new ConflictResolution { Description = "Review", Strategy = ResolutionStrategy.ManualReview });

        // Act
        var findings = await _checker.CheckClaimConsistencyAsync(newClaim);

        // Assert
        findings.Should().HaveCount(1);
        findings[0].Severity.Should().Be(ValidationSeverity.Warning);
    }

    // =========================================================================
    // CheckClaimsConsistencyAsync Tests (Internal Consistency)
    // =========================================================================

    [Fact]
    public async Task CheckClaimsConsistencyAsync_InternalConflicts_DetectsContradictoryClaimsWithinBatch()
    {
        // Arrange — two claims about same subject+predicate, different values
        var entityId = Guid.NewGuid();
        var claim1 = CreateClaim(literalValue: "10", subjectEntityId: entityId);
        var claim2 = CreateClaim(literalValue: "20", subjectEntityId: entityId);

        // Repository returns empty (no existing claims)
        _claimRepository.GetByEntityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Claim>());

        // Internal conflict detection (real detector would find conflict)
        _conflictDetector.DetectConflict(claim1, claim2)
            .Returns(new ConflictResult
            {
                HasConflict = true,
                ConflictType = ConflictType.ValueContradiction,
                Confidence = 0.9f,
                Description = "Internal conflict"
            });

        // Act
        var findings = await _checker.CheckClaimsConsistencyAsync(
            new List<Claim> { claim1, claim2 });

        // Assert — should have at least one internal consistency finding
        findings.Should().Contain(f =>
            f.Code == ConsistencyFindingCodes.ConsistencyConflict &&
            f.Message.Contains("Internal conflict"));
    }

    // =========================================================================
    // GetPotentialConflictsAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetPotentialConflictsAsync_ExcludesCurrentClaim()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var claim = CreateClaim(subjectEntityId: entityId);
        var otherClaim = CreateClaim(literalValue: "20", subjectEntityId: entityId);

        _claimRepository.GetByEntityAsync(entityId, Arg.Any<CancellationToken>())
            .Returns(new[] { claim, otherClaim });

        // Act
        var conflicts = await _checker.GetPotentialConflictsAsync(claim);

        // Assert — should not include the claim itself
        conflicts.Should().NotContain(c => c.Id == claim.Id);
        conflicts.Should().Contain(c => c.Id == otherClaim.Id);
    }
}
