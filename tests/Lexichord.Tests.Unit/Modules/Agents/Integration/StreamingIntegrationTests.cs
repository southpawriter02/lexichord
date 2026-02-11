// -----------------------------------------------------------------------
// <copyright file="StreamingIntegrationTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Integration tests for the CoPilotAgent streaming path.
//   Validates token relay to IStreamingChatHandler, content assembly,
//   completion signaling, license-gated streaming, and cancellation.
//
//   Introduced in v0.6.8b as part of the Integration Test Suite.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Tests.Unit.Modules.Agents.Integration.Fixtures;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Integration;

/// <summary>
/// Integration tests for CoPilotAgent streaming behavior.
/// </summary>
public class StreamingIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task StreamingMode_RelaysTokensToHandler()
    {
        // Arrange — enable streaming (Teams license) + configure tokens
        EnableStreaming();
        LLMServer.ConfigureStreamingTokens("Hello", " ", "world", "!");
        var agent = CreateAgent();
        var request = new AgentRequest("Stream test");

        // Act
        await agent.InvokeAsync(request);

        // Assert — each content token was relayed to the handler
        ReceivedTokens.Should().HaveCount(4);
        ReceivedTokens[0].Text.Should().Be("Hello");
        ReceivedTokens[1].Text.Should().Be(" ");
        ReceivedTokens[2].Text.Should().Be("world");
        ReceivedTokens[3].Text.Should().Be("!");
    }

    [Fact]
    public async Task StreamingMode_AssemblesFullContent()
    {
        // Arrange
        EnableStreaming();
        LLMServer.ConfigureStreamingTokens("Hello", " ", "world", "!");
        var agent = CreateAgent();
        var request = new AgentRequest("Stream content");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert — final response content is the concatenation of all tokens
        response.Content.Should().Be("Hello world!");
    }

    [Fact]
    public async Task StreamingMode_SignalsCompletion()
    {
        // Arrange
        EnableStreaming();
        LLMServer.ConfigureStreamingTokens("Done");
        var agent = CreateAgent();
        var request = new AgentRequest("Complete signal test");

        // Act
        await agent.InvokeAsync(request);

        // Assert — OnStreamComplete was called with the assembled content
        StreamCompleteResponse.Should().NotBeNull();
        StreamCompleteResponse!.Content.Should().Be("Done");
        MockStreamingHandler.Verify(
            h => h.OnStreamComplete(It.IsAny<ChatResponse>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamingMode_RequiresTeamsLicense()
    {
        // Arrange — WriterPro tier (no streaming)
        // Default base class sets WriterPro, so streaming via batch
        LLMServer.ConfigureResponse("Batch fallback content.");
        var agent = CreateAgent();
        var request = new AgentRequest("Should use batch");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert — batch mode used, no streaming handler calls
        response.Content.Should().Be("Batch fallback content.");
        ReceivedTokens.Should().BeEmpty();
        LLMServer.Mock.Verify(
            s => s.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamingMode_Cancellation_StopsStream()
    {
        // Arrange
        EnableStreaming();
        using var cts = new CancellationTokenSource();

        // Configure streaming with enough tokens to cancel mid-stream
        LLMServer.Mock.Setup(s => s.StreamAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((ChatRequest _, CancellationToken ct) =>
                CreateCancellableStream(cts, ct));

        var agent = CreateAgent();
        var request = new AgentRequest("Cancel me");

        // Act & Assert
        await FluentActions.Invoking(() => agent.InvokeAsync(request, cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Creates a token stream that cancels after the first token.
    /// </summary>
    private static async IAsyncEnumerable<StreamingChatToken> CreateCancellableStream(
        CancellationTokenSource cts,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        yield return StreamingChatToken.Content("First");
        await Task.Yield();
        cts.Cancel(); // Cancel after first token
        ct.ThrowIfCancellationRequested();
        yield return StreamingChatToken.Content("Never");
    }
}
