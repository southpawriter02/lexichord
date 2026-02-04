// -----------------------------------------------------------------------
// <copyright file="ResilienceOptionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Resilience;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Resilience;

/// <summary>
/// Unit tests for <see cref="ResilienceOptions"/>.
/// </summary>
public class ResilienceOptionsTests
{
    #region Default Values Tests

    /// <summary>
    /// Tests that the default retry count is 3.
    /// </summary>
    [Fact]
    public void DefaultRetryCount_ShouldBe3()
    {
        // Act
        var options = new ResilienceOptions();

        // Assert
        options.RetryCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that the default retry base delay is 1.0 seconds.
    /// </summary>
    [Fact]
    public void DefaultRetryBaseDelaySeconds_ShouldBe1()
    {
        // Act
        var options = new ResilienceOptions();

        // Assert
        options.RetryBaseDelaySeconds.Should().Be(1.0);
    }

    /// <summary>
    /// Tests that the default retry max delay is 30.0 seconds.
    /// </summary>
    [Fact]
    public void DefaultRetryMaxDelaySeconds_ShouldBe30()
    {
        // Act
        var options = new ResilienceOptions();

        // Assert
        options.RetryMaxDelaySeconds.Should().Be(30.0);
    }

    /// <summary>
    /// Tests that the default circuit breaker threshold is 5.
    /// </summary>
    [Fact]
    public void DefaultCircuitBreakerThreshold_ShouldBe5()
    {
        // Act
        var options = new ResilienceOptions();

        // Assert
        options.CircuitBreakerThreshold.Should().Be(5);
    }

    /// <summary>
    /// Tests that the default circuit breaker duration is 30 seconds.
    /// </summary>
    [Fact]
    public void DefaultCircuitBreakerDurationSeconds_ShouldBe30()
    {
        // Act
        var options = new ResilienceOptions();

        // Assert
        options.CircuitBreakerDurationSeconds.Should().Be(30);
    }

    /// <summary>
    /// Tests that the default timeout is 30 seconds.
    /// </summary>
    [Fact]
    public void DefaultTimeoutSeconds_ShouldBe30()
    {
        // Act
        var options = new ResilienceOptions();

        // Assert
        options.TimeoutSeconds.Should().Be(30);
    }

    /// <summary>
    /// Tests that the default bulkhead max concurrency is 10.
    /// </summary>
    [Fact]
    public void DefaultBulkheadMaxConcurrency_ShouldBe10()
    {
        // Act
        var options = new ResilienceOptions();

        // Assert
        options.BulkheadMaxConcurrency.Should().Be(10);
    }

    /// <summary>
    /// Tests that the default bulkhead max queue is 100.
    /// </summary>
    [Fact]
    public void DefaultBulkheadMaxQueue_ShouldBe100()
    {
        // Act
        var options = new ResilienceOptions();

        // Assert
        options.BulkheadMaxQueue.Should().Be(100);
    }

    #endregion

    #region Section Name Tests

    /// <summary>
    /// Tests that the section name constant has the expected value.
    /// </summary>
    [Fact]
    public void SectionName_ShouldBeLLMResilience()
    {
        // Assert
        ResilienceOptions.SectionName.Should().Be("LLM:Resilience");
    }

    #endregion

    #region Static Presets Tests

    /// <summary>
    /// Tests that the Default preset has expected default values.
    /// </summary>
    [Fact]
    public void DefaultPreset_ShouldHaveDefaultValues()
    {
        // Act
        var preset = ResilienceOptions.Default;

        // Assert
        preset.RetryCount.Should().Be(3);
        preset.RetryBaseDelaySeconds.Should().Be(1.0);
        preset.CircuitBreakerThreshold.Should().Be(5);
        preset.TimeoutSeconds.Should().Be(30);
    }

    /// <summary>
    /// Tests that the Aggressive preset has higher values than Default.
    /// </summary>
    [Fact]
    public void AggressivePreset_ShouldHaveHigherValues()
    {
        // Act
        var preset = ResilienceOptions.Aggressive;

        // Assert
        preset.RetryCount.Should().Be(5);
        preset.RetryBaseDelaySeconds.Should().Be(2.0);
        preset.RetryMaxDelaySeconds.Should().Be(60.0);
        preset.CircuitBreakerThreshold.Should().Be(10);
        preset.CircuitBreakerDurationSeconds.Should().Be(60);
        preset.TimeoutSeconds.Should().Be(60);
        preset.BulkheadMaxConcurrency.Should().Be(20);
        preset.BulkheadMaxQueue.Should().Be(200);
    }

    /// <summary>
    /// Tests that the Minimal preset has lower values than Default.
    /// </summary>
    [Fact]
    public void MinimalPreset_ShouldHaveLowerValues()
    {
        // Act
        var preset = ResilienceOptions.Minimal;

        // Assert
        preset.RetryCount.Should().Be(1);
        preset.RetryBaseDelaySeconds.Should().Be(0.5);
        preset.RetryMaxDelaySeconds.Should().Be(5.0);
        preset.CircuitBreakerThreshold.Should().Be(3);
        preset.CircuitBreakerDurationSeconds.Should().Be(15);
        preset.TimeoutSeconds.Should().Be(10);
        preset.BulkheadMaxConcurrency.Should().Be(5);
        preset.BulkheadMaxQueue.Should().Be(25);
    }

    /// <summary>
    /// Tests that all static presets pass validation.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetAllPresets))]
    public void AllPresets_ShouldBeValid(ResilienceOptions preset)
    {
        // Act
        var isValid = preset.IsValid;

        // Assert
        isValid.Should().BeTrue();
    }

    public static IEnumerable<object[]> GetAllPresets()
    {
        yield return new object[] { ResilienceOptions.Default };
        yield return new object[] { ResilienceOptions.Aggressive };
        yield return new object[] { ResilienceOptions.Minimal };
    }

    #endregion

    #region TimeSpan Property Tests

    /// <summary>
    /// Tests that RetryBaseDelay returns the correct TimeSpan.
    /// </summary>
    [Fact]
    public void RetryBaseDelay_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var options = new ResilienceOptions(RetryBaseDelaySeconds: 2.5);

        // Assert
        options.RetryBaseDelay.Should().Be(TimeSpan.FromSeconds(2.5));
    }

    /// <summary>
    /// Tests that RetryMaxDelay returns the correct TimeSpan.
    /// </summary>
    [Fact]
    public void RetryMaxDelay_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var options = new ResilienceOptions(RetryMaxDelaySeconds: 45.0);

        // Assert
        options.RetryMaxDelay.Should().Be(TimeSpan.FromSeconds(45.0));
    }

    /// <summary>
    /// Tests that CircuitBreakerDuration returns the correct TimeSpan.
    /// </summary>
    [Fact]
    public void CircuitBreakerDuration_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var options = new ResilienceOptions(CircuitBreakerDurationSeconds: 60);

        // Assert
        options.CircuitBreakerDuration.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Tests that Timeout returns the correct TimeSpan.
    /// </summary>
    [Fact]
    public void Timeout_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var options = new ResilienceOptions(TimeoutSeconds: 120);

        // Assert
        options.Timeout.Should().Be(TimeSpan.FromSeconds(120));
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Tests that valid options pass validation.
    /// </summary>
    [Fact]
    public void Validate_WithValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = new ResilienceOptions();

        // Act & Assert
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that negative RetryCount fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeRetryCount_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(RetryCount: -1);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*RetryCount*")
            .WithMessage("*non-negative*");
    }

    /// <summary>
    /// Tests that zero RetryCount is valid (no retries).
    /// </summary>
    [Fact]
    public void Validate_WithZeroRetryCount_ShouldNotThrow()
    {
        // Arrange
        var options = new ResilienceOptions(RetryCount: 0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that zero RetryBaseDelaySeconds fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithZeroRetryBaseDelay_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(RetryBaseDelaySeconds: 0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*RetryBaseDelaySeconds*")
            .WithMessage("*greater than zero*");
    }

    /// <summary>
    /// Tests that negative RetryBaseDelaySeconds fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeRetryBaseDelay_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(RetryBaseDelaySeconds: -1.0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*RetryBaseDelaySeconds*");
    }

    /// <summary>
    /// Tests that zero RetryMaxDelaySeconds fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithZeroRetryMaxDelay_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(RetryMaxDelaySeconds: 0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*RetryMaxDelaySeconds*");
    }

    /// <summary>
    /// Tests that zero CircuitBreakerThreshold fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithZeroCircuitBreakerThreshold_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(CircuitBreakerThreshold: 0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*CircuitBreakerThreshold*")
            .WithMessage("*at least 1*");
    }

    /// <summary>
    /// Tests that zero CircuitBreakerDurationSeconds fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithZeroCircuitBreakerDuration_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(CircuitBreakerDurationSeconds: 0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*CircuitBreakerDurationSeconds*");
    }

    /// <summary>
    /// Tests that zero TimeoutSeconds fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithZeroTimeout_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(TimeoutSeconds: 0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*TimeoutSeconds*");
    }

    /// <summary>
    /// Tests that zero BulkheadMaxConcurrency fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithZeroBulkheadMaxConcurrency_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(BulkheadMaxConcurrency: 0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*BulkheadMaxConcurrency*");
    }

    /// <summary>
    /// Tests that negative BulkheadMaxQueue fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeBulkheadMaxQueue_ShouldThrow()
    {
        // Arrange
        var options = new ResilienceOptions(BulkheadMaxQueue: -1);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*BulkheadMaxQueue*");
    }

    /// <summary>
    /// Tests that zero BulkheadMaxQueue is valid (no queueing allowed).
    /// </summary>
    [Fact]
    public void Validate_WithZeroBulkheadMaxQueue_ShouldNotThrow()
    {
        // Arrange
        var options = new ResilienceOptions(BulkheadMaxQueue: 0);

        // Act & Assert
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    #endregion

    #region GetValidationErrors Tests

    /// <summary>
    /// Tests that valid options return no errors.
    /// </summary>
    [Fact]
    public void GetValidationErrors_WithValidOptions_ShouldReturnEmpty()
    {
        // Arrange
        var options = new ResilienceOptions();

        // Act
        var errors = options.GetValidationErrors();

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that multiple invalid values return multiple errors.
    /// </summary>
    [Fact]
    public void GetValidationErrors_WithMultipleInvalidValues_ShouldReturnAllErrors()
    {
        // Arrange
        var options = new ResilienceOptions(
            RetryCount: -1,
            RetryBaseDelaySeconds: 0,
            CircuitBreakerThreshold: 0);

        // Act
        var errors = options.GetValidationErrors();

        // Assert
        errors.Should().HaveCount(3);
        errors.Should().Contain(e => e.Contains("RetryCount"));
        errors.Should().Contain(e => e.Contains("RetryBaseDelaySeconds"));
        errors.Should().Contain(e => e.Contains("CircuitBreakerThreshold"));
    }

    /// <summary>
    /// Tests that single invalid value returns single error with the actual value.
    /// </summary>
    [Fact]
    public void GetValidationErrors_WithInvalidValue_ShouldIncludeActualValue()
    {
        // Arrange
        var options = new ResilienceOptions(RetryCount: -5);

        // Act
        var errors = options.GetValidationErrors();

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Contain("-5");
    }

    #endregion

    #region IsValid Property Tests

    /// <summary>
    /// Tests that IsValid returns true for valid options.
    /// </summary>
    [Fact]
    public void IsValid_WithValidOptions_ShouldReturnTrue()
    {
        // Arrange
        var options = new ResilienceOptions();

        // Assert
        options.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsValid returns false for invalid options.
    /// </summary>
    [Fact]
    public void IsValid_WithInvalidOptions_ShouldReturnFalse()
    {
        // Arrange
        var options = new ResilienceOptions(RetryCount: -1);

        // Assert
        options.IsValid.Should().BeFalse();
    }

    #endregion

    #region Custom Values Tests

    /// <summary>
    /// Tests that custom values are preserved in the record.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomValues_ShouldPreserveValues()
    {
        // Arrange & Act
        var options = new ResilienceOptions(
            RetryCount: 5,
            RetryBaseDelaySeconds: 2.0,
            RetryMaxDelaySeconds: 60.0,
            CircuitBreakerThreshold: 10,
            CircuitBreakerDurationSeconds: 60,
            TimeoutSeconds: 120,
            BulkheadMaxConcurrency: 20,
            BulkheadMaxQueue: 200);

        // Assert
        options.RetryCount.Should().Be(5);
        options.RetryBaseDelaySeconds.Should().Be(2.0);
        options.RetryMaxDelaySeconds.Should().Be(60.0);
        options.CircuitBreakerThreshold.Should().Be(10);
        options.CircuitBreakerDurationSeconds.Should().Be(60);
        options.TimeoutSeconds.Should().Be(120);
        options.BulkheadMaxConcurrency.Should().Be(20);
        options.BulkheadMaxQueue.Should().Be(200);
    }

    /// <summary>
    /// Tests that records are equal when all values match.
    /// </summary>
    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var options1 = new ResilienceOptions(
            RetryCount: 3,
            RetryBaseDelaySeconds: 1.0);
        var options2 = new ResilienceOptions(
            RetryCount: 3,
            RetryBaseDelaySeconds: 1.0);

        // Assert
        options1.Should().Be(options2);
    }

    /// <summary>
    /// Tests that records are not equal when values differ.
    /// </summary>
    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new ResilienceOptions(RetryCount: 3);
        var options2 = new ResilienceOptions(RetryCount: 5);

        // Assert
        options1.Should().NotBe(options2);
    }

    #endregion

    #region Record With Expression Tests

    /// <summary>
    /// Tests that the with expression creates a new instance with modified values.
    /// </summary>
    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithModifiedValues()
    {
        // Arrange
        var original = new ResilienceOptions();

        // Act
        var modified = original with { RetryCount = 5, TimeoutSeconds = 120 };

        // Assert
        modified.RetryCount.Should().Be(5);
        modified.TimeoutSeconds.Should().Be(120);
        modified.RetryBaseDelaySeconds.Should().Be(original.RetryBaseDelaySeconds); // Unchanged
        modified.CircuitBreakerThreshold.Should().Be(original.CircuitBreakerThreshold); // Unchanged
    }

    #endregion
}
