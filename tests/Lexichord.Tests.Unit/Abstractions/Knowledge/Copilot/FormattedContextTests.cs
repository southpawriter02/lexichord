// =============================================================================
// File: FormattedContextTests.cs
// Tests: Lexichord.Abstractions.Contracts.Knowledge.Copilot.FormattedContext
// SubPart: v0.7.2g
// =============================================================================
// Tests for the FormattedContext record:
//   - Empty static property
//   - Default values
//   - With-expression immutability
//   - Required Content property
//   - Property assertions
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2g")]
public class FormattedContextTests
{
    [Fact]
    public void Empty_ReturnsEmptyContent()
    {
        // Act
        var empty = FormattedContext.Empty;

        // Assert
        empty.Content.Should().BeEmpty();
        empty.TokenCount.Should().Be(0);
        empty.Format.Should().Be(ContextFormat.Markdown);
        empty.EntityCount.Should().Be(0);
        empty.RelationshipCount.Should().Be(0);
        empty.AxiomCount.Should().Be(0);
        empty.WasTruncated.Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_AreZeroAndFalse()
    {
        // Act
        var context = new FormattedContext { Content = "test" };

        // Assert
        context.TokenCount.Should().Be(0);
        context.EntityCount.Should().Be(0);
        context.RelationshipCount.Should().Be(0);
        context.AxiomCount.Should().Be(0);
        context.WasTruncated.Should().BeFalse();
        context.Format.Should().Be(ContextFormat.Markdown); // enum default
    }

    [Fact]
    public void WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new FormattedContext
        {
            Content = "original content",
            TokenCount = 100,
            Format = ContextFormat.Yaml,
            EntityCount = 5,
            WasTruncated = false
        };

        // Act
        var modified = original with { WasTruncated = true, TokenCount = 50 };

        // Assert
        modified.Content.Should().Be("original content");
        modified.TokenCount.Should().Be(50);
        modified.WasTruncated.Should().BeTrue();
        modified.EntityCount.Should().Be(5);
        // Original should be unchanged
        original.TokenCount.Should().Be(100);
        original.WasTruncated.Should().BeFalse();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Act
        var context = new FormattedContext
        {
            Content = "# Knowledge Context\nentities:\n  - type: Endpoint",
            TokenCount = 42,
            Format = ContextFormat.Yaml,
            EntityCount = 3,
            RelationshipCount = 2,
            AxiomCount = 1,
            WasTruncated = true
        };

        // Assert
        context.Content.Should().Contain("Knowledge Context");
        context.TokenCount.Should().Be(42);
        context.Format.Should().Be(ContextFormat.Yaml);
        context.EntityCount.Should().Be(3);
        context.RelationshipCount.Should().Be(2);
        context.AxiomCount.Should().Be(1);
        context.WasTruncated.Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var context1 = new FormattedContext
        {
            Content = "test",
            TokenCount = 1,
            Format = ContextFormat.Plain,
            EntityCount = 1
        };
        var context2 = new FormattedContext
        {
            Content = "test",
            TokenCount = 1,
            Format = ContextFormat.Plain,
            EntityCount = 1
        };

        // Assert
        context1.Should().Be(context2);
    }
}
