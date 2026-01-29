using Lexichord.Abstractions.Entities;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Service for seeding the Terminology Database with default style terms.
/// </summary>
/// <remarks>
/// LOGIC: The seeder provides out-of-the-box style guidance by populating
/// the database with Microsoft Manual of Style terms on first launch.
///
/// Design decisions:
/// - Seed data is embedded in the assembly (compiled, not external files)
/// - Seeding is idempotent (skips if database is not empty)
/// - ReseedAsync provides a development reset capability
///
/// Security:
/// - Embedded data prevents tampering via external files
/// - ReseedAsync(clearExisting: true) requires explicit confirmation
/// </remarks>
public interface ITerminologySeeder
{
    /// <summary>
    /// Seeds the database with default terms if it is empty.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating what was seeded.</returns>
    /// <remarks>
    /// LOGIC: This is the primary seeding method used on application startup.
    /// It is idempotent - calling multiple times has no effect if data exists.
    /// </remarks>
    Task<SeedResult> SeedIfEmptyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reseeds the database, optionally clearing existing terms first.
    /// </summary>
    /// <param name="clearExisting">If true, deletes all existing terms before seeding.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating what was seeded.</returns>
    /// <remarks>
    /// WARNING: When clearExisting is true, ALL user-customized terms will be deleted!
    /// This is intended for development reset scenarios only.
    /// </remarks>
    Task<SeedResult> ReseedAsync(bool clearExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default seed terms without modifying the database.
    /// </summary>
    /// <returns>Read-only list of default style terms.</returns>
    /// <remarks>
    /// LOGIC: Useful for testing, validation, and displaying available defaults.
    /// </remarks>
    IReadOnlyList<StyleTerm> GetDefaultTerms();
}

/// <summary>
/// Result of a seeding operation.
/// </summary>
/// <param name="TermsSeeded">Number of terms inserted.</param>
/// <param name="WasEmpty">Whether the database was empty before seeding.</param>
/// <param name="Duration">Time taken for the seeding operation.</param>
public sealed record SeedResult(
    int TermsSeeded,
    bool WasEmpty,
    TimeSpan Duration)
{
    /// <summary>
    /// Creates a result for when seeding was skipped (database not empty).
    /// </summary>
    public static SeedResult Skipped(TimeSpan duration) => new(0, WasEmpty: false, duration);
}
