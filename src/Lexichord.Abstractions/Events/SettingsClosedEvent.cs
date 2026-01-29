using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// MediatR notification published when the Settings window is closed.
/// </summary>
/// <remarks>
/// LOGIC: Enables modules to react to settings window closure, for example
/// to apply settings that require a UI refresh or to save pending changes.
///
/// This event is published after the window is closed, regardless of how
/// it was closed (Escape key, close button, or programmatic close).
///
/// Version: v0.1.6a
/// </remarks>
/// <example>
/// <code>
/// public class SettingsClosedHandler : INotificationHandler&lt;SettingsClosedEvent&gt;
/// {
///     public Task Handle(SettingsClosedEvent notification, CancellationToken cancellationToken)
///     {
///         // React to settings being closed
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public sealed record SettingsClosedEvent : INotification;
