// <copyright file="IReadabilityService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for calculating readability metrics from text.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.3c - The readability calculator provides writing difficulty analysis.</para>
/// <para>Implements three industry-standard readability formulas:</para>
/// <list type="bullet">
///   <item>Flesch-Kincaid Grade Level - U.S. school grade required</item>
///   <item>Gunning Fog Index - Years of formal education needed</item>
///   <item>Flesch Reading Ease - 0-100 scale (higher = easier)</item>
/// </list>
/// <para>Thread-safe: stateless service suitable for concurrent access.</para>
/// <para>License: Writer Pro tier (soft-gated).</para>
/// </remarks>
/// <example>
/// <code>
/// var metrics = readabilityService.Analyze("The quick brown fox jumps over the lazy dog.");
/// Console.WriteLine($"Grade Level: {metrics.FleschKincaidGradeLevel:F1}");
/// Console.WriteLine($"Fog Index: {metrics.GunningFogIndex:F1}");
/// Console.WriteLine($"Reading Ease: {metrics.FleschReadingEase:F0} ({metrics.ReadingEaseInterpretation})");
/// </code>
/// </example>
public interface IReadabilityService
{
    /// <summary>
    /// Analyzes text and returns readability metrics.
    /// </summary>
    /// <param name="text">The text to analyze. MAY be null or empty.</param>
    /// <returns>
    /// A <see cref="ReadabilityMetrics"/> record containing the analysis results.
    /// Returns <see cref="ReadabilityMetrics.Empty"/> for null, empty, or whitespace-only input.
    /// </returns>
    /// <remarks>
    /// LOGIC: Synchronous analysis suitable for small to medium texts.
    /// For large documents, prefer <see cref="AnalyzeAsync"/> to avoid blocking the UI thread.
    /// </remarks>
    ReadabilityMetrics Analyze(string text);

    /// <summary>
    /// Asynchronously analyzes text and returns readability metrics.
    /// </summary>
    /// <param name="text">The text to analyze. MAY be null or empty.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A task that resolves to a <see cref="ReadabilityMetrics"/> record.
    /// Returns <see cref="ReadabilityMetrics.Empty"/> for null, empty, or whitespace-only input.
    /// </returns>
    /// <remarks>
    /// LOGIC: Executes analysis on a background thread via Task.Run.
    /// Suitable for large documents or when called from the UI thread.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    Task<ReadabilityMetrics> AnalyzeAsync(string text, CancellationToken ct = default);
}
