using FluentAssertions;
using Lexichord.Modules.Style.Services;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for BuiltInProfiles static class.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4a - These tests verify the 5 built-in profiles have
/// correct constraint values matching the specification.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.4a")]
public class BuiltInProfilesTests
{
    #region Profile Structure Tests

    [Fact]
    public void All_ContainsFiveProfiles()
    {
        // Act
        var profiles = BuiltInProfiles.All;

        // Assert
        profiles.Should().HaveCount(5);
    }

    [Fact]
    public void All_ProfilesAreSortedBySortOrder()
    {
        // Act
        var profiles = BuiltInProfiles.All;

        // Assert
        profiles.Should().BeInAscendingOrder(p => p.SortOrder);
    }

    [Fact]
    public void All_ProfilesHaveUniqueIds()
    {
        // Act
        var profiles = BuiltInProfiles.All;
        var ids = profiles.Select(p => p.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ProfilesHaveUniqueNames()
    {
        // Act
        var profiles = BuiltInProfiles.All;
        var names = profiles.Select(p => p.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_AreMarkedAsBuiltIn()
    {
        // Act
        var profiles = BuiltInProfiles.All;

        // Assert
        profiles.Should().AllSatisfy(p => p.IsBuiltIn.Should().BeTrue());
    }

    [Fact]
    public void All_PassValidation()
    {
        // Act
        var profiles = BuiltInProfiles.All;

        // Assert
        foreach (var profile in profiles)
        {
            var valid = profile.Validate(out var errors);
            valid.Should().BeTrue($"{profile.Name} should pass validation");
            errors.Should().BeEmpty();
        }
    }

    #endregion

    #region Technical Profile Tests

    [Fact]
    public void Technical_HasCorrectName()
    {
        BuiltInProfiles.Technical.Name.Should().Be("Technical");
    }

    [Fact]
    public void Technical_HasGradeLevel11()
    {
        BuiltInProfiles.Technical.TargetGradeLevel.Should().Be(11.0);
    }

    [Fact]
    public void Technical_DoesNotAllowPassiveVoice()
    {
        BuiltInProfiles.Technical.AllowPassiveVoice.Should().BeFalse();
    }

    [Fact]
    public void Technical_HasMaxSentenceLength20()
    {
        BuiltInProfiles.Technical.MaxSentenceLength.Should().Be(20);
    }

    [Fact]
    public void Technical_FlagsAdverbs()
    {
        BuiltInProfiles.Technical.FlagAdverbs.Should().BeTrue();
    }

    #endregion

    #region Marketing Profile Tests

    [Fact]
    public void Marketing_HasCorrectName()
    {
        BuiltInProfiles.Marketing.Name.Should().Be("Marketing");
    }

    [Fact]
    public void Marketing_HasGradeLevel9()
    {
        BuiltInProfiles.Marketing.TargetGradeLevel.Should().Be(9.0);
    }

    [Fact]
    public void Marketing_AllowsPassiveVoice()
    {
        BuiltInProfiles.Marketing.AllowPassiveVoice.Should().BeTrue();
    }

    [Fact]
    public void Marketing_HasMaxSentenceLength25()
    {
        BuiltInProfiles.Marketing.MaxSentenceLength.Should().Be(25);
    }

    [Fact]
    public void Marketing_FlagsWeaselWords()
    {
        BuiltInProfiles.Marketing.FlagWeaselWords.Should().BeTrue();
    }

    #endregion

    #region Academic Profile Tests

    [Fact]
    public void Academic_HasCorrectName()
    {
        BuiltInProfiles.Academic.Name.Should().Be("Academic");
    }

    [Fact]
    public void Academic_HasGradeLevel13()
    {
        BuiltInProfiles.Academic.TargetGradeLevel.Should().Be(13.0);
    }

    [Fact]
    public void Academic_AllowsPassiveVoice()
    {
        BuiltInProfiles.Academic.AllowPassiveVoice.Should().BeTrue();
    }

    [Fact]
    public void Academic_HasMaxSentenceLength30()
    {
        BuiltInProfiles.Academic.MaxSentenceLength.Should().Be(30);
    }

    #endregion

    #region Narrative Profile Tests

    [Fact]
    public void Narrative_HasCorrectName()
    {
        BuiltInProfiles.Narrative.Name.Should().Be("Narrative");
    }

    [Fact]
    public void Narrative_HasGradeLevel9()
    {
        BuiltInProfiles.Narrative.TargetGradeLevel.Should().Be(9.0);
    }

    [Fact]
    public void Narrative_DoesNotFlagAdverbs()
    {
        BuiltInProfiles.Narrative.FlagAdverbs.Should().BeFalse();
    }

    [Fact]
    public void Narrative_DoesNotFlagWeaselWords()
    {
        BuiltInProfiles.Narrative.FlagWeaselWords.Should().BeFalse();
    }

    #endregion

    #region Casual Profile Tests

    [Fact]
    public void Casual_HasCorrectName()
    {
        BuiltInProfiles.Casual.Name.Should().Be("Casual");
    }

    [Fact]
    public void Casual_HasGradeLevel7()
    {
        BuiltInProfiles.Casual.TargetGradeLevel.Should().Be(7.0);
    }

    [Fact]
    public void Casual_HasStandardTolerance()
    {
        BuiltInProfiles.Casual.GradeLevelTolerance.Should().Be(2.0);
    }

    [Fact]
    public void Casual_DoesNotFlagAdverbsOrWeaselWords()
    {
        BuiltInProfiles.Casual.FlagAdverbs.Should().BeFalse();
        BuiltInProfiles.Casual.FlagWeaselWords.Should().BeFalse();
    }

    #endregion

    #region Default Profile Tests

    [Fact]
    public void Default_IsTechnical()
    {
        BuiltInProfiles.Default.Should().BeSameAs(BuiltInProfiles.Technical);
    }

    #endregion
}
