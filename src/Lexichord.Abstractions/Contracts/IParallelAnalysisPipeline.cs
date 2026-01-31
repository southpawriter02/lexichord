// <copyright file="IParallelAnalysisPipeline.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Orchestrates parallel execution of multiple analysis scanners.
/// All registered scanners run concurrently using Task.WhenAll(),
/// reducing total analysis time to approximately the longest scanner.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.7b - Parallel Scanner Execution.</para>
/// <para>The pipeline SHALL execute all scanners in parallel.</para>
/// <para>The pipeline SHALL aggregate results from all scanners.</para>
/// <para>The pipeline SHALL handle individual scanner failures gracefully,
/// returning partial results with error information.</para>
/// <para>The pipeline SHALL propagate cancellation to all scanners.</para>
/// </remarks>
/// <example>
/// <code>
/// var result = await _pipeline.ExecuteAsync(request, cancellationToken);
///
/// Console.WriteLine($"Total time: {result.TotalDuration.TotalMilliseconds}ms");
/// Console.WriteLine($"Regex: {result.ScannerDurations["Regex"].TotalMilliseconds}ms");
/// Console.WriteLine($"Violations: {result.StyleViolations.Count}");
/// </code>
/// </example>
public interface IParallelAnalysisPipeline
{
    /// <summary>
    /// Executes all registered scanners in parallel on the provided document.
    /// </summary>
    /// <param name="request">The analysis request containing document content.</param>
    /// <param name="ct">Cancellation token for aborting analysis.</param>
    /// <returns>Aggregated results from all scanners.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when cancellation is requested before completion.
    /// </exception>
    Task<ParallelAnalysisResult> ExecuteAsync(
        AnalysisRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the names of all registered scanners.
    /// </summary>
    IReadOnlyList<string> ScannerNames { get; }
}
