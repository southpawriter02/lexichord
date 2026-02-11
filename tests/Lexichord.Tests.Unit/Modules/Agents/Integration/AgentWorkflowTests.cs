// -----------------------------------------------------------------------
// <copyright file="AgentWorkflowTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Integration tests for the complete CoPilotAgent invocation workflow.
//   Validates end-to-end behavior through mocked dependency chains:
//   context assembly → prompt rendering → LLM invocation → citation extraction
//   → usage metrics.
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
/// Integration tests for end-to-end CoPilotAgent workflows.
/// </summary>
public class AgentWorkflowTests : IntegrationTestBase
{
    [Fact]
    public async Task SendMessage_ReceivesResponse_WithTokenCounts()
    {
        // Arrange
        LLMServer.ConfigureResponse("The sky is blue.", promptTokens: 100, completionTokens: 40);
        var agent = CreateAgent();
        var request = new AgentRequest("Why is the sky blue?");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert
        response.Content.Should().Be("The sky is blue.");
        response.Usage.Should().NotBeNull();
        response.Usage.PromptTokens.Should().Be(100);
        response.Usage.CompletionTokens.Should().Be(40);
        response.Usage.TotalTokens.Should().Be(140);
        response.Usage.EstimatedCost.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task SendMessage_WithDocumentContext_IncludesInContext()
    {
        // Arrange
        LLMServer.ConfigureResponse("Improved sentence.");
        var agent = CreateAgent();
        var request = new AgentRequest(
            "Improve this",
            DocumentPath: "/workspace/chapter1.md");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert — verify ContextInjector received the document path
        response.Content.Should().Be("Improved sentence.");
        MockContextInjector.Verify(c => c.AssembleContextAsync(
            It.Is<ContextRequest>(r => r.CurrentDocumentPath == "/workspace/chapter1.md"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_WithSelection_IncludesInContext()
    {
        // Arrange
        LLMServer.ConfigureResponse("Rewritten selection.");
        var agent = CreateAgent();
        var request = new AgentRequest(
            "Rewrite this paragraph",
            Selection: "The quick brown fox jumps over the lazy dog.");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert — verify prompt renderer received selection in variables
        response.Content.Should().Be("Rewritten selection.");
        MockPromptRenderer.Verify(r => r.RenderMessages(
            It.IsAny<IPromptTemplate>(),
            It.Is<IDictionary<string, object>>(d =>
                d.ContainsKey("selection_text") &&
                d["selection_text"].ToString() == "The quick brown fox jumps over the lazy dog.")),
            Times.Once);
    }

    [Fact]
    public async Task MultiTurnConversation_PreservesHistory()
    {
        // Arrange
        LLMServer.ConfigureResponse("Continuing the conversation.");
        var agent = CreateAgent();
        var history = new[]
        {
            ChatMessage.User("First question"),
            ChatMessage.Assistant("First answer"),
        };
        var request = new AgentRequest("Follow-up question", History: history);

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert — verify CompleteAsync received message list with history prepended
        response.Content.Should().Be("Continuing the conversation.");
        LLMServer.Mock.Verify(s => s.CompleteAsync(
            It.Is<ChatRequest>(r => r.Messages.Length >= 4), // 2 history + system + user
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_EmptyMessage_ThrowsArgumentException()
    {
        // Arrange
        var agent = CreateAgent();
        var request = new AgentRequest("   "); // whitespace-only

        // Act & Assert
        await FluentActions.Invoking(() => agent.InvokeAsync(request))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUsageMetrics_WithCalculatedCost()
    {
        // Arrange
        LLMServer.ConfigureResponse("Response text.", promptTokens: 1000, completionTokens: 500);
        var agent = CreateAgent();
        var request = new AgentRequest("Calculate cost");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert
        response.Usage.PromptTokens.Should().Be(1000);
        response.Usage.CompletionTokens.Should().Be(500);
        response.Usage.TotalTokens.Should().Be(1500);
        response.Usage.EstimatedCost.Should().BeGreaterThan(0m);
        response.Usage.ToDisplayString().Should().Contain("1,500 tokens");
    }

    [Fact]
    public async Task InvokeAsync_PromptRendererReceivesCorrectTemplate()
    {
        // Arrange
        LLMServer.ConfigureResponse("Template test.");
        var agent = CreateAgent();
        var request = new AgentRequest("Hello");

        // Act
        await agent.InvokeAsync(request);

        // Assert
        MockPromptRenderer.Verify(r => r.RenderMessages(
            It.Is<IPromptTemplate>(t => t.TemplateId == "co-pilot-editor"),
            It.IsAny<IDictionary<string, object>>()),
            Times.Once);
    }
}
