// -----------------------------------------------------------------------
// <copyright file="AgentCapabilitiesExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Extension methods for <see cref="AgentCapabilities"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides convenient helper methods for querying agent capabilities.
/// These methods are used by the UI layer to adapt available features,
/// the agent registry to filter agents, and logging to describe capabilities.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6a as part of the Agent Abstractions layer.
/// </para>
/// </remarks>
public static class AgentCapabilitiesExtensions
{
    /// <summary>
    /// Determines whether the agent has the specified capability.
    /// </summary>
    /// <param name="capabilities">The agent's capabilities.</param>
    /// <param name="capability">The capability to check.</param>
    /// <returns>
    /// <c>true</c> if the <paramref name="capability"/> flag is set in
    /// <paramref name="capabilities"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Uses bitwise AND to check if all bits of <paramref name="capability"/>
    /// are present in <paramref name="capabilities"/>. This correctly handles
    /// compound flags (e.g., checking for <c>Chat | DocumentContext</c>).
    /// </remarks>
    /// <example>
    /// <code>
    /// var caps = AgentCapabilities.Chat | AgentCapabilities.RAGContext;
    /// caps.HasCapability(AgentCapabilities.Chat);       // true
    /// caps.HasCapability(AgentCapabilities.RAGContext);  // true
    /// caps.HasCapability(AgentCapabilities.Streaming);   // false
    /// </code>
    /// </example>
    public static bool HasCapability(this AgentCapabilities capabilities, AgentCapabilities capability) =>
        (capabilities & capability) == capability;

    /// <summary>
    /// Determines whether the agent supports any form of context injection.
    /// </summary>
    /// <param name="capabilities">The agent's capabilities.</param>
    /// <returns>
    /// <c>true</c> if document, RAG, or style context is supported; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Returns true if any of <see cref="AgentCapabilities.DocumentContext"/>,
    /// <see cref="AgentCapabilities.RAGContext"/>, or <see cref="AgentCapabilities.StyleEnforcement"/>
    /// is present. Used by the UI to show/hide the context panel.
    /// </remarks>
    /// <example>
    /// <code>
    /// AgentCapabilities.Chat.SupportsContext();                                     // false
    /// AgentCapabilities.DocumentContext.SupportsContext();                           // true
    /// (AgentCapabilities.Chat | AgentCapabilities.RAGContext).SupportsContext();     // true
    /// </code>
    /// </example>
    public static bool SupportsContext(this AgentCapabilities capabilities) =>
        capabilities.HasCapability(AgentCapabilities.DocumentContext) ||
        capabilities.HasCapability(AgentCapabilities.RAGContext) ||
        capabilities.HasCapability(AgentCapabilities.StyleEnforcement);

    /// <summary>
    /// Returns a human-readable list of capability names.
    /// </summary>
    /// <param name="capabilities">The agent's capabilities.</param>
    /// <returns>
    /// An array of capability display names corresponding to the set flags.
    /// Returns an empty array if <paramref name="capabilities"/> is <see cref="AgentCapabilities.None"/>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Iterates through each defined capability flag and adds the
    /// corresponding display name if the flag is present. The display names
    /// are short labels suitable for UI badges:
    /// <list type="bullet">
    ///   <item><description><see cref="AgentCapabilities.Chat"/> → "Chat"</description></item>
    ///   <item><description><see cref="AgentCapabilities.DocumentContext"/> → "Document"</description></item>
    ///   <item><description><see cref="AgentCapabilities.RAGContext"/> → "RAG"</description></item>
    ///   <item><description><see cref="AgentCapabilities.StyleEnforcement"/> → "Style"</description></item>
    ///   <item><description><see cref="AgentCapabilities.Streaming"/> → "Streaming"</description></item>
    ///   <item><description><see cref="AgentCapabilities.CodeGeneration"/> → "CodeGen"</description></item>
    ///   <item><description><see cref="AgentCapabilities.ResearchAssistance"/> → "Research"</description></item>
    ///   <item><description><see cref="AgentCapabilities.Summarization"/> → "Summary"</description></item>
    ///   <item><description><see cref="AgentCapabilities.StructureAnalysis"/> → "Structure"</description></item>
    ///   <item><description><see cref="AgentCapabilities.Brainstorming"/> → "Brainstorm"</description></item>
    ///   <item><description><see cref="AgentCapabilities.Translation"/> → "Translate"</description></item>
    /// </list>
    /// <para>
    /// <b>Extended in:</b> v0.7.1a to include specialist agent capabilities.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var caps = AgentCapabilities.Chat | AgentCapabilities.RAGContext;
    /// var names = caps.GetCapabilityNames();
    /// // Result: ["Chat", "RAG"]
    /// </code>
    /// </example>
    public static string[] GetCapabilityNames(this AgentCapabilities capabilities)
    {
        // LOGIC: Build a list of display names for each enabled capability flag.
        // Pre-allocate with a reasonable capacity since there are at most 11 flags.
        var names = new List<string>(11);

        if (capabilities.HasCapability(AgentCapabilities.Chat))
            names.Add("Chat");
        if (capabilities.HasCapability(AgentCapabilities.DocumentContext))
            names.Add("Document");
        if (capabilities.HasCapability(AgentCapabilities.RAGContext))
            names.Add("RAG");
        if (capabilities.HasCapability(AgentCapabilities.StyleEnforcement))
            names.Add("Style");
        if (capabilities.HasCapability(AgentCapabilities.Streaming))
            names.Add("Streaming");
        if (capabilities.HasCapability(AgentCapabilities.CodeGeneration))
            names.Add("CodeGen");
        if (capabilities.HasCapability(AgentCapabilities.ResearchAssistance))
            names.Add("Research");
        if (capabilities.HasCapability(AgentCapabilities.Summarization))
            names.Add("Summary");
        if (capabilities.HasCapability(AgentCapabilities.StructureAnalysis))
            names.Add("Structure");
        if (capabilities.HasCapability(AgentCapabilities.Brainstorming))
            names.Add("Brainstorm");
        if (capabilities.HasCapability(AgentCapabilities.Translation))
            names.Add("Translate");

        return names.ToArray();
    }
}
