// =============================================================================
// File: MockHttpMessageHandler.cs
// Project: Lexichord.Modules.RAG
// Description: HTTP message handler mock for unit testing embedding service.
// =============================================================================
// LOGIC: Allows unit tests to intercept HTTP requests and provide controlled responses.
//   - Can handle both fixed responses and dynamic responses via callback.
//   - Supports scenarios: successful API calls, rate limiting, server errors.
//   - Enables testing retry policy without hitting real API.
//   - Thread-safe for concurrent test scenarios.
// =============================================================================

using System.Net;

namespace Lexichord.Modules.RAG.Embedding;

/// <summary>
/// HTTP message handler mock for unit testing HTTP-based services.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MockHttpMessageHandler"/> allows tests to intercept HTTP requests
/// and provide controlled responses without hitting real endpoints. It can be
/// configured with a fixed response or a callback function for dynamic behavior.
/// </para>
/// <para>
/// <b>Usage:</b>
/// </para>
/// <code>
/// // Fixed response
/// var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage
/// {
///     StatusCode = HttpStatusCode.OK,
///     Content = new StringContent(@"{ ""data"": [] }")
/// });
///
/// // Dynamic response
/// var mockHandler = new MockHttpMessageHandler(request =>
/// {
///     if (request.RequestUri?.PathAndQuery.Contains("embeddings") == true)
///         return new HttpResponseMessage(HttpStatusCode.OK);
///     return new HttpResponseMessage(HttpStatusCode.NotFound);
/// });
/// </code>
/// </remarks>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;
    private readonly HttpResponseMessage? _fixedResponse;

    /// <summary>
    /// Initializes a new instance with a dynamic response callback.
    /// </summary>
    /// <param name="handler">
    /// A function that receives the request and returns a response.
    /// The function should not dispose the request message.
    /// </param>
    /// <remarks>
    /// <para>
    /// The callback is invoked for each request, allowing test logic to inspect
    /// the request and generate appropriate responses.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="handler"/> is null.
    /// </exception>
    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
    }

    /// <summary>
    /// Initializes a new instance with a fixed response.
    /// </summary>
    /// <param name="response">
    /// The response to return for all requests.
    /// </param>
    /// <remarks>
    /// <para>
    /// All requests will receive the same response object. If the response
    /// content is read by tests, consider using a callback handler instead
    /// to generate fresh responses for each request.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="response"/> is null.
    /// </exception>
    public MockHttpMessageHandler(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _fixedResponse = response;
    }

    /// <summary>
    /// Sends the HTTP request and returns the configured mock response.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configured mock response.</returns>
    /// <remarks>
    /// <para>
    /// If initialized with a callback, invokes the callback with the request.
    /// If initialized with a fixed response, returns that response.
    /// </para>
    /// </remarks>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_handler != null)
        {
            var response = _handler(request);
            return Task.FromResult(response);
        }

        if (_fixedResponse != null)
        {
            return Task.FromResult(_fixedResponse);
        }

        // LOGIC: Fallback to 500 if neither handler nor fixed response configured.
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }
}
