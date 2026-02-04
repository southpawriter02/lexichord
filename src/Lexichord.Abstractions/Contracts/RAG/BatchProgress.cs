// =============================================================================
// File: BatchProgress.cs
// Project: Lexichord.Abstractions
// Description: Progress update record for batch deduplication job monitoring.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Provides real-time progress information for IProgress<BatchProgress>.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents a progress update during batch deduplication execution.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This record is used with <see cref="IProgress{T}"/> to report real-time
/// progress during batch job execution. Updates are published:
/// </para>
/// <list type="bullet">
///   <item><description>After each batch completes.</description></item>
///   <item><description>When job state changes.</description></item>
///   <item><description>At regular intervals for long-running batches.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and safe to use across threads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var progress = new Progress&lt;BatchProgress&gt;(update =>
/// {
///     Console.WriteLine($"Progress: {update.PercentComplete:P1}");
///     Console.WriteLine($"Processed: {update.ChunksProcessed} / {update.TotalChunks}");
///     Console.WriteLine($"ETA: {update.EstimatedRemaining}");
/// });
/// 
/// await batchJob.ExecuteAsync(options, progress, ct);
/// </code>
/// </example>
/// <param name="ChunksProcessed">The number of chunks processed so far.</param>
/// <param name="TotalChunks">The total number of chunks to process, or null if unknown.</param>
/// <param name="PercentComplete">
/// The completion percentage as a value between 0.0 and 1.0.
/// May be null if total is unknown.
/// </param>
/// <param name="Elapsed">The time elapsed since job start.</param>
/// <param name="EstimatedRemaining">
/// Estimated time remaining based on current throughput.
/// May be null if estimate is not available.
/// </param>
/// <param name="CurrentOperation">
/// Description of the current operation (e.g., "Analyzing batch 15 of 100").
/// </param>
public record BatchProgress(
    int ChunksProcessed,
    int? TotalChunks,
    double? PercentComplete,
    TimeSpan Elapsed,
    TimeSpan? EstimatedRemaining,
    string CurrentOperation)
{
    /// <summary>
    /// Creates a new progress update with current statistics.
    /// </summary>
    /// <param name="processed">Chunks processed so far.</param>
    /// <param name="total">Total chunks to process, or null if unknown.</param>
    /// <param name="elapsed">Time elapsed since start.</param>
    /// <param name="operation">Current operation description.</param>
    /// <returns>A new <see cref="BatchProgress"/> with computed estimated time.</returns>
    /// <remarks>
    /// Automatically calculates <see cref="PercentComplete"/> and <see cref="EstimatedRemaining"/>
    /// based on the provided values and current throughput.
    /// </remarks>
    public static BatchProgress Create(int processed, int? total, TimeSpan elapsed, string operation)
    {
        double? percentComplete = null;
        TimeSpan? estimatedRemaining = null;

        if (total.HasValue && total.Value > 0)
        {
            percentComplete = (double)processed / total.Value;

            if (processed > 0)
            {
                var msPerChunk = elapsed.TotalMilliseconds / processed;
                var remainingChunks = total.Value - processed;
                estimatedRemaining = TimeSpan.FromMilliseconds(msPerChunk * remainingChunks);
            }
        }

        return new BatchProgress(
            ChunksProcessed: processed,
            TotalChunks: total,
            PercentComplete: percentComplete,
            Elapsed: elapsed,
            EstimatedRemaining: estimatedRemaining,
            CurrentOperation: operation);
    }

    /// <summary>
    /// Creates an initial progress update before processing begins.
    /// </summary>
    /// <param name="total">Total chunks to process, or null if counting.</param>
    /// <returns>A new <see cref="BatchProgress"/> representing job start.</returns>
    public static BatchProgress Initial(int? total = null) => new(
        ChunksProcessed: 0,
        TotalChunks: total,
        PercentComplete: 0.0,
        Elapsed: TimeSpan.Zero,
        EstimatedRemaining: null,
        CurrentOperation: total.HasValue ? $"Starting job with {total:N0} chunks" : "Counting chunks...");

    /// <summary>
    /// Creates a final progress update when the job completes.
    /// </summary>
    /// <param name="processed">Final count of processed chunks.</param>
    /// <param name="elapsed">Total elapsed time.</param>
    /// <returns>A new <see cref="BatchProgress"/> representing job completion.</returns>
    public static BatchProgress Completed(int processed, TimeSpan elapsed) => new(
        ChunksProcessed: processed,
        TotalChunks: processed,
        PercentComplete: 1.0,
        Elapsed: elapsed,
        EstimatedRemaining: TimeSpan.Zero,
        CurrentOperation: "Completed");
}
