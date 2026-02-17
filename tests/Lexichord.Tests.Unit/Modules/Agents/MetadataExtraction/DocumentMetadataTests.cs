// -----------------------------------------------------------------------
// <copyright file="DocumentMetadataTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.MetadataExtraction;

/// <summary>
/// Unit tests for <see cref="DocumentMetadata"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6b")]
public class DocumentMetadataTests
{
    // ── Failed Factory ──────────────────────────────────────────────────

    [Fact]
    public void Failed_CreatesFailedResult()
    {
        // Act
        var metadata = DocumentMetadata.Failed("Test error message", 500);

        // Assert
        metadata.Success.Should().BeFalse();
        metadata.ErrorMessage.Should().Be("Test error message");
        metadata.WordCount.Should().Be(500);
        metadata.OneLiner.Should().BeEmpty();
        metadata.KeyTerms.Should().BeEmpty();
        metadata.Concepts.Should().BeEmpty();
        metadata.SuggestedTags.Should().BeEmpty();
        metadata.SuggestedTitle.Should().BeNull();
        metadata.PrimaryCategory.Should().BeNull();
        metadata.TargetAudience.Should().BeNull();
        metadata.EstimatedReadingMinutes.Should().Be(0);
        metadata.ComplexityScore.Should().Be(0);
        metadata.DocumentType.Should().Be(DocumentType.Unknown);
        metadata.NamedEntities.Should().BeNull();
        metadata.CharacterCount.Should().Be(0);
        metadata.Usage.Should().Be(UsageMetrics.Zero);
        metadata.Model.Should().BeNull();
    }

    [Fact]
    public void Failed_WithDefaultWordCount_SetsWordCountToZero()
    {
        // Act
        var metadata = DocumentMetadata.Failed("Error");

        // Assert
        metadata.WordCount.Should().Be(0);
    }

    [Fact]
    public void Failed_WithNullErrorMessage_ThrowsArgumentNullException()
    {
        // Act
        var act = () => DocumentMetadata.Failed(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("errorMessage");
    }

    [Fact]
    public void Failed_SetsExtractedAtToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var metadata = DocumentMetadata.Failed("Error");

        // Assert
        var after = DateTimeOffset.UtcNow;
        metadata.ExtractedAt.Should().BeOnOrAfter(before);
        metadata.ExtractedAt.Should().BeOnOrBefore(after);
    }

    // ── CreateMinimal Factory ───────────────────────────────────────────

    [Fact]
    public void CreateMinimal_CreatesMinimalSuccessfulResult()
    {
        // Act
        var metadata = DocumentMetadata.CreateMinimal("Test one-liner", 200);

        // Assert
        metadata.Success.Should().BeTrue();
        metadata.ErrorMessage.Should().BeNull();
        metadata.OneLiner.Should().Be("Test one-liner");
        metadata.WordCount.Should().Be(200);
        metadata.CharacterCount.Should().Be(1000); // 200 * 5
        metadata.EstimatedReadingMinutes.Should().Be(1); // 200/200 = 1
        metadata.ComplexityScore.Should().Be(5); // Neutral
        metadata.DocumentType.Should().Be(DocumentType.Unknown);
        metadata.KeyTerms.Should().BeEmpty();
        metadata.Concepts.Should().BeEmpty();
        metadata.SuggestedTags.Should().BeEmpty();
        metadata.Usage.Should().Be(UsageMetrics.Zero);
    }

    [Fact]
    public void CreateMinimal_WithLargeWordCount_CalculatesCorrectReadingTime()
    {
        // Act
        var metadata = DocumentMetadata.CreateMinimal("Long document", 1000);

        // Assert
        metadata.EstimatedReadingMinutes.Should().Be(5); // 1000/200 = 5
    }

    // ── Required Properties ─────────────────────────────────────────────

    [Fact]
    public void SuccessfulMetadata_HasRequiredProperties()
    {
        // Arrange
        var keyTerms = new[] { KeyTerm.Create("api", 0.8) };
        var concepts = new[] { "web services", "REST" };
        var tags = new[] { "api-design", "rest" };

        // Act
        var metadata = new DocumentMetadata
        {
            OneLiner = "An API design guide",
            KeyTerms = keyTerms,
            Concepts = concepts,
            SuggestedTags = tags,
            Usage = UsageMetrics.Zero
        };

        // Assert
        metadata.OneLiner.Should().Be("An API design guide");
        metadata.KeyTerms.Should().HaveCount(1);
        metadata.Concepts.Should().HaveCount(2);
        metadata.SuggestedTags.Should().HaveCount(2);
        metadata.Success.Should().BeTrue();
        metadata.ErrorMessage.Should().BeNull();
    }

    // ── Default Values ──────────────────────────────────────────────────

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var metadata = new DocumentMetadata
        {
            OneLiner = "Test",
            KeyTerms = Array.Empty<KeyTerm>(),
            Concepts = Array.Empty<string>(),
            SuggestedTags = Array.Empty<string>(),
            Usage = UsageMetrics.Zero
        };

        // Assert
        metadata.Language.Should().Be("en");
        metadata.Success.Should().BeTrue();
        metadata.ErrorMessage.Should().BeNull();
        metadata.SuggestedTitle.Should().BeNull();
        metadata.PrimaryCategory.Should().BeNull();
        metadata.TargetAudience.Should().BeNull();
        metadata.NamedEntities.Should().BeNull();
        metadata.Model.Should().BeNull();
        metadata.DocumentType.Should().Be(DocumentType.Unknown);
        metadata.ComplexityScore.Should().Be(0);
        metadata.EstimatedReadingMinutes.Should().Be(0);
        metadata.WordCount.Should().Be(0);
        metadata.CharacterCount.Should().Be(0);
    }

    // ── Full Metadata Construction ──────────────────────────────────────

    [Fact]
    public void FullMetadata_SetsAllProperties()
    {
        // Arrange
        var keyTerms = new[]
        {
            KeyTerm.CreateTechnical("api", 0.95, 15, "programming"),
            KeyTerm.Create("design", 0.7)
        };
        var concepts = new[] { "software architecture", "REST APIs" };
        var tags = new[] { "api-design", "rest", "architecture" };
        var entities = new[] { "Microsoft", "Google Cloud" };
        var usage = UsageMetrics.Calculate(100, 50, 0.01m, 0.03m);

        // Act
        var metadata = new DocumentMetadata
        {
            SuggestedTitle = "API Design Guide",
            OneLiner = "A comprehensive guide to designing RESTful APIs",
            KeyTerms = keyTerms,
            Concepts = concepts,
            SuggestedTags = tags,
            PrimaryCategory = "Technical Documentation",
            TargetAudience = "software developers",
            EstimatedReadingMinutes = 15,
            ComplexityScore = 7,
            DocumentType = DocumentType.Tutorial,
            NamedEntities = entities,
            Language = "en",
            WordCount = 3000,
            CharacterCount = 18000,
            ExtractedAt = DateTimeOffset.UtcNow,
            Usage = usage,
            Model = "gpt-4o",
            Success = true,
            ErrorMessage = null
        };

        // Assert
        metadata.SuggestedTitle.Should().Be("API Design Guide");
        metadata.OneLiner.Should().Be("A comprehensive guide to designing RESTful APIs");
        metadata.KeyTerms.Should().HaveCount(2);
        metadata.Concepts.Should().HaveCount(2);
        metadata.SuggestedTags.Should().HaveCount(3);
        metadata.PrimaryCategory.Should().Be("Technical Documentation");
        metadata.TargetAudience.Should().Be("software developers");
        metadata.EstimatedReadingMinutes.Should().Be(15);
        metadata.ComplexityScore.Should().Be(7);
        metadata.DocumentType.Should().Be(DocumentType.Tutorial);
        metadata.NamedEntities.Should().HaveCount(2);
        metadata.Language.Should().Be("en");
        metadata.WordCount.Should().Be(3000);
        metadata.CharacterCount.Should().Be(18000);
        metadata.Usage.Should().Be(usage);
        metadata.Model.Should().Be("gpt-4o");
        metadata.Success.Should().BeTrue();
    }
}
