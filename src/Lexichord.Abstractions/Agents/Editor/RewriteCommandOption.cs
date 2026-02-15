// -----------------------------------------------------------------------
// <copyright file="RewriteCommandOption.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Editor;

/// <summary>
/// Defines a rewrite command option available in the context menu.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Each option represents a context menu item in the
/// "Rewrite with AI" submenu. Options are predefined in the
/// <c>EditorAgentContextMenuProvider</c> and used to build
/// <c>ContextMenuItem</c> instances for the editor.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="CommandId"/> - Unique identifier for the command (e.g., "rewrite-formal")</description></item>
///   <item><description><see cref="DisplayName"/> - Text shown in the menu (e.g., "Rewrite Formally")</description></item>
///   <item><description><see cref="Description"/> - Tooltip text describing the command</description></item>
///   <item><description><see cref="Icon"/> - Icon identifier from the icon library (e.g., "sparkles")</description></item>
///   <item><description><see cref="KeyboardShortcut"/> - Optional keyboard shortcut display text (e.g., "Ctrl+Shift+R")</description></item>
///   <item><description><see cref="Intent"/> - The <see cref="RewriteIntent"/> this command triggers</description></item>
///   <item><description><see cref="OpensDialog"/> - Whether this command opens a dialog (e.g., Custom Rewrite)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <param name="CommandId">Unique identifier for the command (kebab-case, e.g., "rewrite-formal").</param>
/// <param name="DisplayName">Display text shown in the menu.</param>
/// <param name="Description">Description for tooltip.</param>
/// <param name="Icon">Icon name from the icon library (e.g., Lucide icons).</param>
/// <param name="KeyboardShortcut">Keyboard shortcut display text (e.g., "Ctrl+Shift+R"), or null if none.</param>
/// <param name="Intent">The rewrite intent this command triggers.</param>
/// <param name="OpensDialog">Whether this command opens a dialog instead of executing immediately.</param>
/// <seealso cref="RewriteIntent"/>
public record RewriteCommandOption(
    string CommandId,
    string DisplayName,
    string Description,
    string Icon,
    string? KeyboardShortcut,
    RewriteIntent Intent,
    bool OpensDialog
)
{
    /// <summary>
    /// Validates the command option for correctness.
    /// </summary>
    /// <returns>A list of validation errors, or an empty list if valid.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Validates that:
    /// <list type="bullet">
    ///   <item><description>CommandId is not empty and follows kebab-case pattern</description></item>
    ///   <item><description>DisplayName is not empty</description></item>
    ///   <item><description>Icon is not empty</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(CommandId))
        {
            errors.Add("CommandId is required.");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(CommandId, @"^[a-z0-9]+(-[a-z0-9]+)*$"))
        {
            errors.Add($"CommandId '{CommandId}' must be kebab-case (e.g., 'rewrite-formal').");
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            errors.Add("DisplayName is required.");
        }

        if (string.IsNullOrWhiteSpace(Icon))
        {
            errors.Add("Icon is required.");
        }

        return errors;
    }
}
