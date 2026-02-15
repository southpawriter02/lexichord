// -----------------------------------------------------------------------
// <copyright file="RewriteRequestedEventHandlerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Editor;
using Lexichord.Modules.Agents.Editor.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor;

/// <summary>
/// Unit tests for <see cref="RewriteRequestedEventHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3b")]
public class RewriteRequestedEventHandlerTests
{
    private readonly Mock<IRewriteCommandHandler> _commandHandlerMock;
    private readonly Mock<ILogger<RewriteRequestedEventHandler>> _loggerMock;
    private readonly RewriteRequestedEventHandler _sut;

    public RewriteRequestedEventHandlerTests()
    {
        _commandHandlerMock = new Mock<IRewriteCommandHandler>();
        _loggerMock = new Mock<ILogger<RewriteRequestedEventHandler>>();

        // Default setup: command handler returns success
        _commandHandlerMock
            .Setup(h => h.ExecuteAsync(
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

        _sut = new RewriteRequestedEventHandler(
            _commandHandlerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidEvent_CallsCommandHandler()
    {
        // Arrange
        var notification = RewriteRequestedEvent.Create(
            RewriteIntent.Formal,
            "Hello world",
            new TextSpan(0, 11),
            "/test/doc.md");

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _commandHandlerMock.Verify(
            h => h.ExecuteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MapsFieldsCorrectly()
    {
        // Arrange
        var notification = RewriteRequestedEvent.Create(
            RewriteIntent.Simplified,
            "Complex text to simplify",
            new TextSpan(100, 24),
            "/docs/chapter.md");

        RewriteRequest? capturedRequest = null;

        _commandHandlerMock
            .Setup(h => h.ExecuteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<RewriteRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new RewriteResult
            {
                OriginalText = "Complex text to simplify",
                RewrittenText = "Simple text",
                Intent = RewriteIntent.Simplified,
                Success = true,
                Usage = UsageMetrics.Zero,
                Duration = TimeSpan.FromMilliseconds(50)
            });

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SelectedText.Should().Be("Complex text to simplify");
        capturedRequest.SelectionSpan.Start.Should().Be(100);
        capturedRequest.SelectionSpan.Length.Should().Be(24);
        capturedRequest.Intent.Should().Be(RewriteIntent.Simplified);
        capturedRequest.DocumentPath.Should().Be("/docs/chapter.md");
        capturedRequest.CustomInstruction.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CustomIntent_IncludesCustomInstruction()
    {
        // Arrange
        var notification = RewriteRequestedEvent.Create(
            RewriteIntent.Custom,
            "Some text",
            new TextSpan(0, 9),
            "/test/doc.md",
            customInstruction: "Make it rhyme");

        RewriteRequest? capturedRequest = null;

        _commandHandlerMock
            .Setup(h => h.ExecuteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<RewriteRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new RewriteResult
            {
                OriginalText = "Some text",
                RewrittenText = "Some text with a rhyme",
                Intent = RewriteIntent.Custom,
                Success = true,
                Usage = UsageMetrics.Zero,
                Duration = TimeSpan.FromMilliseconds(50)
            });

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Intent.Should().Be(RewriteIntent.Custom);
        capturedRequest.CustomInstruction.Should().Be("Make it rhyme");
    }

    [Fact]
    public async Task Handle_HandlerThrows_DoesNotRethrow()
    {
        // Arrange
        _commandHandlerMock
            .Setup(h => h.ExecuteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Pipeline failure"));

        var notification = RewriteRequestedEvent.Create(
            RewriteIntent.Formal,
            "Test text",
            new TextSpan(0, 9),
            "/test/doc.md");

        // Act — Should NOT throw (MediatR handler swallows exceptions)
        var act = () => _sut.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_Cancellation_DoesNotRethrow()
    {
        // Arrange
        _commandHandlerMock
            .Setup(h => h.ExecuteAsync(
                It.IsAny<RewriteRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var notification = RewriteRequestedEvent.Create(
            RewriteIntent.Formal,
            "Test text",
            new TextSpan(0, 9),
            "/test/doc.md");

        // Act
        var act = () => _sut.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
