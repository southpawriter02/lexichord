// =============================================================================
// File: IGraphConnectionFactory.cs
// Project: Lexichord.Abstractions
// Description: Factory interface for creating graph database connections.
// =============================================================================
// LOGIC: Defines the abstraction for graph database connectivity, following the
//   same pattern as IDbConnectionFactory for PostgreSQL. The factory creates
//   sessions with configurable access modes (Read/Write) and supports connection
//   health testing. License gating is enforced at the implementation level:
//   - Write access requires Teams tier
//   - Read access requires WriterPro tier
//   - Core tier cannot access graph features
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Access mode for graph database sessions.
/// </summary>
/// <remarks>
/// <para>
/// Controls whether the session targets read or write operations.
/// In clustered Neo4j deployments, read sessions can be routed to
/// read replicas for better load distribution.
/// </para>
/// <para>
/// <b>License Implications:</b>
/// <list type="bullet">
///   <item><see cref="Read"/> — Requires WriterPro tier or higher.</item>
///   <item><see cref="Write"/> — Requires Teams tier or higher.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
public enum GraphAccessMode
{
    /// <summary>
    /// Read-only queries. Can use read replicas in clustered deployments.
    /// </summary>
    /// <remarks>
    /// LOGIC: Read mode restricts the session to non-mutating Cypher queries
    /// (MATCH, RETURN, WITH). Attempts to execute write operations (CREATE,
    /// MERGE, DELETE, SET) will throw a Neo4j client exception.
    /// </remarks>
    Read,

    /// <summary>
    /// Read-write queries. Requires primary node in clustered deployments.
    /// </summary>
    /// <remarks>
    /// LOGIC: Write mode allows all Cypher operations including mutations.
    /// This mode always routes to the primary node in a cluster configuration.
    /// </remarks>
    Write
}

/// <summary>
/// Factory for creating graph database connections.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IGraphConnectionFactory"/> provides a consistent interface for
/// creating graph database sessions, following the same factory pattern as
/// <c>IDbConnectionFactory</c> for PostgreSQL connections (v0.0.5b).
/// </para>
/// <para>
/// <b>Connection Lifecycle:</b>
/// <list type="number">
///   <item>Factory is created once as a singleton during module initialization.</item>
///   <item>Sessions are created per-operation via <see cref="CreateSessionAsync"/>.</item>
///   <item>Each session wraps a Neo4j driver session with query execution capabilities.</item>
///   <item>Sessions must be disposed after use to return connections to the pool.</item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item>Core tier: Cannot create any sessions (<see cref="FeatureNotLicensedException"/>).</item>
///   <item>WriterPro tier: Can create <see cref="GraphAccessMode.Read"/> sessions only.</item>
///   <item>Teams/Enterprise tier: Can create both Read and Write sessions.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. The underlying
/// driver manages a connection pool shared across all sessions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a write session (requires Teams+)
/// await using var session = await factory.CreateSessionAsync(GraphAccessMode.Write);
/// var result = await session.ExecuteAsync(
///     "CREATE (n:Product {id: $id, name: $name}) RETURN n",
///     new { id = Guid.NewGuid().ToString(), name = "Lexichord" });
///
/// // Create a read-only session (requires WriterPro+)
/// await using var readSession = await factory.CreateSessionAsync(GraphAccessMode.Read);
/// var products = await readSession.QueryAsync&lt;KnowledgeEntity&gt;(
///     "MATCH (n:Product) RETURN n LIMIT 10");
/// </code>
/// </example>
public interface IGraphConnectionFactory
{
    /// <summary>
    /// Creates a new graph database session for query execution.
    /// </summary>
    /// <param name="accessMode">
    /// The access mode for the session. Defaults to <see cref="GraphAccessMode.Write"/>.
    /// Read mode can route to replicas; Write mode requires the primary node.
    /// </param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A new <see cref="IGraphSession"/> that must be disposed after use.</returns>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is insufficient:
    /// <list type="bullet">
    ///   <item>Core tier: Always thrown (Knowledge Graph not available).</item>
    ///   <item>WriterPro tier: Thrown when <paramref name="accessMode"/> is <see cref="GraphAccessMode.Write"/>.</item>
    /// </list>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the graph database driver is not properly configured or initialized.
    /// </exception>
    /// <remarks>
    /// LOGIC: License enforcement order:
    /// 1. Check write access requires Teams+ (if Write mode).
    /// 2. Check read access requires WriterPro+ (for any mode).
    /// 3. Create the underlying driver session with appropriate access mode.
    /// 4. Wrap in IGraphSession with logging and timing.
    /// </remarks>
    Task<IGraphSession> CreateSessionAsync(
        GraphAccessMode accessMode = GraphAccessMode.Write,
        CancellationToken ct = default);

    /// <summary>
    /// Tests the graph database connection.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>true</c> if the connection to the graph database is successful;
    /// <c>false</c> if the connection test fails for any reason.
    /// </returns>
    /// <remarks>
    /// LOGIC: Verifies connectivity to the graph database without creating a
    /// full session. This method does NOT enforce license checks, as it is
    /// used for health monitoring which should work at any tier. Failures
    /// are caught and logged at Warning level rather than thrown.
    /// </remarks>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current database name.
    /// </summary>
    /// <value>
    /// The name of the Neo4j database this factory connects to (e.g., "neo4j").
    /// </value>
    /// <remarks>
    /// LOGIC: Exposes the database name for health check reporting and
    /// diagnostic logging. Defaults to "neo4j" (the default Neo4j database).
    /// </remarks>
    string DatabaseName { get; }
}
