namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a view that can be hosted in a shell region.
/// </summary>
/// <remarks>
/// LOGIC: Shell regions are predefined areas in the Host's MainWindow where
/// modules can contribute UI. This interface allows modules to declare:
/// - Which region they want to occupy
/// - What order they appear relative to other views
/// - The actual view content to display
///
/// The ShellRegionManager in Host is responsible for:
/// 1. Collecting all IShellRegionView registrations from DI
/// 2. Grouping them by TargetRegion
/// 3. Ordering them by Order property
/// 4. Adding ViewContent to the appropriate region container
/// </remarks>
/// <example>
/// <code>
/// public class MyModuleRegionView : IShellRegionView
/// {
///     public ShellRegion TargetRegion => ShellRegion.Left;
///     public int Order => 50;
///     public object ViewContent => _view;
/// }
/// </code>
/// </example>
public interface IShellRegionView
{
    /// <summary>
    /// Gets the target shell region for this view.
    /// </summary>
    ShellRegion TargetRegion { get; }

    /// <summary>
    /// Gets the display order within the region.
    /// </summary>
    /// <remarks>
    /// Lower values appear first. Use values like:
    /// - 0-49: High priority (appears at top/left)
    /// - 50-150: Normal priority (middle)
    /// - 151+: Low priority (appears at bottom/right)
    /// </remarks>
    int Order { get; }

    /// <summary>
    /// Gets the view content to display.
    /// </summary>
    /// <remarks>
    /// This is typically a UserControl but can be any visual element.
    /// The content is lazily created on first access.
    /// </remarks>
    object ViewContent { get; }
}
