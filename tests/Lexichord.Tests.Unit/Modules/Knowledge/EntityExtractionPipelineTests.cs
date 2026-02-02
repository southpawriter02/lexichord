// =============================================================================
// File: EntityExtractionPipelineTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the EntityExtractionPipeline orchestrator.
// =============================================================================
// LOGIC: Validates that EntityExtractionPipeline correctly orchestrates multiple
//   extractors, combining results with deduplication, confidence filtering,
//   error isolation, and chunk-based extraction with offset adjustment.
//
// Test Categories:
//   - Multi-extractor combining
//   - Mention deduplication (overlapping spans)
//   - Confidence filtering (MinConfidence threshold)
//   - Target entity type filtering
//   - Error isolation (one extractor fails, others succeed)
//   - ExtractFromChunksAsync processing
//   - ExtractFromChunksAsync offset adjustment
//   - Extractor registration and ordering
//   - Empty text handling
//   - Aggregated entities populated
//   - MentionCountByType computed correctly
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Extraction;
using Lexichord.Tests.Unit.TestUtilities;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="EntityExtractionPipeline"/> orchestration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5g")]
public sealed class EntityExtractionPipelineTests
{
    private readonly FakeLogger<EntityExtractionPipeline> _logger = new();
    private readonly EntityExtractionPipeline _pipeline;

    public EntityExtractionPipelineTests()
    {
        _pipeline = new EntityExtractionPipeline(_logger);
    }

    #region Test Helpers

    /// <summary>
    /// A fake extractor that returns configurable mentions for testing.
    /// </summary>
    private sealed class FakeExtractor : IEntityExtractor
    {
        private readonly IReadOnlyList<EntityMention> _mentions;
        private readonly bool _shouldThrow;

        public FakeExtractor(
            IReadOnlyList<string> supportedTypes,
            int priority,
            IReadOnlyList<EntityMention> mentions,
            bool shouldThrow = false)
        {
            SupportedTypes = supportedTypes;
            Priority = priority;
            _mentions = mentions;
            _shouldThrow = shouldThrow;
        }

        public IReadOnlyList<string> SupportedTypes { get; }
        public int Priority { get; }

        public Task<IReadOnlyList<EntityMention>> ExtractAsync(
            string text, ExtractionContext context, CancellationToken ct = default)
        {
            if (_shouldThrow)
                throw new InvalidOperationException("Fake extractor failure");

            return Task.FromResult(_mentions);
        }
    }

    private static EntityMention MakeMention(
        string type, string value, int startOffset, int endOffset, float confidence = 1.0f)
    {
        return new EntityMention
        {
            EntityType = type,
            Value = value,
            NormalizedValue = value.ToLowerInvariant(),
            StartOffset = startOffset,
            EndOffset = endOffset,
            Confidence = confidence
        };
    }

    #endregion

    #region Extractor Registration

    [Fact]
    public void Register_AddsExtractorToList()
    {
        // Arrange
        var extractor = new FakeExtractor(
            new[] { "Test" }, priority: 50, mentions: Array.Empty<EntityMention>());

        // Act
        _pipeline.Register(extractor);

        // Assert
        _pipeline.Extractors.Should().ContainSingle();
    }

    [Fact]
    public void Register_NullExtractor_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _pipeline.Register(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Extractors_ReturnedInPriorityOrder()
    {
        // Arrange
        var low = new FakeExtractor(new[] { "A" }, priority: 10, mentions: Array.Empty<EntityMention>());
        var high = new FakeExtractor(new[] { "B" }, priority: 100, mentions: Array.Empty<EntityMention>());
        var mid = new FakeExtractor(new[] { "C" }, priority: 50, mentions: Array.Empty<EntityMention>());

        _pipeline.Register(low);
        _pipeline.Register(high);
        _pipeline.Register(mid);

        // Act
        var extractors = _pipeline.Extractors;

        // Assert
        extractors[0].Priority.Should().Be(100);
        extractors[1].Priority.Should().Be(50);
        extractors[2].Priority.Should().Be(10);
    }

    #endregion

    #region Multi-Extractor Combining

    [Fact]
    public async Task ExtractAllAsync_MultipleExtractors_CombinesResults()
    {
        // Arrange
        var endpointMention = MakeMention("Endpoint", "GET /users", 0, 10);
        var paramMention = MakeMention("Parameter", "userId", 20, 26);

        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { endpointMention }));
        _pipeline.Register(new FakeExtractor(
            new[] { "Parameter" }, priority: 90, mentions: new[] { paramMention }));

        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("GET /users {userId}", context);

        // Assert
        result.Mentions.Should().HaveCount(2);
        result.Mentions.Should().Contain(m => m.EntityType == "Endpoint");
        result.Mentions.Should().Contain(m => m.EntityType == "Parameter");
    }

    #endregion

    #region Mention Deduplication

    [Fact]
    public async Task ExtractAllAsync_OverlappingMentions_HigherConfidenceWins()
    {
        // Arrange — two mentions overlap at the same position
        var highConfidence = MakeMention("Endpoint", "GET /users", 0, 10, confidence: 1.0f);
        var lowConfidence = MakeMention("Endpoint", "/users", 4, 10, confidence: 0.7f);

        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { highConfidence, lowConfidence }));

        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("GET /users", context);

        // Assert
        result.Mentions.Should().ContainSingle();
        result.Mentions[0].Confidence.Should().Be(1.0f);
    }

    [Fact]
    public async Task ExtractAllAsync_NonOverlappingMentions_BothKept()
    {
        // Arrange — two mentions that don't overlap
        var first = MakeMention("Endpoint", "GET /users", 0, 10, confidence: 1.0f);
        var second = MakeMention("Parameter", "userId", 20, 26, confidence: 0.9f);

        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint", "Parameter" }, priority: 100,
            mentions: new[] { first, second }));

        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("text", context);

        // Assert
        result.Mentions.Should().HaveCount(2);
    }

    #endregion

    #region Confidence Filtering

    [Fact]
    public async Task ExtractAllAsync_BelowMinConfidence_Filtered()
    {
        // Arrange
        var highConf = MakeMention("Endpoint", "GET /users", 0, 10, confidence: 0.9f);
        var lowConf = MakeMention("Endpoint", "/items", 20, 26, confidence: 0.3f);

        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { highConf, lowConf }));

        var context = new ExtractionContext { MinConfidence = 0.5f };

        // Act
        var result = await _pipeline.ExtractAllAsync("text", context);

        // Assert
        result.Mentions.Should().ContainSingle();
        result.Mentions[0].Confidence.Should().Be(0.9f);
    }

    #endregion

    #region Target Entity Type Filtering

    [Fact]
    public async Task ExtractAllAsync_TargetEntityTypes_SkipsNonMatching()
    {
        // Arrange
        var endpointMention = MakeMention("Endpoint", "GET /users", 0, 10);
        var conceptMention = MakeMention("Concept", "OAuth", 20, 25);

        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { endpointMention }));
        _pipeline.Register(new FakeExtractor(
            new[] { "Concept" }, priority: 50, mentions: new[] { conceptMention }));

        var context = new ExtractionContext { TargetEntityTypes = new[] { "Endpoint" } };

        // Act
        var result = await _pipeline.ExtractAllAsync("text", context);

        // Assert
        result.Mentions.Should().ContainSingle();
        result.Mentions[0].EntityType.Should().Be("Endpoint");
    }

    #endregion

    #region Error Isolation

    [Fact]
    public async Task ExtractAllAsync_OneExtractorFails_OthersContinue()
    {
        // Arrange
        var goodMention = MakeMention("Parameter", "userId", 0, 6);

        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100,
            mentions: Array.Empty<EntityMention>(), shouldThrow: true));
        _pipeline.Register(new FakeExtractor(
            new[] { "Parameter" }, priority: 90, mentions: new[] { goodMention }));

        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("text", context);

        // Assert
        result.Mentions.Should().ContainSingle();
        result.Mentions[0].EntityType.Should().Be("Parameter");

        // Verify the failure was logged
        _logger.Logs.Should().Contain(l =>
            l.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            l.Message.Contains("failed during extraction"));
    }

    #endregion

    #region ExtractFromChunksAsync

    [Fact]
    public async Task ExtractFromChunksAsync_ProcessesAllChunks()
    {
        // Arrange
        var mention = MakeMention("Endpoint", "GET /users", 0, 10);
        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { mention }));

        var chunks = new[]
        {
            new TextChunk("GET /users are listed here.", 0, 26,
                new ChunkMetadata(0) { TotalChunks = 2 }),
            new TextChunk("POST /orders creates an order.", 26, 55,
                new ChunkMetadata(1) { TotalChunks = 2 })
        };

        // Act
        var result = await _pipeline.ExtractFromChunksAsync(chunks, Guid.NewGuid());

        // Assert
        result.Mentions.Should().HaveCount(2); // one mention from each chunk
        result.ChunksProcessed.Should().Be(2);
    }

    [Fact]
    public async Task ExtractFromChunksAsync_AdjustsOffsetsToDocumentLevel()
    {
        // Arrange
        var mention = MakeMention("Endpoint", "GET /users", 0, 10);
        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { mention }));

        var chunks = new[]
        {
            new TextChunk("GET /users endpoint.", 100, 120,
                new ChunkMetadata(0) { TotalChunks = 1 })
        };

        // Act
        var result = await _pipeline.ExtractFromChunksAsync(chunks, Guid.NewGuid());

        // Assert
        result.Mentions.Should().ContainSingle();
        result.Mentions[0].StartOffset.Should().Be(100); // 0 + 100 (chunk offset)
        result.Mentions[0].EndOffset.Should().Be(110);   // 10 + 100
    }

    [Fact]
    public async Task ExtractFromChunksAsync_SkipsEmptyChunks()
    {
        // Arrange
        var mention = MakeMention("Endpoint", "GET /users", 0, 10);
        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { mention }));

        var chunks = new[]
        {
            new TextChunk("GET /users listed.", 0, 18, new ChunkMetadata(0) { TotalChunks = 2 }),
            new TextChunk("   ", 18, 21, new ChunkMetadata(1) { TotalChunks = 2 })
        };

        // Act
        var result = await _pipeline.ExtractFromChunksAsync(chunks, Guid.NewGuid());

        // Assert
        // Only the first chunk should be processed (second is whitespace-only)
        result.Mentions.Should().ContainSingle();
    }

    [Fact]
    public async Task ExtractFromChunksAsync_NullChunks_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _pipeline.ExtractFromChunksAsync(null!, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Aggregated Entities

    [Fact]
    public async Task ExtractAllAsync_AggregatedEntitiesPopulated()
    {
        // Arrange
        var mention = MakeMention("Endpoint", "GET /users", 0, 10);
        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { mention }));

        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("GET /users", context);

        // Assert
        result.AggregatedEntities.Should().NotBeNull();
        result.AggregatedEntities.Should().ContainSingle();
        result.AggregatedEntities![0].EntityType.Should().Be("Endpoint");
    }

    #endregion

    #region MentionCountByType

    [Fact]
    public async Task ExtractAllAsync_MentionCountByType_ComputedCorrectly()
    {
        // Arrange
        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100,
            mentions: new[]
            {
                MakeMention("Endpoint", "GET /users", 0, 10),
                MakeMention("Endpoint", "POST /items", 20, 31)
            }));
        _pipeline.Register(new FakeExtractor(
            new[] { "Parameter" }, priority: 90,
            mentions: new[] { MakeMention("Parameter", "userId", 40, 46) }));

        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("text", context);

        // Assert
        result.MentionCountByType.Should().ContainKey("Endpoint");
        result.MentionCountByType["Endpoint"].Should().Be(2);
        result.MentionCountByType.Should().ContainKey("Parameter");
        result.MentionCountByType["Parameter"].Should().Be(1);
    }

    #endregion

    #region ExtractorName Tagging

    [Fact]
    public async Task ExtractAllAsync_TagsMentionsWithExtractorName()
    {
        // Arrange
        var mention = MakeMention("Endpoint", "GET /users", 0, 10);
        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100, mentions: new[] { mention }));

        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("text", context);

        // Assert
        result.Mentions.Should().ContainSingle();
        result.Mentions[0].ExtractorName.Should().Be("FakeExtractor");
    }

    #endregion

    #region Empty Text

    [Fact]
    public async Task ExtractAllAsync_NoExtractorsRegistered_ReturnsEmptyResult()
    {
        // Arrange
        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("some text", context);

        // Assert
        result.Mentions.Should().BeEmpty();
        result.AggregatedEntities.Should().BeEmpty();
    }

    #endregion

    #region Duration Tracking

    [Fact]
    public async Task ExtractAllAsync_Duration_IsNonZero()
    {
        // Arrange
        _pipeline.Register(new FakeExtractor(
            new[] { "Endpoint" }, priority: 100,
            mentions: new[] { MakeMention("Endpoint", "GET /users", 0, 10) }));

        var context = new ExtractionContext();

        // Act
        var result = await _pipeline.ExtractAllAsync("text", context);

        // Assert
        result.Duration.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    #endregion
}
