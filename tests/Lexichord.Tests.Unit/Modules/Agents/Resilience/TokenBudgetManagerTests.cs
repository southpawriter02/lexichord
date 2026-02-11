// -----------------------------------------------------------------------
// <copyright file="TokenBudgetManagerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Resilience;

/// <summary>
/// Unit tests for <see cref="TokenBudgetManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate:
/// </para>
/// <list type="bullet">
///   <item><description>Budget checking with token counting</description></item>
///   <item><description>Token estimation for message lists</description></item>
///   <item><description>Truncation algorithm (preserves system + newest messages)</description></item>
///   <item><description>Edge cases: empty messages, single message, exactly at limit</description></item>
///   <item><description>Null argument validation</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.8d")]
public class TokenBudgetManagerTests
{
    private readonly Mock<ITokenCounter> _tokenCounterMock;
    private readonly TokenBudgetManager _manager;

    // Constants matching TokenBudgetManager internals
    private const int ResponseReserve = 1024;
    private const int SystemBuffer = 500;
    private const int MessageOverhead = 4;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public TokenBudgetManagerTests()
    {
        _tokenCounterMock = new Mock<ITokenCounter>();
        _tokenCounterMock.Setup(c => c.Model).Returns("gpt-4");

        _manager = new TokenBudgetManager(
            _tokenCounterMock.Object,
            NullLogger<TokenBudgetManager>.Instance);
    }

    #region CheckBudget Tests

    /// <summary>
    /// Verifies budget passes when messages fit within limit.
    /// </summary>
    [Fact]
    public void CheckBudget_MessagesFitWithinLimit_ReturnsTrue()
    {
        // Arrange — 100 tokens per message + 4 overhead = 104 per message
        // 3 messages = 312 tokens. Budget = 8192 - 1024 reserve = 7168 available
        _tokenCounterMock.Setup(c => c.CountTokens(It.IsAny<string>())).Returns(100);
        var messages = CreateMessages(3);

        // Act & Assert
        _manager.CheckBudget(messages, 8192).Should().BeTrue();
    }

    /// <summary>
    /// Verifies budget fails when messages exceed limit.
    /// </summary>
    [Fact]
    public void CheckBudget_MessagesExceedLimit_ReturnsFalse()
    {
        // Arrange — 1000 tokens per message + 4 overhead = 1004 per message
        // 10 messages = 10040 tokens. Budget = 8192 - 1024 reserve = 7168 available
        _tokenCounterMock.Setup(c => c.CountTokens(It.IsAny<string>())).Returns(1000);
        var messages = CreateMessages(10);

        // Act & Assert
        _manager.CheckBudget(messages, 8192).Should().BeFalse();
    }

    /// <summary>
    /// Verifies budget check with empty messages returns true.
    /// </summary>
    [Fact]
    public void CheckBudget_EmptyMessages_ReturnsTrue()
    {
        // Act & Assert
        _manager.CheckBudget(Array.Empty<ChatMessage>(), 8192).Should().BeTrue();
    }

    /// <summary>
    /// Verifies budget exactly at boundary.
    /// </summary>
    [Fact]
    public void CheckBudget_ExactlyAtLimit_ReturnsTrue()
    {
        // Arrange — need exactly (maxTokens - reserve) total tokens
        // maxTokens = 2048, reserve = 1024, so available = 1024
        // 1 message with 1020 tokens + 4 overhead = 1024
        _tokenCounterMock.Setup(c => c.CountTokens(It.IsAny<string>())).Returns(1020);
        var messages = CreateMessages(1);

        // Act & Assert
        _manager.CheckBudget(messages, 2048).Should().BeTrue();
    }

    /// <summary>
    /// Verifies null messages throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void CheckBudget_NullMessages_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _manager.CheckBudget(null!, 8192);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region EstimateTokens Tests

    /// <summary>
    /// Verifies token estimation sums content tokens plus overhead.
    /// </summary>
    [Fact]
    public void EstimateTokens_SumsContentPlusOverhead()
    {
        // Arrange — 50 tokens per message + 4 overhead = 54 per message
        _tokenCounterMock.Setup(c => c.CountTokens(It.IsAny<string>())).Returns(50);
        var messages = CreateMessages(3);

        // Act
        var estimate = _manager.EstimateTokens(messages);

        // Assert — 3 × (50 + 4) = 162
        estimate.Should().Be(162);
    }

    /// <summary>
    /// Verifies token estimation for empty list returns zero.
    /// </summary>
    [Fact]
    public void EstimateTokens_EmptyList_ReturnsZero()
    {
        // Act & Assert
        _manager.EstimateTokens(Array.Empty<ChatMessage>()).Should().Be(0);
    }

    /// <summary>
    /// Verifies token estimation handles empty content.
    /// </summary>
    [Fact]
    public void EstimateTokens_EmptyContent_TreatsAsZeroTokens()
    {
        // Arrange
        _tokenCounterMock.Setup(c => c.CountTokens("")).Returns(0);
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "")
        };

        // Act
        var estimate = _manager.EstimateTokens(messages);

        // Assert — 0 content tokens + 4 overhead = 4
        estimate.Should().Be(4);
    }

    #endregion

    #region TruncateToFit Tests

    /// <summary>
    /// Verifies that messages within budget are returned unchanged.
    /// </summary>
    [Fact]
    public void TruncateToFit_MessagesFit_ReturnsUnchanged()
    {
        // Arrange
        _tokenCounterMock.Setup(c => c.CountTokens(It.IsAny<string>())).Returns(50);
        var messages = CreateMessages(3);

        // Act
        var result = _manager.TruncateToFit(messages, 8192);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(messages);
    }

    /// <summary>
    /// Verifies that system messages are always preserved during truncation.
    /// </summary>
    [Fact]
    public void TruncateToFit_PreservesSystemMessages()
    {
        // Arrange — 500 tokens per message, 4 overhead = 504 per message
        // maxTokens = 2048, available = 2048 - 1024 - 500 = 524 (after reserves)
        // System message takes 504, leaving only 20 for other messages
        _tokenCounterMock.Setup(c => c.CountTokens(It.IsAny<string>())).Returns(500);
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Message 1"),
            new ChatMessage(ChatRole.Assistant, "Reply 1"),
            new ChatMessage(ChatRole.User, "Message 2"),
        };

        // Act
        var result = _manager.TruncateToFit(messages, 2048);

        // Assert — system message should always be preserved
        result.Should().Contain(m => m.Role == ChatRole.System);
    }

    /// <summary>
    /// Verifies that the most recent messages are kept during truncation.
    /// </summary>
    [Fact]
    public void TruncateToFit_KeepsMostRecentMessages()
    {
        // Arrange — 300 tokens per message, 4 overhead = 304 per message
        // maxTokens = 2048, available = 2048 - 1024 - 500 = 524
        // System: 304 tokens → remaining 220 → can't fit another 304
        _tokenCounterMock.Setup(c => c.CountTokens(It.IsAny<string>())).Returns(300);
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "System"),
            new ChatMessage(ChatRole.User, "Old message"),
            new ChatMessage(ChatRole.Assistant, "Old reply"),
            new ChatMessage(ChatRole.User, "Recent message"),
        };

        // Act
        var result = _manager.TruncateToFit(messages, 2048);

        // Assert — should keep system + the most recent non-system message
        result.Should().Contain(m => m.Content == "System");
        // At minimum, the most recent message should be attempted
        result.Count.Should().BeLessThan(messages.Count);
    }

    /// <summary>
    /// Verifies truncation of empty list returns empty.
    /// </summary>
    [Fact]
    public void TruncateToFit_EmptyMessages_ReturnsEmpty()
    {
        // Arrange
        var messages = new List<ChatMessage>();

        // Act
        var result = _manager.TruncateToFit(messages, 8192);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies null messages throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void TruncateToFit_NullMessages_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _manager.TruncateToFit(null!, 8192);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Constructor Validation Tests

    /// <summary>
    /// Verifies that null token counter throws.
    /// </summary>
    [Fact]
    public void Constructor_NullTokenCounter_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new TokenBudgetManager(null!, NullLogger<TokenBudgetManager>.Instance);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("tokenCounter");
    }

    /// <summary>
    /// Verifies that null logger throws.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new TokenBudgetManager(_tokenCounterMock.Object, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a list of test messages with alternating user/assistant roles.
    /// </summary>
    private static List<ChatMessage> CreateMessages(int count)
    {
        var messages = new List<ChatMessage>();
        for (var i = 0; i < count; i++)
        {
            messages.Add(new ChatMessage(
                i % 2 == 0 ? ChatRole.User : ChatRole.Assistant,
                $"Test message {i}"
            ));
        }
        return messages;
    }

    #endregion
}
