# LCS-DES-076-KG-h: Conflict Resolver

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-076-KG-h |
| **System Breakdown** | LCS-SBD-076-KG |
| **Version** | v0.7.6 |
| **Codename** | Conflict Resolver (CKVS Phase 4c) |
| **Estimated Hours** | 6 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Conflict Resolver** handles detection and resolution of conflicts that arise during synchronization between documents and the knowledge graph. It provides multiple conflict resolution strategies, supports intelligent merging, and enables manual intervention workflows.

### 1.2 Key Responsibilities

- Detect value conflicts between document and graph
- Detect structural conflicts (missing entities, relationships)
- Apply conflict resolution strategies intelligently
- Support automatic conflict merging
- Enable manual conflict review and resolution
- Maintain conflict history and audit trail
- Provide conflict visualization and comparison

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Sync/
      Conflict/
        IConflictDetector.cs
        IConflictResolver.cs
        ConflictDetector.cs
        ConflictResolver.cs
        ConflictMerger.cs
```

---

## 2. Interface Definitions

### 2.1 Conflict Detector

```csharp
namespace Lexichord.KnowledgeGraph.Sync.Conflict;

/// <summary>
/// Detects conflicts between documents and graph.
/// </summary>
public interface IConflictDetector
{
    /// <summary>
    /// Detects conflicts for an extraction.
    /// </summary>
    Task<IReadOnlyList<SyncConflict>> DetectAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default);

    /// <summary>
    /// Detects value conflicts for specific entities.
    /// </summary>
    Task<IReadOnlyList<ConflictDetail>> DetectValueConflictsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);

    /// <summary>
    /// Detects structural conflicts.
    /// </summary>
    Task<IReadOnlyList<ConflictDetail>> DetectStructuralConflictsAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if entities have changed since extraction.
    /// </summary>
    Task<bool> EntitiesChangedAsync(
        ExtractionRecord extraction,
        CancellationToken ct = default);
}
```

### 2.2 Conflict Resolver

```csharp
/// <summary>
/// Resolves detected conflicts using specified strategies.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Resolves conflicts for a document using a strategy.
    /// </summary>
    Task<SyncResult> ResolveAsync(
        Guid documentId,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a specific conflict.
    /// </summary>
    Task<ConflictResolutionResult> ResolveConflictAsync(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves multiple conflicts.
    /// </summary>
    Task<IReadOnlyList<ConflictResolutionResult>> ResolveConflictsAsync(
        IReadOnlyList<SyncConflict> conflicts,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Attempts to merge conflicted values.
    /// </summary>
    Task<ConflictMergeResult> MergeConflictAsync(
        SyncConflict conflict,
        CancellationToken ct = default);

    /// <summary>
    /// Gets unresolved conflicts for a document.
    /// </summary>
    Task<IReadOnlyList<SyncConflict>> GetUnresolvedConflictsAsync(
        Guid documentId,
        CancellationToken ct = default);
}
```

### 2.3 Conflict Merger

```csharp
/// <summary>
/// Intelligently merges conflicted values.
/// </summary>
public interface IConflictMerger
{
    /// <summary>
    /// Merges two conflicting values.
    /// </summary>
    Task<MergeResult> MergeAsync(
        object documentValue,
        object graphValue,
        MergeContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets merge strategy for a conflict type.
    /// </summary>
    IConflictMergeStrategy GetMergeStrategy(ConflictType conflictType);
}
```

---

## 3. Data Types

### 3.1 Conflict Details

```csharp
/// <summary>
/// Detailed information about a conflict.
/// </summary>
public record ConflictDetail
{
    /// <summary>Unique conflict ID.</summary>
    public required Guid ConflictId { get; init; }

    /// <summary>Entity involved in the conflict.</summary>
    public required KnowledgeEntity Entity { get; init; }

    /// <summary>The specific field/property in conflict.</summary>
    public required string ConflictField { get; init; }

    /// <summary>Value from the document.</summary>
    public required object DocumentValue { get; init; }

    /// <summary>Value from the graph.</summary>
    public required object GraphValue { get; init; }

    /// <summary>Type of conflict.</summary>
    public required ConflictType Type { get; init; }

    /// <summary>Severity assessment.</summary>
    public ConflictSeverity Severity { get; init; }

    /// <summary>Detected timestamp.</summary>
    public required DateTimeOffset DetectedAt { get; init; }

    /// <summary>Document last modified timestamp.</summary>
    public DateTimeOffset? DocumentModifiedAt { get; init; }

    /// <summary>Graph entity last modified timestamp.</summary>
    public DateTimeOffset? GraphModifiedAt { get; init; }

    /// <summary>Suggested resolution.</summary>
    public ConflictResolutionStrategy? SuggestedStrategy { get; init; }

    /// <summary>Confidence in suggested resolution (0-1).</summary>
    public float ResolutionConfidence { get; init; }
}
```

### 3.2 Conflict Resolution Result

```csharp
/// <summary>
/// Result of resolving a single conflict.
/// </summary>
public record ConflictResolutionResult
{
    /// <summary>Conflict that was resolved.</summary>
    public required SyncConflict Conflict { get; init; }

    /// <summary>Strategy that was applied.</summary>
    public required ConflictResolutionStrategy Strategy { get; init; }

    /// <summary>Whether resolution succeeded.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Resolved value if successful.</summary>
    public object? ResolvedValue { get; init; }

    /// <summary>Error message if resolution failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Timestamp of resolution.</summary>
    public DateTimeOffset ResolvedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>User who resolved (if manual).</summary>
    public Guid? ResolvedBy { get; init; }

    /// <summary>Whether resolution was automatic.</summary>
    public bool IsAutomatic { get; init; }
}
```

### 3.3 Merge Result

```csharp
/// <summary>
/// Result of a merge operation.
/// </summary>
public record MergeResult
{
    /// <summary>Whether merge succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>Merged value if successful.</summary>
    public object? MergedValue { get; init; }

    /// <summary>Merge strategy that was used.</summary>
    public MergeStrategy UsedStrategy { get; init; }

    /// <summary>Confidence in merge result (0-1).</summary>
    public float Confidence { get; init; }

    /// <summary>Error message if merge failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>How the merge was created.</summary>
    public MergeType MergeType { get; init; }
}

public record ConflictMergeResult : MergeResult
{
    /// <summary>Original document value.</summary>
    public required object DocumentValue { get; init; }

    /// <summary>Original graph value.</summary>
    public required object GraphValue { get; init; }

    /// <summary>Explanation of merge decision.</summary>
    public string? Explanation { get; init; }
}

public enum MergeStrategy
{
    /// <summary>Use document value as-is.</summary>
    DocumentFirst,
    /// <summary>Use graph value as-is.</summary>
    GraphFirst,
    /// <summary>Combine/concatenate values intelligently.</summary>
    Combine,
    /// <summary>Use more recent value based on timestamps.</summary>
    MostRecent,
    /// <summary>Use value with higher confidence score.</summary>
    HighestConfidence,
    /// <summary>Requires manual selection.</summary>
    RequiresManualMerge
}

public enum MergeType
{
    /// <summary>Direct selection of one value.</summary>
    Selection,
    /// <summary>Intelligent combination of values.</summary>
    Intelligent,
    /// <summary>Weighted merge based on confidence.</summary>
    Weighted,
    /// <summary>Manual user selection.</summary>
    Manual,
    /// <summary>Use most recent version.</summary>
    Temporal
}

public record MergeContext
{
    /// <summary>Entity involved in merge.</summary>
    public KnowledgeEntity? Entity { get; init; }

    /// <summary>Document being merged.</summary>
    public Document? Document { get; init; }

    /// <summary>Conflict type for context.</summary>
    public ConflictType? ConflictType { get; init; }

    /// <summary>User initiating merge if manual.</summary>
    public Guid? UserId { get; init; }

    /// <summary>Additional context data.</summary>
    public Dictionary<string, object> ContextData { get; init; } = new();
}
```

### 3.4 Conflict Resolution Options

```csharp
/// <summary>
/// Options for conflict resolution.
/// </summary>
public record ConflictResolutionOptions
{
    /// <summary>Default strategy for unspecified conflicts.</summary>
    public ConflictResolutionStrategy DefaultStrategy { get; init; } = ConflictResolutionStrategy.Merge;

    /// <summary>Strategies for specific conflict types.</summary>
    public Dictionary<ConflictType, ConflictResolutionStrategy> StrategyByType { get; init; } = new();

    /// <summary>Whether to auto-resolve low-severity conflicts.</summary>
    public bool AutoResolveLow { get; init; } = true;

    /// <summary>Whether to auto-resolve medium-severity conflicts.</summary>
    public bool AutoResolveMedium { get; init; } = false;

    /// <summary>Whether to auto-resolve high-severity conflicts.</summary>
    public bool AutoResolveHigh { get; init; } = false;

    /// <summary>Minimum confidence for auto-merge.</summary>
    public float MinMergeConfidence { get; init; } = 0.8f;

    /// <summary>Whether to preserve history of conflicts.</summary>
    public bool PreserveConflictHistory { get; init; } = true;

    /// <summary>Maximum conflict resolution attempts.</summary>
    public int MaxResolutionAttempts { get; init; } = 3;

    /// <summary>Timeout for resolution.</summary>
    public TimeSpan ResolutionTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
```

---

## 4. Implementation

### 4.1 Conflict Detector

```csharp
public class ConflictDetector : IConflictDetector
{
    private readonly IGraphRepository _graphRepository;
    private readonly IEntityComparer _entityComparer;
    private readonly ILogger<ConflictDetector> _logger;

    public ConflictDetector(
        IGraphRepository graphRepository,
        IEntityComparer entityComparer,
        ILogger<ConflictDetector> logger)
    {
        _graphRepository = graphRepository;
        _entityComparer = entityComparer;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SyncConflict>> DetectAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Detecting conflicts for document {DocumentId}",
            document.Id);

        var conflicts = new List<SyncConflict>();

        // Detect value conflicts
        var valueConflicts = await DetectValueConflictsAsync(
            extraction.Entities.Cast<KnowledgeEntity>().ToList(), ct);

        foreach (var vc in valueConflicts)
        {
            conflicts.Add(new SyncConflict
            {
                ConflictTarget = vc.Entity.Name,
                DocumentValue = vc.DocumentValue,
                GraphValue = vc.GraphValue,
                DetectedAt = DateTimeOffset.UtcNow,
                Type = vc.Type,
                Severity = vc.Severity,
                Description = $"Value conflict in {vc.ConflictField}"
            });
        }

        // Detect structural conflicts
        var structuralConflicts = await DetectStructuralConflictsAsync(
            document, extraction, ct);

        foreach (var sc in structuralConflicts)
        {
            conflicts.Add(new SyncConflict
            {
                ConflictTarget = sc.Entity.Name,
                DocumentValue = sc.DocumentValue,
                GraphValue = sc.GraphValue,
                DetectedAt = DateTimeOffset.UtcNow,
                Type = sc.Type,
                Severity = sc.Severity,
                Description = $"Structural conflict: {sc.Type}"
            });
        }

        _logger.LogInformation(
            "Detected {Count} conflicts for document {DocumentId}",
            conflicts.Count, document.Id);

        return conflicts;
    }

    public async Task<IReadOnlyList<ConflictDetail>> DetectValueConflictsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Detecting value conflicts for {Count} entities", entities.Count);

        var conflicts = new List<ConflictDetail>();

        foreach (var entity in entities)
        {
            // Check if entity exists in graph
            var graphEntity = await _graphRepository.GetEntityAsync(entity.Id, ct);

            if (graphEntity == null)
            {
                continue; // New entity, no conflict
            }

            // Compare property values
            var comparison = await _entityComparer.CompareAsync(entity, graphEntity, ct);

            foreach (var diff in comparison.PropertyDifferences)
            {
                conflicts.Add(new ConflictDetail
                {
                    ConflictId = Guid.NewGuid(),
                    Entity = graphEntity,
                    ConflictField = diff.PropertyName,
                    DocumentValue = diff.DocumentValue ?? "(empty)",
                    GraphValue = diff.GraphValue ?? "(empty)",
                    Type = ConflictType.ValueMismatch,
                    Severity = DetermineSeverity(diff),
                    DetectedAt = DateTimeOffset.UtcNow,
                    DocumentModifiedAt = entity.UpdatedAt,
                    GraphModifiedAt = graphEntity.UpdatedAt,
                    SuggestedStrategy = SuggestResolutionStrategy(graphEntity, diff),
                    ResolutionConfidence = diff.Confidence
                });
            }
        }

        return conflicts;
    }

    public async Task<IReadOnlyList<ConflictDetail>> DetectStructuralConflictsAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Detecting structural conflicts for document {DocumentId}",
            document.Id);

        var conflicts = new List<ConflictDetail>();

        // Get entities referenced in document
        var documentEntityIds = extraction.Entities
            .OfType<KnowledgeEntity>()
            .Select(e => e.Id)
            .ToHashSet();

        // Get entities from graph
        var graphEntities = await _graphRepository.GetEntitiesExtractedFromAsync(
            document.Id, ct);

        var graphEntityIds = graphEntities
            .Select(e => e.Id)
            .ToHashSet();

        // Entities in graph but not in document (deleted from document)
        foreach (var entityId in graphEntityIds.Except(documentEntityIds))
        {
            var entity = await _graphRepository.GetEntityAsync(entityId, ct);
            if (entity != null)
            {
                conflicts.Add(new ConflictDetail
                {
                    ConflictId = Guid.NewGuid(),
                    Entity = entity,
                    ConflictField = "Entity",
                    DocumentValue = "(deleted from document)",
                    GraphValue = "(exists in graph)",
                    Type = ConflictType.MissingInDocument,
                    Severity = ConflictSeverity.Medium,
                    DetectedAt = DateTimeOffset.UtcNow,
                    SuggestedStrategy = ConflictResolutionStrategy.Manual,
                    ResolutionConfidence = 0.5f
                });
            }
        }

        return conflicts;
    }

    public async Task<bool> EntitiesChangedAsync(
        ExtractionRecord extraction,
        CancellationToken ct = default)
    {
        var currentEntities = await _graphRepository.GetEntitiesAsync(
            extraction.EntityIds, ct);

        var haveChanged = false;

        foreach (var entity in currentEntities)
        {
            if (entity.UpdatedAt > extraction.ExtractedAt)
            {
                haveChanged = true;
                break;
            }
        }

        return haveChanged;
    }

    private ConflictSeverity DetermineSeverity(PropertyDifference diff)
    {
        return diff.Confidence switch
        {
            >= 0.8f => ConflictSeverity.Low,
            >= 0.5f => ConflictSeverity.Medium,
            _ => ConflictSeverity.High
        };
    }

    private ConflictResolutionStrategy? SuggestResolutionStrategy(
        KnowledgeEntity entity,
        PropertyDifference diff)
    {
        if (diff.Confidence >= 0.8f)
        {
            return ConflictResolutionStrategy.Merge;
        }

        return ConflictResolutionStrategy.Manual;
    }
}

public record PropertyDifference
{
    public required string PropertyName { get; init; }
    public object? DocumentValue { get; init; }
    public object? GraphValue { get; init; }
    public float Confidence { get; init; }
}

public record EntityComparison
{
    public required KnowledgeEntity DocumentEntity { get; init; }
    public required KnowledgeEntity GraphEntity { get; init; }
    public IReadOnlyList<PropertyDifference> PropertyDifferences { get; init; } = [];
}

public interface IEntityComparer
{
    Task<EntityComparison> CompareAsync(
        KnowledgeEntity docEntity,
        KnowledgeEntity graphEntity,
        CancellationToken ct = default);
}
```

### 4.2 Conflict Resolver

```csharp
public class ConflictResolver : IConflictResolver
{
    private readonly IGraphRepository _graphRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IConflictMerger _merger;
    private readonly IConflictDetector _detector;
    private readonly ILogger<ConflictResolver> _logger;

    public ConflictResolver(
        IGraphRepository graphRepository,
        IDocumentRepository documentRepository,
        IConflictMerger merger,
        IConflictDetector detector,
        ILogger<ConflictResolver> logger)
    {
        _graphRepository = graphRepository;
        _documentRepository = documentRepository;
        _merger = merger;
        _detector = detector;
        _logger = logger;
    }

    public async Task<SyncResult> ResolveAsync(
        Guid documentId,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Resolving conflicts for document {DocumentId} using strategy {Strategy}",
            documentId, strategy);

        var stopwatch = Stopwatch.StartNew();
        var resolutionResults = new List<ConflictResolutionResult>();

        try
        {
            // Get unresolved conflicts
            var conflicts = await GetUnresolvedConflictsAsync(documentId, ct);

            if (conflicts.Count == 0)
            {
                stopwatch.Stop();

                return new SyncResult
                {
                    Status = SyncOperationStatus.NoChanges,
                    Duration = stopwatch.Elapsed
                };
            }

            // Resolve each conflict
            foreach (var conflict in conflicts)
            {
                var result = await ResolveConflictAsync(conflict, strategy, ct);
                resolutionResults.Add(result);
            }

            stopwatch.Stop();

            return new SyncResult
            {
                Status = resolutionResults.All(r => r.Succeeded)
                    ? SyncOperationStatus.Success
                    : SyncOperationStatus.PartialSuccess,
                Conflicts = resolutionResults
                    .Where(r => !r.Succeeded)
                    .Select(r => r.Conflict)
                    .ToList(),
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Conflict resolution failed for document {DocumentId}",
                documentId);

            stopwatch.Stop();

            return new SyncResult
            {
                Status = SyncOperationStatus.Failed,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ConflictResolutionResult> ResolveConflictAsync(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Resolving conflict {Target} using strategy {Strategy}",
            conflict.ConflictTarget, strategy);

        try
        {
            object? resolvedValue = null;
            var succeeded = false;

            switch (strategy)
            {
                case ConflictResolutionStrategy.UseDocument:
                    resolvedValue = conflict.DocumentValue;
                    succeeded = true;
                    break;

                case ConflictResolutionStrategy.UseGraph:
                    resolvedValue = conflict.GraphValue;
                    succeeded = true;
                    break;

                case ConflictResolutionStrategy.Merge:
                    var mergeResult = await _merger.MergeAsync(
                        conflict.DocumentValue,
                        conflict.GraphValue,
                        new MergeContext(),
                        ct);

                    if (mergeResult.Success)
                    {
                        resolvedValue = mergeResult.MergedValue;
                        succeeded = true;
                    }
                    break;

                case ConflictResolutionStrategy.Manual:
                    succeeded = false;
                    break;

                default:
                    succeeded = false;
                    break;
            }

            return new ConflictResolutionResult
            {
                Conflict = conflict,
                Strategy = strategy,
                Succeeded = succeeded,
                ResolvedValue = resolvedValue,
                IsAutomatic = strategy != ConflictResolutionStrategy.Manual
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to resolve conflict {Target}",
                conflict.ConflictTarget);

            return new ConflictResolutionResult
            {
                Conflict = conflict,
                Strategy = strategy,
                Succeeded = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<ConflictResolutionResult>> ResolveConflictsAsync(
        IReadOnlyList<SyncConflict> conflicts,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        var results = new List<ConflictResolutionResult>();

        foreach (var conflict in conflicts)
        {
            var result = await ResolveConflictAsync(conflict, strategy, ct);
            results.Add(result);
        }

        return results;
    }

    public async Task<ConflictMergeResult> MergeConflictAsync(
        SyncConflict conflict,
        CancellationToken ct = default)
    {
        var mergeResult = await _merger.MergeAsync(
            conflict.DocumentValue,
            conflict.GraphValue,
            new MergeContext(),
            ct);

        return new ConflictMergeResult
        {
            Success = mergeResult.Success,
            MergedValue = mergeResult.MergedValue,
            UsedStrategy = mergeResult.UsedStrategy,
            Confidence = mergeResult.Confidence,
            DocumentValue = conflict.DocumentValue,
            GraphValue = conflict.GraphValue,
            Explanation = $"Merged using {mergeResult.UsedStrategy}"
        };
    }

    public async Task<IReadOnlyList<SyncConflict>> GetUnresolvedConflictsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await _documentRepository.GetUnresolvedConflictsAsync(documentId, ct);
    }
}
```

---

## 5. Algorithm / Flow

### Conflict Detection Flow

```
1. For each extracted entity:
   - Look up corresponding entity in graph
   - If not found: no conflict (new entity)
   - If found:
     a. Compare all properties
     b. For each property difference:
        - Determine severity
        - Suggest resolution strategy
        - Create ConflictDetail
2. Detect structural conflicts:
   - Find entities in graph but not in document (deleted)
   - Find entities in document but not in graph (new)
3. Return all detected conflicts
```

### Conflict Resolution Flow

```
1. Get all unresolved conflicts for document
2. For each conflict:
   a. Apply specified resolution strategy
   b. Based on strategy:
      - UseDocument: Use document value
      - UseGraph: Use graph value
      - Merge: Attempt intelligent merge
      - Manual: Mark as requiring manual intervention
3. Update conflict status
4. Persist resolved values
5. Return resolution results
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Entity not found | Log, treat as structural conflict |
| Merge fails | Mark as requiring manual intervention |
| Persist fails | Return PartialSuccess with failed conflicts |
| Timeout exceeded | Cancel resolution, return PartialSuccess |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `DetectValueConflicts_ReturnsConflicts` | Conflicts detected |
| `DetectStructuralConflicts_ReturnsStructural` | Structural conflicts detected |
| `ResolveConflict_UseDocument_Works` | Document strategy works |
| `ResolveConflict_UseGraph_Works` | Graph strategy works |
| `ResolveConflict_Merge_Works` | Merge strategy works |
| `ResolveConflict_Manual_RequiresIntervention` | Manual handling works |
| `EntitiesChanged_DetectsUpdates` | Change detection works |

---

## 8. Performance

| Aspect | Target |
| :----- | :----- |
| Value conflict detection | < 200ms per 100 entities |
| Structural conflict detection | < 500ms per 100 entities |
| Single conflict resolution | < 100ms |
| Batch resolution | < 2s per 100 conflicts |
| Merge operation | < 300ms |

---

## 9. License Gating

| Tier | Detect | Auto-Resolve | Merge | Manual |
| :--- | :--- | :--- | :--- | :--- |
| Core | No | N/A | N/A | N/A |
| WriterPro | Basic | Low only | No | Yes |
| Teams | Full | Low/Medium | Yes | Yes |
| Enterprise | Full | All | Advanced | Yes |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
