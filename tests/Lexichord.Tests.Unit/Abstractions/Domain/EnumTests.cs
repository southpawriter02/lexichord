using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Domain;

/// <summary>
/// Unit tests for Style module enums.
/// </summary>
/// <remarks>
/// LOGIC: Verifies enum values match the design specification.
/// </remarks>
public class EnumTests
{
    [Fact]
    public void RuleCategory_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RuleCategory), 0)); // Terminology
        Assert.True(Enum.IsDefined(typeof(RuleCategory), 1)); // Formatting
        Assert.True(Enum.IsDefined(typeof(RuleCategory), 2)); // Syntax
    }

    [Fact]
    public void RuleCategory_HasThreeValues()
    {
        // Assert
        var values = Enum.GetValues<RuleCategory>();
        values.Should().HaveCount(3);
    }

    [Fact]
    public void ViolationSeverity_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ViolationSeverity), 0)); // Error
        Assert.True(Enum.IsDefined(typeof(ViolationSeverity), 1)); // Warning
        Assert.True(Enum.IsDefined(typeof(ViolationSeverity), 2)); // Info
        Assert.True(Enum.IsDefined(typeof(ViolationSeverity), 3)); // Hint
    }

    [Fact]
    public void ViolationSeverity_OrderedByImportance()
    {
        // LOGIC: Error (0) is most severe, Hint (3) is least severe
        // Cast to int for comparison
        ((int)ViolationSeverity.Error).Should().BeLessThan((int)ViolationSeverity.Warning);
        ((int)ViolationSeverity.Warning).Should().BeLessThan((int)ViolationSeverity.Info);
        ((int)ViolationSeverity.Info).Should().BeLessThan((int)ViolationSeverity.Hint);
    }

    [Fact]
    public void PatternType_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(PatternType), 0)); // Regex
        Assert.True(Enum.IsDefined(typeof(PatternType), 1)); // Literal
        Assert.True(Enum.IsDefined(typeof(PatternType), 2)); // LiteralIgnoreCase
        Assert.True(Enum.IsDefined(typeof(PatternType), 3)); // StartsWith
        Assert.True(Enum.IsDefined(typeof(PatternType), 4)); // EndsWith
        Assert.True(Enum.IsDefined(typeof(PatternType), 5)); // Contains
    }

    [Fact]
    public void PatternType_HasSixValues()
    {
        // Assert
        var values = Enum.GetValues<PatternType>();
        values.Should().HaveCount(6);
    }
}
