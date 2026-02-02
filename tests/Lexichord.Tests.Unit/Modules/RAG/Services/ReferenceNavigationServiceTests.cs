// =============================================================================
// File: ReferenceNavigationServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ReferenceNavigationService.
// =============================================================================
// LOGIC: Verifies the complete navigation flow from SearchHit to editor:
//   - Null/invalid input handling
//   - Document open/lookup via IEditorService
//   - Delegation to IEditorNavigationService
//   - ReferenceNavigatedEvent publication
//   - Error handling (file not found, editor failures)
// =============================================================================
// VERSION: v0.4.6c (Source Navigation)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="ReferenceNavigationService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6c")]
public class ReferenceNavigationServiceTests
{
    private readonly Mock<IEditorService> _editorServiceMock;
    private readonly Mock<IEditorNavigationService> _editorNavigationMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ReferenceNavigationService _sut;

    /// <summary>
    /// Initializes test fixtures with default mock behavior.
    /// </summary>
    public ReferenceNavigationServiceTests()
    {
        _editorServiceMock = new Mock<IEditorService>();
        _editorNavigationMock = new Mock<IEditorNavigationService>();
        _mediatorMock = new Mock<IMediator>();

        // Default: document not found by path (simulates closed document).
        _editorServiceMock
            .Setup(e => e.GetDocumentByPath(It.IsAny<string>()))
            .Returns((IManuscriptViewModel?)null);

        // Default: OpenDocumentAsync returns a mock manuscript.
        _editorServiceMock
            .Setup(e => e.OpenDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateMockDocument("test-doc-id"));

        // Default: navigation succeeds.
        _editorNavigationMock
            .Setup(n => n.NavigateToOffsetAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NavigationResult.Succeeded("test-doc-id"));

        _sut = new ReferenceNavigationService(
            _editorServiceMock.Object,
            _editorNavigationMock.Object,
            _mediatorMock.Object,
            NullLogger<ReferenceNavigationService>.Instance);
    }

    // =========================================================================
    // NavigateToHitAsync — Input Validation
    // =========================================================================

    /// <summary>
    /// Verifies that a null hit returns false without throwing.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_NullHit_ReturnsFalse()
    {
        // Act
        var result = await _sut.NavigateToHitAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a hit with no document path returns false.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_NoDocumentPath_ReturnsFalse()
    {
        // Arrange — hit with null Document
        var hit = new SearchHit
        {
            Score = 0.9f,
            Chunk = CreateChunk(0, 100),
            Document = null!
        };

        // Act
        var result = await _sut.NavigateToHitAsync(hit);

        // Assert
        result.Should().BeFalse();
    }

    // =========================================================================
    // NavigateToHitAsync — Document Opening
    // =========================================================================

    /// <summary>
    /// Verifies that a closed document is opened via IEditorService.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_OpensClosedDocument()
    {
        // Arrange — document not found by path (default), so it will be opened.
        var hit = CreateHit("/docs/test.md", 50, 100);

        // Act
        await _sut.NavigateToHitAsync(hit);

        // Assert
        _editorServiceMock.Verify(
            e => e.OpenDocumentAsync("/docs/test.md"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that an already-open document is not opened again.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_SkipsOpenForOpenDocument()
    {
        // Arrange — document already found by path.
        var mockDoc = CreateMockDocument("existing-doc-id");
        _editorServiceMock
            .Setup(e => e.GetDocumentByPath("/docs/test.md"))
            .Returns(mockDoc);

        var hit = CreateHit("/docs/test.md", 50, 100);

        // Act
        await _sut.NavigateToHitAsync(hit);

        // Assert
        _editorServiceMock.Verify(
            e => e.OpenDocumentAsync(It.IsAny<string>()),
            Times.Never);
    }

    // =========================================================================
    // NavigateToHitAsync — Navigation Delegation
    // =========================================================================

    /// <summary>
    /// Verifies that navigation delegates to IEditorNavigationService with correct offset.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_DelegatesToEditorNavigation()
    {
        // Arrange
        var hit = CreateHit("/docs/test.md", 50, 100);

        // Act
        await _sut.NavigateToHitAsync(hit);

        // Assert — offset=50, length=50 (100-50)
        _editorNavigationMock.Verify(
            n => n.NavigateToOffsetAsync(
                "test-doc-id", 50, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that navigation scrolls to the correct chunk offset.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_ScrollsToCorrectOffset()
    {
        // Arrange — chunk at offset 200-350
        var hit = CreateHit("/docs/chapter.md", 200, 350);

        // Act
        await _sut.NavigateToHitAsync(hit);

        // Assert — offset=200, length=150 (350-200)
        _editorNavigationMock.Verify(
            n => n.NavigateToOffsetAsync(
                "test-doc-id", 200, 150, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that navigation highlights the text span via the editor navigation service.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_HighlightsTextSpan()
    {
        // Arrange — chunk at offset 50-100 → length=50
        var hit = CreateHit("/docs/test.md", 50, 100);

        // Act
        var result = await _sut.NavigateToHitAsync(hit);

        // Assert — navigation service receives the correct length for highlighting
        result.Should().BeTrue();
        _editorNavigationMock.Verify(
            n => n.NavigateToOffsetAsync(
                It.IsAny<string>(), 50, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // NavigateToHitAsync — Telemetry
    // =========================================================================

    /// <summary>
    /// Verifies that a ReferenceNavigatedEvent is published on successful navigation.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_PublishesTelemetryEvent()
    {
        // Arrange
        var hit = CreateHit("/docs/test.md", 50, 100, score: 0.85f);

        // Act
        await _sut.NavigateToHitAsync(hit);

        // Assert
        _mediatorMock.Verify(m => m.Publish(
            It.Is<ReferenceNavigatedEvent>(e =>
                e.DocumentPath == "/docs/test.md" &&
                e.Offset == 50 &&
                e.Length == 50 &&
                e.Score == 0.85f),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that no telemetry event is published when navigation fails.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_NavigationFails_DoesNotPublishEvent()
    {
        // Arrange — editor navigation returns failure.
        _editorNavigationMock
            .Setup(n => n.NavigateToOffsetAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NavigationResult.Failed("Activation failed"));

        var hit = CreateHit("/docs/test.md", 50, 100);

        // Act
        var result = await _sut.NavigateToHitAsync(hit);

        // Assert
        result.Should().BeFalse();
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<ReferenceNavigatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // NavigateToHitAsync — Error Handling
    // =========================================================================

    /// <summary>
    /// Verifies that a failed document open returns false.
    /// </summary>
    [Fact]
    public async Task NavigateToHitAsync_OpenFails_ReturnsFalse()
    {
        // Arrange — OpenDocumentAsync returns null (open failed).
        _editorServiceMock
            .Setup(e => e.OpenDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync((IManuscriptViewModel?)null!);

        var hit = CreateHit("/docs/test.md", 50, 100);

        // Act
        var result = await _sut.NavigateToHitAsync(hit);

        // Assert
        result.Should().BeFalse();
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<ReferenceNavigatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // NavigateToOffsetAsync — Input Validation
    // =========================================================================

    /// <summary>
    /// Verifies that empty path returns false.
    /// </summary>
    [Fact]
    public async Task NavigateToOffsetAsync_EmptyPath_ReturnsFalse()
    {
        // Act
        var result = await _sut.NavigateToOffsetAsync(string.Empty, 50, 100);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that negative offset is clamped to zero.
    /// </summary>
    [Fact]
    public async Task NavigateToOffsetAsync_NegativeOffset_ClampsToZero()
    {
        // Act
        await _sut.NavigateToOffsetAsync("/docs/test.md", -10, 50);

        // Assert — offset should be clamped to 0
        _editorNavigationMock.Verify(
            n => n.NavigateToOffsetAsync(
                "test-doc-id", 0, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that zero length passes through correctly (cursor-only navigation).
    /// </summary>
    [Fact]
    public async Task NavigateToOffsetAsync_ZeroLength_PassesThroughCorrectly()
    {
        // Act
        await _sut.NavigateToOffsetAsync("/docs/test.md", 50, 0);

        // Assert — length=0 delegates to editor navigation service which handles cursor-only
        _editorNavigationMock.Verify(
            n => n.NavigateToOffsetAsync(
                "test-doc-id", 50, 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that FileNotFoundException is caught and returns false.
    /// </summary>
    [Fact]
    public async Task NavigateToOffsetAsync_FileNotFound_ReturnsFalse()
    {
        // Arrange — OpenDocumentAsync throws FileNotFoundException.
        _editorServiceMock
            .Setup(e => e.OpenDocumentAsync(It.IsAny<string>()))
            .ThrowsAsync(new FileNotFoundException("File not found", "/docs/missing.md"));

        // Act
        var result = await _sut.NavigateToOffsetAsync("/docs/missing.md", 50, 100);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that unexpected exceptions are caught and return false.
    /// </summary>
    [Fact]
    public async Task NavigateToOffsetAsync_UnexpectedException_ReturnsFalse()
    {
        // Arrange — OpenDocumentAsync throws unexpected exception.
        _editorServiceMock
            .Setup(e => e.OpenDocumentAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _sut.NavigateToOffsetAsync("/docs/test.md", 50, 100);

        // Assert
        result.Should().BeFalse();
    }

    // =========================================================================
    // Constructor Validation
    // =========================================================================

    /// <summary>
    /// Verifies that null editorService throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new ReferenceNavigationService(
                null!,
                _editorNavigationMock.Object,
                _mediatorMock.Object,
                NullLogger<ReferenceNavigationService>.Instance))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("editorService");
    }

    /// <summary>
    /// Verifies that null editorNavigationService throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullEditorNavigationService_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new ReferenceNavigationService(
                _editorServiceMock.Object,
                null!,
                _mediatorMock.Object,
                NullLogger<ReferenceNavigationService>.Instance))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("editorNavigationService");
    }

    /// <summary>
    /// Verifies that null mediator throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new ReferenceNavigationService(
                _editorServiceMock.Object,
                _editorNavigationMock.Object,
                null!,
                NullLogger<ReferenceNavigationService>.Instance))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("mediator");
    }

    /// <summary>
    /// Verifies that null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new ReferenceNavigationService(
                _editorServiceMock.Object,
                _editorNavigationMock.Object,
                _mediatorMock.Object,
                null!))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    // =========================================================================
    // Test Helpers
    // =========================================================================

    /// <summary>
    /// Creates a mock <see cref="IManuscriptViewModel"/> with the given document ID.
    /// </summary>
    /// <param name="documentId">The document ID to return.</param>
    /// <returns>A mock manuscript ViewModel.</returns>
    private static IManuscriptViewModel CreateMockDocument(string documentId)
    {
        var mock = new Mock<IManuscriptViewModel>();
        mock.Setup(d => d.DocumentId).Returns(documentId);
        mock.Setup(d => d.Title).Returns("Test Document");
        mock.Setup(d => d.FilePath).Returns("/docs/test.md");
        return mock.Object;
    }

    /// <summary>
    /// Creates a <see cref="SearchHit"/> for testing.
    /// </summary>
    /// <param name="path">Document file path.</param>
    /// <param name="startOffset">Chunk start offset.</param>
    /// <param name="endOffset">Chunk end offset.</param>
    /// <param name="score">Relevance score.</param>
    /// <returns>A configured SearchHit instance.</returns>
    private static SearchHit CreateHit(
        string path,
        int startOffset,
        int endOffset,
        float score = 0.9f)
    {
        return new SearchHit
        {
            Score = score,
            Chunk = CreateChunk(startOffset, endOffset),
            Document = new Document(
                Id: Guid.NewGuid(),
                ProjectId: Guid.NewGuid(),
                FilePath: path,
                Title: Path.GetFileName(path),
                Hash: "abc123",
                Status: DocumentStatus.Indexed,
                IndexedAt: DateTime.UtcNow,
                FailureReason: null)
        };
    }

    /// <summary>
    /// Creates a <see cref="TextChunk"/> for testing.
    /// </summary>
    /// <param name="startOffset">Start offset.</param>
    /// <param name="endOffset">End offset.</param>
    /// <returns>A configured TextChunk instance.</returns>
    private static TextChunk CreateChunk(int startOffset, int endOffset)
    {
        return new TextChunk(
            Content: "Sample chunk content for testing navigation.",
            StartOffset: startOffset,
            EndOffset: endOffset,
            Metadata: new ChunkMetadata(Index: 0));
    }
}
