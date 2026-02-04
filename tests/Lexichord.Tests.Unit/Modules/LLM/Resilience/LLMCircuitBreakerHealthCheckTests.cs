// -----------------------------------------------------------------------
// <copyright file="LLMCircuitBreakerHealthCheckTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Resilience;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Resilience;

/// <summary>
/// Unit tests for <see cref="LLMCircuitBreakerHealthCheck"/>.
/// </summary>
public class LLMCircuitBreakerHealthCheckTests
{
    private readonly Mock<IResiliencePipeline> _mockPipeline;
    private readonly ILogger<LLMCircuitBreakerHealthCheck> _logger;

    public LLMCircuitBreakerHealthCheckTests()
    {
        _mockPipeline = new Mock<IResiliencePipeline>();
        _logger = NullLogger<LLMCircuitBreakerHealthCheck>.Instance;
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor throws when pipeline is null.
    /// </summary>
    [Fact]
    public void Constructor_NullPipeline_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new LLMCircuitBreakerHealthCheck(null!, _logger);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipeline");
    }

    /// <summary>
    /// Tests that constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that constructor succeeds with valid arguments.
    /// </summary>
    [Fact]
    public void Constructor_ValidArguments_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);
        act.Should().NotThrow();
    }

    #endregion

    #region Name Constant Tests

    /// <summary>
    /// Tests that the Name constant has the expected value.
    /// </summary>
    [Fact]
    public void Name_ShouldBeExpectedValue()
    {
        // Assert
        LLMCircuitBreakerHealthCheck.Name.Should().Be("llm-circuit-breaker");
    }

    #endregion

    #region CheckHealthAsync Tests - Closed State

    /// <summary>
    /// Tests that Closed circuit state returns Healthy status.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitClosed_ShouldReturnHealthy()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Closed);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    /// <summary>
    /// Tests that Closed circuit state includes appropriate description.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitClosed_ShouldIncludeDescription()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Closed);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Description.Should().Contain("closed");
        result.Description.Should().Contain("normally");
    }

    /// <summary>
    /// Tests that Closed circuit state includes circuit state in data.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitClosed_ShouldIncludeStateInData()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Closed);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("circuit_state");
        result.Data["circuit_state"].Should().Be("Closed");
    }

    #endregion

    #region CheckHealthAsync Tests - HalfOpen State

    /// <summary>
    /// Tests that HalfOpen circuit state returns Degraded status.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitHalfOpen_ShouldReturnDegraded()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.HalfOpen);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
    }

    /// <summary>
    /// Tests that HalfOpen circuit state includes appropriate description.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitHalfOpen_ShouldIncludeDescription()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.HalfOpen);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Description.Should().Contain("half-open");
        result.Description.Should().Contain("Testing");
    }

    /// <summary>
    /// Tests that HalfOpen circuit state includes circuit state in data.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitHalfOpen_ShouldIncludeStateInData()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.HalfOpen);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("circuit_state");
        result.Data["circuit_state"].Should().Be("HalfOpen");
    }

    #endregion

    #region CheckHealthAsync Tests - Open State

    /// <summary>
    /// Tests that Open circuit state returns Unhealthy status.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitOpen_ShouldReturnUnhealthy()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Open);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    /// <summary>
    /// Tests that Open circuit state includes appropriate description.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitOpen_ShouldIncludeDescription()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Open);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Description.Should().Contain("open");
        result.Description.Should().Contain("failures");
    }

    /// <summary>
    /// Tests that Open circuit state includes circuit state in data.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitOpen_ShouldIncludeStateInData()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Open);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("circuit_state");
        result.Data["circuit_state"].Should().Be("Open");
    }

    #endregion

    #region CheckHealthAsync Tests - Isolated State

    /// <summary>
    /// Tests that Isolated circuit state returns Unhealthy status.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitIsolated_ShouldReturnUnhealthy()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Isolated);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    /// <summary>
    /// Tests that Isolated circuit state includes appropriate description.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitIsolated_ShouldIncludeDescription()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Isolated);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Description.Should().Contain("isolated");
        result.Description.Should().Contain("rejected");
    }

    /// <summary>
    /// Tests that Isolated circuit state includes circuit state in data.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_CircuitIsolated_ShouldIncludeStateInData()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Isolated);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("circuit_state");
        result.Data["circuit_state"].Should().Be("Isolated");
    }

    #endregion

    #region CheckHealthAsync Tests - All States Coverage

    /// <summary>
    /// Tests that all circuit states return appropriate health statuses.
    /// </summary>
    [Theory]
    [InlineData(CircuitState.Closed, HealthStatus.Healthy)]
    [InlineData(CircuitState.HalfOpen, HealthStatus.Degraded)]
    [InlineData(CircuitState.Open, HealthStatus.Unhealthy)]
    [InlineData(CircuitState.Isolated, HealthStatus.Unhealthy)]
    public async Task CheckHealthAsync_AllStates_ShouldReturnCorrectStatus(
        CircuitState circuitState,
        HealthStatus expectedStatus)
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(circuitState);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(expectedStatus);
    }

    #endregion

    #region Cancellation Tests

    /// <summary>
    /// Tests that cancellation token does not affect result (no async work).
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithCancellationToken_ShouldCompleteNormally()
    {
        // Arrange
        _mockPipeline.Setup(p => p.CircuitState).Returns(CircuitState.Closed);
        var healthCheck = new LLMCircuitBreakerHealthCheck(_mockPipeline.Object, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    #endregion
}
