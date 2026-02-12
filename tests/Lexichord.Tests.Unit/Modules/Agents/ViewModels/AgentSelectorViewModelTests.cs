// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Events;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.ViewModels;

/// <summary>
/// Unit tests for <see cref="AgentSelectorViewModel"/> (v0.7.1d).
/// </summary>
/// <remarks>
/// <para>
/// Validates the Agent Selector UI behavior for:
/// </para>
/// <list type="bullet">
///   <item><description>Agent loading and initialization</description></item>
///   <item><description>Favorites persistence and management</description></item>
///   <item><description>Recent agents tracking</description></item>
///   <item><description>Agent and persona selection</description></item>
///   <item><description>Search filtering</description></item>
///   <item><description>MediatR event handling</description></item>
///   <item><description>License enforcement</description></item>
/// </list>
/// <para>
/// <strong>Spec reference:</strong> LCS-DES-v0.7.1d §4-8
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.1d")]
public class AgentSelectorViewModelTests
{
    private readonly Mock<IAgentRegistry> _registryMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<AgentSelectorViewModel>> _loggerMock;

    /// <summary>
    /// Initializes test fixtures with default mocks.
    /// </summary>
    /// <remarks>
    /// Default setup: Teams license, no favorites/recents, 3 test agents available.
    /// </remarks>
    public AgentSelectorViewModelTests()
    {
        _registryMock = new Mock<IAgentRegistry>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<AgentSelectorViewModel>>();

        // Default: Teams license available
        _licenseContextMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.Teams);

        // Default: No favorites or recents
        _settingsServiceMock.Setup(s => s.GetAsync<List<string>>(
                "agent.favorites", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _settingsServiceMock.Setup(s => s.GetAsync<List<string>>(
                "agent.recent", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _settingsServiceMock.Setup(s => s.GetAsync<string?>(
                "agent.last_used", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _settingsServiceMock.Setup(s => s.GetAsync<string?>(
                "agent.last_persona", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Default: SetAsync always succeeds
        _settingsServiceMock.Setup(s => s.SetAsync(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Default: 3 test agents available
        var agents = new List<AgentConfiguration>
        {
            CreateTestConfiguration("general-chat", LicenseTier.Core),
            CreateTestConfiguration("editor", LicenseTier.WriterPro),
            CreateTestConfiguration("researcher", LicenseTier.Teams)
        };

        _registryMock.Setup(r => r.AvailableAgents)
            .Returns(agents);

        _registryMock.Setup(r => r.GetConfiguration(It.IsAny<string>()))
            .Returns<string>(id => agents.FirstOrDefault(a => a.AgentId == id));

        _registryMock.Setup(r => r.CanAccess(It.IsAny<string>()))
            .Returns<string>(id =>
            {
                var agent = agents.FirstOrDefault(a => a.AgentId == id);
                return agent != null && agent.RequiredTier <= LicenseTier.Teams;
            });

        // Default: MediatR publish always succeeds
        _mediatorMock.Setup(m => m.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // -----------------------------------------------------------------------
    // InitializeAsync Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that InitializeAsync loads all available agents.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsAllAgents()
    {
        // Arrange
        var sut = CreateViewModel();

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        sut.AllAgents.Should().HaveCount(3);
        sut.AllAgents.Select(a => a.AgentId).Should().Contain(new[] { "general-chat", "editor", "researcher" });
        sut.IsLoading.Should().BeFalse();
        sut.ErrorMessage.Should().BeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that InitializeAsync loads favorites from settings.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsFavoritesFromSettings()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetAsync<List<string>>(
                "agent.favorites", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "general-chat", "editor" });

        var sut = CreateViewModel();

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        sut.FavoriteAgents.Should().HaveCount(2);
        sut.FavoriteAgents.Select(a => a.AgentId).Should().Contain(new[] { "general-chat", "editor" });
        sut.AllAgents.Where(a => a.IsFavorite).Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that InitializeAsync loads recent agents from settings.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsRecentAgentsFromSettings()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetAsync<List<string>>(
                "agent.recent", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "editor", "general-chat" });

        var sut = CreateViewModel();

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        sut.RecentAgents.Should().HaveCount(2);
        sut.RecentAgents.Select(a => a.AgentId).Should().ContainInOrder("editor", "general-chat");
    }

    /// <summary>
    /// Verifies that InitializeAsync restores last selected agent and persona.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_RestoresLastSelection()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetAsync<string?>(
                "agent.last_used", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("editor");

        _settingsServiceMock.Setup(s => s.GetAsync<string?>(
                "agent.last_persona", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("strict");

        var sut = CreateViewModel();

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        sut.SelectedAgent.Should().NotBeNull();
        sut.SelectedAgent!.AgentId.Should().Be("editor");
        sut.SelectedAgent.IsSelected.Should().BeTrue();
        sut.SelectedPersona.Should().NotBeNull();
        sut.SelectedPersona!.PersonaId.Should().Be("strict");
    }

    // -----------------------------------------------------------------------
    // SelectAgentAsync Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that SelectAgentCommand updates SelectedAgent.
    /// </summary>
    [Fact]
    public async Task SelectAgentAsync_UpdatesSelectedAgent()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agentToSelect = sut.AllAgents.First(a => a.AgentId == "editor");

        // Act
        await sut.SelectAgentCommand.ExecuteAsync(agentToSelect);

        // Assert
        sut.SelectedAgent.Should().BeSameAs(agentToSelect);
        agentToSelect.IsSelected.Should().BeTrue();
        sut.AllAgents.Where(a => a.IsSelected).Should().ContainSingle();
    }

    /// <summary>
    /// Verifies that SelectAgentCommand updates recent agents list.
    /// </summary>
    [Fact]
    public async Task SelectAgentAsync_UpdatesRecentAgents()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agentToSelect = sut.AllAgents.First(a => a.AgentId == "editor");

        // Act
        await sut.SelectAgentCommand.ExecuteAsync(agentToSelect);

        // Assert
        sut.RecentAgents.Should().ContainSingle();
        sut.RecentAgents.First().AgentId.Should().Be("editor");
    }

    /// <summary>
    /// Verifies that SelectAgentCommand persists selection to settings.
    /// </summary>
    [Fact]
    public async Task SelectAgentAsync_PersistsSelectionToSettings()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agentToSelect = sut.AllAgents.First(a => a.AgentId == "editor");

        // Act
        await sut.SelectAgentCommand.ExecuteAsync(agentToSelect);

        // Assert
        _settingsServiceMock.Verify(s => s.SetAsync(
            "agent.last_used",
            "editor",
            It.IsAny<CancellationToken>()), Times.Once);

        _settingsServiceMock.Verify(s => s.SetAsync(
            "agent.recent",
            It.Is<List<string>>(list => list.Contains("editor")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that SelectAgentCommand for locked agent shows upgrade prompt.
    /// </summary>
    [Fact]
    public async Task SelectAgentAsync_LockedAgent_ShowsUpgradePrompt()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.Core);

        _registryMock.Setup(r => r.CanAccess(It.IsAny<string>()))
            .Returns<string>(id =>
            {
                var agent = _registryMock.Object.AvailableAgents.FirstOrDefault(a => a.AgentId == id);
                return agent != null && agent.RequiredTier <= LicenseTier.Core;
            });

        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var lockedAgent = sut.AllAgents.First(a => a.AgentId == "editor"); // WriterPro tier

        // Act
        await sut.SelectAgentCommand.ExecuteAsync(lockedAgent);

        // Assert
        lockedAgent.IsLocked.Should().BeTrue();
        sut.SelectedAgent.Should().BeNull(); // Selection should not change
        // TODO: Verify ShowUpgradePromptAsync is called once implemented
    }

    // -----------------------------------------------------------------------
    // SelectPersonaAsync Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that SelectPersonaCommand calls IAgentRegistry.SwitchPersona.
    /// </summary>
    [Fact]
    public async Task SelectPersonaAsync_CallsRegistrySwitchPersona()
    {
        // Arrange
        _registryMock.Setup(r => r.SwitchPersona(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agent = sut.AllAgents.First(a => a.AgentId == "editor");
        await sut.SelectAgentCommand.ExecuteAsync(agent);

        var persona = agent.Personas.First(p => p.PersonaId == "friendly");

        // Act
        await sut.SelectPersonaCommand.ExecuteAsync(persona);

        // Assert
        _registryMock.Verify(r => r.SwitchPersona("editor", "friendly"), Times.Once);
    }

    /// <summary>
    /// Verifies that SelectPersonaCommand updates SelectedPersona.
    /// </summary>
    [Fact]
    public async Task SelectPersonaAsync_UpdatesSelectedPersona()
    {
        // Arrange
        _registryMock.Setup(r => r.SwitchPersona(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agent = sut.AllAgents.First(a => a.AgentId == "editor");
        await sut.SelectAgentCommand.ExecuteAsync(agent);

        var persona = agent.Personas.First(p => p.PersonaId == "friendly");

        // Act
        await sut.SelectPersonaCommand.ExecuteAsync(persona);

        // Assert
        sut.SelectedPersona.Should().BeSameAs(persona);
        persona.IsSelected.Should().BeTrue();
        agent.Personas.Where(p => p.IsSelected).Should().ContainSingle();
    }

    // -----------------------------------------------------------------------
    // ToggleFavoriteAsync Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that ToggleFavoriteCommand adds agent to favorites.
    /// </summary>
    [Fact]
    public async Task ToggleFavoriteAsync_AddsToFavorites()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agent = sut.AllAgents.First(a => a.AgentId == "editor");

        // Act
        await sut.ToggleFavoriteCommand.ExecuteAsync(agent);

        // Assert
        agent.IsFavorite.Should().BeTrue();
        sut.FavoriteAgents.Should().ContainSingle();
        sut.FavoriteAgents.First().AgentId.Should().Be("editor");
    }

    /// <summary>
    /// Verifies that ToggleFavoriteCommand removes agent from favorites.
    /// </summary>
    [Fact]
    public async Task ToggleFavoriteAsync_RemovesFromFavorites()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetAsync<List<string>>(
                "agent.favorites", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "editor" });

        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agent = sut.AllAgents.First(a => a.AgentId == "editor");

        // Act
        await sut.ToggleFavoriteCommand.ExecuteAsync(agent);

        // Assert
        agent.IsFavorite.Should().BeFalse();
        sut.FavoriteAgents.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ToggleFavoriteCommand persists favorites to settings.
    /// </summary>
    [Fact]
    public async Task ToggleFavoriteAsync_PersistsFavoritesToSettings()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agent = sut.AllAgents.First(a => a.AgentId == "editor");

        // Act
        await sut.ToggleFavoriteCommand.ExecuteAsync(agent);

        // Assert
        _settingsServiceMock.Verify(s => s.SetAsync(
            "agent.favorites",
            It.Is<List<string>>(list => list.Contains("editor")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // FilteredAgents Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that FilteredAgents filters agents by search query.
    /// </summary>
    [Fact]
    public async Task FilteredAgents_WithSearchQuery_FiltersResults()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.SearchQuery = "edit";

        // Assert
        sut.FilteredAgents.Should().ContainSingle();
        sut.FilteredAgents.First().AgentId.Should().Be("editor");
    }

    // -----------------------------------------------------------------------
    // MediatR Event Handler Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that Receive(AgentRegisteredEvent) adds new agent to list.
    /// </summary>
    [Fact]
    public async Task Receive_AgentRegisteredEvent_AddsToList()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);

        var newConfig = CreateTestConfiguration("storyteller", LicenseTier.WriterPro);
        var newEvent = new AgentRegisteredEvent(newConfig);

        // Update mock to include new agent
        var updatedAgents = _registryMock.Object.AvailableAgents.Concat(new[] { newConfig }).ToList();
        _registryMock.Setup(r => r.AvailableAgents).Returns(updatedAgents);
        _registryMock.Setup(r => r.GetConfiguration("storyteller")).Returns(newConfig);
        _registryMock.Setup(r => r.CanAccess("storyteller")).Returns(true);

        // Act
        await sut.Receive(newEvent, CancellationToken.None);

        // Assert
        sut.AllAgents.Should().HaveCount(4);
        sut.AllAgents.Any(a => a.AgentId == "storyteller").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Receive(PersonaSwitchedEvent) updates SelectedPersona.
    /// </summary>
    [Fact]
    public async Task Receive_PersonaSwitchedEvent_UpdatesSelectedPersona()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);
        var agent = sut.AllAgents.First(a => a.AgentId == "editor");
        await sut.SelectAgentCommand.ExecuteAsync(agent);

        var switchEvent = new PersonaSwitchedEvent("editor", "friendly");

        // Act
        await sut.Receive(switchEvent, CancellationToken.None);

        // Assert
        sut.SelectedPersona.Should().NotBeNull();
        sut.SelectedPersona!.PersonaId.Should().Be("friendly");
    }

    /// <summary>
    /// Verifies that Receive(AgentConfigReloadedEvent) replaces configuration.
    /// </summary>
    [Fact]
    public async Task Receive_AgentConfigReloadedEvent_ReplacesConfiguration()
    {
        // Arrange
        var sut = CreateViewModel();
        await sut.InitializeCommand.ExecuteAsync(null);

        var updatedConfig = CreateTestConfiguration("editor", LicenseTier.WriterPro, "Updated Editor");
        var reloadEvent = new AgentConfigReloadedEvent("editor", updatedConfig);

        // Update mock to return updated config
        _registryMock.Setup(r => r.GetConfiguration("editor")).Returns(updatedConfig);

        // Act
        await sut.Receive(reloadEvent, CancellationToken.None);

        // Assert
        var agent = sut.AllAgents.First(a => a.AgentId == "editor");
        agent.Name.Should().Be("Updated Editor");
    }

    // -----------------------------------------------------------------------
    // Helper Methods
    // -----------------------------------------------------------------------

    private AgentSelectorViewModel CreateViewModel()
    {
        return new AgentSelectorViewModel(
            _registryMock.Object,
            _settingsServiceMock.Object,
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    private static AgentConfiguration CreateTestConfiguration(
        string agentId,
        LicenseTier tier = LicenseTier.Core,
        string? name = null)
    {
        return new AgentConfiguration(
            AgentId: agentId,
            Name: name ?? agentId.Replace("-", " ").ToTitleCase(),
            Description: $"Description for {agentId}",
            Icon: "message",
            TemplateId: $"{agentId}-template",
            Capabilities: AgentCapabilities.Chat | AgentCapabilities.DocumentContext,
            DefaultOptions: new ChatOptions(Model: "gpt-4o", Temperature: 0.7),
            Personas: new List<AgentPersona>
            {
                new("strict", "Strict", "No errors escape", null, 0.1),
                new("friendly", "Friendly", "Gentle suggestions", null, 0.5)
            },
            RequiredTier: tier);
    }
}

/// <summary>
/// Extension method to convert strings to title case.
/// </summary>
internal static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join(" ", words);
    }
}
