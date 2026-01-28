using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for CrashReportService.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify:
/// - Crash reports are written to disk correctly
/// - Report contains required system and exception information
/// - Inner exceptions are captured
/// - Unique filenames are generated
/// </remarks>
public class CrashReportServiceTests : IDisposable
{
    private readonly CrashReportService _sut;
    private readonly Mock<ILogger<CrashReportService>> _mockLogger;
    private readonly string _tempDir;

    public CrashReportServiceTests()
    {
        _mockLogger = new Mock<ILogger<CrashReportService>>();
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _sut = new CrashReportService(_mockLogger.Object, _tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveCrashReportAsync_CreatesFile()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var filePath = await _sut.SaveCrashReportAsync(exception);

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveCrashReportAsync_ContainsExceptionType()
    {
        // Arrange
        var exception = new ArgumentNullException("paramName", "Test message");

        // Act
        var filePath = await _sut.SaveCrashReportAsync(exception);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain("ArgumentNullException");
        content.Should().Contain("Test message");
        content.Should().Contain("paramName");
    }

    [Fact]
    public async Task SaveCrashReportAsync_ContainsStackTrace()
    {
        // Arrange
        Exception? exception = null;
        try
        {
            ThrowException();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var filePath = await _sut.SaveCrashReportAsync(exception!);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain("ThrowException");
        content.Should().Contain("STACK TRACE");
    }

    [Fact]
    public async Task SaveCrashReportAsync_IncludesInnerException()
    {
        // Arrange
        var inner = new ArgumentNullException("innerParam");
        var outer = new InvalidOperationException("Outer message", inner);

        // Act
        var filePath = await _sut.SaveCrashReportAsync(outer);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain("INNER EXCEPTION");
        content.Should().Contain("ArgumentNullException");
        content.Should().Contain("innerParam");
    }

    [Fact]
    public async Task SaveCrashReportAsync_IncludesSystemInfo()
    {
        // Arrange
        var exception = new Exception("Test");

        // Act
        var filePath = await _sut.SaveCrashReportAsync(exception);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain("OS:");
        content.Should().Contain(".NET Version:");
        content.Should().Contain("Machine:");
    }

    [Fact]
    public void CrashReportDirectory_ReturnsConfiguredPath()
    {
        // Assert
        _sut.CrashReportDirectory.Should().Be(_tempDir);
    }

    [Fact]
    public async Task SaveCrashReportAsync_CreatesUniqueFilenames()
    {
        // Arrange
        var exception = new Exception("Test");

        // Act
        var path1 = await _sut.SaveCrashReportAsync(exception);
        await Task.Delay(5); // Small delay to ensure different timestamp
        var path2 = await _sut.SaveCrashReportAsync(exception);

        // Assert
        path1.Should().NotBe(path2);
        File.Exists(path1).Should().BeTrue();
        File.Exists(path2).Should().BeTrue();
    }

    [Fact]
    public async Task SaveCrashReportAsync_ReportContainsHResult()
    {
        // Arrange
        var exception = new Exception("Test");

        // Act
        var filePath = await _sut.SaveCrashReportAsync(exception);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain("HResult:");
    }

    [Fact]
    public async Task SaveCrashReportAsync_HandlesMultipleInnerExceptions()
    {
        // Arrange
        var innermost = new ArgumentException("Innermost");
        var middle = new InvalidOperationException("Middle", innermost);
        var outer = new ApplicationException("Outer", middle);

        // Act
        var filePath = await _sut.SaveCrashReportAsync(outer);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain("Inner Exception 1");
        content.Should().Contain("Inner Exception 2");
        content.Should().Contain("InvalidOperationException");
        content.Should().Contain("ArgumentException");
    }

    /// <summary>
    /// Helper method to generate a real stack trace.
    /// </summary>
    private static void ThrowException()
    {
        throw new InvalidOperationException("Stack trace test");
    }
}

