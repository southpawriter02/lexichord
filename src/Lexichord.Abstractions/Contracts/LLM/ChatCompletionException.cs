// -----------------------------------------------------------------------
// <copyright file="ChatCompletionException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Base exception for chat completion errors.
/// </summary>
/// <remarks>
/// <para>
/// This is the base class for all exceptions thrown by <see cref="IChatCompletionService"/>
/// implementations. Catch this type to handle all chat-related errors uniformly.
/// </para>
/// <para>
/// For more specific error handling, catch the derived exception types:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AuthenticationException"/>: API key or authentication failures</description></item>
///   <item><description><see cref="RateLimitException"/>: Rate limit exceeded errors</description></item>
///   <item><description><see cref="ProviderNotConfiguredException"/>: Missing provider configuration</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var response = await service.CompleteAsync(request);
/// }
/// catch (AuthenticationException ex)
/// {
///     Console.WriteLine($"Auth failed for {ex.ProviderName}: {ex.Message}");
/// }
/// catch (RateLimitException ex)
/// {
///     Console.WriteLine($"Rate limited. Retry after: {ex.RetryAfter}");
/// }
/// catch (ChatCompletionException ex)
/// {
///     Console.WriteLine($"Chat error: {ex.Message}");
/// }
/// </code>
/// </example>
public class ChatCompletionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCompletionException"/> class.
    /// </summary>
    public ChatCompletionException()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCompletionException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ChatCompletionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCompletionException"/> class
    /// with a specified error message and provider name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="providerName">The name of the provider that threw the exception.</param>
    public ChatCompletionException(string message, string providerName)
        : base(message)
    {
        ProviderName = providerName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCompletionException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ChatCompletionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCompletionException"/> class
    /// with a specified error message, provider name, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="providerName">The name of the provider that threw the exception.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ChatCompletionException(string message, string providerName, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = providerName;
    }

    /// <summary>
    /// Gets the name of the provider that threw the exception.
    /// </summary>
    /// <value>The provider name, or null if not specified.</value>
    public string? ProviderName { get; }
}

/// <summary>
/// Exception thrown when API authentication fails.
/// </summary>
/// <remarks>
/// This exception indicates that the API key or authentication credentials
/// are invalid, expired, or missing. Check your provider configuration.
/// </remarks>
public class AuthenticationException : ChatCompletionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
    /// </summary>
    public AuthenticationException()
        : base("Authentication failed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class
    /// with a specified error message and provider name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="providerName">The name of the provider that rejected authentication.</param>
    public AuthenticationException(string message, string providerName)
        : base(message, providerName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public AuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class
    /// with specified details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="providerName">The name of the provider that rejected authentication.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public AuthenticationException(string message, string providerName, Exception innerException)
        : base(message, providerName, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when rate limits are exceeded.
/// </summary>
/// <remarks>
/// <para>
/// This exception indicates that too many requests have been made in a given time period.
/// The <see cref="RetryAfter"/> property provides guidance on when to retry.
/// </para>
/// <para>
/// Consider implementing exponential backoff or a rate limiting strategy to avoid
/// repeatedly hitting rate limits.
/// </para>
/// </remarks>
public class RateLimitException : ChatCompletionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class.
    /// </summary>
    public RateLimitException()
        : base("Rate limit exceeded.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RateLimitException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class
    /// with a specified error message and retry delay.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="retryAfter">The suggested time to wait before retrying.</param>
    public RateLimitException(string message, TimeSpan? retryAfter)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class
    /// with a specified error message, provider name, and retry delay.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="providerName">The name of the provider that rate limited the request.</param>
    /// <param name="retryAfter">The suggested time to wait before retrying.</param>
    public RateLimitException(string message, string providerName, TimeSpan? retryAfter = null)
        : base(message, providerName)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public RateLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the suggested time to wait before retrying the request.
    /// </summary>
    /// <value>The retry delay, or null if not specified by the provider.</value>
    public TimeSpan? RetryAfter { get; }
}

/// <summary>
/// Exception thrown when a provider is not properly configured.
/// </summary>
/// <remarks>
/// <para>
/// This exception indicates that the requested provider has not been configured
/// or registered with the service registry. Check your DI configuration and
/// ensure the provider is properly registered.
/// </para>
/// </remarks>
public class ProviderNotConfiguredException : ChatCompletionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotConfiguredException"/> class.
    /// </summary>
    public ProviderNotConfiguredException()
        : base("Provider is not configured.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotConfiguredException"/> class
    /// with a specified provider name.
    /// </summary>
    /// <param name="providerName">The name of the unconfigured provider.</param>
    public ProviderNotConfiguredException(string providerName)
        : base($"Provider '{providerName}' is not configured.", providerName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotConfiguredException"/> class
    /// with a specified error message and provider name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="providerName">The name of the unconfigured provider.</param>
    public ProviderNotConfiguredException(string message, string providerName)
        : base(message, providerName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotConfiguredException"/> class
    /// with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ProviderNotConfiguredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
