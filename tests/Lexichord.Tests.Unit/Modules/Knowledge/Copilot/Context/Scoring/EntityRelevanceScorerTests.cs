// =============================================================================
// File: EntityRelevanceScorerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for EntityRelevanceScorer.
// =============================================================================
// LOGIC: Tests multi-signal entity relevance scoring including constructor
//   validation, batch/single scoring, per-signal verification, embedding
//   fallback behavior, cancellation handling, and edge cases.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Modules.Knowledge.Copilot.Context.Scoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Copilot.Context.Scoring;

/// <summary>
/// Unit tests for <see cref="EntityRelevanceScorer"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2f")]
public class EntityRelevanceScorerTests
{
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly IOptions<ScoringConfig> _defaultConfig;
    private readonly ILogger<EntityRelevanceScorer> _logger;

    public EntityRelevanceScorerTests()
    {
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _defaultConfig = Options.Create(new ScoringConfig());
        _logger = NullLogger<EntityRelevanceScorer>.Instance;
    }

    private EntityRelevanceScorer CreateSut(
        IEmbeddingService? embeddingService = null,
        ScoringConfig? config = null)
    {
        var configOptions = config is not null
            ? Options.Create(config)
            : _defaultConfig;

        return new EntityRelevanceScorer(
            configOptions,
            _logger,
            embeddingService);
    }

    private static KnowledgeEntity CreateEntity(
        string name = "Test Entity",
        string type = "Concept",
        Dictionary<string, object>? properties = null,
        DateTimeOffset? modifiedAt = null)
    {
        return new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Properties = properties ?? new Dictionary<string, object>(),
            ModifiedAt = modifiedAt ?? DateTimeOffset.UtcNow
        };
    }

    private static ScoringRequest CreateRequest(
        string query = "test query",
        string? documentContent = null,
        IReadOnlySet<string>? preferredTypes = null)
    {
        return new ScoringRequest
        {
            Query = query,
            DocumentContent = documentContent,
            PreferredEntityTypes = preferredTypes
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntityRelevanceScorer(
            null!, _logger, null);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("config");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntityRelevanceScorer(
            _defaultConfig, null!, null);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullEmbeddingService_DoesNotThrow()
    {
        // Act & Assert — embedding service is optional
        var act = () => CreateSut(embeddingService: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithValidParameters_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateSut(embeddingService: _mockEmbeddingService.Object);
        act.Should().NotThrow();
    }

    #endregion

    #region ScoreEntitiesAsync — Empty Input

    [Fact]
    public async Task ScoreEntitiesAsync_WithEmptyEntities_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest();

        // Act
        var result = await sut.ScoreEntitiesAsync(request, []);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.ScoreEntitiesAsync(null!, [CreateEntity()]);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest();

        // Act & Assert
        var act = () => sut.ScoreEntitiesAsync(request, null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ScoreEntitiesAsync — Without Embeddings

    [Fact]
    public async Task ScoreEntitiesAsync_WithoutEmbeddings_ReturnsScoresWithZeroSemantic()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Authentication Endpoint", "Endpoint");
        var request = CreateRequest("authentication");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Signals.SemanticScore.Should().Be(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithoutEmbeddings_RedistributesWeight()
    {
        // Arrange — without embeddings, non-semantic signals should sum to ~1.0
        var sut = CreateSut(embeddingService: null);

        // Entity modified today (recency = 1.0), name matches query, preferred type
        var entity = CreateEntity("authentication service", "Endpoint",
            modifiedAt: DateTimeOffset.UtcNow);
        var request = CreateRequest("authentication service",
            preferredTypes: new HashSet<string> { "Endpoint" });

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — score should be meaningful even without embeddings
        result.Should().HaveCount(1);
        result[0].Score.Should().BeGreaterThan(0.0f);
        result[0].Score.Should().BeLessOrEqualTo(1.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithoutEmbeddings_SortsByDescendingScore()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);

        var highRelevance = CreateEntity("authentication endpoint", "Endpoint");
        var lowRelevance = CreateEntity("unrelated widget", "Widget");

        var request = CreateRequest("authentication endpoint",
            preferredTypes: new HashSet<string> { "Endpoint" });

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [lowRelevance, highRelevance]);

        // Assert
        result.Should().HaveCount(2);
        result[0].Entity.Name.Should().Be("authentication endpoint");
        result[0].Score.Should().BeGreaterOrEqualTo(result[1].Score);
    }

    #endregion

    #region ScoreEntitiesAsync — With Embeddings

    [Fact]
    public async Task ScoreEntitiesAsync_WithEmbeddings_IncludesSemanticScore()
    {
        // Arrange — mock embedding service to return similar vectors
        _mockEmbeddingService.Setup(e => e.EmbedBatchAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> texts, CancellationToken _) =>
            {
                // Return identical vectors for all texts (high similarity)
                return texts.Select(_ => new float[] { 1, 0, 0 }).ToList();
            });

        var sut = CreateSut(embeddingService: _mockEmbeddingService.Object);
        var entity = CreateEntity("Test Entity", "Concept");
        var request = CreateRequest("test query");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Signals.SemanticScore.Should().BeGreaterThan(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WhenEmbeddingServiceThrows_FallsBackGracefully()
    {
        // Arrange
        _mockEmbeddingService.Setup(e => e.EmbedBatchAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmbeddingException("Service unavailable"));

        var sut = CreateSut(embeddingService: _mockEmbeddingService.Object);
        var entity = CreateEntity("Test Entity", "Concept");
        var request = CreateRequest("test query");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — should not throw, semantic score should be 0
        result.Should().HaveCount(1);
        result[0].Signals.SemanticScore.Should().Be(0.0f);
        result[0].Score.Should().BeGreaterOrEqualTo(0.0f);
    }

    #endregion

    #region Mention Score Signal

    [Fact]
    public async Task ScoreEntitiesAsync_WithDocumentMentions_IncludesMentionScore()
    {
        // Arrange — entity name appears in document content
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Authentication Service", "Concept");
        var request = CreateRequest("auth",
            documentContent: "The Authentication Service handles OAuth2. " +
                             "Authentication Service is essential. " +
                             "Call Authentication Service API.");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — 3 name mentions → score = 3/5 = 0.6 (plus term matches)
        result.Should().HaveCount(1);
        result[0].Signals.MentionScore.Should().BeGreaterThan(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithNullDocumentContent_MentionScoreIsZero()
    {
        // Arrange — no document content
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Test Entity", "Concept");
        var request = CreateRequest("test", documentContent: null);

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Signals.MentionScore.Should().Be(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithFivePlusMentions_MentionScoreSaturates()
    {
        // Arrange — 5+ mentions should saturate at 1.0
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Auth", "Concept");
        var request = CreateRequest("auth",
            documentContent: "Auth Auth Auth Auth Auth Auth Auth Auth Auth Auth");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Signals.MentionScore.Should().Be(1.0f);
    }

    #endregion

    #region Type Score Signal

    [Fact]
    public async Task ScoreEntitiesAsync_WithPreferredType_TypeScoreIsOne()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Test", "Endpoint");
        var request = CreateRequest("test",
            preferredTypes: new HashSet<string> { "Endpoint", "Parameter" });

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Signals.TypeScore.Should().Be(1.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithNonPreferredType_TypeScoreIsLow()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Test", "Widget");
        var request = CreateRequest("test",
            preferredTypes: new HashSet<string> { "Endpoint", "Parameter" });

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Signals.TypeScore.Should().Be(0.3f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithNoPreferredTypes_TypeScoreIsNeutral()
    {
        // Arrange — null preferred types = neutral 0.5
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Test", "Anything");
        var request = CreateRequest("test", preferredTypes: null);

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Signals.TypeScore.Should().Be(0.5f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_PreferredTypesFromRequest_OverrideConfig()
    {
        // Arrange — config has no preferred types, request has them
        var sut = CreateSut(embeddingService: null, config: new ScoringConfig
        {
            PreferredTypes = null // No config-level preferences
        });
        var entity = CreateEntity("Test", "Endpoint");
        var request = CreateRequest("test",
            preferredTypes: new HashSet<string> { "Endpoint" });

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — request types should override config
        result.Should().HaveCount(1);
        result[0].Signals.TypeScore.Should().Be(1.0f);
    }

    #endregion

    #region Recency Score Signal

    [Fact]
    public async Task ScoreEntitiesAsync_WithRecentEntity_RecencyScoreIsHigh()
    {
        // Arrange — entity modified today
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity(modifiedAt: DateTimeOffset.UtcNow);
        var request = CreateRequest("test");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — modified today → recency ≈ 1.0
        result.Should().HaveCount(1);
        result[0].Signals.RecencyScore.Should().BeGreaterThan(0.99f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithOldEntity_RecencyScoreIsLow()
    {
        // Arrange — entity modified 300 days ago (default decay = 365 days)
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity(modifiedAt: DateTimeOffset.UtcNow.AddDays(-300));
        var request = CreateRequest("test");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — 300/365 ≈ 0.82 decay → score ≈ 0.18
        result.Should().HaveCount(1);
        result[0].Signals.RecencyScore.Should().BeLessThan(0.25f);
        result[0].Signals.RecencyScore.Should().BeGreaterThan(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithVeryOldEntity_RecencyScoreIsZero()
    {
        // Arrange — entity modified 400 days ago (beyond 365 decay window)
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity(modifiedAt: DateTimeOffset.UtcNow.AddDays(-400));
        var request = CreateRequest("test");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — clamped to 0.0
        result.Should().HaveCount(1);
        result[0].Signals.RecencyScore.Should().Be(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithCustomDecayDays_RespectsConfig()
    {
        // Arrange — custom 30-day decay window
        var sut = CreateSut(embeddingService: null, config: new ScoringConfig
        {
            RecencyDecayDays = 30
        });
        var entity = CreateEntity(modifiedAt: DateTimeOffset.UtcNow.AddDays(-15));
        var request = CreateRequest("test");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — 15/30 = 0.5 decay → score = 0.5
        result.Should().HaveCount(1);
        result[0].Signals.RecencyScore.Should().BeApproximately(0.5f, 0.05f);
    }

    #endregion

    #region Name Match Score Signal

    [Fact]
    public async Task ScoreEntitiesAsync_WithMatchingNameTerms_NameScoreIsPositive()
    {
        // Arrange — query terms appear in entity name
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Authentication Service", "Concept");
        var request = CreateRequest("authentication service");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — both "authentication" and "service" match as substrings
        result.Should().HaveCount(1);
        result[0].Signals.NameMatchScore.Should().BeGreaterThan(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithNoMatchingTerms_NameScoreIsZero()
    {
        // Arrange — no query terms match entity name
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Widget Factory", "Widget");
        var request = CreateRequest("authentication endpoint");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Signals.NameMatchScore.Should().Be(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithShortQueryTerms_FiltersTermsUnder3Chars()
    {
        // Arrange — "a" and "is" are filtered out (≤2 chars)
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("A Test", "Concept");
        var request = CreateRequest("a is");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — no terms extracted from query
        result.Should().HaveCount(1);
        result[0].Signals.NameMatchScore.Should().Be(0.0f);
    }

    #endregion

    #region Matched Terms

    [Fact]
    public async Task ScoreEntitiesAsync_PopulatesMatchedTerms()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Authentication Endpoint", "Endpoint");
        var request = CreateRequest("authentication endpoint handler");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert — "authentication" and "endpoint" match
        result.Should().HaveCount(1);
        result[0].MatchedTerms.Should().Contain("authentication");
        result[0].MatchedTerms.Should().Contain("endpoint");
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithNoMatches_MatchedTermsIsEmpty()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Widget Factory", "Widget");
        var request = CreateRequest("authentication endpoint");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].MatchedTerms.Should().BeEmpty();
    }

    #endregion

    #region ScoreEntityAsync — Single Entity

    [Fact]
    public async Task ScoreEntityAsync_DelegatesToScoreEntitiesAsync()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("Test Entity", "Concept");
        var request = CreateRequest("test");

        // Act
        var result = await sut.ScoreEntityAsync(request, entity);

        // Assert
        result.Should().NotBeNull();
        result.Entity.Should().Be(entity);
        result.Score.Should().BeGreaterOrEqualTo(0.0f);
        result.Score.Should().BeLessOrEqualTo(1.0f);
    }

    [Fact]
    public async Task ScoreEntityAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.ScoreEntityAsync(null!, CreateEntity());
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ScoreEntityAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest();

        // Act & Assert
        var act = () => sut.ScoreEntityAsync(request, null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Score Composition

    [Fact]
    public async Task ScoreEntitiesAsync_ScoreIsClamped()
    {
        // Arrange — all signals should produce a score in [0, 1]
        var sut = CreateSut(embeddingService: null);
        var entity = CreateEntity("authentication", "Endpoint",
            modifiedAt: DateTimeOffset.UtcNow);
        var request = CreateRequest("authentication",
            documentContent: "authentication authentication authentication authentication authentication authentication",
            preferredTypes: new HashSet<string> { "Endpoint" });

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Score.Should().BeLessOrEqualTo(1.0f);
        result[0].Score.Should().BeGreaterOrEqualTo(0.0f);
    }

    [Fact]
    public async Task ScoreEntitiesAsync_WithAllWeightsZero_ScoreIsZero()
    {
        // Arrange — all weights are zero
        var sut = CreateSut(embeddingService: null, config: new ScoringConfig
        {
            SemanticWeight = 0f,
            MentionWeight = 0f,
            TypeWeight = 0f,
            RecencyWeight = 0f,
            NameMatchWeight = 0f
        });
        var entity = CreateEntity("Test", "Concept", modifiedAt: DateTimeOffset.UtcNow);
        var request = CreateRequest("test");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [entity]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Score.Should().Be(0.0f);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task ScoreEntitiesAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange — embedding service respects cancellation
        _mockEmbeddingService.Setup(e => e.EmbedBatchAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateSut(embeddingService: _mockEmbeddingService.Object);
        var entity = CreateEntity("Test", "Concept");
        var request = CreateRequest("test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = () => sut.ScoreEntitiesAsync(request, [entity], cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Multiple Entities

    [Fact]
    public async Task ScoreEntitiesAsync_WithMultipleEntities_RanksCorrectly()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);

        var bestMatch = CreateEntity("authentication endpoint handler", "Endpoint",
            modifiedAt: DateTimeOffset.UtcNow);
        var mediumMatch = CreateEntity("user authentication", "Concept",
            modifiedAt: DateTimeOffset.UtcNow);
        var poorMatch = CreateEntity("logging framework", "Library",
            modifiedAt: DateTimeOffset.UtcNow.AddDays(-400));

        var request = CreateRequest("authentication endpoint",
            preferredTypes: new HashSet<string> { "Endpoint" });

        // Act
        var result = await sut.ScoreEntitiesAsync(
            request, [poorMatch, mediumMatch, bestMatch]);

        // Assert — best match should be first
        result.Should().HaveCount(3);
        result[0].Entity.Name.Should().Be("authentication endpoint handler");
        result[^1].Entity.Name.Should().Be("logging framework");
    }

    [Fact]
    public async Task ScoreEntitiesAsync_AllEntitiesReturnedRegardlessOfScore()
    {
        // Arrange
        var sut = CreateSut(embeddingService: null);
        var entities = Enumerable.Range(0, 5)
            .Select(i => CreateEntity($"Entity_{i}", "Type"))
            .ToList();
        var request = CreateRequest("completely unrelated query xyz");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, entities);

        // Assert — all entities included regardless of score
        result.Should().HaveCount(5);
    }

    #endregion

    #region Custom Config Weights

    [Fact]
    public async Task ScoreEntitiesAsync_WithCustomWeights_RespectsConfig()
    {
        // Arrange — type weight is dominant
        var sut = CreateSut(embeddingService: null, config: new ScoringConfig
        {
            SemanticWeight = 0f,
            MentionWeight = 0f,
            TypeWeight = 1.0f,
            RecencyWeight = 0f,
            NameMatchWeight = 0f,
            PreferredTypes = new HashSet<string> { "Preferred" }
        });

        var preferred = CreateEntity("Widget", "Preferred");
        var other = CreateEntity("authentication", "Other");

        var request = CreateRequest("authentication");

        // Act
        var result = await sut.ScoreEntitiesAsync(request, [other, preferred]);

        // Assert — preferred type should rank higher despite worse name match
        result.Should().HaveCount(2);
        result[0].Entity.Type.Should().Be("Preferred");
    }

    #endregion

    #region ExtractTerms

    [Fact]
    public void ExtractTerms_SplitsOnDelimiters()
    {
        // Act
        var terms = EntityRelevanceScorer.ExtractTerms("hello,world.foo/bar-baz_qux");

        // Assert
        terms.Should().BeEquivalentTo(["hello", "world", "foo", "bar", "baz", "qux"]);
    }

    [Fact]
    public void ExtractTerms_FiltersShortTerms()
    {
        // Act
        var terms = EntityRelevanceScorer.ExtractTerms("a to the authentication endpoint");

        // Assert — "a" and "to" are ≤2 chars and filtered; "the" (3 chars) passes
        terms.Should().BeEquivalentTo(["the", "authentication", "endpoint"]);
    }

    [Fact]
    public void ExtractTerms_DeduplicatesTerms()
    {
        // Act
        var terms = EntityRelevanceScorer.ExtractTerms("hello hello hello");

        // Assert
        terms.Should().HaveCount(1);
        terms.Should().Contain("hello");
    }

    [Fact]
    public void ExtractTerms_ConvertsToLowercase()
    {
        // Act
        var terms = EntityRelevanceScorer.ExtractTerms("Authentication ENDPOINT Handler");

        // Assert
        terms.Should().OnlyContain(t => t == t.ToLowerInvariant());
    }

    #endregion
}
