# Changelog: v0.2.6d Filter Scope

**Version:** 0.2.6d  
**Codename:** The Sentinel (Part 4)  
**Date:** 2026-01-30  
**Design Spec:** [LCS-DES-026d](../specs/v0.2.x/v0.2.6/LCS-DES-026d.md)

---

## Overview

Implements the backend infrastructure for project-wide and multi-document linting, enabling the Filter Scope feature in the Problems Panel.

---

## Changes

### Abstraction Layer

#### New Files

| File                                                                                                                                              | Description                                           |
| :------------------------------------------------------------------------------------------------------------------------------------------------ | :---------------------------------------------------- |
| [IProjectLintingService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/Linting/IProjectLintingService.cs) | Service interface for multi-file linting with caching |

#### New Types

- `IProjectLintingService` - Orchestrates project-wide linting
- `ProjectLintResult` - Result record for project linting with statistics
- `MultiLintResult` - Result record for open documents linting
- `ProjectLintProgress` - Progress reporting record

#### Modified Files

| File                                                                                                                                            | Change                                                        |
| :---------------------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------------------------------ |
| [ILintingConfiguration.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/Linting/ILintingConfiguration.cs) | Added `TargetExtensions` property                             |
| [LintingOptions.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/Linting/LintingOptions.cs)               | Implemented `TargetExtensions` with default `[".md", ".txt"]` |

---

### Implementation Layer

#### New Files

| File                                                                                                                                            | Description                                             |
| :---------------------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------------------------ |
| [ProjectLintingService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Services/Linting/ProjectLintingService.cs) | Implements multi-file linting with caching              |
| [IgnorePatternMatcher.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Services/Linting/IgnorePatternMatcher.cs)   | Gitignore-style pattern matching for `.lexichordignore` |

#### Key Features

- **Open Files Linting**: Scans all open documents via `IEditorService`
- **Project Linting**: Recursive file enumeration with extension filtering
- **Caching**: Thread-safe `ConcurrentDictionary` with LRU eviction (500 files max)
- **Cache Invalidation**: `InvalidateFile()` for on-save updates
- **Progress Reporting**: Via `IProgress<T>` and event handler
- **Ignore Patterns**: Gitignore-compatible `.lexichordignore` support

---

### Integration Layer

#### Modified Files

| File                                                                                                       | Change                                           |
| :--------------------------------------------------------------------------------------------------------- | :----------------------------------------------- |
| [StyleModule.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/StyleModule.cs) | Registered `IProjectLintingService` as singleton |

---

### Unit Tests

#### New Files

| File                                                                                                                                                          | Test Count |
| :------------------------------------------------------------------------------------------------------------------------------------------------------------ | :--------- |
| [ProjectLintingServiceTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/Linting/ProjectLintingServiceTests.cs) | 13 tests   |
| [IgnorePatternMatcherTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/Linting/IgnorePatternMatcherTests.cs)   | 45 tests   |

---

## Verification

```
dotnet test tests/Lexichord.Tests.Unit --verbosity minimal

Test summary: total: 2140, failed: 0, succeeded: 2112, skipped: 28
Build succeeded in 4.5s
```

---

## Dependencies

| Interface               | Version | Used By              |
| :---------------------- | :------ | :------------------- |
| `IStyleEngine`          | v0.2.1a | Content analysis     |
| `IEditorService`        | v0.2.6b | Open document access |
| `ILintingConfiguration` | v0.2.3b | Target extensions    |
| `IFileSystemAccess`     | v0.1.2  | File enumeration     |

---

## Notes

- ViewModel integration (`ProblemsPanelViewModel` updates) deferred to UI integration phase
- Existing `ScopeModeType` enum uses `Workspace` (value 2) - kept for backward compatibility
