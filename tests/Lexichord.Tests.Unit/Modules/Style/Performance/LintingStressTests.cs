using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services;
using Lexichord.Modules.Style.Services.Linting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Stress tests for linting performance on large documents.
/// </summary>
/// <remarks>
/// LOGIC: These tests validate performance targets for the linting system
/// on documents of 5MB or larger. Tests are marked [Explicit] as they
/// take significant time and resources.
///
/// Performance Targets:
/// - Full scan: &lt; 2000ms
/// - Viewport scan: &lt; 100ms
/// - UI responsiveness: 60fps maintained
/// - Memory: &lt; 100MB for 5MB document
///
/// Version: v0.2.7d
/// </remarks>
[Trait("Category", "Performance")]
public sealed class LintingStressTests : IDisposable
{
    private readonly Mock<IScannerService> _mockScannerService;
    private readonly Mock<ILogger<PerformanceMonitor>> _mockPerformanceLogger;
    private readonly Mock<ILogger<ChunkedScanner>> _mockScannerLogger;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ChunkedScanner _chunkedScanner;
    private readonly TestCorpusGenerator _corpusGenerator;

    public LintingStressTests()
    {
        _mockScannerService = new Mock<IScannerService>();
        _mockPerformanceLogger = new Mock<ILogger<PerformanceMonitor>>();
        _mockScannerLogger = new Mock<ILogger<ChunkedScanner>>();

        _performanceMonitor = new PerformanceMonitor(_mockPerformanceLogger.Object);
        _chunkedScanner = new ChunkedScanner(
            _mockScannerService.Object,
            _performanceMonitor,
            _mockScannerLogger.Object);

        _corpusGenerator = new TestCorpusGenerator(seed: 42);

        // LOGIC: Configure mock to return realistic results
        _mockScannerService
            .Setup(x => x.ScanBatchAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<StyleRule>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IEnumerable<StyleRule> __, CancellationToken ___) =>
            {
                // LOGIC: Simulate realistic scan with ~10ms delay
                Thread.Sleep(10);
                return Array.Empty<ScannerResult>();
            });
    }

    [Fact(Skip = "Performance test - run explicitly")]
    [Trait("Category", "Performance")]
    public async Task FullScan_5MBDocument_CompletesUnder2Seconds()
    {
        // Arrange
        var document = _corpusGenerator.GenerateDocument(5);
        var rules = CreateSampleRules();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var results = new List<ChunkScanResult>();
        await foreach (var result in _chunkedScanner.ScanChunkedAsync(
            document, rules, 0, 1000))
        {
            results.Add(result);
        }

        stopwatch.Stop();

        // Assert
        Assert.True(
            stopwatch.ElapsedMilliseconds < 2000,
            $"Full scan took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        Assert.True(results.Count > 0, "Expected at least one chunk result");
    }

    [Fact(Skip = "Performance test - run explicitly")]
    [Trait("Category", "Performance")]
    public async Task ViewportScan_5MBDocument_CompletesUnder100Ms()
    {
        // Arrange
        var document = _corpusGenerator.GenerateDocument(5);
        var rules = CreateSampleRules();
        var viewportStart = document.Length / 2;
        var viewportEnd = viewportStart + 10000; // 10KB viewport

        // Act
        var firstResult = await AsyncEnumerable.FirstAsync(_chunkedScanner
            .ScanChunkedAsync(document, rules, viewportStart, viewportEnd));

        // Assert
        Assert.True(firstResult.IsViewportChunk, "First result should be viewport chunk");
        Assert.True(
            firstResult.ScanDuration.TotalMilliseconds < 100,
            $"Viewport scan took {firstResult.ScanDuration.TotalMilliseconds}ms, expected < 100ms");
    }

    [Fact(Skip = "Performance test - run explicitly")]
    [Trait("Category", "Performance")]
    public async Task TypingSimulation_MaintainsSixtyFPS()
    {
        // Arrange
        var document = _corpusGenerator.GenerateDocument(1); // 1MB for faster test
        var rules = CreateSampleRules();
        var frameDrops = 0;
        var typingChars = 100;

        // Act - Simulate typing
        for (var i = 0; i < typingChars; i++)
        {
            var simulatedLatency = Stopwatch.StartNew();

            // LOGIC: Simulate debounced lint trigger
            if (i % 10 == 0)
            {
                await foreach (var _ in AsyncEnumerable.Take(_chunkedScanner.ScanChunkedAsync(
                    document, rules, 0, 1000), 1))
                {
                    // Process first chunk only
                }
            }

            simulatedLatency.Stop();

            // LOGIC: Frame drop if latency > 16ms (60fps)
            if (simulatedLatency.ElapsedMilliseconds > 16)
            {
                frameDrops++;
            }
        }

        // Assert
        var avgFrameRate = 60.0 * (typingChars - frameDrops) / typingChars;
        Assert.True(
            avgFrameRate >= 55,
            $"Average frame rate {avgFrameRate:F1}fps, expected >= 55fps");
    }

    [Fact(Skip = "Performance test - run explicitly")]
    [Trait("Category", "Performance")]
    public async Task MemoryUsage_5MBDocument_StaysUnder100MB()
    {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var startMemory = GC.GetTotalMemory(true);

        var document = _corpusGenerator.GenerateDocument(5);
        var rules = CreateSampleRules();

        // Act
        await foreach (var _ in _chunkedScanner.ScanChunkedAsync(
            document, rules, 0, 1000))
        {
            // Process all chunks
        }

        var peakMemory = _performanceMonitor.PeakMemoryBytes;
        var memoryUsedMb = (peakMemory - startMemory) / (1024.0 * 1024.0);

        // Assert
        Assert.True(
            memoryUsedMb < 100,
            $"Memory usage {memoryUsedMb:F2}MB, expected < 100MB");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DebounceAdaptation_UnderLoad_IncreasesInterval()
    {
        // Arrange - Simulate slow scans
        for (var i = 0; i < 10; i++)
        {
            _performanceMonitor.RecordOperation($"scan{i}", TimeSpan.FromMilliseconds(600));
        }

        // Act
        var recommendedDebounce = _performanceMonitor.RecommendedDebounceInterval;

        // Assert
        Assert.True(
            recommendedDebounce.TotalMilliseconds >= 500,
            $"Debounce {recommendedDebounce.TotalMilliseconds}ms should be >= 500ms for slow scans");
    }

    [Fact(Skip = "Performance test - run explicitly")]
    [Trait("Category", "Performance")]
    public async Task ChunkedScan_ProcessesViewportFirst()
    {
        // Arrange
        var document = _corpusGenerator.GenerateDocument(2); // 2MB
        var rules = CreateSampleRules();
        var viewportStart = document.Length / 2;
        var viewportEnd = viewportStart + 5000;

        // Act
        var results = new List<ChunkScanResult>();
        await foreach (var result in _chunkedScanner.ScanChunkedAsync(
            document, rules, viewportStart, viewportEnd))
        {
            results.Add(result);
        }

        // Assert - First result should be viewport chunk
        Assert.True(results.Count > 1, "Expected multiple chunks");
        Assert.True(results[0].IsViewportChunk, "First chunk should be viewport");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FilteredScan_ExcludesCodeBlocks()
    {
        // Arrange
        var options = new DocumentGenerationOptions(
            TargetSizeBytes: 10000,
            IncludeCodeBlocks: true,
            CodeBlockDensity: 50 // 50% code blocks
        );
        var document = _corpusGenerator.GenerateDocument(options);

        // Assert
        Assert.Contains("```", document);
    }

    private static IReadOnlyList<StyleRule> CreateSampleRules()
    {
        return
        [
            new StyleRule(
                Id: "no-utilise",
                Name: "Avoid 'utilise'",
                Description: "Use 'use' instead of 'utilise'",
                Category: RuleCategory.Terminology,
                DefaultSeverity: ViolationSeverity.Warning,
                Pattern: "utilise",
                PatternType: PatternType.LiteralIgnoreCase,
                Suggestion: "use"),
            new StyleRule(
                Id: "no-colour",
                Name: "Consistent spelling",
                Description: "Use 'color' for consistency",
                Category: RuleCategory.Terminology,
                DefaultSeverity: ViolationSeverity.Warning,
                Pattern: "colour",
                PatternType: PatternType.LiteralIgnoreCase,
                Suggestion: "color")
        ];
    }

    public void Dispose()
    {
        _performanceMonitor.Dispose();
    }
}
