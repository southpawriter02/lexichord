// -----------------------------------------------------------------------
// <copyright file="RewriteCommandHandlerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Editor;
using Lexichord.Modules.Agents.Editor.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor;

/// <summary>
/// Unit tests for <see cref="RewriteCommandHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3b")]
public class RewriteCommandHandlerTests
{
    private readonly Mock<IEditorAgent> _editorAgentMock;
    private readonly Mock<IRewriteApplicator> _applicatorMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILicenseContext> _licenseMock;
    private readonly Mock<ILogger<RewriteCommandHandler>> _loggerMock;
    private readonly RewriteCommandHandler _sut;

    public RewriteCommandHandlerTests()
    {
        _editorAgentMock = new Mock<IEditorAgent>();
        _applicatorMock = new Mock<IRewriteApplicator>();
        _mediatorMock = new Mock<IMediator>();
        _licenseMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<RewriteCommandHandler>>();

        // Default setup: license is enabled
        _licenseMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.EditorAgent))
            .Returns(true);

        // Default setup: agent returns a successful result
        _editorAgentMock
            .Setup(a => a.RewriteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RewriteResult
            {
                OriginalText = "Original",
                RewrittenText = "Rewritten",
                Intent = RewriteIntent.Formal,
                Success = true,
                Usage = UsageMetrics.Zero,
                Duration = TimeSpan.FromMilliseconds(100)
            });

        // Default setup: applicator returns success
        _applicatorMock
            .Setup(a => a.ApplyRewriteAsync(
                It.IsAny<string>(),
                It.IsAny<TextSpan>(),
                It.IsAny<RewriteResult>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new RewriteCommandHandler(
            _editorAgentMock.Object,
            _applicatorMock.Object,
            _mediatorMock.Object,
            _licenseMock.Object,
            _loggerMock.Object);
    }

    // ── License Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WithLicense_ReturnsResult()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RewrittenText.Should().Be("Rewritten");
    }

    [Fact]
    public async Task ExecuteAsync_WithoutLicense_ReturnsFailure()
    {
        // Arrange
        _licenseMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.EditorAgent))
            .Returns(false);

        var request = CreateRequest();

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Writer Pro license required");
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutLicense_DoesNotInvokeAgent()
    {
        // Arrange
        _licenseMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.EditorAgent))
            .Returns(false);

        var request = CreateRequest();

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        _editorAgentMock.Verify(
            a => a.RewriteAsync(It.IsAny<RewriteRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Event Publishing Tests ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_PublishesStartAndCompletedEvents()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.IsAny<RewriteStartedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Publish(
                It.IsAny<RewriteCompletedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_StartEventHasCorrectFields()
    {
        // Arrange
        var request = CreateRequest();
        RewriteStartedEvent? capturedEvent = null;

        _mediatorMock
            .Setup(m => m.Publish(
                It.IsAny<RewriteStartedEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) =>
                capturedEvent = e as RewriteStartedEvent)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Intent.Should().Be(RewriteIntent.Formal);
        capturedEvent.CharacterCount.Should().Be("Test text".Length);
        capturedEvent.DocumentPath.Should().Be("/test/doc.md");
    }

    [Fact]
    public async Task ExecuteAsync_AgentFails_PublishesFailedCompletion()
    {
        // Arrange
        _editorAgentMock
            .Setup(a => a.RewriteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RewriteResult.Failed(
                "Test text",
                RewriteIntent.Formal,
                "Agent error",
                TimeSpan.FromSeconds(1)));

        RewriteCompletedEvent? capturedEvent = null;

        _mediatorMock
            .Setup(m => m.Publish(
                It.IsAny<RewriteCompletedEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) =>
                capturedEvent = e as RewriteCompletedEvent)
            .Returns(Task.CompletedTask);

        var request = CreateRequest();

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Success.Should().BeFalse();
        capturedEvent.ErrorMessage.Should().Be("Agent error");
    }

    // ── IsExecuting Tests ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SetsIsExecuting()
    {
        // Arrange
        var isExecutingDuringCall = false;

        _editorAgentMock
            .Setup(a => a.RewriteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                isExecutingDuringCall = _sut.IsExecuting;
                return new RewriteResult
                {
                    OriginalText = "Original",
                    RewrittenText = "Rewritten",
                    Intent = RewriteIntent.Formal,
                    Success = true,
                    Usage = UsageMetrics.Zero,
                    Duration = TimeSpan.FromMilliseconds(100)
                };
            });

        var request = CreateRequest();

        // Act
        _sut.IsExecuting.Should().BeFalse();
        await _sut.ExecuteAsync(request);

        // Assert
        isExecutingDuringCall.Should().BeTrue();
        _sut.IsExecuting.Should().BeFalse(); // Reset after completion
    }

    [Fact]
    public async Task ExecuteAsync_ResetsIsExecuting_OnFailure()
    {
        // Arrange
        _editorAgentMock
            .Setup(a => a.RewriteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var request = CreateRequest();

        // Act — Should not throw because the handler catches agent errors
        // but MediatR publish could also fail, so we expect the exception to propagate
        try
        {
            await _sut.ExecuteAsync(request);
        }
        catch
        {
            // Expected — the exception propagates from the handler
        }

        // Assert
        _sut.IsExecuting.Should().BeFalse();
    }

    // ── Applicator Tests ────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WithApplicator_AppliesResult()
    {
        // Arrange
        _applicatorMock
            .Setup(a => a.ApplyRewriteAsync(
                It.IsAny<string>(),
                It.IsAny<TextSpan>(),
                It.IsAny<RewriteResult>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = CreateRequest();

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _applicatorMock.Verify(
            a => a.ApplyRewriteAsync(
                "/test/doc.md",
                It.IsAny<TextSpan>(),
                It.IsAny<RewriteResult>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutApplicator_StillReturnsSuccess()
    {
        // Arrange — Create handler WITHOUT applicator (null)
        var sutWithoutApplicator = new RewriteCommandHandler(
            _editorAgentMock.Object,
            applicator: null,
            _mediatorMock.Object,
            _licenseMock.Object,
            _loggerMock.Object);

        var request = CreateRequest();

        // Act
        var result = await sutWithoutApplicator.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ApplicatorFails_ReturnsFailure()
    {
        // Arrange
        _applicatorMock
            .Setup(a => a.ApplyRewriteAsync(
                It.IsAny<string>(),
                It.IsAny<TextSpan>(),
                It.IsAny<RewriteResult>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = CreateRequest();

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to apply rewrite");
    }

    // ── Cancel Tests ────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenNotExecuting_DoesNotThrow()
    {
        // Act
        var act = () => _sut.Cancel();

        // Assert
        act.Should().NotThrow();
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    private static RewriteRequest CreateRequest() => new()
    {
        SelectedText = "Test text",
        SelectionSpan = new TextSpan(0, 9),
        Intent = RewriteIntent.Formal,
        DocumentPath = "/test/doc.md"
    };
}
