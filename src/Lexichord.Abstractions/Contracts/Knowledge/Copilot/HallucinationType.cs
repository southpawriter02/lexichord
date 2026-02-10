// =============================================================================
// File: HallucinationType.cs
// Project: Lexichord.Abstractions
// Description: Classifies the type of hallucination detected in content.
// =============================================================================
// LOGIC: Categorises hallucinations so that the validator can apply different
//   confidence thresholds and fix strategies per type. UnknownEntity is the
//   most common; ContradictoryValue is the most actionable.
//
// v0.6.6g: Post-Generation Validator (CKVS Phase 3b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Classifies the type of hallucination detected in generated content.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="HallucinationFinding"/> carries a <see cref="HallucinationType"/>
/// so that the validator can apply type-specific handling:
/// </para>
/// <list type="bullet">
///   <item><see cref="UnknownEntity"/> — entity not found in context.</item>
///   <item><see cref="ContradictoryValue"/> — property value contradicts context.</item>
///   <item><see cref="UnsupportedRelationship"/> — relationship not in context.</item>
///   <item><see cref="UnverifiableFact"/> — claim cannot be verified from context.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.6g as part of the Post-Generation Validator.
/// </para>
/// </remarks>
public enum HallucinationType
{
    /// <summary>
    /// Entity mentioned in content but not present in the knowledge context.
    /// </summary>
    /// <remarks>
    /// The detector uses Levenshtein distance to suggest the closest match
    /// from the context when the distance is ≤ 3 characters.
    /// </remarks>
    UnknownEntity,

    /// <summary>
    /// Property value in content contradicts the value in context.
    /// </summary>
    /// <remarks>
    /// Detected via regex pattern matching against known entity properties.
    /// A suggested correction with the correct value is provided.
    /// </remarks>
    ContradictoryValue,

    /// <summary>
    /// Relationship asserted in content is not present in context.
    /// </summary>
    UnsupportedRelationship,

    /// <summary>
    /// A factual claim that cannot be verified from the available context.
    /// </summary>
    UnverifiableFact
}
