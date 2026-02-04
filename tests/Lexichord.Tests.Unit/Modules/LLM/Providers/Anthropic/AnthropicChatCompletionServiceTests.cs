// -----------------------------------------------------------------------
// <copyright file="AnthropicChatCompletionServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.LLM.Providers.Anthropic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Providers.Anthropic;

/// <summary>
/// Unit tests for <see cref="AnthropicChatCompletionService"/>.
/// </summary>
public class AnthropicChatCompletionServiceTests
{
    #region Test Fixtures

    /// <summary>
    /// Sample success response JSON in Anthropic format.
    /// </summary>
    private const string SuccessResponse = """
        {
            "id": "msg_abc123",
            "type": "message",
            "role": "assistant",
            "content": [
                {
                    "type": "text",
                    "text": "The capital of France is Paris."
                }
            ],
            "model": "claude-3-haiku-20240307",
            "stop_reason": "end_turn",
            "usage": {
                "input_tokens": 15,
                "output_tokens": 10
            }
        }
        """;

    /// <summary>
    /// Sample streaming response (multiple SSE events in Anthropic format).
    /// </summary>
    private static readonly string StreamingResponse = string.Join("\n",
        "event: message_start",
        """data: {"type":"message_start","message":{"id":"msg_abc123","type":"message","role":"assistant","model":"claude-3-haiku-20240307"}}""",
        "",
        "event: content_block_start",
        """data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""",
        "",
        "event: content_block_delta",
        """data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"The capital"}}""",
        "",
        "event: content_block_delta",
        """data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" of France"}}""",
        "",
        "event: content_block_delta",
        """data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" is Paris."}}""",
        "",
        "event: content_block_stop",
        """data: {"type":"content_block_stop","index":0}""",
        "",
        "event: message_delta",
        """data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"output_tokens":10}}""",
        "",
        "event: message_stop",
        """data: {"type":"message_stop"}""",
        ""  // Trailing newline
    );

    /// <summary>
    /// Sample authentication error response.
    /// </summary>
    private const string AuthErrorResponse = """
        {
            "error": {
                "type": "authentication_error",
                "message": "Invalid API key provided"
            }
        }
        """;

    /// <summary>
    /// Sample rate limit error response.
    /// </summary>
    private const string RateLimitResponse = """
        {
            "error": {
                "type": "rate_limit_error",
                "message": "Rate limit exceeded"
            }
        }
        """;

    #endregion

    #region Test Setup

    private readonly ISecureVault _vault;
    private readonly ILogger<AnthropicChatCompletionService> _logger;
    private readonly IOptions<AnthropicOptions> _options;

    /// <summary>
    /// Initializes a new instance of the test class.
    /// </summary>
    public AnthropicChatCompletionServiceTests()
    {
        _vault = Substitute.For<ISecureVault>();
        _logger = NullLogger<AnthropicChatCompletionService>.Instance;
        _options = Options.Create(new AnthropicOptions());

        // Default vault behavior: API key exists and can be retrieved
        _vault.SecretExistsAsync(AnthropicOptions.VaultKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(AnthropicOptions.VaultKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-ant-test-api-key"));
    }

    /// <summary>
    /// Creates a service instance with a mock HTTP handler.
    /// </summary>
    private AnthropicChatCompletionService CreateService(MockHttpMessageHandler handler)
    {
        var httpClientFactory = CreateMockHttpClientFactory(handler);
        return new AnthropicChatCompletionService(httpClientFactory, _vault, _options, _logger);
    }

    /// <summary>
    /// Creates a mock HTTP client factory that returns a client with the specified handler.
    /// </summary>
    private static IHttpClientFactory CreateMockHttpClientFactory(HttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.anthropic.com/v1")
        };
        factory.CreateClient(AnthropicOptions.HttpClientName).Returns(client);
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
        // Act
        var act = () => new AnthropicChatCompletionService(null!, _vault, _options, _logger);

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
        var act = () => new AnthropicChatCompletionService(factory, null!, _options, _logger);

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
        var act = () => new AnthropicChatCompletionService(factory, _vault, null!, _logger);

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
        var act = () => new AnthropicChatCompletionService(factory, _vault, _options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("logger");
    }

    #endregion

    #region ProviderName Tests

    /// <summary>
    /// Tests that ProviderName returns "Anthropic".
    /// </summary>
    [Fact]
    public void ProviderName_ShouldReturnAnthropic()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessResponse);
        var service = CreateService(handler);

        // Assert
        service.ProviderName.Should().Be("Anthropic");
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
        response.PromptTokens.Should().Be(15);
        response.CompletionTokens.Should().Be(10);
        response.FinishReason.Should().Be("end_turn");
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
            .Where(ex => ex.ProviderName == "Anthropic");
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
            .Where(ex => ex.ProviderName == "Anthropic");
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
            """{"error":{"type":"server_error","message":"Internal server error"}}""");
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
        _vault.SecretExistsAsync(AnthropicOptions.VaultKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var act = async () => await service.CompleteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ProviderNotConfiguredException>()
            .Where(ex => ex.ProviderName == "Anthropic");
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
    /// Tests that CompleteAsync sends x-api-key header.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldSendXApiKeyHeader()
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
        capturedRequest!.Headers.Should().Contain(h => h.Key == "x-api-key");
        capturedRequest.Headers.GetValues("x-api-key").Should().Contain("sk-ant-test-api-key");
    }

    /// <summary>
    /// Tests that CompleteAsync sends anthropic-version header.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldSendAnthropicVersionHeader()
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
        capturedRequest!.Headers.Should().Contain(h => h.Key == "anthropic-version");
        capturedRequest.Headers.GetValues("anthropic-version").Should().Contain("2024-01-01");
    }

    /// <summary>
    /// Tests that CompleteAsync does NOT send Bearer authorization header (Anthropic uses x-api-key).
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldNotSendBearerAuthorizationHeader()
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
        capturedRequest!.Headers.Authorization.Should().BeNull();
    }

    /// <summary>
    /// Tests that CompleteAsync sends request to correct endpoint.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldSendToMessagesEndpoint()
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
        capturedRequest!.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri!.ToString().Should().Contain("/messages");
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
        lastToken.FinishReason.Should().Be("end_turn");
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
        _vault.SecretExistsAsync(AnthropicOptions.VaultKey, Arg.Any<CancellationToken>())
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

    /// <summary>
    /// Tests that StreamAsync sends x-api-key header.
    /// </summary>
    [Fact]
    public async Task StreamAsync_ShouldSendXApiKeyHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(StreamingResponse, Encoding.UTF8, "text/event-stream")
            };
        });
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        await foreach (var _ in service.StreamAsync(request))
        {
        }

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().Contain(h => h.Key == "x-api-key");
    }

    /// <summary>
    /// Tests that StreamAsync sends anthropic-version header.
    /// </summary>
    [Fact]
    public async Task StreamAsync_ShouldSendAnthropicVersionHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(StreamingResponse, Encoding.UTF8, "text/event-stream")
            };
        });
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        await foreach (var _ in service.StreamAsync(request))
        {
        }

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().Contain(h => h.Key == "anthropic-version");
    }

    #endregion

    #region Overloaded Error Tests

    /// <summary>
    /// Tests that CompleteAsync handles Anthropic overloaded error.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithOverloadedError_ShouldThrowChatCompletionException()
    {
        // Arrange
        var overloadedResponse = """{"error":{"type":"overloaded_error","message":"API is overloaded"}}""";
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.ServiceUnavailable, overloadedResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var act = async () => await service.CompleteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ChatCompletionException>()
            .Where(ex => ex.Message.Contains("overloaded"));
    }

    #endregion

    #region Content Block Assembly Tests

    /// <summary>
    /// Tests that CompleteAsync handles responses with multiple content blocks.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithMultipleContentBlocks_ShouldConcatenateContent()
    {
        // Arrange
        var multiBlockResponse = """
            {
                "id": "msg_abc123",
                "type": "message",
                "role": "assistant",
                "content": [
                    {"type": "text", "text": "First part. "},
                    {"type": "text", "text": "Second part."}
                ],
                "stop_reason": "end_turn",
                "usage": {"input_tokens": 10, "output_tokens": 8}
            }
            """;
        var handler = MockHttpMessageHandler.WithJson(HttpStatusCode.OK, multiBlockResponse);
        var service = CreateService(handler);
        var request = ChatRequest.FromUserMessage("Hello");

        // Act
        var response = await service.CompleteAsync(request);

        // Assert
        response.Content.Should().Be("First part. Second part.");
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
