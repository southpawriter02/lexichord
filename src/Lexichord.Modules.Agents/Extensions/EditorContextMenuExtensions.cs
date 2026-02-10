// -----------------------------------------------------------------------
// <copyright file="EditorContextMenuExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering editor context menu items for Co-pilot.
/// </summary>
/// <remarks>
/// <para>
/// Provides the "Ask Co-pilot about selection" context menu integration.
/// The menu item is visible when the user has a WriterPro license and
/// enabled when there is an active text selection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
public static class EditorContextMenuExtensions
{
    /// <summary>
    /// Adds "Ask Co-pilot about selection" menu item to editor context menu.
    /// </summary>
    /// <param name="editorService">The editor service to register menu items with.</param>
    /// <param name="services">The service provider for resolving dependencies.</param>
    /// <remarks>
    /// LOGIC: Registers a context menu item that:
    /// <list type="bullet">
    ///   <item><description>Visible when user has WriterPro+ license</description></item>
    ///   <item><description>Enabled when there is an active text selection</description></item>
    ///   <item><description>Executes <see cref="SelectionContextCommand"/> on click</description></item>
    ///   <item><description>Grouped under "AI Assistance" at order 100</description></item>
    /// </list>
    /// </remarks>
    public static void RegisterCoPilotContextMenu(
        this IEditorService editorService,
        IServiceProvider services)
    {
        var command = services.GetRequiredService<SelectionContextCommand>();
        var license = services.GetRequiredService<ILicenseContext>();

        editorService.RegisterContextMenuItem(new ContextMenuItem
        {
            Header = "Ask Co-pilot about selection",
            Icon = "mdi-robot-happy",
            Command = command,
            IsVisible = () => license.GetCurrentTier() >= LicenseTier.WriterPro,
            IsEnabled = () => command.CanExecute(null),
            Group = "AI Assistance",
            Order = 100
        });
    }
}
