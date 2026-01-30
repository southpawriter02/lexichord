using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services;
using Lexichord.Modules.Style.Services.Linting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Unit tests for <see cref="ChunkedScanner"/>.
/// </summary>
/// <remarks>
/// LOGIC: Tests verify chunk creation, viewport prioritization,
/// and proper exclusion region adjustment.
///
/// Version: v0.2.7d
/// </remarks>
public sealed class ChunkedScannerTests : IDisposable
{
    private readonly Mock<IScannerService> _mockScannerService;
    private readonly Mock<ILogger<PerformanceMonitor>> _mockPerformanceLogger;
    private readonly Mock<ILogger<ChunkedScanner>> _mockScannerLogger;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ChunkedScanner _sut;

    public ChunkedScannerTests()
    {
        _mockScannerService = new Mock<IScannerService>();
        _mockPerformanceLogger = new Mock<ILogger<PerformanceMonitor>>();
        _mockScannerLogger = new Mock<ILogger<ChunkedScanner>>();

        _performanceMonitor = new PerformanceMonitor(_mockPerformanceLogger.Object);
        _sut = new ChunkedScanner(
            _mockScannerService.Object,
            _performanceMonitor,
            _mockScannerLogger.Object);

        // Default mock setup
        _mockScannerService
            .Setup(x => x.ScanBatchAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<StyleRule>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ScannerResult>());
    }

    [Fact]
    public async Task SmallFile_SingleScan()
    {
        // Arrange
        var content = new string('a', 500_000); // 500KB - under threshold
        var rules = CreateSampleRules();

        // Act
        var results = new List<ChunkScanResult>();
        await foreach (var result in _sut.ScanChunkedAsync(content, rules, 0, 1000))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].TotalChunks.Should().Be(1);
        results[0].IsViewportChunk.Should().BeTrue();
    }

    [Fact]
    public async Task LargeFile_ChunkedScan()
    {
        // Arrange
        var content = new string('a', 2_000_000); // 2MB - over threshold
        var rules = CreateSampleRules();

        // Act
        var results = new List<ChunkScanResult>();
        await foreach (var result in _sut.ScanChunkedAsync(content, rules, 0, 1000))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCountGreaterThan(1);
        results.All(r => r.TotalChunks > 1).Should().BeTrue();
    }

    [Fact]
    public async Task ViewportChunks_ScannedFirst()
    {
        // Arrange
        var content = new string('a', 2_000_000); // 2MB
        var rules = CreateSampleRules();
        var viewportStart = content.Length / 2;
        var viewportEnd = viewportStart + 5000;

        // Act
        var results = new List<ChunkScanResult>();
        await foreach (var result in _sut.ScanChunkedAsync(
            content, rules, viewportStart, viewportEnd))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCountGreaterThan(1);
        results[0].IsViewportChunk.Should().BeTrue();
    }

    [Fact]
    public async Task ChunkBoundaries_AlignToLineBreaks()
    {
        // Arrange
        var lines = Enumerable.Repeat("This is a line of text.\n", 50000);
        var content = string.Join("", lines);
        var rules = CreateSampleRules();

        // Act
        var results = new List<ChunkScanResult>();
        await foreach (var result in _sut.ScanChunkedAsync(content, rules, 0, 1000))
        {
            results.Add(result);
        }

        // Assert - Each chunk should end at a newline
        foreach (var result in results.Take(results.Count - 1))
        {
            if (result.EndOffset < content.Length)
            {
                var charBeforeEnd = content[result.EndOffset - 1];
                charBeforeEnd.Should().Be('\n');
            }
        }
    }

    [Fact]
    public async Task Cancellation_StopsProcessing()
    {
        // Arrange
        var content = new string('a', 2_000_000); // 2MB
        var rules = CreateSampleRules();
        var cts = new CancellationTokenSource();

        // Act
        var results = new List<ChunkScanResult>();
        try
        {
            await foreach (var result in _sut.ScanChunkedAsync(
                content, rules, 0, 1000, cts.Token))
            {
                results.Add(result);
                if (results.Count == 2)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Expected - cancellation throws OperationCanceledException
        }

        // Assert - Should have stopped before all chunks
        results.Should().HaveCountLessOrEqualTo(3);
    }

    [Fact]
    public async Task ChunkProgress_CalculatedCorrectly()
    {
        // Arrange
        var content = new string('a', 2_000_000); // 2MB
        var rules = CreateSampleRules();

        // Act
        var results = new List<ChunkScanResult>();
        await foreach (var result in _sut.ScanChunkedAsync(content, rules, 0, 1000))
        {
            results.Add(result);
        }

        // Assert
        var lastResult = results.Last();
        lastResult.ProgressPercent.Should().Be(100.0);
    }

    private static IReadOnlyList<StyleRule> CreateSampleRules()
    {
        return
        [
            new StyleRule(
                Id: "test-rule",
                Name: "Test Rule",
                Description: "Test description",
                Category: RuleCategory.Terminology,
                DefaultSeverity: ViolationSeverity.Warning,
                Pattern: "test",
                PatternType: PatternType.Literal,
                Suggestion: "replacement")
        ];
    }

    public void Dispose()
    {
        _performanceMonitor.Dispose();
    }
}
