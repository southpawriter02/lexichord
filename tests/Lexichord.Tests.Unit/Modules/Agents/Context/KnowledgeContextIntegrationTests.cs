// -----------------------------------------------------------------------
// <copyright file="KnowledgeContextIntegrationTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Chat.ViewModels;
using Lexichord.Modules.Agents.Context;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using AgentContextOptions = Lexichord.Modules.Agents.Context.ContextOptions;

namespace Lexichord.Tests.Unit.Modules.Agents.Context;

/// <summary>
/// Integration tests verifying the knowledge context strategy integrates correctly
/// with the Context Assembler pipeline — orchestrator priority mapping, fragment
/// ViewModel display, strategy toggle behavior, and end-to-end assembly.
/// </summary>
/// <remarks>
/// <para>
/// These tests exercise the integration points added in v0.7.2h:
/// </para>
/// <list type="bullet">
///   <item><description>Orchestrator priority mapping: knowledge at priority 30</description></item>
///   <item><description>FragmentViewModel: brain emoji (🧠) for knowledge source</description></item>
///   <item><description>StrategyToggleItem: descriptive tooltip for knowledge</description></item>
///   <item><description>Pipeline behavior: inclusion, exclusion, budget trimming, deduplication</description></item>
///   <item><description>Event publishing: ContextAssembledEvent includes knowledge fragments</description></item>
/// </list>
/// <para>
/// <strong>Test Framework:</strong> xUnit + NSubstitute + FluentAssertions.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2h as part of the Context Assembler Integration.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2h")]
public class KnowledgeContextIntegrationTests
{
    #region Helper Factories

    /// <summary>
    /// Shared mock token counter using char/4 heuristic for consistent token estimation.
    /// </summary>
    private readonly ITokenCounter _tokenCounter;

    /// <summary>
    /// Shared mock MediatR mediator for verifying event publishing.
    /// </summary>
    private readonly IMediator _mediator;

    /// <summary>
    /// Shared mock logger for the ContextOrchestrator.
    /// </summary>
    private readonly ILogger<ContextOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeContextIntegrationTests"/> class.
    /// Sets up shared mock dependencies used across all test methods.
    /// </summary>
    public KnowledgeContextIntegrationTests()
    {
        _tokenCounter = Substitute.For<ITokenCounter>();
        _tokenCounter.CountTokens(Arg.Any<string>()).Returns(ci =>
        {
            var text = ci.Arg<string>();
            return string.IsNullOrEmpty(text) ? 0 : text.Length / 4;
        });
        _tokenCounter.TruncateToTokenLimit(Arg.Any<string>(), Arg.Any<int>()).Returns(ci =>
        {
            var text = ci.ArgAt<string>(0);
            var maxTokens = ci.ArgAt<int>(1);
            var maxChars = maxTokens * 4;
            if (text.Length <= maxChars) return (text, false);
            return (text[..maxChars], true);
        });

        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<ContextOrchestrator>>();
    }

    /// <summary>
    /// Creates a mock <see cref="IContextStrategy"/> that returns a fragment
    /// with the specified source ID, content, token estimate, and relevance.
    /// </summary>
    /// <param name="id">Strategy source ID (e.g., "knowledge", "document").</param>
    /// <param name="content">Fragment content text.</param>
    /// <param name="tokens">Estimated token count for the fragment.</param>
    /// <param name="relevance">Relevance score between 0.0 and 1.0.</param>
    /// <returns>A configured mock <see cref="IContextStrategy"/>.</returns>
    private static IContextStrategy CreateMockStrategy(
        string id,
        string content,
        int tokens,
        float relevance = 0.8f)
    {
        var strategy = Substitute.For<IContextStrategy>();
        strategy.StrategyId.Returns(id);
        strategy.DisplayName.Returns($"{id} strategy");
        strategy.Priority.Returns(StrategyPriority.Medium);

        var fragment = new ContextFragment(id, $"{id} content", content, tokens, relevance);
        strategy.GatherAsync(Arg.Any<ContextGatheringRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ContextFragment?>(fragment));

        return strategy;
    }

    /// <summary>
    /// Creates a mock <see cref="IContextStrategyFactory"/> that returns
    /// the specified strategies from <see cref="IContextStrategyFactory.CreateAllStrategies"/>.
    /// </summary>
    /// <param name="strategies">Strategies to include in the factory.</param>
    /// <returns>A configured mock <see cref="IContextStrategyFactory"/>.</returns>
    private static IContextStrategyFactory CreateMockFactory(params IContextStrategy[] strategies)
    {
        var factory = Substitute.For<IContextStrategyFactory>();
        factory.CreateAllStrategies().Returns(strategies.ToList());
        return factory;
    }

    /// <summary>
    /// Creates a <see cref="ContextOrchestrator"/> instance with the shared mock
    /// dependencies and optional overrides for factory and options.
    /// </summary>
    /// <param name="factory">Optional factory override. Defaults to an empty factory.</param>
    /// <param name="options">Optional context options override. Defaults to default options.</param>
    /// <returns>A configured <see cref="ContextOrchestrator"/> instance.</returns>
    private ContextOrchestrator CreateOrchestrator(
        IContextStrategyFactory? factory = null,
        AgentContextOptions? options = null)
    {
        factory ??= CreateMockFactory();
        var opts = Options.Create(options ?? new AgentContextOptions());

        return new ContextOrchestrator(factory, _tokenCounter, _mediator, opts, _logger);
    }

    /// <summary>
    /// Creates a <see cref="ContextGatheringRequest"/> with default test values.
    /// </summary>
    /// <param name="agentId">Agent identifier. Defaults to "test-agent".</param>
    /// <param name="documentPath">Document path. Defaults to "/docs/test.md".</param>
    /// <param name="cursorPosition">Cursor position. Defaults to 100.</param>
    /// <param name="selectedText">Selected text. Defaults to null.</param>
    /// <returns>A configured <see cref="ContextGatheringRequest"/>.</returns>
    private static ContextGatheringRequest CreateRequest(
        string agentId = "test-agent",
        string? documentPath = "/docs/test.md",
        int? cursorPosition = 100,
        string? selectedText = null)
    {
        return new ContextGatheringRequest(documentPath, cursorPosition, selectedText, agentId, null);
    }

    #endregion

    #region Priority Mapping Tests

    /// <summary>
    /// Verifies that the knowledge strategy is sorted at priority 30, which is lower
    /// than style (50) and RAG (60), ensuring it is trimmed before those strategies.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_KnowledgeSortedBelowStyleAndRag()
    {
        // Arrange — three strategies with distinct content to avoid deduplication
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entity data here", 50, 0.9f);
        var styleStrategy = CreateMockStrategy("style", "Style rules and formatting preferences", 50, 0.9f);
        var ragStrategy = CreateMockStrategy("rag", "Semantically related documentation results", 50, 0.9f);
        var factory = CreateMockFactory(knowledgeStrategy, styleStrategy, ragStrategy);
        var sut = CreateOrchestrator(factory, new AgentContextOptions { EnableDeduplication = false });

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — sorted by orchestrator priority: rag (60) > style (50) > knowledge (30)
        result.Fragments.Should().HaveCount(3);
        result.Fragments[0].SourceId.Should().Be("rag");
        result.Fragments[1].SourceId.Should().Be("style");
        result.Fragments[2].SourceId.Should().Be("knowledge");
    }

    /// <summary>
    /// Verifies that knowledge (priority 30) is sorted below document (priority 100)
    /// and selection (priority 80) in the assembly output.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_KnowledgeSortedBelowDocumentAndSelection()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Full document content for editing", 50, 0.9f);
        var selStrategy = CreateMockStrategy("selection", "Selected text from the editor now", 50, 0.9f);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entity information", 50, 0.9f);
        var factory = CreateMockFactory(knowledgeStrategy, docStrategy, selStrategy);
        var sut = CreateOrchestrator(factory, new AgentContextOptions { EnableDeduplication = false });

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — document (100) > selection (80) > knowledge (30)
        result.Fragments.Should().HaveCount(3);
        result.Fragments[0].SourceId.Should().Be("document");
        result.Fragments[1].SourceId.Should().Be("selection");
        result.Fragments[2].SourceId.Should().Be("knowledge");
    }

    /// <summary>
    /// Verifies that when the token budget is tight, knowledge is trimmed first
    /// because it has the lowest orchestrator priority (30) among built-in strategies.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_TightBudget_KnowledgeTrimmedBeforeStyle()
    {
        // Arrange — three strategies, each producing 3000 tokens, but budget is only 6000
        // Priority order: style (50) > knowledge (30), so knowledge should be trimmed first
        var styleStrategy = CreateMockStrategy("style", "Style rules for the document now", 3000, 0.9f);
        var ragStrategy = CreateMockStrategy("rag", "RAG search results for this query", 3000, 0.9f);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge entity data from graph", 3000, 0.9f);
        var factory = CreateMockFactory(styleStrategy, ragStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory, new AgentContextOptions { EnableDeduplication = false });

        // Act — budget allows only 2 of the 3 strategies (6000 / 3000 = 2)
        var result = await sut.AssembleAsync(
            CreateRequest(),
            ContextBudget.WithLimit(6000),
            CancellationToken.None);

        // Assert — knowledge (priority 30) should be trimmed; rag (60) and style (50) should survive
        result.Fragments.Should().Contain(f => f.SourceId == "rag");
        result.Fragments.Should().Contain(f => f.SourceId == "style");
        result.TotalTokens.Should().BeLessOrEqualTo(6000);
    }

    #endregion

    #region Fragment ViewModel Integration Tests

    /// <summary>
    /// Verifies that a knowledge context fragment is displayed with the brain emoji (🧠)
    /// in the <see cref="FragmentViewModel"/>.
    /// </summary>
    [Fact]
    public void FragmentViewModel_KnowledgeSource_DisplaysBrainIcon()
    {
        // Arrange
        var fragment = new ContextFragment("knowledge", "Knowledge Graph", "Entity data", 100, 0.75f);

        // Act
        var vm = new FragmentViewModel(fragment);

        // Assert
        vm.SourceIcon.Should().Be("🧠");
        vm.SourceId.Should().Be("knowledge");
    }

    /// <summary>
    /// Verifies that a knowledge fragment ViewModel displays correct metadata
    /// including label, token count text, and relevance text.
    /// </summary>
    [Fact]
    public void FragmentViewModel_KnowledgeSource_DisplaysCorrectMetadata()
    {
        // Arrange
        var fragment = new ContextFragment(
            "knowledge",
            "Knowledge Graph Entities",
            "Character (Person) - John Doe: A novelist who lives in the countryside.",
            250,
            0.85f);

        // Act
        var vm = new FragmentViewModel(fragment);

        // Assert
        vm.Label.Should().Be("Knowledge Graph Entities");
        vm.FullContent.Should().Contain("John Doe");
        vm.TokenCount.Should().Be(250);
        vm.TokenCountText.Should().Be("250 tokens");
        vm.RelevanceText.Should().Contain("85");
    }

    #endregion

    #region Strategy Toggle Integration Tests

    /// <summary>
    /// Verifies that a <see cref="StrategyToggleItem"/> for the knowledge strategy
    /// returns a tooltip containing "knowledge graph".
    /// </summary>
    [Fact]
    public void StrategyToggleItem_KnowledgeStrategy_ReturnsDescriptiveTooltip()
    {
        // Arrange & Act
        var item = new StrategyToggleItem("knowledge", "Knowledge Graph", true, (_, _) => { });

        // Assert
        item.Tooltip.Should().ContainEquivalentOf("knowledge graph");
        item.StrategyId.Should().Be("knowledge");
        item.DisplayName.Should().Be("Knowledge Graph");
    }

    /// <summary>
    /// Verifies that toggling a knowledge strategy toggle item invokes the callback
    /// with the correct strategy ID and enabled state.
    /// </summary>
    [Fact]
    public void StrategyToggleItem_KnowledgeToggle_InvokesCallback()
    {
        // Arrange
        string? toggledId = null;
        bool? toggledEnabled = null;
        var item = new StrategyToggleItem("knowledge", "Knowledge Graph", true,
            (id, enabled) =>
            {
                toggledId = id;
                toggledEnabled = enabled;
            });

        // Act
        item.IsEnabled = false;

        // Assert
        toggledId.Should().Be("knowledge");
        toggledEnabled.Should().BeFalse();
    }

    #endregion

    #region Orchestrator Pipeline Integration Tests

    /// <summary>
    /// Verifies that a knowledge strategy fragment is included in the assembled context
    /// when combined with other strategies in the full pipeline.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithKnowledge_IncludesKnowledgeFragment()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content text for editing", 100);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entities data", 100);
        var factory = CreateMockFactory(docStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert
        result.HasContext.Should().BeTrue();
        result.Fragments.Should().Contain(f => f.SourceId == "knowledge");
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.HasFragmentFrom("knowledge").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that when the knowledge strategy is disabled via
    /// <see cref="IContextOrchestrator.SetStrategyEnabled"/>, it is excluded
    /// from the assembled context.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_KnowledgeDisabled_ExcludesKnowledgeFragment()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content text for editing", 100);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entities data", 100);
        var factory = CreateMockFactory(docStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory);

        // Disable knowledge strategy
        sut.SetStrategyEnabled("knowledge", false);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.Fragments.Should().NotContain(f => f.SourceId == "knowledge");
        result.HasFragmentFrom("knowledge").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that when the knowledge strategy is excluded via
    /// <see cref="ContextBudget.ExcludedStrategies"/>, it is not executed.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_KnowledgeExcludedByBudget_SkipsKnowledgeStrategy()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content text for editing", 100);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entities data", 100);
        var factory = CreateMockFactory(docStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory);

        var budget = new ContextBudget(8000, null, new[] { "knowledge" });

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), budget, CancellationToken.None);

        // Assert
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.Fragments.Should().NotContain(f => f.SourceId == "knowledge");
    }

    /// <summary>
    /// Verifies that knowledge combines correctly with all other built-in strategies
    /// in a full pipeline assembly without errors.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_AllStrategiesIncludingKnowledge_CombinesCorrectly()
    {
        // Arrange — simulate all 7 built-in strategies
        var strategies = new[]
        {
            CreateMockStrategy("document", "Full document content text here", 500, 1.0f),
            CreateMockStrategy("selection", "Selected paragraph text in editor", 200, 0.95f),
            CreateMockStrategy("cursor", "Text surrounding current cursor pos", 150, 0.9f),
            CreateMockStrategy("heading", "Document heading hierarchy outline", 100, 0.85f),
            CreateMockStrategy("surrounding-text", "Surrounding paragraph context", 250, 0.88f),
            CreateMockStrategy("terminology", "Matching terminology from database", 150, 0.82f),
            CreateMockStrategy("rag", "Semantically related documentation now", 300, 0.8f),
            CreateMockStrategy("style", "Active style rules for this document", 100, 0.75f),
            CreateMockStrategy("knowledge", "Knowledge graph entities and relations", 200, 0.7f),
        };
        var factory = CreateMockFactory(strategies);
        var sut = CreateOrchestrator(factory, new AgentContextOptions { EnableDeduplication = false });

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — all 9 strategies should contribute fragments (v0.7.3c added surrounding-text, terminology)
        result.Fragments.Should().HaveCount(9);
        result.HasFragmentFrom("knowledge").Should().BeTrue();
        result.HasFragmentFrom("surrounding-text").Should().BeTrue();
        result.HasFragmentFrom("terminology").Should().BeTrue();

        // Assert — knowledge should be last (lowest priority at 30)
        result.Fragments[^1].SourceId.Should().Be("knowledge");
    }

    /// <summary>
    /// Verifies that when the knowledge strategy throws an exception,
    /// the orchestrator handles it gracefully and still returns results
    /// from other strategies.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_KnowledgeStrategyFails_OtherStrategiesSucceed()
    {
        // Arrange — knowledge strategy that throws
        var failingKnowledge = Substitute.For<IContextStrategy>();
        failingKnowledge.StrategyId.Returns("knowledge");
        failingKnowledge.DisplayName.Returns("Knowledge Graph");
        failingKnowledge.GatherAsync(Arg.Any<ContextGatheringRequest>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Knowledge provider unavailable"));

        var docStrategy = CreateMockStrategy("document", "Document content text for editing", 100);
        var factory = CreateMockFactory(failingKnowledge, docStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — document should succeed; knowledge should be absent
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.Fragments.Should().NotContain(f => f.SourceId == "knowledge");
    }

    #endregion

    #region Deduplication Tests

    /// <summary>
    /// Verifies that when knowledge and RAG produce near-identical content,
    /// deduplication retains the higher-relevance fragment.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_KnowledgeAndRagDuplicate_HigherRelevanceKept()
    {
        // Arrange — same content from knowledge and RAG, different relevance scores
        var duplicateContent = "Character John Doe is a novelist who lives in the countryside near the lake";
        var ragStrategy = CreateMockStrategy("rag", duplicateContent, 50, 0.9f);
        var knowledgeStrategy = CreateMockStrategy("knowledge", duplicateContent, 50, 0.7f);
        var factory = CreateMockFactory(ragStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — only the higher-relevance fragment (RAG at 0.9) should survive
        result.Fragments.Should().HaveCount(1);
        result.Fragments[0].SourceId.Should().Be("rag");
    }

    #endregion

    #region Event Publishing Tests

    /// <summary>
    /// Verifies that the <see cref="ContextAssembledEvent"/> published by the orchestrator
    /// includes the knowledge fragment when the knowledge strategy contributes.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithKnowledge_PublishesEventWithKnowledgeFragment()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content text for editing", 100);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entities data", 200);
        var factory = CreateMockFactory(docStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — event should include both fragments and correct total tokens
        await _mediator.Received(1).Publish(
            Arg.Is<ContextAssembledEvent>(e =>
                e.AgentId == "test-agent" &&
                e.Fragments.Any(f => f.SourceId == "knowledge") &&
                e.Fragments.Any(f => f.SourceId == "document") &&
                e.TotalTokens == 300),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Variable Extraction Tests

    /// <summary>
    /// Verifies that the FragmentCount and TotalTokens template variables
    /// correctly reflect the inclusion of a knowledge fragment.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithKnowledge_VariablesReflectKnowledgeFragment()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content text for editing", 100);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entities data", 200);
        var factory = CreateMockFactory(docStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — variables should account for both fragments
        result.Variables.Should().ContainKey("FragmentCount");
        result.Variables["FragmentCount"].Should().Be(2);
        result.Variables.Should().ContainKey("TotalTokens");
        result.Variables["TotalTokens"].Should().Be(300);
    }

    #endregion

    #region Combined Content Tests

    /// <summary>
    /// Verifies that <see cref="AssembledContext.GetCombinedContent"/> includes
    /// the knowledge fragment content as a labeled section.
    /// </summary>
    [Fact]
    public async Task GetCombinedContent_WithKnowledge_IncludesKnowledgeSection()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content text for editing", 100);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entities data", 100);
        var factory = CreateMockFactory(docStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);
        var combined = result.GetCombinedContent();

        // Assert — combined content should include both labeled sections
        combined.Should().Contain("## document content");
        combined.Should().Contain("## knowledge content");
        combined.Should().Contain("Knowledge graph entities data");
    }

    #endregion

    #region Re-enable Tests

    /// <summary>
    /// Verifies that a knowledge strategy can be re-enabled after being disabled,
    /// and its fragment is then included in subsequent assembly results.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_KnowledgeReEnabled_IncludesKnowledgeFragment()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content text for editing", 100);
        var knowledgeStrategy = CreateMockStrategy("knowledge", "Knowledge graph entities data", 100);
        var factory = CreateMockFactory(docStrategy, knowledgeStrategy);
        var sut = CreateOrchestrator(factory);

        // Act — disable, then re-enable
        sut.SetStrategyEnabled("knowledge", false);
        sut.SetStrategyEnabled("knowledge", true);
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — knowledge should be included after re-enabling
        result.HasFragmentFrom("knowledge").Should().BeTrue();
    }

    #endregion
}
