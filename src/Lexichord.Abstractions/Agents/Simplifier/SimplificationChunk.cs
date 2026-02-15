// -----------------------------------------------------------------------
// <copyright file="SimplificationChunk.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents a chunk of streaming output from the Simplifier Agent.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Streaming simplification yields incremental progress updates
/// as the LLM generates the response. Each chunk contains:
/// </para>
/// <list type="bullet">
///   <item><description>A text delta (new text fragment since last chunk)</description></item>
///   <item><description>Completion flag indicating if the stream has ended</description></item>
///   <item><description>Optional completed change when a full change record is parsed</description></item>
/// </list>
/// <para>
/// <b>Streaming Pattern:</b>
/// The <see cref="ISimplificationPipeline.SimplifyStreamingAsync"/> method yields
/// chunks as they become available. Consumers should:
/// </para>
/// <list type="number">
///   <item><description>Accumulate <see cref="TextDelta"/> values to build partial output</description></item>
///   <item><description>Display <see cref="CompletedChange"/> when available</description></item>
///   <item><description>Stop processing when <see cref="IsComplete"/> is true</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <param name="TextDelta">
/// The new text fragment received since the last chunk.
/// May be empty for completion or change-only chunks.
/// </param>
/// <param name="IsComplete">
/// Indicates whether the streaming operation has completed.
/// The final chunk will have this set to <c>true</c>.
/// </param>
/// <param name="CompletedChange">
/// Optional change record parsed from the stream.
/// Populated when a complete change block (original → simplified with explanation)
/// has been received and parsed.
/// </param>
/// <example>
/// <code>
/// // Consuming streaming simplification
/// var simplifiedText = new StringBuilder();
/// var changes = new List&lt;SimplificationChange&gt;();
///
/// await foreach (var chunk in pipeline.SimplifyStreamingAsync(request, ct))
/// {
///     // Accumulate text output
///     simplifiedText.Append(chunk.TextDelta);
///
///     // Collect completed changes
///     if (chunk.CompletedChange is not null)
///     {
///         changes.Add(chunk.CompletedChange);
///         Console.WriteLine($"Change detected: {chunk.CompletedChange.ChangeType}");
///     }
///
///     // Update UI with progress
///     UpdatePreview(simplifiedText.ToString());
///
///     if (chunk.IsComplete)
///     {
///         Console.WriteLine("Simplification complete!");
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="ISimplificationPipeline"/>
/// <seealso cref="SimplificationChange"/>
/// <seealso cref="SimplificationResult"/>
public record SimplificationChunk(
    string TextDelta,
    bool IsComplete,
    SimplificationChange? CompletedChange = null)
{
    /// <summary>
    /// Gets a value indicating whether this chunk contains text.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="TextDelta"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for filtering empty chunks during streaming.
    /// </remarks>
    public bool HasContent => !string.IsNullOrEmpty(TextDelta);

    /// <summary>
    /// Gets a value indicating whether this chunk contains a completed change.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="CompletedChange"/> is not null; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for detecting when to update the changes list.
    /// </remarks>
    public bool HasChange => CompletedChange is not null;

    /// <summary>
    /// Creates a text-only chunk without a completed change.
    /// </summary>
    /// <param name="textDelta">The text delta for this chunk.</param>
    /// <returns>A new <see cref="SimplificationChunk"/> with the specified text.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for the common case of streaming text output.
    /// </remarks>
    public static SimplificationChunk Text(string textDelta) =>
        new(textDelta, IsComplete: false, CompletedChange: null);

    /// <summary>
    /// Creates a completion chunk with optional final text.
    /// </summary>
    /// <param name="finalTextDelta">Optional final text delta.</param>
    /// <returns>A new <see cref="SimplificationChunk"/> marked as complete.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for the final chunk in a stream.
    /// </remarks>
    public static SimplificationChunk Complete(string finalTextDelta = "") =>
        new(finalTextDelta, IsComplete: true, CompletedChange: null);

    /// <summary>
    /// Creates a chunk containing a completed change record.
    /// </summary>
    /// <param name="change">The completed change to include.</param>
    /// <param name="textDelta">Optional text delta accompanying the change.</param>
    /// <returns>A new <see cref="SimplificationChunk"/> with the change record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="change"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for chunks that include parsed change records.
    /// </remarks>
    public static SimplificationChunk WithChange(SimplificationChange change, string textDelta = "")
    {
        ArgumentNullException.ThrowIfNull(change);
        return new(textDelta, IsComplete: false, CompletedChange: change);
    }
}
