// -----------------------------------------------------------------------
// <copyright file="AssembledContext.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Context;

/// <summary>
/// The result of context assembly, containing all gathered fragments
/// with metadata about the assembly process.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="AssembledContext"/> is produced by <see cref="IContextOrchestrator.AssembleAsync"/>
/// after gathering, deduplicating, sorting, and trimming context fragments. It provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Fragments:</b> Sorted by priority (highest first), already trimmed to budget</description></item>
///   <item><description><b>Token Accounting:</b> Total token count guaranteed ≤ <see cref="ContextBudget.MaxTokens"/></description></item>
///   <item><description><b>Template Variables:</b> Extracted from request metadata for prompt rendering</description></item>
///   <item><description><b>Performance Data:</b> Assembly duration for monitoring and optimization</description></item>
/// </list>
/// <para>
/// <strong>Immutability:</strong>
/// This record is immutable after construction. Fragments are stored as an
/// <see cref="IReadOnlyList{T}"/> and variables as an
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> to prevent modification.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2c as part of the Context Orchestrator.
/// </para>
/// </remarks>
/// <param name="Fragments">
/// Context fragments sorted by priority (highest first).
/// Already deduplicated and trimmed to fit within the requested budget.
/// </param>
/// <param name="TotalTokens">
/// Total token count across all included fragments.
/// Guaranteed to be less than or equal to <see cref="ContextBudget.MaxTokens"/>
/// (unless required strategies forced inclusion over budget).
/// </param>
/// <param name="Variables">
/// Extracted template variables for prompt substitution.
/// Includes document name, cursor position, fragment count, and other metadata
/// derived from the <see cref="ContextGatheringRequest"/> and assembled fragments.
/// </param>
/// <param name="AssemblyDuration">
/// Time taken to gather and assemble all context.
/// Useful for performance monitoring and identifying slow strategies.
/// </param>
/// <example>
/// <code>
/// var assembled = await orchestrator.AssembleAsync(request, budget, ct);
///
/// // Build context string for prompt
/// var context = assembled.GetCombinedContent();
///
/// // Log performance
/// logger.LogInformation(
///     "Assembled {Tokens} tokens in {Duration}ms",
///     assembled.TotalTokens,
///     assembled.AssemblyDuration.TotalMilliseconds);
///
/// // Check for specific fragment
/// if (assembled.HasFragmentFrom("style"))
/// {
///     var styleRules = assembled.GetFragment("style")!.Content;
/// }
/// </code>
/// </example>
public record AssembledContext(
    IReadOnlyList<ContextFragment> Fragments,
    int TotalTokens,
    IReadOnlyDictionary<string, object> Variables,
    TimeSpan AssemblyDuration)
{
    /// <summary>
    /// Gets an empty context result with no fragments, zero tokens, and zero duration.
    /// </summary>
    /// <value>A static <see cref="AssembledContext"/> instance representing no context.</value>
    /// <remarks>
    /// LOGIC: Used as the return value when no strategies are available,
    /// all strategies return null, or when the strategy filter leaves no
    /// eligible strategies. Avoids null checks at call sites.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (strategies.Count == 0)
    /// {
    ///     return AssembledContext.Empty;
    /// }
    /// </code>
    /// </example>
    public static AssembledContext Empty => new(
        Array.Empty<ContextFragment>(),
        0,
        new Dictionary<string, object>(),
        TimeSpan.Zero);

    /// <summary>
    /// Gets a value indicating whether any context was gathered.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Fragments"/> contains at least one fragment;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Convenience property for callers to check before processing.
    /// Agents may skip context injection entirely when no context is available,
    /// falling back to a simpler prompt without context sections.
    /// </remarks>
    public bool HasContext => Fragments.Count > 0;

    /// <summary>
    /// Gets the combined content of all fragments, formatted as labeled sections.
    /// </summary>
    /// <param name="separator">
    /// Separator between fragment sections. Defaults to double newline for readability.
    /// </param>
    /// <returns>
    /// A single string containing all fragment content, each preceded by a
    /// markdown heading with the fragment's <see cref="ContextFragment.Label"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Formats each fragment as a markdown section with a level-2 heading:
    /// </para>
    /// <code>
    /// ## Document Content
    /// [document content here]
    ///
    /// ## Selected Text
    /// [selection content here]
    /// </code>
    /// <para>
    /// This format is suitable for direct injection into agent prompts,
    /// providing clear section delineation for the LLM.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var context = assembled.GetCombinedContent();
    /// var prompt = $"Context:\n{context}\n\nUser request: {userMessage}";
    /// </code>
    /// </example>
    public string GetCombinedContent(string separator = "\n\n")
    {
        return string.Join(separator,
            Fragments.Select(f => $"## {f.Label}\n{f.Content}"));
    }

    /// <summary>
    /// Gets a specific fragment by its source strategy ID.
    /// </summary>
    /// <param name="sourceId">
    /// The <see cref="ContextFragment.SourceId"/> to look up (e.g., "document", "rag", "style").
    /// </param>
    /// <returns>
    /// The matching <see cref="ContextFragment"/> if present; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Performs a linear search through fragments. Since the fragment count
    /// is typically small (≤6), this is efficient enough without indexing.
    /// </remarks>
    /// <example>
    /// <code>
    /// var docFragment = assembled.GetFragment("document");
    /// if (docFragment is not null)
    /// {
    ///     Console.WriteLine($"Document: {docFragment.TokenEstimate} tokens");
    /// }
    /// </code>
    /// </example>
    public ContextFragment? GetFragment(string sourceId)
        => Fragments.FirstOrDefault(f => f.SourceId == sourceId);

    /// <summary>
    /// Checks if a specific strategy contributed context to this assembly.
    /// </summary>
    /// <param name="sourceId">
    /// The <see cref="ContextFragment.SourceId"/> to check for (e.g., "document", "rag").
    /// </param>
    /// <returns>
    /// <c>true</c> if a fragment from the specified source is present;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Useful for conditional processing when certain context types
    /// affect agent behavior (e.g., the Tuning Agent behaves differently
    /// when style rules are available).
    /// </remarks>
    /// <example>
    /// <code>
    /// if (assembled.HasFragmentFrom("style"))
    /// {
    ///     // Include style enforcement instructions in prompt
    /// }
    /// </code>
    /// </example>
    public bool HasFragmentFrom(string sourceId)
        => Fragments.Any(f => f.SourceId == sourceId);
}
