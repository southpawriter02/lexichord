// -----------------------------------------------------------------------
// <copyright file="EditorAgentTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Editor;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor;

/// <summary>
/// Unit tests for <see cref="EditorAgent"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3b")]
public class EditorAgentTests
{
    private readonly Mock<IChatCompletionService> _chatServiceMock;
    private readonly Mock<IPromptRenderer> _rendererMock;
    private readonly Mock<IPromptTemplateRepository> _templateRepoMock;
    private readonly Mock<IContextOrchestrator> _orchestratorMock;
    private readonly Mock<ILogger<EditorAgent>> _loggerMock;
    private readonly Mock<IPromptTemplate> _templateMock;
    private readonly EditorAgent _sut;

    public EditorAgentTests()
    {
        _chatServiceMock = new Mock<IChatCompletionService>();
        _rendererMock = new Mock<IPromptRenderer>();
        _templateRepoMock = new Mock<IPromptTemplateRepository>();
        _orchestratorMock = new Mock<IContextOrchestrator>();
        _loggerMock = new Mock<ILogger<EditorAgent>>();
        _templateMock = new Mock<IPromptTemplate>();

        // Default setup: context orchestrator returns empty context
        _orchestratorMock
            .Setup(o => o.AssembleAsync(
                It.IsAny<ContextGatheringRequest>(),
                It.IsAny<ContextBudget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssembledContext.Empty);

        // Default setup: template repository returns a template
        _templateRepoMock
            .Setup(r => r.GetTemplate(It.IsAny<string>()))
            .Returns(_templateMock.Object);

        // Default setup: renderer returns system + user messages
        _rendererMock
            .Setup(r => r.RenderMessages(
                It.IsAny<IPromptTemplate>(),
                It.IsAny<IDictionary<string, object>>()))
            .Returns(new[]
            {
                ChatMessage.System("You are an editor."),
                ChatMessage.User("Rewrite this text.")
            });

        // Default setup: LLM returns a rewritten response
        _chatServiceMock
            .Setup(c => c.CompleteAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                "Rewritten text here.",
                PromptTokens: 50,
                CompletionTokens: 20,
                Duration: TimeSpan.FromMilliseconds(250),
                FinishReason: "stop"));

        _sut = new EditorAgent(
            _chatServiceMock.Object,
            _rendererMock.Object,
            _templateRepoMock.Object,
            _orchestratorMock.Object,
            _loggerMock.Object);
    }

    // ── IAgent Property Tests ───────────────────────────────────────────

    [Fact]
    public void AgentId_ReturnsEditor()
    {
        _sut.AgentId.Should().Be("editor");
    }

    [Fact]
    public void Name_ReturnsTheEditor()
    {
        _sut.Name.Should().Be("The Editor");
    }

    [Fact]
    public void Capabilities_IncludesExpectedFlags()
    {
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.Chat);
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.DocumentContext);
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.StyleEnforcement);
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.Streaming);
    }

    [Fact]
    public void Capabilities_DoesNotIncludeRAGContext()
    {
        _sut.Capabilities.Should().NotHaveFlag(AgentCapabilities.RAGContext);
    }

    // ── GetTemplateId Tests ─────────────────────────────────────────────

    [Theory]
    [InlineData(RewriteIntent.Formal, "editor-rewrite-formal")]
    [InlineData(RewriteIntent.Simplified, "editor-rewrite-simplify")]
    [InlineData(RewriteIntent.Expanded, "editor-rewrite-expand")]
    [InlineData(RewriteIntent.Custom, "editor-rewrite-custom")]
    public void GetTemplateId_AllIntents_ReturnCorrectIds(
        RewriteIntent intent,
        string expectedTemplateId)
    {
        // Act
        var templateId = _sut.GetTemplateId(intent);

        // Assert
        templateId.Should().Be(expectedTemplateId);
    }

    [Fact]
    public void GetTemplateId_InvalidIntent_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => _sut.GetTemplateId((RewriteIntent)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── RewriteAsync Tests ──────────────────────────────────────────────

    [Fact]
    public async Task RewriteAsync_FormalIntent_ReturnsRewrittenText()
    {
        // Arrange
        var request = CreateRequest(RewriteIntent.Formal, "hey whats up");

        // Act
        var result = await _sut.RewriteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RewrittenText.Should().Be("Rewritten text here.");
        result.Intent.Should().Be(RewriteIntent.Formal);
        result.OriginalText.Should().Be("hey whats up");
    }

    [Fact]
    public async Task RewriteAsync_SimplifiedIntent_UsesCorrectTemplate()
    {
        // Arrange
        var request = CreateRequest(RewriteIntent.Simplified, "Some complex text");

        // Act
        await _sut.RewriteAsync(request);

        // Assert
        _templateRepoMock.Verify(
            r => r.GetTemplate("editor-rewrite-simplify"),
            Times.Once);
    }

    [Fact]
    public async Task RewriteAsync_ExpandedIntent_UsesCorrectTemplate()
    {
        // Arrange
        var request = CreateRequest(RewriteIntent.Expanded, "Brief text");

        // Act
        await _sut.RewriteAsync(request);

        // Assert
        _templateRepoMock.Verify(
            r => r.GetTemplate("editor-rewrite-expand"),
            Times.Once);
    }

    [Fact]
    public async Task RewriteAsync_CustomIntent_IncludesCustomInstruction()
    {
        // Arrange
        var request = CreateRequest(RewriteIntent.Custom, "Original text");

        // Act
        await _sut.RewriteAsync(request);

        // Assert — Verify that the custom instruction is included in the prompt variables
        _rendererMock.Verify(
            r => r.RenderMessages(
                It.IsAny<IPromptTemplate>(),
                It.Is<IDictionary<string, object>>(v =>
                    v.ContainsKey("custom_instruction") &&
                    (string)v["custom_instruction"] == "Make it better")),
            Times.Once);
    }

    [Fact]
    public async Task RewriteAsync_TrimsLlmResponse()
    {
        // Arrange
        _chatServiceMock
            .Setup(c => c.CompleteAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                "  Trimmed response  \n",
                PromptTokens: 10,
                CompletionTokens: 5,
                Duration: TimeSpan.FromMilliseconds(100),
                FinishReason: "stop"));

        var request = CreateRequest(RewriteIntent.Formal, "Test text");

        // Act
        var result = await _sut.RewriteAsync(request);

        // Assert
        result.RewrittenText.Should().Be("Trimmed response");
    }

    [Fact]
    public async Task RewriteAsync_CalculatesUsageMetrics()
    {
        // Arrange
        var request = CreateRequest(RewriteIntent.Formal, "Test text");

        // Act
        var result = await _sut.RewriteAsync(request);

        // Assert
        result.Usage.PromptTokens.Should().Be(50);
        result.Usage.CompletionTokens.Should().Be(20);
        result.Usage.EstimatedCost.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task RewriteAsync_SetsDuration()
    {
        // Arrange
        var request = CreateRequest(RewriteIntent.Formal, "Test text");

        // Act
        var result = await _sut.RewriteAsync(request);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task RewriteAsync_EmptySelection_ThrowsArgumentException()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "",
            SelectionSpan = new TextSpan(0, 0),
            Intent = RewriteIntent.Formal
        };

        // Act
        var act = () => _sut.RewriteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RewriteAsync_CustomWithoutInstruction_ThrowsArgumentException()
    {
        // Arrange
        var request = new RewriteRequest
        {
            SelectedText = "Some text",
            SelectionSpan = new TextSpan(0, 9),
            Intent = RewriteIntent.Custom,
            CustomInstruction = null
        };

        // Act
        var act = () => _sut.RewriteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RewriteAsync_UserCancellation_ReturnsFailedResult()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // LOGIC: The cancelled token must propagate through the mocked services.
        // A real IChatCompletionService would observe the token and throw, so
        // we set up the mock to do the same when any cancelled token is passed.
        _chatServiceMock
            .Setup(c => c.CompleteAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns<ChatRequest, CancellationToken>((_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new ChatResponse(
                    "Rewritten text", 100, 50, TimeSpan.FromMilliseconds(50), "stop"));
            });

        var request = CreateRequest(RewriteIntent.Formal, "Test text");

        // Act
        var result = await _sut.RewriteAsync(request, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
        result.RewrittenText.Should().Be("Test text"); // Original preserved
    }

    [Fact]
    public async Task RewriteAsync_LlmException_ReturnsFailedResult()
    {
        // Arrange
        _chatServiceMock
            .Setup(c => c.CompleteAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM provider error"));

        var request = CreateRequest(RewriteIntent.Formal, "Test text");

        // Act
        var result = await _sut.RewriteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("LLM provider error");
        result.RewrittenText.Should().Be("Test text");
    }

    [Fact]
    public async Task RewriteAsync_TemplateNotFound_ReturnsFailedResult()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetTemplate(It.IsAny<string>()))
            .Returns((IPromptTemplate?)null);

        var request = CreateRequest(RewriteIntent.Formal, "Test text");

        // Act
        var result = await _sut.RewriteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task RewriteAsync_GathersContextWithCorrectBudget()
    {
        // Arrange
        var request = CreateRequest(RewriteIntent.Formal, "Test text");

        // Act
        await _sut.RewriteAsync(request);

        // Assert
        _orchestratorMock.Verify(
            o => o.AssembleAsync(
                It.Is<ContextGatheringRequest>(r =>
                    r.AgentId == "editor" &&
                    r.SelectedText == "Test text"),
                It.Is<ContextBudget>(b =>
                    b.MaxTokens == 4000 &&
                    b.RequiredStrategies != null &&
                    b.RequiredStrategies.Contains("style") &&
                    b.RequiredStrategies.Contains("terminology")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RewriteAsync_BuildsPromptVariablesFromContext()
    {
        // Arrange
        var fragments = new List<ContextFragment>
        {
            new("style", "Style Rules", "Use formal tone.", 10, 1.0f),
            new("terminology", "Terminology", "LLM = Large Language Model", 8, 0.9f)
        };
        var assembledContext = new AssembledContext(
            fragments,
            TotalTokens: 18,
            Variables: new Dictionary<string, object>(),
            AssemblyDuration: TimeSpan.FromMilliseconds(50));

        _orchestratorMock
            .Setup(o => o.AssembleAsync(
                It.IsAny<ContextGatheringRequest>(),
                It.IsAny<ContextBudget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assembledContext);

        var request = CreateRequest(RewriteIntent.Formal, "Hello world");

        // Act
        await _sut.RewriteAsync(request);

        // Assert — Verify variables include style_rules and terminology
        _rendererMock.Verify(
            r => r.RenderMessages(
                It.IsAny<IPromptTemplate>(),
                It.Is<IDictionary<string, object>>(v =>
                    v.ContainsKey("selection") &&
                    (string)v["selection"] == "Hello world" &&
                    v.ContainsKey("style_rules") &&
                    v.ContainsKey("terminology"))),
            Times.Once);
    }

    [Fact]
    public async Task RewriteAsync_ContextAssemblyFails_StillReturnsResult()
    {
        // Arrange — Context orchestrator throws but agent should gracefully degrade
        _orchestratorMock
            .Setup(o => o.AssembleAsync(
                It.IsAny<ContextGatheringRequest>(),
                It.IsAny<ContextBudget>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Strategy failed"));

        var request = CreateRequest(RewriteIntent.Formal, "Test text");

        // Act
        var result = await _sut.RewriteAsync(request);

        // Assert — Should succeed despite context failure (graceful degradation)
        result.Success.Should().BeTrue();
        result.RewrittenText.Should().Be("Rewritten text here.");
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    private static RewriteRequest CreateRequest(RewriteIntent intent, string text) => new()
    {
        SelectedText = text,
        SelectionSpan = new TextSpan(0, text.Length),
        Intent = intent,
        CustomInstruction = intent == RewriteIntent.Custom ? "Make it better" : null,
        DocumentPath = "/test/document.md"
    };
}
