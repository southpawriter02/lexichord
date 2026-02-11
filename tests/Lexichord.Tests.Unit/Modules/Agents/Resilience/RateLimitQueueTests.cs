// -----------------------------------------------------------------------
// <copyright file="RateLimitQueueTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Resilience;

/// <summary>
/// Unit tests for <see cref="RateLimitQueue"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate:
/// </para>
/// <list type="bullet">
///   <item><description>Request queueing and processing</description></item>
///   <item><description>Queue depth tracking</description></item>
///   <item><description>Rate limit timing and wait estimation</description></item>
///   <item><description>StatusChanged event firing</description></item>
///   <item><description>Cancellation handling</description></item>
///   <item><description>Constructor validation</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.8d")]
public class RateLimitQueueTests
{
    private readonly Mock<IChatCompletionService> _innerMock;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public RateLimitQueueTests()
    {
        _innerMock = new Mock<IChatCompletionService>();
        _innerMock.Setup(s => s.ProviderName).Returns("test-provider");
    }

    #region Enqueue and Process Tests

    /// <summary>
    /// Verifies that an enqueued request is eventually processed.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_RequestIsProcessed_ReturnsResponse()
    {
        // Arrange
        var expectedResponse = new ChatResponse(
            "Hello!", 5, 10, TimeSpan.FromMilliseconds(100), "stop");

        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var queue = new RateLimitQueue(_innerMock.Object, NullLogger<RateLimitQueue>.Instance);

        var request = ChatRequest.FromUserMessage("Hi");

        // Act
        var response = await queue.EnqueueAsync(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Content.Should().Be("Hello!");
    }

    /// <summary>
    /// Verifies that cancellation propagates to enqueued requests.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        // Arrange
        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ChatRequest _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new ChatResponse("Should not reach", 5, 10, TimeSpan.FromMilliseconds(100), "stop");
            });

        var queue = new RateLimitQueue(_innerMock.Object, NullLogger<RateLimitQueue>.Instance);

        var request = ChatRequest.FromUserMessage("Hi");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => queue.EnqueueAsync(request, cts.Token));
    }

    #endregion

    #region EstimatedWaitTime Tests

    /// <summary>
    /// Verifies that wait time is zero when no rate limit is active.
    /// </summary>
    [Fact]
    public void EstimatedWaitTime_NoRateLimit_ReturnsZero()
    {
        // Arrange
        var queue = new RateLimitQueue(_innerMock.Object, NullLogger<RateLimitQueue>.Instance);

        // Act & Assert
        queue.EstimatedWaitTime.Should().Be(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that wait time is positive after setting a rate limit.
    /// </summary>
    [Fact]
    public void EstimatedWaitTime_AfterSetRateLimit_ReturnsPositive()
    {
        // Arrange
        var queue = new RateLimitQueue(_innerMock.Object, NullLogger<RateLimitQueue>.Instance);

        // Act
        queue.SetRateLimit(TimeSpan.FromSeconds(30));

        // Assert
        queue.EstimatedWaitTime.Should().BeGreaterThan(TimeSpan.Zero);
        queue.EstimatedWaitTime.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region QueueDepth Tests

    /// <summary>
    /// Verifies that initial queue depth is zero.
    /// </summary>
    [Fact]
    public void QueueDepth_Initially_IsZero()
    {
        // Arrange
        var queue = new RateLimitQueue(_innerMock.Object, NullLogger<RateLimitQueue>.Instance);

        // Assert
        queue.QueueDepth.Should().Be(0);
    }

    #endregion

    #region StatusChanged Event Tests

    /// <summary>
    /// Verifies that StatusChanged event fires when rate limit is set.
    /// </summary>
    [Fact]
    public void SetRateLimit_FiresStatusChangedEvent()
    {
        // Arrange
        var queue = new RateLimitQueue(_innerMock.Object, NullLogger<RateLimitQueue>.Instance);
        RateLimitStatusEventArgs? capturedArgs = null;
        queue.StatusChanged += (_, args) => capturedArgs = args;

        // Act
        queue.SetRateLimit(TimeSpan.FromSeconds(15));

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.IsRateLimited.Should().BeTrue();
        capturedArgs.EstimatedWait.Should().BeGreaterThan(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that StatusChanged event fires when request is enqueued.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_FiresStatusChangedEvent()
    {
        // Arrange
        _innerMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                "OK", 5, 10, TimeSpan.FromMilliseconds(100), "stop"));

        var queue = new RateLimitQueue(_innerMock.Object, NullLogger<RateLimitQueue>.Instance);
        var eventFired = false;
        queue.StatusChanged += (_, _) => eventFired = true;

        var request = ChatRequest.FromUserMessage("Hi");

        // Act
        await queue.EnqueueAsync(request, CancellationToken.None);

        // Assert
        eventFired.Should().BeTrue();
    }

    #endregion

    #region Constructor Validation Tests

    /// <summary>
    /// Verifies that null inner service throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new RateLimitQueue(null!, NullLogger<RateLimitQueue>.Instance);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    /// <summary>
    /// Verifies that null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new RateLimitQueue(_innerMock.Object, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion
}
