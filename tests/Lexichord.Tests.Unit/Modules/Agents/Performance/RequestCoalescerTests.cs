// -----------------------------------------------------------------------
// <copyright file="RequestCoalescerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Performance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Performance;

/// <summary>
/// Unit tests for <see cref="RequestCoalescer"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate:
/// </para>
/// <list type="bullet">
///   <item><description>Single request processing through the coalescer</description></item>
///   <item><description>Multiple rapid requests being coalesced</description></item>
///   <item><description>Coalescing window configuration</description></item>
///   <item><description>Error propagation from inner service</description></item>
///   <item><description>Null argument validation</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.8c")]
public class RequestCoalescerTests
{
    private readonly Mock<IChatCompletionService> _chatServiceMock;
    private readonly PerformanceOptions _options;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public RequestCoalescerTests()
    {
        _chatServiceMock = new Mock<IChatCompletionService>();
        _options = new PerformanceOptions
        {
            // Use a short window for fast tests
            CoalescingWindow = TimeSpan.FromMilliseconds(50)
        };
    }

    /// <summary>
    /// Creates a new RequestCoalescer with the test dependencies.
    /// </summary>
    private RequestCoalescer CreateCoalescer() => new(
        _chatServiceMock.Object,
        NullLogger<RequestCoalescer>.Instance,
        Options.Create(_options));

    /// <summary>
    /// Creates a simple chat request for testing.
    /// </summary>
    private static ChatRequest CreateTestRequest(string message = "Test message")
        => ChatRequest.FromUserMessage(message);

    /// <summary>
    /// Creates a simple chat response for testing.
    /// </summary>
    private static ChatResponse CreateTestResponse(string content = "Response")
        => new(content, 10, 5, TimeSpan.FromMilliseconds(100), "stop");

    #region CoalesceAsync Tests

    /// <summary>
    /// Verifies that a single request is processed successfully.
    /// </summary>
    [Fact]
    public async Task CoalesceAsync_SingleRequest_ReturnsResponse()
    {
        // Arrange
        var expectedResponse = CreateTestResponse("Hello!");
        _chatServiceMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var coalescer = CreateCoalescer();
        var request = CreateTestRequest("Hello");

        // Act
        var response = await coalescer.CoalesceAsync(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Content.Should().Be("Hello!");
    }

    /// <summary>
    /// Verifies that multiple rapid requests are all processed.
    /// </summary>
    [Fact]
    public async Task CoalesceAsync_MultipleRapidRequests_AllReturnResponses()
    {
        // Arrange
        var response1 = CreateTestResponse("Response 1");
        var response2 = CreateTestResponse("Response 2");

        var callCount = 0;
        _chatServiceMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? response1 : response2;
            });

        var coalescer = CreateCoalescer();

        // Act — submit two requests rapidly
        var task1 = coalescer.CoalesceAsync(CreateTestRequest("Request 1"), CancellationToken.None);
        var task2 = coalescer.CoalesceAsync(CreateTestRequest("Request 2"), CancellationToken.None);

        var responses = await Task.WhenAll(task1, task2);

        // Assert
        responses.Should().HaveCount(2);
        responses.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    /// <summary>
    /// Verifies that exceptions from the inner service are propagated.
    /// </summary>
    [Fact]
    public async Task CoalesceAsync_InnerServiceThrows_PropagatesException()
    {
        // Arrange
        _chatServiceMock
            .Setup(s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        var coalescer = CreateCoalescer();
        var request = CreateTestRequest();

        // Act & Assert
        var act = () => coalescer.CoalesceAsync(request, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Service unavailable");
    }

    /// <summary>
    /// Verifies that null request throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task CoalesceAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var coalescer = CreateCoalescer();

        // Act & Assert
        var act = () => coalescer.CoalesceAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that CoalescingWindow returns the configured value.
    /// </summary>
    [Fact]
    public void CoalescingWindow_ReturnsConfiguredValue()
    {
        // Arrange
        var coalescer = CreateCoalescer();

        // Assert
        coalescer.CoalescingWindow.Should().Be(TimeSpan.FromMilliseconds(50));
    }

    /// <summary>
    /// Verifies that PendingRequestCount starts at zero.
    /// </summary>
    [Fact]
    public void PendingRequestCount_Initial_IsZero()
    {
        // Arrange
        var coalescer = CreateCoalescer();

        // Assert
        coalescer.PendingRequestCount.Should().Be(0);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws for null inner service.
    /// </summary>
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new RequestCoalescer(
            null!,
            NullLogger<RequestCoalescer>.Instance,
            Options.Create(new PerformanceOptions()));

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    /// <summary>
    /// Verifies that constructor throws for null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new RequestCoalescer(
            _chatServiceMock.Object,
            null!,
            Options.Create(new PerformanceOptions()));

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that constructor throws for null options.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new RequestCoalescer(
            _chatServiceMock.Object,
            NullLogger<RequestCoalescer>.Instance,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion
}
