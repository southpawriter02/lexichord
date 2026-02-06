// -----------------------------------------------------------------------
// <copyright file="ContextSnapshot.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// Immutable snapshot of the current context state for prompt assembly.
/// </summary>
/// <remarks>
/// <para>
/// ContextSnapshot captures the enabled context sources at a point in time,
/// providing an immutable view that can be safely passed to the prompt
/// assembly pipeline. Only enabled sources are included in the snapshot.
/// </para>
/// <para>
/// The snapshot is designed to be:
/// </para>
/// <list type="bullet">
///   <item><description>Immutable: Thread-safe for concurrent access</description></item>
///   <item><description>Selective: Only includes enabled context sources</description></item>
///   <item><description>Self-describing: Includes token estimates and item counts</description></item>
/// </list>
/// </remarks>
/// <param name="StyleRules">The enabled style rules to include in context.</param>
/// <param name="RagChunks">The enabled RAG chunks to include in context.</param>
/// <param name="DocumentPath">The current document path, or null if document context is disabled.</param>
/// <param name="SelectedText">The selected text, or null if no selection or disabled.</param>
/// <param name="CustomInstructions">Custom instructions, or null if disabled.</param>
/// <param name="EstimatedTokens">Total estimated tokens across all enabled sources.</param>
/// <seealso cref="ContextPanelViewModel"/>
public sealed record ContextSnapshot(
    ImmutableArray<StyleRuleContextItem> StyleRules,
    ImmutableArray<RagChunkContextItem> RagChunks,
    string? DocumentPath,
    string? SelectedText,
    string? CustomInstructions,
    int EstimatedTokens)
{
    /// <summary>
    /// Gets an empty context snapshot with no content.
    /// </summary>
    /// <value>
    /// A singleton empty snapshot with empty collections and null values.
    /// </value>
    public static ContextSnapshot Empty { get; } = new(
        ImmutableArray<StyleRuleContextItem>.Empty,
        ImmutableArray<RagChunkContextItem>.Empty,
        null,
        null,
        null,
        0);

    /// <summary>
    /// Gets a value indicating whether the snapshot contains any content.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if any of the following are present:
    /// <list type="bullet">
    ///   <item><description>One or more style rules</description></item>
    ///   <item><description>One or more RAG chunks</description></item>
    ///   <item><description>A document path</description></item>
    ///   <item><description>Selected text</description></item>
    ///   <item><description>Custom instructions</description></item>
    /// </list>
    /// </value>
    public bool HasContent =>
        !StyleRules.IsEmpty ||
        !RagChunks.IsEmpty ||
        DocumentPath is not null ||
        SelectedText is not null ||
        CustomInstructions is not null;

    /// <summary>
    /// Gets a value indicating whether style rules are present.
    /// </summary>
    public bool HasStyleRules => !StyleRules.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether RAG chunks are present.
    /// </summary>
    public bool HasRagChunks => !RagChunks.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether document context is present.
    /// </summary>
    public bool HasDocumentContext => DocumentPath is not null;

    /// <summary>
    /// Gets a value indicating whether custom instructions are present.
    /// </summary>
    public bool HasCustomInstructions => !string.IsNullOrEmpty(CustomInstructions);

    /// <summary>
    /// Gets the total number of distinct context sources that are active.
    /// </summary>
    /// <value>
    /// Counts each category of context:
    /// <list type="bullet">
    ///   <item><description>1 if any style rules are present</description></item>
    ///   <item><description>1 if any RAG chunks are present</description></item>
    ///   <item><description>1 if document path is present</description></item>
    ///   <item><description>1 if custom instructions are present</description></item>
    /// </list>
    /// </value>
    public int TotalContextSources =>
        (!StyleRules.IsEmpty ? 1 : 0) +
        (!RagChunks.IsEmpty ? 1 : 0) +
        (DocumentPath is not null ? 1 : 0) +
        (!string.IsNullOrEmpty(CustomInstructions) ? 1 : 0);

    /// <summary>
    /// Gets the total count of items across all context sources.
    /// </summary>
    /// <value>
    /// The sum of:
    /// <list type="bullet">
    ///   <item><description>Number of style rules</description></item>
    ///   <item><description>Number of RAG chunks</description></item>
    ///   <item><description>1 if document path is present</description></item>
    ///   <item><description>1 if custom instructions are present</description></item>
    /// </list>
    /// Note: Selected text is not counted separately as it's part of document context.
    /// </value>
    public int TotalItemCount =>
        StyleRules.Length +
        RagChunks.Length +
        (DocumentPath is not null ? 1 : 0) +
        (CustomInstructions is not null ? 1 : 0);

    /// <summary>
    /// Creates a snapshot from the current state of a <see cref="ContextPanelViewModel"/>.
    /// </summary>
    /// <param name="vm">The ViewModel to capture state from.</param>
    /// <returns>
    /// A new <see cref="ContextSnapshot"/> containing only the enabled context sources
    /// from the ViewModel.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="vm"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method captures a point-in-time snapshot of the ViewModel state.
    /// Disabled sources are excluded from the snapshot entirely (not just
    /// marked as disabled).
    /// </para>
    /// <para>
    /// The estimated tokens value is taken directly from the ViewModel's
    /// <see cref="ContextPanelViewModel.EstimatedContextTokens"/> property,
    /// which already accounts for disabled sources.
    /// </para>
    /// </remarks>
    public static ContextSnapshot FromViewModel(ContextPanelViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);

        return new ContextSnapshot(
            StyleRules: vm.StyleRulesEnabled
                ? vm.ActiveStyleRules.ToImmutableArray()
                : ImmutableArray<StyleRuleContextItem>.Empty,
            RagChunks: vm.RagContextEnabled
                ? vm.RagChunks.ToImmutableArray()
                : ImmutableArray<RagChunkContextItem>.Empty,
            DocumentPath: vm.DocumentContextEnabled ? vm.CurrentDocumentPath : null,
            SelectedText: vm.DocumentContextEnabled ? vm.SelectedText : null,
            CustomInstructions: vm.CustomInstructionsEnabled ? vm.CustomInstructions : null,
            EstimatedTokens: vm.EstimatedContextTokens);
    }
}
