// -----------------------------------------------------------------------
// <copyright file="ContextPreviewBridgeTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Modules.Agents.Context;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context;

/// <summary>
/// Unit tests for <see cref="ContextPreviewBridge"/>.
/// </summary>
/// <remarks>
/// Tests verify that the bridge correctly forwards MediatR notifications
/// to C# event subscribers and handles edge cases gracefully.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2d")]
public class ContextPreviewBridgeTests
{
    #region Helper Factories

    private readonly ILogger<ContextPreviewBridge> _logger;

    public ContextPreviewBridgeTests()
    {
        _logger = Substitute.For<ILogger<ContextPreviewBridge>>();
    }

    private ContextPreviewBridge CreateBridge()
    {
        return new ContextPreviewBridge(_logger);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ContextPreviewBridge(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidLogger_CreatesInstance()
    {
        // Act
        var bridge = CreateBridge();

        // Assert
        bridge.Should().NotBeNull();
    }

    #endregion

    #region Handle ContextAssembledEvent Tests

    [Fact]
    public async Task Handle_ContextAssembledEvent_FiresContextAssembledEvent()
    {
        // Arrange
        var bridge = CreateBridge();
        ContextAssembledEvent? receivedEvent = null;
        bridge.ContextAssembled += e => receivedEvent = e;

        var notification = new ContextAssembledEvent(
            AgentId: "test-agent",
            Fragments: new List<ContextFragment>
            {
                new("doc", "Document", "Content here", 100, 1.0f)
            },
            TotalTokens: 100,
            Duration: TimeSpan.FromMilliseconds(50));

        // Act
        await bridge.Handle(notification, CancellationToken.None);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.AgentId.Should().Be("test-agent");
        receivedEvent.TotalTokens.Should().Be(100);
        receivedEvent.Fragments.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ContextAssembledEvent_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var bridge = CreateBridge();
        var notification = new ContextAssembledEvent(
            "test", new List<ContextFragment>(), 0, TimeSpan.Zero);

        // Act
        var act = () => bridge.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ContextAssembledEvent_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var bridge = CreateBridge();
        var receivedCount = 0;
        bridge.ContextAssembled += _ => receivedCount++;
        bridge.ContextAssembled += _ => receivedCount++;

        var notification = new ContextAssembledEvent(
            "test", new List<ContextFragment>(), 0, TimeSpan.Zero);

        // Act
        await bridge.Handle(notification, CancellationToken.None);

        // Assert
        receivedCount.Should().Be(2);
    }

    #endregion

    #region Handle StrategyToggleEvent Tests

    [Fact]
    public async Task Handle_StrategyToggleEvent_FiresStrategyToggledEvent()
    {
        // Arrange
        var bridge = CreateBridge();
        StrategyToggleEvent? receivedEvent = null;
        bridge.StrategyToggled += e => receivedEvent = e;

        var notification = new StrategyToggleEvent("document", true);

        // Act
        await bridge.Handle(notification, CancellationToken.None);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.StrategyId.Should().Be("document");
        receivedEvent.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StrategyToggleEvent_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var bridge = CreateBridge();
        var notification = new StrategyToggleEvent("rag", false);

        // Act
        var act = () => bridge.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion
}
