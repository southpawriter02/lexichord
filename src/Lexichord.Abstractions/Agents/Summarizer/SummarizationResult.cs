// -----------------------------------------------------------------------
// <copyright file="SummarizationResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Result record for summarization operations (v0.7.6a).
//   Contains the generated summary along with metadata about the operation:
//   word counts, compression ratio, reading time, chunking info, and usage.
//
//   Success vs. Failure:
//     - Success: Summary is populated with the generated content, metrics are valid
//     - Failure: Created via Failed() factory — Summary contains error message,
//       metrics are zeroed, Usage is UsageMetrics.Zero
//
//   Introduced in: v0.7.6a
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Summarizer;

/// <summary>
/// The result of a summarization operation.
/// Contains the generated summary along with metadata about the operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="SummarizationResult"/> is the primary output of
/// <see cref="ISummarizerAgent.SummarizeAsync"/> and <see cref="ISummarizerAgent.SummarizeContentAsync"/>.
/// It encapsulates the summary content, compression metrics, and token usage for transparency.
/// </para>
/// <para>
/// <b>Success vs. Failure:</b>
/// Failed results are created via <see cref="Failed"/> and have <see cref="Success"/> set to
/// <c>false</c> with an <see cref="ErrorMessage"/>. Always check <see cref="Success"/> before
/// using the summary content.
/// </para>
/// <para>
/// <b>Thread safety:</b> This is an immutable record and is inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6a as part of the Summarizer Agent Summarization Modes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await summarizerAgent.SummarizeContentAsync(content, options, ct);
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Summary ({result.Mode}): {result.Summary}");
///     Console.WriteLine($"Compression: {result.OriginalWordCount} → {result.SummaryWordCount} words ({result.CompressionRatio:F1}x)");
///     Console.WriteLine($"Reading time saved: ~{result.OriginalReadingMinutes} min → instant");
/// }
/// else
/// {
///     Console.WriteLine($"Summarization failed: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
/// <seealso cref="SummarizationMode"/>
/// <seealso cref="SummarizationOptions"/>
/// <seealso cref="ISummarizerAgent"/>
public record SummarizationResult
{
    /// <summary>
    /// Gets the generated summary content in Markdown format.
    /// </summary>
    /// <value>
    /// For list modes, contains bullet/numbered list.
    /// For prose modes, contains paragraphs.
    /// Empty string for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The primary output of the summarization process. Format varies
    /// by <see cref="Mode"/>: BulletPoints uses "•" markers, KeyTakeaways uses
    /// "**Takeaway N:**" format, prose modes use paragraph text.
    /// </remarks>
    public required string Summary { get; init; }

    /// <summary>
    /// Gets the mode used to generate this summary.
    /// </summary>
    /// <value>
    /// The <see cref="SummarizationMode"/> that was active during generation.
    /// </value>
    public SummarizationMode Mode { get; init; }

    /// <summary>
    /// Gets the individual items extracted from the summary.
    /// </summary>
    /// <value>
    /// Populated only for <see cref="SummarizationMode.BulletPoints"/> and
    /// <see cref="SummarizationMode.KeyTakeaways"/> modes.
    /// Each item is the text without bullet/number prefix.
    /// <c>null</c> for prose modes.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Items are extracted by parsing the LLM response. For BulletPoints,
    /// lines starting with "•" or "-" are parsed. For KeyTakeaways, lines matching
    /// "**Takeaway N:**" are parsed.
    /// </remarks>
    public IReadOnlyList<string>? Items { get; init; }

    /// <summary>
    /// Gets the estimated reading time of the original document in minutes.
    /// </summary>
    /// <value>
    /// Calculated at 200 words per minute. Minimum 1 minute.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Provides context for the user about how much time the summary saves.
    /// Calculation: <c>Math.Max(1, OriginalWordCount / 200)</c>.
    /// </remarks>
    public int OriginalReadingMinutes { get; init; }

    /// <summary>
    /// Gets the word count of the original document.
    /// </summary>
    /// <value>
    /// Number of words in the source text, counted by splitting on whitespace.
    /// </value>
    public int OriginalWordCount { get; init; }

    /// <summary>
    /// Gets the word count of the generated summary.
    /// </summary>
    /// <value>
    /// Number of words in the summary content, counted by splitting on whitespace.
    /// </value>
    public int SummaryWordCount { get; init; }

    /// <summary>
    /// Gets the compression ratio (original words / summary words).
    /// </summary>
    /// <value>
    /// Higher values indicate more aggressive summarization. Typical range: 5x - 20x.
    /// Returns 0 when <see cref="SummaryWordCount"/> is 0 (failed result).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as <c>(double)OriginalWordCount / SummaryWordCount</c>.
    /// Displayed in the UI and in completion log messages for transparency.
    /// </remarks>
    public double CompressionRatio { get; init; }

    /// <summary>
    /// Gets the token usage for this summarization operation.
    /// </summary>
    /// <value>
    /// Total prompt and completion tokens consumed, along with estimated cost.
    /// <see cref="UsageMetrics.Zero"/> for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Accumulated across all LLM calls (chunk summaries + final combination).
    /// Displayed in the chat panel footer for transparency.
    /// </remarks>
    public required UsageMetrics Usage { get; init; }

    /// <summary>
    /// Gets the timestamp when the summary was generated.
    /// </summary>
    /// <value>
    /// UTC timestamp of when the summarization completed.
    /// </value>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the model used to generate the summary.
    /// </summary>
    /// <value>
    /// The LLM model identifier. Example: "gpt-4o".
    /// <c>null</c> for failed results or when model info is unavailable.
    /// </value>
    public string? Model { get; init; }

    /// <summary>
    /// Gets whether the document required chunking due to length.
    /// </summary>
    /// <value>
    /// <c>true</c> if document exceeded single-chunk token limit (4000 tokens);
    /// otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When chunking is required, the agent performs multiple LLM calls
    /// (one per chunk + one final combination), increasing latency and token usage.
    /// </remarks>
    public bool WasChunked { get; init; }

    /// <summary>
    /// Gets the number of chunks the document was split into.
    /// </summary>
    /// <value>
    /// Number of chunks processed. Always 1 for non-chunked documents.
    /// </value>
    public int ChunkCount { get; init; } = 1;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    /// <value>
    /// <c>true</c> if summarization completed without errors;
    /// otherwise, <c>false</c>. Check <see cref="ErrorMessage"/> for failure details.
    /// </value>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    /// <value>
    /// A user-facing error message describing what went wrong.
    /// <c>null</c> if <see cref="Success"/> is <c>true</c>.
    /// </value>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="mode">The summarization mode that was requested.</param>
    /// <param name="errorMessage">A user-facing error message describing the failure.</param>
    /// <param name="originalWordCount">The word count of the original document, if known.</param>
    /// <returns>
    /// A <see cref="SummarizationResult"/> with <see cref="Success"/> set to <c>false</c>
    /// and zeroed metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errorMessage"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating failed results with consistent structure.
    /// Uses <see cref="UsageMetrics.Zero"/> for token usage and empty string for summary.
    /// Called from catch blocks in the SummarizerAgent's 3-catch error handling pattern.
    /// </remarks>
    public static SummarizationResult Failed(
        SummarizationMode mode,
        string errorMessage,
        int originalWordCount = 0)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        return new SummarizationResult
        {
            Summary = string.Empty,
            Mode = mode,
            Items = null,
            OriginalReadingMinutes = 0,
            OriginalWordCount = originalWordCount,
            SummaryWordCount = 0,
            CompressionRatio = 0,
            Usage = UsageMetrics.Zero,
            GeneratedAt = DateTimeOffset.UtcNow,
            Model = null,
            WasChunked = false,
            ChunkCount = 0,
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
