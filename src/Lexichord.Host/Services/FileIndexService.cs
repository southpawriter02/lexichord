using System.Collections.Concurrent;
using System.Security;
using FuzzySharp;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Host.Services;

/// <summary>
/// File indexing service implementation.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): FileIndexService is a singleton that:
/// - Indexes all files in the workspace when opened
/// - Provides fast fuzzy search via FuzzySharp
/// - Tracks recently accessed files for quick access
/// - Handles incremental updates from file system events
/// 
/// Thread-safety:
/// - ConcurrentDictionary for index storage
/// - Lock for recent files list manipulation
/// </remarks>
public sealed class FileIndexService : IFileIndexService, IDisposable
{
    private const int MinFuzzyScore = 40;
    private const int ProgressReportInterval = 100;

    private readonly ConcurrentDictionary<string, FileIndexEntry> _index = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<string> _recentFiles = new();
    private readonly HashSet<string> _recentFilesSet = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _recentLock = new();

    private readonly IMediator _mediator;
    private readonly ILogger<FileIndexService> _logger;
    private readonly FileIndexSettings _settings;
    private readonly IgnorePatternMatcher _patternMatcher;

    private string? _workspaceRoot;
    private bool _isIndexing;
    private CancellationTokenSource? _indexingCts;

    /// <inheritdoc />
    public int IndexedFileCount => _index.Count;

    /// <inheritdoc />
    public bool IsIndexing => _isIndexing;

    /// <inheritdoc />
    public string? WorkspaceRoot => _workspaceRoot;

    /// <inheritdoc />
    public event EventHandler<FileIndexChangedEventArgs>? IndexChanged;

    /// <inheritdoc />
    public event EventHandler<FileIndexProgressEventArgs>? IndexProgress;

    /// <summary>
    /// Creates a new FileIndexService.
    /// </summary>
    public FileIndexService(
        IMediator mediator,
        ILogger<FileIndexService> logger,
        IOptions<FileIndexSettings>? settings = null)
    {
        _mediator = mediator;
        _logger = logger;
        _settings = settings?.Value ?? new FileIndexSettings();
        _patternMatcher = new IgnorePatternMatcher(_settings.IgnorePatterns);

        _logger.LogDebug("FileIndexService initialized with {PatternCount} ignore patterns",
            _settings.IgnorePatterns.Count);
    }

    /// <inheritdoc />
    public async Task<int> RebuildIndexAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            throw new ArgumentException("Workspace root cannot be empty.", nameof(workspaceRoot));

        if (!Directory.Exists(workspaceRoot))
            throw new DirectoryNotFoundException($"Workspace not found: {workspaceRoot}");

        // Cancel any existing indexing operation
        _indexingCts?.Cancel();
        _indexingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _indexingCts.Token;

        _logger.LogInformation("Starting file index rebuild for: {WorkspaceRoot}", workspaceRoot);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _isIndexing = true;
        var filesIndexed = 0;

        try
        {
            _workspaceRoot = Path.GetFullPath(workspaceRoot);
            _index.Clear();

            lock (_recentLock)
            {
                _recentFiles.Clear();
                _recentFilesSet.Clear();
            }

            await Task.Run(() =>
            {
                var enumerationOptions = new EnumerationOptions
                {
                    RecurseSubdirectories = _settings.Recursive,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.System
                };

                if (_settings.MaxDepth > 0)
                {
                    enumerationOptions.MaxRecursionDepth = _settings.MaxDepth;
                }

                string? currentDirectory = null;

                foreach (var file in Directory.EnumerateFiles(_workspaceRoot, "*", enumerationOptions))
                {
                    token.ThrowIfCancellationRequested();

                    var directory = Path.GetDirectoryName(file);
                    if (directory != currentDirectory)
                    {
                        currentDirectory = directory;
                        if (filesIndexed % ProgressReportInterval == 0)
                        {
                            IndexProgress?.Invoke(this, new FileIndexProgressEventArgs
                            {
                                FilesIndexed = filesIndexed,
                                CurrentDirectory = GetRelativePath(directory)
                            });
                        }
                    }

                    if (TryCreateEntry(file, out var entry))
                    {
                        _index.TryAdd(entry!.FullPath, entry);
                        filesIndexed++;
                    }
                }
            }, token);

            stopwatch.Stop();
            _logger.LogInformation("File index rebuilt: {FileCount} files in {ElapsedMs}ms",
                filesIndexed, stopwatch.ElapsedMilliseconds);

            // Raise events
            IndexChanged?.Invoke(this, new FileIndexChangedEventArgs
            {
                ChangeType = FileIndexChangeType.IndexRebuilt,
                TotalFileCount = filesIndexed
            });

            await _mediator.Publish(new FileIndexRebuiltEvent(_workspaceRoot, filesIndexed), token);

            return filesIndexed;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File index rebuild was cancelled");
            throw;
        }
        finally
        {
            _isIndexing = false;
            _indexingCts = null;
        }
    }

    /// <inheritdoc />
    public void UpdateFile(string filePath, FileIndexAction action)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);

        switch (action)
        {
            case FileIndexAction.Created:
            case FileIndexAction.Modified:
                if (TryCreateEntry(normalizedPath, out var entry))
                {
                    _index[normalizedPath] = entry!;
                    _logger.LogDebug("File {Action}: {Path}", action, normalizedPath);

                    IndexChanged?.Invoke(this, new FileIndexChangedEventArgs
                    {
                        ChangeType = action == FileIndexAction.Created
                            ? FileIndexChangeType.FileAdded
                            : FileIndexChangeType.FileUpdated,
                        FilePath = normalizedPath,
                        TotalFileCount = _index.Count
                    });
                }
                break;

            case FileIndexAction.Deleted:
                if (_index.TryRemove(normalizedPath, out _))
                {
                    _logger.LogDebug("File deleted from index: {Path}", normalizedPath);

                    // Remove from recent files
                    lock (_recentLock)
                    {
                        if (_recentFilesSet.Remove(normalizedPath))
                        {
                            _recentFiles.Remove(normalizedPath);
                        }
                    }

                    IndexChanged?.Invoke(this, new FileIndexChangedEventArgs
                    {
                        ChangeType = FileIndexChangeType.FileRemoved,
                        FilePath = normalizedPath,
                        TotalFileCount = _index.Count
                    });
                }
                break;
        }
    }

    /// <inheritdoc />
    public void UpdateFileRenamed(string oldPath, string newPath)
    {
        if (string.IsNullOrWhiteSpace(oldPath) || string.IsNullOrWhiteSpace(newPath))
            return;

        var normalizedOldPath = Path.GetFullPath(oldPath);
        var normalizedNewPath = Path.GetFullPath(newPath);

        // Remove old entry
        _index.TryRemove(normalizedOldPath, out _);

        // Update recent files
        lock (_recentLock)
        {
            if (_recentFilesSet.Remove(normalizedOldPath))
            {
                var node = _recentFiles.Find(normalizedOldPath);
                if (node != null)
                {
                    _recentFiles.Remove(node);
                }
            }
        }

        // Add new entry if it passes filters
        if (TryCreateEntry(normalizedNewPath, out var entry))
        {
            _index[normalizedNewPath] = entry!;
            _logger.LogDebug("File renamed in index: {OldPath} -> {NewPath}", normalizedOldPath, normalizedNewPath);

            IndexChanged?.Invoke(this, new FileIndexChangedEventArgs
            {
                ChangeType = FileIndexChangeType.FileAdded,
                FilePath = normalizedNewPath,
                TotalFileCount = _index.Count
            });
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _index.Clear();
        _workspaceRoot = null;

        lock (_recentLock)
        {
            _recentFiles.Clear();
            _recentFilesSet.Clear();
        }

        _logger.LogDebug("File index cleared");

        IndexChanged?.Invoke(this, new FileIndexChangedEventArgs
        {
            ChangeType = FileIndexChangeType.IndexCleared,
            TotalFileCount = 0
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<FileIndexEntry> Search(string query, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<FileIndexEntry>();

        var results = new List<(FileIndexEntry Entry, int Score)>();

        foreach (var entry in _index.Values)
        {
            // Score against filename (primary) and path (secondary)
            var nameScore = Fuzz.PartialRatio(query, entry.FileName);
            var pathScore = Fuzz.PartialRatio(query, entry.RelativePath);
            var score = Math.Max(nameScore, pathScore);

            if (score >= MinFuzzyScore)
            {
                results.Add((entry, score));
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Entry.FileName)
            .Take(maxResults)
            .Select(r => r.Entry)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<FileIndexEntry> GetRecentFiles(int maxResults = 20)
    {
        var results = new List<FileIndexEntry>();

        lock (_recentLock)
        {
            foreach (var path in _recentFiles.Take(maxResults))
            {
                if (_index.TryGetValue(path, out var entry))
                {
                    results.Add(entry);
                }
            }
        }

        return results;
    }

    /// <inheritdoc />
    public void RecordFileAccess(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);

        lock (_recentLock)
        {
            // Remove existing entry if present
            if (_recentFilesSet.Contains(normalizedPath))
            {
                _recentFiles.Remove(normalizedPath);
            }
            else
            {
                _recentFilesSet.Add(normalizedPath);
            }

            // Add to front
            _recentFiles.AddFirst(normalizedPath);

            // Trim to max size
            while (_recentFiles.Count > _settings.MaxRecentFiles)
            {
                var last = _recentFiles.Last!.Value;
                _recentFiles.RemoveLast();
                _recentFilesSet.Remove(last);
            }
        }

        _logger.LogTrace("Recorded file access: {Path}", normalizedPath);
    }

    /// <inheritdoc />
    public FileIndexEntry? GetFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var normalizedPath = Path.GetFullPath(filePath);
        return _index.TryGetValue(normalizedPath, out var entry) ? entry : null;
    }

    /// <inheritdoc />
    public bool ContainsFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var normalizedPath = Path.GetFullPath(filePath);
        return _index.ContainsKey(normalizedPath);
    }

    /// <inheritdoc />
    public IReadOnlyList<FileIndexEntry> GetAllFiles()
    {
        return _index.Values.ToList();
    }

    /// <summary>
    /// Attempts to create a FileIndexEntry for the given path.
    /// </summary>
    /// <returns>True if the file should be indexed.</returns>
    private bool TryCreateEntry(string fullPath, out FileIndexEntry? entry)
    {
        entry = null;

        try
        {
            // Get relative path
            var relativePath = GetRelativePath(fullPath);
            if (relativePath == null)
                return false;

            // Check ignore patterns
            if (_patternMatcher.IsIgnored(relativePath))
                return false;

            // Check if hidden file
            var fileName = Path.GetFileName(fullPath);
            if (!_settings.IncludeHiddenFiles && fileName.StartsWith('.'))
                return false;

            // Get file info
            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
                return false;

            // Check file size
            if (fileInfo.Length > _settings.MaxFileSizeBytes)
                return false;

            // Check binary extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (_settings.BinaryExtensions.Contains(extension))
                return false;

            entry = new FileIndexEntry(
                fileName,
                fullPath,
                relativePath,
                fileInfo.LastWriteTimeUtc,
                fileInfo.Length
            );

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            _logger.LogTrace("Skipping inaccessible file: {Path} - {Error}", fullPath, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Gets the path relative to the workspace root.
    /// </summary>
    private string? GetRelativePath(string? fullPath)
    {
        if (fullPath == null || _workspaceRoot == null)
            return null;

        if (fullPath.StartsWith(_workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            var relative = fullPath.Substring(_workspaceRoot.Length).TrimStart(Path.DirectorySeparatorChar);
            return relative;
        }

        return fullPath;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _indexingCts?.Cancel();
        _indexingCts?.Dispose();
    }
}
