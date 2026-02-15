// -----------------------------------------------------------------------
// <copyright file="DiffViewMode.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Specifies the visualization mode for displaying simplification diffs.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="DiffViewMode"/> determines how the original and
/// simplified text are displayed in the <see cref="SimplificationPreviewViewModel"/>.
/// Each mode offers different trade-offs between detail and clarity:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="SideBySide"/> — Shows both texts in parallel columns for easy comparison.
///     Best for seeing the overall structure of changes.
///   </description></item>
///   <item><description>
///     <see cref="Inline"/> — Merges changes into a single view with additions/deletions marked.
///     Best for detailed character-level review.
///   </description></item>
///   <item><description>
///     <see cref="ChangesOnly"/> — Lists only the changed sections without unchanged context.
///     Best for quickly reviewing a large number of changes.
///   </description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Setting view mode in ViewModel
/// viewModel.ViewMode = DiffViewMode.Inline;
///
/// // Using in AXAML binding
/// // &lt;RadioButton IsChecked="{Binding ViewMode, Converter={...}, ConverterParameter=SideBySide}" /&gt;
/// </code>
/// </example>
/// <seealso cref="SimplificationPreviewViewModel"/>
public enum DiffViewMode
{
    /// <summary>
    /// Displays original and simplified text in parallel columns.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> This is the default view mode. Uses synchronized scrolling
    /// to keep corresponding sections aligned. Deletions are highlighted in the
    /// left column (original) and additions in the right column (simplified).
    /// </remarks>
    SideBySide = 0,

    /// <summary>
    /// Displays changes inline with additions and deletions marked.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Merges both texts into a single view where:
    /// <list type="bullet">
    ///   <item><description>Deleted text is shown with strikethrough and red background</description></item>
    ///   <item><description>Added text is shown with green background</description></item>
    ///   <item><description>Unchanged text is shown normally</description></item>
    /// </list>
    /// This mode provides the most detailed view of character-level changes.
    /// </remarks>
    Inline = 1,

    /// <summary>
    /// Displays only the changed sections as a list.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Shows a compact list of all <see cref="Abstractions.Agents.Simplifier.SimplificationChange"/>
    /// items with checkboxes for selective acceptance. Each change shows:
    /// <list type="bullet">
    ///   <item><description>Original text snippet</description></item>
    ///   <item><description>Simplified text snippet</description></item>
    ///   <item><description>Change type badge</description></item>
    ///   <item><description>Explanation</description></item>
    /// </list>
    /// Best for quickly reviewing and accepting/rejecting individual changes.
    /// </remarks>
    ChangesOnly = 2
}
