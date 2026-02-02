// =============================================================================
// File: IEntityExtractor.cs
// Project: Lexichord.Abstractions
// Description: Interface for pluggable entity extractors that identify entity
//              mentions in text content.
// =============================================================================
// LOGIC: Defines the contract for individual entity extractors. Each extractor
//   specializes in identifying one or more entity types (e.g., Endpoint,
//   Parameter, Concept) using pattern matching, heuristics, or other
//   techniques. Extractors are registered in the IEntityExtractionPipeline
//   and executed in priority order.
//
// Key properties:
//   - SupportedTypes: Entity types this extractor can identify.
//   - Priority: Ordering hint (higher runs first) for pipeline execution.
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: EntityMention, ExtractionContext (v0.4.5g)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Extracts entity mentions from text content.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="IEntityExtractor"/> implementation specializes in identifying
/// one or more entity types using pattern matching or heuristic analysis. For
/// example, the <c>EndpointExtractor</c> identifies API endpoint patterns like
/// <c>GET /users/{id}</c>, while the <c>ParameterExtractor</c> identifies
/// parameter mentions in code and documentation.
/// </para>
/// <para>
/// <b>Pipeline Integration:</b> Extractors are registered with an
/// <see cref="IEntityExtractionPipeline"/> and executed in descending
/// <see cref="Priority"/> order. The pipeline handles error isolation,
/// confidence filtering, deduplication, and result aggregation.
/// </para>
/// <para>
/// <b>Confidence Scoring:</b> Each <see cref="EntityMention"/> returned
/// should include an appropriate confidence score:
/// <list type="bullet">
///   <item><description>1.0: Explicit, unambiguous match.</description></item>
///   <item><description>0.8–0.9: Strong heuristic match.</description></item>
///   <item><description>0.6–0.8: Contextual or partial match.</description></item>
///   <item><description>Below 0.6: Uncertain, may require human review.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Implementation Requirements:</b>
/// <list type="bullet">
///   <item><description>Must be stateless and thread-safe.</description></item>
///   <item><description>Must handle null/empty/whitespace text gracefully (return empty list).</description></item>
///   <item><description>Must not throw exceptions for invalid input — return empty results instead.</description></item>
///   <item><description>Should respect <see cref="ExtractionContext.DiscoveryMode"/> for broader matching.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class EndpointExtractor : IEntityExtractor
/// {
///     public IReadOnlyList&lt;string&gt; SupportedTypes => new[] { "Endpoint" };
///     public int Priority => 100;
///
///     public Task&lt;IReadOnlyList&lt;EntityMention&gt;&gt; ExtractAsync(
///         string text, ExtractionContext context, CancellationToken ct)
///     {
///         // Regex-based endpoint detection...
///     }
/// }
/// </code>
/// </example>
public interface IEntityExtractor
{
    /// <summary>
    /// Gets the entity types this extractor can identify.
    /// </summary>
    /// <value>
    /// A read-only list of entity type names (e.g., "Endpoint", "Parameter").
    /// Must match types registered in the Schema Registry (v0.4.5f).
    /// </value>
    IReadOnlyList<string> SupportedTypes { get; }

    /// <summary>
    /// Gets the priority for ordering extractors in the pipeline.
    /// </summary>
    /// <value>
    /// An integer where higher values run first. Default conventions:
    /// <list type="bullet">
    ///   <item><description>100: High priority (specific extractors like EndpointExtractor).</description></item>
    ///   <item><description>90: Medium-high (ParameterExtractor).</description></item>
    ///   <item><description>50: Medium (general extractors like ConceptExtractor).</description></item>
    /// </list>
    /// </value>
    int Priority { get; }

    /// <summary>
    /// Extracts entity mentions from text.
    /// </summary>
    /// <param name="text">Text content to analyze. May be empty or whitespace.</param>
    /// <param name="context">Extraction context with metadata and configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="EntityMention"/> instances found in the text.
    /// Returns an empty list if no entities are found or if the text is empty.
    /// </returns>
    Task<IReadOnlyList<EntityMention>> ExtractAsync(
        string text,
        ExtractionContext context,
        CancellationToken ct = default);
}
