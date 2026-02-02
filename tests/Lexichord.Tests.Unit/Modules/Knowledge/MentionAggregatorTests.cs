// =============================================================================
// File: MentionAggregatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the MentionAggregator mention grouping logic.
// =============================================================================
// LOGIC: Validates that MentionAggregator correctly groups mentions into unique
//   entities by (type, normalized value) and produces AggregatedEntity records
//   with proper canonical values, merged properties, and ordering.
//
// Test Categories:
//   - Single mention aggregation
//   - Multi-mention grouping (same entity)
//   - Canonical value selection (highest confidence)
//   - Property merging (first-seen wins)
//   - Source document collection
//   - Ordering by count desc, then confidence desc
//   - Empty input handling
//   - Case-insensitive grouping
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Extraction;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="MentionAggregator"/> mention grouping logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5g")]
public sealed class MentionAggregatorTests
{
    private readonly MentionAggregator _aggregator = new();

    #region Test Helpers

    private static EntityMention MakeMention(
        string type,
        string value,
        string? normalizedValue = null,
        float confidence = 1.0f,
        Guid? chunkId = null,
        Dictionary<string, object>? properties = null)
    {
        return new EntityMention
        {
            EntityType = type,
            Value = value,
            NormalizedValue = normalizedValue ?? value.ToLowerInvariant(),
            Confidence = confidence,
            ChunkId = chunkId,
            Properties = properties ?? new()
        };
    }

    #endregion

    #region Single Mention Aggregation

    [Fact]
    public void Aggregate_SingleMention_ReturnsOneEntity()
    {
        // Arrange
        var mentions = new[] { MakeMention("Endpoint", "GET /users", "/users") };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().ContainSingle();
        result[0].EntityType.Should().Be("Endpoint");
        result[0].CanonicalValue.Should().Be("/users");
        result[0].Mentions.Should().HaveCount(1);
    }

    #endregion

    #region Multi-Mention Grouping

    [Fact]
    public void Aggregate_SameEntityMultipleMentions_GroupedIntoOne()
    {
        // Arrange
        var mentions = new[]
        {
            MakeMention("Endpoint", "GET /users", "/users", confidence: 1.0f),
            MakeMention("Endpoint", "POST /users", "/users", confidence: 0.9f)
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().ContainSingle();
        result[0].Mentions.Should().HaveCount(2);
    }

    [Fact]
    public void Aggregate_DifferentEntities_SeparateGroups()
    {
        // Arrange
        var mentions = new[]
        {
            MakeMention("Endpoint", "GET /users", "/users"),
            MakeMention("Parameter", "userId", "userid")
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Aggregate_SameValueDifferentTypes_SeparateGroups()
    {
        // Arrange
        var mentions = new[]
        {
            MakeMention("Endpoint", "users", "users"),
            MakeMention("Concept", "users", "users")
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Canonical Value Selection

    [Fact]
    public void Aggregate_MultiMentions_CanonicalValueFromHighestConfidence()
    {
        // Arrange
        var mentions = new[]
        {
            MakeMention("Endpoint", "GET /Users", "/users", confidence: 0.7f),
            MakeMention("Endpoint", "POST /users", "/users", confidence: 1.0f)
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().ContainSingle();
        result[0].CanonicalValue.Should().Be("/users");
        result[0].MaxConfidence.Should().Be(1.0f);
    }

    #endregion

    #region Property Merging

    [Fact]
    public void Aggregate_PropertiesMerged_FirstSeenWins()
    {
        // Arrange
        var mentions = new[]
        {
            MakeMention("Endpoint", "GET /users", "/users", confidence: 1.0f,
                properties: new() { ["method"] = "GET", ["path"] = "/users" }),
            MakeMention("Endpoint", "POST /users", "/users", confidence: 0.9f,
                properties: new() { ["method"] = "POST", ["auth"] = "required" })
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().ContainSingle();
        result[0].MergedProperties["method"].Should().Be("GET"); // First-seen from highest confidence
        result[0].MergedProperties["path"].Should().Be("/users");
        result[0].MergedProperties["auth"].Should().Be("required");
    }

    #endregion

    #region Source Document Collection

    [Fact]
    public void Aggregate_ChunkIds_CollectedAsSourceDocuments()
    {
        // Arrange
        var chunk1 = Guid.NewGuid();
        var chunk2 = Guid.NewGuid();
        var mentions = new[]
        {
            MakeMention("Endpoint", "GET /users", "/users", chunkId: chunk1),
            MakeMention("Endpoint", "POST /users", "/users", chunkId: chunk2)
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().ContainSingle();
        result[0].SourceDocuments.Should().HaveCount(2);
        result[0].SourceDocuments.Should().Contain(chunk1);
        result[0].SourceDocuments.Should().Contain(chunk2);
    }

    [Fact]
    public void Aggregate_NullChunkIds_ExcludedFromSourceDocuments()
    {
        // Arrange
        var mentions = new[]
        {
            MakeMention("Endpoint", "GET /users", "/users", chunkId: null),
            MakeMention("Endpoint", "POST /users", "/users", chunkId: null)
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().ContainSingle();
        result[0].SourceDocuments.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_DuplicateChunkIds_Deduplicated()
    {
        // Arrange
        var chunkId = Guid.NewGuid();
        var mentions = new[]
        {
            MakeMention("Endpoint", "GET /users", "/users", chunkId: chunkId),
            MakeMention("Endpoint", "POST /users", "/users", chunkId: chunkId)
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().ContainSingle();
        result[0].SourceDocuments.Should().ContainSingle();
    }

    #endregion

    #region Ordering

    [Fact]
    public void Aggregate_OrderedByMentionCountDescThenConfidenceDesc()
    {
        // Arrange
        var mentions = new[]
        {
            // Entity A: 1 mention, confidence 1.0
            MakeMention("Endpoint", "GET /single", "/single", confidence: 1.0f),
            // Entity B: 3 mentions, confidence 0.7
            MakeMention("Endpoint", "GET /popular", "/popular", confidence: 0.7f),
            MakeMention("Endpoint", "POST /popular", "/popular", confidence: 0.7f),
            MakeMention("Endpoint", "PUT /popular", "/popular", confidence: 0.7f),
            // Entity C: 1 mention, confidence 0.5
            MakeMention("Concept", "Auth", "auth", confidence: 0.5f)
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().HaveCount(3);
        result[0].CanonicalValue.Should().Be("/popular"); // 3 mentions
        // Second and third ordered by confidence
        result[1].MaxConfidence.Should().BeGreaterOrEqualTo(result[2].MaxConfidence);
    }

    #endregion

    #region Empty Input

    [Fact]
    public void Aggregate_EmptyInput_ReturnsEmpty()
    {
        // Act
        var result = _aggregator.Aggregate(Array.Empty<EntityMention>());

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Case-Insensitive Grouping

    [Fact]
    public void Aggregate_CaseInsensitiveGrouping_GroupedCorrectly()
    {
        // Arrange
        var mentions = new[]
        {
            MakeMention("Endpoint", "GET /Users", "/users", confidence: 1.0f),
            MakeMention("Endpoint", "POST /USERS", "/users", confidence: 0.9f)
        };

        // Act
        var result = _aggregator.Aggregate(mentions);

        // Assert
        result.Should().ContainSingle();
        result[0].Mentions.Should().HaveCount(2);
    }

    #endregion
}
