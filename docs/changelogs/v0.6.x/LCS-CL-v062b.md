# LCS-CL-062b: Detailed Changelog — Anthropic Connector

## Metadata

| Field        | Value                                        |
| :----------- | :------------------------------------------- |
| **Version**  | v0.6.2b                                      |
| **Released** | 2026-02-04                                   |
| **Category** | Implementation / Module                      |
| **Parent**   | [v0.6.2 Changelog](../CHANGELOG.md#v062)     |
| **Spec**     | [LCS-DES-062b](../../specs/v0.6.x/v0.6.2/LCS-DES-v0.6.2b.md) |

---

## Summary

This release implements the Anthropic Connector for the LLM module, providing production-ready integration with the Anthropic Messages API for Claude models. It supports both synchronous completion and streaming responses via Server-Sent Events (SSE), with Anthropic-specific event type handling, robust error handling, resilience policies, and comprehensive logging.

---

## New Features

### 1. Anthropic Configuration Record

Added `AnthropicOptions` record for configuring the Anthropic provider:

```csharp
public record AnthropicOptions(
    string BaseUrl = "https://api.anthropic.com/v1",
    string DefaultModel = "claude-3-haiku-20240307",
    string ApiVersion = "2024-01-01",
    int MaxRetries = 3,
    int TimeoutSeconds = 30)
{
    public const string VaultKey = "anthropic:api-key";
    public const string HttpClientName = "Anthropic";
    public string MessagesEndpoint => $"{BaseUrl}/messages";
    public static IReadOnlyList<string> SupportedModels { get; }
}
```

**Supported Models:**
- claude-3-5-sonnet-20241022 (most capable Sonnet)
- claude-3-opus-20240229 (most capable overall)
- claude-3-sonnet-20240229 (balanced performance)
- claude-3-haiku-20240307 (fast and cost-effective, default)

**File:** `src/Lexichord.Modules.LLM/Providers/Anthropic/AnthropicOptions.cs`

### 2. Anthropic Chat Completion Service

Added `AnthropicChatCompletionService` implementing `IChatCompletionService`:

| Method | Description |
| ------ | ----------- |
| `CompleteAsync(request, ct)` | Sends request and returns complete response |
| `StreamAsync(request, ct)` | Streams tokens via SSE as `IAsyncEnumerable<StreamingChatToken>` |

**Key Features:**

| Feature | Implementation |
| ------- | -------------- |
| Authentication | `x-api-key` header from `ISecureVault` via `anthropic:api-key` |
| Version Header | `anthropic-version: 2024-01-01` required by Anthropic API |
| Request Building | Uses existing `AnthropicParameterMapper.ToRequestBody()` |
| Streaming | Custom event-type aware parsing (not OpenAI's data-only format) |
| Error Handling | Maps HTTP status codes and error types to exception hierarchy |
| Logging | Comprehensive structured logging (events 1700-1715) |

**Error Mapping:**

| Error Type / Status | Exception Type |
| ------------------- | -------------- |
| 401 Unauthorized | `AuthenticationException` |
| 403 Forbidden | `AuthenticationException` |
| rate_limit_error | `RateLimitException` |
| 429 Too Many Requests | `RateLimitException` |
| overloaded_error | `ChatCompletionException` |
| invalid_request_error | `ChatCompletionException` |
| 5xx Server Error | `ChatCompletionException` |
| Missing API Key | `ProviderNotConfiguredException` |

**API Differences from OpenAI:**

| Aspect | OpenAI | Anthropic |
| ------ | ------ | --------- |
| Authentication | `Authorization: Bearer ...` | `x-api-key: ...` |
| Version Header | Not required | `anthropic-version: 2024-01-01` |
| System Message | In messages array | Separate `system` field |
| Response Content | Single string | Array of content blocks |
| Token Usage | `prompt_tokens`, `completion_tokens` | `input_tokens`, `output_tokens` |
| Stop Reason | `finish_reason` | `stop_reason` |
| Streaming Events | `data: {...}` only | `event: type\ndata: {...}` |

**File:** `src/Lexichord.Modules.LLM/Providers/Anthropic/AnthropicChatCompletionService.cs`

### 3. Response Parser

Added `AnthropicResponseParser` for handling API responses:

| Method | Return | Description |
| ------ | ------ | ----------- |
| `ParseSuccessResponse(body, duration)` | `ChatResponse` | Parse successful completion response |
| `ParseStreamingEvent(eventType, data)` | `StreamingChatToken?` | Parse SSE streaming event |
| `ParseErrorResponse(status, body)` | `ChatCompletionException` | Map error to exception |
| `ExtractTokenUsage(body)` | `(int, int)` | Extract input/output token counts |

**Streaming Event Types:**

| Event Type | Handling |
| ---------- | -------- |
| `message_start` | Ignored (metadata) |
| `content_block_start` | Ignored (block metadata) |
| `content_block_delta` | Yields token content |
| `content_block_stop` | Ignored (block end) |
| `message_delta` | Yields complete token with stop_reason |
| `message_stop` | Yields complete token (stream end) |
| `ping` | Ignored (keep-alive) |
| `error` | Throws ChatCompletionException |

**File:** `src/Lexichord.Modules.LLM/Providers/Anthropic/AnthropicResponseParser.cs`

### 4. DI Registration Extensions

Added `AnthropicServiceCollectionExtensions` for service registration:

```csharp
services.AddAnthropicProvider(configuration);
```

**Registers:**
- `AnthropicOptions` bound from `LLM:Providers:Anthropic` configuration section
- Named HTTP client "Anthropic" with resilience policies
- `AnthropicChatCompletionService` as keyed singleton provider
- Provider metadata with `ILLMProviderRegistry`

**Resilience Policies:**
- **Retry**: Exponential backoff (2^n seconds + jitter) for transient errors
  - Handles 429 Too Many Requests
  - Handles 529 Overloaded (Anthropic-specific)
  - Respects Retry-After header when present
- **Circuit Breaker**: Opens after 5 failures, 30-second break duration

**File:** `src/Lexichord.Modules.LLM/Extensions/AnthropicServiceCollectionExtensions.cs`

### 5. Structured Logging Events (LLM Module)

Added Anthropic provider logging events (1700-1715 range):

| Event ID | Level | Description |
| -------- | ----- | ----------- |
| 1700 | Debug | Starting Anthropic completion request |
| 1701 | Debug | Estimated prompt tokens |
| 1702 | Information | Anthropic completion succeeded |
| 1703 | Debug | Starting Anthropic streaming request |
| 1704 | Debug | Anthropic stream started |
| 1705 | Trace | Anthropic stream chunk received |
| 1706 | Information | Anthropic stream completed |
| 1707 | Warning | Anthropic API returned error |
| 1708 | Error | Anthropic HTTP request failed |
| 1709 | Warning | Failed to parse streaming chunk |
| 1710 | Debug | Building Anthropic HTTP request |
| 1711 | Debug | Retrieving API key from vault |
| 1712 | Warning | API key not found in vault |
| 1713 | Debug | Parsing Anthropic success response |
| 1714 | Debug | Parsing Anthropic error response |
| 1715 | Trace | Raw Anthropic response size |

**File:** `src/Lexichord.Modules.LLM/Logging/LLMLogEvents.cs`

---

## Files Changed

### Created (3 files)

| File | Lines | Description |
| ---- | ----- | ----------- |
| `Modules.LLM/Providers/Anthropic/AnthropicOptions.cs` | ~190 | Configuration record |
| `Modules.LLM/Providers/Anthropic/AnthropicChatCompletionService.cs` | ~430 | Service implementation |
| `Modules.LLM/Providers/Anthropic/AnthropicResponseParser.cs` | ~510 | Response/error parsing |
| `Modules.LLM/Extensions/AnthropicServiceCollectionExtensions.cs` | ~200 | DI registration |

### Modified (2 files)

| File | Change |
| ---- | ------ |
| `Modules.LLM/LLMModule.cs` | Added `AddAnthropicProvider()` call, updated module description |
| `Modules.LLM/Logging/LLMLogEvents.cs` | Added events 1700-1715 |

---

## Test Coverage

### New Test Files

| File | Tests | Description |
| ---- | ----- | ----------- |
| `AnthropicOptionsTests.cs` | 22 | Configuration, defaults, computed properties, model list |
| `AnthropicChatCompletionServiceTests.cs` | 24 | Service implementation, error handling, headers |
| `AnthropicResponseParserTests.cs` | 36 | Response parsing, event parsing, error mapping |

### Test Summary

- **New Tests:** 82
- **All Tests Pass:** ✅

---

## Dependencies

### Internal Dependencies

| Dependency | Purpose |
| ---------- | ------- |
| `Lexichord.Abstractions` | LLM contracts (`IChatCompletionService`, exceptions) |
| `ISecureVault` | API key retrieval via `anthropic:api-key` |
| `AnthropicParameterMapper` | Request JSON building (from v0.6.1b) |
| `ILLMProviderRegistry` | Provider registration (from v0.6.1c) |

### External Dependencies

| Package | Version | Purpose |
| ------- | ------- | ------- |
| Microsoft.Extensions.Http | 9.0.0 | `IHttpClientFactory` |
| Polly | 8.5.x | Resilience policies |
| Polly.Extensions.Http | 8.x | HTTP policy extensions |

---

## Usage Example

```csharp
// In service registration (automatic via LLMModule)
services.AddAnthropicProvider(configuration);

// Configuration in appsettings.json
{
  "LLM": {
    "Providers": {
      "Anthropic": {
        "BaseUrl": "https://api.anthropic.com/v1",
        "DefaultModel": "claude-3-haiku-20240307",
        "ApiVersion": "2024-01-01",
        "MaxRetries": 3,
        "TimeoutSeconds": 30
      }
    }
  }
}

// Store API key in secure vault (via settings UI or code)
await vault.StoreSecretAsync("anthropic:api-key", "sk-ant-your-api-key");

// Use the provider
var registry = serviceProvider.GetRequiredService<ILLMProviderRegistry>();
var provider = registry.GetProvider("anthropic");

// Synchronous completion
var request = ChatRequest.FromUserMessage("What is the capital of France?");
var response = await provider.CompleteAsync(request);
Console.WriteLine(response.Content);
// Output: The capital of France is Paris.

// Streaming completion
await foreach (var token in provider.StreamAsync(request))
{
    Console.Write(token.Token);
    if (token.IsComplete)
    {
        Console.WriteLine($"\n[Finished: {token.FinishReason}]");
    }
}
```

---

## Migration Notes

### From v0.6.2a

No breaking changes. The Anthropic connector is additive functionality. Existing code using `ILLMProviderRegistry`, OpenAI provider, and other v0.6.2a services continues to work unchanged.

### API Key Setup

The Anthropic API key must be stored in the secure vault before using the service:

1. **Settings UI**: Navigate to Settings → LLM Providers → Anthropic, enter API key
2. **Programmatic**: `await vault.StoreSecretAsync("anthropic:api-key", "sk-ant-...")`

---

## Design Decisions

### 1. Reuse of Existing Components

Uses existing infrastructure from previous versions:
- `AnthropicParameterMapper` (v0.6.1b) for request JSON building with system message separation
- `AddChatCompletionProvider<T>()` (v0.6.1c) for provider registration

### 2. Custom Streaming Parser

Unlike OpenAI which uses simple `data: {...}` lines, Anthropic SSE includes event types:
```
event: content_block_delta
data: {"type":"content_block_delta","delta":{"text":"Hello"}}
```

Custom parsing logic handles:
- Event type extraction and routing
- Different event types (8 total)
- Error events in stream

### 3. Authentication Header Difference

Anthropic uses `x-api-key` header instead of OAuth Bearer token:
```
x-api-key: sk-ant-...
anthropic-version: 2024-01-01
```

This is intentionally different from OpenAI's `Authorization: Bearer ...` pattern.

### 4. Content Blocks Assembly

Anthropic returns content as array of typed blocks:
```json
{
  "content": [
    {"type": "text", "text": "First part"},
    {"type": "text", "text": "Second part"}
  ]
}
```

Parser concatenates all text blocks to form complete response.

### 5. HTTP 529 Handling

Anthropic uses non-standard HTTP 529 for overloaded errors:
- Added to retry policy alongside standard 429
- Mapped to `ChatCompletionException` with overloaded context

---

## Security Considerations

1. **API Key Storage:** Retrieved from `ISecureVault` using `anthropic:api-key` pattern
2. **No Key Logging:** API key values are never logged (only retrieval status)
3. **x-api-key Authentication:** Key transmitted via custom header over HTTPS
4. **Key Lifetime:** Key is not stored in service fields; retrieved per-request

---

## Known Limitations

1. **Single API Key:** One API key per provider (cannot use multiple keys)
2. **No Tool Use:** Claude tool/function calling support deferred to future version
3. **No Vision:** Image input support deferred to future version
4. **No Retry-After Header:** Anthropic doesn't consistently provide Retry-After, so fallback timing is used

---

## Future Work

- v0.6.2c: Resilience Pipeline (centralized advanced retry/circuit breaker configuration)
- v0.6.2d: Token Counter Integration with tiktoken/approximation
- v0.6.2e: Tool/Function Calling Support for both providers
