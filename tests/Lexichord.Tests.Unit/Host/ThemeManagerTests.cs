using Avalonia;
using Avalonia.Styling;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using LexThemeVariant = Lexichord.Abstractions.Contracts.ThemeVariant;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for ThemeManager.
/// </summary>
/// <remarks>
/// NOTE: These tests are limited because ThemeManager requires an Avalonia Application
/// context to fully function. Tests here focus on the state management and event
/// publishing logic that can be tested without a running Avalonia application.
/// </remarks>
public class ThemeManagerTests
{
    private readonly Mock<Application> _mockApplication = new();
    private readonly Mock<IMediator> _mockMediator = new();
    private readonly Mock<ILogger<ThemeManager>> _mockLogger = new();

    private ThemeManager CreateThemeManager()
    {
        return new ThemeManager(_mockApplication.Object, _mockLogger.Object, _mockMediator.Object);
    }

    #region SetThemeAsync Tests

    [Fact]
    public async Task SetThemeAsync_Light_SetsCurrentThemeToLight()
    {
        // Arrange
        var themeManager = CreateThemeManager();

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Light);

        // Assert
        Assert.Equal(ThemeMode.Light, themeManager.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_Dark_SetsCurrentThemeToDark()
    {
        // Arrange
        var themeManager = CreateThemeManager();

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Dark);

        // Assert
        Assert.Equal(ThemeMode.Dark, themeManager.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_System_SetsCurrentThemeToSystem()
    {
        // Arrange
        var themeManager = CreateThemeManager();

        // Act
        await themeManager.SetThemeAsync(ThemeMode.System);

        // Assert
        Assert.Equal(ThemeMode.System, themeManager.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_SameTheme_DoesNotRaiseEvent()
    {
        // Arrange
        var themeManager = CreateThemeManager();
        await themeManager.SetThemeAsync(ThemeMode.Light);

        var eventRaised = false;
        themeManager.ThemeChanged += (s, e) => eventRaised = true;

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Light);

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public async Task SetThemeAsync_DifferentTheme_RaisesThemeChangedEvent()
    {
        // Arrange
        var themeManager = CreateThemeManager();
        await themeManager.SetThemeAsync(ThemeMode.Light);

        ThemeChangedEventArgs? capturedArgs = null;
        themeManager.ThemeChanged += (s, e) => capturedArgs = e;

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Dark);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(ThemeMode.Light, capturedArgs.OldTheme);
        Assert.Equal(ThemeMode.Dark, capturedArgs.NewTheme);
    }

    [Fact]
    public async Task SetThemeAsync_DifferentTheme_PublishesMediatREvent()
    {
        // Arrange
        var themeManager = CreateThemeManager();

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Dark);

        // Assert
        _mockMediator.Verify(m => m.Publish(
            It.Is<ThemeChangedEvent>(e => e.NewTheme == ThemeMode.Dark),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetThemeAsync_SameTheme_DoesNotPublishMediatREvent()
    {
        // Arrange
        var themeManager = CreateThemeManager();
        await themeManager.SetThemeAsync(ThemeMode.Light);
        _mockMediator.Invocations.Clear();

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Light);

        // Assert
        _mockMediator.Verify(m => m.Publish(
            It.IsAny<ThemeChangedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetThemeAsync_EventArgs_ContainsCorrectEffectiveTheme()
    {
        // Arrange
        var themeManager = CreateThemeManager();

        ThemeChangedEventArgs? capturedArgs = null;
        themeManager.ThemeChanged += (s, e) => capturedArgs = e;

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Dark);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(LexThemeVariant.Dark, capturedArgs.EffectiveTheme);
    }

    [Fact]
    public async Task SetThemeAsync_Light_EffectiveThemeIsLight()
    {
        // Arrange
        var themeManager = CreateThemeManager();

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Light);

        // Assert
        Assert.Equal(LexThemeVariant.Light, themeManager.EffectiveTheme);
    }

    [Fact]
    public async Task SetThemeAsync_Dark_EffectiveThemeIsDark()
    {
        // Arrange
        var themeManager = CreateThemeManager();

        // Act
        await themeManager.SetThemeAsync(ThemeMode.Dark);

        // Assert
        Assert.Equal(LexThemeVariant.Dark, themeManager.EffectiveTheme);
    }

    #endregion

    #region EffectiveTheme Tests

    [Fact]
    public void EffectiveTheme_Initial_DefaultsToSystemLight()
    {
        // Arrange & Act
        var themeManager = CreateThemeManager();

        // Assert - When in System mode with no platform settings, defaults to Light
        Assert.Equal(LexThemeVariant.Light, themeManager.EffectiveTheme);
    }

    #endregion

    #region GetSystemTheme Tests

    [Fact]
    public void GetSystemTheme_NoPlatformSettings_ReturnsLight()
    {
        // Arrange
        var themeManager = CreateThemeManager();

        // Act
        var result = themeManager.GetSystemTheme();

        // Assert
        Assert.Equal(LexThemeVariant.Light, result);
    }

    #endregion

    #region CurrentTheme Tests

    [Fact]
    public void CurrentTheme_Initial_IsSystem()
    {
        // Arrange & Act
        var themeManager = CreateThemeManager();

        // Assert
        Assert.Equal(ThemeMode.System, themeManager.CurrentTheme);
    }

    #endregion
}
