// -----------------------------------------------------------------------
// <copyright file="SummarizationCommandParserTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Summarizer;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Summarizer;

/// <summary>
/// Unit tests for <see cref="SummarizerAgent.ParseCommand"/> method.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6a")]
public class SummarizationCommandParserTests
{
    private readonly SummarizerAgent _sut;

    public SummarizationCommandParserTests()
    {
        // LOGIC: Create SummarizerAgent with mock dependencies.
        // ParseCommand is a pure method that doesn't use any injected services,
        // but we need a valid instance to call it.
        var chatServiceMock = new Mock<IChatCompletionService>();
        var promptRendererMock = new Mock<IPromptRenderer>();
        var templateRepositoryMock = new Mock<IPromptTemplateRepository>();
        var contextOrchestratorMock = new Mock<IContextOrchestrator>();
        var fileServiceMock = new Mock<IFileService>();
        var licenseContextMock = new Mock<ILicenseContext>();
        var mediatorMock = new Mock<IMediator>();

        // Default template setup
        var mockTemplate = new Mock<IPromptTemplate>();
        templateRepositoryMock
            .Setup(r => r.GetTemplate("specialist-summarizer"))
            .Returns(mockTemplate.Object);

        _sut = new SummarizerAgent(
            chatServiceMock.Object,
            promptRendererMock.Object,
            templateRepositoryMock.Object,
            contextOrchestratorMock.Object,
            fileServiceMock.Object,
            licenseContextMock.Object,
            mediatorMock.Object,
            NullLogger<SummarizerAgent>.Instance);
    }

    // ── Mode Detection: Default (BulletPoints) ──────────────────────────

    [Theory]
    [InlineData("Summarize this")]
    [InlineData("Summarize")]
    [InlineData("Can you summarize this document?")]
    public void ParseCommand_DefaultCommand_ReturnsBulletPoints(string command)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.Mode.Should().Be(SummarizationMode.BulletPoints);
        result.MaxItems.Should().Be(5);
    }

    // ── Mode Detection: Abstract ────────────────────────────────────────

    [Theory]
    [InlineData("Create an abstract")]
    [InlineData("Write an abstract for this paper")]
    [InlineData("Generate academic abstract")]
    public void ParseCommand_AbstractKeywords_ReturnsAbstractMode(string command)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.Mode.Should().Be(SummarizationMode.Abstract);
    }

    // ── Mode Detection: TLDR ────────────────────────────────────────────

    [Theory]
    [InlineData("TLDR")]
    [InlineData("TL;DR please")]
    [InlineData("Give me a tldr")]
    [InlineData("Too long didn't read version")]
    public void ParseCommand_TLDRKeywords_ReturnsTLDRMode(string command)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.Mode.Should().Be(SummarizationMode.TLDR);
    }

    // ── Mode Detection: Executive ───────────────────────────────────────

    [Theory]
    [InlineData("Executive summary")]
    [InlineData("Summarize for stakeholders")]
    [InlineData("Summary for management")]
    public void ParseCommand_ExecutiveKeywords_ReturnsExecutiveMode(string command)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.Mode.Should().Be(SummarizationMode.Executive);
    }

    // ── Mode Detection: KeyTakeaways ────────────────────────────────────

    [Theory]
    [InlineData("Key takeaways")]
    [InlineData("What are the insights?")]
    [InlineData("Main learnings from this")]
    public void ParseCommand_TakeawayKeywords_ReturnsKeyTakeawaysMode(string command)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.Mode.Should().Be(SummarizationMode.KeyTakeaways);
    }

    // ── Item Count Extraction ───────────────────────────────────────────

    [Theory]
    [InlineData("Summarize in 3 bullets", 3)]
    [InlineData("Give me 5 key points", 5)]
    [InlineData("7 bullet summary", 7)]
    [InlineData("Summarize in three points", 5)] // "three" not parsed as number
    public void ParseCommand_WithNumber_ExtractsMaxItems(string command, int expectedMaxItems)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.MaxItems.Should().Be(expectedMaxItems);
    }

    [Theory]
    [InlineData("3 key takeaways", 3)]
    [InlineData("Give me 10 takeaways", 10)]
    public void ParseCommand_TakeawaysWithNumber_ExtractsMaxItems(string command, int expectedMaxItems)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.Mode.Should().Be(SummarizationMode.KeyTakeaways);
        result.MaxItems.Should().Be(expectedMaxItems);
    }

    // ── Audience Extraction ─────────────────────────────────────────────

    [Theory]
    [InlineData("Summarize for developers", "developers")]
    [InlineData("TLDR for a technical audience", "technical audience")]
    [InlineData("Executive summary for the board", "the board")]
    public void ParseCommand_WithAudience_ExtractsTargetAudience(string command, string expectedAudienceContains)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.TargetAudience.Should().Contain(expectedAudienceContains);
    }

    [Theory]
    [InlineData("Summarize this for me")]
    [InlineData("Summarize for it")]
    public void ParseCommand_CommonFalsePositiveAudience_ReturnsNull(string command)
    {
        // Act
        var result = _sut.ParseCommand(command);

        // Assert
        result.TargetAudience.Should().BeNull();
    }
}
