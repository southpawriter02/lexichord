// =============================================================================
// File: KnowledgePromptBuilderTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for KnowledgePromptBuilder and related data types.
// =============================================================================
// v0.6.6i: Knowledge-Aware Prompts (CKVS Phase 3b)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Knowledge.Copilot.Prompts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

/// <summary>
/// Unit tests for <see cref="KnowledgePromptBuilder"/> and related data records.
/// </summary>
public sealed class KnowledgePromptBuilderTests
{
    // -------------------------------------------------------------------------
    // Test Helpers
    // -------------------------------------------------------------------------

    private readonly Mock<IKnowledgeContextFormatter> _formatterMock;
    private readonly Mock<IPromptRenderer> _rendererMock;
    private readonly Mock<ILogger<KnowledgePromptBuilder>> _loggerMock;

    public KnowledgePromptBuilderTests()
    {
        _formatterMock = new Mock<IKnowledgeContextFormatter>();
        _rendererMock = new Mock<IPromptRenderer>();
        _loggerMock = new Mock<ILogger<KnowledgePromptBuilder>>();

        // Default formatter behavior: return entity names joined
        _formatterMock
            .Setup(f => f.FormatContext(
                It.IsAny<IReadOnlyList<KnowledgeEntity>>(),
                It.IsAny<IReadOnlyList<KnowledgeRelationship>?>(),
                It.IsAny<IReadOnlyList<Axiom>?>(),
                It.IsAny<ContextFormatOptions>()))
            .Returns((IReadOnlyList<KnowledgeEntity> entities, IReadOnlyList<KnowledgeRelationship>? _, IReadOnlyList<Axiom>? _, ContextFormatOptions _) =>
                string.Join("\n", entities.Select(e => $"{e.Type}: {e.Name}")));

        // Default renderer behavior: pass through the template with variables replaced
        _rendererMock
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
            .Returns((string template, IDictionary<string, object> vars) =>
            {
                var result = template;
                foreach (var kvp in vars)
                {
                    result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value?.ToString() ?? "");
                }
                return result;
            });
    }

    private KnowledgePromptBuilder CreateBuilder() =>
        new(_formatterMock.Object, _rendererMock.Object, _loggerMock.Object);

    private static AgentRequest CreateRequest(string message = "Explain the API") =>
        new(message);

    private static KnowledgeEntity CreateEntity(string type = "Endpoint", string name = "GET /api/users") =>
        new() { Type = type, Name = name };

    private static Axiom CreateAxiom(
        string id = "test-axiom",
        string name = "Test Rule",
        string? description = "All endpoints must have a method") =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            TargetType = "Endpoint",
            Rules = []
        };

    private static KnowledgeRelationship CreateRelationship(
        Guid fromId,
        Guid toId,
        string type = "CONTAINS") =>
        new() { Type = type, FromEntityId = fromId, ToEntityId = toId };

    private static KnowledgeContext CreateContext(
        IReadOnlyList<KnowledgeEntity>? entities = null,
        IReadOnlyList<KnowledgeRelationship>? relationships = null,
        IReadOnlyList<Axiom>? axioms = null) =>
        new()
        {
            Entities = entities ?? [CreateEntity()],
            Relationships = relationships,
            Axioms = axioms
        };

    private static PromptOptions CreateOptions(
        string? templateId = null,
        GroundingLevel grounding = GroundingLevel.Moderate,
        bool includeAxioms = true,
        bool includeRelationships = true,
        string? additionalInstructions = null) =>
        new()
        {
            TemplateId = templateId,
            GroundingLevel = grounding,
            IncludeAxioms = includeAxioms,
            IncludeRelationships = includeRelationships,
            AdditionalInstructions = additionalInstructions
        };

    // -------------------------------------------------------------------------
    // BuildPrompt Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPrompt_IncludesEntities()
    {
        // Arrange
        var builder = CreateBuilder();
        var entity = CreateEntity("Endpoint", "GET /api/users");
        var context = CreateContext(entities: [entity]);
        var options = CreateOptions();

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, options);

        // Assert
        _formatterMock.Verify(f => f.FormatContext(
            It.Is<IReadOnlyList<KnowledgeEntity>>(e => e.Count == 1 && e[0].Name == "GET /api/users"),
            It.IsAny<IReadOnlyList<KnowledgeRelationship>?>(),
            It.IsAny<IReadOnlyList<Axiom>?>(),
            It.IsAny<ContextFormatOptions>()), Times.Once);

        result.UserPrompt.Should().Contain("Endpoint: GET /api/users");
    }

    [Fact]
    public void BuildPrompt_IncludesAxioms()
    {
        // Arrange
        var builder = CreateBuilder();
        var axiom = CreateAxiom();
        var context = CreateContext(axioms: [axiom]);
        var options = CreateOptions();

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, options);

        // Assert
        result.SystemPrompt.Should().Contain("Test Rule: All endpoints must have a method");
    }

    [Theory]
    [InlineData(GroundingLevel.Strict, "STRICT GROUNDING RULES")]
    [InlineData(GroundingLevel.Moderate, "GROUNDING RULES")]
    [InlineData(GroundingLevel.Flexible, "GROUNDING GUIDANCE")]
    public void BuildPrompt_RespectsGroundingLevel(GroundingLevel level, string expectedText)
    {
        // Arrange
        var builder = CreateBuilder();
        var options = CreateOptions(grounding: level);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), CreateContext(), options);

        // Assert
        result.SystemPrompt.Should().Contain(expectedText);
    }

    [Fact]
    public void BuildPrompt_EstimatesTokens()
    {
        // Arrange
        var builder = CreateBuilder();
        var options = CreateOptions();

        // Act
        var result = builder.BuildPrompt(CreateRequest(), CreateContext(), options);

        // Assert — token count should be ~(systemPrompt.Length + userPrompt.Length) / 4
        var expectedApprox = (result.SystemPrompt.Length + result.UserPrompt.Length) / 4;
        result.EstimatedTokens.Should().Be(expectedApprox);
        result.EstimatedTokens.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BuildPrompt_UsesDefaultTemplate_WhenNoTemplateSpecified()
    {
        // Arrange
        var builder = CreateBuilder();
        var options = CreateOptions(templateId: null);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), CreateContext(), options);

        // Assert
        result.TemplateId.Should().Be("copilot-knowledge-aware");
    }

    [Fact]
    public void BuildPrompt_UsesSpecifiedTemplate()
    {
        // Arrange
        var builder = CreateBuilder();
        var options = CreateOptions(templateId: "copilot-strict");

        // Act
        var result = builder.BuildPrompt(CreateRequest(), CreateContext(), options);

        // Assert
        result.TemplateId.Should().Be("copilot-strict");
        result.SystemPrompt.Should().Contain("ONLY state facts");
    }

    [Fact]
    public void BuildPrompt_ThrowsOnUnknownTemplate()
    {
        // Arrange
        var builder = CreateBuilder();
        var options = CreateOptions(templateId: "nonexistent-template");

        // Act
        var act = () => builder.BuildPrompt(CreateRequest(), CreateContext(), options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Template not found: nonexistent-template*");
    }

    [Fact]
    public void BuildPrompt_IncludesRelationships_WhenEnabled()
    {
        // Arrange
        var builder = CreateBuilder();
        var entity1 = CreateEntity("Product", "API Gateway");
        var entity2 = CreateEntity("Endpoint", "GET /users");
        var rel = CreateRelationship(entity1.Id, entity2.Id, "CONTAINS");
        var context = CreateContext(
            entities: [entity1, entity2],
            relationships: [rel]);
        var options = CreateOptions(includeRelationships: true);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, options);

        // Assert
        result.UserPrompt.Should().Contain("API Gateway --[CONTAINS]--> GET /users");
    }

    [Fact]
    public void BuildPrompt_ExcludesRelationships_WhenDisabled()
    {
        // Arrange
        var builder = CreateBuilder();
        var entity1 = CreateEntity("Product", "API Gateway");
        var entity2 = CreateEntity("Endpoint", "GET /users");
        var rel = CreateRelationship(entity1.Id, entity2.Id, "CONTAINS");
        var context = CreateContext(
            entities: [entity1, entity2],
            relationships: [rel]);
        var options = CreateOptions(includeRelationships: false);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, options);

        // Assert
        result.UserPrompt.Should().NotContain("CONTAINS");
    }

    [Fact]
    public void BuildPrompt_ExcludesAxioms_WhenDisabled()
    {
        // Arrange
        var builder = CreateBuilder();
        var axiom = CreateAxiom();
        var context = CreateContext(axioms: [axiom]);
        var options = CreateOptions(includeAxioms: false);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, options);

        // Assert
        result.SystemPrompt.Should().NotContain("Test Rule");
    }

    [Fact]
    public void BuildPrompt_IncludesAdditionalInstructions()
    {
        // Arrange
        var builder = CreateBuilder();
        var options = CreateOptions(additionalInstructions: "Focus on security aspects");

        // Act
        var result = builder.BuildPrompt(CreateRequest(), CreateContext(), options);

        // Assert
        result.SystemPrompt.Should().Contain("Focus on security aspects");
    }

    [Fact]
    public void BuildPrompt_TracksIncludedEntityIds()
    {
        // Arrange
        var builder = CreateBuilder();
        var entity1 = CreateEntity("Endpoint", "GET /users");
        var entity2 = CreateEntity("Endpoint", "POST /users");
        var context = CreateContext(entities: [entity1, entity2]);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, CreateOptions());

        // Assert
        result.IncludedEntityIds.Should().HaveCount(2);
        result.IncludedEntityIds.Should().Contain(entity1.Id);
        result.IncludedEntityIds.Should().Contain(entity2.Id);
    }

    [Fact]
    public void BuildPrompt_TracksIncludedAxiomIds()
    {
        // Arrange
        var builder = CreateBuilder();
        var axiom1 = CreateAxiom(id: "axiom-1", name: "Rule 1");
        var axiom2 = CreateAxiom(id: "axiom-2", name: "Rule 2");
        var context = CreateContext(axioms: [axiom1, axiom2]);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, CreateOptions());

        // Assert
        result.IncludedAxiomIds.Should().HaveCount(2);
        result.IncludedAxiomIds.Should().Contain("axiom-1");
        result.IncludedAxiomIds.Should().Contain("axiom-2");
    }

    [Fact]
    public void BuildPrompt_IncludesAxiomIds_EmptyWhenNoAxioms()
    {
        // Arrange
        var builder = CreateBuilder();
        var context = CreateContext(axioms: null);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, CreateOptions());

        // Assert
        result.IncludedAxiomIds.Should().BeEmpty();
    }

    [Fact]
    public void BuildPrompt_IncludesUserQuery()
    {
        // Arrange
        var builder = CreateBuilder();
        var request = CreateRequest("How do I authenticate?");

        // Act
        var result = builder.BuildPrompt(request, CreateContext(), CreateOptions());

        // Assert
        result.UserPrompt.Should().Contain("How do I authenticate?");
    }

    [Fact]
    public void BuildPrompt_ThrowsOnNullRequest()
    {
        var builder = CreateBuilder();
        var act = () => builder.BuildPrompt(null!, CreateContext(), CreateOptions());
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public void BuildPrompt_ThrowsOnNullContext()
    {
        var builder = CreateBuilder();
        var act = () => builder.BuildPrompt(CreateRequest(), null!, CreateOptions());
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void BuildPrompt_ThrowsOnNullOptions()
    {
        var builder = CreateBuilder();
        var act = () => builder.BuildPrompt(CreateRequest(), CreateContext(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void BuildPrompt_FormatsAxiomsWithoutDescription()
    {
        // Arrange
        var builder = CreateBuilder();
        var axiom = CreateAxiom(description: null);
        var context = CreateContext(axioms: [axiom]);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, CreateOptions());

        // Assert
        result.SystemPrompt.Should().Contain("- Test Rule");
        result.SystemPrompt.Should().NotContain("- Test Rule:");
    }

    [Fact]
    public void BuildPrompt_ResolvesRelationshipEntityNames_UsesIdWhenNotFound()
    {
        // Arrange
        var builder = CreateBuilder();
        var entity = CreateEntity("Endpoint", "GET /users");
        var unknownId = Guid.NewGuid();
        var rel = CreateRelationship(entity.Id, unknownId, "CALLS");
        var context = CreateContext(entities: [entity], relationships: [rel]);

        // Act
        var result = builder.BuildPrompt(CreateRequest(), context, CreateOptions());

        // Assert — unknown entity should fall back to GUID string
        result.UserPrompt.Should().Contain($"GET /users --[CALLS]--> {unknownId}");
    }

    // -------------------------------------------------------------------------
    // GetTemplates Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTemplates_ReturnsDefaultTemplates()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var templates = builder.GetTemplates();

        // Assert
        templates.Should().HaveCount(3);
        templates.Select(t => t.Id).Should().BeEquivalentTo(
            "copilot-knowledge-aware",
            "copilot-strict",
            "copilot-documentation");
    }

    // -------------------------------------------------------------------------
    // RegisterTemplate Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void RegisterTemplate_AddsCustomTemplate()
    {
        // Arrange
        var builder = CreateBuilder();
        var custom = new KnowledgePromptTemplate
        {
            Id = "custom-template",
            Name = "Custom Template",
            SystemTemplate = "System: {{groundingInstructions}}",
            UserTemplate = "User: {{query}}"
        };

        // Act
        builder.RegisterTemplate(custom);

        // Assert
        builder.GetTemplates().Should().HaveCount(4);
        builder.GetTemplates().Should().Contain(t => t.Id == "custom-template");
    }

    [Fact]
    public void RegisterTemplate_OverridesExistingTemplate()
    {
        // Arrange
        var builder = CreateBuilder();
        var overrideTemplate = new KnowledgePromptTemplate
        {
            Id = "copilot-strict",
            Name = "Overridden Strict",
            SystemTemplate = "Custom system",
            UserTemplate = "Custom user: {{query}}"
        };

        // Act
        builder.RegisterTemplate(overrideTemplate);

        // Assert
        builder.GetTemplates().Should().HaveCount(3); // Same count, overridden
        builder.GetTemplates().Should().Contain(t => t.Id == "copilot-strict" && t.Name == "Overridden Strict");
    }

    [Fact]
    public void RegisterTemplate_ThrowsOnNull()
    {
        var builder = CreateBuilder();
        var act = () => builder.RegisterTemplate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Constructor Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsOnNullFormatter()
    {
        var act = () => new KnowledgePromptBuilder(null!, _rendererMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("contextFormatter");
    }

    [Fact]
    public void Constructor_ThrowsOnNullRenderer()
    {
        var act = () => new KnowledgePromptBuilder(_formatterMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("promptRenderer");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        var act = () => new KnowledgePromptBuilder(_formatterMock.Object, _rendererMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // -------------------------------------------------------------------------
    // Data Record Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void KnowledgePrompt_Defaults()
    {
        var prompt = new KnowledgePrompt
        {
            SystemPrompt = "system",
            UserPrompt = "user"
        };

        prompt.EstimatedTokens.Should().Be(0);
        prompt.TemplateId.Should().BeNull();
        prompt.IncludedEntityIds.Should().BeEmpty();
        prompt.IncludedAxiomIds.Should().BeEmpty();
    }

    [Fact]
    public void KnowledgePromptTemplate_Defaults()
    {
        var template = new KnowledgePromptTemplate
        {
            Id = "test",
            Name = "Test",
            SystemTemplate = "sys",
            UserTemplate = "usr"
        };

        template.Description.Should().BeNull();
        template.DefaultOptions.Should().BeNull();
        template.Requirements.RequiresEntities.Should().BeTrue();
        template.Requirements.RequiresRelationships.Should().BeFalse();
        template.Requirements.RequiresAxioms.Should().BeFalse();
        template.Requirements.RequiresClaims.Should().BeFalse();
        template.Requirements.MinEntities.Should().Be(0);
    }

    [Fact]
    public void PromptOptions_Defaults()
    {
        var options = new PromptOptions();

        options.TemplateId.Should().BeNull();
        options.MaxContextTokens.Should().Be(2000);
        options.IncludeAxioms.Should().BeTrue();
        options.IncludeRelationships.Should().BeTrue();
        options.ContextFormat.Should().Be(ContextFormat.Yaml);
        options.GroundingLevel.Should().Be(GroundingLevel.Moderate);
        options.AdditionalInstructions.Should().BeNull();
    }

    [Fact]
    public void PromptRequirements_Defaults()
    {
        var reqs = new PromptRequirements();

        reqs.RequiresEntities.Should().BeTrue();
        reqs.RequiresRelationships.Should().BeFalse();
        reqs.RequiresAxioms.Should().BeFalse();
        reqs.RequiresClaims.Should().BeFalse();
        reqs.MinEntities.Should().Be(0);
    }

    [Theory]
    [InlineData(GroundingLevel.Strict, 0)]
    [InlineData(GroundingLevel.Moderate, 1)]
    [InlineData(GroundingLevel.Flexible, 2)]
    public void GroundingLevel_HasExpectedValues(GroundingLevel level, int expectedValue)
    {
        ((int)level).Should().Be(expectedValue);
    }
}
