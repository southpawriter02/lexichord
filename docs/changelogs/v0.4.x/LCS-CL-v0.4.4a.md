# Changelog: v0.4.4a - Embedding Abstractions

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.4a](../../specs/v0.4.x/v0.4.4/LCS-DES-v0.4.4a.md)

---

## Summary

Defines the core abstractions for the embedding system in `Lexichord.Abstractions.Contracts`. This establishes the contract interfaces for pluggable embedding providers, including the service interface, configuration options, result structures, and exception types. These abstractions form the foundation for the three concrete embedding implementations (OpenAI, Token Counting, Embedding Pipeline) implemented in v0.4.4b-d.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/Embeddings/

| File                         | Type         | Description                                                       |
| :--------------------------- | :----------- | :---------------------------------------------------------------- |
| `IEmbeddingService.cs`       | Interface    | Service contract with methods for embedding text and vectors      |
| `EmbeddingOptions.cs`        | Record       | Configuration with model, dimension, and batch size parameters    |
| `EmbeddingResult.cs`         | Record       | Result structure containing embedding vector and token metadata   |
| `EmbeddingException.cs`      | Exception    | Custom exception with error classification and context handling   |

#### Lexichord.Tests.Unit/Abstractions/Embeddings/

| File                            | Tests | Coverage                                                          |
| :------------------------------ | :---- | :---------------------------------------------------------------- |
| `EmbeddingAbstractionsTests.cs` | 42    | Records, interfaces, validation, exception handling, factory methods |

---

## Technical Details

### Type Summary

| Type                  | Properties                                  | Key Features                          |
| :-------------------- | :------------------------------------------ | :------------------------------------ |
| `IEmbeddingService`   | ServiceName (string)                        | EmbedAsync(), GetDimensionsAsync()    |
| `EmbeddingOptions`    | Model, Dimensions, MaxBatchSize, Timeout    | Validate(), WithDefaults()            |
| `EmbeddingResult`     | Embedding, TokensUsed, Duration, Timestamp  | GetNorm(), GetDistance(), IsValid()   |
| `EmbeddingException`  | ErrorCode, RetryableError, InnerException   | Classification for error handling     |

### IEmbeddingService Contract

| Method                              | Signature                                                 | Description                     |
| :---------------------------------- | :-------------------------------------------------------- | :------------------------------ |
| `EmbedAsync`                        | `Task<EmbeddingResult> EmbedAsync(string text, ...)`      | Generate embedding for text     |
| `EmbedAsync` (batch)                | `Task<List<EmbeddingResult>> EmbedAsync(List<string>, ...)` | Batch embed multiple texts    |
| `GetDimensionsAsync`                | `Task<int> GetDimensionsAsync()`                          | Get vector dimension count      |

### EmbeddingOptions Defaults

| Property           | Default | Description                         |
| :----------------- | :------ | :---------------------------------- |
| `Model`            | null    | Model identifier (provider-specific) |
| `Dimensions`       | 1536    | Output vector dimension             |
| `MaxBatchSize`     | 100     | Maximum texts per batch             |
| `Timeout`          | 30s     | Request timeout duration            |
| `Retries`          | 3       | Number of retry attempts            |
| `RetryDelay`       | 1s      | Initial retry delay (exponential)   |

### Validation Rules

| # | Constraint                              | Error Message                                |
| :- | :--------------------------------------- | :------------------------------------------- |
| 1 | Model is not null or empty              | "Model must be specified"                    |
| 2 | Dimensions > 0                          | "Dimensions must be positive"                |
| 3 | Dimensions <= 16384                     | "Dimensions cannot exceed 16384"             |
| 4 | MaxBatchSize > 0                        | "MaxBatchSize must be positive"              |
| 5 | MaxBatchSize <= 10000                   | "MaxBatchSize cannot exceed 10000"           |
| 6 | Timeout >= 1 second                     | "Timeout must be at least 1 second"          |
| 7 | Retries >= 0                            | "Retries cannot be negative"                 |

### EmbeddingResult Factory Methods

| Method              | Signature                                     | Description                          |
| :------------------ | :--------------------------------------------- | :----------------------------------- |
| `Create`            | `static EmbeddingResult Create(...)`           | Create result from vector data       |
| `CreateFromBase64`  | `static EmbeddingResult CreateFromBase64(...)` | Create from base64-encoded vector    |
| `GetNorm`           | `double GetNorm()`                            | Calculate L2 norm of vector          |
| `GetDistance`       | `double GetDistance(EmbeddingResult other)`    | Cosine distance to another embedding |
| `IsValid`           | `bool IsValid()`                              | Validate vector structure            |

### EmbeddingException Classification

| Error Code           | Retryable | Description                           |
| :------------------- | :-------- | :------------------------------------ |
| `RateLimited`        | Yes       | API rate limit exceeded               |
| `TemporaryUnavailable` | Yes       | Service temporarily unavailable       |
| `NetworkError`       | Yes       | Network connectivity issue            |
| `Unauthorized`       | No        | Invalid credentials or API key        |
| `InvalidInput`       | No        | Malformed or invalid request          |
| `ModelNotFound`      | No        | Specified model does not exist        |
| `DimensionMismatch`  | No        | Vector dimensions incompatible        |
| `InternalError`      | No        | Server-side error                     |
| `Unknown`            | No        | Unclassified error                    |

---

## Verification

```bash
# Build abstractions
dotnet build src/Lexichord.Abstractions
# Result: Build succeeded

# Run v0.4.4a tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.4a"
# Result: 42 tests passed

# Run full regression
dotnet test tests/Lexichord.Tests.Unit
# Result: 3747 passed, no new regressions
```

---

## Test Coverage

| Category                    | Tests |
| :-------------------------- | ----: |
| EmbeddingOptions validation | 7     |
| EmbeddingResult creation    | 8     |
| Factory methods             | 6     |
| Vector operations           | 5     |
| Exception classification    | 9     |
| IEmbeddingService contract  | 4     |
| Integration patterns        | 3     |
| **Total**                   | **42** |

---

## Dependencies

- None (pure abstractions, no external packages required)

## Dependents

- v0.4.4b: OpenAI Connector (implements IEmbeddingService with OpenAI API)
- v0.4.4c: Token Counting (complements embedding with token metrics)
- v0.4.4d: Document Indexing Pipeline (consumes EmbeddingResult output)

---

## Related Documents

- [LCS-DES-v0.4.4a](../../specs/v0.4.x/v0.4.4/LCS-DES-v0.4.4a.md) - Design specification
- [LCS-SBD-v0.4.4 §4.1](../../specs/v0.4.x/v0.4.4/LCS-SBD-v0.4.4.md#41-v044a-embedding-abstractions) - Scope breakdown
- [LCS-DES-v0.4.4-INDEX](../../specs/v0.4.x/v0.4.4/LCS-DES-v0.4.4-INDEX.md) - Version index
- [LCS-CL-v0.4.3d](./LCS-CL-v0.4.3d.md) - Previous version (Markdown Header Chunker)
