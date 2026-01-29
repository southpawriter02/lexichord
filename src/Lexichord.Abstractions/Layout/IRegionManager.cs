using Dock.Model.Core;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Service for managing dock region content from modules.
/// </summary>
/// <remarks>
/// LOGIC: Provides a module-facing API for injecting views into dock regions
/// without direct dependency on Dock.Avalonia. Modules interact with this
/// interface to:
/// - Register tools in Left, Right, Bottom regions
/// - Register documents in the center DocumentDock
/// - Navigate to, close, hide, and show dockables
/// - React to region changes via events
///
/// The implementation (DockRegionManager) handles all Dock.Avalonia specifics
/// internally, maintaining the abstraction boundary.
/// </remarks>
public interface IRegionManager
{
    /// <summary>
    /// Registers a tool in the specified region.
    /// </summary>
    /// <param name="region">The target region (Left, Right, or Bottom).</param>
    /// <param name="id">Unique identifier for the tool.</param>
    /// <param name="title">Display title for the tool tab.</param>
    /// <param name="viewFactory">Factory function to create the view content.</param>
    /// <param name="options">Optional registration configuration.</param>
    /// <returns>A task that completes when the tool is registered.</returns>
    /// <remarks>
    /// LOGIC: Creates a tool dockable and adds it to the specified region.
    /// The viewFactory is executed on the UI thread to ensure proper
    /// Avalonia control instantiation.
    /// </remarks>
    Task<ITool?> RegisterToolAsync(
        ShellRegion region,
        string id,
        string title,
        Func<IServiceProvider, object> viewFactory,
        ToolRegistrationOptions? options = null);

    /// <summary>
    /// Registers a document in the center document dock.
    /// </summary>
    /// <param name="id">Unique identifier for the document.</param>
    /// <param name="title">Display title for the document tab.</param>
    /// <param name="viewFactory">Factory function to create the view content.</param>
    /// <param name="options">Optional registration configuration.</param>
    /// <returns>A task that completes when the document is registered.</returns>
    /// <remarks>
    /// LOGIC: Creates a document dockable and adds it to the center DocumentDock.
    /// The viewFactory is executed on the UI thread to ensure proper
    /// Avalonia control instantiation.
    /// </remarks>
    Task<IDocument?> RegisterDocumentAsync(
        string id,
        string title,
        Func<IServiceProvider, object> viewFactory,
        DocumentRegistrationOptions? options = null);

    /// <summary>
    /// Navigates to and activates an existing dockable.
    /// </summary>
    /// <param name="id">The identifier of the dockable to navigate to.</param>
    /// <returns>True if the dockable was found and activated; otherwise, false.</returns>
    /// <remarks>
    /// LOGIC: Finds the dockable by ID and sets it as the active dockable
    /// in its parent dock. If not found, raises NavigationRequested event
    /// to allow dynamic creation.
    /// </remarks>
    Task<bool> NavigateToAsync(string id);

    /// <summary>
    /// Closes a dockable by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the dockable to close.</param>
    /// <param name="force">If true, bypasses CanCloseAsync check.</param>
    /// <returns>True if the dockable was closed; otherwise, false.</returns>
    /// <remarks>
    /// LOGIC: Removes the dockable from its parent dock. If force is false
    /// and the dockable is a document, CanCloseAsync is called first.
    /// </remarks>
    Task<bool> CloseAsync(string id, bool force = false);

    /// <summary>
    /// Hides a dockable without closing it.
    /// </summary>
    /// <param name="id">The identifier of the dockable to hide.</param>
    /// <returns>True if the dockable was hidden; otherwise, false.</returns>
    /// <remarks>
    /// LOGIC: Removes the dockable from VisibleDockables but retains it
    /// for later restoration via ShowAsync.
    /// </remarks>
    Task<bool> HideAsync(string id);

    /// <summary>
    /// Shows a previously hidden dockable.
    /// </summary>
    /// <param name="id">The identifier of the dockable to show.</param>
    /// <returns>True if the dockable was shown; otherwise, false.</returns>
    /// <remarks>
    /// LOGIC: Restores the dockable to its previous parent dock's
    /// VisibleDockables collection.
    /// </remarks>
    Task<bool> ShowAsync(string id);

    /// <summary>
    /// Gets a dockable by its identifier.
    /// </summary>
    /// <param name="id">The identifier to search for.</param>
    /// <returns>The dockable if found; otherwise, null.</returns>
    IDockable? GetDockable(string id);

    /// <summary>
    /// Gets all visible dockables in a region.
    /// </summary>
    /// <param name="region">The region to query.</param>
    /// <returns>The visible dockables in the region.</returns>
    IEnumerable<IDockable> GetDockablesInRegion(ShellRegion region);

    /// <summary>
    /// Raised when a region changes (dockable added, removed, or activated).
    /// </summary>
    event EventHandler<RegionChangedEventArgs>? RegionChanged;

    /// <summary>
    /// Raised when navigation is requested for an unknown dockable ID.
    /// </summary>
    event EventHandler<RegionNavigationRequestedEventArgs>? NavigationRequested;
}
