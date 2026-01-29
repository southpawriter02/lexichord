using Dock.Model.Core;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Represents a dockable tool pane with Lexichord-specific properties.
/// </summary>
/// <remarks>
/// LOGIC: Extends IDockable to add tool-specific behavior:
/// - Preferred region for initial placement
/// - Size constraints for minimum dimensions
/// - Close behavior control
///
/// Tools are typically displayed in the Left, Right, or Bottom regions
/// and represent auxiliary panels (file browser, properties, output, etc.).
/// </remarks>
public interface ITool : IDockable
{
    /// <summary>
    /// Gets the preferred shell region for this tool.
    /// </summary>
    /// <remarks>
    /// LOGIC: Determines where the tool is placed when first added.
    /// Users can drag tools to other regions, but this is the default.
    /// </remarks>
    ShellRegion PreferredRegion { get; }

    /// <summary>
    /// Gets the minimum width for the tool pane.
    /// </summary>
    /// <remarks>
    /// LOGIC: The tool cannot be resized smaller than this width.
    /// Default should be 150px for reasonable content display.
    /// </remarks>
    double MinWidth { get; }

    /// <summary>
    /// Gets the minimum height for the tool pane.
    /// </summary>
    /// <remarks>
    /// LOGIC: The tool cannot be resized smaller than this height.
    /// Default should be 100px for reasonable content display.
    /// </remarks>
    double MinHeight { get; }

    /// <summary>
    /// Gets a value indicating whether the tool can be closed by the user.
    /// </summary>
    /// <remarks>
    /// LOGIC: Some tools (like output windows) may be essential and
    /// should not be closable, only minimizable or hidden.
    /// </remarks>
    new bool CanClose { get; }
}
