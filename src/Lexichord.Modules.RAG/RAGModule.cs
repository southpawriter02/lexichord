// =============================================================================
// File: RAGModule.cs
// Project: Lexichord.Modules.RAG
// Description: Module registration for the RAG subsystem.
// =============================================================================
// LOGIC: Registers all RAG-related services and initializes the vector type handler.
//   - VectorTypeHandler must be registered before any repository operations.
//   - DocumentRepository and ChunkRepository are scoped for per-request lifecycle.
//   - IngestionQueue and IngestionBackgroundService handle queued file processing (v0.4.2d).
// =============================================================================

using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Ingestion;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Data;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG;


/// <summary>
/// Module registration for the RAG (Retrieval-Augmented Generation) subsystem.
/// </summary>
/// <remarks>
/// <para>
/// The RAG module provides semantic search capabilities for the Lexichord platform,
/// enabling context-aware writing assistance through vector similarity matching.
/// </para>
/// <para>
/// <b>Components:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="VectorTypeHandler"/>: Dapper type mapping for pgvector.</description></item>
///   <item><description><see cref="DocumentRepository"/>: Document CRUD operations.</description></item>
///   <item><description><see cref="ChunkRepository"/>: Chunk storage and vector search.</description></item>
///   <item><description><see cref="FileWatcherIngestionHandler"/>: File change detection for auto-indexing (v0.4.2c).</description></item>
///   <item><description><see cref="IngestionQueue"/>: Priority-based processing queue for file ingestion (v0.4.2d).</description></item>
///   <item><description><see cref="IngestionBackgroundService"/>: Background processor for the ingestion queue (v0.4.2d).</description></item>
/// </list>
/// <para>
/// <b>Database Requirements:</b> The PostgreSQL database must have the pgvector
/// extension enabled and the schema migrations from v0.4.1b applied.
/// </para>
/// </remarks>
public sealed class RAGModule : IModule
{
    private ILogger<RAGModule>? _logger;

    /// <inheritdoc/>
    public ModuleInfo Info => new(
        Id: "rag",
        Name: "RAG Subsystem",
        Version: new Version(0, 4, 2),
        Author: "Lexichord Team",
        Description: "Retrieval-Augmented Generation subsystem for semantic search and context-aware assistance"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>CRITICAL:</b> The <see cref="VectorTypeHandler"/> is registered with Dapper's
    /// <see cref="SqlMapper"/> during service registration. This is a static operation
    /// that affects all Dapper queries globally and must happen before any repository
    /// operations.
    /// </para>
    /// <para>
    /// Repositories are registered as scoped services to align with database connection
    /// lifecycle patterns (one connection per unit of work).
    /// </para>
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // CRITICAL: Register the vector type handler with Dapper's static mapper.
        // This must happen before any repository operations that involve vector columns.
        SqlMapper.AddTypeHandler(new VectorTypeHandler());

        // LOGIC: Register repositories as scoped to match connection-per-request pattern.
        // IDbConnectionFactory is expected to be registered by Infrastructure.
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();

        // LOGIC: Register FileHashService as singleton - it is stateless and thread-safe.
        // Used for hash-based change detection in the ingestion pipeline (v0.4.2b).
        services.AddSingleton<IFileHashService, FileHashService>();

        // LOGIC: Configure FileWatcherOptions with defaults using Options pattern.
        // The defaults are set via ConfigureOptions which allows proper IOptions<T> injection.
        // These can be overridden via configuration binding in the host.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(FileWatcherOptions.Default));

        // LOGIC: Register FileWatcherIngestionHandler as singleton.
        // It subscribes to ExternalFileChangesEvent and publishes FileIndexingRequestedEvent.
        // Singleton ensures a single debounce state across the application lifetime (v0.4.2c).
        // Also register as INotificationHandler for MediatR discovery.
        services.AddSingleton<FileWatcherIngestionHandler>();
        services.AddSingleton<MediatR.INotificationHandler<Lexichord.Abstractions.Events.ExternalFileChangesEvent>>(
            sp => sp.GetRequiredService<FileWatcherIngestionHandler>());

        // LOGIC: Configure IngestionQueueOptions with defaults using Options pattern.
        // The defaults are set via Options.Create which allows proper IOptions<T> injection.
        // These can be overridden via configuration binding in the host (v0.4.2d).
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(IngestionQueueOptions.Default));

        // LOGIC: Register IngestionQueue as singleton.
        // The queue must be singleton to maintain state across the application lifetime.
        // It handles priority ordering, duplicate detection, and capacity management (v0.4.2d).
        services.AddSingleton<IIngestionQueue, IngestionQueue>();

        // LOGIC: Register IngestionBackgroundService as a hosted service.
        // It continuously processes items from the queue using IIngestionService (v0.4.2d).
        // Note: IIngestionService itself is registered by downstream consumers (e.g., v0.4.2e).
        services.AddHostedService<IngestionBackgroundService>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// RAG module initialization is minimal as the repositories are stateless
    /// and database schema is managed through migrations.
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<RAGModule>>();
        _logger.LogInformation("Initializing RAG module v{Version}", Info.Version);

        // LOGIC: Verify that IDbConnectionFactory is available.
        // This is a sanity check - the actual connection test happens on first use.
        var connectionFactory = provider.GetService<IDbConnectionFactory>();
        if (connectionFactory is null)
        {
            _logger.LogWarning(
                "IDbConnectionFactory not registered. RAG repositories will fail at runtime. " +
                "Ensure Infrastructure module is loaded before RAG module.");
        }
        else
        {
            _logger.LogDebug("IDbConnectionFactory available. RAG repositories ready.");
        }

        await Task.CompletedTask;
    }
}
