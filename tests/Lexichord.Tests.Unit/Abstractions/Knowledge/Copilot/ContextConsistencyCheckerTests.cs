// =============================================================================
// File: ContextConsistencyCheckerTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.Validation.ContextConsistencyChecker
// Feature: v0.6.6f
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.6f")]
public class ContextConsistencyCheckerTests
{
    private readonly IContextConsistencyChecker _checker;

    public ContextConsistencyCheckerTests()
    {
        var checkerType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Validation.ContextConsistencyChecker")!;
        var loggerType = typeof(Logger<>).MakeGenericType(checkerType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);

        _checker = (IContextConsistencyChecker)Activator.CreateInstance(
            checkerType,
            logger)!;
    }

    // =========================================================================
    // CheckConsistency Tests
    // =========================================================================

    [Fact]
    public void CheckConsistency_EmptyContext_ReturnsNoIssues()
    {
        // Arrange
        var context = KnowledgeContext.Empty;

        // Act
        var issues = _checker.CheckConsistency(context);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckConsistency_UniqueEntities_ReturnsNoIssues()
    {
        // Arrange
        var context = new KnowledgeContext
        {
            Entities =
            [
                new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" },
                new KnowledgeEntity { Type = "Parameter", Name = "userId" }
            ],
            FormattedContext = "",
            TokenCount = 0
        };

        // Act
        var issues = _checker.CheckConsistency(context);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckConsistency_DuplicateEntityNames_ReturnsWarning()
    {
        // Arrange
        var context = new KnowledgeContext
        {
            Entities =
            [
                new KnowledgeEntity { Type = "Endpoint", Name = "UserService" },
                new KnowledgeEntity { Type = "Service", Name = "userservice" } // case-insensitive match
            ],
            FormattedContext = "",
            TokenCount = 0
        };

        // Act
        var issues = _checker.CheckConsistency(context);

        // Assert
        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(ContextIssueCodes.ConflictingEntities);
        issues[0].Severity.Should().Be(ContextIssueSeverity.Warning);
        issues[0].Message.Should().Contain("userservice");
    }

    [Fact]
    public void CheckConsistency_ConflictingBooleanProperties_ReturnsWarning()
    {
        // Arrange — two endpoints with conflicting "required" values
        var context = new KnowledgeContext
        {
            Entities =
            [
                new KnowledgeEntity
                {
                    Type = "Parameter",
                    Name = "userId",
                    Properties = new Dictionary<string, object> { ["required"] = true }
                },
                new KnowledgeEntity
                {
                    Type = "Parameter",
                    Name = "userName",
                    Properties = new Dictionary<string, object> { ["required"] = false }
                },
            ],
            FormattedContext = "",
            TokenCount = 0
        };

        // Act
        var issues = _checker.CheckConsistency(context);

        // Assert
        issues.Should().Contain(i =>
            i.Code == ContextIssueCodes.ConflictingEntities &&
            i.Message.Contains("required"));
    }

    [Fact]
    public void CheckConsistency_NonBooleanPropertyDifferences_NoConflict()
    {
        // Arrange — two endpoints with different descriptions (not a conflict)
        var context = new KnowledgeContext
        {
            Entities =
            [
                new KnowledgeEntity
                {
                    Type = "Endpoint",
                    Name = "GET /api/users",
                    Properties = new Dictionary<string, object> { ["description"] = "Get users" }
                },
                new KnowledgeEntity
                {
                    Type = "Endpoint",
                    Name = "POST /api/users",
                    Properties = new Dictionary<string, object> { ["description"] = "Create user" }
                },
            ],
            FormattedContext = "",
            TokenCount = 0
        };

        // Act
        var issues = _checker.CheckConsistency(context);

        // Assert — description differences are not flagged
        issues.Where(i => i.Code == ContextIssueCodes.ConflictingEntities)
            .Should().BeEmpty();
    }

    [Fact]
    public void CheckConsistency_DanglingRelationship_ReturnsWarning()
    {
        // Arrange
        var entity = new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" };
        var danglingId = Guid.NewGuid();

        var context = new KnowledgeContext
        {
            Entities = [entity],
            Relationships =
            [
                new KnowledgeRelationship
                {
                    Type = "ACCEPTS",
                    FromEntityId = entity.Id,
                    ToEntityId = danglingId // Not in entities list
                }
            ],
            FormattedContext = "",
            TokenCount = 0
        };

        // Act
        var issues = _checker.CheckConsistency(context);

        // Assert — only the target (ToEntityId) should be flagged
        issues.Should().ContainSingle(i =>
            i.Code == ContextIssueCodes.MissingRequiredEntity &&
            i.Severity == ContextIssueSeverity.Warning);
    }

    [Fact]
    public void CheckConsistency_ValidRelationships_ReturnsNoIssues()
    {
        // Arrange
        var entity1 = new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" };
        var entity2 = new KnowledgeEntity { Type = "Parameter", Name = "userId" };

        var context = new KnowledgeContext
        {
            Entities = [entity1, entity2],
            Relationships =
            [
                new KnowledgeRelationship
                {
                    Type = "ACCEPTS",
                    FromEntityId = entity1.Id,
                    ToEntityId = entity2.Id
                }
            ],
            FormattedContext = "",
            TokenCount = 0
        };

        // Act
        var issues = _checker.CheckConsistency(context);

        // Assert
        issues.Should().BeEmpty();
    }

    // =========================================================================
    // CheckRequestConsistency Tests
    // =========================================================================

    [Fact]
    public void CheckRequestConsistency_EmptyRequest_ReturnsError()
    {
        // Arrange
        var request = new AgentRequest("   ");
        var context = KnowledgeContext.Empty;

        // Act
        var issues = _checker.CheckRequestConsistency(request, context);

        // Assert
        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(ContextIssueCodes.AmbiguousRequest);
        issues[0].Severity.Should().Be(ContextIssueSeverity.Error);
    }

    [Fact]
    public void CheckRequestConsistency_ValidRequest_NoIssues()
    {
        // Arrange
        var request = new AgentRequest("Tell me about users");
        var context = new KnowledgeContext
        {
            Entities =
            [
                new KnowledgeEntity { Type = "Endpoint", Name = "users" }
            ],
            FormattedContext = "",
            TokenCount = 0
        };

        // Act
        var issues = _checker.CheckRequestConsistency(request, context);

        // Assert — "users" term is short (5 chars) and lowercase, not entity-like
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckRequestConsistency_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _checker.CheckRequestConsistency(null!, KnowledgeContext.Empty);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CheckConsistency_NullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _checker.CheckConsistency(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
