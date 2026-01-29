namespace Lexichord.Abstractions.Events;

using MediatR;

/// <summary>
/// Notification published when a file should be opened in the editor.
/// </summary>
/// <param name="FilePath">The absolute path to the file to open.</param>
/// <remarks>
/// LOGIC: Published by the Project Explorer when a file is double-clicked.
/// The Editor module (v0.1.3) will handle this notification to open the file
/// in a new tab or navigate to an existing tab if already open.
///
/// Subscribers should validate that the file exists before attempting to open it,
/// as the file may have been deleted between the double-click and handling.
/// </remarks>
public record FileOpenRequestedEvent(string FilePath) : INotification;
