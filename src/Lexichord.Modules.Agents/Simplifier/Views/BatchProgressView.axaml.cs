// -----------------------------------------------------------------------
// <copyright file="BatchProgressView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.Simplifier.Views;

/// <summary>
/// Code-behind for the Batch Progress View.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This is the progress dialog for batch simplification operations.
/// It provides real-time feedback during document-wide simplification:
/// </para>
/// <list type="bullet">
///   <item><description>Progress bar with percentage and paragraph counts</description></item>
///   <item><description>Estimated time remaining calculation</description></item>
///   <item><description>Current paragraph preview</description></item>
///   <item><description>Token usage tracking</description></item>
///   <item><description>Recent activity log</description></item>
///   <item><description>Cancel button with graceful cancellation</description></item>
/// </list>
/// <para>
/// <b>Cancellation:</b>
/// When the user clicks Cancel, the operation completes the current paragraph
/// before stopping. No changes are applied for cancelled operations (unless
/// partial application was explicitly requested).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.
/// </para>
/// </remarks>
/// <seealso cref="BatchProgressViewModel"/>
/// <seealso cref="BatchCompletionView"/>
public partial class BatchProgressView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProgressView"/> class.
    /// </summary>
    public BatchProgressView()
    {
        InitializeComponent();
    }
}
