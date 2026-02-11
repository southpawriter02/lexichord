// -----------------------------------------------------------------------
// <copyright file="ContextInjectionIntegrationTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Integration tests for context assembly integration in CoPilotAgent.
//   Validates that RAG search context, style rules, and combined context
//   flow correctly through the pipeline to produce citations in the
//   final AgentResponse. Also verifies graceful degradation when
//   context assembly fails.
//
//   Introduced in v0.6.8b as part of the Integration Test Suite.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Tests.Unit.Modules.Agents.Integration.Fixtures;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Integration;

/// <summary>
/// Integration tests for context injection in CoPilotAgent.
/// </summary>
public class ContextInjectionIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task ContextInjection_RAGSearch_IncludedInContext()
    {
        // Arrange — context injector returns RAG search hits in context
        var searchHits = CreateSearchHits("RAG context paragraph.");
        MockContextInjector
            .Setup(c => c.AssembleContextAsync(
                It.IsAny<ContextRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object>
            {
                ["search_hits"] = searchHits,
                ["rag_context"] = "RAG context paragraph.",
            });

        LLMServer.ConfigureResponse("Answer with citations.");
        var agent = CreateAgent();
        var request = new AgentRequest("Tell me about the topic");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert — prompt renderer received RAG context
        response.Content.Should().Be("Answer with citations.");
        MockPromptRenderer.Verify(r => r.RenderMessages(
            It.IsAny<IPromptTemplate>(),
            It.Is<IDictionary<string, object>>(d => d.ContainsKey("rag_context"))),
            Times.Once);
    }

    [Fact]
    public async Task ContextInjection_StyleRules_IncludedInContext()
    {
        // Arrange
        MockContextInjector
            .Setup(c => c.AssembleContextAsync(
                It.IsAny<ContextRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object>
            {
                ["style_rules"] = "Use active voice. Avoid jargon.",
            });

        LLMServer.ConfigureResponse("Styled response.");
        var agent = CreateAgent();
        var request = new AgentRequest("Apply style");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert
        response.Content.Should().Be("Styled response.");
        MockPromptRenderer.Verify(r => r.RenderMessages(
            It.IsAny<IPromptTemplate>(),
            It.Is<IDictionary<string, object>>(d =>
                d.ContainsKey("style_rules") &&
                d["style_rules"].ToString() == "Use active voice. Avoid jargon.")),
            Times.Once);
    }

    [Fact]
    public async Task ContextInjection_Failure_GracefulDegradation()
    {
        // Arrange — context injector throws
        MockContextInjector
            .Setup(c => c.AssembleContextAsync(
                It.IsAny<ContextRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Context service unavailable"));

        LLMServer.ConfigureResponse("Response without context.");
        var agent = CreateAgent();
        var request = new AgentRequest("Works anyway");

        // Act — should NOT throw
        var response = await agent.InvokeAsync(request);

        // Assert — agent still returns a valid response
        response.Content.Should().Be("Response without context.");
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task ContextInjection_SearchHits_ProduceCitations()
    {
        // Arrange — search hits in context + citation service returns citations
        var searchHits = CreateSearchHits("Source paragraph.");
        var expectedCitations = new List<Citation>
        {
            new Citation(
                Guid.NewGuid(),
                "/docs/source.md",
                "Source Document",
                0, 100, "Introduction", 1,
                DateTime.UtcNow),
        };

        MockContextInjector
            .Setup(c => c.AssembleContextAsync(
                It.IsAny<ContextRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object>
            {
                ["search_hits"] = searchHits,
            });

        MockCitationService
            .Setup(c => c.CreateCitations(It.IsAny<IEnumerable<SearchHit>>()))
            .Returns(expectedCitations);

        LLMServer.ConfigureResponse("Cited response.");
        var agent = CreateAgent();
        var request = new AgentRequest("Cite sources");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert
        response.Content.Should().Be("Cited response.");
        response.Citations.Should().NotBeNull();
        response.Citations.Should().HaveCount(1);
        response.Citations![0].DocumentPath.Should().Be("/docs/source.md");
    }

    [Fact]
    public async Task ContextInjection_NoSearchHits_NoCitations()
    {
        // Arrange — empty context (no search_hits key)
        MockContextInjector
            .Setup(c => c.AssembleContextAsync(
                It.IsAny<ContextRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object>());

        LLMServer.ConfigureResponse("Plain response.");
        var agent = CreateAgent();
        var request = new AgentRequest("No citations needed");

        // Act
        var response = await agent.InvokeAsync(request);

        // Assert
        response.Content.Should().Be("Plain response.");
        response.Citations.Should().BeNull();
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static IEnumerable<SearchHit> CreateSearchHits(string content)
    {
        var metadata = new ChunkMetadata(Index: 0, Heading: "Test Section");
        var chunk = new TextChunk(content, StartOffset: 0, EndOffset: content.Length, metadata);

        var doc = new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: "/docs/source.md",
            Title: "Source Document",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

        return new[]
        {
            new SearchHit { Chunk = chunk, Document = doc, Score = 0.92f },
        };
    }
}
