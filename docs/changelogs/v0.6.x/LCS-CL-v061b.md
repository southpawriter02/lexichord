# LCS-CL-061b: Detailed Changelog — Chat Options Model

## Metadata

| Field        | Value                                        |
| :----------- | :------------------------------------------- |
| **Version**  | v0.6.1b                                      |
| **Released** | 2026-02-04                                   |
| **Category** | Abstraction / Module                         |
| **Parent**   | [v0.6.1 Changelog](../CHANGELOG.md#v061)     |
| **Spec**     | [LCS-DES-061b](../../specs/v0.6.x/v0.6.1/LCS-DES-v0.6.1b.md) |

---

## Summary

This release extends the v0.6.1a Chat Completion Abstractions with comprehensive validation, model discovery, token estimation, and provider-specific parameter mapping. It introduces the new `Lexichord.Modules.LLM` module as the gateway for LLM provider interactions.

---

## New Features

### 1. Extended ChatOptions Presets (Abstractions)

Added 5 new presets to `ChatOptions` following the spec's preset selection matrix:

| Preset          | Temperature | TopP | FreqPenalty | PresPenalty | MaxTokens | StopSequences    |
| --------------- | ----------- | ---- | ----------- | ----------- | --------- | ---------------- |
| CodeGeneration  | 0.0         | 1.0  | 0.0         | 0.0         | -         | ["```", "---"]   |
| Conversational  | 0.7         | 0.9  | 0.3         | 0.3         | -         | -                |
| Summarization   | 0.5         | 0.85 | 0.0         | 0.0         | 1024      | -                |
| Editing         | 0.4         | 0.9  | 0.2         | 0.1         | -         | -                |
| Brainstorming   | 1.5         | 0.98 | 0.8         | 0.8         | -         | -                |

**File:** `src/Lexichord.Abstractions/Contracts/LLM/ChatOptions.cs`

### 2. ChatOptions Validation (Abstractions)

Added `ChatOptionsValidator` with FluentValidation rules:

| Parameter        | Range        | Error Code                      |
| ---------------- | ------------ | ------------------------------- |
| Temperature      | 0.0 - 2.0    | TEMPERATURE_OUT_OF_RANGE        |
| MaxTokens        | > 0          | MAX_TOKENS_INVALID              |
| TopP             | 0.0 - 1.0    | TOP_P_OUT_OF_RANGE              |
| FrequencyPenalty | -2.0 - 2.0   | FREQUENCY_PENALTY_OUT_OF_RANGE  |
| PresencePenalty  | -2.0 - 2.0   | PRESENCE_PENALTY_OUT_OF_RANGE   |
| StopSequences    | <= 4 items   | TOO_MANY_STOP_SEQUENCES         |

**Files:**
- `src/Lexichord.Abstractions/Contracts/LLM/ChatOptionsValidator.cs`
- `src/Lexichord.Abstractions/Contracts/LLM/ChatOptionsValidationException.cs`

### 3. Context Window Management (Abstractions)

Added `TokenEstimate` record and `ChatOptionsContextExtensions`:

```csharp
public record TokenEstimate(
    int EstimatedPromptTokens,
    int AvailableResponseTokens,
    int ContextWindow,
    bool WouldExceedContext);

// Extension methods
options.AdjustForContext(estimate)  // Clamps MaxTokens or throws
options.WouldFitInContext(estimate) // Non-throwing check
options.GetEffectiveMaxTokens(estimate) // Calculated effective max
```

**Files:**
- `src/Lexichord.Abstractions/Contracts/LLM/TokenEstimate.cs`
- `src/Lexichord.Abstractions/Contracts/LLM/ContextWindowExceededException.cs`
- `src/Lexichord.Abstractions/Contracts/LLM/ChatOptionsContextExtensions.cs`

### 4. Model Discovery Interface (Abstractions)

Added `IModelProvider` interface and `ModelInfo` record for dynamic model discovery:

```csharp
public interface IModelProvider
{
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken ct);
}

public record ModelInfo(
    string Id,
    string DisplayName,
    int ContextWindow,
    int MaxOutputTokens,
    bool SupportsVision = false,
    bool SupportsTools = false);
```

**Files:**
- `src/Lexichord.Abstractions/Contracts/LLM/IModelProvider.cs`
- `src/Lexichord.Abstractions/Contracts/LLM/ModelInfo.cs`

### 5. LLM Module (New Module)

Created `Lexichord.Modules.LLM` as the gateway module for LLM provider interactions.

#### Configuration Classes

| Class               | Purpose                                    |
| ------------------- | ------------------------------------------ |
| `LLMOptions`        | Root configuration for appsettings.json    |
| `ProviderOptions`   | Per-provider configuration (URL, model)    |
| `ChatOptionsDefaults` | Default chat options for resolution      |
| `ModelDefaults`     | Static model registry with known models    |

**Files:**
- `src/Lexichord.Modules.LLM/Configuration/LLMOptions.cs`
- `src/Lexichord.Modules.LLM/Configuration/ProviderOptions.cs`
- `src/Lexichord.Modules.LLM/Configuration/ChatOptionsDefaults.cs`
- `src/Lexichord.Modules.LLM/Configuration/ModelDefaults.cs`

#### Services

| Service                  | Purpose                                          |
| ------------------------ | ------------------------------------------------ |
| `ModelRegistry`          | Caches models across providers with fallback     |
| `TokenEstimator`         | Estimates token usage for requests               |
| `ChatOptionsResolver`    | Resolution pipeline: defaults → user → request   |

**Files:**
- `src/Lexichord.Modules.LLM/Services/ModelRegistry.cs`
- `src/Lexichord.Modules.LLM/Services/TokenEstimator.cs`
- `src/Lexichord.Modules.LLM/Services/ChatOptionsResolver.cs`

#### Provider Mappers

| Mapper                     | Provider  | Key Transformations                |
| -------------------------- | --------- | ---------------------------------- |
| `OpenAIParameterMapper`    | OpenAI    | Direct mapping, "stop" array       |
| `AnthropicParameterMapper` | Anthropic | Temperature /2, "stop_sequences"   |

**Files:**
- `src/Lexichord.Modules.LLM/Providers/OpenAI/OpenAIParameterMapper.cs`
- `src/Lexichord.Modules.LLM/Providers/Anthropic/AnthropicParameterMapper.cs`

#### Validation

Added `ProviderAwareChatOptionsValidator` with provider-specific rules:

- Anthropic: Temperature warning for values > 2.0 (clamped to 1.0)
- Anthropic: Warning for unsupported FrequencyPenalty/PresencePenalty
- All: Async model availability checking via `ModelRegistry`

**File:** `src/Lexichord.Modules.LLM/Validation/ProviderAwareChatOptionsValidator.cs`

#### Logging

Added structured logging via `LLMLogEvents` using `[LoggerMessage]` source generators:

| Event ID | Level       | Description                        |
| -------- | ----------- | ---------------------------------- |
| 1001     | Debug       | Resolving ChatOptions for provider |
| 1002     | Debug       | Resolved model with source         |
| 1003     | Warning     | Validation failed                  |
| 1004     | Information | Final resolved options             |
| 1100     | Information | Fetching models for provider       |
| 1101     | Information | Cached models                      |
| 1200     | Debug       | Token estimation results           |
| 1201     | Warning     | Context window exceeded            |

**File:** `src/Lexichord.Modules.LLM/Logging/LLMLogEvents.cs`

#### Module Registration

- `LLMModule` implements `IModule` for automatic discovery
- `LLMServiceExtensions` provides `AddLLMServices()` extension method

**Files:**
- `src/Lexichord.Modules.LLM/LLMModule.cs`
- `src/Lexichord.Modules.LLM/Extensions/LLMServiceExtensions.cs`

---

## Files Changed

### Created (19 files)

| File | Lines | Description |
| ---- | ----- | ----------- |
| `Contracts/LLM/ModelInfo.cs` | ~60 | Model metadata record |
| `Contracts/LLM/TokenEstimate.cs` | ~55 | Token estimation record |
| `Contracts/LLM/IModelProvider.cs` | ~45 | Model discovery interface |
| `Contracts/LLM/ChatOptionsValidator.cs` | ~150 | FluentValidation validator |
| `Contracts/LLM/ChatOptionsValidationException.cs` | ~85 | Validation exception |
| `Contracts/LLM/ContextWindowExceededException.cs` | ~65 | Context overflow exception |
| `Contracts/LLM/ChatOptionsContextExtensions.cs` | ~170 | Context adjustment extensions |
| `Modules.LLM/Lexichord.Modules.LLM.csproj` | ~46 | Module project file |
| `Modules.LLM/LLMModule.cs` | ~120 | Module implementation |
| `Modules.LLM/Configuration/LLMOptions.cs` | ~100 | Root configuration |
| `Modules.LLM/Configuration/ProviderOptions.cs` | ~80 | Provider configuration |
| `Modules.LLM/Configuration/ChatOptionsDefaults.cs` | ~80 | Default values |
| `Modules.LLM/Configuration/ModelDefaults.cs` | ~230 | Static model registry |
| `Modules.LLM/Services/ModelRegistry.cs` | ~220 | Model caching service |
| `Modules.LLM/Services/TokenEstimator.cs` | ~200 | Token estimation service |
| `Modules.LLM/Services/ChatOptionsResolver.cs` | ~260 | Options resolution |
| `Modules.LLM/Providers/OpenAI/OpenAIParameterMapper.cs` | ~140 | OpenAI JSON mapping |
| `Modules.LLM/Providers/Anthropic/AnthropicParameterMapper.cs` | ~170 | Anthropic JSON mapping |
| `Modules.LLM/Validation/ProviderAwareChatOptionsValidator.cs` | ~170 | Provider validation |
| `Modules.LLM/Logging/LLMLogEvents.cs` | ~230 | Structured logging |
| `Modules.LLM/Extensions/LLMServiceExtensions.cs` | ~120 | DI extensions |

### Modified (2 files)

| File | Change |
| ---- | ------ |
| `Contracts/LLM/ChatOptions.cs` | Added 5 new presets |
| `Tests.Unit/Abstractions/LLM/ChatOptionsTests.cs` | Added preset tests |

---

## Test Coverage

### New Test Files

| File | Tests | Description |
| ---- | ----- | ----------- |
| `ChatOptionsValidatorTests.cs` | 17 | Validation rules for all parameters |
| `ModelDefaultsTests.cs` | 19 | Static model registry tests |
| `AnthropicParameterMapperTests.cs` | 10 | Temperature clamping, mapping |

### Updated Test Files

| File | New Tests | Description |
| ---- | --------- | ----------- |
| `ChatOptionsTests.cs` | 6 | New preset value verification |

### Test Summary

- **New Tests:** 52
- **Modified Tests:** 6
- **Total v0.6.1b Tests:** 58
- **All Tests Pass:** ✅

---

## Dependencies

### NuGet Packages (LLM Module)

| Package | Version | Purpose |
| ------- | ------- | ------- |
| FluentValidation | 11.9.2 | Validation framework |
| Microsoft.Extensions.DependencyInjection | 9.0.0 | DI container |
| Microsoft.Extensions.Options.ConfigurationExtensions | 9.0.0 | IOptions binding |
| Microsoft.Extensions.Configuration | 9.0.0 | Configuration access |
| Microsoft.Extensions.Caching.Memory | 9.0.0 | Model caching |
| System.Text.Json | 9.0.0 | JSON serialization |

### Internal Dependencies

- `Lexichord.Abstractions` → LLM contracts
- `ITokenCounter` (v0.4.4c) → Token estimation
- `ISystemSettingsRepository` → User preferences

---

## Configuration Example

```json
{
  "LLM": {
    "DefaultProvider": "openai",
    "Providers": {
      "OpenAI": {
        "BaseUrl": "https://api.openai.com/v1",
        "DefaultModel": "gpt-4o-mini",
        "MaxRetries": 3,
        "TimeoutSeconds": 30
      },
      "Anthropic": {
        "BaseUrl": "https://api.anthropic.com/v1",
        "DefaultModel": "claude-3-haiku-20240307",
        "MaxRetries": 3,
        "TimeoutSeconds": 30
      }
    },
    "Defaults": {
      "Temperature": 0.7,
      "MaxTokens": 2048,
      "TopP": 1.0
    }
  }
}
```

---

## Migration Notes

### From v0.6.1a

No breaking changes. The existing `ChatOptions` record is extended with new presets. All existing code continues to work unchanged.

### New Module Loading

The `Lexichord.Modules.LLM.dll` will be automatically discovered by `ModuleLoader` from the `Modules/` directory. No manual registration required.

---

## Known Limitations

1. **ChatMessage.Name**: The spec references a `Name` property on `ChatMessage` that doesn't exist in the current implementation. Token estimation skips name overhead calculation.

2. **ILLMProviderRegistry**: The spec references this interface which doesn't exist. `ModelRegistry` falls back to static model lists for unknown providers.

3. **Async Model Validation**: `ProviderAwareChatOptionsValidator` requires async validation for model availability checking. Use `ValidateAsync()` instead of `Validate()` for full validation.

---

## Future Work

- v0.6.1c: Provider implementations (OpenAI, Anthropic HTTP clients)
- v0.6.1d: Streaming support with SSE parsing
- v0.6.1e: Rate limiting and retry policies
