// =============================================================================
// File: FlagResolution.cs
// Project: Lexichord.Abstractions
// Description: Resolution options for document flags.
// =============================================================================
// LOGIC: Categorizes how a flag was resolved to track outcomes and
//   improve future sync suggestions.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Describes how a document flag was resolved.
/// </summary>
/// <remarks>
/// <para>
/// When a flag is resolved, the resolution type is recorded:
/// </para>
/// <list type="bullet">
///   <item><b>UpdatedWithGraphChanges:</b> Document was updated to match graph.</item>
///   <item><b>RejectedGraphChanges:</b> Graph changes were not applied to document.</item>
///   <item><b>ManualMerge:</b> User manually merged changes.</item>
///   <item><b>Dismissed:</b> Flag was dismissed as non-critical.</item>
/// </list>
/// <para>
/// Resolution data can be used to improve future sync suggestions
/// by learning user preferences.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public enum FlagResolution
{
    /// <summary>
    /// Document was updated to reflect the graph changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: User accepted the graph changes and updated the document
    /// accordingly. The document now reflects current graph state.
    /// </remarks>
    UpdatedWithGraphChanges = 0,

    /// <summary>
    /// Graph changes were rejected; document unchanged.
    /// </summary>
    /// <remarks>
    /// LOGIC: User determined the document content is correct and
    /// the graph changes should not be applied. May indicate the
    /// graph change was incorrect or document has different context.
    /// </remarks>
    RejectedGraphChanges = 1,

    /// <summary>
    /// User performed a manual merge of changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: User selectively applied some changes, combining
    /// document and graph content. Neither fully accepted nor rejected.
    /// </remarks>
    ManualMerge = 2,

    /// <summary>
    /// Flag was dismissed as non-critical.
    /// </summary>
    /// <remarks>
    /// LOGIC: User determined the flag does not require action.
    /// The change is not relevant to the document content or
    /// the impact is acceptable as-is.
    /// </remarks>
    Dismissed = 3
}
