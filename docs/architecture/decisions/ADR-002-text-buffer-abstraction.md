# ADR-002: Text Buffer Abstraction for CRDT Compatibility

## Status

**Accepted** — 2026-01-27

## Context

The Future Horizons Roadmap item **[COL-01] Real-Time Sync** aims to implement Conflict-free Replicated Data Types (CRDTs) for Google Docs-style collaborative editing.

The current v0.1.3 implementation directly couples:

- `ManuscriptViewModel.Content` (string property)
- `TextEditor.Text` (AvalonEdit's internal string representation)

This architecture is **incompatible with CRDTs** because:

1. CRDTs represent text as operation logs, not strings
2. Mutations must be applied as operations (insert/delete at position), not full string replacements
3. Change events need to convey operations, not just "text changed"

Retrofitting CRDT support after v1.0 would require rewriting all modules that depend on `IManuscriptViewModel` and `IEditorService`.

## Decision

Introduce an **`ITextBuffer`** abstraction between the ViewModel and the underlying text representation.

```
┌──────────────────────┐      ┌──────────────────────┐
│  ManuscriptViewModel │ ──→  │     ITextBuffer      │
│                      │      │                      │
│  - Uses ITextBuffer  │      │  - GetText()         │
│  - Never sees raw    │      │  - ApplyOperation()  │
│    string mutations  │      │  - Changes stream    │
└──────────────────────┘      └──────────────────────┘
                                       │
                    ┌──────────────────┴──────────────────┐
                    ↓                                     ↓
         ┌──────────────────────┐           ┌──────────────────────┐
         │  AvalonEditTextBuffer │           │    CrdtTextBuffer    │
         │  (v0.1.3 default)     │           │    (v2.0+ Yjs/Loro)  │
         └──────────────────────┘           └──────────────────────┘
```

### Interface Definition

```csharp
public interface ITextBuffer
{
    /// <summary>Gets the current text snapshot.</summary>
    string GetText();

    /// <summary>Applies a text operation (insert/delete).</summary>
    void ApplyOperation(TextOperation operation);

    /// <summary>Observable stream of text changes.</summary>
    IObservable<TextChange> Changes { get; }

    /// <summary>Gets the character count.</summary>
    int Length { get; }
}

public record TextOperation(
    TextOperationType Type,
    int Position,
    string? Text = null,
    int? DeleteCount = null);

public enum TextOperationType { Insert, Delete, Replace }

public record TextChange(
    TextOperation Operation,
    string DocumentSnapshot);
```

## Consequences

### Positive

- **Future-proof**: Swapping to CRDT requires only a new `ITextBuffer` implementation
- **Testability**: Can mock `ITextBuffer` for ViewModel tests
- **Consistency**: All text mutations go through a single interface

### Negative

- **Slight complexity**: Additional abstraction layer in v0.1.3
- **Migration work**: Existing code must be updated to use interface

### Neutral

- v0.1.3 ships with `AvalonEditTextBuffer` wrapping `TextDocument`
- No user-visible changes in v1.0

## Implementation

### Files to Create (v0.1.3)

1. `Lexichord.Abstractions/Contracts/ITextBuffer.cs`
2. `Lexichord.Abstractions/Contracts/TextOperation.cs`
3. `Lexichord.Modules.Editor/Services/AvalonEditTextBuffer.cs`

### Files to Modify

1. `ManuscriptViewModel.cs` — Use `ITextBuffer` instead of `string Content`
2. `ManuscriptView.axaml.cs` — Bind to buffer, not raw text
3. `EditorModule.cs` — Register `AvalonEditTextBuffer` as default

## Related Documents

- [LCS-DES-013-INDEX](../../specs/v0.1.x/v0.1.3/LCS-DES-013-INDEX.md) — Editor Module Spec
- [LCS-DES-013a](../../specs/v0.1.x/v0.1.3/LCS-DES-013a.md) — AvalonEdit Integration
- Future: [COL-01] Real-Time Sync (v2.0+ Roadmap)
