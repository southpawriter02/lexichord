using FluentAssertions;
using Lexichord.Abstractions.Contracts.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Threading;

/// <summary>
/// Unit tests for IThreadMarshaller implementations.
/// </summary>
/// <remarks>
/// LOGIC: Tests the contract behavior of IThreadMarshaller using a test double.
/// Cannot test Avalonia's Dispatcher directly in unit tests, so we use a
/// mock-based test marshaller to verify the interface contract.
///
/// Version: v0.2.7a
/// </remarks>
public class ThreadMarshallerTests
{
    private readonly TestThreadMarshaller _sut;

    public ThreadMarshallerTests()
    {
        _sut = new TestThreadMarshaller();
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_ExecutesAction_WhenOnUIThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = true;
        var executed = false;

        // Act
        await _sut.InvokeOnUIThreadAsync(() => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_ExecutesAction_WhenOnBackgroundThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = false;
        var executed = false;

        // Act
        await _sut.InvokeOnUIThreadAsync(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        // Verify it went through marshalling path (not direct execution)
        _sut.IsOnUIThread.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_ReturnsValue_WhenOnUIThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = true;

        // Act
        var result = await _sut.InvokeOnUIThreadAsync(() => 42);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_ReturnsValue_WhenOnBackgroundThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = false;

        // Act
        var result = await _sut.InvokeOnUIThreadAsync(() => "hello");

        // Assert
        result.Should().Be("hello");
        _sut.MarshalledFuncCount.Should().Be(1);
    }

    [Fact]
    public void PostToUIThread_ExecutesAction_WhenOnUIThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = true;
        var executed = false;

        // Act
        _sut.PostToUIThread(() => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void PostToUIThread_QueuesAction_WhenOnBackgroundThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = false;

        // Act
        _sut.PostToUIThread(() => { });

        // Assert
        _sut.PostedActionCount.Should().Be(1);
    }

    [Fact]
    public void IsOnUIThread_ReturnsTrue_WhenSimulatedOnUIThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = true;

        // Assert
        _sut.IsOnUIThread.Should().BeTrue();
    }

    [Fact]
    public void IsOnUIThread_ReturnsFalse_WhenSimulatedOnBackgroundThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = false;

        // Assert
        _sut.IsOnUIThread.Should().BeFalse();
    }

    [Fact]
    public void AssertUIThread_DoesNotThrow_WhenOnUIThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = true;

        // Act & Assert
        var act = () => _sut.AssertUIThread("TestOperation");
        act.Should().NotThrow();
    }

    [Fact]
    public void AssertUIThread_Throws_WhenNotOnUIThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = false;

        // Act & Assert
        var act = () => _sut.AssertUIThread("TestOperation");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestOperation*UI thread*");
    }

    [Fact]
    public void AssertBackgroundThread_DoesNotThrow_WhenNotOnUIThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = false;

        // Act & Assert
        var act = () => _sut.AssertBackgroundThread("TestOperation");
        act.Should().NotThrow();
    }

    [Fact]
    public void AssertBackgroundThread_Throws_WhenOnUIThread()
    {
        // Arrange
        _sut.SimulateOnUIThread = true;

        // Act & Assert
        var act = () => _sut.AssertBackgroundThread("TestOperation");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestOperation*NOT*UI thread*");
    }

    /// <summary>
    /// Test implementation of IThreadMarshaller for unit testing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Simulates thread behavior without depending on Avalonia's Dispatcher.
    /// All actions execute synchronously in test context while tracking calls.
    /// </remarks>
    private sealed class TestThreadMarshaller : IThreadMarshaller
    {
        public bool SimulateOnUIThread { get; set; } = true;
        public int MarshalledActionCount { get; private set; }
        public int MarshalledFuncCount { get; private set; }
        public int PostedActionCount { get; private set; }

        public bool IsOnUIThread => SimulateOnUIThread;

        public Task InvokeOnUIThreadAsync(Action action)
        {
            if (!IsOnUIThread)
                MarshalledActionCount++;
            action();
            return Task.CompletedTask;
        }

        public Task<T> InvokeOnUIThreadAsync<T>(Func<T> func)
        {
            if (!IsOnUIThread)
                MarshalledFuncCount++;
            return Task.FromResult(func());
        }

        public void PostToUIThread(Action action)
        {
            if (IsOnUIThread)
            {
                action();
            }
            else
            {
                PostedActionCount++;
                // In real implementation, this would queue to UI thread
                action();
            }
        }

        public void AssertUIThread(string operation)
        {
            if (!IsOnUIThread)
                throw new InvalidOperationException(
                    $"Operation '{operation}' must be called on the UI thread.");
        }

        public void AssertBackgroundThread(string operation)
        {
            if (IsOnUIThread)
                throw new InvalidOperationException(
                    $"Operation '{operation}' must NOT be called on the UI thread.");
        }
    }
}
