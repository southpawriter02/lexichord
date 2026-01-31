# LCS-CL-036c: Override UI Changelog

**Version:** v0.3.6c  
**Feature ID:** INF-036c  
**Status:** ✅ Completed  
**Date:** 2026-01-31

## Summary

Implements the Override UI infrastructure enabling users to ignore rules and exclude terms via project-level configuration. Provides atomic YAML writes, license gating, and workspace validation.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                                       | Change                                                  |
| ------------------------------------------ | ------------------------------------------------------- |
| `Contracts/IProjectConfigurationWriter.cs` | New interface for project-level configuration overrides |

### Implementation (`Lexichord.Modules.Style`)

| File                                     | Change                                              |
| ---------------------------------------- | --------------------------------------------------- |
| `Models/OverrideAction.cs`               | Enum for context menu action types                  |
| `Services/ProjectConfigurationWriter.cs` | Service implementing atomic YAML writes             |
| `Services/StyleConfigurationDto`         | Mutable DTO for YAML deserialization                |
| `ViewModels/OverrideMenuViewModel.cs`    | ViewModel with license checks and command execution |
| `StyleModule.cs`                         | DI registrations for new services                   |

### Tests (`Lexichord.Tests.Unit`)

| File                                               | Change        |
| -------------------------------------------------- | ------------- |
| `Modules/Style/ProjectConfigurationWriterTests.cs` | 28 unit tests |
| `Modules/Style/OverrideMenuViewModelTests.cs`      | 13 unit tests |

## Key Features

- **Atomic Writes**: Temp file + rename pattern prevents corruption
- **YAML Deserialization Fix**: `StyleConfigurationDto` handles `IReadOnlyList` properties
- **License Gating**: Requires `Style.GlobalDictionary` feature (Writer Pro)
- **Workspace Validation**: Commands disabled when no workspace is open
- **Case-Insensitive**: All rule/term matching ignores case
- **Alphabetical Sorting**: Ignored rules and excluded terms sorted for readability

## Dependencies

| Interface              | Version | Purpose                                |
| ---------------------- | ------- | -------------------------------------- |
| `IWorkspaceService`    | v0.1.1  | Workspace path detection               |
| `ILicenseContext`      | v0.0.4  | Feature flag verification              |
| `StyleConfiguration`   | v0.3.6a | Configuration settings                 |
| `ILintingOrchestrator` | v0.2.3  | Re-analysis trigger (via file watcher) |

## Verification

```
Build: ✅ Succeeded
Tests: ✅ 2907 passed, 2 pre-existing flaky, 33 skipped
New Tests: ✅ 41 v0.3.6c tests (28 + 13)
```
