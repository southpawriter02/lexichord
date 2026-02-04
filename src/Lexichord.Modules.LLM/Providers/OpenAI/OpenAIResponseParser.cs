// -----------------------------------------------------------------------
// <copyright file="OpenAIResponseParser.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.LLM.Providers.OpenAI;

/// <summary>
/// Parses responses from the OpenAI Chat Completions API.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides methods to parse both successful responses and error
/// responses from the OpenAI API, converting them to the appropriate Lexichord types.
/// </para>
/// <para>
/// <b>Response Format (Success):</b>
/// </para>
/// <code>
/// {
///   "id": "chatcmpl-abc123",
///   "choices": [{
///     "message": { "role": "assistant", "content": "..." },
///     "finish_reason": "stop"
///   }],
///   "usage": { "prompt_tokens": 13, "completion_tokens": 7 }
/// }
/// </code>
/// <para>
/// <b>Response Format (Error):</b>
/// </para>
/// <code>
/// {
///   "error": {
///     "message": "...",
///     "type": "invalid_request_error",
///     "code": "..."
///   }
/// }
/// </code>
/// </remarks>
internal static class OpenAIResponseParser
{
    /// <summary>
    /// The provider name used in exceptions.
    /// </summary>
    private const string ProviderName = "OpenAI";

    /// <summary>
    /// Parses a successful response from the OpenAI Chat Completions API.
    /// </summary>
    /// <param name="responseBody">The JSON response body string.</param>
    /// <param name="duration">The duration of the request.</param>
    /// <returns>A <see cref="ChatResponse"/> containing the parsed completion data.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the response body contains invalid JSON or is missing required fields.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method extracts the assistant's response content, token usage statistics,
    /// and finish reason from the OpenAI response format.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var response = OpenAIResponseParser.ParseSuccessResponse(jsonBody, stopwatch.Elapsed);
    /// Console.WriteLine($"Response: {response.Content}");
    /// Console.WriteLine($"Tokens used: {response.TotalTokens}");
    /// </code>
    /// </example>
    public static ChatResponse ParseSuccessResponse(string responseBody, TimeSpan duration)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // LOGIC: Extract the first choice's message content.
        // OpenAI returns an array of choices, but we only support single completions.
        var content = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        // LOGIC: Extract the finish reason for the completion.
        // Common values: "stop", "length", "content_filter", "tool_calls"
        var finishReason = root
            .GetProperty("choices")[0]
            .GetProperty("finish_reason")
            .GetString();

        // LOGIC: Extract token usage from the usage object.
        var usage = root.GetProperty("usage");
        var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
        var completionTokens = usage.GetProperty("completion_tokens").GetInt32();

        return new ChatResponse(
            Content: content,
            PromptTokens: promptTokens,
            CompletionTokens: completionTokens,
            Duration: duration,
            FinishReason: finishReason);
    }

    /// <summary>
    /// Parses a streaming chunk from the OpenAI Chat Completions API.
    /// </summary>
    /// <param name="data">The JSON data payload from an SSE event (without the "data: " prefix).</param>
    /// <returns>
    /// A <see cref="StreamingChatToken"/> if the chunk contains content or a finish reason;
    /// otherwise, <c>null</c> if the chunk should be skipped (e.g., empty delta).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Streaming chunks have a different format than complete responses:
    /// </para>
    /// <code>
    /// {
    ///   "choices": [{
    ///     "index": 0,
    ///     "delta": { "content": "Hello" },
    ///     "finish_reason": null
    ///   }]
    /// }
    /// </code>
    /// <para>
    /// The method handles three cases:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Content chunk: Returns token with content</description></item>
    ///   <item><description>Finish chunk: Returns complete token with finish reason</description></item>
    ///   <item><description>Empty delta: Returns null (skip this chunk)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var token = OpenAIResponseParser.ParseStreamingChunk(data);
    /// if (token != null)
    /// {
    ///     if (token.IsComplete)
    ///         Console.WriteLine($"Stream finished: {token.FinishReason}");
    ///     else
    ///         Console.Write(token.Token);
    /// }
    /// </code>
    /// </example>
    public static StreamingChatToken? ParseStreamingChunk(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            var choices = root.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                return null;
            }

            var choice = choices[0];

            // LOGIC: Check for finish reason first.
            // When finish_reason is present, the stream is completing.
            if (choice.TryGetProperty("finish_reason", out var finishReasonElement) &&
                finishReasonElement.ValueKind != JsonValueKind.Null)
            {
                var finishReason = finishReasonElement.GetString();
                return StreamingChatToken.Complete(finishReason ?? "stop");
            }

            // LOGIC: Extract content from the delta object.
            // Delta may be empty, contain role, or contain content.
            if (choice.TryGetProperty("delta", out var delta))
            {
                if (delta.TryGetProperty("content", out var contentProp))
                {
                    var content = contentProp.GetString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        return StreamingChatToken.Content(content);
                    }
                }
            }

            // LOGIC: Empty delta or role-only delta - skip this chunk.
            return null;
        }
        catch (JsonException)
        {
            // LOGIC: Malformed JSON - skip this chunk.
            // Caller should log this as a warning.
            return null;
        }
    }

    /// <summary>
    /// Parses an error response from the OpenAI API and returns the appropriate exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status code from the response.</param>
    /// <param name="responseBody">The error response body string.</param>
    /// <param name="retryAfterHeader">
    /// The value of the Retry-After header, if present. Used for rate limit exceptions.
    /// </param>
    /// <returns>
    /// A <see cref="ChatCompletionException"/> or derived exception appropriate
    /// for the error type.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method maps HTTP status codes and OpenAI error types to the appropriate
    /// exception types in the Lexichord exception hierarchy:
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Status Code</term>
    ///     <description>Exception Type</description>
    ///   </listheader>
    ///   <item>
    ///     <term>401 Unauthorized</term>
    ///     <description><see cref="AuthenticationException"/></description>
    ///   </item>
    ///   <item>
    ///     <term>429 Too Many Requests</term>
    ///     <description><see cref="RateLimitException"/> with RetryAfter</description>
    ///   </item>
    ///   <item>
    ///     <term>5xx Server Error</term>
    ///     <description><see cref="ChatCompletionException"/> with server error message</description>
    ///   </item>
    ///   <item>
    ///     <term>Other</term>
    ///     <description><see cref="ChatCompletionException"/> with error details</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!httpResponse.IsSuccessStatusCode)
    /// {
    ///     var body = await httpResponse.Content.ReadAsStringAsync();
    ///     var retryAfter = httpResponse.Headers.RetryAfter?.Delta;
    ///     throw OpenAIResponseParser.ParseErrorResponse(httpResponse.StatusCode, body, retryAfter);
    /// }
    /// </code>
    /// </example>
    public static ChatCompletionException ParseErrorResponse(
        HttpStatusCode statusCode,
        string responseBody,
        TimeSpan? retryAfterHeader = null)
    {
        string? errorMessage = null;
        string? errorType = null;

        // LOGIC: Try to parse the error object from the response body.
        // OpenAI returns errors in a consistent JSON format.
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
            errorMessage = responseBody;
        }

        // LOGIC: Map HTTP status codes to exception types.
        return statusCode switch
        {
            // 401: Invalid API key or authentication failure
            HttpStatusCode.Unauthorized =>
                new AuthenticationException(
                    errorMessage ?? "Authentication failed. Please check your API key.",
                    ProviderName),

            // 429: Rate limit exceeded
            HttpStatusCode.TooManyRequests =>
                new RateLimitException(
                    errorMessage ?? "Rate limit exceeded. Please wait before retrying.",
                    ProviderName,
                    retryAfterHeader ?? ParseRetryAfterFromBody(responseBody)),

            // 5xx: Server errors
            >= HttpStatusCode.InternalServerError =>
                new ChatCompletionException(
                    errorMessage ?? $"OpenAI server error: HTTP {(int)statusCode}",
                    ProviderName),

            // Other errors: Return generic exception with details
            _ => new ChatCompletionException(
                errorMessage ?? $"OpenAI API error: HTTP {(int)statusCode} ({errorType ?? "unknown"})",
                ProviderName)
        };
    }

    /// <summary>
    /// Attempts to parse a retry-after duration from the error response body.
    /// </summary>
    /// <param name="responseBody">The error response body.</param>
    /// <returns>
    /// The retry-after duration if found in the response; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Some OpenAI rate limit responses include retry timing in the error message.
    /// This method attempts to extract that timing for the <see cref="RateLimitException"/>.
    /// </para>
    /// </remarks>
    private static TimeSpan? ParseRetryAfterFromBody(string responseBody)
    {
        // LOGIC: OpenAI sometimes includes retry info in the message.
        // This is a best-effort attempt; the header should be preferred.
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var message = error.TryGetProperty("message", out var msg)
                    ? msg.GetString()
                    : null;

                // LOGIC: Look for common patterns like "Please retry after X seconds"
                // or "Please try again in Xs"
                if (message != null && message.Contains("Please retry after", StringComparison.OrdinalIgnoreCase))
                {
                    // Simple extraction - look for a number followed by "seconds"
                    var parts = message.Split(' ');
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        if (parts[i + 1].StartsWith("second", StringComparison.OrdinalIgnoreCase) &&
                            double.TryParse(parts[i], out var seconds))
                        {
                            return TimeSpan.FromSeconds(seconds);
                        }
                    }
                }
            }
        }
        catch
        {
            // LOGIC: If parsing fails, return null - header value will be used.
        }

        return null;
    }

    /// <summary>
    /// Extracts token usage from an OpenAI response for logging purposes.
    /// </summary>
    /// <param name="responseBody">The JSON response body string.</param>
    /// <returns>
    /// A tuple containing (promptTokens, completionTokens), or (0, 0) if parsing fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is useful for logging token usage without fully parsing the response.
    /// </para>
    /// </remarks>
    public static (int PromptTokens, int CompletionTokens) ExtractTokenUsage(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                var promptTokens = usage.TryGetProperty("prompt_tokens", out var pt)
                    ? pt.GetInt32()
                    : 0;
                var completionTokens = usage.TryGetProperty("completion_tokens", out var ct)
                    ? ct.GetInt32()
                    : 0;

                return (promptTokens, completionTokens);
            }
        }
        catch (JsonException)
        {
            // LOGIC: Return zeros if parsing fails.
        }

        return (0, 0);
    }
}
