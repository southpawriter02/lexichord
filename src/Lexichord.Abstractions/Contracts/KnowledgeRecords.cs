// =============================================================================
// File: KnowledgeRecords.cs
// Project: Lexichord.Abstractions
// Description: Domain records for the knowledge graph entity model.
// =============================================================================
// LOGIC: Defines the core entity and relationship records for the knowledge
//   graph. These records are used throughout the Knowledge module as the
//   primary data transfer types between the graph database, extraction
//   pipeline, and UI layers.
//
// Records defined:
//   - KnowledgeEntity: A node in the knowledge graph (Product, Endpoint, etc.)
//   - KnowledgeRelationship: An edge connecting two entities (CONTAINS, ACCEPTS, etc.)
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// A node in the knowledge graph representing a discrete piece of knowledge.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KnowledgeEntity"/> represents a typed node in the Neo4j graph database.
/// Entity types (e.g., "Product", "Endpoint", "Parameter") are governed by the
/// Schema Registry (v0.4.5f) and validated before storage.
/// </para>
/// <para>
/// <b>Neo4j Mapping:</b>
/// <list type="bullet">
///   <item><see cref="Type"/> maps to the Neo4j node label.</item>
///   <item><see cref="Id"/>, <see cref="Name"/> map to node properties.</item>
///   <item><see cref="Properties"/> maps to additional node properties.</item>
/// </list>
/// </para>
/// <para>
/// <b>Source Tracking:</b> The <see cref="SourceDocuments"/> list links entities
/// back to the documents from which they were extracted, enabling provenance
/// tracking and re-extraction on document updates.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var endpoint = new KnowledgeEntity
/// {
///     Type = "Endpoint",
///     Name = "GET /api/users",
///     Properties = new Dictionary&lt;string, object&gt;
///     {
///         ["path"] = "/api/users",
///         ["method"] = "GET",
///         ["description"] = "Returns a list of users"
///     },
///     SourceDocuments = [documentId]
/// };
/// </code>
/// </example>
public record KnowledgeEntity
{
    /// <summary>
    /// Unique identifier for the entity (UUID).
    /// </summary>
    /// <value>A globally unique identifier. Defaults to a new GUID on creation.</value>
    /// <remarks>
    /// LOGIC: Auto-generated on creation. Stored as a string property in Neo4j
    /// (Neo4j does not natively support .NET Guid type). Parsing from string
    /// is handled by the session's record mapper.
    /// </remarks>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Entity type name (must match a registered schema type).
    /// </summary>
    /// <value>
    /// The type name (e.g., "Product", "Endpoint", "Parameter", "Component", "Concept").
    /// Maps to the Neo4j node label.
    /// </value>
    /// <remarks>
    /// LOGIC: Validated against the Schema Registry (v0.4.5f) before storage.
    /// Unknown types are rejected with a schema validation error.
    /// </remarks>
    public required string Type { get; init; }

    /// <summary>
    /// Human-readable display name for the entity.
    /// </summary>
    /// <value>
    /// The entity name (e.g., "GET /api/users", "userId", "Authentication Service").
    /// </value>
    /// <remarks>
    /// LOGIC: Primary identifier for display in the Entity Browser (v0.4.7).
    /// Should be unique within a given type, though this is not enforced at
    /// the database level.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Additional properties as key-value pairs.
    /// </summary>
    /// <value>
    /// A mutable dictionary of typed property values. Common keys include
    /// "path", "method", "description", "version", "required", "default_value".
    /// </value>
    /// <remarks>
    /// LOGIC: Property names and types are validated against the Schema Registry.
    /// Properties are stored as individual Neo4j node properties for efficient
    /// querying. The "id" and "name" keys are reserved and should not be
    /// duplicated here.
    /// </remarks>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// Source document IDs that reference this entity.
    /// </summary>
    /// <value>
    /// A list of document GUIDs from the RAG document index (v0.4.1c) that
    /// mention this entity. Updated during entity extraction.
    /// </value>
    /// <remarks>
    /// LOGIC: Enables provenance tracking. When a document is re-indexed,
    /// its entity mentions can be refreshed. Stored in the PostgreSQL
    /// <c>document_entities</c> link table rather than in Neo4j.
    /// </remarks>
    public List<Guid> SourceDocuments { get; init; } = new();

    /// <summary>
    /// When the entity was created in the knowledge graph.
    /// </summary>
    /// <value>UTC timestamp of entity creation. Defaults to current time.</value>
    /// <remarks>
    /// LOGIC: Set once on initial creation. Stored as an ISO 8601 string
    /// in Neo4j for cross-platform compatibility.
    /// </remarks>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the entity was last modified.
    /// </summary>
    /// <value>UTC timestamp of last modification. Defaults to current time.</value>
    /// <remarks>
    /// LOGIC: Updated on any property change or source document update.
    /// Used for staleness detection and cache invalidation.
    /// </remarks>
    public DateTimeOffset ModifiedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A relationship (edge) between two entities in the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KnowledgeRelationship"/> represents a typed, directed edge
/// in the Neo4j graph database connecting two <see cref="KnowledgeEntity"/>
/// instances. Relationship types (e.g., "CONTAINS", "ACCEPTS") are governed
/// by the Schema Registry (v0.4.5f).
/// </para>
/// <para>
/// <b>Neo4j Mapping:</b>
/// <list type="bullet">
///   <item><see cref="Type"/> maps to the Neo4j relationship type.</item>
///   <item><see cref="FromEntityId"/> maps to the start node.</item>
///   <item><see cref="ToEntityId"/> maps to the end node.</item>
///   <item><see cref="Properties"/> maps to relationship properties.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var containsRelation = new KnowledgeRelationship
/// {
///     Type = "CONTAINS",
///     FromEntityId = productId,
///     ToEntityId = endpointId,
///     Properties = new Dictionary&lt;string, object&gt;
///     {
///         ["since"] = "v1.0"
///     }
/// };
/// </code>
/// </example>
public record KnowledgeRelationship
{
    /// <summary>
    /// Unique identifier for the relationship.
    /// </summary>
    /// <value>A globally unique identifier. Defaults to a new GUID on creation.</value>
    /// <remarks>
    /// LOGIC: Auto-generated on creation. Stored as a string property on the
    /// Neo4j relationship for external reference.
    /// </remarks>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Relationship type name (must match a registered schema type).
    /// </summary>
    /// <value>
    /// The relationship type (e.g., "CONTAINS", "ACCEPTS", "DEPENDS_ON").
    /// Maps to the Neo4j relationship type.
    /// </value>
    /// <remarks>
    /// LOGIC: Validated against the Schema Registry (v0.4.5f) before storage.
    /// The schema also validates that the source and target entity types are
    /// compatible with this relationship type.
    /// </remarks>
    public required string Type { get; init; }

    /// <summary>
    /// Source entity ID (start node of the relationship).
    /// </summary>
    /// <value>The GUID of the entity from which the relationship originates.</value>
    /// <remarks>
    /// LOGIC: Must reference an existing <see cref="KnowledgeEntity"/> in the graph.
    /// The entity type must match the "from" constraint in the Schema Registry.
    /// </remarks>
    public required Guid FromEntityId { get; init; }

    /// <summary>
    /// Target entity ID (end node of the relationship).
    /// </summary>
    /// <value>The GUID of the entity to which the relationship points.</value>
    /// <remarks>
    /// LOGIC: Must reference an existing <see cref="KnowledgeEntity"/> in the graph.
    /// The entity type must match the "to" constraint in the Schema Registry.
    /// </remarks>
    public required Guid ToEntityId { get; init; }

    /// <summary>
    /// Additional properties on the relationship.
    /// </summary>
    /// <value>
    /// A mutable dictionary of typed property values. Common keys include
    /// "location" (for parameter relationships), "since" (for versioning).
    /// </value>
    /// <remarks>
    /// LOGIC: Optional properties validated against the Schema Registry.
    /// Stored as Neo4j relationship properties.
    /// </remarks>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// When the relationship was created.
    /// </summary>
    /// <value>UTC timestamp of relationship creation. Defaults to current time.</value>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
