// -----------------------------------------------------------------------
// <copyright file="IBatchSimplificationService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Processes entire documents through the simplification pipeline paragraph by paragraph.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="IBatchSimplificationService"/> orchestrates document-wide
/// simplification operations by:
/// </para>
/// <list type="number">
///   <item><description>Parsing the document into paragraphs with offset tracking</description></item>
///   <item><description>Evaluating each paragraph against skip conditions</description></item>
///   <item><description>Processing eligible paragraphs through <see cref="ISimplificationPipeline"/></description></item>
///   <item><description>Reporting progress via <see cref="IProgress{T}"/></description></item>
///   <item><description>Supporting cancellation via <see cref="CancellationToken"/></description></item>
///   <item><description>Applying changes atomically with undo support</description></item>
///   <item><description>Publishing events for analytics and logging</description></item>
/// </list>
/// <para>
/// <b>License Requirement:</b>
/// Batch simplification requires the WriterPro license tier. Invoking methods without
/// the required license throws <see cref="Licensing.LicenseRequiredException"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations should be thread-safe and support concurrent invocations on
/// different documents. However, concurrent operations on the same document are
/// not recommended due to potential offset conflicts.
/// </para>
/// <para>
/// <b>Undo Integration:</b>
/// Successful batch operations are wrapped in an undo group, allowing users to
/// revert all changes with a single undo action (Ctrl+Z).
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// // Basic document simplification
/// var target = await targetService.GetTargetAsync("general-public");
/// var result = await batchService.SimplifyDocumentAsync(
///     documentPath: "/documents/report.md",
///     target: target);
///
/// Console.WriteLine($"Simplified {result.SimplifiedParagraphs} paragraphs");
/// Console.WriteLine($"Grade reduction: {result.GradeLevelReduction:F1}");
///
/// // With progress reporting and cancellation
/// var cts = new CancellationTokenSource();
/// var progress = new Progress&lt;BatchSimplificationProgress&gt;(p =>
/// {
///     progressBar.Value = p.PercentComplete;
///     statusText.Text = p.StatusMessage;
/// });
///
/// var options = new BatchSimplificationOptions
/// {
///     SkipAlreadySimple = true,
///     MinParagraphWords = 15,
///     Strategy = SimplificationStrategy.Aggressive
/// };
///
/// var result = await batchService.SimplifyDocumentAsync(
///     documentPath, target, options, progress, cts.Token);
///
/// // Streaming mode for real-time updates
/// await foreach (var paragraphResult in batchService.SimplifyDocumentStreamingAsync(
///     documentPath, target, options, cancellationToken))
/// {
///     if (paragraphResult.WasSimplified)
///     {
///         ApplyChange(paragraphResult);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="ISimplificationPipeline"/>
/// <seealso cref="BatchSimplificationOptions"/>
/// <seealso cref="BatchSimplificationResult"/>
/// <seealso cref="BatchSimplificationProgress"/>
public interface IBatchSimplificationService
{
    /// <summary>
    /// Simplifies an entire document, processing paragraph by paragraph.
    /// </summary>
    /// <param name="documentPath">
    /// Path to the document to simplify. Must be a valid file path accessible
    /// by the editor service.
    /// </param>
    /// <param name="target">
    /// The readability target to achieve. Obtain from <see cref="IReadabilityTargetService"/>.
    /// </param>
    /// <param name="options">
    /// Optional batch processing options. If <c>null</c>, default options are used.
    /// </param>
    /// <param name="progress">
    /// Optional progress reporter for UI updates. Reports are sent during initialization,
    /// after each paragraph, and upon completion.
    /// </param>
    /// <param name="ct">
    /// Cancellation token. When cancelled, processing stops and a partial result
    /// is returned with <see cref="BatchSimplificationResult.WasCancelled"/> = true.
    /// </param>
    /// <returns>
    /// A <see cref="BatchSimplificationResult"/> containing aggregate metrics,
    /// individual paragraph results, and token usage.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="target"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="documentPath"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="Licensing.LicenseRequiredException">
    /// Thrown when the required WriterPro license is not active.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the specified document does not exist.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via <paramref name="ct"/>.
    /// Note: Partial results are available via <see cref="BatchSimplificationResult.Cancelled"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method orchestrates the complete batch simplification workflow:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates license and inputs</description></item>
    ///   <item><description>Reads document content via <see cref="IEditorService"/></description></item>
    ///   <item><description>Parses document into paragraphs with offset tracking</description></item>
    ///   <item><description>Calculates original document metrics</description></item>
    ///   <item><description>Begins undo group for atomic reversion</description></item>
    ///   <item><description>Processes each paragraph (skip or simplify)</description></item>
    ///   <item><description>Applies changes to document</description></item>
    ///   <item><description>Publishes completion events</description></item>
    /// </list>
    /// </remarks>
    Task<BatchSimplificationResult> SimplifyDocumentAsync(
        string documentPath,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null,
        IProgress<BatchSimplificationProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Simplifies multiple text selections in batch.
    /// </summary>
    /// <param name="selections">
    /// The text selections to simplify. Must contain at least one selection.
    /// Selections should not overlap.
    /// </param>
    /// <param name="target">
    /// The readability target to achieve.
    /// </param>
    /// <param name="options">
    /// Optional batch processing options.
    /// </param>
    /// <param name="progress">
    /// Optional progress reporter for UI updates.
    /// </param>
    /// <param name="ct">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// A <see cref="BatchSimplificationResult"/> containing results for each selection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="selections"/> or <paramref name="target"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="selections"/> is empty or contains overlapping selections.
    /// </exception>
    /// <exception cref="Licensing.LicenseRequiredException">
    /// Thrown when the required WriterPro license is not active.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method processes multiple non-contiguous selections as a batch.
    /// Selections are sorted by offset and processed in reverse order to maintain
    /// offset validity during document modification.
    /// </para>
    /// <para>
    /// <b>Overlap Detection:</b>
    /// Selections are validated for overlap before processing. If selections overlap,
    /// an <see cref="ArgumentException"/> is thrown to prevent undefined behavior.
    /// </para>
    /// </remarks>
    Task<BatchSimplificationResult> SimplifySelectionsAsync(
        IReadOnlyList<TextSelection> selections,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null,
        IProgress<BatchSimplificationProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Streams simplification results as each paragraph completes.
    /// </summary>
    /// <param name="documentPath">
    /// Path to the document to simplify.
    /// </param>
    /// <param name="target">
    /// The readability target to achieve.
    /// </param>
    /// <param name="options">
    /// Optional batch processing options.
    /// </param>
    /// <param name="ct">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// An async enumerable of <see cref="ParagraphSimplificationResult"/> records,
    /// yielded as each paragraph is processed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="target"/> is null.
    /// </exception>
    /// <exception cref="Licensing.LicenseRequiredException">
    /// Thrown when the required WriterPro license is not active.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Streaming mode is useful for real-time document updates where
    /// each paragraph change should be applied immediately. Unlike
    /// <see cref="SimplifyDocumentAsync"/>, this method does not batch changes
    /// into a single undo group.
    /// </para>
    /// <para>
    /// <b>Caller Responsibility:</b>
    /// The caller is responsible for applying changes to the document and managing
    /// undo behavior when using streaming mode.
    /// </para>
    /// </remarks>
    IAsyncEnumerable<ParagraphSimplificationResult> SimplifyDocumentStreamingAsync(
        string documentPath,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Estimates time and token cost for batch simplification.
    /// </summary>
    /// <param name="documentPath">
    /// Path to the document to analyze.
    /// </param>
    /// <param name="target">
    /// The readability target for estimation.
    /// </param>
    /// <param name="options">
    /// Optional options that affect which paragraphs would be processed.
    /// </param>
    /// <param name="ct">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// A <see cref="BatchSimplificationEstimate"/> with projected costs and time.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="target"/> is null.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the specified document does not exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method analyzes the document without performing simplification.
    /// It evaluates skip conditions and calculates estimates based on typical
    /// processing characteristics.
    /// </para>
    /// <para>
    /// <b>Use Case:</b>
    /// Display the estimate to users before starting a batch operation, allowing
    /// them to make informed decisions about cost and time.
    /// </para>
    /// <para>
    /// <b>Note:</b> This method does not require the WriterPro license, allowing
    /// users to see estimates before upgrading.
    /// </para>
    /// </remarks>
    Task<BatchSimplificationEstimate> EstimateCostAsync(
        string documentPath,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null,
        CancellationToken ct = default);
}
