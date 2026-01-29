namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Contract for tab-aware document ViewModels.
/// </summary>
/// <remarks>
/// LOGIC: Provides the interface that document ViewModels must implement to participate
/// in the tab infrastructure. Key behaviors include:
/// - Dirty state tracking with visual indicators
/// - Pin state for tab priority and protection
/// - Close confirmation with save prompts
/// 
/// Implementations should inherit from <see cref="Lexichord.Abstractions.ViewModels.DocumentViewModelBase"/>
/// which provides default implementations of most methods.
/// </remarks>
public interface IDocumentTab
{
    /// <summary>
    /// Gets the unique identifier for this document.
    /// </summary>
    /// <remarks>
    /// LOGIC: Should be a stable, unique identifier (e.g., file path or URI).
    /// Used for persistence, navigation, and tab service coordination.
    /// </remarks>
    string DocumentId { get; }

    /// <summary>
    /// Gets the display title of the document.
    /// </summary>
    /// <remarks>
    /// LOGIC: The base title without dirty indicators (e.g., "Chapter 1.md").
    /// </remarks>
    string Title { get; }

    /// <summary>
    /// Gets the display title including state indicators.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns Title with an asterisk suffix when dirty (e.g., "Chapter 1.md*").
    /// UI should bind to this for tab headers.
    /// </remarks>
    string DisplayTitle { get; }

    /// <summary>
    /// Gets or sets whether the document has unsaved changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: When true:
    /// - DisplayTitle shows asterisk suffix
    /// - CanCloseAsync prompts for save confirmation
    /// - Document appears in GetDirtyDocumentIds()
    /// </remarks>
    bool IsDirty { get; set; }

    /// <summary>
    /// Gets or sets whether the document tab is pinned.
    /// </summary>
    /// <remarks>
    /// LOGIC: Pinned documents:
    /// - Appear at the start of the tab strip
    /// - Are skipped during "Close All" operations (unless forced)
    /// - Have distinct visual styling
    /// </remarks>
    bool IsPinned { get; set; }

    /// <summary>
    /// Gets whether the document can be closed at all.
    /// </summary>
    /// <remarks>
    /// LOGIC: Some documents (e.g., Welcome tab) may not be closable.
    /// When false, the close button is hidden and close operations skip this tab.
    /// </remarks>
    bool CanClose { get; }

    /// <summary>
    /// Determines whether the document can be closed now.
    /// </summary>
    /// <returns>True if close can proceed; false to cancel.</returns>
    /// <remarks>
    /// LOGIC: Called before closing. When dirty:
    /// 1. Shows save confirmation dialog
    /// 2. If Save: calls SaveAsync(), returns its result
    /// 3. If DontSave: returns true (discard changes)
    /// 4. If Cancel: returns false
    /// 
    /// When clean, returns true immediately.
    /// </remarks>
    Task<bool> CanCloseAsync();

    /// <summary>
    /// Saves the document.
    /// </summary>
    /// <returns>True if save succeeded; false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Implementations should:
    /// 1. Persist the document content
    /// 2. Set IsDirty = false on success
    /// 3. Return false on error (IsDirty remains true)
    /// 
    /// The default implementation returns true immediately.
    /// </remarks>
    Task<bool> SaveAsync();

    /// <summary>
    /// Raised when document state changes (IsDirty, IsPinned).
    /// </summary>
    /// <remarks>
    /// LOGIC: Enables TabService to react to state changes for:
    /// - Tab reordering when IsPinned changes
    /// - Dirty document tracking
    /// - UI updates
    /// </remarks>
    event EventHandler<DocumentStateChangedEventArgs>? StateChanged;
}
