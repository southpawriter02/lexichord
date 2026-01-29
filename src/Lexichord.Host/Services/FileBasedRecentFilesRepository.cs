using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// File-based implementation of IRecentFilesRepository for local development.
/// </summary>
/// <remarks>
/// LOGIC: This implementation stores recent files as JSON in the user's AppData directory.
/// It provides a database-free fallback for development scenarios where PostgreSQL
/// isn't running. For production use with persistence across devices, the database-backed
/// repository in Lexichord.Infrastructure should be used.
/// </remarks>
public sealed class FileBasedRecentFilesRepository : IRecentFilesRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly ILogger<FileBasedRecentFilesRepository> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileBasedRecentFilesRepository(ILogger<FileBasedRecentFilesRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Store in user's AppData alongside other Lexichord settings
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var lexichordDir = Path.Combine(appData, "Lexichord");
        Directory.CreateDirectory(lexichordDir);
        _filePath = Path.Combine(lexichordDir, "recent-files.json");

        _logger.LogDebug("FileBasedRecentFilesRepository initialized. Path: {FilePath}", _filePath);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentFileEntry>> GetRecentFilesAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);
            return entries
                .OrderByDescending(e => e.LastOpenedAt)
                .Take(maxCount)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(
        RecentFileEntry entry,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);

            // LOGIC: Remove existing entry with same path, then add new entry
            var updated = entries
                .Where(e => !string.Equals(e.FilePath, entry.FilePath, StringComparison.OrdinalIgnoreCase))
                .Append(entry)
                .OrderByDescending(e => e.LastOpenedAt)
                .ToList();

            await SaveEntriesAsync(updated, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Upserted recent file: {FilePath}", entry.FilePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);
            var before = entries.Count;

            var updated = entries
                .Where(e => !string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (updated.Count < before)
            {
                await SaveEntriesAsync(updated, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Deleted recent file: {FilePath}", filePath);
                return true;
            }

            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> TrimToCountAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);

            if (entries.Count > maxCount)
            {
                var trimmed = entries
                    .OrderByDescending(e => e.LastOpenedAt)
                    .Take(maxCount)
                    .ToList();

                var deletedCount = entries.Count - trimmed.Count;
                await SaveEntriesAsync(trimmed, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Trimmed recent files from {Before} to {After}", entries.Count, maxCount);
                return deletedCount;
            }

            return 0;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RecentFileEntry?> GetByFilePathAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);
            return entries.FirstOrDefault(e => 
                string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);
            return entries.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SaveEntriesAsync(new List<RecentFileEntry>(), cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Cleared all recent files");
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<RecentFileEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<RecentFileEntry>();
            }

            var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<RecentFileEntry>>(json, JsonOptions) ?? new List<RecentFileEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load recent files from {FilePath}", _filePath);
            return new List<RecentFileEntry>();
        }
    }

    private async Task SaveEntriesAsync(List<RecentFileEntry> entries, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(entries, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save recent files to {FilePath}", _filePath);
        }
    }
}
