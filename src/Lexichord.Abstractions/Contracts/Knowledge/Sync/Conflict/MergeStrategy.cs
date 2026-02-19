// =============================================================================
// File: MergeStrategy.cs
// Project: Lexichord.Abstractions
// Description: Defines strategies for merging conflicting values during sync.
// =============================================================================
// LOGIC: When merging conflicting document and graph values, different
//   strategies determine which value wins or how they are combined.
//   The strategy is selected based on conflict type and configuration.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Strategy for merging conflicting values between document and graph.
/// </summary>
/// <remarks>
/// <para>
/// Determines how conflicting values are resolved during merge operations:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="DocumentFirst"/>: Use document value as-is.</description></item>
///   <item><description><see cref="GraphFirst"/>: Use graph value as-is.</description></item>
///   <item><description><see cref="Combine"/>: Combine/concatenate values intelligently.</description></item>
///   <item><description><see cref="MostRecent"/>: Use more recent value based on timestamps.</description></item>
///   <item><description><see cref="HighestConfidence"/>: Use value with higher confidence score.</description></item>
///   <item><description><see cref="RequiresManualMerge"/>: Requires manual selection by user.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public enum MergeStrategy
{
    /// <summary>
    /// Use document value as-is.
    /// </summary>
    /// <remarks>
    /// LOGIC: The document is treated as the authoritative source.
    /// Graph value is discarded in favor of the document value.
    /// Suitable when the document represents the latest intended state.
    /// </remarks>
    DocumentFirst = 0,

    /// <summary>
    /// Use graph value as-is.
    /// </summary>
    /// <remarks>
    /// LOGIC: The graph is treated as the authoritative source.
    /// Document value is discarded in favor of the graph value.
    /// Suitable when the graph has been curated or verified.
    /// </remarks>
    GraphFirst = 1,

    /// <summary>
    /// Combine/concatenate values intelligently.
    /// </summary>
    /// <remarks>
    /// LOGIC: Attempts to combine both values into a unified result.
    /// For collections, performs union. For text, may concatenate or merge.
    /// Suitable when both sources contain complementary information.
    /// </remarks>
    Combine = 2,

    /// <summary>
    /// Use more recent value based on timestamps.
    /// </summary>
    /// <remarks>
    /// LOGIC: Compares modification timestamps from both sources.
    /// The value with the more recent timestamp wins.
    /// Requires both sources to have reliable timestamp metadata.
    /// </remarks>
    MostRecent = 3,

    /// <summary>
    /// Use value with higher confidence score.
    /// </summary>
    /// <remarks>
    /// LOGIC: Compares confidence scores associated with each value.
    /// The value with higher confidence is selected.
    /// Useful when values have been extracted with confidence metrics.
    /// </remarks>
    HighestConfidence = 4,

    /// <summary>
    /// Requires manual selection by user.
    /// </summary>
    /// <remarks>
    /// LOGIC: Automatic merge is not possible or not appropriate.
    /// The conflict is flagged for manual user intervention.
    /// Used for complex conflicts or when confidence is too low.
    /// </remarks>
    RequiresManualMerge = 5
}
