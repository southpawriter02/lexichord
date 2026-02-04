// =============================================================================
// File: ClaimDiffService.cs
// Project: Lexichord.Modules.Knowledge
// Description: Service for comparing claims between versions.
// =============================================================================
// LOGIC: Compares claim sets to identify added, removed, and modified claims.
//   Uses ID matching with semantic fallback. Generates field-level changes
//   and impact assessments.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: IClaimRepository (v0.5.6h), SemanticMatcher (v0.5.6i)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Claims.Diff;

/// <summary>
/// Service for comparing claims between versions.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IClaimDiffService"/> to provide:
/// </para>
/// <list type="bullet">
///   <item><b>Set comparison:</b> Compare two claim lists.</item>
///   <item><b>Version diffing:</b> Compare document versions.</item>
///   <item><b>Baseline diffing:</b> Compare to snapshots.</item>
///   <item><b>Contradiction detection:</b> Find conflicting claims.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
public class ClaimDiffService : IClaimDiffService
{
    private readonly IClaimRepository _repository;
    private readonly SemanticMatcher _semanticMatcher;
    private readonly ILogger<ClaimDiffService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimDiffService"/> class.
    /// </summary>
    /// <param name="repository">Claim repository for data access.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">If any dependency is null.</exception>
    public ClaimDiffService(
        IClaimRepository repository,
        ILogger<ClaimDiffService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _semanticMatcher = new SemanticMatcher();
    }

    /// <inheritdoc />
    public ClaimDiffResult Diff(
        IReadOnlyList<Claim> oldClaims,
        IReadOnlyList<Claim> newClaims,
        DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(oldClaims);
        ArgumentNullException.ThrowIfNull(newClaims);

        options ??= DiffOptions.Default;

        _logger.LogDebug(
            "Starting claim diff: {OldCount} old claims vs {NewCount} new claims",
            oldClaims.Count, newClaims.Count);

        // Index claims by ID for efficient lookup
        var oldById = oldClaims.ToDictionary(c => c.Id, c => c);
        var newById = newClaims.ToDictionary(c => c.Id, c => c);

        var added = new List<ClaimChange>();
        var removed = new List<ClaimChange>();
        var modified = new List<ClaimModification>();
        var unchanged = new List<Claim>();

        // Track which claims have been matched
        var matchedOldIds = new HashSet<Guid>();
        var matchedNewIds = new HashSet<Guid>();

        // Pass 1: Find exact ID matches and modifications
        foreach (var oldClaim in oldClaims)
        {
            if (newById.TryGetValue(oldClaim.Id, out var newClaim))
            {
                matchedOldIds.Add(oldClaim.Id);
                matchedNewIds.Add(newClaim.Id);

                var modification = DetectModification(oldClaim, newClaim, options, isSemanticMatch: false);
                if (modification is not null)
                {
                    modified.Add(modification);
                }
                else
                {
                    unchanged.Add(newClaim);
                }
            }
        }

        // Pass 2: Find semantic matches for unmatched claims
        var unmatchedOld = oldClaims.Where(c => !matchedOldIds.Contains(c.Id)).ToList();
        var unmatchedNew = newClaims.Where(c => !matchedNewIds.Contains(c.Id)).ToList();

        if (options.UseSemanticMatching && unmatchedOld.Count > 0 && unmatchedNew.Count > 0)
        {
            _logger.LogDebug(
                "Attempting semantic matching for {OldCount} unmatched old and {NewCount} unmatched new claims",
                unmatchedOld.Count, unmatchedNew.Count);

            var semanticMatches = FindSemanticMatches(unmatchedOld, unmatchedNew, options);

            foreach (var (oldClaim, newClaim, similarity) in semanticMatches)
            {
                matchedOldIds.Add(oldClaim.Id);
                matchedNewIds.Add(newClaim.Id);

                var modification = DetectModification(oldClaim, newClaim, options, isSemanticMatch: true, similarity);
                if (modification is not null)
                {
                    modified.Add(modification);
                }
                else
                {
                    unchanged.Add(newClaim);
                }
            }
        }

        // Pass 3: Remaining unmatched old = removed, unmatched new = added
        foreach (var oldClaim in oldClaims.Where(c => !matchedOldIds.Contains(c.Id)))
        {
            removed.Add(CreateClaimChange(oldClaim, ClaimChangeType.Removed, options));
        }

        foreach (var newClaim in newClaims.Where(c => !matchedNewIds.Contains(c.Id)))
        {
            added.Add(CreateClaimChange(newClaim, ClaimChangeType.Added, options));
        }

        // Build statistics
        var stats = BuildStats(added, removed, modified, unchanged);

        // Build groups if requested
        IReadOnlyList<ClaimChangeGroup>? groups = null;
        if (options.GroupRelatedChanges)
        {
            groups = GroupChanges(added, removed, modified);
        }

        var result = new ClaimDiffResult
        {
            Added = added,
            Removed = removed,
            Modified = modified,
            Unchanged = unchanged,
            OldClaimCount = oldClaims.Count,
            NewClaimCount = newClaims.Count,
            Stats = stats,
            Groups = groups
        };

        _logger.LogInformation(
            "Claim diff complete: {Added} added, {Removed} removed, {Modified} modified, {Unchanged} unchanged",
            added.Count, removed.Count, modified.Count, unchanged.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<ClaimDiffResult> DiffDocumentVersionsAsync(
        Guid documentId,
        int oldVersion,
        int newVersion,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Diffing document {DocumentId} versions {Old} to {New}",
            documentId, oldVersion, newVersion);

        // For now, get all claims by document
        // Future: Add version-aware repository methods
        var claims = await _repository.GetByDocumentAsync(documentId, ct);

        // Filter by version (assuming Version property exists)
        var oldClaims = claims.Where(c => c.Version == oldVersion).ToList();
        var newClaims = claims.Where(c => c.Version == newVersion).ToList();

        return Diff(oldClaims, newClaims);
    }

    /// <inheritdoc />
    public async Task<ClaimDiffResult> DiffFromBaselineAsync(
        Guid documentId,
        Guid baselineSnapshotId,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Diffing document {DocumentId} from baseline {SnapshotId}",
            documentId, baselineSnapshotId);

        // Get current claims
        var currentClaims = await _repository.GetByDocumentAsync(documentId, ct);

        // Get baseline claim IDs from snapshot (would need snapshot storage)
        // For now, return empty diff - implementation depends on snapshot storage
        _logger.LogWarning(
            "Baseline snapshot storage not yet implemented, returning empty diff");

        return ClaimDiffResult.NoChanges(currentClaims);
    }

    /// <inheritdoc />
    public async Task<ClaimSnapshot> CreateSnapshotAsync(
        Guid documentId,
        string? label = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating snapshot for document {DocumentId} with label '{Label}'",
            documentId, label);

        var claims = await _repository.GetByDocumentAsync(documentId, ct);

        var snapshot = new ClaimSnapshot
        {
            DocumentId = documentId,
            Label = label,
            ClaimIds = claims.Select(c => c.Id).ToList(),
            Claims = claims
        };

        // Would persist snapshot here
        _logger.LogInformation(
            "Created snapshot {SnapshotId} with {Count} claims",
            snapshot.Id, snapshot.ClaimCount);

        return snapshot;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ClaimHistoryEntry>> GetHistoryAsync(
        Guid documentId,
        int limit = 20,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Getting history for document {DocumentId}, limit {Limit}",
            documentId, limit);

        // Would retrieve from history storage
        // For now, return empty list
        return Task.FromResult<IReadOnlyList<ClaimHistoryEntry>>(
            Array.Empty<ClaimHistoryEntry>());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClaimContradiction>> FindContradictionsAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Finding contradictions in project {ProjectId}", projectId);

        var contradictions = new List<ClaimContradiction>();

        // Get all claims for project
        var searchResult = await _repository.SearchAsync(
            new ClaimSearchCriteria { ProjectId = projectId, PageSize = 1000 }, ct);

        var claims = searchResult.Claims;

        // Group by subject and predicate for direct contradiction detection
        var grouped = claims
            .GroupBy(c => (
                Subject: c.Subject.NormalizedForm ?? c.Subject.SurfaceForm.ToLowerInvariant(),
                Predicate: c.Predicate))
            .Where(g => g.Count() > 1);

        foreach (var group in grouped)
        {
            var claimList = group.ToList();

            // Check for different objects (direct contradictions)
            for (int i = 0; i < claimList.Count - 1; i++)
            {
                for (int j = i + 1; j < claimList.Count; j++)
                {
                    var c1 = claimList[i];
                    var c2 = claimList[j];

                    // Skip if from same document (not cross-document contradiction)
                    if (c1.DocumentId == c2.DocumentId)
                        continue;

                    // Check if objects differ
                    var obj1 = c1.Object.ToCanonicalForm();
                    var obj2 = c2.Object.ToCanonicalForm();

                    if (!obj1.Equals(obj2, StringComparison.OrdinalIgnoreCase))
                    {
                        contradictions.Add(new ClaimContradiction
                        {
                            ProjectId = projectId,
                            Claim1 = c1,
                            Claim2 = c2,
                            Type = ContradictionType.DirectContradiction,
                            Description = $"Conflicting claims: '{c1.Subject.SurfaceForm} {c1.Predicate}' " +
                                        $"has different values: '{obj1}' vs '{obj2}'"
                        });
                    }
                }
            }
        }

        _logger.LogInformation(
            "Found {Count} contradictions in project {ProjectId}",
            contradictions.Count, projectId);

        return contradictions;
    }

    #region Private Methods

    private List<(Claim Old, Claim New, float Similarity)> FindSemanticMatches(
        List<Claim> unmatchedOld,
        List<Claim> unmatchedNew,
        DiffOptions options)
    {
        var matches = new List<(Claim, Claim, float)>();
        var usedNewIds = new HashSet<Guid>();

        foreach (var oldClaim in unmatchedOld)
        {
            var candidates = unmatchedNew.Where(c => !usedNewIds.Contains(c.Id)).ToList();
            var (match, similarity) = _semanticMatcher.FindMatch(
                oldClaim, candidates, options.SemanticMatchThreshold);

            if (match is not null)
            {
                matches.Add((oldClaim, match, similarity));
                usedNewIds.Add(match.Id);
            }
        }

        return matches;
    }

    private ClaimModification? DetectModification(
        Claim oldClaim,
        Claim newClaim,
        DiffOptions options,
        bool isSemanticMatch,
        float similarity = 1.0f)
    {
        var changeTypes = new List<ClaimChangeType>();
        var fieldChanges = new List<FieldChange>();

        // Compare subject
        if (!SubjectsEqual(oldClaim.Subject, newClaim.Subject))
        {
            changeTypes.Add(ClaimChangeType.SubjectChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "Subject",
                OldValue = oldClaim.Subject.SurfaceForm,
                NewValue = newClaim.Subject.SurfaceForm,
                Description = $"Subject changed from '{oldClaim.Subject.SurfaceForm}' to '{newClaim.Subject.SurfaceForm}'"
            });
        }

        // Compare predicate
        if (!string.Equals(oldClaim.Predicate, newClaim.Predicate, StringComparison.OrdinalIgnoreCase))
        {
            changeTypes.Add(ClaimChangeType.PredicateChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "Predicate",
                OldValue = oldClaim.Predicate,
                NewValue = newClaim.Predicate,
                Description = $"Predicate changed from '{oldClaim.Predicate}' to '{newClaim.Predicate}'"
            });
        }

        // Compare object
        if (!ObjectsEqual(oldClaim.Object, newClaim.Object))
        {
            changeTypes.Add(ClaimChangeType.ObjectChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "Object",
                OldValue = oldClaim.Object.ToCanonicalForm(),
                NewValue = newClaim.Object.ToCanonicalForm(),
                Description = $"Object changed from '{oldClaim.Object.ToCanonicalForm()}' to '{newClaim.Object.ToCanonicalForm()}'"
            });
        }

        // Compare confidence (with threshold)
        var confidenceDiff = Math.Abs(oldClaim.Confidence - newClaim.Confidence);
        if (confidenceDiff >= options.IgnoreConfidenceChangeBelow)
        {
            changeTypes.Add(ClaimChangeType.ConfidenceChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "Confidence",
                OldValue = oldClaim.Confidence,
                NewValue = newClaim.Confidence,
                Description = $"Confidence changed from {oldClaim.Confidence:P0} to {newClaim.Confidence:P0}"
            });
        }

        // Compare validation status
        if (options.TrackValidationChanges && oldClaim.ValidationStatus != newClaim.ValidationStatus)
        {
            changeTypes.Add(ClaimChangeType.ValidationChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "ValidationStatus",
                OldValue = oldClaim.ValidationStatus,
                NewValue = newClaim.ValidationStatus,
                Description = $"Validation status changed from '{oldClaim.ValidationStatus}' to '{newClaim.ValidationStatus}'"
            });
        }

        // Compare evidence
        if (options.IncludeEvidence && !EvidenceEqual(oldClaim.Evidence, newClaim.Evidence))
        {
            changeTypes.Add(ClaimChangeType.EvidenceChanged);
            fieldChanges.Add(new FieldChange
            {
                FieldName = "Evidence",
                OldValue = oldClaim.Evidence?.Sentence,
                NewValue = newClaim.Evidence?.Sentence,
                Description = "Evidence source text changed"
            });
        }

        if (changeTypes.Count == 0)
            return null;

        return new ClaimModification
        {
            OldClaim = oldClaim,
            NewClaim = newClaim,
            ChangeTypes = changeTypes,
            FieldChanges = fieldChanges,
            Description = string.Join("; ", fieldChanges.Select(f => f.Description)),
            Impact = AssessImpact(changeTypes, oldClaim, newClaim),
            IsSemanticMatch = isSemanticMatch,
            Similarity = similarity
        };
    }

    private static bool SubjectsEqual(ClaimEntity a, ClaimEntity b)
    {
        if (a.IsResolved && b.IsResolved)
            return a.EntityId == b.EntityId;

        return string.Equals(
            a.NormalizedForm ?? a.SurfaceForm,
            b.NormalizedForm ?? b.SurfaceForm,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool ObjectsEqual(ClaimObject a, ClaimObject b)
    {
        return a.ToCanonicalForm().Equals(b.ToCanonicalForm(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvidenceEqual(ClaimEvidence? a, ClaimEvidence? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return a.StartOffset == b.StartOffset &&
               a.EndOffset == b.EndOffset &&
               string.Equals(a.Sentence, b.Sentence, StringComparison.Ordinal);
    }

    private ClaimChange CreateClaimChange(Claim claim, ClaimChangeType changeType, DiffOptions options)
    {
        var action = changeType == ClaimChangeType.Added ? "Added" : "Removed";

        return new ClaimChange
        {
            Claim = claim,
            ChangeType = changeType,
            Description = $"{action}: {claim.Subject.SurfaceForm} {claim.Predicate} {claim.Object.ToCanonicalForm()}",
            Evidence = options.IncludeEvidence ? claim.Evidence?.Sentence : null,
            Impact = AssessAddRemoveImpact(claim)
        };
    }

    private static ChangeImpact AssessImpact(List<ClaimChangeType> changeTypes, Claim oldClaim, Claim newClaim)
    {
        // Critical: Type changes, deprecation predicates
        if (changeTypes.Contains(ClaimChangeType.PredicateChanged))
            return ChangeImpact.Critical;

        if (newClaim.Predicate == ClaimPredicate.IS_DEPRECATED ||
            newClaim.Predicate == ClaimPredicate.REPLACED_BY)
            return ChangeImpact.High;

        // High: Subject or object changes
        if (changeTypes.Contains(ClaimChangeType.SubjectChanged) ||
            changeTypes.Contains(ClaimChangeType.ObjectChanged))
            return ChangeImpact.High;

        // Medium: Validation changes
        if (changeTypes.Contains(ClaimChangeType.ValidationChanged))
            return ChangeImpact.Medium;

        // Low: Confidence, evidence changes
        return ChangeImpact.Low;
    }

    private static ChangeImpact AssessAddRemoveImpact(Claim claim)
    {
        // Deprecation is high impact
        if (claim.Predicate == ClaimPredicate.IS_DEPRECATED)
            return ChangeImpact.High;

        // Requirements are high impact
        if (claim.Predicate is ClaimPredicate.REQUIRES or ClaimPredicate.REQUIRED_BY)
            return ChangeImpact.High;

        // Type information is critical
        if (claim.Predicate == ClaimPredicate.HAS_TYPE)
            return ChangeImpact.Critical;

        // Standard predicates are medium
        if (claim.Predicate is ClaimPredicate.ACCEPTS or ClaimPredicate.RETURNS)
            return ChangeImpact.Medium;

        return ChangeImpact.Low;
    }

    private static DiffStats BuildStats(
        List<ClaimChange> added,
        List<ClaimChange> removed,
        List<ClaimModification> modified,
        List<Claim> unchanged)
    {
        var allChangedClaims = added.Select(a => a.Claim)
            .Concat(removed.Select(r => r.Claim))
            .Concat(modified.Select(m => m.NewClaim));

        var byPredicate = allChangedClaims
            .GroupBy(c => c.Predicate)
            .ToDictionary(g => g.Key, g => g.Count());

        var allChangeTypes = added.Select(a => a.ChangeType)
            .Concat(removed.Select(r => r.ChangeType))
            .Concat(modified.SelectMany(m => m.ChangeTypes));

        var byType = allChangeTypes
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        return new DiffStats
        {
            AddedCount = added.Count,
            RemovedCount = removed.Count,
            ModifiedCount = modified.Count,
            UnchangedCount = unchanged.Count,
            ChangesByPredicate = byPredicate,
            ChangesByType = byType
        };
    }

    private static List<ClaimChangeGroup> GroupChanges(
        List<ClaimChange> added,
        List<ClaimChange> removed,
        List<ClaimModification> modified)
    {
        var groups = new Dictionary<string, (
            string Label,
            List<ClaimChange> Changes,
            List<ClaimModification> Modifications,
            ChangeImpact MaxImpact)>();

        void AddToGroup(Claim claim, ClaimChange? change, ClaimModification? modification, ChangeImpact impact)
        {
            var key = claim.Subject.NormalizedForm ?? claim.Subject.SurfaceForm.ToLowerInvariant();
            var label = claim.Subject.SurfaceForm;

            if (!groups.TryGetValue(key, out var group))
            {
                group = (label, new List<ClaimChange>(), new List<ClaimModification>(), ChangeImpact.Low);
                groups[key] = group;
            }

            if (change is not null) group.Changes.Add(change);
            if (modification is not null) group.Modifications.Add(modification);
            if (impact > group.MaxImpact)
                groups[key] = (group.Label, group.Changes, group.Modifications, impact);
        }

        foreach (var change in added.Concat(removed))
            AddToGroup(change.Claim, change, null, change.Impact);

        foreach (var mod in modified)
            AddToGroup(mod.NewClaim, null, mod, mod.Impact);

        return groups.Select(kvp => new ClaimChangeGroup
        {
            GroupId = kvp.Key,
            Label = kvp.Value.Label,
            Changes = kvp.Value.Changes,
            Modifications = kvp.Value.Modifications,
            MaxImpact = kvp.Value.MaxImpact
        }).ToList();
    }

    #endregion
}
