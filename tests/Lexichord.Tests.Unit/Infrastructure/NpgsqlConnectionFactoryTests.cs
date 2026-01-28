using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lexichord.Abstractions.Contracts;
using Lexichord.Infrastructure.Data;

namespace Lexichord.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for NpgsqlConnectionFactory.
/// </summary>
public class NpgsqlConnectionFactoryTests
{
    private readonly Mock<ILogger<NpgsqlConnectionFactory>> _mockLogger;
    private readonly IOptions<DatabaseOptions> _options;

    public NpgsqlConnectionFactoryTests()
    {
        _mockLogger = new Mock<ILogger<NpgsqlConnectionFactory>>();
        _options = Options.Create(new DatabaseOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            MaxPoolSize = 10,
            MinPoolSize = 1
        });
    }

    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Arrange & Act
        using var factory = new NpgsqlConnectionFactory(_options, _mockLogger.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database connection factory initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void DataSource_ShouldNotBeNull()
    {
        // Arrange
        using var factory = new NpgsqlConnectionFactory(_options, _mockLogger.Object);

        // Act & Assert
        factory.DataSource.Should().NotBeNull();
    }

    [Fact]
    public void IsHealthy_ShouldBeTrueInitially()
    {
        // Arrange
        using var factory = new NpgsqlConnectionFactory(_options, _mockLogger.Object);

        // Act & Assert
        factory.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldLogDisposal()
    {
        // Arrange
        var factory = new NpgsqlConnectionFactory(_options, _mockLogger.Object);

        // Act
        factory.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
