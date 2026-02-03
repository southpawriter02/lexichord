// =============================================================================
// File: ExpandedChunkTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ExpandedChunk record.
// =============================================================================
// VERSION: v0.5.3a (Context Expansion Service)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Models;

/// <summary>
/// Unit tests for the <see cref="ExpandedChunk"/> record.
/// </summary>
public sealed class ExpandedChunkTests
{
    private static Chunk CreateTestChunk(
        int chunkIndex = 5,
        Guid? id = null,
        Guid? documentId = null)
    {
        return new Chunk(
            Id: id ?? Guid.NewGuid(),
            DocumentId: documentId ?? Guid.NewGuid(),
            Content: "Test chunk content",
            Embedding: null,
            ChunkIndex: chunkIndex,
            StartOffset: 100,
            EndOffset: 200);
    }

    #region HasBefore Property

    [Fact]
    public void HasBefore_WithEmptyList_ReturnsFalse()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        // Assert
        Assert.False(expanded.HasBefore);
    }

    [Fact]
    public void HasBefore_WithChunks_ReturnsTrue()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(chunkIndex: 5),
            Before: new[] { CreateTestChunk(chunkIndex: 4) },
            After: Array.Empty<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        // Assert
        Assert.True(expanded.HasBefore);
    }

    #endregion

    #region HasAfter Property

    [Fact]
    public void HasAfter_WithEmptyList_ReturnsFalse()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        // Assert
        Assert.False(expanded.HasAfter);
    }

    [Fact]
    public void HasAfter_WithChunks_ReturnsTrue()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(chunkIndex: 5),
            Before: Array.Empty<Chunk>(),
            After: new[] { CreateTestChunk(chunkIndex: 6) },
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        // Assert
        Assert.True(expanded.HasAfter);
    }

    #endregion

    #region HasBreadcrumb Property

    [Fact]
    public void HasBreadcrumb_WithEmptyList_ReturnsFalse()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        // Assert
        Assert.False(expanded.HasBreadcrumb);
    }

    [Fact]
    public void HasBreadcrumb_WithHeadings_ReturnsTrue()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: "Section 1",
            HeadingBreadcrumb: new[] { "Chapter 1", "Section 1" });

        // Assert
        Assert.True(expanded.HasBreadcrumb);
    }

    #endregion

    #region TotalChunks Property

    [Fact]
    public void TotalChunks_WithNoContext_Returns1()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        // Assert
        Assert.Equal(1, expanded.TotalChunks);
    }

    [Fact]
    public void TotalChunks_WithContext_ReturnsTotalCount()
    {
        // Arrange - 2 before + 1 core + 3 after = 6
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(chunkIndex: 5),
            Before: new[] { CreateTestChunk(chunkIndex: 3), CreateTestChunk(chunkIndex: 4) },
            After: new[] { CreateTestChunk(chunkIndex: 6), CreateTestChunk(chunkIndex: 7), CreateTestChunk(chunkIndex: 8) },
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        // Assert
        Assert.Equal(6, expanded.TotalChunks);
    }

    #endregion

    #region FormatBreadcrumb Method

    [Fact]
    public void FormatBreadcrumb_WithDefaultSeparator_JoinsWithArrow()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: "Section 1",
            HeadingBreadcrumb: new[] { "Document", "Chapter 1", "Section 1" });

        // Act
        var formatted = expanded.FormatBreadcrumb();

        // Assert
        Assert.Equal("Document > Chapter 1 > Section 1", formatted);
    }

    [Fact]
    public void FormatBreadcrumb_WithCustomSeparator_UsesCustomSeparator()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: "Section 1",
            HeadingBreadcrumb: new[] { "Document", "Chapter 1", "Section 1" });

        // Act
        var formatted = expanded.FormatBreadcrumb(" / ");

        // Assert
        Assert.Equal("Document / Chapter 1 / Section 1", formatted);
    }

    [Fact]
    public void FormatBreadcrumb_WithEmptyBreadcrumb_ReturnsEmptyString()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        // Act
        var formatted = expanded.FormatBreadcrumb();

        // Assert
        Assert.Equal(string.Empty, formatted);
    }

    [Fact]
    public void FormatBreadcrumb_WithSingleHeading_ReturnsSingleItem()
    {
        // Arrange
        var expanded = new ExpandedChunk(
            Core: CreateTestChunk(),
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: "Introduction",
            HeadingBreadcrumb: new[] { "Introduction" });

        // Act
        var formatted = expanded.FormatBreadcrumb();

        // Assert
        Assert.Equal("Introduction", formatted);
    }

    #endregion
}
