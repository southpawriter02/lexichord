# Lexichord Genesis Roadmap (v0.0.1 - v0.0.8)

This breakdown focuses purely on **v0.0.x**—the foundational code required to build a Modular Monolith before a single user-facing feature (like an Editor or AI) is built.

**Total Sub-Parts:** 32 distinct implementation steps.

## v0.0.1: The Architecture Skeleton
**Goal:** Establish the strict solution hierarchy and separation of concerns.
*   **v0.0.1a:** **Solution Scaffolding.** Create the `.sln` with the three-tier folder structure: `src/Core` (Host), `src/Abstractions` (Interfaces), and `src/Modules` (Plugins).
*   **v0.0.1b:** **Project Reference Lock.** Configure `.csproj` files to strictly enforce dependencies. Ensure `Lexichord.Abstractions` has *zero* dependencies on the Host.
*   **v0.0.1c:** **Test Suite Genesis.** Initialize `Lexichord.Tests.Unit` (xUnit) and `Lexichord.Tests.Integration` (TestContainers). Establish the first passing "TrueIsTrue" test.
*   **v0.0.1d:** **CI/CD Pipeline.** Create `.github/workflows/build.yml` to restore, build, and run tests on every push. *Rule: The build fails if the Architecture is violated.*

## v0.0.2: The Host Shell (UI Framework)
**Goal:** Get a blank Avalonia window running on Windows, Mac, and Linux.
*   **v0.0.2a:** **Avalonia Bootstrap.** Initialize `Lexichord.Host` with `App.axaml` and `Program.cs`. Configure the `ClassicDesktopStyleApplicationLifetime`.
*   **v0.0.2b:** **The Podium Layout.** Design the `MainWindow` shell using a Grid layout: Top Bar (Menu), Left Rail (Nav), Center (Content Region), Bottom (Status Bar).
*   **v0.0.2c:** **Theme Infrastructure.** Implement `ThemeManager` in the Host. Add `ResourceDictionaries` for "Lexichord Dark" and "Lexichord Light" colors (using Semantic Naming: `Brush.Surface`, `Brush.Text.Primary`).
*   **v0.0.2d:** **Window State Persistence.** Create a service to save/load window position, size, and monitor location to `appstate.json` on shutdown/startup.

## v0.0.3: The Nervous System (Logging & DI)
**Goal:** Enable the app to "think" and report errors.
*   **v0.0.3a:** **Dependency Injection Root.** Set up `Microsoft.Extensions.DependencyInjection` in `Program.cs`. Create the `Host.CreateDefaultBuilder()` pattern.
*   **v0.0.3b:** **Serilog Pipeline.** Install `Serilog`. Configure Sinks for `Console` (Debug) and `File` (Rolling logs in `%AppData%/Lexichord/Logs`). Format: `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}`.
*   **v0.0.3c:** **Global Exception Trap.** Wrap the `BuildAvaloniaApp().StartWithClassicDesktopLifetime()` in a `try/catch`. Implement a "Crash Report" dialog that captures the stack trace before the app dies.
*   **v0.0.3d:** **Configuration Service.** Implement `IConfiguration` loading from `appsettings.json`, environment variables, and CLI arguments (e.g., `--debug-mode`).

## v0.0.4: The Module Protocol (The Core)
**Goal:** The capability to load external DLLs. **This is the most critical architectural step.**
*   **v0.0.4a:** **The Contract.** Define `interface IModule` in `Lexichord.Abstractions`.
    ```csharp
    public interface IModule {
        void RegisterServices(IServiceCollection services);
        Task InitializeAsync(IServiceProvider provider);
    }
    ```
*   **v0.0.4b:** **The Discovery Engine.** Implement `ModuleLoader.cs` in the Host. Logic: Scan `./Modules/*.dll`, load Assembly via Reflection, find types implementing `IModule`.
*   **v0.0.4c:** **The License Gate (Skeleton).** Create the `[RequiresLicense(Tier)]` attribute. Update `ModuleLoader` to inspect this attribute and skip loading if a hard-coded check fails (pre-cursor to real licensing).
*   **v0.0.4d:** **The Sandbox Module.** Create a separate project `Lexichord.Modules.Sandbox`. Build it, drop the DLL in the Host folder, and verify the Host logs: *"Loaded Module: Sandbox"*.

## v0.0.5: The Memory (Data Layer)
**Goal:** Persistent structured storage.
*   **v0.0.5a:** **Docker Orchestration.** Create `docker-compose.yml` defining the PostgreSQL 16 service.
*   **v0.0.5b:** **Database Connector.** Install `Npgsql`. Implement `IDatabaseConnectionFactory` in the Host with connection string pooling/resiliency policies (Polly).
*   **v0.0.5c:** **FluentMigrator Runner.** Configure the migration runner service. Create `Migration_001_InitSystem.cs` to create the `Users` and `SystemSettings` tables.
*   **v0.0.5d:** **Repository Base.** Define `IGenericRepository<T>` in Abstractions and implement the Dapper-based concrete class in the Host.

## v0.0.6: The Vault (Security)
**Goal:** Storing secrets (API Keys) without leaving them in plain text.
*   **v0.0.6a:** **Vault Interface.** Define `ISecureVault` in Abstractions. Methods: `StoreSecret(key, value)`, `GetSecret(key)`, `DeleteSecret(key)`.
*   **v0.0.6b:** **Windows Implementation.** Implement `WindowsSecureVault` using `System.Security.Cryptography.ProtectedData` (DPAPI).
*   **v0.0.6c:** **Unix Implementation.** Implement `LinuxSecureVault` using `libsecret` or file-based AES-256 encryption with a machine-key.
*   **v0.0.6d:** **Integration Test.** Write a test that stores a string "sk-12345", restarts the service, and retrieves it successfully.

## v0.0.7: The Event Bus (Communication)
**Goal:** Allow Module A to talk to Module B without referencing it.
*   **v0.0.7a:** **MediatR Setup.** Install `MediatR` in the Host. Register it in the DI container.
*   **v0.0.7b:** **Shared Events.** Define `ContentCreatedEvent` and `SettingsChangedEvent` in `Lexichord.Abstractions`.
*   **v0.0.7c:** **Pipeline Behaviors.** Implement a MediatR Pipeline Behavior for **Logging**. (Every command sent through the bus is automatically logged).
*   **v0.0.7d:** **Pipeline Behaviors.** Implement a Pipeline Behavior for **Validation** (using FluentValidation).

## v0.0.8: The "Hello World" (End-to-End Proof)
**Goal:** A working application that demonstrates the architecture is sound.
*   **v0.0.8a:** **Status Bar Module.** Create `Lexichord.Modules.StatusBar`. Have it register a view into the Bottom Region of the Host Shell.
*   **v0.0.8b:** **Database Check.** Have the Status Bar Module query the Database for "Uptime".
*   **v0.0.8c:** **Secure Check.** Have the Status Bar Module check if an API Key exists in the Vault.
*   **v0.0.8d:** **Release v0.0.8.** Tag the repo. This is the **"Golden Skeleton"** release. If this works, the Monolith is ready for features.

---

### Implementation Guide: How to execute a Sub-Part

**Example: v0.0.4c (The License Gate)**

1.  **LCS-01 (Design):** Define the `LicenseTier` enum in Abstractions.
2.  **LCS-02 (Rehearsal):**
    *   Test: Create a class `[RequiresLicense(Tier.Enterprise)] class SuperBot : IModule`.
    *   Test: Mock the `ILicenseContext` to return `Tier.Core`.
    *   Assert: `ModuleLoader` returns `null` or skips `SuperBot`.
3.  **LCS-03 (Performance):**
    *   Write the Attribute class.
    *   Modify the `foreach` loop in `ModuleLoader.cs` to use `GetCustomAttribute<RequiresLicenseAttribute>()`.
    *   Add `Log.Warning("Skipping {Module} due to license restrictions.")`.
4.  **Commit:** `feat(core): implement license attribute filtering [v0.0.4c]`