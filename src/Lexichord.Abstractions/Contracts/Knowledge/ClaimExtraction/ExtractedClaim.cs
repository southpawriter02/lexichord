// =============================================================================
// File: ExtractedClaim.cs
// Project: Lexichord.Abstractions
// Description: A claim extracted before post-processing.
// =============================================================================
// LOGIC: Represents a raw extracted claim before entity linking and confidence
//   adjustment. Used as intermediate representation between extractors and
//   the final Claim record.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Parsing;

namespace Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;

/// <summary>
/// A claim extracted before post-processing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Intermediate representation of a claim during extraction.
/// Contains raw text spans and extraction metadata before entity linking
/// and confidence scoring produce the final <see cref="Claim"/> record.
/// </para>
/// <para>
/// <b>Workflow:</b>
/// <list type="number">
/// <item><description>Extractor produces ExtractedClaim with text spans.</description></item>
/// <item><description>Service links spans to entities (if available).</description></item>
/// <item><description>Confidence scorer adjusts raw confidence.</description></item>
/// <item><description>Converted to final Claim record.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6g as part of the Claim Extractor.
/// </para>
/// </remarks>
public record ExtractedClaim
{
    /// <summary>
    /// Gets the text span for the claim subject.
    /// </summary>
    public required TextSpan SubjectSpan { get; init; }

    /// <summary>
    /// Gets the text span for the claim object.
    /// </summary>
    public required TextSpan ObjectSpan { get; init; }

    /// <summary>
    /// Gets the predicate for this claim.
    /// </summary>
    /// <value>Should match a value from <see cref="ClaimPredicate"/>.</value>
    public required string Predicate { get; init; }

    /// <summary>
    /// Gets the extraction method used.
    /// </summary>
    public required ClaimExtractionMethod Method { get; init; }

    /// <summary>
    /// Gets the ID of the pattern that matched (if pattern-based).
    /// </summary>
    public string? PatternId { get; init; }

    /// <summary>
    /// Gets the raw confidence before adjustments.
    /// </summary>
    /// <value>A value between 0 and 1.</value>
    public float RawConfidence { get; init; }

    /// <summary>
    /// Gets the source sentence.
    /// </summary>
    public required ParsedSentence Sentence { get; init; }

    /// <summary>
    /// Gets the matched linked entity for subject (if available).
    /// </summary>
    public LinkedEntity? SubjectEntity { get; init; }

    /// <summary>
    /// Gets the matched linked entity for object (if available).
    /// </summary>
    public LinkedEntity? ObjectEntity { get; init; }

    /// <summary>
    /// Gets the literal value if object is a literal.
    /// </summary>
    public object? LiteralValue { get; init; }

    /// <summary>
    /// Gets the type of literal (e.g., "int", "bool", "string").
    /// </summary>
    public string? LiteralType { get; init; }

    /// <summary>
    /// Gets whether the object is a literal value.
    /// </summary>
    public bool IsLiteral => LiteralValue != null;
}

/// <summary>
/// Represents an entity that has been linked to the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Temporary placeholder for entity linking results.
/// In production, this would come from the Entity Linking service (v0.5.5g).
/// </para>
/// <para>
/// <b>Note:</b> This is a simplified version for v0.5.6g. Full entity
/// linking will be implemented in a later version.
/// </para>
/// </remarks>
public record LinkedEntity
{
    /// <summary>
    /// Gets the mention that was linked.
    /// </summary>
    public required EntityMention Mention { get; init; }

    /// <summary>
    /// Gets the resolved entity from the knowledge graph.
    /// </summary>
    public ResolvedEntity? ResolvedEntity { get; init; }

    /// <summary>
    /// Gets the linking confidence score.
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Gets whether the entity was successfully resolved.
    /// </summary>
    public bool IsResolved => ResolvedEntity != null;
}

/// <summary>
/// Represents a mention of an entity in text.
/// </summary>
public record EntityMention
{
    /// <summary>
    /// Gets the text value of the mention.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the entity type (e.g., "Endpoint", "Parameter").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the start character offset.
    /// </summary>
    public int StartOffset { get; init; }

    /// <summary>
    /// Gets the end character offset.
    /// </summary>
    public int EndOffset { get; init; }
}

/// <summary>
/// Represents an entity resolved from the knowledge graph.
/// </summary>
public record ResolvedEntity
{
    /// <summary>
    /// Gets the entity ID in the knowledge graph.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the canonical name of the entity.
    /// </summary>
    public required string CanonicalName { get; init; }

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public required string EntityType { get; init; }
}
