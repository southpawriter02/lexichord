// -----------------------------------------------------------------------
// <copyright file="RewriteApplicatorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Undo;
using Lexichord.Modules.Agents.Editor;
using Lexichord.Modules.Agents.Editor.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor;

/// <summary>
/// Unit tests for <see cref="RewriteApplicator"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3d")]
public class RewriteApplicatorTests : IDisposable
{
    private readonly Mock<IEditorService> _editorServiceMock;
    private readonly Mock<IUndoRedoService> _undoServiceMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RewriteApplicator _sut;
    private readonly RewriteApplicator _sutWithoutUndo;

    public RewriteApplicatorTests()
    {
        _editorServiceMock = new Mock<IEditorService>();
        _undoServiceMock = new Mock<IUndoRedoService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerFactory = NullLoggerFactory.Instance;

        // LOGIC: Default setup — mediator Publish returns completed task.
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // LOGIC: SUT with undo service available
        _sut = new RewriteApplicator(
            _editorServiceMock.Object,
            _undoServiceMock.Object,
            _mediatorMock.Object,
            _loggerFactory);

        // LOGIC: SUT without undo service (nullable)
        _sutWithoutUndo = new RewriteApplicator(
            _editorServiceMock.Object,
            undoRedoService: null,
            _mediatorMock.Object,
            _loggerFactory);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _sutWithoutUndo.Dispose();
    }

    // ── Constructor Validation Tests ────────────────────────────────────

    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RewriteApplicator(
            null!, _undoServiceMock.Object, _mediatorMock.Object, _loggerFactory);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RewriteApplicator(
            _editorServiceMock.Object, _undoServiceMock.Object, null!, _loggerFactory);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RewriteApplicator(
            _editorServiceMock.Object, _undoServiceMock.Object, _mediatorMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("loggerFactory");
    }

    [Fact]
    public void Constructor_NullUndoService_DoesNotThrow()
    {
        // Act
        var act = () => new RewriteApplicator(
            _editorServiceMock.Object, null, _mediatorMock.Object, _loggerFactory);

        // Assert
        act.Should().NotThrow();
    }

    // ── IsPreviewActive Tests ───────────────────────────────────────────

    [Fact]
    public void IsPreviewActive_Initially_ReturnsFalse()
    {
        // Assert
        _sut.IsPreviewActive.Should().BeFalse();
    }

    // ── ApplyRewriteAsync — Success Tests ───────────────────────────────

    [Fact]
    public async Task ApplyRewriteAsync_SuccessResult_ReturnsTrue()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var applied = await _sut.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert
        applied.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyRewriteAsync_SuccessResult_PushesToUndoStack()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        await _sut.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert
        _undoServiceMock.Verify(
            u => u.Push(It.IsAny<IUndoableOperation>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyRewriteAsync_SuccessResult_PublishesRewriteAppliedEvent()
    {
        // Arrange
        var result = CreateSuccessResult();
        RewriteAppliedEvent? capturedEvent = null;

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<RewriteAppliedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => capturedEvent = e as RewriteAppliedEvent)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.DocumentPath.Should().Be("/test.md");
        capturedEvent.Intent.Should().Be(RewriteIntent.Formal);
        capturedEvent.OriginalText.Should().Be("hello");
        capturedEvent.RewrittenText.Should().Be("Hello, World!");
    }

    [Fact]
    public async Task ApplyRewriteAsync_SuccessResult_ExecutesTextReplacement()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        await _sut.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert — Verifies DeleteText + InsertText were called
        _editorServiceMock.Verify(e => e.DeleteText(0, 5), Times.Once);
        _editorServiceMock.Verify(e => e.InsertText(0, "Hello, World!"), Times.Once);
    }

    // ── ApplyRewriteAsync — Failed Result Tests ─────────────────────────

    [Fact]
    public async Task ApplyRewriteAsync_FailedResult_ReturnsFalse()
    {
        // Arrange
        var result = RewriteResult.Failed("hello", RewriteIntent.Formal, "Error", TimeSpan.Zero);

        // Act
        var applied = await _sut.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert
        applied.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyRewriteAsync_FailedResult_DoesNotPushToUndoStack()
    {
        // Arrange
        var result = RewriteResult.Failed("hello", RewriteIntent.Formal, "Error", TimeSpan.Zero);

        // Act
        await _sut.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert
        _undoServiceMock.Verify(
            u => u.Push(It.IsAny<IUndoableOperation>()),
            Times.Never);
    }

    // ── ApplyRewriteAsync — Exception Tests ─────────────────────────────

    [Fact]
    public async Task ApplyRewriteAsync_EditorThrows_ReturnsFalse()
    {
        // Arrange
        _editorServiceMock
            .Setup(e => e.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Editor error"));

        var result = CreateSuccessResult();

        // Act
        var applied = await _sut.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert
        applied.Should().BeFalse();
    }

    // ── ApplyRewriteAsync — No Undo Service Tests ───────────────────────

    [Fact]
    public async Task ApplyRewriteAsync_NoUndoService_StillAppliesText()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var applied = await _sutWithoutUndo.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert
        applied.Should().BeTrue();
        _editorServiceMock.Verify(e => e.DeleteText(0, 5), Times.Once);
        _editorServiceMock.Verify(e => e.InsertText(0, "Hello, World!"), Times.Once);
    }

    [Fact]
    public async Task ApplyRewriteAsync_NoUndoService_StillPublishesEvent()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        await _sutWithoutUndo.ApplyRewriteAsync(
            "/test.md", new TextSpan(0, 5), result);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<RewriteAppliedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── PreviewRewriteAsync Tests ───────────────────────────────────────

    [Fact]
    public async Task PreviewRewriteAsync_SetsIsPreviewActive()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");

        // Act
        await _sut.PreviewRewriteAsync(
            "/test.md", new TextSpan(0, 5), "HELLO");

        // Assert
        _sut.IsPreviewActive.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewRewriteAsync_ReplacesTextInDocument()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");

        // Act
        await _sut.PreviewRewriteAsync(
            "/test.md", new TextSpan(0, 5), "HELLO");

        // Assert
        _editorServiceMock.Verify(e => e.DeleteText(0, 5), Times.Once);
        _editorServiceMock.Verify(e => e.InsertText(0, "HELLO"), Times.Once);
    }

    [Fact]
    public async Task PreviewRewriteAsync_PublishesPreviewStartedEvent()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");

        // Act
        await _sut.PreviewRewriteAsync(
            "/test.md", new TextSpan(0, 5), "HELLO");

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<RewritePreviewStartedEvent>(e =>
                    e.DocumentPath == "/test.md" &&
                    e.PreviewText == "HELLO"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PreviewRewriteAsync_NoDocumentContent_DoesNotSetPreview()
    {
        // Arrange — No document setup (GetDocumentByPath returns null)

        // Act
        await _sut.PreviewRewriteAsync(
            "/missing.md", new TextSpan(0, 5), "preview");

        // Assert
        _sut.IsPreviewActive.Should().BeFalse();
    }

    // ── CommitPreviewAsync Tests ────────────────────────────────────────

    [Fact]
    public async Task CommitPreviewAsync_ActivePreview_ClearsPreviewState()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        // Act
        await _sut.CommitPreviewAsync();

        // Assert
        _sut.IsPreviewActive.Should().BeFalse();
    }

    [Fact]
    public async Task CommitPreviewAsync_ActivePreview_PushesToUndoStack()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        // Act
        await _sut.CommitPreviewAsync();

        // Assert
        _undoServiceMock.Verify(
            u => u.Push(It.IsAny<IUndoableOperation>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitPreviewAsync_ActivePreview_PublishesCommittedEvent()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        // Act
        await _sut.CommitPreviewAsync();

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<RewritePreviewCommittedEvent>(e => e.DocumentPath == "/test.md"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitPreviewAsync_NoPreview_IsNoOp()
    {
        // Act
        await _sut.CommitPreviewAsync();

        // Assert
        _undoServiceMock.Verify(
            u => u.Push(It.IsAny<IUndoableOperation>()),
            Times.Never);
    }

    // ── CancelPreviewAsync Tests ────────────────────────────────────────

    [Fact]
    public async Task CancelPreviewAsync_ActivePreview_RestoresOriginalText()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        // Reset mocks to verify only cancel calls
        _editorServiceMock.Invocations.Clear();

        // Act
        await _sut.CancelPreviewAsync();

        // Assert — Deletes preview text length (5 for "HELLO") and inserts original ("hello")
        _editorServiceMock.Verify(e => e.DeleteText(0, 5), Times.Once);
        _editorServiceMock.Verify(e => e.InsertText(0, "hello"), Times.Once);
    }

    [Fact]
    public async Task CancelPreviewAsync_ActivePreview_ClearsPreviewState()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        // Act
        await _sut.CancelPreviewAsync();

        // Assert
        _sut.IsPreviewActive.Should().BeFalse();
    }

    [Fact]
    public async Task CancelPreviewAsync_ActivePreview_PublishesCancelledEvent()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        // Act
        await _sut.CancelPreviewAsync();

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<RewritePreviewCancelledEvent>(e => e.DocumentPath == "/test.md"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelPreviewAsync_ActivePreview_DoesNotPushToUndoStack()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        // Act
        await _sut.CancelPreviewAsync();

        // Assert
        _undoServiceMock.Verify(
            u => u.Push(It.IsAny<IUndoableOperation>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelPreviewAsync_NoPreview_IsNoOp()
    {
        // Arrange — Clear any prior invocations
        _editorServiceMock.Invocations.Clear();

        // Act
        await _sut.CancelPreviewAsync();

        // Assert — No text operations should occur
        _editorServiceMock.Verify(
            e => e.DeleteText(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    // ── ApplyRewriteAsync Cancels Active Preview ────────────────────────

    [Fact]
    public async Task ApplyRewriteAsync_WithActivePreview_CancelsPreviewFirst()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        var result = CreateSuccessResult();

        // Act
        await _sut.ApplyRewriteAsync("/test.md", new TextSpan(0, 5), result);

        // Assert — Preview should be cancelled (IsPreviewActive = false)
        _sut.IsPreviewActive.Should().BeFalse();

        // Assert — CancelledEvent should be published (from preview cancel)
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<RewritePreviewCancelledEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Preview Replaces Existing Preview ───────────────────────────────

    [Fact]
    public async Task PreviewRewriteAsync_WithExistingPreview_CancelsOldPreview()
    {
        // Arrange
        SetupDocumentContent("/test.md", "hello world");
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "HELLO");

        // Reset to set up for the new preview's document content read
        SetupDocumentContent("/test.md", "HELLO world");

        // Act — Start a new preview (should cancel old first)
        await _sut.PreviewRewriteAsync("/test.md", new TextSpan(0, 5), "GREETINGS");

        // Assert — Still in preview mode with new preview
        _sut.IsPreviewActive.Should().BeTrue();

        // Assert — CancelledEvent published for old preview
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<RewritePreviewCancelledEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Dispose Tests ───────────────────────────────────────────────────

    [Fact]
    public void Dispose_MultipleCallsDoNotThrow()
    {
        // Arrange
        var applicator = new RewriteApplicator(
            _editorServiceMock.Object, null, _mediatorMock.Object, _loggerFactory);

        // Act
        var act = () =>
        {
            applicator.Dispose();
            applicator.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    private static RewriteResult CreateSuccessResult() => new()
    {
        OriginalText = "hello",
        RewrittenText = "Hello, World!",
        Intent = RewriteIntent.Formal,
        Success = true,
        Usage = UsageMetrics.Zero,
        Duration = TimeSpan.FromMilliseconds(100)
    };

    private void SetupDocumentContent(string path, string content)
    {
        var docMock = new Mock<IManuscriptViewModel>();
        docMock.Setup(d => d.Content).Returns(content);

        _editorServiceMock
            .Setup(e => e.GetDocumentByPath(path))
            .Returns(docMock.Object);
    }
}
