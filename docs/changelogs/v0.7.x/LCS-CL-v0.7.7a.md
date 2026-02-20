# LCS-CL-v0.7.7a — Workflow Designer UI

**Version:** v0.7.7a
**Codename:** The Blueprint Designer
**Date:** 2026-02-20
**Specification:** [LCS-DES-v0.7.7a](../../specs/v0.7.x/v0.7.7/LCS-DES-v0.7.7a.md)

---

## Summary

Introduces the Workflow Designer UI — a visual, drag-and-drop workflow builder that allows users to create multi-step agent pipelines. This sub-version delivers the core data contracts, designer service (CRUD, validation, YAML import/export), and ViewModel layer for the designer canvas, palette, and step configuration panel.

## New Files

### Data Contracts
| File | Description |
|------|-------------|
| `Workflows/WorkflowDefinition.cs` | `WorkflowDefinition`, `WorkflowStepDefinition`, `WorkflowStepCondition` records; `ConditionType` enum |
| `Workflows/WorkflowMetadata.cs` | `WorkflowMetadata` record; `WorkflowCategory` enum |
| `Workflows/WorkflowValidationResult.cs` | `WorkflowValidationResult`, `WorkflowValidationError`, `WorkflowValidationWarning`, `WorkflowSummary` records |

### Service Layer
| File | Description |
|------|-------------|
| `Workflows/IWorkflowDesignerService.cs` | Interface: CRUD, validation, YAML, duplication, listing |
| `Workflows/WorkflowDesignerService.cs` | Implementation: in-memory `ConcurrentDictionary` storage, 5 error + 3 warning validation rules |
| `Workflows/WorkflowYamlSerializer.cs` | YamlDotNet-based serializer with snake_case DTO mapping |
| `Workflows/WorkflowImportException.cs` | Custom exception for import failures |
| `IConfigurationService.cs` | Minimal abstraction in `Lexichord.Abstractions.Contracts` |

### ViewModel Layer
| File | Description |
|------|-------------|
| `ViewModels/IWorkflowDesignerViewModel.cs` | Interface with 10 commands and all observable properties |
| `ViewModels/WorkflowDesignerViewModel.cs` | Full implementation: palette population, step CRUD, reorder, license gating, validation |
| `ViewModels/WorkflowStepViewModel.cs` | Per-step ViewModel with identity, config, condition, and UI state properties |
| `ViewModels/AgentPaletteItemViewModel.cs` | Record for palette agent cards |
| `ViewModels/PersonaOption.cs` | Record for persona dropdown |

### Modified Files
| File | Change |
|------|--------|
| `AgentsModule.cs` | Added `IWorkflowDesignerService → WorkflowDesignerService` (Singleton), `WorkflowDesignerViewModel` (Transient), init verification |

## Validation Rules

### Errors (block save)
| Code | Description |
|------|-------------|
| `MISSING_NAME` | Workflow name is required |
| `EMPTY_WORKFLOW` | At least one step is required |
| `UNKNOWN_AGENT` | Step references unregistered agent |
| `UNKNOWN_PERSONA` | Step references unregistered persona |
| `DUPLICATE_STEP_ID` | Step IDs must be unique |

### Warnings (informational)
| Code | Description |
|------|-------------|
| `SINGLE_STEP` | Only one step — consider adding more |
| `MISSING_DESCRIPTION` | Workflow has no description |
| `ALL_SAME_AGENT` | All steps use the same agent |

## Test Results

- **v0.7.7a tests:** 19 passed, 0 failed
- **Full regression:** 11,047 passed, 33 skipped, 1 pre-existing failure (Knowledge module, unrelated)
- **Regressions introduced:** 0

## License Gating

- **Editing:** Requires `LicenseTier.Teams` or higher
- **Read-only access:** Available at all tiers
