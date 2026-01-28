# Lexichord Changelog

All notable changes to the Lexichord project are documented here.

This changelog is written for stakeholders and users, focusing on **what changed** rather than technical implementation details. For detailed technical changes, see the version-specific changelogs in this directory.

---

## [v0.0.6] - 2026-01-28 (In Progress)

### The Vault (Secure Secrets Storage)

This release establishes the secure secrets management infrastructure that protects sensitive credentials. Lexichord can now store API keys, connection strings, and OAuth tokens using OS-native encryption.

#### What's New

- **ISecureVault Interface** — Platform-agnostic contract for secure secret storage with CRUD operations, metadata access, and streaming key listing.

#### Why This Matters

This work enables:

1. **Security** — Credentials encrypted at rest using platform-native APIs.
2. **Cross-Platform** — Windows DPAPI, Linux/macOS libsecret support.
3. **Foundation** — Enables secure LLM API key storage (v0.3.x+).

---

## [v0.0.5] - 2026-01-28 (In Progress)

### The Memory (Data Layer)

This release establishes the persistent data layer that gives Lexichord its "memory." The application can now store and retrieve data across sessions using PostgreSQL.

#### What's New

- **Docker Orchestration** — A reproducible PostgreSQL 16 development environment via Docker Compose with health checks, data persistence, and optional pgAdmin administration UI.

- **Database Connector** — Npgsql-based connectivity with connection pooling and Polly resilience patterns (retry with exponential backoff, circuit breaker).

- **FluentMigrator Runner** — Database schema versioning with CLI commands (`--migrate`, `--migrate:down`, `--migrate:list`) and the initial `Migration_001_InitSystem` creating Users and SystemSettings tables.

- **Repository Base** — Generic repository pattern with Dapper for type-safe CRUD operations, including entity-specific repositories for Users and SystemSettings plus Unit of Work for transactions.

#### Why This Matters

This work enables:

1. **Data Persistence** — User data and settings survive application restarts.
2. **Schema Evolution** — Migrations enable safe database updates.
3. **Foundation** — Enables all data-dependent features (v0.0.6+).

---

## [v0.0.4] - 2026-01-28 (In Progress)

### The Module Protocol

This release establishes the module architecture that enables Lexichord's "modular monolith" — allowing features to be developed as independent modules while running in a single process.

#### What's New

- **Module Contract (`IModule`)** — A standardized interface defining how modules register services, initialize, and expose metadata.

- **Module Metadata (`ModuleInfo`)** — Immutable record containing module identity, version, author, and optional dependencies.

- **Module Loader (`IModuleLoader`)** — Discovers and loads modules from `./Modules/` at startup with two-phase loading (service registration before DI build, initialization after).

- **License Gate (Skeleton)** — Establishes license tier gating with `LicenseTier`, `ILicenseContext`, and `RequiresLicenseAttribute`. Stub implementation returns Core tier; v1.x will provide real validation.

- **Sandbox Module (Proof of Concept)** — First feature module validating the module architecture. Demonstrates discovery, service registration, and async initialization.

#### Why This Matters

This work enables:

1. **Extensibility** — Third-party and internal modules can be developed independently.
2. **Feature Isolation** — Each module encapsulates its own services and state.
3. **License Gating** — Future versions will enable per-module licensing tiers.

---

## [v0.0.3] - 2026-01-28 (In Progress)

### The Nervous System (Logging & DI)

This release establishes the runtime infrastructure that enables the application to "think" and report errors. This transforms the Avalonia shell into a properly instrumented, dependency-injectable application.

#### What's New

- **Dependency Injection Container** — Microsoft.Extensions.DependencyInjection is now the sole IoC container. All services are resolved from the DI container instead of manual instantiation.

- **Structured Logging** — Serilog provides comprehensive logging with colorized console output for development and rolling file logs for production diagnostics.

#### Why This Matters

This work enables:

1. **Testability** — Services can now be mocked and isolated for unit testing.
2. **Module Foundation** — Module registration (v0.0.4) will build on this DI container.
3. **Flexibility** — Services can be swapped or decorated without modifying consuming code.

---

## [v0.0.2] - 2026-01-28 (In Progress)

### The Host Shell & UI Foundation

This release introduces the visual foundation of Lexichord — the Avalonia-based desktop application that will host all writing features.

#### What's New

- **Avalonia UI Framework Bootstrapped** — The application now launches as a proper desktop window instead of a console application. This establishes the foundation for all future UI work.

- **Podium Layout Shell** — The main window now features a structured layout with a top bar, navigation rail, content host, and status bar — the "Podium" where all future writing tools will perform.

- **Runtime Theme Switching** — Users can toggle between Dark and Light themes via the StatusBar button, with the application detecting and respecting OS preferences on first launch.

- **Window State Persistence** — Window position, size, maximized state, and theme preference are now remembered between sessions.

#### Why This Matters

This work establishes:

1. **Visual Identity** — Lexichord now has a window, title, and theme infrastructure for building the interface.
2. **Cross-Platform Support** — Avalonia enables Windows, macOS, and Linux deployment from a single codebase.
3. **Modern UI Patterns** — The architecture supports compiled bindings, resource dictionaries, and theme switching.

---

## [v0.0.1] - 2026-01-28

### The Architecture Skeleton

This release establishes the foundation of the Lexichord application. While no visible features exist yet, this work creates the structural backbone that all future features will build upon.

#### What's New

- **Project Structure Established** — The codebase now follows a clean "Modular Monolith" architecture that separates core application code from future plugins and extensions.

- **Code Quality Safeguards Added** — Build-time checks ensure code consistency across the project, catching potential issues before they become problems.

- **Automated Testing Infrastructure** — A comprehensive test suite framework is in place, enabling developers to verify changes don't break existing functionality.

- **Continuous Integration Pipeline** — Every push and pull request now triggers automated builds and tests, ensuring code quality before it reaches the main branch.

#### Why This Matters

This foundational work ensures:

1. **Maintainability** — Clean separation of concerns prevents "spaghetti code" as the project grows.
2. **Extensibility** — The plugin architecture (coming in v0.0.4) will build directly on this work.
3. **Quality Assurance** — Automated tests catch regressions before they reach users.
4. **Developer Confidence** — Clear structure and testing make it easier for contributors to add new features safely.
5. **Continuous Integration** — Automated pipelines validate every change, preventing broken code from entering the main branch.

---

## Version History

| Version | Date       | Codename                       | Summary                                                 |
| :------ | :--------- | :----------------------------- | :------------------------------------------------------ |
| v0.0.6  | 2026-01-28 | The Vault (Secure Storage)     | Secure secrets interface, metadata, exception hierarchy |
| v0.0.5  | 2026-01-28 | The Memory (Data Layer)        | Docker orchestration, database connector, migrations    |
| v0.0.4  | 2026-01-28 | The Module Protocol            | Module contract, metadata, loader, license gating       |
| v0.0.3  | 2026-01-28 | The Nervous System             | Dependency Injection, Logging, Crash Handling, Config   |
| v0.0.2  | 2026-01-28 | The Host Shell & UI Foundation | Avalonia bootstrap, window stub, theme infrastructure   |
| v0.0.1  | 2026-01-28 | The Architecture Skeleton      | Modular Monolith foundation, test infrastructure, CI/CD |

---

## Changelog Format

Each major version has detailed technical changelogs organized by sub-part:

### v0.0.6 Sub-Parts

| Document                        | Sub-Part | Title                  |
| :------------------------------ | :------- | :--------------------- |
| [LCS-CL-006a](./LCS-CL-006a.md) | v0.0.6a  | ISecureVault Interface |

### v0.0.5 Sub-Parts

| Document                        | Sub-Part | Title                 |
| :------------------------------ | :------- | :-------------------- |
| [LCS-CL-005a](./LCS-CL-005a.md) | v0.0.5a  | Docker Orchestration  |
| [LCS-CL-005b](./LCS-CL-005b.md) | v0.0.5b  | Database Connector    |
| [LCS-CL-005c](./LCS-CL-005c.md) | v0.0.5c  | FluentMigrator Runner |
| [LCS-CL-005d](./LCS-CL-005d.md) | v0.0.5d  | Repository Base       |

### v0.0.4 Sub-Parts

| Document                        | Sub-Part | Title             |
| :------------------------------ | :------- | :---------------- |
| [LCS-CL-004a](./LCS-CL-004a.md) | v0.0.4a  | IModule Interface |

### v0.0.3 Sub-Parts

| Document                        | Sub-Part | Title                     |
| :------------------------------ | :------- | :------------------------ |
| [LCS-CL-003a](./LCS-CL-003a.md) | v0.0.3a  | Dependency Injection Root |
| [LCS-CL-003b](./LCS-CL-003b.md) | v0.0.3b  | Serilog Pipeline          |
| [LCS-CL-003c](./LCS-CL-003c.md) | v0.0.3c  | Global Exception Trap     |
| [LCS-CL-003d](./LCS-CL-003d.md) | v0.0.3d  | Configuration Service     |

### v0.0.2 Sub-Parts

| Document                        | Sub-Part | Title                    |
| :------------------------------ | :------- | :----------------------- |
| [LCS-CL-002a](./LCS-CL-002a.md) | v0.0.2a  | Avalonia Bootstrap       |
| [LCS-CL-002b](./LCS-CL-002b.md) | v0.0.2b  | Podium Layout            |
| [LCS-CL-002c](./LCS-CL-002c.md) | v0.0.2c  | Runtime Theme Switching  |
| [LCS-CL-002d](./LCS-CL-002d.md) | v0.0.2d  | Window State Persistence |

### v0.0.1 Sub-Parts

| Document                        | Sub-Part | Title                           |
| :------------------------------ | :------- | :------------------------------ |
| [LCS-CL-001a](./LCS-CL-001a.md) | v0.0.1a  | Solution Scaffolding            |
| [LCS-CL-001b](./LCS-CL-001b.md) | v0.0.1b  | Dependency Graph Enforcement    |
| [LCS-CL-001c](./LCS-CL-001c.md) | v0.0.1c  | Test Suite Foundation           |
| [LCS-CL-001d](./LCS-CL-001d.md) | v0.0.1d  | Continuous Integration Pipeline |

Individual sub-part changelogs provide implementation-level detail for developers.
