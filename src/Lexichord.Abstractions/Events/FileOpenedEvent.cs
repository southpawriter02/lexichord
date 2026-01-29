using System;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Domain event published when a file is successfully opened.
/// </summary>
/// <param name="FilePath">Absolute path to the opened file.</param>
/// <param name="OpenedAt">When the file was opened.</param>
/// <param name="Source">How the file was opened.</param>
/// <remarks>
/// LOGIC: This event is published by the document system after a file is
/// successfully loaded. The RecentFilesService subscribes to this event
/// to automatically track opened files in the MRU history.
///
/// Handlers should not throw exceptions as this is a notification,
/// not a command. Any errors in handlers are logged but do not
/// affect the file opening operation.
/// </remarks>
public record FileOpenedEvent(
    string FilePath,
    DateTimeOffset OpenedAt,
    FileOpenSource Source = FileOpenSource.Menu) : INotification;

/// <summary>
/// Indicates how a file was opened.
/// </summary>
/// <remarks>
/// LOGIC: Tracking the source allows for analytics and potentially
/// different behavior based on how users access files.
/// </remarks>
public enum FileOpenSource
{
    /// <summary>Opened via File menu or toolbar.</summary>
    Menu,

    /// <summary>Opened from the Recent Files submenu.</summary>
    RecentFiles,

    /// <summary>Opened by dragging onto the application.</summary>
    DragDrop,

    /// <summary>Opened via command-line argument.</summary>
    CommandLine,

    /// <summary>Opened via OS file association (double-click).</summary>
    ShellAssociation
}
