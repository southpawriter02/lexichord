// =============================================================================
// File: PreviewPaneViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for the split-view preview pane.
// =============================================================================
// LOGIC: Manages state and commands for the preview pane UI.
//   - SelectedHit: Bound from parent, triggers async preview loading.
//   - Content: Current PreviewContent (or null if loading/empty).
//   - IsLoading: True while fetching content from builder.
//   - IsVisible: Toggle pane visibility.
//   - Commands: Toggle, OpenInEditor, CopyContent, CopyAll.
//   - License gating via ShowUpgradePrompt.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.1.3a: IEditorService (open documents).
//   - v0.0.4c: ILicenseContext (license gating).
//   - v0.5.7c: IPreviewContentBuilder, PreviewContent.
// =============================================================================

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for the split-view preview pane.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PreviewPaneViewModel"/> manages the state and behavior of the
/// preview pane UI, which displays expanded context for the currently selected
/// search result.
/// </para>
/// <para>
/// <b>Async Loading:</b> Selection changes trigger async preview loading with
/// cancellation support. Rapid selection changes cancel previous loads to prevent
/// race conditions.
/// </para>
/// <para>
/// <b>License Gating:</b> The preview pane is gated behind
/// <see cref="FeatureFlags.RAG.ReferenceDock"/>. Unlicensed users see an upgrade
/// prompt instead of preview content.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7c as part of the Preview Pane feature.
/// </para>
/// </remarks>
public partial class PreviewPaneViewModel : ObservableObject, IDisposable
{
    private readonly IPreviewContentBuilder _contentBuilder;
    private readonly IEditorService _editorService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<PreviewPaneViewModel> _logger;
    private CancellationTokenSource? _loadingCts;
    private bool _disposed;

    // =========================================================================
    // State Properties
    // =========================================================================

    /// <summary>
    /// The currently selected search hit (bound from parent ViewModel).
    /// </summary>
    /// <remarks>
    /// LOGIC: Setting this property triggers async preview loading via
    /// <see cref="OnSelectedHitChanged"/>.
    /// </remarks>
    [ObservableProperty]
    private SearchHit? _selectedHit;

    /// <summary>
    /// The preview content to display.
    /// </summary>
    /// <remarks>
    /// LOGIC: Null when no selection, loading, or error. Set by async load.
    /// </remarks>
    [ObservableProperty]
    private PreviewContent? _content;

    /// <summary>
    /// Whether the preview is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Whether the preview pane is visible.
    /// </summary>
    /// <remarks>
    /// LOGIC: Toggled by <see cref="TogglePreviewCommand"/>. When false,
    /// <see cref="PreviewWidth"/> returns 0 to collapse the pane.
    /// </remarks>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// Error message if preview loading failed.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Whether to show the upgrade prompt for unlicensed users.
    /// </summary>
    [ObservableProperty]
    private bool _showUpgradePrompt;

    // =========================================================================
    // Computed Properties
    // =========================================================================

    /// <summary>
    /// Width of the preview pane column for GridSplitter binding.
    /// </summary>
    /// <value>
    /// <c>1*</c> when visible; <c>0</c> when hidden.
    /// </value>
    public GridLength PreviewWidth => IsVisible
        ? new GridLength(1, GridUnitType.Star)
        : new GridLength(0);

    /// <summary>
    /// Whether the preview pane should show placeholder content.
    /// </summary>
    /// <value>
    /// <c>true</c> when not loading, no content, and no upgrade prompt.
    /// </value>
    public bool ShowPlaceholder =>
        !IsLoading && Content?.HasContent != true && !ShowUpgradePrompt && ErrorMessage is null;

    /// <summary>
    /// Whether there is content to display.
    /// </summary>
    public bool HasContent => Content?.HasContent == true;

    // =========================================================================
    // Lifecycle
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewPaneViewModel"/>.
    /// </summary>
    /// <param name="contentBuilder">Builder for preview content.</param>
    /// <param name="editorService">Service for opening documents in editor.</param>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public PreviewPaneViewModel(
        IPreviewContentBuilder contentBuilder,
        IEditorService editorService,
        ILicenseContext licenseContext,
        ILogger<PreviewPaneViewModel> logger)
    {
        _contentBuilder = contentBuilder ?? throw new ArgumentNullException(nameof(contentBuilder));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("PreviewPaneViewModel initialized");
    }

    /// <summary>
    /// Handles selection changes to update preview content.
    /// </summary>
    partial void OnSelectedHitChanged(SearchHit? value)
    {
        _logger.LogDebug("Selected hit changed: {HasHit}", value is not null);
        _ = LoadPreviewAsync(value);
    }

    /// <summary>
    /// Handles visibility changes to update grid width.
    /// </summary>
    partial void OnIsVisibleChanged(bool value)
    {
        _logger.LogDebug("Preview visibility changed: {IsVisible}", value);
        OnPropertyChanged(nameof(PreviewWidth));
    }

    // =========================================================================
    // Commands
    // =========================================================================

    /// <summary>
    /// Toggles the visibility of the preview pane.
    /// </summary>
    [RelayCommand]
    private void TogglePreview()
    {
        IsVisible = !IsVisible;
        _logger.LogDebug("Preview toggled: visible={IsVisible}", IsVisible);
    }

    /// <summary>
    /// Opens the current result in the main editor.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenInEditor))]
    private async Task OpenInEditorAsync()
    {
        if (SelectedHit is null) return;

        var path = SelectedHit.Document.FilePath;
        var line = Content?.LineNumber ?? 0;

        _logger.LogDebug("Opening in editor: {Path} at line {Line}", path, line);

        try
        {
            await _editorService.OpenDocumentAsync(path);
            _logger.LogInformation("Opened document in editor: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open document in editor: {Path}", path);
        }
    }

    private bool CanOpenInEditor() => SelectedHit is not null;

    /// <summary>
    /// Copies the matched content to clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCopyContent))]
    private async Task CopyContentAsync()
    {
        if (Content is null) return;

        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            _logger.LogWarning("Clipboard not available");
            return;
        }

        await clipboard.SetTextAsync(Content.MatchedContent);
        _logger.LogDebug("Copied preview content to clipboard ({Length} chars)",
            Content.MatchedContent.Length);
    }

    private bool CanCopyContent() => Content?.HasContent == true;

    /// <summary>
    /// Copies all content (context + match) to clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCopyContent))]
    private async Task CopyAllAsync()
    {
        if (Content is null) return;

        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            _logger.LogWarning("Clipboard not available");
            return;
        }

        var full = string.Join("\n",
            Content.PrecedingContext,
            Content.MatchedContent,
            Content.FollowingContext).Trim();

        await clipboard.SetTextAsync(full);
        _logger.LogDebug("Copied full preview content to clipboard ({Length} chars)", full.Length);
    }

    // =========================================================================
    // Public Methods
    // =========================================================================

    /// <summary>
    /// Clears the current preview content and state.
    /// </summary>
    public void Clear()
    {
        _loadingCts?.Cancel();
        SelectedHit = null;
        Content = null;
        ErrorMessage = null;
        ShowUpgradePrompt = false;
        IsLoading = false;

        _logger.LogDebug("Preview pane cleared");
    }

    /// <summary>
    /// Refreshes the preview for the current selection.
    /// </summary>
    public void Refresh()
    {
        if (SelectedHit is not null)
        {
            _ = LoadPreviewAsync(SelectedHit);
        }
    }

    // =========================================================================
    // Private Methods
    // =========================================================================

    /// <summary>
    /// Loads preview content asynchronously for a search hit.
    /// </summary>
    private async Task LoadPreviewAsync(SearchHit? hit)
    {
        // Cancel any in-progress load
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();
        var ct = _loadingCts.Token;

        // Clear state
        ErrorMessage = null;
        ShowUpgradePrompt = false;

        if (hit is null)
        {
            Content = null;
            return;
        }

        // Check license
        if (!_licenseContext.IsFeatureEnabled("RAG-PREVIEW-PANE"))
        {
            _logger.LogDebug("Preview pane requires license — showing upgrade prompt");
            ShowUpgradePrompt = true;
            Content = null;
            return;
        }

        IsLoading = true;
        var sw = Stopwatch.StartNew();

        try
        {
            var options = PreviewOptions.Default;
            var content = await _contentBuilder.BuildAsync(hit, options, ct);

            if (ct.IsCancellationRequested) return;

            Content = content;

            sw.Stop();
            _logger.LogDebug(
                "Preview updated for document {DocumentPath} in {ElapsedMs}ms",
                hit.Document.FilePath, sw.ElapsedMilliseconds);

            // Update computed properties
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(ShowPlaceholder));
        }
        catch (OperationCanceledException)
        {
            // Ignore — new selection started
            _logger.LogDebug("Preview load cancelled (new selection)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load preview for {Path}", hit.Document.FilePath);
            ErrorMessage = "Failed to load preview";
            Content = null;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowPlaceholder));
        }
    }

    /// <summary>
    /// Gets the system clipboard from the application lifetime.
    /// </summary>
    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }

        return null;
    }

    /// <summary>
    /// Disposes resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _disposed = true;

        _logger.LogDebug("PreviewPaneViewModel disposed");
        GC.SuppressFinalize(this);
    }
}
