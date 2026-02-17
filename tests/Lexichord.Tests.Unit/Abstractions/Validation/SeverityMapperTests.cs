// -----------------------------------------------------------------------
// <copyright file="SeverityMapperTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;

namespace Lexichord.Tests.Unit.Abstractions.Validation;

/// <summary>
/// Unit tests for <see cref="SeverityMapper"/> static helper class.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>FromViolationSeverity — Direct mapping tests</description></item>
///   <item><description>FromValidationSeverity — Inverted mapping tests</description></item>
///   <item><description>FromDeviationPriority — Reverse mapping tests</description></item>
///   <item><description>ToViolationSeverity — Reverse direct mapping tests</description></item>
///   <item><description>ToValidationSeverity — Reverse inverted mapping tests</description></item>
///   <item><description>ToDeviationPriority — Reverse mapping tests</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5e")]
public class SeverityMapperTests
{
    #region FromViolationSeverity Tests

    /// <summary>
    /// Verifies that ViolationSeverity.Error maps to UnifiedSeverity.Error.
    /// </summary>
    [Fact]
    public void FromViolationSeverity_Error_MapsToError()
    {
        // Act
        var result = SeverityMapper.FromViolationSeverity(ViolationSeverity.Error);

        // Assert
        result.Should().Be(UnifiedSeverity.Error);
    }

    /// <summary>
    /// Verifies that ViolationSeverity.Warning maps to UnifiedSeverity.Warning.
    /// </summary>
    [Fact]
    public void FromViolationSeverity_Warning_MapsToWarning()
    {
        // Act
        var result = SeverityMapper.FromViolationSeverity(ViolationSeverity.Warning);

        // Assert
        result.Should().Be(UnifiedSeverity.Warning);
    }

    /// <summary>
    /// Verifies that ViolationSeverity.Info maps to UnifiedSeverity.Info.
    /// </summary>
    [Fact]
    public void FromViolationSeverity_Info_MapsToInfo()
    {
        // Act
        var result = SeverityMapper.FromViolationSeverity(ViolationSeverity.Info);

        // Assert
        result.Should().Be(UnifiedSeverity.Info);
    }

    /// <summary>
    /// Verifies that ViolationSeverity.Hint maps to UnifiedSeverity.Hint.
    /// </summary>
    [Fact]
    public void FromViolationSeverity_Hint_MapsToHint()
    {
        // Act
        var result = SeverityMapper.FromViolationSeverity(ViolationSeverity.Hint);

        // Assert
        result.Should().Be(UnifiedSeverity.Hint);
    }

    #endregion

    #region FromValidationSeverity Tests

    /// <summary>
    /// Verifies that ValidationSeverity.Error maps to UnifiedSeverity.Error.
    /// </summary>
    [Fact]
    public void FromValidationSeverity_Error_MapsToError()
    {
        // Act
        var result = SeverityMapper.FromValidationSeverity(ValidationSeverity.Error);

        // Assert
        result.Should().Be(UnifiedSeverity.Error);
    }

    /// <summary>
    /// Verifies that ValidationSeverity.Warning maps to UnifiedSeverity.Warning.
    /// </summary>
    [Fact]
    public void FromValidationSeverity_Warning_MapsToWarning()
    {
        // Act
        var result = SeverityMapper.FromValidationSeverity(ValidationSeverity.Warning);

        // Assert
        result.Should().Be(UnifiedSeverity.Warning);
    }

    /// <summary>
    /// Verifies that ValidationSeverity.Info maps to UnifiedSeverity.Info.
    /// </summary>
    [Fact]
    public void FromValidationSeverity_Info_MapsToInfo()
    {
        // Act
        var result = SeverityMapper.FromValidationSeverity(ValidationSeverity.Info);

        // Assert
        result.Should().Be(UnifiedSeverity.Info);
    }

    #endregion

    #region FromDeviationPriority Tests

    /// <summary>
    /// Verifies that DeviationPriority.Critical maps to UnifiedSeverity.Error.
    /// </summary>
    [Fact]
    public void FromDeviationPriority_Critical_MapsToError()
    {
        // Act
        var result = SeverityMapper.FromDeviationPriority(DeviationPriority.Critical);

        // Assert
        result.Should().Be(UnifiedSeverity.Error);
    }

    /// <summary>
    /// Verifies that DeviationPriority.High maps to UnifiedSeverity.Warning.
    /// </summary>
    [Fact]
    public void FromDeviationPriority_High_MapsToWarning()
    {
        // Act
        var result = SeverityMapper.FromDeviationPriority(DeviationPriority.High);

        // Assert
        result.Should().Be(UnifiedSeverity.Warning);
    }

    /// <summary>
    /// Verifies that DeviationPriority.Normal maps to UnifiedSeverity.Info.
    /// </summary>
    [Fact]
    public void FromDeviationPriority_Normal_MapsToInfo()
    {
        // Act
        var result = SeverityMapper.FromDeviationPriority(DeviationPriority.Normal);

        // Assert
        result.Should().Be(UnifiedSeverity.Info);
    }

    /// <summary>
    /// Verifies that DeviationPriority.Low maps to UnifiedSeverity.Hint.
    /// </summary>
    [Fact]
    public void FromDeviationPriority_Low_MapsToHint()
    {
        // Act
        var result = SeverityMapper.FromDeviationPriority(DeviationPriority.Low);

        // Assert
        result.Should().Be(UnifiedSeverity.Hint);
    }

    #endregion

    #region ToViolationSeverity Tests

    /// <summary>
    /// Verifies that UnifiedSeverity.Error maps to ViolationSeverity.Error.
    /// </summary>
    [Fact]
    public void ToViolationSeverity_Error_MapsToError()
    {
        // Act
        var result = SeverityMapper.ToViolationSeverity(UnifiedSeverity.Error);

        // Assert
        result.Should().Be(ViolationSeverity.Error);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Warning maps to ViolationSeverity.Warning.
    /// </summary>
    [Fact]
    public void ToViolationSeverity_Warning_MapsToWarning()
    {
        // Act
        var result = SeverityMapper.ToViolationSeverity(UnifiedSeverity.Warning);

        // Assert
        result.Should().Be(ViolationSeverity.Warning);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Info maps to ViolationSeverity.Info.
    /// </summary>
    [Fact]
    public void ToViolationSeverity_Info_MapsToInfo()
    {
        // Act
        var result = SeverityMapper.ToViolationSeverity(UnifiedSeverity.Info);

        // Assert
        result.Should().Be(ViolationSeverity.Info);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Hint maps to ViolationSeverity.Hint.
    /// </summary>
    [Fact]
    public void ToViolationSeverity_Hint_MapsToHint()
    {
        // Act
        var result = SeverityMapper.ToViolationSeverity(UnifiedSeverity.Hint);

        // Assert
        result.Should().Be(ViolationSeverity.Hint);
    }

    #endregion

    #region ToValidationSeverity Tests

    /// <summary>
    /// Verifies that UnifiedSeverity.Error maps to ValidationSeverity.Error.
    /// </summary>
    [Fact]
    public void ToValidationSeverity_Error_MapsToError()
    {
        // Act
        var result = SeverityMapper.ToValidationSeverity(UnifiedSeverity.Error);

        // Assert
        result.Should().Be(ValidationSeverity.Error);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Warning maps to ValidationSeverity.Warning.
    /// </summary>
    [Fact]
    public void ToValidationSeverity_Warning_MapsToWarning()
    {
        // Act
        var result = SeverityMapper.ToValidationSeverity(UnifiedSeverity.Warning);

        // Assert
        result.Should().Be(ValidationSeverity.Warning);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Info maps to ValidationSeverity.Info.
    /// </summary>
    [Fact]
    public void ToValidationSeverity_Info_MapsToInfo()
    {
        // Act
        var result = SeverityMapper.ToValidationSeverity(UnifiedSeverity.Info);

        // Assert
        result.Should().Be(ValidationSeverity.Info);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Hint maps to ValidationSeverity.Info (downgrade).
    /// </summary>
    [Fact]
    public void ToValidationSeverity_Hint_MapsToInfo()
    {
        // Act
        var result = SeverityMapper.ToValidationSeverity(UnifiedSeverity.Hint);

        // Assert
        // Note: ValidationSeverity doesn't have Hint, so it downgrades to Info
        result.Should().Be(ValidationSeverity.Info);
    }

    #endregion

    #region ToDeviationPriority Tests

    /// <summary>
    /// Verifies that UnifiedSeverity.Error maps to DeviationPriority.Critical.
    /// </summary>
    [Fact]
    public void ToDeviationPriority_Error_MapsToCritical()
    {
        // Act
        var result = SeverityMapper.ToDeviationPriority(UnifiedSeverity.Error);

        // Assert
        result.Should().Be(DeviationPriority.Critical);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Warning maps to DeviationPriority.High.
    /// </summary>
    [Fact]
    public void ToDeviationPriority_Warning_MapsToHigh()
    {
        // Act
        var result = SeverityMapper.ToDeviationPriority(UnifiedSeverity.Warning);

        // Assert
        result.Should().Be(DeviationPriority.High);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Info maps to DeviationPriority.Normal.
    /// </summary>
    [Fact]
    public void ToDeviationPriority_Info_MapsToNormal()
    {
        // Act
        var result = SeverityMapper.ToDeviationPriority(UnifiedSeverity.Info);

        // Assert
        result.Should().Be(DeviationPriority.Normal);
    }

    /// <summary>
    /// Verifies that UnifiedSeverity.Hint maps to DeviationPriority.Low.
    /// </summary>
    [Fact]
    public void ToDeviationPriority_Hint_MapsToLow()
    {
        // Act
        var result = SeverityMapper.ToDeviationPriority(UnifiedSeverity.Hint);

        // Assert
        result.Should().Be(DeviationPriority.Low);
    }

    #endregion

    #region Round-Trip Tests

    /// <summary>
    /// Verifies that ViolationSeverity round-trips correctly.
    /// </summary>
    [Theory]
    [InlineData(ViolationSeverity.Error)]
    [InlineData(ViolationSeverity.Warning)]
    [InlineData(ViolationSeverity.Info)]
    [InlineData(ViolationSeverity.Hint)]
    public void ViolationSeverity_RoundTrips_Correctly(ViolationSeverity original)
    {
        // Act
        var unified = SeverityMapper.FromViolationSeverity(original);
        var roundTripped = SeverityMapper.ToViolationSeverity(unified);

        // Assert
        roundTripped.Should().Be(original);
    }

    /// <summary>
    /// Verifies that DeviationPriority round-trips correctly.
    /// </summary>
    [Theory]
    [InlineData(DeviationPriority.Critical)]
    [InlineData(DeviationPriority.High)]
    [InlineData(DeviationPriority.Normal)]
    [InlineData(DeviationPriority.Low)]
    public void DeviationPriority_RoundTrips_Correctly(DeviationPriority original)
    {
        // Act
        var unified = SeverityMapper.FromDeviationPriority(original);
        var roundTripped = SeverityMapper.ToDeviationPriority(unified);

        // Assert
        roundTripped.Should().Be(original);
    }

    #endregion
}
