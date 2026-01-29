using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Host.Services;

/// <summary>
/// Service implementation for managing the Most Recently Used files list.
/// </summary>
/// <remarks>
/// LOGIC: This service:
/// - Uses IRecentFilesRepository for persistence
/// - Checks File.Exists() when loading entries
/// - Auto-trims to MaxEntries after additions
/// - Handles FileOpenedEvent to automatically track opened files
/// - Raises RecentFilesChanged for UI updates
/// </remarks>
public sealed class RecentFilesService : IRecentFilesService, INotificationHandler<FileOpenedEvent>
{
    private readonly IRecentFilesRepository _repository;
    private readonly RecentFilesOptions _options;
    private readonly ILogger<RecentFilesService> _logger;

    public RecentFilesService(
        IRecentFilesRepository repository,
        IOptions<RecentFilesOptions> options,
        ILogger<RecentFilesService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public event EventHandler<RecentFilesChangedEventArgs>? RecentFilesChanged;

    /// <inheritdoc />
    public int MaxEntries => _options.MaxEntries;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentFileEntry>> GetRecentFilesAsync(
        int maxCount = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting recent files (max: {MaxCount})", maxCount);

        var effectiveMax = Math.Min(maxCount, _options.MaxEntries);
        var entries = await _repository.GetRecentFilesAsync(effectiveMax, cancellationToken);

        // LOGIC: Check file existence for each entry
        var results = entries.Select(e => e with
        {
            Exists = File.Exists(e.FilePath)
        }).ToList();

        var existingCount = results.Count(r => r.Exists);
        _logger.LogDebug(
            "Retrieved {Total} recent files ({Existing} exist)",
            results.Count, existingCount);

        return results;
    }

    /// <inheritdoc />
    public async Task AddRecentFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        _logger.LogInformation("Adding to recent files: {FilePath}", filePath);

        var fileName = Path.GetFileName(filePath);
        var entry = new RecentFileEntry(
            filePath,
            fileName,
            DateTimeOffset.UtcNow,
            OpenCount: 1,
            Exists: true);

        await _repository.UpsertAsync(entry, cancellationToken);

        // LOGIC: Trim to max entries
        await _repository.TrimToCountAsync(_options.MaxEntries, cancellationToken);

        RaiseChanged(RecentFilesChangeType.Added, filePath, entry);
    }

    /// <inheritdoc />
    public async Task RemoveRecentFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        _logger.LogInformation("Removing from recent files: {FilePath}", filePath);

        var deleted = await _repository.DeleteAsync(filePath, cancellationToken);

        if (deleted)
        {
            RaiseChanged(RecentFilesChangeType.Removed, filePath);
        }
    }

    /// <inheritdoc />
    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing recent files history");

        await _repository.ClearAllAsync(cancellationToken);

        RaiseChanged(RecentFilesChangeType.Cleared);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> PruneInvalidEntriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pruning invalid entries from recent files");

        var entries = await _repository.GetRecentFilesAsync(
            _options.MaxEntries,
            cancellationToken);

        var prunedPaths = new List<string>();

        foreach (var entry in entries)
        {
            if (!File.Exists(entry.FilePath))
            {
                await _repository.DeleteAsync(entry.FilePath, cancellationToken);
                prunedPaths.Add(entry.FilePath);
            }
        }

        if (prunedPaths.Count > 0)
        {
            _logger.LogInformation("Pruned {Count} invalid entries", prunedPaths.Count);
            RaiseChanged(RecentFilesChangeType.Pruned);
        }

        return prunedPaths;
    }

    /// <summary>
    /// Handles FileOpenedEvent to automatically track opened files.
    /// </summary>
    /// <param name="notification">The file opened event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(FileOpenedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Handling FileOpenedEvent: {FilePath} (source: {Source})",
            notification.FilePath, notification.Source);

        await AddRecentFileAsync(notification.FilePath, cancellationToken);
    }

    private void RaiseChanged(
        RecentFilesChangeType changeType,
        string? filePath = null,
        RecentFileEntry? entry = null)
    {
        RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs
        {
            ChangeType = changeType,
            FilePath = filePath,
            Entry = entry
        });
    }
}
