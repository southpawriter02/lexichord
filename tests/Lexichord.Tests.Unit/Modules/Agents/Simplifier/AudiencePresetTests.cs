// -----------------------------------------------------------------------
// <copyright file="AudiencePresetTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Simplifier;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="AudiencePreset"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4a")]
public class AudiencePresetTests
{
    // ── Record Construction Tests ─────────────────────────────────────────

    [Fact]
    public void Constructor_ValidParameters_CreatesPreset()
    {
        // Arrange & Act
        var preset = new AudiencePreset(
            Id: "test-preset",
            Name: "Test Preset",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true,
            Description: "A test preset",
            IsBuiltIn: false);

        // Assert
        preset.Id.Should().Be("test-preset");
        preset.Name.Should().Be("Test Preset");
        preset.TargetGradeLevel.Should().Be(8.0);
        preset.MaxSentenceLength.Should().Be(20);
        preset.AvoidJargon.Should().BeTrue();
        preset.Description.Should().Be("A test preset");
        preset.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public void Constructor_MinimalParameters_UsesDefaults()
    {
        // Arrange & Act
        var preset = new AudiencePreset(
            Id: "minimal",
            Name: "Minimal Preset",
            TargetGradeLevel: 10.0,
            MaxSentenceLength: 25,
            AvoidJargon: false);

        // Assert
        preset.Description.Should().BeNull();
        preset.IsBuiltIn.Should().BeFalse();
    }

    // ── IsValid Tests ─────────────────────────────────────────────────────

    [Fact]
    public void IsValid_ValidPreset_ReturnsTrue()
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "valid",
            Name: "Valid Preset",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var isValid = preset.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_EmptyId_ReturnsFalse(string? id)
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: id!,
            Name: "Test",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var isValid = preset.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_EmptyName_ReturnsFalse(string? name)
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "test",
            Name: name!,
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var isValid = preset.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(100)]
    public void IsValid_GradeLevelOutOfRange_ReturnsFalse(double gradeLevel)
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "test",
            Name: "Test",
            TargetGradeLevel: gradeLevel,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var isValid = preset.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(20)]
    public void IsValid_GradeLevelInRange_ReturnsTrue(double gradeLevel)
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "test",
            Name: "Test",
            TargetGradeLevel: gradeLevel,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var isValid = preset.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(51)]
    [InlineData(100)]
    public void IsValid_SentenceLengthOutOfRange_ReturnsFalse(int sentenceLength)
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "test",
            Name: "Test",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: sentenceLength,
            AvoidJargon: true);

        // Act
        var isValid = preset.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(20)]
    [InlineData(50)]
    public void IsValid_SentenceLengthInRange_ReturnsTrue(int sentenceLength)
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "test",
            Name: "Test",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: sentenceLength,
            AvoidJargon: true);

        // Act
        var isValid = preset.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    // ── Validate Tests ────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidPreset_ReturnsNoErrors()
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "valid",
            Name: "Valid Preset",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var (isValid, errors) = preset.Validate();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "",
            Name: "",
            TargetGradeLevel: 25,
            MaxSentenceLength: 3,
            AvoidJargon: true);

        // Act
        var (isValid, errors) = preset.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCount(4);
        errors.Should().Contain(e => e.Contains("Id"));
        errors.Should().Contain(e => e.Contains("Name"));
        errors.Should().Contain(e => e.Contains("grade level"));
        errors.Should().Contain(e => e.Contains("sentence length"));
    }

    // ── CloneWithId Tests ─────────────────────────────────────────────────

    [Fact]
    public void CloneWithId_ValidId_CreatesNewPresetWithDifferentId()
    {
        // Arrange
        var original = new AudiencePreset(
            Id: "original",
            Name: "Original Preset",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true,
            Description: "Original description",
            IsBuiltIn: true);

        // Act
        var clone = original.CloneWithId("cloned");

        // Assert
        clone.Id.Should().Be("cloned");
        clone.Name.Should().Be("Original Preset");
        clone.TargetGradeLevel.Should().Be(8.0);
        clone.MaxSentenceLength.Should().Be(20);
        clone.AvoidJargon.Should().BeTrue();
        clone.Description.Should().Be("Original description");
        clone.IsBuiltIn.Should().BeFalse(); // Always false for clones
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CloneWithId_EmptyId_ThrowsArgumentException(string? newId)
    {
        // Arrange
        var original = new AudiencePreset(
            Id: "original",
            Name: "Original",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var act = () => original.CloneWithId(newId!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("newId");
    }

    // ── Record Equality Tests ─────────────────────────────────────────────

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var preset1 = new AudiencePreset("id", "Name", 8.0, 20, true);
        var preset2 = new AudiencePreset("id", "Name", 8.0, 20, true);

        // Act & Assert
        preset1.Should().Be(preset2);
        (preset1 == preset2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var preset1 = new AudiencePreset("id1", "Name", 8.0, 20, true);
        var preset2 = new AudiencePreset("id2", "Name", 8.0, 20, true);

        // Act & Assert
        preset1.Should().NotBe(preset2);
        (preset1 == preset2).Should().BeFalse();
    }

    [Fact]
    public void WithExpression_ChangesProperty_CreatesNewInstance()
    {
        // Arrange
        var original = new AudiencePreset("id", "Name", 8.0, 20, true);

        // Act
        var modified = original with { MaxSentenceLength = 15 };

        // Assert
        original.MaxSentenceLength.Should().Be(20);
        modified.MaxSentenceLength.Should().Be(15);
        modified.Id.Should().Be("id");
        modified.Name.Should().Be("Name");
    }
}
