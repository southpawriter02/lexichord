// =============================================================================
// File: GraphRecords.cs
// Project: Lexichord.Abstractions
// Description: Records and exceptions for graph database operations.
// =============================================================================
// LOGIC: Defines the data transfer types for graph write results and the
//   custom exception type for graph query failures. These types decouple
//   consumers from the Neo4j driver, ensuring that no Neo4j-specific types
//   leak through the abstraction boundary.
//
// Types defined:
//   - GraphWriteResult: Statistics from Cypher write operations
//   - GraphQueryException: Wrapper for graph database failures
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Result of a graph write operation containing mutation statistics.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IGraphSession.ExecuteAsync"/> and
/// <see cref="IGraphTransaction.ExecuteAsync"/> to report the effects
/// of Cypher write operations (CREATE, MERGE, DELETE, SET).
/// </para>
/// <para>
/// <b>Usage:</b> Check <see cref="TotalAffected"/> for a quick confirmation
/// that the operation had the expected effect, or inspect individual
/// counters for detailed verification.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await session.ExecuteAsync(
///     "CREATE (n:Product {id: $id, name: $name})",
///     new { id = Guid.NewGuid().ToString(), name = "Widget" });
///
/// if (result.NodesCreated == 1)
///     Console.WriteLine("Product created successfully");
/// </code>
/// </example>
public record GraphWriteResult
{
    /// <summary>
    /// Number of nodes created by the operation.
    /// </summary>
    /// <value>A non-negative integer representing created node count.</value>
    /// <remarks>
    /// LOGIC: Incremented by CREATE statements that produce new nodes.
    /// MERGE statements increment this only when the pattern did not exist.
    /// </remarks>
    public int NodesCreated { get; init; }

    /// <summary>
    /// Number of nodes deleted by the operation.
    /// </summary>
    /// <value>A non-negative integer representing deleted node count.</value>
    /// <remarks>
    /// LOGIC: Incremented by DELETE and DETACH DELETE statements.
    /// Deleting a node also deletes all its relationships (if DETACH DELETE).
    /// </remarks>
    public int NodesDeleted { get; init; }

    /// <summary>
    /// Number of relationships created by the operation.
    /// </summary>
    /// <value>A non-negative integer representing created relationship count.</value>
    /// <remarks>
    /// LOGIC: Incremented by CREATE statements that produce new relationships
    /// (e.g., <c>CREATE (a)-[:CONTAINS]->(b)</c>).
    /// </remarks>
    public int RelationshipsCreated { get; init; }

    /// <summary>
    /// Number of relationships deleted by the operation.
    /// </summary>
    /// <value>A non-negative integer representing deleted relationship count.</value>
    /// <remarks>
    /// LOGIC: Incremented by DELETE statements on relationships and
    /// DETACH DELETE statements on nodes (which cascade to relationships).
    /// </remarks>
    public int RelationshipsDeleted { get; init; }

    /// <summary>
    /// Number of properties set by the operation.
    /// </summary>
    /// <value>A non-negative integer representing the count of property mutations.</value>
    /// <remarks>
    /// LOGIC: Incremented by SET statements and property assignments in CREATE/MERGE.
    /// Each individual property assignment counts as one (e.g., setting name and
    /// version on a node counts as 2).
    /// </remarks>
    public int PropertiesSet { get; init; }

    /// <summary>
    /// Total number of affected items across all mutation types.
    /// </summary>
    /// <value>
    /// The sum of <see cref="NodesCreated"/>, <see cref="NodesDeleted"/>,
    /// <see cref="RelationshipsCreated"/>, <see cref="RelationshipsDeleted"/>,
    /// and <see cref="PropertiesSet"/>.
    /// </value>
    /// <remarks>
    /// LOGIC: Convenience property for quick "did anything happen?" checks.
    /// A value of 0 indicates the operation had no effect (e.g., a MERGE
    /// on an existing pattern with no SET clause).
    /// </remarks>
    public int TotalAffected =>
        NodesCreated + NodesDeleted +
        RelationshipsCreated + RelationshipsDeleted +
        PropertiesSet;

    /// <summary>
    /// A write result representing no changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Sentinel value for operations that complete successfully
    /// but produce no mutations (e.g., MERGE on existing data).
    /// </remarks>
    public static readonly GraphWriteResult Empty = new();
}

/// <summary>
/// Exception thrown when a graph database query or operation fails.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GraphQueryException"/> wraps Neo4j driver exceptions to
/// prevent implementation-specific types from leaking through the abstraction
/// boundary. Consumer code should catch this exception type rather than
/// Neo4j-specific exceptions.
/// </para>
/// <para>
/// <b>Common Causes:</b>
/// <list type="bullet">
///   <item>Invalid Cypher syntax.</item>
///   <item>Constraint violations (unique property, schema rules).</item>
///   <item>Connection timeouts or network failures.</item>
///   <item>Transaction deadlocks.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
public class GraphQueryException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="GraphQueryException"/> with a message.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <remarks>
    /// LOGIC: Use this constructor when no inner exception is available
    /// (e.g., for application-level validation failures before query execution).
    /// </remarks>
    public GraphQueryException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQueryException"/> with a message
    /// and inner exception.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">The underlying exception from the graph database driver.</param>
    /// <remarks>
    /// LOGIC: Use this constructor to wrap Neo4j driver exceptions. The inner
    /// exception preserves the full stack trace for diagnostics while the outer
    /// message provides a consumer-friendly description.
    /// </remarks>
    public GraphQueryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
