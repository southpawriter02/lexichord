// -----------------------------------------------------------------------
// <copyright file="ChangesOnlyView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.Simplifier.Controls;

/// <summary>
/// Code-behind for the Changes Only View control.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This control displays a list of individual simplification changes
/// with selection checkboxes, allowing users to:
/// </para>
/// <list type="bullet">
///   <item><description>Select/deselect individual changes for acceptance</description></item>
///   <item><description>View change type with categorized badges</description></item>
///   <item><description>Compare original and simplified text side-by-side</description></item>
///   <item><description>Expand explanations for each change</description></item>
///   <item><description>See confidence scores and length differences</description></item>
/// </list>
/// <para>
/// <b>Change Types:</b>
/// Changes are categorized and color-coded:
/// <list type="bullet">
///   <item><description>Blue — Structural (sentence split, clause reduction)</description></item>
///   <item><description>Green — Voice (passive to active)</description></item>
///   <item><description>Orange — Vocabulary (jargon, word simplification)</description></item>
///   <item><description>Purple — Flow (transitions, redundancy)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <seealso cref="SimplificationPreviewViewModel"/>
/// <seealso cref="SimplificationChangeViewModel"/>
public partial class ChangesOnlyView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangesOnlyView"/> class.
    /// </summary>
    public ChangesOnlyView()
    {
        InitializeComponent();
    }
}
