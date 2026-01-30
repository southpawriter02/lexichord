using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style.Rendering;

/// <summary>
/// Unit tests for <see cref="ViolationColorProvider"/>.
/// </summary>
public sealed class ViolationColorProviderTests
{
    private ViolationColorProvider _sut = null!;

    public ViolationColorProviderTests()
    {
        _sut = new ViolationColorProvider();
    }

    #region Light Theme Underline Colors

    [Fact]
    public void GetUnderlineColor_LightTheme_Error_ReturnsLightError()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Error);

        // Assert
        Assert.Equal(UnderlineColor.LightError, color);
    }

    [Fact]
    public void GetUnderlineColor_LightTheme_Warning_ReturnsLightWarning()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Warning);

        // Assert
        Assert.Equal(UnderlineColor.LightWarning, color);
    }

    [Fact]
    public void GetUnderlineColor_LightTheme_Info_ReturnsLightInfo()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Info);

        // Assert
        Assert.Equal(UnderlineColor.LightInfo, color);
    }

    [Fact]
    public void GetUnderlineColor_LightTheme_Hint_ReturnsLightHint()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Hint);

        // Assert
        Assert.Equal(UnderlineColor.LightHint, color);
    }

    #endregion

    #region Dark Theme Underline Colors

    [Fact]
    public void GetUnderlineColor_DarkTheme_Error_ReturnsDarkError()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Dark);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Error);

        // Assert
        Assert.Equal(UnderlineColor.DarkError, color);
    }

    [Fact]
    public void GetUnderlineColor_DarkTheme_Warning_ReturnsDarkWarning()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Dark);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Warning);

        // Assert
        Assert.Equal(UnderlineColor.DarkWarning, color);
    }

    [Fact]
    public void GetUnderlineColor_DarkTheme_Info_ReturnsDarkInfo()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Dark);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Info);

        // Assert
        Assert.Equal(UnderlineColor.DarkInfo, color);
    }

    [Fact]
    public void GetUnderlineColor_DarkTheme_Hint_ReturnsDarkHint()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Dark);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Hint);

        // Assert
        Assert.Equal(UnderlineColor.DarkHint, color);
    }

    #endregion

    #region Theme Switching

    [Fact]
    public void SetTheme_SwitchesBetweenThemes()
    {
        // Arrange - start with light theme
        _sut.SetTheme(ThemeVariant.Light);
        var lightColor = _sut.GetUnderlineColor(ViolationSeverity.Error);

        // Act - switch to dark theme
        _sut.SetTheme(ThemeVariant.Dark);
        var darkColor = _sut.GetUnderlineColor(ViolationSeverity.Error);

        // Assert - colors should be different
        Assert.NotEqual(lightColor, darkColor);
        Assert.Equal(UnderlineColor.LightError, lightColor);
        Assert.Equal(UnderlineColor.DarkError, darkColor);
    }

    [Fact]
    public void Constructor_WithThemeManager_UsesEffectiveTheme()
    {
        // Arrange
        var mockThemeManager = new Mock<IThemeManager>();
        mockThemeManager.Setup(t => t.EffectiveTheme).Returns(ThemeVariant.Dark);

        // Act
        var provider = new ViolationColorProvider(mockThemeManager.Object);
        var color = provider.GetUnderlineColor(ViolationSeverity.Error);

        // Assert - should use dark theme color
        Assert.Equal(UnderlineColor.DarkError, color);
    }

    #endregion

    #region Background Colors

    [Fact]
    public void GetBackgroundColor_LightTheme_ReturnsTranslucentColor()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);

        // Act
        var color = _sut.GetBackgroundColor(ViolationSeverity.Error);

        // Assert
        Assert.NotNull(color);
        Assert.True(color.Value.A < 255); // Should be translucent
        Assert.Equal(0x20, color.Value.A); // ~12% opacity
    }

    [Fact]
    public void GetBackgroundColor_DarkTheme_ReturnsTranslucentColor()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Dark);

        // Act
        var color = _sut.GetBackgroundColor(ViolationSeverity.Error);

        // Assert
        Assert.NotNull(color);
        Assert.True(color.Value.A < 255); // Should be translucent
        Assert.Equal(0x30, color.Value.A); // ~19% opacity
    }

    [Theory]
    [InlineData(ViolationSeverity.Error)]
    [InlineData(ViolationSeverity.Warning)]
    [InlineData(ViolationSeverity.Info)]
    [InlineData(ViolationSeverity.Hint)]
    public void GetBackgroundColor_AllSeverities_ReturnsValue(ViolationSeverity severity)
    {
        // Act
        var color = _sut.GetBackgroundColor(severity);

        // Assert
        Assert.NotNull(color);
    }

    #endregion

    #region Tooltip Border Colors

    [Fact]
    public void GetTooltipBorderColor_ReturnsSameAsUnderlineColor()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);

        // Act
        var underlineColor = _sut.GetUnderlineColor(ViolationSeverity.Error);
        var borderColor = _sut.GetTooltipBorderColor(ViolationSeverity.Error);

        // Assert - should be the same
        Assert.Equal(underlineColor, borderColor);
    }

    #endregion

    #region Severity Icons

    [Theory]
    [InlineData(ViolationSeverity.Error)]
    [InlineData(ViolationSeverity.Warning)]
    [InlineData(ViolationSeverity.Info)]
    [InlineData(ViolationSeverity.Hint)]
    public void GetSeverityIcon_AllSeverities_ReturnsNonEmptyPath(ViolationSeverity severity)
    {
        // Act
        var iconPath = _sut.GetSeverityIcon(severity);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(iconPath));
        Assert.Contains("M", iconPath); // SVG paths start with M (MoveTo)
    }

    [Fact]
    public void GetSeverityIcon_DifferentSeverities_ReturnDifferentIcons()
    {
        // Act
        var errorIcon = _sut.GetSeverityIcon(ViolationSeverity.Error);
        var warningIcon = _sut.GetSeverityIcon(ViolationSeverity.Warning);
        var infoIcon = _sut.GetSeverityIcon(ViolationSeverity.Info);

        // Assert - each severity should have a unique icon
        Assert.NotEqual(errorIcon, warningIcon);
        Assert.NotEqual(warningIcon, infoIcon);
        Assert.NotEqual(errorIcon, infoIcon);
    }

    #endregion

    #region Color Value Verification

    [Fact]
    public void GetUnderlineColor_LightError_HasExpectedRgbValues()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);
        var expected = new UnderlineColor(0xE5, 0x14, 0x00);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Error);

        // Assert
        Assert.Equal(expected.R, color.R);
        Assert.Equal(expected.G, color.G);
        Assert.Equal(expected.B, color.B);
    }

    [Fact]
    public void GetUnderlineColor_DarkError_HasExpectedRgbValues()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Dark);
        var expected = new UnderlineColor(0xFF, 0x6B, 0x6B);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Error);

        // Assert
        Assert.Equal(expected.R, color.R);
        Assert.Equal(expected.G, color.G);
        Assert.Equal(expected.B, color.B);
    }

    [Fact]
    public void GetUnderlineColor_LightWarning_HasExpectedRgbValues()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);
        var expected = new UnderlineColor(0xF0, 0xA3, 0x0A);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Warning);

        // Assert
        Assert.Equal(expected.R, color.R);
        Assert.Equal(expected.G, color.G);
        Assert.Equal(expected.B, color.B);
    }

    [Fact]
    public void GetUnderlineColor_LightInfo_HasExpectedRgbValues()
    {
        // Arrange
        _sut.SetTheme(ThemeVariant.Light);
        var expected = new UnderlineColor(0x00, 0x78, 0xD4);

        // Act
        var color = _sut.GetUnderlineColor(ViolationSeverity.Info);

        // Assert
        Assert.Equal(expected.R, color.R);
        Assert.Equal(expected.G, color.G);
        Assert.Equal(expected.B, color.B);
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

        // Assert - verify it's not a default/empty color (R, G, B should be set)
        Assert.True(color.R != 0 || color.G != 0 || color.B != 0);
        Assert.Equal(255, color.A); // Full opacity for underlines
    }

    #endregion
}
