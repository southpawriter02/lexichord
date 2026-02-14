// -----------------------------------------------------------------------
// <copyright file="ContextFragment.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents.Context;

/// <summary>
/// Represents a single piece of context gathered by an <see cref="IContextStrategy"/>.
/// Contains the actual content along with metadata for sorting and display.
/// </summary>
/// <remarks>
/// <para>
/// Fragments are the output of context strategies and the input to the orchestrator.
/// Each fragment contains:
/// </para>
/// <list type="bullet">
///   <item><description><b>Content:</b> The actual text to include in the agent prompt</description></item>
///   <item><description><b>Metadata:</b> Source ID, label, token estimate, relevance score</description></item>
///   <item><description><b>Budget Info:</b> Token estimate for budget management</description></item>
///   <item><description><b>Sorting Keys:</b> Priority (via source strategy) and relevance</description></item>
/// </list>
/// <para>
/// <strong>Token Estimation:</strong>
/// The <see cref="TokenEstimate"/> property provides a pre-computed token count
/// to avoid redundant calculations during budget management. Strategies should
/// estimate tokens at creation time via <see cref="ITokenCounter"/>.
/// </para>
/// <para>
/// <strong>Relevance Scoring:</strong>
/// The <see cref="Relevance"/> property (0.0 to 1.0) allows strategies to indicate
/// how relevant this specific fragment is to the current request. The orchestrator
/// uses this for sorting fragments with equal priority.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
/// </para>
/// </remarks>
/// <param name="SourceId">Unique identifier of the source strategy that produced this fragment.</param>
/// <param name="Label">Human-readable label for display in the Context Preview panel.</param>
/// <param name="Content">The actual context content to be included in the agent prompt.</param>
/// <param name="TokenEstimate">Estimated token count for this fragment's content.</param>
/// <param name="Relevance">Relevance score from 0.0 (irrelevant) to 1.0 (perfectly relevant).</param>
/// <example>
/// <code>
/// // Creating a fragment from a strategy
/// var fragment = new ContextFragment(
///     SourceId: "document",
///     Label: "Document Content",
///     Content: "# Chapter 3\n\nThis chapter covers integration patterns...",
///     TokenEstimate: 450,
///     Relevance: 1.0f);
///
/// // Checking if fragment has content
/// if (fragment.HasContent)
/// {
///     Console.WriteLine($"Fragment from {fragment.SourceId}: {fragment.TokenEstimate} tokens");
/// }
///
/// // Truncating to budget
/// var truncated = fragment.TruncateTo(maxTokens: 200, tokenCounter);
/// </code>
/// </example>
public record ContextFragment(
    string SourceId,
    string Label,
    string Content,
    int TokenEstimate,
    float Relevance)
{
    /// <summary>
    /// Creates an empty fragment indicating a strategy produced no content.
    /// </summary>
    /// <param name="sourceId">Identifier of the source strategy.</param>
    /// <param name="label">Display label for the fragment.</param>
    /// <returns>A new <see cref="ContextFragment"/> with empty content and zero tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceId"/> or <paramref name="label"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// LOGIC: Used primarily for testing or placeholder scenarios. Most strategies
    /// should return <c>null</c> rather than an empty fragment when no context is available.
    /// This method exists for scenarios where a fragment instance is required but
    /// no meaningful content can be provided.
    /// </para>
    /// <para>
    /// <strong>Design Note:</strong>
    /// Returning <c>null</c> from <see cref="IContextStrategy.GatherAsync"/> is
    /// preferred over returning an empty fragment because it provides a clearer
    /// signal to the orchestrator that the strategy had nothing to contribute.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Testing scenario where a fragment is required
    /// var placeholder = ContextFragment.Empty("test-source", "Test Label");
    /// Assert.False(placeholder.HasContent);
    /// Assert.Equal(0, placeholder.TokenEstimate);
    /// </code>
    /// </example>
    public static ContextFragment Empty(string sourceId, string label)
    {
        ArgumentNullException.ThrowIfNull(sourceId);
        ArgumentNullException.ThrowIfNull(label);
        return new(sourceId, label, string.Empty, 0, 0f);
    }

    /// <summary>
    /// Gets a value indicating whether this fragment contains meaningful content.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Content"/> is not null or whitespace; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Used by orchestrator to filter out empty fragments before assembly.
    /// Uses <see cref="string.IsNullOrWhiteSpace"/> to catch both null and
    /// whitespace-only content that would not contribute meaningful context.
    /// </remarks>
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Creates a copy with truncated content to fit a token limit.
    /// </summary>
    /// <param name="maxTokens">Maximum tokens to retain.</param>
    /// <param name="tokenCounter">Token counter for estimation.</param>
    /// <returns>
    /// A new fragment with truncated content if necessary; otherwise, returns this instance unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenCounter"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// LOGIC: Smart truncation that prefers paragraph boundaries to maintain readability.
    /// If the current fragment already fits within the limit, returns <c>this</c> unchanged
    /// for efficiency (no unnecessary allocations).
    /// </para>
    /// <para>
    /// <strong>Truncation Strategy:</strong>
    /// The method uses paragraph-aware truncation (splitting on double newlines) to
    /// preserve logical document structure. This produces more coherent truncated
    /// content than character-based truncation.
    /// </para>
    /// <para>
    /// <strong>Performance:</strong>
    /// When no truncation is needed, the method returns <c>this</c> without creating
    /// a new instance. This optimization avoids unnecessary allocations during
    /// budget management when most fragments fit within limits.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var fragment = new ContextFragment(
    ///     SourceId: "document",
    ///     Label: "Document Content",
    ///     Content: longContent,
    ///     TokenEstimate: 1000,
    ///     Relevance: 1.0f);
    ///
    /// // Truncate to fit budget
    /// var truncated = fragment.TruncateTo(500, tokenCounter);
    /// if (truncated.TokenEstimate &lt; fragment.TokenEstimate)
    /// {
    ///     _logger.LogWarning("Fragment was truncated from {Original} to {New} tokens",
    ///         fragment.TokenEstimate, truncated.TokenEstimate);
    /// }
    /// </code>
    /// </example>
    public ContextFragment TruncateTo(int maxTokens, ITokenCounter tokenCounter)
    {
        ArgumentNullException.ThrowIfNull(tokenCounter);

        // LOGIC: Early return if already within limit (avoid unnecessary work)
        if (TokenEstimate <= maxTokens) return this;

        // LOGIC: Perform smart truncation preserving paragraph structure
        var truncated = TruncateContent(Content, maxTokens, tokenCounter);
        var newTokens = tokenCounter.CountTokens(truncated);

        // LOGIC: Return new instance with updated content and token estimate
        return this with
        {
            Content = truncated,
            TokenEstimate = newTokens
        };
    }

    /// <summary>
    /// Truncates content while preserving paragraph boundaries for readability.
    /// </summary>
    /// <param name="content">Content to truncate.</param>
    /// <param name="maxTokens">Maximum tokens allowed.</param>
    /// <param name="counter">Token counter for estimation.</param>
    /// <returns>Truncated content that fits within token limit.</returns>
    /// <remarks>
    /// LOGIC: Splits content on paragraph boundaries (double newlines) and includes
    /// complete paragraphs until the token limit would be exceeded. This maintains
    /// document structure and readability better than character-based truncation.
    /// </remarks>
    private static string TruncateContent(string content, int maxTokens, ITokenCounter counter)
    {
        // LOGIC: Split on paragraph boundaries to preserve structure
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();

        foreach (var para in paragraphs)
        {
            // LOGIC: Test if adding this paragraph would exceed limit
            var testContent = result.Length > 0
                ? result.ToString() + "\n\n" + para
                : para;

            if (counter.CountTokens(testContent) > maxTokens)
                break;

            // LOGIC: Paragraph fits, add it with separator
            if (result.Length > 0) result.Append("\n\n");
            result.Append(para);
        }

        return result.ToString();
    }
}
