# v0.6.6c — Agent Registry

**Phase:** Agent Discovery & Management  
**Status:** Complete  
**Date:** 2026-02-09  

---

## Summary

Implements the `AgentRegistry` — a singleton service for agent discovery, license-based filtering, default agent selection, custom agent registration (Teams-only), and automatic refresh on license changes. Provides the `IAgentRegistry` interface and supporting abstractions for agent lifecycle management.

## New Files

### Abstractions (`src/Lexichord.Abstractions/Agents/`)

| File | Description |
|------|-------------|
| `IAgentRegistry.cs` | Registry interface with `AvailableAgents`, `GetAgent`, `TryGetAgent`, `GetDefaultAgent`, `RegisterCustomAgent`, `UnregisterCustomAgent`, `Refresh`, and `AgentsChanged` event |
| `AgentListChangedEventArgs.cs` | Event args carrying `Reason` (enum) and `AvailableAgents` list |
| `AgentListChangeReason.cs` | Enum: `LicenseChanged`, `AgentRegistered`, `AgentUnregistered`, `ManualRefresh` |
| `AgentNotFoundException.cs` | Thrown when agent ID is not found in the registry |
| `NoAgentAvailableException.cs` | Thrown when no agents are accessible after exhausting fallback chain |
| `AgentAlreadyRegisteredException.cs` | Thrown when registering an agent with a duplicate ID |
| `LicenseTierException.cs` | Thrown when license tier is insufficient, includes `RequiredTier` property |

### Implementation (`src/Lexichord.Modules.Agents/Chat/Registry/`)

| File | Description |
|------|-------------|
| `AgentMetadata.cs` | Immutable record caching agent info (ID, Name, Description, Capabilities, RequiredLicense, AgentType) to avoid repeated reflection |
| `AgentDiscovery.cs` | Static discovery helper scanning DI for `IAgent` implementations and extracting `RequiresLicenseAttribute` metadata |
| `AgentRegistry.cs` | Singleton `IAgentRegistry` implementation with `ConcurrentDictionary`-based thread safety, license filtering via `GetCurrentTier()`, custom agent persistence via `ISettingsService`, and `LicenseChanged` event subscription |
| `CustomAgentDefinition.cs` | Record for serializing custom agent configs to user settings |

### Tests (`tests/Lexichord.Tests.Unit/Modules/Agents/Chat/Registry/`)

| File | Tests |
|------|-------|
| `AgentRegistryTests.cs` | 11 |

## Modified Files

| File | Change |
|------|--------|
| `AgentsModule.cs` | Updated version to 0.6.6, added `services.AddAgentRegistry()`, added registry verification in `InitializeAsync` |
| `Extensions/AgentsServiceCollectionExtensions.cs` | Added `AddAgentRegistry()` extension registering `AgentRegistry` as singleton |
| `Abstractions/Contracts/ILicenseContext.cs` | Added `Tier` default property and `LicenseChanged` event |
| `Abstractions/Contracts/ISettingsService.cs` | Added `Set<T>(string key, T value)` method |
| `Abstractions/Contracts/ILicenseService.cs` | Added `new` keyword to `LicenseChanged` to fix CS0108 hiding |
| `Host/Services/HardcodedLicenseContext.cs` | Added `LicenseChanged` event stub (pragma-suppressed CS0067) |

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Singleton lifetime | Shared caching and license event subscription across the application |
| `GetCurrentTier()` over `Tier` property | Default interface members don't delegate correctly through Moq proxies |
| Concrete test agents with `[RequiresLicense]` | Moq proxy types don't carry custom attributes; concrete stubs needed for reflection-based filtering |
| Teams-only custom agent registration | Matches established license gating pattern from v0.6.5d |
| Default agent fallback chain | Settings → co-pilot → first available → throw `NoAgentAvailableException` |
| `ConcurrentDictionary` + lock for refresh | Thread-safe reads with atomic cache invalidation on license changes |

## Verification

- **Build:** 0 errors, 0 warnings across all projects
- **Tests:** 11/11 passed (0 failed, 0 skipped)
- **Regression:** 8045/8045 passed (33 skipped, 0 failed)
