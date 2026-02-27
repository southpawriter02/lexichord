# v0.7.7h — Pre-built Workflows (Validation Workflows — CKVS Phase 4d)

**Released:** 2026-02-26
**Specification:** LCS-DES-077-KG-h
**Test Count:** 22 (all passing)

---

## Overview

This sub-part delivers three production-ready validation workflow templates that ship as embedded YAML resources within the Lexichord.Modules.Agents assembly. These pre-built workflows provide out-of-the-box validation capabilities using the step types introduced in v0.7.7e (Validation), v0.7.7f (Gating), and v0.7.7g (Sync).

## New Types

### Interfaces (namespace: `Lexichord.Modules.Agents.Workflows.Validation.Templates`)

| Interface                     | Methods | Purpose                                                      |
| ----------------------------- | ------- | ------------------------------------------------------------ |
| `IValidationWorkflowRegistry` | 6       | Central registry for pre-built + custom validation workflows |
| `IValidationWorkflowLoader`   | 1       | Loads workflow definitions from embedded YAML resources      |
| `IValidationWorkflowStorage`  | 5       | CRUD for custom (user-defined) workflows (stub in v0.7.7h)   |

### Records & Enums

| Type                                   | Kind   | Properties/Values                                                                                                                                                                                               |
| -------------------------------------- | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ValidationWorkflowDefinition`         | Record | 15 properties (Id, Name, Description, Version, Trigger, IsPrebuilt, EnabledByDefault, TimeoutMinutes, Steps, LicenseRequirement, ExpectedDurationMinutes, PerformanceTargets, CreatedAt, ModifiedAt, CreatedBy) |
| `ValidationWorkflowStepDef`            | Record | 7 properties (Id, Name, Type, Order, Enabled, TimeoutMs, Options)                                                                                                                                               |
| `ValidationWorkflowLicenseRequirement` | Record | 4 booleans (Core, WriterPro, Teams, Enterprise)                                                                                                                                                                 |
| `ValidationWorkflowTrigger`            | Enum   | 5 values (Manual, OnSave, PrePublish, ScheduledNightly, Custom)                                                                                                                                                 |

### Implementations

| Class                                      | Implements                    | Description                                                             |
| ------------------------------------------ | ----------------------------- | ----------------------------------------------------------------------- |
| `ValidationWorkflowRegistry`               | `IValidationWorkflowRegistry` | Three-tier lookup (cache → storage → loader), pre-built mutation guards |
| `EmbeddedResourceValidationWorkflowLoader` | `IValidationWorkflowLoader`   | YamlDotNet deserialization with snake_case naming convention            |
| `InMemoryValidationWorkflowStorage`        | `IValidationWorkflowStorage`  | ConcurrentDictionary-based stub for custom workflows                    |

## Pre-built Workflow Templates

### 1. On-Save Validation (`on-save-validation`)

- **Trigger:** OnSave
- **License:** WriterPro+
- **Steps (3):** schema-validation → consistency-check → reference-validation
- **Timeout:** 5 min | **Expected:** 2 min

### 2. Pre-Publish Gate (`pre-publish-gate`)

- **Trigger:** PrePublish
- **License:** Teams+
- **Steps (7):** schema-validation → grammar-check → consistency-check → kg-alignment → reference-validation → publish-gate → kg-sync
- **Timeout:** 15 min | **Expected:** 8 min

### 3. Nightly Health Check (`nightly-health-check`)

- **Trigger:** ScheduledNightly
- **License:** Teams+
- **Steps (5):** workspace-scan → batch-schema-validation → batch-consistency-check → batch-reference-check → health-report
- **Timeout:** 60 min | **Expected:** 30 min

## Unit Tests

| Test Class                                      | Count | Description                                                                  |
| ----------------------------------------------- | ----- | ---------------------------------------------------------------------------- |
| `ValidationWorkflowRegistryTests`               | 10    | Three-tier lookup, caching, mutation guards, license verification            |
| `EmbeddedResourceValidationWorkflowLoaderTests` | 8     | YAML parsing for all 3 templates, license, performance targets, step options |
| `ValidationWorkflowDefinitionTests`             | 4     | Record defaults, enum completeness, step defaults                            |

## Files Changed

### New Files (16)

- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/ValidationWorkflowTrigger.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/ValidationWorkflowLicenseRequirement.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/ValidationWorkflowStepDef.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/ValidationWorkflowDefinition.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/IValidationWorkflowLoader.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/IValidationWorkflowStorage.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/IValidationWorkflowRegistry.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/InMemoryValidationWorkflowStorage.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/EmbeddedResourceValidationWorkflowLoader.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Templates/ValidationWorkflowRegistry.cs`
- `src/Lexichord.Modules.Agents/Resources/Workflows/Validation/on-save-validation.yaml`
- `src/Lexichord.Modules.Agents/Resources/Workflows/Validation/pre-publish-gate.yaml`
- `src/Lexichord.Modules.Agents/Resources/Workflows/Validation/nightly-health-check.yaml`
- `tests/Lexichord.Tests.Unit/Modules/Agents/Workflows/ValidationWorkflowRegistryTests.cs`
- `tests/Lexichord.Tests.Unit/Modules/Agents/Workflows/EmbeddedResourceValidationWorkflowLoaderTests.cs`
- `tests/Lexichord.Tests.Unit/Modules/Agents/Workflows/ValidationWorkflowDefinitionTests.cs`

### Modified Files (2)

- `src/Lexichord.Modules.Agents/Lexichord.Modules.Agents.csproj` — Added EmbeddedResource glob for `Resources\Workflows\Validation\*.yaml`
- `docs/changelogs/CHANGELOG.md` — Added v0.7.7h entry
