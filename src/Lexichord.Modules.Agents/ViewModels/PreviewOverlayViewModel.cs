// -----------------------------------------------------------------------
// <copyright file="PreviewOverlayViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel for the preview overlay control.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: Manages the visibility, content, and positioning of the inline
/// suggestion preview overlay. Subscribes to <see cref="IEditorInsertionService.PreviewStateChanged"/>
/// to synchronize state with the service layer.
/// </para>
/// <para><b>Introduced in:</b> v0.6.7b as part of the Inline Suggestions feature.</para>
/// </remarks>
public partial class PreviewOverlayViewModel : ObservableObject
{
    private readonly IEditorInsertionService _insertionService;
    private readonly ILogger<PreviewOverlayViewModel> _logger;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _previewText = string.Empty;

    [ObservableProperty]
    private double _top;

    [ObservableProperty]
    private double _left;

    /// <summary>
    /// Initializes a new instance of the PreviewOverlayViewModel.
    /// </summary>
    public PreviewOverlayViewModel(
        IEditorInsertionService insertionService,
        ILogger<PreviewOverlayViewModel> logger)
    {
        _insertionService = insertionService ?? throw new ArgumentNullException(nameof(insertionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to preview state changes
        _insertionService.PreviewStateChanged += OnPreviewStateChanged;
    }

    /// <summary>
    /// Command to accept the preview.
    /// </summary>
    [RelayCommand]
    private async Task AcceptAsync()
    {
        _logger.LogDebug("Accept command invoked");
        await _insertionService.AcceptPreviewAsync();
    }

    /// <summary>
    /// Command to reject the preview.
    /// </summary>
    [RelayCommand]
    private async Task RejectAsync()
    {
        _logger.LogDebug("Reject command invoked");
        await _insertionService.RejectPreviewAsync();
    }

    private void OnPreviewStateChanged(object? sender, PreviewStateChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsVisible = e.IsActive;
            PreviewText = e.PreviewText ?? string.Empty;

            if (e.IsActive && e.Location != null)
            {
                // Position overlay near the target location
                // This would involve coordinate translation from editor
                UpdatePosition(e.Location);
            }
        });
    }

    private void UpdatePosition(TextSpan location)
    {
        // Position calculation would be implemented based on editor layout
        // This is a placeholder for the actual coordinate translation
    }
}
