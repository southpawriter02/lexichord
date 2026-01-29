namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the contract for a settings page that can be contributed by modules.
/// </summary>
/// <remarks>
/// LOGIC: Each module can implement this interface to contribute settings pages
/// to the centralized Settings dialog. Pages are organized hierarchically using
/// ParentCategoryId and SortOrder properties.
///
/// Implementation Requirements:
/// - CategoryId must be unique across all registered pages
/// - CategoryId should use dot notation for hierarchy (e.g., "editor.fonts")
/// - CreateView() should return a new instance each time for proper lifecycle
/// - CreateView() may throw exceptions, which the host will catch and display gracefully
/// - CreateView() should return an Avalonia Control (typed as object to avoid Avalonia dependency in Abstractions)
///
/// Thread Safety:
/// - Implementations should be thread-safe for property access
/// - CreateView() is called on the UI thread
///
/// Version: v0.1.6a
/// </remarks>
/// <example>
/// <code>
/// public class EditorSettingsPage : ISettingsPage
/// {
///     public string CategoryId => "editor";
///     public string DisplayName => "Editor";
///     public string? ParentCategoryId => null;
///     public string? Icon => "Settings";
///     public int SortOrder => 100;
///     public LicenseTier RequiredTier => LicenseTier.Core;
///     public IReadOnlyList&lt;string&gt; SearchKeywords => ["text", "fonts", "display"];
///
///     public object CreateView() => new EditorSettingsView();
/// }
/// </code>
/// </example>
public interface ISettingsPage
{
    /// <summary>
    /// Gets the unique identifier for this settings category.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for navigation, deep linking, and parent-child relationships.
    /// Should be lowercase with dots for hierarchy (e.g., "editor.fonts").
    /// Must be unique across all registered pages.
    /// </remarks>
    string CategoryId { get; }

    /// <summary>
    /// Gets the display name shown in the settings navigation tree.
    /// </summary>
    /// <remarks>
    /// LOGIC: User-facing name that appears in the left navigation panel.
    /// Should be concise and descriptive.
    /// </remarks>
    string DisplayName { get; }

    /// <summary>
    /// Gets the parent category ID for hierarchical organization.
    /// </summary>
    /// <remarks>
    /// LOGIC: If null, this is a root-level category.
    /// Otherwise, this page appears as a child of the specified parent.
    /// The parent must exist or the page will be shown at root level.
    /// </remarks>
    string? ParentCategoryId { get; }

    /// <summary>
    /// Gets the icon name for the settings page.
    /// </summary>
    /// <remarks>
    /// LOGIC: Optional icon displayed next to the page name in navigation.
    /// Uses the application's icon system.
    /// </remarks>
    string? Icon { get; }

    /// <summary>
    /// Gets the sort order within the parent category.
    /// </summary>
    /// <remarks>
    /// LOGIC: Lower values appear first. Pages with the same SortOrder
    /// are sorted alphabetically by DisplayName.
    /// Recommended ranges: Core (0-99), Modules (100-999), Extensions (1000+).
    /// </remarks>
    int SortOrder { get; }

    /// <summary>
    /// Gets the minimum license tier required to see this settings page.
    /// </summary>
    /// <remarks>
    /// LOGIC: Pages are filtered based on the user's current license tier.
    /// Users with Core tier won't see WriterPro-only settings.
    /// Default implementation returns Core (visible to all).
    /// </remarks>
    LicenseTier RequiredTier => LicenseTier.Core;

    /// <summary>
    /// Gets additional keywords for search functionality.
    /// </summary>
    /// <remarks>
    /// LOGIC: These keywords are searched in addition to DisplayName and CategoryId
    /// when the user types in the settings search box.
    /// Include synonyms and related terms for better discoverability.
    /// </remarks>
    IReadOnlyList<string> SearchKeywords => Array.Empty<string>();

    /// <summary>
    /// Creates the view for this settings page.
    /// </summary>
    /// <returns>An Avalonia Control to display in the settings content area (typed as object to avoid framework dependency).</returns>
    /// <remarks>
    /// LOGIC: Called when the user selects this page in the navigation tree.
    /// Should return a new instance each time for proper memory management.
    /// The view typically has its own ViewModel for settings logic.
    /// The returned object should be an Avalonia Control.
    ///
    /// Exception Handling: If this method throws, the host will display
    /// an error message instead of crashing.
    /// </remarks>
    object CreateView();
}
