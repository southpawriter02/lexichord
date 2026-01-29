namespace Lexichord.Abstractions.Events;

using MediatR;

/// <summary>
/// Event published when a command is registered.
/// </summary>
/// <param name="CommandId">The registered command ID.</param>
/// <param name="CommandTitle">The command display title.</param>
/// <param name="Category">The command category.</param>
public record CommandRegisteredMediatREvent(
    string CommandId,
    string CommandTitle,
    string Category
) : INotification;

/// <summary>
/// Event published when a command is executed.
/// </summary>
/// <param name="CommandId">The executed command ID.</param>
/// <param name="CommandTitle">The command display title.</param>
/// <param name="Source">How the command was invoked.</param>
/// <param name="DurationMs">Execution duration in milliseconds.</param>
/// <param name="Success">Whether execution succeeded.</param>
public record CommandExecutedMediatREvent(
    string CommandId,
    string CommandTitle,
    CommandSource Source,
    double DurationMs,
    bool Success
) : INotification;

/// <summary>
/// Source of command execution.
/// </summary>
public enum CommandSource
{
    /// <summary>Command executed via Command Palette.</summary>
    CommandPalette,

    /// <summary>Command executed via keyboard shortcut.</summary>
    KeyboardShortcut,

    /// <summary>Command executed via menu item.</summary>
    MenuItem,

    /// <summary>Command executed via context menu.</summary>
    ContextMenu,

    /// <summary>Command executed programmatically.</summary>
    Programmatic,

    /// <summary>Command executed via toolbar button.</summary>
    Toolbar
}
