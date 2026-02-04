// -----------------------------------------------------------------------
// <copyright file="ChatCompletionExceptionTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for the chat completion exception hierarchy.
/// </summary>
public class ChatCompletionExceptionTests
{
    /// <summary>
    /// Tests ChatCompletionException default constructor.
    /// </summary>
    [Fact]
    public void ChatCompletionException_DefaultConstructor_ShouldCreateInstance()
    {
        // Act
        var exception = new ChatCompletionException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNullOrEmpty();
        exception.ProviderName.Should().BeNull();
    }

    /// <summary>
    /// Tests ChatCompletionException with message.
    /// </summary>
    [Fact]
    public void ChatCompletionException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        var exception = new ChatCompletionException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    /// <summary>
    /// Tests ChatCompletionException with message and provider.
    /// </summary>
    [Fact]
    public void ChatCompletionException_WithMessageAndProvider_ShouldSetBoth()
    {
        // Arrange
        const string message = "Test error";
        const string provider = "openai";

        // Act
        var exception = new ChatCompletionException(message, provider);

        // Assert
        exception.Message.Should().Be(message);
        exception.ProviderName.Should().Be(provider);
    }

    /// <summary>
    /// Tests ChatCompletionException with inner exception.
    /// </summary>
    [Fact]
    public void ChatCompletionException_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new ChatCompletionException("Outer error", inner);

        // Assert
        exception.InnerException.Should().Be(inner);
    }

    /// <summary>
    /// Tests AuthenticationException default constructor.
    /// </summary>
    [Fact]
    public void AuthenticationException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        // Act
        var exception = new AuthenticationException();

        // Assert
        exception.Message.Should().Contain("Authentication failed");
    }

    /// <summary>
    /// Tests AuthenticationException with provider.
    /// </summary>
    [Fact]
    public void AuthenticationException_WithProvider_ShouldSetProvider()
    {
        // Arrange
        const string provider = "anthropic";

        // Act
        var exception = new AuthenticationException("Auth failed", provider);

        // Assert
        exception.ProviderName.Should().Be(provider);
    }

    /// <summary>
    /// Tests RateLimitException default constructor.
    /// </summary>
    [Fact]
    public void RateLimitException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        // Act
        var exception = new RateLimitException();

        // Assert
        exception.Message.Should().Contain("Rate limit");
        exception.RetryAfter.Should().BeNull();
    }

    /// <summary>
    /// Tests RateLimitException with retry after.
    /// </summary>
    [Fact]
    public void RateLimitException_WithRetryAfter_ShouldSetRetryAfter()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(30);

        // Act
        var exception = new RateLimitException("Rate limited", retryAfter);

        // Assert
        exception.RetryAfter.Should().Be(retryAfter);
    }

    /// <summary>
    /// Tests RateLimitException with provider and retry after.
    /// </summary>
    [Fact]
    public void RateLimitException_WithProviderAndRetryAfter_ShouldSetBoth()
    {
        // Arrange
        var retryAfter = TimeSpan.FromMinutes(1);
        const string provider = "openai";

        // Act
        var exception = new RateLimitException("Rate limited", provider, retryAfter);

        // Assert
        exception.ProviderName.Should().Be(provider);
        exception.RetryAfter.Should().Be(retryAfter);
    }

    /// <summary>
    /// Tests ProviderNotConfiguredException default constructor.
    /// </summary>
    [Fact]
    public void ProviderNotConfiguredException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        // Act
        var exception = new ProviderNotConfiguredException();

        // Assert
        exception.Message.Should().Contain("not configured");
    }

    /// <summary>
    /// Tests ProviderNotConfiguredException with provider name only.
    /// </summary>
    [Fact]
    public void ProviderNotConfiguredException_WithProviderName_ShouldIncludeProviderInMessage()
    {
        // Arrange
        const string provider = "custom-provider";

        // Act
        var exception = new ProviderNotConfiguredException(provider);

        // Assert
        exception.Message.Should().Contain(provider);
        exception.ProviderName.Should().Be(provider);
    }

    /// <summary>
    /// Tests that exception hierarchy is correct.
    /// </summary>
    [Fact]
    public void ExceptionHierarchy_ShouldBeCorrect()
    {
        // Assert
        typeof(AuthenticationException).Should().BeDerivedFrom<ChatCompletionException>();
        typeof(RateLimitException).Should().BeDerivedFrom<ChatCompletionException>();
        typeof(ProviderNotConfiguredException).Should().BeDerivedFrom<ChatCompletionException>();
        typeof(ChatCompletionException).Should().BeDerivedFrom<Exception>();
    }

    /// <summary>
    /// Tests catching derived exceptions as base type.
    /// </summary>
    [Fact]
    public void CatchingDerivedAsBase_ShouldWork()
    {
        // Arrange
        ChatCompletionException? caught = null;

        // Act
        try
        {
            throw new AuthenticationException("Test", "provider");
        }
        catch (ChatCompletionException ex)
        {
            caught = ex;
        }

        // Assert
        caught.Should().NotBeNull();
        caught.Should().BeOfType<AuthenticationException>();
        caught!.ProviderName.Should().Be("provider");
    }
}
