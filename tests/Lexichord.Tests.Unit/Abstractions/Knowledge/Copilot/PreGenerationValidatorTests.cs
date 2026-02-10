// =============================================================================
// File: PreGenerationValidatorTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.Validation.PreGenerationValidator
// Feature: v0.6.6f
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.6f")]
public class PreGenerationValidatorTests
{
    private readonly IContextConsistencyChecker _consistencyChecker;
    private readonly IPreGenerationValidator _validator;

    public PreGenerationValidatorTests()
    {
        _consistencyChecker = Substitute.For<IContextConsistencyChecker>();

        var validatorType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Validation.PreGenerationValidator")!;
        var loggerType = typeof(Logger<>).MakeGenericType(validatorType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);

        _validator = (IPreGenerationValidator)Activator.CreateInstance(
            validatorType,
            _consistencyChecker,
            logger)!;
    }

    private static AgentRequest DefaultRequest => new("Tell me about the users endpoint");

    // =========================================================================
    // ValidateAsync Tests
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_EmptyContext_ReturnsWarningButProceeds()
    {
        // Arrange
        var context = KnowledgeContext.Empty;
        _consistencyChecker.CheckConsistency(context).Returns([]);
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context).Returns([]);

        // Act
        var result = await _validator.ValidateAsync(DefaultRequest, context);

        // Assert
        result.CanProceed.Should().BeTrue();
        result.Issues.Should().ContainSingle(i =>
            i.Code == ContextIssueCodes.EmptyContext &&
            i.Severity == ContextIssueSeverity.Warning);
    }

    [Fact]
    public async Task ValidateAsync_CleanContext_ReturnsPass()
    {
        // Arrange
        var context = new KnowledgeContext
        {
            Entities = [new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" }],
            FormattedContext = "context",
            TokenCount = 100
        };
        _consistencyChecker.CheckConsistency(context).Returns([]);
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context).Returns([]);

        // Act
        var result = await _validator.ValidateAsync(DefaultRequest, context);

        // Assert
        result.CanProceed.Should().BeTrue();
        result.Issues.Should().BeEmpty();
        result.UserMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ConsistencyIssuesAggregated()
    {
        // Arrange
        var context = new KnowledgeContext
        {
            Entities = [new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" }],
            FormattedContext = "",
            TokenCount = 0
        };

        var consistencyIssue = new ContextIssue
        {
            Code = ContextIssueCodes.ConflictingEntities,
            Message = "Duplicate entity found",
            Severity = ContextIssueSeverity.Warning
        };

        _consistencyChecker.CheckConsistency(context).Returns([consistencyIssue]);
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context).Returns([]);

        // Act
        var result = await _validator.ValidateAsync(DefaultRequest, context);

        // Assert
        result.CanProceed.Should().BeTrue(); // Warnings don't block
        result.Issues.Should().ContainSingle();
        result.Warnings.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateAsync_ErrorLevelIssue_BlocksGeneration()
    {
        // Arrange
        var context = new KnowledgeContext
        {
            Entities = [new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" }],
            FormattedContext = "",
            TokenCount = 0
        };

        var blockingIssue = new ContextIssue
        {
            Code = ContextIssueCodes.AmbiguousRequest,
            Message = "Request is empty",
            Severity = ContextIssueSeverity.Error
        };

        _consistencyChecker.CheckConsistency(context).Returns([]);
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context)
            .Returns([blockingIssue]);

        // Act
        var result = await _validator.ValidateAsync(DefaultRequest, context);

        // Assert
        result.CanProceed.Should().BeFalse();
        result.BlockingIssues.Should().HaveCount(1);
        result.UserMessage.Should().Contain("Request is empty");
    }

    [Fact]
    public async Task ValidateAsync_AxiomViolation_CreatesIssue()
    {
        // Arrange
        var axiom = new Axiom
        {
            Id = "endpoint-requires-method",
            Name = "Method Required",
            TargetType = "Endpoint",
            Severity = AxiomSeverity.Error,
            Rules = [new AxiomRule { Property = "method", Constraint = AxiomConstraintType.Required }]
        };

        var entity = new KnowledgeEntity
        {
            Type = "Endpoint",
            Name = "GET /api/users"
            // No "method" property — axiom violation
        };

        var context = new KnowledgeContext
        {
            Entities = [entity],
            Axioms = [axiom],
            FormattedContext = "",
            TokenCount = 0
        };

        _consistencyChecker.CheckConsistency(context).Returns([]);
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context).Returns([]);

        // Act
        var result = await _validator.ValidateAsync(DefaultRequest, context);

        // Assert
        result.CanProceed.Should().BeFalse(); // Error severity axiom blocks
        result.Issues.Should().Contain(i =>
            i.Code == ContextIssueCodes.AxiomViolation &&
            i.RelatedEntity == entity &&
            i.RelatedAxiom == axiom);
    }

    [Fact]
    public async Task ValidateAsync_AxiomSatisfied_NoIssue()
    {
        // Arrange
        var axiom = new Axiom
        {
            Id = "endpoint-requires-method",
            Name = "Method Required",
            TargetType = "Endpoint",
            Severity = AxiomSeverity.Error,
            Rules = [new AxiomRule { Property = "method", Constraint = AxiomConstraintType.Required }]
        };

        var entity = new KnowledgeEntity
        {
            Type = "Endpoint",
            Name = "GET /api/users",
            Properties = new Dictionary<string, object> { ["method"] = "GET" }
        };

        var context = new KnowledgeContext
        {
            Entities = [entity],
            Axioms = [axiom],
            FormattedContext = "",
            TokenCount = 0
        };

        _consistencyChecker.CheckConsistency(context).Returns([]);
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context).Returns([]);

        // Act
        var result = await _validator.ValidateAsync(DefaultRequest, context);

        // Assert
        result.CanProceed.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_ConsistencyCheckerThrows_GracefulDegradation()
    {
        // Arrange
        var context = new KnowledgeContext
        {
            Entities = [new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" }],
            FormattedContext = "",
            TokenCount = 0
        };

        _consistencyChecker.CheckConsistency(context)
            .Throws(new InvalidOperationException("Service unavailable"));
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context).Returns([]);

        // Act
        var result = await _validator.ValidateAsync(DefaultRequest, context);

        // Assert — should still proceed, with a warning
        result.CanProceed.Should().BeTrue();
        result.Issues.Should().Contain(i =>
            i.Code == ContextIssueCodes.StaleContext &&
            i.Severity == ContextIssueSeverity.Warning);
    }

    // =========================================================================
    // CanProceedAsync Tests
    // =========================================================================

    [Fact]
    public async Task CanProceedAsync_DelegatesTo_ValidateAsync()
    {
        // Arrange
        var context = new KnowledgeContext
        {
            Entities = [new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" }],
            FormattedContext = "",
            TokenCount = 0
        };
        _consistencyChecker.CheckConsistency(context).Returns([]);
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context).Returns([]);

        // Act
        var canProceed = await _validator.CanProceedAsync(DefaultRequest, context);

        // Assert
        canProceed.Should().BeTrue();
    }

    [Fact]
    public async Task CanProceedAsync_WithBlockingIssue_ReturnsFalse()
    {
        // Arrange
        var context = new KnowledgeContext
        {
            Entities = [new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" }],
            FormattedContext = "",
            TokenCount = 0
        };

        _consistencyChecker.CheckConsistency(context).Returns([]);
        _consistencyChecker.CheckRequestConsistency(DefaultRequest, context)
            .Returns([new ContextIssue
            {
                Code = ContextIssueCodes.AmbiguousRequest,
                Message = "Error",
                Severity = ContextIssueSeverity.Error
            }]);

        // Act
        var canProceed = await _validator.CanProceedAsync(DefaultRequest, context);

        // Assert
        canProceed.Should().BeFalse();
    }

    // =========================================================================
    // PreValidationResult Static Factory Tests
    // =========================================================================

    [Fact]
    public void PreValidationResult_Pass_Creates_CleanResult()
    {
        var result = PreValidationResult.Pass();

        result.CanProceed.Should().BeTrue();
        result.Issues.Should().BeEmpty();
        result.BlockingIssues.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
        result.UserMessage.Should().BeNull();
    }

    [Fact]
    public void PreValidationResult_Block_Creates_BlockingResult()
    {
        var issues = new List<ContextIssue>
        {
            new()
            {
                Code = ContextIssueCodes.AmbiguousRequest,
                Message = "Bad request",
                Severity = ContextIssueSeverity.Error
            }
        };

        var result = PreValidationResult.Block(issues, "Cannot proceed");

        result.CanProceed.Should().BeFalse();
        result.Issues.Should().HaveCount(1);
        result.UserMessage.Should().Be("Cannot proceed");
    }
}
