// -----------------------------------------------------------------------
// <copyright file="SimplificationPreviewView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.Simplifier.Views;

/// <summary>
/// Code-behind for the Simplification Preview View.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This is the main preview panel for the Simplifier Agent.
/// It provides a comprehensive UI for reviewing and accepting simplification changes:
/// </para>
/// <list type="bullet">
///   <item><description>Readability metrics comparison (before/after)</description></item>
///   <item><description>Tabbed diff views (Side-by-Side, Inline, Changes Only)</description></item>
///   <item><description>Preset selector for re-simplification</description></item>
///   <item><description>Accept/Reject action buttons</description></item>
///   <item><description>Keyboard shortcuts for quick actions</description></item>
/// </list>
/// <para>
/// <b>Keyboard Shortcuts:</b>
/// <list type="bullet">
///   <item><description><c>Ctrl+Enter</c> — Accept all changes</description></item>
///   <item><description><c>Escape</c> — Reject and close</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// The Simplifier Agent requires WriterPro tier. If the user's license is
/// insufficient, a warning banner is shown and accept buttons are disabled.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <seealso cref="SimplificationPreviewViewModel"/>
/// <seealso cref="ReadabilityComparisonPanel"/>
/// <seealso cref="Controls.DiffTextBox"/>
/// <seealso cref="Controls.InlineDiffView"/>
/// <seealso cref="Controls.ChangesOnlyView"/>
public partial class SimplificationPreviewView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimplificationPreviewView"/> class.
    /// </summary>
    public SimplificationPreviewView()
    {
        InitializeComponent();
    }
}
