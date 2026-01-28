using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace Lexichord.Tests.Integration;

/// <summary>
/// Integration tests verifying Docker Desktop connectivity.
/// These tests require Docker Desktop to be installed and running.
/// </summary>
/// <remarks>
/// LOGIC: Integration tests are marked with the "Integration" trait to allow
/// developers without Docker to skip them using:
/// <code>dotnet test --filter Category!=Integration</code>
/// </remarks>
[Trait("Category", "Integration")]
public class DockerConnectivityTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;

    /// <summary>
    /// Initializes the PostgreSQL container before each test.
    /// </summary>
    public Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("lexichord_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the PostgreSQL container after each test.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that Docker Desktop is reachable and can spin up a PostgreSQL container.
    /// </summary>
    /// <remarks>
    /// LOGIC: This test is the "canary in the coal mine" for Docker integration.
    /// If this test fails:
    /// 1. Verify Docker Desktop is running.
    /// 2. Verify the user has permission to create containers.
    /// 3. Check for port conflicts on the default PostgreSQL port range.
    /// </remarks>
    [Fact]
    public async Task Docker_StartsPostgreSqlContainer_WhenDockerDesktopIsRunning()
    {
        // Arrange
        _postgresContainer.Should().NotBeNull("PostgreSQL container should be initialized");

        // Act
        await _postgresContainer!.StartAsync();

        // Assert
        _postgresContainer.State.Should().Be(TestcontainersStates.Running,
            because: "the PostgreSQL container should be running after StartAsync completes");

        // Cleanup is handled by DisposeAsync via IAsyncLifetime
    }

    /// <summary>
    /// Verifies that we can establish a connection to the containerized PostgreSQL instance.
    /// </summary>
    [Fact]
    public async Task PostgreSql_AcceptsConnections_WhenContainerIsRunning()
    {
        // Arrange
        await _postgresContainer!.StartAsync();
        var connectionString = _postgresContainer.GetConnectionString();

        // Act
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Assert
        connection.State.Should().Be(System.Data.ConnectionState.Open,
            because: "we should be able to connect to the PostgreSQL container");

        // Verify we can execute a simple query
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        var result = await command.ExecuteScalarAsync();
        result.Should().Be(1, because: "the database should respond to queries");
    }

    /// <summary>
    /// Verifies that the container gracefully stops when disposed.
    /// </summary>
    [Fact]
    public async Task Docker_StopsContainer_WhenDisposed()
    {
        // Arrange
        await _postgresContainer!.StartAsync();
        _postgresContainer.State.Should().Be(TestcontainersStates.Running);

        // Act
        await _postgresContainer.StopAsync();

        // Assert
        _postgresContainer.State.Should().Be(TestcontainersStates.Exited,
            because: "the container should stop gracefully when StopAsync is called");
    }
}
