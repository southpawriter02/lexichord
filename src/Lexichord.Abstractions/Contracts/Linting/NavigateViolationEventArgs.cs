namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Event arguments for violation navigation requests.
/// </summary>
/// <remarks>
/// LOGIC: Raised by ViolationTooltipViewModel when user clicks
/// prev/next navigation buttons in the tooltip.
///
/// Version: v0.2.4c
/// </remarks>
public class NavigateViolationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the navigation direction requested.
    /// </summary>
    public required NavigateDirection Direction { get; init; }
}
