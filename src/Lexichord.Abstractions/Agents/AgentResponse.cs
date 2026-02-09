// -----------------------------------------------------------------------
// <copyright file="AgentResponse.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Immutable record containing the results of an agent invocation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AgentResponse"/> encapsulates everything returned by an agent:
/// the generated content, any citations from RAG sources, and usage metrics
/// for transparency and budget tracking.
/// </para>
/// <para>
/// This record is immutable by design, ensuring thread-safety when passed
/// to UI components and telemetry handlers.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6a as part of the Agent Abstractions layer.
/// </para>
/// </remarks>
/// <param name="Content">
/// The generated text response from the agent. May contain markdown formatting,
/// code blocks, and inline citation references (e.g., [1], [2]).
/// </param>
/// <param name="Citations">
/// Optional list of citations referencing source content used in the response.
/// Each citation corresponds to a numbered reference in the content.
/// Set to <c>null</c> if no RAG context was used.
/// </param>
/// <param name="Usage">
/// Token and cost metrics for this invocation. Always populated even if
/// usage tracking is disabled (will contain zero values via <see cref="UsageMetrics.Zero"/>).
/// </param>
/// <example>
/// <code>
/// // Response with citations
/// var response = new AgentResponse(
///     Content: "The hero's journey [1] consists of three phases...",
///     Citations: new[] { someCitation },
///     Usage: new UsageMetrics(150, 75, 0.0045m)
/// );
///
/// // Response without citations
/// var simpleResponse = new AgentResponse(
///     Content: "Here is my suggestion...",
///     Citations: null,
///     Usage: new UsageMetrics(100, 50, 0.003m)
/// );
/// </code>
/// </example>
public record AgentResponse(
    string Content,
    IReadOnlyList<Citation>? Citations,
    UsageMetrics Usage)
{
    /// <summary>
    /// Indicates whether this response includes citations.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Citations"/> is non-null and contains at least
    /// one citation; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Used by the UI to conditionally render the citations panel
    /// below the response content.
    /// </remarks>
    public bool HasCitations => Citations is { Count: > 0 };

    /// <summary>
    /// Gets the number of citations in this response.
    /// </summary>
    /// <value>
    /// The count of citations, or 0 if <see cref="Citations"/> is <c>null</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Null-safe count accessor used for logging and UI display.
    /// </remarks>
    public int CitationCount => Citations?.Count ?? 0;

    /// <summary>
    /// Gets the total token count (prompt + completion).
    /// </summary>
    /// <value>
    /// Delegates to <see cref="UsageMetrics.TotalTokens"/> for the combined token count.
    /// </value>
    /// <remarks>
    /// LOGIC: Convenience accessor that delegates to the <see cref="Usage"/> record.
    /// Enables direct access without navigating through the Usage property.
    /// </remarks>
    public int TotalTokens => Usage.TotalTokens;

    /// <summary>
    /// Creates an empty response for error cases.
    /// </summary>
    /// <value>
    /// A shared <see cref="AgentResponse"/> instance with empty content,
    /// no citations, and zero usage metrics.
    /// </value>
    /// <remarks>
    /// LOGIC: Static sentinel instance for cases where no response could be generated
    /// but an exception is not appropriate (e.g., graceful degradation).
    /// </remarks>
    public static AgentResponse Empty { get; } = new(
        string.Empty,
        null,
        UsageMetrics.Zero);

    /// <summary>
    /// Creates an error response with a message.
    /// </summary>
    /// <param name="errorMessage">The error message to display to the user.</param>
    /// <returns>
    /// A new <see cref="AgentResponse"/> containing the error message as content,
    /// no citations, and zero usage metrics.
    /// </returns>
    /// <remarks>
    /// LOGIC: Factory method for creating user-facing error responses without
    /// throwing exceptions. The error message is set as the content so it can
    /// be displayed in the chat panel like a normal response.
    /// </remarks>
    /// <example>
    /// <code>
    /// var errorResponse = AgentResponse.Error("Unable to reach the LLM provider. Please try again.");
    /// </code>
    /// </example>
    public static AgentResponse Error(string errorMessage) => new(
        errorMessage,
        null,
        UsageMetrics.Zero);
}
