using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Lexichord.Abstractions.Contracts;
using Lexichord.Infrastructure.Data;

namespace Lexichord.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for NpgsqlConnectionFactory that require a running PostgreSQL instance.
/// </summary>
/// <remarks>
/// Prerequisites:
/// 1. Run ./scripts/db-start.sh to start the PostgreSQL container
/// 2. Ensure the connection string in test options matches docker-compose settings
/// 
/// These tests are marked with Skip by default. Remove Skip attribute to run.
/// </remarks>
public class ConnectionFactoryIntegrationTests : IDisposable
{
    private readonly NpgsqlConnectionFactory _factory;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;
    private readonly DatabaseOptions _dbOptions;

    public ConnectionFactoryIntegrationTests()
    {
        // Use NullLogger for integration tests (output goes to test runner instead)
        _logger = NullLogger<NpgsqlConnectionFactory>.Instance;

        _dbOptions = new DatabaseOptions
        {
            // Match docker-compose.yml settings
            ConnectionString = "Host=localhost;Port=5432;Database=lexichord;Username=lexichord;Password=lexichord_dev_password",
            MaxPoolSize = 10,
            MinPoolSize = 1,
            ConnectionTimeoutSeconds = 10,
            EnableMultiplexing = true
        };

        _factory = new NpgsqlConnectionFactory(Options.Create(_dbOptions), _logger);
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task CreateConnectionAsync_ShouldReturnOpenConnection()
    {
        // Act
        await using var connection = await _factory.CreateConnectionAsync();

        // Assert
        connection.Should().NotBeNull();
        connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task CreateConnectionAsync_ShouldExecuteSimpleQuery()
    {
        // Arrange
        await using var connection = await _factory.CreateConnectionAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 + 1 AS result";
        var result = await command.ExecuteScalarAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task CanConnectAsync_ShouldReturnTrue_WhenDatabaseIsRunning()
    {
        // Act
        var canConnect = await _factory.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task CreateConnectionAsync_ShouldPoolConnections()
    {
        // Act - Create multiple connections
        var connections = new List<Npgsql.NpgsqlConnection>();
        for (int i = 0; i < 5; i++)
        {
            connections.Add(await _factory.CreateConnectionAsync());
        }

        // Assert - DataSource should be accessible (pool stats not available in Npgsql 9.x public API)
        _factory.DataSource.Should().NotBeNull();

        // All connections should be open
        connections.Should().AllSatisfy(c => c.State.Should().Be(System.Data.ConnectionState.Open));

        // Cleanup - return connections to pool
        foreach (var conn in connections)
        {
            await conn.DisposeAsync();
        }
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task CreateConnectionAsync_ShouldHandleMultipleConcurrentConnections()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10)
            .Select(async i =>
            {
                await using var connection = await _factory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT {i} AS value";
                return await command.ExecuteScalarAsync();
            });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().BeEquivalentTo(Enumerable.Range(0, 10).Select(i => (object)i));
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
