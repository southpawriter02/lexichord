// -----------------------------------------------------------------------
// <copyright file="SimplifierAgentTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Simplifier;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="SimplifierAgent"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4b")]
public class SimplifierAgentTests
{
    private readonly Mock<IChatCompletionService> _chatServiceMock;
    private readonly Mock<IPromptRenderer> _promptRendererMock;
    private readonly Mock<IPromptTemplateRepository> _templateRepositoryMock;
    private readonly Mock<IContextOrchestrator> _contextOrchestratorMock;
    private readonly Mock<IReadabilityTargetService> _targetServiceMock;
    private readonly Mock<IReadabilityService> _readabilityServiceMock;
    private readonly Mock<ISimplificationResponseParser> _responseParserMock;
    private readonly SimplifierAgent _sut;

    public SimplifierAgentTests()
    {
        _chatServiceMock = new Mock<IChatCompletionService>();
        _promptRendererMock = new Mock<IPromptRenderer>();
        _templateRepositoryMock = new Mock<IPromptTemplateRepository>();
        _contextOrchestratorMock = new Mock<IContextOrchestrator>();
        _targetServiceMock = new Mock<IReadabilityTargetService>();
        _readabilityServiceMock = new Mock<IReadabilityService>();
        _responseParserMock = new Mock<ISimplificationResponseParser>();

        // Default template setup
        var mockTemplate = new Mock<IPromptTemplate>();
        _templateRepositoryMock
            .Setup(r => r.GetTemplate("specialist-simplifier"))
            .Returns(mockTemplate.Object);

        // Default context setup
        _contextOrchestratorMock
            .Setup(o => o.AssembleAsync(It.IsAny<ContextGatheringRequest>(), It.IsAny<ContextBudget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssembledContext.Empty);

        // Default target service setup
        _targetServiceMock
            .Setup(t => t.GetTargetAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReadabilityTarget.FromExplicit(8.0, 20, true));

        // Default readability service setup
        _readabilityServiceMock
            .Setup(r => r.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadabilityMetrics
            {
                FleschKincaidGradeLevel = 12.0,
                GunningFogIndex = 13.0,
                FleschReadingEase = 50,
                WordCount = 100,
                SentenceCount = 5,
                SyllableCount = 150,
                ComplexWordCount = 15
            });

        // Default prompt renderer setup
        _promptRendererMock
            .Setup(r => r.RenderMessages(It.IsAny<IPromptTemplate>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new ChatMessage[]
            {
                ChatMessage.System("System prompt"),
                ChatMessage.User("User prompt")
            });

        _sut = new SimplifierAgent(
            _chatServiceMock.Object,
            _promptRendererMock.Object,
            _templateRepositoryMock.Object,
            _contextOrchestratorMock.Object,
            _targetServiceMock.Object,
            _readabilityServiceMock.Object,
            _responseParserMock.Object,
            NullLogger<SimplifierAgent>.Instance);
    }

    // ── IAgent Properties Tests ────────────────────────────────────────

    [Fact]
    public void AgentId_ReturnsSimplifier()
    {
        _sut.AgentId.Should().Be("simplifier");
    }

    [Fact]
    public void Name_ReturnsTheSimplifier()
    {
        _sut.Name.Should().Be("The Simplifier");
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        _sut.Description.Should().NotBeNullOrWhiteSpace();
        _sut.Description.ToLowerInvariant().Should().Contain("simplif");
    }

    [Fact]
    public void Template_IsNotNull()
    {
        _sut.Template.Should().NotBeNull();
    }

    [Fact]
    public void Capabilities_IncludesChat()
    {
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.Chat);
    }

    [Fact]
    public void Capabilities_IncludesDocumentContext()
    {
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.DocumentContext);
    }

    [Fact]
    public void Capabilities_IncludesStreaming()
    {
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.Streaming);
    }

    [Fact]
    public void Capabilities_IncludesStyleEnforcement()
    {
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.StyleEnforcement);
    }

    // ── Constructor Validation Tests ────────────────────────────────────

    [Fact]
    public void Constructor_NullChatService_ThrowsArgumentNullException()
    {
        var act = () => new SimplifierAgent(
            null!,
            _promptRendererMock.Object,
            _templateRepositoryMock.Object,
            _contextOrchestratorMock.Object,
            _targetServiceMock.Object,
            _readabilityServiceMock.Object,
            _responseParserMock.Object,
            NullLogger<SimplifierAgent>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("chatService");
    }

    [Fact]
    public void Constructor_NullPromptRenderer_ThrowsArgumentNullException()
    {
        var act = () => new SimplifierAgent(
            _chatServiceMock.Object,
            null!,
            _templateRepositoryMock.Object,
            _contextOrchestratorMock.Object,
            _targetServiceMock.Object,
            _readabilityServiceMock.Object,
            _responseParserMock.Object,
            NullLogger<SimplifierAgent>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("promptRenderer");
    }

    [Fact]
    public void Constructor_NullTemplateRepository_ThrowsArgumentNullException()
    {
        var act = () => new SimplifierAgent(
            _chatServiceMock.Object,
            _promptRendererMock.Object,
            null!,
            _contextOrchestratorMock.Object,
            _targetServiceMock.Object,
            _readabilityServiceMock.Object,
            _responseParserMock.Object,
            NullLogger<SimplifierAgent>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("templateRepository");
    }

    [Fact]
    public void Constructor_NullContextOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new SimplifierAgent(
            _chatServiceMock.Object,
            _promptRendererMock.Object,
            _templateRepositoryMock.Object,
            null!,
            _targetServiceMock.Object,
            _readabilityServiceMock.Object,
            _responseParserMock.Object,
            NullLogger<SimplifierAgent>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contextOrchestrator");
    }

    [Fact]
    public void Constructor_NullTargetService_ThrowsArgumentNullException()
    {
        var act = () => new SimplifierAgent(
            _chatServiceMock.Object,
            _promptRendererMock.Object,
            _templateRepositoryMock.Object,
            _contextOrchestratorMock.Object,
            null!,
            _readabilityServiceMock.Object,
            _responseParserMock.Object,
            NullLogger<SimplifierAgent>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("targetService");
    }

    [Fact]
    public void Constructor_NullReadabilityService_ThrowsArgumentNullException()
    {
        var act = () => new SimplifierAgent(
            _chatServiceMock.Object,
            _promptRendererMock.Object,
            _templateRepositoryMock.Object,
            _contextOrchestratorMock.Object,
            _targetServiceMock.Object,
            null!,
            _responseParserMock.Object,
            NullLogger<SimplifierAgent>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("readabilityService");
    }

    [Fact]
    public void Constructor_NullResponseParser_ThrowsArgumentNullException()
    {
        var act = () => new SimplifierAgent(
            _chatServiceMock.Object,
            _promptRendererMock.Object,
            _templateRepositoryMock.Object,
            _contextOrchestratorMock.Object,
            _targetServiceMock.Object,
            _readabilityServiceMock.Object,
            null!,
            NullLogger<SimplifierAgent>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("responseParser");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SimplifierAgent(
            _chatServiceMock.Object,
            _promptRendererMock.Object,
            _templateRepositoryMock.Object,
            _contextOrchestratorMock.Object,
            _targetServiceMock.Object,
            _readabilityServiceMock.Object,
            _responseParserMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ── SimplifyAsync Tests ────────────────────────────────────────

    [Fact]
    public async Task SimplifyAsync_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var request = new SimplificationRequest
        {
            OriginalText = "This is complex text that needs simplification.",
            Target = target,
            Strategy = SimplificationStrategy.Balanced
        };

        _chatServiceMock
            .Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse("Simplified text.", 500, 100, TimeSpan.FromSeconds(2), "stop"));

        _responseParserMock
            .Setup(p => p.Parse(It.IsAny<string>()))
            .Returns(new SimplificationParseResult(
                "Simplified text.",
                Array.Empty<SimplificationChange>(),
                null));

        _readabilityServiceMock
            .SetupSequence(r => r.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadabilityMetrics { FleschKincaidGradeLevel = 12.0, WordCount = 50 })
            .ReturnsAsync(new ReadabilityMetrics { FleschKincaidGradeLevel = 8.0, WordCount = 40 });

        // Act
        var result = await _sut.SimplifyAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.SimplifiedText.Should().Be("Simplified text.");
        result.OriginalMetrics.FleschKincaidGradeLevel.Should().Be(12.0);
        result.SimplifiedMetrics.FleschKincaidGradeLevel.Should().Be(8.0);
        result.StrategyUsed.Should().Be(SimplificationStrategy.Balanced);
        result.TokenUsage.Should().NotBe(UsageMetrics.Zero);
    }

    [Fact]
    public async Task SimplifyAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.SimplifyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task SimplifyAsync_TemplateNotFound_ReturnsFailedResult()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var request = new SimplificationRequest
        {
            OriginalText = "Text to simplify.",
            Target = target
        };

        _templateRepositoryMock
            .Setup(r => r.GetTemplate("specialist-simplifier"))
            .Returns((IPromptTemplate?)null);

        // Act
        var result = await _sut.SimplifyAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("template");
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task SimplifyAsync_HandlesUserCancellation()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var request = new SimplificationRequest
        {
            OriginalText = "Text to simplify.",
            Target = target
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // LOGIC: The readability service must throw when cancelled to
        // trigger the user cancellation handling path in SimplifierAgent.
        _readabilityServiceMock
            .Setup(r => r.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act
        var result = await _sut.SimplifyAsync(request, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task SimplifyAsync_GathersContextWithCorrectBudget()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var request = new SimplificationRequest
        {
            OriginalText = "Text to simplify.",
            Target = target,
            DocumentPath = "/path/to/document.md"
        };

        SetupSuccessfulCompletion();

        ContextBudget? capturedBudget = null;
        _contextOrchestratorMock
            .Setup(o => o.AssembleAsync(
                It.IsAny<ContextGatheringRequest>(),
                It.IsAny<ContextBudget>(),
                It.IsAny<CancellationToken>()))
            .Callback<ContextGatheringRequest, ContextBudget, CancellationToken>((_, b, _) => capturedBudget = b)
            .ReturnsAsync(AssembledContext.Empty);

        // Act
        await _sut.SimplifyAsync(request);

        // Assert
        capturedBudget.Should().NotBeNull();
        capturedBudget!.MaxTokens.Should().Be(4000);
        capturedBudget.RequiredStrategies.Should().Contain("style");
        capturedBudget.RequiredStrategies.Should().Contain("terminology");
    }

    [Fact]
    public async Task SimplifyAsync_BuildsPromptVariablesCorrectly()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var request = new SimplificationRequest
        {
            OriginalText = "Complex text here.",
            Target = target,
            Strategy = SimplificationStrategy.Aggressive,
            GenerateGlossary = true
        };

        SetupSuccessfulCompletion();

        IDictionary<string, object>? capturedVariables = null;
        _promptRendererMock
            .Setup(r => r.RenderMessages(It.IsAny<IPromptTemplate>(), It.IsAny<IDictionary<string, object>>()))
            .Callback<IPromptTemplate, IDictionary<string, object>>((_, v) => capturedVariables = v)
            .Returns(new ChatMessage[] { ChatMessage.User("Test") });

        // Act
        await _sut.SimplifyAsync(request);

        // Assert
        capturedVariables.Should().NotBeNull();
        capturedVariables!["original_text"].Should().Be("Complex text here.");
        capturedVariables["target_grade_level"].Should().Be("8.0");
        capturedVariables["strategy"].Should().Be("Aggressive");
        capturedVariables["strategy_aggressive"].Should().Be(true);
        capturedVariables["strategy_balanced"].Should().Be(false);
        capturedVariables["generate_glossary"].Should().Be(true);
    }

    // ── ValidateRequest Tests ────────────────────────────────────────

    [Fact]
    public void ValidateRequest_InvalidRequest_ReturnsErrors()
    {
        // Arrange
        var request = new SimplificationRequest
        {
            OriginalText = "",
            Target = null!
        };

        // Act
        var result = _sut.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("Original text"));
        result.Errors.Should().Contain(e => e.Contains("target"));
    }

    [Fact]
    public void ValidateRequest_TextTooLong_ReturnsError()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var longText = new string('a', SimplificationRequest.MaxTextLength + 1);
        var request = new SimplificationRequest
        {
            OriginalText = longText,
            Target = target
        };

        // Act
        var result = _sut.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("exceeds maximum"));
    }

    [Fact]
    public void ValidateRequest_ValidRequest_ReturnsValid()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var request = new SimplificationRequest
        {
            OriginalText = "Valid text that needs simplification.",
            Target = target
        };

        // Act
        var result = _sut.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRequest_LongText_ReturnsWarning()
    {
        // Arrange
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var longishText = new string('a', 25_000); // Above warning threshold but below max
        var request = new SimplificationRequest
        {
            OriginalText = longishText,
            Target = target
        };

        // Act
        var result = _sut.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("very long"));
    }

    [Fact]
    public void ValidateRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.ValidateRequest(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    // ── InvokeAsync Tests ────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_DelegatesToSimplifyAsync()
    {
        // Arrange
        var agentRequest = new AgentRequest(
            UserMessage: "Simplify this text",
            Selection: "Complex text that needs simplification.");

        SetupSuccessfulCompletion();

        // Act
        var result = await _sut.InvokeAsync(agentRequest);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
        _chatServiceMock.Verify(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.InvokeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    // ── Helper Methods ────────────────────────────────────────────────

    private void SetupSuccessfulCompletion()
    {
        _chatServiceMock
            .Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse("Simplified text.", 500, 100, TimeSpan.FromSeconds(2), "stop"));

        _responseParserMock
            .Setup(p => p.Parse(It.IsAny<string>()))
            .Returns(new SimplificationParseResult(
                "Simplified text.",
                Array.Empty<SimplificationChange>(),
                null));
    }
}
