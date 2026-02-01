// =============================================================================
// File: GraphConfiguration.cs
// Project: Lexichord.Modules.Knowledge
// Description: Configuration record for Neo4j graph database connection.
// =============================================================================
// LOGIC: Strongly-typed configuration for the Neo4j connection, bound to the
//   "Knowledge:Graph" section of appsettings.json via IOptions<T> pattern.
//   Follows the same pattern as DatabaseOptions for PostgreSQL (v0.0.5b).
//
// Configuration precedence (lowest to highest):
//   1. Default values in this record
//   2. appsettings.json (Knowledge:Graph section)
//   3. appsettings.{Environment}.json overrides
//   4. Environment variables (LEXICHORD_KNOWLEDGE__GRAPH__URI)
//   5. Command-line arguments
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: Microsoft.Extensions.Options
// =============================================================================

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Configuration for Neo4j graph database connection.
/// </summary>
/// <remarks>
/// <para>
/// Bound to the <c>Knowledge:Graph</c> section of <c>appsettings.json</c> via the
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> pattern.
/// </para>
/// <para>
/// <b>Security:</b> The <see cref="Password"/> property serves as a fallback only.
/// Production deployments should store the Neo4j password in <c>ISecureVault</c>
/// under the key <c>"neo4j:password"</c>. The factory checks the vault first and
/// falls back to this configuration property.
/// </para>
/// <para>
/// <b>Docker Compose Defaults:</b> The default values match the Neo4j container
/// configuration in <c>docker-compose.yml</c> for seamless local development.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // appsettings.json
/// {
///   "Knowledge": {
///     "Graph": {
///       "Uri": "bolt://localhost:7687",
///       "Database": "neo4j",
///       "Username": "neo4j",
///       "MaxConnectionPoolSize": 100,
///       "ConnectionTimeoutSeconds": 30,
///       "QueryTimeoutSeconds": 60,
///       "Encrypted": false
///     }
///   }
/// }
/// </code>
/// </example>
public record GraphConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    /// <value>The string <c>"Knowledge:Graph"</c>.</value>
    /// <remarks>
    /// LOGIC: Used by the module's service registration to bind configuration:
    /// <c>services.Configure&lt;GraphConfiguration&gt;(config.GetSection(SectionName))</c>.
    /// </remarks>
    public const string SectionName = "Knowledge:Graph";

    /// <summary>
    /// Neo4j Bolt URI for the graph database.
    /// </summary>
    /// <value>
    /// A Bolt protocol URI (e.g., <c>"bolt://localhost:7687"</c>).
    /// Defaults to <c>"bolt://localhost:7687"</c> for local Docker development.
    /// </value>
    /// <remarks>
    /// LOGIC: The Bolt protocol is Neo4j's native binary protocol, offering
    /// better performance than HTTP. Supports <c>bolt://</c>, <c>bolt+s://</c>
    /// (encrypted), and <c>neo4j://</c> (routing) schemes.
    /// </remarks>
    public string Uri { get; init; } = "bolt://localhost:7687";

    /// <summary>
    /// Neo4j database name.
    /// </summary>
    /// <value>
    /// The target database name. Defaults to <c>"neo4j"</c> (the default database
    /// in Neo4j Community Edition).
    /// </value>
    /// <remarks>
    /// LOGIC: Neo4j Community Edition supports only the default database.
    /// Enterprise Edition supports multiple named databases for multi-tenancy.
    /// </remarks>
    public string Database { get; init; } = "neo4j";

    /// <summary>
    /// Username for Neo4j authentication.
    /// </summary>
    /// <value>
    /// The Neo4j username. Defaults to <c>"neo4j"</c> (the built-in admin user).
    /// </value>
    /// <remarks>
    /// LOGIC: The default Neo4j installation uses "neo4j" as the admin username.
    /// Custom users can be created via the Neo4j admin console.
    /// </remarks>
    public string Username { get; init; } = "neo4j";

    /// <summary>
    /// Password for Neo4j authentication (fallback — prefer ISecureVault).
    /// </summary>
    /// <value>
    /// The password string. Nullable — if null, the factory checks
    /// <c>ISecureVault</c> for the key <c>"neo4j:password"</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: This property is a fallback for development environments.
    /// Production deployments should store the password in <c>ISecureVault</c>
    /// and leave this property null. The factory retrieval order is:
    /// 1. ISecureVault.GetSecretAsync("neo4j:password")
    /// 2. This configuration property
    /// 3. Throw InvalidOperationException if neither is available
    /// </remarks>
    public string? Password { get; init; }

    /// <summary>
    /// Maximum number of connections in the driver's connection pool.
    /// </summary>
    /// <value>
    /// A positive integer. Defaults to <c>100</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Controls the upper bound of concurrent connections to Neo4j.
    /// The driver automatically manages pool sizing within this limit.
    /// Set lower for development (e.g., 10) and higher for production workloads.
    /// </remarks>
    public int MaxConnectionPoolSize { get; init; } = 100;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    /// <value>
    /// Timeout for establishing a new connection. Defaults to <c>30</c> seconds.
    /// </value>
    /// <remarks>
    /// LOGIC: If a connection cannot be established within this timeout,
    /// the operation fails. Applies to initial connection and pool acquisition.
    /// </remarks>
    public int ConnectionTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Query execution timeout in seconds.
    /// </summary>
    /// <value>
    /// Maximum allowed execution time for a single Cypher query.
    /// Defaults to <c>60</c> seconds.
    /// </value>
    /// <remarks>
    /// LOGIC: Long-running queries are terminated after this timeout.
    /// Used to prevent runaway queries from consuming resources indefinitely.
    /// Note: This is an application-level timeout; Neo4j also has server-side
    /// transaction timeouts that may terminate queries earlier.
    /// </remarks>
    public int QueryTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Whether to use encrypted (TLS) connections.
    /// </summary>
    /// <value>
    /// <c>true</c> for encrypted connections; <c>false</c> for plaintext.
    /// Defaults to <c>false</c> for local Docker development.
    /// </value>
    /// <remarks>
    /// LOGIC: Encryption should be enabled for production deployments,
    /// especially when connecting over untrusted networks. The Docker
    /// Compose Neo4j container uses plaintext by default.
    /// When using <c>bolt+s://</c> URI scheme, this property is ignored
    /// (encryption is implied by the scheme).
    /// </remarks>
    public bool Encrypted { get; init; } = false;
}
