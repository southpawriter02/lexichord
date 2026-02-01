using System;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Exception thrown when an embedding operation fails after exhausting retries.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EmbeddingException"/> is the primary exception for embedding service errors.
/// It encapsulates HTTP status codes, retry attempts, and transient failure classification
/// to enable sophisticated error handling strategies.
/// </para>
/// <para>
/// <b>Exception Hierarchy:</b>
/// <list type="bullet">
///   <item><see cref="EmbeddingException"/> - Base type for all embedding errors</item>
/// </list>
/// </para>
/// <para>
/// <b>Handling Pattern - Transient vs. Permanent Failures:</b>
/// <code>
/// try
/// {
///     var embedding = await embeddingService.EmbedAsync(text);
/// }
/// catch (EmbeddingException ex)
/// {
///     if (ex.IsTransient)
///     {
///         // Transient failure (timeout, rate limit, temporary error)
///         // Application should retry with backoff
///         await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, ex.RetryCount)));
///         // ... retry logic ...
///     }
///     else
///     {
///         // Permanent failure (invalid API key, bad model, authentication error)
///         // Application should not retry, but notify user or fallback
///         Logger.LogError("Permanent embedding failure: {Message}", ex.Message);
///         // ... fallback or error handling ...
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Status Code Meanings:</b>
/// <list type="bullet">
///   <item>4xx errors: Typically permanent (invalid request, authentication, resource not found).</item>
///   <item>5xx errors: Typically transient (server error, overloaded, temporarily unavailable).</item>
///   <item>408: Request timeout - always transient.</item>
///   <item>429: Rate limit - transient, indicates backoff needed.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.4a as part of the Embedding Abstractions layer.
/// </para>
/// </remarks>
public class EmbeddingException : Exception
{
    /// <summary>
    /// Initializes a new instance with a message only.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <remarks>
    /// LOGIC: Minimal constructor for simple error cases. Uses defaults:
    /// StatusCode = 0, RetryCount = 0, IsTransient = false.
    /// </remarks>
    public EmbeddingException(string message)
        : base(message)
    {
        StatusCode = 0;
        RetryCount = 0;
        IsTransient = false;
    }

    /// <summary>
    /// Initializes a new instance with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    /// <remarks>
    /// LOGIC: Constructor for wrapping other exceptions (network errors, timeouts, etc.).
    /// Uses defaults: StatusCode = 0, RetryCount = 0, IsTransient = false.
    /// </remarks>
    public EmbeddingException(string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = 0;
        RetryCount = 0;
        IsTransient = false;
    }

    /// <summary>
    /// Initializes a new instance with a message, HTTP status code, and transient indicator.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code from the API response (0 if not applicable).</param>
    /// <param name="isTransient">Whether the error is transient and retriable.</param>
    /// <remarks>
    /// LOGIC: Constructor for API errors with status code classification. Enables
    /// sophisticated error handling based on HTTP semantics. Default RetryCount = 0.
    /// </remarks>
    public EmbeddingException(string message, int statusCode, bool isTransient)
        : base(message)
    {
        StatusCode = statusCode;
        IsTransient = isTransient;
        RetryCount = 0;
    }

    /// <summary>
    /// Initializes a new instance with all diagnostic information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code from the API response (0 if not applicable).</param>
    /// <param name="isTransient">Whether the error is transient and retriable.</param>
    /// <param name="retryCount">The number of retry attempts made before this exception.</param>
    /// <remarks>
    /// LOGIC: Full constructor with complete diagnostic information. Enables
    /// precise error tracking and retry strategy decisions.
    /// </remarks>
    public EmbeddingException(string message, int statusCode, bool isTransient, int retryCount)
        : base(message)
    {
        StatusCode = statusCode;
        IsTransient = isTransient;
        RetryCount = retryCount;
    }

    /// <summary>
    /// Initializes a new instance with all diagnostic information and an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code from the API response (0 if not applicable).</param>
    /// <param name="isTransient">Whether the error is transient and retriable.</param>
    /// <param name="retryCount">The number of retry attempts made before this exception.</param>
    /// <param name="innerException">The underlying exception.</param>
    /// <remarks>
    /// LOGIC: Most comprehensive constructor for detailed error diagnostics. Captures
    /// the full context of a failure including the original exception, making debugging easier.
    /// </remarks>
    public EmbeddingException(string message, int statusCode, bool isTransient, int retryCount, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        IsTransient = isTransient;
        RetryCount = retryCount;
    }

    /// <summary>
    /// Gets the HTTP status code from the API response, if applicable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Zero indicates the error is not HTTP-related (network error, timeout, etc.).
    /// Non-zero values are HTTP status codes:
    /// <list type="bullet">
    ///   <item>4xx: Client error (typically permanent)</item>
    ///   <item>5xx: Server error (typically transient)</item>
    ///   <item>408: Request timeout (transient)</item>
    ///   <item>429: Too many requests (transient, rate limit)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>HTTP status code (0 if not an HTTP error).</value>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the number of retry attempts made before this exception was thrown.
    /// </summary>
    /// <remarks>
    /// LOGIC: Indicates how many times the operation was retried before exhausting
    /// the retry budget. Useful for diagnostics and implementing exponential backoff
    /// in higher-level retry logic.
    /// </remarks>
    /// <value>Retry count (0 if no retries were attempted).</value>
    public int RetryCount { get; }

    /// <summary>
    /// Gets a value indicating whether the error is transient and retriable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Transient errors are temporary and likely to succeed on retry:
    /// <list type="bullet">
    ///   <item>HTTP 5xx errors (server errors)</item>
    ///   <item>HTTP 429 (rate limit)</item>
    ///   <item>HTTP 408 (request timeout)</item>
    ///   <item>Network timeouts</item>
    ///   <item>Connection reset errors</item>
    /// </list>
    /// </para>
    /// <para>
    /// Permanent errors should not be retried:
    /// <list type="bullet">
    ///   <item>HTTP 401 (invalid API key)</item>
    ///   <item>HTTP 403 (insufficient permissions)</item>
    ///   <item>HTTP 404 (model not found)</item>
    ///   <item>HTTP 400 (invalid request format)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>
    /// <c>true</c> if the error is transient and may succeed on retry;
    /// <c>false</c> if the error is permanent and retrying is unlikely to help.
    /// </value>
    public bool IsTransient { get; }
}
