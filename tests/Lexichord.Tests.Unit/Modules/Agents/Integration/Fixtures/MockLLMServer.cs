// -----------------------------------------------------------------------
// <copyright file="MockLLMServer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Configurable mock LLM server fixture for integration tests (v0.6.8b).
//   Wraps Mock<IChatCompletionService> with fluent configuration helpers
//   for setting up batch responses, streaming token sequences, and error
//   conditions. Each method returns `this` for chaining.
//
//   Deviation from spec: Uses Moq instead of WireMock.Net since CoPilotAgent
//   consumes IChatCompletionService directly (no HTTP calls involved).
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Integration.Fixtures;

/// <summary>
/// Configurable mock LLM server for integration testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent API for configuring <see cref="IChatCompletionService"/>
/// mock behavior, including batch responses, streaming token sequences,
/// and error conditions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8b as part of the Integration Tests.
/// </para>
/// </remarks>
public sealed class MockLLMServer : IDisposable
{
    private readonly Mock<IChatCompletionService> _mock;

    /// <summary>
    /// Initializes a new instance of <see cref="MockLLMServer"/>.
    /// </summary>
    public MockLLMServer()
    {
        _mock = new Mock<IChatCompletionService>(MockBehavior.Strict);
        _mock.Setup(s => s.ProviderName).Returns("mock-llm");
    }

    /// <summary>
    /// Gets the underlying mock for direct verification.
    /// </summary>
    public Mock<IChatCompletionService> Mock => _mock;

    /// <summary>
    /// Gets the configured mock service instance.
    /// </summary>
    public IChatCompletionService Service => _mock.Object;

    /// <summary>
    /// Configures a batch (non-streaming) response with the specified content and token counts.
    /// </summary>
    /// <param name="content">The response text content.</param>
    /// <param name="promptTokens">Number of prompt tokens in the response.</param>
    /// <param name="completionTokens">Number of completion tokens in the response.</param>
    /// <param name="finishReason">The finish reason (default: "stop").</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MockLLMServer ConfigureResponse(
        string content,
        int promptTokens = 50,
        int completionTokens = 25,
        string finishReason = "stop")
    {
        var response = new ChatResponse(
            content,
            promptTokens,
            completionTokens,
            TimeSpan.FromMilliseconds(150),
            finishReason);

        _mock.Setup(s => s.CompleteAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return this;
    }

    /// <summary>
    /// Configures streaming to yield the specified token strings sequentially.
    /// </summary>
    /// <param name="tokens">The token text values to yield.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MockLLMServer ConfigureStreamingTokens(params string[] tokens)
    {
        _mock.Setup(s => s.StreamAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((ChatRequest _, CancellationToken ct) =>
                CreateTokenStream(tokens, ct));

        return this;
    }

    /// <summary>
    /// Configures the mock to throw the specified exception type on CompleteAsync.
    /// </summary>
    /// <typeparam name="TException">The exception type to throw.</typeparam>
    /// <param name="message">The exception message.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MockLLMServer ConfigureError<TException>(string message)
        where TException : Exception
    {
        var exception = (TException)Activator.CreateInstance(typeof(TException), message)!;

        _mock.Setup(s => s.CompleteAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        return this;
    }

    /// <summary>
    /// Configures the mock to throw a <see cref="TaskCanceledException"/> to simulate a timeout.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public MockLLMServer ConfigureTimeout()
    {
        _mock.Setup(s => s.CompleteAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("The request timed out."));

        return this;
    }

    /// <summary>
    /// Configures streaming to throw the specified exception after yielding partial tokens.
    /// </summary>
    /// <typeparam name="TException">The exception type to throw.</typeparam>
    /// <param name="message">The exception message.</param>
    /// <param name="partialTokens">Tokens to yield before the error.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MockLLMServer ConfigureStreamingError<TException>(
        string message,
        params string[] partialTokens)
        where TException : Exception
    {
        var exception = (TException)Activator.CreateInstance(typeof(TException), message)!;

        _mock.Setup(s => s.StreamAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((ChatRequest _, CancellationToken _) =>
                CreateErrorStream(partialTokens, exception));

        return this;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // No resources to dispose; placeholder for fixture lifecycle.
    }

    // ── Private Helpers ────────────────────────────────────────────────

    private static async IAsyncEnumerable<StreamingChatToken> CreateTokenStream(
        string[] tokens,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var token in tokens)
        {
            ct.ThrowIfCancellationRequested();
            yield return StreamingChatToken.Content(token);
            await Task.Yield(); // Simulate async delivery
        }

        yield return StreamingChatToken.Complete("stop");
    }

    private static async IAsyncEnumerable<StreamingChatToken> CreateErrorStream(
        string[] partialTokens,
        Exception error)
    {
        foreach (var token in partialTokens)
        {
            yield return StreamingChatToken.Content(token);
            await Task.Yield();
        }

        throw error;
    }
}
