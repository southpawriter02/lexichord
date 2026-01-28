# LCS-DES-071-INDEX: The Agent Registry - Design Specification Index

## Document Control

| Field            | Value                                   |
| :--------------- | :-------------------------------------- |
| **Document ID**  | LCS-DES-071-INDEX                       |
| **Feature ID**   | AGT-071                                 |
| **Version**      | v0.7.1                                  |
| **Module Scope** | Lexichord.Modules.Agents / Abstractions |
| **Swimlane**     | Agents                                  |
| **Codename**     | The Agent Registry                      |
| **License Tier** | Core (base), WriterPro (specialists)    |
| **Feature Gate** | `Feature.AgentRegistry`                 |
| **Status**       | Draft                                   |
| **Last Updated** | 2026-01-28                              |

---

## 1. Overview

### 1.1 Problem Statement

The current agent system (v0.6.6) provides a single, general-purpose Co-pilot agent. Writers need specialized agents that understand specific writing contexts (editing, research, structure analysis), and the ability to switch between agent personalities without losing conversation context.

### 1.2 Solution Summary

Version 0.7.1 introduces the **Agent Registry**, a configuration-driven system that:

1. **Defines Agent Models:** `AgentConfiguration` and `AgentPersona` records establish the data contracts for agent identity, capabilities, and personality variants.

2. **Manages Agent Instances:** Extended `IAgentRegistry` provides singleton caching, runtime persona switching, and assembly scanning for agent discovery.

3. **Supports YAML Configuration:** Agents can be defined via YAML files loaded from embedded resources (built-in) or workspace directories (custom), with hot-reload on file changes.

4. **Provides Agent Selection UI:** An intuitive dropdown in the Co-pilot panel enables agent discovery, persona switching, and favorite management.

### 1.3 Business Value

- **Specialization:** Purpose-built agents deliver better results for specific writing tasks.
- **Personalization:** Persona variants allow writers to adjust agent behavior to their preferences.
- **Extensibility:** Teams can create custom agents tailored to their projects.
- **Monetization:** Tiered access to specialist agents drives subscription upgrades.

---

## 2. Sub-Part Index

| Sub-Part | Document ID  | Title                         | License Tier | Est. Hours |
| :------- | :----------- | :---------------------------- | :----------- | :--------- |
| v0.7.1a  | LCS-DES-071a | Agent Configuration Model     | Core         | 4          |
| v0.7.1b  | LCS-DES-071b | Agent Registry Implementation | Core         | 8          |
| v0.7.1c  | LCS-DES-071c | Agent Configuration Files     | WriterPro    | 10         |
| v0.7.1d  | LCS-DES-071d | Agent Selector UI             | Core         | 12         |

**Total Estimated Effort:** 34 hours

### Sub-Part Links

- [LCS-DES-071a.md](LCS-DES-071a.md) - Agent Configuration Model
- [LCS-DES-071b.md](LCS-DES-071b.md) - Agent Registry Implementation
- [LCS-DES-071c.md](LCS-DES-071c.md) - Agent Configuration Files
- [LCS-DES-071d.md](LCS-DES-071d.md) - Agent Selector UI

---

## 3. Dependency Graph

```mermaid
graph TB
    subgraph "v0.7.1 Agent Registry"
        A[v0.7.1a<br/>Configuration Model]
        B[v0.7.1b<br/>Registry Implementation]
        C[v0.7.1c<br/>Configuration Files]
        D[v0.7.1d<br/>Agent Selector UI]
    end

    subgraph "v0.6.x Agents Foundation"
        E[v0.6.6a<br/>IAgent]
        F[v0.6.6c<br/>IAgentRegistry Base]
        G[v0.6.1a<br/>IChatCompletionService]
        H[v0.6.3b<br/>IPromptRenderer]
    end

    subgraph "v0.1.x Infrastructure"
        I[v0.1.2b<br/>IRobustFileSystemWatcher]
        J[v0.1.6a<br/>ISettingsService]
    end

    subgraph "v0.0.x Foundation"
        K[v0.0.4c<br/>ILicenseContext]
        L[v0.0.7a<br/>IMediator]
        M[v0.0.3b<br/>ILogger]
    end

    A --> B
    B --> C
    B --> D
    C --> D

    B --> E
    B --> F
    B --> G
    B --> H
    C --> I
    D --> J
    B --> K
    B --> L
    B --> M

    classDef current fill:#C8E6C9,stroke:#388E3C
    classDef agents fill:#BBDEFB,stroke:#1976D2
    classDef infra fill:#FFE0B2,stroke:#F57C00
    classDef foundation fill:#E1BEE7,stroke:#7B1FA2

    class A,B,C,D current
    class E,F,G,H agents
    class I,J infra
    class K,L,M foundation
```

---

## 4. Interface Summary

### 4.1 New Interfaces

| Interface               | Module         | Purpose                      |
| :---------------------- | :------------- | :--------------------------- |
| `IAgentRegistry` (ext.) | Abstractions   | Agent and persona management |
| `IAgentConfigLoader`    | Modules.Agents | YAML configuration loading   |

### 4.2 New Records

| Record                        | Module         | Purpose                     |
| :---------------------------- | :------------- | :-------------------------- |
| `AgentConfiguration`          | Abstractions   | Agent identity and settings |
| `AgentPersona`                | Abstractions   | Persona variant definition  |
| `AgentConfigValidationResult` | Modules.Agents | YAML validation result      |

### 4.3 New Enums

| Enum                | Module       | Values                                                                                           |
| :------------------ | :----------- | :----------------------------------------------------------------------------------------------- |
| `AgentCapabilities` | Abstractions | None, Chat, DocumentContext, StyleEnforcement, CodeGeneration, ResearchAssistance, Summarization |

### 4.4 New Attributes

| Attribute                  | Module       | Purpose                         |
| :------------------------- | :----------- | :------------------------------ |
| `AgentDefinitionAttribute` | Abstractions | Marks classes for assembly scan |

### 4.5 New MediatR Events

| Event                      | Module       | When Published                 |
| :------------------------- | :----------- | :----------------------------- |
| `AgentRegisteredEvent`     | Abstractions | Agent registered with registry |
| `PersonaSwitchedEvent`     | Abstractions | User switches active persona   |
| `AgentConfigReloadedEvent` | Abstractions | YAML file hot-reloaded         |

---

## 5. Data Flow Overview

### 5.1 Agent Registration Flow

```mermaid
sequenceDiagram
    participant Host as Host Application
    participant Mod as AgentsModule
    participant Load as IAgentConfigLoader
    participant Reg as IAgentRegistry
    participant Med as IMediator

    Host->>Mod: InitializeAsync()
    Mod->>Load: LoadBuiltInAgentsAsync()
    Load-->>Mod: List<AgentConfiguration>

    loop For each configuration
        Mod->>Reg: RegisterAgent(config, factory)
        Reg->>Med: Publish(AgentRegisteredEvent)
    end

    Mod->>Load: LoadWorkspaceAgentsAsync(path)
    Load-->>Mod: List<AgentConfiguration>

    loop For each custom agent
        Mod->>Reg: RegisterAgent(config, factory)
        Reg->>Med: Publish(AgentRegisteredEvent)
    end
```

### 5.2 Persona Switching Flow

```mermaid
sequenceDiagram
    participant User
    participant UI as AgentSelectorView
    participant VM as AgentSelectorViewModel
    participant Reg as IAgentRegistry
    participant Med as IMediator
    participant Agent as IAgent

    User->>UI: Select persona
    UI->>VM: SwitchPersonaCommand.Execute()
    VM->>Reg: SwitchPersona(agentId, personaId)
    Reg->>Agent: ApplyPersona(persona)
    Reg->>Med: Publish(PersonaSwitchedEvent)
    Med-->>VM: Handle(PersonaSwitchedEvent)
    VM-->>UI: Update display
```

### 5.3 Configuration Hot-Reload Flow

```mermaid
sequenceDiagram
    participant FS as FileSystem
    participant Watch as IRobustFileSystemWatcher
    participant Load as IAgentConfigLoader
    participant Reg as IAgentRegistry
    participant Med as IMediator
    participant UI as AgentSelectorView

    FS->>Watch: File changed
    Watch->>Load: OnFileChanged(path)
    Load->>Load: Validate(yamlContent)

    alt Valid YAML
        Load->>Reg: UpdateAgent(config)
        Reg->>Med: Publish(AgentConfigReloadedEvent)
        Med-->>UI: Refresh agent list
    else Invalid YAML
        Load->>Load: Log validation errors
    end
```

---

## 6. Architecture Overview

```mermaid
graph TB
    subgraph "Presentation Layer"
        ASV[AgentSelectorView.axaml]
        AVM[AgentSelectorViewModel]
        AIV[AgentItemViewModel]
        PIV[PersonaItemViewModel]
    end

    subgraph "Application Layer"
        REG[AgentRegistry]
        LOAD[YamlAgentConfigLoader]
        VAL[AgentConfigValidator]
    end

    subgraph "Domain Layer"
        CFG[AgentConfiguration]
        PER[AgentPersona]
        CAP[AgentCapabilities]
        EVT[MediatR Events]
    end

    subgraph "Infrastructure Layer"
        FSW[IRobustFileSystemWatcher]
        SET[ISettingsService]
        LIC[ILicenseContext]
        LOG[ILogger]
        MED[IMediator]
    end

    subgraph "Storage"
        EMB[Embedded YAML Resources]
        WSP[.lexichord/agents/*.yaml]
        FAV[User Settings - Favorites]
    end

    ASV --> AVM
    AVM --> AIV
    AIV --> PIV
    AVM --> REG
    AVM --> SET
    AVM --> LIC

    REG --> LOAD
    REG --> CFG
    REG --> EVT
    REG --> LIC
    REG --> LOG
    REG --> MED

    LOAD --> VAL
    LOAD --> FSW
    LOAD --> EMB
    LOAD --> WSP

    CFG --> PER
    CFG --> CAP

    SET --> FAV

    classDef presentation fill:#E1BEE7,stroke:#7B1FA2
    classDef application fill:#BBDEFB,stroke:#1976D2
    classDef domain fill:#C8E6C9,stroke:#388E3C
    classDef infra fill:#FFE0B2,stroke:#F57C00
    classDef storage fill:#ECEFF1,stroke:#607D8B

    class ASV,AVM,AIV,PIV presentation
    class REG,LOAD,VAL application
    class CFG,PER,CAP,EVT domain
    class FSW,SET,LIC,LOG,MED infra
    class EMB,WSP,FAV storage
```

---

## 7. License Gating Strategy

### 7.1 Behavior by Tier

| Tier       | Built-in Agents | Custom Agents | Personas | Features               |
| :--------- | :-------------- | :------------ | :------- | :--------------------- |
| Core       | General Chat    | ❌            | Default  | Basic chat             |
| Writer     | General Chat    | ❌            | Default  | Basic chat             |
| WriterPro  | All specialists | ✅ Read-only  | All      | Full specialist access |
| Teams      | All specialists | ✅ Read/Write | All      | Team agent sharing     |
| Enterprise | All specialists | ✅ Read/Write | All      | SSO + Admin controls   |

### 7.2 Feature Gate Keys

| Feature Gate Key           | Min Tier  | Description                  |
| :------------------------- | :-------- | :--------------------------- |
| `Feature.AgentRegistry`    | Core      | Base registry functionality  |
| `Feature.SpecialistAgents` | WriterPro | Access to specialist agents  |
| `Feature.CustomAgents`     | WriterPro | Load workspace agents (read) |
| `Feature.CustomAgentsEdit` | Teams     | Create/edit workspace agents |
| `Feature.AgentSharing`     | Teams     | Share agents across team     |

---

## 8. DI Registration

```csharp
// Lexichord.Modules.Agents/DependencyInjection.cs
public static class AgentsDependencyInjection
{
    public static IServiceCollection AddAgentRegistry(this IServiceCollection services)
    {
        // Core registry
        services.AddSingleton<IAgentRegistry, AgentRegistry>();

        // Configuration loading
        services.AddSingleton<IAgentConfigLoader, YamlAgentConfigLoader>();
        services.AddSingleton<IAgentConfigValidator, AgentConfigValidator>();

        // ViewModels
        services.AddTransient<AgentSelectorViewModel>();
        services.AddTransient<AgentItemViewModel>();
        services.AddTransient<PersonaItemViewModel>();

        // MediatR handlers
        services.AddTransient<INotificationHandler<AgentRegisteredEvent>,
            AgentRegisteredEventHandler>();
        services.AddTransient<INotificationHandler<PersonaSwitchedEvent>,
            PersonaSwitchedEventHandler>();
        services.AddTransient<INotificationHandler<AgentConfigReloadedEvent>,
            AgentConfigReloadedEventHandler>();

        return services;
    }
}
```

---

## 9. Test Coverage Summary

| Sub-Part | Test Class                    | Test Count | Focus Areas                      |
| :------- | :---------------------------- | :--------- | :------------------------------- |
| v0.7.1a  | `AgentConfigurationTests`     | 8          | Record equality, validation      |
| v0.7.1a  | `AgentPersonaTests`           | 6          | Persona defaults, overrides      |
| v0.7.1a  | `AgentCapabilitiesTests`      | 4          | Flag combinations                |
| v0.7.1b  | `AgentRegistryTests`          | 15         | Registration, caching, switching |
| v0.7.1b  | `AgentDefinitionScannerTests` | 5          | Assembly attribute scanning      |
| v0.7.1c  | `YamlAgentConfigLoaderTests`  | 12         | Parsing, validation, errors      |
| v0.7.1c  | `AgentConfigHotReloadTests`   | 6          | File watcher integration         |
| v0.7.1d  | `AgentSelectorViewModelTests` | 10         | Selection, filtering, favorites  |
| v0.7.1d  | `AgentItemViewModelTests`     | 6          | Tier display, lock state         |

**Total Tests:** 72+  
**Target Coverage:** ≥ 90%

---

## 10. Success Criteria Summary

| Criterion                | Target             | Measurement            |
| :----------------------- | :----------------- | :--------------------- |
| Agent Load Time          | < 100ms            | Performance benchmark  |
| Configuration Validation | 100% schema errors | Unit test suite        |
| Hot Reload Latency       | < 500ms            | Integration test       |
| Agent Discovery Time     | < 50ms             | UI responsiveness test |
| Unit Test Coverage       | ≥ 90%              | Coverage report        |
| Zero Regressions         | 0 failures         | Existing test suite    |

---

## 11. What This Enables

### 11.1 Immediate Dependencies (v0.7.2+)

| Version | Feature               | Uses From v0.7.1                       |
| :------ | :-------------------- | :------------------------------------- |
| v0.7.2  | Task Specialists      | `AgentConfiguration`, `IAgentRegistry` |
| v0.7.3  | Agent Memory          | `AgentPersona` context keys            |
| v0.7.4  | Agent Tools           | `AgentCapabilities` flags              |
| v0.7.5  | Multi-Agent Workflows | Registry + persona switching           |

### 11.2 Long-Term Vision

The Agent Registry establishes the foundation for a rich ecosystem of specialized writing assistants. Future versions will add:

- **Agent Memory:** Persistent context that survives sessions.
- **Agent Tools:** External integrations (web search, database queries).
- **Agent Marketplace:** Community-contributed agent definitions.
- **Multi-Agent Orchestration:** Coordinated workflows across specialists.

---

## 12. Related Documents

### 12.1 Parent Documents

- [LCS-SBD-071.md](LCS-SBD-071.md) - Scope Breakdown Document
- [roadmap-v0.7.x.md](../roadmap-v0.7.x.md) - Version 0.7.x Roadmap

### 12.2 Sub-Part Specifications

- [LCS-DES-071a.md](LCS-DES-071a.md) - Agent Configuration Model
- [LCS-DES-071b.md](LCS-DES-071b.md) - Agent Registry Implementation
- [LCS-DES-071c.md](LCS-DES-071c.md) - Agent Configuration Files
- [LCS-DES-071d.md](LCS-DES-071d.md) - Agent Selector UI

### 12.3 Dependency Documents

- [LCS-DES-066a.md](../../v0.6.x/v0.6.6/LCS-DES-066a.md) - IAgent Interface (v0.6.6a)
- [LCS-DES-066c.md](../../v0.6.x/v0.6.6/LCS-DES-066c.md) - IAgentRegistry Base (v0.6.6c)
- [LCS-DES-061-INDEX.md](../../v0.6.x/v0.6.1/LCS-DES-061-INDEX.md) - Gateway (v0.6.1)

### 12.4 Reference Documents

- [DEPENDENCY-MATRIX.md](../../DEPENDENCY-MATRIX.md) - Interface Registry
- [SUBSCRIPTION_MATRIX.md](../../SUBSCRIPTION_MATRIX.md) - License Tier Reference

---

## Document History

| Version | Date       | Author | Changes       |
| :------ | :--------- | :----- | :------------ |
| 0.1     | 2026-01-28 | System | Initial draft |
