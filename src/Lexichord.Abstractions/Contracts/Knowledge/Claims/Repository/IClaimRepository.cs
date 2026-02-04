// =============================================================================
// File: IClaimRepository.cs
// Project: Lexichord.Abstractions
// Description: Repository interface for claim persistence.
// =============================================================================
// LOGIC: Provides CRUD operations, search, and bulk operations for claims
//   with PostgreSQL-backed storage, full-text search, and caching.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// Dependencies: Claim (v0.5.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;

/// <summary>
/// Repository interface for claim persistence.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides persistent storage for extracted claims with
/// full-text search, pagination, and bulk operations.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
/// <item><description>CRUD operations for individual claims</description></item>
/// <item><description>Bulk create and upsert operations</description></item>
/// <item><description>Flexible search with multiple criteria</description></item>
/// <item><description>Full-text search on evidence text</description></item>
/// <item><description>Pagination and sorting support</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IClaimRepository
{
    #region Read Operations

    /// <summary>
    /// Gets a claim by its unique identifier.
    /// </summary>
    /// <param name="id">The claim identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claim if found; otherwise, null.</returns>
    Task<Claim?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple claims by their identifiers.
    /// </summary>
    /// <param name="ids">The claim identifiers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of found claims (may be fewer than requested).</returns>
    Task<IReadOnlyList<Claim>> GetManyAsync(
        IEnumerable<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Searches claims by criteria with pagination.
    /// </summary>
    /// <param name="criteria">Search criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated query result.</returns>
    Task<ClaimQueryResult> SearchAsync(
        ClaimSearchCriteria criteria,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all claims for a specific document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of claims ordered by evidence offset.</returns>
    Task<IReadOnlyList<Claim>> GetByDocumentAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets claims involving an entity (as subject or object).
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of claims involving the entity.</returns>
    Task<IReadOnlyList<Claim>> GetByEntityAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets claims by predicate type.
    /// </summary>
    /// <param name="predicate">The predicate (e.g., ACCEPTS, RETURNS).</param>
    /// <param name="projectId">Optional project filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of claims with the specified predicate.</returns>
    Task<IReadOnlyList<Claim>> GetByPredicateAsync(
        string predicate,
        Guid? projectId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the count of claims matching criteria.
    /// </summary>
    /// <param name="criteria">Optional search criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of matching claims.</returns>
    Task<int> CountAsync(
        ClaimSearchCriteria? criteria = null,
        CancellationToken ct = default);

    /// <summary>
    /// Performs full-text search on claim evidence and entity names.
    /// </summary>
    /// <param name="query">Search query text.</param>
    /// <param name="projectId">Optional project filter.</param>
    /// <param name="limit">Maximum results to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Claims matching the text query, ranked by relevance.</returns>
    Task<IReadOnlyList<Claim>> FullTextSearchAsync(
        string query,
        Guid? projectId = null,
        int limit = 50,
        CancellationToken ct = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates a new claim.
    /// </summary>
    /// <param name="claim">The claim to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created claim.</returns>
    /// <remarks>
    /// Publishes a <see cref="Events.ClaimCreatedEvent"/> after creation.
    /// </remarks>
    Task<Claim> CreateAsync(Claim claim, CancellationToken ct = default);

    /// <summary>
    /// Creates multiple claims in a single transaction.
    /// </summary>
    /// <param name="claims">The claims to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created claims.</returns>
    Task<IReadOnlyList<Claim>> CreateManyAsync(
        IEnumerable<Claim> claims,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing claim.
    /// </summary>
    /// <param name="claim">The claim with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated claim.</returns>
    /// <remarks>
    /// Publishes a <see cref="Events.ClaimUpdatedEvent"/> after update.
    /// </remarks>
    Task<Claim> UpdateAsync(Claim claim, CancellationToken ct = default);

    /// <summary>
    /// Upserts multiple claims (creates new or updates existing).
    /// </summary>
    /// <param name="claims">The claims to upsert.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with counts of created/updated/unchanged claims.</returns>
    Task<BulkUpsertResult> UpsertManyAsync(
        IEnumerable<Claim> claims,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a claim by identifier.
    /// </summary>
    /// <param name="id">The claim identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    /// <remarks>
    /// Publishes a <see cref="Events.ClaimDeletedEvent"/> after deletion.
    /// </remarks>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deletes all claims for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of claims deleted.</returns>
    Task<int> DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken ct = default);

    #endregion
}
