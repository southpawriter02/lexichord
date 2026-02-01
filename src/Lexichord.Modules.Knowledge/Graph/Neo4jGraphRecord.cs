// =============================================================================
// File: Neo4jGraphRecord.cs
// Project: Lexichord.Modules.Knowledge
// Description: IGraphRecord implementation wrapping a Neo4j IRecord.
// =============================================================================
// LOGIC: Adapts the Neo4j driver's IRecord to the Lexichord IGraphRecord
//   abstraction. Provides type-safe access to query result fields without
//   exposing Neo4j driver types to consumers.
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: Neo4j.Driver
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Neo4j.Driver;

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Wraps a Neo4j <see cref="IRecord"/> as an <see cref="IGraphRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides a driver-agnostic interface for accessing query result fields.
/// Used by <see cref="Neo4jGraphSession.QueryRawAsync"/> to return results
/// without exposing Neo4j-specific types.
/// </para>
/// <para>
/// <b>Type Conversion:</b> The Neo4j driver stores values as boxed objects.
/// The <see cref="Get{T}"/> method uses <see cref="ValueExtensions.As{T}"/>
/// from the Neo4j driver for type conversion.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
internal sealed class Neo4jGraphRecord : IGraphRecord
{
    private readonly IRecord _record;

    /// <summary>
    /// Initializes a new instance wrapping the specified Neo4j record.
    /// </summary>
    /// <param name="record">The Neo4j driver record to wrap.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="record"/> is null.
    /// </exception>
    public Neo4jGraphRecord(IRecord record)
    {
        _record = record ?? throw new ArgumentNullException(nameof(record));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Delegates to Neo4j's IRecord indexer and ValueExtensions.As&lt;T&gt;()
    /// for type conversion. Throws KeyNotFoundException if the key doesn't exist
    /// in the record, or InvalidCastException if the type conversion fails.
    /// </remarks>
    public T Get<T>(string key)
    {
        return _record[key].As<T>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Safe accessor that catches exceptions from missing keys or
    /// type conversion failures. Returns false and default(T) on any error.
    /// </remarks>
    public bool TryGet<T>(string key, out T? value)
    {
        try
        {
            if (_record.Keys.Contains(key))
            {
                var rawValue = _record[key];
                if (rawValue is null)
                {
                    value = default;
                    return true;
                }

                value = rawValue.As<T>();
                return true;
            }

            value = default;
            return false;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Returns the field names from the RETURN clause of the Cypher query.
    /// For example, <c>RETURN n.name AS name, n.version AS version</c> would
    /// return <c>["name", "version"]</c>.
    /// </remarks>
    public IReadOnlyList<string> Keys => _record.Keys.ToList();
}
