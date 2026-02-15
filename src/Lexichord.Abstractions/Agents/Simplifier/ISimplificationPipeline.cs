// -----------------------------------------------------------------------
// <copyright file="ISimplificationPipeline.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Contract for the Simplifier Agent's text simplification pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ISimplificationPipeline"/> defines the core operations
/// for text simplification. It is implemented by the SimplifierAgent (v0.7.4b) and
/// consumed by command handlers and UI components.
/// </para>
/// <para>
/// <b>Pipeline Stages:</b>
/// <list type="number">
///   <item><description>Validate the request parameters</description></item>
///   <item><description>Analyze original text readability</description></item>
///   <item><description>Gather document context (surrounding text, style rules, terminology)</description></item>
///   <item><description>Build and render the prompt template</description></item>
///   <item><description>Invoke the LLM with appropriate settings for the strategy</description></item>
///   <item><description>Parse the structured response (simplified text, changes, glossary)</description></item>
///   <item><description>Analyze simplified text readability</description></item>
///   <item><description>Return the complete result with metrics</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Batch vs. Streaming:</b>
/// <list type="bullet">
///   <item><description><see cref="SimplifyAsync"/>: Waits for complete response, returns full result</description></item>
///   <item><description><see cref="SimplifyStreamingAsync"/>: Yields chunks as they arrive, enables real-time preview</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// The implementing SimplifierAgent requires <see cref="Lexichord.Abstractions.Contracts.LicenseTier.WriterPro"/>
/// tier or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Batch simplification
/// var pipeline = serviceProvider.GetRequiredService&lt;ISimplificationPipeline&gt;();
///
/// var request = new SimplificationRequest
/// {
///     OriginalText = complexText,
///     Target = await targetService.GetTargetAsync(presetId: "general-public"),
///     Strategy = SimplificationStrategy.Balanced
/// };
///
/// // Validate before processing
/// var validation = pipeline.ValidateRequest(request);
/// if (!validation.IsValid)
/// {
///     foreach (var error in validation.Errors)
///     {
///         Console.WriteLine($"Error: {error}");
///     }
///     return;
/// }
///
/// // Execute simplification
/// var result = await pipeline.SimplifyAsync(request, cancellationToken);
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Simplified from grade {result.OriginalMetrics.FleschKincaidGradeLevel:F1} " +
///                       $"to {result.SimplifiedMetrics.FleschKincaidGradeLevel:F1}");
///     Console.WriteLine(result.SimplifiedText);
/// }
/// </code>
/// </example>
/// <example>
/// <code>
/// // Streaming simplification with real-time preview
/// var partialText = new StringBuilder();
///
/// await foreach (var chunk in pipeline.SimplifyStreamingAsync(request, ct))
/// {
///     partialText.Append(chunk.TextDelta);
///     UpdatePreview(partialText.ToString());
///
///     if (chunk.HasChange)
///     {
///         AddChangeToList(chunk.CompletedChange!);
///     }
///
///     if (chunk.IsComplete)
///     {
///         ShowCompletionMessage();
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SimplificationRequest"/>
/// <seealso cref="SimplificationResult"/>
/// <seealso cref="SimplificationChunk"/>
/// <seealso cref="SimplificationValidation"/>
public interface ISimplificationPipeline
{
    /// <summary>
    /// Simplifies text to the target readability level.
    /// </summary>
    /// <param name="request">The simplification request containing text and target parameters.</param>
    /// <param name="cancellationToken">
    /// Cancellation token for aborting the operation. If cancellation is requested,
    /// the method returns a failed result with an appropriate error message.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the
    /// <see cref="SimplificationResult"/> with the simplified text, metrics, and changes.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="request"/> validation fails.</exception>
    /// <remarks>
    /// <para>
    /// This is the primary entry point for text simplification. The method:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates the request (throws if invalid)</description></item>
    ///   <item><description>Analyzes original text readability</description></item>
    ///   <item><description>Gathers context from the document (if path provided)</description></item>
    ///   <item><description>Builds prompt variables and renders the template</description></item>
    ///   <item><description>Invokes the LLM with strategy-appropriate temperature</description></item>
    ///   <item><description>Parses the structured response</description></item>
    ///   <item><description>Analyzes simplified text readability</description></item>
    ///   <item><description>Returns the complete result</description></item>
    /// </list>
    /// <para>
    /// <b>Error Handling:</b>
    /// Instead of throwing exceptions for runtime errors (timeouts, LLM failures),
    /// the method returns a <see cref="SimplificationResult"/> with
    /// <see cref="SimplificationResult.Success"/> set to <c>false</c> and
    /// <see cref="SimplificationResult.ErrorMessage"/> populated.
    /// </para>
    /// <para>
    /// <b>Cancellation:</b>
    /// If <paramref name="cancellationToken"/> is triggered, the method returns a
    /// failed result with "Simplification cancelled by user." message.
    /// </para>
    /// <para>
    /// <b>Timeout:</b>
    /// If <see cref="SimplificationRequest.Timeout"/> is exceeded, the method returns
    /// a failed result with "Simplification timed out after X seconds." message.
    /// </para>
    /// </remarks>
    Task<SimplificationResult> SimplifyAsync(
        SimplificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Simplifies text with streaming output for real-time preview.
    /// </summary>
    /// <param name="request">The simplification request containing text and target parameters.</param>
    /// <param name="cancellationToken">
    /// Cancellation token for aborting the operation. If triggered, the stream ends
    /// with a completion chunk.
    /// </param>
    /// <returns>
    /// An async enumerable yielding <see cref="SimplificationChunk"/> records
    /// as the LLM generates the response.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="request"/> validation fails.</exception>
    /// <remarks>
    /// <para>
    /// Streaming simplification enables real-time preview in the UI. Chunks are
    /// yielded as soon as content is available from the LLM.
    /// </para>
    /// <para>
    /// <b>Chunk Types:</b>
    /// <list type="bullet">
    ///   <item><description>Text chunks: Contain <see cref="SimplificationChunk.TextDelta"/> with new content</description></item>
    ///   <item><description>Change chunks: Contain <see cref="SimplificationChunk.CompletedChange"/> when a change is parsed</description></item>
    ///   <item><description>Completion chunk: Has <see cref="SimplificationChunk.IsComplete"/> set to true</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage Pattern:</b>
    /// Consumers should accumulate <see cref="SimplificationChunk.TextDelta"/> values
    /// to build the complete simplified text, collect <see cref="SimplificationChunk.CompletedChange"/>
    /// records as they arrive, and stop processing when <see cref="SimplificationChunk.IsComplete"/>
    /// is true.
    /// </para>
    /// <para>
    /// <b>Note:</b> For the full <see cref="SimplificationResult"/> with metrics,
    /// use <see cref="SimplifyAsync"/> instead.
    /// </para>
    /// </remarks>
    IAsyncEnumerable<SimplificationChunk> SimplifyStreamingAsync(
        SimplificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a simplification request without executing it.
    /// </summary>
    /// <param name="request">The simplification request to validate.</param>
    /// <returns>
    /// A <see cref="SimplificationValidation"/> containing validation results.
    /// Check <see cref="SimplificationValidation.IsValid"/> to determine if
    /// the request can be processed.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Use this method to validate requests before calling <see cref="SimplifyAsync"/>
    /// or <see cref="SimplifyStreamingAsync"/>. This provides detailed error messages
    /// and warnings without throwing exceptions.
    /// </para>
    /// <para>
    /// <b>Validation Checks:</b>
    /// <list type="bullet">
    ///   <item><description>Text is not null, empty, or whitespace (error)</description></item>
    ///   <item><description>Text length is within acceptable bounds (error if &gt;50,000 chars)</description></item>
    ///   <item><description>Target is not null (error)</description></item>
    ///   <item><description>Strategy is a valid enum value (error)</description></item>
    ///   <item><description>Timeout is positive and reasonable (error)</description></item>
    ///   <item><description>Text is very long (&gt;20,000 chars) (warning)</description></item>
    ///   <item><description>Target may be difficult to achieve (warning)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note:</b> Unlike <see cref="SimplificationRequest.Validate"/>, this method
    /// never throws exceptions for validation failures—it returns them in the
    /// <see cref="SimplificationValidation.Errors"/> collection.
    /// </para>
    /// </remarks>
    SimplificationValidation ValidateRequest(SimplificationRequest request);
}
