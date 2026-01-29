using Dock.Model.Core;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Represents a dockable document with Lexichord-specific properties.
/// </summary>
/// <remarks>
/// LOGIC: Extends IDockable to add document-specific behavior:
/// - Dirty state tracking for unsaved changes
/// - Pin state for tab behavior
/// - Close confirmation for unsaved work
///
/// Documents are typically displayed in the center DocumentDock
/// and represent content being edited (manuscripts, notes, etc.).
/// </remarks>
public interface IDocument : IDockable
{
    /// <summary>
    /// Gets a value indicating whether the document has unsaved changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: When true, the document title should display with a dirty indicator (e.g., "*")
    /// and close operations should prompt for confirmation.
    /// </remarks>
    bool IsDirty { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the document tab is pinned.
    /// </summary>
    /// <remarks>
    /// LOGIC: Pinned documents are excluded from "close all" operations
    /// and remain visible when switching contexts.
    /// </remarks>
    bool IsPinned { get; set; }

    /// <summary>
    /// Gets the unique identifier for the document used for serialization.
    /// </summary>
    /// <remarks>
    /// LOGIC: This identifier persists across sessions for layout restoration.
    /// It should be stable and unique within the application.
    /// </remarks>
    string DocumentId { get; }

    /// <summary>
    /// Confirms whether the document can be closed.
    /// </summary>
    /// <returns>True if the document can close; false to cancel the close operation.</returns>
    /// <remarks>
    /// LOGIC: Override this to prompt users to save before closing dirty documents.
    /// Default implementation should return false if IsDirty is true.
    /// </remarks>
    Task<bool> CanCloseAsync();
}
