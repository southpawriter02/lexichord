namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents the debounce state machine states.
/// </summary>
/// <remarks>
/// LOGIC: Explicit state tracking for the debounce controller.
/// Enables precise monitoring and testing of state transitions.
///
/// Version: v0.2.3b
/// </remarks>
public enum DebounceState
{
    /// <summary>
    /// No pending content; waiting for changes.
    /// </summary>
    Idle,

    /// <summary>
    /// Content received; throttle timer running.
    /// </summary>
    Waiting,

    /// <summary>
    /// Throttle completed; scan in progress.
    /// </summary>
    Scanning,

    /// <summary>
    /// Previous scan was cancelled due to new content.
    /// </summary>
    /// <remarks>
    /// LOGIC: Transient state that immediately transitions to Waiting
    /// when new content triggers a scan request.
    /// </remarks>
    Cancelled
}
