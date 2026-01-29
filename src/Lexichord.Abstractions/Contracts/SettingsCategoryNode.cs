namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a node in the settings navigation tree.
/// </summary>
/// <remarks>
/// LOGIC: Used by the SettingsViewModel to build and display the hierarchical
/// navigation tree in the Settings window. Each node wraps an ISettingsPage
/// and contains its child nodes.
///
/// The tree structure is built dynamically from the flat list of registered
/// pages using their ParentCategoryId properties.
///
/// Version: v0.1.6a
/// </remarks>
/// <param name="Page">The settings page this node represents.</param>
/// <param name="Children">Child nodes under this category.</param>
public sealed record SettingsCategoryNode(
    ISettingsPage Page,
    IReadOnlyList<SettingsCategoryNode> Children
)
{
    /// <summary>
    /// Gets the unique category ID from the underlying page.
    /// </summary>
    public string CategoryId => Page.CategoryId;

    /// <summary>
    /// Gets the display name from the underlying page.
    /// </summary>
    public string DisplayName => Page.DisplayName;

    /// <summary>
    /// Gets the icon from the underlying page.
    /// </summary>
    public string? Icon => Page.Icon;

    /// <summary>
    /// Gets a value indicating whether this node has children.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Gets or sets a value indicating whether this node is expanded in the tree.
    /// </summary>
    /// <remarks>
    /// LOGIC: Mutable property for UI state. Defaults to true so categories
    /// are expanded by default.
    /// </remarks>
    public bool IsExpanded { get; set; } = true;
}
