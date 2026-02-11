// -----------------------------------------------------------------------
// <copyright file="AgentExceptionTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Agents.Resilience;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Resilience;

/// <summary>
/// Unit tests for the <see cref="AgentException"/> hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate:
/// </para>
/// <list type="bullet">
///   <item><description>Base <see cref="AgentException"/> properties and defaults</description></item>
///   <item><description><see cref="AgentRateLimitException"/> with retry timing and queue position</description></item>
///   <item><description><see cref="AgentAuthenticationException"/> non-recoverable behavior</description></item>
///   <item><description><see cref="ProviderUnavailableException"/> with estimated recovery</description></item>
///   <item><description><see cref="InvalidResponseException"/> retry strategy</description></item>
///   <item><description><see cref="TokenLimitException"/> with token count properties</description></item>
///   <item><description><see cref="ContextAssemblyException"/> defaults</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.8d")]
public class AgentExceptionTests
{
    #region AgentException Base Tests

    /// <summary>
    /// Verifies that AgentException stores the user message correctly.
    /// </summary>
    [Fact]
    public void AgentException_UserMessage_StoredCorrectly()
    {
        // Arrange & Act
        var ex = new AgentException("Something went wrong.");

        // Assert
        ex.UserMessage.Should().Be("Something went wrong.");
        ex.Message.Should().Be("Something went wrong.");
    }

    /// <summary>
    /// Verifies that AgentException defaults to recoverable.
    /// </summary>
    [Fact]
    public void AgentException_IsRecoverable_DefaultsToTrue()
    {
        // Act
        var ex = new AgentException("Error");

        // Assert
        ex.IsRecoverable.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that AgentException defaults to Retry strategy.
    /// </summary>
    [Fact]
    public void AgentException_Strategy_DefaultsToRetry()
    {
        // Act
        var ex = new AgentException("Error");

        // Assert
        ex.Strategy.Should().Be(RecoveryStrategy.Retry);
    }

    /// <summary>
    /// Verifies that AgentException wraps inner exception.
    /// </summary>
    [Fact]
    public void AgentException_InnerException_WrappedCorrectly()
    {
        // Arrange
        var inner = new InvalidOperationException("Root cause");

        // Act
        var ex = new AgentException("Wrapped error", inner);

        // Assert
        ex.InnerException.Should().BeSameAs(inner);
    }

    /// <summary>
    /// Verifies that TechnicalDetails can be set via init.
    /// </summary>
    [Fact]
    public void AgentException_TechnicalDetails_CanBeSetViaInit()
    {
        // Act
        var ex = new AgentException("Error")
        {
            TechnicalDetails = "HTTP 503 from openai"
        };

        // Assert
        ex.TechnicalDetails.Should().Be("HTTP 503 from openai");
    }

    /// <summary>
    /// Verifies that TechnicalDetails defaults to null.
    /// </summary>
    [Fact]
    public void AgentException_TechnicalDetails_DefaultsToNull()
    {
        // Act
        var ex = new AgentException("Error");

        // Assert
        ex.TechnicalDetails.Should().BeNull();
    }

    #endregion

    #region ProviderException Tests

    /// <summary>
    /// Verifies that ProviderException stores the provider name.
    /// </summary>
    [Fact]
    public void ProviderException_Provider_StoredCorrectly()
    {
        // Act
        var ex = new ProviderException("openai", "Provider error");

        // Assert
        ex.Provider.Should().Be("openai");
        ex.UserMessage.Should().Be("Provider error");
    }

    /// <summary>
    /// Verifies that ProviderException wraps inner exception.
    /// </summary>
    [Fact]
    public void ProviderException_InnerException_WrappedCorrectly()
    {
        // Arrange
        var inner = new HttpRequestException("Connection refused");

        // Act
        var ex = new ProviderException("lmstudio", "Cannot connect", inner);

        // Assert
        ex.InnerException.Should().BeSameAs(inner);
        ex.Provider.Should().Be("lmstudio");
    }

    #endregion

    #region AgentRateLimitException Tests

    /// <summary>
    /// Verifies that rate limit exception stores retry-after duration.
    /// </summary>
    [Fact]
    public void AgentRateLimitException_RetryAfter_StoredCorrectly()
    {
        // Act
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(30));

        // Assert
        ex.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Verifies that rate limit exception has Queue strategy.
    /// </summary>
    [Fact]
    public void AgentRateLimitException_Strategy_IsQueue()
    {
        // Act
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(10));

        // Assert
        ex.Strategy.Should().Be(RecoveryStrategy.Queue);
    }

    /// <summary>
    /// Verifies that rate limit exception generates user-friendly message.
    /// </summary>
    [Fact]
    public void AgentRateLimitException_UserMessage_IncludesWaitTime()
    {
        // Act
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(30));

        // Assert
        ex.UserMessage.Should().Contain("30");
        ex.UserMessage.Should().Contain("seconds");
    }

    /// <summary>
    /// Verifies that rate limit exception stores queue position.
    /// </summary>
    [Fact]
    public void AgentRateLimitException_QueuePosition_DefaultsToZero()
    {
        // Act
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(10));

        // Assert
        ex.QueuePosition.Should().Be(0);
    }

    /// <summary>
    /// Verifies that rate limit exception queue position can be set.
    /// </summary>
    [Fact]
    public void AgentRateLimitException_QueuePosition_CanBeSet()
    {
        // Act
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(10), queuePosition: 5);

        // Assert
        ex.QueuePosition.Should().Be(5);
    }

    /// <summary>
    /// Verifies that rate limit exception is recoverable.
    /// </summary>
    [Fact]
    public void AgentRateLimitException_IsRecoverable_IsTrue()
    {
        // Act
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(10));

        // Assert
        ex.IsRecoverable.Should().BeTrue();
    }

    #endregion

    #region AgentAuthenticationException Tests

    /// <summary>
    /// Verifies that authentication exception is NOT recoverable.
    /// </summary>
    [Fact]
    public void AgentAuthenticationException_IsRecoverable_IsFalse()
    {
        // Act
        var ex = new AgentAuthenticationException("openai");

        // Assert
        ex.IsRecoverable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that authentication exception has None strategy.
    /// </summary>
    [Fact]
    public void AgentAuthenticationException_Strategy_IsNone()
    {
        // Act
        var ex = new AgentAuthenticationException("openai");

        // Assert
        ex.Strategy.Should().Be(RecoveryStrategy.None);
    }

    /// <summary>
    /// Verifies that authentication exception has default message.
    /// </summary>
    [Fact]
    public void AgentAuthenticationException_DefaultMessage_PointsToSettings()
    {
        // Act
        var ex = new AgentAuthenticationException("openai");

        // Assert
        ex.UserMessage.Should().Contain("Settings");
        ex.UserMessage.Should().Contain("API key");
    }

    /// <summary>
    /// Verifies that authentication exception accepts custom message.
    /// </summary>
    [Fact]
    public void AgentAuthenticationException_CustomMessage_Override()
    {
        // Act
        var ex = new AgentAuthenticationException("openai", "Custom auth error");

        // Assert
        ex.UserMessage.Should().Be("Custom auth error");
    }

    #endregion

    #region ProviderUnavailableException Tests

    /// <summary>
    /// Verifies that unavailable exception stores estimated recovery time.
    /// </summary>
    [Fact]
    public void ProviderUnavailableException_EstimatedRecovery_StoredCorrectly()
    {
        // Act
        var ex = new ProviderUnavailableException("openai", TimeSpan.FromSeconds(15));

        // Assert
        ex.EstimatedRecovery.Should().Be(TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Verifies that unavailable exception has Retry strategy.
    /// </summary>
    [Fact]
    public void ProviderUnavailableException_Strategy_IsRetry()
    {
        // Act
        var ex = new ProviderUnavailableException("openai");

        // Assert
        ex.Strategy.Should().Be(RecoveryStrategy.Retry);
    }

    /// <summary>
    /// Verifies that unavailable exception message includes recovery time.
    /// </summary>
    [Fact]
    public void ProviderUnavailableException_WithRecovery_MessageIncludesTime()
    {
        // Act
        var ex = new ProviderUnavailableException("openai", TimeSpan.FromSeconds(15));

        // Assert
        ex.UserMessage.Should().Contain("15");
    }

    /// <summary>
    /// Verifies that unavailable exception without recovery has generic message.
    /// </summary>
    [Fact]
    public void ProviderUnavailableException_WithoutRecovery_GenericMessage()
    {
        // Act
        var ex = new ProviderUnavailableException("openai");

        // Assert
        ex.UserMessage.Should().Contain("temporarily unavailable");
        ex.EstimatedRecovery.Should().BeNull();
    }

    #endregion

    #region InvalidResponseException Tests

    /// <summary>
    /// Verifies that invalid response exception has Retry strategy.
    /// </summary>
    [Fact]
    public void InvalidResponseException_Strategy_IsRetry()
    {
        // Act
        var ex = new InvalidResponseException("openai");

        // Assert
        ex.Strategy.Should().Be(RecoveryStrategy.Retry);
    }

    /// <summary>
    /// Verifies that invalid response exception has default message.
    /// </summary>
    [Fact]
    public void InvalidResponseException_DefaultMessage_IsRetryMessage()
    {
        // Act
        var ex = new InvalidResponseException("openai");

        // Assert
        ex.UserMessage.Should().Contain("invalid response");
    }

    /// <summary>
    /// Verifies that invalid response exception is recoverable.
    /// </summary>
    [Fact]
    public void InvalidResponseException_IsRecoverable_IsTrue()
    {
        // Act
        var ex = new InvalidResponseException("openai");

        // Assert
        ex.IsRecoverable.Should().BeTrue();
    }

    #endregion

    #region TokenLimitException Tests

    /// <summary>
    /// Verifies that token limit exception stores token counts.
    /// </summary>
    [Fact]
    public void TokenLimitException_TokenCounts_StoredCorrectly()
    {
        // Act
        var ex = new TokenLimitException(12000, 8192, 7500);

        // Assert
        ex.RequestedTokens.Should().Be(12000);
        ex.MaxTokens.Should().Be(8192);
        ex.TruncatedTo.Should().Be(7500);
    }

    /// <summary>
    /// Verifies that token limit exception has Truncate strategy.
    /// </summary>
    [Fact]
    public void TokenLimitException_Strategy_IsTruncate()
    {
        // Act
        var ex = new TokenLimitException(12000, 8192, 7500);

        // Assert
        ex.Strategy.Should().Be(RecoveryStrategy.Truncate);
    }

    /// <summary>
    /// Verifies that token limit exception message includes token counts.
    /// </summary>
    [Fact]
    public void TokenLimitException_UserMessage_IncludesTokenCounts()
    {
        // Act
        var ex = new TokenLimitException(12000, 8192, 7500);

        // Assert
        ex.UserMessage.Should().Contain("12,000");
        ex.UserMessage.Should().Contain("7,500");
    }

    /// <summary>
    /// Verifies that token limit exception is recoverable.
    /// </summary>
    [Fact]
    public void TokenLimitException_IsRecoverable_IsTrue()
    {
        // Act
        var ex = new TokenLimitException(12000, 8192, 7500);

        // Assert
        ex.IsRecoverable.Should().BeTrue();
    }

    #endregion

    #region ContextAssemblyException Tests

    /// <summary>
    /// Verifies that context assembly exception has default message.
    /// </summary>
    [Fact]
    public void ContextAssemblyException_DefaultMessage_IsContextError()
    {
        // Act
        var ex = new ContextAssemblyException();

        // Assert
        ex.UserMessage.Should().Contain("context");
    }

    /// <summary>
    /// Verifies that context assembly exception accepts custom message.
    /// </summary>
    [Fact]
    public void ContextAssemblyException_CustomMessage_Override()
    {
        // Act
        var ex = new ContextAssemblyException("Custom context error");

        // Assert
        ex.UserMessage.Should().Be("Custom context error");
    }

    /// <summary>
    /// Verifies that context assembly exception has Retry strategy.
    /// </summary>
    [Fact]
    public void ContextAssemblyException_Strategy_IsRetry()
    {
        // Act
        var ex = new ContextAssemblyException();

        // Assert
        ex.Strategy.Should().Be(RecoveryStrategy.Retry);
    }

    /// <summary>
    /// Verifies that context assembly exception is recoverable.
    /// </summary>
    [Fact]
    public void ContextAssemblyException_IsRecoverable_IsTrue()
    {
        // Act
        var ex = new ContextAssemblyException();

        // Assert
        ex.IsRecoverable.Should().BeTrue();
    }

    #endregion

    #region RecoveryStrategy Enum Tests

    /// <summary>
    /// Verifies that RecoveryStrategy enum has all expected values.
    /// </summary>
    [Fact]
    public void RecoveryStrategy_HasAllExpectedValues()
    {
        // Assert
        Enum.GetValues<RecoveryStrategy>().Should().HaveCount(5);
        Enum.IsDefined(RecoveryStrategy.Retry).Should().BeTrue();
        Enum.IsDefined(RecoveryStrategy.Queue).Should().BeTrue();
        Enum.IsDefined(RecoveryStrategy.Truncate).Should().BeTrue();
        Enum.IsDefined(RecoveryStrategy.Fallback).Should().BeTrue();
        Enum.IsDefined(RecoveryStrategy.None).Should().BeTrue();
    }

    #endregion

    #region AgentErrorEvent Tests

    /// <summary>
    /// Verifies that AgentErrorEvent record creates correctly.
    /// </summary>
    [Fact]
    public void AgentErrorEvent_CreatesWithAllProperties()
    {
        // Act
        var timestamp = DateTimeOffset.UtcNow;
        var evt = new AgentErrorEvent(
            ErrorType: "AgentRateLimitException",
            UserMessage: "Rate limit exceeded.",
            TechnicalDetails: "HTTP 429",
            WasRecovered: true,
            Timestamp: timestamp);

        // Assert
        evt.ErrorType.Should().Be("AgentRateLimitException");
        evt.UserMessage.Should().Be("Rate limit exceeded.");
        evt.TechnicalDetails.Should().Be("HTTP 429");
        evt.WasRecovered.Should().BeTrue();
        evt.Timestamp.Should().Be(timestamp);
    }

    /// <summary>
    /// Verifies that AgentErrorEvent supports null technical details.
    /// </summary>
    [Fact]
    public void AgentErrorEvent_NullTechnicalDetails_IsValid()
    {
        // Act
        var evt = new AgentErrorEvent(
            ErrorType: "AgentException",
            UserMessage: "Error",
            TechnicalDetails: null,
            WasRecovered: false,
            Timestamp: DateTimeOffset.UtcNow);

        // Assert
        evt.TechnicalDetails.Should().BeNull();
    }

    #endregion

    #region RateLimitStatusEventArgs Tests

    /// <summary>
    /// Verifies that RateLimitStatusEventArgs stores properties correctly.
    /// </summary>
    [Fact]
    public void RateLimitStatusEventArgs_CreatesCorrectly()
    {
        // Act
        var args = new RateLimitStatusEventArgs(
            IsRateLimited: true,
            EstimatedWait: TimeSpan.FromSeconds(30),
            QueueDepth: 5);

        // Assert
        args.IsRateLimited.Should().BeTrue();
        args.EstimatedWait.Should().Be(TimeSpan.FromSeconds(30));
        args.QueueDepth.Should().Be(5);
    }

    #endregion
}
