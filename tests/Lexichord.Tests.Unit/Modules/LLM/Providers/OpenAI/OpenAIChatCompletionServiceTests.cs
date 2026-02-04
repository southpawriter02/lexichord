// -----------------------------------------------------------------------
// <copyright file="OpenAIChatCompletionServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.LLM.Providers.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Providers.OpenAI;

/// <summary>
/// Unit tests for <see cref="OpenAIChatCompletionService"/>.
/// </summary>
public class OpenAIChatCompletionServiceTests
{
    #region Test Fixtures

    /// <summary>
    /// Sample success response JSON.
    /// </summary>
    private const string SuccessResponse = """
        {
            "id": "chatcmpl-test123",
            "object": "chat.completion",
            "created": 1677858242,
            "model": "gpt-4o-mini",
            "usage": {
                "prompt_tokens": 10,
                "completion_tokens": 5,
                "total_tokens": 15
            },
            "choices": [{
                "message": {
                    "role": "assistant",
                    "content": "The capital of France is Paris."
                },
                "finish_reason": "stop",
                "index": 0
            }]
        }
        """;

    /// <summary>
    /// Sample streaming response (multiple SSE chunks).
    /// </summary>
    private static readonly string StreamingResponse = string.Join("\n\n",
        """data: {"id":"test","choices":[{"index":0,"delta":{"role":"assistant"},"finish_reason":null}]}""",
        """data: {"id":"test","choices":[{"index":0,"delta":{"content":"The capital"},"finish_reason":null}]}""",
        """data: {"id":"test","choices":[{"index":0,"delta":{"content":" of France"},"finish_reason":null}]}""",
        """data: {"id":"test","choices":[{"index":0,"delta":{"content":" is Paris."},"finish_reason":null}]}""",
        """data: {"id":"test","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}""",
        "data: [DONE]",
        ""  // Trailing newline
    );

    /// <summary>
    /// Sample authentication error response.
    /// </summary>
    private const string AuthErrorResponse = """
        {
            "error": {
                "message": "Incorrect API key provided",
                "type": "invalid_api_key",
                "code": "invalid_api_key"
            }
        }
        """;

    /// <summary>
    /// Sample rate limit error response.
    /// </summary>
    private const string RateLimitResponse = """
        {
            "error": {
                "message": "Rate limit reached",
                "type": "rate_limit_error",
                "code": "rate_limit_exceeded"
            }
        }
        """;

    #endregion

    #region Test Setup

    private readonly ISecureVault _vault;
    private readonly ILogger<OpenAIChatCompletionService> _logger;
    private readonly IOptions<OpenAIOptions> _options;

    /// <summary>
    /// Initializes a new instance of the test class.
    /// </summary>
    public OpenAIChatCompletionServiceTests()
    {
        _vault = Substitute.For<ISecureVault>();
        _logger = NullLogger<OpenAIChatCompletionService>.Instance;
        _options = Options.Create(new OpenAIOptions());

        // Default vault behavior: API key exists and can be retrieved
        _vault.SecretExistsAsync(OpenAIOptions.VaultKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(OpenAIOptions.VaultKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test-api-key"));
    }

    /// <summary>
    /// Creates a service instance with a mock HTTP handler.
    /// </summary>
    private OpenAIChatCompletionService CreateService(MockHttpMessageHandler handler)
    {
        var httpClientFactory = CreateMockHttpClientFactory(handler);
        return new OpenAIChatCompletionService(httpClientFactory, _vault, _options, _logger);
    }

    /// <summary>
    /// Creates a mock HTTP client factory that returns a client with the specified handler.
    /// </summary>
    private static IHttpClientFactory CreateMockHttpClientFactory(HttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/v1")
        };
        factory.CreateClient(OpenAIOptions.HttpClientName).Returns(client);
        return factory;
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor throws on null HTTP client factory.
    /// </summary>
    [Fact]
    public void Constructor_WithNullHttpClientFactory_ShouldThrow()
    {
        // Arrange
        var factory = Substitute.For<IHttpClientFactory>();

        // Act
        var act = () => new OpenAIChatCompletionService(null!, _vault, _options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("httpClientFactory");
    }

    /// <summary>
    /// Tests that constructor throws on null vault.
    /// </summary>
    [Fact]
    public void Constructor_WithNullVault_ShouldThrow()
    {
        // Arrange
        var factory = Substitute.For<IHttpClientFactory>();

        // Act
        var act = () => new OpenAIChatCompletionService(factory, null!, _options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("vault");
    }

    /// <summary>
    /// Tests that constructor throws on null options.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrow()
    {
        // Arrange
        var factory = Substitute.For<IHttpClientFactory>();

        // Act
        var act = () => new OpenAIChatCompletionService(factory, _vault, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("options");
    }

    /// <summary>
    /// Tests that constructor throws on null logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Arrange
        var factory = Substitute.For<IHttpClientFactory>();

        // Act
        var act = () => new OpenAIChatCompletionService(factory, _vault, _options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("logger");
    }

    #endregion

    #region ProviderName Tests

    /// <summary>
    /// Tests that ProviderName returns "OpenAI".
    /// </summary>
    [Fact]
    public void ProviderName_ShouldReturnOpenAI()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessResponse);
        var service = CreateService(handler);

        // Assert
        service.ProviderName.Should().Be("OpenAI");
    }

    #endregion

    #region CompleteAsync Tests

    /// <summary>
    /// Tests that CompleteAsync returns response for valid request.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithValidRequest_ShouldReturnResponse()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("What is the capital of France?");

        // Act
        var response = await service.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Content.Should().Be("The capital of France is Paris.");
        response.PromptTokens.Should().Be(10);
        response.CompletionTokens.Should().Be(5);
        response.FinishReason.Should().Be("stop");
    }

    /// <summary>
    /// Tests that CompleteAsync throws ArgumentNullException for null request.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithNullRequest_ShouldThrow()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessResponse);
        var service = CreateService(handler);

        // Act
        var act = async () => await service.CompleteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "request");
    }

    /// <summary>
    /// Tests that CompleteAsync throws AuthenticationException for 401 response.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_With401_ShouldThrowAuthenticationException()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.Unauthorized, AuthErrorResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var act = async () => await service.CompleteAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .Where(ex => ex.ProviderName == "OpenAI");
    }

    /// <summary>
    /// Tests that CompleteAsync throws RateLimitException for 429 response.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_With429_ShouldThrowRateLimitException()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.TooManyRequests, RateLimitResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var act = async () => await service.CompleteAsync(request);

        // Assert
        await act.Should().ThrowAsync<RateLimitException>()
            .Where(ex => ex.ProviderName == "OpenAI");
    }

    /// <summary>
    /// Tests that CompleteAsync throws ChatCompletionException for 500 response.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_With500_ShouldThrowChatCompletionException()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJson(
            HttpStatusCode.InternalServerError,
            """{"error":{"message":"Internal server error"}}""");
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var act = async () => await service.CompleteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ChatCompletionException>();
    }

    /// <summary>
    /// Tests that CompleteAsync throws ProviderNotConfiguredException when API key missing.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithMissingApiKey_ShouldThrowProviderNotConfiguredException()
    {
        // Arrange
        _vault.SecretExistsAsync(OpenAIOptions.VaultKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var act = async () => await service.CompleteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ProviderNotConfiguredException>()
            .Where(ex => ex.ProviderName == "OpenAI");
    }

    /// <summary>
    /// Tests that CompleteAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithDelay(TimeSpan.FromSeconds(10), HttpStatusCode.OK, SuccessResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var act = async () => await service.CompleteAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that CompleteAsync sends authorization header.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldSendAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SuccessResponse, Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        await service.CompleteAsync(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization!.Parameter.Should().Be("sk-test-api-key");
    }

    #endregion

    #region StreamAsync Tests

    /// <summary>
    /// Tests that StreamAsync yields tokens for valid request.
    /// </summary>
    [Fact]
    public async Task StreamAsync_WithValidRequest_ShouldYieldTokens()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithStreaming(HttpStatusCode.OK, StreamingResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("What is the capital of France?");
        var tokens = new List<StreamingChatToken>();

        // Act
        await foreach (var token in service.StreamAsync(request))
        {
            tokens.Add(token);
        }

        // Assert
        tokens.Should().NotBeEmpty();
        tokens.Should().Contain(t => t.Token == "The capital");
        tokens.Should().Contain(t => t.Token == " of France");
        tokens.Should().Contain(t => t.Token == " is Paris.");
    }

    /// <summary>
    /// Tests that StreamAsync yields complete token with finish reason.
    /// </summary>
    [Fact]
    public async Task StreamAsync_ShouldYieldCompleteToken()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithStreaming(HttpStatusCode.OK, StreamingResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");
        StreamingChatToken? lastToken = null;

        // Act
        await foreach (var token in service.StreamAsync(request))
        {
            lastToken = token;
        }

        // Assert
        lastToken.Should().NotBeNull();
        lastToken!.IsComplete.Should().BeTrue();
        lastToken.FinishReason.Should().Be("stop");
    }

    /// <summary>
    /// Tests that StreamAsync throws ArgumentNullException for null request.
    /// </summary>
    [Fact]
    public async Task StreamAsync_WithNullRequest_ShouldThrow()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithStreaming(HttpStatusCode.OK, StreamingResponse);
        var service = CreateService(handler);

        // Act
        var act = async () =>
        {
            await foreach (var _ in service.StreamAsync(null!))
            {
            }
        };

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "request");
    }

    /// <summary>
    /// Tests that StreamAsync throws AuthenticationException for 401.
    /// </summary>
    [Fact]
    public async Task StreamAsync_With401_ShouldThrowAuthenticationException()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.Unauthorized, AuthErrorResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var act = async () =>
        {
            await foreach (var _ in service.StreamAsync(request))
            {
            }
        };

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    /// <summary>
    /// Tests that StreamAsync throws ProviderNotConfiguredException when API key missing.
    /// </summary>
    [Fact]
    public async Task StreamAsync_WithMissingApiKey_ShouldThrowProviderNotConfiguredException()
    {
        // Arrange
        _vault.SecretExistsAsync(OpenAIOptions.VaultKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = MockHttpMessageHandler.WithStreaming(HttpStatusCode.OK, StreamingResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var act = async () =>
        {
            await foreach (var _ in service.StreamAsync(request))
            {
            }
        };

        // Assert
        await act.Should().ThrowAsync<ProviderNotConfiguredException>();
    }

    #endregion

    #region MockHttpMessageHandler

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        private readonly TimeSpan _delay;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler, TimeSpan? delay = null)
        {
            _handler = handler;
            _delay = delay ?? TimeSpan.Zero;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return _handler(request);
        }

        public static MockHttpMessageHandler WithJson(HttpStatusCode status, string json)
        {
            return new MockHttpMessageHandler(_ =>
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
        }

        public static MockHttpMessageHandler WithDelay(TimeSpan delay, HttpStatusCode status, string json)
        {
            return new MockHttpMessageHandler(_ =>
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }, delay);
        }

        public static MockHttpMessageHandler WithStreaming(HttpStatusCode status, string sseContent)
        {
            return new MockHttpMessageHandler(_ =>
            {
                var content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream");
                return new HttpResponseMessage(status)
                {
                    Content = content
                };
            });
        }
    }

    #endregion
}
