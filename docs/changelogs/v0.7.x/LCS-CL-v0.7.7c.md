# LCS-CL-v0.7.7c — Preset Workflows

| Field        | Value                                                                         |
|:-------------|:------------------------------------------------------------------------------|
| **Version**  | v0.7.7c                                                                       |
| **Codename** | The Preset Gallery                                                            |
| **Date**     | 2026-02-23                                                                    |
| **Specification** | [LCS-DES-v0.7.7c](../../specs/v0.7.x/v0.7.7/LCS-DES-v0.7.7c.md)        |

---

## Summary

Introduces 5 production-ready preset workflows as embedded YAML resources, accessible via `IPresetWorkflowRepository`. Presets provide immediate value by offering ready-to-use agent pipelines for common document processing tasks without requiring users to design workflows from scratch.

## New Files

| File | Module | Purpose |
|:-----|:-------|:--------|
| `Workflows/IPresetWorkflowRepository.cs` | Modules.Agents | Repository interface with 5 query methods |
| `Workflows/PresetWorkflowSummary.cs` | Modules.Agents | Display-ready summary record (9 properties) |
| `Workflows/PresetWorkflowRepository.cs` | Modules.Agents | Implementation with embedded resource loading and YAML DTOs |
| `Resources/Workflows/technical-review.yaml` | Modules.Agents | 4-step pipeline: editor→simplifier→tuning→summarizer |
| `Resources/Workflows/marketing-polish.yaml` | Modules.Agents | 4-step pipeline: simplifier→editor→tuning→summarizer |
| `Resources/Workflows/quick-edit.yaml` | Modules.Agents | 1-step pipeline: editor |
| `Resources/Workflows/academic-review.yaml` | Modules.Agents | 3-step pipeline: editor→tuning→summarizer |
| `Resources/Workflows/executive-summary.yaml` | Modules.Agents | 3-step pipeline: simplifier→editor→summarizer |

## Modified Files

| File | Change |
|:-----|:-------|
| `Lexichord.Modules.Agents.csproj` | Added `EmbeddedResource` glob for `Resources\Workflows\*.yaml` |
| `AgentsModule.cs` | Added DI registration and InitializeAsync verification for `IPresetWorkflowRepository` |

## DI Registrations

| Registration | Lifetime | Purpose |
|:-------------|:---------|:--------|
| `IPresetWorkflowRepository → PresetWorkflowRepository` | Singleton | Preset workflow access |

## Preset Workflows

| ID | Name | Category | Steps | License |
|:---|:-----|:---------|:------|:--------|
| `preset-technical-review` | Technical Review | Technical | 4 | WriterPro |
| `preset-marketing-polish` | Marketing Polish | Marketing | 4 | WriterPro |
| `preset-quick-edit` | Quick Edit | General | 1 | WriterPro |
| `preset-academic-review` | Academic Review | Academic | 3 | Teams |
| `preset-executive-summary` | Executive Summary | General | 3 | Teams |

## Test Results

| Suite | Tests | Passed | Failed | Skipped |
|:------|:------|:-------|:-------|:--------|
| PresetWorkflowRepositoryTests | 16 | 16 | 0 | 0 |
| Full Regression | 11,110 | 11,076 | 1* | 33 |

*Pre-existing failure in `IgnorePatternMatcherTests.IsIgnored_WildcardPattern_MatchesCorrectly` (Style module, unrelated).
