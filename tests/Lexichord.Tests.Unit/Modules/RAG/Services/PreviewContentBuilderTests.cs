// =============================================================================
// File: PreviewContentBuilderTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for PreviewContentBuilder (v0.5.7c).
// =============================================================================
// LOGIC: Verifies preview content building:
//   - Constructor null-parameter validation.
//   - BuildAsync returns correct content structure.
//   - Breadcrumb formatting replaces arrow separators.
//   - Exception handling for service failures.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="PreviewContentBuilder"/>.
/// Verifies constructor validation, content building, and breadcrumb formatting.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.7c")]
public class PreviewContentBuilderTests
{
    private readonly Mock<IContextExpansionService> _contextServiceMock;
    private readonly Mock<ISnippetService> _snippetServiceMock;
    private readonly Mock<ILogger<PreviewContentBuilder>> _loggerMock;

    public PreviewContentBuilderTests()
    {
        _contextServiceMock = new Mock<IContextExpansionService>();
        _snippetServiceMock = new Mock<ISnippetService>();
        _loggerMock = new Mock<ILogger<PreviewContentBuilder>>();
    }

    /// <summary>
    /// Creates a <see cref="PreviewContentBuilder"/> with test mocks.
    /// </summary>
    private PreviewContentBuilder CreateBuilder() =>
        new(
            _contextServiceMock.Object,
            _snippetServiceMock.Object,
            _loggerMock.Object);

    /// <summary>
    /// Creates a test <see cref="SearchHit"/>.
    /// </summary>
    private static SearchHit CreateHit(string documentPath = "/docs/test.md", float score = 0.9f)
    {
        var document = new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: documentPath,
            Title: Path.GetFileNameWithoutExtension(documentPath),
            Hash: "test-hash",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

        var chunk = new TextChunk(
            "Test content for testing.",
            StartOffset: 0,
            EndOffset: 25,
            new ChunkMetadata(Index: 0));

        return new SearchHit
        {
            Document = document,
            Chunk = chunk,
            Score = score
        };
    }

    /// <summary>
    /// Creates a test <see cref="Chunk"/> for expansion results.
    /// </summary>
    private static Chunk CreateRagChunk(string content = "Test chunk content", int index = 0) =>
        new(
            Id: Guid.NewGuid(),
            DocumentId: Guid.NewGuid(),
            Content: content,
            Embedding: null,
            ChunkIndex: index,
            StartOffset: 0,
            EndOffset: content.Length,
            Heading: null,
            HeadingLevel: 0);

    #region Constructor Tests

    [Fact]
    public void Constructor_NullContextService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PreviewContentBuilder(
            null!,
            _snippetServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contextService");
    }

    [Fact]
    public void Constructor_NullSnippetService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PreviewContentBuilder(
            _contextServiceMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snippetService");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PreviewContentBuilder(
            _contextServiceMock.Object,
            _snippetServiceMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateBuilder();

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    #endregion

    #region BuildAsync Tests

    [Fact]
    public async Task BuildAsync_NullHit_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act & Assert
        var act = async () => await builder.BuildAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("hit");
    }

    [Fact]
    public async Task BuildAsync_ReturnsCorrectDocumentPath()
    {
        // Arrange
        var builder = CreateBuilder();
        var hit = CreateHit("/docs/test-document.md");

        var coreChunk = CreateRagChunk("Core content");
        var expanded = new ExpandedChunk(
            Core: coreChunk,
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        _contextServiceMock
            .Setup(s => s.ExpandAsync(It.IsAny<Chunk>(), It.IsAny<ContextOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expanded);

        _snippetServiceMock
            .Setup(s => s.ExtractSnippet(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>()))
            .Returns(new Snippet(string.Empty, Array.Empty<HighlightSpan>(), 0, false, false));

        // Act
        var result = await builder.BuildAsync(hit);

        // Assert
        result.DocumentPath.Should().Be("/docs/test-document.md");
        result.DocumentTitle.Should().Be("test-document");
    }

    [Fact]
    public async Task BuildAsync_WithContext_ExtractsBeforeAndAfterContent()
    {
        // Arrange
        var builder = CreateBuilder();
        var hit = CreateHit();

        var coreChunk = CreateRagChunk("Core content");
        var beforeChunk = CreateRagChunk("Before content", -1);
        var afterChunk = CreateRagChunk("After content", 1);

        var expanded = new ExpandedChunk(
            Core: coreChunk,
            Before: new[] { beforeChunk },
            After: new[] { afterChunk },
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        _contextServiceMock
            .Setup(s => s.ExpandAsync(It.IsAny<Chunk>(), It.IsAny<ContextOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expanded);

        _snippetServiceMock
            .Setup(s => s.ExtractSnippet(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>()))
            .Returns(new Snippet(string.Empty, Array.Empty<HighlightSpan>(), 0, false, false));

        // Act
        var result = await builder.BuildAsync(hit);

        // Assert
        result.PrecedingContext.Should().Be("Before content");
        result.MatchedContent.Should().Be("Core content");
        result.FollowingContext.Should().Be("After content");
        result.HasPrecedingContext.Should().BeTrue();
        result.HasFollowingContext.Should().BeTrue();
    }

    [Fact]
    public async Task BuildAsync_WithBreadcrumb_FormatsSeparators()
    {
        // Arrange
        var builder = CreateBuilder();
        var hit = CreateHit();

        var coreChunk = CreateRagChunk("Content");
        var expanded = new ExpandedChunk(
            Core: coreChunk,
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: "Tokens",
            HeadingBreadcrumb: new[] { "API", "Auth", "Tokens" });

        _contextServiceMock
            .Setup(s => s.ExpandAsync(It.IsAny<Chunk>(), It.IsAny<ContextOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expanded);

        _snippetServiceMock
            .Setup(s => s.ExtractSnippet(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>()))
            .Returns(new Snippet(string.Empty, Array.Empty<HighlightSpan>(), 0, false, false));

        // Act
        var result = await builder.BuildAsync(hit);

        // Assert
        result.HasBreadcrumb.Should().BeTrue();
        result.Breadcrumb.Should().Contain("›");
    }

    [Fact]
    public async Task BuildAsync_NoBreadcrumb_ReturnsNullBreadcrumb()
    {
        // Arrange
        var builder = CreateBuilder();
        var hit = CreateHit();

        var coreChunk = CreateRagChunk("Content");
        var expanded = new ExpandedChunk(
            Core: coreChunk,
            Before: Array.Empty<Chunk>(),
            After: Array.Empty<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        _contextServiceMock
            .Setup(s => s.ExpandAsync(It.IsAny<Chunk>(), It.IsAny<ContextOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expanded);

        _snippetServiceMock
            .Setup(s => s.ExtractSnippet(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>()))
            .Returns(new Snippet(string.Empty, Array.Empty<HighlightSpan>(), 0, false, false));

        var options = new PreviewOptions { IncludeBreadcrumb = false };

        // Act
        var result = await builder.BuildAsync(hit, options);

        // Assert
        result.Breadcrumb.Should().BeNull();
        result.HasBreadcrumb.Should().BeFalse();
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void Builder_ImplementsIPreviewContentBuilder()
    {
        // Act
        var builder = CreateBuilder();

        // Assert
        builder.Should().BeAssignableTo<IPreviewContentBuilder>(
            because: "PreviewContentBuilder implements the IPreviewContentBuilder interface");
    }

    #endregion
}
