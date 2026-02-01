// =============================================================================
// File: Neo4jGraphSessionTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Neo4jGraphSession internal behavior.
// =============================================================================
// LOGIC: Tests the Neo4jGraphSession's logging, timing, and exception wrapping
//   using mocked Neo4j driver types. These tests verify the session's behavior
//   without requiring a running Neo4j instance.
//
// Note: Neo4jGraphSession is internal but visible to the test project via
//   InternalsVisibleTo in the Knowledge module .csproj.
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Graph;
using Lexichord.Tests.Unit.TestUtilities;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="Neo4jGraphSession"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5e")]
public sealed class Neo4jGraphSessionTests
{
    private readonly Mock<IAsyncSession> _sessionMock = new();
    private readonly FakeLogger<Neo4jConnectionFactory> _logger = new();

    /// <summary>
    /// Creates a Neo4jGraphSession wrapping the mocked session.
    /// </summary>
    private Neo4jGraphSession CreateSession(int queryTimeoutSeconds = 60)
    {
        return new Neo4jGraphSession(_sessionMock.Object, queryTimeoutSeconds, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Neo4jGraphSession(null!, 60, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("session");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Neo4jGraphSession(_sessionMock.Object, 60, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region SlowQueryThreshold Tests

    [Fact]
    public void SlowQueryThresholdMs_Is100()
    {
        // Assert
        Neo4jGraphSession.SlowQueryThresholdMs.Should().Be(100);
    }

    #endregion

    #region QueryAsync Exception Wrapping Tests

    [Fact]
    public async Task QueryAsync_Neo4jException_WrapsInGraphQueryException()
    {
        // Arrange
        var session = CreateSession();
        _sessionMock.Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ThrowsAsync(new ClientException("Invalid Cypher"));

        // Act
        var act = () => session.QueryAsync<int>("BAD CYPHER");

        // Assert
        await act.Should().ThrowAsync<GraphQueryException>()
            .WithMessage("*Graph query failed*");
    }

    [Fact]
    public async Task QueryAsync_Neo4jException_PreservesInnerException()
    {
        // Arrange
        var session = CreateSession();
        var neo4jEx = new ClientException("Neo4j error");
        _sessionMock.Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ThrowsAsync(neo4jEx);

        // Act
        GraphQueryException? caught = null;
        try { await session.QueryAsync<int>("BAD CYPHER"); }
        catch (GraphQueryException ex) { caught = ex; }

        // Assert
        caught.Should().NotBeNull();
        caught!.InnerException.Should().BeSameAs(neo4jEx);
    }

    [Fact]
    public async Task QueryAsync_Neo4jException_LogsError()
    {
        // Arrange
        var session = CreateSession();
        _sessionMock.Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ThrowsAsync(new ClientException("test error"));

        // Act
        try { await session.QueryAsync<int>("MATCH (n) RETURN n"); }
        catch (GraphQueryException) { }

        // Assert
        _logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Error &&
            log.Message.Contains("Cypher query failed"));
    }

    #endregion

    #region ExecuteAsync Exception Wrapping Tests

    [Fact]
    public async Task ExecuteAsync_Neo4jException_WrapsInGraphQueryException()
    {
        // Arrange
        var session = CreateSession();
        _sessionMock.Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ThrowsAsync(new ClientException("Write failed"));

        // Act
        var act = () => session.ExecuteAsync("CREATE (n:Bad)");

        // Assert
        await act.Should().ThrowAsync<GraphQueryException>()
            .WithMessage("*Graph write failed*");
    }

    #endregion

    #region QueryRawAsync Exception Wrapping Tests

    [Fact]
    public async Task QueryRawAsync_Neo4jException_WrapsInGraphQueryException()
    {
        // Arrange
        var session = CreateSession();
        _sessionMock.Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ThrowsAsync(new ClientException("Query failed"));

        // Act
        var act = () => session.QueryRawAsync("BAD QUERY");

        // Assert
        await act.Should().ThrowAsync<GraphQueryException>();
    }

    #endregion

    #region BeginTransactionAsync Tests

    [Fact]
    public async Task BeginTransactionAsync_Neo4jException_WrapsInGraphQueryException()
    {
        // Arrange
        var session = CreateSession();
        _sessionMock.Setup(s => s.BeginTransactionAsync())
            .ThrowsAsync(new ClientException("Transaction failed"));

        // Act
        var act = () => session.BeginTransactionAsync();

        // Assert
        await act.Should().ThrowAsync<GraphQueryException>()
            .WithMessage("*Failed to begin transaction*");
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_CallsSessionClose()
    {
        // Arrange
        var session = CreateSession();

        // Act
        await session.DisposeAsync();

        // Assert
        _sessionMock.Verify(s => s.CloseAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_CloseThrows_SwallowsException()
    {
        // Arrange
        var session = CreateSession();
        _sessionMock.Setup(s => s.CloseAsync()).ThrowsAsync(new InvalidOperationException("already closed"));

        // Act — should NOT throw
        await session.DisposeAsync();

        // Assert
        _logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Warning &&
            log.Message.Contains("Error closing graph session"));
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task QueryAsync_LogsDebugOnExecution()
    {
        // Arrange — make RunAsync throw so we can still verify the debug log
        // (which is emitted before the driver call).
        var session = CreateSession();
        _sessionMock.Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ThrowsAsync(new ClientException("test"));

        // Act — ignore the exception; we only care about logging
        try { await session.QueryAsync<int>("RETURN 1"); }
        catch (GraphQueryException) { }

        // Assert
        _logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Debug &&
            log.Message.Contains("Executing Cypher query"));
    }

    #endregion
}
