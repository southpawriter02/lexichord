// -----------------------------------------------------------------------
// <copyright file="ReadabilityComparisonPanel.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.Simplifier.Views;

/// <summary>
/// Code-behind for the Readability Comparison Panel view.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This panel displays before/after readability metrics for
/// simplified text, allowing users to see the improvement achieved by
/// the simplification process. It shows:
/// </para>
/// <list type="bullet">
///   <item><description>Original vs. simplified Flesch-Kincaid grade level</description></item>
///   <item><description>Reading ease score comparison</description></item>
///   <item><description>Word count changes</description></item>
///   <item><description>Grade level reduction indicator</description></item>
///   <item><description>Target achievement status</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <seealso cref="SimplificationPreviewViewModel"/>
public partial class ReadabilityComparisonPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadabilityComparisonPanel"/> class.
    /// </summary>
    public ReadabilityComparisonPanel()
    {
        InitializeComponent();
    }
}
