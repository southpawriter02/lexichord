// -----------------------------------------------------------------------
// <copyright file="AnthropicChatCompletionService.cs" company="Lexichord">
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

namespace Lexichord.Modules.LLM.Providers.Anthropic;

/// <summary>
/// Anthropic Claude implementation of the <see cref="IChatCompletionService"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This service provides integration with the Anthropic Messages API, supporting
/// both synchronous completion and streaming responses via Server-Sent Events (SSE).
/// </para>
/// <para>
/// <b>Authentication:</b> The API key is retrieved from <see cref="ISecureVault"/>
/// using the key pattern <c>anthropic:api-key</c>. Unlike OpenAI, Anthropic uses
/// the <c>x-api-key</c> header instead of Bearer token authentication.
/// </para>
/// <para>
/// <b>Supported Models:</b>
/// </para>
/// <list type="bullet">
///   <item><description>claude-3-5-sonnet-20241022 - Most capable Sonnet model</description></item>
///   <item><description>claude-3-opus-20240229 - Most capable model overall</description></item>
///   <item><description>claude-3-sonnet-20240229 - Balanced performance</description></item>
///   <item><description>claude-3-haiku-20240307 - Fast and cost-effective (default)</description></item>
/// </list>
/// <para>
/// <b>API Differences from OpenAI:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Authentication via <c>x-api-key</c> header (not Bearer token)</description></item>
///   <item><description>Requires <c>anthropic-version</c> header</description></item>
///   <item><description>System messages sent via separate "system" field</description></item>
///   <item><description>Response content is array of typed blocks</description></item>
///   <item><description>Different streaming event types</description></item>
/// </list>
/// <para>
/// <b>Error Handling:</b> The service maps Anthropic API errors to the Lexichord
/// exception hierarchy:
/// </para>
/// <list type="bullet">
///   <item><description>401 → <see cref="AuthenticationException"/></description></item>
///   <item><description>rate_limit_error → <see cref="RateLimitException"/></description></item>
///   <item><description>overloaded_error → <see cref="ChatCompletionException"/></description></item>
///   <item><description>5xx → <see cref="ChatCompletionException"/></description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Using with dependency injection
/// var service = serviceProvider.GetRequiredService&lt;AnthropicChatCompletionService&gt;();
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
public class AnthropicChatCompletionService : IChatCompletionService
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
    /// The Anthropic provider configuration options.
    /// </summary>
    private readonly AnthropicOptions _options;

    /// <summary>
    /// The logger instance for this service.
    /// </summary>
    private readonly ILogger<AnthropicChatCompletionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicChatCompletionService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating clients.</param>
    /// <param name="vault">The secure vault for API key storage.</param>
    /// <param name="options">The Anthropic configuration options.</param>
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
    public AnthropicChatCompletionService(
        IHttpClientFactory httpClientFactory,
        ISecureVault vault,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicChatCompletionService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <value>Returns "Anthropic".</value>
    public string ProviderName => "Anthropic";

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
        LLMLogEvents.AnthropicCompletionStarting(_logger, model);

        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Retrieve API key from secure vault.
        var apiKey = await GetApiKeyAsync(cancellationToken).ConfigureAwait(false);

        // LOGIC: Create HTTP client with configured timeout and resilience policies.
        var httpClient = _httpClientFactory.CreateClient(AnthropicOptions.HttpClientName);

        // LOGIC: Build the HTTP request using the existing parameter mapper.
        using var httpRequest = BuildHttpRequest(request, apiKey, stream: false);
        LLMLogEvents.AnthropicBuildingRequest(_logger, _options.MessagesEndpoint);

        try
        {
            // LOGIC: Send the request and get the response.
            using var httpResponse = await httpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            var responseBody = await httpResponse.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            LLMLogEvents.AnthropicRawResponse(_logger, responseBody.Length);

            // LOGIC: Handle error responses by mapping to appropriate exceptions.
            if (!httpResponse.IsSuccessStatusCode)
            {
                LLMLogEvents.AnthropicApiError(_logger, (int)httpResponse.StatusCode, null);
                throw AnthropicResponseParser.ParseErrorResponse(
                    httpResponse.StatusCode,
                    responseBody);
            }

            // LOGIC: Parse successful response.
            LLMLogEvents.AnthropicParsingSuccessResponse(_logger);
            var response = AnthropicResponseParser.ParseSuccessResponse(responseBody, stopwatch.Elapsed);

            LLMLogEvents.AnthropicCompletionSucceeded(
                _logger,
                stopwatch.ElapsedMilliseconds,
                response.PromptTokens,
                response.CompletionTokens);

            return response;
        }
        catch (HttpRequestException ex)
        {
            LLMLogEvents.AnthropicHttpRequestFailed(_logger, ex, ex.Message);
            throw new ChatCompletionException(
                $"HTTP request to Anthropic failed: {ex.Message}",
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
        LLMLogEvents.AnthropicStreamingStarting(_logger, model);

        // LOGIC: Retrieve API key from secure vault.
        var apiKey = await GetApiKeyAsync(cancellationToken).ConfigureAwait(false);

        // LOGIC: Create HTTP client with configured timeout and resilience policies.
        var httpClient = _httpClientFactory.CreateClient(AnthropicOptions.HttpClientName);

        // LOGIC: Build the HTTP request with streaming enabled.
        using var httpRequest = BuildHttpRequest(request, apiKey, stream: true);
        LLMLogEvents.AnthropicBuildingRequest(_logger, _options.MessagesEndpoint);

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
                LLMLogEvents.AnthropicApiError(_logger, (int)httpResponse.StatusCode, null);
                throw AnthropicResponseParser.ParseErrorResponse(
                    httpResponse.StatusCode,
                    errorBody);
            }

            LLMLogEvents.AnthropicStreamStarted(_logger);

            // LOGIC: Read the stream and parse SSE events.
            // Anthropic SSE format includes event types, unlike OpenAI's data-only format.
            using var stream = await httpResponse.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            using var reader = new StreamReader(stream);

            string? currentEventType = null;

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                // LOGIC: Skip empty lines (SSE uses empty lines as event delimiters).
                if (string.IsNullOrWhiteSpace(line))
                {
                    currentEventType = null;
                    continue;
                }

                // LOGIC: Parse event type line.
                if (line.StartsWith("event: ", StringComparison.Ordinal))
                {
                    currentEventType = line.Substring(7);
                    continue;
                }

                // LOGIC: Parse data line and process with event type context.
                if (line.StartsWith("data: ", StringComparison.Ordinal) && currentEventType != null)
                {
                    var data = line.Substring(6);

                    // LOGIC: Parse the streaming event using the response parser.
                    StreamingChatToken? token;
                    try
                    {
                        token = AnthropicResponseParser.ParseStreamingEvent(currentEventType, data);
                    }
                    catch (ChatCompletionException)
                    {
                        // LOGIC: Stream error from API - re-throw
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // LOGIC: Parsing error - log and skip this chunk
                        LLMLogEvents.AnthropicStreamChunkParseFailed(_logger, ex.Message);
                        continue;
                    }

                    if (token == null)
                    {
                        // LOGIC: Non-content event (metadata, keep-alive, etc.) - skip
                        continue;
                    }

                    LLMLogEvents.AnthropicStreamChunkReceived(_logger, token.Token?.Length ?? 0);

                    if (token.IsComplete)
                    {
                        LLMLogEvents.AnthropicStreamCompleted(_logger, token.FinishReason);
                    }

                    yield return token;

                    // LOGIC: Stop iteration after complete token
                    if (token.IsComplete)
                    {
                        yield break;
                    }
                }
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
        LLMLogEvents.AnthropicRetrievingApiKey(_logger);

        try
        {
            // LOGIC: Check if the secret exists first to provide a clear error message.
            var exists = await _vault.SecretExistsAsync(AnthropicOptions.VaultKey, cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                LLMLogEvents.AnthropicApiKeyNotFound(_logger);
                throw new ProviderNotConfiguredException(ProviderName);
            }

            // LOGIC: Retrieve the actual secret value.
            var apiKey = await _vault.GetSecretAsync(AnthropicOptions.VaultKey, cancellationToken)
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
            LLMLogEvents.AnthropicApiKeyNotFound(_logger);
            throw new ProviderNotConfiguredException(
                $"Failed to retrieve API key for {ProviderName}: {ex.Message}",
                ProviderName);
        }
    }

    /// <summary>
    /// Builds an HTTP request message for the Anthropic Messages API.
    /// </summary>
    /// <param name="request">The chat request to send.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="stream">Whether to enable streaming mode.</param>
    /// <returns>An <see cref="HttpRequestMessage"/> ready to be sent.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the existing <see cref="AnthropicParameterMapper"/> to build
    /// the JSON request body, ensuring consistent parameter mapping.
    /// </para>
    /// <para>
    /// <b>Anthropic-specific headers:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>x-api-key</c>: API key (NOT Bearer token)</description></item>
    ///   <item><description><c>anthropic-version</c>: API version (e.g., "2024-01-01")</description></item>
    /// </list>
    /// </remarks>
    private HttpRequestMessage BuildHttpRequest(ChatRequest request, string apiKey, bool stream)
    {
        // LOGIC: Use the existing parameter mapper to build the request body.
        var body = AnthropicParameterMapper.ToRequestBody(request);

        // LOGIC: Add streaming flag if requested.
        if (stream)
        {
            AnthropicParameterMapper.WithStreaming(body, enableStreaming: true);
        }

        var json = body.ToJsonString();
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.MessagesEndpoint)
        {
            Content = content
        };

        // LOGIC: Set Anthropic-specific headers.
        // Note: Anthropic uses x-api-key header, NOT Bearer authorization.
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", _options.ApiVersion);

        // LOGIC: Set Accept header for JSON response.
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return httpRequest;
    }
}
