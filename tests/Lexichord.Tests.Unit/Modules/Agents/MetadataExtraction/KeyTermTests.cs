// -----------------------------------------------------------------------
// <copyright file="KeyTermTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.MetadataExtraction;

/// <summary>
/// Unit tests for <see cref="KeyTerm"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6b")]
public class KeyTermTests
{
    // ── Constructor ──────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var relatedTerms = new[] { "related1", "related2" };

        // Act
        var keyTerm = new KeyTerm(
            Term: "dependency injection",
            Importance: 0.85,
            Frequency: 12,
            IsTechnical: true,
            Definition: "A design pattern for achieving IoC",
            Category: "programming",
            RelatedTerms: relatedTerms);

        // Assert
        keyTerm.Term.Should().Be("dependency injection");
        keyTerm.Importance.Should().Be(0.85);
        keyTerm.Frequency.Should().Be(12);
        keyTerm.IsTechnical.Should().BeTrue();
        keyTerm.Definition.Should().Be("A design pattern for achieving IoC");
        keyTerm.Category.Should().Be("programming");
        keyTerm.RelatedTerms.Should().BeEquivalentTo(relatedTerms);
    }

    [Fact]
    public void Constructor_WithNullOptionalParameters_SetsNullValues()
    {
        // Act
        var keyTerm = new KeyTerm(
            Term: "testing",
            Importance: 0.5,
            Frequency: 5,
            IsTechnical: false,
            Definition: null,
            Category: null,
            RelatedTerms: null);

        // Assert
        keyTerm.Term.Should().Be("testing");
        keyTerm.Importance.Should().Be(0.5);
        keyTerm.Frequency.Should().Be(5);
        keyTerm.IsTechnical.Should().BeFalse();
        keyTerm.Definition.Should().BeNull();
        keyTerm.Category.Should().BeNull();
        keyTerm.RelatedTerms.Should().BeNull();
    }

    // ── Factory Methods ──────────────────────────────────────────────────

    [Fact]
    public void Create_ReturnsMinimalKeyTerm()
    {
        // Act
        var keyTerm = KeyTerm.Create("api", 0.75);

        // Assert
        keyTerm.Term.Should().Be("api");
        keyTerm.Importance.Should().Be(0.75);
        keyTerm.Frequency.Should().Be(1);
        keyTerm.IsTechnical.Should().BeFalse();
        keyTerm.Definition.Should().BeNull();
        keyTerm.Category.Should().BeNull();
        keyTerm.RelatedTerms.Should().BeNull();
    }

    [Fact]
    public void CreateTechnical_ReturnsTechnicalKeyTerm()
    {
        // Act
        var keyTerm = KeyTerm.CreateTechnical("microservice", 0.9, 8, "architecture");

        // Assert
        keyTerm.Term.Should().Be("microservice");
        keyTerm.Importance.Should().Be(0.9);
        keyTerm.Frequency.Should().Be(8);
        keyTerm.IsTechnical.Should().BeTrue();
        keyTerm.Definition.Should().BeNull();
        keyTerm.Category.Should().Be("architecture");
        keyTerm.RelatedTerms.Should().BeNull();
    }

    [Fact]
    public void CreateTechnical_WithoutCategory_ReturnsTechnicalKeyTermWithNullCategory()
    {
        // Act
        var keyTerm = KeyTerm.CreateTechnical("async", 0.7, 15);

        // Assert
        keyTerm.Term.Should().Be("async");
        keyTerm.Importance.Should().Be(0.7);
        keyTerm.Frequency.Should().Be(15);
        keyTerm.IsTechnical.Should().BeTrue();
        keyTerm.Category.Should().BeNull();
    }

    // ── Importance Score Ranges ──────────────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Create_WithValidImportance_SetsImportance(double importance)
    {
        // Act
        var keyTerm = KeyTerm.Create("term", importance);

        // Assert
        keyTerm.Importance.Should().Be(importance);
    }

    // ── Record Equality ──────────────────────────────────────────────────

    [Fact]
    public void Equality_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var term1 = KeyTerm.Create("api", 0.8);
        var term2 = KeyTerm.Create("api", 0.8);

        // Act & Assert
        term1.Should().Be(term2);
        (term1 == term2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var term1 = KeyTerm.Create("api", 0.8);
        var term2 = KeyTerm.Create("api", 0.9);

        // Act & Assert
        term1.Should().NotBe(term2);
        (term1 != term2).Should().BeTrue();
    }

    // ── With Expression ──────────────────────────────────────────────────

    [Fact]
    public void WithExpression_ModifiesProperty()
    {
        // Arrange
        var original = KeyTerm.Create("api", 0.8);

        // Act
        var modified = original with { Importance = 0.95 };

        // Assert
        modified.Term.Should().Be("api");
        modified.Importance.Should().Be(0.95);
        original.Importance.Should().Be(0.8); // Original unchanged
    }
}
