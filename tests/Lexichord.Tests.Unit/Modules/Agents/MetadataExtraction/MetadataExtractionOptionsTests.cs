// -----------------------------------------------------------------------
// <copyright file="MetadataExtractionOptionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.MetadataExtraction;

/// <summary>
/// Unit tests for <see cref="MetadataExtractionOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6b")]
public class MetadataExtractionOptionsTests
{
    // ── Default Values ──────────────────────────────────────────────────

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new MetadataExtractionOptions();

        // Assert
        options.MaxKeyTerms.Should().Be(10);
        options.MaxConcepts.Should().Be(5);
        options.MaxTags.Should().Be(5);
        options.ExistingTags.Should().BeNull();
        options.ExtractNamedEntities.Should().BeTrue();
        options.InferAudience.Should().BeTrue();
        options.CalculateComplexity.Should().BeTrue();
        options.DetectDocumentType.Should().BeTrue();
        options.IncludeDefinitions.Should().BeTrue();
        options.WordsPerMinute.Should().Be(200);
        options.DomainContext.Should().BeNull();
        options.MinimumTermImportance.Should().Be(0.3);
        options.MaxResponseTokens.Should().Be(2048);
    }

    // ── Static Presets ──────────────────────────────────────────────────

    [Fact]
    public void Default_ReturnsDefaultOptions()
    {
        // Act
        var options = MetadataExtractionOptions.Default;

        // Assert
        options.MaxKeyTerms.Should().Be(10);
        options.MaxConcepts.Should().Be(5);
        options.MaxTags.Should().Be(5);
    }

    [Fact]
    public void Quick_ReturnsQuickOptions()
    {
        // Act
        var options = MetadataExtractionOptions.Quick;

        // Assert
        options.MaxKeyTerms.Should().Be(5);
        options.MaxConcepts.Should().Be(3);
        options.MaxTags.Should().Be(3);
        options.ExtractNamedEntities.Should().BeFalse();
        options.InferAudience.Should().BeFalse();
        options.IncludeDefinitions.Should().BeFalse();
        options.MaxResponseTokens.Should().Be(1024);
    }

    [Fact]
    public void Comprehensive_ReturnsComprehensiveOptions()
    {
        // Act
        var options = MetadataExtractionOptions.Comprehensive;

        // Assert
        options.MaxKeyTerms.Should().Be(20);
        options.MaxConcepts.Should().Be(10);
        options.MaxTags.Should().Be(10);
        options.ExtractNamedEntities.Should().BeTrue();
        options.InferAudience.Should().BeTrue();
        options.CalculateComplexity.Should().BeTrue();
        options.DetectDocumentType.Should().BeTrue();
        options.IncludeDefinitions.Should().BeTrue();
        options.MinimumTermImportance.Should().Be(0.2);
        options.MaxResponseTokens.Should().Be(4096);
    }

    // ── Validate: MaxKeyTerms ──────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    [InlineData(100)]
    public void Validate_MaxKeyTermsOutOfRange_ThrowsArgumentException(int maxKeyTerms)
    {
        // Arrange
        var options = new MetadataExtractionOptions { MaxKeyTerms = maxKeyTerms };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxKeyTerms")
            .WithMessage("*between 1 and 50*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public void Validate_MaxKeyTermsInRange_DoesNotThrow(int maxKeyTerms)
    {
        // Arrange
        var options = new MetadataExtractionOptions { MaxKeyTerms = maxKeyTerms };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: MaxConcepts ──────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(100)]
    public void Validate_MaxConceptsOutOfRange_ThrowsArgumentException(int maxConcepts)
    {
        // Arrange
        var options = new MetadataExtractionOptions { MaxConcepts = maxConcepts };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxConcepts")
            .WithMessage("*between 1 and 20*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void Validate_MaxConceptsInRange_DoesNotThrow(int maxConcepts)
    {
        // Arrange
        var options = new MetadataExtractionOptions { MaxConcepts = maxConcepts };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: MaxTags ──────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(100)]
    public void Validate_MaxTagsOutOfRange_ThrowsArgumentException(int maxTags)
    {
        // Arrange
        var options = new MetadataExtractionOptions { MaxTags = maxTags };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxTags")
            .WithMessage("*between 1 and 20*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void Validate_MaxTagsInRange_DoesNotThrow(int maxTags)
    {
        // Arrange
        var options = new MetadataExtractionOptions { MaxTags = maxTags };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: WordsPerMinute ────────────────────────────────────────

    [Theory]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(401)]
    [InlineData(1000)]
    public void Validate_WordsPerMinuteOutOfRange_ThrowsArgumentException(int wordsPerMinute)
    {
        // Arrange
        var options = new MetadataExtractionOptions { WordsPerMinute = wordsPerMinute };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("WordsPerMinute")
            .WithMessage("*between 100 and 400*");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(400)]
    public void Validate_WordsPerMinuteInRange_DoesNotThrow(int wordsPerMinute)
    {
        // Arrange
        var options = new MetadataExtractionOptions { WordsPerMinute = wordsPerMinute };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: MinimumTermImportance ─────────────────────────────────

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.0)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Validate_MinimumTermImportanceOutOfRange_ThrowsArgumentException(double minImportance)
    {
        // Arrange
        var options = new MetadataExtractionOptions { MinimumTermImportance = minImportance };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MinimumTermImportance")
            .WithMessage("*between 0.0 and 1.0*");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Validate_MinimumTermImportanceInRange_DoesNotThrow(double minImportance)
    {
        // Arrange
        var options = new MetadataExtractionOptions { MinimumTermImportance = minImportance };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: Combined Valid Options ────────────────────────────────

    [Fact]
    public void Validate_ValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new MetadataExtractionOptions
        {
            MaxKeyTerms = 15,
            MaxConcepts = 8,
            MaxTags = 10,
            ExistingTags = new[] { "api", "design", "architecture" },
            ExtractNamedEntities = true,
            InferAudience = true,
            CalculateComplexity = true,
            DetectDocumentType = true,
            IncludeDefinitions = true,
            WordsPerMinute = 180,
            DomainContext = "software engineering",
            MinimumTermImportance = 0.4,
            MaxResponseTokens = 3000
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
