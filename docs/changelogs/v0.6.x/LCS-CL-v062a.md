# LCS-CL-062a: Detailed Changelog — OpenAI Connector

## Metadata

| Field        | Value                                        |
| :----------- | :------------------------------------------- |
| **Version**  | v0.6.2a                                      |
| **Released** | 2026-02-04                                   |
| **Category** | Implementation / Module                      |
| **Parent**   | [v0.6.2 Changelog](../CHANGELOG.md#v062)     |
| **Spec**     | [LCS-DES-062a](../../specs/v0.6.x/v0.6.2/LCS-DES-v0.6.2a.md) |

---

## Summary

This release implements the OpenAI Connector for the LLM module, providing production-ready integration with the OpenAI Chat Completions API. It supports both synchronous completion and streaming responses via Server-Sent Events (SSE), with robust error handling, resilience policies, and comprehensive logging.

---

## New Features

### 1. OpenAI Configuration Record

Added `OpenAIOptions` record for configuring the OpenAI provider:

```csharp
public record OpenAIOptions(
    string BaseUrl = "https://api.openai.com/v1",
    string DefaultModel = "gpt-4o-mini",
    int MaxRetries = 3,
    int TimeoutSeconds = 30)
{
    public const string VaultKey = "openai:api-key";
    public const string HttpClientName = "OpenAI";
    public string CompletionsEndpoint => $"{BaseUrl}/chat/completions";
    public static IReadOnlyList<string> SupportedModels { get; }
}
```

**Supported Models:**
- gpt-4o
- gpt-4o-mini (default)
- gpt-4-turbo
- gpt-3.5-turbo

**File:** `src/Lexichord.Modules.LLM/Providers/OpenAI/OpenAIOptions.cs`

### 2. OpenAI Chat Completion Service

Added `OpenAIChatCompletionService` implementing `IChatCompletionService`:

| Method | Description |
| ------ | ----------- |
| `CompleteAsync(request, ct)` | Sends request and returns complete response |
| `StreamAsync(request, ct)` | Streams tokens via SSE as `IAsyncEnumerable<StreamingChatToken>` |

**Key Features:**

| Feature | Implementation |
| ------- | -------------- |
| Authentication | Bearer token from `ISecureVault` via `openai:api-key` |
| Request Building | Uses existing `OpenAIParameterMapper.ToRequestBody()` |
| Streaming | Uses existing `SseParser.ParseStreamAsync()` |
| Error Handling | Maps HTTP status codes to exception hierarchy |
| Logging | Comprehensive structured logging (events 1600-1615) |

**Error Mapping:**

| HTTP Status | Exception Type |
| ----------- | -------------- |
| 401 Unauthorized | `AuthenticationException` |
| 429 Too Many Requests | `RateLimitException` with RetryAfter |
| 5xx Server Error | `ChatCompletionException` |
| Missing API Key | `ProviderNotConfiguredException` |

**File:** `src/Lexichord.Modules.LLM/Providers/OpenAI/OpenAIChatCompletionService.cs`

### 3. Response Parser

Added `OpenAIResponseParser` for handling API responses:

| Method | Return | Description |
| ------ | ------ | ----------- |
| `ParseSuccessResponse(body, duration)` | `ChatResponse` | Parse successful completion response |
| `ParseStreamingChunk(data)` | `StreamingChatToken?` | Parse SSE streaming chunk |
| `ParseErrorResponse(status, body, retryAfter)` | `ChatCompletionException` | Map error to exception |
| `ExtractTokenUsage(body)` | `(int, int)` | Extract prompt/completion token counts |

**File:** `src/Lexichord.Modules.LLM/Providers/OpenAI/OpenAIResponseParser.cs`

### 4. DI Registration Extensions

Added `OpenAIServiceCollectionExtensions` for service registration:

```csharp
services.AddOpenAIProvider(configuration);
```

**Registers:**
- `OpenAIOptions` bound from `LLM:Providers:OpenAI` configuration section
- Named HTTP client "OpenAI" with resilience policies
- `OpenAIChatCompletionService` as keyed singleton provider
- Provider metadata with `ILLMProviderRegistry`

**Resilience Policies:**
- **Retry**: Exponential backoff (2^n seconds + jitter) for transient errors
- **Circuit Breaker**: Opens after 5 failures, 30-second break duration

**File:** `src/Lexichord.Modules.LLM/Extensions/OpenAIServiceCollectionExtensions.cs`

### 5. Structured Logging Events (LLM Module)

Added OpenAI provider logging events (1600-1615 range):

| Event ID | Level | Description |
| -------- | ----- | ----------- |
| 1600 | Debug | Starting OpenAI completion request |
| 1601 | Debug | Estimated prompt tokens |
| 1602 | Information | OpenAI completion succeeded |
| 1603 | Debug | Starting OpenAI streaming request |
| 1604 | Debug | OpenAI stream started |
| 1605 | Trace | OpenAI stream chunk received |
| 1606 | Information | OpenAI stream completed |
| 1607 | Warning | OpenAI API returned error |
| 1608 | Error | OpenAI HTTP request failed |
| 1609 | Warning | Failed to parse streaming chunk |
| 1610 | Debug | Building OpenAI HTTP request |
| 1611 | Debug | Retrieving API key from vault |
| 1612 | Warning | API key not found in vault |
| 1613 | Debug | Parsing OpenAI success response |
| 1614 | Debug | Parsing OpenAI error response |
| 1615 | Trace | Raw OpenAI response size |

**File:** `src/Lexichord.Modules.LLM/Logging/LLMLogEvents.cs`

---

## Files Changed

### Created (4 files)

| File | Lines | Description |
| ---- | ----- | ----------- |
| `Modules.LLM/Providers/OpenAI/OpenAIOptions.cs` | ~120 | Configuration record |
| `Modules.LLM/Providers/OpenAI/OpenAIChatCompletionService.cs` | ~300 | Service implementation |
| `Modules.LLM/Providers/OpenAI/OpenAIResponseParser.cs` | ~280 | Response/error parsing |
| `Modules.LLM/Extensions/OpenAIServiceCollectionExtensions.cs` | ~140 | DI registration |

### Modified (3 files)

| File | Change |
| ---- | ------ |
| `Modules.LLM/LLMModule.cs` | Added `AddOpenAIProvider()` call, version bump to 0.6.2 |
| `Modules.LLM/Logging/LLMLogEvents.cs` | Added events 1600-1615 |
| `Modules.LLM/Lexichord.Modules.LLM.csproj` | Added Microsoft.Extensions.Http.Polly 9.0.0, version bump |

---

## Test Coverage

### New Test Files

| File | Tests | Description |
| ---- | ----- | ----------- |
| `OpenAIOptionsTests.cs` | 15 | Configuration, defaults, computed properties |
| `OpenAIChatCompletionServiceTests.cs` | 18 | Service implementation, error handling |
| `OpenAIResponseParserTests.cs` | 21 | Response parsing, error mapping |

### Test Summary

- **New Tests:** 54
- **All Tests Pass:** ✅

---

## Dependencies

### Internal Dependencies

| Dependency | Purpose |
| ---------- | ------- |
| `Lexichord.Abstractions` | LLM contracts (`IChatCompletionService`, exceptions) |
| `ISecureVault` | API key retrieval via `openai:api-key` |
| `OpenAIParameterMapper` | Request JSON building (from v0.6.1b) |
| `SseParser` | SSE stream parsing (from v0.6.1a) |
| `ILLMProviderRegistry` | Provider registration (from v0.6.1c) |

### External Dependencies

| Package | Version | Purpose |
| ------- | ------- | ------- |
| Microsoft.Extensions.Http | 9.0.0 | `IHttpClientFactory` |
| Microsoft.Extensions.Http.Polly | 9.0.0 | Resilience policies |
| Polly.Extensions.Http | 3.0.0 | HTTP policy extensions |

---

## Usage Example

```csharp
// In service registration (automatic via LLMModule)
services.AddOpenAIProvider(configuration);

// Configuration in appsettings.json
{
  "LLM": {
    "Providers": {
      "OpenAI": {
        "BaseUrl": "https://api.openai.com/v1",
        "DefaultModel": "gpt-4o-mini",
        "MaxRetries": 3,
        "TimeoutSeconds": 30
      }
    }
  }
}

// Store API key in secure vault (via settings UI or code)
await vault.StoreSecretAsync("openai:api-key", "sk-your-api-key");

// Use the provider
var registry = serviceProvider.GetRequiredService<ILLMProviderRegistry>();
var provider = registry.GetProvider("openai");

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

### From v0.6.1d

No breaking changes. The OpenAI connector is additive functionality. Existing code using `ILLMProviderRegistry`, settings UI, and other v0.6.1d services continues to work unchanged.

### API Key Setup

The OpenAI API key must be stored in the secure vault before using the service:

1. **Settings UI**: Navigate to Settings → LLM Providers → OpenAI, enter API key
2. **Programmatic**: `await vault.StoreSecretAsync("openai:api-key", "sk-...")`

---

## Design Decisions

### 1. Reuse of Existing Components

Uses existing infrastructure from previous versions:
- `OpenAIParameterMapper` (v0.6.1b) for request JSON building
- `SseParser` (v0.6.1a) for streaming response parsing
- `AddChatCompletionProvider<T>()` (v0.6.1c) for provider registration

### 2. HTTP Client Configuration

Uses `IHttpClientFactory` with named client for:
- Connection pooling and lifetime management
- Resilience policies via Polly
- Per-client timeout configuration

### 3. Resilience Strategy

Implements defense-in-depth resilience:
- **Retry Policy**: Handles transient failures with exponential backoff
- **Circuit Breaker**: Prevents cascade failures during outages
- **Retry-After Header**: Respects API rate limit headers

### 4. Streaming Implementation

Uses `HttpCompletionOption.ResponseHeadersRead` for streaming:
- Starts processing immediately without buffering full response
- Leverages existing `SseParser` for consistent SSE handling
- Yields tokens incrementally via `IAsyncEnumerable<StreamingChatToken>`

### 5. Error Handling Strategy

Maps OpenAI errors to Lexichord exception hierarchy:
- Enables consistent error handling across providers
- Preserves provider-specific error messages
- Includes retry-after timing for rate limits

---

## Security Considerations

1. **API Key Storage:** Retrieved from `ISecureVault` using `openai:api-key` pattern
2. **No Key Logging:** API key values are never logged (only retrieval status)
3. **Bearer Authentication:** Key transmitted via Authorization header over HTTPS
4. **Key Lifetime:** Key is not stored in service fields; retrieved per-request

---

## Known Limitations

1. **Single API Key:** One API key per provider (cannot use multiple keys)
2. **No Function Calling:** Tool/function calling support deferred to v0.6.2e
3. **No Vision:** Image input support deferred to future version
4. **Synchronous Settings:** Settings repository uses sync-over-async internally

---

## Future Work

- v0.6.2b: Anthropic Connector
- v0.6.2c: Resilience Pipeline (advanced retry/circuit breaker)
- v0.6.2d: Token Counter Integration
- v0.6.2e: Tool/Function Calling Support
