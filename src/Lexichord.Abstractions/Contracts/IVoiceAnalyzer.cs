// <copyright file="IVoiceAnalyzer.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Unified facade for voice-related text analysis.
/// Combines passive voice detection and weak word scanning into a single interface.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.7b - Voice analysis facade for parallel pipeline.</para>
/// <para>Wraps IPassiveVoiceDetector and IWeakWordScanner.</para>
/// <para>Thread-safe: implementations should be stateless.</para>
/// <para>License: Writer Pro tier (inherited from underlying scanners).</para>
/// </remarks>
/// <example>
/// <code>
/// var result = await _voiceAnalyzer.AnalyzeAsync(content, cancellationToken);
/// Console.WriteLine($"Passive constructions: {result.PassiveVoice.Instances.Count}");
/// Console.WriteLine($"Weak words: {result.WeakWords.Matches.Count}");
/// </code>
/// </example>
public interface IVoiceAnalyzer
{
    /// <summary>
    /// Analyzes text for voice-related metrics including passive voice and weak words.
    /// </summary>
    /// <param name="content">The text content to analyze.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>Combined voice analysis results.</returns>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    Task<VoiceAnalysisResult> AnalyzeAsync(string content, CancellationToken ct = default);
}
