# v0.6.6a - Agent Abstractions

**Date**: 2026-02-08
**Component**: Core Abstractions / Agents
**Type**: New Feature (Abstractions)

## Summary

Defined the core agent abstraction layer establishing standardized contracts for all AI-powered assistants in Lexichord. These primitives form the foundation for the entire agent framework (v0.6.6 and beyond), enabling capability discovery, consistent request/response patterns, and transparent usage tracking.

## Features Added

### IAgent Interface

- Core contract for all agent implementations with `AgentId`, `Name`, `Description`, `Template`, `Capabilities`, and `InvokeAsync`
- Designed for stateless invocations — conversation state passed externally via `AgentRequest.History`
- Supports `[RequiresLicense]` decoration for license-tier gating

### AgentCapabilities Flags Enum

- `[Flags]` enum with 5 capabilities: `Chat` (1), `DocumentContext` (2), `RAGContext` (4), `StyleEnforcement` (8), `Streaming` (16)
- `None` (0) and convenience `All` (31) values
- Extension methods: `HasCapability()`, `SupportsContext()`, `GetCapabilityNames()`

### AgentRequest Record

- Immutable record with `UserMessage`, `History`, `DocumentPath`, `Selection`
- `Validate()` method throws on empty/whitespace messages
- Helper properties: `HasDocumentContext`, `HasSelection`, `HasHistory`, `HistoryCount`

### AgentResponse Record

- Immutable record with `Content`, `Citations` (IReadOnlyList<Citation>?), `Usage` (UsageMetrics)
- `Empty` and `Error()` factory methods for error-case responses
- Computed properties: `HasCitations`, `CitationCount`, `TotalTokens`

### UsageMetrics Record

- Immutable record with `PromptTokens`, `CompletionTokens`, `EstimatedCost`
- `Zero` sentinel for error/disabled cases
- `Add()` for accumulating across invocations
- `ToDisplayString()` for UI formatting (e.g., "2,000 tokens (~$0.0450)")
- `Calculate()` factory for computing cost from per-1K pricing

## Files Added

| Path                                                   | Lines |
| ------------------------------------------------------ | ----- |
| `src/Lexichord.Abstractions/Agents/IAgent.cs`          | ~155  |
| `src/Lexichord.Abstractions/Agents/AgentCapabilities.cs` | ~108  |
| `src/Lexichord.Abstractions/Agents/AgentCapabilitiesExtensions.cs` | ~115  |
| `src/Lexichord.Abstractions/Agents/AgentRequest.cs`    | ~126  |
| `src/Lexichord.Abstractions/Agents/AgentResponse.cs`   | ~128  |
| `src/Lexichord.Abstractions/Agents/UsageMetrics.cs`    | ~152  |

## Test Files Added

| Path                                                                    | Tests |
| ----------------------------------------------------------------------- | ----- |
| `tests/.../Abstractions/Agents/AgentCapabilitiesTests.cs`               | 11    |
| `tests/.../Abstractions/Agents/AgentRequestTests.cs`                    | 12    |
| `tests/.../Abstractions/Agents/AgentResponseTests.cs`                   | 11    |
| `tests/.../Abstractions/Agents/UsageMetricsTests.cs`                    | 13    |

## Dependencies

- No new package dependencies
- References existing types: `ChatMessage`, `ChatRole`, `Citation`, `IPromptTemplate` from `Lexichord.Abstractions`

## Related Documents

- [LCS-DES-v0.6.6a.md](../../specs/v0.6.x/v0.6.6/LCS-DES-v0.6.6a.md) - Design Specification
- [LCS-SBD-v0.6.6.md](../../specs/v0.6.x/v0.6.6/LCS-SBD-v0.6.6.md) - Scope Breakdown
- [LCS-DES-v0.6.6-INDEX.md](../../specs/v0.6.x/v0.6.6/LCS-DES-v0.6.6-INDEX.md) - Design Index
