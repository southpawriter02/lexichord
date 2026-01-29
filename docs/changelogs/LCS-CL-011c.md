# LCS-CL-011c: Layout Serialization

**Version:** v0.1.1c  
**Category:** Host Infrastructure  
**Feature Name:** Layout Serialization  
**Date:** 2026-01-29

---

## Summary

Implements JSON-based layout persistence enabling save/load of dock hierarchies. The `ILayoutService` abstraction provides profile management, auto-save with debouncing, and schema versioning for future migrations.

---

## New Features

### Core Interfaces (Lexichord.Abstractions)

- **ILayoutService** — Primary service interface for layout persistence
    - `SaveLayoutAsync()` — Serializes current layout to JSON file
    - `LoadLayoutAsync()` — Restores layout from JSON file
    - `DeleteLayoutAsync()` — Removes a saved profile
    - `GetProfileNamesAsync()` — Lists all available profiles
    - `ProfileExistsAsync()` — Checks if profile exists
    - `ResetToDefaultAsync()` — Recreates default layout
    - `ExportLayoutAsync()` — Exports profile to external path
    - `ImportLayoutAsync()` — Imports layout from external path
    - `TriggerAutoSave()` — Manually triggers debounced auto-save

### Layout DTOs (Lexichord.Abstractions)

- **LayoutData** — Root container for serialized layouts
    - `Metadata` — Schema version, profile name, timestamp, app version
    - `Root` — Dock hierarchy starting from root node

- **LayoutMetadata** — Versioning and identification data
    - `SchemaVersion` — Current schema version (v1)
    - `ProfileName` — User-defined profile name
    - `SavedAt` — UTC timestamp of save
    - `AppVersion` — Application version at save time

- **DockNodeData** — Recursive dock node representation
    - `Id` — Unique node identifier
    - `Type` — Node type enum
    - `Properties` — Type-specific properties
    - `Children` — Child nodes for containers

- **DockNodeProperties** — Node configuration properties
    - `Title`, `Proportion`, `Orientation`, `Alignment`
    - `IsCollapsed`, `IsActive`, `ActiveChildId`
    - `CanClose`, `CanFloat`

- **Enums** — `DockNodeType`, `DockOrientation`, `DockAlignment`

- **Event Args** — `LayoutSavedEventArgs`, `LayoutLoadedEventArgs`

### Implementation (Lexichord.Host)

- **JsonLayoutService** — Concrete `ILayoutService` implementation
    - Atomic writes via temp file + rename
    - Auto-save with 500ms debounce
    - Profile storage in `{AppData}/Lexichord/Layouts/`
    - Path sanitization for security
    - Schema version migration stub

---

## Modified Files

### New Files

| File                                                                     | Description       |
| :----------------------------------------------------------------------- | :---------------- |
| `src/Lexichord.Abstractions/Layout/ILayoutService.cs`                    | Service interface |
| `src/Lexichord.Abstractions/Layout/LayoutData.cs`                        | Root DTO          |
| `src/Lexichord.Abstractions/Layout/LayoutMetadata.cs`                    | Metadata record   |
| `src/Lexichord.Abstractions/Layout/DockNodeData.cs`                      | Node DTO          |
| `src/Lexichord.Abstractions/Layout/DockNodeProperties.cs`                | Properties record |
| `src/Lexichord.Abstractions/Layout/DockNodeType.cs`                      | Node type enum    |
| `src/Lexichord.Abstractions/Layout/DockOrientation.cs`                   | Orientation enum  |
| `src/Lexichord.Abstractions/Layout/DockAlignment.cs`                     | Alignment enum    |
| `src/Lexichord.Abstractions/Layout/LayoutEventArgs.cs`                   | Event args        |
| `src/Lexichord.Host/Layout/JsonLayoutService.cs`                         | Implementation    |
| `tests/Lexichord.Tests.Unit/Host/Layout/JsonLayoutServiceTests.cs`       | Service tests     |
| `tests/Lexichord.Tests.Unit/Host/Layout/LayoutDataSerializationTests.cs` | DTO tests         |

### Modified Files

| File                                 | Changes                                             |
| :----------------------------------- | :-------------------------------------------------- |
| `src/Lexichord.Host/HostServices.cs` | Added `ILayoutService` singleton registration       |
| `src/Lexichord.Host/App.axaml.cs`    | Added `InitializeLayoutAsync()` startup integration |

---

## Technical Notes

### Atomic File Writes

Layout files are written atomically to prevent corruption:

```csharp
var tempPath = filePath + ".tmp";
await File.WriteAllTextAsync(tempPath, json, cancellationToken);
File.Move(tempPath, filePath, overwrite: true);
```

### Auto-Save Debouncing

Uses a timer to debounce rapid layout changes:

```csharp
_autoSaveTimer.Change(AutoSaveDelayMs, Timeout.Infinite);
```

### Schema Versioning

The `LayoutMetadata.CurrentSchemaVersion` constant enables future migrations:

```csharp
if (layoutData.Metadata.SchemaVersion < LayoutMetadata.CurrentSchemaVersion)
{
    layoutData = MigrateLayout(layoutData);
}
```

---

## Unit Tests

| Test                                              | Description            |
| :------------------------------------------------ | :--------------------- |
| `SaveLayoutAsync_NullRootDock_ReturnsFalse`       | Handles missing dock   |
| `SaveLayoutAsync_ValidRoot_CreatesFile`           | Verifies file creation |
| `SaveLayoutAsync_ContainsSchemaVersion`           | Validates JSON content |
| `LoadLayoutAsync_NonExistentFile_ReturnsFalse`    | Handles missing file   |
| `LoadLayoutAsync_ValidFile_ReturnsTrue`           | Verifies loading       |
| `LoadLayoutAsync_RaisesLayoutLoadedEvent`         | Event notification     |
| `GetProfileNamesAsync_ReturnsFileNames`           | Profile enumeration    |
| `ProfileExistsAsync_ExistingProfile_ReturnsTrue`  | Existence check        |
| `DeleteLayoutAsync_ExistingProfile_DeletesFile`   | Profile deletion       |
| `ExportLayoutAsync_CopiesFile`                    | Export functionality   |
| `ImportLayoutAsync_ValidFile_ImportsSuccessfully` | Import functionality   |
| `ResetToDefaultAsync_CreatesNewDefaultLayout`     | Reset verification     |
| `LayoutData_RoundTrip_PreservesData`              | Serialization fidelity |
| `DockNodeType_SerializesAsString`                 | Enum serialization     |

---

## Verification Commands

```bash
# Build verification
dotnet build

# Run layout tests
dotnet test --filter "FullyQualifiedName~Layout"

# Verify file locations
ls src/Lexichord.Abstractions/Layout/ILayoutService.cs
ls src/Lexichord.Host/Layout/JsonLayoutService.cs
```

---

## Related Documents

- **Specification:** [LCS-DES-011c.md](../specs/v0.1.x/v0.1.1/LCS-DES-011c.md)
- **Previous Sub-Part:** [LCS-CL-011b.md](./LCS-CL-011b.md)
