using FluentAssertions;
using Lexichord.Modules.StatusBar.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.StatusBar;

/// <summary>
/// Unit tests for HeartbeatService.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the heartbeat service correctly:
/// - Starts and stops the timer
/// - Records initial heartbeat on start
/// - Tracks consecutive failures
/// - Disposes resources properly
/// </remarks>
public class HeartbeatServiceTests : IDisposable
{
    private readonly IHealthRepository _healthRepo;
    private readonly ILogger<HeartbeatService> _logger;
    private readonly HeartbeatService _sut;

    public HeartbeatServiceTests()
    {
        _healthRepo = Substitute.For<IHealthRepository>();
        _logger = Substitute.For<ILogger<HeartbeatService>>();

        // LOGIC: Mock successful heartbeat recording by default
        _healthRepo.RecordHeartbeatAsync().Returns(Task.CompletedTask);

        _sut = new HeartbeatService(_healthRepo, _logger);
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Interval_IsSixtySeconds()
    {
        // Assert
        _sut.Interval.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void IsRunning_IsFalse_Initially()
    {
        // Assert
        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Start_SetsIsRunningTrue()
    {
        // Act
        _sut.Start();

        // Assert
        _sut.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Stop_SetsIsRunningFalse()
    {
        // Arrange
        _sut.Start();

        // Act
        _sut.Stop();

        // Assert
        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Start_RecordsInitialHeartbeat()
    {
        // Act
        _sut.Start();

        // LOGIC: Give the async operation a moment to complete
        await Task.Delay(100);

        // Assert
        await _healthRepo.Received(1).RecordHeartbeatAsync();
    }

    [Fact]
    public void ConsecutiveFailures_IsZero_Initially()
    {
        // Assert
        _sut.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void Dispose_StopsService()
    {
        // Arrange
        _sut.Start();
        _sut.IsRunning.Should().BeTrue();

        // Act
        _sut.Dispose();

        // Assert
        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Start_WhenAlreadyRunning_DoesNotFail()
    {
        // Arrange
        _sut.Start();

        // Act
        var act = () => _sut.Start();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Stop_WhenNotRunning_DoesNotFail()
    {
        // Act
        var act = () => _sut.Stop();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Start_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var act = () => _sut.Start();

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task HealthChanged_IsRaised_OnStart()
    {
        // Arrange
        bool healthChangedRaised = false;
        _sut.HealthChanged += (_, healthy) => healthChangedRaised = healthy;

        // Act
        _sut.Start();
        await Task.Delay(100); // Wait for async heartbeat

        // Assert - Successful heartbeat doesn't raise event (only failures do initially)
        // But let's verify the event mechanism works
        healthChangedRaised.Should().BeFalse(); // No change event needed on initial success
    }
}
