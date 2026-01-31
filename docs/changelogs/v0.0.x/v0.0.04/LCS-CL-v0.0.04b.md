# Changelog: v0.0.4b — The Discovery Engine (ModuleLoader)

| Field    | Value                |
| -------- | -------------------- |
| Version  | v0.0.4b              |
| Codename | The Discovery Engine |
| Date     | 2026-01-28           |
| Spec     | LCS-DES-004b         |
| Status   | Complete             |

---

## Summary

Implements the `ModuleLoader` service, which discovers and loads plugin modules from the `./Modules/` directory at application startup. Includes stub implementations of license-related types that v0.0.4c will expand.

---

## What's New

### Abstractions

- **`IModuleLoader`** interface with two-phase loading (`DiscoverAndLoadAsync` before DI build, `InitializeModulesAsync` after)
- **`ModuleLoadFailure`** record for capturing module load failures with rich diagnostics
- **`LicenseTier`** enum (stub): `Core`, `WriterPro`, `Teams`, `Enterprise`
- **`ILicenseContext`** interface (stub): `GetCurrentTier()`, `IsFeatureEnabled()`
- **`RequiresLicenseAttribute`** (stub): For marking modules with license requirements

### Host Services

- **`ModuleLoader`** implementation with:
    - Directory scanning for `*.dll` files
    - Assembly loading via `AssemblyLoadContext.Default`
    - Reflection-based `IModule` discovery
    - License tier checking before instantiation
    - Service registration and async initialization
    - Robust error handling for all failure modes
- **`HardcodedLicenseContext`** stub returning `LicenseTier.Core`

### Host Integration

- **`App.axaml.cs`** updated with two-phase module loading:
    1. Phase 1: `DiscoverAndLoadAsync` before `BuildServiceProvider()`
    2. Phase 2: `InitializeModulesAsync` after provider is built
- New `CreateModuleLoader()` helper method with early-stage Serilog logging

---

## Files Created

| File                                                 | Description                      |
| ---------------------------------------------------- | -------------------------------- |
| `Abstractions/Contracts/IModuleLoader.cs`            | Module loader interface          |
| `Abstractions/Contracts/ModuleLoadFailure.cs`        | Failure record                   |
| `Abstractions/Contracts/LicenseTier.cs`              | License tier enum (stub)         |
| `Abstractions/Contracts/ILicenseContext.cs`          | License context interface (stub) |
| `Abstractions/Contracts/RequiresLicenseAttribute.cs` | License attribute (stub)         |
| `Host/Services/ModuleLoader.cs`                      | ModuleLoader implementation      |
| `Host/Services/HardcodedLicenseContext.cs`           | Stub license context             |

## Files Modified

| File                | Changes                                     |
| ------------------- | ------------------------------------------- |
| `Host/App.axaml.cs` | Added module loading integration to startup |

---

## Testing

| Test File                          | Tests  | Status |
| ---------------------------------- | ------ | ------ |
| `ModuleLoadFailureTests.cs`        | 4      | ✅     |
| `ModuleLoaderDiscoveryTests.cs`    | 4      | ✅     |
| `ModuleLoaderRegistrationTests.cs` | 3      | ✅     |
| **Total**                          | **11** | ✅     |

---

## Related Documents

- [LCS-DES-004b](../specs/v0.0.x/v0.0.4/LCS-DES-004b.md) — Design Specification
- [LCS-CL-004a](./LCS-CL-004a.md) — Module Contract changelog
- [LCS-DES-004c](../specs/v0.0.x/v0.0.4/LCS-DES-004c.md) — License Gate (next)
