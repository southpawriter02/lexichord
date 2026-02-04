# LCS-CL-062d: Detailed Changelog — Token Counting Service

## Metadata

| Field        | Value                                        |
| :----------- | :------------------------------------------- |
| **Version**  | v0.6.2d                                      |
| **Released** | 2026-02-04                                   |
| **Category** | Implementation / Module                      |
| **Parent**   | [v0.6.2 Changelog](../CHANGELOG.md#v062)     |
| **Spec**     | [LCS-DES-062d](../../specs/v0.6.x/v0.6.2/LCS-DES-v0.6.2d.md) |

---

## Summary

This release implements a comprehensive token counting service for the LLM module. It provides model-specific tokenization (exact for GPT models, approximate for Claude), cost estimation, and context window management. The implementation uses Microsoft.ML.Tokenizers for Tiktoken-compatible tokenization and includes a caching layer for efficient tokenizer reuse across requests.

---

## New Features

### 1. ILLMTokenCounter Interface

Added `ILLMTokenCounter` interface providing the public API for token counting:

```csharp
public interface ILLMTokenCounter
{
    int CountTokens(string? text, string model);
    int CountTokens(IEnumerable<ChatMessage>? messages, string model);
    int EstimateResponseTokens(int promptTokens, int maxTokens);
    int GetModelLimit(string model);
    int GetMaxOutputTokens(string model);
    decimal CalculateCost(string model, int inputTokens, int outputTokens);
    bool IsExactTokenizer(string model);
}
```

**Features:**
- Text and message token counting
- Response token estimation (60% of max tokens heuristic)
- Context window and max output token queries
- Cost estimation with model-specific pricing
- Tokenizer accuracy check (exact vs approximate)

**File:** `src/Lexichord.Modules.LLM/TokenCounting/ILLMTokenCounter.cs`

### 2. ITokenizer Internal Interface

Added internal `ITokenizer` interface for tokenizer abstraction:

```csharp
internal interface ITokenizer
{
    string ModelFamily { get; }
    bool IsExact { get; }
    int CountTokens(string text);
}
```

**Implementations:**
| Class | IsExact | Model Families |
| ----- | ------- | -------------- |
| `MlTokenizerWrapper` | true | GPT-4o, GPT-4, GPT-3.5 |
| `ApproximateTokenizer` | false | Claude, unknown |

**File:** `src/Lexichord.Modules.LLM/TokenCounting/ITokenizer.cs`

### 3. MlTokenizerWrapper

Added `MlTokenizerWrapper` for exact ML-based tokenization:

```csharp
internal sealed class MlTokenizerWrapper : ITokenizer
{
    public MlTokenizerWrapper(Tokenizer tokenizer, string modelFamily);
    public string ModelFamily { get; }
    public bool IsExact => true;
    public int CountTokens(string text);
}
```

**Features:**
- Wraps `Microsoft.ML.Tokenizers.Tokenizer`
- Uses `EncodeToIds` for efficient token counting
- Thread-safe through underlying tokenizer
- Returns 0 for null/empty input

**Encoding Support:**
| Encoding | Models |
| -------- | ------ |
| o200k_base | GPT-4o, GPT-4o-mini |
| cl100k_base | GPT-4, GPT-4 Turbo, GPT-3.5 Turbo |

**File:** `src/Lexichord.Modules.LLM/TokenCounting/MlTokenizerWrapper.cs`

### 4. ApproximateTokenizer

Added `ApproximateTokenizer` for heuristic-based tokenization:

```csharp
internal sealed class ApproximateTokenizer : ITokenizer
{
    public const double DefaultCharsPerToken = 4.0;
    public const double MinCharsPerToken = 0.5;
    public const double MaxCharsPerToken = 10.0;

    public ApproximateTokenizer(double charsPerToken = 4.0, string modelFamily = "unknown");
    public double CharsPerToken { get; }
    public string ModelFamily { get; }
    public bool IsExact => false;
    public int CountTokens(string text);
}
```

**Features:**
- Default 4 characters per token ratio
- Uses ceiling for conservative estimates
- Thread-safe and stateless
- Accuracy: ~10-20% variance from actual counts

**File:** `src/Lexichord.Modules.LLM/TokenCounting/ApproximateTokenizer.cs`

### 5. TokenizerFactory

Added `TokenizerFactory` for creating model-specific tokenizers:

```csharp
internal sealed class TokenizerFactory
{
    public TokenizerFactory(ILogger<TokenizerFactory> logger);
    public ITokenizer CreateForModel(string model);
    public bool IsExactTokenizer(string model);
}
```

**Model-to-Tokenizer Mapping:**
| Model Pattern | Tokenizer | Encoding |
| ------------- | --------- | -------- |
| gpt-4o* | MlTokenizerWrapper | o200k_base |
| gpt-4* | MlTokenizerWrapper | cl100k_base |
| gpt-3.5* | MlTokenizerWrapper | cl100k_base |
| claude* | ApproximateTokenizer | ~4 chars/token |
| (other) | ApproximateTokenizer | ~4 chars/token |

**File:** `src/Lexichord.Modules.LLM/TokenCounting/TokenizerFactory.cs`

### 6. TokenizerCache

Added `TokenizerCache` for thread-safe tokenizer instance caching:

```csharp
internal sealed class TokenizerCache
{
    public TokenizerCache(ILogger<TokenizerCache> logger);
    public ITokenizer GetOrCreate(string model, Func<ITokenizer> factory);
    public bool ContainsKey(string model);
    public void Clear();
    public int Count { get; }
}
```

**Cache Key Normalization:**
| Input | Cache Key |
| ----- | --------- |
| gpt-4o, gpt-4o-mini | gpt-4o |
| gpt-4, gpt-4-turbo | gpt-4 |
| gpt-3.5-turbo | gpt-3.5 |
| claude-3-*, claude-* | claude |
| (other) | lowercase model name |

**Thread Safety:** Uses `ConcurrentDictionary<string, Lazy<ITokenizer>>` for exactly-once initialization under concurrent access.

**File:** `src/Lexichord.Modules.LLM/TokenCounting/TokenizerCache.cs`

### 7. ModelTokenLimits

Added `ModelTokenLimits` static class for pricing and limit data:

```csharp
public static class ModelTokenLimits
{
    public const int DefaultContextWindow = 8192;
    public const int DefaultMaxOutputTokens = 4096;

    public static int GetContextWindow(string model);
    public static int GetMaxOutputTokens(string model);
    public static ModelPricing? GetPricing(string model);
    public static decimal CalculateCost(string model, int inputTokens, int outputTokens);
    public static bool HasPricing(string model);
    public static IReadOnlyList<string> GetAllKnownModels();
}

public readonly record struct ModelPricing(
    int ContextWindow,
    int MaxOutputTokens,
    decimal InputPricePerMillion,
    decimal OutputPricePerMillion);
```

**Supported Models:**
| Model | Context | Max Output | Input $/M | Output $/M |
| ----- | ------- | ---------- | --------- | ---------- |
| gpt-4o | 128,000 | 16,384 | $2.50 | $10.00 |
| gpt-4o-mini | 128,000 | 16,384 | $0.15 | $0.60 |
| gpt-4-turbo | 128,000 | 4,096 | $10.00 | $30.00 |
| gpt-4 | 8,192 | 4,096 | $30.00 | $60.00 |
| gpt-3.5-turbo | 16,385 | 4,096 | $0.50 | $1.50 |
| claude-3-5-sonnet-20241022 | 200,000 | 8,192 | $3.00 | $15.00 |
| claude-3-opus-20240229 | 200,000 | 4,096 | $15.00 | $75.00 |
| claude-3-sonnet-20240229 | 200,000 | 4,096 | $3.00 | $15.00 |
| claude-3-haiku-20240307 | 200,000 | 4,096 | $0.25 | $1.25 |

**File:** `src/Lexichord.Modules.LLM/TokenCounting/ModelTokenLimits.cs`

### 8. LLMTokenCounter Implementation

Added `LLMTokenCounter` implementing `ILLMTokenCounter`:

```csharp
internal sealed class LLMTokenCounter : ILLMTokenCounter
{
    public const int DefaultMessageOverheadTokens = 4;
    public const double DefaultResponseEstimateFactor = 0.6;

    public LLMTokenCounter(
        TokenizerCache cache,
        TokenizerFactory factory,
        ILogger<LLMTokenCounter> logger);
}
```

**Message Overhead:** Adds 4 tokens per message for role tokens and delimiters.

**Response Estimation:** Uses 60% of max tokens as typical response estimate.

**File:** `src/Lexichord.Modules.LLM/TokenCounting/LLMTokenCounter.cs`

### 9. DI Registration Extension

Added `TokenCountingServiceCollectionExtensions` for service registration:

```csharp
public static class TokenCountingServiceCollectionExtensions
{
    public static IServiceCollection AddTokenCounting(this IServiceCollection services);
}
```

**Registered Services:**
| Service | Lifetime | Description |
| ------- | -------- | ----------- |
| `TokenizerCache` | Singleton | Caches tokenizer instances |
| `TokenizerFactory` | Singleton | Creates model-specific tokenizers |
| `ILLMTokenCounter` | Singleton | Main token counting service |

**File:** `src/Lexichord.Modules.LLM/Extensions/TokenCountingServiceCollectionExtensions.cs`

### 10. Structured Logging Events

Added token counting logging events (1900-1912 range):

| Event ID | Level | Description |
| -------- | ----- | ----------- |
| 1900 | Trace | Tokens counted for text |
| 1901 | Debug | Tokens counted for messages |
| 1902 | Debug | Response tokens estimated |
| 1903 | Debug | Cost calculated |
| 1904 | Debug | Tokenizer created for known model |
| 1905 | Warning | Tokenizer created for unknown model |
| 1906 | Trace | Tokenizer cache hit |
| 1907 | Debug | Tokenizer cache creating |
| 1908 | Information | Tokenizer cache cleared |
| 1909 | Debug | Model pricing not found |
| 1910 | Trace | Individual message token detail |
| 1911 | Trace | Model limit queried |
| 1912 | Trace | Max output tokens queried |

**File:** `src/Lexichord.Modules.LLM/Logging/LLMLogEvents.cs`

---

## Modified Files

### LLMModule

Added token counting service registration:

```csharp
// LOGIC: Register token counting service (v0.6.2d).
services.AddTokenCounting();
```

Updated module description to include token counting service.

Updated services provided documentation to include `ILLMTokenCounter`.

**File:** `src/Lexichord.Modules.LLM/LLMModule.cs`

### Lexichord.Modules.LLM.csproj

Added NuGet package references:

```xml
<!-- v0.6.2d: ML Tokenizers for accurate token counting (Tiktoken) -->
<PackageReference Include="Microsoft.ML.Tokenizers" Version="0.22.0" />
<PackageReference Include="Microsoft.ML.Tokenizers.Data.Cl100kBase" Version="0.22.0" />
<PackageReference Include="Microsoft.ML.Tokenizers.Data.O200kBase" Version="0.22.0" />
```

Added `InternalsVisibleTo` for NSubstitute proxy generation:

```xml
<InternalsVisibleTo Include="DynamicProxyGenAssembly2, PublicKey=..." />
```

**File:** `src/Lexichord.Modules.LLM/Lexichord.Modules.LLM.csproj`

---

## New Files

| File Path | Description |
| --------- | ----------- |
| `src/Lexichord.Modules.LLM/TokenCounting/ITokenizer.cs` | Internal tokenizer abstraction |
| `src/Lexichord.Modules.LLM/TokenCounting/ILLMTokenCounter.cs` | Public token counting interface |
| `src/Lexichord.Modules.LLM/TokenCounting/LLMTokenCounter.cs` | Main implementation |
| `src/Lexichord.Modules.LLM/TokenCounting/TokenizerFactory.cs` | Model-specific tokenizer creation |
| `src/Lexichord.Modules.LLM/TokenCounting/TokenizerCache.cs` | Thread-safe tokenizer caching |
| `src/Lexichord.Modules.LLM/TokenCounting/MlTokenizerWrapper.cs` | ML-based tokenizer wrapper |
| `src/Lexichord.Modules.LLM/TokenCounting/ApproximateTokenizer.cs` | Heuristic tokenizer |
| `src/Lexichord.Modules.LLM/TokenCounting/ModelTokenLimits.cs` | Pricing and limit data |
| `src/Lexichord.Modules.LLM/Extensions/TokenCountingServiceCollectionExtensions.cs` | DI registration |

---

## Unit Tests

Added comprehensive unit tests for token counting components:

| Test File | Test Count | Coverage |
| --------- | ---------- | -------- |
| `ApproximateTokenizerTests.cs` | 15 | Constructor, CountTokens, constants |
| `MlTokenizerWrapperTests.cs` | 14 | Constructor, CountTokens, ModelFamily |
| `TokenizerFactoryTests.cs` | 18 | All model families, validation |
| `TokenizerCacheTests.cs` | 14 | GetOrCreate, Clear, ContainsKey, normalization |
| `ModelTokenLimitsTests.cs` | 20 | Context windows, pricing, cost calculation |
| `LLMTokenCounterTests.cs` | 23 | All interface methods, constants |
| **Total** | **~104** | |

Test files location: `tests/Lexichord.Tests.Unit/Modules/LLM/TokenCounting/`

---

## Usage Examples

### Basic Token Counting

```csharp
var tokenCounter = serviceProvider.GetRequiredService<ILLMTokenCounter>();

// Count tokens in text
var count = tokenCounter.CountTokens("Hello, world!", "gpt-4o");

// Count tokens in messages
var messages = new[]
{
    ChatMessage.System("You are a helpful assistant."),
    ChatMessage.User("What is 2+2?"),
};
var messageCount = tokenCounter.CountTokens(messages, "gpt-4o");
```

### Cost Estimation

```csharp
var tokenCounter = serviceProvider.GetRequiredService<ILLMTokenCounter>();

// Calculate cost for a request
var inputTokens = 1000;
var outputTokens = 500;
var cost = tokenCounter.CalculateCost("gpt-4o", inputTokens, outputTokens);
// cost = $0.0075

// Estimate response tokens
var estimated = tokenCounter.EstimateResponseTokens(inputTokens, maxTokens: 4096);
// estimated = 2458 (60% of maxTokens)
```

### Context Window Management

```csharp
var tokenCounter = serviceProvider.GetRequiredService<ILLMTokenCounter>();

// Get model limits
var contextWindow = tokenCounter.GetModelLimit("gpt-4o");      // 128000
var maxOutput = tokenCounter.GetMaxOutputTokens("gpt-4o");      // 16384

// Calculate available tokens
var messages = new[] { ChatMessage.User("Hello!") };
var usedTokens = tokenCounter.CountTokens(messages, "gpt-4o");
var availableForResponse = contextWindow - usedTokens - maxOutput;
```

### Tokenizer Accuracy Check

```csharp
var tokenCounter = serviceProvider.GetRequiredService<ILLMTokenCounter>();

if (tokenCounter.IsExactTokenizer("gpt-4o"))
{
    // Token count is exact
}
else
{
    // Token count is approximate (~10-20% variance)
}
```

---

## Dependencies

### Internal Dependencies

| Interface | Version | Usage |
| --------- | ------- | ----- |
| `ChatMessage` | v0.4.3a | Message token counting |
| `ILogger<T>` | v0.0.3b | Structured logging |

### External Dependencies

| Package | Version | Usage |
| ------- | ------- | ----- |
| `Microsoft.ML.Tokenizers` | 0.22.0 | Tiktoken tokenization |
| `Microsoft.ML.Tokenizers.Data.Cl100kBase` | 0.22.0 | GPT-4/3.5 encoding data |
| `Microsoft.ML.Tokenizers.Data.O200kBase` | 0.22.0 | GPT-4o encoding data |

---

## Breaking Changes

None. This is a new feature addition with no impact on existing code.

---

## Migration Guide

No migration required. The token counting service is automatically registered when using the LLM module.

To use the service, inject `ILLMTokenCounter` via dependency injection:

```csharp
public class MyService
{
    private readonly ILLMTokenCounter _tokenCounter;

    public MyService(ILLMTokenCounter tokenCounter)
    {
        _tokenCounter = tokenCounter;
    }
}
```
