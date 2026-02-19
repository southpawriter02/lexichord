// =============================================================================
// File: EnhancedConflictResolver.cs
// Project: Lexichord.Modules.Knowledge
// Description: Enhanced conflict resolver with detailed resolution results.
// =============================================================================
// LOGIC: EnhancedConflictResolver extends the basic ConflictResolver from
//   v0.7.6e with detailed resolution results, conflict merging support,
//   unresolved conflict tracking, and MediatR event publishing.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: IConflictMerger, ConflictResolutionResult, ConflictMergeResult,
//               ConflictResolutionOptions (v0.7.6h), IConflictResolver (v0.7.6e),
//               ILicenseContext (v0.0.4c), IMediator (MediatR)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Enhanced conflict resolver with detailed resolution results and merge support.
/// </summary>
/// <remarks>
/// <para>
/// Provides extended conflict resolution capabilities:
/// </para>
/// <list type="bullet">
///   <item>Detailed resolution results via <see cref="ConflictResolutionResult"/>.</item>
///   <item>Conflict merging via <see cref="IConflictMerger"/>.</item>
///   <item>Unresolved conflict tracking via <see cref="ConflictStore"/>.</item>
///   <item>MediatR event publishing for resolution completion.</item>
///   <item>License gating for conflict resolution features.</item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: No conflict resolution.</item>
///   <item>WriterPro: Basic resolution (Low severity auto-resolve).</item>
///   <item>Teams: Full resolution (Low/Medium auto-resolve, merge).</item>
///   <item>Enterprise: Advanced resolution (all auto-resolve, advanced merge).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public sealed class EnhancedConflictResolver
{
    private readonly IConflictMerger _merger;
    private readonly ConflictStore _conflictStore;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<EnhancedConflictResolver> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EnhancedConflictResolver"/>.
    /// </summary>
    /// <param name="merger">The conflict merger for merge operations.</param>
    /// <param name="conflictStore">The conflict store for tracking.</param>
    /// <param name="licenseContext">The license context for tier checking.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger instance.</param>
    public EnhancedConflictResolver(
        IConflictMerger merger,
        ConflictStore conflictStore,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<EnhancedConflictResolver> logger)
    {
        _merger = merger;
        _conflictStore = conflictStore;
        _licenseContext = licenseContext;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Resolves a specific conflict using the specified strategy.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="strategy">The resolution strategy to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A detailed <see cref="ConflictResolutionResult"/>.</returns>
    public async Task<ConflictResolutionResult> ResolveConflictAsync(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Resolving conflict {Target} using strategy {Strategy}",
            conflict.ConflictTarget, strategy);

        // LOGIC: Check license tier for resolution capabilities.
        if (!CanResolve(conflict.Severity, strategy))
        {
            _logger.LogWarning(
                "License tier {Tier} does not support resolving {Severity} conflicts with {Strategy}",
                _licenseContext.Tier, conflict.Severity, strategy);

            return ConflictResolutionResult.Failure(
                conflict,
                strategy,
                $"License tier {_licenseContext.Tier} does not support this resolution type");
        }

        try
        {
            var result = strategy switch
            {
                ConflictResolutionStrategy.UseDocument =>
                    await ResolveWithDocumentValueAsync(conflict, ct),

                ConflictResolutionStrategy.UseGraph =>
                    ResolveWithGraphValue(conflict),

                ConflictResolutionStrategy.Merge =>
                    await ResolveWithMergeAsync(conflict, ct),

                ConflictResolutionStrategy.Manual =>
                    ConflictResolutionResult.RequiresManualIntervention(conflict),

                ConflictResolutionStrategy.DiscardDocument =>
                    ResolveWithGraphValue(conflict),

                ConflictResolutionStrategy.DiscardGraph =>
                    await ResolveWithDocumentValueAsync(conflict, ct),

                _ => ConflictResolutionResult.Failure(
                    conflict,
                    strategy,
                    $"Unknown resolution strategy: {strategy}")
            };

            // LOGIC: Publish event if resolution succeeded.
            if (result.Succeeded)
            {
                await PublishResolvedEventAsync(conflict, result, ct);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Conflict resolution cancelled for {Target}", conflict.ConflictTarget);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Conflict resolution failed for {Target}",
                conflict.ConflictTarget);

            return ConflictResolutionResult.Failure(conflict, strategy, ex.Message);
        }
    }

    /// <summary>
    /// Resolves multiple conflicts using the specified strategy.
    /// </summary>
    /// <param name="conflicts">The conflicts to resolve.</param>
    /// <param name="strategy">The resolution strategy to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of resolution results.</returns>
    public async Task<IReadOnlyList<ConflictResolutionResult>> ResolveConflictsAsync(
        IReadOnlyList<SyncConflict> conflicts,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Resolving {Count} conflicts using strategy {Strategy}",
            conflicts.Count, strategy);

        var results = new List<ConflictResolutionResult>();

        foreach (var conflict in conflicts)
        {
            ct.ThrowIfCancellationRequested();

            var result = await ResolveConflictAsync(conflict, strategy, ct);
            results.Add(result);
        }

        var successCount = results.Count(r => r.Succeeded);
        _logger.LogInformation(
            "Conflict resolution completed: {Success}/{Total} succeeded",
            successCount, results.Count);

        return results;
    }

    /// <summary>
    /// Attempts to merge a conflict's values.
    /// </summary>
    /// <param name="conflict">The conflict to merge.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ConflictMergeResult"/> with merge details.</returns>
    public async Task<ConflictMergeResult> MergeConflictAsync(
        SyncConflict conflict,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Merging conflict {Target}", conflict.ConflictTarget);

        // LOGIC: Check license for merge capability.
        if (_licenseContext.Tier < LicenseTier.Teams)
        {
            return new ConflictMergeResult
            {
                Success = false,
                DocumentValue = conflict.DocumentValue,
                GraphValue = conflict.GraphValue,
                ErrorMessage = "Merge requires Teams license or higher",
                UsedStrategy = MergeStrategy.RequiresManualMerge,
                MergeType = MergeType.Manual
            };
        }

        var context = new MergeContext
        {
            ConflictType = conflict.Type
        };

        var mergeResult = await _merger.MergeAsync(
            conflict.DocumentValue,
            conflict.GraphValue,
            context,
            ct);

        return ConflictMergeResult.FromMergeResult(
            mergeResult,
            conflict.DocumentValue,
            conflict.GraphValue,
            $"Merged using {mergeResult.UsedStrategy}");
    }

    /// <summary>
    /// Gets all unresolved conflicts for a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of unresolved conflicts.</returns>
    public Task<IReadOnlyList<SyncConflict>> GetUnresolvedConflictsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        var conflicts = _conflictStore.GetUnresolvedForDocument(documentId);
        return Task.FromResult(conflicts);
    }

    /// <summary>
    /// Resolves all conflicts for a document using options.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="options">Resolution options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="SyncResult"/> with resolution outcome.</returns>
    public async Task<SyncResult> ResolveAllAsync(
        Guid documentId,
        ConflictResolutionOptions? options,
        CancellationToken ct = default)
    {
        var resolveOptions = options ?? ConflictResolutionOptions.Default;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Resolving all conflicts for document {DocumentId}",
            documentId);

        try
        {
            var conflicts = _conflictStore.GetUnresolvedForDocument(documentId);

            if (conflicts.Count == 0)
            {
                return new SyncResult
                {
                    Status = SyncOperationStatus.NoChanges,
                    Duration = stopwatch.Elapsed
                };
            }

            var resolvedCount = 0;
            var failedCount = 0;
            var remainingConflicts = new List<SyncConflict>();

            foreach (var conflict in conflicts)
            {
                ct.ThrowIfCancellationRequested();

                // LOGIC: Check if auto-resolution is allowed for this severity.
                if (!resolveOptions.CanAutoResolve(conflict.Severity))
                {
                    remainingConflicts.Add(conflict);
                    continue;
                }

                // LOGIC: Get strategy for this conflict type.
                var strategy = resolveOptions.GetStrategy(conflict.Type);

                var result = await ResolveConflictAsync(conflict, strategy, ct);

                if (result.Succeeded)
                {
                    resolvedCount++;
                }
                else
                {
                    failedCount++;
                    remainingConflicts.Add(conflict);
                }
            }

            stopwatch.Stop();

            var status = remainingConflicts.Count == 0
                ? SyncOperationStatus.Success
                : resolvedCount > 0
                    ? SyncOperationStatus.SuccessWithConflicts
                    : SyncOperationStatus.Failed;

            _logger.LogInformation(
                "Conflict resolution completed: Resolved={Resolved}, Failed={Failed}, Remaining={Remaining}",
                resolvedCount, failedCount, remainingConflicts.Count);

            return new SyncResult
            {
                Status = status,
                Conflicts = remainingConflicts,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Conflict resolution cancelled for document {DocumentId}", documentId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Conflict resolution failed for document {DocumentId}",
                documentId);

            return new SyncResult
            {
                Status = SyncOperationStatus.Failed,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Checks if the current license tier supports resolving conflicts.
    /// </summary>
    private bool CanResolve(ConflictSeverity severity, ConflictResolutionStrategy strategy)
    {
        var tier = _licenseContext.Tier;

        // LOGIC: Core tier cannot resolve conflicts.
        if (tier == LicenseTier.Core)
        {
            return false;
        }

        // LOGIC: WriterPro can resolve Low severity and Manual only.
        if (tier == LicenseTier.WriterPro)
        {
            if (severity > ConflictSeverity.Low && strategy != ConflictResolutionStrategy.Manual)
            {
                return false;
            }
            if (strategy == ConflictResolutionStrategy.Merge)
            {
                return false;
            }
        }

        // LOGIC: Teams can resolve Low/Medium and use Merge.
        if (tier == LicenseTier.Teams)
        {
            if (severity == ConflictSeverity.High && strategy != ConflictResolutionStrategy.Manual)
            {
                return false;
            }
        }

        // LOGIC: Enterprise can resolve all.
        return true;
    }

    /// <summary>
    /// Resolves conflict using document value.
    /// </summary>
    private Task<ConflictResolutionResult> ResolveWithDocumentValueAsync(
        SyncConflict conflict,
        CancellationToken ct)
    {
        var result = ConflictResolutionResult.Success(
            conflict,
            ConflictResolutionStrategy.UseDocument,
            conflict.DocumentValue,
            isAutomatic: true);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Resolves conflict using graph value.
    /// </summary>
    private ConflictResolutionResult ResolveWithGraphValue(SyncConflict conflict)
    {
        return ConflictResolutionResult.Success(
            conflict,
            ConflictResolutionStrategy.UseGraph,
            conflict.GraphValue,
            isAutomatic: true);
    }

    /// <summary>
    /// Resolves conflict using merge.
    /// </summary>
    private async Task<ConflictResolutionResult> ResolveWithMergeAsync(
        SyncConflict conflict,
        CancellationToken ct)
    {
        var mergeResult = await MergeConflictAsync(conflict, ct);

        if (mergeResult.Success)
        {
            return ConflictResolutionResult.Success(
                conflict,
                ConflictResolutionStrategy.Merge,
                mergeResult.MergedValue,
                isAutomatic: true);
        }
        else
        {
            return ConflictResolutionResult.Failure(
                conflict,
                ConflictResolutionStrategy.Merge,
                mergeResult.ErrorMessage ?? "Merge failed");
        }
    }

    /// <summary>
    /// Publishes a conflict resolved event.
    /// </summary>
    private async Task PublishResolvedEventAsync(
        SyncConflict conflict,
        ConflictResolutionResult result,
        CancellationToken ct)
    {
        try
        {
            var evt = ConflictResolvedEvent.Create(conflict, result);
            await _mediator.Publish(evt, ct);

            _logger.LogDebug(
                "Published ConflictResolvedEvent for {Target}",
                conflict.ConflictTarget);
        }
        catch (Exception ex)
        {
            // LOGIC: Don't fail resolution if event publishing fails.
            _logger.LogWarning(ex,
                "Failed to publish ConflictResolvedEvent for {Target}",
                conflict.ConflictTarget);
        }
    }
}
