using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.Editing;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services.QuickFix;

/// <summary>
/// Integrates quick-fix actions with the editor context menu.
/// </summary>
/// <remarks>
/// LOGIC: ContextMenuIntegration handles UI concerns:
///
/// 1. Attach to TextArea for right-click and Ctrl+.
/// 2. Build ContextMenu from available QuickFixActions
/// 3. Handle menu item clicks to trigger ApplyQuickFix
///
/// Menu Structure:
/// - Single fix: Direct menu item
/// - Multiple fixes: Each fix as separate menu item
///
/// Thread Safety: All operations run on UI thread.
///
/// Version: v0.2.4d
/// </remarks>
public sealed class ContextMenuIntegration : IDisposable
{
    private readonly IQuickFixService _quickFixService;
    private readonly ILogger<ContextMenuIntegration> _logger;
    private TextArea? _textArea;
    private ContextMenu? _contextMenu;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextMenuIntegration"/> class.
    /// </summary>
    /// <param name="quickFixService">The quick-fix service.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ContextMenuIntegration(
        IQuickFixService quickFixService,
        ILogger<ContextMenuIntegration> logger)
    {
        _quickFixService = quickFixService ?? throw new ArgumentNullException(nameof(quickFixService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Raised when a fix is about to be applied (for document access).
    /// </summary>
    public event Action<QuickFixAction, Action<int, int, string>>? ApplyFixRequested;

    /// <summary>
    /// Attaches the context menu integration to a TextArea.
    /// </summary>
    /// <param name="textArea">The TextArea to attach to.</param>
    public void AttachToTextArea(TextArea textArea)
    {
        ArgumentNullException.ThrowIfNull(textArea);
        ThrowIfDisposed();

        DetachFromTextArea();
        _textArea = textArea;

        // LOGIC: Hook context menu opening
        _textArea.ContextRequested += OnContextRequested;

        _logger.LogDebug("ContextMenuIntegration attached to TextArea");
    }

    /// <summary>
    /// Detaches from the current TextArea.
    /// </summary>
    public void DetachFromTextArea()
    {
        if (_textArea is not null)
        {
            _textArea.ContextRequested -= OnContextRequested;
            _textArea = null;
        }

        HideMenu();
    }

    /// <summary>
    /// Registers the Ctrl+. keyboard shortcut.
    /// </summary>
    /// <param name="textArea">The TextArea to register on.</param>
    public void RegisterKeyboardShortcuts(TextArea textArea)
    {
        ArgumentNullException.ThrowIfNull(textArea);
        ThrowIfDisposed();

        textArea.KeyDown += OnKeyDown;
        _logger.LogDebug("Ctrl+. shortcut registered");
    }

    /// <summary>
    /// Shows the quick-fix menu at the current caret position.
    /// </summary>
    public void ShowQuickFixMenuAtCaret()
    {
        if (_textArea is null)
            return;

        ThrowIfDisposed();

        var offset = _textArea.Caret.Offset;
        ShowMenuAtOffset(offset);
    }

    /// <summary>
    /// Builds a context menu for the fixes at an offset.
    /// </summary>
    /// <param name="offset">The document offset.</param>
    /// <returns>The context menu, or null if no fixes available.</returns>
    public ContextMenu? BuildContextMenu(int offset)
    {
        ThrowIfDisposed();

        var fixes = _quickFixService.GetQuickFixesAtOffset(offset);

        if (fixes.Count == 0)
        {
            _logger.LogDebug("No quick-fixes available at offset {Offset}", offset);
            return null;
        }

        var menu = new ContextMenu();

        foreach (var fix in fixes)
        {
            var menuItem = new MenuItem
            {
                Header = fix.Title,
                Tag = fix
            };

            menuItem.Click += OnMenuItemClick;
            menu.Items.Add(menuItem);
        }

        _logger.LogDebug(
            "Built context menu with {Count} items",
            fixes.Count);

        return menu;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        DetachFromTextArea();
        _disposed = true;
        _logger.LogDebug("ContextMenuIntegration disposed");
    }

    /// <summary>
    /// Handles context menu requested event.
    /// </summary>
    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (_textArea is null)
            return;

        var offset = _textArea.Caret.Offset;
        var menu = BuildContextMenu(offset);

        if (menu is not null)
        {
            HideMenu();
            _contextMenu = menu;
            _contextMenu.Open(_textArea);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles keyboard shortcut (Ctrl+.).
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.OemPeriod && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            ShowQuickFixMenuAtCaret();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Shows the menu at a specific offset.
    /// </summary>
    private void ShowMenuAtOffset(int offset)
    {
        if (_textArea is null)
            return;

        var menu = BuildContextMenu(offset);
        if (menu is null)
        {
            _logger.LogDebug("No quick-fixes to show at offset {Offset}", offset);
            return;
        }

        HideMenu();
        _contextMenu = menu;

        // LOGIC: Position menu at caret visual position
        _contextMenu.Open(_textArea);

        _logger.LogDebug("Quick-fix menu shown at offset {Offset}", offset);
    }

    /// <summary>
    /// Handles menu item click.
    /// </summary>
    private void OnMenuItemClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: QuickFixAction fix })
        {
            _logger.LogDebug("Menu item clicked: {Title}", fix.Title);

            // LOGIC: Request the fix to be applied via event
            ApplyFixRequested?.Invoke(fix, (start, length, text) =>
            {
                _quickFixService.ApplyQuickFix(fix, (s, l, t) =>
                {
                    // This callback will be handled by ManuscriptView
                });
            });

            // LOGIC: Use the direct callback pattern
            if (ApplyFixRequested is null)
            {
                _logger.LogWarning("No handler for ApplyFixRequested - fix not applied");
            }
        }

        HideMenu();
    }

    /// <summary>
    /// Hides the current context menu.
    /// </summary>
    private void HideMenu()
    {
        if (_contextMenu is not null)
        {
            _contextMenu.Close();
            _contextMenu = null;
        }
    }

    /// <summary>
    /// Throws if disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
