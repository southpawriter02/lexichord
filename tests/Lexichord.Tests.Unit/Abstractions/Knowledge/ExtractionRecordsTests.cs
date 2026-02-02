// =============================================================================
// File: ExtractionRecordsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for entity extraction records and their computed
//              properties.
// =============================================================================
// LOGIC: Validates the correctness of all extraction record types defined in
//   Lexichord.Abstractions.Contracts: EntityMention, ExtractionContext,
//   ExtractionResult, and AggregatedEntity.
//
// Test Categories:
//   - EntityMention: Required properties, defaults, computed Length, equality
//   - ExtractionContext: Defaults, configuration properties
//   - ExtractionResult: Computed AverageConfidence, empty results
//   - AggregatedEntity: Required properties, defaults
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge;

/// <summary>
/// Unit tests for entity extraction records and their computed properties.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5g")]
public sealed class ExtractionRecordsTests
{
    #region EntityMention Tests

    [Fact]
    public void EntityMention_RequiredProperties_Populated()
    {
        // Arrange & Act
        var mention = new EntityMention
        {
            EntityType = "Endpoint",
            Value = "GET /users"
        };

        // Assert
        mention.EntityType.Should().Be("Endpoint");
        mention.Value.Should().Be("GET /users");
    }

    [Fact]
    public void EntityMention_DefaultValues_Correct()
    {
        // Arrange & Act
        var mention = new EntityMention
        {
            EntityType = "Endpoint",
            Value = "GET /users"
        };

        // Assert
        mention.NormalizedValue.Should().BeNull();
        mention.StartOffset.Should().Be(0);
        mention.EndOffset.Should().Be(0);
        mention.Confidence.Should().Be(1.0f);
        mention.Properties.Should().BeEmpty();
        mention.ChunkId.Should().BeNull();
        mention.ExtractorName.Should().BeNull();
        mention.SourceSnippet.Should().BeNull();
    }

    [Fact]
    public void EntityMention_Length_ComputedFromOffsets()
    {
        // Arrange & Act
        var mention = new EntityMention
        {
            EntityType = "Endpoint",
            Value = "GET /users",
            StartOffset = 10,
            EndOffset = 20
        };

        // Assert
        mention.Length.Should().Be(10);
    }

    [Fact]
    public void EntityMention_Length_ZeroWhenOffsetsEqual()
    {
        // Arrange & Act
        var mention = new EntityMention
        {
            EntityType = "Endpoint",
            Value = "test",
            StartOffset = 5,
            EndOffset = 5
        };

        // Assert
        mention.Length.Should().Be(0);
    }

    [Fact]
    public void EntityMention_WithAllProperties_Populated()
    {
        // Arrange
        var chunkId = Guid.NewGuid();

        // Act
        var mention = new EntityMention
        {
            EntityType = "Parameter",
            Value = "userId",
            NormalizedValue = "userid",
            StartOffset = 42,
            EndOffset = 48,
            Confidence = 0.95f,
            Properties = new() { ["name"] = "userId", ["location"] = "path" },
            ChunkId = chunkId,
            ExtractorName = "ParameterExtractor",
            SourceSnippet = "GET /users/{userId}"
        };

        // Assert
        mention.NormalizedValue.Should().Be("userid");
        mention.Confidence.Should().Be(0.95f);
        mention.Properties.Should().HaveCount(2);
        mention.ChunkId.Should().Be(chunkId);
        mention.ExtractorName.Should().Be("ParameterExtractor");
        mention.SourceSnippet.Should().Be("GET /users/{userId}");
    }

    [Fact]
    public void EntityMention_Equality_SameValuesAndSamePropertyReference()
    {
        // Arrange — records with Dictionary properties require the same
        // Dictionary reference for structural equality (Dictionary uses
        // reference equality).
        var props = new Dictionary<string, object>();
        var a = new EntityMention
        {
            EntityType = "Endpoint",
            Value = "GET /users",
            StartOffset = 0,
            EndOffset = 10,
            Confidence = 1.0f,
            Properties = props
        };
        var b = new EntityMention
        {
            EntityType = "Endpoint",
            Value = "GET /users",
            StartOffset = 0,
            EndOffset = 10,
            Confidence = 1.0f,
            Properties = props
        };

        // Assert
        a.Should().Be(b);
    }

    [Fact]
    public void EntityMention_Equality_DifferentValues()
    {
        // Arrange
        var a = new EntityMention { EntityType = "Endpoint", Value = "GET /users" };
        var b = new EntityMention { EntityType = "Parameter", Value = "userId" };

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void EntityMention_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new EntityMention
        {
            EntityType = "Endpoint",
            Value = "GET /users",
            Confidence = 1.0f
        };

        // Act
        var modified = original with { ExtractorName = "EndpointExtractor" };

        // Assert
        modified.EntityType.Should().Be("Endpoint");
        modified.Value.Should().Be("GET /users");
        modified.ExtractorName.Should().Be("EndpointExtractor");
        original.ExtractorName.Should().BeNull();
    }

    #endregion

    #region ExtractionContext Tests

    [Fact]
    public void ExtractionContext_DefaultValues_Correct()
    {
        // Arrange & Act
        var context = new ExtractionContext();

        // Assert
        context.DocumentId.Should().BeNull();
        context.ChunkId.Should().BeNull();
        context.DocumentTitle.Should().BeNull();
        context.Heading.Should().BeNull();
        context.HeadingLevel.Should().BeNull();
        context.Schema.Should().BeNull();
        context.MinConfidence.Should().Be(0.5f);
        context.TargetEntityTypes.Should().BeNull();
        context.DiscoveryMode.Should().BeFalse();
    }

    [Fact]
    public void ExtractionContext_WithAllProperties_Populated()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();

        // Act
        var context = new ExtractionContext
        {
            DocumentId = docId,
            ChunkId = chunkId,
            DocumentTitle = "API Documentation",
            Heading = "Authentication",
            HeadingLevel = 2,
            MinConfidence = 0.8f,
            TargetEntityTypes = new[] { "Endpoint", "Parameter" },
            DiscoveryMode = true
        };

        // Assert
        context.DocumentId.Should().Be(docId);
        context.ChunkId.Should().Be(chunkId);
        context.DocumentTitle.Should().Be("API Documentation");
        context.Heading.Should().Be("Authentication");
        context.HeadingLevel.Should().Be(2);
        context.MinConfidence.Should().Be(0.8f);
        context.TargetEntityTypes.Should().HaveCount(2);
        context.DiscoveryMode.Should().BeTrue();
    }

    [Fact]
    public void ExtractionContext_Equality_SameValues()
    {
        // Arrange
        var a = new ExtractionContext { MinConfidence = 0.7f, DiscoveryMode = true };
        var b = new ExtractionContext { MinConfidence = 0.7f, DiscoveryMode = true };

        // Assert
        a.Should().Be(b);
    }

    #endregion

    #region ExtractionResult Tests

    [Fact]
    public void ExtractionResult_AverageConfidence_ComputedFromMentions()
    {
        // Arrange & Act
        var result = new ExtractionResult
        {
            Mentions = new[]
            {
                new EntityMention { EntityType = "Endpoint", Value = "A", Confidence = 1.0f },
                new EntityMention { EntityType = "Endpoint", Value = "B", Confidence = 0.5f }
            }
        };

        // Assert
        result.AverageConfidence.Should().Be(0.75f);
    }

    [Fact]
    public void ExtractionResult_AverageConfidence_ZeroWhenEmpty()
    {
        // Arrange & Act
        var result = new ExtractionResult
        {
            Mentions = Array.Empty<EntityMention>()
        };

        // Assert
        result.AverageConfidence.Should().Be(0f);
    }

    [Fact]
    public void ExtractionResult_DefaultValues_Correct()
    {
        // Arrange & Act
        var result = new ExtractionResult
        {
            Mentions = Array.Empty<EntityMention>()
        };

        // Assert
        result.AggregatedEntities.Should().BeNull();
        result.Duration.Should().Be(TimeSpan.Zero);
        result.ChunksProcessed.Should().Be(0);
        result.MentionCountByType.Should().BeEmpty();
    }

    [Fact]
    public void ExtractionResult_WithAllProperties_Populated()
    {
        // Arrange & Act
        var mentions = new[]
        {
            new EntityMention { EntityType = "Endpoint", Value = "GET /users", Confidence = 1.0f }
        };

        var aggregated = new[]
        {
            new AggregatedEntity
            {
                EntityType = "Endpoint",
                CanonicalValue = "/users",
                Mentions = mentions,
                MaxConfidence = 1.0f
            }
        };

        var result = new ExtractionResult
        {
            Mentions = mentions,
            AggregatedEntities = aggregated,
            Duration = TimeSpan.FromMilliseconds(42),
            ChunksProcessed = 3,
            MentionCountByType = new() { ["Endpoint"] = 1 }
        };

        // Assert
        result.Mentions.Should().HaveCount(1);
        result.AggregatedEntities.Should().HaveCount(1);
        result.Duration.TotalMilliseconds.Should().Be(42);
        result.ChunksProcessed.Should().Be(3);
        result.MentionCountByType.Should().ContainKey("Endpoint");
    }

    #endregion

    #region AggregatedEntity Tests

    [Fact]
    public void AggregatedEntity_RequiredProperties_Populated()
    {
        // Arrange
        var mentions = new[]
        {
            new EntityMention { EntityType = "Endpoint", Value = "GET /users" }
        };

        // Act
        var entity = new AggregatedEntity
        {
            EntityType = "Endpoint",
            CanonicalValue = "/users",
            Mentions = mentions
        };

        // Assert
        entity.EntityType.Should().Be("Endpoint");
        entity.CanonicalValue.Should().Be("/users");
        entity.Mentions.Should().HaveCount(1);
    }

    [Fact]
    public void AggregatedEntity_DefaultValues_Correct()
    {
        // Arrange & Act
        var entity = new AggregatedEntity
        {
            EntityType = "Concept",
            CanonicalValue = "rate limiting",
            Mentions = Array.Empty<EntityMention>()
        };

        // Assert
        entity.MaxConfidence.Should().Be(0f);
        entity.MergedProperties.Should().BeEmpty();
        entity.SourceDocuments.Should().BeEmpty();
    }

    [Fact]
    public void AggregatedEntity_WithAllProperties_Populated()
    {
        // Arrange
        var chunkId = Guid.NewGuid();
        var mentions = new[]
        {
            new EntityMention
            {
                EntityType = "Endpoint",
                Value = "GET /users",
                ChunkId = chunkId,
                Confidence = 1.0f
            }
        };

        // Act
        var entity = new AggregatedEntity
        {
            EntityType = "Endpoint",
            CanonicalValue = "/users",
            Mentions = mentions,
            MaxConfidence = 1.0f,
            MergedProperties = new() { ["method"] = "GET", ["path"] = "/users" },
            SourceDocuments = new[] { chunkId }
        };

        // Assert
        entity.MaxConfidence.Should().Be(1.0f);
        entity.MergedProperties.Should().HaveCount(2);
        entity.SourceDocuments.Should().ContainSingle();
    }

    [Fact]
    public void AggregatedEntity_Equality_SameValuesAndSamePropertyReference()
    {
        // Arrange — records with Dictionary properties require the same
        // Dictionary reference for structural equality.
        var mentions = Array.Empty<EntityMention>();
        var props = new Dictionary<string, object>();
        var a = new AggregatedEntity
        {
            EntityType = "X", CanonicalValue = "y", Mentions = mentions,
            MergedProperties = props
        };
        var b = new AggregatedEntity
        {
            EntityType = "X", CanonicalValue = "y", Mentions = mentions,
            MergedProperties = props
        };

        // Assert
        a.Should().Be(b);
    }

    #endregion
}
