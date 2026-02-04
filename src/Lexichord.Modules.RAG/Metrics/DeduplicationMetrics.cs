// =============================================================================
// File: DeduplicationMetrics.cs
// Project: Lexichord.Modules.RAG
// Description: Static metrics definitions for deduplication observability.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Provides lightweight internal metrics tracking for deduplication operations.
//   - Uses Interlocked operations for thread-safe counter increments.
//   - Histogram buckets use pre-defined ranges for P50/P90/P99 calculations.
//   - Designed for future export to Prometheus/OpenTelemetry backends.
// =============================================================================

using System.Collections.Concurrent;

namespace Lexichord.Modules.RAG.Metrics;

/// <summary>
/// Static metrics definitions for the deduplication pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// This class provides lightweight, thread-safe metrics tracking using
/// <see cref="Interlocked"/> operations for counters and lock-free data
/// structures for histograms.
/// </para>
/// <para>
/// <b>Design:</b> The metrics are exposed as static members to allow low-overhead
/// recording from any service without dependency injection overhead. The service
/// layer (<see cref="Lexichord.Modules.RAG.Services.DeduplicationMetricsService"/>)
/// provides higher-level aggregation and dashboard data.
/// </para>
/// <para>
/// <b>Future Extension:</b> These internal metrics can be exported to external
/// monitoring systems (Prometheus, OpenTelemetry) by implementing an exporter
/// that reads from these static counters.
/// </para>
/// </remarks>
public static class DeduplicationMetrics
{
    #region Counters

    /// <summary>
    /// Total chunks processed through the deduplication pipeline.
    /// </summary>
    private static long _chunksProcessedTotal;

    /// <summary>
    /// Chunks stored as new canonical records.
    /// </summary>
    private static long _chunksStoredAsNew;

    /// <summary>
    /// Chunks merged into existing canonical records.
    /// </summary>
    private static long _chunksMerged;

    /// <summary>
    /// Chunks linked to existing records as complementary.
    /// </summary>
    private static long _chunksLinked;

    /// <summary>
    /// Chunks flagged as contradictions.
    /// </summary>
    private static long _chunksFlaggedContradiction;

    /// <summary>
    /// Chunks queued for manual review.
    /// </summary>
    private static long _chunksQueuedForReview;

    /// <summary>
    /// Processing errors encountered.
    /// </summary>
    private static long _processingErrors;

    /// <summary>
    /// Total similarity queries executed.
    /// </summary>
    private static long _similarityQueriesTotal;

    /// <summary>
    /// Total classification requests.
    /// </summary>
    private static long _classificationRequestsTotal;

    /// <summary>
    /// Rule-based classifications performed.
    /// </summary>
    private static long _classificationsRuleBased;

    /// <summary>
    /// LLM-based classifications performed.
    /// </summary>
    private static long _classificationsLlmBased;

    /// <summary>
    /// Cached classifications returned.
    /// </summary>
    private static long _classificationsCached;

    /// <summary>
    /// Total contradictions detected.
    /// </summary>
    private static long _contradictionsDetectedTotal;

    /// <summary>
    /// Low severity contradictions detected.
    /// </summary>
    private static long _contradictionsLow;

    /// <summary>
    /// Medium severity contradictions detected.
    /// </summary>
    private static long _contradictionsMedium;

    /// <summary>
    /// High severity contradictions detected.
    /// </summary>
    private static long _contradictionsHigh;

    /// <summary>
    /// Critical severity contradictions detected.
    /// </summary>
    private static long _contradictionsCritical;

    /// <summary>
    /// Total batch jobs completed.
    /// </summary>
    private static long _batchJobsCompleted;

    /// <summary>
    /// Total matches found across all similarity queries.
    /// </summary>
    private static long _similarityMatchesTotal;

    #endregion

    #region Histograms (using ConcurrentBag for lock-free collection)

    /// <summary>
    /// Processing duration samples in milliseconds.
    /// </summary>
    private static readonly ConcurrentBag<double> ProcessingDurationsSamples = new();

    /// <summary>
    /// Similarity query duration samples in milliseconds.
    /// </summary>
    private static readonly ConcurrentBag<double> SimilarityQueryDurationsSamples = new();

    /// <summary>
    /// Classification duration samples in milliseconds.
    /// </summary>
    private static readonly ConcurrentBag<double> ClassificationDurationsSamples = new();

    /// <summary>
    /// Maximum number of samples to retain in memory per histogram.
    /// </summary>
    private const int MaxHistogramSamples = 10000;

    #endregion

    #region Recording Methods

    /// <summary>
    /// Increments the chunks processed counter for the specified action.
    /// </summary>
    /// <param name="action">The deduplication action taken.</param>
    /// <param name="processingDurationMs">The processing duration in milliseconds.</param>
    public static void RecordChunkProcessed(Abstractions.Contracts.RAG.DeduplicationAction action, double processingDurationMs)
    {
        Interlocked.Increment(ref _chunksProcessedTotal);

        switch (action)
        {
            case Abstractions.Contracts.RAG.DeduplicationAction.StoredAsNew:
                Interlocked.Increment(ref _chunksStoredAsNew);
                break;
            case Abstractions.Contracts.RAG.DeduplicationAction.MergedIntoExisting:
                Interlocked.Increment(ref _chunksMerged);
                break;
            case Abstractions.Contracts.RAG.DeduplicationAction.LinkedToExisting:
                Interlocked.Increment(ref _chunksLinked);
                break;
            case Abstractions.Contracts.RAG.DeduplicationAction.FlaggedAsContradiction:
                Interlocked.Increment(ref _chunksFlaggedContradiction);
                break;
            case Abstractions.Contracts.RAG.DeduplicationAction.QueuedForReview:
                Interlocked.Increment(ref _chunksQueuedForReview);
                break;
            case Abstractions.Contracts.RAG.DeduplicationAction.SupersededExisting:
                // LOGIC: Superseding is tracked separately as it replaces existing content.
                // The count is implicitly part of the total processed.
                break;
        }

        // Record to histogram (with size limit)
        if (ProcessingDurationsSamples.Count < MaxHistogramSamples)
        {
            ProcessingDurationsSamples.Add(processingDurationMs);
        }
    }

    /// <summary>
    /// Records a similarity query execution.
    /// </summary>
    /// <param name="durationMs">The query duration in milliseconds.</param>
    /// <param name="matchCount">The number of matches found.</param>
    public static void RecordSimilarityQuery(double durationMs, int matchCount)
    {
        Interlocked.Increment(ref _similarityQueriesTotal);
        Interlocked.Add(ref _similarityMatchesTotal, matchCount);

        if (SimilarityQueryDurationsSamples.Count < MaxHistogramSamples)
        {
            SimilarityQueryDurationsSamples.Add(durationMs);
        }
    }

    /// <summary>
    /// Records a classification operation.
    /// </summary>
    /// <param name="method">The classification method used.</param>
    /// <param name="durationMs">The classification duration in milliseconds.</param>
    public static void RecordClassification(Abstractions.Contracts.RAG.ClassificationMethod method, double durationMs)
    {
        Interlocked.Increment(ref _classificationRequestsTotal);

        switch (method)
        {
            case Abstractions.Contracts.RAG.ClassificationMethod.RuleBased:
                Interlocked.Increment(ref _classificationsRuleBased);
                break;
            case Abstractions.Contracts.RAG.ClassificationMethod.LlmBased:
                Interlocked.Increment(ref _classificationsLlmBased);
                break;
            case Abstractions.Contracts.RAG.ClassificationMethod.Cached:
                Interlocked.Increment(ref _classificationsCached);
                break;
        }

        if (ClassificationDurationsSamples.Count < MaxHistogramSamples)
        {
            ClassificationDurationsSamples.Add(durationMs);
        }
    }

    /// <summary>
    /// Records a contradiction detection.
    /// </summary>
    /// <param name="severity">The contradiction severity.</param>
    public static void RecordContradictionDetected(Abstractions.Contracts.RAG.ContradictionSeverity severity)
    {
        Interlocked.Increment(ref _contradictionsDetectedTotal);

        switch (severity)
        {
            case Abstractions.Contracts.RAG.ContradictionSeverity.Low:
                Interlocked.Increment(ref _contradictionsLow);
                break;
            case Abstractions.Contracts.RAG.ContradictionSeverity.Medium:
                Interlocked.Increment(ref _contradictionsMedium);
                break;
            case Abstractions.Contracts.RAG.ContradictionSeverity.High:
                Interlocked.Increment(ref _contradictionsHigh);
                break;
            case Abstractions.Contracts.RAG.ContradictionSeverity.Critical:
                Interlocked.Increment(ref _contradictionsCritical);
                break;
        }
    }

    /// <summary>
    /// Records a batch job completion.
    /// </summary>
    public static void RecordBatchJobCompleted()
    {
        Interlocked.Increment(ref _batchJobsCompleted);
    }

    /// <summary>
    /// Records a processing error.
    /// </summary>
    /// <remarks>
    /// Call this from exception handlers in the deduplication pipeline to track
    /// errors that occur during processing.
    /// </remarks>
    public static void RecordProcessingError()
    {
        Interlocked.Increment(ref _processingErrors);
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets the total count of chunks processed.
    /// </summary>
    public static long ChunksProcessedTotal => Interlocked.Read(ref _chunksProcessedTotal);

    /// <summary>
    /// Gets the count of chunks stored as new.
    /// </summary>
    public static long ChunksStoredAsNew => Interlocked.Read(ref _chunksStoredAsNew);

    /// <summary>
    /// Gets the count of chunks merged.
    /// </summary>
    public static long ChunksMerged => Interlocked.Read(ref _chunksMerged);

    /// <summary>
    /// Gets the count of chunks linked.
    /// </summary>
    public static long ChunksLinked => Interlocked.Read(ref _chunksLinked);

    /// <summary>
    /// Gets the count of chunks flagged as contradictions.
    /// </summary>
    public static long ChunksFlaggedContradiction => Interlocked.Read(ref _chunksFlaggedContradiction);

    /// <summary>
    /// Gets the count of chunks queued for review.
    /// </summary>
    public static long ChunksQueuedForReview => Interlocked.Read(ref _chunksQueuedForReview);

    /// <summary>
    /// Gets the count of processing errors.
    /// </summary>
    public static long ProcessingErrors => Interlocked.Read(ref _processingErrors);

    /// <summary>
    /// Gets the total similarity queries executed.
    /// </summary>
    public static long SimilarityQueriesTotal => Interlocked.Read(ref _similarityQueriesTotal);

    /// <summary>
    /// Gets the total matches found.
    /// </summary>
    public static long SimilarityMatchesTotal => Interlocked.Read(ref _similarityMatchesTotal);

    /// <summary>
    /// Gets the total classification requests.
    /// </summary>
    public static long ClassificationRequestsTotal => Interlocked.Read(ref _classificationRequestsTotal);

    /// <summary>
    /// Gets the total contradictions detected.
    /// </summary>
    public static long ContradictionsDetectedTotal => Interlocked.Read(ref _contradictionsDetectedTotal);

    /// <summary>
    /// Gets the total batch jobs completed.
    /// </summary>
    public static long BatchJobsCompleted => Interlocked.Read(ref _batchJobsCompleted);

    /// <summary>
    /// Calculates the P99 processing duration in milliseconds.
    /// </summary>
    /// <returns>P99 latency or 0.0 if no samples.</returns>
    public static double GetProcessingDurationP99()
    {
        return CalculatePercentile(ProcessingDurationsSamples.ToArray(), 99);
    }

    /// <summary>
    /// Calculates the P99 similarity query duration in milliseconds.
    /// </summary>
    /// <returns>P99 latency or 0.0 if no samples.</returns>
    public static double GetSimilarityQueryDurationP99()
    {
        return CalculatePercentile(SimilarityQueryDurationsSamples.ToArray(), 99);
    }

    /// <summary>
    /// Calculates the P99 classification duration in milliseconds.
    /// </summary>
    /// <returns>P99 latency or 0.0 if no samples.</returns>
    public static double GetClassificationDurationP99()
    {
        return CalculatePercentile(ClassificationDurationsSamples.ToArray(), 99);
    }

    /// <summary>
    /// Gets the average processing duration in milliseconds.
    /// </summary>
    /// <returns>Average duration or 0.0 if no samples.</returns>
    public static double GetAverageProcessingDuration()
    {
        var samples = ProcessingDurationsSamples.ToArray();
        return samples.Length > 0 ? samples.Average() : 0.0;
    }

    /// <summary>
    /// Calculates the deduplication rate as a percentage.
    /// </summary>
    /// <returns>Percentage (0-100) of chunks that were deduplicated.</returns>
    public static double GetDeduplicationRate()
    {
        var total = ChunksProcessedTotal;
        if (total == 0) return 0.0;

        var deduplicated = ChunksMerged + ChunksLinked;
        return (double)deduplicated / total * 100.0;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Calculates the specified percentile from a set of samples.
    /// </summary>
    /// <param name="samples">The sample values.</param>
    /// <param name="percentile">The percentile to calculate (0-100).</param>
    /// <returns>The percentile value or 0.0 if no samples.</returns>
    private static double CalculatePercentile(double[] samples, int percentile)
    {
        if (samples.Length == 0) return 0.0;

        Array.Sort(samples);
        var index = (int)Math.Ceiling(percentile / 100.0 * samples.Length) - 1;
        index = Math.Max(0, Math.Min(index, samples.Length - 1));
        return samples[index];
    }

    /// <summary>
    /// Resets all metrics to zero. For testing purposes only.
    /// </summary>
    internal static void Reset()
    {
        _chunksProcessedTotal = 0;
        _chunksStoredAsNew = 0;
        _chunksMerged = 0;
        _chunksLinked = 0;
        _chunksFlaggedContradiction = 0;
        _chunksQueuedForReview = 0;
        _processingErrors = 0;
        _similarityQueriesTotal = 0;
        _similarityMatchesTotal = 0;
        _classificationRequestsTotal = 0;
        _classificationsRuleBased = 0;
        _classificationsLlmBased = 0;
        _classificationsCached = 0;
        _contradictionsDetectedTotal = 0;
        _contradictionsLow = 0;
        _contradictionsMedium = 0;
        _contradictionsHigh = 0;
        _contradictionsCritical = 0;
        _batchJobsCompleted = 0;

        // Clear histograms by creating new instances would break references,
        // so we just note they'll accumulate (ok for testing with Reset() at start)
        while (ProcessingDurationsSamples.TryTake(out _)) { }
        while (SimilarityQueryDurationsSamples.TryTake(out _)) { }
        while (ClassificationDurationsSamples.TryTake(out _)) { }
    }

    #endregion
}
