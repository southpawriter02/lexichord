// -----------------------------------------------------------------------
// <copyright file="ChatOptionsValidatorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatOptionsValidator"/>.
/// </summary>
public class ChatOptionsValidatorTests
{
    private readonly ChatOptionsValidator _validator = new();

    #region Temperature Validation Tests

    /// <summary>
    /// Tests that valid temperature values pass validation.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void Validate_WithValidTemperature_ShouldPass(double temperature)
    {
        // Arrange
        var options = new ChatOptions(Temperature: temperature);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that negative temperature fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeTemperature_ShouldFail()
    {
        // Arrange
        var options = new ChatOptions(Temperature: -0.1);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.ErrorCode == "TEMPERATURE_OUT_OF_RANGE");
    }

    /// <summary>
    /// Tests that temperature above 2.0 fails validation.
    /// </summary>
    [Theory]
    [InlineData(2.1)]
    [InlineData(3.0)]
    [InlineData(5.0)]
    public void Validate_WithTemperatureAbove2_ShouldFail(double temperature)
    {
        // Arrange
        var options = new ChatOptions(Temperature: temperature);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorCode == "TEMPERATURE_OUT_OF_RANGE");
    }

    /// <summary>
    /// Tests that null temperature (use default) passes validation.
    /// </summary>
    [Fact]
    public void Validate_WithNullTemperature_ShouldPass()
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region MaxTokens Validation Tests

    /// <summary>
    /// Tests that valid max tokens pass validation.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(2048)]
    [InlineData(4096)]
    public void Validate_WithValidMaxTokens_ShouldPass(int maxTokens)
    {
        // Arrange
        var options = new ChatOptions(MaxTokens: maxTokens);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that zero max tokens fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithZeroMaxTokens_ShouldFail()
    {
        // Arrange
        var options = new ChatOptions(MaxTokens: 0);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorCode == "MAX_TOKENS_INVALID");
    }

    /// <summary>
    /// Tests that negative max tokens fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeMaxTokens_ShouldFail()
    {
        // Arrange
        var options = new ChatOptions(MaxTokens: -1);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorCode == "MAX_TOKENS_INVALID");
    }

    #endregion

    #region TopP Validation Tests

    /// <summary>
    /// Tests that valid top-p values pass validation.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Validate_WithValidTopP_ShouldPass(double topP)
    {
        // Arrange
        var options = new ChatOptions(TopP: topP);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that top-p above 1.0 fails validation.
    /// </summary>
    [Theory]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Validate_WithTopPAbove1_ShouldFail(double topP)
    {
        // Arrange
        var options = new ChatOptions(TopP: topP);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorCode == "TOP_P_OUT_OF_RANGE");
    }

    /// <summary>
    /// Tests that negative top-p fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeTopP_ShouldFail()
    {
        // Arrange
        var options = new ChatOptions(TopP: -0.1);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorCode == "TOP_P_OUT_OF_RANGE");
    }

    #endregion

    #region Penalty Validation Tests

    /// <summary>
    /// Tests that valid frequency penalty passes validation.
    /// </summary>
    [Theory]
    [InlineData(-2.0)]
    [InlineData(0.0)]
    [InlineData(2.0)]
    public void Validate_WithValidFrequencyPenalty_ShouldPass(double penalty)
    {
        // Arrange
        var options = new ChatOptions(FrequencyPenalty: penalty);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that frequency penalty outside range fails validation.
    /// </summary>
    [Theory]
    [InlineData(-2.1)]
    [InlineData(2.1)]
    public void Validate_WithInvalidFrequencyPenalty_ShouldFail(double penalty)
    {
        // Arrange
        var options = new ChatOptions(FrequencyPenalty: penalty);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorCode == "FREQUENCY_PENALTY_OUT_OF_RANGE");
    }

    /// <summary>
    /// Tests that valid presence penalty passes validation.
    /// </summary>
    [Theory]
    [InlineData(-2.0)]
    [InlineData(0.0)]
    [InlineData(2.0)]
    public void Validate_WithValidPresencePenalty_ShouldPass(double penalty)
    {
        // Arrange
        var options = new ChatOptions(PresencePenalty: penalty);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that presence penalty outside range fails validation.
    /// </summary>
    [Theory]
    [InlineData(-2.1)]
    [InlineData(2.1)]
    public void Validate_WithInvalidPresencePenalty_ShouldFail(double penalty)
    {
        // Arrange
        var options = new ChatOptions(PresencePenalty: penalty);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorCode == "PRESENCE_PENALTY_OUT_OF_RANGE");
    }

    #endregion

    #region StopSequences Validation Tests

    /// <summary>
    /// Tests that up to 4 stop sequences pass validation.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Validate_WithValidStopSequenceCount_ShouldPass(int count)
    {
        // Arrange
        var sequences = Enumerable.Range(0, count).Select(i => $"stop{i}").ToImmutableArray();
        var options = new ChatOptions(StopSequences: sequences);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that more than 4 stop sequences fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithTooManyStopSequences_ShouldFail()
    {
        // Arrange
        var sequences = ImmutableArray.Create("a", "b", "c", "d", "e");
        var options = new ChatOptions(StopSequences: sequences);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorCode == "TOO_MANY_STOP_SEQUENCES");
    }

    /// <summary>
    /// Tests that empty stop sequences pass validation.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyStopSequences_ShouldPass()
    {
        // Arrange
        var options = new ChatOptions(StopSequences: ImmutableArray<string>.Empty);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Preset Validation Tests

    /// <summary>
    /// Tests that all presets pass validation.
    /// </summary>
    [Fact]
    public void Validate_AllPresets_ShouldPass()
    {
        // Arrange & Act & Assert
        _validator.Validate(ChatOptions.Default).IsValid.Should().BeTrue();
        _validator.Validate(ChatOptions.Creative).IsValid.Should().BeTrue();
        _validator.Validate(ChatOptions.Precise).IsValid.Should().BeTrue();
        _validator.Validate(ChatOptions.Balanced).IsValid.Should().BeTrue();
        _validator.Validate(ChatOptions.CodeGeneration).IsValid.Should().BeTrue();
        _validator.Validate(ChatOptions.Conversational).IsValid.Should().BeTrue();
        _validator.Validate(ChatOptions.Summarization).IsValid.Should().BeTrue();
        _validator.Validate(ChatOptions.Editing).IsValid.Should().BeTrue();
        _validator.Validate(ChatOptions.Brainstorming).IsValid.Should().BeTrue();
    }

    #endregion

    #region Multiple Errors Tests

    /// <summary>
    /// Tests that multiple validation failures are reported.
    /// </summary>
    [Fact]
    public void Validate_WithMultipleInvalidValues_ShouldReportAllErrors()
    {
        // Arrange
        var options = new ChatOptions(
            Temperature: 3.0,
            MaxTokens: 0,
            TopP: 2.0,
            FrequencyPenalty: 3.0,
            PresencePenalty: 3.0);

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(5);
    }

    #endregion

    #region Constants Tests

    /// <summary>
    /// Tests that validator constants match expected values.
    /// </summary>
    [Fact]
    public void ValidatorConstants_ShouldHaveExpectedValues()
    {
        // Assert
        ChatOptionsValidator.MinTemperature.Should().Be(0.0);
        ChatOptionsValidator.MaxTemperature.Should().Be(2.0);
        ChatOptionsValidator.MinMaxTokens.Should().Be(1);
        ChatOptionsValidator.MinTopP.Should().Be(0.0);
        ChatOptionsValidator.MaxTopP.Should().Be(1.0);
        ChatOptionsValidator.MinPenalty.Should().Be(-2.0);
        ChatOptionsValidator.MaxPenalty.Should().Be(2.0);
        ChatOptionsValidator.MaxStopSequences.Should().Be(4);
    }

    #endregion
}
