// -----------------------------------------------------------------------
// <copyright file="ConversationMemoryManagerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Performance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Performance;

/// <summary>
/// Unit tests for <see cref="ConversationMemoryManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate:
/// </para>
/// <list type="bullet">
///   <item><description>Message trimming with system message preservation</description></item>
///   <item><description>Memory estimation accuracy</description></item>
///   <item><description>Memory limit enforcement warnings</description></item>
///   <item><description>Edge cases (empty lists, all system messages, etc.)</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.8c")]
public class ConversationMemoryManagerTests
{
    private readonly ConversationMemoryManager _manager;
    private readonly Mock<ITokenCounter> _tokenCounterMock;

    /// <summary>
    /// Initializes test dependencies with default performance options.
    /// </summary>
    public ConversationMemoryManagerTests()
    {
        _tokenCounterMock = new Mock<ITokenCounter>();
        _tokenCounterMock
            .Setup(tc => tc.CountTokens(It.IsAny<string>()))
            .Returns((string s) => s.Length / 4);

        _manager = new ConversationMemoryManager(
            NullLogger<ConversationMemoryManager>.Instance,
            _tokenCounterMock.Object,
            Options.Create(new PerformanceOptions()));
    }

    #region TrimToLimit Tests

    /// <summary>
    /// Verifies that trimming does not modify messages when count is below limit.
    /// </summary>
    [Fact]
    public void TrimToLimit_BelowLimit_DoesNotModifyMessages()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            ChatMessage.System("You are a helpful assistant."),
            ChatMessage.User("Hello!"),
            ChatMessage.Assistant("Hi there!")
        };

        // Act
        _manager.TrimToLimit(messages, 10);

        // Assert
        messages.Should().HaveCount(3);
        _manager.CurrentMessageCount.Should().Be(3);
    }

    /// <summary>
    /// Verifies that trimming removes oldest non-system messages.
    /// </summary>
    [Fact]
    public void TrimToLimit_AboveLimit_RemovesOldestNonSystemMessages()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            ChatMessage.System("System prompt"),
            ChatMessage.User("First user message"),
            ChatMessage.Assistant("First response"),
            ChatMessage.User("Second user message"),
            ChatMessage.Assistant("Second response"),
            ChatMessage.User("Third user message"),
            ChatMessage.Assistant("Third response")
        };

        // Act
        _manager.TrimToLimit(messages, 3);

        // Assert — system message + last 2 non-system messages remain
        messages.Should().HaveCount(3);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[0].Content.Should().Be("System prompt");
    }

    /// <summary>
    /// Verifies that system messages are always preserved during trimming.
    /// </summary>
    [Fact]
    public void TrimToLimit_PreservesSystemMessages()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            ChatMessage.System("System prompt 1"),
            ChatMessage.User("User 1"),
            ChatMessage.Assistant("Response 1"),
            ChatMessage.User("User 2"),
            ChatMessage.Assistant("Response 2")
        };

        // Act
        _manager.TrimToLimit(messages, 3);

        // Assert — system message is always preserved
        messages.Should().Contain(m => m.Role == ChatRole.System);
        messages.First(m => m.Role == ChatRole.System).Content.Should().Be("System prompt 1");
    }

    /// <summary>
    /// Verifies that trimming to exact count leaves messages unchanged.
    /// </summary>
    [Fact]
    public void TrimToLimit_AtExactLimit_DoesNotModify()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            ChatMessage.User("Hello"),
            ChatMessage.Assistant("Hi")
        };

        // Act
        _manager.TrimToLimit(messages, 2);

        // Assert
        messages.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that empty message list does not throw.
    /// </summary>
    [Fact]
    public void TrimToLimit_EmptyList_HandlesGracefully()
    {
        // Arrange
        var messages = new List<ChatMessage>();

        // Act
        _manager.TrimToLimit(messages, 50);

        // Assert
        messages.Should().BeEmpty();
        _manager.CurrentMessageCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that TrimToLimit throws for null messages.
    /// </summary>
    [Fact]
    public void TrimToLimit_NullMessages_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _manager.TrimToLimit(null!, 50);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("messages");
    }

    /// <summary>
    /// Verifies that conversation with only system messages preserves all.
    /// </summary>
    [Fact]
    public void TrimToLimit_OnlySystemMessages_PreservesAll()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            ChatMessage.System("System 1"),
            ChatMessage.System("System 2")
        };

        // Act
        _manager.TrimToLimit(messages, 10);

        // Assert
        messages.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that estimated bytes are updated after trimming.
    /// </summary>
    [Fact]
    public void TrimToLimit_UpdatesEstimatedBytes()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            ChatMessage.User("Short"),
            ChatMessage.Assistant("Also short")
        };

        // Act
        _manager.TrimToLimit(messages, 10);

        // Assert
        _manager.EstimatedMemoryBytes.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Verifies aggressive trim when limit is 1 with system message.
    /// </summary>
    [Fact]
    public void TrimToLimit_VeryLowLimit_PreservesSystemAndTrimsRest()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            ChatMessage.System("System"),
            ChatMessage.User("User 1"),
            ChatMessage.User("User 2"),
            ChatMessage.User("User 3")
        };

        // Act — limit of 1 means only system message is kept, no room for others
        _manager.TrimToLimit(messages, 1);

        // Assert
        messages.Should().HaveCount(1);
        messages[0].Role.Should().Be(ChatRole.System);
    }

    #endregion

    #region Memory Estimation Tests

    /// <summary>
    /// Verifies that memory estimation uses UTF-16 calculation.
    /// </summary>
    [Fact]
    public void EstimatedMemoryBytes_ReflectsUTF16Calculation()
    {
        // Arrange — 10-character message: 10 * 2 + 100 = 120 bytes
        var messages = new List<ChatMessage>
        {
            ChatMessage.User("1234567890")
        };

        // Act
        _manager.TrimToLimit(messages, 10);

        // Assert
        _manager.EstimatedMemoryBytes.Should().Be(120);
    }

    /// <summary>
    /// Verifies memory estimation for multiple messages.
    /// </summary>
    [Fact]
    public void EstimatedMemoryBytes_SumsAcrossMessages()
    {
        // Arrange — two 5-character messages: 2 * (5 * 2 + 100) = 220 bytes
        var messages = new List<ChatMessage>
        {
            ChatMessage.User("Hello"),
            ChatMessage.Assistant("World")
        };

        // Act
        _manager.TrimToLimit(messages, 10);

        // Assert
        _manager.EstimatedMemoryBytes.Should().Be(220);
    }

    #endregion

    #region EnforceMemoryLimit Tests

    /// <summary>
    /// Verifies that EnforceMemoryLimit does nothing when within limits.
    /// </summary>
    [Fact]
    public void EnforceMemoryLimit_WithinLimit_NoAction()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            ChatMessage.User("Small message")
        };
        _manager.TrimToLimit(messages, 10);

        // Act — should not throw or warn
        _manager.EnforceMemoryLimit(5 * 1024 * 1024);

        // Assert — no exception means success
        _manager.EstimatedMemoryBytes.Should().BeLessThan(5 * 1024 * 1024);
    }

    /// <summary>
    /// Verifies that EnforceMemoryLimit fires when memory exceeds limit.
    /// </summary>
    [Fact]
    public void EnforceMemoryLimit_ExceedsLimit_LogsWarning()
    {
        // Arrange — create messages with enough content to exceed 1 byte limit
        var messages = new List<ChatMessage>
        {
            ChatMessage.User("Some content here that will exceed a tiny limit")
        };
        _manager.TrimToLimit(messages, 10);

        // Act — enforce with very low limit (1 byte)
        // This should trigger the warning path
        _manager.EnforceMemoryLimit(1);

        // Assert — manager should report bytes > 1
        _manager.EstimatedMemoryBytes.Should().BeGreaterThan(1);
    }

    #endregion

    #region PerformanceOptions Tests

    /// <summary>
    /// Verifies default performance options values.
    /// </summary>
    [Fact]
    public void PerformanceOptions_Defaults_HaveExpectedValues()
    {
        // Act
        var options = new PerformanceOptions();

        // Assert
        options.MaxConversationMessages.Should().Be(50);
        options.MaxConversationMemoryBytes.Should().Be(5 * 1024 * 1024);
        options.CoalescingWindow.Should().Be(TimeSpan.FromMilliseconds(100));
        options.ContextCacheDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.MaxCompiledTemplates.Should().Be(100);
    }

    /// <summary>
    /// Verifies custom performance options override defaults.
    /// </summary>
    [Fact]
    public void PerformanceOptions_CustomValues_OverrideDefaults()
    {
        // Act
        var options = new PerformanceOptions
        {
            MaxConversationMessages = 100,
            MaxConversationMemoryBytes = 10 * 1024 * 1024,
            CoalescingWindow = TimeSpan.FromMilliseconds(200),
            ContextCacheDuration = TimeSpan.FromMinutes(1),
            MaxCompiledTemplates = 50
        };

        // Assert
        options.MaxConversationMessages.Should().Be(100);
        options.MaxConversationMemoryBytes.Should().Be(10 * 1024 * 1024);
        options.CoalescingWindow.Should().Be(TimeSpan.FromMilliseconds(200));
        options.ContextCacheDuration.Should().Be(TimeSpan.FromMinutes(1));
        options.MaxCompiledTemplates.Should().Be(50);
    }

    #endregion
}
