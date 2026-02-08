// =============================================================================
// File: IConsistencyChecker.cs
// Project: Lexichord.Abstractions
// Description: Extended validator interface for claim consistency checking.
// =============================================================================
// LOGIC: Extends IValidator with claim-specific consistency methods so
//   callers can check individual claims or batches for contradictions
//   against existing knowledge in the claim repository.
//
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// Dependencies: IValidator (v0.6.5e), Claim (v0.5.6e),
//               ValidationFinding (v0.6.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Extended validator interface for claim consistency checking.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IConsistencyChecker"/> extends <see cref="IValidator"/> to
/// provide direct claim-level consistency methods. While
/// <see cref="IValidator.ValidateAsync"/> operates on a <see cref="ValidationContext"/>
/// (document-centric), these methods allow callers to check individual
/// <see cref="Claim"/> instances or batches for contradictions against
/// existing knowledge.
/// </para>
/// <para>
/// <b>Pipeline Integration:</b> The <see cref="IValidator.ValidateAsync"/> method
/// extracts claims from the context metadata and delegates to
/// <see cref="CheckClaimsConsistencyAsync"/>. It also checks for internal
/// consistency (conflicts within the new claims themselves).
/// </para>
/// <para>
/// <b>License Requirement:</b> <see cref="LicenseTier.Teams"/> or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public interface IConsistencyChecker : IValidator
{
    /// <summary>
    /// Checks a single claim for conflicts with existing knowledge.
    /// </summary>
    /// <param name="claim">The claim to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="ValidationFinding"/> instances describing
    /// consistency conflicts. Empty if no conflicts are found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Validation steps (in order):
    /// <list type="number">
    ///   <item>Fetch potential conflicts from <see cref="Claims.Repository.IClaimRepository"/>.</item>
    ///   <item>Detect conflicts using <see cref="IConflictDetector"/>.</item>
    ///   <item>Suggest resolutions using <see cref="IContradictionResolver"/>.</item>
    ///   <item>Build <see cref="ConsistencyFinding"/> records.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<ValidationFinding>> CheckClaimConsistencyAsync(
        Claim claim,
        CancellationToken ct = default);

    /// <summary>
    /// Checks multiple claims for conflicts with existing knowledge and
    /// internal consistency.
    /// </summary>
    /// <param name="claims">The claims to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All consistency findings across all claims.</returns>
    Task<IReadOnlyList<ValidationFinding>> CheckClaimsConsistencyAsync(
        IReadOnlyList<Claim> claims,
        CancellationToken ct = default);

    /// <summary>
    /// Gets existing claims that may conflict with a new claim.
    /// </summary>
    /// <param name="claim">The new claim to find potential conflicts for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A read-only list of potentially conflicting claims from the repository.
    /// </returns>
    /// <remarks>
    /// LOGIC: Searches by subject entity and predicate, then also retrieves
    /// claims where the new claim's object entity is a subject.
    /// </remarks>
    Task<IReadOnlyList<Claim>> GetPotentialConflictsAsync(
        Claim claim,
        CancellationToken ct = default);
}
