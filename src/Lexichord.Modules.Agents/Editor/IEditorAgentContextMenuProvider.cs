// -----------------------------------------------------------------------
// <copyright file="IEditorAgentContextMenuProvider.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Editor;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Provides context menu items for the Editor Agent rewrite functionality.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This interface abstracts the Editor Agent's context menu
/// integration, enabling dependency injection and testability. The implementation
/// is responsible for:
/// </para>
/// <list type="bullet">
///   <item><description>Providing the list of available rewrite command options</description></item>
///   <item><description>Tracking the current selection state to enable/disable menu items</description></item>
///   <item><description>Checking license status to gate access to rewrite features</description></item>
///   <item><description>Publishing MediatR events when rewrite commands are executed</description></item>
/// </list>
/// <para>
/// <b>Lifetime:</b> Registered as singleton in DI to maintain selection state
/// subscription across the application lifetime.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="EditorAgentContextMenuProvider"/>
/// <seealso cref="RewriteCommandOption"/>
/// <seealso cref="RewriteIntent"/>
public interface IEditorAgentContextMenuProvider
{
    /// <summary>
    /// Gets the available rewrite command options for the context menu.
    /// </summary>
    /// <returns>A read-only list of rewrite command options.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Returns the predefined list of rewrite options:
    /// <list type="bullet">
    ///   <item><description>Rewrite Formally (Ctrl+Shift+R)</description></item>
    ///   <item><description>Simplify (Ctrl+Shift+S)</description></item>
    ///   <item><description>Expand (Ctrl+Shift+E)</description></item>
    ///   <item><description>Custom Rewrite... (Ctrl+Shift+C)</description></item>
    /// </list>
    /// </remarks>
    IReadOnlyList<RewriteCommandOption> GetRewriteMenuItems();

    /// <summary>
    /// Gets whether rewrite commands can be executed.
    /// </summary>
    /// <value>
    /// <c>true</c> if there is an active text selection and the user is licensed;
    /// <c>false</c> otherwise.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Combined check of <see cref="HasSelection"/> and
    /// <see cref="IsLicensed"/>. When <c>false</c>, rewrite menu items
    /// should be disabled or show an upgrade prompt.
    /// </remarks>
    bool CanRewrite { get; }

    /// <summary>
    /// Gets whether there is an active text selection in the editor.
    /// </summary>
    /// <value>
    /// <c>true</c> if the user has selected text in the active document;
    /// <c>false</c> otherwise.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Reflects the current selection state from
    /// <see cref="Abstractions.Contracts.Editor.IEditorService.HasSelection"/>.
    /// Updated via the <see cref="Abstractions.Contracts.Editor.IEditorService.SelectionChanged"/> event.
    /// </remarks>
    bool HasSelection { get; }

    /// <summary>
    /// Gets whether the user has a valid license for Editor Agent features.
    /// </summary>
    /// <value>
    /// <c>true</c> if the user has WriterPro or higher license tier;
    /// <c>false</c> otherwise.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Checks the license tier via
    /// <see cref="Abstractions.Contracts.ILicenseContext.GetCurrentTier"/>.
    /// Updated via the <see cref="Abstractions.Contracts.ILicenseContext.LicenseChanged"/> event.
    /// </remarks>
    bool IsLicensed { get; }

    /// <summary>
    /// Raised when <see cref="CanRewrite"/> changes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> This event is raised when either <see cref="HasSelection"/>
    /// or <see cref="IsLicensed"/> changes, allowing the UI to update the
    /// enabled state of rewrite menu items and commands.
    /// </remarks>
    event EventHandler? CanRewriteChanged;

    /// <summary>
    /// Executes the specified rewrite command.
    /// </summary>
    /// <param name="intent">The type of rewrite transformation to perform.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Validates preconditions and publishes the appropriate MediatR event:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>If not licensed: publishes <see cref="Events.ShowUpgradeModalEvent"/></description></item>
    ///   <item><description>If no selection: logs warning and returns</description></item>
    ///   <item><description>If Custom intent: publishes <see cref="Events.ShowCustomRewriteDialogEvent"/></description></item>
    ///   <item><description>Otherwise: publishes <see cref="Events.RewriteRequestedEvent"/></description></item>
    /// </list>
    /// </remarks>
    Task ExecuteRewriteAsync(RewriteIntent intent, CancellationToken cancellationToken = default);
}
