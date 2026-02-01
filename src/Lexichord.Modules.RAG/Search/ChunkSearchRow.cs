// =============================================================================
// File: ChunkSearchRow.cs
// Project: Lexichord.Modules.RAG
// Description: Internal Dapper result mapping record for vector similarity
//              search queries against the chunks table.
// =============================================================================
// LOGIC: This record maps directly to the SQL result set from the pgvector
//   cosine similarity query in PgVectorSearchService. The Score property
//   is computed as 1 - cosine_distance (higher = more similar).
//
//   Column mappings use double-quoted aliases in SQL to match property names:
//     c.id AS "Id", c.document_id AS "DocumentId", etc.
//
//   The Metadata column maps to the JSONB column in the chunks table and is
//   deserialized to ChunkMetadata by the service. Heading and HeadingLevel
//   are also available as direct columns for ChunkMetadata construction.
// =============================================================================

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Internal record for mapping Dapper query results from vector similarity searches.
/// </summary>
/// <remarks>
/// <para>
/// This record is used exclusively by <see cref="PgVectorSearchService"/> to map
/// the raw SQL result set from pgvector cosine similarity queries. Each row represents
/// a chunk that matches the query embedding above the minimum score threshold.
/// </para>
/// <para>
/// <b>Score Calculation:</b> The <see cref="Score"/> property contains
/// <c>1 - cosine_distance</c>, where cosine distance is computed by pgvector's
/// <c>&lt;=&gt;</c> operator. A score of 1.0 indicates identical vectors;
/// 0.0 indicates orthogonal vectors.
/// </para>
/// <para>
/// <b>Internal Visibility:</b> This record is marked <c>internal</c> as it is an
/// implementation detail of the search service. External consumers interact with
/// <see cref="Lexichord.Abstractions.Contracts.SearchHit"/> instead.
/// </para>
/// <para>
/// <b>Introduced:</b> v0.4.5b.
/// </para>
/// </remarks>
internal sealed record ChunkSearchRow
{
    /// <summary>
    /// The unique identifier of the chunk.
    /// </summary>
    /// <value>Maps to <c>chunks.id</c> column.</value>
    public Guid Id { get; init; }

    /// <summary>
    /// The parent document's unique identifier.
    /// </summary>
    /// <value>Maps to <c>chunks.document_id</c> column.</value>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// The text content of the chunk.
    /// </summary>
    /// <value>Maps to <c>chunks.content</c> column.</value>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// The zero-based index of this chunk within its parent document.
    /// </summary>
    /// <value>Maps to <c>chunks.chunk_index</c> column.</value>
    public int ChunkIndex { get; init; }

    /// <summary>
    /// Optional JSONB metadata associated with the chunk.
    /// </summary>
    /// <value>Maps to <c>chunks.metadata</c> JSONB column. May be null.</value>
    public string? Metadata { get; init; }

    /// <summary>
    /// The section heading this chunk belongs to, if available.
    /// </summary>
    /// <value>Maps to <c>chunks.heading</c> column. May be null.</value>
    public string? Heading { get; init; }

    /// <summary>
    /// The heading level (1-6 for H1-H6) or null if no heading context.
    /// </summary>
    /// <value>Maps to <c>chunks.heading_level</c> column. May be null.</value>
    public int? HeadingLevel { get; init; }

    /// <summary>
    /// The character offset from the start of the source document.
    /// </summary>
    /// <value>Maps to <c>chunks.start_offset</c> column.</value>
    public int StartOffset { get; init; }

    /// <summary>
    /// The character offset marking the end of this chunk (exclusive).
    /// </summary>
    /// <value>Maps to <c>chunks.end_offset</c> column.</value>
    public int EndOffset { get; init; }

    /// <summary>
    /// The cosine similarity score (0.0 to 1.0).
    /// </summary>
    /// <value>
    /// Computed as <c>1 - (embedding &lt;=&gt; query_embedding)</c> in SQL.
    /// Higher values indicate greater semantic similarity.
    /// </value>
    public float Score { get; init; }
}
