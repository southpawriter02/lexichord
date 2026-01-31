using FluentAssertions;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Contracts;

/// <summary>
/// Unit tests for VoiceProfile record validation.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4a - These tests verify the Validate method catches
/// invalid constraint values at profile creation time.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.4a")]
public class VoiceProfileTests
{
    #region Validation Tests

    [Fact]
    public void Validate_ValidProfile_ReturnsTrue()
    {
        // Arrange
        var profile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Test Profile",
            TargetGradeLevel = 10.0,
            MaxSentenceLength = 25
        };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyOrWhitespaceName_ReturnsFalse(string? name)
    {
        // Arrange
        var profile = new VoiceProfile { Name = name! };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("name is required"));
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsFalse()
    {
        // Arrange
        var profile = new VoiceProfile { Name = new string('a', 51) };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("50 characters"));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(21.0)]
    public void Validate_TargetGradeLevelOutOfRange_ReturnsFalse(double gradeLevel)
    {
        // Arrange
        var profile = new VoiceProfile
        {
            Name = "Test",
            TargetGradeLevel = gradeLevel
        };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("grade level must be between 0 and 20"));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(10.0)]
    [InlineData(20.0)]
    public void Validate_TargetGradeLevelInRange_IsValid(double gradeLevel)
    {
        // Arrange
        var profile = new VoiceProfile
        {
            Name = "Test",
            TargetGradeLevel = gradeLevel
        };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullTargetGradeLevel_IsValid()
    {
        // Arrange - null means no grade level enforcement
        var profile = new VoiceProfile
        {
            Name = "Test",
            TargetGradeLevel = null
        };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public void Validate_GradeLevelToleranceOutOfRange_ReturnsFalse(double tolerance)
    {
        // Arrange
        var profile = new VoiceProfile
        {
            Name = "Test",
            GradeLevelTolerance = tolerance
        };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("tolerance must be between 0 and 10"));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(101)]
    public void Validate_MaxSentenceLengthOutOfRange_ReturnsFalse(int length)
    {
        // Arrange
        var profile = new VoiceProfile
        {
            Name = "Test",
            MaxSentenceLength = length
        };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("sentence length must be between 5 and 100"));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(100.1)]
    public void Validate_MaxPassiveVoicePercentageOutOfRange_ReturnsFalse(double percentage)
    {
        // Arrange
        var profile = new VoiceProfile
        {
            Name = "Test",
            MaxPassiveVoicePercentage = percentage
        };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("passive voice percentage must be between 0 and 100"));
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var profile = new VoiceProfile
        {
            Name = "", // Error 1
            TargetGradeLevel = 25.0, // Error 2
            MaxSentenceLength = 3 // Error 3
        };

        // Act
        var result = profile.Validate(out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().HaveCount(3);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameProperties_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var profile1 = new VoiceProfile { Id = id, Name = "Test" };
        var profile2 = new VoiceProfile { Id = id, Name = "Test" };

        // Act & Assert
        profile1.Should().Be(profile2);
    }

    [Fact]
    public void Equality_DifferentName_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var profile1 = new VoiceProfile { Id = id, Name = "Test1" };
        var profile2 = new VoiceProfile { Id = id, Name = "Test2" };

        // Act & Assert
        profile1.Should().NotBe(profile2);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var profile = new VoiceProfile { Name = "Test" };

        // Assert
        profile.GradeLevelTolerance.Should().Be(2.0);
        profile.MaxSentenceLength.Should().Be(25);
        profile.MaxPassiveVoicePercentage.Should().Be(10.0);
        profile.AllowPassiveVoice.Should().BeFalse();
        profile.FlagAdverbs.Should().BeTrue();
        profile.FlagWeaselWords.Should().BeTrue();
        profile.IsBuiltIn.Should().BeFalse();
        profile.ForbiddenCategories.Should().BeEmpty();
    }

    #endregion
}
