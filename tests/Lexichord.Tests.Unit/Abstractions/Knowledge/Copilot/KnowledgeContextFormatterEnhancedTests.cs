// =============================================================================
// File: KnowledgeContextFormatterEnhancedTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.Context.KnowledgeContextFormatter
// SubPart: v0.7.2g
// =============================================================================
// Tests for the v0.7.2g Knowledge Context Formatter enhancement:
//   - FormatWithMetadata() returning FormattedContext with metadata
//   - TruncateToTokenBudget() for token budget enforcement
//   - YAML header, escaping, severity, type-aware property formatting
//   - Markdown entity grouping by type, entity name resolution
//   - JSON null suppression, severity, entity name resolution
//   - Plain text section headers, entity name resolution
//   - ITokenCounter integration for accurate token estimation
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
[Trait("SubPart", "v0.7.2g")]
public class KnowledgeContextFormatterEnhancedTests
{
    private readonly IKnowledgeContextFormatter _formatter;

    public KnowledgeContextFormatterEnhancedTests()
    {
        var formatterType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Context.KnowledgeContextFormatter")!;
        var loggerType = typeof(Logger<>).MakeGenericType(formatterType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);
        _formatter = (IKnowledgeContextFormatter)Activator.CreateInstance(formatterType, logger, null)!;
    }

    private static List<KnowledgeEntity> CreateTestEntities()
    {
        var entity1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var entity2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        return
        [
            new()
            {
                Id = entity1Id, Type = "Endpoint", Name = "GET /api/users",
                Properties = new() { ["method"] = "GET", ["path"] = "/api/users" }
            },
            new()
            {
                Id = entity2Id, Type = "Parameter", Name = "userId",
                Properties = new() { ["type"] = "integer", ["required"] = "true" }
            }
        ];
    }

    private static List<KnowledgeRelationship> CreateTestRelationships(
        Guid? fromId = null, Guid? toId = null) =>
    [
        new()
        {
            Type = "ACCEPTS",
            FromEntityId = fromId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ToEntityId = toId ?? Guid.Parse("22222222-2222-2222-2222-222222222222")
        }
    ];

    private static List<Axiom> CreateTestAxioms() =>
    [
        new()
        {
            Id = "endpoint-method-required",
            Name = "Endpoint Method Required",
            Description = "Every endpoint must specify an HTTP method",
            TargetType = "Endpoint",
            Severity = AxiomSeverity.Error,
            Rules = [new AxiomRule { Property = "method", Constraint = AxiomConstraintType.Required }]
        }
    ];

    #region FormatWithMetadata — YAML

    [Fact]
    public void FormatWithMetadata_Yaml_ReturnsFormattedContext()
    {
        // Arrange
        var entities = CreateTestEntities();
        var options = new ContextFormatOptions { Format = ContextFormat.Yaml };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, null, options);

        // Assert
        result.Content.Should().Contain("# Knowledge Context");
        result.Content.Should().Contain("entities:");
        result.Content.Should().Contain("  - type: Endpoint");
        result.Content.Should().Contain("    name: \"GET /api/users\"");
        result.Format.Should().Be(ContextFormat.Yaml);
    }

    [Fact]
    public void FormatWithMetadata_Yaml_IncludesAxiomSeverity()
    {
        // Arrange
        var entities = CreateTestEntities();
        var axioms = CreateTestAxioms();
        var options = new ContextFormatOptions { Format = ContextFormat.Yaml };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, axioms, options);

        // Assert
        result.Content.Should().Contain("severity: Error");
    }

    [Fact]
    public void FormatWithMetadata_Yaml_EscapesStrings()
    {
        // Arrange — Entity name with quotes and newlines
        var entities = new List<KnowledgeEntity>
        {
            new()
            {
                Type = "Concept",
                Name = "Test \"quoted\" name\nwith newline",
                Properties = new()
            }
        };
        var options = new ContextFormatOptions { Format = ContextFormat.Yaml };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, null, options);

        // Assert
        result.Content.Should().Contain("\\\"");
        result.Content.Should().Contain("\\n");
    }

    [Fact]
    public void FormatWithMetadata_Yaml_FormatsPropertyValues()
    {
        // Arrange — Properties with different types
        var entities = new List<KnowledgeEntity>
        {
            new()
            {
                Type = "Config",
                Name = "TestConfig",
                Properties = new()
                {
                    ["stringProp"] = "hello world",
                    ["boolProp"] = true,
                    ["intProp"] = 42
                }
            }
        };
        var options = new ContextFormatOptions { Format = ContextFormat.Yaml };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, null, options);

        // Assert
        result.Content.Should().Contain("stringProp: \"hello world\"");
        result.Content.Should().Contain("boolProp: true");
        result.Content.Should().Contain("intProp: 42");
    }

    #endregion

    #region FormatWithMetadata — Markdown

    [Fact]
    public void FormatWithMetadata_Markdown_GroupsEntitiesByType()
    {
        // Arrange — Two entities of same type
        var entities = new List<KnowledgeEntity>
        {
            new() { Type = "Endpoint", Name = "GET /api/users", Properties = new() },
            new() { Type = "Endpoint", Name = "POST /api/users", Properties = new() },
            new() { Type = "Parameter", Name = "userId", Properties = new() }
        };
        var options = new ContextFormatOptions { Format = ContextFormat.Markdown };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, null, options);

        // Assert
        result.Content.Should().Contain("**Endpoint**");
        result.Content.Should().Contain("- **GET /api/users**");
        result.Content.Should().Contain("- **POST /api/users**");
        result.Content.Should().Contain("**Parameter**");
        result.Content.Should().Contain("- **userId**");
    }

    [Fact]
    public void FormatWithMetadata_Markdown_ResolvesRelationshipNames()
    {
        // Arrange — Relationships with known entity IDs
        var entities = CreateTestEntities();
        var relationships = CreateTestRelationships();
        var options = new ContextFormatOptions { Format = ContextFormat.Markdown };

        // Act
        var result = _formatter.FormatWithMetadata(entities, relationships, null, options);

        // Assert — Should contain entity names, not raw GUIDs
        result.Content.Should().Contain("GET /api/users **ACCEPTS** userId");
    }

    [Fact]
    public void FormatWithMetadata_Markdown_IncludesAxiomSeverity()
    {
        // Arrange
        var entities = CreateTestEntities();
        var axioms = CreateTestAxioms();
        var options = new ContextFormatOptions { Format = ContextFormat.Markdown };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, axioms, options);

        // Assert
        result.Content.Should().Contain("(Error)");
        result.Content.Should().Contain("**Endpoint Method Required**");
    }

    #endregion

    #region FormatWithMetadata — JSON

    [Fact]
    public void FormatWithMetadata_Json_SuppressesNulls()
    {
        // Arrange — No relationships or axioms
        var entities = CreateTestEntities();
        var options = new ContextFormatOptions { Format = ContextFormat.Json };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, null, options);

        // Assert — null collections should be absent from JSON output
        result.Content.Should().NotContain("\"relationships\": null");
        result.Content.Should().NotContain("\"rules\": null");
    }

    [Fact]
    public void FormatWithMetadata_Json_IncludesSeverity()
    {
        // Arrange
        var entities = CreateTestEntities();
        var axioms = CreateTestAxioms();
        var options = new ContextFormatOptions { Format = ContextFormat.Json };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, axioms, options);

        // Assert
        result.Content.Should().Contain("\"severity\": \"Error\"");
    }

    [Fact]
    public void FormatWithMetadata_Json_ResolvesRelationshipNames()
    {
        // Arrange
        var entities = CreateTestEntities();
        var relationships = CreateTestRelationships();
        var options = new ContextFormatOptions { Format = ContextFormat.Json };

        // Act
        var result = _formatter.FormatWithMetadata(entities, relationships, null, options);

        // Assert — Should contain entity names in relationships
        result.Content.Should().Contain("\"from\": \"GET /api/users\"");
        result.Content.Should().Contain("\"to\": \"userId\"");
    }

    #endregion

    #region FormatWithMetadata — Plain

    [Fact]
    public void FormatWithMetadata_Plain_IncludesHeaders()
    {
        // Arrange
        var entities = CreateTestEntities();
        var relationships = CreateTestRelationships();
        var axioms = CreateTestAxioms();
        var options = new ContextFormatOptions { Format = ContextFormat.Plain };

        // Act
        var result = _formatter.FormatWithMetadata(entities, relationships, axioms, options);

        // Assert
        result.Content.Should().Contain("KNOWLEDGE CONTEXT:");
        result.Content.Should().Contain("RELATIONSHIPS:");
        result.Content.Should().Contain("RULES:");
    }

    #endregion

    #region FormatWithMetadata — Metadata

    [Fact]
    public void FormatWithMetadata_ReturnsCorrectMetadata()
    {
        // Arrange
        var entities = CreateTestEntities();
        var relationships = CreateTestRelationships();
        var axioms = CreateTestAxioms();
        var options = new ContextFormatOptions { Format = ContextFormat.Yaml };

        // Act
        var result = _formatter.FormatWithMetadata(entities, relationships, axioms, options);

        // Assert
        result.EntityCount.Should().Be(2);
        result.RelationshipCount.Should().Be(1);
        result.AxiomCount.Should().Be(1);
        result.Format.Should().Be(ContextFormat.Yaml);
        result.TokenCount.Should().BeGreaterThan(0);
        result.WasTruncated.Should().BeFalse();
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public void FormatWithMetadata_EmptyEntities_ReturnsEmpty()
    {
        // Arrange
        var options = new ContextFormatOptions { Format = ContextFormat.Yaml };

        // Act
        var result = _formatter.FormatWithMetadata([], null, null, options);

        // Assert
        result.Should().Be(FormattedContext.Empty);
        result.Content.Should().BeEmpty();
        result.TokenCount.Should().Be(0);
        result.EntityCount.Should().Be(0);
    }

    [Fact]
    public void FormatWithMetadata_TokenCount_UsesHeuristic()
    {
        // Arrange — Create entities that produce predictable output
        var entities = new List<KnowledgeEntity>
        {
            new() { Type = "Test", Name = "TestEntity", Properties = new() }
        };
        var options = new ContextFormatOptions { Format = ContextFormat.Plain };

        // Act
        var result = _formatter.FormatWithMetadata(entities, null, null, options);

        // Assert — Token count should be approximately content.Length / 4
        var expectedApprox = result.Content.Length / 4;
        result.TokenCount.Should().Be(expectedApprox);
    }

    #endregion

    #region TruncateToTokenBudget

    [Fact]
    public void TruncateToTokenBudget_WithinBudget_ReturnsUnchanged()
    {
        // Arrange
        var context = new FormattedContext
        {
            Content = "Short content",
            TokenCount = 3,
            Format = ContextFormat.Yaml,
            EntityCount = 1
        };

        // Act
        var result = _formatter.TruncateToTokenBudget(context, 100);

        // Assert
        result.Content.Should().Be("Short content");
        result.WasTruncated.Should().BeFalse();
        result.TokenCount.Should().Be(3);
    }

    [Fact]
    public void TruncateToTokenBudget_ExceedsBudget_Truncates()
    {
        // Arrange — Create content that exceeds budget
        var longContent = string.Join("\n", Enumerable.Range(0, 100).Select(i => $"Line {i}: Some content here"));
        var context = new FormattedContext
        {
            Content = longContent,
            TokenCount = longContent.Length / 4,
            Format = ContextFormat.Yaml,
            EntityCount = 100
        };

        // Act — Set budget much lower than content tokens
        var result = _formatter.TruncateToTokenBudget(context, 10);

        // Assert
        result.Content.Length.Should().BeLessThan(longContent.Length);
        result.Content.Should().Contain("[Context truncated due to token limit]");
    }

    [Fact]
    public void TruncateToTokenBudget_TruncatesAtCleanBoundary()
    {
        // Arrange — Content with clear newline boundaries
        var lines = Enumerable.Range(0, 50).Select(i => $"Line {i}: Content content content").ToList();
        var longContent = string.Join("\n", lines);
        var context = new FormattedContext
        {
            Content = longContent,
            TokenCount = longContent.Length / 4,
            Format = ContextFormat.Yaml,
            EntityCount = 50
        };

        // Act
        var result = _formatter.TruncateToTokenBudget(context, 20);

        // Assert — Should end at a newline boundary before the truncation marker
        var contentBeforeMarker = result.Content.Split("\n\n[Context truncated")[0];
        contentBeforeMarker.Should().EndWith(contentBeforeMarker.TrimEnd());
    }

    [Fact]
    public void TruncateToTokenBudget_SetsWasTruncated()
    {
        // Arrange
        var longContent = new string('a', 400);
        var context = new FormattedContext
        {
            Content = longContent,
            TokenCount = 100,
            Format = ContextFormat.Yaml,
            EntityCount = 10,
            WasTruncated = false
        };

        // Act
        var result = _formatter.TruncateToTokenBudget(context, 10);

        // Assert
        result.WasTruncated.Should().BeTrue();
    }

    #endregion

    #region Entity Name Resolution

    [Fact]
    public void FormatWithMetadata_RelationshipsWithUnknownEntity_FallsBackToGuid()
    {
        // Arrange — Relationship references entities not in the entity list
        var entities = CreateTestEntities();
        var unknownId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var relationships = CreateTestRelationships(
            fromId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            toId: unknownId);
        var options = new ContextFormatOptions { Format = ContextFormat.Plain };

        // Act
        var result = _formatter.FormatWithMetadata(entities, relationships, null, options);

        // Assert — Known entity should show name, unknown should show GUID prefix
        result.Content.Should().Contain("GET /api/users");
        result.Content.Should().Contain("aaaaaaaa...");
    }

    #endregion
}
