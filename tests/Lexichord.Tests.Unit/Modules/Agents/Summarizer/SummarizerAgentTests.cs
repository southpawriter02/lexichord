// -----------------------------------------------------------------------
// <copyright file="SummarizerAgentTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Summarizer;
using Lexichord.Modules.Agents.Summarizer.Events;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Summarizer;

/// <summary>
/// Unit tests for <see cref="SummarizerAgent"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6a")]
public class SummarizerAgentTests
{
    private readonly Mock<IChatCompletionService> _chatServiceMock;
    private readonly Mock<IPromptRenderer> _promptRendererMock;
    private readonly Mock<IPromptTemplateRepository> _templateRepositoryMock;
    private readonly Mock<IContextOrchestrator> _contextOrchestratorMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SummarizerAgent _sut;

    // ── Test Constants ──────────────────────────────────────────────────

    private const string SampleDocument =
        "The quick brown fox jumps over the lazy dog. " +
        "This is a sample document for testing purposes. " +
        "It contains multiple sentences to ensure proper word counting. " +
        "The document should be long enough to test basic summarization.";

    private const string BulletResponse =
        "• Identifies key patterns in document processing\n" +
        "• Demonstrates effective summarization techniques\n" +
        "• Shows proper handling of multi-sentence content\n" +
        "• Validates word count and compression metrics\n" +
        "• Confirms output format matches expectations";

    private const string AbstractResponse =
        "This document examines the principles of document processing and summarization. " +
        "Through analysis of text patterns, it demonstrates how content can be effectively " +
        "condensed while maintaining meaning and intent. The findings suggest that automated " +
        "summarization can achieve significant compression ratios while preserving key information. " +
        "These results have implications for content management and knowledge extraction workflows.";

    private const string TLDRResponse =
        "This document covers text processing and summarization techniques, showing that automated systems can effectively compress content while preserving meaning.";

    private const string KeyTakeawaysResponse =
        "**Takeaway 1:** Automated summarization achieves consistent compression ratios\n\n" +
        "The system reliably reduces document length by 5-20x depending on the mode used.\n\n" +
        "**Takeaway 2:** Content fidelity is maintained across modes\n\n" +
        "Key information is preserved regardless of the output format selected.\n\n" +
        "**Takeaway 3:** Natural language parsing enables intuitive interaction\n\n" +
        "Users can specify their desired output format using everyday language.";

    public SummarizerAgentTests()
    {
        _chatServiceMock = new Mock<IChatCompletionService>();
        _promptRendererMock = new Mock<IPromptRenderer>();
        _templateRepositoryMock = new Mock<IPromptTemplateRepository>();
        _contextOrchestratorMock = new Mock<IContextOrchestrator>();
        _fileServiceMock = new Mock<IFileService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();

        // Default template setup
        var mockTemplate = new Mock<IPromptTemplate>();
        _templateRepositoryMock
            .Setup(r => r.GetTemplate("specialist-summarizer"))
            .Returns(mockTemplate.Object);

        // Default context setup
        _contextOrchestratorMock
            .Setup(o => o.AssembleAsync(
                It.IsAny<ContextGatheringRequest>(),
                It.IsAny<ContextBudget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssembledContext.Empty);

        // Default prompt renderer setup
        _promptRendererMock
            .Setup(r => r.RenderMessages(
                It.IsAny<IPromptTemplate>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(new ChatMessage[]
            {
                ChatMessage.System("System prompt"),
                ChatMessage.User("User prompt")
            });

        // Default LLM response setup
        _chatServiceMock
            .Setup(c => c.CompleteAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                BulletResponse,
                PromptTokens: 500,
                CompletionTokens: 200,
                Duration: TimeSpan.FromSeconds(2),
                FinishReason: "stop"));

        // Default mediator setup — accept all publishes
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new SummarizerAgent(
            _chatServiceMock.Object,
            _promptRendererMock.Object,
            _templateRepositoryMock.Object,
            _contextOrchestratorMock.Object,
            _fileServiceMock.Object,
            _licenseContextMock.Object,
            _mediatorMock.Object,
            NullLogger<SummarizerAgent>.Instance);
    }

    // ── IAgent Properties Tests ─────────────────────────────────────────

    [Fact]
    public void AgentId_ReturnsSummarizer()
    {
        _sut.AgentId.Should().Be("summarizer");
    }

    [Fact]
    public void Name_ReturnsTheSummarizer()
    {
        _sut.Name.Should().Be("The Summarizer");
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        _sut.Description.Should().NotBeNullOrWhiteSpace();
        _sut.Description.ToLowerInvariant().Should().Contain("summar");
    }

    [Fact]
    public void Capabilities_IncludesSummarization()
    {
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.Summarization);
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.Chat);
        _sut.Capabilities.Should().HaveFlag(AgentCapabilities.DocumentContext);
    }

    // ── InvokeAsync Tests ───────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_DelegatesToSummarizeContentAsync()
    {
        // Arrange
        var request = new AgentRequest(
            UserMessage: "Summarize this document",
            History: null,
            DocumentPath: null,
            Selection: SampleDocument);

        // Act
        var response = await _sut.InvokeAsync(request, CancellationToken.None);

        // Assert
        response.Content.Should().NotBeNullOrWhiteSpace();
        _chatServiceMock.Verify(
            c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    // ── SummarizeContentAsync Tests ─────────────────────────────────────

    [Fact]
    public async Task SummarizeContentAsync_BulletPointsMode_ReturnsItems()
    {
        // Arrange
        var options = new SummarizationOptions
        {
            Mode = SummarizationMode.BulletPoints,
            MaxItems = 5
        };

        // Act
        var result = await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Mode.Should().Be(SummarizationMode.BulletPoints);
        result.Items.Should().NotBeNull();
        result.Items!.Count.Should().Be(5);
        result.Summary.Should().Contain("•");
    }

    [Fact]
    public async Task SummarizeContentAsync_AbstractMode_ReturnsProse()
    {
        // Arrange
        _chatServiceMock
            .Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                AbstractResponse,
                PromptTokens: 500,
                CompletionTokens: 300,
                Duration: TimeSpan.FromSeconds(3),
                FinishReason: "stop"));

        var options = new SummarizationOptions
        {
            Mode = SummarizationMode.Abstract,
            TargetWordCount = 200
        };

        // Act
        var result = await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Mode.Should().Be(SummarizationMode.Abstract);
        result.Items.Should().BeNull(); // Prose mode, no items
    }

    [Fact]
    public async Task SummarizeContentAsync_TLDRMode_ReturnsSingleParagraph()
    {
        // Arrange
        _chatServiceMock
            .Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                TLDRResponse,
                PromptTokens: 400,
                CompletionTokens: 100,
                Duration: TimeSpan.FromSeconds(1),
                FinishReason: "stop"));

        var options = new SummarizationOptions { Mode = SummarizationMode.TLDR };

        // Act
        var result = await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Mode.Should().Be(SummarizationMode.TLDR);
        result.Items.Should().BeNull(); // TLDR is prose mode
    }

    [Fact]
    public async Task SummarizeContentAsync_KeyTakeawaysMode_ReturnsFormattedTakeaways()
    {
        // Arrange
        _chatServiceMock
            .Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                KeyTakeawaysResponse,
                PromptTokens: 500,
                CompletionTokens: 250,
                Duration: TimeSpan.FromSeconds(2),
                FinishReason: "stop"));

        var options = new SummarizationOptions
        {
            Mode = SummarizationMode.KeyTakeaways,
            MaxItems = 3
        };

        // Act
        var result = await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Mode.Should().Be(SummarizationMode.KeyTakeaways);
        result.Summary.Should().Contain("**Takeaway 1:**");
        result.Summary.Should().Contain("**Takeaway 2:**");
        result.Summary.Should().Contain("**Takeaway 3:**");
        result.Items.Should().NotBeNull();
        result.Items!.Count.Should().Be(3);
    }

    [Fact]
    public async Task SummarizeContentAsync_CalculatesCompressionRatio()
    {
        // Arrange
        var options = new SummarizationOptions { Mode = SummarizationMode.BulletPoints };

        // Act
        var result = await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.OriginalWordCount.Should().BeGreaterThan(0);
        result.SummaryWordCount.Should().BeGreaterThan(0);
        result.CompressionRatio.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SummarizeContentAsync_CalculatesOriginalReadingMinutes()
    {
        // Arrange
        var options = new SummarizationOptions { Mode = SummarizationMode.BulletPoints };

        // Act
        var result = await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert
        result.OriginalReadingMinutes.Should().BeGreaterOrEqualTo(1);
    }

    // ── SummarizeAsync (File-based) Tests ───────────────────────────────

    [Fact]
    public async Task SummarizeAsync_ReadsFileViaFileService()
    {
        // Arrange
        var filePath = "/test/document.md";
        _fileServiceMock.Setup(f => f.Exists(filePath)).Returns(true);
        _fileServiceMock
            .Setup(f => f.LoadAsync(filePath, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoadResult.Succeeded(filePath, SampleDocument, System.Text.Encoding.UTF8));

        var options = new SummarizationOptions { Mode = SummarizationMode.BulletPoints };

        // Act
        var result = await _sut.SummarizeAsync(filePath, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _fileServiceMock.Verify(f => f.LoadAsync(filePath, null, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task SummarizeAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = "/test/missing.md";
        _fileServiceMock.Setup(f => f.Exists(filePath)).Returns(false);

        var options = new SummarizationOptions { Mode = SummarizationMode.BulletPoints };

        // Act
        var act = async () => await _sut.SummarizeAsync(filePath, options, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    // ── Chunking Tests ──────────────────────────────────────────────────

    [Fact]
    public async Task SummarizeContentAsync_ShortDocument_ProcessesAsSingleChunk()
    {
        // Arrange — SampleDocument is well under 4000 tokens
        var options = new SummarizationOptions { Mode = SummarizationMode.BulletPoints };

        // Act
        var result = await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert
        result.WasChunked.Should().BeFalse();
        result.ChunkCount.Should().Be(1);
        _chatServiceMock.Verify(
            c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task SummarizeContentAsync_LongDocument_ChunksAndCombines()
    {
        // Arrange — Generate content exceeding 4000 tokens (4000 * 4 = 16000 chars)
        var longContent = string.Join(" ", Enumerable.Repeat("The quick brown fox jumps over the lazy dog.", 500));
        var options = new SummarizationOptions { Mode = SummarizationMode.BulletPoints };

        // Act
        var result = await _sut.SummarizeContentAsync(longContent, options, CancellationToken.None);

        // Assert
        result.WasChunked.Should().BeTrue();
        result.ChunkCount.Should().BeGreaterThan(1);
        // LLM called multiple times: once per chunk + final combination
        _chatServiceMock.Verify(
            c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public void ChunkContent_PreservesSectionBoundaries()
    {
        // Arrange — Content with section headings and enough text per section
        var content =
            "## Section 1\n" +
            string.Join(" ", Enumerable.Repeat("Content for section one with enough text to matter.", 200)) + "\n\n" +
            "## Section 2\n" +
            string.Join(" ", Enumerable.Repeat("Content for section two with different material.", 200)) + "\n\n" +
            "## Section 3\n" +
            string.Join(" ", Enumerable.Repeat("Content for section three wrapping things up.", 200));

        // Act
        var chunks = _sut.ChunkContent(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);

        // Each chunk (after the first) should start near a section boundary
        foreach (var chunk in chunks.Skip(1))
        {
            chunk.TrimStart().Should().Contain("##");
        }
    }

    // ── Event Publishing Tests ──────────────────────────────────────────

    [Fact]
    public async Task SummarizeContentAsync_PublishesStartedAndCompletedEvents()
    {
        // Arrange
        var options = new SummarizationOptions { Mode = SummarizationMode.BulletPoints };

        // Act
        await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert — Started event published
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<SummarizationStartedEvent>(e => e.Mode == SummarizationMode.BulletPoints),
                It.IsAny<CancellationToken>()),
            Times.Once());

        // Assert — Completed event published
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<SummarizationCompletedEvent>(e =>
                    e.Mode == SummarizationMode.BulletPoints &&
                    e.OriginalWordCount > 0),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task SummarizeContentAsync_OnFailure_PublishesFailedEvent()
    {
        // Arrange — Make the LLM throw
        _chatServiceMock
            .Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM service error"));

        var options = new SummarizationOptions { Mode = SummarizationMode.BulletPoints };

        // Act
        var result = await _sut.SummarizeContentAsync(SampleDocument, options, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("LLM service error");

        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<SummarizationFailedEvent>(e =>
                    e.Mode == SummarizationMode.BulletPoints &&
                    e.ErrorMessage.Contains("LLM service error")),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    // ── GetDefaultOptions Tests ─────────────────────────────────────────

    [Theory]
    [InlineData(SummarizationMode.Abstract, 200)]
    [InlineData(SummarizationMode.TLDR, 75)]
    [InlineData(SummarizationMode.Executive, 150)]
    public void GetDefaultOptions_ProseMode_ReturnsCorrectTargetWordCount(
        SummarizationMode mode, int expectedWordCount)
    {
        // Act
        var options = _sut.GetDefaultOptions(mode);

        // Assert
        options.Mode.Should().Be(mode);
        options.TargetWordCount.Should().Be(expectedWordCount);
    }

    [Theory]
    [InlineData(SummarizationMode.BulletPoints)]
    [InlineData(SummarizationMode.KeyTakeaways)]
    public void GetDefaultOptions_ListMode_ReturnsMaxItems5(SummarizationMode mode)
    {
        // Act
        var options = _sut.GetDefaultOptions(mode);

        // Assert
        options.Mode.Should().Be(mode);
        options.MaxItems.Should().Be(5);
    }

    // ── SummarizationResult.Failed Tests ────────────────────────────────

    [Fact]
    public void SummarizationResult_Failed_ReturnsFailedResult()
    {
        // Act
        var result = SummarizationResult.Failed(
            SummarizationMode.Abstract,
            "Test error message",
            originalWordCount: 500);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Test error message");
        result.Summary.Should().BeEmpty();
        result.Mode.Should().Be(SummarizationMode.Abstract);
        result.OriginalWordCount.Should().Be(500);
        result.Usage.Should().Be(UsageMetrics.Zero);
        result.WasChunked.Should().BeFalse();
        result.ChunkCount.Should().Be(0);
    }

    [Fact]
    public void SummarizationResult_Failed_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Act
        var act = () => SummarizationResult.Failed(
            SummarizationMode.Abstract,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
