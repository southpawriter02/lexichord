using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Domain;

/// <summary>
/// Unit tests for StyleSheet record.
/// </summary>
/// <remarks>
/// LOGIC: Verifies rule management, filtering, and merge semantics.
/// </remarks>
public class StyleSheetTests
{
    [Fact]
    public void Empty_HasNoRules()
    {
        // Assert
        StyleSheet.Empty.Rules.Should().BeEmpty();
        StyleSheet.Empty.Name.Should().Be("Empty");
    }

    [Fact]
    public void GetEnabledRules_FiltersDisabled()
    {
        // Arrange
        var rules = new List<StyleRule>
        {
            CreateRule("r1", isEnabled: true),
            CreateRule("r2", isEnabled: false),
            CreateRule("r3", isEnabled: true),
        };
        var sheet = new StyleSheet("Test", rules);

        // Act
        var enabled = sheet.GetEnabledRules();

        // Assert
        enabled.Should().HaveCount(2);
        enabled.Select(r => r.Id).Should().BeEquivalentTo(["r1", "r3"]);
    }

    [Fact]
    public void GetRulesByCategory_FiltersCorrectly()
    {
        // Arrange
        var rules = new List<StyleRule>
        {
            CreateRule("r1", category: RuleCategory.Terminology),
            CreateRule("r2", category: RuleCategory.Formatting),
            CreateRule("r3", category: RuleCategory.Terminology),
            CreateRule("r4", category: RuleCategory.Syntax),
        };
        var sheet = new StyleSheet("Test", rules);

        // Act
        var terminologyRules = sheet.GetRulesByCategory(RuleCategory.Terminology);

        // Assert
        terminologyRules.Should().HaveCount(2);
        terminologyRules.Select(r => r.Id).Should().BeEquivalentTo(["r1", "r3"]);
    }

    [Fact]
    public void GetRulesBySeverity_FiltersBySeverityThreshold()
    {
        // Arrange
        var rules = new List<StyleRule>
        {
            CreateRule("r1", severity: ViolationSeverity.Error),
            CreateRule("r2", severity: ViolationSeverity.Warning),
            CreateRule("r3", severity: ViolationSeverity.Info),
            CreateRule("r4", severity: ViolationSeverity.Hint),
        };
        var sheet = new StyleSheet("Test", rules);

        // Act - get rules at Warning severity or higher (Error=0, Warning=1)
        var warningOrHigher = sheet.GetRulesBySeverity(ViolationSeverity.Warning);

        // Assert
        warningOrHigher.Should().HaveCount(2);
        warningOrHigher.Select(r => r.Id).Should().BeEquivalentTo(["r1", "r2"]);
    }

    [Fact]
    public void MergeWith_CustomRulesOverrideBase()
    {
        // Arrange
        var baseRules = new List<StyleRule>
        {
            CreateRule("r1", severity: ViolationSeverity.Warning),
            CreateRule("r2", severity: ViolationSeverity.Info),
        };
        var customRules = new List<StyleRule>
        {
            CreateRule("r1", severity: ViolationSeverity.Error), // Override
        };

        var baseSheet = new StyleSheet("Base", baseRules);
        var customSheet = new StyleSheet("Custom", customRules, Extends: "default");

        // Act
        var merged = customSheet.MergeWith(baseSheet);

        // Assert
        merged.Rules.Should().HaveCount(2);
        var r1 = merged.FindRuleById("r1")!;
        r1.DefaultSeverity.Should().Be(ViolationSeverity.Error);
    }

    [Fact]
    public void MergeWith_AddsBaseRulesNotInCustom()
    {
        // Arrange
        var baseRules = new List<StyleRule>
        {
            CreateRule("base-only"),
            CreateRule("shared"),
        };
        var customRules = new List<StyleRule>
        {
            CreateRule("custom-only"),
            CreateRule("shared"),
        };

        var baseSheet = new StyleSheet("Base", baseRules);
        var customSheet = new StyleSheet("Custom", customRules);

        // Act
        var merged = customSheet.MergeWith(baseSheet);

        // Assert
        merged.Rules.Should().HaveCount(3);
        merged.FindRuleById("base-only").Should().NotBeNull();
        merged.FindRuleById("custom-only").Should().NotBeNull();
        merged.FindRuleById("shared").Should().NotBeNull();
    }

    [Fact]
    public void MergeWith_ClearsExtendsProperty()
    {
        // Arrange
        var baseSheet = new StyleSheet("Base", []);
        var customSheet = new StyleSheet("Custom", [], Extends: "default");

        // Act
        var merged = customSheet.MergeWith(baseSheet);

        // Assert
        merged.Extends.Should().BeNull();
        merged.HasBaseSheet.Should().BeFalse();
    }

    [Fact]
    public void FindRuleById_IsCaseInsensitive()
    {
        // Arrange
        var rules = new List<StyleRule> { CreateRule("my-rule") };
        var sheet = new StyleSheet("Test", rules);

        // Act & Assert
        sheet.FindRuleById("MY-RULE").Should().NotBeNull();
        sheet.FindRuleById("My-Rule").Should().NotBeNull();
        sheet.FindRuleById("my-rule").Should().NotBeNull();
    }

    [Fact]
    public void FindRuleById_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var sheet = new StyleSheet("Test", []);

        // Act & Assert
        sheet.FindRuleById("nonexistent").Should().BeNull();
    }

    [Fact]
    public void DisableRule_CreatesSheetWithRuleDisabled()
    {
        // Arrange
        var rules = new List<StyleRule>
        {
            CreateRule("r1", isEnabled: true),
            CreateRule("r2", isEnabled: true),
        };
        var sheet = new StyleSheet("Test", rules);

        // Act
        var updated = sheet.DisableRule("r1");

        // Assert
        updated.FindRuleById("r1")!.IsEnabled.Should().BeFalse();
        updated.FindRuleById("r2")!.IsEnabled.Should().BeTrue();
        sheet.FindRuleById("r1")!.IsEnabled.Should().BeTrue(); // Original unchanged
    }

    [Fact]
    public void SetRuleSeverity_CreatesSheetWithUpdatedSeverity()
    {
        // Arrange
        var rules = new List<StyleRule>
        {
            CreateRule("r1", severity: ViolationSeverity.Warning),
        };
        var sheet = new StyleSheet("Test", rules);

        // Act
        var updated = sheet.SetRuleSeverity("r1", ViolationSeverity.Error);

        // Assert
        updated.FindRuleById("r1")!.DefaultSeverity.Should().Be(ViolationSeverity.Error);
        sheet.FindRuleById("r1")!.DefaultSeverity.Should().Be(ViolationSeverity.Warning);
    }

    [Fact]
    public void EnabledRuleCount_ReturnsCorrectCount()
    {
        // Arrange
        var rules = new List<StyleRule>
        {
            CreateRule("r1", isEnabled: true),
            CreateRule("r2", isEnabled: false),
            CreateRule("r3", isEnabled: true),
        };
        var sheet = new StyleSheet("Test", rules);

        // Assert
        sheet.EnabledRuleCount.Should().Be(2);
    }

    [Fact]
    public void HasBaseSheet_ReturnsTrueWhenExtendsSet()
    {
        // Arrange
        var sheetWithBase = new StyleSheet("Test", [], Extends: "default");
        var sheetWithoutBase = new StyleSheet("Test", []);

        // Assert
        sheetWithBase.HasBaseSheet.Should().BeTrue();
        sheetWithoutBase.HasBaseSheet.Should().BeFalse();
    }

    private static StyleRule CreateRule(
        string id,
        bool isEnabled = true,
        ViolationSeverity severity = ViolationSeverity.Warning,
        RuleCategory category = RuleCategory.Terminology)
    {
        return new StyleRule(
            Id: id,
            Name: $"Rule {id}",
            Description: "Test rule",
            Category: category,
            DefaultSeverity: severity,
            Pattern: "test",
            PatternType: PatternType.Literal,
            Suggestion: null,
            IsEnabled: isEnabled);
    }
}
