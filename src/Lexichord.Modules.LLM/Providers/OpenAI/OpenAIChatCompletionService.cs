// -----------------------------------------------------------------------
// <copyright file="OpenAIChatCompletionService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.LLM.Providers.OpenAI;

/// <summary>
/// OpenAI implementation of the <see cref="IChatCompletionService"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This service provides integration with the OpenAI Chat Completions API, supporting
/// both synchronous completion and streaming responses via Server-Sent Events (SSE).
/// </para>
/// <para>
/// <b>Authentication:</b> The API key is retrieved from <see cref="ISecureVault"/>
/// using the key pattern <c>openai:api-key</c>. The key must be stored before
/// using this service.
/// </para>
/// <para>
/// <b>Supported Models:</b>
/// </para>
/// <list type="bullet">
///   <item><description>gpt-4o - Latest GPT-4 Omni model</description></item>
///   <item><description>gpt-4o-mini - Efficient GPT-4 Omni variant (default)</description></item>
///   <item><description>gpt-4-turbo - GPT-4 Turbo model</description></item>
///   <item><description>gpt-3.5-turbo - Cost-effective legacy model</description></item>
/// </list>
/// <para>
/// <b>Error Handling:</b> The service maps OpenAI API errors to the Lexichord
/// exception hierarchy:
/// </para>
/// <list type="bullet">
///   <item><description>401 → <see cref="AuthenticationException"/></description></item>
///   <item><description>429 → <see cref="RateLimitException"/> with RetryAfter</description></item>
///   <item><description>5xx → <see cref="ChatCompletionException"/></description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Using with dependency injection
/// var service = serviceProvider.GetRequiredService&lt;OpenAIChatCompletionService&gt;();
///
/// // Synchronous completion
/// var request = ChatRequest.FromUserMessage("What is the capital of France?");
/// var response = await service.CompleteAsync(request);
/// Console.WriteLine(response.Content);
///
/// // Streaming completion
/// await foreach (var token in service.StreamAsync(request))
/// {
///     Console.Write(token.Token);
/// }
/// </code>
/// </example>
public class OpenAIChatCompletionService : IChatCompletionService
{
    /// <summary>
    /// The HTTP client factory for creating HTTP clients.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// The secure vault for API key retrieval.
    /// </summary>
    private readonly ISecureVault _vault;

    /// <summary>
    /// The OpenAI provider configuration options.
    /// </summary>
    private readonly OpenAIOptions _options;

    /// <summary>
    /// The logger instance for this service.
    /// </summary>
    private readonly ILogger<OpenAIChatCompletionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIChatCompletionService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating clients.</param>
    /// <param name="vault">The secure vault for API key storage.</param>
    /// <param name="options">The OpenAI configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required parameter is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This constructor validates all dependencies at construction time to fail fast
    /// if the service is misconfigured.
    /// </para>
    /// </remarks>
    public OpenAIChatCompletionService(
        IHttpClientFactory httpClientFactory,
        ISecureVault vault,
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIChatCompletionService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <value>Returns "OpenAI".</value>
    public string ProviderName => "OpenAI";

    /// <inheritdoc />
    /// <exception cref="ProviderNotConfiguredException">
    /// Thrown when the API key is not found in the secure vault.
    /// </exception>
    public async Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var model = request.Options?.Model ?? _options.DefaultModel;
        LLMLogEvents.OpenAICompletionStarting(_logger, model);

        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Retrieve API key from secure vault.
        var apiKey = await GetApiKeyAsync(cancellationToken).ConfigureAwait(false);

        // LOGIC: Create HTTP client with configured timeout and resilience policies.
        var httpClient = _httpClientFactory.CreateClient(OpenAIOptions.HttpClientName);

        // LOGIC: Build the HTTP request using the existing parameter mapper.
        using var httpRequest = BuildHttpRequest(request, apiKey, stream: false);
        LLMLogEvents.OpenAIBuildingRequest(_logger, _options.CompletionsEndpoint);

        try
        {
            // LOGIC: Send the request and get the response.
            using var httpResponse = await httpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            var responseBody = await httpResponse.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            LLMLogEvents.OpenAIRawResponse(_logger, responseBody.Length);

            // LOGIC: Handle error responses by mapping to appropriate exceptions.
            if (!httpResponse.IsSuccessStatusCode)
            {
                var retryAfter = httpResponse.Headers.RetryAfter?.Delta;
                LLMLogEvents.OpenAIApiError(_logger, (int)httpResponse.StatusCode, null);
                throw OpenAIResponseParser.ParseErrorResponse(
                    httpResponse.StatusCode,
                    responseBody,
                    retryAfter);
            }

            // LOGIC: Parse successful response.
            LLMLogEvents.OpenAIParsingSuccessResponse(_logger);
            var response = OpenAIResponseParser.ParseSuccessResponse(responseBody, stopwatch.Elapsed);

            LLMLogEvents.OpenAICompletionSucceeded(
                _logger,
                stopwatch.ElapsedMilliseconds,
                response.PromptTokens,
                response.CompletionTokens);

            return response;
        }
        catch (HttpRequestException ex)
        {
            LLMLogEvents.OpenAIHttpRequestFailed(_logger, ex, ex.Message);
            throw new ChatCompletionException(
                $"HTTP request to OpenAI failed: {ex.Message}",
                ProviderName,
                ex);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ProviderNotConfiguredException">
    /// Thrown when the API key is not found in the secure vault.
    /// </exception>
    public async IAsyncEnumerable<StreamingChatToken> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var model = request.Options?.Model ?? _options.DefaultModel;
        LLMLogEvents.OpenAIStreamingStarting(_logger, model);

        // LOGIC: Retrieve API key from secure vault.
        var apiKey = await GetApiKeyAsync(cancellationToken).ConfigureAwait(false);

        // LOGIC: Create HTTP client with configured timeout and resilience policies.
        var httpClient = _httpClientFactory.CreateClient(OpenAIOptions.HttpClientName);

        // LOGIC: Build the HTTP request with streaming enabled.
        using var httpRequest = BuildHttpRequest(request, apiKey, stream: true);
        LLMLogEvents.OpenAIBuildingRequest(_logger, _options.CompletionsEndpoint);

        HttpResponseMessage? httpResponse = null;
        try
        {
            // LOGIC: Send request with ResponseHeadersRead to start streaming immediately.
            httpResponse = await httpClient
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            // LOGIC: Handle error responses before attempting to read the stream.
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                var retryAfter = httpResponse.Headers.RetryAfter?.Delta;
                LLMLogEvents.OpenAIApiError(_logger, (int)httpResponse.StatusCode, null);
                throw OpenAIResponseParser.ParseErrorResponse(
                    httpResponse.StatusCode,
                    errorBody,
                    retryAfter);
            }

            LLMLogEvents.OpenAIStreamStarted(_logger);

            // LOGIC: Read the stream and parse SSE events.
            using var stream = await httpResponse.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            // LOGIC: Use the existing SseParser to parse the stream.
            await foreach (var data in SseParser.ParseStreamAsync(stream, cancellationToken).ConfigureAwait(false))
            {
                // LOGIC: Parse each chunk into a StreamingChatToken.
                var token = OpenAIResponseParser.ParseStreamingChunk(data);

                if (token == null)
                {
                    // LOGIC: Empty delta or role-only - skip this chunk.
                    continue;
                }

                LLMLogEvents.OpenAIStreamChunkReceived(_logger, token.Token?.Length ?? 0);

                if (token.IsComplete)
                {
                    LLMLogEvents.OpenAIStreamCompleted(_logger, token.FinishReason);
                }

                yield return token;
            }
        }
        finally
        {
            // LOGIC: Ensure the response is disposed even if enumeration is cancelled.
            httpResponse?.Dispose();
        }
    }

    /// <summary>
    /// Retrieves the API key from the secure vault.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API key string.</returns>
    /// <exception cref="ProviderNotConfiguredException">
    /// Thrown when the API key is not found in the secure vault.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>Security:</b> The API key value is never logged. Only the action
    /// of retrieving (or failing to retrieve) the key is logged.
    /// </para>
    /// </remarks>
    private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
    {
        LLMLogEvents.OpenAIRetrievingApiKey(_logger);

        try
        {
            // LOGIC: Check if the secret exists first to provide a clear error message.
            var exists = await _vault.SecretExistsAsync(OpenAIOptions.VaultKey, cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                LLMLogEvents.OpenAIApiKeyNotFound(_logger);
                throw new ProviderNotConfiguredException(ProviderName);
            }

            // LOGIC: Retrieve the actual secret value.
            var apiKey = await _vault.GetSecretAsync(OpenAIOptions.VaultKey, cancellationToken)
                .ConfigureAwait(false);

            return apiKey;
        }
        catch (ProviderNotConfiguredException)
        {
            // LOGIC: Re-throw ProviderNotConfiguredException without wrapping.
            throw;
        }
        catch (Exception ex)
        {
            // LOGIC: Wrap other vault exceptions in ProviderNotConfiguredException.
            LLMLogEvents.OpenAIApiKeyNotFound(_logger);
            throw new ProviderNotConfiguredException(
                $"Failed to retrieve API key for {ProviderName}: {ex.Message}",
                ProviderName);
        }
    }

    /// <summary>
    /// Builds an HTTP request message for the OpenAI Chat Completions API.
    /// </summary>
    /// <param name="request">The chat request to send.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="stream">Whether to enable streaming mode.</param>
    /// <returns>An <see cref="HttpRequestMessage"/> ready to be sent.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the existing <see cref="OpenAIParameterMapper"/> to build
    /// the JSON request body, ensuring consistent parameter mapping.
    /// </para>
    /// </remarks>
    private HttpRequestMessage BuildHttpRequest(ChatRequest request, string apiKey, bool stream)
    {
        // LOGIC: Use the existing parameter mapper to build the request body.
        var body = OpenAIParameterMapper.ToRequestBody(request);

        // LOGIC: Add streaming flag if requested.
        if (stream)
        {
            OpenAIParameterMapper.WithStreaming(body, enableStreaming: true);
        }

        var json = body.ToJsonString();
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.CompletionsEndpoint)
        {
            Content = content
        };

        // LOGIC: Set authorization header with Bearer token.
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // LOGIC: Set Accept header for JSON response.
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return httpRequest;
    }
}
