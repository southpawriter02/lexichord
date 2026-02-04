// =============================================================================
// File: BatchDeduplicationOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration record for batch deduplication job execution.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Defines configuration parameters for controlling batch job behavior,
//   including scope, thresholds, throttling, and processing limits.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Configuration options for batch deduplication job execution.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This record provides immutable configuration for controlling how batch jobs
/// process existing chunks. Key configuration areas:
/// </para>
/// <list type="bullet">
///   <item><description><b>Scope:</b> Filter by project, limit chunk count.</description></item>
///   <item><description><b>Thresholds:</b> Similarity cutoff, confidence for auto-merge.</description></item>
///   <item><description><b>Throttling:</b> Batch size, delay between batches.</description></item>
///   <item><description><b>Behavior:</b> Dry-run mode, LLM confirmation, contradiction detection.</description></item>
/// </list>
/// <para>
/// <b>Dry-Run Mode:</b> When <see cref="DryRun"/> is <c>true</c>, the job analyzes
/// duplicates without making any changes. This is useful for previewing the impact
/// of deduplication before committing.
/// </para>
/// <para>
/// <b>License:</b> Batch deduplication requires Teams tier.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Standard batch job
/// var options = new BatchDeduplicationOptions();
/// 
/// // Scoped to a specific project with custom threshold
/// var projectOptions = new BatchDeduplicationOptions
/// {
///     ProjectId = myProjectId,
///     SimilarityThreshold = 0.92f
/// };
/// 
/// // Dry-run preview
/// var previewOptions = new BatchDeduplicationOptions { DryRun = true };
/// </code>
/// </example>
public record BatchDeduplicationOptions
{
    /// <summary>
    /// Gets the default configuration with standard settings.
    /// </summary>
    /// <remarks>
    /// Default values:
    /// <list type="bullet">
    ///   <item><description>SimilarityThreshold: 0.90</description></item>
    ///   <item><description>BatchSize: 100</description></item>
    ///   <item><description>BatchDelayMs: 100</description></item>
    ///   <item><description>Priority: Normal (50)</description></item>
    ///   <item><description>DryRun: false</description></item>
    ///   <item><description>EnableContradictionDetection: true</description></item>
    /// </list>
    /// </remarks>
    public static BatchDeduplicationOptions Default { get; } = new();

    /// <summary>
    /// Gets or initializes the optional project ID to scope the batch job.
    /// </summary>
    /// <value>
    /// When set, only chunks from this project are processed.
    /// When <c>null</c>, chunks from all projects are processed.
    /// </value>
    /// <remarks>
    /// Use project scoping for:
    /// <list type="bullet">
    ///   <item><description>Testing on a single project before full rollout.</description></item>
    ///   <item><description>Processing high-priority projects first.</description></item>
    ///   <item><description>Limiting resource usage during production hours.</description></item>
    /// </list>
    /// </remarks>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// Gets or initializes whether this is a dry-run (preview only).
    /// </summary>
    /// <value>
    /// <c>true</c> to analyze duplicates without making changes;
    /// <c>false</c> to perform actual merges. Default: <c>false</c>.
    /// </value>
    /// <remarks>
    /// In dry-run mode:
    /// <list type="bullet">
    ///   <item><description>No chunks are merged or deleted.</description></item>
    ///   <item><description>No canonical records are created.</description></item>
    ///   <item><description>Statistics reflect what <i>would</i> happen.</description></item>
    /// </list>
    /// Use dry-run to preview impact before committing to changes.
    /// </remarks>
    public bool DryRun { get; init; }

    /// <summary>
    /// Gets or initializes the minimum similarity score for duplicate consideration.
    /// </summary>
    /// <value>
    /// A value between 0.0 and 1.0. Default: 0.90.
    /// Higher values are more conservative (fewer matches).
    /// </value>
    /// <remarks>
    /// <para>
    /// This threshold is stricter than the ingestion-time threshold (0.85)
    /// because batch processing sees chunks that have already passed ingestion.
    /// A higher threshold reduces false positives in retroactive processing.
    /// </para>
    /// <para>
    /// <b>Recommended ranges:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>0.85-0.90: Liberal (more merges, higher false positive risk).</description></item>
    ///   <item><description>0.90-0.95: Balanced (recommended for most cases).</description></item>
    ///   <item><description>0.95-1.0: Conservative (only near-exact matches).</description></item>
    /// </list>
    /// </remarks>
    public float SimilarityThreshold { get; init; } = 0.90f;

    /// <summary>
    /// Gets or initializes the number of chunks to process per batch.
    /// </summary>
    /// <value>
    /// Number of chunks per processing batch. Default: 100.
    /// Valid range: 10-1000.
    /// </value>
    /// <remarks>
    /// Larger batches improve throughput but:
    /// <list type="bullet">
    ///   <item><description>Increase memory usage.</description></item>
    ///   <item><description>Create longer delays between progress updates.</description></item>
    ///   <item><description>May cause database lock contention.</description></item>
    /// </list>
    /// </remarks>
    public int BatchSize { get; init; } = 100;

    /// <summary>
    /// Gets or initializes the delay between batches in milliseconds.
    /// </summary>
    /// <value>
    /// Delay in milliseconds. Default: 100. Valid range: 0-5000.
    /// </value>
    /// <remarks>
    /// <para>
    /// Throttling prevents batch jobs from monopolizing system resources.
    /// This delay is applied after each batch completes.
    /// </para>
    /// <para>
    /// <b>Recommendations:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Production (during work hours): 500-1000ms</description></item>
    ///   <item><description>Off-peak processing: 50-100ms</description></item>
    ///   <item><description>Maximum throughput: 0ms (no throttling)</description></item>
    /// </list>
    /// </remarks>
    public int BatchDelayMs { get; init; } = 100;

    /// <summary>
    /// Gets or initializes the maximum number of chunks to process.
    /// </summary>
    /// <value>
    /// Maximum chunk count. Default: 0 (unlimited).
    /// </value>
    /// <remarks>
    /// Use this to limit the scope of a batch job:
    /// <list type="bullet">
    ///   <item><description>Testing: Set to 100-1000 for validation runs.</description></item>
    ///   <item><description>Phased rollout: Process in increments of 10,000.</description></item>
    ///   <item><description>Full processing: Leave at 0 for unlimited.</description></item>
    /// </list>
    /// </remarks>
    public int MaxChunks { get; init; } = 0;

    /// <summary>
    /// Gets or initializes whether to require LLM confirmation for merges.
    /// </summary>
    /// <value>
    /// <c>true</c> to require LLM classification before merging;
    /// <c>false</c> to use rule-based fast-path only. Default: <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled, the <see cref="IRelationshipClassifier"/> will use LLM
    /// for all classifications, bypassing the rule-based fast-path.
    /// </para>
    /// <para>
    /// <b>Tradeoffs:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Higher accuracy for ambiguous cases.</description></item>
    ///   <item><description>Significantly slower (LLM latency per pair).</description></item>
    ///   <item><description>Increased API costs.</description></item>
    /// </list>
    /// </remarks>
    public bool RequireLlmConfirmation { get; init; }

    /// <summary>
    /// Gets or initializes whether to detect and flag contradictions.
    /// </summary>
    /// <value>
    /// <c>true</c> to flag contradictory chunks via <see cref="IContradictionService"/>;
    /// <c>false</c> to skip contradiction handling. Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// When enabled, chunks classified as <see cref="RelationshipType.Contradictory"/>
    /// are flagged for manual review rather than being merged or skipped.
    /// </remarks>
    public bool EnableContradictionDetection { get; init; } = true;

    /// <summary>
    /// Gets or initializes the job priority for scheduling.
    /// </summary>
    /// <value>
    /// Priority value from 0 (lowest) to 100 (highest). Default: 50 (normal).
    /// </value>
    /// <remarks>
    /// Priority affects job ordering when multiple batch jobs are queued.
    /// Higher priority jobs are executed first.
    /// </remarks>
    public int Priority { get; init; } = 50;

    /// <summary>
    /// Gets or initializes an optional label for the job.
    /// </summary>
    /// <value>
    /// User-defined label for identification. Maximum 256 characters.
    /// </value>
    /// <remarks>
    /// Labels help identify jobs in the history list. Examples:
    /// <list type="bullet">
    ///   <item><description>"Weekly cleanup - Research project"</description></item>
    ///   <item><description>"Pre-migration dedup - v2.0"</description></item>
    ///   <item><description>"Dry-run test - 2024-01-15"</description></item>
    /// </list>
    /// </remarks>
    public string? Label { get; init; }

    /// <summary>
    /// Gets or initializes the minimum confidence score for auto-merge.
    /// </summary>
    /// <value>
    /// Minimum confidence for automatic merging. Default: 0.70.
    /// Valid range: 0.0-1.0.
    /// </value>
    /// <remarks>
    /// Chunks with classification confidence below this threshold are
    /// queued for manual review instead of being auto-merged.
    /// </remarks>
    public float AutoMergeConfidenceThreshold { get; init; } = 0.70f;

    /// <summary>
    /// Gets or initializes the checkpoint frequency.
    /// </summary>
    /// <value>
    /// Number of chunks between checkpoint saves. Default: 500.
    /// </value>
    /// <remarks>
    /// Checkpoints enable job resumption after interruption.
    /// More frequent checkpoints:
    /// <list type="bullet">
    ///   <item><description>Reduce work lost on failure.</description></item>
    ///   <item><description>Add slight overhead for database writes.</description></item>
    /// </list>
    /// </remarks>
    public int CheckpointFrequency { get; init; } = 500;
}
