// =============================================================================
// File: FolderNodeViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for a folder node in the filter tree with selection propagation.
// =============================================================================
// LOGIC: FolderNodeViewModel represents a folder in the workspace tree. Key behaviors:
//   1. Selection propagates to all children (selecting parent selects all children).
//   2. IsPartiallySelected indicates when some but not all children are selected.
//   3. GetGlobPattern() returns the glob pattern for filtering (e.g., "docs/**").
//   4. IsExpanded controls tree node expansion state.
// =============================================================================
// VERSION: v0.5.5b (Filter UI Component)
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for a folder node in the search filter tree.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FolderNodeViewModel"/> represents a folder in the workspace directory tree
/// displayed in the search filter panel. Users can select folders to filter search
/// results to only documents within those folders.
/// </para>
/// <para>
/// <b>Selection Behavior:</b>
/// <list type="bullet">
///   <item><description>Selecting a parent folder selects all child folders recursively.</description></item>
///   <item><description>Deselecting a parent folder deselects all child folders recursively.</description></item>
///   <item><description>Partial selection is indicated when some but not all children are selected.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Glob Pattern:</b>
/// Selected folders are converted to glob patterns using <see cref="GetGlobPattern"/>.
/// For example, selecting "docs/" generates the pattern "docs/**" to match all
/// files recursively within that folder.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5b as part of The Filter System feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a folder hierarchy
/// var docs = new FolderNodeViewModel("docs", "/workspace/docs");
/// var specs = new FolderNodeViewModel("specs", "/workspace/docs/specs");
/// var guides = new FolderNodeViewModel("guides", "/workspace/docs/guides");
/// docs.Children.Add(specs);
/// docs.Children.Add(guides);
///
/// // Selecting parent propagates to children
/// docs.IsSelected = true;
/// // specs.IsSelected == true
/// // guides.IsSelected == true
///
/// // Get glob pattern for filtering
/// var pattern = docs.GetGlobPattern(); // "/workspace/docs/**"
/// </code>
/// </example>
public partial class FolderNodeViewModel : ObservableObject
{
    // =========================================================================
    // Fields
    // =========================================================================

    /// <summary>
    /// Flag to prevent recursive propagation loops.
    /// </summary>
    private bool _isPropagating;

    // =========================================================================
    // Properties
    // =========================================================================

    /// <summary>
    /// Gets the folder name for display.
    /// </summary>
    /// <value>The folder name without path (e.g., "docs", "specs").</value>
    public string Name { get; }

    /// <summary>
    /// Gets the full path to the folder.
    /// </summary>
    /// <value>The absolute path to the folder.</value>
    /// <remarks>
    /// The path is relative to the workspace root and uses forward slashes
    /// for consistency across platforms.
    /// </remarks>
    public string Path { get; }

    /// <summary>
    /// Gets or sets whether this folder is selected for filtering.
    /// </summary>
    /// <value><c>true</c> if selected; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <para>
    /// When changed, automatically propagates the selection state to all child
    /// folders recursively via <see cref="OnIsSelectedChanged"/>.
    /// </para>
    /// <para>
    /// Also notifies <see cref="IsPartiallySelected"/> for UI update.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPartiallySelected))]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets whether this folder node is expanded in the tree.
    /// </summary>
    /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Initial value is <c>true</c> for the first two levels of the tree.
    /// </remarks>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// Gets the child folder nodes.
    /// </summary>
    /// <value>An observable collection of child folders.</value>
    /// <remarks>
    /// The collection is observable to support dynamic tree updates.
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<FolderNodeViewModel> _children = new();

    /// <summary>
    /// Gets whether some but not all children are selected.
    /// </summary>
    /// <value>
    /// <c>true</c> if at least one but not all children are selected;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Used for tri-state checkbox display in the UI:
    /// <list type="bullet">
    ///   <item><description>Unchecked: <see cref="IsSelected"/> = false, <see cref="IsPartiallySelected"/> = false</description></item>
    ///   <item><description>Checked: <see cref="IsSelected"/> = true, <see cref="IsPartiallySelected"/> = false</description></item>
    ///   <item><description>Indeterminate: <see cref="IsSelected"/> = false, <see cref="IsPartiallySelected"/> = true</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Returns <c>false</c> if there are no children.
    /// </para>
    /// </remarks>
    public bool IsPartiallySelected
    {
        get
        {
            if (Children.Count == 0)
            {
                return false;
            }

            var selectedCount = Children.Count(c => c.IsSelected || c.IsPartiallySelected);
            return selectedCount > 0 && selectedCount < Children.Count;
        }
    }

    // =========================================================================
    // Constructor
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderNodeViewModel"/> class.
    /// </summary>
    /// <param name="name">The folder name for display.</param>
    /// <param name="path">The full path to the folder.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="path"/> is null.
    /// </exception>
    public FolderNodeViewModel(string name, string path)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    // =========================================================================
    // Methods
    // =========================================================================

    /// <summary>
    /// Gets the glob pattern for this folder suitable for search filtering.
    /// </summary>
    /// <returns>
    /// A glob pattern matching all files recursively in this folder
    /// (e.g., "/workspace/docs/**").
    /// </returns>
    /// <remarks>
    /// The pattern uses "**" to match all files and subdirectories recursively.
    /// </remarks>
    /// <example>
    /// <code>
    /// var folder = new FolderNodeViewModel("docs", "/workspace/docs");
    /// var pattern = folder.GetGlobPattern();
    /// // pattern == "/workspace/docs/**"
    /// </code>
    /// </example>
    public string GetGlobPattern()
    {
        return $"{Path}/**";
    }

    // =========================================================================
    // Property Change Handlers
    // =========================================================================

    /// <summary>
    /// Called when <see cref="IsSelected"/> changes.
    /// Propagates the selection state to all children recursively.
    /// </summary>
    /// <param name="value">The new selection state.</param>
    /// <remarks>
    /// Uses a guard flag (<see cref="_isPropagating"/>) to prevent infinite
    /// recursion when parent selection triggers child selection which would
    /// otherwise trigger parent notification.
    /// </remarks>
    partial void OnIsSelectedChanged(bool value)
    {
        // LOGIC: Prevent infinite recursion during propagation.
        if (_isPropagating)
        {
            return;
        }

        _isPropagating = true;
        try
        {
            // LOGIC: Propagate selection state to all children.
            foreach (var child in Children)
            {
                child.IsSelected = value;
            }
        }
        finally
        {
            _isPropagating = false;
        }
    }
}
