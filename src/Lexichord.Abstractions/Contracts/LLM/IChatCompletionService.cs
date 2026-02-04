// -----------------------------------------------------------------------
// <copyright file="IChatCompletionService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Defines the contract for chat completion services that communicate with LLM providers.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary abstraction for Large Language Model communication in Lexichord.
/// Implementations handle provider-specific API calls, authentication, and response parsing.
/// </para>
/// <para>
/// The service supports both synchronous (request-response) and streaming modes:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><see cref="CompleteAsync"/>: Returns the complete response after all tokens are generated.</description>
///   </item>
///   <item>
///     <description><see cref="StreamAsync"/>: Yields tokens incrementally as they are generated.</description>
///   </item>
/// </list>
/// <para>
/// Implementations should handle transient failures gracefully and throw typed exceptions
/// (see <see cref="ChatCompletionException"/>) for permanent failures.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Synchronous completion
/// var request = ChatRequest.FromUserMessage("What is the capital of France?");
/// var response = await service.CompleteAsync(request);
/// Console.WriteLine(response.Content);
///
/// // Streaming completion
/// var request = ChatRequest.FromUserMessage("Write a story about dragons.");
/// await foreach (var token in service.StreamAsync(request))
/// {
///     Console.Write(token.Token);
/// }
/// </code>
/// </example>
public interface IChatCompletionService
{
    /// <summary>
    /// Gets the unique identifier for this LLM provider.
    /// </summary>
    /// <value>
    /// A string identifying the provider (e.g., "openai", "anthropic", "local").
    /// This should be lowercase and use hyphens for multi-word names.
    /// </value>
    /// <remarks>
    /// This property is used for provider selection, logging, and configuration lookup.
    /// It should remain stable across application restarts.
    /// </remarks>
    string ProviderName { get; }

    /// <summary>
    /// Sends a chat completion request and returns the complete response.
    /// </summary>
    /// <param name="request">The chat request containing messages and options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the complete
    /// <see cref="ChatResponse"/> with content and usage metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="AuthenticationException">Thrown when API authentication fails.</exception>
    /// <exception cref="RateLimitException">Thrown when rate limits are exceeded.</exception>
    /// <exception cref="ChatCompletionException">Thrown for other provider errors.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// <para>
    /// This method waits for the complete response before returning. For long-running
    /// requests or when real-time feedback is needed, consider using <see cref="StreamAsync"/>.
    /// </para>
    /// <para>
    /// Implementations should respect the <see cref="ChatOptions"/> in the request,
    /// falling back to provider defaults for unspecified options.
    /// </para>
    /// </remarks>
    Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a chat completion request and streams tokens as they are generated.
    /// </summary>
    /// <param name="request">The chat request containing messages and options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An async enumerable yielding <see cref="StreamingChatToken"/> instances
    /// as they are received from the provider.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="AuthenticationException">Thrown when API authentication fails.</exception>
    /// <exception cref="RateLimitException">Thrown when rate limits are exceeded.</exception>
    /// <exception cref="ChatCompletionException">Thrown for other provider errors.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// <para>
    /// Streaming provides a better user experience for long responses by displaying
    /// partial results immediately. The final token will have <see cref="StreamingChatToken.IsComplete"/>
    /// set to true.
    /// </para>
    /// <para>
    /// Note that token counts are typically not available until the stream completes.
    /// If you need usage metrics, consider using <see cref="CompleteAsync"/> instead
    /// or tracking token counts client-side.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var tokens = new List&lt;string&gt;();
    /// await foreach (var token in service.StreamAsync(request))
    /// {
    ///     tokens.Add(token.Token);
    ///     if (token.IsComplete)
    ///     {
    ///         Console.WriteLine($"\nFinished: {token.FinishReason}");
    ///     }
    /// }
    /// var fullResponse = string.Concat(tokens);
    /// </code>
    /// </example>
    IAsyncEnumerable<StreamingChatToken> StreamAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}
