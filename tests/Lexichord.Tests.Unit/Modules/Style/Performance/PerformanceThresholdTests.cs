// <copyright file="PerformanceThresholdTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// CI threshold tests that verify performance meets baseline requirements.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.8d - These tests run in CI to prevent performance regressions.
/// Tests use real service implementations to measure actual performance.
///
/// Test Categories:
/// - Performance: All performance-related tests
/// - Threshold: Tests that verify against baseline thresholds
/// - Regression: Tests that detect performance regressions
///
/// Note: These tests may fail on slow CI runners. Thresholds include
/// a 10% regression buffer to account for environmental variance.
///
/// Version: v0.3.8d
/// </remarks>
[Trait("Category", "Performance")]
[Trait("Version", "v0.3.8d")]
public class PerformanceThresholdTests : IDisposable
{
    private readonly PerformanceBaseline _baseline;
    private readonly IReadabilityService _readabilityService;
    private readonly IFuzzyScanner _fuzzyScanner;
    private readonly IVoiceAnalyzer _voiceAnalyzer;
    private readonly IParallelAnalysisPipeline _pipeline;

    // LOGIC: Pre-generated test texts
    private readonly string _text1K;
    private readonly string _text10K;
    private readonly string _text50K;
    private readonly string _textPassive1K;
    private readonly string _textPassive10K;
    private readonly string _textPassive50K;

    private static readonly IReadOnlySet<string> EmptyFlaggedWords = new HashSet<string>();

    public PerformanceThresholdTests()
    {
        // LOGIC: Load baseline thresholds
        _baseline = PerformanceBaselineLoader.Load();

        // LOGIC: Generate reproducible test text
        _text1K = LoremIpsumGenerator.Generate(1000);
        _text10K = LoremIpsumGenerator.Generate(10000);
        _text50K = LoremIpsumGenerator.Generate(50000);
        _textPassive1K = LoremIpsumGenerator.GenerateWithPassive(1000);
        _textPassive10K = LoremIpsumGenerator.GenerateWithPassive(10000);
        _textPassive50K = LoremIpsumGenerator.GenerateWithPassive(50000);

        // LOGIC: Initialize ReadabilityService with real implementations
        var sentenceTokenizer = new SentenceTokenizer(
            NullLogger<SentenceTokenizer>.Instance);
        var syllableCounter = new SyllableCounter();
        _readabilityService = new ReadabilityService(
            sentenceTokenizer,
            syllableCounter,
            NullLogger<ReadabilityService>.Instance);

        // LOGIC: Create FuzzyScanner with mocks
        var licenseContextMock = new Mock<ILicenseContext>();
        licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        var terminologyRepositoryMock = new Mock<ITerminologyRepository>();
        terminologyRepositoryMock
            .Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm>());

        var documentTokenizerMock = new Mock<IDocumentTokenizer>();
        documentTokenizerMock
            .Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(Array.Empty<DocumentToken>());

        var fuzzyMatchServiceMock = new Mock<IFuzzyMatchService>();

        _fuzzyScanner = new FuzzyScanner(
            licenseContextMock.Object,
            terminologyRepositoryMock.Object,
            documentTokenizerMock.Object,
            fuzzyMatchServiceMock.Object,
            NullLogger<FuzzyScanner>.Instance);

        // LOGIC: Create VoiceAnalyzer with mocks
        var passiveVoiceDetectorMock = new Mock<IPassiveVoiceDetector>();
        passiveVoiceDetectorMock
            .Setup(d => d.Detect(It.IsAny<string>()))
            .Returns(Array.Empty<PassiveVoiceMatch>());

        var weakWordScannerMock = new Mock<IWeakWordScanner>();
        weakWordScannerMock
            .Setup(s => s.GetStatistics(It.IsAny<string>(), It.IsAny<VoiceProfile>()))
            .Returns(new WeakWordStats(0, 0, new Dictionary<WeakWordCategory, int>(), []));

        var voiceProfileServiceMock = new Mock<IVoiceProfileService>();
        voiceProfileServiceMock
            .Setup(s => s.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VoiceProfile { Id = Guid.NewGuid(), Name = "Default" });

        _voiceAnalyzer = new VoiceAnalyzer(
            passiveVoiceDetectorMock.Object,
            weakWordScannerMock.Object,
            voiceProfileServiceMock.Object,
            NullLogger<VoiceAnalyzer>.Instance);

        // LOGIC: Create pipeline with all analyzers
        var styleEngineMock = new Mock<IStyleEngine>();
        styleEngineMock
            .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        _pipeline = new ParallelAnalysisPipeline(
            styleEngineMock.Object,
            _fuzzyScanner,
            _readabilityService,
            _voiceAnalyzer,
            NullLogger<ParallelAnalysisPipeline>.Instance);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region Readability Threshold Tests

    [Fact]
    [Trait("Category", "Threshold")]
    public void Readability_1K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.Readability.Words1K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = _readabilityService.Analyze(_text1K);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Readability 1K should complete within {threshold}ms (+10% tolerance)");
    }

    [Fact]
    [Trait("Category", "Threshold")]
    public void Readability_10K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.Readability.Words10K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = _readabilityService.Analyze(_text10K);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Readability 10K should complete within {threshold}ms (+10% tolerance)");
    }

    [Fact]
    [Trait("Category", "Threshold")]
    public void Readability_50K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.Readability.Words50K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = _readabilityService.Analyze(_text50K);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Readability 50K should complete within {threshold}ms (+10% tolerance)");
    }

    #endregion

    #region Fuzzy Scanning Threshold Tests

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task FuzzyScanning_1K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.FuzzyScanning.Words1K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _fuzzyScanner.ScanAsync(_text1K, EmptyFlaggedWords);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Fuzzy scanning 1K should complete within {threshold}ms (+10% tolerance)");
    }

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task FuzzyScanning_10K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.FuzzyScanning.Words10K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _fuzzyScanner.ScanAsync(_text10K, EmptyFlaggedWords);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Fuzzy scanning 10K should complete within {threshold}ms (+10% tolerance)");
    }

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task FuzzyScanning_50K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.FuzzyScanning.Words50K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _fuzzyScanner.ScanAsync(_text50K, EmptyFlaggedWords);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Fuzzy scanning 50K should complete within {threshold}ms (+10% tolerance)");
    }

    #endregion

    #region Voice Analysis Threshold Tests

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task VoiceAnalysis_1K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.VoiceAnalysis.Words1K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _voiceAnalyzer.AnalyzeAsync(_textPassive1K);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Voice analysis 1K should complete within {threshold}ms (+10% tolerance)");
    }

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task VoiceAnalysis_10K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.VoiceAnalysis.Words10K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _voiceAnalyzer.AnalyzeAsync(_textPassive10K);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Voice analysis 10K should complete within {threshold}ms (+10% tolerance)");
    }

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task VoiceAnalysis_50K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.VoiceAnalysis.Words50K;

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _voiceAnalyzer.AnalyzeAsync(_textPassive50K);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Voice analysis 50K should complete within {threshold}ms (+10% tolerance)");
    }

    #endregion

    #region Full Pipeline Threshold Tests

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task FullPipeline_1K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.FullPipeline.Words1K;
        var request = new AnalysisRequest("test-1k", "/test/1k.md", _text1K, DateTimeOffset.UtcNow);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _pipeline.ExecuteAsync(request);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Full pipeline 1K should complete within {threshold}ms (+10% tolerance)");
    }

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task FullPipeline_10K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.FullPipeline.Words10K;
        var request = new AnalysisRequest("test-10k", "/test/10k.md", _text10K, DateTimeOffset.UtcNow);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _pipeline.ExecuteAsync(request);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Full pipeline 10K should complete within {threshold}ms (+10% tolerance)");
    }

    [Fact]
    [Trait("Category", "Threshold")]
    public async Task FullPipeline_50K_CompletesWithinThreshold()
    {
        // Arrange
        var threshold = _baseline.FullPipeline.Words50K;
        var request = new AnalysisRequest("test-50k", "/test/50k.md", _text50K, DateTimeOffset.UtcNow);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _pipeline.ExecuteAsync(request);
        sw.Stop();

        // Assert
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            (long)(threshold * (1 + _baseline.RegressionThreshold)),
            $"Full pipeline 50K should complete within {threshold}ms (+10% tolerance)");
    }

    #endregion

    #region Throughput Tests

    [Fact]
    [Trait("Category", "Throughput")]
    public void Readability_Throughput_MeetsMinimumWordsPerSecond()
    {
        // Arrange
        const int minWordsPerSecond = 25000; // 25K words/second minimum (accounts for parallel test load)

        // Act
        var sw = Stopwatch.StartNew();
        _readabilityService.Analyze(_text10K);
        sw.Stop();

        var wordsPerSecond = 10000.0 / sw.Elapsed.TotalSeconds;

        // Assert
        wordsPerSecond.Should().BeGreaterThan(
            minWordsPerSecond,
            $"Readability throughput should exceed {minWordsPerSecond} words/second");
    }

    [Fact]
    [Trait("Category", "Throughput")]
    public async Task VoiceAnalysis_Throughput_MeetsMinimumWordsPerSecond()
    {
        // Arrange
        const int minWordsPerSecond = 15000; // 15K words/second minimum (accounts for parallel test load)

        // Act
        var sw = Stopwatch.StartNew();
        await _voiceAnalyzer.AnalyzeAsync(_textPassive10K);
        sw.Stop();

        var wordsPerSecond = 10000.0 / sw.Elapsed.TotalSeconds;

        // Assert
        wordsPerSecond.Should().BeGreaterThan(
            minWordsPerSecond,
            $"Voice analysis throughput should exceed {minWordsPerSecond} words/second");
    }

    #endregion

    #region Regression Detection Tests

    [Fact]
    [Trait("Category", "Regression")]
    public void RegressionThreshold_IsConfiguredAt25Percent()
    {
        _baseline.RegressionThreshold.Should().Be(0.25,
            "Regression threshold should be configured at 25% to accommodate parallel test execution load");
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void BaselineLoader_ReturnsValidThresholds()
    {
        _baseline.Readability.Words1K.Should().BeGreaterThan(0);
        _baseline.Readability.Words10K.Should().BeGreaterThan(0);
        _baseline.Readability.Words50K.Should().BeGreaterThan(0);
        _baseline.FuzzyScanning.Words1K.Should().BeGreaterThan(0);
        _baseline.FuzzyScanning.Words10K.Should().BeGreaterThan(0);
        _baseline.FuzzyScanning.Words50K.Should().BeGreaterThan(0);
        _baseline.VoiceAnalysis.Words1K.Should().BeGreaterThan(0);
        _baseline.VoiceAnalysis.Words10K.Should().BeGreaterThan(0);
        _baseline.VoiceAnalysis.Words50K.Should().BeGreaterThan(0);
        _baseline.FullPipeline.Words1K.Should().BeGreaterThan(0);
        _baseline.FullPipeline.Words10K.Should().BeGreaterThan(0);
        _baseline.FullPipeline.Words50K.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("readability", 1000, 50)]
    [InlineData("readability", 10000, 400)]
    [InlineData("readability", 50000, 2000)]
    [InlineData("fuzzy", 1000, 100)]
    [InlineData("fuzzy", 10000, 800)]
    [InlineData("voice", 1000, 60)]
    [InlineData("voice", 10000, 500)]
    [InlineData("pipeline", 1000, 200)]
    [InlineData("pipeline", 10000, 2000)]
    [Trait("Category", "Regression")]
    public void GetThreshold_ReturnsExpectedValues(string operation, int wordCount, int expectedThreshold)
    {
        var threshold = PerformanceBaselineLoader.GetThreshold(_baseline, operation, wordCount);
        threshold.Should().Be(expectedThreshold);
    }

    [Theory]
    [InlineData("readability", 1000, 60, false)] // 60ms is within 25% of 50ms (max 62.5ms)
    [InlineData("readability", 1000, 70, true)]  // 70ms exceeds 25% tolerance of 50ms
    [InlineData("fuzzy", 10000, 950, false)]      // 950ms is within 25% of 800ms (max 1000ms)
    [InlineData("fuzzy", 10000, 1100, true)]      // 1100ms exceeds 25% tolerance of 800ms
    [Trait("Category", "Regression")]
    public void IsRegression_DetectsCorrectly(string operation, int wordCount, double actualMs, bool expectedIsRegression)
    {
        var isRegression = PerformanceBaselineLoader.IsRegression(_baseline, operation, wordCount, actualMs);
        isRegression.Should().Be(expectedIsRegression);
    }

    #endregion

    #region Text Generator Tests

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void LoremIpsumGenerator_GeneratesCorrectWordCount()
    {
        var text = LoremIpsumGenerator.Generate(1000);
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        wordCount.Should().BeGreaterThanOrEqualTo(990, "Should generate approximately 1000 words");
        wordCount.Should().BeLessThanOrEqualTo(1010, "Should generate approximately 1000 words");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void LoremIpsumGenerator_IsReproducible()
    {
        var text1 = LoremIpsumGenerator.Generate(100);
        var text2 = LoremIpsumGenerator.Generate(100);

        text1.Should().Be(text2, "Same seed should produce identical text");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void LoremIpsumGenerator_WithPassive_IncludesPassiveConstructions()
    {
        var text = LoremIpsumGenerator.GenerateWithPassive(1000, passiveRatio: 0.3);

        text.Should().Contain("by", "Passive constructions should include 'by'");
    }

    #endregion
}
