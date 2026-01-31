# LCS-SBD: Scope Breakdown - v0.0.4

**Target Version:** `v0.0.4`
**Codename:** The Module Protocol (The Core)
**Timeline:** Sprint 3 (Module Architecture Foundation)
**Owner:** Lead Architect
**Prerequisites:** v0.0.3d complete (Logging, DI, Exception Handling, Configuration).

## 1. Executive Summary

**v0.0.4** establishes the most critical architectural component of Lexichord: the **Module Loading System**. This release transforms the application from a monolithic executable into a true **Modular Monolith** capable of dynamically loading feature DLLs. The success of this release is measured by:

1. `IModule` interface defines the contract for all feature modules.
2. `ModuleLoader` discovers and loads assemblies from `./Modules/` directory.
3. `[RequiresLicense]` attribute gates module loading by license tier.
4. A `SandboxModule` successfully loads and registers with the Host.

**This is the architectural cornerstone.** If the module protocol is flawed:
- Features cannot be decoupled into separate assemblies.
- Licensing gates cannot be enforced at load time.
- The Teams/Enterprise tier features will be impossible to deliver.

---

## 2. Sub-Part Specifications

### v0.0.4a: The Contract (IModule Interface)

**Goal:** Define the interface contract that all feature modules must implement.

- **Task 1.1: IModule Interface**
    - Create `IModule` interface in `Lexichord.Abstractions.Contracts`.
    - Define lifecycle methods: `RegisterServices()`, `InitializeAsync()`.
    - Add metadata properties: `Name`, `Version`, `Description`.
- **Task 1.2: Module Metadata**
    - Create `ModuleInfo` record for module metadata.
    - Include: Id, Name, Version, Author, Dependencies.
- **Task 1.3: Module Lifecycle Events**
    - Define `ModuleLoadedEvent` and `ModuleUnloadedEvent` in Abstractions.
    - These events will be published via MediatR (v0.0.7).
- **Task 1.4: Documentation**
    - XML documentation for all interface members.
    - LOGIC comments explaining the module lifecycle.

**Definition of Done:**

- `IModule` interface exists in Abstractions.
- `ModuleInfo` record exists with all required metadata fields.
- Interface is well-documented with examples.
- No Host references in Abstractions (verified by architecture test).

---

### v0.0.4b: The Discovery Engine (ModuleLoader)

**Goal:** Implement the service that discovers and loads module assemblies.

- **Task 1.1: ModuleLoader Service**
    - Create `IModuleLoader` interface in Abstractions.
    - Create `ModuleLoader` implementation in Host.
    - Scan `{AppDir}/Modules/*.dll` for assemblies.
- **Task 1.2: Assembly Loading**
    - Use `AssemblyLoadContext` for isolated loading.
    - Find types implementing `IModule` via reflection.
    - Instantiate modules and store references.
- **Task 1.3: Module Registration**
    - Call `IModule.RegisterServices()` during Host startup.
    - Pass `IServiceCollection` to allow service registration.
    - Maintain registry of loaded modules.
- **Task 1.4: Error Handling**
    - Log detailed errors when module loading fails.
    - Continue loading other modules on failure (fail-safe).
    - Expose `LoadedModules` and `FailedModules` collections.

**Definition of Done:**

- `ModuleLoader` scans `./Modules/` directory on startup.
- Successfully loaded modules are registered in DI container.
- Failed modules are logged with detailed error information.
- Application continues if a module fails to load.

---

### v0.0.4c: The License Gate (Skeleton)

**Goal:** Create the license attribute that controls module loading.

- **Task 1.1: License Tier Enum**
    - Create `LicenseTier` enum in Abstractions.
    - Values: `Core = 0`, `WriterPro = 1`, `Teams = 2`, `Enterprise = 3`.
- **Task 1.2: RequiresLicense Attribute**
    - Create `[RequiresLicense(LicenseTier)]` attribute.
    - Target: Class (modules and services).
    - Include optional `FeatureCode` property.
- **Task 1.3: License Check in ModuleLoader**
    - Update `ModuleLoader` to check `[RequiresLicense]` attribute.
    - Compare against current license tier (hardcoded `Core` for now).
    - Skip modules that require higher tier.
- **Task 1.4: ILicenseContext Interface**
    - Create `ILicenseContext` interface for future license service.
    - Methods: `GetCurrentTier()`, `IsFeatureEnabled(string code)`.
    - Implement stub `HardcodedLicenseContext` returning `Core`.

**Definition of Done:**

- `[RequiresLicense(LicenseTier.Teams)]` attribute compiles.
- `ModuleLoader` skips modules requiring higher license tier.
- Log message: "Skipping {Module} due to license restrictions".
- `ILicenseContext` interface exists for future implementation.

---

### v0.0.4d: The Sandbox Module (Proof of Concept)

**Goal:** Create a test module that proves the architecture works.

- **Task 1.1: Create Sandbox Project**
    - Create new project `Lexichord.Modules.Sandbox`.
    - Reference only `Lexichord.Abstractions` (verify no Host reference).
    - Configure output to `$(SolutionDir)Modules/` directory.
- **Task 1.2: Implement IModule**
    - Create `SandboxModule : IModule`.
    - Register a simple `ISandboxService` in `RegisterServices()`.
    - Log "Sandbox module initialized" in `InitializeAsync()`.
- **Task 1.3: Build & Deploy**
    - Build Sandbox module separately from Host.
    - Verify DLL appears in `./Modules/` directory.
    - Run Host and verify "Loaded Module: Sandbox" log message.
- **Task 1.4: Integration Test**
    - Create test that verifies `ISandboxService` is resolvable.
    - Verify module appears in `ModuleLoader.LoadedModules`.

**Definition of Done:**

- `Lexichord.Modules.Sandbox` project exists.
- Project only references `Lexichord.Abstractions`.
- DLL outputs to `./Modules/` directory.
- Host logs "Loaded Module: Sandbox" on startup.
- `ISandboxService` is resolvable from DI container.

---

## 3. Implementation Checklist (for Developer)

| Step     | Description                                                                      | Status |
| :------- | :------------------------------------------------------------------------------- | :----- |
| **0.4a** | `IModule` interface created in `Lexichord.Abstractions.Contracts`.               | [ ]    |
| **0.4a** | `ModuleInfo` record created with Id, Name, Version, Author, Dependencies.        | [ ]    |
| **0.4a** | `ModuleLoadedEvent` and `ModuleUnloadedEvent` defined.                           | [ ]    |
| **0.4b** | `IModuleLoader` interface created in Abstractions.                               | [ ]    |
| **0.4b** | `ModuleLoader` implementation scans `./Modules/*.dll`.                           | [ ]    |
| **0.4b** | Modules discovered via reflection on `IModule` interface.                        | [ ]    |
| **0.4b** | `RegisterServices()` called for each discovered module.                          | [ ]    |
| **0.4c** | `LicenseTier` enum created (Core, WriterPro, Teams, Enterprise).                 | [ ]    |
| **0.4c** | `[RequiresLicense]` attribute created with tier parameter.                       | [ ]    |
| **0.4c** | `ModuleLoader` checks license attribute and skips restricted modules.            | [ ]    |
| **0.4c** | `ILicenseContext` interface created with stub implementation.                    | [ ]    |
| **0.4d** | `Lexichord.Modules.Sandbox` project created.                                     | [ ]    |
| **0.4d** | Sandbox only references Abstractions (no Host reference).                        | [ ]    |
| **0.4d** | Sandbox DLL outputs to `./Modules/` directory.                                   | [ ]    |
| **0.4d** | Host logs "Loaded Module: Sandbox" on startup.                                   | [ ]    |

## 4. Risks & Mitigations

- **Risk:** Assembly version conflicts between Host and Module.
    - _Mitigation:_ Pin shared package versions in `Directory.Build.props`.
- **Risk:** Module crashes during initialization.
    - _Mitigation:_ Wrap `InitializeAsync()` in try/catch; log error and continue.
- **Risk:** Circular reference between Host and Module.
    - _Mitigation:_ Architecture tests verify no Host references in Modules.
- **Risk:** License attribute bypassed by reflection.
    - _Mitigation:_ Acceptable for v0.0.4 (skeleton); real licensing in v1.x.
- **Risk:** Module output path not correctly configured.
    - _Mitigation:_ Use `$(SolutionDir)Modules/` in .csproj; verify in CI.
