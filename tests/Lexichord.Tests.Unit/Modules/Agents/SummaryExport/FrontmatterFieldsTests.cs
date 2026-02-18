// -----------------------------------------------------------------------
// <copyright file="FrontmatterFieldsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.SummaryExport;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.SummaryExport;

/// <summary>
/// Unit tests for <see cref="FrontmatterFields"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6c")]
public class FrontmatterFieldsTests
{
    // ── None Value ──────────────────────────────────────────────────────

    [Fact]
    public void None_HasValueZero()
    {
        // Assert
        ((int)FrontmatterFields.None).Should().Be(0);
    }

    [Fact]
    public void None_HasNoFlags()
    {
        // Arrange
        var fields = FrontmatterFields.None;

        // Assert
        fields.HasFlag(FrontmatterFields.Abstract).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.Tags).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.KeyTerms).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.ReadingTime).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.Category).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.Audience).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.GeneratedAt).Should().BeFalse();
    }

    // ── Individual Flags ────────────────────────────────────────────────

    [Theory]
    [InlineData(FrontmatterFields.Abstract, 1)]
    [InlineData(FrontmatterFields.Tags, 2)]
    [InlineData(FrontmatterFields.KeyTerms, 4)]
    [InlineData(FrontmatterFields.ReadingTime, 8)]
    [InlineData(FrontmatterFields.Category, 16)]
    [InlineData(FrontmatterFields.Audience, 32)]
    [InlineData(FrontmatterFields.GeneratedAt, 64)]
    public void IndividualFlags_HaveCorrectValues(FrontmatterFields flag, int expectedValue)
    {
        // Assert
        ((int)flag).Should().Be(expectedValue);
    }

    [Fact]
    public void IndividualFlags_ArePowersOfTwo()
    {
        // Arrange
        var flags = new[]
        {
            FrontmatterFields.Abstract,
            FrontmatterFields.Tags,
            FrontmatterFields.KeyTerms,
            FrontmatterFields.ReadingTime,
            FrontmatterFields.Category,
            FrontmatterFields.Audience,
            FrontmatterFields.GeneratedAt
        };

        // Assert
        foreach (var flag in flags)
        {
            var value = (int)flag;
            var isPowerOfTwo = value > 0 && (value & (value - 1)) == 0;
            isPowerOfTwo.Should().BeTrue($"{flag} should be a power of two");
        }
    }

    // ── All Value ───────────────────────────────────────────────────────

    [Fact]
    public void All_ContainsAllFlags()
    {
        // Arrange
        var all = FrontmatterFields.All;

        // Assert
        all.HasFlag(FrontmatterFields.Abstract).Should().BeTrue();
        all.HasFlag(FrontmatterFields.Tags).Should().BeTrue();
        all.HasFlag(FrontmatterFields.KeyTerms).Should().BeTrue();
        all.HasFlag(FrontmatterFields.ReadingTime).Should().BeTrue();
        all.HasFlag(FrontmatterFields.Category).Should().BeTrue();
        all.HasFlag(FrontmatterFields.Audience).Should().BeTrue();
        all.HasFlag(FrontmatterFields.GeneratedAt).Should().BeTrue();
    }

    [Fact]
    public void All_EqualsUnionOfAllFlags()
    {
        // Arrange
        var expected = FrontmatterFields.Abstract |
                       FrontmatterFields.Tags |
                       FrontmatterFields.KeyTerms |
                       FrontmatterFields.ReadingTime |
                       FrontmatterFields.Category |
                       FrontmatterFields.Audience |
                       FrontmatterFields.GeneratedAt;

        // Assert
        FrontmatterFields.All.Should().Be(expected);
    }

    // ── Flag Combinations ───────────────────────────────────────────────

    [Fact]
    public void Flags_CanBeCombinedWithBitwiseOr()
    {
        // Arrange
        var combined = FrontmatterFields.Abstract | FrontmatterFields.Tags;

        // Assert
        combined.HasFlag(FrontmatterFields.Abstract).Should().BeTrue();
        combined.HasFlag(FrontmatterFields.Tags).Should().BeTrue();
        combined.HasFlag(FrontmatterFields.KeyTerms).Should().BeFalse();
    }

    [Fact]
    public void Flags_CanBeRemovedWithBitwiseAnd()
    {
        // Arrange
        var fields = FrontmatterFields.All;

        // Act — Remove Tags
        var result = fields & ~FrontmatterFields.Tags;

        // Assert
        result.HasFlag(FrontmatterFields.Abstract).Should().BeTrue();
        result.HasFlag(FrontmatterFields.Tags).Should().BeFalse();
        result.HasFlag(FrontmatterFields.KeyTerms).Should().BeTrue();
    }

    [Fact]
    public void Flags_CanBeTestedWithHasFlag()
    {
        // Arrange
        var fields = FrontmatterFields.Abstract | FrontmatterFields.ReadingTime | FrontmatterFields.Audience;

        // Assert
        fields.HasFlag(FrontmatterFields.Abstract).Should().BeTrue();
        fields.HasFlag(FrontmatterFields.ReadingTime).Should().BeTrue();
        fields.HasFlag(FrontmatterFields.Audience).Should().BeTrue();
        fields.HasFlag(FrontmatterFields.Tags).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.KeyTerms).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.Category).Should().BeFalse();
        fields.HasFlag(FrontmatterFields.GeneratedAt).Should().BeFalse();
    }

    // ── Enum Attributes ─────────────────────────────────────────────────

    [Fact]
    public void Enum_HasFlagsAttribute()
    {
        // Arrange
        var type = typeof(FrontmatterFields);

        // Assert
        type.IsDefined(typeof(FlagsAttribute), inherit: false).Should().BeTrue();
    }
}
