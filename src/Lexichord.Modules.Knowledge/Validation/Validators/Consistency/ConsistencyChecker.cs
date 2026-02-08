// =============================================================================
// File: ConsistencyChecker.cs
// Project: Lexichord.Modules.Knowledge
// Description: IValidator implementation for claim consistency checking.
// =============================================================================
// LOGIC: Orchestrates consistency checking by:
//   1. Extracting claims from ValidationContext metadata
//   2. Querying IClaimRepository for potential conflicts per claim
//   3. Detecting conflicts via IConflictDetector
//   4. Suggesting resolutions via IContradictionResolver
//   5. Checking internal consistency within the claim batch
//   6. Building ConsistencyFinding records
//
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// Dependencies: IValidator (v0.6.5e), IConsistencyChecker (v0.6.5h),
//               IClaimRepository (v0.5.6h), IConflictDetector (v0.6.5h),
//               IContradictionResolver (v0.6.5h), IClaimDiffService (v0.5.6i),
//               Claim (v0.5.6e), ValidationFinding (v0.6.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Consistency;

/// <summary>
/// Consistency checker that integrates with the v0.6.5e validation pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ConsistencyChecker"/> implements <see cref="IConsistencyChecker"/>
/// (which extends <see cref="IValidator"/>) to detect contradictions between
/// new claims and existing knowledge in the Knowledge Graph. It coordinates
/// <see cref="IClaimRepository"/> for lookup, <see cref="IConflictDetector"/>
/// for detection, and <see cref="IContradictionResolver"/> for resolution
/// suggestions.
/// </para>
/// <para>
/// <b>Pipeline Integration:</b> The <see cref="ValidateAsync"/> method extracts
/// claims from <c>context.Metadata["claims"]</c> and delegates to
/// <see cref="CheckClaimsConsistencyAsync"/>. If no claims key is present,
/// returns empty.
/// </para>
/// <para>
/// <b>Internal Consistency:</b> In addition to checking against the repository,
/// the checker also detects conflicts within the batch of new claims themselves
/// via <see cref="CheckInternalConsistency"/>.
/// </para>
/// <para>
/// <b>License Requirement:</b> <see cref="LicenseTier.Teams"/> or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public class ConsistencyChecker : IConsistencyChecker
{
    private readonly IClaimRepository _claimRepository;
    private readonly IConflictDetector _conflictDetector;
    private readonly IContradictionResolver _resolver;
    private readonly ILogger<ConsistencyChecker> _logger;

    /// <inheritdoc />
    public string Id => "consistency-checker";

    /// <inheritdoc />
    public string DisplayName => "Consistency Checker";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Consistency checking runs in all validation modes because
    /// contradictions can be introduced during any editing workflow.
    /// </remarks>
    public ValidationMode SupportedModes => ValidationMode.All;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Full consistency checking requires Teams tier for access to
    /// the claim repository and diff service.
    /// </remarks>
    public LicenseTier RequiredLicenseTier => LicenseTier.Teams;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsistencyChecker"/> class.
    /// </summary>
    /// <param name="claimRepository">The claim repository for fetching existing claims.</param>
    /// <param name="conflictDetector">The conflict detector for comparing claims.</param>
    /// <param name="resolver">The contradiction resolver for suggesting resolutions.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="claimRepository"/>, <paramref name="conflictDetector"/>,
    /// <paramref name="resolver"/>, or <paramref name="logger"/> is null.
    /// </exception>
    public ConsistencyChecker(
        IClaimRepository claimRepository,
        IConflictDetector conflictDetector,
        IContradictionResolver resolver,
        ILogger<ConsistencyChecker> logger)
    {
        _claimRepository = claimRepository ?? throw new ArgumentNullException(nameof(claimRepository));
        _conflictDetector = conflictDetector ?? throw new ArgumentNullException(nameof(conflictDetector));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Extracts claims from <c>context.Metadata["claims"]</c> and delegates
    /// to <see cref="CheckClaimsConsistencyAsync"/>. If no claims key is present
    /// or the value is not <c>IReadOnlyList&lt;Claim&gt;</c>, returns empty.
    /// </remarks>
    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: Extract claims from metadata (same pattern as AxiomValidatorService).
        if (!context.Metadata.TryGetValue("claims", out var claimsObj) ||
            claimsObj is not IReadOnlyList<Claim> claims ||
            claims.Count == 0)
        {
            _logger.LogDebug("No claims to check for consistency");
            return [];
        }

        return await CheckClaimsConsistencyAsync(claims, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ValidationFinding>> CheckClaimConsistencyAsync(
        Claim claim,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();

        try
        {
            // LOGIC: Fetch potentially conflicting claims from the repository.
            var potentialConflicts = await GetPotentialConflictsAsync(claim, ct);

            _logger.LogDebug(
                "Found {Count} potential conflicts for claim {ClaimId}",
                potentialConflicts.Count,
                claim.Id);

            foreach (var existingClaim in potentialConflicts)
            {
                var conflict = _conflictDetector.DetectConflict(claim, existingClaim);

                if (conflict.HasConflict)
                {
                    var resolution = _resolver.SuggestResolution(
                        claim, existingClaim, conflict.ConflictType);

                    findings.Add(CreateConsistencyFinding(
                        claim, existingClaim, conflict, resolution));
                }
            }
        }
        catch (Exception ex)
        {
            // LOGIC: Repository failures are non-fatal — log and skip.
            _logger.LogWarning(ex,
                "Failed to check consistency for claim {ClaimId}. " +
                "Skipping consistency check for this claim.",
                claim.Id);
        }

        return findings;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ValidationFinding>> CheckClaimsConsistencyAsync(
        IReadOnlyList<Claim> claims,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();

        // LOGIC: Check each claim against existing knowledge.
        foreach (var claim in claims)
        {
            ct.ThrowIfCancellationRequested();
            var claimFindings = await CheckClaimConsistencyAsync(claim, ct);
            findings.AddRange(claimFindings);
        }

        // LOGIC: Also check for internal consistency (conflicts within the new claims).
        var internalConflicts = CheckInternalConsistency(claims);
        findings.AddRange(internalConflicts);

        return findings;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Searches for claims about the same entity using two queries:
    /// <list type="number">
    ///   <item>Claims with the same subject entity (via <see cref="IClaimRepository.GetByEntityAsync"/>).</item>
    ///   <item>Claims where the new claim's object entity is a subject.</item>
    /// </list>
    /// Results are deduplicated by claim ID and the current claim is excluded.
    /// </remarks>
    public async Task<IReadOnlyList<Claim>> GetPotentialConflictsAsync(
        Claim claim,
        CancellationToken ct = default)
    {
        var results = new List<Claim>();

        // LOGIC: Get claims involving the same subject entity.
        if (claim.Subject.EntityId.HasValue)
        {
            var subjectClaims = await _claimRepository.GetByEntityAsync(
                claim.Subject.EntityId.Value, ct);
            results.AddRange(subjectClaims);
        }

        // LOGIC: Also search for claims where the object entity is a subject.
        if (claim.Object.Entity?.EntityId != null)
        {
            var relatedClaims = await _claimRepository.GetByEntityAsync(
                claim.Object.Entity.EntityId.Value, ct);
            results.AddRange(relatedClaims);
        }

        // LOGIC: Deduplicate and exclude the current claim.
        return results
            .Where(c => c.Id != claim.Id)
            .DistinctBy(c => c.Id)
            .ToList();
    }

    /// <summary>
    /// Checks for internal consistency within a batch of new claims.
    /// </summary>
    /// <param name="claims">The claims to check against each other.</param>
    /// <returns>Findings for any internal conflicts detected.</returns>
    /// <remarks>
    /// LOGIC: Groups claims by subject, then by predicate. For each predicate
    /// group with 2+ claims, runs pairwise conflict detection. This catches
    /// contradictions within a single document before they enter the repository.
    /// </remarks>
    private IReadOnlyList<ValidationFinding> CheckInternalConsistency(
        IReadOnlyList<Claim> claims)
    {
        var findings = new List<ValidationFinding>();

        // LOGIC: Group claims by subject entity ID.
        var claimsBySubject = claims
            .Where(c => c.Subject.EntityId.HasValue)
            .GroupBy(c => c.Subject.EntityId!.Value);

        foreach (var group in claimsBySubject)
        {
            var subjectClaims = group.ToList();

            // LOGIC: Check for conflicting predicates within same subject.
            var predicateGroups = subjectClaims.GroupBy(c => c.Predicate);

            foreach (var predGroup in predicateGroups)
            {
                var predClaims = predGroup.ToList();
                if (predClaims.Count <= 1) continue;

                // LOGIC: Pairwise comparison for conflicting objects.
                for (int i = 0; i < predClaims.Count; i++)
                {
                    for (int j = i + 1; j < predClaims.Count; j++)
                    {
                        var conflict = _conflictDetector.DetectConflict(
                            predClaims[i], predClaims[j]);

                        if (conflict.HasConflict)
                        {
                            findings.Add(new ConsistencyFinding(
                                ValidatorId: Id,
                                Severity: ValidationSeverity.Error,
                                Code: ConsistencyFindingCodes.ConsistencyConflict,
                                Message: $"Internal conflict: document contains contradictory claims " +
                                         $"about '{predClaims[i].Subject.SurfaceForm}' {predClaims[i].Predicate}")
                            {
                                ExistingClaim = predClaims[j],
                                ExistingClaimDocumentId = predClaims[j].DocumentId,
                                ConflictType = conflict.ConflictType,
                                ConflictConfidence = conflict.Confidence
                            });
                        }
                    }
                }
            }
        }

        return findings;
    }

    /// <summary>
    /// Creates a <see cref="ConsistencyFinding"/> from a detected conflict.
    /// </summary>
    /// <param name="newClaim">The new claim that triggered the conflict.</param>
    /// <param name="existingClaim">The existing claim from the knowledge base.</param>
    /// <param name="conflict">The conflict detection result.</param>
    /// <param name="resolution">The suggested resolution.</param>
    /// <returns>A fully populated consistency finding.</returns>
    /// <remarks>
    /// LOGIC: Maps conflict confidence to severity:
    /// &gt;0.8 → Error, ≤0.8 → Warning. The finding code is derived from
    /// the conflict type via <see cref="GetFindingCode"/>.
    /// </remarks>
    private ConsistencyFinding CreateConsistencyFinding(
        Claim newClaim,
        Claim existingClaim,
        ConflictResult conflict,
        ConflictResolution resolution)
    {
        return new ConsistencyFinding(
            ValidatorId: Id,
            Severity: conflict.Confidence > 0.8f
                ? ValidationSeverity.Error
                : ValidationSeverity.Warning,
            Code: GetFindingCode(conflict.ConflictType),
            Message: conflict.Description ?? "Conflict with existing claim",
            SuggestedFix: resolution.Description)
        {
            ExistingClaim = existingClaim,
            ExistingClaimDocumentId = existingClaim.DocumentId,
            ConflictType = conflict.ConflictType,
            ConflictConfidence = conflict.Confidence,
            Resolution = resolution
        };
    }

    /// <summary>
    /// Maps a <see cref="ConflictType"/> to the appropriate finding code.
    /// </summary>
    /// <param name="type">The conflict type to map.</param>
    /// <returns>The corresponding <see cref="ConsistencyFindingCodes"/> constant.</returns>
    /// <remarks>
    /// LOGIC: Maps each conflict type to its specific finding code for
    /// programmatic filtering. Unmapped types fall through to the generic
    /// CONSISTENCY_CONFLICT code.
    /// </remarks>
    private static string GetFindingCode(ConflictType type)
    {
        return type switch
        {
            ConflictType.ValueContradiction => ConsistencyFindingCodes.ValueContradiction,
            ConflictType.PropertyConflict => ConsistencyFindingCodes.PropertyConflict,
            ConflictType.RelationshipContradiction => ConsistencyFindingCodes.RelationshipConflict,
            ConflictType.TemporalConflict => ConsistencyFindingCodes.TemporalConflict,
            ConflictType.SemanticContradiction => ConsistencyFindingCodes.SemanticConflict,
            _ => ConsistencyFindingCodes.ConsistencyConflict
        };
    }
}
