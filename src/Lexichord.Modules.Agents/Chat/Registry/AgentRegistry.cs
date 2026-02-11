// -----------------------------------------------------------------------
// <copyright file="AgentRegistry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Singleton implementation of IAgentRegistry that manages the
//   lifecycle of agent discovery, license-based filtering, default agent
//   selection, and custom agent registration. Uses ConcurrentDictionary
//   for thread-safe access and subscribes to license change events for
//   automatic refresh.
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Events;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Registry;

/// <summary>
/// Default implementation of <see cref="IAgentRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// The AgentRegistry is a singleton service that manages the lifecycle of
/// agent discovery and access. It performs the following on initialization:
/// </para>
/// <list type="number">
///   <item>Scans the DI container for IAgent implementations</item>
///   <item>Extracts metadata (AgentId, Name, Capabilities, License requirements)</item>
///   <item>Filters based on current license tier</item>
///   <item>Loads any custom agents from user settings (Teams only)</item>
/// </list>
/// <para>
/// The registry subscribes to license change events and automatically
/// refreshes the available agents when the user's tier changes.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Uses <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// for all agent collections and a lock for cache rebuilds.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
public sealed class AgentRegistry : IAgentRegistry, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILicenseContext _licenseContext;
    private readonly ISettingsService _settingsService;
    private readonly IMediator _mediator;
    private readonly ILogger<AgentRegistry> _logger;

    private readonly ConcurrentDictionary<string, AgentMetadata> _registeredAgents = new();
    private readonly ConcurrentDictionary<string, IAgent> _customAgents = new();
    private readonly object _refreshLock = new();

    // v0.7.1b: Factory-based registration support
    private readonly ConcurrentDictionary<string, AgentRegistration> _factoryRegistrations = new();
    private readonly ConcurrentDictionary<string, string> _activePersonas = new();
    private readonly ConcurrentDictionary<string, IAgent> _agentInstanceCache = new();

    private IReadOnlyList<IAgent>? _cachedAvailableAgents;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of <see cref="AgentRegistry"/>.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider for agent resolution.</param>
    /// <param name="licenseContext">The license context for tier checks.</param>
    /// <param name="settingsService">The settings service for custom agent persistence.</param>
    /// <param name="mediator">The MediatR mediator for publishing registry events (v0.7.1b).</param>
    /// <param name="logger">Logger for registry diagnostics.</param>
    /// <remarks>
    /// LOGIC: Constructor performs initial agent discovery and subscribes
    /// to license change events. Custom agents are loaded if the user
    /// has a Teams license.
    /// </remarks>
    public AgentRegistry(
        IServiceProvider serviceProvider,
        ILicenseContext licenseContext,
        ISettingsService settingsService,
        IMediator mediator,
        ILogger<AgentRegistry> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Subscribe to license changes to auto-refresh.
        _licenseContext.LicenseChanged += OnLicenseChanged;

        // LOGIC: Perform initial discovery of DI-registered agents.
        DiscoverAgents();

        // LOGIC: Load custom agents from settings (Teams only).
        LoadCustomAgents();

        _logger.LogInformation(
            "AgentRegistry initialized with {AgentCount} agents",
            _registeredAgents.Count);
    }

    /// <inheritdoc />
    public IReadOnlyList<IAgent> AvailableAgents
    {
        get
        {
            if (_cachedAvailableAgents is null)
            {
                lock (_refreshLock)
                {
                    _cachedAvailableAgents ??= BuildAvailableAgentsList();
                }
            }
            return _cachedAvailableAgents;
        }
    }

    /// <inheritdoc />
    public event EventHandler<AgentListChangedEventArgs>? AgentsChanged;

    /// <inheritdoc />
    public IAgent GetAgent(string agentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        if (TryGetAgent(agentId, out var agent))
        {
            return agent;
        }

        // LOGIC: If the agent exists but isn't accessible, it's a license issue.
        if (_registeredAgents.TryGetValue(agentId, out var metadata))
        {
            _logger.LogWarning(
                "Agent '{AgentId}' requires {RequiredLicense} license, user has {CurrentTier}",
                agentId, metadata.RequiredLicense, _licenseContext.GetCurrentTier());

            throw new LicenseTierException(
                $"Agent '{agentId}' requires {metadata.RequiredLicense} license",
                metadata.RequiredLicense);
        }

        _logger.LogWarning("Agent not found: {AgentId}", agentId);
        throw new AgentNotFoundException(agentId);
    }

    /// <inheritdoc />
    public bool TryGetAgent(string agentId, [MaybeNullWhen(false)] out IAgent agent)
    {
        agent = null;

        if (string.IsNullOrEmpty(agentId))
            return false;

        // LOGIC: Check custom agents first (higher priority).
        if (_customAgents.TryGetValue(agentId, out agent))
        {
            if (IsAgentAccessible(agent))
            {
                _logger.LogDebug("Retrieved custom agent: {AgentId}", agentId);
                return true;
            }
            agent = null;
            return false;
        }

        // LOGIC: Check registered agents via metadata.
        if (!_registeredAgents.TryGetValue(agentId, out var metadata))
            return false;

        if (!IsMetadataAccessible(metadata))
            return false;

        // LOGIC: Resolve the agent instance from DI.
        agent = ResolveAgent(agentId);
        if (agent is not null)
        {
            _logger.LogDebug("Retrieved agent: {AgentId}", agentId);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public IAgent GetDefaultAgent()
    {
        // LOGIC: Step 1 — Try user's preferred agent.
        var defaultId = _settingsService.Get("Agent:DefaultAgentId", "co-pilot");

        if (TryGetAgent(defaultId, out var agent))
        {
            _logger.LogDebug("Returning default agent: {AgentId}", defaultId);
            return agent;
        }

        // LOGIC: Step 2 — Fallback to co-pilot.
        if (defaultId != "co-pilot" && TryGetAgent("co-pilot", out agent))
        {
            _logger.LogDebug("Falling back to co-pilot agent");
            return agent;
        }

        // LOGIC: Step 3 — Return any available agent.
        var available = AvailableAgents;
        if (available.Count > 0)
        {
            _logger.LogWarning(
                "Default agent not available, using first available: {AgentId}",
                available[0].AgentId);
            return available[0];
        }

        // LOGIC: Step 4 — No agents at all.
        _logger.LogError("No agents available for the current license tier");
        throw new NoAgentAvailableException();
    }

    /// <inheritdoc />
    [Obsolete("Use RegisterAgent(AgentConfiguration, Func<IServiceProvider, IAgent>) instead. This method will be removed in v0.8.0")]
    public void RegisterCustomAgent(IAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        _logger.LogWarning(
            "Using obsolete RegisterCustomAgent for {AgentId}. Migrate to RegisterAgent with AgentConfiguration.",
            agent.AgentId);

        // LOGIC: Custom agent registration requires Teams license.
        if (_licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            _logger.LogWarning(
                "Custom agent registration rejected: requires Teams, user has {Tier}",
                _licenseContext.GetCurrentTier());

            throw new LicenseTierException(
                "Custom agent registration requires Teams license",
                LicenseTier.Teams);
        }

        // LOGIC: Prevent duplicate agent IDs.
        if (_registeredAgents.ContainsKey(agent.AgentId) ||
            _customAgents.ContainsKey(agent.AgentId))
        {
            throw new AgentAlreadyRegisteredException(agent.AgentId);
        }

        _customAgents[agent.AgentId] = agent;
        SaveCustomAgents();
        InvalidateCache();

        _logger.LogInformation("Registered custom agent: {AgentId}", agent.AgentId);
        OnAgentsChanged(AgentListChangeReason.AgentRegistered);
    }

    /// <inheritdoc />
    public bool UnregisterCustomAgent(string agentId)
    {
        if (_customAgents.TryRemove(agentId, out _))
        {
            SaveCustomAgents();
            InvalidateCache();

            _logger.LogInformation("Unregistered custom agent: {AgentId}", agentId);
            OnAgentsChanged(AgentListChangeReason.AgentUnregistered);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Refresh()
    {
        _logger.LogDebug("Refreshing agent registry");
        InvalidateCache();
        OnAgentsChanged(AgentListChangeReason.Refreshed);
    }

    // -----------------------------------------------------------------------
    // v0.7.1b: Persona Management & Factory Registration
    // -----------------------------------------------------------------------

    /// <inheritdoc />
    public IReadOnlyList<AgentPersona> AvailablePersonas =>
        _factoryRegistrations.Values
            .SelectMany(r => r.Configuration.Personas)
            .Distinct()
            .ToList()
            .AsReadOnly();

    /// <inheritdoc />
    public void RegisterAgent(AgentConfiguration config, Func<IServiceProvider, IAgent> factory)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(factory);

        // LOGIC: Validate configuration before registration
        var errors = config.Validate();
        if (errors.Count > 0)
        {
            _logger.LogError(
                "Invalid agent configuration for {AgentId}: {Errors}",
                config.AgentId,
                string.Join("; ", errors));
            throw new ArgumentException($"Invalid configuration: {string.Join("; ", errors)}");
        }

        // LOGIC: Store factory-based registration
        _factoryRegistrations[config.AgentId] = new AgentRegistration(config, factory);
        InvalidateCache();

        _logger.LogInformation(
            "Registered agent: {AgentId} with {PersonaCount} personas",
            config.AgentId,
            config.Personas.Count);

        // LOGIC: Publish event (fire-and-forget with error handling)
        try
        {
            _ = _mediator.Publish(
                new AgentRegisteredEvent(config, DateTimeOffset.UtcNow),
                default);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish AgentRegisteredEvent for {AgentId}",
                config.AgentId);
        }
    }

    /// <inheritdoc />
    public IAgent GetAgentWithPersona(string agentId, string personaId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        ArgumentException.ThrowIfNullOrEmpty(personaId);

        // LOGIC: Get or create agent instance
        var agent = GetOrCreateAgentFromFactory(agentId);

        // LOGIC: Validate persona exists in agent configuration
        if (!_factoryRegistrations.TryGetValue(agentId, out var registration))
            throw new AgentNotFoundException(agentId);

        if (registration.Configuration.GetPersona(personaId) is null)
        {
            _logger.LogWarning(
                "Persona '{PersonaId}' not found for agent '{AgentId}'",
                personaId,
                agentId);
            throw new PersonaNotFoundException(agentId, personaId);
        }

        // LOGIC: Apply persona if agent supports it
        SwitchPersonaInternal(agentId, personaId, agent);

        _logger.LogDebug(
            "Retrieved agent '{AgentId}' with persona '{PersonaId}'",
            agentId,
            personaId);

        return agent;
    }

    /// <inheritdoc />
    public void UpdateAgent(AgentConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!_factoryRegistrations.TryGetValue(config.AgentId, out var existing))
        {
            _logger.LogWarning(
                "Cannot update non-existent agent: {AgentId}",
                config.AgentId);
            throw new AgentNotFoundException(config.AgentId);
        }

        // LOGIC: Validate new configuration
        var errors = config.Validate();
        if (errors.Count > 0)
        {
            _logger.LogWarning(
                "Agent update rejected for {AgentId}: {Errors}",
                config.AgentId,
                string.Join("; ", errors));
            return;
        }

        // LOGIC: Update registration with new config, preserve factory
        _factoryRegistrations[config.AgentId] = existing with { Configuration = config };

        // LOGIC: Invalidate cached agent instance to force recreation with new config
        _agentInstanceCache.TryRemove(config.AgentId, out _);
        InvalidateCache();

        _logger.LogInformation(
            "Hot-reloaded configuration for agent '{AgentId}'",
            config.AgentId);

        // LOGIC: Publish event
        try
        {
            _ = _mediator.Publish(
                new AgentConfigReloadedEvent(
                    config.AgentId,
                    existing.Configuration,
                    config,
                    DateTimeOffset.UtcNow),
                default);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish AgentConfigReloadedEvent");
        }
    }

    /// <inheritdoc />
    public void SwitchPersona(string agentId, string personaId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        ArgumentException.ThrowIfNullOrEmpty(personaId);

        // LOGIC: Validate persona exists in configuration
        if (!_factoryRegistrations.TryGetValue(agentId, out var registration))
            throw new AgentNotFoundException(agentId);

        var persona = registration.Configuration.GetPersona(personaId);
        if (persona is null)
        {
            _logger.LogWarning(
                "Persona '{PersonaId}' not found for agent '{AgentId}'",
                personaId,
                agentId);
            throw new PersonaNotFoundException(agentId, personaId);
        }

        // LOGIC: If agent is cached, apply persona immediately. Otherwise,
        // record preference for next retrieval.
        if (_agentInstanceCache.TryGetValue(agentId, out var agent))
        {
            SwitchPersonaInternal(agentId, personaId, agent);
        }
        else
        {
            _activePersonas[agentId] = personaId;
            _logger.LogDebug(
                "Persona preference recorded for {AgentId}: {PersonaId}",
                agentId,
                personaId);
        }
    }

    /// <inheritdoc />
    public AgentPersona? GetActivePersona(string agentId)
    {
        if (!_factoryRegistrations.TryGetValue(agentId, out var registration))
            return null;

        // LOGIC: Return explicitly set persona, or default persona, or null
        if (_activePersonas.TryGetValue(agentId, out var personaId))
            return registration.Configuration.GetPersona(personaId);

        return registration.Configuration.DefaultPersona;
    }

    /// <inheritdoc />
    public bool CanAccess(string agentId)
    {
        if (!_factoryRegistrations.TryGetValue(agentId, out var registration))
            return false;

        return _licenseContext.GetCurrentTier() >= registration.Configuration.RequiredTier;
    }

    /// <inheritdoc />
    public AgentConfiguration? GetConfiguration(string agentId) =>
        _factoryRegistrations.TryGetValue(agentId, out var reg)
            ? reg.Configuration
            : null;

    /// <summary>
    /// Disposes the registry and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _licenseContext.LicenseChanged -= OnLicenseChanged;
        _isDisposed = true;
    }

    // -----------------------------------------------------------------------
    // Private methods
    // -----------------------------------------------------------------------

    /// <summary>
    /// Discovers agents from the DI container.
    /// </summary>
    private void DiscoverAgents()
    {
        var discovered = AgentDiscovery.DiscoverAgents(_serviceProvider, _logger);

        foreach (var (agentId, metadata) in discovered)
        {
            _registeredAgents[agentId] = metadata;
        }
    }

    /// <summary>
    /// Loads custom agents from user settings.
    /// </summary>
    /// <remarks>
    /// LOGIC: Only loads custom agents if the user has a Teams license.
    /// Invalid definitions are logged and skipped.
    /// </remarks>
    private void LoadCustomAgents()
    {
        if (_licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            _logger.LogDebug("Skipping custom agent load (requires Teams license)");
            return;
        }

        var customAgentDefs = _settingsService.Get<CustomAgentDefinition[]?>(
            "Agent:CustomAgents", null);

        if (customAgentDefs is null || customAgentDefs.Length == 0)
        {
            _logger.LogDebug("No custom agents found in settings");
            return;
        }

        foreach (var def in customAgentDefs)
        {
            try
            {
                // LOGIC: Custom agents loaded from settings are represented
                // as simple metadata entries rather than full agent instances,
                // since we don't have the actual implementation class.
                var metadata = new AgentMetadata(
                    AgentId: def.AgentId,
                    Name: def.Name,
                    Description: def.Description,
                    Capabilities: AgentCapabilities.Chat,
                    RequiredLicense: LicenseTier.Teams,
                    AgentType: typeof(IAgent));

                _registeredAgents[def.AgentId] = metadata;
                _logger.LogDebug("Loaded custom agent definition: {AgentId}", def.AgentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load custom agent: {AgentId}", def.AgentId);
            }
        }
    }

    /// <summary>
    /// Saves custom agents to user settings.
    /// </summary>
    private void SaveCustomAgents()
    {
        var definitions = _customAgents.Values
            .Select(a => new CustomAgentDefinition(a.AgentId, a.Name, a.Description))
            .ToArray();

        _settingsService.Set("Agent:CustomAgents", definitions);
    }

    /// <summary>
    /// Builds the filtered list of available agents.
    /// </summary>
    /// <remarks>
    /// LOGIC: Iterates all registered agents, checks license accessibility,
    /// and resolves accessible agents from DI. Also includes accessible
    /// custom agents.
    /// </remarks>
    private IReadOnlyList<IAgent> BuildAvailableAgentsList()
    {
        var availableAgents = new List<IAgent>();

        // LOGIC: Add accessible registered agents resolved from DI.
        foreach (var (agentId, metadata) in _registeredAgents)
        {
            if (IsMetadataAccessible(metadata))
            {
                var agent = ResolveAgent(agentId);
                if (agent is not null)
                {
                    availableAgents.Add(agent);
                }
            }
        }

        // LOGIC: Add accessible custom agents (already instantiated).
        foreach (var (_, agent) in _customAgents)
        {
            if (IsAgentAccessible(agent))
            {
                availableAgents.Add(agent);
            }
        }

        return availableAgents.AsReadOnly();
    }

    /// <summary>
    /// Resolves an agent from the DI container by its ID.
    /// </summary>
    /// <param name="agentId">The agent ID to resolve.</param>
    /// <returns>The agent instance, or null if not found.</returns>
    /// <remarks>
    /// LOGIC: Creates a scope per resolution since IAgent is registered as scoped.
    /// Searches all registered IAgent services for the matching AgentId.
    /// </remarks>
    private IAgent? ResolveAgent(string agentId)
    {
        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetServices<IAgent>()
            .FirstOrDefault(a => a.AgentId == agentId);
    }

    /// <summary>
    /// Checks if agent metadata is accessible to the current user.
    /// </summary>
    /// <param name="metadata">The agent metadata to check.</param>
    /// <returns>True if the user's tier meets the requirement.</returns>
    private bool IsMetadataAccessible(AgentMetadata metadata) =>
        _licenseContext.GetCurrentTier() >= metadata.RequiredLicense;

    /// <summary>
    /// Checks if an agent instance is accessible to the current user.
    /// </summary>
    /// <param name="agent">The agent to check.</param>
    /// <returns>True if the user's tier meets the requirement.</returns>
    /// <remarks>
    /// LOGIC: Uses reflection to read the RequiresLicenseAttribute from
    /// the agent's concrete type. Agents without the attribute default
    /// to Core tier (accessible to all).
    /// </remarks>
    private bool IsAgentAccessible(IAgent agent)
    {
        var licenseAttr = agent.GetType().GetCustomAttribute<RequiresLicenseAttribute>();
        var required = licenseAttr?.Tier ?? LicenseTier.Core;
        return _licenseContext.GetCurrentTier() >= required;
    }

    /// <summary>
    /// Invalidates the cached agent list, forcing a rebuild on next access.
    /// </summary>
    private void InvalidateCache()
    {
        lock (_refreshLock)
        {
            _cachedAvailableAgents = null;
        }
    }

    /// <summary>
    /// Handles license change events by refreshing the agent list.
    /// </summary>
    private void OnLicenseChanged(object? sender, LicenseChangedEventArgs e)
    {
        _logger.LogInformation(
            "License changed from {OldTier} to {NewTier}, refreshing agents",
            e.OldTier, e.NewTier);

        InvalidateCache();
        OnAgentsChanged(AgentListChangeReason.LicenseChanged);
    }

    /// <summary>
    /// Raises the <see cref="AgentsChanged"/> event.
    /// </summary>
    /// <param name="reason">The reason for the change.</param>
    private void OnAgentsChanged(AgentListChangeReason reason)
    {
        AgentsChanged?.Invoke(this, new AgentListChangedEventArgs(reason, AvailableAgents));
    }

    // -----------------------------------------------------------------------
    // v0.7.1b: Private helper methods for persona management
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gets or creates an agent instance from factory registration.
    /// </summary>
    /// <param name="agentId">The agent ID to retrieve.</param>
    /// <returns>The agent instance.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method implements singleton caching for factory-based agents.
    /// Once created, the instance is cached in <see cref="_agentInstanceCache"/>
    /// until explicitly invalidated (e.g., via <see cref="UpdateAgent"/>).
    /// </para>
    /// <para>
    /// <b>License Validation:</b> Checks the user's tier before creating the
    /// agent. Throws <see cref="AgentAccessDeniedException"/> if insufficient.
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> Uses <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd"/>
    /// for atomic cache-or-create logic.
    /// </para>
    /// </remarks>
    /// <exception cref="AgentNotFoundException">
    /// Thrown if no factory registration exists for <paramref name="agentId"/>.
    /// </exception>
    /// <exception cref="AgentAccessDeniedException">
    /// Thrown if the user's license tier is insufficient.
    /// </exception>
    private IAgent GetOrCreateAgentFromFactory(string agentId)
    {
        return _agentInstanceCache.GetOrAdd(agentId, _ =>
        {
            // LOGIC: Validate factory registration exists
            if (!_factoryRegistrations.TryGetValue(agentId, out var registration))
            {
                _logger.LogWarning("Agent not found in factory registrations: {AgentId}", agentId);
                throw new AgentNotFoundException(agentId);
            }

            // LOGIC: Check license tier before creating agent
            var currentTier = _licenseContext.GetCurrentTier();
            if (currentTier < registration.Configuration.RequiredTier)
            {
                _logger.LogWarning(
                    "Agent '{AgentId}' requires {RequiredLicense} license, user has {CurrentTier}",
                    agentId,
                    registration.Configuration.RequiredTier,
                    currentTier);

                throw new AgentAccessDeniedException(
                    agentId,
                    registration.Configuration.RequiredTier,
                    currentTier);
            }

            _logger.LogDebug("Creating agent instance from factory: {AgentId}", agentId);

            try
            {
                // LOGIC: Invoke factory to create agent instance
                return registration.Factory(_serviceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create agent '{AgentId}' from factory",
                    agentId);
                throw;
            }
        });
    }

    /// <summary>
    /// Switches the persona for a cached agent instance.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="personaId">The target persona ID.</param>
    /// <param name="agent">The agent instance to apply persona to.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: This method performs the following:
    /// </para>
    /// <list type="number">
    ///   <item>Check if persona is already active (no-op if same)</item>
    ///   <item>Update <see cref="_activePersonas"/> dictionary</item>
    ///   <item>
    ///     If agent implements <see cref="IPersonaAwareAgent"/>, call
    ///     <see cref="IPersonaAwareAgent.ApplyPersona"/>
    ///   </item>
    ///   <item>Publish <see cref="PersonaSwitchedEvent"/> via MediatR</item>
    /// </list>
    /// <para>
    /// <b>Non-Persona-Aware Agents:</b> If the agent doesn't implement
    /// <see cref="IPersonaAwareAgent"/>, the persona preference is still
    /// recorded but won't affect runtime behavior. A warning is logged.
    /// </para>
    /// <para>
    /// <b>Event Publishing:</b> Persona switch events are published fire-and-forget.
    /// Failures are logged but don't prevent the operation from succeeding.
    /// </para>
    /// </remarks>
    private void SwitchPersonaInternal(string agentId, string personaId, IAgent agent)
    {
        var previousPersonaId = _activePersonas.TryGetValue(agentId, out var prev) ? prev : null;

        // LOGIC: No-op if persona is already active
        if (previousPersonaId == personaId)
        {
            _logger.LogDebug(
                "Persona '{PersonaId}' already active for agent '{AgentId}'",
                personaId,
                agentId);
            return;
        }

        // LOGIC: Update active persona tracking
        _activePersonas[agentId] = personaId;

        // LOGIC: Apply persona if agent supports it
        if (agent is IPersonaAwareAgent personaAware)
        {
            var config = _factoryRegistrations[agentId].Configuration;
            var persona = config.GetPersona(personaId)!;
            personaAware.ApplyPersona(persona);

            _logger.LogInformation(
                "Applied persona '{PersonaId}' to agent '{AgentId}'",
                personaId,
                agentId);
        }
        else
        {
            _logger.LogWarning(
                "Agent '{AgentId}' does not support persona switching (not IPersonaAwareAgent)",
                agentId);
        }

        // LOGIC: Publish event (fire-and-forget)
        try
        {
            _ = _mediator.Publish(
                new PersonaSwitchedEvent(
                    agentId,
                    previousPersonaId,
                    personaId,
                    DateTimeOffset.UtcNow),
                default);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish PersonaSwitchedEvent");
        }
    }

    // -----------------------------------------------------------------------
    // v0.7.1b: Internal nested record for factory storage
    // -----------------------------------------------------------------------

    /// <summary>
    /// Represents a factory-based agent registration (v0.7.1b).
    /// </summary>
    /// <param name="Configuration">The agent's configuration metadata.</param>
    /// <param name="Factory">The factory function to create agent instances.</param>
    /// <remarks>
    /// <para>
    /// This record is used internally by <see cref="AgentRegistry"/> to store
    /// factory-based registrations. Unlike v0.6.6c's metadata-only approach,
    /// this enables:
    /// </para>
    /// <list type="bullet">
    ///   <item>Lazy agent instantiation (only created when first accessed)</item>
    ///   <item>Singleton caching with hot-reload invalidation</item>
    ///   <item>Persona management without recreating instances</item>
    ///   <item>Configuration-first registration approach</item>
    /// </list>
    /// <para>
    /// <b>Lifetime:</b> The factory is invoked at most once per agent (cached),
    /// unless the agent configuration is updated via <see cref="AgentRegistry.UpdateAgent"/>,
    /// which invalidates the cache.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b as part of factory-based registration.
    /// </para>
    /// </remarks>
    private sealed record AgentRegistration(
        AgentConfiguration Configuration,
        Func<IServiceProvider, IAgent> Factory);
}
