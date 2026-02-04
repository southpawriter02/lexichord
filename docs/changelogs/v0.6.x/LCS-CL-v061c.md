# LCS-CL-061c: Detailed Changelog — Provider Registry

## Metadata

| Field        | Value                                        |
| :----------- | :------------------------------------------- |
| **Version**  | v0.6.1c                                      |
| **Released** | 2026-02-04                                   |
| **Category** | Abstraction / Module                         |
| **Parent**   | [v0.6.1 Changelog](../CHANGELOG.md#v061)     |
| **Spec**     | [LCS-DES-061c](../../specs/v0.6.x/v0.6.1/LCS-DES-v0.6.1c.md) |

---

## Summary

This release implements the Provider Registry for the LLM module, enabling dynamic provider management, selection, and configuration status checking. It builds upon v0.6.1a (Chat Completion Abstractions) and v0.6.1b (Chat Options Model) to provide central management of LLM provider instances.

---

## New Features

### 1. Provider Registry Interface (Abstractions)

Added `ILLMProviderRegistry` interface for managing LLM provider instances:

```csharp
public interface ILLMProviderRegistry
{
    IReadOnlyList<LLMProviderInfo> AvailableProviders { get; }
    IChatCompletionService GetProvider(string providerName);
    IChatCompletionService GetDefaultProvider();
    void SetDefaultProvider(string providerName);
    bool IsProviderConfigured(string providerName);
}
```

**Key Features:**
- Discovery of available providers and their capabilities
- Resolution of provider instances by name (case-insensitive)
- Default provider selection and persistence
- Configuration status checking for API key presence

**File:** `src/Lexichord.Abstractions/Contracts/LLM/ILLMProviderRegistry.cs`

### 2. Provider Information Record (Abstractions)

Added `LLMProviderInfo` record for provider metadata:

```csharp
public record LLMProviderInfo(
    string Name,
    string DisplayName,
    IReadOnlyList<string> SupportedModels,
    bool IsConfigured,
    bool SupportsStreaming);
```

**Factory Methods:**
| Method | Description |
| ------ | ----------- |
| `Unconfigured(name, displayName)` | Creates unconfigured provider info |
| `Create(name, displayName, models, streaming)` | Creates provider with known models |

**Helper Methods:**
| Method | Description |
| ------ | ----------- |
| `SupportsModel(modelId)` | Checks if provider supports a model |
| `WithConfigurationStatus(bool)` | Creates copy with updated status |
| `WithModels(models)` | Creates copy with updated models |

**File:** `src/Lexichord.Abstractions/Contracts/LLM/LLMProviderInfo.cs`

### 3. Provider Not Found Exception (Abstractions)

Added `ProviderNotFoundException` for registry-level errors:

```csharp
public class ProviderNotFoundException : Exception
{
    public string ProviderName { get; }
    public bool HasProviderName { get; }
}
```

**Note:** This exception inherits from `Exception` (not `ChatCompletionException`) as it represents a registry-level error, not a completion error.

**File:** `src/Lexichord.Abstractions/Contracts/LLM/ProviderNotFoundException.cs`

### 4. Provider Registry Implementation (LLM Module)

Added `LLMProviderRegistry` as the default implementation:

| Feature | Implementation |
| ------- | -------------- |
| Thread Safety | `ConcurrentDictionary<string, LLMProviderInfo>` |
| Provider Resolution | .NET 8+ Keyed Services (`GetKeyedService`) |
| Default Persistence | `ISystemSettingsRepository` with key `LLM.DefaultProvider` |
| API Key Checking | `ISecureVault` with pattern `{provider}:api-key` |
| Name Matching | Case-insensitive (normalized to lowercase) |

**Internal Methods:**
| Method | Purpose |
| ------ | ------- |
| `RegisterProvider(info)` | Registers provider metadata |
| `RefreshConfigurationStatusAsync()` | Updates IsConfigured from vault |

**File:** `src/Lexichord.Modules.LLM/Infrastructure/LLMProviderRegistry.cs`

### 5. Provider Service Extensions (LLM Module)

Added DI extension methods for provider registration:

```csharp
// Register the provider registry
services.AddLLMProviderRegistry();

// Register a chat completion provider with metadata
services.AddChatCompletionProvider<OpenAIService>(
    "openai",
    "OpenAI",
    ["gpt-4o", "gpt-4o-mini"],
    supportsStreaming: true);

// Initialize registry at startup
await provider.InitializeProviderRegistryAsync();
```

**File:** `src/Lexichord.Modules.LLM/Extensions/LLMProviderServiceExtensions.cs`

### 6. Structured Logging Events (LLM Module)

Added provider registry logging events (1400-1499 range):

| Event ID | Level       | Description                              |
| -------- | ----------- | ---------------------------------------- |
| 1400     | Debug       | Resolving provider                       |
| 1401     | Warning     | Provider not found in registry           |
| 1402     | Warning     | Provider not configured (missing API key)|
| 1403     | Debug       | Provider resolved successfully           |
| 1404     | Debug       | Using persisted default provider         |
| 1405     | Warning     | Persisted default not registered         |
| 1406     | Error       | No configured providers available        |
| 1407     | Information | Falling back to first configured         |
| 1408     | Information | Default provider set                     |
| 1409     | Information | Provider registered                      |
| 1410     | Debug       | Refreshing configuration status          |
| 1411     | Debug       | Provider configuration status changed    |
| 1412     | Information | Configuration refresh completed          |
| 1413     | Debug       | Provider already registered (updating)   |
| 1414     | Information | Initializing provider registry           |
| 1415     | Trace       | Checking provider configuration          |

**File:** `src/Lexichord.Modules.LLM/Logging/LLMLogEvents.cs`

---

## Files Changed

### Created (4 files)

| File | Lines | Description |
| ---- | ----- | ----------- |
| `Contracts/LLM/ILLMProviderRegistry.cs` | ~255 | Provider registry interface |
| `Contracts/LLM/LLMProviderInfo.cs` | ~205 | Provider metadata record |
| `Contracts/LLM/ProviderNotFoundException.cs` | ~130 | Provider not found exception |
| `Modules.LLM/Infrastructure/LLMProviderRegistry.cs` | ~350 | Registry implementation |
| `Modules.LLM/Extensions/LLMProviderServiceExtensions.cs` | ~200 | DI extension methods |

### Modified (3 files)

| File | Change |
| ---- | ------ |
| `Modules.LLM/LLMModule.cs` | Added registry initialization |
| `Modules.LLM/Extensions/LLMServiceExtensions.cs` | Added registry registration |
| `Modules.LLM/Logging/LLMLogEvents.cs` | Added events 1400-1415 |

---

## Test Coverage

### New Test Files

| File | Tests | Description |
| ---- | ----- | ----------- |
| `LLMProviderInfoTests.cs` | 26 | Record creation, factory methods, equality |
| `ProviderNotFoundExceptionTests.cs` | 15 | Exception constructors, inheritance |
| `LLMProviderRegistryTests.cs` | 31 | Registry operations, thread safety |

### Test Summary

- **New Tests:** 72
- **All Tests Pass:** ✅

---

## Dependencies

### Internal Dependencies

| Dependency | Purpose |
| ---------- | ------- |
| `Lexichord.Abstractions` | LLM contracts, `IChatCompletionService` |
| `ISystemSettingsRepository` | Default provider persistence |
| `ISecureVault` | API key configuration checking |

### External Dependencies

| Package | Version | Purpose |
| ------- | ------- | ------- |
| Microsoft.Extensions.DependencyInjection | 9.0.0 | Keyed service resolution |

---

## Usage Example

```csharp
// In service registration
services.AddLLMProviderRegistry();
services.AddChatCompletionProvider<OpenAIService>(
    "openai",
    "OpenAI",
    ["gpt-4o", "gpt-4o-mini"]);
services.AddChatCompletionProvider<AnthropicService>(
    "anthropic",
    "Anthropic",
    ["claude-3-opus", "claude-3-sonnet"]);

// At startup
await serviceProvider.InitializeProviderRegistryAsync();

// In application code
var registry = serviceProvider.GetRequiredService<ILLMProviderRegistry>();

// List available providers
foreach (var provider in registry.AvailableProviders)
{
    Console.WriteLine($"{provider.DisplayName}: {(provider.IsConfigured ? "Ready" : "Not configured")}");
}

// Get the default provider
var defaultProvider = registry.GetDefaultProvider();
var response = await defaultProvider.CompleteAsync(request);

// Get a specific provider
if (registry.IsProviderConfigured("anthropic"))
{
    var anthropic = registry.GetProvider("anthropic");
    var response = await anthropic.CompleteAsync(request);
}

// Set user's default provider
registry.SetDefaultProvider("anthropic");
```

---

## Migration Notes

### From v0.6.1b

No breaking changes. The provider registry is additive functionality. Existing code using `ChatOptions`, `ModelRegistry`, and other v0.6.1b services continues to work unchanged.

### New Registration Pattern

Provider modules should register their services using the new extension method:

```csharp
// Before (v0.6.1b)
services.AddSingleton<IChatCompletionService, OpenAIService>();

// After (v0.6.1c)
services.AddChatCompletionProvider<OpenAIService>(
    "openai",
    "OpenAI",
    ["gpt-4o", "gpt-4o-mini"]);
```

---

## Design Decisions

### 1. Exception Hierarchy

`ProviderNotFoundException` inherits from `Exception` rather than `ChatCompletionException` because:
- It represents a registry/configuration error, not a completion error
- The exception is thrown before any API call is made
- Different catch blocks may handle registry vs completion errors

### 2. Keyed Services

Uses .NET 8+ keyed service registration for provider resolution:
- Type-safe service resolution by provider name
- Supports multiple implementations of `IChatCompletionService`
- Integrates with standard DI patterns

### 3. Synchronous Default Provider Access

`SetDefaultProvider` and `GetDefaultProvider` use synchronous settings access:
- Settings are typically fast and cached
- Simplifies API surface for common operations
- Avoids async proliferation in UI code

### 4. Case-Insensitive Matching

Provider names are normalized to lowercase:
- "OpenAI", "openai", "OPENAI" all resolve to the same provider
- Consistent with URL/API conventions
- Reduces user confusion

---

## Known Limitations

1. **Synchronous Settings Access**: Settings operations use `GetAwaiter().GetResult()` internally. This works for cached settings but could block on slow I/O.

2. **No Hot-Swap**: Providers cannot be added or removed at runtime. This feature is deferred to a future sub-version.

3. **Single Vault Key Pattern**: All providers use the `{provider}:api-key` pattern. Providers requiring multiple secrets need custom handling.

---

## Future Work

- v0.6.1d: Provider implementations (OpenAI, Anthropic HTTP clients)
- v0.6.1e: Streaming support with SSE parsing
- v0.6.1f: Rate limiting and retry policies
- Future: Health monitoring, hot-swap, response caching
