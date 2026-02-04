// =============================================================================
// File: RelationshipClassification.cs
// Project: Lexichord.Abstractions
// Description: Record representing the result of a relationship classification.
// =============================================================================
// VERSION: v0.5.9b (Relationship Classification)
// LOGIC: Contains the classification result with confidence and metadata.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the result of classifying the semantic relationship between two chunks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9b as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// The classification includes the determined relationship type, a confidence score,
/// an optional human-readable explanation, and the method used to derive the result.
/// </para>
/// </remarks>
/// <param name="Type">The determined semantic relationship type.</param>
/// <param name="Confidence">
/// Confidence score for the classification (0.0 to 1.0).
/// Higher values indicate greater certainty in the classification.
/// </param>
/// <param name="Explanation">
/// Optional human-readable explanation of why this classification was chosen.
/// Only populated when <see cref="ClassificationOptions.IncludeExplanation"/> is true.
/// </param>
/// <param name="Method">
/// The method used to perform the classification.
/// Defaults to <see cref="ClassificationMethod.RuleBased"/>.
/// </param>
public record RelationshipClassification(
    RelationshipType Type,
    float Confidence,
    string? Explanation,
    ClassificationMethod Method = ClassificationMethod.RuleBased);
