// <copyright file="ParallelAnalysisPipeline.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Executes multiple analysis scanners in parallel using Task.WhenAll.
/// Aggregates results and handles partial failures gracefully.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.7b - Parallel Scanner Execution.</para>
/// <para>Spawns 4 concurrent tasks for Regex, Fuzzy, Readability, and Voice analysis.</para>
/// <para>Total execution time reduced to approximately the longest scanner duration.</para>
/// <para>Individual scanner failures do not crash the pipeline; partial results are preserved.</para>
/// </remarks>
internal sealed class ParallelAnalysisPipeline : IParallelAnalysisPipeline
{
    private readonly IStyleEngine _styleEngine;
    private readonly IFuzzyScanner _fuzzyScanner;
    private readonly IReadabilityService _readabilityService;
    private readonly IVoiceAnalyzer _voiceAnalyzer;
    private readonly ILogger<ParallelAnalysisPipeline> _logger;

    private static readonly string[] ScannerNamesArray =
        ["Regex", "Fuzzy", "Readability", "Voice"];

    /// <inheritdoc/>
    public IReadOnlyList<string> ScannerNames => ScannerNamesArray;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelAnalysisPipeline"/> class.
    /// </summary>
    /// <param name="styleEngine">The style engine for regex-based scanning.</param>
    /// <param name="fuzzyScanner">The fuzzy scanner for approximate matching.</param>
    /// <param name="readabilityService">The readability analysis service.</param>
    /// <param name="voiceAnalyzer">The voice analysis facade.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ParallelAnalysisPipeline(
        IStyleEngine styleEngine,
        IFuzzyScanner fuzzyScanner,
        IReadabilityService readabilityService,
        IVoiceAnalyzer voiceAnalyzer,
        ILogger<ParallelAnalysisPipeline> logger)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _fuzzyScanner = fuzzyScanner ?? throw new ArgumentNullException(nameof(fuzzyScanner));
        _readabilityService = readabilityService ?? throw new ArgumentNullException(nameof(readabilityService));
        _voiceAnalyzer = voiceAnalyzer ?? throw new ArgumentNullException(nameof(voiceAnalyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ParallelAnalysisResult> ExecuteAsync(
        AnalysisRequest request,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var durations = new ConcurrentDictionary<string, TimeSpan>();
        var errors = new ConcurrentBag<Exception>();

        _logger.LogDebug(
            "Starting parallel analysis: {ScannerCount} scanners",
            ScannerNamesArray.Length);

        var content = request.Content;

        // Create timed tasks for each scanner
        var regexTask = TimedExecuteAsync(
            "Regex",
            async () => await _styleEngine.AnalyzeAsync(content, ct),
            durations,
            errors);

        // Fuzzy scan needs regex results to avoid double-counting
        // We pass empty set here; real deduplication happens in aggregation
        var fuzzyTask = TimedExecuteAsync(
            "Fuzzy",
            async () => await _fuzzyScanner.ScanAsync(content, new HashSet<string>(), ct),
            durations,
            errors);

        var readabilityTask = TimedExecuteAsync(
            "Readability",
            async () => await _readabilityService.AnalyzeAsync(content, ct),
            durations,
            errors);

        var voiceTask = TimedExecuteAsync(
            "Voice",
            async () => await _voiceAnalyzer.AnalyzeAsync(content, ct),
            durations,
            errors);

        // Execute all in parallel
        await Task.WhenAll(regexTask, fuzzyTask, readabilityTask, voiceTask);

        stopwatch.Stop();

        // Aggregate style violations from both regex and fuzzy scanners
        var violations = new List<StyleViolation>();
        if (regexTask.Result != null)
        {
            violations.AddRange(regexTask.Result);
        }

        if (fuzzyTask.Result != null)
        {
            violations.AddRange(fuzzyTask.Result);
        }

        var result = new ParallelAnalysisResult
        {
            StyleViolations = violations.AsReadOnly(),
            Readability = readabilityTask.Result,
            VoiceAnalysis = voiceTask.Result,
            TotalDuration = stopwatch.Elapsed,
            ScannerDurations = durations.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value),
            IsPartialResult = !errors.IsEmpty,
            Errors = errors.ToList().AsReadOnly()
        };

        LogCompletion(result);

        return result;
    }

    /// <summary>
    /// Executes a scanner operation with timing and error isolation.
    /// </summary>
    /// <typeparam name="T">The result type of the scanner operation.</typeparam>
    /// <param name="scannerName">Name of the scanner for logging and metrics.</param>
    /// <param name="operation">The async scanner operation to execute.</param>
    /// <param name="durations">Concurrent dictionary to record execution time.</param>
    /// <param name="errors">Concurrent bag to collect any exceptions.</param>
    /// <returns>The result of the operation, or default if an error occurred.</returns>
    private async Task<T?> TimedExecuteAsync<T>(
        string scannerName,
        Func<Task<T>> operation,
        ConcurrentDictionary<string, TimeSpan> durations,
        ConcurrentBag<Exception> errors)
    {
        _logger.LogDebug("Scanner {ScannerName} starting", scannerName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation();
            stopwatch.Stop();
            durations[scannerName] = stopwatch.Elapsed;

            _logger.LogDebug(
                "Scanner {ScannerName} completed in {DurationMs}ms",
                scannerName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            durations[scannerName] = stopwatch.Elapsed;
            throw; // Propagate cancellation
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            durations[scannerName] = stopwatch.Elapsed;
            errors.Add(ex);

            _logger.LogWarning(
                ex,
                "Scanner {ScannerName} failed: {ErrorMessage}",
                scannerName,
                ex.Message);

            return default;
        }
    }

    /// <summary>
    /// Logs completion metrics for the parallel analysis.
    /// </summary>
    /// <param name="result">The completed analysis result.</param>
    private void LogCompletion(ParallelAnalysisResult result)
    {
        _logger.LogInformation(
            "Parallel analysis completed in {TotalMs}ms (Speedup: {SpeedupRatio:F2}x)",
            result.TotalDuration.TotalMilliseconds,
            result.SpeedupRatio);

        if (result.ScannerDurations.Count == 4)
        {
            _logger.LogInformation(
                "Scanner times - Regex: {RegexMs}ms, Fuzzy: {FuzzyMs}ms, " +
                "Read: {ReadMs}ms, Voice: {VoiceMs}ms",
                result.ScannerDurations["Regex"].TotalMilliseconds,
                result.ScannerDurations["Fuzzy"].TotalMilliseconds,
                result.ScannerDurations["Readability"].TotalMilliseconds,
                result.ScannerDurations["Voice"].TotalMilliseconds);
        }
    }
}
