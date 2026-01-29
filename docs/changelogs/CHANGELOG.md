# Lexichord Changelog

All notable changes to the Lexichord project are documented here.

This changelog is written for stakeholders and users, focusing on **what changed** rather than technical implementation details. For detailed technical changes, see the version-specific changelogs in this directory.

---

## [v0.2.1] - 2026-01 (In Progress)

### The Rulebook (Style Module)

This release introduces **The Rulebook**, the Style Module that enables governed writing environments with consistent style enforcement.

#### What's New

- **Style Module Foundation** — New `Lexichord.Modules.Style` project establishing the infrastructure for style checking. Implements `IModule` with "The Rulebook" identity and proper service registration.

- **IStyleEngine Interface** — Core contract for style analysis. Features active style sheet management, thread-safe analysis methods, and event-driven sheet change notifications.

- **Domain Model** — Style domain types including `StyleSheet`, `StyleRule`, `StyleViolation` records with supporting enums for rule categories (`Vocabulary`, `Grammar`, `Punctuation`), violation severity levels, and pattern types.

- **Stub Services** — Skeleton implementations for `YamlStyleSheetLoader` and `FileSystemStyleWatcher` ready for full implementation in upcoming sub-parts.

#### Philosophy: Concordance

> "Rules over improvisation"

The Rulebook provides governed writing environments where style guides are applied consistently across all content. Writers focus on content while the system enforces house style.

#### Sub-Part Changelogs

| Version                          | Title              | Status      |
| -------------------------------- | ------------------ | ----------- |
| [v0.2.1a](v0.2.x/LCS-CL-021a.md) | Module Scaffolding | ✅ Complete |
| v0.2.1b                          | Style Rules        | 🔲 Planned  |
| v0.2.1c                          | YAML Loading       | 🔲 Planned  |
| v0.2.1d                          | Hot Reload         | 🔲 Planned  |

---

## [v0.1.7] - 2026-01 (In Progress)

### Distribution & Updates

This release integrates Velopack for native platform packaging and auto-updates.

#### What's New

- **Velopack Integration** — Native auto-update framework replacing stub implementation. Supports delta updates for efficient downloads, channel-specific update feeds (Stable/Insider), and in-app update installation with automatic restart.

- **Update Service Enhancements** — Extended `IUpdateService` with real download/apply functionality including progress reporting, ready-state tracking, and platform-aware behavior (skips updates in development mode).

- **Build Scripts** — Cross-platform packaging scripts (`pack-windows.ps1`, `pack-macos.sh`) for creating distributable installers via Velopack CLI.

- **Signing Infrastructure** — CI pipeline for code signing Windows executables (PFX/SignTool) and macOS applications (Developer ID/Notarization). Signed binaries avoid SmartScreen and Gatekeeper security warnings.

- **Release Notes Viewer** — Automatic display of `CHANGELOG.md` after updates. First-run detection compares stored vs. current version, opening release notes in a new editor tab. Settings persist to track run history and installation ID.

- **Telemetry Hooks** — Opt-in crash reporting via Sentry SDK integration. User-configurable toggle in Settings > Privacy with clear data disclosure. Robust PII scrubbing ensures file paths and email addresses are never transmitted. Disabled by default until explicitly enabled.

#### Sub-Part Changelogs

| Version                          | Title                  | Status      |
| -------------------------------- | ---------------------- | ----------- |
| [v0.1.7a](v0.1.x/LCS-CL-017a.md) | Velopack Integration   | ✅ Complete |
| [v0.1.7b](v0.1.x/LCS-CL-017b.md) | Signing Infrastructure | ✅ Complete |
| [v0.1.7c](v0.1.x/LCS-CL-017c.md) | Release Notes Viewer   | ✅ Complete |
| [v0.1.7d](v0.1.x/LCS-CL-017d.md) | Telemetry Hooks        | ✅ Complete |

---

## [v0.1.6] - 2026-01 (In Progress)

### The Settings Framework

This release establishes the centralized settings UI framework, enabling modules to contribute configuration pages to a unified Settings dialog, with live theme preview as the first module-contributed settings page.

#### What's New

- **Settings Page Registry** — Central registry for module-contributed settings pages with hierarchical organization, license tier filtering, and full-text search capabilities.

- **Settings ViewModel** — State management for the Settings dialog with tree navigation, search filtering, error handling for page loading failures, and MediatR event integration.

- **Settings Window** — Modal dialog with split-panel layout (220px navigation tree, flexible content area), search box, TreeView navigation, and keyboard shortcuts (Ctrl+,, Escape).

- **Live Theme Preview** — Real-time theme switching from the Appearance settings page. Select Light, Dark, or System themes with instant visual feedback. OS theme synchronization for System mode.

- **License Management UI** — Account settings page for license activation and tier management. Three-stage validation (format, checksum, server), secure key storage via `ISecureVault`, tier-colored badges, expiration warnings, and feature availability matrix.

- **Update Channel Selector** — Updates settings page for switching between Stable and Insider channels. Assembly-driven version display, manual update checking (stub), and local settings persistence.

- **Module Integration** — Modules implement `ISettingsPage` interface and register via `ISettingsPageRegistry` during initialization. Pages support hierarchical nesting via `ParentCategoryId`.

#### Sub-Part Changelogs

| Version                   | Title                     | Status      |
| ------------------------- | ------------------------- | ----------- |
| [v0.1.6a](LCS-CL-016a.md) | Settings Dialog Framework | ✅ Complete |
| [v0.1.6b](LCS-CL-016b.md) | Live Theme Preview        | ✅ Complete |
| [v0.1.6c](LCS-CL-016c.md) | License Management UI     | ✅ Complete |
| [v0.1.6d](LCS-CL-016d.md) | Update Channel Selector   | ✅ Complete |

---

## [v0.1.5] - 2026-01 (In Progress)

### The Conductor's Baton (Command Palette)

This release establishes the keyboard-centric command system that enables rapid access to all application functionality through commands, shortcuts, and a searchable palette.

#### What's New

- **Command Registry** — Centralized system for registering, querying, and executing application commands. Commands use `module.action` naming convention with metadata for display titles, categories, icons, and keyboard shortcuts.

- **Command Palette UI** — Keyboard-centric modal overlay for discovering and executing commands. Features fuzzy search via FuzzySharp, keyboard navigation (↑/↓, Page Up/Down, Home/End), and mode switching between Commands and Files. Shortcuts: Ctrl+Shift+P (commands), Ctrl+P (files).

- **File Jumper** — Workspace file indexing service enabling fast file search via the Command Palette. Features background indexing with progress reporting, fuzzy file name matching, LRU-based recent files tracking, and incremental updates from file system watcher events. Configurable ignore patterns and binary file filtering.

- **Keybinding Service** — User-customizable keyboard shortcuts for commands with JSON-based configuration persistence. Features conflict detection with context-aware filtering, file watching for hot-reload, and platform-agnostic gesture processing. Gestures like Ctrl+S, Ctrl+Shift+P are routed to commands registered in the Command Registry.

#### Sub-Part Changelogs

| Version                          | Title              | Status      |
| -------------------------------- | ------------------ | ----------- |
| [v0.1.5a](LCS-CL-015a.md)        | Command Registry   | ✅ Complete |
| [v0.1.5b](LCS-CL-015b.md)        | Command Palette UI | ✅ Complete |
| [v0.1.5c](v0.1.x/LCS-CL-015c.md) | File Jumper        | ✅ Complete |
| [v0.1.5d](LCS-CL-015d.md)        | Keybinding Service | ✅ Complete |

---

## [v0.1.4] - 2026-01 (In Progress)

### Scribe (IO & Persistence)

This release establishes file I/O and document persistence capabilities, starting with dirty state tracking and atomic saves.

#### What's New

- **Dirty State Tracking** — Visual "\*" indicator in tab titles when documents have unsaved changes. Debounced updates (50ms) prevent excessive state changes during rapid typing. Content hashing enables detection when undo returns to saved state.

- **Atomic Saves** — Corruption-proof file saves using Write-Temp-Delete-Rename strategy. Three-phase execution ensures original file is preserved on any failure. Includes encoding detection, error recovery, and MediatR events for save success/failure.

- **Safe Close Workflow** — Window close interception prompts users to save unsaved changes. Modal dialog offers Save All, Discard All, or Cancel options. Prevents accidental data loss when closing with dirty documents.

- **Recent Files History** — MRU (Most Recently Used) file tracking with database persistence. Files automatically tracked when opened and displayed in "File > Open Recent" menu. Missing files shown as disabled with option to prune.

#### Sub-Part Changelogs

| Version                   | Title                | Status      |
| ------------------------- | -------------------- | ----------- |
| [v0.1.4a](LCS-CL-014a.md) | Dirty State Tracking | ✅ Complete |
| [v0.1.4b](LCS-CL-014b.md) | Atomic Saves         | ✅ Complete |
| [v0.1.4c](LCS-CL-014c.md) | Safe Close Workflow  | ✅ Complete |
| [v0.1.4d](LCS-CL-014d.md) | Recent Files History | ✅ Complete |

---

## [v0.1.3] - 2026-01 (In Progress)

### The Manuscript (Editor Module)

This release establishes the core text editing infrastructure using AvaloniaEdit, providing a high-performance editor foundation for manuscript creation and editing.

#### What's New

- **AvaloniaEdit Integration** — High-performance text editor based on AvaloniaEdit 11.1.0, supporting large documents with syntax highlighting.

- **Syntax Highlighting Service** — Theme-aware syntax highlighting for Markdown, JSON, YAML, and XML with automatic theme adaptation. XSHD definitions loaded from embedded resources with light/dark variants.

- **Search & Replace Overlay** — Inline search and replace with live highlighting, match navigation (F3/Shift+F3), and replace all. Supports case-sensitive, whole word, and regex modes with ReDoS protection.

- **ManuscriptViewModel** — Document ViewModel extending `DocumentViewModelBase` with full editor state management including caret position, selection tracking, and document statistics.

- **EditorService** — Document lifecycle management with open/create/save/close operations, thread-safe document tracking, and file path deduplication.

- **Editor Configuration** — Configurable editor settings including font family, font size, line numbers, word wrap, and whitespace display with JSON file persistence. Font fallback chain resolves missing fonts automatically.

- **Ctrl+Scroll Zoom** — Real-time font size adjustment via Ctrl+Mouse Wheel with debounced persistence. Keyboard shortcuts: Ctrl+0 (reset), Ctrl++/- (zoom in/out).

- **Keyboard Shortcuts** — Ctrl+S (save), Ctrl+F (search overlay), Ctrl+H (toggle replace), F3/Shift+F3 (find next/previous), Escape (hide search), Ctrl+0/+/- (zoom).

#### Why This Matters

This work enables:

- Professional-quality text editing experience for technical writers
- Syntax highlighting for common technical writing formats
- Foundation for search and other editor features
- Live document statistics (line/word/character counts) for writers
- Customizable editor appearance with persistent settings

#### Sub-Part Changelogs

| Version                   | Title                            | Status      |
| ------------------------- | -------------------------------- | ----------- |
| [v0.1.3a](LCS-CL-013a.md) | AvalonEdit Integration           | ✅ Complete |
| [v0.1.3b](LCS-CL-013b.md) | Syntax Highlighting              | ✅ Complete |
| [v0.1.3c](LCS-CL-013c.md) | Search Overlay                   | ✅ Complete |
| [v0.1.3d](LCS-CL-013d.md) | Editor Configuration Persistence | ✅ Complete |

---

## [v0.1.2] - 2026-01-28 (In Progress)

### The Explorer (Project Management)

This release establishes workspace and project management capabilities, enabling Lexichord to open folders and provide file system awareness.

#### What's New

- **Workspace Service** — Core service managing the current open folder state. Tracks active workspace path, persists recent workspaces (up to 10), and coordinates workspace lifecycle.

- **Workspace Events** — MediatR notifications (`WorkspaceOpenedEvent`, `WorkspaceClosedEvent`) enable cross-module reactions to workspace changes without direct dependencies.

- **Path Validation** — `WorkspaceInfo.ContainsPath()` provides security-aware path validation, preventing directory traversal attacks when filtering files.

- **Robust File System Watcher** — Production-ready file change detection with:
    - **Debouncing** — Accumulates rapid changes (default 100ms) to prevent event storms
    - **Batching** — Emits grouped changes as single events for efficiency
    - **Ignore Patterns** — Configurable glob patterns to filter `.git`, `node_modules`, temp files, etc.
    - **Buffer Overflow Detection** — Signals need for directory rescan when OS buffer overflows
    - **Error Recovery** — Automatic restart attempts (up to 3) on watcher failures

- **External File Changes Event** — `ExternalFileChangesEvent` MediatR notification enables modules to react to file system changes detected by the watcher.

- **Tree View UI (Project Explorer)** — Hierarchical file browser with:
    - **Material Design Icons** — File type icons based on extension (programming, documents, etc.)
    - **Lazy Loading** — Directory contents loaded on-demand when expanded
    - **Smart Sorting** — Directories before files, alphabetical within each category
    - **Live Updates** — Tree updates automatically when external file changes are detected
    - **MediatR Integration** — Publishes `FileOpenRequestedEvent` when files are opened

- **Context Menu Actions** — Full file and folder management in the Project Explorer:
    - **New File/Folder** — Create new items with inline name editing (Ctrl+N, Ctrl+Shift+N)
    - **Rename** — Inline renaming with validation (F2)
    - **Delete** — Remove files/folders with recursive support
    - **Reveal in Explorer** — Open containing folder in OS file browser (Windows/macOS/Linux)
    - **Protected Paths** — Prevents modification of `.git` folders and workspace root
    - **Name Validation** — Blocks invalid characters, reserved names, and path traversal

#### Why This Matters

This work enables:

1. **Project Context** — The application can track which project folder is open, enabling file-aware features.
2. **Recent Projects** — Users can quickly reopen previously used workspaces.
3. **Cross-Module Coordination** — Other modules can react to workspace and file changes via MediatR events.
4. **Reliable File Watching** — External edits (git checkout, npm install, IDE refactors) are detected reliably without event storms.
5. **File Management** — Users can create, rename, and delete files/folders without leaving the IDE.

> [!NOTE]
> For detailed technical changes, see [LCS-CL-012a.md](./LCS-CL-012a.md), [LCS-CL-012b.md](./LCS-CL-012b.md), [LCS-CL-012c.md](./LCS-CL-012c.md), and [LCS-CL-012d.md](./LCS-CL-012d.md).

---

## [v0.1.1] - 2026-01-28 (In Progress)

### The Layout Engine (Docking System)

This release establishes a flexible, IDE-like docking system using `Dock.Avalonia`. Modules can contribute documents and tool panes to resizable, draggable regions.

#### What's New

- **Dock Library Integration** — Integrated `Dock.Avalonia` to provide resizable splitters and draggable panels in the main window. Replaced static grid layout with a 4-region dock system (Left, Center, Right, Bottom).

- **Layout Abstractions** — Created `IDockFactory`, `IDocument`, `ITool`, and `DockRegionConfig` interfaces that decouple modules from the docking library implementation.

- **Region Injection Service** — Added `IRegionManager` service enabling modules to inject tool panes and documents into dock regions without direct Dock.Avalonia dependency. Includes MediatR integration for region change notifications.

- **Layout Serialization** — JSON-based layout persistence enables saving/loading workspace arrangements. Features named profiles, auto-save with debouncing, atomic file writes, and schema versioning for future migrations.

- **Tab Infrastructure** — Core abstractions for document tabs including `IDocumentTab`, `DocumentViewModelBase`, and `ITabService`. Features dirty state tracking with visual indicators, pinning with automatic tab reordering, save confirmation dialogs, and a standard IDE context menu. MediatR integration enables lifecycle notifications for close, pin, and dirty state changes.

- **MainWindowViewModel** — New ViewModel managing the dock layout lifecycle with proper DI integration.

#### Why This Matters

This work enables:

1. **Flexible Layouts** — Users can resize, drag, and rearrange panels to customize their workspace.
2. **Module Panel Registration** — Modules can contribute tool panes and documents without knowledge of the docking library.
3. **Document Management** — Documents can track unsaved changes, prompt for save on close, and respect pinned state.

> [!NOTE]
> For detailed technical changes, see [LCS-CL-011a.md](./LCS-CL-011a.md), [LCS-CL-011b.md](./LCS-CL-011b.md), [LCS-CL-011c.md](./LCS-CL-011c.md), and [LCS-CL-011d.md](./LCS-CL-011d.md).

---

## [v0.0.8] - 2026-01-29 (In Progress)

### The Hello World (Golden Skeleton)

This release establishes the first feature module as a reference implementation for all future Lexichord modules. The Status Bar module demonstrates the complete module lifecycle and introduces the Shell Region system for decoupled UI composition.

#### What's New

- **Shell Region Infrastructure** — Modules can now contribute UI to host window regions (Top, Left, Center, Right, Bottom) via `IShellRegionView` without direct host dependencies.

- **Status Bar Module** — The canonical "Golden Skeleton" reference implementation demonstrating proper module structure, service registration, and async initialization patterns.

- **Database Health Monitoring** — SQLite-based health tracking with `HealthRepository` for uptime tracking and `HeartbeatService` for 60-second heartbeat recording. Includes staleness detection and `SystemHealthChangedEvent` for cross-module health updates.

- **Vault Status Tracking** — Secure vault integration with `VaultStatusService` that verifies API key presence, handles platform support detection, and provides "Ready", "No Key", "Error", or "N/A" status display. Includes `ApiKeyDialog` for user key entry.

#### Why This Matters

This work enables:

1. **Module Development Pattern** — Future module developers have a complete working example to follow.
2. **Decoupled UI** — Modules contribute views without knowing about the host layout.
3. **End-to-End Validation** — Proves the module system works from discovery to rendering.

---

## [v0.0.7] - 2026-01-28 (In Progress)

### The Event Bus (Communication)

This release establishes MediatR-based in-process messaging that enables loose coupling between Lexichord modules. Commands, queries, and domain events now flow through a central mediator with pipeline behaviors for cross-cutting concerns.

#### What's New

- **MediatR Bootstrap** — Core messaging infrastructure with IMediator registration, assembly scanning for handler discovery, and CQRS marker interfaces (ICommand, IQuery, IDomainEvent).

- **Shared Domain Events** — DomainEventBase, ContentCreatedEvent, and SettingsChangedEvent enable modules to publish and subscribe to state changes without direct dependencies.

- **Logging Pipeline Behavior** — Automatic request/response logging with timing, slow request warnings, sensitive data redaction (`[SensitiveData]`, `[NoLog]`), and configurable thresholds.

- **Validation Pipeline Behavior** — FluentValidation integration with automatic request validation, structured error aggregation (`ValidationException`, `ValidationError`), and validator auto-discovery.

#### Why This Matters

This work enables:

1. **Loose Coupling** — Modules communicate via messages, not direct dependencies.
2. **CQRS Pattern** — Clear separation between commands (writes) and queries (reads).
3. **Extensibility** — Pipeline behaviors enable cross-cutting concerns (logging, validation).
4. **Input Validation** — Commands are validated before handlers execute, with structured error responses.

---

## [v0.0.6] - 2026-01-28 (In Progress)

### The Vault (Secure Secrets Storage)

This release establishes the secure secrets management infrastructure that protects sensitive credentials. Lexichord can now store API keys, connection strings, and OAuth tokens using OS-native encryption.

#### What's New

- **ISecureVault Interface** — Platform-agnostic contract for secure secret storage with CRUD operations, metadata access, and streaming key listing.

- **WindowsSecureVault (DPAPI)** — Windows-specific implementation using Data Protection API with user-scoped encryption, per-installation entropy, and secure file-based storage.

- **UnixSecureVault (libsecret/AES-256)** — Linux and macOS implementation with libsecret integration and robust AES-256-GCM file-based encryption fallback.

- **Integration Testing** — Comprehensive test suite verifying secrets survive restarts, CRUD lifecycle, metadata accuracy, and platform factory selection.

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

| Version | Date       | Codename                          | Summary                                                    |
| :------ | :--------- | :-------------------------------- | :--------------------------------------------------------- |
| v0.1.5  | 2026-01-29 | The Conductor's Baton (Commands)  | Command Registry, command execution and discovery          |
| v0.1.2  | 2026-01-28 | The Explorer (Project Management) | Workspace service, file system watcher, project context    |
| v0.1.1  | 2026-01-28 | The Layout Engine (Docking)       | Dock.Avalonia integration, region injection, serialization |
| v0.0.8  | 2026-01-29 | The Hello World (Golden Skeleton) | Shell regions, Status Bar module reference implementation  |
| v0.0.7  | 2026-01-28 | The Event Bus (Communication)     | MediatR bootstrap, CQRS interfaces, handler discovery      |
| v0.0.6  | 2026-01-28 | The Vault (Secure Storage)        | Secure secrets interface, metadata, exception hierarchy    |
| v0.0.5  | 2026-01-28 | The Memory (Data Layer)           | Docker orchestration, database connector, migrations       |
| v0.0.4  | 2026-01-28 | The Module Protocol               | Module contract, metadata, loader, license gating          |
| v0.0.3  | 2026-01-28 | The Nervous System                | Dependency Injection, Logging, Crash Handling, Config      |
| v0.0.2  | 2026-01-28 | The Host Shell & UI Foundation    | Avalonia bootstrap, window stub, theme infrastructure      |
| v0.0.1  | 2026-01-28 | The Architecture Skeleton         | Modular Monolith foundation, test infrastructure, CI/CD    |

---

## Changelog Format

Each major version has detailed technical changelogs organized by sub-part:

### v0.1.5 Sub-Parts

| Document                               | Sub-Part | Title              |
| :------------------------------------- | :------- | :----------------- |
| [LCS-CL-015a](./LCS-CL-015a.md)        | v0.1.5a  | Command Registry   |
| [LCS-CL-015b](./LCS-CL-015b.md)        | v0.1.5b  | Command Palette UI |
| [LCS-CL-015c](./v0.1.x/LCS-CL-015c.md) | v0.1.5c  | File Jumper        |
| [LCS-CL-015d](./LCS-CL-015d.md)        | v0.1.5d  | Keybinding Service |

### v0.1.2 Sub-Parts

| Document                        | Sub-Part | Title               |
| :------------------------------ | :------- | :------------------ |
| [LCS-CL-012a](./LCS-CL-012a.md) | v0.1.2a  | Workspace Service   |
| [LCS-CL-012b](./LCS-CL-012b.md) | v0.1.2b  | File System Watcher |
| [LCS-CL-012c](./LCS-CL-012c.md) | v0.1.2c  | Tree View UI        |

### v0.1.1 Sub-Parts

| Document                        | Sub-Part | Title                    |
| :------------------------------ | :------- | :----------------------- |
| [LCS-CL-011a](./LCS-CL-011a.md) | v0.1.1a  | Dock Library Integration |
| [LCS-CL-011b](./LCS-CL-011b.md) | v0.1.1b  | Region Injection Service |
| [LCS-CL-011c](./LCS-CL-011c.md) | v0.1.1c  | Layout Serialization     |

### v0.0.8 Sub-Parts

| Document                        | Sub-Part | Title                   |
| :------------------------------ | :------- | :---------------------- |
| [LCS-CL-008a](./LCS-CL-008a.md) | v0.0.8a  | Status Bar Module       |
| [LCS-CL-008b](./LCS-CL-008b.md) | v0.0.8b  | Database Health Check   |
| [LCS-CL-008c](./LCS-CL-008c.md) | v0.0.8c  | Secure Vault Check      |
| [LCS-CL-008d](./LCS-CL-008d.md) | v0.0.8d  | Golden Skeleton Release |

### v0.0.7 Sub-Parts

| Document                        | Sub-Part | Title                        |
| :------------------------------ | :------- | :--------------------------- |
| [LCS-CL-007a](./LCS-CL-007a.md) | v0.0.7a  | MediatR Bootstrap            |
| [LCS-CL-007b](./LCS-CL-007b.md) | v0.0.7b  | Shared Domain Events         |
| [LCS-CL-007c](./LCS-CL-007c.md) | v0.0.7c  | Logging Pipeline Behavior    |
| [LCS-CL-007d](./LCS-CL-007d.md) | v0.0.7d  | Validation Pipeline Behavior |

### v0.0.6 Sub-Parts

| Document                        | Sub-Part | Title                               |
| :------------------------------ | :------- | :---------------------------------- |
| [LCS-CL-006a](./LCS-CL-006a.md) | v0.0.6a  | ISecureVault Interface              |
| [LCS-CL-006b](./LCS-CL-006b.md) | v0.0.6b  | WindowsSecureVault (DPAPI)          |
| [LCS-CL-006c](./LCS-CL-006c.md) | v0.0.6c  | UnixSecureVault (libsecret/AES-256) |
| [LCS-CL-006d](./LCS-CL-006d.md) | v0.0.6d  | Integration Testing                 |

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
