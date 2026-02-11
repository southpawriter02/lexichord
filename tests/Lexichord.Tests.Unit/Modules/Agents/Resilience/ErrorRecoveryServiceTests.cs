// -----------------------------------------------------------------------
// <copyright file="ErrorRecoveryServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Modules.Agents.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Resilience;

/// <summary>
/// Unit tests for <see cref="ErrorRecoveryService"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate:
/// </para>
/// <list type="bullet">
///   <item><description>Strategy mapping for all exception types</description></item>
///   <item><description>CanRecover logic for recoverable vs non-recoverable exceptions</description></item>
///   <item><description>User message generation including template formatting</description></item>
///   <item><description>AttemptRecoveryAsync behavior for different strategies</description></item>
///   <item><description>Null argument validation</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.8d")]
public class ErrorRecoveryServiceTests
{
    private readonly ErrorRecoveryService _service;
    private readonly Mock<ITokenBudgetManager> _tokenBudgetMock;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public ErrorRecoveryServiceTests()
    {
        _tokenBudgetMock = new Mock<ITokenBudgetManager>();
        _service = new ErrorRecoveryService(
            _tokenBudgetMock.Object,
            NullLogger<ErrorRecoveryService>.Instance);
    }

    #region CanRecover Tests

    /// <summary>
    /// Verifies that rate limit exception IS recoverable.
    /// </summary>
    [Fact]
    public void CanRecover_RateLimitException_ReturnsTrue()
    {
        // Arrange
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(30));

        // Act & Assert
        _service.CanRecover(ex).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that token limit exception IS recoverable.
    /// </summary>
    [Fact]
    public void CanRecover_TokenLimitException_ReturnsTrue()
    {
        // Arrange
        var ex = new TokenLimitException(12000, 8192, 7500);

        // Act & Assert
        _service.CanRecover(ex).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that authentication exception is NOT recoverable.
    /// </summary>
    [Fact]
    public void CanRecover_AuthenticationException_ReturnsFalse()
    {
        // Arrange
        var ex = new AgentAuthenticationException("openai");

        // Act & Assert
        _service.CanRecover(ex).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that provider unavailable exception IS recoverable.
    /// </summary>
    [Fact]
    public void CanRecover_ProviderUnavailableException_ReturnsTrue()
    {
        // Arrange
        var ex = new ProviderUnavailableException("openai", TimeSpan.FromSeconds(15));

        // Act & Assert
        _service.CanRecover(ex).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that invalid response exception IS recoverable.
    /// </summary>
    [Fact]
    public void CanRecover_InvalidResponseException_ReturnsTrue()
    {
        // Arrange
        var ex = new InvalidResponseException("openai");

        // Act & Assert
        _service.CanRecover(ex).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that null exception throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void CanRecover_NullException_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _service.CanRecover(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }

    #endregion

    #region GetStrategy Tests

    /// <summary>
    /// Verifies rate limit maps to Queue strategy.
    /// </summary>
    [Fact]
    public void GetStrategy_RateLimit_ReturnsQueue()
    {
        // Arrange
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(10));

        // Act & Assert
        _service.GetStrategy(ex).Should().Be(RecoveryStrategy.Queue);
    }

    /// <summary>
    /// Verifies token limit maps to Truncate strategy.
    /// </summary>
    [Fact]
    public void GetStrategy_TokenLimit_ReturnsTruncate()
    {
        // Arrange
        var ex = new TokenLimitException(12000, 8192, 7500);

        // Act & Assert
        _service.GetStrategy(ex).Should().Be(RecoveryStrategy.Truncate);
    }

    /// <summary>
    /// Verifies provider unavailable maps to Retry strategy.
    /// </summary>
    [Fact]
    public void GetStrategy_ProviderUnavailable_ReturnsRetry()
    {
        // Arrange
        var ex = new ProviderUnavailableException("openai");

        // Act & Assert
        _service.GetStrategy(ex).Should().Be(RecoveryStrategy.Retry);
    }

    /// <summary>
    /// Verifies authentication maps to None strategy.
    /// </summary>
    [Fact]
    public void GetStrategy_Authentication_ReturnsNone()
    {
        // Arrange
        var ex = new AgentAuthenticationException("openai");

        // Act & Assert
        _service.GetStrategy(ex).Should().Be(RecoveryStrategy.None);
    }

    /// <summary>
    /// Verifies invalid response maps to Retry strategy.
    /// </summary>
    [Fact]
    public void GetStrategy_InvalidResponse_ReturnsRetry()
    {
        // Arrange
        var ex = new InvalidResponseException("openai");

        // Act & Assert
        _service.GetStrategy(ex).Should().Be(RecoveryStrategy.Retry);
    }

    /// <summary>
    /// Verifies context assembly maps to Retry strategy.
    /// </summary>
    [Fact]
    public void GetStrategy_ContextAssembly_ReturnsRetry()
    {
        // Arrange
        var ex = new ContextAssemblyException();

        // Act & Assert
        _service.GetStrategy(ex).Should().Be(RecoveryStrategy.Retry);
    }

    /// <summary>
    /// Verifies unknown exception type defaults to None strategy.
    /// </summary>
    [Fact]
    public void GetStrategy_UnknownExceptionType_ReturnsNone()
    {
        // Arrange — base AgentException is not in the strategy map
        var ex = new AgentException("Unknown error");

        // Act & Assert
        _service.GetStrategy(ex).Should().Be(RecoveryStrategy.None);
    }

    /// <summary>
    /// Verifies null exception throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetStrategy_NullException_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _service.GetStrategy(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }

    #endregion

    #region GetUserMessage Tests

    /// <summary>
    /// Verifies rate limit message includes wait time.
    /// </summary>
    [Fact]
    public void GetUserMessage_RateLimit_IncludesWaitTime()
    {
        // Arrange
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(30));

        // Act
        var message = _service.GetUserMessage(ex);

        // Assert
        message.Should().Contain("30s");
    }

    /// <summary>
    /// Verifies authentication message mentions API key.
    /// </summary>
    [Fact]
    public void GetUserMessage_Authentication_MentionsApiKey()
    {
        // Arrange
        var ex = new AgentAuthenticationException("openai");

        // Act
        var message = _service.GetUserMessage(ex);

        // Assert
        message.Should().Contain("API key");
        message.Should().Contain("Settings");
    }

    /// <summary>
    /// Verifies token limit message mentions truncation.
    /// </summary>
    [Fact]
    public void GetUserMessage_TokenLimit_MentionsTruncation()
    {
        // Arrange
        var ex = new TokenLimitException(12000, 8192, 7500);

        // Act
        var message = _service.GetUserMessage(ex);

        // Assert
        message.Should().Contain("truncat", because: "should mention truncation");
    }

    /// <summary>
    /// Verifies unknown exception falls back to its own UserMessage.
    /// </summary>
    [Fact]
    public void GetUserMessage_UnknownType_FallsBackToExceptionUserMessage()
    {
        // Arrange
        var ex = new AgentException("Fallback message");

        // Act
        var message = _service.GetUserMessage(ex);

        // Assert
        message.Should().Be("Fallback message");
    }

    /// <summary>
    /// Verifies null exception throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetUserMessage_NullException_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _service.GetUserMessage(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }

    #endregion

    #region AttemptRecoveryAsync Tests

    /// <summary>
    /// Verifies truncate recovery returns null (signals caller to truncate and retry).
    /// </summary>
    [Fact]
    public async Task AttemptRecoveryAsync_TokenLimit_ReturnsNullForCallerRetry()
    {
        // Arrange
        var ex = new TokenLimitException(12000, 8192, 7500);
        var request = new AgentRequest("Test message");

        // Act
        var result = await _service.AttemptRecoveryAsync(ex, request, CancellationToken.None);

        // Assert — null signals the caller to apply truncation and retry
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies retry strategy recovery returns null (handled by Polly).
    /// </summary>
    [Fact]
    public async Task AttemptRecoveryAsync_ProviderUnavailable_ReturnsNull()
    {
        // Arrange
        var ex = new ProviderUnavailableException("openai");
        var request = new AgentRequest("Test message");

        // Act
        var result = await _service.AttemptRecoveryAsync(ex, request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies queue strategy recovery returns null (handled by RateLimitQueue).
    /// </summary>
    [Fact]
    public async Task AttemptRecoveryAsync_RateLimit_ReturnsNull()
    {
        // Arrange
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(30));
        var request = new AgentRequest("Test message");

        // Act
        var result = await _service.AttemptRecoveryAsync(ex, request, CancellationToken.None);

        // Assert — handled by IRateLimitQueue, not by recovery service
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies none strategy recovery returns null.
    /// </summary>
    [Fact]
    public async Task AttemptRecoveryAsync_Authentication_ReturnsNull()
    {
        // Arrange
        var ex = new AgentAuthenticationException("openai");
        var request = new AgentRequest("Test message");

        // Act
        var result = await _service.AttemptRecoveryAsync(ex, request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies null exception throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task AttemptRecoveryAsync_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new AgentRequest("Test");

        // Act
        var action = () => _service.AttemptRecoveryAsync(null!, request, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>().WithParameterName("exception");
    }

    /// <summary>
    /// Verifies null request throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task AttemptRecoveryAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var ex = new AgentRateLimitException("openai", TimeSpan.FromSeconds(10));

        // Act
        var action = () => _service.AttemptRecoveryAsync(ex, null!, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>().WithParameterName("originalRequest");
    }

    #endregion

    #region Constructor Validation Tests

    /// <summary>
    /// Verifies that null token budget manager throws.
    /// </summary>
    [Fact]
    public void Constructor_NullTokenBudget_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new ErrorRecoveryService(
            null!,
            NullLogger<ErrorRecoveryService>.Instance);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("tokenBudget");
    }

    /// <summary>
    /// Verifies that null logger throws.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new ErrorRecoveryService(
            _tokenBudgetMock.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion
}
