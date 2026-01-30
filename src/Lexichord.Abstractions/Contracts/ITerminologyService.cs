using Lexichord.Abstractions.Entities;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service interface for terminology CRUD operations.
/// </summary>
/// <remarks>
/// LOGIC: Provides a clean abstraction over repository operations
/// with validation, event publishing, and statistics aggregation.
///
/// Design decisions:
/// - Returns Result&lt;T&gt; for explicit success/failure handling
/// - Publishes LexiconChangedEvent on mutations
/// - Soft-delete via IsActive flag (Delete/Reactivate)
/// - Thread-safe for concurrent access
/// </remarks>
public interface ITerminologyService
{
    #region CRUD Operations

    /// <summary>
    /// Creates a new terminology entry.
    /// </summary>
    /// <param name="command">The creation command with term details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the new term's ID on success.</returns>
    /// <remarks>
    /// LOGIC: Validates pattern, creates entity, publishes LexiconChangedEvent.Created.
    /// Fails if: pattern is invalid, regex is unsafe, or database error.
    /// </remarks>
    Task<Result<Guid>> CreateAsync(CreateTermCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing terminology entry.
    /// </summary>
    /// <param name="command">The update command with modified details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <remarks>
    /// LOGIC: Validates pattern, updates entity, publishes LexiconChangedEvent.Updated.
    /// Fails if: term not found, pattern is invalid, or database error.
    /// </remarks>
    Task<Result<bool>> UpdateAsync(UpdateTermCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a terminology entry (sets IsActive to false).
    /// </summary>
    /// <param name="id">The unique identifier of the term to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <remarks>
    /// LOGIC: Soft-delete preserves data integrity. Publishes LexiconChangedEvent.Deleted.
    /// Fails if: term not found, already inactive, or database error.
    /// </remarks>
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a previously deleted terminology entry.
    /// </summary>
    /// <param name="id">The unique identifier of the term to reactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <remarks>
    /// LOGIC: Restores soft-deleted terms. Publishes LexiconChangedEvent.Reactivated.
    /// Fails if: term not found, already active, or database error.
    /// </remarks>
    Task<Result<bool>> ReactivateAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Query Operations

    /// <summary>
    /// Gets a term by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the term.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The term if found, null otherwise.</returns>
    Task<StyleTerm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all terms (active and inactive).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All terminology entries.</returns>
    Task<IEnumerable<StyleTerm>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active terms.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active terminology entries.</returns>
    Task<IEnumerable<StyleTerm>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets terms by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Terms matching the specified category.</returns>
    Task<IEnumerable<StyleTerm>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets terms by severity.
    /// </summary>
    /// <param name="severity">The severity to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Terms matching the specified severity.</returns>
    Task<IEnumerable<StyleTerm>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches terms using fuzzy matching.
    /// </summary>
    /// <param name="searchTerm">The search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Terms matching the search query.</returns>
    Task<IEnumerable<StyleTerm>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets aggregated statistics about the terminology database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics including counts by category and severity.</returns>
    Task<TermStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion
}
