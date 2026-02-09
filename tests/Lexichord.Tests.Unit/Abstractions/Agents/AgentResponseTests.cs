// -----------------------------------------------------------------------
// <copyright file="AgentResponseTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents;

/// <summary>
/// Unit tests for <see cref="AgentResponse"/> record.
/// </summary>
/// <remarks>
/// Tests cover construction, factory methods, computed properties, and record equality.
/// Introduced in v0.6.6a.
/// </remarks>
public class AgentResponseTests
{
    // -----------------------------------------------------------------------
    // Constructor Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor creates an immutable response with all properties set.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Constructor_CreatesImmutableResponse()
    {
        // Arrange
        var citations = new[]
        {
            new Citation(
                ChunkId: Guid.NewGuid(),
                DocumentPath: "/path/to/doc.md",
                DocumentTitle: "Test Document",
                StartOffset: 0,
                EndOffset: 100,
                Heading: "Introduction",
                LineNumber: 1,
                IndexedAt: DateTime.UtcNow)
        };
        var usage = new UsageMetrics(100, 50, 0.003m);

        // Act
        var response = new AgentResponse("Content", citations, usage);

        // Assert
        response.Content.Should().Be("Content");
        response.Citations.Should().HaveCount(1);
        response.Usage.Should().Be(usage);
    }

    /// <summary>
    /// Verifies that the constructor accepts null citations.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Constructor_NullCitations_CreatesResponse()
    {
        // Arrange
        var usage = new UsageMetrics(100, 50, 0.003m);

        // Act
        var response = new AgentResponse("Content", null, usage);

        // Assert
        response.Content.Should().Be("Content");
        response.Citations.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // HasCitations / CitationCount Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that HasCitations returns true when citations are present.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasCitations_WithCitations_ReturnsTrue()
    {
        // Arrange
        var citation = new Citation(
            ChunkId: Guid.NewGuid(),
            DocumentPath: "/path/to/doc.md",
            DocumentTitle: "Test Document",
            StartOffset: 0,
            EndOffset: 100,
            Heading: null,
            LineNumber: 1,
            IndexedAt: DateTime.UtcNow);
        var response = new AgentResponse("Content", new[] { citation }, UsageMetrics.Zero);

        // Assert
        response.HasCitations.Should().BeTrue();
        response.CitationCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that HasCitations returns false when citations are null.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasCitations_NullCitations_ReturnsFalse()
    {
        // Arrange
        var response = new AgentResponse("Content", null, UsageMetrics.Zero);

        // Assert
        response.HasCitations.Should().BeFalse();
        response.CitationCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that HasCitations returns false when citations list is empty.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasCitations_EmptyCitations_ReturnsFalse()
    {
        // Arrange
        var response = new AgentResponse("Content", Array.Empty<Citation>(), UsageMetrics.Zero);

        // Assert
        response.HasCitations.Should().BeFalse();
        response.CitationCount.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // TotalTokens Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that TotalTokens delegates to UsageMetrics.TotalTokens.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void TotalTokens_DelegatesToUsageMetrics()
    {
        // Arrange
        var usage = new UsageMetrics(100, 50, 0.003m);
        var response = new AgentResponse("Content", null, usage);

        // Assert
        response.TotalTokens.Should().Be(150);
        response.TotalTokens.Should().Be(usage.TotalTokens);
    }

    // -----------------------------------------------------------------------
    // Factory Method Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that Empty returns a response with empty content and zero usage.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Empty_ReturnsEmptyResponse()
    {
        // Act
        var empty = AgentResponse.Empty;

        // Assert
        empty.Content.Should().BeEmpty();
        empty.Citations.Should().BeNull();
        empty.Usage.Should().Be(UsageMetrics.Zero);
        empty.TotalTokens.Should().Be(0);
    }

    /// <summary>
    /// Verifies that Empty is a shared singleton instance.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Empty_ReturnsSameInstance()
    {
        // Act
        var empty1 = AgentResponse.Empty;
        var empty2 = AgentResponse.Empty;

        // Assert — should be the same reference
        ReferenceEquals(empty1, empty2).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Error creates a response with the error message as content.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Error_CreatesErrorResponse()
    {
        // Act
        var error = AgentResponse.Error("Something went wrong");

        // Assert
        error.Content.Should().Be("Something went wrong");
        error.HasCitations.Should().BeFalse();
        error.Usage.Should().Be(UsageMetrics.Zero);
        error.TotalTokens.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // Record Equality Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that record equality works for AgentResponse.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var usage = new UsageMetrics(100, 50, 0.003m);
        var response1 = new AgentResponse("Content", null, usage);
        var response2 = new AgentResponse("Content", null, usage);

        // Assert
        response1.Should().Be(response2);
    }
}
