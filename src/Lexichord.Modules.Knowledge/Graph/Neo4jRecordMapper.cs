// =============================================================================
// File: Neo4jRecordMapper.cs
// Project: Lexichord.Modules.Knowledge
// Description: Maps Neo4j IRecord results to typed objects.
// =============================================================================
// LOGIC: Centralizes the logic for converting Neo4j query results to .NET types.
//   Handles primitive types, KnowledgeEntity mapping from graph nodes, and
//   fallback to the first column value for unsupported types.
//
// Supported type mappings:
//   - Primitives (int, long, string, bool, double, float): First column value
//   - KnowledgeEntity: Mapped from Neo4j INode (labels → Type, properties → fields)
//   - Other types: First column value via Neo4j's As<T>() conversion
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: Neo4j.Driver
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Neo4j.Driver;

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Maps Neo4j <see cref="IRecord"/> results to typed .NET objects.
/// </summary>
/// <remarks>
/// <para>
/// Provides centralized record-to-object mapping used by both
/// <see cref="Neo4jGraphSession"/> and <see cref="Neo4jGraphTransaction"/>.
/// </para>
/// <para>
/// <b>Mapping Strategy:</b>
/// <list type="bullet">
///   <item>Primitive types: Extract from first column via <c>As&lt;T&gt;()</c>.</item>
///   <item><see cref="KnowledgeEntity"/>: Map from <see cref="INode"/> properties.</item>
///   <item>Other types: Attempt first column conversion via <c>As&lt;T&gt;()</c>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
internal static class Neo4jRecordMapper
{
    /// <summary>
    /// Maps a single Neo4j record to the requested type.
    /// </summary>
    /// <typeparam name="T">The target type for mapping.</typeparam>
    /// <param name="record">The Neo4j record to map.</param>
    /// <returns>The mapped object of type <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// LOGIC: Type dispatch based on the target type:
    /// 1. Primitives and strings: Direct extraction from first column.
    /// 2. KnowledgeEntity: Full property mapping from INode.
    /// 3. Default: Attempt generic conversion from first column.
    /// </remarks>
    public static T MapRecord<T>(IRecord record)
    {
        // LOGIC: Handle primitive types — extract directly from first column.
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
        {
            return record[0].As<T>();
        }

        // LOGIC: Handle KnowledgeEntity — map from Neo4j INode with label-based typing.
        if (typeof(T) == typeof(KnowledgeEntity))
        {
            return (T)(object)MapKnowledgeEntity(record);
        }

        // LOGIC: Default — attempt generic conversion from first column.
        // This covers Neo4j driver types (INode, IRelationship, IPath) and
        // any other types the driver can convert to.
        return record[0].As<T>();
    }

    /// <summary>
    /// Maps a Neo4j record containing a node to a <see cref="KnowledgeEntity"/>.
    /// </summary>
    /// <param name="record">The record containing a node in the first column.</param>
    /// <returns>A populated <see cref="KnowledgeEntity"/>.</returns>
    /// <remarks>
    /// LOGIC: Extracts entity metadata from the Neo4j node:
    /// - Type: First label on the node (Neo4j nodes can have multiple labels).
    /// - Id: Parsed from the "id" string property (stored as UUID string).
    /// - Name: Extracted from the "name" property.
    /// - Properties: All remaining properties (excluding id, name, createdAt, modifiedAt).
    /// - CreatedAt/ModifiedAt: Parsed from ISO 8601 strings if present.
    /// </remarks>
    private static KnowledgeEntity MapKnowledgeEntity(IRecord record)
    {
        var node = record[0].As<INode>();

        // LOGIC: Extract the entity type from the first Neo4j label.
        // Knowledge graph entities should have exactly one label (the entity type).
        var entityType = node.Labels.FirstOrDefault() ?? "Unknown";

        // LOGIC: Parse the UUID from the "id" string property.
        var id = node.Properties.TryGetValue("id", out var idValue)
            ? Guid.Parse(idValue.As<string>())
            : Guid.NewGuid();

        // LOGIC: Extract the display name.
        var name = node.Properties.TryGetValue("name", out var nameValue)
            ? nameValue.As<string>()
            : string.Empty;

        // LOGIC: Extract remaining properties, excluding system fields.
        var systemFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id", "name", "createdAt", "modifiedAt"
        };

        var properties = node.Properties
            .Where(p => !systemFields.Contains(p.Key))
            .ToDictionary(p => p.Key, p => p.Value);

        // LOGIC: Parse timestamps from ISO 8601 strings.
        var createdAt = node.Properties.TryGetValue("createdAt", out var ca)
            ? DateTimeOffset.Parse(ca.As<string>())
            : DateTimeOffset.UtcNow;

        var modifiedAt = node.Properties.TryGetValue("modifiedAt", out var ma)
            ? DateTimeOffset.Parse(ma.As<string>())
            : DateTimeOffset.UtcNow;

        return new KnowledgeEntity
        {
            Id = id,
            Type = entityType,
            Name = name,
            Properties = properties,
            CreatedAt = createdAt,
            ModifiedAt = modifiedAt
        };
    }
}
