// -----------------------------------------------------------------------
// <copyright file="RewriteKeyboardShortcuts.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Configures keyboard bindings for Editor Agent rewrite features.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Registers keyboard shortcuts for the four rewrite commands:
/// </para>
/// <list type="bullet">
///   <item><description><c>Ctrl+Shift+R</c> → Rewrite Formally</description></item>
///   <item><description><c>Ctrl+Shift+S</c> → Simplify</description></item>
///   <item><description><c>Ctrl+Shift+E</c> → Expand</description></item>
///   <item><description><c>Ctrl+Shift+C</c> → Custom Rewrite</description></item>
/// </list>
/// <para>
/// All bindings are conditioned on <see cref="EditorHasSelectionCondition"/> to
/// prevent activation when no text is selected. The commands execute through
/// <see cref="IEditorAgentContextMenuProvider.ExecuteRewriteAsync"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="IKeyBindingConfiguration"/>
/// <seealso cref="IEditorAgentContextMenuProvider"/>
public class RewriteKeyboardShortcuts : IKeyBindingConfiguration
{
    #region Command Identifiers

    /// <summary>
    /// The command identifier for Rewrite Formally.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Maps to <see cref="Abstractions.Agents.Editor.RewriteIntent.Formal"/>.
    /// </remarks>
    public const string RewriteFormalCommandId = "editorAgent.rewriteFormal";

    /// <summary>
    /// The command identifier for Simplify.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Maps to <see cref="Abstractions.Agents.Editor.RewriteIntent.Simplified"/>.
    /// </remarks>
    public const string SimplifyCommandId = "editorAgent.simplify";

    /// <summary>
    /// The command identifier for Expand.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Maps to <see cref="Abstractions.Agents.Editor.RewriteIntent.Expanded"/>.
    /// </remarks>
    public const string ExpandCommandId = "editorAgent.expand";

    /// <summary>
    /// The command identifier for Custom Rewrite.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Maps to <see cref="Abstractions.Agents.Editor.RewriteIntent.Custom"/>.
    /// </remarks>
    public const string CustomRewriteCommandId = "editorAgent.customRewrite";

    #endregion

    #region Keyboard Gestures

    /// <summary>
    /// The default keyboard gesture for Rewrite Formally.
    /// </summary>
    public const string RewriteFormalGesture = "Ctrl+Shift+R";

    /// <summary>
    /// The default keyboard gesture for Simplify.
    /// </summary>
    public const string SimplifyGesture = "Ctrl+Shift+S";

    /// <summary>
    /// The default keyboard gesture for Expand.
    /// </summary>
    public const string ExpandGesture = "Ctrl+Shift+E";

    /// <summary>
    /// The default keyboard gesture for Custom Rewrite.
    /// </summary>
    public const string CustomRewriteGesture = "Ctrl+Shift+C";

    #endregion

    #region Context Conditions

    /// <summary>
    /// The context condition requiring an active text selection.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Same condition used by <see cref="Configuration.SelectionContextKeyBindings"/>.
    /// Ensures rewrite shortcuts only activate when text is selected.
    /// </remarks>
    public const string EditorHasSelectionCondition = "EditorHasSelection";

    #endregion

    /// <summary>
    /// Registers rewrite keyboard shortcuts.
    /// </summary>
    /// <param name="keyBindingService">The key binding service to register with.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Registers all four rewrite shortcuts with the
    /// <see cref="EditorHasSelectionCondition"/> context condition.
    /// </remarks>
    public void Configure(IKeyBindingService keyBindingService)
    {
        // LOGIC: Rewrite Formally - Ctrl+Shift+R
        keyBindingService.SetBinding(
            RewriteFormalCommandId,
            RewriteFormalGesture,
            EditorHasSelectionCondition);

        // LOGIC: Simplify - Ctrl+Shift+S
        keyBindingService.SetBinding(
            SimplifyCommandId,
            SimplifyGesture,
            EditorHasSelectionCondition);

        // LOGIC: Expand - Ctrl+Shift+E
        keyBindingService.SetBinding(
            ExpandCommandId,
            ExpandGesture,
            EditorHasSelectionCondition);

        // LOGIC: Custom Rewrite - Ctrl+Shift+C
        keyBindingService.SetBinding(
            CustomRewriteCommandId,
            CustomRewriteGesture,
            EditorHasSelectionCondition);
    }
}
