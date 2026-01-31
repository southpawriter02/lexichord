// <copyright file="VoiceAnalyzer.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Unified facade for voice-related text analysis.
/// Combines passive voice detection and weak word scanning into a single async interface.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.7b - Voice analysis facade for parallel pipeline.</para>
/// <para>Wraps <see cref="IPassiveVoiceDetector"/> and <see cref="IWeakWordScanner"/>.</para>
/// <para>Thread-safe: stateless service suitable for concurrent access.</para>
/// </remarks>
internal sealed class VoiceAnalyzer : IVoiceAnalyzer
{
    private readonly IPassiveVoiceDetector _passiveVoiceDetector;
    private readonly IWeakWordScanner _weakWordScanner;
    private readonly IVoiceProfileService _voiceProfileService;
    private readonly ILogger<VoiceAnalyzer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceAnalyzer"/> class.
    /// </summary>
    /// <param name="passiveVoiceDetector">The passive voice detector service.</param>
    /// <param name="weakWordScanner">The weak word scanner service.</param>
    /// <param name="voiceProfileService">The voice profile service for getting active profile.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public VoiceAnalyzer(
        IPassiveVoiceDetector passiveVoiceDetector,
        IWeakWordScanner weakWordScanner,
        IVoiceProfileService voiceProfileService,
        ILogger<VoiceAnalyzer> logger)
    {
        _passiveVoiceDetector = passiveVoiceDetector ??
            throw new ArgumentNullException(nameof(passiveVoiceDetector));
        _weakWordScanner = weakWordScanner ??
            throw new ArgumentNullException(nameof(weakWordScanner));
        _voiceProfileService = voiceProfileService ??
            throw new ArgumentNullException(nameof(voiceProfileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<VoiceAnalysisResult> AnalyzeAsync(
        string content,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return VoiceAnalysisResult.Empty;
        }

        ct.ThrowIfCancellationRequested();

        _logger.LogDebug("Starting voice analysis for {ContentLength} characters", content.Length);

        // Run both analyses in parallel using Task.Run for CPU-bound work
        var passiveTask = Task.Run(() => _passiveVoiceDetector.Detect(content), ct);

        var profile = await _voiceProfileService.GetActiveProfileAsync(ct);
        var weakWordsTask = Task.Run(() => _weakWordScanner.GetStatistics(content, profile), ct);

        await Task.WhenAll(passiveTask, weakWordsTask);

        var result = new VoiceAnalysisResult
        {
            PassiveVoiceMatches = passiveTask.Result,
            WeakWordStats = weakWordsTask.Result
        };

        _logger.LogDebug(
            "Voice analysis complete: {PassiveCount} passive instances, {WeakWordCount} weak words",
            result.PassiveVoiceMatches.Count,
            result.WeakWordStats?.TotalWeakWords ?? 0);

        return result;
    }
}
