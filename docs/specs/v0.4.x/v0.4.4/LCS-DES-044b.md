# LCS-DES-044b: OpenAI Connector

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-044b                             |
| **Version**      | v0.4.4b                                  |
| **Title**        | OpenAI Connector                         |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | WriterPro                                |

---

## 1. Overview

### 1.1 Purpose

This specification defines the `OpenAIEmbeddingService`, which implements `IEmbeddingService` using OpenAI's `text-embedding-3-small` model. The service handles API authentication, request formatting, retry logic, and batch processing.

### 1.2 Goals

- Implement embedding generation via OpenAI API
- Securely retrieve API key from `ISecureVault`
- Handle rate limits (429) with Polly retry policies
- Support batch embedding for efficiency
- Provide comprehensive error handling and logging

### 1.3 Non-Goals

- Alternative embedding providers (future)
- Local model support (v0.6.x)
- Caching (v0.4.8)

---

## 2. API Integration

### 2.1 OpenAI Embeddings Endpoint

```http
POST https://api.openai.com/v1/embeddings
Authorization: Bearer sk-...
Content-Type: application/json

{
  "model": "text-embedding-3-small",
  "input": ["Hello world", "Goodbye world"],
  "dimensions": 1536
}
```

**Response:**

```json
{
  "object": "list",
  "data": [
    {
      "object": "embedding",
      "index": 0,
      "embedding": [0.0023064255, -0.009327292, ...]
    },
    {
      "object": "embedding",
      "index": 1,
      "embedding": [-0.0028842522, 0.0073921545, ...]
    }
  ],
  "model": "text-embedding-3-small",
  "usage": {
    "prompt_tokens": 5,
    "total_tokens": 5
  }
}
```

### 2.2 Error Responses

| Status | Meaning | Action |
| :----- | :------ | :----- |
| 400 | Bad Request | Fail immediately, request malformed |
| 401 | Unauthorized | Fail immediately, API key invalid |
| 429 | Too Many Requests | Retry with exponential backoff |
| 500 | Server Error | Retry with exponential backoff |
| 503 | Service Unavailable | Retry with exponential backoff |

---

## 3. Implementation

### 3.1 Class Definition

```csharp
namespace Lexichord.Modules.RAG.Embedding;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

/// <summary>
/// Embedding service using OpenAI's text-embedding-3-small model.
/// </summary>
public sealed class OpenAIEmbeddingService : IEmbeddingService, IDisposable
{
    private const string DefaultApiEndpoint = "https://api.openai.com/v1/embeddings";

    private readonly HttpClient _httpClient;
    private readonly ISecureVault _vault;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<OpenAIEmbeddingService> _logger;
    private readonly string _apiEndpoint;

    public string ModelName => _options.Model;
    public int Dimensions => _options.Dimensions;
    public int MaxTokens => _options.MaxTokens;

    public OpenAIEmbeddingService(
        IHttpClientFactory httpFactory,
        ISecureVault vault,
        IOptions<EmbeddingOptions> options,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _httpClient = httpFactory.CreateClient("OpenAI");
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);

        _vault = vault;
        _options = options.Value;
        _logger = logger;
        _apiEndpoint = _options.ApiBaseUrl ?? DefaultApiEndpoint;

        _retryPolicy = CreateRetryPolicy();
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));

        var results = await EmbedBatchAsync(new[] { text }, ct);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        if (texts.Count == 0)
            return Array.Empty<float[]>();

        if (texts.Count > _options.MaxBatchSize)
            throw new ArgumentException(
                $"Batch size {texts.Count} exceeds maximum of {_options.MaxBatchSize}",
                nameof(texts));

        if (texts.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Texts cannot contain null or empty strings", nameof(texts));

        var apiKey = await GetApiKeyAsync();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var request = new OpenAIEmbeddingRequest
            {
                Model = _options.Model,
                Input = texts.ToList(),
                Dimensions = _options.Dimensions
            };

            _logger.LogDebug(
                "Sending embedding request for {Count} texts to {Model}",
                texts.Count, _options.Model);

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
                {
                    Content = JsonContent.Create(request)
                };
                httpRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                return await _httpClient.SendAsync(httpRequest, ct);
            });

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new EmbeddingException(
                    $"OpenAI API returned {response.StatusCode}: {errorContent}",
                    statusCode: (int)response.StatusCode,
                    isTransient: IsTransientError(response.StatusCode));
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(ct)
                ?? throw new EmbeddingException("Empty response from OpenAI API");

            _logger.LogDebug(
                "Embedded {Count} texts in {LatencyMs}ms, tokens: {Tokens}",
                texts.Count, stopwatch.ElapsedMilliseconds, result.Usage.TotalTokens);

            return result.Data
                .OrderBy(d => d.Index)
                .Select(d => d.Embedding)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP error during embedding request");
            throw new EmbeddingException("Network error during embedding", innerException: ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Embedding request timed out after {Timeout}s", _options.TimeoutSeconds);
            throw new EmbeddingException(
                $"Request timed out after {_options.TimeoutSeconds}s",
                isTransient: true,
                innerException: ex);
        }
    }

    private async Task<string> GetApiKeyAsync()
    {
        var apiKey = await _vault.GetSecretAsync(_options.SecretKeyName);

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new EmbeddingException(
                $"OpenAI API key not found. Configure '{_options.SecretKeyName}' in secure vault.");
        }

        return apiKey;
    }

    private AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => IsTransientError(r.StatusCode))
            .WaitAndRetryAsync(
                _options.MaxRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    var reason = outcome.Exception?.Message
                        ?? $"HTTP {(int?)outcome.Result?.StatusCode}";

                    _logger.LogWarning(
                        "Retry {Attempt}/{MaxRetries} after {Delay}s: {Reason}",
                        attempt, _options.MaxRetries, delay.TotalSeconds, reason);
                });
    }

    private static bool IsTransientError(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
```

### 3.2 Request/Response DTOs

```csharp
namespace Lexichord.Modules.RAG.Embedding;

using System.Text.Json.Serialization;

internal record OpenAIEmbeddingRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("input")]
    public required IReadOnlyList<string> Input { get; init; }

    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; init; }

    [JsonPropertyName("encoding_format")]
    public string? EncodingFormat { get; init; } = "float";
}

internal record OpenAIEmbeddingResponse
{
    [JsonPropertyName("object")]
    public string Object { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public IReadOnlyList<OpenAIEmbeddingData> Data { get; init; } = Array.Empty<OpenAIEmbeddingData>();

    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("usage")]
    public OpenAIUsage Usage { get; init; } = new();
}

internal record OpenAIEmbeddingData
{
    [JsonPropertyName("object")]
    public string Object { get; init; } = string.Empty;

    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("embedding")]
    public float[] Embedding { get; init; } = Array.Empty<float>();
}

internal record OpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; init; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; init; }
}
```

### 3.3 DI Registration

```csharp
// In RAGModule.cs
services.AddHttpClient("OpenAI", client =>
{
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

services.Configure<EmbeddingOptions>(
    configuration.GetSection("Embedding"));

services.AddSingleton<OpenAIEmbeddingService>();
services.AddSingleton<IEmbeddingService>(sp =>
    sp.GetRequiredService<OpenAIEmbeddingService>());
```

---

## 4. Retry Policy

### 4.1 Configuration

| Parameter | Value | Description |
| :-------- | :---- | :---------- |
| Max Retries | 3 | Maximum retry attempts |
| Base Delay | 2s | Initial delay (doubles each retry) |
| Retry On | 429, 5xx | HTTP status codes |

### 4.2 Backoff Schedule

| Attempt | Delay | Cumulative |
| :------ | :---- | :--------- |
| 1 | 2s | 2s |
| 2 | 4s | 6s |
| 3 | 8s | 14s |

### 4.3 Retry Flow

```text
REQUEST
  │
  ├── Success (200) → Return embeddings
  │
  ├── Rate Limited (429)
  │   └── Wait 2^attempt seconds → RETRY
  │
  ├── Server Error (5xx)
  │   └── Wait 2^attempt seconds → RETRY
  │
  ├── Unauthorized (401)
  │   └── Fail immediately (not transient)
  │
  └── Bad Request (400)
      └── Fail immediately (not transient)
```

---

## 5. Configuration

### 5.1 appsettings.json

```json
{
  "Embedding": {
    "Model": "text-embedding-3-small",
    "Dimensions": 1536,
    "MaxTokens": 8191,
    "MaxBatchSize": 100,
    "TimeoutSeconds": 60,
    "MaxRetries": 3,
    "SecretKeyName": "openai:api-key"
  }
}
```

### 5.2 Secure Vault Setup

```csharp
// Store API key securely
await vault.SetSecretAsync("openai:api-key", "sk-...");
```

---

## 6. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.4b")]
public class OpenAIEmbeddingServiceTests
{
    private Mock<ISecureVault> CreateVaultMock(string? apiKey = "sk-test-key")
    {
        var mock = new Mock<ISecureVault>();
        mock.Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync(apiKey);
        return mock;
    }

    private OpenAIEmbeddingService CreateService(
        HttpMessageHandler handler,
        ISecureVault? vault = null)
    {
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        return new OpenAIEmbeddingService(
            httpFactory.Object,
            vault ?? CreateVaultMock().Object,
            Options.Create(EmbeddingOptions.Default),
            NullLogger<OpenAIEmbeddingService>.Instance);
    }

    [Fact]
    public async Task EmbedAsync_ValidText_ReturnsEmbedding()
    {
        // Arrange
        var embedding = Enumerable.Range(0, 1536).Select(i => (float)i * 0.001f).ToArray();
        var response = CreateSuccessResponse(embedding);
        var handler = new MockHttpMessageHandler(response);
        var service = CreateService(handler);

        // Act
        var result = await service.EmbedAsync("Hello world");

        // Assert
        result.Should().HaveCount(1536);
        result.Should().BeEquivalentTo(embedding);
    }

    [Fact]
    public async Task EmbedBatchAsync_MultipleTexts_ReturnsCorrectCount()
    {
        // Arrange
        var texts = new[] { "Text 1", "Text 2", "Text 3" };
        var response = CreateBatchSuccessResponse(texts.Length);
        var handler = new MockHttpMessageHandler(response);
        var service = CreateService(handler);

        // Act
        var results = await service.EmbedBatchAsync(texts);

        // Assert
        results.Should().HaveCount(3);
        results.All(e => e.Length == 1536).Should().BeTrue();
    }

    [Fact]
    public async Task EmbedAsync_EmptyText_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService(new MockHttpMessageHandler());

        // Act & Assert
        await service.Invoking(s => s.EmbedAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EmbedBatchAsync_ExceedsMaxBatch_ThrowsArgumentException()
    {
        // Arrange
        var texts = Enumerable.Range(0, 101).Select(i => $"Text {i}").ToList();
        var service = CreateService(new MockHttpMessageHandler());

        // Act & Assert
        await service.Invoking(s => s.EmbedBatchAsync(texts))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceeds maximum*");
    }

    [Fact]
    public async Task EmbedAsync_NoApiKey_ThrowsEmbeddingException()
    {
        // Arrange
        var vault = CreateVaultMock(null);
        var service = CreateService(new MockHttpMessageHandler(), vault.Object);

        // Act & Assert
        await service.Invoking(s => s.EmbedAsync("Test"))
            .Should().ThrowAsync<EmbeddingException>()
            .WithMessage("*API key not found*");
    }

    [Fact]
    public async Task EmbedAsync_RateLimited_RetriesWithBackoff()
    {
        // Arrange
        var attempts = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            attempts++;
            if (attempts < 3)
            {
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent("{\"error\":\"rate_limited\"}")
                };
            }
            return CreateSuccessResponse(new float[1536]);
        });
        var service = CreateService(handler);

        // Act
        var result = await service.EmbedAsync("Test");

        // Assert
        attempts.Should().Be(3);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task EmbedAsync_Unauthorized_FailsImmediately()
    {
        // Arrange
        var attempts = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"invalid_api_key\"}")
            };
        });
        var service = CreateService(handler);

        // Act & Assert
        await service.Invoking(s => s.EmbedAsync("Test"))
            .Should().ThrowAsync<EmbeddingException>()
            .Where(e => e.StatusCode == 401);

        attempts.Should().Be(1); // No retries for 401
    }

    private HttpResponseMessage CreateSuccessResponse(float[] embedding)
    {
        var response = new OpenAIEmbeddingResponse
        {
            Object = "list",
            Data = new[]
            {
                new OpenAIEmbeddingData { Index = 0, Embedding = embedding }
            },
            Usage = new OpenAIUsage { TotalTokens = 5 }
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };
    }

    private HttpResponseMessage CreateBatchSuccessResponse(int count)
    {
        var data = Enumerable.Range(0, count)
            .Select(i => new OpenAIEmbeddingData
            {
                Index = i,
                Embedding = new float[1536]
            })
            .ToArray();

        var response = new OpenAIEmbeddingResponse
        {
            Object = "list",
            Data = data,
            Usage = new OpenAIUsage { TotalTokens = count * 5 }
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };
    }
}
```

---

## 7. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Debug | "Sending embedding request for {Count} texts to {Model}" | Before API call |
| Debug | "Embedded {Count} texts in {LatencyMs}ms, tokens: {Tokens}" | After success |
| Warning | "Retry {Attempt}/{MaxRetries} after {Delay}s: {Reason}" | On retry |
| Error | "HTTP error during embedding request" | On network failure |
| Error | "Embedding request timed out after {Timeout}s" | On timeout |

---

## 8. File Locations

| File | Path |
| :--- | :--- |
| Service implementation | `src/Lexichord.Modules.RAG/Embedding/OpenAIEmbeddingService.cs` |
| DTOs | `src/Lexichord.Modules.RAG/Embedding/OpenAIEmbeddingModels.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Embedding/OpenAIEmbeddingServiceTests.cs` |

---

## 9. Dependencies

| Dependency | Version | Purpose |
| :--------- | :------ | :------ |
| `ISecureVault` | v0.0.6a | API key retrieval |
| `Polly` | 8.x | Retry policies |
| `System.Net.Http.Json` | 9.0.x | JSON serialization |
| `Microsoft.Extensions.Http` | 9.0.x | HttpClientFactory |

---

## 10. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Single text embeds to 1536-dimensional vector | [ ] |
| 2 | Batch of 100 texts embeds in single request | [ ] |
| 3 | API key retrieved securely from ISecureVault | [ ] |
| 4 | 429 responses trigger retry with exponential backoff | [ ] |
| 5 | 5xx responses trigger retry | [ ] |
| 6 | 401 responses fail immediately (no retry) | [ ] |
| 7 | Timeout after configured TimeoutSeconds | [ ] |
| 8 | Empty/null text throws ArgumentException | [ ] |
| 9 | Batch exceeding MaxBatchSize throws ArgumentException | [ ] |
| 10 | All unit tests pass | [ ] |

---

## 11. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
