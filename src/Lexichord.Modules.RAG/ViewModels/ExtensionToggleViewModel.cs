// =============================================================================
// File: ExtensionToggleViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for a file extension toggle button in the filter panel.
// =============================================================================
// LOGIC: ExtensionToggleViewModel manages the state of a single extension toggle
//        in the search filter panel. Each toggle represents a file extension that
//        can be selected to filter search results by file type. Changes to IsSelected
//        propagate via PropertyChanged to update the filter chips.
// =============================================================================
// VERSION: v0.5.5b (Filter UI Component)
// =============================================================================

using CommunityToolkit.Mvvm.ComponentModel;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for a file extension toggle button in the search filter panel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ExtensionToggleViewModel"/> represents a single file extension toggle
/// in the filter panel. Users can select one or more extensions to filter search
/// results to only documents with matching file types.
/// </para>
/// <para>
/// <b>Default Extensions:</b>
/// <list type="bullet">
///   <item><description>md - Markdown files</description></item>
///   <item><description>txt - Plain text files</description></item>
///   <item><description>json - JSON files</description></item>
///   <item><description>yaml - YAML files</description></item>
///   <item><description>rst - reStructuredText files</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5b as part of The Filter System feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var toggle = new ExtensionToggleViewModel("md", "Markdown", "M↓");
///
/// // Subscribe to selection changes
/// toggle.PropertyChanged += (s, e) =>
/// {
///     if (e.PropertyName == nameof(ExtensionToggleViewModel.IsSelected))
///     {
///         // Update filter chips
///     }
/// };
///
/// // Toggle selection
/// toggle.IsSelected = true;
/// </code>
/// </example>
public partial class ExtensionToggleViewModel : ObservableObject
{
    /// <summary>
    /// Gets the file extension without the leading dot.
    /// </summary>
    /// <value>The file extension (e.g., "md", "txt", "json").</value>
    /// <remarks>
    /// The extension is stored without the dot for consistency with
    /// <see cref="Abstractions.Contracts.SearchFilter.FileExtensions"/>.
    /// </remarks>
    public string Extension { get; }

    /// <summary>
    /// Gets the human-readable display name for the extension.
    /// </summary>
    /// <value>The display name (e.g., "Markdown", "Text", "JSON").</value>
    /// <remarks>
    /// Used in tooltips and accessibility labels.
    /// </remarks>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the icon or symbol for this extension.
    /// </summary>
    /// <value>A short string icon (e.g., "M↓", "T", "{}").</value>
    /// <remarks>
    /// Used as visual indicator on the toggle button when screen space is limited.
    /// </remarks>
    public string Icon { get; }

    /// <summary>
    /// Gets or sets whether this extension is selected for filtering.
    /// </summary>
    /// <value><c>true</c> if selected; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <para>
    /// When changed, raises <see cref="ObservableObject.PropertyChanged"/> event
    /// which the parent <see cref="SearchFilterPanelViewModel"/> listens to
    /// for updating filter chips.
    /// </para>
    /// <para>
    /// Initial value is <c>false</c> (extension not selected).
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionToggleViewModel"/> class.
    /// </summary>
    /// <param name="extension">The file extension without leading dot (e.g., "md").</param>
    /// <param name="displayName">The human-readable display name (e.g., "Markdown").</param>
    /// <param name="icon">The icon or symbol for the toggle button (e.g., "M↓").</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="extension"/>, <paramref name="displayName"/>,
    /// or <paramref name="icon"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// var markdownToggle = new ExtensionToggleViewModel("md", "Markdown", "M↓");
    /// var jsonToggle = new ExtensionToggleViewModel("json", "JSON", "{}");
    /// </code>
    /// </example>
    public ExtensionToggleViewModel(string extension, string displayName, string icon)
    {
        Extension = extension ?? throw new ArgumentNullException(nameof(extension));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
    }
}
