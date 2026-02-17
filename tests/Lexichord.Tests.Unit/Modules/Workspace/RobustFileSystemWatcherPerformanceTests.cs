using System.Diagnostics;
using System.Reflection;
using FluentAssertions;
using Lexichord.Modules.Workspace.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Workspace;

public class RobustFileSystemWatcherPerformanceTests : IDisposable
{
    private readonly ILogger<RobustFileSystemWatcher> _mockLogger;
    private readonly RobustFileSystemWatcher _sut;
    private readonly string _testDirectory;

    public RobustFileSystemWatcherPerformanceTests()
    {
        _mockLogger = Substitute.For<ILogger<RobustFileSystemWatcher>>();
        _sut = new RobustFileSystemWatcher(_mockLogger);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"fsw_perf_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task TryRecoverWatcherAsync_IsNonBlocking_AndTakesExpectedTime()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);

        // Get private method
        var methodInfo = typeof(RobustFileSystemWatcher).GetMethod("TryRecoverWatcherAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        methodInfo.Should().NotBeNull("TryRecoverWatcherAsync method should exist");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = methodInfo!.Invoke(_sut, null);
        stopwatch.Stop();

        // Assert
        result.Should().BeAssignableTo<Task<bool>>();

        // Verify it returned immediately (non-blocking)
        // Invocation overhead + async machine start should be very fast, well under 200ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "Method should return task immediately without blocking");

        // Now wait for completion
        stopwatch.Restart();
        var task = (Task<bool>)result!;
        var recovered = await task;
        stopwatch.Stop();

        recovered.Should().BeTrue();
        // Verify it waited for recovery delay (1000ms)
        // We allow some slack for system scheduling
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(900, "Task should complete after recovery delay");
    }
}
