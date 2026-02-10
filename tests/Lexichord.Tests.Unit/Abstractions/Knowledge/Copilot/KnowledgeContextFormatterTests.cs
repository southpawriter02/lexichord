// =============================================================================
// File: KnowledgeContextFormatterTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.Context.KnowledgeContextFormatter
// Feature: v0.6.6e
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.6e")]
public class KnowledgeContextFormatterTests
{
    private readonly IKnowledgeContextFormatter _formatter;

    public KnowledgeContextFormatterTests()
    {
        var formatterType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Context.KnowledgeContextFormatter")!;
        var loggerType = typeof(Logger<>).MakeGenericType(formatterType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);
        _formatter = (IKnowledgeContextFormatter)Activator.CreateInstance(formatterType, logger)!;
    }

    private static List<KnowledgeEntity> CreateTestEntities() =>
    [
        new() { Type = "Endpoint", Name = "GET /api/users", Properties = new()
        {
            ["method"] = "GET",
            ["path"] = "/api/users"
        }},
        new() { Type = "Parameter", Name = "userId", Properties = new()
        {
            ["type"] = "integer",
            ["required"] = "true"
        }}
    ];

    private static List<KnowledgeRelationship> CreateTestRelationships() =>
    [
        new() { Type = "ACCEPTS", FromEntityId = Guid.NewGuid(), ToEntityId = Guid.NewGuid() }
    ];

    private static List<Axiom> CreateTestAxioms() =>
    [
        new()
        {
            Id = "endpoint-method-required",
            Name = "Endpoint Method Required",
            Description = "Every endpoint must specify an HTTP method",
            TargetType = "Endpoint",
            Rules = [new AxiomRule { Property = "method", Constraint = AxiomConstraintType.Required }]
        }
    ];

    [Fact]
    public void FormatContext_Markdown_ContainsHeaders()
    {
        // Arrange
        var entities = CreateTestEntities();
        var options = new ContextFormatOptions { Format = ContextFormat.Markdown };

        // Act
        var result = _formatter.FormatContext(entities, null, null, options);

        // Assert
        result.Should().Contain("## Knowledge Entities");
        result.Should().Contain("### Endpoint: GET /api/users");
        result.Should().Contain("### Parameter: userId");
        result.Should().Contain("- **method**: GET");
    }

    [Fact]
    public void FormatContext_Yaml_ContainsStructuredData()
    {
        // Arrange
        var entities = CreateTestEntities();
        var options = new ContextFormatOptions { Format = ContextFormat.Yaml };

        // Act
        var result = _formatter.FormatContext(entities, null, null, options);

        // Assert
        result.Should().Contain("entities:");
        result.Should().Contain("  - type: Endpoint");
        result.Should().Contain("    name: GET /api/users");
        result.Should().Contain("      method: GET");
    }

    [Fact]
    public void FormatContext_Json_IsValidJson()
    {
        // Arrange
        var entities = CreateTestEntities();
        var options = new ContextFormatOptions { Format = ContextFormat.Json };

        // Act
        var result = _formatter.FormatContext(entities, null, null, options);

        // Assert
        result.Should().Contain("\"entities\"");
        result.Should().Contain("\"type\": \"Endpoint\"");
        result.Should().Contain("\"name\": \"GET /api/users\"");
        // Verify it's valid JSON by parsing
        var json = System.Text.Json.JsonDocument.Parse(result);
        json.RootElement.GetProperty("entities").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void FormatContext_Plain_ContainsEntityInfo()
    {
        // Arrange
        var entities = CreateTestEntities();
        var options = new ContextFormatOptions { Format = ContextFormat.Plain };

        // Act
        var result = _formatter.FormatContext(entities, null, null, options);

        // Assert
        result.Should().Contain("Endpoint: GET /api/users");
        result.Should().Contain("  method: GET");
    }

    [Fact]
    public void FormatContext_WithRelationshipsAndAxioms_IncludesSections()
    {
        // Arrange
        var entities = CreateTestEntities();
        var relationships = CreateTestRelationships();
        var axioms = CreateTestAxioms();
        var options = new ContextFormatOptions { Format = ContextFormat.Markdown };

        // Act
        var result = _formatter.FormatContext(entities, relationships, axioms, options);

        // Assert
        result.Should().Contain("## Relationships");
        result.Should().Contain("ACCEPTS");
        result.Should().Contain("## Domain Rules");
        result.Should().Contain("Endpoint Method Required");
    }

    [Fact]
    public void EstimateTokens_ApproximatesCorrectly()
    {
        // Arrange — 100 characters should be ~25 tokens
        var text = new string('a', 100);

        // Act
        var estimate = _formatter.EstimateTokens(text);

        // Assert
        estimate.Should().Be(25);
    }

    [Fact]
    public void FormatContext_EmptyEntities_ReturnsEmpty()
    {
        // Arrange
        var options = new ContextFormatOptions { Format = ContextFormat.Markdown };

        // Act
        var result = _formatter.FormatContext([], null, null, options);

        // Assert
        result.Should().BeEmpty();
    }
}
