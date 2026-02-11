// -----------------------------------------------------------------------
// <copyright file="ResilientChatServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Resilience;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Resilience;

/// <summary>
/// Unit tests for <see cref="ResilientChatService"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate:
/// </para>
/// <list type="bullet">
///   <item><description>Successful requests pass through transparently</description></item>
///   <item><description>Rate limit exceptions are redirected to the queue</description></item>
///   <item><description>OperationCanceledException is not wrapped</description></item>
///   <item><description>Unexpected exceptions are wrapped in AgentException</description></item>
///   <item><description>Error events are published via MediatR</description></item>
///   <item><description>ProviderName delegates to inner service</description></item>
///   <item><description>Constructor validation</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.8d")]
public class ResilientChatServiceTests
{
    private readonly Mock<IChatCompletionService> _innerMock;
    private readonly Mock<IErrorRecoveryService> _recoveryMock;
    private readonly Mock<IRateLimitQueue> _rateLimitQueueMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ResilientChatService _service;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public ResilientChatServiceTests()
    {
        _innerMock = new Mock<IChatCompletionService>();
        _innerMock.Setup(s => s.ProviderName).Returns("test-provider");

        _recoveryMock = new Mock<IErrorRecoveryService>();
        _rateLimitQueueMock = new Mock<IRateLimitQueue>();
        _mediatorMock = new Mock<IMediator>();

        _service = new ResilientChatService(
            _innerMock.Object,
            _recoveryMock.Object,
            _rateLimitQueueMock.Object,
            NullLogger<ResilientChatService>.Instance,
            _mediatorMock.Object);
    }

    #region ProviderName Tests

    /// <summary>
    /// Verifies ProviderName delegates to inner service.
    /// </summary>
    [Fact]
    public void ProviderName_DelegatesFromInner()
    {
        // Assert
        _service.ProviderName.Should().Be("test-provider");
    }

    #endregion

    #region CompleteAsync Success Tests

    /// <summary>
    /// Verifies successful request passes through transparently.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_Success_ReturnsResponse()
    {
        // Arrange
        var expected = new ChatResponse("Hello!", 5, 10, TimeSpan.FromMilliseconds(100), "stop");
        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var request = CreateTestRequest();

        // Act
        var result = await _service.CompleteAsync(request);

        // Assert
        result.Should().BeSameAs(expected);
    }

    #endregion

    #region Rate Limit Handling Tests

    /// <summary>
    /// Verifies rate limit exceptions are redirected to the queue.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_RateLimit_QueuesRequest()
    {
        // Arrange
        var rlException = new Lexichord.Abstractions.Contracts.LLM.RateLimitException(
            "Rate limited",
            "test-provider",
            TimeSpan.FromSeconds(30));

        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(rlException);

        var queuedResponse = new ChatResponse("Queued result", 5, 10, TimeSpan.FromMilliseconds(100), "stop");
        _rateLimitQueueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queuedResponse);

        var request = CreateTestRequest();

        // Act
        var result = await _service.CompleteAsync(request);

        // Assert
        result.Should().BeSameAs(queuedResponse);
        _rateLimitQueueMock.Verify(
            q => q.EnqueueAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that rate limit publishes an error event.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_RateLimit_PublishesErrorEvent()
    {
        // Arrange
        var rlException = new Lexichord.Abstractions.Contracts.LLM.RateLimitException(
            "Rate limited",
            "test-provider",
            TimeSpan.FromSeconds(30));

        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(rlException);

        _rateLimitQueueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse("OK", 5, 10, TimeSpan.FromMilliseconds(100), "stop"));

        // Act
        await _service.CompleteAsync(CreateTestRequest());

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.Is<AgentErrorEvent>(e => e.WasRecovered == true), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region OperationCanceled Tests

    /// <summary>
    /// Verifies that OperationCanceledException is not wrapped.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_Canceled_ThrowsOperationCanceled()
    {
        // Arrange
        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.CompleteAsync(CreateTestRequest()));
    }

    #endregion

    #region Unexpected Exception Tests

    /// <summary>
    /// Verifies that unexpected exceptions are wrapped in AgentException.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_UnexpectedException_WrapsInAgentException()
    {
        // Arrange
        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected!"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AgentException>(
            () => _service.CompleteAsync(CreateTestRequest()));

        ex.UserMessage.Should().Contain("unexpected");
        ex.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that unexpected exceptions publish error events.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_UnexpectedException_PublishesErrorEvent()
    {
        // Arrange
        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected!"));

        // Act
        try
        {
            await _service.CompleteAsync(CreateTestRequest());
        }
        catch (AgentException)
        {
            // Expected
        }

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<AgentErrorEvent>(e => e.WasRecovered == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region AgentException Pass-through Tests

    /// <summary>
    /// Verifies that AgentException is rethrown without wrapping.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_AgentException_RethrowsWithoutWrapping()
    {
        // Arrange
        var agentEx = new AgentException("Already wrapped");
        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(agentEx);

        // Act & Assert
        var thrown = await Assert.ThrowsAsync<AgentException>(
            () => _service.CompleteAsync(CreateTestRequest()));

        thrown.Should().BeSameAs(agentEx);
    }

    #endregion

    #region Null Argument Tests

    /// <summary>
    /// Verifies null request throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CompleteAsync(null!));
    }

    #endregion

    #region Constructor Validation Tests

    /// <summary>
    /// Verifies null inner service throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var action = () => new ResilientChatService(
            null!, _recoveryMock.Object, _rateLimitQueueMock.Object,
            NullLogger<ResilientChatService>.Instance, _mediatorMock.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    /// <summary>
    /// Verifies null recovery service throws.
    /// </summary>
    [Fact]
    public void Constructor_NullRecovery_ThrowsArgumentNullException()
    {
        var action = () => new ResilientChatService(
            _innerMock.Object, null!, _rateLimitQueueMock.Object,
            NullLogger<ResilientChatService>.Instance, _mediatorMock.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("recovery");
    }

    /// <summary>
    /// Verifies null rate limit queue throws.
    /// </summary>
    [Fact]
    public void Constructor_NullRateLimitQueue_ThrowsArgumentNullException()
    {
        var action = () => new ResilientChatService(
            _innerMock.Object, _recoveryMock.Object, null!,
            NullLogger<ResilientChatService>.Instance, _mediatorMock.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("rateLimitQueue");
    }

    /// <summary>
    /// Verifies null logger throws.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var action = () => new ResilientChatService(
            _innerMock.Object, _recoveryMock.Object, _rateLimitQueueMock.Object,
            null!, _mediatorMock.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies null mediator throws.
    /// </summary>
    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        var action = () => new ResilientChatService(
            _innerMock.Object, _recoveryMock.Object, _rateLimitQueueMock.Object,
            NullLogger<ResilientChatService>.Instance, null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("mediator");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal test request.
    /// </summary>
    private static ChatRequest CreateTestRequest()
    {
        return ChatRequest.FromUserMessage("Test");
    }

    #endregion
}
