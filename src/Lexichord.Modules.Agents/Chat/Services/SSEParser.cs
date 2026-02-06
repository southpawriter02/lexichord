// -----------------------------------------------------------------------
// <copyright file="SSEParser.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Lexichord.Modules.Agents.Chat.Abstractions;
using Lexichord.Modules.Agents.Chat.Models;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Services;

/// <summary>
/// Parses Server-Sent Events (SSE) streams from LLM providers into
/// <see cref="StreamingChatToken"/> sequences.
/// </summary>
/// <remarks>
/// <para>
/// This parser is stateless and thread-safe, designed for singleton DI registration.
/// It reads an HTTP response stream line-by-line, extracts JSON payloads from
/// <c>data:</c>-prefixed SSE lines, and parses provider-specific JSON to yield
/// individual tokens.
/// </para>
/// <para>
/// <strong>Supported Providers:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <strong>OpenAI</strong> — JSON format: <c>{"choices":[{"delta":{"content":"token"}}]}</c>.
///     Termination signal: <c>[DONE]</c>.
///   </description></item>
///   <item><description>
///     <strong>Anthropic</strong> — JSON format: <c>{"type":"content_block_delta","delta":{"text":"token"}}</c>.
///     Termination signal: <c>{"type":"message_stop"}</c>.
///   </description></item>
/// </list>
/// <para>
/// <strong>Error Handling:</strong> Malformed JSON is logged at Warning level and skipped.
/// The stream continues processing subsequent lines to maintain resilience.
/// </para>
/// <para>
/// <strong>Performance:</strong> Uses <see cref="StreamReader"/> for efficient line-by-line
/// reading, <see cref="ReadOnlySpan{T}"/> for prefix slicing, and <see cref="JsonDocument"/>
/// for allocation-efficient JSON parsing. Memory usage is bounded by processing one line
/// at a time.
/// </para>
/// </remarks>
/// <seealso cref="ISSEParser"/>
/// <seealso cref="StreamingChatToken"/>
public sealed class SSEParser : ISSEParser
{
    // ── Constants ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The SSE data line prefix. Lines not starting with this are ignored.
    /// </summary>
    private const string DataPrefix = "data: ";

    /// <summary>
    /// OpenAI stream termination marker.
    /// </summary>
    private const string OpenAIDoneMarker = "[DONE]";

    /// <summary>
    /// Anthropic event type indicating stream end.
    /// </summary>
    private const string AnthropicStopType = "message_stop";

    /// <summary>
    /// Anthropic event type containing token content.
    /// </summary>
    private const string AnthropicDeltaType = "content_block_delta";

    // ── Fields ─────────────────────────────────────────────────────────────────

    private readonly ILogger<SSEParser> _logger;

    // ── Constructor ────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="SSEParser"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SSEParser(ILogger<SSEParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The parser processes the stream in a single pass:
    /// </para>
    /// <list type="number">
    ///   <item><description>Read a line from the stream</description></item>
    ///   <item><description>Skip empty lines and non-data SSE lines</description></item>
    ///   <item><description>Check for provider-specific termination signals</description></item>
    ///   <item><description>Parse the JSON payload into a <see cref="StreamingChatToken"/></description></item>
    ///   <item><description>Yield the token and increment the index</description></item>
    /// </list>
    /// </remarks>
    public async IAsyncEnumerable<StreamingChatToken> ParseSSEStreamAsync(
        Stream responseStream,
        string provider,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // GUARD: Validate inputs.
        ArgumentNullException.ThrowIfNull(responseStream);

        if (!IsSupportedProvider(provider))
        {
            _logger.LogError("Unsupported SSE provider requested: {Provider}", provider);
            throw new NotSupportedException($"Provider '{provider}' is not supported. Supported providers: OpenAI, Anthropic.");
        }

        _logger.LogDebug("Starting SSE stream parse for provider: {Provider}", provider);

        // LOGIC: Read the stream line-by-line using StreamReader for efficient buffering.
        using var reader = new StreamReader(responseStream, Encoding.UTF8, leaveOpen: false);
        var index = 0;
        var startTime = Stopwatch.GetTimestamp();

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("SSE stream parse cancelled at token index {Index}", index);
                yield break;
            }

            // LOGIC: Skip empty lines — SSE uses blank lines as event separators.
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // LOGIC: Skip non-data lines (event:, id:, retry:, comments starting with ':').
            if (!line.StartsWith(DataPrefix, StringComparison.Ordinal))
            {
                _logger.LogTrace("Skipping non-data SSE line: {Line}", line);
                continue;
            }

            // LOGIC: Extract the JSON payload after "data: " prefix.
            var json = line.AsSpan()[DataPrefix.Length..];

            // LOGIC: Check for provider-specific stream termination signals.
            if (IsStreamTermination(json, provider))
            {
                _logger.LogDebug(
                    "Stream termination signal received from {Provider} at token index {Index}",
                    provider, index);
                yield return StreamingChatToken.Complete(index, "stop");
                yield break;
            }

            // LOGIC: Parse the JSON payload into a StreamingChatToken.
            var token = ParseToken(json.ToString(), provider, index);
            if (token is not null)
            {
                index++;
                yield return token;
            }
        }

        // LOGIC: Log stream completion with timing metrics.
        var elapsed = Stopwatch.GetElapsedTime(startTime);
        _logger.LogInformation(
            "SSE stream completed for {Provider}: {TokenCount} tokens parsed in {ElapsedMs:F1}ms",
            provider, index, elapsed.TotalMilliseconds);
    }

    // ── Provider Support ───────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether the specified provider is supported.
    /// </summary>
    /// <param name="provider">The provider name to check.</param>
    /// <returns><c>true</c> if the provider is supported; otherwise, <c>false</c>.</returns>
    private static bool IsSupportedProvider(string provider) =>
        string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase);

    // ── Termination Detection ──────────────────────────────────────────────────

    /// <summary>
    /// Checks whether the given SSE data payload represents a stream termination signal
    /// for the specified provider.
    /// </summary>
    /// <param name="json">The raw JSON payload (without the "data: " prefix).</param>
    /// <param name="provider">The LLM provider name.</param>
    /// <returns><c>true</c> if this payload signals end-of-stream; otherwise, <c>false</c>.</returns>
    private static bool IsStreamTermination(ReadOnlySpan<char> json, string provider)
    {
        // LOGIC: OpenAI sends a literal "[DONE]" marker (not valid JSON).
        if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return json.Trim().SequenceEqual(OpenAIDoneMarker.AsSpan());
        }

        // LOGIC: Anthropic sends a JSON object with "type": "message_stop".
        if (string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = json.Trim();

            // Quick check: must look like JSON object.
            if (trimmed.Length < 2 || trimmed[0] != '{')
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(trimmed.ToString());
                if (doc.RootElement.TryGetProperty("type", out var typeElement))
                {
                    return string.Equals(
                        typeElement.GetString(),
                        AnthropicStopType,
                        StringComparison.Ordinal);
                }
            }
            catch (JsonException)
            {
                // Not valid JSON — cannot be a termination signal.
                return false;
            }
        }

        return false;
    }

    // ── Token Parsing ──────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a JSON payload into a <see cref="StreamingChatToken"/> using
    /// the appropriate provider-specific strategy.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="provider">The LLM provider name.</param>
    /// <param name="index">The current token index in the stream.</param>
    /// <returns>
    /// A <see cref="StreamingChatToken"/> if content was extracted; <c>null</c> if the
    /// payload contained no displayable content or was malformed.
    /// </returns>
    private StreamingChatToken? ParseToken(string json, string provider, int index)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return ParseOpenAIToken(root, index);
            }

            if (string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
            {
                return ParseAnthropicToken(root, index);
            }
        }
        catch (JsonException ex)
        {
            // LOGIC: Malformed JSON is logged and skipped — the stream continues.
            _logger.LogWarning(
                ex,
                "Malformed JSON in SSE stream at token index {Index}: {Json}",
                index, json);
        }

        return null;
    }

    /// <summary>
    /// Extracts token content from an OpenAI streaming response JSON payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Expected format:
    /// <code>
    /// {"choices":[{"index":0,"delta":{"content":"token text"}}]}
    /// </code>
    /// </para>
    /// <para>
    /// Returns <c>null</c> when the <c>delta</c> object has no <c>content</c> property
    /// (e.g., the initial role-only delta or function call deltas).
    /// </para>
    /// </remarks>
    /// <param name="root">The parsed JSON root element.</param>
    /// <param name="index">The current token index.</param>
    /// <returns>A content token, or <c>null</c> if no content was present.</returns>
    private static StreamingChatToken? ParseOpenAIToken(JsonElement root, int index)
    {
        // LOGIC: Navigate choices[0].delta.content.
        if (!root.TryGetProperty("choices", out var choices))
        {
            return null;
        }

        if (choices.GetArrayLength() == 0)
        {
            return null;
        }

        var firstChoice = choices[0];

        if (!firstChoice.TryGetProperty("delta", out var delta))
        {
            return null;
        }

        if (!delta.TryGetProperty("content", out var content))
        {
            return null;
        }

        var text = content.GetString();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return StreamingChatToken.Content(text, index);
    }

    /// <summary>
    /// Extracts token content from an Anthropic streaming response JSON payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Expected format for content deltas:
    /// <code>
    /// {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"token text"}}
    /// </code>
    /// </para>
    /// <para>
    /// Non-delta event types (e.g., <c>message_start</c>, <c>content_block_start</c>,
    /// <c>ping</c>) are ignored by returning <c>null</c>.
    /// </para>
    /// </remarks>
    /// <param name="root">The parsed JSON root element.</param>
    /// <param name="index">The current token index.</param>
    /// <returns>A content token, or <c>null</c> if no content was present.</returns>
    private static StreamingChatToken? ParseAnthropicToken(JsonElement root, int index)
    {
        // LOGIC: Only process content_block_delta events.
        if (!root.TryGetProperty("type", out var typeElement))
        {
            return null;
        }

        if (!string.Equals(typeElement.GetString(), AnthropicDeltaType, StringComparison.Ordinal))
        {
            return null;
        }

        // LOGIC: Navigate delta.text.
        if (!root.TryGetProperty("delta", out var delta))
        {
            return null;
        }

        if (!delta.TryGetProperty("text", out var text))
        {
            return null;
        }

        var content = text.GetString();
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        return StreamingChatToken.Content(content, index);
    }
}
