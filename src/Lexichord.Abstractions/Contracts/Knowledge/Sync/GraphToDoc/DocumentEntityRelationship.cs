// =============================================================================
// File: DocumentEntityRelationship.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of how a document can reference a knowledge graph entity.
// =============================================================================
// LOGIC: Categorizes the relationship between a document and a graph entity
//   to determine impact assessment and suggest appropriate review actions.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Describes how a document references a knowledge graph entity.
/// </summary>
/// <remarks>
/// <para>
/// When a graph entity changes, documents referencing it may need review.
/// The relationship type determines:
/// </para>
/// <list type="bullet">
///   <item><b>Impact severity:</b> Explicit references have higher impact than implicit.</item>
///   <item><b>Suggested actions:</b> DerivedFrom may need content updates.</item>
///   <item><b>Review priority:</b> Direct references typically need immediate review.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public enum DocumentEntityRelationship
{
    /// <summary>
    /// Document explicitly references the entity by name or identifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: Direct, intentional references extracted from document text.
    /// These have the highest confidence and typically require immediate
    /// review when the entity changes.
    /// Examples: "See the ProductX API", "Using AuthService v2".
    /// </remarks>
    ExplicitReference = 0,

    /// <summary>
    /// Document implicitly references the entity via text similarity.
    /// </summary>
    /// <remarks>
    /// LOGIC: References detected through semantic analysis rather than
    /// exact text matching. Lower confidence than explicit references.
    /// May represent concepts related to but not directly naming the entity.
    /// Examples: Discussing similar functionality without naming the service.
    /// </remarks>
    ImplicitReference = 1,

    /// <summary>
    /// The entity was derived from this document's content.
    /// </summary>
    /// <remarks>
    /// LOGIC: The document is the source of the entity, created during
    /// doc-to-graph sync. Changes to the entity may indicate the document
    /// is outdated or needs to be resynchronized.
    /// Examples: Entity extracted from this document in a previous sync.
    /// </remarks>
    DerivedFrom = 2,

    /// <summary>
    /// Document and entity both reference common sources or concepts.
    /// </summary>
    /// <remarks>
    /// LOGIC: Indirect relationship through shared context or sources.
    /// Lowest confidence level; may not require immediate action.
    /// Used for surfacing potentially related content during review.
    /// Examples: Both reference the same external specification.
    /// </remarks>
    IndirectReference = 3
}
