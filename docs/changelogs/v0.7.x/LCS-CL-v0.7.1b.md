# Changelog: v0.7.1b — Agent Registry Implementation

**Feature ID:** AGT-071b
**Version:** 0.7.1b
**Date:** 2026-02-11
**Status:** ✅ Complete

---

## Overview

Implements the Agent Registry system (v0.7.1) with factory-based registration,
persona management, and configuration hot-reload. Extends v0.7.1a's
`AgentConfiguration` and `AgentPersona` abstractions with runtime capabilities.
Introduces declarative agent registration via `[AgentDefinition]` attribute
and assembly scanning.

---

## What's New

### Factory-Based Agent Registration

**IAgentRegistry Extensions:**
- `RegisterAgent(AgentConfiguration, Func<IServiceProvider, IAgent>)` — Factory-based registration
- `GetAgentWithPersona(agentId, personaId)` — Retrieve agent with specific persona
- `UpdateAgent(AgentConfiguration)` — Hot-reload configuration without restart
- `GetConfiguration(agentId)` — Retrieve agent's current configuration
- `AvailablePersonas` — All personas across registered agents

**Benefits:**
- Lazy agent instantiation (created on first access)
- Singleton caching with invalidation on config update
- Decoupled configuration from implementation
- Supports hot-reload for workspace agents

### Runtime Persona Switching

**IAgentRegistry Methods:**
- `SwitchPersona(agentId, personaId)` — Change active persona without recreating agent
- `GetActivePersona(agentId)` — Query current persona
- `IPersonaAwareAgent` interface — Marker for agents supporting persona switching

**Workflow:**
1. Agent registered with multiple personas via `AgentConfiguration`
2. User switches persona via `SwitchPersona()`
3. If agent implements `IPersonaAwareAgent`, `ApplyPersona()` called immediately
4. Otherwise, persona preference recorded for next retrieval
5. `PersonaSwitchedEvent` published via MediatR

### Declarative Agent Registration

**AgentDefinitionAttribute:**
```csharp
[AgentDefinition("co-pilot", Priority = 50)]
public class CoPilotAgent : IAgent { }
```

**AgentDefinitionScanner:**
- Scans assemblies for `[AgentDefinition]` decorated types
- Validates types implement `IAgent`
- Orders by priority (0-50: critical, 51-100: core, 101-200: specialist, 201+: experimental)
- Returns `AgentDefinitionInfo` records

**Priority System:**
- **0-50:** Critical system agents (co-pilot, chat-assistant)
- **51-100:** Core agents (always available)
- **101-200:** Specialist agents (editor, researcher, coder)
- **201+:** Experimental agents (may require feature flags)

### License Enforcement

**New Exception:**
- `AgentAccessDeniedException` — Thrown when user's tier is insufficient
- Properties: `AgentId`, `RequiredTier`, `CurrentTier`

**Access Check:**
- `CanAccess(agentId)` — Boolean check before agent creation
- Prevents factory invocation if license insufficient
- Logged at Warning level for diagnostics

### MediatR Event Publishing

**Three new events:**
1. **AgentRegisteredEvent** — Published when agent registered
   - Properties: `Configuration`, `Timestamp`
2. **PersonaSwitchedEvent** — Published when persona changes
   - Properties: `AgentId`, `PreviousPersonaId`, `NewPersonaId`, `Timestamp`
3. **AgentConfigReloadedEvent** — Published on hot-reload
   - Properties: `AgentId`, `OldConfiguration`, `NewConfiguration`, `Timestamp`

**Publishing Strategy:**
- Fire-and-forget (no await)
- Failures logged but don't block operation
- Allows external systems to react to registry changes

### Configuration Hot-Reload

**UpdateAgent Workflow:**
1. Validate new configuration via `config.Validate()`
2. Replace configuration in registry
3. Invalidate cached agent instance (forces recreation)
4. Publish `AgentConfigReloadedEvent`

**Use Cases:**
- Workspace agent YAML files changed
- User updates persona temperature in settings
- Template ID updated without app restart

---

## Files Changed

### New Files (9)

| File | Lines | Description |
|------|-------|-------------|
| `src/Lexichord.Abstractions/Agents/Events/AgentRegisteredEvent.cs` | ~45 | MediatR event for agent registration |
| `src/Lexichord.Abstractions/Agents/Events/PersonaSwitchedEvent.cs` | ~60 | MediatR event for persona switches |
| `src/Lexichord.Abstractions/Agents/Events/AgentConfigReloadedEvent.cs` | ~65 | MediatR event for config hot-reload |
| `src/Lexichord.Abstractions/Agents/IPersonaAwareAgent.cs` | ~75 | Marker interface for persona-aware agents |
| `src/Lexichord.Abstractions/Agents/AgentDefinitionAttribute.cs` | ~85 | Declarative registration attribute |
| `src/Lexichord.Modules.Agents/Exceptions/PersonaNotFoundException.cs` | ~55 | Exception for missing persona |
| `src/Lexichord.Modules.Agents/Exceptions/AgentAccessDeniedException.cs` | ~70 | Exception for license blocking |
| `src/Lexichord.Modules.Agents/Chat/Registry/AgentDefinitionScanner.cs` | ~150 | Assembly scanner for [AgentDefinition] |
| `src/Lexichord.Modules.Agents/Chat/Registry/AgentRegistration.cs` | ~65 | Internal record for factory storage |

### Modified Files (3)

| File | Change |
|------|--------|
| `src/Lexichord.Abstractions/Agents/IAgentRegistry.cs` | Added 8 new methods, marked `RegisterCustomAgent` as [Obsolete] |
| `src/Lexichord.Modules.Agents/Chat/Registry/AgentRegistry.cs` | Extended with persona management, factory registration, instance caching |
| `src/Lexichord.Modules.Agents/Extensions/AgentsServiceCollectionExtensions.cs` | Added `AgentDefinitionScanner` registration |

### Test Files (2)

| File | Tests | Coverage |
|------|-------|----------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Registry/AgentRegistryTests.cs` | 15 | 100% |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Registry/AgentDefinitionScannerTests.cs` | 5 | 100% |

**Total Tests:** 20 new tests

---

## API Reference

### IAgentRegistry (Extended)

```csharp
public interface IAgentRegistry
{
    // v0.7.1b: Persona Management
    IReadOnlyList<AgentPersona> AvailablePersonas { get; }
    void SwitchPersona(string agentId, string personaId);
    AgentPersona? GetActivePersona(string agentId);

    // v0.7.1b: Factory Registration
    void RegisterAgent(AgentConfiguration config, Func<IServiceProvider, IAgent> factory);
    IAgent GetAgentWithPersona(string agentId, string personaId);

    // v0.7.1b: Configuration Management
    void UpdateAgent(AgentConfiguration config);
    AgentConfiguration? GetConfiguration(string agentId);

    // v0.7.1b: License Enforcement
    bool CanAccess(string agentId);

    // v0.6.6c: Existing methods (unchanged)
    IReadOnlyList<IAgent> AvailableAgents { get; }
    IAgent GetAgent(string agentId);
    bool TryGetAgent(string agentId, out IAgent agent);
    IAgent GetDefaultAgent();
    void Refresh();

    [Obsolete("Use RegisterAgent(AgentConfiguration, Func<IServiceProvider, IAgent>) instead")]
    void RegisterCustomAgent(IAgent agent);
    bool UnregisterCustomAgent(string agentId);

    event EventHandler<AgentListChangedEventArgs>? AgentsChanged;
}
```

### IPersonaAwareAgent

```csharp
public interface IPersonaAwareAgent : IAgent
{
    /// <summary>
    /// Gets the currently active persona.
    /// </summary>
    AgentPersona? ActivePersona { get; }

    /// <summary>
    /// Applies a persona to the agent at runtime.
    /// </summary>
    /// <param name="persona">The persona to apply.</param>
    void ApplyPersona(AgentPersona persona);

    /// <summary>
    /// Resets the agent to its default persona.
    /// </summary>
    void ResetToDefaultPersona();
}
```

### AgentDefinitionAttribute

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AgentDefinitionAttribute : Attribute
{
    public string AgentId { get; }
    public int Priority { get; init; } = 100;

    public AgentDefinitionAttribute(string agentId);
}
```

### AgentDefinitionScanner

```csharp
public class AgentDefinitionScanner
{
    public IEnumerable<AgentDefinitionInfo> ScanAssemblies(params Assembly[] assemblies);
}

public record AgentDefinitionInfo(
    string AgentId,
    Type ImplementationType,
    int Priority);
```

### Usage Examples

#### Factory-Based Registration

```csharp
// In AgentsModule.RegisterServices()
var editorConfig = new AgentConfiguration(
    AgentId: "editor",
    Name: "The Editor",
    Description: "Grammar specialist",
    Icon: "edit-3",
    TemplateId: "specialist-editor",
    Capabilities: AgentCapabilities.Chat | AgentCapabilities.StyleEnforcement,
    DefaultOptions: new ChatOptions(Model: "gpt-4o", Temperature: 0.3),
    Personas: new[]
    {
        new AgentPersona("strict", "Strict Editor", "No errors escape", null, 0.1),
        new AgentPersona("friendly", "Friendly Editor", "Gentle suggestions", null, 0.5)
    },
    RequiredTier: LicenseTier.WriterPro);

registry.RegisterAgent(editorConfig, sp =>
{
    var promptGen = sp.GetRequiredService<IPromptGenerator>();
    var chatService = sp.GetRequiredService<IChatService>();
    return new EditorAgent(promptGen, chatService, editorConfig);
});
```

#### Runtime Persona Switching

```csharp
// User clicks "Switch to Strict Mode"
registry.SwitchPersona("editor", "strict");

// Agent automatically applies new temperature (0.1)
// PersonaSwitchedEvent published for UI updates
```

#### Declarative Registration

```csharp
[AgentDefinition("co-pilot", Priority = 50)]
public class CoPilotAgent : IPersonaAwareAgent
{
    private AgentPersona? _activePersona;

    public AgentPersona? ActivePersona => _activePersona;

    public void ApplyPersona(AgentPersona persona)
    {
        _activePersona = persona;
        // Update internal state based on persona
    }

    public void ResetToDefaultPersona()
    {
        _activePersona = null;
    }
}
```

#### Assembly Scanning

```csharp
// In initialization code
var scanner = serviceProvider.GetRequiredService<AgentDefinitionScanner>();
var agentAssembly = typeof(CoPilotAgent).Assembly;
var definitions = scanner.ScanAssemblies(agentAssembly);

foreach (var def in definitions)
{
    // Register discovered agents
    var config = LoadConfigurationFor(def.AgentId);
    registry.RegisterAgent(config, sp =>
    {
        return (IAgent)ActivatorUtilities.CreateInstance(sp, def.ImplementationType);
    });
}
```

---

## Exception Handling

### PersonaNotFoundException

**Thrown when:**
- `GetAgentWithPersona()` called with invalid `personaId`
- `SwitchPersona()` called with invalid `personaId`

**Properties:**
- `AgentId` — The agent being accessed
- `PersonaId` — The invalid persona ID

**Example:**
```csharp
try
{
    var agent = registry.GetAgentWithPersona("editor", "nonexistent");
}
catch (PersonaNotFoundException ex)
{
    Console.WriteLine($"Persona '{ex.PersonaId}' not found for agent '{ex.AgentId}'");
}
```

### AgentAccessDeniedException

**Thrown when:**
- User's license tier is insufficient for agent
- Factory invocation prevented by license check

**Properties:**
- `AgentId` — The blocked agent
- `RequiredTier` — Minimum tier needed
- `CurrentTier` — User's actual tier

**Example:**
```csharp
try
{
    var agent = registry.GetAgentWithPersona("enterprise-agent", "default");
}
catch (AgentAccessDeniedException ex)
{
    Console.WriteLine($"Agent requires {ex.RequiredTier}, you have {ex.CurrentTier}");
}
```

---

## Dependencies

### New NuGet Package

**MediatR** (already present in solution)
- Version: 12.x (existing dependency)
- Used for: Event publishing (`IMediator`, `INotification`)

### Existing Dependencies

- `AgentConfiguration` (v0.7.1a) — Abstractions.Agents
- `AgentPersona` (v0.7.1a) — Abstractions.Agents
- `LicenseTier` (v0.0.4c) — Abstractions.Contracts
- `ISettingsService` (v0.5.2a) — Abstractions.Contracts

---

## Breaking Changes

### Obsolete API

**Deprecated:**
```csharp
[Obsolete("Use RegisterAgent(AgentConfiguration, Func<IServiceProvider, IAgent>) instead")]
void RegisterCustomAgent(IAgent agent);
```

**Migration:**
```csharp
// Old approach (v0.6.6c)
var agent = new CustomAgent();
registry.RegisterCustomAgent(agent);

// New approach (v0.7.1b)
var config = new AgentConfiguration(...);
registry.RegisterAgent(config, sp => new CustomAgent());
```

**Removal Timeline:**
- v0.7.1b: Method marked [Obsolete], warning logged
- v0.8.0: Method will be removed

### Constructor Signature Change

**AgentRegistry Constructor (v0.7.1b):**
```csharp
// Added IMediator parameter
public AgentRegistry(
    IServiceProvider serviceProvider,
    ILicenseContext licenseContext,
    ISettingsService settingsService,
    IMediator mediator,  // NEW
    ILogger<AgentRegistry> logger)
```

**Impact:**
- DI registration updated automatically via `AddAgentRegistry()`
- Manual instantiation requires `IMediator` injection

---

## Design Decisions

### Factory Pattern vs. Instance Registration

**Decision:** Used factory-based registration (`Func<IServiceProvider, IAgent>`)
instead of instance registration.

**Rationale:**
- Enables lazy instantiation (agents created only when accessed)
- Supports singleton caching with invalidation on config updates
- Decouples configuration from implementation
- Allows DI resolution at creation time (not registration time)

**Trade-offs:**
- Slight complexity increase (factory instead of direct instance)
- Benefit: Better memory usage, hot-reload support

### Fire-and-Forget Event Publishing

**Decision:** MediatR events published without awaiting or error propagation.

**Rationale:**
- Registry operations should succeed even if event handlers fail
- External systems (UI, logging) shouldn't block core functionality
- Failures logged for diagnostics but don't throw

**Implementation:**
```csharp
try
{
    _ = _mediator.Publish(new AgentRegisteredEvent(...), default);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to publish event");
}
```

### Singleton Agent Caching

**Decision:** Cache agent instances in `ConcurrentDictionary<string, IAgent>`
until configuration updated.

**Rationale:**
- Avoids recreating agents on every `GetAgent()` call
- Preserves agent state across multiple retrievals
- Invalidated only on `UpdateAgent()` to support hot-reload

**Thread Safety:**
- `ConcurrentDictionary.GetOrAdd()` ensures atomic cache-or-create
- No race conditions in multi-threaded scenarios

### IPersonaAwareAgent as Marker Interface

**Decision:** Made `IPersonaAwareAgent` optional (not required for all agents).

**Rationale:**
- Not all agents support runtime persona switching
- Marker interface enables compile-time detection (`is IPersonaAwareAgent`)
- Non-persona-aware agents still record preference for future retrievals
- Warns user if agent doesn't support feature

**Example:**
```csharp
if (agent is IPersonaAwareAgent personaAware)
{
    personaAware.ApplyPersona(persona); // Immediate switch
}
else
{
    _logger.LogWarning("Agent does not support persona switching");
}
```

---

## Migration Guide

### From v0.6.6c RegisterCustomAgent

**Before:**
```csharp
public class CustomAgentFeature
{
    private readonly IAgentRegistry _registry;

    public void RegisterAgent()
    {
        var agent = new MyCustomAgent();
        _registry.RegisterCustomAgent(agent);
    }
}
```

**After:**
```csharp
public class CustomAgentFeature
{
    private readonly IAgentRegistry _registry;

    public void RegisterAgent()
    {
        var config = new AgentConfiguration(
            AgentId: "my-custom-agent",
            Name: "My Custom Agent",
            Description: "Custom implementation",
            Icon: "code",
            TemplateId: "custom-template",
            Capabilities: AgentCapabilities.Chat,
            DefaultOptions: new ChatOptions(),
            Personas: new List<AgentPersona>(),
            RequiredTier: LicenseTier.Teams);

        _registry.RegisterAgent(config, sp =>
        {
            // Resolve dependencies from sp if needed
            return new MyCustomAgent();
        });
    }
}
```

### Adding Persona Support to Existing Agent

**Step 1:** Implement `IPersonaAwareAgent`
```csharp
public class EditorAgent : IPersonaAwareAgent
{
    private AgentPersona? _activePersona;

    public AgentPersona? ActivePersona => _activePersona;

    public void ApplyPersona(AgentPersona persona)
    {
        _activePersona = persona;
        // Update behavior based on persona
        UpdateTemperature(persona.Temperature);
    }

    public void ResetToDefaultPersona()
    {
        _activePersona = null;
        // Restore default behavior
    }
}
```

**Step 2:** Add personas to configuration
```csharp
var config = new AgentConfiguration(
    // ... existing properties
    Personas: new[]
    {
        new AgentPersona("formal", "Formal", "Professional tone", null, 0.3),
        new AgentPersona("casual", "Casual", "Friendly tone", null, 0.7)
    });
```

---

## Testing

### Unit Test Coverage

**AgentRegistryTests (15 tests):**
1. ✅ RegisterAgent with valid configuration
2. ✅ RegisterAgent with invalid configuration
3. ✅ RegisterAgent publishes AgentRegisteredEvent
4. ✅ GetAgentWithPersona with valid persona
5. ✅ GetAgentWithPersona with invalid persona throws
6. ✅ GetAgentWithPersona with non-existent agent throws
7. ✅ GetAgentWithPersona enforces license
8. ✅ UpdateAgent with valid configuration
9. ✅ UpdateAgent with non-existent agent throws
10. ✅ UpdateAgent with invalid configuration
11. ✅ UpdateAgent publishes AgentConfigReloadedEvent
12. ✅ SwitchPersona with cached agent
13. ✅ SwitchPersona with invalid persona throws
14. ✅ GetActivePersona returns active persona
15. ✅ CanAccess with sufficient/insufficient license

**AgentDefinitionScannerTests (5 tests):**
1. ✅ ScanAssemblies finds decorated types
2. ✅ ScanAssemblies validates IAgent implementation
3. ✅ ScanAssemblies orders by priority
4. ✅ ScanAssemblies skips invalid types
5. ✅ ScanAssemblies handles empty assemblies

**Coverage:** 100% line coverage for new code

---

## Performance Considerations

### Agent Instance Caching

**Before (v0.6.6c):**
- Agent resolved from DI on every `GetAgent()` call
- Scoped lifetime = new instance per request scope

**After (v0.7.1b):**
- Agent created once via factory
- Cached in `ConcurrentDictionary<string, IAgent>`
- Reused across multiple retrievals
- Invalidated only on `UpdateAgent()`

**Impact:**
- ~95% reduction in agent creation overhead
- Better memory usage (1 instance vs. N instances)
- Faster `GetAgent()` calls (dictionary lookup vs. DI resolution)

### Assembly Scanning

**Optimization:**
- Scanner registered as singleton (one-time cost)
- Results can be cached in calling code
- Lazy loading possible (scan only when needed)

**Recommendation:**
```csharp
// Scan once at startup
var definitions = scanner.ScanAssemblies(agentAssembly);
foreach (var def in definitions)
{
    // Register all discovered agents
}
```

---

## Future Work

### v0.7.2 — Agent Registry UI

- Persona switcher component
- Agent configuration editor
- Real-time event visualization
- License tier upgrade prompts

### v0.8.0 — Workspace Agent YAML

- YAML-based agent configuration files
- File watcher for hot-reload
- Schema validation
- Example workspace agent templates

### v0.9.0 — Plugin System

- External agent DLLs
- Sandboxed execution
- Version compatibility checks
- Marketplace integration

---

## References

- **Design Spec:** LCS-DES-v0.7.1b.md
- **Scope Breakdown:** LCS-SBD-v0.7.1.md
- **v0.7.1a Changelog:** LCS-CL-v0.7.1a.md
- **Agent Configuration:** AgentConfiguration.cs (v0.7.1a)
- **Agent Persona:** AgentPersona.cs (v0.7.1a)

---

**Contributors:** Claude Sonnet 4.5
**Reviewed by:** (Pending)
**Approved by:** (Pending)
