// <copyright file="AnalysisBenchmarks.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// BenchmarkDotNet benchmarks for Lexichord analysis operations.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.8d - Comprehensive performance profiling with memory tracking.
///
/// Run benchmarks with:
/// dotnet run -c Release -- --filter "*AnalysisBenchmarks*"
///
/// Benchmark Categories:
/// - Readability: Flesch-Kincaid, Gunning Fog, Reading Ease
/// - Fuzzy: Levenshtein-based terminology scanning
/// - Voice: Passive voice and weak word detection
/// - Pipeline: Full parallel analysis execution
///
/// Word Count Tiers:
/// - 1K: Small documents (quick edits)
/// - 10K: Medium documents (typical manuscripts)
/// - 50K: Large documents (novels, technical manuals)
///
/// Version: v0.3.8d
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class AnalysisBenchmarks
{
    // LOGIC: Pre-generated text for each word count tier
    private string _text1K = string.Empty;
    private string _text10K = string.Empty;
    private string _text50K = string.Empty;
    private string _textPassive1K = string.Empty;
    private string _textPassive10K = string.Empty;
    private string _textPassive50K = string.Empty;

    // LOGIC: Service instances (real implementations where possible)
    private IReadabilityService _readabilityService = null!;
    private IFuzzyScanner _fuzzyScanner = null!;
    private IVoiceAnalyzer _voiceAnalyzer = null!;
    private IParallelAnalysisPipeline _pipeline = null!;

    // LOGIC: Empty set for fuzzy scanner (no pre-flagged words)
    private static readonly IReadOnlySet<string> EmptyFlaggedWords = new HashSet<string>();

    /// <summary>
    /// Global setup executed once before all benchmarks.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        // LOGIC: Generate reproducible test text
        _text1K = LoremIpsumGenerator.Generate(1000);
        _text10K = LoremIpsumGenerator.Generate(10000);
        _text50K = LoremIpsumGenerator.Generate(50000);

        // LOGIC: Generate text with passive voice for voice analysis
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

        // LOGIC: Create FuzzyScanner with mocks (fast path for benchmarking)
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

    #region Readability Benchmarks

    /// <summary>
    /// Benchmark readability analysis on 1,000 words.
    /// </summary>
    [Benchmark(Description = "Readability 1K words")]
    [BenchmarkCategory("Readability", "1K")]
    public ReadabilityMetrics Readability_1K() => _readabilityService.Analyze(_text1K);

    /// <summary>
    /// Benchmark readability analysis on 10,000 words.
    /// </summary>
    [Benchmark(Description = "Readability 10K words")]
    [BenchmarkCategory("Readability", "10K")]
    public ReadabilityMetrics Readability_10K() => _readabilityService.Analyze(_text10K);

    /// <summary>
    /// Benchmark readability analysis on 50,000 words.
    /// </summary>
    [Benchmark(Description = "Readability 50K words")]
    [BenchmarkCategory("Readability", "50K")]
    public ReadabilityMetrics Readability_50K() => _readabilityService.Analyze(_text50K);

    #endregion

    #region Fuzzy Scanning Benchmarks

    /// <summary>
    /// Benchmark fuzzy scanning on 1,000 words.
    /// </summary>
    [Benchmark(Description = "Fuzzy Scan 1K words")]
    [BenchmarkCategory("Fuzzy", "1K")]
    public async Task<IReadOnlyList<StyleViolation>> FuzzyScan_1K() =>
        await _fuzzyScanner.ScanAsync(_text1K, EmptyFlaggedWords);

    /// <summary>
    /// Benchmark fuzzy scanning on 10,000 words.
    /// </summary>
    [Benchmark(Description = "Fuzzy Scan 10K words")]
    [BenchmarkCategory("Fuzzy", "10K")]
    public async Task<IReadOnlyList<StyleViolation>> FuzzyScan_10K() =>
        await _fuzzyScanner.ScanAsync(_text10K, EmptyFlaggedWords);

    /// <summary>
    /// Benchmark fuzzy scanning on 50,000 words.
    /// </summary>
    [Benchmark(Description = "Fuzzy Scan 50K words")]
    [BenchmarkCategory("Fuzzy", "50K")]
    public async Task<IReadOnlyList<StyleViolation>> FuzzyScan_50K() =>
        await _fuzzyScanner.ScanAsync(_text50K, EmptyFlaggedWords);

    #endregion

    #region Voice Analysis Benchmarks

    /// <summary>
    /// Benchmark voice analysis on 1,000 words.
    /// </summary>
    [Benchmark(Description = "Voice Analysis 1K words")]
    [BenchmarkCategory("Voice", "1K")]
    public async Task<VoiceAnalysisResult> VoiceAnalysis_1K() =>
        await _voiceAnalyzer.AnalyzeAsync(_textPassive1K);

    /// <summary>
    /// Benchmark voice analysis on 10,000 words.
    /// </summary>
    [Benchmark(Description = "Voice Analysis 10K words")]
    [BenchmarkCategory("Voice", "10K")]
    public async Task<VoiceAnalysisResult> VoiceAnalysis_10K() =>
        await _voiceAnalyzer.AnalyzeAsync(_textPassive10K);

    /// <summary>
    /// Benchmark voice analysis on 50,000 words.
    /// </summary>
    [Benchmark(Description = "Voice Analysis 50K words")]
    [BenchmarkCategory("Voice", "50K")]
    public async Task<VoiceAnalysisResult> VoiceAnalysis_50K() =>
        await _voiceAnalyzer.AnalyzeAsync(_textPassive50K);

    #endregion

    #region Full Pipeline Benchmarks

    /// <summary>
    /// Benchmark full parallel pipeline on 1,000 words.
    /// </summary>
    [Benchmark(Description = "Full Pipeline 1K words")]
    [BenchmarkCategory("Pipeline", "1K")]
    public async Task<ParallelAnalysisResult> FullPipeline_1K()
    {
        var request = new AnalysisRequest("bench-1k", "/bench/1k.md", _text1K, DateTimeOffset.UtcNow);
        return await _pipeline.ExecuteAsync(request);
    }

    /// <summary>
    /// Benchmark full parallel pipeline on 10,000 words.
    /// </summary>
    [Benchmark(Description = "Full Pipeline 10K words")]
    [BenchmarkCategory("Pipeline", "10K")]
    public async Task<ParallelAnalysisResult> FullPipeline_10K()
    {
        var request = new AnalysisRequest("bench-10k", "/bench/10k.md", _text10K, DateTimeOffset.UtcNow);
        return await _pipeline.ExecuteAsync(request);
    }

    /// <summary>
    /// Benchmark full parallel pipeline on 50,000 words.
    /// </summary>
    [Benchmark(Description = "Full Pipeline 50K words")]
    [BenchmarkCategory("Pipeline", "50K")]
    public async Task<ParallelAnalysisResult> FullPipeline_50K()
    {
        var request = new AnalysisRequest("bench-50k", "/bench/50k.md", _text50K, DateTimeOffset.UtcNow);
        return await _pipeline.ExecuteAsync(request);
    }

    #endregion
}
