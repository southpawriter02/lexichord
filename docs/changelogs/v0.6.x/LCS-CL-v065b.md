# Changelog — v0.6.5b: SSE Parser

**Status:** ✅ Complete
**Date:** 2026-02-06

## Summary

Implemented the Server-Sent Events (SSE) Parser for extracting streaming tokens from LLM provider HTTP response streams. The parser supports OpenAI and Anthropic JSON formats, detects provider-specific stream termination signals, and yields `StreamingChatToken` objects asynchronously for real-time display.

## What Changed

### New Files

| File | Description |
|:-----|:------------|
| `Chat/Abstractions/ISSEParser.cs` | Interface defining the SSE parsing contract with `ParseSSEStreamAsync` method |
| `Chat/Services/SSEParser.cs` | Stateless, thread-safe implementation with OpenAI and Anthropic JSON parsing |

### Modified Files

| File | Description |
|:-----|:------------|
| `AgentsModule.cs` | Registered `ISSEParser` → `SSEParser` as singleton; updated module version to 0.6.5 |

### Test Files

| File | Tests |
|:-----|:------|
| `SSEParserTests.cs` | 16 tests (36 with Theory expansion) covering both providers, error handling, guard clauses, cancellation, and edge cases |

## Key Design Decisions

- **Stateless singleton** — No instance state means the parser is inherently thread-safe and can be shared across concurrent requests without synchronization.
- **Malformed JSON resilience** — Bad JSON lines are logged at Warning level and skipped; the stream continues processing to avoid dropping entire responses due to a single corrupt line.
- **Provider-dispatched parsing** — Each provider has dedicated JSON extraction methods (`ParseOpenAIToken`, `ParseAnthropicToken`) for clean separation of format-specific logic.
- **ReadOnlySpan prefix slicing** — SSE `data:` prefix is sliced using `AsSpan()` to avoid unnecessary string allocations during line classification.

## Dependencies

- `StreamingChatToken` (v0.6.5a) — Token data contract
- `System.Text.Json` — JSON parsing
- `Microsoft.Extensions.Logging` — Diagnostic logging

## Verification

```bash
dotnet test --filter "FullyQualifiedName~SSEParserTests" --verbosity normal
# Result: 36 passed, 0 failed
```
