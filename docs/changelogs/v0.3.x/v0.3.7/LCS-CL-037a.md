# LCS-CL-037a: Background Buffering

| Field           | Value                                                            |
| --------------- | ---------------------------------------------------------------- |
| **Document ID** | LCS-CL-037a                                                      |
| **Status**      | ✅ Complete                                                      |
| **Version**     | v0.3.7a                                                          |
| **Parent**      | [LCS-DES-037](../../../specs/v0.3.x/v0.3.7/LCS-DES-037-INDEX.md) |

## Summary

Implements the analysis request buffer using System.Reactive for debouncing and latest-wins semantics. This is the first component of the "Performance Tuning" feature set that improves typing responsiveness by buffering rapid document changes.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                       | Change                                                       |
| -------------------------- | ------------------------------------------------------------ |
| `AnalysisRequest.cs`       | New record capturing document analysis request with snapshot |
| `AnalysisBufferOptions.cs` | New configuration class for buffer idle-period and limits    |
| `IAnalysisBuffer.cs`       | New interface defining buffer contract with Rx observable    |

### Style Module (`Lexichord.Modules.Style`)

| File                | Change                                                    |
| ------------------- | --------------------------------------------------------- |
| `AnalysisBuffer.cs` | New implementation using System.Reactive GroupBy+Throttle |
| `StyleModule.cs`    | Registered `IAnalysisBuffer` as singleton                 |

### Tests (`Lexichord.Tests.Unit`)

| File                     | Tests |
| ------------------------ | ----- |
| `AnalysisBufferTests.cs` | 20    |

## Key Implementation Details

### AnalysisRequest Record

```csharp
public sealed record AnalysisRequest(
    string DocumentId,
    string? FilePath,
    string Content,
    DateTimeOffset RequestedAt,
    CancellationToken CancellationToken = default);
```

- Factory method `Create()` for convenience with auto timestamp
- `WithCancellationToken()` for linked token support

### AnalysisBuffer Pipeline

Uses System.Reactive operators for per-document debouncing:

```csharp
_inputSubject
    .GroupBy(r => r.DocumentId)
    .SelectMany(group => group.Throttle(idlePeriod))
    .Subscribe(OnDebounceComplete);
```

- **GroupBy**: Separates requests into per-document streams
- **Throttle**: Implements idle-period debouncing (latest-wins)
- **ConcurrentDictionary**: Tracks pending CancellationTokenSources

### Configuration Options

| Option                   | Default | Description                    |
| ------------------------ | ------- | ------------------------------ |
| `IdlePeriodMilliseconds` | 300     | Quiet period before processing |
| `MaxBufferedDocuments`   | 100     | Maximum pending documents      |
| `Enabled`                | true    | Enable/disable buffering       |

## Verification

- **Build**: ✅ 0 warnings, 0 errors
- **Unit Tests**: ✅ 20 new tests passing
- **Regression**: ✅ All 260 linting tests passing

## Dependencies

- Consumes: `System.Reactive` v6.0.1
- Produces: `IObservable<AnalysisRequest>` for downstream consumers
