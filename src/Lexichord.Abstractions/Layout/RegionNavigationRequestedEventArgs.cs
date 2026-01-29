namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Event arguments for navigation requests to unknown dockable IDs.
/// </summary>
/// <param name="RequestedId">The identifier that was requested but not found.</param>
/// <remarks>
/// LOGIC: Raised when NavigateToAsync is called with an ID that doesn't
/// exist in the current layout. Handlers can use this to dynamically
/// create the requested dockable on demand.
/// </remarks>
public record RegionNavigationRequestedEventArgs(
    string RequestedId
)
{
    /// <summary>
    /// Gets or sets whether the navigation request was handled.
    /// </summary>
    /// <remarks>
    /// LOGIC: If a handler creates the requested dockable, it should set
    /// this to true to prevent further processing or error logging.
    /// </remarks>
    public bool Handled { get; set; }
}
