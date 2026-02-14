// =============================================================================
// File: KnowledgeContextProviderTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.Context.KnowledgeContextProvider
// Feature: v0.6.6e
// =============================================================================
// Updated for v0.7.2f: Constructor accepts optional IEntityRelevanceScorer?.
// Updated for v0.7.2g: Provider now uses FormatWithMetadata() instead of
//   FormatContext() + EstimateTokens() separately.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Modules.Knowledge.Axioms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.6e")]
public class KnowledgeContextProviderTests
{
    private readonly IGraphRepository _graphRepository;
    private readonly IAxiomStore _axiomStore;
    private readonly IClaimRepository _claimRepository;
    private readonly IEntityRelevanceRanker _ranker;
    private readonly IKnowledgeContextFormatter _formatter;
    private readonly IKnowledgeContextProvider _provider;

    public KnowledgeContextProviderTests()
    {
        _graphRepository = Substitute.For<IGraphRepository>();
        _axiomStore = Substitute.For<IAxiomStore>();
        _claimRepository = Substitute.For<IClaimRepository>();
        _ranker = Substitute.For<IEntityRelevanceRanker>();
        _formatter = Substitute.For<IKnowledgeContextFormatter>();

        var providerType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Context.KnowledgeContextProvider")!;
        var loggerType = typeof(Logger<>).MakeGenericType(providerType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);

        // v0.7.2f: Constructor now accepts optional IEntityRelevanceScorer? as 7th param.
        // Pass null to use fallback term-based ranker.
        _provider = (IKnowledgeContextProvider)Activator.CreateInstance(
            providerType,
            _graphRepository,
            _axiomStore,
            _claimRepository,
            _ranker,
            _formatter,
            logger,
            null)!;
    }

    private static KnowledgeContextOptions DefaultOptions => new()
    {
        MaxTokens = 2000,
        MaxEntities = 20,
        IncludeRelationships = true,
        IncludeAxioms = true,
        IncludeClaims = false,
        Format = ContextFormat.Markdown
    };

    /// <summary>
    /// Configures the formatter mock to return a FormattedContext from FormatWithMetadata.
    /// v0.7.2g: Provider now calls FormatWithMetadata() instead of FormatContext() + EstimateTokens().
    /// </summary>
    private void SetupFormatterMock(string content, int tokenCount)
    {
        _formatter.FormatWithMetadata(
                Arg.Any<IReadOnlyList<KnowledgeEntity>>(),
                Arg.Any<IReadOnlyList<KnowledgeRelationship>?>(),
                Arg.Any<IReadOnlyList<Axiom>?>(),
                Arg.Any<ContextFormatOptions>())
            .Returns(new FormattedContext
            {
                Content = content,
                TokenCount = tokenCount,
                Format = ContextFormat.Markdown
            });
    }

    [Fact]
    public async Task GetContext_ReturnsRelevantEntities()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            new() { Type = "Endpoint", Name = "GET /api/users" },
            new() { Type = "Parameter", Name = "userId" }
        };

        _graphRepository.SearchEntitiesAsync(Arg.Any<EntitySearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(entities);

        var ranked = entities.Select(e => new RankedEntity
        {
            Entity = e,
            RelevanceScore = 1.0f,
            EstimatedTokens = 50
        }).ToList();
        _ranker.RankEntities("users", Arg.Any<IReadOnlyList<KnowledgeEntity>>())
            .Returns(ranked);
        _ranker.SelectWithinBudget(Arg.Any<IReadOnlyList<RankedEntity>>(), Arg.Any<int>())
            .Returns(entities);

        _graphRepository.GetRelationshipsForEntityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeRelationship>());
        _axiomStore.GetAxiomsForType(Arg.Any<string>())
            .Returns(new List<Axiom>());

        SetupFormatterMock("## Formatted context", 50);

        // Act
        var context = await _provider.GetContextAsync("users", DefaultOptions);

        // Assert
        context.Entities.Should().HaveCount(2);
        context.FormattedContext.Should().Contain("Formatted context");
        context.OriginalQuery.Should().Be("users");
    }

    [Fact]
    public async Task GetContext_EmptyResults_ReturnsEmpty()
    {
        // Arrange
        _graphRepository.SearchEntitiesAsync(Arg.Any<EntitySearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeEntity>());

        // Act
        var context = await _provider.GetContextAsync("nonexistent", DefaultOptions);

        // Assert
        context.Entities.Should().BeEmpty();
        context.FormattedContext.Should().BeEmpty();
        context.TokenCount.Should().Be(0);
    }

    [Fact]
    public async Task GetContext_RespectsTokenBudget()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            new() { Type = "Endpoint", Name = "GET /api/users" },
            new() { Type = "Endpoint", Name = "POST /api/orders" },
            new() { Type = "Endpoint", Name = "DELETE /api/products" }
        };

        _graphRepository.SearchEntitiesAsync(Arg.Any<EntitySearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(entities);

        var ranked = entities.Select((e, i) => new RankedEntity
        {
            Entity = e,
            RelevanceScore = 1.0f - (i * 0.1f),
            EstimatedTokens = 50
        }).ToList();
        _ranker.RankEntities(Arg.Any<string>(), Arg.Any<IReadOnlyList<KnowledgeEntity>>())
            .Returns(ranked);

        // Only select 2 of 3 entities
        var selected = entities.Take(2).ToList();
        _ranker.SelectWithinBudget(Arg.Any<IReadOnlyList<RankedEntity>>(), Arg.Any<int>())
            .Returns(selected);

        _graphRepository.GetRelationshipsForEntityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeRelationship>());
        _axiomStore.GetAxiomsForType(Arg.Any<string>())
            .Returns(new List<Axiom>());

        SetupFormatterMock("context", 100);

        // Act
        var context = await _provider.GetContextAsync("api", DefaultOptions);

        // Assert
        context.WasTruncated.Should().BeTrue();
        context.Entities.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetContext_IncludesRelationships()
    {
        // Arrange
        var entity = new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" };
        var entities = new List<KnowledgeEntity> { entity };

        _graphRepository.SearchEntitiesAsync(Arg.Any<EntitySearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(entities);

        var ranked = new List<RankedEntity>
        {
            new() { Entity = entity, RelevanceScore = 1.0f, EstimatedTokens = 50 }
        };
        _ranker.RankEntities(Arg.Any<string>(), Arg.Any<IReadOnlyList<KnowledgeEntity>>())
            .Returns(ranked);
        _ranker.SelectWithinBudget(Arg.Any<IReadOnlyList<RankedEntity>>(), Arg.Any<int>())
            .Returns(entities);

        var relationships = new List<KnowledgeRelationship>
        {
            new() { Type = "ACCEPTS", FromEntityId = entity.Id, ToEntityId = Guid.NewGuid() }
        };
        _graphRepository.GetRelationshipsForEntityAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(relationships);

        _axiomStore.GetAxiomsForType(Arg.Any<string>())
            .Returns(new List<Axiom>());

        SetupFormatterMock("context", 50);

        // Act
        var options = DefaultOptions with { IncludeRelationships = true };
        var context = await _provider.GetContextAsync("users", options);

        // Assert
        context.Relationships.Should().NotBeNull();
        context.Relationships!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetContextForEntities_SkipsInvalidIds()
    {
        // Arrange
        var validEntity = new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" };
        var validId = validEntity.Id;
        var invalidId = Guid.NewGuid();

        _graphRepository.GetByIdAsync(validId, Arg.Any<CancellationToken>())
            .Returns(validEntity);
        _graphRepository.GetByIdAsync(invalidId, Arg.Any<CancellationToken>())
            .Returns((KnowledgeEntity?)null);

        _graphRepository.GetRelationshipsForEntityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeRelationship>());
        _axiomStore.GetAxiomsForType(Arg.Any<string>())
            .Returns(new List<Axiom>());

        SetupFormatterMock("context", 20);

        // Act
        var context = await _provider.GetContextForEntitiesAsync(
            [validId, invalidId], DefaultOptions);

        // Assert — only the valid entity should be in the context
        context.Entities.Should().HaveCount(1);
        context.Entities[0].Id.Should().Be(validId);
    }
}
