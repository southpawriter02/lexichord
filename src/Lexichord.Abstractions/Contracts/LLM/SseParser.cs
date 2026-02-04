// -----------------------------------------------------------------------
// <copyright file="SseParser.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Parses Server-Sent Events (SSE) streams from LLM APIs.
/// </summary>
/// <remarks>
/// <para>
/// Server-Sent Events is the standard streaming protocol used by most LLM APIs
/// (OpenAI, Anthropic, etc.). This parser handles the SSE format and extracts
/// the data payloads for processing.
/// </para>
/// <para>
/// SSE format:
/// <code>
/// data: {"token": "Hello"}
/// data: {"token": " world"}
/// data: [DONE]
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await foreach (var data in SseParser.ParseStreamAsync(responseStream))
/// {
///     var token = JsonSerializer.Deserialize&lt;TokenPayload&gt;(data);
///     Console.Write(token.Text);
/// }
/// </code>
/// </example>
public static class SseParser
{
    /// <summary>
    /// The prefix for data lines in SSE format.
    /// </summary>
    public const string DataPrefix = "data: ";

    /// <summary>
    /// The marker indicating the stream has ended.
    /// </summary>
    public const string DoneMarker = "[DONE]";

    /// <summary>
    /// Parses an SSE stream into individual data payloads.
    /// </summary>
    /// <param name="stream">The stream to parse.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of data payload strings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// <para>
    /// This method reads the stream line by line, extracting data payloads from
    /// lines prefixed with "data: ". Empty lines and other SSE fields (event:, id:, retry:)
    /// are ignored.
    /// </para>
    /// <para>
    /// The stream terminates when the "[DONE]" marker is received, the stream ends,
    /// or the cancellation token is triggered.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var httpResponse = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    /// await using var stream = await httpResponse.Content.ReadAsStreamAsync();
    ///
    /// await foreach (var payload in SseParser.ParseStreamAsync(stream, cancellationToken))
    /// {
    ///     // Process each JSON payload
    ///     var token = JsonSerializer.Deserialize&lt;StreamToken&gt;(payload);
    ///     yield return new StreamingChatToken(token.Text);
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<string> ParseStreamAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);

            // Skip empty lines (SSE uses empty lines as event delimiters)
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            // Skip non-data lines (like event:, id:, retry:)
            if (!line.StartsWith(DataPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            // Extract the data payload
            var data = line[DataPrefix.Length..];

            // Check for the done marker
            if (string.Equals(data, DoneMarker, StringComparison.Ordinal))
            {
                yield break;
            }

            yield return data;
        }
    }

    /// <summary>
    /// Parses an SSE stream with buffering support for multi-line data.
    /// </summary>
    /// <param name="stream">The stream to parse.</param>
    /// <param name="options">Streaming options for buffer configuration.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of data payload strings.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream"/> or <paramref name="options"/> is null.
    /// </exception>
    /// <remarks>
    /// This overload accepts <see cref="StreamingOptions"/> for advanced configuration.
    /// Currently, the buffer size option is reserved for future use.
    /// </remarks>
    public static async IAsyncEnumerable<string> ParseStreamAsync(
        Stream stream,
        StreamingOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        // For now, delegate to the simple implementation
        // Future: implement buffering based on options.BufferSize
        await foreach (var data in ParseStreamAsync(stream, ct).ConfigureAwait(false))
        {
            yield return data;
        }
    }

    /// <summary>
    /// Checks if a line represents the end of an SSE stream.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line signals the end of the stream; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// if (SseParser.IsEndOfStream("data: [DONE]"))
    /// {
    ///     // Stream has ended
    /// }
    /// </code>
    /// </example>
    public static bool IsEndOfStream(string? line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return false;
        }

        if (!line.StartsWith(DataPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var data = line[DataPrefix.Length..];
        return string.Equals(data, DoneMarker, StringComparison.Ordinal);
    }

    /// <summary>
    /// Extracts the data payload from an SSE line.
    /// </summary>
    /// <param name="line">The SSE line to parse.</param>
    /// <returns>The data payload, or null if the line is not a data line.</returns>
    /// <example>
    /// <code>
    /// var payload = SseParser.ExtractData("data: {\"token\": \"Hello\"}");
    /// // payload = "{\"token\": \"Hello\"}"
    /// </code>
    /// </example>
    public static string? ExtractData(string? line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return null;
        }

        if (!line.StartsWith(DataPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        return line[DataPrefix.Length..];
    }
}
