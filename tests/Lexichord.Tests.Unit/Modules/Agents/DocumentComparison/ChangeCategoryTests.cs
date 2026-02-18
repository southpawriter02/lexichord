// -----------------------------------------------------------------------
// <copyright file="ChangeCategoryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.DocumentComparison;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.DocumentComparison;

/// <summary>
/// Unit tests for <see cref="ChangeCategory"/> enum.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6d")]
public class ChangeCategoryTests
{
    // ── Enum Values ──────────────────────────────────────────────────────

    [Fact]
    public void ChangeCategory_HasEightValues()
    {
        // Act
        var values = Enum.GetValues<ChangeCategory>();

        // Assert
        values.Should().HaveCount(8);
    }

    [Fact]
    public void ChangeCategory_HasCorrectIntegerValues()
    {
        // Assert
        ((int)ChangeCategory.Added).Should().Be(0);
        ((int)ChangeCategory.Removed).Should().Be(1);
        ((int)ChangeCategory.Modified).Should().Be(2);
        ((int)ChangeCategory.Restructured).Should().Be(3);
        ((int)ChangeCategory.Clarified).Should().Be(4);
        ((int)ChangeCategory.Formatting).Should().Be(5);
        ((int)ChangeCategory.Correction).Should().Be(6);
        ((int)ChangeCategory.Terminology).Should().Be(7);
    }

    [Theory]
    [InlineData(ChangeCategory.Added)]
    [InlineData(ChangeCategory.Removed)]
    [InlineData(ChangeCategory.Modified)]
    [InlineData(ChangeCategory.Restructured)]
    [InlineData(ChangeCategory.Clarified)]
    [InlineData(ChangeCategory.Formatting)]
    [InlineData(ChangeCategory.Correction)]
    [InlineData(ChangeCategory.Terminology)]
    public void ChangeCategory_AllValuesAreDefined(ChangeCategory category)
    {
        // Assert
        Enum.IsDefined(category).Should().BeTrue();
    }

    // ── ToString ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ChangeCategory.Added, "Added")]
    [InlineData(ChangeCategory.Removed, "Removed")]
    [InlineData(ChangeCategory.Modified, "Modified")]
    [InlineData(ChangeCategory.Restructured, "Restructured")]
    [InlineData(ChangeCategory.Clarified, "Clarified")]
    [InlineData(ChangeCategory.Formatting, "Formatting")]
    [InlineData(ChangeCategory.Correction, "Correction")]
    [InlineData(ChangeCategory.Terminology, "Terminology")]
    public void ChangeCategory_ToStringReturnsName(ChangeCategory category, string expected)
    {
        // Act
        var result = category.ToString();

        // Assert
        result.Should().Be(expected);
    }

    // ── Parsing ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Added", ChangeCategory.Added)]
    [InlineData("Removed", ChangeCategory.Removed)]
    [InlineData("Modified", ChangeCategory.Modified)]
    [InlineData("Restructured", ChangeCategory.Restructured)]
    [InlineData("Clarified", ChangeCategory.Clarified)]
    [InlineData("Formatting", ChangeCategory.Formatting)]
    [InlineData("Correction", ChangeCategory.Correction)]
    [InlineData("Terminology", ChangeCategory.Terminology)]
    public void ChangeCategory_CanParseFromString(string name, ChangeCategory expected)
    {
        // Act
        var result = Enum.Parse<ChangeCategory>(name);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("added", ChangeCategory.Added)]
    [InlineData("REMOVED", ChangeCategory.Removed)]
    [InlineData("mOdIfIeD", ChangeCategory.Modified)]
    public void ChangeCategory_CanParseIgnoringCase(string name, ChangeCategory expected)
    {
        // Act
        var result = Enum.Parse<ChangeCategory>(name, ignoreCase: true);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ChangeCategory_ParseInvalidValue_ThrowsArgumentException()
    {
        // Act
        var act = () => Enum.Parse<ChangeCategory>("Invalid");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // ── TryParse ─────────────────────────────────────────────────────────

    [Fact]
    public void ChangeCategory_TryParseValidValue_ReturnsTrue()
    {
        // Act
        var success = Enum.TryParse<ChangeCategory>("Modified", out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(ChangeCategory.Modified);
    }

    [Fact]
    public void ChangeCategory_TryParseInvalidValue_ReturnsFalse()
    {
        // Act
        var success = Enum.TryParse<ChangeCategory>("NotACategory", out _);

        // Assert
        success.Should().BeFalse();
    }
}
