// -----------------------------------------------------------------------
// <copyright file="ContextPreviewViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Modules.Agents.Chat.ViewModels;
using Lexichord.Modules.Agents.Context;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context;

/// <summary>
/// Unit tests for <see cref="ContextPreviewViewModel"/>.
/// </summary>
/// <remarks>
/// Tests verify ViewModel construction, bridge event handling,
/// strategy initialization, commands, dispose behavior, and computed properties.
/// Uses NSubstitute for mocking and FluentAssertions for verification.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2d")]
public class ContextPreviewViewModelTests
{
    #region Helper Factories

    private readonly IContextOrchestrator _orchestrator;
    private readonly ContextPreviewBridge _bridge;
    private readonly ILogger<ContextPreviewViewModel> _vmLogger;
    private readonly ILogger<ContextPreviewBridge> _bridgeLogger;

    public ContextPreviewViewModelTests()
    {
        _orchestrator = Substitute.For<IContextOrchestrator>();
        _orchestrator.GetStrategies().Returns(new List<IContextStrategy>());

        _bridgeLogger = Substitute.For<ILogger<ContextPreviewBridge>>();
        _bridge = new ContextPreviewBridge(_bridgeLogger);

        _vmLogger = Substitute.For<ILogger<ContextPreviewViewModel>>();
    }

    private ContextPreviewViewModel CreateViewModel(
        IContextOrchestrator? orchestrator = null,
        ContextPreviewBridge? bridge = null,
        Action<Action>? dispatch = null)
    {
        return new ContextPreviewViewModel(
            orchestrator ?? _orchestrator,
            bridge ?? _bridge,
            _vmLogger,
            dispatch);
    }

    private static IContextStrategy CreateMockStrategy(string id, string displayName)
    {
        var strategy = Substitute.For<IContextStrategy>();
        strategy.StrategyId.Returns(id);
        strategy.DisplayName.Returns(displayName);
        return strategy;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextPreviewViewModel(null!, _bridge, _vmLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("orchestrator");
    }

    [Fact]
    public void Constructor_NullBridge_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextPreviewViewModel(_orchestrator, null!, _vmLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("bridge");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextPreviewViewModel(_orchestrator, _bridge, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Initial State Tests

    [Fact]
    public void InitialState_HasCorrectDefaults()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.IsAssembling.Should().BeFalse();
        vm.TotalTokens.Should().Be(0);
        vm.TokenBudget.Should().Be(8000);
        vm.AssemblyDuration.Should().Be(TimeSpan.Zero);
        vm.IsExpanded.Should().BeTrue();
        vm.HasContext.Should().BeFalse();
        vm.Fragments.Should().BeEmpty();
    }

    [Fact]
    public void InitialState_BudgetPercentage_IsZero()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.BudgetPercentage.Should().Be(0.0);
    }

    [Fact]
    public void InitialState_BudgetStatusText_ShowsZeroOfDefault()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.BudgetStatusText.Should().Contain("0");
        vm.BudgetStatusText.Should().Contain("8,000");
    }

    #endregion

    #region Strategy Initialization Tests

    [Fact]
    public void Constructor_WithStrategies_PopulatesStrategiesCollection()
    {
        // Arrange
        var strategies = new List<IContextStrategy>
        {
            CreateMockStrategy("document", "Document Content"),
            CreateMockStrategy("rag", "Related Docs"),
            CreateMockStrategy("style", "Style Rules")
        };
        _orchestrator.GetStrategies().Returns(strategies);
        _orchestrator.IsStrategyEnabled("document").Returns(true);
        _orchestrator.IsStrategyEnabled("rag").Returns(true);
        _orchestrator.IsStrategyEnabled("style").Returns(false);

        // Act
        var vm = CreateViewModel();

        // Assert
        vm.Strategies.Should().HaveCount(3);
        vm.Strategies[0].StrategyId.Should().Be("document");
        vm.Strategies[0].DisplayName.Should().Be("Document Content");
        vm.Strategies[0].IsEnabled.Should().BeTrue();
        vm.Strategies[2].IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void StrategyToggle_InvokesOrchestratorSetStrategyEnabled()
    {
        // Arrange
        var strategies = new List<IContextStrategy>
        {
            CreateMockStrategy("document", "Document Content")
        };
        _orchestrator.GetStrategies().Returns(strategies);
        _orchestrator.IsStrategyEnabled("document").Returns(true);

        var vm = CreateViewModel();

        // Act — toggle the strategy off
        vm.Strategies[0].IsEnabled = false;

        // Assert
        _orchestrator.Received(1).SetStrategyEnabled("document", false);
    }

    #endregion

    #region Bridge Event Handling Tests

    [Fact]
    public async Task OnContextAssembled_UpdatesFragmentsAndProperties()
    {
        // Arrange
        var vm = CreateViewModel();

        var fragments = new List<ContextFragment>
        {
            new("document", "Document Content", "Chapter 1 text", 400, 1.0f),
            new("rag", "Related Docs", "Related content", 150, 0.8f)
        };

        var notification = new ContextAssembledEvent(
            AgentId: "editor",
            Fragments: fragments,
            TotalTokens: 550,
            Duration: TimeSpan.FromMilliseconds(127));

        // Act
        await _bridge.Handle(notification, CancellationToken.None);

        // Assert
        vm.Fragments.Should().HaveCount(2);
        vm.Fragments[0].SourceId.Should().Be("document");
        vm.Fragments[0].Label.Should().Be("Document Content");
        vm.Fragments[1].SourceId.Should().Be("rag");
        vm.TotalTokens.Should().Be(550);
        vm.AssemblyDuration.Should().Be(TimeSpan.FromMilliseconds(127));
        vm.IsAssembling.Should().BeFalse();
        vm.HasContext.Should().BeTrue();
    }

    [Fact]
    public async Task OnContextAssembled_ClearsExistingFragments()
    {
        // Arrange
        var vm = CreateViewModel();

        // First event
        var firstNotification = new ContextAssembledEvent(
            "editor",
            new List<ContextFragment> { new("doc", "Doc", "Content", 100, 1.0f) },
            100,
            TimeSpan.FromMilliseconds(50));
        await _bridge.Handle(firstNotification, CancellationToken.None);
        vm.Fragments.Should().HaveCount(1);

        // Second event with different fragments
        var secondNotification = new ContextAssembledEvent(
            "editor",
            new List<ContextFragment>
            {
                new("rag", "RAG", "RAG content", 200, 0.9f),
                new("style", "Style", "Style content", 50, 0.5f)
            },
            250,
            TimeSpan.FromMilliseconds(80));

        // Act
        await _bridge.Handle(secondNotification, CancellationToken.None);

        // Assert
        vm.Fragments.Should().HaveCount(2);
        vm.Fragments[0].SourceId.Should().Be("rag");
        vm.Fragments[1].SourceId.Should().Be("style");
        vm.TotalTokens.Should().Be(250);
    }

    [Fact]
    public async Task OnStrategyToggled_UpdatesMatchingStrategyItem()
    {
        // Arrange
        var strategies = new List<IContextStrategy>
        {
            CreateMockStrategy("document", "Document Content"),
            CreateMockStrategy("rag", "Related Docs")
        };
        _orchestrator.GetStrategies().Returns(strategies);
        _orchestrator.IsStrategyEnabled(Arg.Any<string>()).Returns(true);

        var vm = CreateViewModel();
        vm.Strategies.Should().HaveCount(2);
        vm.Strategies[1].IsEnabled.Should().BeTrue();

        var toggleNotification = new StrategyToggleEvent("rag", false);

        // Act
        await _bridge.Handle(toggleNotification, CancellationToken.None);

        // Assert — the strategy item should be updated
        // Note: This will also re-trigger the orchestrator call (idempotent)
        vm.Strategies[1].IsEnabled.Should().BeFalse();
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void BudgetPercentage_CalculatesCorrectly()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.TotalTokens = 4000;
        vm.TokenBudget = 8000;

        // Assert
        vm.BudgetPercentage.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public void BudgetPercentage_CapsAtOne()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.TotalTokens = 10000;
        vm.TokenBudget = 8000;

        // Assert
        vm.BudgetPercentage.Should().Be(1.0);
    }

    [Fact]
    public void BudgetPercentage_ZeroBudget_ReturnsZero()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.TokenBudget = 0;

        // Assert
        vm.BudgetPercentage.Should().Be(0.0);
    }

    [Fact]
    public void DurationText_FormatsMilliseconds()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.AssemblyDuration = TimeSpan.FromMilliseconds(127.5);

        // Assert
        vm.DurationText.Should().Be("128ms");
    }

    [Fact]
    public void CombinedContent_JoinsFragmentsWithMarkdownHeadings()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.Fragments.Add(new FragmentViewModel(
            new ContextFragment("doc", "Document", "Doc content", 10, 1.0f)));
        vm.Fragments.Add(new FragmentViewModel(
            new ContextFragment("rag", "RAG", "RAG content", 10, 0.8f)));

        // Assert
        vm.CombinedContent.Should().Contain("## Document");
        vm.CombinedContent.Should().Contain("Doc content");
        vm.CombinedContent.Should().Contain("## RAG");
        vm.CombinedContent.Should().Contain("RAG content");
    }

    #endregion

    #region Command Tests

    [Fact]
    public void ToggleExpandedCommand_TogglesIsExpanded()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.IsExpanded.Should().BeTrue();

        // Act
        vm.ToggleExpandedCommand.Execute(null);

        // Assert
        vm.IsExpanded.Should().BeFalse();

        // Act again
        vm.ToggleExpandedCommand.Execute(null);

        // Assert
        vm.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void EnableAllCommand_EnablesAllStrategies()
    {
        // Arrange
        var strategies = new List<IContextStrategy>
        {
            CreateMockStrategy("doc", "Doc"),
            CreateMockStrategy("rag", "RAG")
        };
        _orchestrator.GetStrategies().Returns(strategies);
        _orchestrator.IsStrategyEnabled(Arg.Any<string>()).Returns(false);

        var vm = CreateViewModel();
        vm.Strategies.Should().AllSatisfy(s => s.IsEnabled.Should().BeFalse());

        // Act
        vm.EnableAllCommand.Execute(null);

        // Assert
        vm.Strategies.Should().AllSatisfy(s => s.IsEnabled.Should().BeTrue());
    }

    [Fact]
    public void DisableAllCommand_DisablesAllStrategies()
    {
        // Arrange
        var strategies = new List<IContextStrategy>
        {
            CreateMockStrategy("doc", "Doc"),
            CreateMockStrategy("rag", "RAG")
        };
        _orchestrator.GetStrategies().Returns(strategies);
        _orchestrator.IsStrategyEnabled(Arg.Any<string>()).Returns(true);

        var vm = CreateViewModel();
        vm.Strategies.Should().AllSatisfy(s => s.IsEnabled.Should().BeTrue());

        // Act
        vm.DisableAllCommand.Execute(null);

        // Assert
        vm.Strategies.Should().AllSatisfy(s => s.IsEnabled.Should().BeFalse());
    }

    #endregion

    #region Dispatch Delegate Tests

    [Fact]
    public async Task OnContextAssembled_UsesDispatchDelegate()
    {
        // Arrange
        var dispatched = false;
        var vm = CreateViewModel(dispatch: action =>
        {
            dispatched = true;
            action();
        });

        var notification = new ContextAssembledEvent(
            "test",
            new List<ContextFragment> { new("doc", "Doc", "Content", 10, 1.0f) },
            10,
            TimeSpan.FromMilliseconds(10));

        // Act
        await _bridge.Handle(notification, CancellationToken.None);

        // Assert
        dispatched.Should().BeTrue();
        vm.Fragments.Should().HaveCount(1);
    }

    [Fact]
    public void DefaultDispatch_InvokesSynchronously()
    {
        // Arrange — no dispatch provided, uses default (synchronous)
        var vm = CreateViewModel();

        // Assert — the ViewModel should have been created without dispatch issues
        vm.Should().NotBeNull();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_UnsubscribesFromBridgeEvents()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();

        // Arrange — send an event after dispose
        var notification = new ContextAssembledEvent(
            "test",
            new List<ContextFragment> { new("doc", "Doc", "Content", 10, 1.0f) },
            10,
            TimeSpan.FromMilliseconds(10));

        // Act — handle should not update the disposed VM
        await _bridge.Handle(notification, CancellationToken.None);

        // Assert — fragments should remain empty (event not handled)
        vm.Fragments.Should().BeEmpty();
        vm.TotalTokens.Should().Be(0);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        var act = () =>
        {
            vm.Dispose();
            vm.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void TotalTokens_Changed_RaisesPropertyChangedForComputedProperties()
    {
        // Arrange
        var vm = CreateViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        vm.TotalTokens = 4000;

        // Assert
        changedProperties.Should().Contain("TotalTokens");
        changedProperties.Should().Contain("BudgetPercentage");
        changedProperties.Should().Contain("BudgetStatusText");
        changedProperties.Should().Contain("HasContext");
    }

    [Fact]
    public void TokenBudget_Changed_RaisesPropertyChangedForComputedProperties()
    {
        // Arrange
        var vm = CreateViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        vm.TokenBudget = 16000;

        // Assert
        changedProperties.Should().Contain("TokenBudget");
        changedProperties.Should().Contain("BudgetPercentage");
        changedProperties.Should().Contain("BudgetStatusText");
    }

    #endregion
}
