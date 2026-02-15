// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationPhase.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents the current phase of a batch simplification operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="BatchSimplificationPhase"/> indicates which stage
/// of the batch operation is currently executing. This information is included
/// in <see cref="BatchSimplificationProgress"/> for UI display and logging.
/// </para>
/// <para>
/// <b>Phase Sequence (successful operation):</b>
/// <code>
/// Initializing → AnalyzingDocument → ProcessingParagraphs → AggregatingResults → Completed
/// </code>
/// </para>
/// <para>
/// <b>Phase Sequence (cancelled operation):</b>
/// <code>
/// Initializing → AnalyzingDocument → ProcessingParagraphs → Cancelled
/// </code>
/// </para>
/// <para>
/// <b>Phase Sequence (failed operation):</b>
/// <code>
/// Initializing → AnalyzingDocument → ProcessingParagraphs → Failed
/// </code>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <seealso cref="BatchSimplificationProgress"/>
/// <seealso cref="IBatchSimplificationService"/>
public enum BatchSimplificationPhase
{
    /// <summary>
    /// The batch operation is initializing.
    /// </summary>
    /// <remarks>
    /// During this phase, the service is loading the document content,
    /// validating inputs, and preparing for processing.
    /// </remarks>
    Initializing = 0,

    /// <summary>
    /// The document is being analyzed for paragraph structure.
    /// </summary>
    /// <remarks>
    /// During this phase, the document is parsed into paragraphs,
    /// and initial readability metrics are calculated for skip detection.
    /// </remarks>
    AnalyzingDocument = 1,

    /// <summary>
    /// Paragraphs are being processed through the simplification pipeline.
    /// </summary>
    /// <remarks>
    /// This is the main processing phase where each eligible paragraph
    /// is submitted to the <see cref="ISimplificationPipeline"/> for simplification.
    /// Progress reports during this phase include paragraph index and time estimates.
    /// </remarks>
    ProcessingParagraphs = 2,

    /// <summary>
    /// Results are being aggregated and changes applied.
    /// </summary>
    /// <remarks>
    /// During this phase, individual paragraph results are combined into
    /// an aggregate result, glossary entries are merged, and changes are
    /// applied to the document within an undo group.
    /// </remarks>
    AggregatingResults = 3,

    /// <summary>
    /// The batch operation completed successfully.
    /// </summary>
    /// <remarks>
    /// All paragraphs were processed (or skipped), changes were applied,
    /// and the <see cref="Events.SimplificationCompletedEvent"/> was published.
    /// </remarks>
    Completed = 4,

    /// <summary>
    /// The batch operation was cancelled by the user.
    /// </summary>
    /// <remarks>
    /// The cancellation token was triggered before all paragraphs were processed.
    /// Partial results may be available, but no changes were applied to the document.
    /// The <see cref="Events.BatchSimplificationCancelledEvent"/> was published.
    /// </remarks>
    Cancelled = 5,

    /// <summary>
    /// The batch operation failed due to an error.
    /// </summary>
    /// <remarks>
    /// An unrecoverable error occurred during processing. The error message
    /// is available in <see cref="BatchSimplificationResult.ErrorMessage"/>.
    /// No changes were applied to the document.
    /// </remarks>
    Failed = 6
}
