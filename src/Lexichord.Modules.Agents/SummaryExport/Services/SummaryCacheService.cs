// -----------------------------------------------------------------------
// <copyright file="SummaryCacheService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implementation of summary caching with hybrid storage (v0.7.6c).
//   Uses IMemoryCache for fast access + JSON file storage for persistence.
//
//   Cache Storage Locations:
//     - Memory: IMemoryCache with sliding expiration
//     - File: .lexichord/cache/summaries/{hash}.json (workspace-level)
//
//   Cache Invalidation:
//     - Content hash changes (document modified)
//     - Cache entry expires (default: 7 days)
//     - User manually clears cache
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Agents.SummaryExport;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.SummaryExport.Services;

/// <summary>
/// Interface for summary caching operations.
/// </summary>
/// <remarks>
/// <b>LOGIC:</b> Internal interface for caching operations, separate from
/// ISummaryExporter to allow independent testing and potential future
/// cache implementations.
/// </remarks>
public interface ISummaryCacheService
{
    /// <summary>
    /// Gets a cached summary for a document if valid.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached summary if valid, otherwise <c>null</c>.</returns>
    Task<CachedSummary?> GetAsync(string documentPath, CancellationToken ct = default);

    /// <summary>
    /// Stores a summary in the cache.
    /// </summary>
    /// <param name="documentPath">Path to the source document.</param>
    /// <param name="summary">The summary to cache.</param>
    /// <param name="metadata">Optional metadata to cache.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetAsync(string documentPath, SummarizationResult summary, DocumentMetadata? metadata, CancellationToken ct = default);

    /// <summary>
    /// Removes a cached summary.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ClearAsync(string documentPath, CancellationToken ct = default);

    /// <summary>
    /// Computes the content hash for a document.
    /// </summary>
    /// <param name="content">The document content.</param>
    /// <returns>A SHA256 hash prefixed with "sha256:".</returns>
    string ComputeContentHash(string content);
}

/// <summary>
/// Implementation of summary caching with hybrid memory + file storage.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This service provides a two-tier caching strategy:
/// <list type="number">
/// <item><description>Memory cache (IMemoryCache): Fast access with sliding expiration</description></item>
/// <item><description>File cache (JSON): Persistence across application restarts</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Cache Key:</b>
/// Uses a hash of the normalized document path as the cache key for both
/// memory and file storage. This ensures consistent key generation.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// IMemoryCache is thread-safe. File operations use async methods with
/// proper locking considerations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
public sealed class SummaryCacheService : ISummaryCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IFileService _fileService;
    private readonly ILogger<SummaryCacheService> _logger;

    private const string CachePrefix = "summary_cache_";
    private const string CacheDirectory = ".lexichord/cache/summaries";
    private static readonly TimeSpan DefaultMemoryCacheExpiration = TimeSpan.FromHours(4);
    private static readonly TimeSpan DefaultFileCacheExpiration = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryCacheService"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache instance.</param>
    /// <param name="fileService">The file service for persistence.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public SummaryCacheService(
        IMemoryCache memoryCache,
        IFileService fileService,
        ILogger<SummaryCacheService> logger)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(fileService);
        ArgumentNullException.ThrowIfNull(logger);

        _memoryCache = memoryCache;
        _fileService = fileService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CachedSummary?> GetAsync(string documentPath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);

        var cacheKey = GetCacheKey(documentPath);
        _logger.LogDebug("Looking up cached summary for {DocumentPath} (key: {CacheKey})", documentPath, cacheKey);

        // LOGIC: Try memory cache first (fast path)
        if (_memoryCache.TryGetValue<CachedSummary>(cacheKey, out var cached) && cached != null)
        {
            if (cached.IsExpired)
            {
                _logger.LogDebug("Memory cache hit but entry expired for {DocumentPath}", documentPath);
                _memoryCache.Remove(cacheKey);
                return null;
            }

            // Validate content hash
            if (!await ValidateContentHashAsync(documentPath, cached.ContentHash, ct))
            {
                _logger.LogDebug("Memory cache hit but content hash changed for {DocumentPath}", documentPath);
                _memoryCache.Remove(cacheKey);
                return null;
            }

            _logger.LogDebug("Memory cache hit for {DocumentPath}, age={Age}", documentPath, cached.Age);
            return cached;
        }

        // LOGIC: Try file cache (slower but persistent)
        var fileCached = await LoadFromFileAsync(documentPath, ct);
        if (fileCached != null)
        {
            if (fileCached.IsExpired)
            {
                _logger.LogDebug("File cache hit but entry expired for {DocumentPath}", documentPath);
                await DeleteCacheFileAsync(documentPath);
                return null;
            }

            // Validate content hash
            if (!await ValidateContentHashAsync(documentPath, fileCached.ContentHash, ct))
            {
                _logger.LogDebug("File cache hit but content hash changed for {DocumentPath}", documentPath);
                await DeleteCacheFileAsync(documentPath);
                return null;
            }

            // Promote to memory cache
            _memoryCache.Set(cacheKey, fileCached, new MemoryCacheEntryOptions
            {
                SlidingExpiration = DefaultMemoryCacheExpiration
            });

            _logger.LogDebug("File cache hit for {DocumentPath}, promoted to memory cache, age={Age}",
                documentPath, fileCached.Age);
            return fileCached;
        }

        _logger.LogDebug("Cache miss for {DocumentPath}", documentPath);
        return null;
    }

    /// <inheritdoc/>
    public async Task SetAsync(string documentPath, SummarizationResult summary, DocumentMetadata? metadata, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(summary);

        var cacheKey = GetCacheKey(documentPath);

        // Compute content hash
        var content = await LoadDocumentContentAsync(documentPath, ct);
        if (content == null)
        {
            _logger.LogWarning("Cannot cache summary - document not found: {DocumentPath}", documentPath);
            return;
        }

        var contentHash = ComputeContentHash(content);
        var cached = CachedSummary.Create(documentPath, contentHash, summary, metadata, 7);

        // Store in memory cache
        _memoryCache.Set(cacheKey, cached, new MemoryCacheEntryOptions
        {
            SlidingExpiration = DefaultMemoryCacheExpiration
        });

        // Store in file cache
        await SaveToFileAsync(documentPath, cached, ct);

        _logger.LogDebug("Cached summary for {DocumentPath} (key: {CacheKey}, hash: {Hash})",
            documentPath, cacheKey, contentHash[..20] + "...");
    }

    /// <inheritdoc/>
    public async Task ClearAsync(string documentPath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);

        var cacheKey = GetCacheKey(documentPath);

        // Remove from memory cache
        _memoryCache.Remove(cacheKey);

        // Remove from file cache
        await DeleteCacheFileAsync(documentPath);

        _logger.LogDebug("Cleared cache for {DocumentPath}", documentPath);
    }

    /// <inheritdoc/>
    public string ComputeContentHash(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"sha256:{hashString}";
    }

    /// <summary>
    /// Gets the cache key for a document path.
    /// </summary>
    private static string GetCacheKey(string documentPath)
    {
        // LOGIC: Use a hash of the normalized path as the key
        var normalizedPath = documentPath.Replace('\\', '/');
        var pathBytes = Encoding.UTF8.GetBytes(normalizedPath);
        var hashBytes = SHA256.HashData(pathBytes);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"{CachePrefix}{hashString}";
    }

    /// <summary>
    /// Gets the file path for a cached summary.
    /// </summary>
    private string GetCacheFilePath(string documentPath)
    {
        var cacheKey = GetCacheKey(documentPath);
        var workspaceRoot = Path.GetDirectoryName(documentPath) ?? ".";

        // LOGIC: Store in workspace .lexichord/cache/summaries directory
        return Path.Combine(workspaceRoot, CacheDirectory, $"{cacheKey[CachePrefix.Length..]}.json");
    }

    /// <summary>
    /// Validates that the document's current content hash matches the cached hash.
    /// </summary>
    private async Task<bool> ValidateContentHashAsync(string documentPath, string cachedHash, CancellationToken ct)
    {
        var content = await LoadDocumentContentAsync(documentPath, ct);
        if (content == null)
        {
            return false;
        }

        var currentHash = ComputeContentHash(content);
        return currentHash == cachedHash;
    }

    /// <summary>
    /// Loads document content from the file system.
    /// </summary>
    private async Task<string?> LoadDocumentContentAsync(string documentPath, CancellationToken ct)
    {
        try
        {
            var result = await _fileService.LoadAsync(documentPath, null, ct);
            return result.Success ? result.Content : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load document content for hash validation: {DocumentPath}", documentPath);
            return null;
        }
    }

    /// <summary>
    /// Loads a cached summary from file storage.
    /// </summary>
    private async Task<CachedSummary?> LoadFromFileAsync(string documentPath, CancellationToken ct)
    {
        var filePath = GetCacheFilePath(documentPath);

        try
        {
            var result = await _fileService.LoadAsync(filePath, null, ct);
            if (!result.Success || string.IsNullOrEmpty(result.Content))
            {
                return null;
            }

            return JsonSerializer.Deserialize<CachedSummary>(result.Content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cached summary from file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Saves a cached summary to file storage.
    /// </summary>
    private async Task SaveToFileAsync(string documentPath, CachedSummary cached, CancellationToken ct)
    {
        var filePath = GetCacheFilePath(documentPath);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(cached, JsonOptions);
            await _fileService.SaveAsync(filePath, json, null, ct);

            _logger.LogDebug("Saved cached summary to file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save cached summary to file: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Deletes a cached summary file.
    /// </summary>
    private async Task DeleteCacheFileAsync(string documentPath)
    {
        var filePath = GetCacheFilePath(documentPath);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted cached summary file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete cached summary file: {FilePath}", filePath);
        }

        await Task.CompletedTask;
    }
}
