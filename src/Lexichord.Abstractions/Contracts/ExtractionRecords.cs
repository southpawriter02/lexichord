// =============================================================================
// File: ExtractionRecords.cs
// Project: Lexichord.Abstractions
// Description: Domain records for entity extraction from document text.
// =============================================================================
// LOGIC: Defines the core data transfer records for the entity extraction
//   pipeline. These records carry extraction inputs (context), outputs
//   (mentions), and aggregated results between the extraction layer and
//   consumers (graph storage, UI display).
//
// Records defined:
//   - EntityMention: A single mention of an entity found in text.
//   - ExtractionContext: Configuration and metadata for an extraction operation.
//   - ExtractionResult: Aggregated output of an extraction pipeline run.
//   - AggregatedEntity: Deduplicated entity from multiple mentions.
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: ISchemaRegistry (v0.4.5f), TextChunk (v0.4.3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// A mention of an entity found in text by an <see cref="IEntityExtractor"/>.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="EntityMention"/> represents a single occurrence of an entity
/// detected in source text. Each mention carries the entity type, extracted
/// value, position information, and a confidence score indicating extraction
/// quality.
/// </para>
/// <para>
/// <b>Confidence Scoring:</b>
/// <list type="bullet">
///   <item><description>1.0: High confidence — explicit pattern match (e.g., <c>GET /users</c>).</description></item>
///   <item><description>0.8–0.9: Medium-high — strong heuristic (e.g., code block definition).</description></item>
///   <item><description>0.6–0.8: Medium — contextual match (e.g., standalone path).</description></item>
///   <item><description>Below 0.6: Low — requires human review.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Normalization:</b> The <see cref="NormalizedValue"/> provides a canonical
/// form of the extracted value for grouping and deduplication. For example,
/// endpoint paths are lowercased with query parameters stripped.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mention = new EntityMention
/// {
///     EntityType = "Endpoint",
///     Value = "GET /api/users",
///     NormalizedValue = "/api/users",
///     StartOffset = 42,
///     EndOffset = 56,
///     Confidence = 1.0f,
///     Properties = new() { ["method"] = "GET", ["path"] = "/api/users" }
/// };
/// </code>
/// </example>
public record EntityMention
{
    /// <summary>
    /// The entity type (must match a registered schema type, e.g., "Endpoint", "Parameter", "Concept").
    /// </summary>
    /// <value>
    /// A non-null string identifying the entity type. Validated against the
    /// Schema Registry (v0.4.5f) when storing entities in the knowledge graph.
    /// </value>
    public required string EntityType { get; init; }

    /// <summary>
    /// The extracted text value (e.g., "GET /users", "userId", "Rate Limiting").
    /// </summary>
    /// <value>
    /// The raw text value as extracted from the source document.
    /// </value>
    public required string Value { get; init; }

    /// <summary>
    /// Normalized/canonical form of the value for grouping and deduplication.
    /// </summary>
    /// <value>
    /// A normalized representation of the value. For example, endpoint paths
    /// are lowercased with query parameters stripped. May be <c>null</c> if
    /// normalization is not applicable.
    /// </value>
    /// <remarks>
    /// LOGIC: Used by <see cref="MentionAggregator"/> to group mentions of the
    /// same entity across different chunks. Falls back to <see cref="Value"/>
    /// when <c>null</c>.
    /// </remarks>
    public string? NormalizedValue { get; init; }

    /// <summary>
    /// Character offset where the mention starts in the source text.
    /// </summary>
    /// <value>Zero-based character position from the start of the analyzed text.</value>
    public int StartOffset { get; init; }

    /// <summary>
    /// Character offset where the mention ends in the source text (exclusive).
    /// </summary>
    /// <value>Character position immediately after the last character of the mention.</value>
    public int EndOffset { get; init; }

    /// <summary>
    /// Confidence score for this extraction (0.0 to 1.0).
    /// </summary>
    /// <value>
    /// A float between 0.0 and 1.0, defaulting to 1.0. Higher values indicate
    /// greater confidence in the extraction quality. Mentions below the
    /// <see cref="ExtractionContext.MinConfidence"/> threshold are filtered out.
    /// </value>
    public float Confidence { get; init; } = 1.0f;

    /// <summary>
    /// Additional properties extracted alongside the entity mention.
    /// </summary>
    /// <value>
    /// A mutable dictionary of property name-value pairs. Common keys include
    /// "method" and "path" for endpoints, "name" and "location" for parameters,
    /// "name" and "definition" for concepts.
    /// </value>
    /// <remarks>
    /// LOGIC: Properties are merged during aggregation. When multiple mentions
    /// of the same entity provide different properties, first-seen values take
    /// precedence per key.
    /// </remarks>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// Source chunk ID (if extracted from chunked content).
    /// </summary>
    /// <value>
    /// The GUID of the chunk from which this mention was extracted, or <c>null</c>
    /// if extracted from non-chunked text.
    /// </value>
    public Guid? ChunkId { get; init; }

    /// <summary>
    /// Name of the extractor that found this mention.
    /// </summary>
    /// <value>
    /// The simple class name of the <see cref="IEntityExtractor"/> that produced
    /// this mention (e.g., "EndpointExtractor"). Set by the pipeline, not
    /// by individual extractors.
    /// </value>
    public string? ExtractorName { get; init; }

    /// <summary>
    /// The original text snippet containing the mention with surrounding context.
    /// </summary>
    /// <value>
    /// A short text excerpt showing the mention in its original context.
    /// Useful for display in the Entity Browser (v0.4.7) and for debugging.
    /// </value>
    public string? SourceSnippet { get; init; }

    /// <summary>
    /// Gets the mention span length in characters.
    /// </summary>
    /// <value>
    /// The difference between <see cref="EndOffset"/> and <see cref="StartOffset"/>.
    /// </value>
    public int Length => EndOffset - StartOffset;
}

/// <summary>
/// Context and configuration for an entity extraction operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ExtractionContext"/> provides metadata about the source text
/// being analyzed and configuration for how extraction should be performed.
/// It is passed to every <see cref="IEntityExtractor.ExtractAsync"/> invocation.
/// </para>
/// <para>
/// <b>Filtering:</b> Use <see cref="TargetEntityTypes"/> to limit extraction
/// to specific entity types, and <see cref="MinConfidence"/> to filter out
/// low-confidence results.
/// </para>
/// <para>
/// <b>Discovery Mode:</b> When <see cref="DiscoveryMode"/> is <c>true</c>,
/// extractors may use lower thresholds and broader patterns to find more
/// potential entities. This is useful for initial corpus analysis.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
public record ExtractionContext
{
    /// <summary>
    /// Source document ID for provenance tracking.
    /// </summary>
    /// <value>
    /// The GUID of the document being analyzed, or <c>null</c> if the text
    /// is not associated with a specific document.
    /// </value>
    public Guid? DocumentId { get; init; }

    /// <summary>
    /// Source chunk ID within the document.
    /// </summary>
    /// <value>
    /// The GUID of the specific chunk being analyzed, or <c>null</c> if
    /// analyzing a full document or non-chunked text.
    /// </value>
    public Guid? ChunkId { get; init; }

    /// <summary>
    /// Document title for contextual enrichment.
    /// </summary>
    /// <value>
    /// The title of the source document, or <c>null</c> if unknown.
    /// </value>
    public string? DocumentTitle { get; init; }

    /// <summary>
    /// Current heading context from Markdown structure.
    /// </summary>
    /// <value>
    /// The Markdown heading text that contains the text being analyzed
    /// (e.g., "Authentication Endpoints"). Provides section context for
    /// entity classification.
    /// </value>
    public string? Heading { get; init; }

    /// <summary>
    /// Heading level (1-6) from Markdown structure.
    /// </summary>
    /// <value>
    /// The heading level (1 for <c>#</c>, 6 for <c>######</c>), or <c>null</c>
    /// if no heading context applies.
    /// </value>
    public int? HeadingLevel { get; init; }

    /// <summary>
    /// Schema to validate extracted entity types against.
    /// </summary>
    /// <value>
    /// An <see cref="ISchemaRegistry"/> instance for validating that extracted
    /// entity types are registered, or <c>null</c> to skip validation.
    /// </value>
    /// <remarks>
    /// LOGIC: When provided, the extraction pipeline can validate extracted
    /// entity types against the schema before returning results. Invalid
    /// types are logged as warnings and excluded from results.
    /// </remarks>
    public ISchemaRegistry? Schema { get; init; }

    /// <summary>
    /// Minimum confidence threshold for inclusion in results.
    /// </summary>
    /// <value>
    /// A float between 0.0 and 1.0, defaulting to 0.5. Mentions with
    /// <see cref="EntityMention.Confidence"/> below this threshold are
    /// filtered out by the pipeline.
    /// </value>
    public float MinConfidence { get; init; } = 0.5f;

    /// <summary>
    /// Entity types to extract (null = all types).
    /// </summary>
    /// <value>
    /// A list of entity type names to target, or <c>null</c> to extract all
    /// supported types. Used to skip extractors that don't produce the
    /// requested types.
    /// </value>
    public IReadOnlyList<string>? TargetEntityTypes { get; init; }

    /// <summary>
    /// Whether extraction is in "discovery" mode with broader matching.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable discovery mode with lower confidence thresholds
    /// and broader patterns (e.g., capitalized multi-word terms as concepts).
    /// Defaults to <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Discovery mode is intended for initial corpus analysis where
    /// recall is preferred over precision. Extractors may enable additional
    /// patterns that would otherwise be suppressed.
    /// </remarks>
    public bool DiscoveryMode { get; init; } = false;
}

/// <summary>
/// Result of an entity extraction operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ExtractionResult"/> aggregates all entity mentions found
/// by the extraction pipeline, along with processing statistics and
/// deduplicated entity summaries.
/// </para>
/// <para>
/// <b>Aggregation:</b> The <see cref="AggregatedEntities"/> list provides
/// deduplicated entities by grouping mentions with the same type and
/// normalized value. This is useful for creating graph nodes without
/// duplicates.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
public record ExtractionResult
{
    /// <summary>
    /// All extracted entity mentions (after filtering and deduplication).
    /// </summary>
    /// <value>
    /// A read-only list of mentions sorted by position within the source text.
    /// Empty if no entities were found.
    /// </value>
    public required IReadOnlyList<EntityMention> Mentions { get; init; }

    /// <summary>
    /// Unique entities after aggregation across mentions.
    /// </summary>
    /// <value>
    /// A list of <see cref="AggregatedEntity"/> instances grouping mentions
    /// by type and canonical value. <c>null</c> if aggregation was not performed.
    /// </value>
    public IReadOnlyList<AggregatedEntity>? AggregatedEntities { get; init; }

    /// <summary>
    /// Total processing duration for the extraction operation.
    /// </summary>
    /// <value>
    /// The elapsed time from start to completion of the extraction pipeline run.
    /// </value>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of chunks processed in this extraction.
    /// </summary>
    /// <value>
    /// The count of <see cref="TextChunk"/> instances processed. 1 for
    /// single-text extraction, or the chunk count for
    /// <see cref="IEntityExtractionPipeline.ExtractFromChunksAsync"/>.
    /// </value>
    public int ChunksProcessed { get; init; }

    /// <summary>
    /// Extraction statistics broken down by entity type.
    /// </summary>
    /// <value>
    /// A dictionary mapping entity type names to the count of mentions
    /// found for that type. Empty if no mentions were found.
    /// </value>
    public Dictionary<string, int> MentionCountByType { get; init; } = new();

    /// <summary>
    /// Gets the average confidence score across all mentions.
    /// </summary>
    /// <value>
    /// The arithmetic mean of all mention confidence scores, or 0.0 if
    /// <see cref="Mentions"/> is empty.
    /// </value>
    public float AverageConfidence =>
        Mentions.Count > 0 ? Mentions.Average(m => m.Confidence) : 0f;
}

/// <summary>
/// An entity aggregated from multiple <see cref="EntityMention"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AggregatedEntity"/> represents a unique entity derived from
/// one or more mentions across different locations in the source text (or
/// across multiple chunks/documents). It provides a canonical representation
/// suitable for creating knowledge graph nodes.
/// </para>
/// <para>
/// <b>Canonical Value:</b> The <see cref="CanonicalValue"/> is derived from
/// the highest-confidence mention's <see cref="EntityMention.NormalizedValue"/>
/// (falling back to <see cref="EntityMention.Value"/>).
/// </para>
/// <para>
/// <b>Property Merging:</b> Properties from all mentions are merged using
/// first-seen-wins semantics per key. This ensures that the most confident
/// mention's properties take precedence.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
public record AggregatedEntity
{
    /// <summary>
    /// Entity type (e.g., "Endpoint", "Parameter", "Concept").
    /// </summary>
    /// <value>The type name shared by all grouped mentions.</value>
    public required string EntityType { get; init; }

    /// <summary>
    /// Canonical name/value for this entity.
    /// </summary>
    /// <value>
    /// The normalized value from the highest-confidence mention, used as
    /// the primary identifier for this entity in the knowledge graph.
    /// </value>
    public required string CanonicalValue { get; init; }

    /// <summary>
    /// All mentions of this entity in the source text.
    /// </summary>
    /// <value>
    /// A read-only list of all <see cref="EntityMention"/> instances that
    /// were grouped into this aggregated entity.
    /// </value>
    public required IReadOnlyList<EntityMention> Mentions { get; init; }

    /// <summary>
    /// Highest confidence score among all mentions.
    /// </summary>
    /// <value>
    /// The maximum <see cref="EntityMention.Confidence"/> value across
    /// all mentions in <see cref="Mentions"/>.
    /// </value>
    public float MaxConfidence { get; init; }

    /// <summary>
    /// Merged properties from all mentions (first-seen-wins per key).
    /// </summary>
    /// <value>
    /// A dictionary combining properties from all mentions. When multiple
    /// mentions provide the same key, the first-seen value (from the
    /// highest-confidence mention) is retained.
    /// </value>
    public Dictionary<string, object> MergedProperties { get; init; } = new();

    /// <summary>
    /// Document IDs where this entity appears.
    /// </summary>
    /// <value>
    /// A distinct list of chunk IDs from which this entity's mentions
    /// were extracted. Used for provenance tracking.
    /// </value>
    public IReadOnlyList<Guid> SourceDocuments { get; init; } = Array.Empty<Guid>();
}
