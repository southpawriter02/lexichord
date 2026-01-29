using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// SQLite implementation of the health repository.
/// </summary>
/// <remarks>
/// LOGIC: This repository demonstrates proper database access patterns:
/// - Uses Microsoft.Data.Sqlite for connection management
/// - Runs migrations on construction (ensure table exists)
/// - Uses parameterized queries to prevent SQL injection
/// - Handles errors gracefully with logging
///
/// The singleton row pattern (id=1) ensures only one health record exists.
/// This simplifies queries and updates.
/// </remarks>
public sealed class HealthRepository : IHealthRepository
{
    private readonly ILogger<HealthRepository> _logger;
    private readonly DateTime _startupTime;
    private readonly string _connectionString;

    private const int CurrentSchemaVersion = 1;
    private const string TableName = "system_health";

    public HealthRepository(ILogger<HealthRepository> logger)
    {
        _logger = logger;
        _startupTime = DateTime.UtcNow;

        // LOGIC: Store SQLite database in user's config directory
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Lexichord");
        Directory.CreateDirectory(configDir);

        var dbPath = Path.Combine(configDir, "health.db");
        _connectionString = $"Data Source={dbPath}";

        // LOGIC: Ensure table exists on construction
        // This is safe because it uses CREATE TABLE IF NOT EXISTS
        EnsureTableExistsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Ensures the system_health table exists.
    /// </summary>
    private async Task EnsureTableExistsAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string createTableSql = """
                CREATE TABLE IF NOT EXISTS system_health (
                    id INTEGER PRIMARY KEY CHECK (id = 1),
                    started_at TEXT NOT NULL,
                    last_heartbeat TEXT NOT NULL,
                    database_version INTEGER NOT NULL
                )
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = createTableSql;
            await command.ExecuteNonQueryAsync();

            _logger.LogDebug("System health table verified/created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure system_health table exists");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<TimeSpan> GetSystemUptimeAsync()
    {
        // LOGIC: Calculate uptime from in-memory startup time
        // This is more accurate than reading from database
        var uptime = DateTime.UtcNow - _startupTime;
        return Task.FromResult(uptime);
    }

    /// <inheritdoc/>
    public async Task<DateTime?> GetLastHeartbeatAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = $"SELECT last_heartbeat FROM {TableName} WHERE id = 1";

            var result = await command.ExecuteScalarAsync();

            if (result is null or DBNull)
            {
                _logger.LogDebug("No heartbeat record found");
                return null;
            }

            var timestamp = DateTime.Parse((string)result, null,
                System.Globalization.DateTimeStyles.RoundtripKind);

            _logger.LogDebug("Last heartbeat: {Timestamp}", timestamp);
            return timestamp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last heartbeat");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task RecordHeartbeatAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();

            var now = DateTime.UtcNow.ToString("O");

            command.CommandText = $"""
                UPDATE {TableName}
                SET last_heartbeat = @heartbeat
                WHERE id = 1
                """;
            command.Parameters.AddWithValue("@heartbeat", now);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                _logger.LogDebug("Heartbeat recorded at {Timestamp}", now);
            }
            else
            {
                _logger.LogWarning("Heartbeat update affected 0 rows - record may not exist");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record heartbeat");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RecordStartupAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();

            var now = DateTime.UtcNow.ToString("O");

            // LOGIC: Upsert pattern - insert if not exists, update if exists
            // The CHECK constraint (id = 1) ensures only one row ever exists
            command.CommandText = $"""
                INSERT INTO {TableName} (id, started_at, last_heartbeat, database_version)
                VALUES (1, @started, @heartbeat, @version)
                ON CONFLICT(id) DO UPDATE SET
                    started_at = @started,
                    last_heartbeat = @heartbeat,
                    database_version = @version
                """;

            command.Parameters.AddWithValue("@started", now);
            command.Parameters.AddWithValue("@heartbeat", now);
            command.Parameters.AddWithValue("@version", CurrentSchemaVersion);

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Application startup recorded at {Timestamp}", now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record startup");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetDatabaseVersionAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = $"SELECT database_version FROM {TableName} WHERE id = 1";

            var result = await command.ExecuteScalarAsync();

            if (result is null or DBNull)
                return 0;

            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database version");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();

            // Simple query to verify database is responsive
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync();

            return result is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return false;
        }
    }
}
