// -----------------------------------------------------------------------
// <copyright file="KeyTermChip.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Code-behind for the Key Term Chip component (v0.7.6c).
//   A simple chip displaying a key term with importance visualization.
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.SummaryExport.Views;

/// <summary>
/// Code-behind for the Key Term Chip component.
/// </summary>
/// <remarks>
/// <para>
/// This component displays a key term with importance visualization using
/// filled and empty dots (1-5 scale). Technical terms may receive distinct
/// styling.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <seealso cref="ViewModels.KeyTermViewModel"/>
public partial class KeyTermChip : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyTermChip"/> class.
    /// </summary>
    public KeyTermChip()
    {
        InitializeComponent();
    }
}
