# LCS-CL-v065a: Changelog — Streaming Token Model

| Field       | Value      |
| :---------- | :--------- |
| **Version** | v0.6.5a    |
| **Date**    | 2026-02-06 |
| **Status**  | Complete   |
| **Module**  | Lexichord.Modules.Agents |

---

## Summary

Defines the core data contracts for real-time streaming LLM responses — the foundational primitives consumed by the SSE parser (v0.6.5b), handler implementation (v0.6.5c), and orchestration layer (v0.6.5d).

## New Features

### StreamingChatToken Record
- Immutable record with `Text`, `Index`, `IsComplete`, `FinishReason` properties
- `Content(text, index)` factory for content tokens
- `Complete(index, finishReason)` factory for completion tokens
- `HasContent` computed property

### IStreamingChatHandler Interface
- `OnTokenReceived(StreamingChatToken)` — per-token callback
- `OnStreamComplete(ChatResponse)` — stream finalization (uses v0.6.1a `ChatResponse`)
- `OnStreamError(Exception)` — error handling

### StreamingState Enum
- 6 states: `Idle`, `Connecting`, `Streaming`, `Completed`, `Cancelled`, `Error`
- Extension methods: `IsActive()`, `IsTerminal()`, `CanCancel()`, `ShowTypingIndicator()`, `InputEnabled()`

## New Files

| File | Location |
| :--- | :------- |
| `StreamingChatToken.cs` | `Modules.Agents/Chat/Models/` |
| `StreamingState.cs` | `Modules.Agents/Chat/Models/` |
| `IStreamingChatHandler.cs` | `Modules.Agents/Chat/Abstractions/` |
| `StreamingChatTokenTests.cs` | `Tests.Unit/Modules/Agents/Chat/Models/` |
| `StreamingStateTests.cs` | `Tests.Unit/Modules/Agents/Chat/Models/` |

## Unit Tests

9 test methods (5 token + 4 state), all passing.

## Dependencies

- **v0.6.1a** — `ChatResponse` (used by `IStreamingChatHandler.OnStreamComplete`)

## Design Rationale

- **Separate from v0.6.1a `StreamingChatToken`**: The Abstractions version is a lightweight DTO for `IChatCompletionService.StreamAsync`. This Agents version adds `Index` for ordering, supporting the richer streaming pipeline.
- **`InputEnabled` uses `!IsActive()`**: Matches the UI spec table (§7.1) — input is enabled for all non-active states including `Completed` and `Cancelled` (transient states that auto-transition to Idle).

## Verification

```bash
dotnet build src/Lexichord.Modules.Agents/Lexichord.Modules.Agents.csproj
dotnet test tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj --filter "FullyQualifiedName~Modules.Agents.Chat.Models"
```
