using FluentAssertions;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for DebounceController.
/// </summary>
/// <remarks>
/// LOGIC: Verifies the debounce state machine transitions correctly
/// and handles cancellation and disposal per LCS-DES-023b.
/// </remarks>
public class DebounceControllerTests : IDisposable
{
    private readonly List<(string Content, CancellationToken Token)> _scanRequests = [];
    private DebounceController? _sut;

    public void Dispose()
    {
        _sut?.Dispose();
    }

    private DebounceController CreateController(int debounceMs = 10)
    {
        _sut = new DebounceController(
            debounceMs,
            (content, token) => _scanRequests.Add((content, token)));
        return _sut;
    }

    [Fact]
    public void InitialState_IsIdle()
    {
        // Arrange & Act
        var controller = CreateController();

        // Assert
        controller.CurrentState.Should().Be(DebounceState.Idle);
    }

    [Fact]
    public void RequestScan_TransitionsToWaiting()
    {
        // Arrange
        var controller = CreateController();

        // Act
        controller.RequestScan("test content");

        // Assert
        controller.CurrentState.Should().Be(DebounceState.Waiting);
    }

    [Fact]
    public async Task AfterDebounceDelay_TransitionsToScanning()
    {
        // Arrange
        var controller = CreateController(debounceMs: 50);
        controller.RequestScan("test content");

        // Act - wait for debounce (50ms + margin)
        await Task.Delay(100);

        // Assert
        controller.CurrentState.Should().Be(DebounceState.Scanning);
        _scanRequests.Should().HaveCount(1);
        _scanRequests[0].Content.Should().Be("test content");
    }

    [Fact]
    public async Task RapidEdits_ResetDebounceTimer()
    {
        // Arrange - use generous timing to avoid flakiness under CPU load
        var controller = CreateController(debounceMs: 200);

        // Act - rapid edits (each well within debounce window)
        controller.RequestScan("content 1");
        await Task.Delay(50);
        controller.RequestScan("content 2");
        await Task.Delay(50);
        controller.RequestScan("content 3");

        // Wait for debounce to complete (200ms from last edit + generous margin)
        await Task.Delay(400);

        // Assert - only last content should have triggered scan
        _scanRequests.Should().HaveCount(1);
        _scanRequests[0].Content.Should().Be("content 3");
    }

    [Fact]
    public void CancelCurrent_WhenWaiting_TransitionsToCancelled()
    {
        // Arrange
        var controller = CreateController();
        controller.RequestScan("test content");

        // Act
        controller.CancelCurrent();

        // Assert
        controller.CurrentState.Should().Be(DebounceState.Cancelled);
    }

    [Fact]
    public async Task CancelCurrent_WhenScanning_TransitionsToCancelled()
    {
        // Arrange
        var controller = CreateController(debounceMs: 50);
        controller.RequestScan("test content");
        await Task.Delay(100); // Enter scanning state

        // Act
        controller.CancelCurrent();

        // Assert
        controller.CurrentState.Should().Be(DebounceState.Cancelled);
    }

    [Fact]
    public async Task CancelCurrent_CancelsToken()
    {
        // Arrange
        var controller = CreateController(debounceMs: 50);
        controller.RequestScan("test content");
        await Task.Delay(100); // Wait for scan to start

        // Act
        controller.CancelCurrent();

        // Assert
        _scanRequests.Should().HaveCount(1);
        _scanRequests[0].Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task MarkCompleted_TransitionsBackToIdle()
    {
        // Arrange
        var controller = CreateController(debounceMs: 50);
        controller.RequestScan("test content");
        await Task.Delay(100);
        controller.CurrentState.Should().Be(DebounceState.Scanning);

        // Act
        controller.MarkCompleted();

        // Assert
        controller.CurrentState.Should().Be(DebounceState.Idle);
    }

    [Fact]
    public void MarkCancelled_TransitionsToCancelled()
    {
        // Arrange
        var controller = CreateController();
        controller.RequestScan("test content");

        // Act
        controller.MarkCancelled();

        // Assert
        controller.CurrentState.Should().Be(DebounceState.Cancelled);
    }

    [Fact]
    public void Dispose_CancelsPendingOperations()
    {
        // Arrange
        var controller = CreateController();
        controller.RequestScan("test content");

        // Act
        controller.Dispose();

        // Assert - subsequent requests should be ignored
        controller.RequestScan("ignored content");
        _scanRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task Dispose_CancelsInFlightScan()
    {
        // Arrange
        var controller = CreateController(debounceMs: 50);
        controller.RequestScan("test content");
        await Task.Delay(100);

        // Act
        controller.Dispose();

        // Assert
        _scanRequests.Should().HaveCount(1);
        _scanRequests[0].Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ThrowsOnNullCallback()
    {
        // Act
        var act = () => new DebounceController(100, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task NewRequestDuringScanning_CancelsPreviousAndStartsNew()
    {
        // Arrange
        var controller = CreateController(debounceMs: 50);
        controller.RequestScan("first content");
        await Task.Delay(100); // Enter scanning state

        // Act - new request during scan
        controller.RequestScan("second content");
        await Task.Delay(100); // Wait for new scan

        // Assert
        _scanRequests.Should().HaveCount(2);

        // First scan should have been cancelled
        _scanRequests[0].Token.IsCancellationRequested.Should().BeTrue();

        // Second scan should be running
        _scanRequests[1].Content.Should().Be("second content");
    }

    [Fact]
    public async Task ConfigurableDelay_Respected()
    {
        // Arrange - long debounce
        var controller = CreateController(debounceMs: 100);
        controller.RequestScan("test content");

        // Act - check before debounce
        await Task.Delay(30);
        var stateBeforeDebounce = controller.CurrentState;

        // Wait for debounce to complete
        await Task.Delay(100);
        var stateAfterDebounce = controller.CurrentState;

        // Assert
        stateBeforeDebounce.Should().Be(DebounceState.Waiting);
        stateAfterDebounce.Should().Be(DebounceState.Scanning);
    }
}
