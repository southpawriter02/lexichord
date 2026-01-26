# LCS-SBD: Scope Breakdown - v0.0.7

**Target Version:** `v0.0.7`
**Codename:** The Event Bus (Communication)
**Timeline:** Sprint 2 (Core Infrastructure)
**Owner:** Lead Architect
**Prerequisites:** v0.0.6d complete (Structured logging, configuration system).

## 1. Executive Summary

**v0.0.7** establishes the **inter-module communication foundation** for Lexichord. This release introduces MediatR as the in-process message bus, enabling Module A to communicate with Module B without direct project references. The success of this release is measured by:

1. MediatR is installed and registered in the DI container.
2. Shared domain events (`ContentCreatedEvent`, `SettingsChangedEvent`) are defined in Abstractions.
3. A logging pipeline behavior auto-logs every command/query.
4. A validation pipeline behavior integrates FluentValidation for request validation.

If this foundation is flawed, the modular architecture cannot achieve loose coupling—modules would need hard references to each other, defeating the plugin system.

---

## 2. The Problem Statement

### 2.1 Why Module Communication Matters

Lexichord's architecture separates concerns into modules:

- **Module.Documents** — Manages documents, projects, chapters.
- **Module.StyleGovernance** — Enforces writing style rules.
- **Module.RAG** — Handles memory and retrieval-augmented generation.
- **Module.Agents** — Orchestrates AI agents.

These modules need to communicate:

- When a document is created, the RAG module needs to index it.
- When settings change, multiple modules may need to react.
- When an agent completes, the documents module may need to update.

**Without an event bus:**

```text
Module.Documents ──────────────────────> Module.RAG (hard reference)
        │
        └──────────────────────> Module.Agents (hard reference)
```

This creates tight coupling and circular dependency nightmares.

**With MediatR event bus:**

```text
Module.Documents ──────> IMediator.Publish(ContentCreatedEvent)
                                    │
        ┌───────────────────────────┴───────────────────────────┐
        │                           │                           │
        ▼                           ▼                           ▼
   RAG Handler               Agent Handler              Analytics Handler
```

Modules only depend on `Lexichord.Abstractions` (which contains the event contracts), not on each other.

---

## 3. Sub-Part Specifications

### v0.0.7a: MediatR Setup

**Goal:** Install MediatR and configure it in the DI container with proper assembly scanning.

- **Task 1.1: Package Installation**
    - Add `MediatR` NuGet package to `Lexichord.Abstractions` (interfaces only).
    - Add `MediatR` NuGet package to `Lexichord.Host` (implementation and DI).
    - Pin version in `Directory.Build.props` for consistency.
- **Task 1.2: DI Registration**
    - Create `AddMediatRServices()` extension method in `Lexichord.Host`.
    - Configure assembly scanning for handlers across all loaded module assemblies.
    - Register pipeline behaviors in correct order (Logging -> Validation -> Handler).
- **Task 1.3: Base Contracts**
    - Define `ICommand<TResponse>` marker interface in Abstractions.
    - Define `IQuery<TResponse>` marker interface in Abstractions.
    - Define `IDomainEvent` marker interface in Abstractions.

**Definition of Done:**

- `IMediator` can be resolved from the DI container.
- A test handler responds to a test command.
- Assembly scanning discovers handlers from Host and future modules.

---

### v0.0.7b: Shared Domain Events

**Goal:** Define core domain events that modules can publish and subscribe to.

- **Task 1.1: ContentCreatedEvent**
    - Define `ContentCreatedEvent` record in `Lexichord.Abstractions.Events`.
    - Properties: `ContentId`, `ContentType`, `Title`, `CreatedAt`, `CreatedBy`.
    - Document the event's purpose and expected handlers.
- **Task 1.2: SettingsChangedEvent**
    - Define `SettingsChangedEvent` record in `Lexichord.Abstractions.Events`.
    - Properties: `SettingKey`, `OldValue`, `NewValue`, `ChangedAt`, `ChangedBy`.
    - Document cascading effects and handler responsibilities.
- **Task 1.3: Event Base Class**
    - Create `DomainEventBase` abstract record with common properties.
    - Properties: `EventId` (Guid), `OccurredAt` (DateTimeOffset), `CorrelationId`.
    - Ensure all events inherit from this base.

**Definition of Done:**

- Events compile and can be instantiated.
- Events implement `INotification` (MediatR's publish/subscribe interface).
- Event documentation explains when to publish and expected handlers.

---

### v0.0.7c: Logging Pipeline Behavior

**Goal:** Implement a pipeline behavior that automatically logs every command and query.

- **Task 1.1: LoggingBehavior Implementation**
    - Create `LoggingBehavior<TRequest, TResponse>` implementing `IPipelineBehavior`.
    - Log request start with request type, correlation ID, and relevant properties.
    - Log request completion with duration and success/failure status.
    - Use structured logging with proper log levels (Debug for start, Info for completion).
- **Task 1.2: Sensitive Data Handling**
    - Create `[SensitiveData]` attribute for properties that should not be logged.
    - Implement property filtering in logging behavior.
    - Document which properties should be marked sensitive.
- **Task 1.3: Performance Metrics**
    - Log execution time for all requests.
    - Add warning log if execution exceeds configurable threshold (default: 500ms).
    - Include request/response sizes in debug output.

**Definition of Done:**

- Every command/query automatically generates log entries.
- Sensitive data is redacted from logs.
- Slow requests generate warning logs.
- Unit tests verify logging behavior.

---

### v0.0.7d: Validation Pipeline Behavior

**Goal:** Implement a pipeline behavior that validates requests using FluentValidation.

- **Task 1.1: FluentValidation Setup**
    - Add `FluentValidation` NuGet package to Abstractions.
    - Add `FluentValidation.DependencyInjectionExtensions` to Host.
    - Configure automatic validator discovery via assembly scanning.
- **Task 1.2: ValidationBehavior Implementation**
    - Create `ValidationBehavior<TRequest, TResponse>` implementing `IPipelineBehavior`.
    - Collect all validators for the request type.
    - Execute validators and aggregate failures.
    - Throw `ValidationException` with all failures if validation fails.
- **Task 1.3: Validation Exception Handling**
    - Create `ValidationException` class with structured error details.
    - Create `ValidationError` record with `PropertyName`, `ErrorMessage`, `ErrorCode`.
    - Document how handlers should respond to validation failures.
- **Task 1.4: Sample Validator**
    - Create example validator for a sample command.
    - Demonstrate validation rules (NotEmpty, MaxLength, Must, etc.).
    - Write unit tests for the validator.

**Definition of Done:**

- Invalid requests are rejected before reaching handlers.
- Validation errors include property names and messages.
- Validators are automatically discovered and registered.
- Unit tests verify validation behavior.

---

## 4. Implementation Checklist (for Developer)

| Step     | Description                                                            | Status |
| :------- | :--------------------------------------------------------------------- | :----- |
| **0.7a** | MediatR NuGet package added to Abstractions and Host.                  | [ ]    |
| **0.7a** | `AddMediatRServices()` extension method created.                       | [ ]    |
| **0.7a** | `ICommand<T>`, `IQuery<T>`, `IDomainEvent` interfaces defined.         | [ ]    |
| **0.7a** | Assembly scanning configured for handler discovery.                    | [ ]    |
| **0.7b** | `DomainEventBase` abstract record created.                             | [ ]    |
| **0.7b** | `ContentCreatedEvent` defined with all properties.                     | [ ]    |
| **0.7b** | `SettingsChangedEvent` defined with all properties.                    | [ ]    |
| **0.7c** | `LoggingBehavior<TRequest, TResponse>` implemented.                    | [ ]    |
| **0.7c** | `[SensitiveData]` attribute created and honored.                       | [ ]    |
| **0.7c** | Slow request warning threshold configurable.                           | [ ]    |
| **0.7d** | FluentValidation packages added.                                       | [ ]    |
| **0.7d** | `ValidationBehavior<TRequest, TResponse>` implemented.                 | [ ]    |
| **0.7d** | `ValidationException` with structured errors created.                  | [ ]    |
| **0.7d** | Sample validator demonstrates FluentValidation rules.                  | [ ]    |

---

## 5. Risks & Mitigations

| Risk                                        | Impact | Mitigation                                                              |
| :------------------------------------------ | :----- | :---------------------------------------------------------------------- |
| Pipeline behavior order incorrect           | High   | Explicitly order behaviors: Logging -> Validation -> Handler.           |
| Assembly scanning misses module handlers    | High   | Test with mock modules; document required assembly naming conventions.  |
| Validation throws in async context          | Medium | Use async validators properly; test with async operations.              |
| Logging impacts performance                 | Low    | Use structured logging; lazy property evaluation; configurable levels.  |
| Circular event handling                     | Medium | Document event handling best practices; consider event idempotency.     |
| MediatR version conflicts with modules      | High   | Pin version in Directory.Build.props; use central package management.   |

---

## 6. Success Metrics

| Metric                          | Target                                   |
| :------------------------------ | :--------------------------------------- |
| Test command handled            | Handler receives command via IMediator   |
| Test event published            | Multiple handlers receive notification   |
| Logging behavior active         | All requests generate log entries        |
| Validation behavior active      | Invalid requests rejected with errors    |
| Handler discovery               | Handlers from multiple assemblies found  |
| Pipeline execution order        | Logging -> Validation -> Handler         |

---

## 7. Dependencies Graph

```text
v0.0.7a (MediatR Setup)
    │
    ├──> v0.0.7b (Shared Events)
    │        │
    │        └──> v0.0.7c (Logging Behavior)
    │                 │
    │                 └──> v0.0.7d (Validation Behavior)
    │
    └──> Depends on: v0.0.6 (Logging infrastructure)
```

Each sub-part builds on the previous. Validation (v0.0.7d) depends on the core MediatR setup (v0.0.7a) and benefits from logging (v0.0.7c) being in place to log validation failures.
