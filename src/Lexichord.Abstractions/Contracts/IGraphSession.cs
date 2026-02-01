// =============================================================================
// File: IGraphSession.cs
// Project: Lexichord.Abstractions
// Description: Session, record, and transaction interfaces for graph queries.
// =============================================================================
// LOGIC: Defines the graph session abstraction for executing Cypher queries
//   against the knowledge graph database. Sessions are short-lived and must
//   be disposed after use. The interfaces decouple consumers from the Neo4j
//   driver implementation, enabling testability and future provider swaps.
//
// Interfaces defined:
//   - IGraphSession: Main query execution interface (IAsyncDisposable)
//   - IGraphRecord: Raw record access for untyped query results
//   - IGraphTransaction: Explicit transaction support for multi-statement ops
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// A session for executing graph database queries.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IGraphSession"/> provides a simplified interface for executing
/// Cypher queries against the knowledge graph. It wraps the underlying Neo4j
/// driver session with consistent error handling, timing, and logging.
/// </para>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
///   <item>Created via <see cref="IGraphConnectionFactory.CreateSessionAsync"/>.</item>
///   <item>Execute one or more queries/writes.</item>
///   <item>Dispose when done (returns connection to pool).</item>
/// </list>
/// </para>
/// <para>
/// <b>Error Handling:</b> All Neo4j-specific exceptions are wrapped in
/// <see cref="GraphQueryException"/> to prevent driver implementation leakage
/// into consumer code.
/// </para>
/// <para>
/// <b>Performance Monitoring:</b> Queries exceeding 100ms are logged at
/// Warning level with the (truncated) Cypher query text.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Sessions are NOT thread-safe. Use one session per
/// logical operation and do not share across threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await using var session = await factory.CreateSessionAsync(GraphAccessMode.Read);
///
/// // Typed query
/// var entities = await session.QueryAsync&lt;KnowledgeEntity&gt;(
///     "MATCH (n:Product) RETURN n LIMIT 10");
///
/// // Raw query
/// var records = await session.QueryRawAsync(
///     "MATCH (n) RETURN n.name AS name, labels(n) AS types LIMIT 5");
/// foreach (var record in records)
/// {
///     var name = record.Get&lt;string&gt;("name");
///     var types = record.Get&lt;IReadOnlyList&lt;string&gt;&gt;("types");
/// }
///
/// // Write operation
/// var result = await session.ExecuteAsync(
///     "CREATE (n:Product {id: $id, name: $name})",
///     new { id = Guid.NewGuid().ToString(), name = "Lexichord" });
/// Console.WriteLine($"Nodes created: {result.NodesCreated}");
/// </code>
/// </example>
public interface IGraphSession : IAsyncDisposable
{
    /// <summary>
    /// Executes a Cypher query and returns typed results.
    /// </summary>
    /// <typeparam name="T">
    /// Result type. Supported types:
    /// <list type="bullet">
    ///   <item>Primitive types (int, string, bool, etc.)</item>
    ///   <item><see cref="KnowledgeEntity"/> — automatically mapped from graph nodes.</item>
    /// </list>
    /// </typeparam>
    /// <param name="cypher">The Cypher query string to execute.</param>
    /// <param name="parameters">
    /// Optional query parameters. Can be an anonymous object (e.g., <c>new { id, name }</c>)
    /// or a <see cref="Dictionary{TKey, TValue}"/>. Properties are mapped to Cypher
    /// parameters by name (e.g., <c>$id</c>, <c>$name</c>).
    /// </param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A read-only list of typed query results.</returns>
    /// <exception cref="GraphQueryException">
    /// Thrown when the Cypher query fails. Wraps the underlying driver exception
    /// with the query text for diagnostics.
    /// </exception>
    /// <remarks>
    /// LOGIC: Executes the Cypher query, maps results to the requested type,
    /// and logs timing information. Slow queries (>100ms) are logged at Warning level.
    /// </remarks>
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a Cypher query and returns raw records.
    /// </summary>
    /// <param name="cypher">The Cypher query string to execute.</param>
    /// <param name="parameters">
    /// Optional query parameters. Can be an anonymous object or dictionary.
    /// </param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A read-only list of <see cref="IGraphRecord"/> instances.</returns>
    /// <exception cref="GraphQueryException">
    /// Thrown when the Cypher query fails.
    /// </exception>
    /// <remarks>
    /// LOGIC: Use this method when the result schema is dynamic or when you need
    /// access to multiple named columns. For single-type results, prefer
    /// <see cref="QueryAsync{T}"/>.
    /// </remarks>
    Task<IReadOnlyList<IGraphRecord>> QueryRawAsync(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a Cypher write operation (CREATE, MERGE, DELETE, SET).
    /// </summary>
    /// <param name="cypher">The Cypher statement to execute.</param>
    /// <param name="parameters">
    /// Optional query parameters. Can be an anonymous object or dictionary.
    /// </param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="GraphWriteResult"/> containing statistics about the operation
    /// (nodes created/deleted, relationships created/deleted, properties set).
    /// </returns>
    /// <exception cref="GraphQueryException">
    /// Thrown when the Cypher write operation fails.
    /// </exception>
    /// <remarks>
    /// LOGIC: Consumes the result cursor to extract write statistics from the
    /// query summary. Logs timing information at Debug level and slow queries
    /// at Warning level.
    /// </remarks>
    Task<GraphWriteResult> ExecuteAsync(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Begins an explicit transaction for multi-statement operations.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// An <see cref="IGraphTransaction"/> that must be committed or rolled back,
    /// then disposed.
    /// </returns>
    /// <exception cref="GraphQueryException">
    /// Thrown when the transaction cannot be started.
    /// </exception>
    /// <remarks>
    /// LOGIC: Explicit transactions group multiple Cypher statements into a
    /// single atomic unit. If the transaction is disposed without calling
    /// <see cref="IGraphTransaction.CommitAsync"/>, it is automatically
    /// rolled back.
    /// </remarks>
    Task<IGraphTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

/// <summary>
/// A raw record from graph query results.
/// </summary>
/// <remarks>
/// <para>
/// Provides key-based access to individual fields in a query result record.
/// Used with <see cref="IGraphSession.QueryRawAsync"/> for dynamic result schemas.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var records = await session.QueryRawAsync(
///     "MATCH (n:Product) RETURN n.name AS name, n.version AS version");
/// foreach (var record in records)
/// {
///     var name = record.Get&lt;string&gt;("name");
///     if (record.TryGet&lt;string&gt;("version", out var version))
///     {
///         Console.WriteLine($"{name} v{version}");
///     }
/// }
/// </code>
/// </example>
public interface IGraphRecord
{
    /// <summary>
    /// Gets a value by key, casting to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="key">The field name in the query result.</param>
    /// <returns>The value cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the <paramref name="key"/> does not exist in the record.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// Thrown when the value cannot be cast to <typeparamref name="T"/>.
    /// </exception>
    /// <remarks>
    /// LOGIC: Retrieves a named field from the query result. Field names
    /// correspond to aliases in the RETURN clause of the Cypher query.
    /// </remarks>
    T Get<T>(string key);

    /// <summary>
    /// Tries to get a value by key, returning false if the key doesn't exist.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="key">The field name in the query result.</param>
    /// <param name="value">
    /// When this method returns <c>true</c>, contains the value cast to
    /// <typeparamref name="T"/>. When <c>false</c>, contains the default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the key exists and the value was successfully retrieved;
    /// <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// LOGIC: Safe accessor that does not throw on missing keys or null values.
    /// Preferred over <see cref="Get{T}"/> when fields are optional.
    /// </remarks>
    bool TryGet<T>(string key, out T? value);

    /// <summary>
    /// Gets all keys available in the record.
    /// </summary>
    /// <value>
    /// A read-only list of field names corresponding to the RETURN clause
    /// aliases in the Cypher query.
    /// </value>
    /// <remarks>
    /// LOGIC: Enables introspection of result schema for dynamic queries
    /// where the return columns are not known at compile time.
    /// </remarks>
    IReadOnlyList<string> Keys { get; }
}

/// <summary>
/// An explicit graph transaction for grouping multiple operations atomically.
/// </summary>
/// <remarks>
/// <para>
/// Transactions allow multiple Cypher statements to be executed as a single
/// atomic unit. Either all statements succeed (commit) or none take effect
/// (rollback).
/// </para>
/// <para>
/// <b>Important:</b> If the transaction is disposed without calling
/// <see cref="CommitAsync"/>, it is automatically rolled back.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await using var tx = await session.BeginTransactionAsync();
/// try
/// {
///     await tx.ExecuteAsync("CREATE (a:Product {id: $id, name: $name})",
///         new { id = "1", name = "Widget" });
///     await tx.ExecuteAsync("CREATE (b:Component {id: $id, name: $name})",
///         new { id = "2", name = "Part A" });
///     await tx.ExecuteAsync(
///         "MATCH (a:Product {id: $pid}), (b:Component {id: $cid}) " +
///         "CREATE (a)-[:CONTAINS]->(b)",
///         new { pid = "1", cid = "2" });
///
///     await tx.CommitAsync();
/// }
/// catch
/// {
///     await tx.RollbackAsync();
///     throw;
/// }
/// </code>
/// </example>
public interface IGraphTransaction : IAsyncDisposable
{
    /// <summary>
    /// Executes a Cypher query within the transaction and returns typed results.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="cypher">The Cypher query string.</param>
    /// <param name="parameters">Optional query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of typed results.</returns>
    /// <exception cref="GraphQueryException">Thrown when the query fails.</exception>
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a Cypher write operation within the transaction.
    /// </summary>
    /// <param name="cypher">The Cypher statement.</param>
    /// <param name="parameters">Optional query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Write statistics for the operation.</returns>
    /// <exception cref="GraphQueryException">Thrown when the write fails.</exception>
    Task<GraphWriteResult> ExecuteAsync(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Commits the transaction, making all changes permanent.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="GraphQueryException">Thrown when the commit fails.</exception>
    /// <remarks>
    /// LOGIC: After commit, the transaction cannot be reused. Attempting
    /// further operations will throw.
    /// </remarks>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Rolls back the transaction, discarding all changes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="GraphQueryException">Thrown when the rollback fails.</exception>
    /// <remarks>
    /// LOGIC: Explicit rollback. Also called automatically if the transaction
    /// is disposed without a prior commit.
    /// </remarks>
    Task RollbackAsync(CancellationToken ct = default);
}
