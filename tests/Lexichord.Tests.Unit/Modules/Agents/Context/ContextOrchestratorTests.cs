// -----------------------------------------------------------------------
// <copyright file="ContextOrchestratorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
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
/// Unit tests for <see cref="ContextOrchestrator"/>.
/// </summary>
/// <remarks>
/// Tests verify the orchestrator's assembly pipeline including strategy execution,
/// deduplication, priority sorting, budget trimming, event publishing, and error handling.
/// Uses NSubstitute for mocking and FluentAssertions for verification.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2c")]
public class ContextOrchestratorTests
{
    #region Helper Factories

    private readonly ITokenCounter _tokenCounter;
    private readonly IMediator _mediator;
    private readonly ILogger<ContextOrchestrator> _logger;

    public ContextOrchestratorTests()
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

    private static IContextStrategyFactory CreateMockFactory(params IContextStrategy[] strategies)
    {
        var factory = Substitute.For<IContextStrategyFactory>();
        factory.CreateAllStrategies().Returns(strategies.ToList());
        return factory;
    }

    private ContextOrchestrator CreateOrchestrator(
        IContextStrategyFactory? factory = null,
        AgentContextOptions? options = null)
    {
        factory ??= CreateMockFactory();
        var opts = Options.Create(options ?? new AgentContextOptions());

        return new ContextOrchestrator(factory, _tokenCounter, _mediator, opts, _logger);
    }

    private static ContextGatheringRequest CreateRequest(
        string agentId = "test-agent",
        string? documentPath = "/docs/test.md",
        int? cursorPosition = 100,
        string? selectedText = null)
    {
        return new ContextGatheringRequest(documentPath, cursorPosition, selectedText, agentId, null);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws when factory is null.
    /// </summary>
    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextOrchestrator(
            null!,
            _tokenCounter,
            _mediator,
            Options.Create(new AgentContextOptions()),
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    /// <summary>
    /// Verifies that constructor throws when tokenCounter is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTokenCounter_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextOrchestrator(
            CreateMockFactory(),
            null!,
            _mediator,
            Options.Create(new AgentContextOptions()),
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("tokenCounter");
    }

    /// <summary>
    /// Verifies that constructor throws when mediator is null.
    /// </summary>
    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextOrchestrator(
            CreateMockFactory(),
            _tokenCounter,
            null!,
            Options.Create(new AgentContextOptions()),
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("mediator");
    }

    /// <summary>
    /// Verifies that constructor throws when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextOrchestrator(
            CreateMockFactory(),
            _tokenCounter,
            _mediator,
            null!,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    /// <summary>
    /// Verifies that constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextOrchestrator(
            CreateMockFactory(),
            _tokenCounter,
            _mediator,
            Options.Create(new AgentContextOptions()),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region AssembleAsync Tests

    /// <summary>
    /// Verifies that AssembleAsync combines results from multiple strategies.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithMultipleStrategies_CombinesResults()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content here", 100);
        var selStrategy = CreateMockStrategy("selection", "Selected text here", 50);
        var factory = CreateMockFactory(docStrategy, selStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert
        result.HasContext.Should().BeTrue();
        result.Fragments.Should().HaveCount(2);
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.Fragments.Should().Contain(f => f.SourceId == "selection");
    }

    /// <summary>
    /// Verifies that AssembleAsync trims fragments when budget is exceeded.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_ExceedingBudget_TrimsFragments()
    {
        // Arrange — two strategies, each producing 5000 tokens, but budget is 6000
        var docStrategy = CreateMockStrategy("document", "Document content here", 5000);
        var selStrategy = CreateMockStrategy("selection", "Selected text here", 5000);
        var factory = CreateMockFactory(docStrategy, selStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(
            CreateRequest(),
            ContextBudget.WithLimit(6000),
            CancellationToken.None);

        // Assert — only the higher-priority fragment should fit
        result.TotalTokens.Should().BeLessOrEqualTo(6000);
    }

    /// <summary>
    /// Verifies that AssembleAsync deduplicates fragments with similar content.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithDuplicateContent_Deduplicates()
    {
        // Arrange — same content from two different strategies
        var content = "The quick brown fox jumps over the lazy dog and runs away fast";
        var docStrategy = CreateMockStrategy("document", content, 50, 1.0f);
        var selStrategy = CreateMockStrategy("selection", content, 50, 0.9f);
        var factory = CreateMockFactory(docStrategy, selStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — only one fragment should survive deduplication
        result.Fragments.Should().HaveCount(1);
        result.Fragments[0].SourceId.Should().Be("document");
    }

    /// <summary>
    /// Verifies that AssembleAsync sorts fragments by orchestrator priority.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_SortsFragmentsByPriority()
    {
        // Arrange — style has lower orchestrator priority than document
        var styleStrategy = CreateMockStrategy("style", "Style rules content here", 50, 0.9f);
        var docStrategy = CreateMockStrategy("document", "Document content here now", 50, 0.5f);
        var factory = CreateMockFactory(styleStrategy, docStrategy);
        var sut = CreateOrchestrator(factory, new AgentContextOptions { EnableDeduplication = false });

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — document (priority 100) should be before style (priority 50)
        result.Fragments.Should().HaveCount(2);
        result.Fragments[0].SourceId.Should().Be("document");
        result.Fragments[1].SourceId.Should().Be("style");
    }

    /// <summary>
    /// Verifies that a slow strategy is timed out and excluded.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithSlowStrategy_TimesOut()
    {
        // Arrange — strategy that takes too long
        var slowStrategy = Substitute.For<IContextStrategy>();
        slowStrategy.StrategyId.Returns("slow");
        slowStrategy.DisplayName.Returns("Slow Strategy");
        slowStrategy.GatherAsync(Arg.Any<ContextGatheringRequest>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var ct = ci.ArgAt<CancellationToken>(1);
                await Task.Delay(5000, ct); // Will be cancelled
                return (ContextFragment?)null;
            });

        var fastStrategy = CreateMockStrategy("document", "Fast content here now", 50);
        var factory = CreateMockFactory(slowStrategy, fastStrategy);

        // Short timeout to trigger cancellation
        var sut = CreateOrchestrator(factory, new AgentContextOptions { StrategyTimeout = 100 });

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — only the fast strategy should have contributed
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.Fragments.Should().NotContain(f => f.SourceId == "slow");
    }

    /// <summary>
    /// Verifies that disabled strategies are excluded from assembly.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithDisabledStrategy_ExcludesIt()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content here", 50);
        var ragStrategy = CreateMockStrategy("rag", "RAG search results here", 50);
        var factory = CreateMockFactory(docStrategy, ragStrategy);
        var sut = CreateOrchestrator(factory);

        // Disable RAG strategy
        sut.SetStrategyEnabled("rag", false);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.Fragments.Should().NotContain(f => f.SourceId == "rag");
    }

    /// <summary>
    /// Verifies that budget-excluded strategies are skipped.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithExcludedStrategy_SkipsIt()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content here", 50);
        var ragStrategy = CreateMockStrategy("rag", "RAG search results here", 50);
        var factory = CreateMockFactory(docStrategy, ragStrategy);
        var sut = CreateOrchestrator(factory);

        var budget = new ContextBudget(8000, null, new[] { "rag" });

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), budget, CancellationToken.None);

        // Assert
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.Fragments.Should().NotContain(f => f.SourceId == "rag");
    }

    /// <summary>
    /// Verifies that a ContextAssembledEvent is published after assembly.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_PublishesContextAssembledEvent()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content here", 50);
        var factory = CreateMockFactory(docStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<ContextAssembledEvent>(e =>
                e.AgentId == "test-agent" &&
                e.Fragments.Count == 1 &&
                e.TotalTokens > 0),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that AssembleAsync extracts template variables from request.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_ExtractsVariables()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content here", 50);
        var factory = CreateMockFactory(docStrategy);
        var sut = CreateOrchestrator(factory);

        var request = CreateRequest(
            documentPath: "/docs/chapter1.md",
            cursorPosition: 42,
            selectedText: "selected portion");

        // Act
        var result = await sut.AssembleAsync(request, ContextBudget.Default, CancellationToken.None);

        // Assert
        result.Variables.Should().ContainKey("DocumentName");
        result.Variables["DocumentName"].Should().Be("chapter1.md");
        result.Variables.Should().ContainKey("DocumentPath");
        result.Variables["DocumentPath"].Should().Be("/docs/chapter1.md");
        result.Variables.Should().ContainKey("CursorPosition");
        result.Variables["CursorPosition"].Should().Be(42);
        result.Variables.Should().ContainKey("SelectionLength");
        result.Variables["SelectionLength"].Should().Be(16);
        result.Variables.Should().ContainKey("FragmentCount");
        result.Variables.Should().ContainKey("TotalTokens");
    }

    /// <summary>
    /// Verifies that AssembleAsync returns Empty when no strategies are available.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_WithNoStrategies_ReturnsEmpty()
    {
        // Arrange
        var factory = CreateMockFactory(); // No strategies
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert
        result.HasContext.Should().BeFalse();
        result.Fragments.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a failing strategy does not crash the orchestrator.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_FailingStrategy_DoesNotCrashOrchestrator()
    {
        // Arrange
        var failingStrategy = Substitute.For<IContextStrategy>();
        failingStrategy.StrategyId.Returns("failing");
        failingStrategy.DisplayName.Returns("Failing Strategy");
        failingStrategy.GatherAsync(Arg.Any<ContextGatheringRequest>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Strategy broke"));

        var goodStrategy = CreateMockStrategy("document", "Good content here now", 50);
        var factory = CreateMockFactory(failingStrategy, goodStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — good strategy should still produce results
        result.Fragments.Should().Contain(f => f.SourceId == "document");
        result.Fragments.Should().NotContain(f => f.SourceId == "failing");
    }

    /// <summary>
    /// Verifies that assembly records non-zero duration.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_RecordsDuration()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Document content here", 50);
        var factory = CreateMockFactory(docStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert
        result.AssemblyDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that deduplication is skipped when disabled.
    /// </summary>
    [Fact]
    public async Task AssembleAsync_DeduplicationDisabled_KeepsAllFragments()
    {
        // Arrange — same content from two different strategies
        var content = "The quick brown fox jumps over the lazy dog and runs away fast";
        var docStrategy = CreateMockStrategy("document", content, 50, 1.0f);
        var selStrategy = CreateMockStrategy("selection", content, 50, 0.9f);
        var factory = CreateMockFactory(docStrategy, selStrategy);
        var sut = CreateOrchestrator(factory, new AgentContextOptions { EnableDeduplication = false });

        // Act
        var result = await sut.AssembleAsync(CreateRequest(), ContextBudget.Default, CancellationToken.None);

        // Assert — both fragments should be kept
        result.Fragments.Should().HaveCount(2);
    }

    #endregion

    #region IsStrategyEnabled / SetStrategyEnabled Tests

    /// <summary>
    /// Verifies that strategies are enabled by default.
    /// </summary>
    [Fact]
    public void IsStrategyEnabled_DefaultsToTrue()
    {
        // Arrange
        var sut = CreateOrchestrator();

        // Act & Assert
        sut.IsStrategyEnabled("document").Should().BeTrue();
        sut.IsStrategyEnabled("rag").Should().BeTrue();
        sut.IsStrategyEnabled("unknown").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that SetStrategyEnabled updates the enabled state.
    /// </summary>
    [Fact]
    public void SetStrategyEnabled_UpdatesState()
    {
        // Arrange
        var sut = CreateOrchestrator();

        // Act
        sut.SetStrategyEnabled("rag", false);

        // Assert
        sut.IsStrategyEnabled("rag").Should().BeFalse();

        // Act — re-enable
        sut.SetStrategyEnabled("rag", true);

        // Assert
        sut.IsStrategyEnabled("rag").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that SetStrategyEnabled publishes a StrategyToggleEvent.
    /// </summary>
    [Fact]
    public void SetStrategyEnabled_PublishesToggleEvent()
    {
        // Arrange
        var sut = CreateOrchestrator();

        // Act
        sut.SetStrategyEnabled("rag", false);

        // Assert
        _mediator.Received(1).Publish(
            Arg.Is<StrategyToggleEvent>(e =>
                e.StrategyId == "rag" &&
                e.IsEnabled == false),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetStrategies Tests

    /// <summary>
    /// Verifies that GetStrategies delegates to the factory.
    /// </summary>
    [Fact]
    public void GetStrategies_DelegatesToFactory()
    {
        // Arrange
        var docStrategy = CreateMockStrategy("document", "Content", 50);
        var factory = CreateMockFactory(docStrategy);
        var sut = CreateOrchestrator(factory);

        // Act
        var result = sut.GetStrategies();

        // Assert
        result.Should().HaveCount(1);
        result[0].StrategyId.Should().Be("document");
        factory.Received(1).CreateAllStrategies();
    }

    #endregion
}
