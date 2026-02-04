// -----------------------------------------------------------------------
// <copyright file="AnthropicResponseParserTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Providers.Anthropic;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Providers.Anthropic;

/// <summary>
/// Unit tests for <see cref="AnthropicResponseParser"/>.
/// </summary>
public class AnthropicResponseParserTests
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
    /// Sample success response with multiple content blocks.
    /// </summary>
    private const string MultiBlockResponse = """
        {
            "id": "msg_abc123",
            "type": "message",
            "role": "assistant",
            "content": [
                {
                    "type": "text",
                    "text": "First paragraph. "
                },
                {
                    "type": "text",
                    "text": "Second paragraph."
                }
            ],
            "model": "claude-3-haiku-20240307",
            "stop_reason": "end_turn",
            "usage": {
                "input_tokens": 20,
                "output_tokens": 15
            }
        }
        """;

    /// <summary>
    /// Sample rate limit error response JSON.
    /// </summary>
    private const string RateLimitResponse = """
        {
            "error": {
                "type": "rate_limit_error",
                "message": "Rate limit exceeded. Please wait before retrying."
            }
        }
        """;

    /// <summary>
    /// Sample authentication error response JSON.
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
    /// Sample overloaded error response JSON.
    /// </summary>
    private const string OverloadedResponse = """
        {
            "error": {
                "type": "overloaded_error",
                "message": "Anthropic API is currently overloaded"
            }
        }
        """;

    /// <summary>
    /// Sample invalid request error response JSON.
    /// </summary>
    private const string InvalidRequestResponse = """
        {
            "error": {
                "type": "invalid_request_error",
                "message": "max_tokens must be greater than 0"
            }
        }
        """;

    /// <summary>
    /// Sample content_block_delta event data.
    /// </summary>
    private const string ContentBlockDeltaData = """
        {
            "type": "content_block_delta",
            "index": 0,
            "delta": {
                "type": "text_delta",
                "text": "Hello"
            }
        }
        """;

    /// <summary>
    /// Sample message_delta event data with stop reason.
    /// </summary>
    private const string MessageDeltaData = """
        {
            "type": "message_delta",
            "delta": {
                "stop_reason": "end_turn"
            },
            "usage": {
                "output_tokens": 25
            }
        }
        """;

    /// <summary>
    /// Sample message_start event data.
    /// </summary>
    private const string MessageStartData = """
        {
            "type": "message_start",
            "message": {
                "id": "msg_abc123",
                "type": "message",
                "role": "assistant",
                "model": "claude-3-haiku-20240307"
            }
        }
        """;

    /// <summary>
    /// Sample streaming error event data.
    /// </summary>
    private const string StreamErrorData = """
        {
            "type": "error",
            "error": {
                "type": "overloaded_error",
                "message": "API is currently overloaded"
            }
        }
        """;

    #endregion

    #region ParseSuccessResponse Tests

    /// <summary>
    /// Tests that ParseSuccessResponse extracts content correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_ShouldExtractContent()
    {
        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.Content.Should().Be("The capital of France is Paris.");
    }

    /// <summary>
    /// Tests that ParseSuccessResponse concatenates multiple content blocks.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_WithMultipleBlocks_ShouldConcatenateContent()
    {
        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(
            MultiBlockResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.Content.Should().Be("First paragraph. Second paragraph.");
    }

    /// <summary>
    /// Tests that ParseSuccessResponse extracts input tokens correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_ShouldExtractInputTokensAsPromptTokens()
    {
        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.PromptTokens.Should().Be(15);
    }

    /// <summary>
    /// Tests that ParseSuccessResponse extracts output tokens correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_ShouldExtractOutputTokensAsCompletionTokens()
    {
        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.CompletionTokens.Should().Be(10);
    }

    /// <summary>
    /// Tests that ParseSuccessResponse extracts stop reason correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_ShouldExtractStopReasonAsFinishReason()
    {
        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.FinishReason.Should().Be("end_turn");
    }

    /// <summary>
    /// Tests that ParseSuccessResponse sets duration correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_ShouldSetDuration()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(SuccessResponse, duration);

        // Assert
        response.Duration.Should().Be(duration);
    }

    /// <summary>
    /// Tests that ParseSuccessResponse computes TotalTokens correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_TotalTokens_ShouldBeSum()
    {
        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.TotalTokens.Should().Be(25); // 15 + 10
    }

    /// <summary>
    /// Tests that ParseSuccessResponse handles response with no usage data.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_WithNoUsage_ShouldReturnZeroTokens()
    {
        // Arrange
        var responseWithoutUsage = """
            {
                "id": "msg_abc123",
                "type": "message",
                "role": "assistant",
                "content": [{"type": "text", "text": "Hello"}],
                "model": "claude-3-haiku-20240307",
                "stop_reason": "end_turn"
            }
            """;

        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(
            responseWithoutUsage,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.PromptTokens.Should().Be(0);
        response.CompletionTokens.Should().Be(0);
    }

    /// <summary>
    /// Tests that ParseSuccessResponse handles response with no stop reason.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_WithNoStopReason_ShouldReturnNullFinishReason()
    {
        // Arrange
        var responseWithoutStopReason = """
            {
                "id": "msg_abc123",
                "type": "message",
                "role": "assistant",
                "content": [{"type": "text", "text": "Hello"}],
                "model": "claude-3-haiku-20240307",
                "usage": {"input_tokens": 5, "output_tokens": 3}
            }
            """;

        // Act
        var response = AnthropicResponseParser.ParseSuccessResponse(
            responseWithoutStopReason,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.FinishReason.Should().BeNull();
    }

    #endregion

    #region ParseStreamingEvent Tests

    /// <summary>
    /// Tests that ParseStreamingEvent extracts content from content_block_delta.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_ContentBlockDelta_ShouldReturnToken()
    {
        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("content_block_delta", ContentBlockDeltaData);

        // Assert
        token.Should().NotBeNull();
        token!.Token.Should().Be("Hello");
        token.IsComplete.Should().BeFalse();
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns complete token from message_delta with stop reason.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_MessageDelta_ShouldReturnCompleteToken()
    {
        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("message_delta", MessageDeltaData);

        // Assert
        token.Should().NotBeNull();
        token!.IsComplete.Should().BeTrue();
        token.FinishReason.Should().Be("end_turn");
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns complete token for message_stop.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_MessageStop_ShouldReturnCompleteToken()
    {
        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("message_stop", "{}");

        // Assert
        token.Should().NotBeNull();
        token!.IsComplete.Should().BeTrue();
        token.FinishReason.Should().Be("end_turn");
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns null for message_start.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_MessageStart_ShouldReturnNull()
    {
        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("message_start", MessageStartData);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns null for content_block_start.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_ContentBlockStart_ShouldReturnNull()
    {
        // Arrange
        var data = """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""";

        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("content_block_start", data);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns null for content_block_stop.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_ContentBlockStop_ShouldReturnNull()
    {
        // Arrange
        var data = """{"type":"content_block_stop","index":0}""";

        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("content_block_stop", data);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns null for ping events.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_Ping_ShouldReturnNull()
    {
        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("ping", "{}");

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingEvent throws for error events.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_Error_ShouldThrowChatCompletionException()
    {
        // Act
        var act = () => AnthropicResponseParser.ParseStreamingEvent("error", StreamErrorData);

        // Assert
        act.Should().Throw<ChatCompletionException>()
            .Where(ex => ex.Message.Contains("overloaded"));
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns null for unknown event types.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_UnknownEventType_ShouldReturnNull()
    {
        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("unknown_event", "{}");

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns null for invalid JSON.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_WithInvalidJson_ShouldReturnNull()
    {
        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("content_block_delta", "not valid json");

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingEvent returns null for empty text delta.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_WithEmptyTextDelta_ShouldReturnNull()
    {
        // Arrange
        var data = """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":""}}""";

        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("content_block_delta", data);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingEvent handles delta without text field.
    /// </summary>
    [Fact]
    public void ParseStreamingEvent_DeltaWithoutText_ShouldReturnNull()
    {
        // Arrange
        var data = """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta"}}""";

        // Act
        var token = AnthropicResponseParser.ParseStreamingEvent("content_block_delta", data);

        // Assert
        token.Should().BeNull();
    }

    #endregion

    #region ParseErrorResponse Tests

    /// <summary>
    /// Tests that ParseErrorResponse returns AuthenticationException for 401.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With401_ShouldReturnAuthenticationException()
    {
        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.Unauthorized,
            AuthErrorResponse);

        // Assert
        exception.Should().BeOfType<AuthenticationException>();
        exception.Message.Should().Contain("Invalid API key");
        exception.ProviderName.Should().Be("Anthropic");
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns AuthenticationException for 403.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With403_ShouldReturnAuthenticationException()
    {
        // Arrange
        var forbiddenResponse = """{"error":{"type":"permission_error","message":"Access denied"}}""";

        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.Forbidden,
            forbiddenResponse);

        // Assert
        exception.Should().BeOfType<AuthenticationException>();
        exception.ProviderName.Should().Be("Anthropic");
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns RateLimitException for rate_limit_error.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_WithRateLimitError_ShouldReturnRateLimitException()
    {
        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.TooManyRequests,
            RateLimitResponse);

        // Assert
        exception.Should().BeOfType<RateLimitException>();
        exception.Message.Should().Contain("Rate limit");
        exception.ProviderName.Should().Be("Anthropic");
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns RateLimitException for 429 status.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With429_ShouldReturnRateLimitException()
    {
        // Arrange
        var genericRateLimit = """{"error":{"message":"Too many requests"}}""";

        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.TooManyRequests,
            genericRateLimit);

        // Assert
        exception.Should().BeOfType<RateLimitException>();
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns ChatCompletionException for overloaded_error.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_WithOverloadedError_ShouldReturnChatCompletionException()
    {
        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.ServiceUnavailable,
            OverloadedResponse);

        // Assert
        exception.Should().BeOfType<ChatCompletionException>();
        exception.Message.Should().Contain("overloaded");
        exception.ProviderName.Should().Be("Anthropic");
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns ChatCompletionException for invalid_request_error.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_WithInvalidRequestError_ShouldReturnChatCompletionException()
    {
        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.BadRequest,
            InvalidRequestResponse);

        // Assert
        exception.Should().BeOfType<ChatCompletionException>();
        exception.Message.Should().Contain("max_tokens");
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns ChatCompletionException for 500.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With500_ShouldReturnChatCompletionException()
    {
        // Arrange
        var serverError = """{"error":{"type":"server_error","message":"Internal server error"}}""";

        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.InternalServerError,
            serverError);

        // Assert
        exception.Should().BeOfType<ChatCompletionException>();
        exception.Message.Should().Contain("Internal server error");
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns ChatCompletionException for 404.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With404_ShouldReturnChatCompletionException()
    {
        // Arrange
        var notFound = """{"error":{"type":"not_found_error","message":"Model not found"}}""";

        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.NotFound,
            notFound);

        // Assert
        exception.Should().BeOfType<ChatCompletionException>();
        exception.Message.Should().Contain("Model not found");
    }

    /// <summary>
    /// Tests that ParseErrorResponse handles invalid JSON in error response.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_WithInvalidJson_ShouldUseRawBody()
    {
        // Arrange
        var invalidJson = "Not valid JSON response";

        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.BadRequest,
            invalidJson);

        // Assert
        exception.Message.Should().Contain("Not valid JSON response");
    }

    /// <summary>
    /// Tests that ParseErrorResponse handles 503 Service Unavailable.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With503_ShouldReturnChatCompletionException()
    {
        // Arrange
        var unavailable = """{"error":{"message":"Service temporarily unavailable"}}""";

        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.ServiceUnavailable,
            unavailable);

        // Assert
        exception.Should().BeOfType<ChatCompletionException>();
        exception.Message.Should().Contain("Service temporarily unavailable");
    }

    /// <summary>
    /// Tests that ParseErrorResponse handles empty error body.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_WithEmptyBody_ShouldProvideDefaultMessage()
    {
        // Act
        var exception = AnthropicResponseParser.ParseErrorResponse(
            HttpStatusCode.InternalServerError,
            "");

        // Assert
        exception.Should().BeOfType<ChatCompletionException>();
        exception.Message.Should().NotBeEmpty();
    }

    #endregion

    #region ExtractTokenUsage Tests

    /// <summary>
    /// Tests that ExtractTokenUsage extracts tokens correctly.
    /// </summary>
    [Fact]
    public void ExtractTokenUsage_WithValidResponse_ShouldExtractTokens()
    {
        // Act
        var (inputTokens, outputTokens) = AnthropicResponseParser.ExtractTokenUsage(SuccessResponse);

        // Assert
        inputTokens.Should().Be(15);
        outputTokens.Should().Be(10);
    }

    /// <summary>
    /// Tests that ExtractTokenUsage returns zeros for invalid JSON.
    /// </summary>
    [Fact]
    public void ExtractTokenUsage_WithInvalidJson_ShouldReturnZeros()
    {
        // Act
        var (inputTokens, outputTokens) = AnthropicResponseParser.ExtractTokenUsage("not valid json");

        // Assert
        inputTokens.Should().Be(0);
        outputTokens.Should().Be(0);
    }

    /// <summary>
    /// Tests that ExtractTokenUsage returns zeros for missing usage field.
    /// </summary>
    [Fact]
    public void ExtractTokenUsage_WithMissingUsage_ShouldReturnZeros()
    {
        // Arrange
        var noUsage = """{"id":"msg_test","content":[]}""";

        // Act
        var (inputTokens, outputTokens) = AnthropicResponseParser.ExtractTokenUsage(noUsage);

        // Assert
        inputTokens.Should().Be(0);
        outputTokens.Should().Be(0);
    }

    /// <summary>
    /// Tests that ExtractTokenUsage handles partial usage data.
    /// </summary>
    [Fact]
    public void ExtractTokenUsage_WithPartialUsage_ShouldExtractAvailableTokens()
    {
        // Arrange
        var partialUsage = """{"usage":{"input_tokens":10}}""";

        // Act
        var (inputTokens, outputTokens) = AnthropicResponseParser.ExtractTokenUsage(partialUsage);

        // Assert
        inputTokens.Should().Be(10);
        outputTokens.Should().Be(0);
    }

    #endregion
}
