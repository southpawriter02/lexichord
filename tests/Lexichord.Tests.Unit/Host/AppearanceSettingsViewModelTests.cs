using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for AppearanceSettingsViewModel.
/// </summary>
public class AppearanceSettingsViewModelTests
{
    private readonly Mock<IThemeManager> _mockThemeManager = new();
    private readonly Mock<ILogger<AppearanceSettingsViewModel>> _mockLogger = new();

    private AppearanceSettingsViewModel CreateViewModel()
    {
        return new AppearanceSettingsViewModel(
            _mockThemeManager.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithLightTheme_WhenCurrentThemeIsLight()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.IsLightSelected);
        Assert.False(viewModel.IsDarkSelected);
        Assert.False(viewModel.IsSystemSelected);
        Assert.Equal(ThemeMode.Light, viewModel.SelectedTheme);
    }

    [Fact]
    public void Constructor_InitializesWithDarkTheme_WhenCurrentThemeIsDark()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Dark);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsLightSelected);
        Assert.True(viewModel.IsDarkSelected);
        Assert.False(viewModel.IsSystemSelected);
        Assert.Equal(ThemeMode.Dark, viewModel.SelectedTheme);
    }

    [Fact]
    public void Constructor_InitializesWithSystemTheme_WhenCurrentThemeIsSystem()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.System);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsLightSelected);
        Assert.False(viewModel.IsDarkSelected);
        Assert.True(viewModel.IsSystemSelected);
        Assert.Equal(ThemeMode.System, viewModel.SelectedTheme);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenThemeManagerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AppearanceSettingsViewModel(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AppearanceSettingsViewModel(_mockThemeManager.Object, null!));
    }

    #endregion

    #region SelectLightThemeCommand Tests

    [Fact]
    public async Task SelectLightThemeCommand_ExecutesThemeChange()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.System);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectLightThemeCommand.ExecuteAsync(null);

        // Assert
        _mockThemeManager.Verify(t => t.SetThemeAsync(ThemeMode.Light), Times.Once);
    }

    [Fact]
    public async Task SelectLightThemeCommand_UpdatesSelectionState()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Dark);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectLightThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.IsLightSelected);
        Assert.False(viewModel.IsDarkSelected);
        Assert.False(viewModel.IsSystemSelected);
    }

    [Fact]
    public async Task SelectLightThemeCommand_DoesNotCallSetTheme_WhenAlreadyLightSelected()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);
        var viewModel = CreateViewModel();
        _mockThemeManager.Invocations.Clear();

        // Act
        await viewModel.SelectLightThemeCommand.ExecuteAsync(null);

        // Assert
        _mockThemeManager.Verify(t => t.SetThemeAsync(It.IsAny<ThemeMode>()), Times.Never);
    }

    #endregion

    #region SelectDarkThemeCommand Tests

    [Fact]
    public async Task SelectDarkThemeCommand_ExecutesThemeChange()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.System);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectDarkThemeCommand.ExecuteAsync(null);

        // Assert
        _mockThemeManager.Verify(t => t.SetThemeAsync(ThemeMode.Dark), Times.Once);
    }

    [Fact]
    public async Task SelectDarkThemeCommand_UpdatesSelectionState()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectDarkThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.False(viewModel.IsLightSelected);
        Assert.True(viewModel.IsDarkSelected);
        Assert.False(viewModel.IsSystemSelected);
    }

    [Fact]
    public async Task SelectDarkThemeCommand_DoesNotCallSetTheme_WhenAlreadyDarkSelected()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Dark);
        var viewModel = CreateViewModel();
        _mockThemeManager.Invocations.Clear();

        // Act
        await viewModel.SelectDarkThemeCommand.ExecuteAsync(null);

        // Assert
        _mockThemeManager.Verify(t => t.SetThemeAsync(It.IsAny<ThemeMode>()), Times.Never);
    }

    #endregion

    #region SelectSystemThemeCommand Tests

    [Fact]
    public async Task SelectSystemThemeCommand_ExecutesThemeChange()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectSystemThemeCommand.ExecuteAsync(null);

        // Assert
        _mockThemeManager.Verify(t => t.SetThemeAsync(ThemeMode.System), Times.Once);
    }

    [Fact]
    public async Task SelectSystemThemeCommand_UpdatesSelectionState()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectSystemThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.False(viewModel.IsLightSelected);
        Assert.False(viewModel.IsDarkSelected);
        Assert.True(viewModel.IsSystemSelected);
    }

    [Fact]
    public async Task SelectSystemThemeCommand_DoesNotCallSetTheme_WhenAlreadySystemSelected()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.System);
        var viewModel = CreateViewModel();
        _mockThemeManager.Invocations.Clear();

        // Act
        await viewModel.SelectSystemThemeCommand.ExecuteAsync(null);

        // Assert
        _mockThemeManager.Verify(t => t.SetThemeAsync(It.IsAny<ThemeMode>()), Times.Never);
    }

    #endregion

    #region SelectedTheme Property Tests

    [Fact]
    public void SelectedTheme_ReturnsLight_WhenIsLightSelectedIsTrue()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.Equal(ThemeMode.Light, viewModel.SelectedTheme);
    }

    [Fact]
    public void SelectedTheme_ReturnsDark_WhenIsDarkSelectedIsTrue()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Dark);
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.Equal(ThemeMode.Dark, viewModel.SelectedTheme);
    }

    [Fact]
    public void SelectedTheme_ReturnsSystem_WhenIsSystemSelectedIsTrue()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.System);
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.Equal(ThemeMode.System, viewModel.SelectedTheme);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SelectTheme_RevertsSelection_WhenThemeManagerThrows()
    {
        // Arrange
        _mockThemeManager.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);
        _mockThemeManager.Setup(t => t.SetThemeAsync(ThemeMode.Dark))
            .ThrowsAsync(new Exception("Theme application failed"));
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectDarkThemeCommand.ExecuteAsync(null);

        // Assert - Selection should revert to Light (current theme from manager)
        Assert.True(viewModel.IsLightSelected);
        Assert.False(viewModel.IsDarkSelected);
    }

    #endregion
}
