// =============================================================================
// File: OpenAIEmbeddingService.cs
// Project: Lexichord.Modules.RAG
// Description: OpenAI-backed implementation of IEmbeddingService with Polly retry policy.
// =============================================================================
// LOGIC: Production-ready embedding service with comprehensive error handling.
//   - Constructor validates options and retrieves API key from secure vault.
//   - EmbedAsync delegates to EmbedBatchAsync for single-text operations.
//   - EmbedBatchAsync validates inputs, partitions large batches, applies retry policy.
//   - Polly retry policy: exponential backoff (2^attempt), retries on 429/5xx, fails on 4xx.
//   - IsTransientError() determines if error warrants retry or immediate failure.
//   - Comprehensive logging at Debug, Warning, and Error levels for observability.
//   - Thread-safe and stateless for concurrent usage.
// =============================================================================

using System.Net;
using System.Text.Json;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Security;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Lexichord.Modules.RAG.Embedding;

/// <summary>
/// OpenAI-backed implementation of <see cref="IEmbeddingService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OpenAIEmbeddingService"/> provides semantic embeddings by delegating to
/// the OpenAI Embeddings API with automatic retry logic and comprehensive error handling.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>Supports text-embedding-3-small and text-embedding-3-large models.</item>
///   <item>Automatic batch partitioning for large inputs.</item>
///   <item>Polly-based exponential backoff retry strategy.</item>
///   <item>Transient vs. permanent error classification.</item>
///   <item>Comprehensive structured logging.</item>
///   <item>Thread-safe for concurrent operations.</item>
/// </list>
/// </para>
/// <para>
/// <b>Error Handling:</b>
/// </para>
/// <list type="bullet">
///   <item>Retries: 429 (rate limit), 500, 502, 503, 504 (transient).</item>
///   <item>Fails immediately: 400 (bad request), 401 (unauthorized).</item>
///   <item>Timeouts and network errors: Retried per policy.</item>
/// </list>
/// <para>
/// <b>Configuration:</b> Requires <see cref="EmbeddingOptions"/> with Model, Dimensions,
/// and MaxTokens set appropriately for the chosen model. The API key is retrieved from
/// <see cref="ISecureVault"/> using the key name in <see cref="EmbeddingOptions.SecretKeyName"/>.
/// </para>
/// </remarks>
public sealed class OpenAIEmbeddingService : IEmbeddingService, IDisposable
{
    private const string OpenAIApiBaseUrl = "https://api.openai.com/v1";
    private const string EmbeddingsEndpoint = "/embeddings";
    private const int MaxRetries = 3;
    private const int InitialBackoffSeconds = 2;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecureVault _vault;
    private readonly IOptions<EmbeddingOptions> _optionsAccessor;
    private readonly ILogger<OpenAIEmbeddingService> _logger;
    private readonly EmbeddingOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private string? _cachedApiKey;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIEmbeddingService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for API requests.</param>
    /// <param name="vault">Secure vault for retrieving the API key.</param>
    /// <param name="optionsAccessor">Options containing model, dimensions, and vault key name.</param>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <remarks>
    /// <para>
    /// The constructor validates that <see cref="EmbeddingOptions"/> contains valid
    /// configuration values and logs initialization success.
    /// </para>
    /// <para>
    /// <b>API Key Retrieval:</b> The API key is retrieved lazily on first use via
    /// <see cref="GetApiKeyAsync"/> to defer vault access until needed.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="EmbeddingOptions"/> validation fails (see <see cref="EmbeddingOptions.Validate"/>).
    /// </exception>
    public OpenAIEmbeddingService(
        IHttpClientFactory httpClientFactory,
        ISecureVault vault,
        IOptions<EmbeddingOptions> optionsAccessor,
        ILogger<OpenAIEmbeddingService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(vault);
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _vault = vault;
        _optionsAccessor = optionsAccessor;
        _logger = logger;

        // LOGIC: Validate options to ensure consistent configuration.
        _options = optionsAccessor.Value;
        _options.Validate();

        // LOGIC: Create Polly retry policy with exponential backoff.
        _retryPolicy = CreateRetryPolicy();

        _logger.LogDebug(
            "Initialized OpenAI Embedding Service with model '{Model}', dimensions {Dimensions}",
            _options.Model, _options.Dimensions);
    }

    /// <inheritdoc/>
    public string ModelName => _options.Model;

    /// <inheritdoc/>
    public int Dimensions => _options.Dimensions;

    /// <inheritdoc/>
    public int MaxTokens => _options.MaxTokens;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method delegates to <see cref="EmbedBatchAsync"/> with a single-element
    /// collection for consistent error handling and retry logic.
    /// </para>
    /// </remarks>
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));

        _logger.LogDebug("Embedding text of length {TextLength}", text.Length);

        var results = await EmbedBatchAsync(new[] { text }, ct).ConfigureAwait(false);
        return results[0];
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method validates inputs, partitions large batches into API-compatible sizes,
    /// and applies the Polly retry policy to each batch request. Results are returned
    /// in the same order as inputs.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        // LOGIC: Validate input batch.
        if (texts == null)
            throw new ArgumentNullException(nameof(texts), "Texts collection cannot be null");

        if (texts.Count == 0)
            throw new ArgumentException("Texts collection cannot be empty", nameof(texts));

        if (texts.Any(t => t == null))
            throw new ArgumentException("Texts collection cannot contain null elements", nameof(texts));

        if (texts.Count > _options.MaxBatchSize)
        {
            _logger.LogWarning(
                "Batch size {BatchSize} exceeds maximum {MaxBatchSize}, partitioning into smaller batches",
                texts.Count, _options.MaxBatchSize);
        }

        _logger.LogDebug("Embedding batch of {TextCount} texts", texts.Count);

        // LOGIC: Partition batch if it exceeds MaxBatchSize and recursively embed each partition.
        if (texts.Count > _options.MaxBatchSize)
        {
            var allResults = new List<float[]>();
            for (int i = 0; i < texts.Count; i += _options.MaxBatchSize)
            {
                var partitionSize = Math.Min(_options.MaxBatchSize, texts.Count - i);
                var partition = texts.Skip(i).Take(partitionSize).ToList();
                var partitionResults = await EmbedBatchAsync(partition, ct).ConfigureAwait(false);
                allResults.AddRange(partitionResults);
            }
            return allResults;
        }

        // LOGIC: Retrieve API key from secure vault.
        var apiKey = await GetApiKeyAsync(ct).ConfigureAwait(false);

        // LOGIC: Build and send API request with retry policy.
        var request = new OpenAIEmbeddingRequest
        {
            Model = _options.Model,
            Input = texts,
            Dimensions = _options.Dimensions > 0 ? _options.Dimensions : null,
            EncodingFormat = "float"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var httpClient = _httpClientFactory.CreateClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Post,
            new Uri($"{_options.ApiBaseUrl ?? OpenAIApiBaseUrl}{EmbeddingsEndpoint}"))
        {
            Content = requestContent
        };
        requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
        requestMessage.Headers.Add("User-Agent", "Lexichord/0.4.4b");

        _logger.LogDebug(
            "Sending embedding request to {Endpoint} with {TextCount} texts",
            requestMessage.RequestUri, texts.Count);

        // LOGIC: Apply retry policy to the HTTP request.
        var response = await _retryPolicy.ExecuteAsync(
            () => httpClient.SendAsync(requestMessage, ct)).ConfigureAwait(false);

        // LOGIC: Handle non-successful responses.
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!IsTransientError(response.StatusCode))
            {
                _logger.LogError(
                    "Non-transient error embedding texts: {StatusCode} {ReasonPhrase}. Response: {Content}",
                    response.StatusCode, response.ReasonPhrase, content);

                throw new EmbeddingException(
                    $"OpenAI API returned {response.StatusCode}: {response.ReasonPhrase}. {content}",
                    statusCode: (int)response.StatusCode);
            }

            // LOGIC: Transient error after retry exhaustion.
            _logger.LogError(
                "Transient error persisted after retries: {StatusCode} {ReasonPhrase}",
                response.StatusCode, response.ReasonPhrase);

            throw new EmbeddingException(
                $"OpenAI API returned transient error {response.StatusCode} after retries: {response.ReasonPhrase}",
                isTransient: true,
                statusCode: (int)response.StatusCode);
        }

        // LOGIC: Parse successful response.
        var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        OpenAIEmbeddingResponse? apiResponse;
        try
        {
            apiResponse = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(responseContent);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize OpenAI API response: {Content}", responseContent);
            throw new EmbeddingException("Invalid response format from OpenAI API", innerException: ex);
        }

        if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
        {
            _logger.LogError("OpenAI API response contains no embeddings");
            throw new EmbeddingException("OpenAI API returned empty embeddings data");
        }

        if (apiResponse.Data.Count != texts.Count)
        {
            _logger.LogWarning(
                "Expected {ExpectedCount} embeddings but got {ActualCount}",
                texts.Count, apiResponse.Data.Count);
        }

        // LOGIC: Extract embeddings, preserving order by index.
        var embeddings = apiResponse.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding.ToArray())
            .ToList();

        _logger.LogDebug(
            "Successfully embedded {TextCount} texts, token usage: {PromptTokens}",
            texts.Count, apiResponse.Usage?.PromptTokens ?? 0);

        return embeddings;
    }

    /// <summary>
    /// Creates a Polly retry policy with exponential backoff.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The policy retries transient errors (429, 500, 502, 503, 504) with exponential
    /// backoff: 2^attempt seconds. A jitter of ±10% is added to prevent thundering herd.
    /// Permanent errors (4xx except 429) fail immediately without retry.
    /// </para>
    /// </remarks>
    /// <returns>An <see cref="IAsyncPolicy{HttpResponseMessage}"/> implementing the retry strategy.</returns>
    private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r =>
            {
                var code = r.StatusCode;
                return code == HttpStatusCode.TooManyRequests ||
                       code == HttpStatusCode.InternalServerError ||
                       code == HttpStatusCode.BadGateway ||
                       code == HttpStatusCode.ServiceUnavailable ||
                       code == HttpStatusCode.GatewayTimeout;
            })
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetries,
                sleepDurationProvider: attempt =>
                {
                    // LOGIC: Exponential backoff: 2^attempt seconds with ±10% jitter.
                    var baseDelay = Math.Pow(InitialBackoffSeconds, attempt);
                    var jitter = 0.9 + (new Random().NextDouble() * 0.2); // ±10%
                    var delaySeconds = baseDelay * jitter;
                    return TimeSpan.FromSeconds(delaySeconds);
                },
                onRetry: (outcome, timespan, retryNumber, context) =>
                {
                    var message = outcome.Exception != null
                        ? $"Exception: {outcome.Exception.GetType().Name}"
                        : $"Status: {outcome.Result?.StatusCode}";

                    _logger.LogWarning(
                        "Retry {RetryNumber} after {DelayMs}ms due to {Message}",
                        retryNumber, timespan.TotalMilliseconds, message);
                });
    }

    /// <summary>
    /// Retrieves the API key from the secure vault, with caching for performance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The API key is cached after first retrieval to avoid repeated vault access.
    /// This assumes the API key does not change during the service lifetime.
    /// </para>
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The decrypted API key.</returns>
    /// <exception cref="EmbeddingException">
    /// Thrown when the API key cannot be retrieved from the vault.
    /// </exception>
    private async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        // LOGIC: Return cached API key if available.
        if (!string.IsNullOrEmpty(_cachedApiKey))
            return _cachedApiKey;

        _logger.LogDebug("Retrieving API key from secure vault using key '{SecretKeyName}'",
            _options.SecretKeyName);

        try
        {
            _cachedApiKey = await _vault.GetSecretAsync(_options.SecretKeyName, ct).ConfigureAwait(false);

            if (string.IsNullOrEmpty(_cachedApiKey))
            {
                throw new EmbeddingException(
                    $"API key retrieved from vault is empty. Check '{_options.SecretKeyName}' is properly configured.");
            }

            _logger.LogDebug("Successfully retrieved API key from vault");
            return _cachedApiKey;
        }
        catch (Exception ex) when (!(ex is EmbeddingException))
        {
            _logger.LogError(ex, "Failed to retrieve API key from vault using key '{SecretKeyName}'",
                _options.SecretKeyName);

            throw new EmbeddingException(
                $"Failed to retrieve API key from secure vault: {ex.Message}",
                innerException: ex);
        }
    }

    /// <summary>
    /// Determines if an HTTP status code represents a transient error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transient errors (429, 5xx) should be retried. Permanent errors (4xx except 429)
    /// should fail immediately without retry.
    /// </para>
    /// </remarks>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>
    /// <c>true</c> if the error is transient (429, 500-504);
    /// <c>false</c> if the error is permanent (400-499 except 429).
    /// </returns>
    private static bool IsTransientError(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests ||
               (int)statusCode >= 500 && (int)statusCode < 600;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Disposes the HTTP client factory if applicable and clears the cached API key.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        // LOGIC: Clear cached API key from memory.
        if (!string.IsNullOrEmpty(_cachedApiKey))
        {
            Array.Clear(_cachedApiKey.ToCharArray(), 0, _cachedApiKey.Length);
            _cachedApiKey = null;
        }

        _disposed = true;
    }
}
