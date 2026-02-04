// -----------------------------------------------------------------------
// <copyright file="OpenAIResponseParserTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Providers.OpenAI;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Providers.OpenAI;

/// <summary>
/// Unit tests for <see cref="OpenAIResponseParser"/>.
/// </summary>
public class OpenAIResponseParserTests
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
                    "content": "Test response content"
                },
                "finish_reason": "stop",
                "index": 0
            }]
        }
        """;

    /// <summary>
    /// Sample rate limit error response JSON.
    /// </summary>
    private const string RateLimitResponse = """
        {
            "error": {
                "message": "Rate limit reached. Please retry after 30 seconds.",
                "type": "rate_limit_error",
                "code": "rate_limit_exceeded"
            }
        }
        """;

    /// <summary>
    /// Sample authentication error response JSON.
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
    /// Sample streaming chunk with content.
    /// </summary>
    private const string StreamingChunkWithContent = """
        {
            "id": "chatcmpl-test123",
            "object": "chat.completion.chunk",
            "created": 1677858242,
            "model": "gpt-4o-mini",
            "choices": [{
                "index": 0,
                "delta": {
                    "content": "Hello"
                },
                "finish_reason": null
            }]
        }
        """;

    /// <summary>
    /// Sample streaming chunk with finish reason.
    /// </summary>
    private const string StreamingChunkWithFinishReason = """
        {
            "id": "chatcmpl-test123",
            "object": "chat.completion.chunk",
            "created": 1677858242,
            "model": "gpt-4o-mini",
            "choices": [{
                "index": 0,
                "delta": {},
                "finish_reason": "stop"
            }]
        }
        """;

    /// <summary>
    /// Sample streaming chunk with empty delta (role only).
    /// </summary>
    private const string StreamingChunkWithRoleOnly = """
        {
            "id": "chatcmpl-test123",
            "object": "chat.completion.chunk",
            "created": 1677858242,
            "model": "gpt-4o-mini",
            "choices": [{
                "index": 0,
                "delta": {
                    "role": "assistant"
                },
                "finish_reason": null
            }]
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
        var response = OpenAIResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.Content.Should().Be("Test response content");
    }

    /// <summary>
    /// Tests that ParseSuccessResponse extracts prompt tokens correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_ShouldExtractPromptTokens()
    {
        // Act
        var response = OpenAIResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.PromptTokens.Should().Be(10);
    }

    /// <summary>
    /// Tests that ParseSuccessResponse extracts completion tokens correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_ShouldExtractCompletionTokens()
    {
        // Act
        var response = OpenAIResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.CompletionTokens.Should().Be(5);
    }

    /// <summary>
    /// Tests that ParseSuccessResponse extracts finish reason correctly.
    /// </summary>
    [Fact]
    public void ParseSuccessResponse_ShouldExtractFinishReason()
    {
        // Act
        var response = OpenAIResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.FinishReason.Should().Be("stop");
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
        var response = OpenAIResponseParser.ParseSuccessResponse(SuccessResponse, duration);

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
        var response = OpenAIResponseParser.ParseSuccessResponse(
            SuccessResponse,
            TimeSpan.FromMilliseconds(100));

        // Assert
        response.TotalTokens.Should().Be(15);
    }

    #endregion

    #region ParseStreamingChunk Tests

    /// <summary>
    /// Tests that ParseStreamingChunk extracts content correctly.
    /// </summary>
    [Fact]
    public void ParseStreamingChunk_WithContent_ShouldReturnToken()
    {
        // Act
        var token = OpenAIResponseParser.ParseStreamingChunk(StreamingChunkWithContent);

        // Assert
        token.Should().NotBeNull();
        token!.Token.Should().Be("Hello");
        token.IsComplete.Should().BeFalse();
    }

    /// <summary>
    /// Tests that ParseStreamingChunk returns complete token with finish reason.
    /// </summary>
    [Fact]
    public void ParseStreamingChunk_WithFinishReason_ShouldReturnCompleteToken()
    {
        // Act
        var token = OpenAIResponseParser.ParseStreamingChunk(StreamingChunkWithFinishReason);

        // Assert
        token.Should().NotBeNull();
        token!.IsComplete.Should().BeTrue();
        token.FinishReason.Should().Be("stop");
    }

    /// <summary>
    /// Tests that ParseStreamingChunk returns null for role-only delta.
    /// </summary>
    [Fact]
    public void ParseStreamingChunk_WithRoleOnly_ShouldReturnNull()
    {
        // Act
        var token = OpenAIResponseParser.ParseStreamingChunk(StreamingChunkWithRoleOnly);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingChunk returns null for empty choices.
    /// </summary>
    [Fact]
    public void ParseStreamingChunk_WithEmptyChoices_ShouldReturnNull()
    {
        // Arrange
        var emptyChoices = """{"choices":[]}""";

        // Act
        var token = OpenAIResponseParser.ParseStreamingChunk(emptyChoices);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingChunk returns null for invalid JSON.
    /// </summary>
    [Fact]
    public void ParseStreamingChunk_WithInvalidJson_ShouldReturnNull()
    {
        // Act
        var token = OpenAIResponseParser.ParseStreamingChunk("not valid json");

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// Tests that ParseStreamingChunk handles empty content string.
    /// </summary>
    [Fact]
    public void ParseStreamingChunk_WithEmptyContent_ShouldReturnNull()
    {
        // Arrange
        var emptyContent = """
            {
                "choices": [{
                    "index": 0,
                    "delta": { "content": "" },
                    "finish_reason": null
                }]
            }
            """;

        // Act
        var token = OpenAIResponseParser.ParseStreamingChunk(emptyContent);

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
        var exception = OpenAIResponseParser.ParseErrorResponse(
            HttpStatusCode.Unauthorized,
            AuthErrorResponse);

        // Assert
        exception.Should().BeOfType<AuthenticationException>();
        exception.Message.Should().Contain("Incorrect API key");
        exception.ProviderName.Should().Be("OpenAI");
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns RateLimitException for 429.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With429_ShouldReturnRateLimitException()
    {
        // Act
        var exception = OpenAIResponseParser.ParseErrorResponse(
            HttpStatusCode.TooManyRequests,
            RateLimitResponse);

        // Assert
        exception.Should().BeOfType<RateLimitException>();
        exception.Message.Should().Contain("Rate limit");
        exception.ProviderName.Should().Be("OpenAI");
    }

    /// <summary>
    /// Tests that ParseErrorResponse uses retry-after header when provided.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With429AndRetryAfter_ShouldIncludeRetryAfter()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(60);

        // Act
        var exception = OpenAIResponseParser.ParseErrorResponse(
            HttpStatusCode.TooManyRequests,
            RateLimitResponse,
            retryAfter);

        // Assert
        exception.Should().BeOfType<RateLimitException>();
        var rateLimitEx = (RateLimitException)exception;
        rateLimitEx.RetryAfter.Should().Be(retryAfter);
    }

    /// <summary>
    /// Tests that ParseErrorResponse returns ChatCompletionException for 500.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_With500_ShouldReturnChatCompletionException()
    {
        // Arrange
        var serverError = """{"error":{"message":"Internal server error","type":"server_error"}}""";

        // Act
        var exception = OpenAIResponseParser.ParseErrorResponse(
            HttpStatusCode.InternalServerError,
            serverError);

        // Assert
        exception.Should().BeOfType<ChatCompletionException>();
        exception.Message.Should().Contain("Internal server error");
        exception.ProviderName.Should().Be("OpenAI");
    }

    /// <summary>
    /// Tests that ParseErrorResponse extracts error message from JSON.
    /// </summary>
    [Fact]
    public void ParseErrorResponse_ShouldExtractErrorMessage()
    {
        // Arrange
        var customError = """{"error":{"message":"Custom error message","type":"custom_error"}}""";

        // Act
        var exception = OpenAIResponseParser.ParseErrorResponse(
            HttpStatusCode.BadRequest,
            customError);

        // Assert
        exception.Message.Should().Contain("Custom error message");
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
        var exception = OpenAIResponseParser.ParseErrorResponse(
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
        var exception = OpenAIResponseParser.ParseErrorResponse(
            HttpStatusCode.ServiceUnavailable,
            unavailable);

        // Assert
        exception.Should().BeOfType<ChatCompletionException>();
        exception.Message.Should().Contain("Service temporarily unavailable");
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
        var (promptTokens, completionTokens) = OpenAIResponseParser.ExtractTokenUsage(SuccessResponse);

        // Assert
        promptTokens.Should().Be(10);
        completionTokens.Should().Be(5);
    }

    /// <summary>
    /// Tests that ExtractTokenUsage returns zeros for invalid JSON.
    /// </summary>
    [Fact]
    public void ExtractTokenUsage_WithInvalidJson_ShouldReturnZeros()
    {
        // Act
        var (promptTokens, completionTokens) = OpenAIResponseParser.ExtractTokenUsage("not valid json");

        // Assert
        promptTokens.Should().Be(0);
        completionTokens.Should().Be(0);
    }

    /// <summary>
    /// Tests that ExtractTokenUsage returns zeros for missing usage field.
    /// </summary>
    [Fact]
    public void ExtractTokenUsage_WithMissingUsage_ShouldReturnZeros()
    {
        // Arrange
        var noUsage = """{"id":"test","choices":[]}""";

        // Act
        var (promptTokens, completionTokens) = OpenAIResponseParser.ExtractTokenUsage(noUsage);

        // Assert
        promptTokens.Should().Be(0);
        completionTokens.Should().Be(0);
    }

    #endregion
}
