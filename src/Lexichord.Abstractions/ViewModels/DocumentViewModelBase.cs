using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Layout;
using Lexichord.Abstractions.Services;

namespace Lexichord.Abstractions.ViewModels;

/// <summary>
/// Base class for document ViewModels that participate in the tab infrastructure.
/// </summary>
/// <remarks>
/// LOGIC: Provides default implementation of <see cref="IDocumentTab"/>:
/// - Automatic DisplayTitle with asterisk when dirty
/// - CanCloseAsync with save dialog integration
/// - StateChanged event propagation
/// - MarkDirty/MarkClean helper methods
/// 
/// Derived classes should:
/// 1. Set Title and DocumentId in constructor
/// 2. Override SaveAsync() to implement actual persistence
/// 3. Call MarkDirty() when content changes
/// </remarks>
public abstract partial class DocumentViewModelBase : ObservableObject, IDocumentTab
{
    private readonly ISaveDialogService? _saveDialogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentViewModelBase"/> class.
    /// </summary>
    /// <param name="saveDialogService">Service for showing save dialogs. Can be null for testing.</param>
    protected DocumentViewModelBase(ISaveDialogService? saveDialogService = null)
    {
        _saveDialogService = saveDialogService;
    }

    /// <inheritdoc />
    public abstract string DocumentId { get; }

    /// <inheritdoc />
    public abstract string Title { get; }

    /// <inheritdoc />
    public virtual string DisplayTitle => IsDirty ? $"{Title}*" : Title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isPinned;

    /// <inheritdoc />
    public virtual bool CanClose => true;

    /// <inheritdoc />
    public event EventHandler<DocumentStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Raises the StateChanged event.
    /// </summary>
    protected virtual void OnStateChanged(string propertyName, object? oldValue, object? newValue)
    {
        StateChanged?.Invoke(this, new DocumentStateChangedEventArgs(propertyName, oldValue, newValue));
    }

    partial void OnIsDirtyChanged(bool oldValue, bool newValue)
    {
        OnStateChanged(nameof(IsDirty), oldValue, newValue);
    }

    partial void OnIsPinnedChanged(bool oldValue, bool newValue)
    {
        OnStateChanged(nameof(IsPinned), oldValue, newValue);
    }

    /// <inheritdoc />
    public virtual async Task<bool> CanCloseAsync()
    {
        // LOGIC: Clean documents can always close
        if (!IsDirty)
        {
            return true;
        }

        // LOGIC: If no dialog service, block close for dirty documents
        if (_saveDialogService == null)
        {
            return false;
        }

        // LOGIC: Show save confirmation dialog
        var result = await _saveDialogService.ShowSaveDialogAsync(Title);

        return result switch
        {
            SaveDialogResult.Save => await SaveAsync(),
            SaveDialogResult.DontSave => true,
            SaveDialogResult.Cancel => false,
            _ => false
        };
    }

    /// <inheritdoc />
    public virtual Task<bool> SaveAsync()
    {
        // LOGIC: Default implementation does nothing and returns true
        // Derived classes should override to implement actual persistence
        IsDirty = false;
        return Task.FromResult(true);
    }

    /// <summary>
    /// Marks the document as having unsaved changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Call this method when content is modified. The dirty state
    /// triggers visual indicators and close confirmation prompts.
    /// </remarks>
    protected void MarkDirty()
    {
        if (!IsDirty)
        {
            IsDirty = true;
        }
    }

    /// <summary>
    /// Marks the document as saved/clean.
    /// </summary>
    /// <remarks>
    /// LOGIC: Call this after a successful save operation.
    /// </remarks>
    protected void MarkClean()
    {
        if (IsDirty)
        {
            IsDirty = false;
        }
    }
}
