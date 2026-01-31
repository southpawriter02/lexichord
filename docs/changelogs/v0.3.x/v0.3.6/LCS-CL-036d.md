# Changelog v0.3.6d: Ignored Files

**Date:** 2026-01-31
**Version:** v0.3.6d
**Status:** ✅ Complete

## Summary

Implemented `.lexichordignore` file support for excluding files from style analysis using glob patterns. This feature integrates with the linting pipeline to skip ignored files before scanning, with license-gated pattern limits (Core: 5 patterns, Writer Pro: unlimited) and hot-reload support via file watcher.

## Changes by Layer

### Abstractions Layer

#### [NEW] [IIgnorePatternService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/IIgnorePatternService.cs)

Interface defining the ignore pattern service contract:

- `IsIgnored(filePath)` - Check if a path matches ignore patterns
- `RegisterPatterns(patterns)` - Load patterns from collection
- `LoadFromWorkspaceAsync(path)` - Load from `.lexichord/.lexichordignore`
- `Clear()` - Remove all patterns and stop file watcher
- `PatternCount`, `TruncatedPatternCount`, `IsWatching` properties
- `PatternsReloaded` event for hot-reload notifications

#### [NEW] [PatternsReloadedEventArgs.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/PatternsReloadedEventArgs.cs)

Event args record carrying pattern reload data:

- `PatternCount` - Active patterns after reload
- `TruncatedCount` - Patterns dropped due to license limit
- `Source` - File path that triggered reload

---

### Service Layer

#### [NEW] [IgnorePatternService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Services/IgnorePatternService.cs)

Full implementation with:

- **Glob Matching**: Uses `Microsoft.Extensions.FileSystemGlobbing` for pattern matching
- **Negation Patterns**: `!` prefix to un-ignore previously matched files
- **License Gating**: Core tier limited to 5 patterns, Writer Pro unlimited
- **Hot-Reload**: FileSystemWatcher with 100ms debounce for `.lexichordignore` changes
- **Thread Safety**: `ReaderWriterLockSlim` for concurrent read access
- **File Limits**: 100KB max file size, comment (`#`) and empty line support

#### [MODIFY] [LintingOrchestrator.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Services/Linting/LintingOrchestrator.cs)

- Added optional `IIgnorePatternService` constructor parameter
- Early-exit in `Subscribe()` for ignored files, returning empty disposable

#### [MODIFY] [StyleModule.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/StyleModule.cs)

- Registered `IIgnorePatternService` as singleton

#### [MODIFY] [Lexichord.Modules.Style.csproj](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Lexichord.Modules.Style.csproj)

- Added `Microsoft.Extensions.FileSystemGlobbing` package reference

---

### Tests

#### [NEW] [IgnorePatternServiceTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/IgnorePatternServiceTests.cs)

28 comprehensive tests covering:

- Glob pattern matching (simple, directory, deep nesting)
- Negation patterns and re-ignore behavior
- Case-insensitive matching
- License tier enforcement (Core truncation, Writer Pro unlimited)
- File loading and hot-reload
- Thread safety (concurrent reads, writes during reads)
- Performance (warmup + 100 paths in <500ms)
- Constructor validation

## Verification Results

```
Build: ✅ Succeeded
Tests: 28 passed, 0 failed, 0 skipped
Duration: <1s
```

## Dependencies

| Dependency                              | Version | Purpose               |
| --------------------------------------- | ------- | --------------------- |
| Microsoft.Extensions.FileSystemGlobbing | 9.0.3   | Glob pattern matching |

## File Path Convention

Ignore file location: `{workspace}/.lexichord/.lexichordignore`

## Pattern Syntax

| Pattern          | Description                     |
| ---------------- | ------------------------------- |
| `*.log`          | All .log files                  |
| `build/**`       | All files under build/          |
| `**/temp/**`     | Any temp directory at any depth |
| `!important.log` | Exception to previous pattern   |
| `# comment`      | Comment (ignored)               |
