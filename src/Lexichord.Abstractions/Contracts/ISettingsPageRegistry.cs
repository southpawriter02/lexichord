namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Registry for managing settings pages contributed by modules.
/// </summary>
/// <remarks>
/// LOGIC: Provides a centralized registry for all settings pages in the application.
/// Modules register their pages during initialization, and the Settings window
/// queries this registry to build its navigation tree.
///
/// Thread Safety:
/// - All methods must be thread-safe
/// - Registration typically happens during module initialization (single-threaded)
/// - Queries may happen from UI thread
///
/// Singleton Lifetime:
/// - Registered as singleton in DI container
/// - Lives for the entire application lifetime
///
/// Version: v0.1.6a
/// </remarks>
/// <example>
/// <code>
/// // In module initialization:
/// public class MyModule : IModule
/// {
///     public Task InitializeAsync(IServiceProvider services)
///     {
///         var registry = services.GetRequiredService&lt;ISettingsPageRegistry&gt;();
///         registry.RegisterPage(new MySettingsPage(services));
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface ISettingsPageRegistry
{
    /// <summary>
    /// Registers a settings page with the registry.
    /// </summary>
    /// <param name="page">The settings page to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when page is null.</exception>
    /// <exception cref="ArgumentException">Thrown when CategoryId is null/empty or already registered.</exception>
    /// <remarks>
    /// LOGIC: Pages are validated on registration:
    /// - CategoryId must be non-null and non-empty
    /// - CategoryId must be unique
    /// - DisplayName must be non-null and non-empty
    ///
    /// After registration, PageRegistered event is raised.
    /// </remarks>
    void RegisterPage(ISettingsPage page);

    /// <summary>
    /// Unregisters a settings page from the registry.
    /// </summary>
    /// <param name="categoryId">The unique category ID of the page to remove.</param>
    /// <returns>True if the page was found and removed, false otherwise.</returns>
    /// <remarks>
    /// LOGIC: After successful removal, PageUnregistered event is raised.
    /// Returns false if no page with the given ID was registered.
    /// </remarks>
    bool UnregisterPage(string categoryId);

    /// <summary>
    /// Gets all registered settings pages.
    /// </summary>
    /// <returns>A read-only list of all registered pages, sorted by SortOrder then DisplayName.</returns>
    /// <remarks>
    /// LOGIC: Returns a snapshot of all registered pages.
    /// Does not filter by license tier.
    /// </remarks>
    IReadOnlyList<ISettingsPage> GetPages();

    /// <summary>
    /// Gets all registered settings pages filtered by license tier.
    /// </summary>
    /// <param name="tier">The maximum tier to include.</param>
    /// <returns>A read-only list of pages where RequiredTier &lt;= tier.</returns>
    /// <remarks>
    /// LOGIC: Used by the Settings window to show only pages
    /// the user is licensed to access.
    /// </remarks>
    IReadOnlyList<ISettingsPage> GetPages(LicenseTier tier);

    /// <summary>
    /// Gets a specific settings page by its category ID.
    /// </summary>
    /// <param name="categoryId">The unique category ID.</param>
    /// <returns>The page if found, null otherwise.</returns>
    ISettingsPage? GetPage(string categoryId);

    /// <summary>
    /// Gets all child pages of a given parent category.
    /// </summary>
    /// <param name="parentCategoryId">The parent category ID, or null for root pages.</param>
    /// <returns>A read-only list of child pages, sorted by SortOrder then DisplayName.</returns>
    /// <remarks>
    /// LOGIC: Used to build the hierarchical navigation tree.
    /// Pass null to get all root-level pages.
    /// </remarks>
    IReadOnlyList<ISettingsPage> GetChildPages(string? parentCategoryId);

    /// <summary>
    /// Searches for pages matching a query string.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="tier">The maximum license tier to include.</param>
    /// <returns>Pages matching the query, filtered by tier.</returns>
    /// <remarks>
    /// LOGIC: Searches across:
    /// - DisplayName (contains, case-insensitive)
    /// - CategoryId (contains, case-insensitive)
    /// - SearchKeywords (any keyword contains query, case-insensitive)
    /// </remarks>
    IReadOnlyList<ISettingsPage> SearchPages(string query, LicenseTier tier);

    /// <summary>
    /// Raised when a settings page is registered.
    /// </summary>
    event EventHandler<SettingsPageEventArgs>? PageRegistered;

    /// <summary>
    /// Raised when a settings page is unregistered.
    /// </summary>
    event EventHandler<SettingsPageEventArgs>? PageUnregistered;
}
