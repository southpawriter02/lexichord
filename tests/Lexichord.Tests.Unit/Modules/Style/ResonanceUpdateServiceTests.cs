// <copyright file="ResonanceUpdateServiceTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reactive.Linq;
using FluentAssertions;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Style.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="ResonanceUpdateService"/>.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5c - Tests cover lifecycle, debouncing, immediate dispatch, and license gating.</para>
/// </remarks>
[Trait("Feature", "v0.3.5c")]
[Trait("Module", "Style")]
public class ResonanceUpdateServiceTests : IDisposable
{
    private readonly Mock<IChartDataService> _chartDataServiceMock;
    private readonly Mock<ILicenseService> _licenseServiceMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ResonanceUpdateService>> _loggerMock;
    private ResonanceUpdateService _sut;

    public ResonanceUpdateServiceTests()
    {
        _chartDataServiceMock = new Mock<IChartDataService>();
        _licenseServiceMock = new Mock<ILicenseService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ResonanceUpdateService>>();

        SetupDefaultMocks();
        _sut = CreateSut();
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Lifecycle Tests

    [Fact]
    public void StartListening_SetsIsListeningTrue()
    {
        // Arrange
        _sut.IsListening.Should().BeFalse();

        // Act
        _sut.StartListening();

        // Assert
        _sut.IsListening.Should().BeTrue();
    }

    [Fact]
    public void StopListening_SetsIsListeningFalse()
    {
        // Arrange
        _sut.StartListening();
        _sut.IsListening.Should().BeTrue();

        // Act
        _sut.StopListening();

        // Assert
        _sut.IsListening.Should().BeFalse();
    }

    [Fact]
    public void StartListening_CalledMultipleTimes_IsIdempotent()
    {
        // Act
        _sut.StartListening();
        _sut.StartListening();
        _sut.StartListening();

        // Assert
        _sut.IsListening.Should().BeTrue();
    }

    [Fact]
    public void StopListening_CalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        _sut.StartListening();

        // Act
        _sut.StopListening();
        _sut.StopListening();
        _sut.StopListening();

        // Assert
        _sut.IsListening.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert
        _sut.Dispose();
        _sut.Dispose();
        _sut.Dispose();
    }

    [Fact]
    public void StartListening_AfterDispose_DoesNotThrow()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        var action = () => _sut.StartListening();
        action.Should().NotThrow();

        // Service should remain not listening
        _sut.IsListening.Should().BeFalse();
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public async Task Handle_ReadabilityEvent_WhenNotLicensed_DoesNotDispatch()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.ResonanceDashboard))
            .Returns(false);
        _sut = CreateSut();
        _sut.StartListening();

        var receivedEvents = new List<ChartUpdateEventArgs>();
        using var subscription = _sut.UpdateRequested.Subscribe(e => receivedEvents.Add(e));

        // Act
        await _sut.Handle(
            new ReadabilityAnalyzedEvent(Guid.NewGuid(), CreateTestMetrics(), TimeSpan.Zero),
            CancellationToken.None);

        // Small delay for potential debounce
        await Task.Delay(400);

        // Assert
        receivedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProfileChangedEvent_WhenNotLicensed_DoesNotDispatch()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.ResonanceDashboard))
            .Returns(false);
        _sut = CreateSut();
        _sut.StartListening();

        var receivedEvents = new List<ChartUpdateEventArgs>();
        using var subscription = _sut.UpdateRequested.Subscribe(e => receivedEvents.Add(e));

        // Act
        await _sut.Handle(
            new ProfileChangedEvent(Guid.NewGuid(), Guid.NewGuid(), "Test"),
            CancellationToken.None);

        // Assert
        receivedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReadabilityEvent_WhenNotListening_DoesNotDispatch()
    {
        // Arrange - service is licensed but not listening
        var receivedEvents = new List<ChartUpdateEventArgs>();
        using var subscription = _sut.UpdateRequested.Subscribe(e => receivedEvents.Add(e));

        // Act
        await _sut.Handle(
            new ReadabilityAnalyzedEvent(Guid.NewGuid(), CreateTestMetrics(), TimeSpan.Zero),
            CancellationToken.None);

        // Small delay for potential debounce
        await Task.Delay(400);

        // Assert
        receivedEvents.Should().BeEmpty();
    }

    #endregion

    #region Immediate Dispatch Tests

    [Fact]
    public async Task Handle_ProfileChangedEvent_DispatchesImmediately()
    {
        // Arrange
        _sut.StartListening();
        var receivedEvents = new List<ChartUpdateEventArgs>();
        using var subscription = _sut.UpdateRequested.Subscribe(e => receivedEvents.Add(e));

        // Act
        await _sut.Handle(
            new ProfileChangedEvent(Guid.NewGuid(), Guid.NewGuid(), "Test Profile"),
            CancellationToken.None);

        // Assert - should be immediate, no debounce delay needed
        receivedEvents.Should().ContainSingle();
        receivedEvents[0].Trigger.Should().Be(UpdateTrigger.ProfileChanged);
        receivedEvents[0].WasImmediate.Should().BeTrue();
    }

    [Fact]
    public async Task ForceUpdateAsync_DispatchesImmediately()
    {
        // Arrange
        _sut.StartListening();
        var receivedEvents = new List<ChartUpdateEventArgs>();
        using var subscription = _sut.UpdateRequested.Subscribe(e => receivedEvents.Add(e));

        // Act
        await _sut.ForceUpdateAsync();

        // Assert - should be immediate
        receivedEvents.Should().ContainSingle();
        receivedEvents[0].Trigger.Should().Be(UpdateTrigger.ForceUpdate);
        receivedEvents[0].WasImmediate.Should().BeTrue();
    }

    [Fact]
    public async Task ForceUpdateAsync_WhenNotLicensed_DoesNotDispatch()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.ResonanceDashboard))
            .Returns(false);
        _sut = CreateSut();
        _sut.StartListening();

        var receivedEvents = new List<ChartUpdateEventArgs>();
        using var subscription = _sut.UpdateRequested.Subscribe(e => receivedEvents.Add(e));

        // Act
        await _sut.ForceUpdateAsync();

        // Assert
        receivedEvents.Should().BeEmpty();
    }

    #endregion

    #region Debouncing Tests

    [Fact]
    public async Task Handle_MultipleRapidReadabilityEvents_DebouncesToSingleOutput()
    {
        // Arrange
        _sut.StartListening();
        var receivedEvents = new List<ChartUpdateEventArgs>();
        using var subscription = _sut.UpdateRequested.Subscribe(e => receivedEvents.Add(e));

        // Act - Fire multiple events in rapid succession
        for (int i = 0; i < 5; i++)
        {
            await _sut.Handle(
                new ReadabilityAnalyzedEvent(Guid.NewGuid(), CreateTestMetrics(), TimeSpan.Zero),
                CancellationToken.None);
            await Task.Delay(50); // 50ms between events
        }

        // Wait for debounce to complete (300ms + buffer)
        await Task.Delay(500);

        // Assert - Only one event should have been dispatched
        receivedEvents.Should().ContainSingle();
        receivedEvents[0].Trigger.Should().Be(UpdateTrigger.ReadabilityAnalyzed);
        receivedEvents[0].WasImmediate.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReadabilityEvent_DispatchesAfterDebounceWindow()
    {
        // Arrange
        _sut.StartListening();
        var receivedEvents = new List<ChartUpdateEventArgs>();
        using var subscription = _sut.UpdateRequested.Subscribe(e => receivedEvents.Add(e));

        // Act
        await _sut.Handle(
            new ReadabilityAnalyzedEvent(Guid.NewGuid(), CreateTestMetrics(), TimeSpan.Zero),
            CancellationToken.None);

        // Assert - No immediate dispatch
        receivedEvents.Should().BeEmpty();

        // Wait for debounce window (300ms + buffer)
        await Task.Delay(500);

        // Assert - Event dispatched after debounce
        receivedEvents.Should().ContainSingle();
        receivedEvents[0].Trigger.Should().Be(UpdateTrigger.ReadabilityAnalyzed);
    }

    [Fact]
    public async Task Handle_DebouncedEvent_InvalidatesCache()
    {
        // Arrange
        _sut.StartListening();
        var cacheInvalidated = false;
        _chartDataServiceMock
            .Setup(x => x.InvalidateCache())
            .Callback(() => cacheInvalidated = true);

        // Act
        await _sut.Handle(
            new ReadabilityAnalyzedEvent(Guid.NewGuid(), CreateTestMetrics(), TimeSpan.Zero),
            CancellationToken.None);

        // Wait for debounce
        await Task.Delay(500);

        // Assert
        cacheInvalidated.Should().BeTrue();
    }

    #endregion

    #region MediatR Publishing Tests

    [Fact]
    public async Task Handle_ProfileChangedEvent_PublishesChartUpdateEvent()
    {
        // Arrange
        _sut.StartListening();

        // Act
        await _sut.Handle(
            new ProfileChangedEvent(Guid.NewGuid(), Guid.NewGuid(), "Test"),
            CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(
                It.Is<ChartUpdateEvent>(e => e.Trigger == UpdateTrigger.ProfileChanged),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Test Helpers

    private void SetupDefaultMocks()
    {
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.ResonanceDashboard))
            .Returns(true);

        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ResonanceUpdateService CreateSut()
    {
        return new ResonanceUpdateService(
            _chartDataServiceMock.Object,
            _licenseServiceMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    private static ReadabilityMetrics CreateTestMetrics()
    {
        return new ReadabilityMetrics
        {
            FleschReadingEase = 60,
            FleschKincaidGradeLevel = 8,
            GunningFogIndex = 10,
            WordCount = 100,
            SentenceCount = 7,
            SyllableCount = 150,
            ComplexWordCount = 10
        };
    }

    #endregion
}
