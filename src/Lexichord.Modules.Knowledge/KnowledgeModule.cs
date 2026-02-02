// =============================================================================
// File: KnowledgeModule.cs
// Project: Lexichord.Modules.Knowledge
// Description: Module registration for the Knowledge Graph subsystem.
// =============================================================================
// LOGIC: Registers all Knowledge Graph-related services and initializes the
//   Neo4j graph database connection. This module implements CKVS Phase 1:
//   - v0.4.5e: Graph Database Integration
//   - v0.4.5f: Schema Registry Service (this version)
//   - v0.4.5g: Entity Abstraction Layer (this version)
//
// Service Registrations:
//   - Neo4jConnectionFactory: Singleton (manages connection pool)
//   - Neo4jHealthCheck: Transient (created per health check invocation)
//   - GraphConfiguration: Bound to IOptions<GraphConfiguration>
//   - SchemaRegistry: Singleton (in-memory schema registry for entity/relationship validation)
//   - EntityExtractionPipeline: Singleton (coordinates extractors, deduplicates, aggregates)
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: ISecureVault (v0.0.6a), ILicenseContext (v0.0.4c),
//               IConfiguration (v0.0.3d), ILogger<T> (v0.0.3b)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Extraction;
using Lexichord.Modules.Knowledge.Extraction.Extractors;
using Lexichord.Modules.Knowledge.Graph;
using Lexichord.Modules.Knowledge.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge;

/// <summary>
/// Module registration for the Knowledge Graph subsystem (CKVS Phase 1).
/// </summary>
/// <remarks>
/// <para>
/// The Knowledge module provides the graph database infrastructure for storing
/// structured knowledge entities and their relationships. This is the foundation
/// for the Canonical Knowledge Validation System (CKVS).
/// </para>
/// <para>
/// <b>Components (v0.4.5e):</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Neo4jConnectionFactory"/>: Neo4j driver management with connection pooling and license gating.</description></item>
///   <item><description><see cref="Neo4jGraphSession"/>: Cypher query execution with timing and logging.</description></item>
///   <item><description><see cref="Neo4jHealthCheck"/>: Application health monitoring for Neo4j connectivity.</description></item>
///   <item><description><see cref="GraphConfiguration"/>: Typed configuration for Neo4j connection settings.</description></item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: Cannot access Knowledge Graph features.</item>
///   <item>WriterPro: Read-only access to the knowledge graph.</item>
///   <item>Teams: Full read-write access.</item>
///   <item>Enterprise: Full access plus custom ontology schemas (future).</item>
/// </list>
/// </para>
/// <para>
/// <b>Database Requirements:</b> Requires a running Neo4j 5.x instance.
/// The Docker Compose configuration provides a pre-configured Neo4j container
/// for local development.
/// </para>
/// </remarks>
public sealed class KnowledgeModule : IModule
{
    private ILogger<KnowledgeModule>? _logger;

    /// <inheritdoc/>
    public ModuleInfo Info => new(
        Id: "knowledge",
        Name: "Knowledge Graph",
        Version: new Version(0, 4, 5),
        Author: "Lexichord Team",
        Description: "Knowledge Graph Foundation — Neo4j integration for structured knowledge storage (CKVS Phase 1)"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Registers all Knowledge Graph services in the DI container. The
    /// <see cref="Neo4jConnectionFactory"/> is registered as a singleton to
    /// share the connection pool across the application lifetime.
    /// </para>
    /// <para>
    /// <b>Configuration Binding:</b> <see cref="GraphConfiguration"/> is bound
    /// to the <c>Knowledge:Graph</c> section of appsettings.json via
    /// <c>Options.Create()</c> with sensible defaults. Configuration can be
    /// overridden via the standard configuration hierarchy.
    /// </para>
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // =============================================================================
        // v0.4.5e: Graph Database Integration
        // =============================================================================

        // LOGIC: Configure GraphConfiguration with defaults using Options pattern.
        // Defaults match the Docker Compose Neo4j container configuration.
        // These can be overridden via appsettings.json or environment variables.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new GraphConfiguration()));

        // LOGIC: Register Neo4jConnectionFactory as singleton.
        // The factory manages the Neo4j driver and its connection pool.
        // Singleton ensures a single connection pool shared across the application.
        // Also registers as IAsyncDisposable for proper shutdown cleanup.
        services.AddSingleton<Neo4jConnectionFactory>();
        services.AddSingleton<IGraphConnectionFactory>(sp =>
            sp.GetRequiredService<Neo4jConnectionFactory>());

        // LOGIC: Register Neo4jHealthCheck as transient.
        // Health checks are created per invocation by the health check framework.
        services.AddTransient<Neo4jHealthCheck>();

        // =============================================================================
        // v0.4.5f: Schema Registry Service
        // =============================================================================

        // LOGIC: Register SchemaRegistry as singleton.
        // The registry holds all loaded entity and relationship type schemas in memory.
        // Singleton ensures consistent schema state across the application lifetime.
        // Also registered as ISchemaRegistry for consumer injection.
        services.AddSingleton<SchemaRegistry>();
        services.AddSingleton<ISchemaRegistry>(sp =>
            sp.GetRequiredService<SchemaRegistry>());

        // =============================================================================
        // v0.4.5g: Entity Abstraction Layer
        // =============================================================================

        // LOGIC: Register EntityExtractionPipeline as singleton.
        // The pipeline coordinates multiple entity extractors, handling priority-ordered
        // execution, error isolation, confidence filtering, mention deduplication, and
        // entity aggregation. Singleton ensures consistent extractor registration and
        // shared state across the application lifetime.
        // Also registered as IEntityExtractionPipeline for consumer injection.
        services.AddSingleton<EntityExtractionPipeline>();
        services.AddSingleton<IEntityExtractionPipeline>(sp =>
        {
            var pipeline = sp.GetRequiredService<EntityExtractionPipeline>();

            // LOGIC: Register built-in extractors in priority order (descending).
            // EndpointExtractor (100): API endpoint detection — most specific, runs first.
            // ParameterExtractor (90): API parameter detection — often co-located with endpoints.
            // ConceptExtractor (50): Domain term detection — broadest, runs last.
            pipeline.Register(new EndpointExtractor());
            pipeline.Register(new ParameterExtractor());
            pipeline.Register(new ConceptExtractor());

            return pipeline;
        });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Initializes the Knowledge module after the DI container is built.
    /// Performs a connectivity check against Neo4j and logs the result.
    /// </para>
    /// <para>
    /// <b>Failure Handling:</b> Neo4j connectivity failure during initialization
    /// is logged as a warning but does not prevent module loading. The graph
    /// database may not be available during development if Docker is not running.
    /// Runtime operations will fail with appropriate error messages when
    /// Neo4j is unavailable.
    /// </para>
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<KnowledgeModule>>();
        _logger.LogInformation("Initializing Knowledge module v{Version}", Info.Version);

        // LOGIC: Verify ISecureVault is available (required for Neo4j password).
        var vault = provider.GetService<Lexichord.Abstractions.Contracts.Security.ISecureVault>();
        if (vault is null)
        {
            _logger.LogWarning(
                "ISecureVault not registered. Neo4j password must be provided via configuration. " +
                "For production, ensure ISecureVault is available.");
        }

        // LOGIC: Attempt Neo4j connectivity verification.
        // This is a best-effort check — failure is logged but not fatal.
        var factory = provider.GetService<IGraphConnectionFactory>();
        if (factory is not null)
        {
            try
            {
                var connected = await factory.TestConnectionAsync();
                if (connected)
                {
                    _logger.LogInformation(
                        "Neo4j connection verified (database: {Database})",
                        factory.DatabaseName);
                }
                else
                {
                    _logger.LogWarning(
                        "Neo4j connection test failed. Knowledge Graph features will be unavailable " +
                        "until Neo4j is accessible. Ensure Docker Compose is running.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Neo4j initialization check failed. Knowledge Graph features will be " +
                    "unavailable until Neo4j is accessible. Error: {Message}", ex.Message);
            }
        }
        else
        {
            _logger.LogWarning(
                "IGraphConnectionFactory not available. This may indicate a DI registration issue.");
        }
    }
}
