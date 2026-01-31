// <copyright file="ParallelAnalysisPipelineTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Unit tests for <see cref="ParallelAnalysisPipeline"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.7b")]
public class ParallelAnalysisPipelineTests
{
    private readonly Mock<IStyleEngine> _styleEngineMock = new();
    private readonly Mock<IFuzzyScanner> _fuzzyScannerMock = new();
    private readonly Mock<IReadabilityService> _readabilityServiceMock = new();
    private readonly Mock<IVoiceAnalyzer> _voiceAnalyzerMock = new();
    private readonly ILogger<ParallelAnalysisPipeline> _logger =
        NullLogger<ParallelAnalysisPipeline>.Instance;

    private ParallelAnalysisPipeline CreatePipeline() =>
        new(
            _styleEngineMock.Object,
            _fuzzyScannerMock.Object,
            _readabilityServiceMock.Object,
            _voiceAnalyzerMock.Object,
            _logger);

    private static AnalysisRequest CreateRequest(string content) =>
        new("doc-1", "/test/file.md", content, DateTimeOffset.UtcNow);

    private static readonly StyleRule TestRule = new(
        Id: "TST-001",
        Name: "Test Rule",
        Description: "Test rule for unit tests",
        Category: RuleCategory.Terminology,
        DefaultSeverity: ViolationSeverity.Warning,
        Pattern: ".*",
        PatternType: PatternType.Regex,
        Suggestion: null);

    private static StyleViolation CreateViolation(string matchedText) =>
        new(
            Rule: TestRule,
            Message: $"Test violation for {matchedText}",
            StartOffset: 0,
            EndOffset: matchedText.Length,
            StartLine: 1,
            StartColumn: 1,
            EndLine: 1,
            EndColumn: matchedText.Length + 1,
            MatchedText: matchedText,
            Suggestion: null,
            Severity: ViolationSeverity.Warning);

    [Fact]
    public async Task ExecuteAsync_AllSucceed_ReturnsCompleteResult()
    {
        // Arrange
        var regexViolations = new[] { CreateViolation("regex") };
        var fuzzyViolations = new[] { CreateViolation("fuzzy") };
        var readabilityMetrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 8.5,
            GunningFogIndex = 10.2,
            FleschReadingEase = 65.0,
            WordCount = 100,
            SentenceCount = 5,
            SyllableCount = 150
        };
        var voiceResult = new VoiceAnalysisResult
        {
            PassiveVoiceMatches = [],
            WeakWordStats = null
        };

        _styleEngineMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(regexViolations);

        _fuzzyScannerMock
            .Setup(s => s.ScanAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlySet<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fuzzyViolations);

        _readabilityServiceMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readabilityMetrics);

        _voiceAnalyzerMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(voiceResult);

        var pipeline = CreatePipeline();
        var request = CreateRequest("Test content");

        // Act
        var result = await pipeline.ExecuteAsync(request);

        // Assert
        result.IsPartialResult.Should().BeFalse();
        result.StyleViolations.Should().HaveCount(2);
        result.Readability.Should().NotBeNull();
        result.VoiceAnalysis.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
        result.ScannerDurations.Should().ContainKey("Regex");
        result.ScannerDurations.Should().ContainKey("Fuzzy");
        result.ScannerDurations.Should().ContainKey("Readability");
        result.ScannerDurations.Should().ContainKey("Voice");
    }

    [Fact]
    public async Task ExecuteAsync_OneFails_ReturnsPartialResult()
    {
        // Arrange
        _styleEngineMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test failure"));

        _fuzzyScannerMock
            .Setup(s => s.ScanAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlySet<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateViolation("fuzzy") });

        _readabilityServiceMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadabilityMetrics());

        _voiceAnalyzerMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceAnalysisResult.Empty);

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(CreateRequest("Test"));

        // Assert
        result.IsPartialResult.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InvalidOperationException>();
        result.StyleViolations.Should().HaveCount(1); // Fuzzy succeeded
    }

    [Fact]
    public async Task ExecuteAsync_Cancelled_ThrowsOperationCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        _styleEngineMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(500, ct);
                return Array.Empty<StyleViolation>();
            });

        _fuzzyScannerMock
            .Setup(s => s.ScanAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlySet<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string _, IReadOnlySet<string> _, CancellationToken ct) =>
            {
                await Task.Delay(500, ct);
                return Array.Empty<StyleViolation>();
            });

        _readabilityServiceMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(500, ct);
                return new ReadabilityMetrics();
            });

        _voiceAnalyzerMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(500, ct);
                return VoiceAnalysisResult.Empty;
            });

        var pipeline = CreatePipeline();

        // Act
        var task = pipeline.ExecuteAsync(CreateRequest("Test"), cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsDurations()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(50);

        _styleEngineMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(delay, ct);
                return Array.Empty<StyleViolation>();
            });

        _fuzzyScannerMock
            .Setup(s => s.ScanAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlySet<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string _, IReadOnlySet<string> _, CancellationToken ct) =>
            {
                await Task.Delay(delay, ct);
                return Array.Empty<StyleViolation>();
            });

        _readabilityServiceMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(delay, ct);
                return new ReadabilityMetrics();
            });

        _voiceAnalyzerMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(delay, ct);
                return VoiceAnalysisResult.Empty;
            });

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(CreateRequest("Test"));

        // Assert
        result.ScannerDurations.Should().HaveCount(4);
        result.ScannerDurations.Should().ContainKey("Regex");
        result.ScannerDurations.Should().ContainKey("Fuzzy");
        result.ScannerDurations.Should().ContainKey("Readability");
        result.ScannerDurations.Should().ContainKey("Voice");

        foreach (var duration in result.ScannerDurations.Values)
        {
            duration.TotalMilliseconds.Should().BeGreaterOrEqualTo(40);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ParallelIsFaster_ThanSequential()
    {
        // Arrange
        var scannerDelay = TimeSpan.FromMilliseconds(50);

        _styleEngineMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(scannerDelay, ct);
                return Array.Empty<StyleViolation>();
            });

        _fuzzyScannerMock
            .Setup(s => s.ScanAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlySet<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string _, IReadOnlySet<string> _, CancellationToken ct) =>
            {
                await Task.Delay(scannerDelay, ct);
                return Array.Empty<StyleViolation>();
            });

        _readabilityServiceMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(scannerDelay, ct);
                return new ReadabilityMetrics();
            });

        _voiceAnalyzerMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(scannerDelay, ct);
                return VoiceAnalysisResult.Empty;
            });

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(CreateRequest("Test"));

        // Assert
        // Sequential would be ~200ms (4 x 50ms), parallel should be ~50ms
        var sequentialWouldBe = result.ScannerDurations.Values.Sum(d => d.TotalMilliseconds);
        var actualTotal = result.TotalDuration.TotalMilliseconds;

        // Parallel should be at most 60% of sequential
        actualTotal.Should().BeLessThan(sequentialWouldBe * 0.7);
        result.SpeedupRatio.Should().BeGreaterThan(1.4);
    }

    [Fact]
    public async Task ExecuteAsync_AggregatesViolationsFromMultipleScanners()
    {
        // Arrange
        var regexViolations = new[]
        {
            CreateViolation("regex1"),
            CreateViolation("regex2")
        };
        var fuzzyViolations = new[]
        {
            CreateViolation("fuzzy1"),
            CreateViolation("fuzzy2"),
            CreateViolation("fuzzy3")
        };

        _styleEngineMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(regexViolations);

        _fuzzyScannerMock
            .Setup(s => s.ScanAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlySet<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fuzzyViolations);

        _readabilityServiceMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadabilityMetrics());

        _voiceAnalyzerMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceAnalysisResult.Empty);

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(CreateRequest("Test"));

        // Assert
        result.StyleViolations.Should().HaveCount(5);
        result.TotalViolationCount.Should().Be(5);
    }

    [Fact]
    public void ScannerNames_ReturnsAllFourScanners()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act
        var names = pipeline.ScannerNames;

        // Assert
        names.Should().HaveCount(4);
        names.Should().Contain("Regex");
        names.Should().Contain("Fuzzy");
        names.Should().Contain("Readability");
        names.Should().Contain("Voice");
    }
}
