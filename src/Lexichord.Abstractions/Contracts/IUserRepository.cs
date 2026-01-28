using Lexichord.Abstractions.Entities;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Repository for User entity operations.
/// </summary>
/// <remarks>
/// LOGIC: Extends IGenericRepository with User-specific query methods.
/// These methods provide optimized access patterns for common operations.
/// </remarks>
public interface IUserRepository : IGenericRepository<User, Guid>
{
    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <param name="email">The email address (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All active users.</returns>
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email address is already registered.
    /// </summary>
    /// <param name="email">The email address (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email exists.</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches users by display name or email.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="maxResults">Maximum results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching users.</returns>
    Task<IEnumerable<User>> SearchAsync(
        string searchTerm,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user (soft delete).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user was deactivated.</returns>
    Task<bool> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user was reactivated.</returns>
    Task<bool> ReactivateAsync(Guid userId, CancellationToken cancellationToken = default);
}
