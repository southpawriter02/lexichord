using Lexichord.Abstractions.Contracts.Ingestion;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Unit tests for the <see cref="IngestionQueueOptions"/> record.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the default values, validation logic,
/// and preset configurations for queue options.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.2d")]
public class IngestionQueueOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Default_HasExpectedValues()
    {
        // Act
        var options = IngestionQueueOptions.Default;

        // Assert
        options.MaxQueueSize.Should().Be(1000);
        options.ThrottleDelayMs.Should().Be(100);
        options.EnableDuplicateDetection.Should().BeTrue();
        options.DuplicateWindowSeconds.Should().Be(60);
        options.MaxConcurrentProcessing.Should().Be(1);
        options.ShutdownTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void ParameterlessConstructor_HasExpectedDefaults()
    {
        // Act
        var options = new IngestionQueueOptions();

        // Assert
        options.MaxQueueSize.Should().Be(1000);
        options.ThrottleDelayMs.Should().Be(100);
        options.EnableDuplicateDetection.Should().BeTrue();
        options.DuplicateWindowSeconds.Should().Be(60);
        options.MaxConcurrentProcessing.Should().Be(1);
        options.ShutdownTimeoutSeconds.Should().Be(30);
    }

    #endregion

    #region Preset Configurations Tests

    [Fact]
    public void HighThroughput_HasExpectedValues()
    {
        // Act
        var options = IngestionQueueOptions.HighThroughput;

        // Assert
        options.MaxQueueSize.Should().Be(5000);
        options.ThrottleDelayMs.Should().Be(0);
        options.EnableDuplicateDetection.Should().BeTrue();
        options.DuplicateWindowSeconds.Should().Be(30);
        options.MaxConcurrentProcessing.Should().Be(4);
        options.ShutdownTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void LowLatency_HasExpectedValues()
    {
        // Act
        var options = IngestionQueueOptions.LowLatency;

        // Assert
        options.MaxQueueSize.Should().Be(100);
        options.ThrottleDelayMs.Should().Be(50);
        options.EnableDuplicateDetection.Should().BeTrue();
        options.DuplicateWindowSeconds.Should().Be(10);
        options.MaxConcurrentProcessing.Should().Be(1);
        options.ShutdownTimeoutSeconds.Should().Be(10);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        // Arrange
        var options = IngestionQueueOptions.Default;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroMaxQueueSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new IngestionQueueOptions(MaxQueueSize: 0);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("MaxQueueSize");
    }

    [Fact]
    public void Validate_NegativeMaxQueueSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new IngestionQueueOptions(MaxQueueSize: -1);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("MaxQueueSize");
    }

    [Fact]
    public void Validate_NegativeThrottleDelayMs_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new IngestionQueueOptions(ThrottleDelayMs: -1);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("ThrottleDelayMs");
    }

    [Fact]
    public void Validate_ZeroThrottleDelayMs_DoesNotThrow()
    {
        // Arrange
        var options = new IngestionQueueOptions(ThrottleDelayMs: 0);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroDuplicateWindowSeconds_WhenEnabled_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new IngestionQueueOptions(
            EnableDuplicateDetection: true,
            DuplicateWindowSeconds: 0);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("DuplicateWindowSeconds");
    }

    [Fact]
    public void Validate_ZeroDuplicateWindowSeconds_WhenDisabled_DoesNotThrow()
    {
        // Arrange
        var options = new IngestionQueueOptions(
            EnableDuplicateDetection: false,
            DuplicateWindowSeconds: 0);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroMaxConcurrentProcessing_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new IngestionQueueOptions(MaxConcurrentProcessing: 0);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("MaxConcurrentProcessing");
    }

    [Fact]
    public void Validate_ZeroShutdownTimeoutSeconds_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new IngestionQueueOptions(ShutdownTimeoutSeconds: 0);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("ShutdownTimeoutSeconds");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var options1 = new IngestionQueueOptions(500, 50, true, 30, 2, 15);
        var options2 = new IngestionQueueOptions(500, 50, true, 30, 2, 15);

        // Assert
        options1.Should().Be(options2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var options1 = new IngestionQueueOptions(500, 50, true, 30, 2, 15);
        var options2 = new IngestionQueueOptions(1000, 50, true, 30, 2, 15);

        // Assert
        options1.Should().NotBe(options2);
    }

    #endregion
}
