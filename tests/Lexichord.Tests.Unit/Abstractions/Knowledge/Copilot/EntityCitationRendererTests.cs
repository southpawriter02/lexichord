// =============================================================================
// File: EntityCitationRendererTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.UI.EntityCitationRenderer
// Feature: v0.6.6h
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.6h")]
public class EntityCitationRendererTests
{
    private readonly IEntityCitationRenderer _renderer;

    public EntityCitationRendererTests()
    {
        var rendererType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.UI.EntityCitationRenderer")!;
        var loggerType = typeof(Logger<>).MakeGenericType(rendererType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);

        _renderer = (IEntityCitationRenderer)Activator.CreateInstance(rendererType, logger)!;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PostValidationResult ValidResult => PostValidationResult.Valid();

    private static PostValidationResult ResultWithWarnings(
        IReadOnlyList<ValidationFinding>? findings = null,
        IReadOnlyList<Claim>? claims = null) =>
        new()
        {
            IsValid = true,
            Status = PostValidationStatus.ValidWithWarnings,
            Findings = findings ?? [],
            Hallucinations = [],
            SuggestedFixes = [],
            VerifiedEntities = [],
            ExtractedClaims = claims ?? [],
            ValidationScore = 0.8f,
            UserMessage = "Warnings found"
        };

    private static PostValidationResult InvalidResult(
        IReadOnlyList<ValidationFinding>? findings = null) =>
        new()
        {
            IsValid = false,
            Status = PostValidationStatus.Invalid,
            Findings = findings ?? [],
            Hallucinations = [],
            SuggestedFixes = [],
            VerifiedEntities = [],
            ExtractedClaims = [],
            ValidationScore = 0.3f,
            UserMessage = "Errors found"
        };

    private static ValidatedGenerationResult CreateResult(
        string content = "The GET /api/users endpoint returns a list.",
        IReadOnlyList<KnowledgeEntity>? entities = null,
        PostValidationResult? postValidation = null) =>
        new()
        {
            Content = content,
            SourceEntities = entities ?? [],
            PostValidation = postValidation ?? ValidResult
        };

    private static KnowledgeEntity CreateEntity(
        string type = "Endpoint",
        string name = "GET /api/users",
        Guid? id = null,
        Dictionary<string, object>? properties = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Type = type,
            Name = name,
            Properties = properties ?? new()
        };

    // =========================================================================
    // GenerateCitations Tests
    // =========================================================================

    [Fact]
    public void GenerateCitations_CompactFormat_FormatsCorrectly()
    {
        // Arrange
        var entity = CreateEntity(type: "Endpoint", name: "GET /api/users",
            properties: new() { ["method"] = "GET" });
        var result = CreateResult(entities: [entity]);
        var options = new CitationOptions { Format = CitationFormat.Compact };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Citations.Should().HaveCount(1);
        markup.Citations[0].EntityType.Should().Be("Endpoint");
        markup.Citations[0].EntityName.Should().Be("GET /api/users");
        markup.Citations[0].TypeIcon.Should().Be("🔗");
        markup.Citations[0].IsVerified.Should().BeTrue();
        markup.ValidationStatus.Should().Be("Validation passed");
        markup.Icon.Should().Be(ValidationIcon.CheckMark);
        markup.FormattedMarkup.Should().Contain("📚 Based on:");
    }

    [Fact]
    public void GenerateCitations_GroupsByType_OrdersCorrectly()
    {
        // Arrange
        var endpoint = CreateEntity(type: "Endpoint", name: "GET /api/users");
        var param = CreateEntity(type: "Parameter", name: "userId");
        var schema = CreateEntity(type: "Schema", name: "UserSchema");
        var result = CreateResult(entities: [param, schema, endpoint]);
        var options = new CitationOptions { GroupByType = true };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Citations.Should().HaveCount(3);
        // Sorted alphabetically by type: Endpoint, Parameter, Schema
        markup.Citations[0].EntityType.Should().Be("Endpoint");
        markup.Citations[1].EntityType.Should().Be("Parameter");
        markup.Citations[2].EntityType.Should().Be("Schema");
    }

    [Fact]
    public void GenerateCitations_NoGrouping_PreservesOriginalOrder()
    {
        // Arrange
        var param = CreateEntity(type: "Parameter", name: "userId");
        var endpoint = CreateEntity(type: "Endpoint", name: "GET /api/users");
        var result = CreateResult(entities: [param, endpoint]);
        var options = new CitationOptions { GroupByType = false };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Citations[0].EntityType.Should().Be("Parameter");
        markup.Citations[1].EntityType.Should().Be("Endpoint");
    }

    [Fact]
    public void GenerateCitations_MaxCitations_LimitsResults()
    {
        // Arrange
        var entities = Enumerable.Range(0, 20)
            .Select(i => CreateEntity(name: $"Entity{i}"))
            .ToList();
        var result = CreateResult(entities: entities);
        var options = new CitationOptions { MaxCitations = 5 };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Citations.Should().HaveCount(5);
    }

    [Fact]
    public void GenerateCitations_NoEntities_ReturnsEmptyCitations()
    {
        // Arrange
        var result = CreateResult(entities: []);
        var options = new CitationOptions();

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Citations.Should().BeEmpty();
        markup.ValidationStatus.Should().Be("Validation passed");
        markup.Icon.Should().Be(ValidationIcon.CheckMark);
    }

    [Fact]
    public void GenerateCitations_DetailedFormat_GroupsByType()
    {
        // Arrange
        var endpoint = CreateEntity(type: "Endpoint", name: "GET /api/users");
        var param = CreateEntity(type: "Parameter", name: "userId");
        var result = CreateResult(entities: [endpoint, param]);
        var options = new CitationOptions { Format = CitationFormat.Detailed };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.FormattedMarkup.Should().Contain("## Knowledge Sources");
        markup.FormattedMarkup.Should().Contain("### Endpoint");
        markup.FormattedMarkup.Should().Contain("### Parameter");
    }

    [Fact]
    public void GenerateCitations_TreeViewFormat_ShowsHierarchy()
    {
        // Arrange
        var endpoint = CreateEntity(type: "Endpoint", name: "GET /api/users");
        var param = CreateEntity(type: "Parameter", name: "userId");
        var result = CreateResult(entities: [endpoint, param]);
        var options = new CitationOptions { Format = CitationFormat.TreeView };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.FormattedMarkup.Should().Contain("Knowledge Sources");
        markup.FormattedMarkup.Should().Contain("├── ");
    }

    [Fact]
    public void GenerateCitations_TypeIcons_MappedCorrectly()
    {
        // Arrange
        var entities = new[]
        {
            CreateEntity(type: "Endpoint", name: "E1"),
            CreateEntity(type: "Parameter", name: "P1"),
            CreateEntity(type: "Response", name: "R1"),
            CreateEntity(type: "Schema", name: "S1"),
            CreateEntity(type: "Entity", name: "N1"),
            CreateEntity(type: "Error", name: "Er1"),
            CreateEntity(type: "Custom", name: "C1")
        };
        var result = CreateResult(entities: entities);
        var options = new CitationOptions { GroupByType = false };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert — verify known type icons
        markup.Citations[0].TypeIcon.Should().Be("🔗"); // Endpoint
        markup.Citations[1].TypeIcon.Should().Be("📝"); // Parameter
        markup.Citations[2].TypeIcon.Should().Be("📤"); // Response
        markup.Citations[3].TypeIcon.Should().Be("📋"); // Schema
        markup.Citations[4].TypeIcon.Should().Be("📦"); // Entity
        markup.Citations[5].TypeIcon.Should().Be("⚠️"); // Error
        markup.Citations[6].TypeIcon.Should().Be("📄"); // Unknown default
    }

    [Fact]
    public void GenerateCitations_DisplayLabel_EndpointFormat()
    {
        // Arrange
        var entity = CreateEntity(
            type: "Endpoint",
            name: "GET /api/users",
            properties: new() { ["method"] = "GET" });
        var result = CreateResult(entities: [entity]);
        var options = new CitationOptions { GroupByType = false };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert — Endpoint label = "METHOD name"
        markup.Citations[0].DisplayLabel.Should().Be("GET GET /api/users");
    }

    [Fact]
    public void GenerateCitations_DisplayLabel_ParameterFormat()
    {
        // Arrange
        var entity = CreateEntity(
            type: "Parameter",
            name: "userId",
            properties: new() { ["location"] = (object)"query" });
        var result = CreateResult(entities: [entity]);
        var options = new CitationOptions { GroupByType = false };

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert — Parameter label = "name (location)"
        markup.Citations[0].DisplayLabel.Should().Be("userId (query)");
    }

    // =========================================================================
    // Verification Status Tests
    // =========================================================================

    [Fact]
    public void GenerateCitations_ValidStatus_CheckMarkIcon()
    {
        // Arrange
        var result = CreateResult(
            entities: [CreateEntity()],
            postValidation: ValidResult);
        var options = new CitationOptions();

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Icon.Should().Be(ValidationIcon.CheckMark);
        markup.ValidationStatus.Should().Be("Validation passed");
    }

    [Fact]
    public void GenerateCitations_WarningStatus_WarningIcon()
    {
        // Arrange
        var findings = new[]
        {
            ValidationFinding.Warn("test", "W001", "Minor issue")
        };
        var result = CreateResult(
            entities: [CreateEntity()],
            postValidation: ResultWithWarnings(findings: findings));
        var options = new CitationOptions();

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Icon.Should().Be(ValidationIcon.Warning);
        markup.ValidationStatus.Should().Contain("warning");
    }

    [Fact]
    public void GenerateCitations_InvalidStatus_ErrorIcon()
    {
        // Arrange
        var findings = new[]
        {
            ValidationFinding.Error("test", "E001", "Serious issue")
        };
        var result = CreateResult(
            entities: [CreateEntity()],
            postValidation: InvalidResult(findings: findings));
        var options = new CitationOptions();

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Icon.Should().Be(ValidationIcon.Error);
        markup.ValidationStatus.Should().Contain("error");
    }

    [Fact]
    public void GenerateCitations_EntityWithError_NotVerified()
    {
        // Arrange
        var entity = CreateEntity(name: "BadEndpoint");
        var findings = new[]
        {
            ValidationFinding.Error("test", "E001", "Issue with BadEndpoint")
        };
        var result = CreateResult(
            entities: [entity],
            postValidation: InvalidResult(findings: findings));
        var options = new CitationOptions();

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert
        markup.Citations[0].IsVerified.Should().BeFalse();
    }

    [Fact]
    public void GenerateCitations_EntityWithoutError_Verified()
    {
        // Arrange
        var entity = CreateEntity(name: "GoodEndpoint");
        var findings = new[]
        {
            ValidationFinding.Error("test", "E001", "Issue with OtherEndpoint")
        };
        var result = CreateResult(
            entities: [entity],
            postValidation: InvalidResult(findings: findings));
        var options = new CitationOptions();

        // Act
        var markup = _renderer.GenerateCitations(result, options);

        // Assert — no error references "GoodEndpoint" by name
        markup.Citations[0].IsVerified.Should().BeTrue();
    }

    // =========================================================================
    // GetCitationDetail Tests
    // =========================================================================

    [Fact]
    public void GetCitationDetail_FindsUsedProperties()
    {
        // Arrange
        var entity = CreateEntity(
            type: "Endpoint",
            name: "GET /api/users",
            properties: new()
            {
                ["path"] = "/api/users",
                ["method"] = "GET",
                ["description"] = "Returns a list of users"
            });
        var result = CreateResult(
            content: "The GET /api/users endpoint returns a list of users.",
            entities: [entity]);

        // Act
        var detail = _renderer.GetCitationDetail(entity, result);

        // Assert — "path", "method" and "description" values appear in content
        detail.UsedProperties.Should().ContainKey("path");
        detail.UsedProperties.Should().ContainKey("method");
        detail.UsedProperties.Should().ContainKey("description");
    }

    [Fact]
    public void GetCitationDetail_IgnoresUnusedProperties()
    {
        // Arrange
        var entity = CreateEntity(
            type: "Endpoint",
            name: "GET /api/users",
            properties: new()
            {
                ["path"] = "/api/users",
                ["deprecated"] = "true",
                ["internal_code"] = "XYZ-999"
            });
        var result = CreateResult(
            content: "The /api/users endpoint.",
            entities: [entity]);

        // Act
        var detail = _renderer.GetCitationDetail(entity, result);

        // Assert — only "path" value appears in content
        detail.UsedProperties.Should().ContainKey("path");
        detail.UsedProperties.Should().NotContainKey("internal_code");
    }

    [Fact]
    public void GetCitationDetail_FindsDerivedClaims()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = CreateEntity(id: entityId, name: "GET /api/users");

        var claim = new Claim
        {
            Subject = ClaimEntity.Unresolved("GET /api/users", "Endpoint", 0, 14) with
            {
                EntityId = entityId
            },
            Predicate = "ACCEPTS",
            Object = ClaimObject.FromString("limit")
        };

        var postValidation = ResultWithWarnings(claims: [claim]);
        var result = CreateResult(entities: [entity], postValidation: postValidation);

        // Act
        var detail = _renderer.GetCitationDetail(entity, result);

        // Assert
        detail.DerivedClaims.Should().HaveCount(1);
        detail.DerivedClaims[0].Subject.EntityId.Should().Be(entityId);
    }

    [Fact]
    public void GetCitationDetail_GeneratesBrowserLink()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = CreateEntity(id: entityId, name: "GET /api/users");
        var result = CreateResult(entities: [entity]);

        // Act
        var detail = _renderer.GetCitationDetail(entity, result);

        // Assert
        detail.BrowserLink.Should().Be($"lexichord://graph/entity/{entityId}");
    }

    [Fact]
    public void GetCitationDetail_ReturnsEntity()
    {
        // Arrange
        var entity = CreateEntity();
        var result = CreateResult(entities: [entity]);

        // Act
        var detail = _renderer.GetCitationDetail(entity, result);

        // Assert
        detail.Entity.Should().BeSameAs(entity);
    }

    // =========================================================================
    // Data Record Tests
    // =========================================================================

    [Fact]
    public void CitationOptions_DefaultValues_AreCorrect()
    {
        var options = new CitationOptions();

        options.Format.Should().Be(CitationFormat.Compact);
        options.MaxCitations.Should().Be(10);
        options.ShowValidationStatus.Should().BeTrue();
        options.ShowConfidence.Should().BeFalse();
        options.GroupByType.Should().BeTrue();
    }

    [Fact]
    public void ValidatedGenerationResult_RequiredProperties_AreSet()
    {
        var result = new ValidatedGenerationResult
        {
            Content = "test",
            SourceEntities = [],
            PostValidation = ValidResult
        };

        result.Content.Should().Be("test");
        result.SourceEntities.Should().BeEmpty();
        result.PostValidation.IsValid.Should().BeTrue();
    }
}
