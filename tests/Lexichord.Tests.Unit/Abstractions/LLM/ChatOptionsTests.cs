// -----------------------------------------------------------------------
// <copyright file="ChatOptionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatOptions"/> record.
/// </summary>
public class ChatOptionsTests
{
    /// <summary>
    /// Tests that Default creates options with all null values.
    /// </summary>
    [Fact]
    public void Default_ShouldReturnOptionsWithNullValues()
    {
        // Act
        var options = ChatOptions.Default;

        // Assert
        options.Model.Should().BeNull();
        options.Temperature.Should().BeNull();
        options.MaxTokens.Should().BeNull();
        options.TopP.Should().BeNull();
        options.FrequencyPenalty.Should().BeNull();
        options.PresencePenalty.Should().BeNull();
        options.StopSequences.Should().BeNull();
    }

    /// <summary>
    /// Tests that Precise preset has temperature 0.
    /// </summary>
    [Fact]
    public void Precise_ShouldHaveZeroTemperature()
    {
        // Act
        var options = ChatOptions.Precise;

        // Assert
        options.Temperature.Should().Be(0.0);
    }

    /// <summary>
    /// Tests that Creative preset has temperature 0.9.
    /// </summary>
    [Fact]
    public void Creative_ShouldHaveHighTemperature()
    {
        // Act
        var options = ChatOptions.Creative;

        // Assert
        options.Temperature.Should().Be(0.9);
    }

    /// <summary>
    /// Tests that Balanced preset has temperature 0.7.
    /// </summary>
    [Fact]
    public void Balanced_ShouldHaveModerateTemperature()
    {
        // Act
        var options = ChatOptions.Balanced;

        // Assert
        options.Temperature.Should().Be(0.7);
    }

    /// <summary>
    /// Tests WithModel creates new options with specified model.
    /// </summary>
    [Fact]
    public void WithModel_ShouldCreateNewOptionsWithModel()
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var newOptions = options.WithModel("gpt-4");

        // Assert
        newOptions.Model.Should().Be("gpt-4");
        options.Model.Should().BeNull(); // Original unchanged
    }

    /// <summary>
    /// Tests WithTemperature creates new options with specified temperature.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void WithTemperature_WithValidValue_ShouldCreateNewOptions(double temperature)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var newOptions = options.WithTemperature(temperature);

        // Assert
        newOptions.Temperature.Should().Be(temperature);
    }

    /// <summary>
    /// Tests WithTemperature throws on invalid values.
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    [InlineData(-1.0)]
    [InlineData(3.0)]
    public void WithTemperature_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(double temperature)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var action = () => options.WithTemperature(temperature);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests WithMaxTokens creates new options with specified max tokens.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(4096)]
    public void WithMaxTokens_WithValidValue_ShouldCreateNewOptions(int maxTokens)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var newOptions = options.WithMaxTokens(maxTokens);

        // Assert
        newOptions.MaxTokens.Should().Be(maxTokens);
    }

    /// <summary>
    /// Tests WithMaxTokens throws on invalid values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithMaxTokens_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(int maxTokens)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var action = () => options.WithMaxTokens(maxTokens);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests WithTopP creates new options with specified top-p.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void WithTopP_WithValidValue_ShouldCreateNewOptions(double topP)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var newOptions = options.WithTopP(topP);

        // Assert
        newOptions.TopP.Should().Be(topP);
    }

    /// <summary>
    /// Tests WithTopP throws on invalid values.
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void WithTopP_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(double topP)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var action = () => options.WithTopP(topP);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests WithFrequencyPenalty creates new options with specified penalty.
    /// </summary>
    [Theory]
    [InlineData(-2.0)]
    [InlineData(0.0)]
    [InlineData(2.0)]
    public void WithFrequencyPenalty_WithValidValue_ShouldCreateNewOptions(double penalty)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var newOptions = options.WithFrequencyPenalty(penalty);

        // Assert
        newOptions.FrequencyPenalty.Should().Be(penalty);
    }

    /// <summary>
    /// Tests WithFrequencyPenalty throws on invalid values.
    /// </summary>
    [Theory]
    [InlineData(-2.1)]
    [InlineData(2.1)]
    public void WithFrequencyPenalty_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(double penalty)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var action = () => options.WithFrequencyPenalty(penalty);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests WithPresencePenalty creates new options with specified penalty.
    /// </summary>
    [Theory]
    [InlineData(-2.0)]
    [InlineData(0.0)]
    [InlineData(2.0)]
    public void WithPresencePenalty_WithValidValue_ShouldCreateNewOptions(double penalty)
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var newOptions = options.WithPresencePenalty(penalty);

        // Assert
        newOptions.PresencePenalty.Should().Be(penalty);
    }

    /// <summary>
    /// Tests WithStopSequences creates new options with specified sequences.
    /// </summary>
    [Fact]
    public void WithStopSequences_ShouldCreateNewOptionsWithSequences()
    {
        // Arrange
        var options = ChatOptions.Default;

        // Act
        var newOptions = options.WithStopSequences("END", "STOP");

        // Assert
        newOptions.StopSequences.Should().NotBeNull();
        newOptions.StopSequences!.Value.Should().HaveCount(2);
        newOptions.StopSequences!.Value.Should().Contain("END");
        newOptions.StopSequences!.Value.Should().Contain("STOP");
    }

    /// <summary>
    /// Tests that With methods can be chained.
    /// </summary>
    [Fact]
    public void WithMethods_ShouldBeChainable()
    {
        // Act
        var options = ChatOptions.Default
            .WithModel("gpt-4")
            .WithTemperature(0.7)
            .WithMaxTokens(1000)
            .WithTopP(0.9);

        // Assert
        options.Model.Should().Be("gpt-4");
        options.Temperature.Should().Be(0.7);
        options.MaxTokens.Should().Be(1000);
        options.TopP.Should().Be(0.9);
    }
}
