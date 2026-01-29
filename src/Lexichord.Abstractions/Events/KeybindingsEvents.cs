namespace Lexichord.Abstractions.Events;

using MediatR;

/// <summary>
/// Event published when keybindings are reloaded.
/// </summary>
/// <remarks>
/// LOGIC: Published after LoadBindingsAsync completes.
/// Allows UI components to refresh keybinding displays.
/// </remarks>
/// <param name="TotalBindings">Total number of active bindings.</param>
/// <param name="UserOverrides">Number of user-defined overrides.</param>
/// <param name="ConflictCount">Number of detected conflicts.</param>
public record KeybindingsReloadedEvent(
    int TotalBindings,
    int UserOverrides,
    int ConflictCount
) : INotification;

/// <summary>
/// Event published when a specific binding changes.
/// </summary>
/// <remarks>
/// LOGIC: Published when SetBinding or ResetBinding is called.
/// Allows UI components to update individual keybinding displays.
/// </remarks>
/// <param name="CommandId">The affected command.</param>
/// <param name="OldGesture">Previous gesture string (null if was unbound).</param>
/// <param name="NewGesture">New gesture string (null if now unbound).</param>
public record KeybindingChangedMediatREvent(
    string CommandId,
    string? OldGesture,
    string? NewGesture
) : INotification;
