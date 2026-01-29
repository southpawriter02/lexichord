namespace Lexichord.Abstractions.Events;

using MediatR;

/// <summary>
/// Event published when first run scenario is detected.
/// </summary>
/// <param name="IsFirstRunEver">True if fresh installation.</param>
/// <param name="IsFirstRunAfterUpdate">True if updated from previous version.</param>
/// <param name="PreviousVersion">Previous version, if update.</param>
/// <param name="CurrentVersion">Current running version.</param>
/// <remarks>
/// LOGIC: Published during startup when first-run detection completes.
/// Allows other modules to react to updates (e.g., show migration notices).
///
/// Version: v0.1.7c
/// </remarks>
public record FirstRunDetectedEvent(
    bool IsFirstRunEver,
    bool IsFirstRunAfterUpdate,
    string? PreviousVersion,
    string CurrentVersion
) : INotification;

/// <summary>
/// Event published when release notes are displayed.
/// </summary>
/// <param name="Version">Version whose notes were displayed.</param>
/// <param name="DisplayedAt">When notes were shown.</param>
/// <remarks>
/// LOGIC: Published after successfully opening CHANGELOG.md in editor.
/// Useful for analytics tracking of update awareness.
///
/// Version: v0.1.7c
/// </remarks>
public record ReleaseNotesDisplayedEvent(
    string Version,
    DateTimeOffset DisplayedAt
) : INotification;
