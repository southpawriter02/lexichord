// -----------------------------------------------------------------------
// <copyright file="StreamingOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Configuration options for streaming chat completion operations.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record encapsulates configuration for streaming behavior,
/// including buffering, timeouts, and backpressure handling.
/// </para>
/// <para>
/// Use the <see cref="Default"/> property for sensible defaults in most scenarios.
/// </para>
/// </remarks>
/// <param name="BufferSize">
/// The maximum number of tokens to buffer before applying backpressure.
/// Default is 100 tokens.
/// </param>
/// <param name="TokenTimeout">
/// The maximum time to wait for the next token before timing out.
/// Default is <see cref="TimeSpan.Zero"/> (no timeout).
/// </param>
/// <param name="EnableBackpressure">
/// Whether to enable backpressure when the consumer falls behind.
/// Default is true.
/// </param>
/// <example>
/// <code>
/// // Use defaults
/// var options = StreamingOptions.Default;
///
/// // Custom configuration
/// var options = new StreamingOptions(
///     BufferSize: 50,
///     TokenTimeout: TimeSpan.FromSeconds(30),
///     EnableBackpressure: true
/// );
///
/// // Using with SSE parser
/// await foreach (var data in SseParser.ParseStreamAsync(stream, options))
/// {
///     // Process data
/// }
/// </code>
/// </example>
public record StreamingOptions(
    int BufferSize = 100,
    TimeSpan TokenTimeout = default,
    bool EnableBackpressure = true)
{
    /// <summary>
    /// Gets the default streaming options.
    /// </summary>
    /// <value>A <see cref="StreamingOptions"/> instance with default values.</value>
    /// <remarks>
    /// Default values:
    /// <list type="bullet">
    ///   <item><description>BufferSize: 100</description></item>
    ///   <item><description>TokenTimeout: Zero (no timeout)</description></item>
    ///   <item><description>EnableBackpressure: true</description></item>
    /// </list>
    /// </remarks>
    public static StreamingOptions Default => new();

    /// <summary>
    /// Gets options configured for low-latency streaming.
    /// </summary>
    /// <value>A <see cref="StreamingOptions"/> instance with smaller buffer.</value>
    /// <remarks>
    /// Use this configuration when low latency is more important than throughput.
    /// The smaller buffer (25 tokens) reduces memory usage and improves responsiveness.
    /// </remarks>
    public static StreamingOptions LowLatency => new(BufferSize: 25);

    /// <summary>
    /// Gets options configured for high-throughput streaming.
    /// </summary>
    /// <value>A <see cref="StreamingOptions"/> instance with larger buffer.</value>
    /// <remarks>
    /// Use this configuration when throughput is more important than latency.
    /// The larger buffer (500 tokens) reduces backpressure frequency.
    /// </remarks>
    public static StreamingOptions HighThroughput => new(BufferSize: 500);

    /// <summary>
    /// Gets the buffer size in tokens.
    /// </summary>
    /// <value>A positive integer representing the buffer size.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown during construction if value is less than 1.
    /// </exception>
    public int BufferSize { get; init; } = BufferSize >= 1
        ? BufferSize
        : throw new ArgumentOutOfRangeException(nameof(BufferSize), "BufferSize must be at least 1.");

    /// <summary>
    /// Creates a new instance with the specified buffer size.
    /// </summary>
    /// <param name="size">The buffer size in tokens.</param>
    /// <returns>A new <see cref="StreamingOptions"/> with the specified buffer size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="size"/> is less than 1.
    /// </exception>
    public StreamingOptions WithBufferSize(int size) => this with { BufferSize = size };

    /// <summary>
    /// Creates a new instance with the specified token timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>A new <see cref="StreamingOptions"/> with the specified timeout.</returns>
    public StreamingOptions WithTokenTimeout(TimeSpan timeout)
        => this with { TokenTimeout = timeout };

    /// <summary>
    /// Creates a new instance with backpressure enabled or disabled.
    /// </summary>
    /// <param name="enabled">Whether to enable backpressure.</param>
    /// <returns>A new <see cref="StreamingOptions"/> with the specified backpressure setting.</returns>
    public StreamingOptions WithBackpressure(bool enabled)
        => this with { EnableBackpressure = enabled };
}
