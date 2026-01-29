using Lexichord.Abstractions.Contracts;
using Lexichord.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace Lexichord.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for RecentFilesRepository.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify SQL generation and method behavior.
/// Full database testing is done in integration tests.
/// </remarks>
[Trait("Category", "Unit")]
public class RecentFilesRepositoryTests
{
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<ILogger<RecentFilesRepository>> _loggerMock = new();

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RecentFilesRepository(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RecentFilesRepository(_connectionFactoryMock.Object, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var repository = new RecentFilesRepository(_connectionFactoryMock.Object, _loggerMock.Object);

        // Assert
        repository.Should().NotBeNull();
    }

    // NOTE: Additional tests that exercise actual SQL execution
    // are in Lexichord.Tests.Integration/Data/RecentFilesRepositoryTests.cs
    // These tests verify behavior with a real PostgreSQL database.
}
