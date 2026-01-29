using Dock.Model.Controls;
using Dock.Model.Core;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Factory for creating and managing dock layouts.
/// </summary>
/// <remarks>
/// LOGIC: Abstracts Dock.Avalonia from modules, providing a stable interface
/// for layout management. Modules interact with this interface rather than
/// directly with the docking library.
///
/// Key responsibilities:
/// - Create the default application layout with predefined regions
/// - Create documents and tools for modules to populate
/// - Provide access to the dock hierarchy for programmatic manipulation
/// </remarks>
public interface IDockFactory
{
    /// <summary>
    /// Creates the default application layout with Left, Center, Right, and Bottom regions.
    /// </summary>
    /// <returns>The root dock of the layout.</returns>
    /// <remarks>
    /// LOGIC: The default layout follows the standard IDE pattern:
    /// - Left region (200px): Navigation, file tree
    /// - Center region: Main document area (DocumentDock)
    /// - Right region (250px): Properties, inspector
    /// - Bottom region (200px): Output, terminal, logs
    /// </remarks>
    IRootDock CreateDefaultLayout();

    /// <summary>
    /// Creates a new document dockable.
    /// </summary>
    /// <param name="id">Unique identifier for the document (used for serialization).</param>
    /// <param name="title">Display title for the document tab.</param>
    /// <param name="content">The content object to display in the document.</param>
    /// <returns>A new document instance.</returns>
    IDocument CreateDocument(string id, string title, object content);

    /// <summary>
    /// Creates a new tool pane dockable.
    /// </summary>
    /// <param name="region">The preferred shell region for the tool.</param>
    /// <param name="id">Unique identifier for the tool (used for serialization).</param>
    /// <param name="title">Display title for the tool.</param>
    /// <param name="content">The content object to display in the tool.</param>
    /// <returns>A new tool instance.</returns>
    ITool CreateTool(ShellRegion region, string id, string title, object content);

    /// <summary>
    /// Finds a dockable by its unique identifier.
    /// </summary>
    /// <param name="id">The identifier to search for.</param>
    /// <returns>The dockable if found; otherwise, null.</returns>
    IDockable? FindDockable(string id);

    /// <summary>
    /// Gets the root dock of the current layout.
    /// </summary>
    IRootDock? RootDock { get; }

    /// <summary>
    /// Gets the main document dock for adding documents.
    /// </summary>
    IDocumentDock? DocumentDock { get; }

    /// <summary>
    /// Gets the tool dock for the specified shell region.
    /// </summary>
    /// <param name="region">The shell region to get the tool dock for.</param>
    /// <returns>The tool dock if available; otherwise, null.</returns>
    IToolDock? GetToolDock(ShellRegion region);
}
