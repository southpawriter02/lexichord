// =============================================================================
// File: Neo4jConnectionTests.cs
// Project: Lexichord.Tests.Integration
// Description: Integration tests for Neo4j graph database connectivity.
// =============================================================================
// LOGIC: End-to-end tests that verify the Neo4j integration works correctly
//   with a running Neo4j instance. All tests are skipped by default and
//   require a running Neo4j container (docker compose up neo4j -d).
//
// Prerequisites:
//   - Neo4j 5.x running on localhost:7687
//   - Auth: neo4j/lexichord_dev_password
//   - Run: docker compose up neo4j -d
//
// Test Coverage:
//   - Connection test via IGraphConnectionFactory
//   - Session creation with Teams license
//   - Simple Cypher query execution
//   - Node CRUD operations
//   - KnowledgeEntity mapping from graph nodes
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.Knowledge.Graph;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lexichord.Tests.Integration.Knowledge;

/// <summary>
/// Integration tests for Neo4j graph database connectivity.
/// </summary>
/// <remarks>
/// LOGIC: These tests require a running Neo4j instance. They are skipped by default
/// and can be enabled by removing the Skip parameter and starting Neo4j via Docker Compose.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.5e")]
public sealed class Neo4jConnectionTests : IAsyncDisposable
{
    /// <summary>
    /// Creates a mock ISecureVault that returns the development password.
    /// </summary>
    private static ISecureVault CreateDevVault()
    {
        var mock = new Moq.Mock<ISecureVault>();
        mock.Setup(v => v.GetSecretAsync("neo4j:password", It.IsAny<CancellationToken>()))
            .ReturnsAsync("lexichord_dev_password");
        return mock.Object;
    }

    /// <summary>
    /// Creates a mock ILicenseContext at the specified tier.
    /// </summary>
    private static ILicenseContext CreateLicense(LicenseTier tier = LicenseTier.Teams)
    {
        var mock = new Moq.Mock<ILicenseContext>();
        mock.Setup(l => l.GetCurrentTier()).Returns(tier);
        return mock.Object;
    }

    /// <summary>
    /// Creates a Neo4jConnectionFactory configured for local Docker development.
    /// </summary>
    private static Neo4jConnectionFactory CreateFactory(LicenseTier tier = LicenseTier.Teams)
    {
        var config = Options.Create(new GraphConfiguration
        {
            Uri = "bolt://localhost:7687",
            Database = "neo4j",
            Username = "neo4j",
            MaxConnectionPoolSize = 10,
            ConnectionTimeoutSeconds = 10,
            QueryTimeoutSeconds = 30
        });

        return new Neo4jConnectionFactory(
            config,
            CreateDevVault(),
            CreateLicense(tier),
            NullLogger<Neo4jConnectionFactory>.Instance);
    }

    private Neo4jConnectionFactory? _factory;

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    #region Connection Tests

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task TestConnectionAsync_WhenNeo4jRunning_ReturnsTrue()
    {
        // Arrange
        _factory = CreateFactory();

        // Act
        var result = await _factory.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public Task DatabaseName_ReturnsNeo4j()
    {
        // Arrange
        _factory = CreateFactory();

        // Assert
        _factory.DatabaseName.Should().Be("neo4j");
        return Task.CompletedTask;
    }

    #endregion

    #region Session Creation Tests

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task CreateSessionAsync_TeamsLicense_ReturnsSession()
    {
        // Arrange
        _factory = CreateFactory(LicenseTier.Teams);

        // Act
        await using var session = await _factory.CreateSessionAsync(GraphAccessMode.Write);

        // Assert
        session.Should().NotBeNull();
    }

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task CreateSessionAsync_WriterProRead_ReturnsSession()
    {
        // Arrange
        _factory = CreateFactory(LicenseTier.WriterPro);

        // Act
        await using var session = await _factory.CreateSessionAsync(GraphAccessMode.Read);

        // Assert
        session.Should().NotBeNull();
    }

    #endregion

    #region Query Tests

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task QueryAsync_SimpleReturn_ReturnsResults()
    {
        // Arrange
        _factory = CreateFactory();
        await using var session = await _factory.CreateSessionAsync();

        // Act
        var results = await session.QueryAsync<int>("RETURN 1 AS value");

        // Assert
        results.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task QueryAsync_StringResult_ReturnsCorrectly()
    {
        // Arrange
        _factory = CreateFactory();
        await using var session = await _factory.CreateSessionAsync();

        // Act
        var results = await session.QueryAsync<string>("RETURN 'hello' AS greeting");

        // Assert
        results.Should().ContainSingle().Which.Should().Be("hello");
    }

    #endregion

    #region CRUD Tests

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task ExecuteAsync_CreateNode_ReturnsWriteResult()
    {
        // Arrange
        _factory = CreateFactory();
        await using var session = await _factory.CreateSessionAsync(GraphAccessMode.Write);
        var testId = Guid.NewGuid().ToString();

        try
        {
            // Act
            var result = await session.ExecuteAsync(
                "CREATE (n:TestEntity {id: $id, name: $name})",
                new { id = testId, name = "IntegrationTest" });

            // Assert
            result.NodesCreated.Should().Be(1);
            result.TotalAffected.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            await session.ExecuteAsync(
                "MATCH (n:TestEntity {id: $id}) DELETE n",
                new { id = testId });
        }
    }

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task QueryAsync_KnowledgeEntity_MapsCorrectly()
    {
        // Arrange
        _factory = CreateFactory();
        await using var session = await _factory.CreateSessionAsync(GraphAccessMode.Write);
        var testId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow.ToString("o");

        try
        {
            // Create test node
            await session.ExecuteAsync(
                "CREATE (n:Product {id: $id, name: $name, version: $version, createdAt: $createdAt, modifiedAt: $modifiedAt})",
                new { id = testId, name = "TestProduct", version = "1.0", createdAt = now, modifiedAt = now });

            // Act
            var results = await session.QueryAsync<KnowledgeEntity>(
                "MATCH (n:Product {id: $id}) RETURN n",
                new { id = testId });

            // Assert
            results.Should().ContainSingle();
            var entity = results[0];
            entity.Type.Should().Be("Product");
            entity.Name.Should().Be("TestProduct");
            entity.Id.Should().Be(Guid.Parse(testId));
            entity.Properties.Should().ContainKey("version");
            entity.Properties["version"].Should().Be("1.0");
        }
        finally
        {
            // Cleanup
            await session.ExecuteAsync(
                "MATCH (n:Product {id: $id}) DELETE n",
                new { id = testId });
        }
    }

    #endregion

    #region Transaction Tests

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task Transaction_CommitAndQuery_DataPersists()
    {
        // Arrange
        _factory = CreateFactory();
        await using var session = await _factory.CreateSessionAsync(GraphAccessMode.Write);
        var testId = Guid.NewGuid().ToString();

        try
        {
            // Act — create node in transaction
            await using (var tx = await session.BeginTransactionAsync())
            {
                await tx.ExecuteAsync(
                    "CREATE (n:TestEntity {id: $id, name: $name})",
                    new { id = testId, name = "TxTest" });
                await tx.CommitAsync();
            }

            // Query outside transaction
            var results = await session.QueryAsync<string>(
                "MATCH (n:TestEntity {id: $id}) RETURN n.name AS name",
                new { id = testId });

            // Assert
            results.Should().ContainSingle().Which.Should().Be("TxTest");
        }
        finally
        {
            // Cleanup
            await session.ExecuteAsync(
                "MATCH (n:TestEntity {id: $id}) DELETE n",
                new { id = testId });
        }
    }

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task Transaction_Rollback_DataNotPersisted()
    {
        // Arrange
        _factory = CreateFactory();
        await using var session = await _factory.CreateSessionAsync(GraphAccessMode.Write);
        var testId = Guid.NewGuid().ToString();

        // Act — create node in transaction then rollback
        await using (var tx = await session.BeginTransactionAsync())
        {
            await tx.ExecuteAsync(
                "CREATE (n:TestEntity {id: $id, name: $name})",
                new { id = testId, name = "RollbackTest" });
            await tx.RollbackAsync();
        }

        // Query outside transaction — node should not exist
        var results = await session.QueryAsync<string>(
            "MATCH (n:TestEntity {id: $id}) RETURN n.name AS name",
            new { id = testId });

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region Health Check Tests

    [Fact(Skip = "Requires running Neo4j container. Run 'docker compose up neo4j -d' first.")]
    public async Task Neo4jHealthCheck_WhenConnected_ReturnsHealthy()
    {
        // Arrange
        _factory = CreateFactory();
        var healthCheck = new Neo4jHealthCheck(_factory);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        // Assert
        result.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
    }

    #endregion
}
