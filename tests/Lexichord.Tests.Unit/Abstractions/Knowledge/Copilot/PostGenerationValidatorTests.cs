// =============================================================================
// File: PostGenerationValidatorTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.Validation.PostGenerationValidator
// Feature: v0.6.6g
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TextSpan = Lexichord.Abstractions.Contracts.Knowledge.TextSpan;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.6g")]
public class PostGenerationValidatorTests
{
    private readonly IClaimExtractionService _claimExtractor;
    private readonly IValidationEngine _validationEngine;
    private readonly IHallucinationDetector _hallucinationDetector;
    private readonly IPostGenerationValidator _validator;

    public PostGenerationValidatorTests()
    {
        _claimExtractor = Substitute.For<IClaimExtractionService>();
        _validationEngine = Substitute.For<IValidationEngine>();
        _hallucinationDetector = Substitute.For<IHallucinationDetector>();

        var validatorType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Validation.PostGenerationValidator")!;
        var loggerType = typeof(Logger<>).MakeGenericType(validatorType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);

        _validator = (IPostGenerationValidator)Activator.CreateInstance(
            validatorType,
            _claimExtractor,
            _validationEngine,
            _hallucinationDetector,
            logger)!;
    }

    private static AgentRequest DefaultRequest => new("Tell me about the users endpoint");

    private static KnowledgeContext ContextWithEntities(params KnowledgeEntity[] entities) =>
        new()
        {
            Entities = entities,
            FormattedContext = "test context",
            TokenCount = 100
        };

    private void SetupCleanDependencies(KnowledgeContext? context = null)
    {
        _claimExtractor.ExtractClaimsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<LinkedEntity>>(),
            Arg.Any<ClaimExtractionContext>(),
            Arg.Any<CancellationToken>())
            .Returns(ClaimExtractionResult.Empty);

        _validationEngine.ValidateDocumentAsync(
            Arg.Any<ValidationContext>(),
            Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Valid());

        _hallucinationDetector.DetectAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<HallucinationFinding>());
    }

    // =========================================================================
    // ValidateAsync Tests
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_EmptyContent_ReturnsValid()
    {
        // Arrange
        SetupCleanDependencies();

        // Act
        var result = await _validator.ValidateAsync("", KnowledgeContext.Empty, DefaultRequest);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(PostValidationStatus.Valid);
    }

    [Fact]
    public async Task ValidateAsync_CleanContent_ReturnsValid()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        SetupCleanDependencies(context);

        // Act
        var result = await _validator.ValidateAsync(
            "The GET /api/users endpoint returns a list of users.",
            context, DefaultRequest);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(PostValidationStatus.Valid);
        result.ValidationScore.Should().Be(1.0f);
        result.VerifiedEntities.Should().ContainSingle();
        result.UserMessage.Should().Contain("✓");
    }

    [Fact]
    public async Task ValidateAsync_WithHallucinations_ReturnsInvalid()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        SetupCleanDependencies(context);

        // Override hallucination detector to return a high-confidence finding
        _hallucinationDetector.DetectAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<HallucinationFinding>
            {
                new()
                {
                    ClaimText = "DELETE /api/admin",
                    Confidence = 0.9f,
                    Type = HallucinationType.UnknownEntity
                }
            });

        // Act
        var result = await _validator.ValidateAsync(
            "The DELETE /api/admin endpoint requires auth.",
            context, DefaultRequest);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(PostValidationStatus.Invalid);
        result.Hallucinations.Should().ContainSingle();
        result.UserMessage.Should().Contain("✗");
    }

    [Fact]
    public async Task ValidateAsync_WithWarnings_ReturnsValidWithWarnings()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        SetupCleanDependencies(context);

        // Override hallucination detector to return a low-confidence finding
        _hallucinationDetector.DetectAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<HallucinationFinding>
            {
                new()
                {
                    ClaimText = "some entity",
                    Confidence = 0.6f,
                    Type = HallucinationType.UnknownEntity
                }
            });

        // Act
        var result = await _validator.ValidateAsync(
            "The GET /api/users endpoint uses some entity.",
            context, DefaultRequest);

        // Assert
        result.Status.Should().Be(PostValidationStatus.ValidWithWarnings);
        result.UserMessage.Should().Contain("⚠");
    }

    [Fact]
    public async Task ValidateAsync_ValidationErrors_ReturnsInvalid()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        _claimExtractor.ExtractClaimsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<LinkedEntity>>(),
            Arg.Any<ClaimExtractionContext>(),
            Arg.Any<CancellationToken>())
            .Returns(ClaimExtractionResult.Empty);

        _validationEngine.ValidateDocumentAsync(
            Arg.Any<ValidationContext>(),
            Arg.Any<CancellationToken>())
            .Returns(ValidationResult.WithFindings(
            [
                ValidationFinding.Error("post-validator", "ERR001", "Invalid claim")
            ]));

        _hallucinationDetector.DetectAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<HallucinationFinding>());

        // Act
        var result = await _validator.ValidateAsync(
            "Content with validation errors.",
            context, DefaultRequest);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(PostValidationStatus.Invalid);
        result.Findings.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateAsync_ClaimExtractionFails_ReturnsInconclusive()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        _claimExtractor.ExtractClaimsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<LinkedEntity>>(),
            Arg.Any<ClaimExtractionContext>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act
        var result = await _validator.ValidateAsync(
            "Some content.",
            context, DefaultRequest);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(PostValidationStatus.Inconclusive);
    }

    [Fact]
    public async Task ValidateAsync_HallucinationDetectorFails_DegracesGracefully()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        _claimExtractor.ExtractClaimsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<LinkedEntity>>(),
            Arg.Any<ClaimExtractionContext>(),
            Arg.Any<CancellationToken>())
            .Returns(ClaimExtractionResult.Empty);

        _validationEngine.ValidateDocumentAsync(
            Arg.Any<ValidationContext>(),
            Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Valid());

        _hallucinationDetector.DetectAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContext>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Detection timed out"));

        // Act
        var result = await _validator.ValidateAsync(
            "Some content.",
            context, DefaultRequest);

        // Assert — should not throw; hallucinations list should be empty
        result.Status.Should().Be(PostValidationStatus.Valid);
        result.Hallucinations.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_VerifiesEntitiesFromContent()
    {
        // Arrange
        var entity1 = new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" };
        var entity2 = new KnowledgeEntity { Type = "Endpoint", Name = "POST /api/orders" };
        var context = ContextWithEntities(entity1, entity2);

        SetupCleanDependencies(context);

        // Act — content only mentions entity1
        var result = await _validator.ValidateAsync(
            "The GET /api/users endpoint returns a list.",
            context, DefaultRequest);

        // Assert
        result.VerifiedEntities.Should().ContainSingle();
        result.VerifiedEntities[0].Name.Should().Be("GET /api/users");
    }

    // =========================================================================
    // ValidateAndFixAsync Tests
    // =========================================================================

    [Fact]
    public async Task ValidateAndFixAsync_NoFixes_ReturnsSameResult()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        SetupCleanDependencies(context);

        // Act
        var result = await _validator.ValidateAndFixAsync(
            "Clean content.", context, DefaultRequest);

        // Assert
        result.CorrectedContent.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAndFixAsync_AppliesAutoFixes()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        _claimExtractor.ExtractClaimsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<LinkedEntity>>(),
            Arg.Any<ClaimExtractionContext>(),
            Arg.Any<CancellationToken>())
            .Returns(ClaimExtractionResult.Empty);

        _validationEngine.ValidateDocumentAsync(
            Arg.Any<ValidationContext>(),
            Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Valid());

        // Return a hallucination with auto-fixable correction
        _hallucinationDetector.DetectAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<HallucinationFinding>
            {
                new()
                {
                    ClaimText = "wrong",
                    Location = new TextSpan { Start = 4, End = 9 },
                    Confidence = 0.95f,
                    Type = HallucinationType.ContradictoryValue,
                    SuggestedCorrection = "right"
                }
            });

        // Act
        var result = await _validator.ValidateAndFixAsync(
            "The wrong value here.", context, DefaultRequest);

        // Assert
        result.CorrectedContent.Should().NotBeNull();
        result.CorrectedContent.Should().Be("The right value here.");
    }

    // =========================================================================
    // PostValidationResult Factory Tests
    // =========================================================================

    [Fact]
    public void PostValidationResult_Valid_HasCorrectDefaults()
    {
        var result = PostValidationResult.Valid();

        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(PostValidationStatus.Valid);
        result.Findings.Should().BeEmpty();
        result.Hallucinations.Should().BeEmpty();
        result.SuggestedFixes.Should().BeEmpty();
        result.CorrectedContent.Should().BeNull();
        result.ValidationScore.Should().Be(1.0f);
    }

    [Fact]
    public void PostValidationResult_Inconclusive_HasCorrectDefaults()
    {
        var result = PostValidationResult.Inconclusive("test reason");

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(PostValidationStatus.Inconclusive);
        result.Findings.Should().BeEmpty();
        result.ValidationScore.Should().Be(0.0f);
        result.UserMessage.Should().Be("test reason");
    }
}
