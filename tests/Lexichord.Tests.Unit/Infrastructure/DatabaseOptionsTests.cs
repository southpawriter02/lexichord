using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for DatabaseOptions configuration.
/// </summary>
public class DatabaseOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeDatabase()
    {
        // Arrange & Act & Assert
        DatabaseOptions.SectionName.Should().Be("Database");
    }

    [Fact]
    public void DefaultValues_ShouldHaveExpectedDefaults()
    {
        // Arrange
        var options = new DatabaseOptions { ConnectionString = "Host=test" };

        // Assert
        options.MaxPoolSize.Should().Be(100);
        options.MinPoolSize.Should().Be(10);
        options.ConnectionLifetimeSeconds.Should().Be(300);
        options.ConnectionTimeoutSeconds.Should().Be(30);
        options.CommandTimeoutSeconds.Should().Be(30);
        options.EnableMultiplexing.Should().BeTrue();
    }

    [Fact]
    public void RetryOptions_ShouldHaveExpectedDefaults()
    {
        // Arrange
        var options = new RetryOptions();

        // Assert
        options.MaxRetryAttempts.Should().Be(4);
        options.BaseDelayMs.Should().Be(1000);
        options.MaxDelayMs.Should().Be(30000);
        options.UseJitter.Should().BeTrue();
    }

    [Fact]
    public void CircuitBreakerOptions_ShouldHaveExpectedDefaults()
    {
        // Arrange
        var options = new CircuitBreakerOptions();

        // Assert
        options.FailureRatio.Should().Be(0.5);
        options.SamplingDurationSeconds.Should().Be(30);
        options.MinimumThroughput.Should().Be(5);
        options.BreakDurationSeconds.Should().Be(30);
    }
}
