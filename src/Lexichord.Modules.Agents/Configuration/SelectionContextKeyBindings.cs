// -----------------------------------------------------------------------
// <copyright file="SelectionContextKeyBindings.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Configuration;

/// <summary>
/// Configures keyboard bindings for selection context features.
/// </summary>
/// <remarks>
/// <para>
/// Registers the <c>Ctrl+Shift+A</c> keyboard shortcut for sending
/// the current editor selection to Co-pilot. The binding is conditioned
/// on "EditorHasSelection" to prevent activation when no text is selected.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
public class SelectionContextKeyBindings : IKeyBindingConfiguration
{
    /// <summary>
    /// The command identifier for sending selection to Co-pilot.
    /// </summary>
    public const string SendSelectionCommandId = "copilot.sendSelection";

    /// <summary>
    /// The default keyboard gesture for the command.
    /// </summary>
    public const string DefaultGesture = "Ctrl+Shift+A";

    /// <summary>
    /// The context condition for the binding.
    /// </summary>
    public const string EditorHasSelectionCondition = "EditorHasSelection";

    /// <summary>
    /// Registers selection context keyboard shortcuts.
    /// </summary>
    /// <param name="keyBindingService">The key binding service to register with.</param>
    /// <remarks>
    /// LOGIC: Registers <c>Ctrl+Shift+A</c> → <c>copilot.sendSelection</c>
    /// with "EditorHasSelection" context condition. The binding only activates
    /// when the editor has focused text that is selected.
    /// </remarks>
    public void Configure(IKeyBindingService keyBindingService)
    {
        keyBindingService.SetBinding(
            SendSelectionCommandId,
            DefaultGesture,
            EditorHasSelectionCondition);
    }
}
