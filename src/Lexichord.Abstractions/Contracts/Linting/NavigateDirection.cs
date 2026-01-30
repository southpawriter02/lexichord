namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Direction for navigating between violations at the same position.
/// </summary>
/// <remarks>
/// LOGIC: Used by ViolationTooltipService to cycle through multiple
/// violations that overlap at the same document offset.
///
/// Version: v0.2.4c
/// </remarks>
public enum NavigateDirection
{
    /// <summary>
    /// Navigate to the previous violation in the list.
    /// </summary>
    Previous,

    /// <summary>
    /// Navigate to the next violation in the list.
    /// </summary>
    Next
}
