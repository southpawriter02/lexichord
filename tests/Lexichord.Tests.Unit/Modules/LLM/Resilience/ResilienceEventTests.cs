// -----------------------------------------------------------------------
// <copyright file="ResilienceEventTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Resilience;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Resilience;

/// <summary>
/// Unit tests for <see cref="ResilienceEvent"/>.
/// </summary>
public class ResilienceEventTests
{
    #region Factory Method Tests - CreateRetry

    /// <summary>
    /// Tests that CreateRetry creates an event with correct properties.
    /// </summary>
    [Fact]
    public void CreateRetry_ShouldCreateEventWithCorrectProperties()
    {
        // Act
        var evt = ResilienceEvent.CreateRetry(1, TimeSpan.FromSeconds(2));

        // Assert
        evt.PolicyName.Should().Be(ResilienceEvent.PolicyNames.Retry);
        evt.EventType.Should().Be(ResilienceEvent.RetryEventTypes.Retry);
        evt.AttemptNumber.Should().Be(1);
        evt.Duration.Should().Be(TimeSpan.FromSeconds(2));
        evt.Exception.Should().BeNull();
    }

    /// <summary>
    /// Tests that CreateRetry preserves the exception when provided.
    /// </summary>
    [Fact]
    public void CreateRetry_WithException_ShouldPreserveException()
    {
        // Arrange
        var exception = new HttpRequestException("Connection failed");

        // Act
        var evt = ResilienceEvent.CreateRetry(2, TimeSpan.FromSeconds(4), exception);

        // Assert
        evt.Exception.Should().BeSameAs(exception);
        evt.AttemptNumber.Should().Be(2);
    }

    /// <summary>
    /// Tests that IsRetry returns true for retry events.
    /// </summary>
    [Fact]
    public void CreateRetry_IsRetry_ShouldReturnTrue()
    {
        // Act
        var evt = ResilienceEvent.CreateRetry(1, TimeSpan.FromSeconds(1));

        // Assert
        evt.IsRetry.Should().BeTrue();
    }

    #endregion

    #region Factory Method Tests - CreateCircuitBreak

    /// <summary>
    /// Tests that CreateCircuitBreak creates an event with correct properties.
    /// </summary>
    [Fact]
    public void CreateCircuitBreak_ShouldCreateEventWithCorrectProperties()
    {
        // Act
        var evt = ResilienceEvent.CreateCircuitBreak(TimeSpan.FromSeconds(30));

        // Assert
        evt.PolicyName.Should().Be(ResilienceEvent.PolicyNames.CircuitBreaker);
        evt.EventType.Should().Be(ResilienceEvent.CircuitBreakerEventTypes.Break);
        evt.Duration.Should().Be(TimeSpan.FromSeconds(30));
        evt.AttemptNumber.Should().BeNull();
        evt.Exception.Should().BeNull();
    }

    /// <summary>
    /// Tests that CreateCircuitBreak preserves the exception when provided.
    /// </summary>
    [Fact]
    public void CreateCircuitBreak_WithException_ShouldPreserveException()
    {
        // Arrange
        var exception = new HttpRequestException("Service unavailable");

        // Act
        var evt = ResilienceEvent.CreateCircuitBreak(TimeSpan.FromSeconds(30), exception);

        // Assert
        evt.Exception.Should().BeSameAs(exception);
    }

    /// <summary>
    /// Tests that IsCircuitBreak returns true for circuit break events.
    /// </summary>
    [Fact]
    public void CreateCircuitBreak_IsCircuitBreak_ShouldReturnTrue()
    {
        // Act
        var evt = ResilienceEvent.CreateCircuitBreak(TimeSpan.FromSeconds(30));

        // Assert
        evt.IsCircuitBreak.Should().BeTrue();
    }

    #endregion

    #region Factory Method Tests - CreateCircuitReset

    /// <summary>
    /// Tests that CreateCircuitReset creates an event with correct properties.
    /// </summary>
    [Fact]
    public void CreateCircuitReset_ShouldCreateEventWithCorrectProperties()
    {
        // Act
        var evt = ResilienceEvent.CreateCircuitReset();

        // Assert
        evt.PolicyName.Should().Be(ResilienceEvent.PolicyNames.CircuitBreaker);
        evt.EventType.Should().Be(ResilienceEvent.CircuitBreakerEventTypes.Reset);
        evt.Duration.Should().BeNull();
        evt.AttemptNumber.Should().BeNull();
        evt.Exception.Should().BeNull();
    }

    /// <summary>
    /// Tests that IsCircuitBreak returns false for reset events.
    /// </summary>
    [Fact]
    public void CreateCircuitReset_IsCircuitBreak_ShouldReturnFalse()
    {
        // Act
        var evt = ResilienceEvent.CreateCircuitReset();

        // Assert
        evt.IsCircuitBreak.Should().BeFalse();
    }

    #endregion

    #region Factory Method Tests - CreateCircuitHalfOpen

    /// <summary>
    /// Tests that CreateCircuitHalfOpen creates an event with correct properties.
    /// </summary>
    [Fact]
    public void CreateCircuitHalfOpen_ShouldCreateEventWithCorrectProperties()
    {
        // Act
        var evt = ResilienceEvent.CreateCircuitHalfOpen();

        // Assert
        evt.PolicyName.Should().Be(ResilienceEvent.PolicyNames.CircuitBreaker);
        evt.EventType.Should().Be(ResilienceEvent.CircuitBreakerEventTypes.HalfOpen);
        evt.Duration.Should().BeNull();
        evt.AttemptNumber.Should().BeNull();
    }

    #endregion

    #region Factory Method Tests - CreateTimeout

    /// <summary>
    /// Tests that CreateTimeout creates an event with correct properties.
    /// </summary>
    [Fact]
    public void CreateTimeout_ShouldCreateEventWithCorrectProperties()
    {
        // Act
        var evt = ResilienceEvent.CreateTimeout(TimeSpan.FromSeconds(30));

        // Assert
        evt.PolicyName.Should().Be(ResilienceEvent.PolicyNames.Timeout);
        evt.EventType.Should().Be(ResilienceEvent.TimeoutEventTypes.Timeout);
        evt.Duration.Should().Be(TimeSpan.FromSeconds(30));
        evt.AttemptNumber.Should().BeNull();
        evt.Exception.Should().BeNull();
    }

    /// <summary>
    /// Tests that IsTimeout returns true for timeout events.
    /// </summary>
    [Fact]
    public void CreateTimeout_IsTimeout_ShouldReturnTrue()
    {
        // Act
        var evt = ResilienceEvent.CreateTimeout(TimeSpan.FromSeconds(30));

        // Assert
        evt.IsTimeout.Should().BeTrue();
    }

    #endregion

    #region Factory Method Tests - CreateBulkheadRejection

    /// <summary>
    /// Tests that CreateBulkheadRejection creates an event with correct properties.
    /// </summary>
    [Fact]
    public void CreateBulkheadRejection_ShouldCreateEventWithCorrectProperties()
    {
        // Act
        var evt = ResilienceEvent.CreateBulkheadRejection();

        // Assert
        evt.PolicyName.Should().Be(ResilienceEvent.PolicyNames.Bulkhead);
        evt.EventType.Should().Be(ResilienceEvent.BulkheadEventTypes.Rejected);
        evt.Duration.Should().BeNull();
        evt.AttemptNumber.Should().BeNull();
        evt.Exception.Should().BeNull();
    }

    /// <summary>
    /// Tests that IsBulkheadRejection returns true for bulkhead rejection events.
    /// </summary>
    [Fact]
    public void CreateBulkheadRejection_IsBulkheadRejection_ShouldReturnTrue()
    {
        // Act
        var evt = ResilienceEvent.CreateBulkheadRejection();

        // Assert
        evt.IsBulkheadRejection.Should().BeTrue();
    }

    #endregion

    #region Boolean Property Tests

    /// <summary>
    /// Tests that IsRetry returns false for non-retry events.
    /// </summary>
    [Fact]
    public void IsRetry_ForCircuitBreakEvent_ShouldReturnFalse()
    {
        // Act
        var evt = ResilienceEvent.CreateCircuitBreak(TimeSpan.FromSeconds(30));

        // Assert
        evt.IsRetry.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsCircuitBreak returns false for non-circuit-break events.
    /// </summary>
    [Fact]
    public void IsCircuitBreak_ForRetryEvent_ShouldReturnFalse()
    {
        // Act
        var evt = ResilienceEvent.CreateRetry(1, TimeSpan.FromSeconds(1));

        // Assert
        evt.IsCircuitBreak.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsTimeout returns false for non-timeout events.
    /// </summary>
    [Fact]
    public void IsTimeout_ForRetryEvent_ShouldReturnFalse()
    {
        // Act
        var evt = ResilienceEvent.CreateRetry(1, TimeSpan.FromSeconds(1));

        // Assert
        evt.IsTimeout.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsBulkheadRejection returns false for non-bulkhead events.
    /// </summary>
    [Fact]
    public void IsBulkheadRejection_ForTimeoutEvent_ShouldReturnFalse()
    {
        // Act
        var evt = ResilienceEvent.CreateTimeout(TimeSpan.FromSeconds(30));

        // Assert
        evt.IsBulkheadRejection.Should().BeFalse();
    }

    /// <summary>
    /// Tests that all boolean properties are mutually exclusive.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetAllEventTypes))]
    public void BooleanProperties_ShouldBeMutuallyExclusive(ResilienceEvent evt, string expectedTrueProperty)
    {
        // Arrange
        var properties = new Dictionary<string, bool>
        {
            { nameof(ResilienceEvent.IsRetry), evt.IsRetry },
            { nameof(ResilienceEvent.IsCircuitBreak), evt.IsCircuitBreak },
            { nameof(ResilienceEvent.IsTimeout), evt.IsTimeout },
            { nameof(ResilienceEvent.IsBulkheadRejection), evt.IsBulkheadRejection }
        };

        // Assert
        properties[expectedTrueProperty].Should().BeTrue();
        var otherProperties = properties.Where(p => p.Key != expectedTrueProperty);
        foreach (var prop in otherProperties)
        {
            prop.Value.Should().BeFalse($"{prop.Key} should be false when {expectedTrueProperty} is true");
        }
    }

    public static IEnumerable<object[]> GetAllEventTypes()
    {
        yield return new object[] { ResilienceEvent.CreateRetry(1, TimeSpan.FromSeconds(1)), nameof(ResilienceEvent.IsRetry) };
        yield return new object[] { ResilienceEvent.CreateCircuitBreak(TimeSpan.FromSeconds(30)), nameof(ResilienceEvent.IsCircuitBreak) };
        yield return new object[] { ResilienceEvent.CreateTimeout(TimeSpan.FromSeconds(30)), nameof(ResilienceEvent.IsTimeout) };
        yield return new object[] { ResilienceEvent.CreateBulkheadRejection(), nameof(ResilienceEvent.IsBulkheadRejection) };
    }

    #endregion

    #region Timestamp Tests

    /// <summary>
    /// Tests that Timestamp is set to current time on creation.
    /// </summary>
    [Fact]
    public void Timestamp_ShouldBeSetToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = ResilienceEvent.CreateRetry(1, TimeSpan.FromSeconds(1));

        // Assert
        var after = DateTimeOffset.UtcNow;
        evt.Timestamp.Should().BeOnOrAfter(before);
        evt.Timestamp.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Tests that Timestamp can be customized via init property.
    /// </summary>
    [Fact]
    public void Timestamp_CanBeCustomizedViaInit()
    {
        // Arrange
        var customTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var evt = ResilienceEvent.CreateRetry(1, TimeSpan.FromSeconds(1)) with { Timestamp = customTime };

        // Assert
        evt.Timestamp.Should().Be(customTime);
    }

    #endregion

    #region Constants Tests

    /// <summary>
    /// Tests that PolicyNames constants have expected values.
    /// </summary>
    [Fact]
    public void PolicyNames_ShouldHaveExpectedValues()
    {
        // Assert
        ResilienceEvent.PolicyNames.Retry.Should().Be("Retry");
        ResilienceEvent.PolicyNames.CircuitBreaker.Should().Be("CircuitBreaker");
        ResilienceEvent.PolicyNames.Timeout.Should().Be("Timeout");
        ResilienceEvent.PolicyNames.Bulkhead.Should().Be("Bulkhead");
    }

    /// <summary>
    /// Tests that RetryEventTypes constants have expected values.
    /// </summary>
    [Fact]
    public void RetryEventTypes_ShouldHaveExpectedValues()
    {
        // Assert
        ResilienceEvent.RetryEventTypes.Retry.Should().Be("Retry");
        ResilienceEvent.RetryEventTypes.Backoff.Should().Be("Backoff");
        ResilienceEvent.RetryEventTypes.RetryAfter.Should().Be("RetryAfter");
    }

    /// <summary>
    /// Tests that CircuitBreakerEventTypes constants have expected values.
    /// </summary>
    [Fact]
    public void CircuitBreakerEventTypes_ShouldHaveExpectedValues()
    {
        // Assert
        ResilienceEvent.CircuitBreakerEventTypes.Break.Should().Be("Break");
        ResilienceEvent.CircuitBreakerEventTypes.Reset.Should().Be("Reset");
        ResilienceEvent.CircuitBreakerEventTypes.HalfOpen.Should().Be("HalfOpen");
        ResilienceEvent.CircuitBreakerEventTypes.Isolated.Should().Be("Isolated");
    }

    /// <summary>
    /// Tests that TimeoutEventTypes constants have expected values.
    /// </summary>
    [Fact]
    public void TimeoutEventTypes_ShouldHaveExpectedValues()
    {
        // Assert
        ResilienceEvent.TimeoutEventTypes.Timeout.Should().Be("Timeout");
    }

    /// <summary>
    /// Tests that BulkheadEventTypes constants have expected values.
    /// </summary>
    [Fact]
    public void BulkheadEventTypes_ShouldHaveExpectedValues()
    {
        // Assert
        ResilienceEvent.BulkheadEventTypes.Rejected.Should().Be("Rejected");
    }

    #endregion

    #region Record Equality Tests

    /// <summary>
    /// Tests that events with same values are equal.
    /// </summary>
    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evt1 = new ResilienceEvent("Retry", "Retry", TimeSpan.FromSeconds(1), null, 1) { Timestamp = timestamp };
        var evt2 = new ResilienceEvent("Retry", "Retry", TimeSpan.FromSeconds(1), null, 1) { Timestamp = timestamp };

        // Assert
        evt1.Should().Be(evt2);
    }

    /// <summary>
    /// Tests that events with different values are not equal.
    /// </summary>
    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var evt1 = ResilienceEvent.CreateRetry(1, TimeSpan.FromSeconds(1));
        var evt2 = ResilienceEvent.CreateRetry(2, TimeSpan.FromSeconds(2));

        // Assert
        evt1.Should().NotBe(evt2);
    }

    #endregion
}
