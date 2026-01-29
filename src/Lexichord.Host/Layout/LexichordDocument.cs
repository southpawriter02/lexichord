using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using Lexichord.Abstractions.Layout;

namespace Lexichord.Host.Layout;

/// <summary>
/// Lexichord-specific document implementation.
/// </summary>
/// <remarks>
/// LOGIC: Extends Dock.Model.Mvvm's Document with:
/// - Dirty state tracking for unsaved changes
/// - Pin state for tab behavior
/// - Close confirmation hook
///
/// Uses CommunityToolkit.Mvvm for observable property generation.
/// </remarks>
public partial class LexichordDocument : Document, IDocument
{
    /// <summary>
    /// Gets or sets a value indicating whether the document has unsaved changes.
    /// </summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>
    /// Gets or sets a value indicating whether the document tab is pinned.
    /// </summary>
    [ObservableProperty]
    private bool _isPinned;

    /// <summary>
    /// Gets the unique identifier for serialization.
    /// </summary>
    public string DocumentId => Id ?? string.Empty;

    /// <summary>
    /// Gets the display title with dirty indicator.
    /// </summary>
    /// <remarks>
    /// LOGIC: Appends an asterisk (*) to the title when the document has unsaved changes,
    /// following the standard convention for dirty documents.
    /// </remarks>
    public string DisplayTitle => IsDirty ? $"{Title}*" : Title ?? string.Empty;

    /// <summary>
    /// Called when IsDirty changes to notify DisplayTitle observers.
    /// </summary>
    partial void OnIsDirtyChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayTitle));
    }

    /// <summary>
    /// Confirms whether the document can be closed.
    /// </summary>
    /// <returns>True if the document can close; false to cancel the close operation.</returns>
    /// <remarks>
    /// LOGIC: Default implementation returns false if IsDirty is true.
    /// Subclasses should override to show a save dialog before closing dirty documents.
    /// </remarks>
    public virtual Task<bool> CanCloseAsync()
    {
        // LOGIC: Block close if document has unsaved changes
        // Subclasses should override to prompt user for save
        return Task.FromResult(!IsDirty);
    }

    /// <summary>
    /// Called when the document is being closed.
    /// </summary>
    /// <returns>True if the close should proceed; false to cancel.</returns>
    public override bool OnClose()
    {
        // LOGIC: Synchronous close check for immediate response
        // For async checks, use CanCloseAsync() in higher-level handlers
        return !IsDirty || base.OnClose();
    }
}
