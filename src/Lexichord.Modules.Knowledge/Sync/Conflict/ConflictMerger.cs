// =============================================================================
// File: ConflictMerger.cs
// Project: Lexichord.Modules.Knowledge
// Description: Intelligently merges conflicted values using various strategies.
// =============================================================================
// LOGIC: ConflictMerger provides intelligent value merging capabilities.
//   It selects appropriate merge strategies based on conflict type and
//   context, then executes the merge to produce a unified value.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: IConflictMerger, MergeResult, MergeContext, MergeStrategy,
//               MergeType (v0.7.6h), ConflictType (v0.7.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Service for intelligently merging conflicted values.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IConflictMerger"/> with multiple merge strategies:
/// </para>
/// <list type="bullet">
///   <item><see cref="MergeStrategy.DocumentFirst"/>: Use document value.</item>
///   <item><see cref="MergeStrategy.GraphFirst"/>: Use graph value.</item>
///   <item><see cref="MergeStrategy.Combine"/>: Combine values intelligently.</item>
///   <item><see cref="MergeStrategy.MostRecent"/>: Use most recent value.</item>
///   <item><see cref="MergeStrategy.HighestConfidence"/>: Use highest confidence value.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public sealed class ConflictMerger : IConflictMerger
{
    private readonly ILogger<ConflictMerger> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictMerger"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ConflictMerger(ILogger<ConflictMerger> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<MergeResult> MergeAsync(
        object documentValue,
        object graphValue,
        MergeContext context,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Merging values: Document='{DocValue}', Graph='{GraphValue}'",
            documentValue, graphValue);

        try
        {
            // LOGIC: Determine merge strategy from context.
            var strategy = context.ConflictType.HasValue
                ? GetMergeStrategy(context.ConflictType.Value)
                : MergeStrategy.MostRecent;

            // LOGIC: Check if preferred strategy is in context data.
            if (context.ContextData.TryGetValue("PreferredStrategy", out var preferredObj) &&
                preferredObj is MergeStrategy preferred)
            {
                strategy = preferred;
            }

            var result = strategy switch
            {
                MergeStrategy.DocumentFirst => MergeDocumentFirst(documentValue),
                MergeStrategy.GraphFirst => MergeGraphFirst(graphValue),
                MergeStrategy.Combine => MergeCombine(documentValue, graphValue),
                MergeStrategy.MostRecent => MergeMostRecent(documentValue, graphValue, context),
                MergeStrategy.HighestConfidence => MergeHighestConfidence(documentValue, graphValue, context),
                MergeStrategy.RequiresManualMerge => MergeResult.RequiresManual("Automatic merge not supported for this conflict"),
                _ => MergeResult.RequiresManual($"Unknown strategy: {strategy}")
            };

            _logger.LogDebug(
                "Merge completed: Success={Success}, Strategy={Strategy}",
                result.Success, result.UsedStrategy);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge operation failed");
            return Task.FromResult(MergeResult.Failed(ex.Message));
        }
    }

    /// <inheritdoc/>
    public MergeStrategy GetMergeStrategy(ConflictType conflictType)
    {
        // LOGIC: Select strategy based on conflict type.
        return conflictType switch
        {
            ConflictType.ValueMismatch => MergeStrategy.MostRecent,
            ConflictType.MissingInGraph => MergeStrategy.DocumentFirst,
            ConflictType.MissingInDocument => MergeStrategy.RequiresManualMerge,
            ConflictType.RelationshipMismatch => MergeStrategy.RequiresManualMerge,
            ConflictType.ConcurrentEdit => MergeStrategy.RequiresManualMerge,
            _ => MergeStrategy.RequiresManualMerge
        };
    }

    /// <summary>
    /// Merges using DocumentFirst strategy.
    /// </summary>
    private MergeResult MergeDocumentFirst(object documentValue)
    {
        return new MergeResult
        {
            Success = true,
            MergedValue = documentValue,
            UsedStrategy = MergeStrategy.DocumentFirst,
            Confidence = 1.0f,
            MergeType = MergeType.Selection
        };
    }

    /// <summary>
    /// Merges using GraphFirst strategy.
    /// </summary>
    private MergeResult MergeGraphFirst(object graphValue)
    {
        return new MergeResult
        {
            Success = true,
            MergedValue = graphValue,
            UsedStrategy = MergeStrategy.GraphFirst,
            Confidence = 1.0f,
            MergeType = MergeType.Selection
        };
    }

    /// <summary>
    /// Merges by combining values intelligently.
    /// </summary>
    private MergeResult MergeCombine(object documentValue, object graphValue)
    {
        // LOGIC: Attempt intelligent combination based on value types.

        // Handle string concatenation
        if (documentValue is string docStr && graphValue is string graphStr)
        {
            // LOGIC: If one contains the other, use the longer one.
            if (docStr.Contains(graphStr, StringComparison.OrdinalIgnoreCase))
            {
                return new MergeResult
                {
                    Success = true,
                    MergedValue = docStr,
                    UsedStrategy = MergeStrategy.Combine,
                    Confidence = 0.9f,
                    MergeType = MergeType.Intelligent
                };
            }

            if (graphStr.Contains(docStr, StringComparison.OrdinalIgnoreCase))
            {
                return new MergeResult
                {
                    Success = true,
                    MergedValue = graphStr,
                    UsedStrategy = MergeStrategy.Combine,
                    Confidence = 0.9f,
                    MergeType = MergeType.Intelligent
                };
            }

            // LOGIC: Concatenate with separator if different.
            var combined = $"{docStr}; {graphStr}";
            return new MergeResult
            {
                Success = true,
                MergedValue = combined,
                UsedStrategy = MergeStrategy.Combine,
                Confidence = 0.7f,
                MergeType = MergeType.Intelligent
            };
        }

        // Handle numeric averaging
        if (IsNumeric(documentValue) && IsNumeric(graphValue))
        {
            try
            {
                var docNum = Convert.ToDouble(documentValue);
                var graphNum = Convert.ToDouble(graphValue);
                var average = (docNum + graphNum) / 2.0;

                return new MergeResult
                {
                    Success = true,
                    MergedValue = average,
                    UsedStrategy = MergeStrategy.Combine,
                    Confidence = 0.8f,
                    MergeType = MergeType.Weighted
                };
            }
            catch
            {
                // Fall through to default
            }
        }

        // LOGIC: Cannot combine automatically - require manual merge.
        return MergeResult.RequiresManual("Cannot automatically combine these value types");
    }

    /// <summary>
    /// Merges using MostRecent strategy based on timestamps.
    /// </summary>
    private MergeResult MergeMostRecent(object documentValue, object graphValue, MergeContext context)
    {
        // LOGIC: Compare timestamps from entity and document if available.
        var docModified = context.Document?.IndexedAt;
        var graphModified = context.Entity?.ModifiedAt;

        if (docModified.HasValue && graphModified.HasValue)
        {
            // LOGIC: Convert DateTime to DateTimeOffset for comparison.
            var docOffset = new DateTimeOffset(docModified.Value, TimeSpan.Zero);

            if (docOffset > graphModified.Value)
            {
                return new MergeResult
                {
                    Success = true,
                    MergedValue = documentValue,
                    UsedStrategy = MergeStrategy.MostRecent,
                    Confidence = 0.9f,
                    MergeType = MergeType.Temporal
                };
            }
            else
            {
                return new MergeResult
                {
                    Success = true,
                    MergedValue = graphValue,
                    UsedStrategy = MergeStrategy.MostRecent,
                    Confidence = 0.9f,
                    MergeType = MergeType.Temporal
                };
            }
        }

        // LOGIC: If no timestamps, default to document value (assumed more recent).
        _logger.LogDebug("No timestamps available, defaulting to document value");
        return new MergeResult
        {
            Success = true,
            MergedValue = documentValue,
            UsedStrategy = MergeStrategy.MostRecent,
            Confidence = 0.6f, // Lower confidence without timestamps
            MergeType = MergeType.Temporal
        };
    }

    /// <summary>
    /// Merges using HighestConfidence strategy.
    /// </summary>
    private MergeResult MergeHighestConfidence(object documentValue, object graphValue, MergeContext context)
    {
        // LOGIC: Check if confidence scores are in context data.
        var docConfidence = 0.5f;
        var graphConfidence = 0.5f;

        if (context.ContextData.TryGetValue("DocumentConfidence", out var docConfObj) &&
            docConfObj is float docConf)
        {
            docConfidence = docConf;
        }

        if (context.ContextData.TryGetValue("GraphConfidence", out var graphConfObj) &&
            graphConfObj is float graphConf)
        {
            graphConfidence = graphConf;
        }

        // LOGIC: Also check entity properties for confidence.
        if (context.Entity?.Properties.TryGetValue("Confidence", out var entityConfObj) == true &&
            entityConfObj is float entityConf)
        {
            graphConfidence = entityConf;
        }

        if (docConfidence >= graphConfidence)
        {
            return new MergeResult
            {
                Success = true,
                MergedValue = documentValue,
                UsedStrategy = MergeStrategy.HighestConfidence,
                Confidence = docConfidence,
                MergeType = MergeType.Weighted
            };
        }
        else
        {
            return new MergeResult
            {
                Success = true,
                MergedValue = graphValue,
                UsedStrategy = MergeStrategy.HighestConfidence,
                Confidence = graphConfidence,
                MergeType = MergeType.Weighted
            };
        }
    }

    /// <summary>
    /// Checks if a value is numeric.
    /// </summary>
    private static bool IsNumeric(object value)
    {
        return value is int or long or float or double or decimal
            or short or byte or sbyte or uint or ulong or ushort;
    }
}
