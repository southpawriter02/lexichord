# LCS-CL-024a: Rendering Transformer (StyleViolationRenderer)

**Version**: v0.2.4a
**Status**: ✅ Complete
**Date**: 2026-01-30

## Summary

Implemented the `StyleViolationRenderer` system for displaying wavy underlines beneath style violations in the AvaloniaEdit text editor. This completes the visual feedback loop for the linting engine, enabling users to see style violations in real-time.

## What's New

### Abstractions (`Lexichord.Abstractions`)

| File                                              | Description                                                                        |
| ------------------------------------------------- | ---------------------------------------------------------------------------------- |
| `Contracts/Linting/IViolationProvider.cs`         | Interface for providing violations to the rendering layer with change notification |
| `Contracts/Linting/IViolationColorProvider.cs`    | Interface for mapping severity levels to underline colors                          |
| `Contracts/Linting/ViolationsChangedEventArgs.cs` | Event args with change type and per-severity counts                                |
| `Contracts/Linting/UnderlineSegment.cs`           | Record for underline segment data (position, color, violation ID)                  |

### Style Module (`Lexichord.Modules.Style`)

| File                                         | Description                                            |
| -------------------------------------------- | ------------------------------------------------------ |
| `Services/Linting/ViolationProvider.cs`      | Thread-safe implementation of `IViolationProvider`     |
| `Services/Linting/ViolationColorProvider.cs` | Implementation mapping severity to standard IDE colors |

### Editor Module (`Lexichord.Modules.Editor`)

| File                                                    | Description                                                        |
| ------------------------------------------------------- | ------------------------------------------------------------------ |
| `Services/Rendering/StyleViolationRenderer.cs`          | `DocumentColorizingTransformer` that registers underlines per line |
| `Services/Rendering/WavyUnderlineBackgroundRenderer.cs` | `IBackgroundRenderer` that draws wavy lines using bezier curves    |

### Unit Tests

| File                                       | Tests                                          |
| ------------------------------------------ | ---------------------------------------------- |
| `Rendering/ViolationColorProviderTests.cs` | 8 tests for color mappings                     |
| `Rendering/ViolationProviderTests.cs`      | 15 tests for CRUD operations and thread safety |
| `Rendering/StyleViolationRendererTests.cs` | 12 tests for subscription management           |

## Technical Details

### Architecture

```
IViolationAggregator (Style)
         │
         ▼
IViolationProvider (Abstractions) ◄── ViolationProvider (Style)
         │
         ▼
StyleViolationRenderer (Editor)
         │
         ▼
WavyUnderlineBackgroundRenderer (Editor)
         │
         ▼
TextView (AvaloniaEdit)
```

### Color Scheme

| Severity | Color         | Hex       |
| -------- | ------------- | --------- |
| Error    | Red           | `#E51400` |
| Warning  | Yellow/Orange | `#FFC000` |
| Info     | Blue          | `#1E90FF` |
| Hint     | Gray          | `#A0A0A0` |

### Wavy Line Algorithm

The wavy underline is drawn using quadratic bezier curves:

- **Amplitude**: 1.5px (half peak-to-peak height)
- **Period**: 4px (one complete wave cycle)
- **Thickness**: 1px stroke width

### Thread Safety

- `ViolationProvider` uses lock-based synchronization for writes
- Read operations return immutable snapshots
- UI invalidation is marshaled to UI thread via `Dispatcher.UIThread.Post()`

## Dependencies

### New Interfaces

| Interface                 | Module       | Since   |
| ------------------------- | ------------ | ------- |
| `IViolationProvider`      | Abstractions | v0.2.4a |
| `IViolationColorProvider` | Abstractions | v0.2.4a |

### Service Registrations

```csharp
// StyleModule.cs
services.AddSingleton<IViolationColorProvider, ViolationColorProvider>();
```

> **Note**: `IViolationProvider` is scoped per-document, created by `ManuscriptViewModel` (integration in v0.2.4b).

## Verification

```bash
# Build both modules
dotnet build src/Lexichord.Modules.Style
dotnet build src/Lexichord.Modules.Editor

# Run unit tests
dotnet test --filter "FullyQualifiedName~Rendering"
# Result: 35 tests passed
```

## Related Documents

- **Design Spec**: [LCS-DES-024a](../specs/v0.2.x/v0.2.4/LCS-DES-024a.md)
- **Scope Breakdown**: [LCS-SBD-024](../specs/v0.2.x/v0.2.4/LCS-SBD-024.md)
- **Previous**: [LCS-CL-023d](LCS-CL-023d.md) (Violation Aggregator)
