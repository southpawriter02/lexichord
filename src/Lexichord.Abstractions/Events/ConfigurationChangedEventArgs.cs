namespace Lexichord.Abstractions.Events;

/// <summary>
/// Event arguments raised when the effective configuration changes.
/// </summary>
/// <remarks>
/// LOGIC: Published when any configuration source is modified or when
/// the cache is invalidated. Subscribers should re-evaluate their
/// configuration-dependent state.
///
/// Common Triggers:
/// - Workspace opened/closed (project config availability changed)
/// - Configuration file modified (file watcher detected change)
/// - Cache explicitly invalidated
/// - License tier changed (project config eligibility changed)
///
/// Version: v0.3.6a
/// </remarks>
public sealed class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the source that triggered the configuration change.
    /// </summary>
    /// <remarks>
    /// LOGIC: Identifies which layer caused the change. May be null
    /// if the change was triggered by cache invalidation.
    /// </remarks>
    public Contracts.ConfigurationSource? ChangedSource { get; init; }

    /// <summary>
    /// Gets whether project configuration availability changed.
    /// </summary>
    /// <remarks>
    /// LOGIC: True when project config was added or removed (e.g., workspace opened/closed).
    /// Consumers may need to re-evaluate more state than just config values.
    /// </remarks>
    public bool ProjectConfigAvailabilityChanged { get; init; }

    /// <summary>
    /// Gets the timestamp of the configuration change.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
