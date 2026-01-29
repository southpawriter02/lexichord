# v0.1.3b: Syntax Highlighting Service

**Specification**: LCS-DES-013b (Syntax Highlighting Design) / LCS-SBD-013 (Scope Breakdown)  
**Released**: 2026-01 (Alpha)  
**Breaking Changes**: None

---

## Overview

Implementation of the syntax highlighting service for the Editor module, providing theme-aware syntax coloring for Markdown, JSON, YAML, and XML file types. This version loads XSHD definitions from embedded resources and automatically adapts colors when the application theme changes.

---

## New Files

### Abstractions Layer

| File                                              | Description                                               |
| ------------------------------------------------- | --------------------------------------------------------- |
| `Contracts/Editor/SyntaxHighlightingContracts.cs` | `EditorTheme` enum, `HighlightingChangedEventArgs`, enums |

### Editor Module

| File                                        | Description                                               |
| ------------------------------------------- | --------------------------------------------------------- |
| `Services/XshdHighlightingService.cs`       | `ISyntaxHighlightingService` interface and implementation |
| `Resources/Highlighting/Markdown.xshd`      | Light theme Markdown highlighting                         |
| `Resources/Highlighting/Markdown.Dark.xshd` | Dark theme Markdown highlighting                          |
| `Resources/Highlighting/JSON.xshd`          | Light theme JSON highlighting                             |
| `Resources/Highlighting/JSON.Dark.xshd`     | Dark theme JSON highlighting                              |
| `Resources/Highlighting/YAML.xshd`          | Light theme YAML highlighting                             |
| `Resources/Highlighting/YAML.Dark.xshd`     | Dark theme YAML highlighting                              |
| `Resources/Highlighting/XML.xshd`           | Light theme XML highlighting                              |
| `Resources/Highlighting/XML.Dark.xshd`      | Dark theme XML highlighting                               |

### Tests

| File                                             | Description                            |
| ------------------------------------------------ | -------------------------------------- |
| `Modules/Editor/XshdHighlightingServiceTests.cs` | 35 unit tests for highlighting service |

---

## New Contracts

### ISyntaxHighlightingService

```csharp
public interface ISyntaxHighlightingService
{
    IHighlightingDefinition? GetHighlighting(string fileExtension);
    IHighlightingDefinition? GetHighlightingByName(string name);
    void RegisterHighlighting(string name, IReadOnlyList<string> extensions, IHighlightingDefinition definition);
    void RegisterHighlightingFromXshd(string name, IReadOnlyList<string> extensions, Stream xshdStream);
    void SetTheme(EditorTheme theme);
    EditorTheme CurrentTheme { get; }
    IReadOnlyList<string> GetAvailableHighlightings();
    IReadOnlyList<string> GetExtensionsForHighlighting(string name);
    Task LoadDefinitionsAsync();
    event EventHandler<HighlightingChangedEventArgs>? HighlightingChanged;
}
```

### EditorTheme Enum

```csharp
public enum EditorTheme
{
    Light,
    Dark
}
```

### HighlightingChangeReason Enum

```csharp
public enum HighlightingChangeReason
{
    ThemeChanged,
    DefinitionRegistered,
    DefinitionUnregistered,
    DefinitionsReloaded
}
```

---

## Implementation Details

### Extension Mapping

| Language | Extensions                                                                                              |
| -------- | ------------------------------------------------------------------------------------------------------- |
| Markdown | `.md`, `.markdown`, `.mdown`, `.mkd`                                                                    |
| JSON     | `.json`, `.jsonc`                                                                                       |
| YAML     | `.yml`, `.yaml`                                                                                         |
| XML      | `.xml`, `.xsd`, `.xsl`, `.xslt`, `.xaml`, `.axaml`, `.xshd`, `.config`, `.csproj`, `.props`, `.targets` |

### Theme Adaptation Flow

1. `IThemeManager.ThemeChanged` event fires
2. `XshdHighlightingService.OnThemeChanged` maps `ThemeMode` → `EditorTheme`
3. `SetTheme` clears definition cache
4. `HighlightingChanged` event fires with `ThemeChanged` reason
5. Editors re-fetch definitions, which load from theme-appropriate XSHD

### Definition Loading Strategy

- **Light Theme**: Loads `{Name}.xshd`
- **Dark Theme**: Loads `{Name}.Dark.xshd`, falls back to `{Name}.xshd`

### Custom Registration

Modules can register custom highlighting definitions:

```csharp
highlightingService.RegisterHighlighting("Python", [".py"], customDefinition);
// Or from XSHD stream:
highlightingService.RegisterHighlightingFromXshd("Python", [".py"], xshdStream);
```

---

## Test Summary

| Test Class                   | Tests | Status    |
| ---------------------------- | ----- | --------- |
| XshdHighlightingServiceTests | 35    | ✅ Passed |

### Coverage Areas

- Built-in definition loading (Markdown, JSON, YAML, XML)
- Extension-to-highlighting mapping
- Case-insensitive extension handling
- Theme change cache invalidation
- Custom definition registration
- Theme manager integration

---

## Dependencies

| Dependency             | Version | Purpose                              |
| ---------------------- | ------- | ------------------------------------ |
| Avalonia.AvaloniaEdit  | 11.1.0  | XSHD parsing and highlighting engine |
| Lexichord.Abstractions | -       | Core contracts (`IThemeManager`)     |

---

## Design Notes

> [!NOTE]
> **Interface Location**: `ISyntaxHighlightingService` is defined in the module layer (not Abstractions) because it depends on `AvaloniaEdit.Highlighting.IHighlightingDefinition`. This maintains the Abstractions layer's independence from AvaloniaEdit.

---

## Deferred Features

| Feature                 | Target Version |
| ----------------------- | -------------- |
| C# syntax highlighting  | Future         |
| TypeScript highlighting | Future         |
| CSS/HTML highlighting   | Future         |
| Custom theme import     | Future         |

---

## Related Specifications

- [LCS-DES-013b: Syntax Highlighting Design](../specs/v0.1.x/v0.1.3/LCS-DES-013b.md)
- [LCS-DES-013-INDEX: Editor Design Index](../specs/v0.1.x/v0.1.3/LCS-DES-013-INDEX.md)
- [LCS-SBD-013: v0.1.3 Scope Breakdown](../specs/v0.1.x/v0.1.3/LCS-SBD-013.md)
