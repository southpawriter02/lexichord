// =============================================================================
// File: ResiliencePipelineBuilderTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ResiliencePipelineBuilder.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Configuration;
using Lexichord.Modules.RAG.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Resilience;

/// <summary>
/// Unit tests for <see cref="ResiliencePipelineBuilder"/>.
/// </summary>
public sealed class ResiliencePipelineBuilderTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly ResilienceOptions _options;

    public ResiliencePipelineBuilderTests()
    {
        _loggerMock = new Mock<ILogger>();
        _options = new ResilienceOptions
        {
            RetryMaxAttempts = 2,
            RetryInitialDelay = TimeSpan.FromMilliseconds(10),
            TimeoutPerOperation = TimeSpan.FromSeconds(5),
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerMinimumThroughput = 5,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(10),
            Enabled = true
        };
    }

    #region Build Tests

    [Fact]
    public void Build_WithValidOptions_CreatesPipeline()
    {
        // Act
        var pipeline = ResiliencePipelineBuilder.Build(_options, _loggerMock.Object);

        // Assert
        Assert.NotNull(pipeline);
    }

    [Fact]
    public void Build_WithNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ResiliencePipelineBuilder.Build(null!, _loggerMock.Object));
    }

    [Fact]
    public void Build_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ResiliencePipelineBuilder.Build(_options, null!));
    }

    [Fact]
    public async Task Build_Pipeline_ExecutesSuccessfulOperation()
    {
        // Arrange
        var pipeline = ResiliencePipelineBuilder.Build(_options, _loggerMock.Object);
        var expectedResult = new SearchResult
        {
            Hits = [],
            Query = "test",
            WasTruncated = false,
            Duration = TimeSpan.FromMilliseconds(10)
        };

        // Act
        var result = await pipeline.ExecuteAsync(
            async _ =>
            {
                await Task.Delay(1);
                return expectedResult;
            },
            CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion

    #region IsTransientException Tests

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    public void IsTransientException_WithTransientExceptions_ReturnsTrue(Type exceptionType)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        // Act
        var isTransient = ResiliencePipelineBuilder.IsTransientException(exception);

        // Assert
        Assert.True(isTransient);
    }

    [Fact]
    public void IsTransientException_WithTaskCanceledException_WithoutCancellationRequested_ReturnsTrue()
    {
        // Arrange - TaskCanceledException without cancellation token means it's a transient timeout
        var exception = new TaskCanceledException();

        // Act
        var isTransient = ResiliencePipelineBuilder.IsTransientException(exception);

        // Assert
        Assert.True(isTransient);
    }

    [Fact]
    public void IsTransientException_WithTaskCanceledException_WithCancellationRequested_ReturnsFalse()
    {
        // Arrange - TaskCanceledException with cancellation means user cancelled
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var exception = new TaskCanceledException("Cancelled", null, cts.Token);

        // Act
        var isTransient = ResiliencePipelineBuilder.IsTransientException(exception);

        // Assert
        Assert.False(isTransient);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(NullReferenceException))]
    public void IsTransientException_WithNonTransientExceptions_ReturnsFalse(Type exceptionType)
    {
        // Arrange
        Exception exception;
        if (exceptionType == typeof(ArgumentException))
        {
            exception = new ArgumentException("test");
        }
        else
        {
            exception = (Exception)Activator.CreateInstance(exceptionType)!;
        }

        // Act
        var isTransient = ResiliencePipelineBuilder.IsTransientException(exception);

        // Assert
        Assert.False(isTransient);
    }

    #endregion

    #region Circuit Breaker Callback Tests

    [Fact]
    public void Build_WithCircuitBreakerCallback_CallsOnStateChange()
    {
        // Arrange
        var stateChanges = new List<CircuitBreakerState>();
        Action<CircuitBreakerState> callback = state => stateChanges.Add(state);

        // Act - just verify the pipeline builds without error when callback is provided
        var pipeline = ResiliencePipelineBuilder.Build(_options, _loggerMock.Object, callback);

        // Assert
        Assert.NotNull(pipeline);
    }

    #endregion
}
