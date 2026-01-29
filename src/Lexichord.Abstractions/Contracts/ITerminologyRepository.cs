using Lexichord.Abstractions.Entities;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Repository interface for terminology (style term) operations.
/// </summary>
/// <remarks>
/// LOGIC: Extends IGenericRepository with terminology-specific methods
/// that leverage caching for high-performance document analysis.
///
/// Caching strategy:
/// - GetAllActiveTermsAsync: Cached HashSet for O(1) pattern lookups
/// - Other queries: Direct database access (less frequent)
/// - Write operations: Automatically invalidate cache
///
/// Design decisions:
/// - Returns HashSet for active terms to enable fast Contains() checks
/// - SearchAsync uses trigram matching for fuzzy search
/// - Cache invalidation is automatic on any write operation
/// </remarks>
public interface ITerminologyRepository : IGenericRepository<StyleTerm, Guid>
{
    #region Cached Queries

    /// <summary>
    /// Gets all active terms as a cached HashSet.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HashSet of active style terms for O(1) lookups.</returns>
    /// <remarks>
    /// LOGIC: This is the primary method for document analysis.
    /// Results are cached with sliding expiration for sub-millisecond response.
    /// Cache is automatically invalidated on write operations.
    /// </remarks>
    Task<HashSet<StyleTerm>> GetAllActiveTermsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Filtered Queries

    /// <summary>
    /// Gets all terms in a specific category.
    /// </summary>
    /// <param name="category">Category to filter by (case-sensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Terms matching the category.</returns>
    Task<IEnumerable<StyleTerm>> GetByCategoryAsync(
        string category, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches terms using fuzzy matching (trigram similarity).
    /// </summary>
    /// <param name="searchTerm">Search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Terms matching the search query.</returns>
    /// <remarks>
    /// LOGIC: Uses PostgreSQL pg_trgm extension for fuzzy matching.
    /// Results are ordered by similarity score.
    /// </remarks>
    Task<IEnumerable<StyleTerm>> SearchAsync(
        string searchTerm, 
        CancellationToken cancellationToken = default);

    #endregion

    #region Cache Management

    /// <summary>
    /// Explicitly invalidates the active terms cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the invalidation operation.</returns>
    /// <remarks>
    /// LOGIC: Cache is automatically invalidated on write operations.
    /// This method is for explicit cache refresh scenarios (e.g., bulk import).
    /// </remarks>
    Task InvalidateCacheAsync(CancellationToken cancellationToken = default);

    #endregion
}
