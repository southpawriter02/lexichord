// =============================================================================
// File: Neo4jConnectionFactory.cs
// Project: Lexichord.Modules.Knowledge
// Description: IGraphConnectionFactory implementation using Neo4j.Driver.
// =============================================================================
// LOGIC: Creates and manages Neo4j driver connections with:
//   - Connection pooling via the Neo4j driver's built-in pool
//   - License tier enforcement (Teams for write, WriterPro for read)
//   - Secure password retrieval from ISecureVault with config fallback
//   - Health check support via VerifyConnectivityAsync()
//
// Lifecycle:
//   - Singleton — one factory per application lifetime
//   - Driver created in constructor (connection pool initialized)
//   - Sessions created per-operation and disposed after use
//   - Factory disposal closes the driver and all pooled connections
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: Neo4j.Driver, ISecureVault (v0.0.6a), ILicenseContext (v0.0.4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Factory for creating Neo4j graph database sessions.
/// </summary>
/// <remarks>
/// <para>
/// Creates <see cref="IGraphSession"/> instances backed by the Neo4j Bolt driver.
/// The factory manages the driver lifecycle and connection pool, and enforces
/// license tier requirements on session creation.
/// </para>
/// <para>
/// <b>Password Resolution:</b>
/// <list type="number">
///   <item>Check <see cref="ISecureVault"/> for key <c>"neo4j:password"</c>.</item>
///   <item>Fall back to <see cref="GraphConfiguration.Password"/>.</item>
///   <item>Throw <see cref="InvalidOperationException"/> if neither is available.</item>
/// </list>
/// </para>
/// <para>
/// <b>License Enforcement:</b>
/// <list type="bullet">
///   <item>Core tier: Cannot create any sessions.</item>
///   <item>WriterPro: Read-only sessions only.</item>
///   <item>Teams/Enterprise: Full read-write access.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Thread-safe. The Neo4j driver and its connection pool
/// are designed for concurrent access from multiple threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
public sealed class Neo4jConnectionFactory : IGraphConnectionFactory, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly GraphConfiguration _config;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<Neo4jConnectionFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jConnectionFactory"/>.
    /// </summary>
    /// <param name="config">Neo4j connection configuration.</param>
    /// <param name="vault">Secure vault for password retrieval.</param>
    /// <param name="licenseContext">License context for tier enforcement.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Neo4j password cannot be resolved from either the
    /// secure vault or the configuration.
    /// </exception>
    /// <remarks>
    /// LOGIC: Constructor resolves the password and creates the Neo4j driver.
    /// The driver initializes its connection pool but does not establish a
    /// connection until the first session is created. Password resolution
    /// order: vault → config → throw.
    /// </remarks>
    public Neo4jConnectionFactory(
        IOptions<GraphConfiguration> config,
        ISecureVault vault,
        ILicenseContext licenseContext,
        ILogger<Neo4jConnectionFactory> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(vault);

        // LOGIC: Resolve password from secure vault, falling back to configuration.
        // The vault check is synchronous here because constructor cannot be async.
        // In practice, the vault should be pre-populated during setup.
        string? password = null;
        try
        {
            password = vault.GetSecretAsync("neo4j:password").GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not retrieve Neo4j password from vault, falling back to configuration");
        }

        password ??= _config.Password;

        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Neo4j password not configured. Set it in ISecureVault under key 'neo4j:password' " +
                "or provide it in the Knowledge:Graph:Password configuration setting.");
        }

        // LOGIC: Create the Neo4j driver with connection pooling and timeouts.
        _driver = GraphDatabase.Driver(
            _config.Uri,
            AuthTokens.Basic(_config.Username, password),
            builder =>
            {
                builder
                    .WithMaxConnectionPoolSize(_config.MaxConnectionPoolSize)
                    .WithConnectionTimeout(TimeSpan.FromSeconds(_config.ConnectionTimeoutSeconds))
                    .WithDefaultReadBufferSize(64 * 1024);

                // LOGIC: Encryption configuration.
                // When using bolt+s:// URI scheme, encryption is implicit.
                // This setting applies when using bolt:// scheme.
                if (_config.Encrypted)
                {
                    builder.WithEncryptionLevel(EncryptionLevel.Encrypted);
                }
                else
                {
                    builder.WithEncryptionLevel(EncryptionLevel.None);
                }
            });

        _logger.LogInformation(
            "Neo4j driver created for {Uri}, database: {Database}, pool size: {PoolSize}",
            _config.Uri, _config.Database, _config.MaxConnectionPoolSize);
    }

    /// <inheritdoc/>
    public string DatabaseName => _config.Database;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: License enforcement order:
    /// 1. If Write mode requested, check tier >= Teams.
    /// 2. For any mode, check tier >= WriterPro.
    /// 3. Create Neo4j session with appropriate access mode.
    /// 4. Wrap in Neo4jGraphSession with logging and timing.
    /// </remarks>
    public Task<IGraphSession> CreateSessionAsync(
        GraphAccessMode accessMode = GraphAccessMode.Write,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Creating graph session with access mode: {AccessMode}", accessMode);

        // LOGIC: Check write access requires Teams tier.
        if (accessMode == GraphAccessMode.Write &&
            _licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            _logger.LogWarning(
                "License tier {Tier} insufficient for Knowledge Graph write access (requires Teams)",
                _licenseContext.GetCurrentTier());

            throw new FeatureNotLicensedException(
                "Knowledge Graph write access requires a Teams license or higher.",
                LicenseTier.Teams);
        }

        // LOGIC: Check read access requires WriterPro tier.
        if (_licenseContext.GetCurrentTier() < LicenseTier.WriterPro)
        {
            _logger.LogWarning(
                "License tier {Tier} insufficient for Knowledge Graph access (requires WriterPro)",
                _licenseContext.GetCurrentTier());

            throw new FeatureNotLicensedException(
                "Knowledge Graph requires a WriterPro license or higher.",
                LicenseTier.WriterPro);
        }

        // LOGIC: Map Lexichord access mode to Neo4j driver access mode.
        var neo4jAccessMode = accessMode == GraphAccessMode.Read
            ? AccessMode.Read
            : AccessMode.Write;

        // LOGIC: Create the underlying Neo4j session with database and access mode.
        var session = _driver.AsyncSession(builder => builder
            .WithDatabase(_config.Database)
            .WithDefaultAccessMode(neo4jAccessMode));

        _logger.LogDebug(
            "Graph session created: database={Database}, mode={AccessMode}",
            _config.Database, accessMode);

        return Task.FromResult<IGraphSession>(
            new Neo4jGraphSession(session, _config.QueryTimeoutSeconds, _logger));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Uses the driver's built-in VerifyConnectivityAsync() method.
    /// This does NOT enforce license checks — health monitoring should work
    /// regardless of license tier. Failures are caught and logged at Warning
    /// level rather than thrown, returning false instead.
    /// </remarks>
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Testing Neo4j connection to {Uri}", _config.Uri);

            await _driver.VerifyConnectivityAsync();

            _logger.LogDebug("Neo4j connection verified successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Neo4j connection test failed for {Uri}: {Message}",
                _config.Uri, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Disposes the Neo4j driver and all pooled connections.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called during application shutdown. Closes all active connections
    /// and releases the connection pool resources. After disposal, no new
    /// sessions can be created.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing Neo4j driver for {Uri}", _config.Uri);

        try
        {
            await _driver.DisposeAsync();
            _logger.LogInformation("Neo4j driver disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing Neo4j driver");
        }
    }
}
