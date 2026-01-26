# LCS-SBD: Scope Breakdown - v0.0.8

**Target Version:** `v0.0.8`
**Codename:** The Hello World (End-to-End Proof)
**Timeline:** Sprint 5 (Golden Skeleton Milestone)
**Owner:** Lead Architect
**Prerequisites:** v0.0.7d complete (MediatR Event Bus with Shell Region notifications).

## 1. Executive Summary

**v0.0.8** is the **culmination of the foundation phase**. This release proves that the entire modular monolith architecture works end-to-end by implementing a real feature module that exercises all core systems:

1. **Module Loading** (v0.0.4) - StatusBar module is discovered and loaded from `./Modules/`.
2. **Shell Regions** (v0.0.5) - StatusBar view registers itself in the Bottom Region.
3. **Database** (v0.0.6) - StatusBar queries SQLite for system uptime/health.
4. **Secure Vault** (v0.0.7) - StatusBar verifies API key presence in encrypted vault.
5. **Event Bus** (v0.0.7) - StatusBar publishes and subscribes to health events.

**This is the "Hello World" of the architecture.** If this release succeeds:
- All foundational systems are proven to work together.
- Future feature modules have a blueprint to follow.
- The architecture is tagged as "Golden Skeleton" and frozen for v0.0.x.
- Development can proceed to v0.1.x (actual features).

---

## 2. Sub-Part Specifications

### v0.0.8a: Status Bar Module - Shell Region Registration

**Goal:** Create `Lexichord.Modules.StatusBar` that registers a view in the Host shell's Bottom Region.

- **Task 1.1: Create StatusBar Module Project**
    - Create new project `Lexichord.Modules.StatusBar`.
    - Reference only `Lexichord.Abstractions` (no Host reference).
    - Configure output to `$(SolutionDir)Modules/` directory.
- **Task 1.2: Implement IModule**
    - Create `StatusBarModule : IModule`.
    - Define `ModuleInfo` with Id: "statusbar", Name: "Status Bar".
    - Register module services in `RegisterServices()`.
- **Task 1.3: Create Status Bar View**
    - Create `StatusBarView.axaml` UserControl.
    - Display three indicator panels: Database, Vault, Events.
    - Minimal styling: horizontal stack of status indicators.
- **Task 1.4: Register in Shell Region**
    - Implement `IShellRegionView` interface.
    - Register view for `ShellRegion.Bottom` during initialization.
    - View should appear in Host MainWindow's bottom area.

**Definition of Done:**

- `Lexichord.Modules.StatusBar.dll` appears in `./Modules/`.
- Module loads without errors on Host startup.
- Status bar view is visible in MainWindow bottom region.
- Log: "StatusBar module registered in Bottom region".

---

### v0.0.8b: Database Health Check

**Goal:** StatusBar queries the SQLite database to display system uptime and health status.

- **Task 1.1: Create Health Repository**
    - Create `IHealthRepository` interface in Abstractions.
    - Methods: `GetSystemUptimeAsync()`, `GetLastHeartbeatAsync()`.
    - Create `HealthRepository` implementation using SQLite.
- **Task 1.2: System Health Table**
    - Define `system_health` table schema.
    - Columns: `id`, `started_at`, `last_heartbeat`, `database_version`.
    - Run migration on first module initialization.
- **Task 1.3: Heartbeat Service**
    - Create `IHeartbeatService` to update `last_heartbeat` periodically.
    - Update every 60 seconds while application is running.
    - Store heartbeat in SQLite.
- **Task 1.4: Status Bar Display**
    - StatusBarView shows "DB: Connected" with uptime.
    - Display format: "Uptime: 2h 15m" or "Uptime: 3d 4h".
    - Show warning icon if heartbeat is stale (> 2 minutes).

**Definition of Done:**

- `system_health` table exists in SQLite database.
- StatusBar displays current uptime.
- Heartbeat updates every 60 seconds.
- Connection errors are handled gracefully (show "DB: Error").

---

### v0.0.8c: Secure Vault API Key Check

**Goal:** StatusBar verifies that an API key exists in the ISecureVault.

- **Task 1.1: Define Test API Key**
    - Use a placeholder key name: `lexichord:test-api-key`.
    - This simulates future LLM API key storage.
    - No real API calls; just vault presence check.
- **Task 1.2: Vault Status Service**
    - Create `IVaultStatusService` interface.
    - Method: `CheckApiKeyPresenceAsync()` returns bool.
    - Inject `ISecureVault` to perform the check.
- **Task 1.3: Status Bar Display**
    - StatusBarView shows "Vault: Ready" if key exists.
    - Shows "Vault: No Key" with warning icon if missing.
    - Shows "Vault: Error" if vault is inaccessible.
- **Task 1.4: Interactive Key Setup**
    - Clicking "Vault: No Key" opens a simple dialog.
    - Dialog allows entering a test API key.
    - Key is stored in ISecureVault on submit.

**Definition of Done:**

- StatusBar accurately reflects API key presence in vault.
- Clicking "No Key" status opens key entry dialog.
- Entered key is persisted to secure vault.
- Key check works on subsequent application launches.

---

### v0.0.8d: Golden Skeleton Release

**Goal:** Tag the repository as "Golden Skeleton" and document the proven architecture.

- **Task 1.1: Integration Test Suite**
    - Create integration tests proving all systems work together.
    - Test: Module loads and registers in region.
    - Test: Database query returns valid uptime.
    - Test: Vault check returns consistent results.
    - Test: Events flow through MediatR bus.
- **Task 1.2: Architecture Documentation**
    - Update `docs/architecture/` with proven patterns.
    - Document the StatusBar module as reference implementation.
    - Create "Module Developer Guide" using StatusBar as example.
- **Task 1.3: Release Notes**
    - Write comprehensive release notes for v0.0.8.
    - List all systems proven by this milestone.
    - Include known limitations and next steps.
- **Task 1.4: Git Tag**
    - Tag repository as `v0.0.8-golden-skeleton`.
    - This marks the architecture as stable for v0.0.x.
    - Future architectural changes require v0.1.x or higher.

**Definition of Done:**

- All integration tests pass.
- Architecture documentation is complete.
- Release notes published.
- Git tag `v0.0.8-golden-skeleton` created.
- CI/CD pipeline passes all checks.

---

## 3. Implementation Checklist (for Developer)

| Step     | Description                                                                  | Status |
| :------- | :--------------------------------------------------------------------------- | :----- |
| **0.8a** | `Lexichord.Modules.StatusBar` project created.                               | [ ]    |
| **0.8a** | Module references only `Lexichord.Abstractions`.                             | [ ]    |
| **0.8a** | `StatusBarModule : IModule` implemented.                                     | [ ]    |
| **0.8a** | `StatusBarView.axaml` created with three indicator panels.                   | [ ]    |
| **0.8a** | View registered in `ShellRegion.Bottom`.                                     | [ ]    |
| **0.8a** | Status bar visible in MainWindow bottom area.                                | [ ]    |
| **0.8b** | `IHealthRepository` interface created.                                       | [ ]    |
| **0.8b** | `system_health` table created in SQLite.                                     | [ ]    |
| **0.8b** | `IHeartbeatService` updates heartbeat every 60 seconds.                      | [ ]    |
| **0.8b** | StatusBar shows database connection status and uptime.                       | [ ]    |
| **0.8c** | `IVaultStatusService` checks for API key presence.                           | [ ]    |
| **0.8c** | StatusBar shows vault status (Ready/No Key/Error).                           | [ ]    |
| **0.8c** | Key entry dialog stores key in ISecureVault.                                 | [ ]    |
| **0.8d** | Integration tests for all systems created.                                   | [ ]    |
| **0.8d** | Architecture documentation updated.                                          | [ ]    |
| **0.8d** | Release notes written.                                                       | [ ]    |
| **0.8d** | Git tag `v0.0.8-golden-skeleton` created.                                    | [ ]    |

## 4. Risks & Mitigations

- **Risk:** Shell Regions not fully implemented in v0.0.5.
    - _Mitigation:_ v0.0.8a provides detailed region registration patterns.
- **Risk:** SQLite migrations fail on first run.
    - _Mitigation:_ Wrap migration in try/catch; log error and show "DB: Error".
- **Risk:** Secure vault not accessible on all platforms.
    - _Mitigation:_ Test on Windows, macOS, Linux; use fallback file storage.
- **Risk:** Module loading order causes race conditions.
    - _Mitigation:_ StatusBar initializes lazily; services resolve on first access.
- **Risk:** Heartbeat timer causes memory leaks.
    - _Mitigation:_ Implement `IDisposable`; stop timer on module unload.
- **Risk:** Integration tests are flaky.
    - _Mitigation:_ Use test fixtures with known database state; avoid timing dependencies.

---

## 5. Success Metrics

This release is successful when:

| Metric                        | Target               | Verification                                    |
| :---------------------------- | :------------------- | :---------------------------------------------- |
| Module Discovery              | 100% success         | StatusBar loads without errors                  |
| Region Registration           | View visible         | StatusBarView appears in Bottom region          |
| Database Connectivity         | Query executes       | Uptime displays correctly                       |
| Vault Integration             | Key check works      | Status reflects actual vault state              |
| Event Bus                     | Events flow          | Health events published and received            |
| Integration Tests             | 100% passing         | All E2E tests pass in CI                        |
| Documentation                 | Complete             | Module Developer Guide published                |
| Release Tag                   | Created              | `v0.0.8-golden-skeleton` exists                 |

---

## 6. What This Proves

When v0.0.8 is complete, we have proven:

1. **Modular Monolith Works** - Modules can be developed, compiled, and loaded independently.
2. **DI Integration Works** - Module services register in the main container.
3. **Shell Regions Work** - Modules can contribute UI to Host-defined regions.
4. **Database Access Works** - Modules can query SQLite through repository pattern.
5. **Secure Storage Works** - Sensitive data can be stored and retrieved from vault.
6. **Event Bus Works** - Components can communicate without direct coupling.
7. **License Gating Works** - Modules respect `[RequiresLicense]` attributes.
8. **Logging Works** - All operations are observable in structured logs.
9. **Configuration Works** - Module behavior can be configured via appsettings.json.
10. **Exception Handling Works** - Failures are captured and reported gracefully.

**The Golden Skeleton is complete.**
