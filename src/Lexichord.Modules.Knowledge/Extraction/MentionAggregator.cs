// =============================================================================
// File: MentionAggregator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Aggregates entity mentions into unique deduplicated entities.
// =============================================================================
// LOGIC: Groups EntityMention instances by (type, normalized value) to produce
//   AggregatedEntity records suitable for graph storage. The aggregation
//   process:
//   1. Groups by "{EntityType}::{NormalizedValue}" (case-insensitive).
//   2. Selects highest-confidence mention as the canonical representative.
//   3. Merges properties from all mentions (first-seen wins per key).
//   4. Collects source document/chunk IDs for provenance.
//   5. Orders results by mention count (descending), then confidence.
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: EntityMention, AggregatedEntity (v0.4.5g)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Knowledge.Extraction;

/// <summary>
/// Aggregates entity mentions into unique deduplicated entities.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MentionAggregator"/> groups <see cref="EntityMention"/>
/// instances that refer to the same entity (same type and normalized value)
/// into <see cref="AggregatedEntity"/> records. This is essential for creating
/// knowledge graph nodes without duplicates when an entity is mentioned
/// multiple times across a document or corpus.
/// </para>
/// <para>
/// <b>Grouping Key:</b> Mentions are grouped by a composite key of
/// <c>{EntityType}::{NormalizedValue}</c> (case-insensitive). When
/// <see cref="EntityMention.NormalizedValue"/> is null, the raw
/// <see cref="EntityMention.Value"/> is used.
/// </para>
/// <para>
/// <b>Property Merging:</b> Properties from all mentions in a group are
/// merged using first-seen-wins semantics. The mentions are processed in
/// descending confidence order, so the most confident mention's properties
/// take precedence.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
internal sealed class MentionAggregator
{
    /// <summary>
    /// Aggregates a collection of mentions into unique entities.
    /// </summary>
    /// <param name="mentions">Mentions to aggregate. May be empty.</param>
    /// <returns>
    /// A read-only list of <see cref="AggregatedEntity"/> records, ordered
    /// by mention count (descending) then by maximum confidence (descending).
    /// Returns an empty list if no mentions are provided.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Aggregation steps:
    /// <list type="number">
    ///   <item><description>Group mentions by <c>{EntityType}::{NormalizedValue}</c> (case-insensitive).</description></item>
    ///   <item><description>For each group, select the highest-confidence mention as the canonical representative.</description></item>
    ///   <item><description>Merge properties from all mentions (first-seen wins per key, ordered by confidence).</description></item>
    ///   <item><description>Collect distinct chunk IDs for provenance tracking.</description></item>
    ///   <item><description>Order groups by mention count descending, then max confidence descending.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public IReadOnlyList<AggregatedEntity> Aggregate(IEnumerable<EntityMention> mentions)
    {
        var grouped = mentions
            .GroupBy(m => GetGroupKey(m), StringComparer.OrdinalIgnoreCase)
            .Select(g => CreateAggregatedEntity(g))
            .OrderByDescending(e => e.Mentions.Count)
            .ThenByDescending(e => e.MaxConfidence)
            .ToList();

        return grouped;
    }

    /// <summary>
    /// Generates the grouping key for a mention.
    /// </summary>
    /// <param name="mention">The mention to generate a key for.</param>
    /// <returns>
    /// A composite key in the format <c>{EntityType}::{NormalizedValue}</c>.
    /// Falls back to <see cref="EntityMention.Value"/> when
    /// <see cref="EntityMention.NormalizedValue"/> is null.
    /// </returns>
    private static string GetGroupKey(EntityMention mention)
    {
        // LOGIC: Use normalized value for grouping to merge mentions of the
        // same entity that differ only in formatting (e.g., "/Users" vs "/users").
        return $"{mention.EntityType}::{mention.NormalizedValue ?? mention.Value}";
    }

    /// <summary>
    /// Creates an <see cref="AggregatedEntity"/> from a group of mentions.
    /// </summary>
    /// <param name="group">A group of mentions with the same entity type and normalized value.</param>
    /// <returns>An aggregated entity combining all mentions in the group.</returns>
    private static AggregatedEntity CreateAggregatedEntity(IGrouping<string, EntityMention> group)
    {
        var mentions = group.ToList();

        // LOGIC: Select the highest-confidence mention as the canonical representative.
        // Its normalized value (or value) becomes the canonical value for the entity.
        var bestMention = mentions.OrderByDescending(m => m.Confidence).First();

        // LOGIC: Merge properties from all mentions. Process in descending
        // confidence order so that the most confident mention's properties
        // take precedence (first-seen wins per key).
        var mergedProperties = new Dictionary<string, object>();
        foreach (var mention in mentions.OrderByDescending(m => m.Confidence))
        {
            foreach (var (key, value) in mention.Properties)
            {
                if (!mergedProperties.ContainsKey(key))
                {
                    mergedProperties[key] = value;
                }
            }
        }

        // LOGIC: Collect distinct chunk IDs for provenance tracking.
        // These link back to the document chunks where the entity was found.
        var sourceDocuments = mentions
            .Select(m => m.ChunkId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        return new AggregatedEntity
        {
            EntityType = bestMention.EntityType,
            CanonicalValue = bestMention.NormalizedValue ?? bestMention.Value,
            Mentions = mentions,
            MaxConfidence = mentions.Max(m => m.Confidence),
            MergedProperties = mergedProperties,
            SourceDocuments = sourceDocuments
        };
    }
}
