// -----------------------------------------------------------------------
// <copyright file="RagChunkContextItem.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// Represents a RAG (Retrieval-Augmented Generation) chunk in the context panel.
/// </summary>
/// <remarks>
/// <para>
/// RAG chunks are retrieved from the knowledge base based on semantic
/// similarity to the user's query. Each chunk shows its source, summary,
/// and relevance score to help users understand what knowledge is being
/// used to augment the AI's responses.
/// </para>
/// <para>
/// The record provides computed properties for UI display including:
/// </para>
/// <list type="bullet">
///   <item><description>Truncated summaries for compact display</description></item>
///   <item><description>Content previews showing first line</description></item>
///   <item><description>Relevance percentages and tier classifications</description></item>
///   <item><description>Visual relevance indicators</description></item>
/// </list>
/// </remarks>
/// <param name="Id">Unique identifier for the RAG chunk.</param>
/// <param name="SourceDocument">Name of the source document (e.g., "design-spec.md").</param>
/// <param name="SourcePath">Full path to the source document.</param>
/// <param name="Content">The full text content of the chunk.</param>
/// <param name="Summary">A brief summary of the chunk's content.</param>
/// <param name="EstimatedTokens">Estimated token count for this chunk.</param>
/// <param name="RelevanceScore">Semantic similarity score from 0.0 to 1.0.</param>
/// <seealso cref="ContextPanelViewModel"/>
/// <seealso cref="RelevanceTier"/>
/// <seealso cref="ContextSnapshot"/>
public sealed record RagChunkContextItem(
    string Id,
    string SourceDocument,
    string SourcePath,
    string Content,
    string Summary,
    int EstimatedTokens,
    float RelevanceScore)
{
    /// <summary>
    /// Maximum length for truncated summary display.
    /// </summary>
    private const int MaxSummaryLength = 80;

    /// <summary>
    /// Maximum length for content preview display.
    /// </summary>
    private const int MaxPreviewLength = 100;

    /// <summary>
    /// Gets a truncated version of the summary for compact display.
    /// </summary>
    /// <value>
    /// The summary truncated to <see cref="MaxSummaryLength"/> characters
    /// with "..." appended if truncation occurred.
    /// </value>
    public string TruncatedSummary => Summary.Length <= MaxSummaryLength
        ? Summary
        : Summary[..(MaxSummaryLength - 3)] + "...";

    /// <summary>
    /// Gets a preview of the content showing the first line.
    /// </summary>
    /// <value>
    /// The first line of content, truncated to <see cref="MaxPreviewLength"/>
    /// characters if necessary.
    /// </value>
    public string ContentPreview
    {
        get
        {
            var firstLine = Content.Split('\n')[0];
            return firstLine.Length <= MaxPreviewLength
                ? firstLine
                : firstLine[..(MaxPreviewLength - 3)] + "...";
        }
    }

    /// <summary>
    /// Gets the relevance score as a percentage (0-100).
    /// </summary>
    /// <value>The relevance score multiplied by 100 and rounded to an integer.</value>
    public int RelevancePercentage => (int)(RelevanceScore * 100);

    /// <summary>
    /// Gets the relevance tier classification based on the score.
    /// </summary>
    /// <value>
    /// A <see cref="RelevanceTier"/> value based on score thresholds:
    /// <list type="bullet">
    ///   <item><description>VeryHigh: ≥0.90</description></item>
    ///   <item><description>High: ≥0.75</description></item>
    ///   <item><description>Medium: ≥0.50</description></item>
    ///   <item><description>Low: ≥0.25</description></item>
    ///   <item><description>VeryLow: &lt;0.25</description></item>
    /// </list>
    /// </value>
    public RelevanceTier Relevance => RelevanceScore switch
    {
        >= 0.90f => RelevanceTier.VeryHigh,
        >= 0.75f => RelevanceTier.High,
        >= 0.50f => RelevanceTier.Medium,
        >= 0.25f => RelevanceTier.Low,
        _ => RelevanceTier.VeryLow
    };

    /// <summary>
    /// Gets an emoji indicator for the relevance tier.
    /// </summary>
    /// <value>
    /// An emoji character representing the relevance tier:
    /// <list type="bullet">
    ///   <item><description>🟢 VeryHigh</description></item>
    ///   <item><description>🟡 High</description></item>
    ///   <item><description>🟠 Medium</description></item>
    ///   <item><description>🔴 Low</description></item>
    ///   <item><description>⚫ VeryLow</description></item>
    /// </list>
    /// </value>
    public string RelevanceIndicator => Relevance switch
    {
        RelevanceTier.VeryHigh => "🟢",
        RelevanceTier.High => "🟡",
        RelevanceTier.Medium => "🟠",
        RelevanceTier.Low => "🔴",
        RelevanceTier.VeryLow => "⚫",
        _ => "⚫"
    };

    /// <summary>
    /// Gets the full tooltip text for hover display.
    /// </summary>
    /// <value>
    /// Multi-line text containing:
    /// <list type="bullet">
    ///   <item><description>Source document name</description></item>
    ///   <item><description>Full source path</description></item>
    ///   <item><description>Relevance percentage</description></item>
    ///   <item><description>Full summary</description></item>
    /// </list>
    /// </value>
    public string TooltipText => $"Source: {SourceDocument}\nPath: {SourcePath}\nRelevance: {RelevancePercentage}%\n{Summary}";
}
