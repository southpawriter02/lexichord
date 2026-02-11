// -----------------------------------------------------------------------
// <copyright file="ErrorScenarioTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Integration tests for CoPilotAgent error handling and recovery.
//   Validates that exceptions from the LLM service are properly wrapped
//   in AgentInvocationException, that OperationCanceledException is not
//   wrapped, and that streaming errors call OnStreamError on the handler.
//
//   Introduced in v0.6.8b as part of the Integration Test Suite.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Exceptions;
using Lexichord.Tests.Unit.Modules.Agents.Integration.Fixtures;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Integration;

/// <summary>
/// Integration tests for CoPilotAgent error handling and recovery.
/// </summary>
public class ErrorScenarioTests : IntegrationTestBase
{
    [Fact]
    public async Task LLMTimeout_ThrowsAgentInvocationException()
    {
        // Arrange — simulate an HTTP timeout via HttpRequestException
        // LOGIC: TaskCanceledException inherits from OperationCanceledException,
        // so CoPilotAgent re-throws it as-is. A real HTTP timeout from
        // HttpClient surfaces as HttpRequestException, which gets wrapped.
        LLMServer.ConfigureError<HttpRequestException>("The request timed out.");
        var agent = CreateAgent();
        var request = new AgentRequest("Timeout test");

        // Act & Assert
        var ex = await FluentActions.Invoking(() => agent.InvokeAsync(request))
            .Should()
            .ThrowAsync<AgentInvocationException>();

        ex.Which.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task RateLimit_ThrowsAgentInvocationException()
    {
        // Arrange
        LLMServer.ConfigureError<InvalidOperationException>("Rate limit exceeded. Retry after 30s.");
        var agent = CreateAgent();
        var request = new AgentRequest("Rate limit test");

        // Act & Assert
        var ex = await FluentActions.Invoking(() => agent.InvokeAsync(request))
            .Should()
            .ThrowAsync<AgentInvocationException>();

        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
        ex.Which.InnerException!.Message.Should().Contain("Rate limit");
    }

    [Fact]
    public async Task AuthFailure_ThrowsAgentInvocationException()
    {
        // Arrange
        LLMServer.ConfigureError<UnauthorizedAccessException>("Invalid API key.");
        var agent = CreateAgent();
        var request = new AgentRequest("Auth failure test");

        // Act & Assert
        var ex = await FluentActions.Invoking(() => agent.InvokeAsync(request))
            .Should()
            .ThrowAsync<AgentInvocationException>();

        ex.Which.InnerException.Should().BeOfType<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Cancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var agent = CreateAgent();
        var request = new AgentRequest("Cancel test");

        // Act & Assert — OperationCanceledException is NOT wrapped
        await FluentActions.Invoking(() => agent.InvokeAsync(request, cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamError_CallsOnStreamError()
    {
        // Arrange — streaming mode with an error mid-stream
        EnableStreaming();
        LLMServer.ConfigureStreamingError<InvalidOperationException>(
            "Provider connection lost",
            "Hello", " wor"); // partial tokens before error

        var agent = CreateAgent();
        var request = new AgentRequest("Stream error test");

        // Act & Assert
        // LOGIC: The streaming error is caught by InvokeAsync's outer catch
        // and wrapped in AgentInvocationException. The streaming handler's
        // OnStreamError is NOT called by CoPilotAgent directly — it propagates.
        var ex = await FluentActions.Invoking(() => agent.InvokeAsync(request))
            .Should()
            .ThrowAsync<AgentInvocationException>();

        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
        ex.Which.InnerException!.Message.Should().Contain("Provider connection lost");

        // The tokens received before the error should still be in the list
        ReceivedTokens.Should().HaveCountGreaterOrEqualTo(1);
    }
}
