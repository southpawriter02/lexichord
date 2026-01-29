using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Thread-safe implementation of the settings page registry.
/// </summary>
/// <remarks>
/// LOGIC: Manages registration and retrieval of settings pages contributed by modules.
/// Uses lock-based synchronization to ensure thread safety during concurrent access.
///
/// Registration Flow:
/// 1. Validate page (non-null, valid CategoryId, valid DisplayName)
/// 2. Check for duplicates
/// 3. Add to internal list
/// 4. Sort pages
/// 5. Raise PageRegistered event
///
/// Query Flow:
/// - Returns snapshots of the internal list to prevent external modification
/// - Filters by license tier when requested
///
/// Version: v0.1.6a
/// </remarks>
public sealed class SettingsPageRegistry : ISettingsPageRegistry
{
    private readonly List<ISettingsPage> _pages = [];
    private readonly object _lock = new();
    private readonly ILogger<SettingsPageRegistry> _logger;

    /// <inheritdoc />
    public event EventHandler<SettingsPageEventArgs>? PageRegistered;

    /// <inheritdoc />
    public event EventHandler<SettingsPageEventArgs>? PageUnregistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public SettingsPageRegistry(ILogger<SettingsPageRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void RegisterPage(ISettingsPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (string.IsNullOrWhiteSpace(page.CategoryId))
        {
            throw new ArgumentException(
                "CategoryId cannot be null or empty.",
                nameof(page));
        }

        if (string.IsNullOrWhiteSpace(page.DisplayName))
        {
            throw new ArgumentException(
                "DisplayName cannot be null or empty.",
                nameof(page));
        }

        lock (_lock)
        {
            // LOGIC: Check for duplicate registration
            if (_pages.Any(p => p.CategoryId.Equals(page.CategoryId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(
                    $"A settings page with CategoryId '{page.CategoryId}' is already registered.",
                    nameof(page));
            }

            _pages.Add(page);

            // LOGIC: Sort pages by SortOrder, then by DisplayName for consistent ordering
            _pages.Sort((a, b) =>
            {
                var orderCompare = a.SortOrder.CompareTo(b.SortOrder);
                return orderCompare != 0
                    ? orderCompare
                    : string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });
        }

        _logger.LogDebug(
            "Registered settings page: {CategoryId} ({DisplayName})",
            page.CategoryId,
            page.DisplayName);

        PageRegistered?.Invoke(this, new SettingsPageEventArgs(page));
    }

    /// <inheritdoc />
    public bool UnregisterPage(string categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return false;
        }

        ISettingsPage? removedPage = null;

        lock (_lock)
        {
            var page = _pages.FirstOrDefault(
                p => p.CategoryId.Equals(categoryId, StringComparison.OrdinalIgnoreCase));

            if (page is null)
            {
                return false;
            }

            _pages.Remove(page);
            removedPage = page;
        }

        _logger.LogDebug(
            "Unregistered settings page: {CategoryId}",
            categoryId);

        PageUnregistered?.Invoke(this, new SettingsPageEventArgs(removedPage));
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<ISettingsPage> GetPages()
    {
        lock (_lock)
        {
            return _pages.ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ISettingsPage> GetPages(LicenseTier tier)
    {
        lock (_lock)
        {
            return _pages
                .Where(p => p.RequiredTier <= tier)
                .ToList();
        }
    }

    /// <inheritdoc />
    public ISettingsPage? GetPage(string categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return null;
        }

        lock (_lock)
        {
            return _pages.FirstOrDefault(
                p => p.CategoryId.Equals(categoryId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ISettingsPage> GetChildPages(string? parentCategoryId)
    {
        lock (_lock)
        {
            return _pages
                .Where(p => string.Equals(p.ParentCategoryId, parentCategoryId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ISettingsPage> SearchPages(string query, LicenseTier tier)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetPages(tier);
        }

        lock (_lock)
        {
            return _pages
                .Where(p => p.RequiredTier <= tier && MatchesSearch(p, query))
                .ToList();
        }
    }

    /// <summary>
    /// Checks if a page matches the search query.
    /// </summary>
    private static bool MatchesSearch(ISettingsPage page, string query)
    {
        // LOGIC: Search across DisplayName, CategoryId, and SearchKeywords
        if (page.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (page.CategoryId.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var keyword in page.SearchKeywords)
        {
            if (keyword.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
