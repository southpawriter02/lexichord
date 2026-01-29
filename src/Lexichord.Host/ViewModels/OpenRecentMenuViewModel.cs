using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// ViewModel for the "File > Open Recent" menu.
/// </summary>
/// <remarks>
/// LOGIC: This ViewModel:
/// - Loads recent files from IRecentFilesService
/// - Creates RecentFileMenuItemViewModel for each entry
/// - Handles file opening via IFileService
/// - Subscribes to RecentFilesChanged for live updates
/// - Provides ClearHistoryCommand
/// </remarks>
public partial class OpenRecentMenuViewModel : ObservableObject
{
    private readonly IRecentFilesService _recentFilesService;
    private readonly IMediator _mediator;
    private readonly ILogger<OpenRecentMenuViewModel> _logger;

    public OpenRecentMenuViewModel(
        IRecentFilesService recentFilesService,
        IMediator mediator,
        ILogger<OpenRecentMenuViewModel> logger)
    {
        _recentFilesService = recentFilesService ?? throw new ArgumentNullException(nameof(recentFilesService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _recentFilesService.RecentFilesChanged += OnRecentFilesChanged;

        // Load initial data
        _ = RefreshAsync();
    }

    /// <summary>
    /// Collection of recent file menu items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RecentFileMenuItemViewModel> _recentFiles = [];

    /// <summary>
    /// Whether there are any recent files to display.
    /// </summary>
    public bool HasRecentFiles => RecentFiles.Count > 0;

    /// <summary>
    /// Refreshes the recent files list from the service.
    /// </summary>
    public async Task RefreshAsync()
    {
        try
        {
            var entries = await _recentFilesService.GetRecentFilesAsync();

            RecentFiles.Clear();

            foreach (var entry in entries)
            {
                RecentFiles.Add(new RecentFileMenuItemViewModel(entry, e => _ = OpenFileAsync(e)));
            }

            OnPropertyChanged(nameof(HasRecentFiles));
            _logger.LogDebug("Refreshed recent files menu: {Count} items", RecentFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh recent files");
        }
    }

    /// <summary>
    /// Opens a file from the recent files list.
    /// </summary>
    /// <param name="entry">The recent file entry to open.</param>
    [RelayCommand]
    private async Task OpenFileAsync(RecentFileEntry entry)
    {
        if (!entry.Exists)
        {
            // LOGIC: File doesn't exist - remove from list
            _logger.LogWarning("Recent file not found: {FilePath}", entry.FilePath);
            await _recentFilesService.RemoveRecentFileAsync(entry.FilePath);
            // TODO: Show dialog "File not found. Removed from list."
            return;
        }

        _logger.LogInformation("Opening recent file: {FilePath}", entry.FilePath);

        // LOGIC: Publish event for document system to handle
        // The actual file loading is handled by the document manager
        await _mediator.Publish(new FileOpenedEvent(
            entry.FilePath,
            DateTimeOffset.UtcNow,
            FileOpenSource.RecentFiles));
    }

    /// <summary>
    /// Clears all recent file history.
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        _logger.LogInformation("Clearing recent files history");
        await _recentFilesService.ClearHistoryAsync();
    }

    private async void OnRecentFilesChanged(object? sender, RecentFilesChangedEventArgs e)
    {
        _logger.LogDebug("Recent files changed: {ChangeType}", e.ChangeType);
        await RefreshAsync();
    }
}
