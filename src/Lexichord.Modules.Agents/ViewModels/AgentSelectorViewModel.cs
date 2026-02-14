// -----------------------------------------------------------------------
// <copyright file="AgentSelectorViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Events;
using Lexichord.Abstractions.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel for the Agent Selector dropdown component.
/// </summary>
/// <remarks>
/// <para>
/// Manages agent selection, persona switching, favorites, and recent agents.
/// Subscribes to MediatR events for real-time updates when agents are registered,
/// personas are switched, or configurations are reloaded.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Agent discovery and selection with license gating</description></item>
///   <item><description>Persona switching within selected agent</description></item>
///   <item><description>Favorites management with persistence</description></item>
///   <item><description>Recent agents tracking (max 5)</description></item>
///   <item><description>Search/filter functionality</description></item>
///   <item><description>Upgrade prompts for locked agents</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.7.1d as part of the Agent Selector UI feature.
/// </para>
/// </remarks>
public sealed partial class AgentSelectorViewModel : ObservableObject,
    IRecipient<AgentRegisteredEvent>,
    IRecipient<PersonaSwitchedEvent>,
    IRecipient<AgentConfigReloadedEvent>
{
    #region Fields

    /// <summary>
    /// Agent registry for retrieving available agents and personas.
    /// </summary>
    private readonly IAgentRegistry _registry;

    /// <summary>
    /// Settings service for persisting favorites, recents, and last selection.
    /// </summary>
    private readonly ISettingsService _settings;

    /// <summary>
    /// License context for checking user's tier.
    /// </summary>
    private readonly ILicenseContext _licenseContext;

    /// <summary>
    /// Messenger for publishing upgrade prompts and subscribing to agent events.
    /// </summary>
    private readonly IMessenger _messenger;

    /// <summary>
    /// Logger for diagnostic output.
    /// </summary>
    private readonly ILogger<AgentSelectorViewModel> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSelectorViewModel"/> class.
    /// </summary>
    /// <param name="registry">The agent registry.</param>
    /// <param name="settings">The settings service.</param>
    /// <param name="licenseContext">The license context.</param>
    /// <param name="messenger">The messenger for event notifications.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <remarks>
    /// LOGIC: Subscribes to MVVM Toolkit messaging events for live updates when agents are
    /// registered, personas are switched, or configurations are reloaded.
    /// </remarks>
    public AgentSelectorViewModel(
        IAgentRegistry registry,
        ISettingsService settings,
        ILicenseContext licenseContext,
        IMessenger messenger,
        ILogger<AgentSelectorViewModel> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Register for MVVM Toolkit messaging event notifications.
        // This enables real-time UI updates when agents are registered/updated.
        _messenger.RegisterAll(this);

        _logger.LogDebug("AgentSelectorViewModel created");
    }

    #endregion

    #region Observable Properties

    /// <summary>
    /// Search query text for filtering agents.
    /// </summary>
    /// <remarks>
    /// LOGIC: When changed, triggers re-evaluation of <see cref="FilteredAgents"/>.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredAgents))]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Currently selected agent.
    /// </summary>
    [ObservableProperty]
    private AgentItemViewModel? _selectedAgent;

    /// <summary>
    /// Currently selected persona within the selected agent.
    /// </summary>
    [ObservableProperty]
    private PersonaItemViewModel? _selectedPersona;

    /// <summary>
    /// Indicates whether the dropdown is currently open.
    /// </summary>
    [ObservableProperty]
    private bool _isDropdownOpen;

    /// <summary>
    /// Indicates whether agents are currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>
    /// Error message to display if initialization fails.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    #endregion

    #region Collections

    /// <summary>
    /// Gets all available agents.
    /// </summary>
    public ObservableCollection<AgentItemViewModel> AllAgents { get; } = [];

    /// <summary>
    /// Gets favorite agents.
    /// </summary>
    public ObservableCollection<AgentItemViewModel> FavoriteAgents { get; } = [];

    /// <summary>
    /// Gets recently used agents (max 5).
    /// </summary>
    public ObservableCollection<AgentItemViewModel> RecentAgents { get; } = [];

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets filtered agents based on search query.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns all agents if search query is empty, otherwise filters
    /// by agent name or description (case-insensitive).
    /// </remarks>
    public IEnumerable<AgentItemViewModel> FilteredAgents =>
        string.IsNullOrWhiteSpace(SearchQuery)
            ? AllAgents
            : AllAgents.Where(a =>
                a.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a value indicating whether there are any favorite agents.
    /// </summary>
    public bool HasFavorites => FavoriteAgents.Count > 0;

    /// <summary>
    /// Gets a value indicating whether there are any recent agents.
    /// </summary>
    public bool HasRecentAgents => RecentAgents.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the user can access specialist agents.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true if user's tier is WriterPro or higher.
    /// </remarks>
    public bool CanAccessSpecialists => _licenseContext.GetCurrentTier() >= LicenseTier.WriterPro;

    #endregion

    #region Commands

    /// <summary>
    /// Command to initialize the view model.
    /// </summary>
    /// <remarks>
    /// LOGIC: Loads agents from registry, restores favorites/recents from settings,
    /// and restores last selected agent/persona.
    /// </remarks>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _logger.LogDebug("Initializing AgentSelectorViewModel");

            // LOGIC: Load all agents from registry.
            await LoadAgentsAsync();

            // LOGIC: Restore favorites and recents from settings.
            await LoadFavoritesAsync();
            await LoadRecentAsync();

            // LOGIC: Restore last used agent and persona.
            var lastAgentId = _settings.Get<string>("agent.last_used", string.Empty);
            var lastPersonaId = _settings.Get<string>("agent.last_persona", string.Empty);

            if (!string.IsNullOrEmpty(lastAgentId))
            {
                var agent = AllAgents.FirstOrDefault(a => a.AgentId == lastAgentId);
                if (agent is not null)
                {
                    await SelectAgentAsync(agent, lastPersonaId);
                    _logger.LogDebug("Restored last selection: {AgentId}, persona: {PersonaId}", lastAgentId, lastPersonaId);
                    return;
                }
            }

            // LOGIC: Fall back to first accessible agent if no last selection.
            var defaultAgent = AllAgents.FirstOrDefault(a => a.CanAccess);
            if (defaultAgent is not null)
            {
                await SelectAgentAsync(defaultAgent);
                _logger.LogDebug("Selected default agent: {AgentId}", defaultAgent.AgentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize agent selector");
            ErrorMessage = "Failed to load agents. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to select an agent from the UI.
    /// </summary>
    /// <param name="agent">The agent to select.</param>
    /// <remarks>
    /// LOGIC: UI command that delegates to <see cref="SelectAgentAsync"/> with no persona override.
    /// </remarks>
    [RelayCommand]
    private Task SelectAgent(AgentItemViewModel agent) => SelectAgentAsync(agent, personaId: null);

    /// <summary>
    /// Selects an agent with optional persona.
    /// </summary>
    /// <param name="agent">The agent to select.</param>
    /// <param name="personaId">Optional persona ID to apply.</param>
    /// <remarks>
    /// LOGIC: If agent is locked (insufficient tier), shows upgrade prompt.
    /// Otherwise, selects agent, optionally applies persona, updates recents,
    /// and persists selection to settings.
    /// </remarks>
    private async Task SelectAgentAsync(AgentItemViewModel agent, string? personaId = null)
    {
        if (agent is null)
        {
            _logger.LogWarning("SelectAgentAsync called with null agent");
            return;
        }

        // LOGIC: Check if agent is accessible (license tier check).
        if (!agent.CanAccess)
        {
            _logger.LogDebug("Agent {AgentId} not accessible, showing upgrade prompt", agent.AgentId);
            await ShowUpgradePromptAsync(agent);
            return;
        }

        _logger.LogDebug("Selecting agent: {AgentId}", agent.AgentId);

        // LOGIC: Update selected agent and mark as selected in UI.
        if (SelectedAgent is not null)
        {
            SelectedAgent.IsSelected = false;
        }

        SelectedAgent = agent;
        agent.IsSelected = true;

        // LOGIC: Select persona (provided or default).
        var persona = personaId is not null
            ? agent.Personas.FirstOrDefault(p => p.PersonaId == personaId)
            : agent.DefaultPersona;

        if (persona is not null)
        {
            await SelectPersonaAsync(persona);
        }

        // LOGIC: Update recents list (move to front, trim to 5).
        await UpdateRecentAgentsAsync(agent);

        // LOGIC: Persist selection to settings for restoration on next launch.
        _settings.Set("agent.last_used", agent.AgentId);

        // LOGIC: Close dropdown after selection.
        IsDropdownOpen = false;

        _logger.LogInformation("Agent selected: {AgentId}", agent.AgentId);
    }

    /// <summary>
    /// Command to select a persona within the current agent.
    /// </summary>
    /// <param name="persona">The persona to select.</param>
    /// <remarks>
    /// LOGIC: Calls <see cref="IAgentRegistry.SwitchPersona"/> to apply the persona,
    /// updates UI selection state, and persists to settings.
    /// </remarks>
    [RelayCommand]
    private Task SelectPersonaAsync(PersonaItemViewModel persona)
    {
        if (SelectedAgent is null || persona is null)
        {
            _logger.LogWarning("SelectPersonaAsync called with null agent or persona");
            return Task.CompletedTask;
        }

        _logger.LogDebug("Selecting persona: {PersonaId} for {AgentId}", persona.PersonaId, SelectedAgent.AgentId);

        // LOGIC: Update registry with new persona.
        _registry.SwitchPersona(SelectedAgent.AgentId, persona.PersonaId);

        // LOGIC: Update selected persona and UI state.
        SelectedPersona = persona;

        foreach (var p in SelectedAgent.Personas)
        {
            p.IsSelected = p.PersonaId == persona.PersonaId;
        }

        // LOGIC: Persist persona selection to settings.
        _settings.Set("agent.last_persona", persona.PersonaId);

        _logger.LogInformation("Persona selected: {PersonaId}", persona.PersonaId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Command to toggle an agent's favorite status.
    /// </summary>
    /// <param name="agent">The agent to toggle.</param>
    /// <remarks>
    /// LOGIC: Adds/removes agent from favorites collection and persists to settings.
    /// </remarks>
    [RelayCommand]
    private async Task ToggleFavoriteAsync(AgentItemViewModel agent)
    {
        if (agent is null)
        {
            _logger.LogWarning("ToggleFavoriteAsync called with null agent");
            return;
        }

        // LOGIC: Toggle favorite state.
        agent.IsFavorite = !agent.IsFavorite;

        if (agent.IsFavorite)
        {
            FavoriteAgents.Add(agent);
            _logger.LogDebug("Added {AgentId} to favorites", agent.AgentId);
        }
        else
        {
            FavoriteAgents.Remove(agent);
            _logger.LogDebug("Removed {AgentId} from favorites", agent.AgentId);
        }

        // LOGIC: Persist favorites to settings.
        await SaveFavoritesAsync();

        // LOGIC: Notify UI that HasFavorites may have changed.
        OnPropertyChanged(nameof(HasFavorites));
    }

    /// <summary>
    /// Command to toggle the dropdown open/closed.
    /// </summary>
    [RelayCommand]
    private void ToggleDropdown()
    {
        IsDropdownOpen = !IsDropdownOpen;

        // LOGIC: Clear search when opening dropdown.
        if (IsDropdownOpen)
        {
            SearchQuery = string.Empty;
        }
    }

    /// <summary>
    /// Command to close the dropdown.
    /// </summary>
    [RelayCommand]
    private void CloseDropdown()
    {
        IsDropdownOpen = false;
        SearchQuery = string.Empty;
    }

    #endregion

    #region MediatR Event Handlers

    /// <summary>
    /// Handles <see cref="AgentRegisteredEvent"/> notifications.
    /// </summary>
    /// <param name="message">The event message.</param>
    /// <remarks>
    /// LOGIC: When a new agent is registered (e.g., workspace agent loaded),
    /// add it to the AllAgents collection for immediate UI update.
    /// </remarks>
    public void Receive(AgentRegisteredEvent message)
    {
        _logger.LogDebug("Received AgentRegisteredEvent: {AgentId}", message.Configuration.AgentId);

        // LOGIC: UI updates must be marshaled to UI thread.
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var vm = CreateAgentViewModel(message.Configuration);
            AllAgents.Add(vm);
            _logger.LogDebug("Added agent {AgentId} to AllAgents", message.Configuration.AgentId);
        });
    }

    /// <summary>
    /// Handles <see cref="PersonaSwitchedEvent"/> notifications.
    /// </summary>
    /// <param name="message">The event message.</param>
    /// <remarks>
    /// LOGIC: If the switched persona is for the currently selected agent,
    /// update the SelectedPersona to reflect the change.
    /// </remarks>
    public void Receive(PersonaSwitchedEvent message)
    {
        _logger.LogDebug("Received PersonaSwitchedEvent: {AgentId} -> {PersonaId}",
            message.AgentId, message.NewPersonaId);

        // LOGIC: Only update if the event is for the currently selected agent.
        if (SelectedAgent?.AgentId == message.AgentId)
        {
            var persona = SelectedAgent.Personas.FirstOrDefault(p => p.PersonaId == message.NewPersonaId);
            if (persona is not null)
            {
                SelectedPersona = persona;
                _logger.LogDebug("Updated SelectedPersona to {PersonaId}", message.NewPersonaId);
            }
        }
    }

    /// <summary>
    /// Handles <see cref="AgentConfigReloadedEvent"/> notifications.
    /// </summary>
    /// <param name="message">The event message.</param>
    /// <remarks>
    /// LOGIC: When an agent configuration is hot-reloaded (e.g., YAML file changed),
    /// replace the old configuration with the new one in the AllAgents collection.
    /// </remarks>
    public void Receive(AgentConfigReloadedEvent message)
    {
        _logger.LogDebug("Received AgentConfigReloadedEvent: {AgentId}", message.AgentId);

        // LOGIC: UI updates must be marshaled to UI thread.
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existing = AllAgents.FirstOrDefault(a => a.AgentId == message.AgentId);
            if (existing is not null)
            {
                var index = AllAgents.IndexOf(existing);
                AllAgents.RemoveAt(index);

                var updated = CreateAgentViewModel(message.NewConfiguration);
                AllAgents.Insert(index, updated);

                _logger.LogDebug("Replaced agent {AgentId} in AllAgents", message.AgentId);
            }
        });
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Loads agents from the registry.
    /// </summary>
    /// <remarks>
    /// LOGIC: Retrieves all available agents from <see cref="IAgentRegistry.AvailableAgents"/>,
    /// creates ViewModels for each, and adds them to AllAgents collection.
    /// </remarks>
    private async Task LoadAgentsAsync()
    {
        await Task.Run(() =>
        {
            var agents = _registry.AvailableAgents;

            // LOGIC: Marshal to UI thread for collection updates.
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var agent in agents.OrderBy(a => a.Name))
                {
                    var config = _registry.GetConfiguration(agent.AgentId);
                    if (config is null)
                    {
                        _logger.LogWarning("No configuration found for agent: {AgentId}", agent.AgentId);
                        continue;
                    }

                    var vm = CreateAgentViewModel(config);
                    AllAgents.Add(vm);
                }

                _logger.LogDebug("Loaded {Count} agents", AllAgents.Count);
            });
        });
    }

    /// <summary>
    /// Creates an <see cref="AgentItemViewModel"/> from an <see cref="AgentConfiguration"/>.
    /// </summary>
    /// <param name="config">The agent configuration.</param>
    /// <returns>The created view model.</returns>
    /// <remarks>
    /// LOGIC: Checks license access via <see cref="IAgentRegistry.CanAccess"/> and
    /// creates PersonaItemViewModels for all personas defined in the configuration.
    /// </remarks>
    private AgentItemViewModel CreateAgentViewModel(AgentConfiguration config)
    {
        var canAccess = _registry.CanAccess(config.AgentId);

        var personas = config.Personas
            .Select(p => new PersonaItemViewModel
            {
                PersonaId = p.PersonaId,
                DisplayName = p.DisplayName,
                Tagline = p.Tagline,
                VoiceDescription = p.VoiceDescription,
                Temperature = p.Temperature,
                IsSelected = false
            })
            .ToList();

        var viewModel = new AgentItemViewModel
        {
            AgentId = config.AgentId,
            Name = config.Name,
            Description = config.Description,
            Icon = config.Icon,
            Capabilities = config.Capabilities,
            RequiredTier = config.RequiredTier,
            CanAccess = canAccess,
            IsFavorite = false
        };

        // LOGIC: Add personas to the existing collection (Personas property is get-only)
        foreach (var persona in personas)
        {
            viewModel.Personas.Add(persona);
        }

        return viewModel;
    }

    /// <summary>
    /// Loads favorite agents from settings.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reads "agent.favorites" list from settings, marks corresponding
    /// agents as favorites, and adds them to FavoriteAgents collection.
    /// </remarks>
    private Task LoadFavoritesAsync()
    {
        var favoriteIds = _settings.Get<List<string>>("agent.favorites", []);

        foreach (var id in favoriteIds)
        {
            var agent = AllAgents.FirstOrDefault(a => a.AgentId == id);
            if (agent is not null)
            {
                agent.IsFavorite = true;
                FavoriteAgents.Add(agent);
            }
        }

        OnPropertyChanged(nameof(HasFavorites));
        _logger.LogDebug("Loaded {Count} favorites", FavoriteAgents.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves favorite agents to settings.
    /// </summary>
    /// <remarks>
    /// LOGIC: Persists the list of favorite agent IDs to "agent.favorites" setting.
    /// </remarks>
    private Task SaveFavoritesAsync()
    {
        var favoriteIds = FavoriteAgents.Select(a => a.AgentId).ToList();
        _settings.Set("agent.favorites", favoriteIds);
        _logger.LogDebug("Saved {Count} favorites", favoriteIds.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads recent agents from settings.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reads "agent.recent" list from settings (max 5), and adds
    /// corresponding agents to RecentAgents collection.
    /// </remarks>
    private Task LoadRecentAsync()
    {
        var recentIds = _settings.Get<List<string>>("agent.recent", []);

        foreach (var id in recentIds.Take(5))
        {
            var agent = AllAgents.FirstOrDefault(a => a.AgentId == id);
            if (agent is not null)
            {
                RecentAgents.Add(agent);
            }
        }

        OnPropertyChanged(nameof(HasRecentAgents));
        _logger.LogDebug("Loaded {Count} recent agents", RecentAgents.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the recent agents list with the newly selected agent.
    /// </summary>
    /// <param name="agent">The agent to add to recents.</param>
    /// <remarks>
    /// LOGIC: Removes agent if already in list, inserts at front, trims to 5,
    /// and persists to settings.
    /// </remarks>
    private Task UpdateRecentAgentsAsync(AgentItemViewModel agent)
    {
        // LOGIC: Remove if already present (to move to front).
        RecentAgents.Remove(agent);

        // LOGIC: Insert at front.
        RecentAgents.Insert(0, agent);

        // LOGIC: Trim to max 5 entries.
        while (RecentAgents.Count > 5)
        {
            RecentAgents.RemoveAt(RecentAgents.Count - 1);
        }

        // LOGIC: Persist to settings.
        var recentIds = RecentAgents.Select(a => a.AgentId).ToList();
        _settings.Set("agent.recent", recentIds);

        OnPropertyChanged(nameof(HasRecentAgents));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows an upgrade prompt for a locked agent.
    /// </summary>
    /// <param name="agent">The locked agent.</param>
    /// <remarks>
    /// LOGIC: Publishes a ShowUpgradePromptRequest event via MediatR for
    /// the host to display an upgrade dialog.
    /// </remarks>
    private Task ShowUpgradePromptAsync(AgentItemViewModel agent)
    {
        // NOTE: ShowUpgradePromptRequest is not yet defined in the codebase.
        // This is a placeholder for future upgrade prompt integration.
        // For now, just log the request.
        _logger.LogInformation(
            "Upgrade prompt requested for agent {AgentId} (requires {Tier})",
            agent.AgentId,
            agent.RequiredTier);

        // TODO: Uncomment when ShowUpgradePromptRequest is implemented.
        // await _mediator.Publish(new ShowUpgradePromptRequest(
        //     Feature: $"agent:{agent.AgentId}",
        //     RequiredTier: agent.RequiredTier,
        //     Message: $"'{agent.Name}' requires {agent.RequiredTier} tier."));
        return Task.CompletedTask;
    }

    #endregion
}
