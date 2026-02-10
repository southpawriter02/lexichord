// =============================================================================
// File: EntityRelevanceRankerTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.Context.EntityRelevanceRanker
// Feature: v0.6.6e
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.6e")]
public class EntityRelevanceRankerTests
{
    private readonly IEntityRelevanceRanker _ranker;

    public EntityRelevanceRankerTests()
    {
        var rankerType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Context.EntityRelevanceRanker")!;
        // Create a typed ILogger<T> via NullLoggerFactory for the internal type
        var loggerType = typeof(Logger<>).MakeGenericType(rankerType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);
        _ranker = (IEntityRelevanceRanker)Activator.CreateInstance(rankerType, logger)!;
    }

    [Fact]
    public void RankEntities_HighestScoreFirst()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            new() { Type = "Endpoint", Name = "GET /api/users", Properties = new() { ["method"] = "GET" } },
            new() { Type = "Parameter", Name = "userId", Properties = new() { ["type"] = "integer" } },
            new() { Type = "Endpoint", Name = "POST /api/orders", Properties = new() { ["method"] = "POST" } }
        };

        // Act
        var ranked = _ranker.RankEntities("GET users", entities);

        // Assert
        ranked.Should().HaveCount(3);
        ranked[0].Entity.Name.Should().Be("GET /api/users");
        ranked[0].RelevanceScore.Should().BeGreaterThan(ranked[1].RelevanceScore);
    }

    [Fact]
    public void RankEntities_EmptyQuery_ReturnsAllZero()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            new() { Type = "Endpoint", Name = "GET /api/users" }
        };

        // Act
        var ranked = _ranker.RankEntities("", entities);

        // Assert — all scores should be 0 since there are no query terms
        ranked.Should().HaveCount(1);
        ranked[0].RelevanceScore.Should().Be(0);
    }

    [Fact]
    public void SelectWithinBudget_RespectsLimit()
    {
        // Arrange — create entities with enough properties to consume tokens
        var entities = new List<KnowledgeEntity>
        {
            new() { Type = "Endpoint", Name = "GET /api/users", Properties = new()
            {
                ["method"] = "GET",
                ["path"] = "/api/users",
                ["description"] = "Retrieves all users from the system"
            }},
            new() { Type = "Endpoint", Name = "POST /api/orders", Properties = new()
            {
                ["method"] = "POST",
                ["path"] = "/api/orders",
                ["description"] = "Creates a new order in the system"
            }},
            new() { Type = "Endpoint", Name = "DELETE /api/products", Properties = new()
            {
                ["method"] = "DELETE",
                ["path"] = "/api/products",
                ["description"] = "Deletes a product from the catalog"
            }}
        };

        var ranked = _ranker.RankEntities("api endpoint", entities);

        // Act — use a budget that can fit 1-2 entities but not all 3
        // Each entity estimates to ~30 tokens (chars/4 + 10 overhead)
        var selected = _ranker.SelectWithinBudget(ranked, 50);

        // Assert
        selected.Count.Should().BeGreaterThan(0);
        selected.Count.Should().BeLessThan(ranked.Count);
    }

    [Fact]
    public void RankEntities_MatchedTerms_ArePopulated()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            new() { Type = "Endpoint", Name = "GET /api/users", Properties = new() { ["method"] = "GET" } }
        };

        // Act
        var ranked = _ranker.RankEntities("users", entities);

        // Assert
        ranked[0].MatchedTerms.Should().Contain("users");
    }
}
