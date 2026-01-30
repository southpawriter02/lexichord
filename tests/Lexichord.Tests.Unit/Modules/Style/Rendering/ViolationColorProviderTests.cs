using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;

namespace Lexichord.Tests.Unit.Modules.Style.Rendering;

/// <summary>
/// Unit tests for <see cref="ViolationColorProvider"/>.
/// </summary>
public sealed class ViolationColorProviderTests
{
    private readonly ViolationColorProvider _sut = new();

    [Fact]
    public void GetUnderlineColor_Error_ReturnsErrorRed()
    {
        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Error);

        // Assert
        Assert.Equal(UnderlineColor.ErrorRed, color);
    }

    [Fact]
    public void GetUnderlineColor_Warning_ReturnsWarningYellow()
    {
        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Warning);

        // Assert
        Assert.Equal(UnderlineColor.WarningYellow, color);
    }

    [Fact]
    public void GetUnderlineColor_Info_ReturnsInfoBlue()
    {
        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Info);

        // Assert
        Assert.Equal(UnderlineColor.InfoBlue, color);
    }

    [Fact]
    public void GetUnderlineColor_Hint_ReturnsHintGray()
    {
        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Hint);

        // Assert
        Assert.Equal(UnderlineColor.HintGray, color);
    }

    [Theory]
    [InlineData(ViolationSeverity.Error)]
    [InlineData(ViolationSeverity.Warning)]
    [InlineData(ViolationSeverity.Info)]
    [InlineData(ViolationSeverity.Hint)]
    public void GetUnderlineColor_AllSeverities_ReturnsNonDefaultColor(ViolationSeverity severity)
    {
        // Act
        var color = _sut.GetUnderlineColor(severity);

        // Assert
        // Verify it's not a default/empty color (R, G, B should be set)
        Assert.True(color.R != 0 || color.G != 0 || color.B != 0);
        Assert.Equal(255, color.A); // Full opacity
    }

    [Fact]
    public void GetUnderlineColor_ErrorRed_HasExpectedRgbValues()
    {
        // Arrange
        var expected = new UnderlineColor(0xE5, 0x14, 0x00);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Error);

        // Assert
        Assert.Equal(expected.R, color.R);
        Assert.Equal(expected.G, color.G);
        Assert.Equal(expected.B, color.B);
    }

    [Fact]
    public void GetUnderlineColor_WarningYellow_HasExpectedRgbValues()
    {
        // Arrange
        var expected = new UnderlineColor(0xFF, 0xC0, 0x00);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Warning);

        // Assert
        Assert.Equal(expected.R, color.R);
        Assert.Equal(expected.G, color.G);
        Assert.Equal(expected.B, color.B);
    }
}
