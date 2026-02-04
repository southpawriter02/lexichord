// -----------------------------------------------------------------------
// <copyright file="AnthropicResponseParser.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.LLM.Providers.Anthropic;

/// <summary>
/// Parses responses from the Anthropic Messages API.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides methods to parse both successful responses and error
/// responses from the Anthropic API, converting them to the appropriate Lexichord types.
/// </para>
/// <para>
/// <b>Response Format (Success):</b>
/// </para>
/// <code>
/// {
///   "id": "msg_abc123",
///   "type": "message",
///   "role": "assistant",
///   "content": [{"type": "text", "text": "Hello!"}],
///   "model": "claude-3-haiku-20240307",
///   "stop_reason": "end_turn",
///   "usage": {"input_tokens": 15, "output_tokens": 10}
/// }
/// </code>
/// <para>
/// <b>Response Format (Error):</b>
/// </para>
/// <code>
/// {
///   "error": {
///     "type": "invalid_request_error",
///     "message": "..."
///   }
/// }
/// </code>
/// <para>
/// <b>Streaming Event Types:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>message_start</c>: Initial message metadata</description></item>
///   <item><description><c>content_block_start</c>: Content block metadata</description></item>
///   <item><description><c>content_block_delta</c>: Incremental text content</description></item>
///   <item><description><c>content_block_stop</c>: Content block end</description></item>
///   <item><description><c>message_delta</c>: Stop reason and final usage</description></item>
///   <item><description><c>message_stop</c>: End of message stream</description></item>
///   <item><description><c>ping</c>: Keep-alive ping</description></item>
///   <item><description><c>error</c>: Stream error</description></item>
/// </list>
/// </remarks>
internal static class AnthropicResponseParser
{
    /// <summary>
    /// The provider name used in exceptions.
    /// </summary>
    private const string ProviderName = "Anthropic";

    /// <summary>
    /// Parses a successful response from the Anthropic Messages API.
    /// </summary>
    /// <param name="responseBody">The JSON response body string.</param>
    /// <param name="duration">The duration of the request.</param>
    /// <returns>A <see cref="ChatResponse"/> containing the parsed completion data.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the response body contains invalid JSON or is missing required fields.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method extracts the assistant's response content from the content blocks array,
    /// token usage statistics, and stop reason from the Anthropic response format.
    /// </para>
    /// <para>
    /// Anthropic returns content as an array of blocks. This method concatenates all
    /// text blocks to form the complete response content.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var response = AnthropicResponseParser.ParseSuccessResponse(jsonBody, stopwatch.Elapsed);
    /// Console.WriteLine($"Response: {response.Content}");
    /// Console.WriteLine($"Tokens used: {response.TotalTokens}");
    /// </code>
    /// </example>
    public static ChatResponse ParseSuccessResponse(string responseBody, TimeSpan duration)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // LOGIC: Extract content from the content blocks array.
        // Anthropic returns content as an array of typed blocks.
        var contentBlocks = root.GetProperty("content");
        var content = new StringBuilder();

        foreach (var block in contentBlocks.EnumerateArray())
        {
            // LOGIC: Only process text blocks (other types like "tool_use" are ignored).
            if (block.TryGetProperty("type", out var typeElement) &&
                typeElement.GetString() == "text" &&
                block.TryGetProperty("text", out var textElement))
            {
                content.Append(textElement.GetString());
            }
        }

        // LOGIC: Extract stop reason (Anthropic uses "stop_reason" instead of "finish_reason").
        var stopReason = root.TryGetProperty("stop_reason", out var stopReasonElement)
            ? stopReasonElement.GetString()
            : null;

        // LOGIC: Extract token usage from the usage object.
        // Anthropic uses "input_tokens" and "output_tokens" instead of
        // "prompt_tokens" and "completion_tokens".
        var inputTokens = 0;
        var outputTokens = 0;

        if (root.TryGetProperty("usage", out var usage))
        {
            inputTokens = usage.TryGetProperty("input_tokens", out var inputProp)
                ? inputProp.GetInt32()
                : 0;
            outputTokens = usage.TryGetProperty("output_tokens", out var outputProp)
                ? outputProp.GetInt32()
                : 0;
        }

        return new ChatResponse(
            Content: content.ToString(),
            PromptTokens: inputTokens,
            CompletionTokens: outputTokens,
            Duration: duration,
            FinishReason: stopReason);
    }

    /// <summary>
    /// Parses a streaming event from the Anthropic Messages API.
    /// </summary>
    /// <param name="eventType">The SSE event type (e.g., "content_block_delta").</param>
    /// <param name="data">The JSON data payload from the SSE event.</param>
    /// <returns>
    /// A <see cref="StreamingChatToken"/> if the event contains content or a finish reason;
    /// otherwise, <c>null</c> if the event should be skipped (e.g., metadata events).
    /// </returns>
    /// <exception cref="ChatCompletionException">
    /// Thrown when the event type is "error" containing stream error details.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Anthropic streaming uses different event types than OpenAI:
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Event Type</term>
    ///     <description>Handling</description>
    ///   </listheader>
    ///   <item>
    ///     <term>message_start</term>
    ///     <description>Ignored (metadata only)</description>
    ///   </item>
    ///   <item>
    ///     <term>content_block_start</term>
    ///     <description>Ignored (block metadata)</description>
    ///   </item>
    ///   <item>
    ///     <term>content_block_delta</term>
    ///     <description>Yields token content</description>
    ///   </item>
    ///   <item>
    ///     <term>content_block_stop</term>
    ///     <description>Ignored (block end marker)</description>
    ///   </item>
    ///   <item>
    ///     <term>message_delta</term>
    ///     <description>Yields complete token with stop reason</description>
    ///   </item>
    ///   <item>
    ///     <term>message_stop</term>
    ///     <description>Yields complete token (stream end)</description>
    ///   </item>
    ///   <item>
    ///     <term>ping</term>
    ///     <description>Ignored (keep-alive)</description>
    ///   </item>
    ///   <item>
    ///     <term>error</term>
    ///     <description>Throws ChatCompletionException</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var token = AnthropicResponseParser.ParseStreamingEvent("content_block_delta", data);
    /// if (token != null)
    /// {
    ///     if (token.IsComplete)
    ///         Console.WriteLine($"Stream finished: {token.FinishReason}");
    ///     else
    ///         Console.Write(token.Token);
    /// }
    /// </code>
    /// </example>
    public static StreamingChatToken? ParseStreamingEvent(string eventType, string data)
    {
        try
        {
            return eventType switch
            {
                "content_block_delta" => ParseContentBlockDelta(data),
                "message_stop" => StreamingChatToken.Complete("end_turn"),
                "message_delta" => ParseMessageDelta(data),
                "error" => throw ParseStreamingError(data),
                "ping" => null,             // Keep-alive, ignore
                "message_start" => null,    // Metadata only, ignore
                "content_block_start" => null, // Block metadata, ignore
                "content_block_stop" => null,  // Block end marker, ignore
                _ => null                   // Unknown event types are ignored
            };
        }
        catch (JsonException)
        {
            // LOGIC: Malformed JSON - return null to skip this chunk.
            // Caller should log this as a warning.
            return null;
        }
    }

    /// <summary>
    /// Parses a content_block_delta event to extract the text content.
    /// </summary>
    /// <param name="data">The JSON data payload.</param>
    /// <returns>
    /// A <see cref="StreamingChatToken"/> with the text content, or <c>null</c> if empty.
    /// </returns>
    private static StreamingChatToken? ParseContentBlockDelta(string data)
    {
        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        // LOGIC: Extract text from the delta object.
        // Format: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hello"}}
        if (root.TryGetProperty("delta", out var delta))
        {
            // LOGIC: Check for text_delta type
            if (delta.TryGetProperty("type", out var typeElement) &&
                typeElement.GetString() == "text_delta" &&
                delta.TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    return StreamingChatToken.Content(text);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a message_delta event to extract the stop reason.
    /// </summary>
    /// <param name="data">The JSON data payload.</param>
    /// <returns>
    /// A <see cref="StreamingChatToken"/> with completion status, or <c>null</c> if no stop reason.
    /// </returns>
    private static StreamingChatToken? ParseMessageDelta(string data)
    {
        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        // LOGIC: Extract stop_reason from the delta object.
        // Format: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"output_tokens":10}}
        if (root.TryGetProperty("delta", out var delta) &&
            delta.TryGetProperty("stop_reason", out var stopReasonElement))
        {
            var stopReason = stopReasonElement.GetString();
            if (!string.IsNullOrEmpty(stopReason))
            {
                return StreamingChatToken.Complete(stopReason);
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a streaming error event and returns an appropriate exception.
    /// </summary>
    /// <param name="data">The JSON data payload containing error details.</param>
    /// <returns>A <see cref="ChatCompletionException"/> with the error details.</returns>
    private static ChatCompletionException ParseStreamingError(string data)
    {
        string? errorType = null;
        string? errorMessage = null;

        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                errorType = error.TryGetProperty("type", out var t) ? t.GetString() : null;
                errorMessage = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            }
        }
        catch (JsonException)
        {
            errorMessage = data;
        }

        return new ChatCompletionException(
            errorMessage ?? $"Stream error: {errorType ?? "unknown"}",
            ProviderName);
    }

    /// <summary>
    /// Parses an error response from the Anthropic API and returns the appropriate exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status code from the response.</param>
    /// <param name="responseBody">The error response body string.</param>
    /// <returns>
    /// A <see cref="ChatCompletionException"/> or derived exception appropriate
    /// for the error type.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method maps HTTP status codes and Anthropic error types to the appropriate
    /// exception types in the Lexichord exception hierarchy:
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Error Type / Status</term>
    ///     <description>Exception Type</description>
    ///   </listheader>
    ///   <item>
    ///     <term>401 Unauthorized</term>
    ///     <description><see cref="AuthenticationException"/></description>
    ///   </item>
    ///   <item>
    ///     <term>403 Forbidden</term>
    ///     <description><see cref="AuthenticationException"/> (permission denied)</description>
    ///   </item>
    ///   <item>
    ///     <term>rate_limit_error</term>
    ///     <description><see cref="RateLimitException"/></description>
    ///   </item>
    ///   <item>
    ///     <term>overloaded_error</term>
    ///     <description><see cref="ChatCompletionException"/> (service unavailable)</description>
    ///   </item>
    ///   <item>
    ///     <term>invalid_request_error</term>
    ///     <description><see cref="ChatCompletionException"/> (bad request)</description>
    ///   </item>
    ///   <item>
    ///     <term>5xx Server Error</term>
    ///     <description><see cref="ChatCompletionException"/> with server error message</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!httpResponse.IsSuccessStatusCode)
    /// {
    ///     var body = await httpResponse.Content.ReadAsStringAsync();
    ///     throw AnthropicResponseParser.ParseErrorResponse(httpResponse.StatusCode, body);
    /// }
    /// </code>
    /// </example>
    public static ChatCompletionException ParseErrorResponse(
        HttpStatusCode statusCode,
        string responseBody)
    {
        string? errorMessage = null;
        string? errorType = null;

        // LOGIC: Try to parse the error object from the response body.
        // Anthropic returns errors in a consistent JSON format.
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                errorMessage = error.TryGetProperty("message", out var msg)
                    ? msg.GetString()
                    : null;
                errorType = error.TryGetProperty("type", out var type)
                    ? type.GetString()
                    : null;
            }
        }
        catch (JsonException)
        {
            // LOGIC: If JSON parsing fails, use the raw response body as the message.
            // Only use non-empty bodies to avoid passing empty strings that bypass null-coalescing.
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                errorMessage = responseBody;
            }
        }

        // LOGIC: Map error types and status codes to exception types.
        // Anthropic error types take precedence over HTTP status codes.
        return (errorType, statusCode) switch
        {
            // Authentication errors
            (_, HttpStatusCode.Unauthorized) =>
                new AuthenticationException(
                    errorMessage ?? "Authentication failed. Please check your API key.",
                    ProviderName),

            (_, HttpStatusCode.Forbidden) =>
                new AuthenticationException(
                    errorMessage ?? "Access forbidden. Check your API key permissions.",
                    ProviderName),

            // Rate limit errors (can be identified by type or status)
            ("rate_limit_error", _) =>
                new RateLimitException(
                    errorMessage ?? "Rate limit exceeded. Please wait before retrying.",
                    ProviderName,
                    null), // Anthropic doesn't consistently provide Retry-After

            (_, HttpStatusCode.TooManyRequests) =>
                new RateLimitException(
                    errorMessage ?? "Rate limit exceeded. Please wait before retrying.",
                    ProviderName,
                    null),

            // Overload errors (Anthropic-specific)
            ("overloaded_error", _) =>
                new ChatCompletionException(
                    errorMessage ?? "Anthropic API is currently overloaded. Please retry later.",
                    ProviderName),

            // Invalid request errors
            ("invalid_request_error", _) =>
                new ChatCompletionException(
                    errorMessage ?? "Invalid request. Check your request parameters.",
                    ProviderName),

            // Model not found (404)
            (_, HttpStatusCode.NotFound) =>
                new ChatCompletionException(
                    errorMessage ?? "Model not found. Check the model name.",
                    ProviderName),

            // Server errors (5xx)
            (_, >= HttpStatusCode.InternalServerError) =>
                new ChatCompletionException(
                    errorMessage ?? $"Anthropic server error: HTTP {(int)statusCode}",
                    ProviderName),

            // Other errors: Return generic exception with details
            _ => new ChatCompletionException(
                errorMessage ?? $"Anthropic API error: HTTP {(int)statusCode} ({errorType ?? "unknown"})",
                ProviderName)
        };
    }

    /// <summary>
    /// Extracts token usage from an Anthropic API response for logging purposes.
    /// </summary>
    /// <param name="responseBody">The JSON response body string.</param>
    /// <returns>
    /// A tuple containing (inputTokens, outputTokens), or (0, 0) if parsing fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is useful for logging token usage without fully parsing the response.
    /// Anthropic uses "input_tokens" and "output_tokens" instead of
    /// "prompt_tokens" and "completion_tokens".
    /// </para>
    /// </remarks>
    public static (int InputTokens, int OutputTokens) ExtractTokenUsage(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                var inputTokens = usage.TryGetProperty("input_tokens", out var input)
                    ? input.GetInt32()
                    : 0;
                var outputTokens = usage.TryGetProperty("output_tokens", out var output)
                    ? output.GetInt32()
                    : 0;

                return (inputTokens, outputTokens);
            }
        }
        catch (JsonException)
        {
            // LOGIC: Return zeros if parsing fails.
        }

        return (0, 0);
    }
}
