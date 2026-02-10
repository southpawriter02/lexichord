// -----------------------------------------------------------------------
// <copyright file="SelectionContextIndicator.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.Views;

/// <summary>
/// Code-behind for the selection context indicator UI component.
/// </summary>
/// <remarks>
/// <para>
/// Displays the active selection context in the Co-pilot chat panel,
/// including a summary (character count), preview text, and a clear button.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
public partial class SelectionContextIndicator : UserControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="SelectionContextIndicator"/>.
    /// </summary>
    public SelectionContextIndicator()
    {
        InitializeComponent();
    }
}
