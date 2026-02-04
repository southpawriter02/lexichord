namespace Lexichord.Abstractions.Contracts.Navigation;

/// <summary>
/// Defines the main navigation sections in the application.
/// </summary>
/// <remarks>
/// LOGIC: These sections map to the NavigationRail buttons and represent
/// the primary functional areas of the application.
/// </remarks>
public enum NavigationSection
{
    /// <summary>
    /// Documents section - file explorer and document management.
    /// </summary>
    Documents,

    /// <summary>
    /// Style Guide section - terminology, style rules, and writing analysis.
    /// </summary>
    StyleGuide,

    /// <summary>
    /// Memory section - RAG search, references, and knowledge base.
    /// </summary>
    Memory,

    /// <summary>
    /// Agents section - AI agent configuration and interactions.
    /// </summary>
    Agents
}

/// <summary>
/// Service for navigating between main application sections.
/// </summary>
/// <remarks>
/// LOGIC: Provides a high-level abstraction over the dock region system
/// for section-based navigation. The NavigationRail uses this service
/// to switch between sections, and the implementation handles:
/// - Mapping sections to dockable IDs
/// - Creating section views on demand
/// - Activating existing section views
/// </remarks>
public interface ISectionNavigationService
{
    /// <summary>
    /// Navigates to the specified section.
    /// </summary>
    /// <param name="section">The section to navigate to.</param>
    /// <returns>True if navigation succeeded; otherwise, false.</returns>
    /// <remarks>
    /// LOGIC: If the section view doesn't exist, it will be created.
    /// If it exists but is hidden, it will be shown.
    /// If it exists and is visible, it will be activated.
    /// </remarks>
    Task<bool> NavigateToSectionAsync(NavigationSection section);

    /// <summary>
    /// Gets the currently active section, if any.
    /// </summary>
    NavigationSection? ActiveSection { get; }

    /// <summary>
    /// Gets the dockable ID for a section.
    /// </summary>
    /// <param name="section">The section.</param>
    /// <returns>The dockable ID for the section.</returns>
    string GetSectionId(NavigationSection section);

    /// <summary>
    /// Raised when the active section changes.
    /// </summary>
    event EventHandler<NavigationSection?>? ActiveSectionChanged;
}
